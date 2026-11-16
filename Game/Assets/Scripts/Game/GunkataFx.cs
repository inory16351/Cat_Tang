// 건카타 총구 폭발 연출 (참고: 몸 둘레 여러 곳에서 동시에 뭉게뭉게 터지는 불덩이 뭉치 + 가는 빛줄기 + 붉은 흩뿌림
// + 가운데 쌓이는 회색 연기 + 몸 주위를 도는 어두운 소용돌이 궤적). 스프라이트는 docs/tools/fx_sprites.py가 만듦.
// - Burst : 총구 한 발. 불덩이 뭉치 2~3개가 바깥으로 줄지어 부풀고, 하얀 빛줄기 + (본체) 붉은 흩뿌림
// - Smoke : 회색 연기 뭉치 (천천히 부풀며 떠오르다 사라짐)
// - Swirl : 몸 주위를 기울어진 원으로 빠르게 도는 어두운 곡선 (회전감)
using UnityEngine;

namespace CatTang
{
    public static class GunkataFx
    {
        static Material[] puffMats, smokeMats;
        static Material lineMat;
        internal static Mesh quad;
        static readonly Color Ray = new Color(1f, 0.97f, 0.82f);
        static readonly Color Spray = new Color(1f, 0.36f, 0.2f, 0.9f);
        static readonly Color SwirlCol = new Color(0.22f, 0.13f, 0.11f, 0.85f);

        static bool Init()
        {
            if (puffMats != null) return puffMats.Length > 0;
            var sh = Shader.Find("Sprites/Default");
            puffMats = Load("FX/fire_puff_", 3, sh, 3002);
            smokeMats = Load("FX/smoke_", 2, sh, 3000);
            lineMat = new Material(sh) { renderQueue = 3001 };
            quad = new Mesh { name = "FxQuad" };
            quad.vertices = new[] { new Vector3(-0.5f, -0.5f, 0), new Vector3(0.5f, -0.5f, 0), new Vector3(-0.5f, 0.5f, 0), new Vector3(0.5f, 0.5f, 0) };
            quad.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
            quad.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            quad.RecalculateBounds();
            return puffMats.Length > 0;
        }

        static Material[] Load(string prefix, int n, Shader sh, int queue)
        {
            var list = new System.Collections.Generic.List<Material>();
            for (int i = 0; i < n; i++)
            {
                var t = Resources.Load<Texture2D>(prefix + i);
                if (t) list.Add(new Material(sh) { mainTexture = t, renderQueue = queue });
            }
            return list.ToArray();
        }

        /// <summary>총구 한 발. p = 총구, d = 바깥 방향(수평), full = 본체(붉은 흩뿌림까지) / 분신(뭉치 + 빛줄기)</summary>
        public static void Burst(Vector3 p, Vector3 d, float scale, bool full)
        {
            if (!Init()) return;
            var side = Vector3.Cross(Vector3.up, d).normalized;
            int n = full ? 3 : 2;
            for (int i = 0; i < n; i++)
            {
                var pos = p + d * (0.3f + 0.5f * i) * scale + side * Random.Range(-0.22f, 0.22f) * scale + Vector3.up * Random.Range(-0.1f, 0.2f);
                float size = (1.7f - 0.3f * i) * scale * Random.Range(0.85f, 1.15f);
                FxSprite.Spawn(puffMats[Random.Range(0, puffMats.Length)], pos, size, 0.3f, i * 0.018f, d * 1.6f * scale + Vector3.up * 0.4f, FxSprite.Style.Fire);
            }
            // 하얀 빛줄기: 총구에서 바깥으로 쭉
            FxLine.Ray(p, d, 3.0f * scale, 0.2f * scale, Ray, 0.09f);
            if (!full) return;
            // 붉은 흩뿌림: 부채꼴 가는 선
            for (int k = 0; k < 4; k++)
            {
                var dd = Quaternion.AngleAxis(Random.Range(-20f, 20f), Vector3.up) * d;
                dd.y = Random.Range(-0.08f, 0.12f);
                FxLine.Ray(p + dd * 0.4f * scale, dd.normalized, Random.Range(2.2f, 3.8f) * scale, 0.035f * scale, Spray, 0.13f);
            }
        }

        /// <summary>회색 연기 뭉치</summary>
        public static void Smoke(Vector3 at, float size, float life = 0.9f)
        {
            if (!Init() || smokeMats.Length == 0) return;
            FxSprite.Spawn(smokeMats[Random.Range(0, smokeMats.Length)], at, size, life, 0, Vector3.up * 0.5f + Random.insideUnitSphere * 0.4f, FxSprite.Style.Smoke);
        }

        /// <summary>몸 주위를 기울어진 원으로 도는 어두운 곡선 (spin = 도/초)</summary>
        public static void Swirl(Vector3 center, float radius, float spin, float life = 0.35f)
        {
            if (!Init()) return;
            FxLine.Arc(center, radius, spin, SwirlCol, life);
        }

        internal static Material LineMat { get { Init(); return lineMat; } }
    }

    /// <summary>카메라를 보는 스프라이트 한 장 (불덩이 / 연기)</summary>
    public class FxSprite : MonoBehaviour
    {
        public enum Style { Fire, Smoke }
        static readonly int ColorId = Shader.PropertyToID("_Color");
        MeshRenderer mr;
        MaterialPropertyBlock mpb;
        float born, life, delay, size, roll, rollSpeed;
        Vector3 drift;
        Style style;

        public static void Spawn(Material mat, Vector3 pos, float size, float life, float delay, Vector3 drift, Style style)
        {
            var go = new GameObject(style == Style.Fire ? "FirePuff" : "Smoke");
            go.transform.position = pos;
            go.AddComponent<MeshFilter>().sharedMesh = GunkataFx.quad;
            var s = go.AddComponent<FxSprite>();
            s.mr = go.AddComponent<MeshRenderer>();
            s.mr.sharedMaterial = mat;
            s.mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            s.mr.receiveShadows = false;
            s.mpb = new MaterialPropertyBlock();
            s.born = Time.time; s.life = life; s.delay = delay; s.size = size; s.drift = drift; s.style = style;
            s.roll = Random.Range(0f, 360f);
            s.rollSpeed = Random.Range(-90f, 90f);
            s.Pose(0);
        }

        void LateUpdate()
        {
            float t = (Time.time - born - delay) / life;
            if (t >= 1) { Destroy(gameObject); return; }
            transform.position += drift * Time.deltaTime;
            drift *= Mathf.Exp(-4f * Time.deltaTime);
            roll += rollSpeed * Time.deltaTime;
            Pose(t);
        }

        void Pose(float t)
        {
            mr.enabled = t >= 0;
            if (t < 0) return;
            var cam = Camera.main;
            if (cam) transform.rotation = cam.transform.rotation * Quaternion.Euler(0, 0, roll);
            float sc; Color c;
            if (style == Style.Fire)
            {
                // 0.06초 만에 확 부풀고(0.35→1) 천천히 더 커지며 → 하얗게 뜨겁다가 그을린 갈색으로 식으며 사라짐
                float pop = t < 0.2f ? 1 - (1 - t / 0.2f) * (1 - t / 0.2f) : 1;
                sc = Mathf.Lerp(0.35f, 1f, pop) * (1 + 0.15f * t);
                float cool = Mathf.Clamp01((t - 0.3f) / 0.5f);
                c = Color.Lerp(Color.white, new Color(0.55f, 0.38f, 0.33f), cool);
                c.a = t < 0.6f ? 1 : 1 - (t - 0.6f) / 0.4f;
            }
            else
            {
                // 연기: 부풀며 진해졌다가 옅어짐
                sc = Mathf.Lerp(0.7f, 1.5f, 1 - (1 - t) * (1 - t));
                c = Color.white;
                c.a = t < 0.15f ? t / 0.15f * 0.8f : 0.8f * (1 - (t - 0.15f) / 0.85f);
            }
            transform.localScale = Vector3.one * size * sc;
            mpb.SetColor(ColorId, c);
            mr.SetPropertyBlock(mpb);
        }
    }

    /// <summary>선 이펙트: 빛줄기(Ray) · 소용돌이 곡선(Arc)</summary>
    public class FxLine : MonoBehaviour
    {
        LineRenderer lr;
        float born, life, len, width, radius, spin, startAng;
        Vector3 origin, dir;
        Quaternion tilt;
        Color col;
        bool arc;
        const int ArcPts = 18;

        static FxLine Make(Color c, float life, int points)
        {
            var go = new GameObject("FxLine");
            var f = go.AddComponent<FxLine>();
            f.lr = go.AddComponent<LineRenderer>();
            f.lr.sharedMaterial = GunkataFx.LineMat;
            f.lr.positionCount = points;
            f.lr.numCapVertices = 2;
            f.lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            f.lr.receiveShadows = false;
            f.col = c; f.life = life; f.born = Time.time;
            return f;
        }

        /// <summary>총구에서 바깥으로 뻗는 가는 빛줄기 (뿌리 굵게 → 끝 뾰족)</summary>
        public static void Ray(Vector3 from, Vector3 dir, float len, float width, Color c, float life)
        {
            var f = Make(c, life, 2);
            f.origin = from; f.dir = dir.normalized; f.len = len; f.width = width;
            f.Pose(0);
        }

        /// <summary>몸 주위를 기울어진 원으로 도는 곡선 (150° 호, 꼬리로 갈수록 가늘게)</summary>
        public static void Arc(Vector3 center, float radius, float spin, Color c, float life)
        {
            var f = Make(c, life, ArcPts);
            f.arc = true;
            f.origin = center; f.radius = radius; f.spin = spin;
            f.startAng = Random.Range(0f, 360f);
            f.tilt = Quaternion.Euler(Random.Range(-28f, 28f), Random.Range(0f, 360f), Random.Range(-28f, 28f));
            f.width = Random.Range(0.12f, 0.2f);
            f.Pose(0);
        }

        void LateUpdate()
        {
            float t = (Time.time - born) / life;
            if (t >= 1) { Destroy(gameObject); return; }
            Pose(t);
        }

        void Pose(float t)
        {
            var c = col;
            c.a *= t < 0.5f ? 1 : 1 - (t - 0.5f) / 0.5f;
            if (!arc)
            {
                float grow = 1 - (1 - Mathf.Clamp01(t / 0.35f)) * (1 - Mathf.Clamp01(t / 0.35f));
                lr.SetPosition(0, origin);
                lr.SetPosition(1, origin + dir * len * Mathf.Lerp(0.4f, 1f, grow));
                lr.startWidth = width * (1 - t * 0.7f);
                lr.endWidth = 0;
                lr.startColor = c;
                var e = c; e.a *= 0.3f; lr.endColor = e;
                return;
            }
            // 소용돌이: 반지름이 조금 커지며 빠르게 돎
            float r = radius * (1 + 0.3f * t);
            float a0 = startAng + spin * (Time.time - born);
            for (int i = 0; i < ArcPts; i++)
            {
                float a = (a0 - Mathf.Sign(spin) * i * 150f / (ArcPts - 1)) * Mathf.Deg2Rad;   // 꼬리는 도는 방향 뒤쪽
                lr.SetPosition(i, origin + tilt * new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r));
            }
            lr.startWidth = width;
            lr.endWidth = 0;
            lr.startColor = c;
            var tail = c; tail.a = 0; lr.endColor = tail;
        }
    }
}
