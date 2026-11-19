// 이펙트 개편 3차 (2026-11-17 사용자가 하나씩 확인): 예전엔 전부 '섬광 덩어리 + 고리 + 빛살'(GunkataFx.Flare)을 색만 바꿔 썼음 → 모양 · 움직임으로 구분
// - 확률 회피: 고양이 잔상 두 개가 화면 좌우로 빗겨나며 사라지고, 옆으로 짧은 바람 가시
// - 범위 기절: 발밑에서 금빛 흙먼지 띠가 범위 끝까지 바닥을 쓸고 나감 + 닿은 적마다 짧은 번쩍
// - 범위 둔화: 바닥에 푸른 모래 소용돌이(나선 팔 4개)가 천천히 돌며 범위만큼 퍼졌다 가라앉음
// - 레벨 업: 발밑에서 청록 빛기둥이 솟고 경험치 보석 조각이 빙글빙글 위로 흩날림 + 머리 위 'LEVEL UP' 글자(Codex 그림, Resources/UI/level_up_text)
// 레벨 업은 3택 창이 게임을 멈추므로 실제 시간(unscaled)으로 돎
// 4차 (13 · 14):
// - 총알 비(불릿 파티): 탄착 직전 하늘에서 가는 빛줄이 비스듬히 내리꽂히고 → 바닥엔 고리 대신 흙먼지 + 작은 탄자국(Codex Resources/FX/bullet_hole)
// - 관통: 탄도 방향으로 길쭉하게 뚫고 나가는 불꽃 꼬리 / 튕김: 꺾인 자리에서 지그재그로 튀는 불똥 + 짧은 흰 번쩍
using UnityEngine;

namespace CatTang
{
    public static class ExtraFx
    {
        static Material lineMat;
        static Material LineMat()
        {
            if (!lineMat) lineMat = new Material(Shader.Find("CatTang/Sprite") ?? Shader.Find("Sprites/Default")) { renderQueue = 3050 };
            return lineMat;
        }

        public static LineRenderer Line(Transform parent, int n, Color c, float width)
        {
            var go = new GameObject("Line");
            go.transform.SetParent(parent, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial = LineMat();
            lr.useWorldSpace = true;
            lr.positionCount = n;
            lr.numCapVertices = 3;
            lr.widthMultiplier = width;
            lr.startColor = lr.endColor = c;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            return lr;
        }

        static Vector3 ScreenSide()
        {
            var cam = Camera.main;
            var r = cam ? cam.transform.right : Vector3.right;
            r.y = 0;
            return r.sqrMagnitude > 1e-4f ? r.normalized : Vector3.right;
        }

        // ---------- 7. 확률 회피 ----------
        public static void ChanceDodge(Transform model, Vector3 feet, Color col)
        {
            if (!model) return;
            var side = ScreenSide();
            for (int k = -1; k <= 1; k += 2)
                Afterimage.DashIn(model, model.position, model.position + side * k * 0.85f, model.rotation, new Color(col.r, col.g, col.b, 0.7f), 0.1f, 0.28f);
            if (!GunkataFx.Ready) return;
            // 옆으로 스치는 바람 가시 3~4개 (몸 높이 여기저기, 좌우 한쪽으로 몰아서)
            float dirSign = Random.value < 0.5f ? -1f : 1f;
            int n = Random.Range(3, 5);
            for (int i = 0; i < n; i++)
            {
                var d = (side * dirSign + Vector3.up * Random.Range(-0.1f, 0.15f)).normalized;
                var at = feet + Vector3.up * Random.Range(0.5f, 1.5f) - side * dirSign * 0.4f;
                GunkataFx.Streak(at, d, Random.Range(0.8f, 1.3f), 0.08f, col, Random.Range(5f, 8f));
            }
        }

        // ---------- 8. 범위 기절: 바닥을 쓸고 가는 금빛 흙먼지 띠 ----------
        public static void GroundWave(Vector3 at, float radius, Color col)
        {
            var go = new GameObject("FX_GroundWave");
            go.transform.position = new Vector3(at.x, at.y + 0.05f, at.z);
            var w = go.AddComponent<GroundWaveFx>();
            w.radius = radius; w.col = col;
        }

        // ---------- 9. 범위 둔화: 푸른 모래 소용돌이 ----------
        public static void SandSwirl(Vector3 at, float radius, Color col)
        {
            var go = new GameObject("FX_SandSwirl");
            go.transform.position = new Vector3(at.x, at.y + 0.04f, at.z);
            var s = go.AddComponent<SandSwirlFx>();
            s.radius = radius; s.col = col;
        }

        // ---------- 13. 총알 비 한 방울 ----------
        public static void RainDrop(Vector3 ground, Color col)
        {
            var go = new GameObject("FX_RainDrop");
            go.transform.position = ground;
            go.AddComponent<RainDropFx>().col = col;
        }

        /// <summary>하늘 줄 없이 바닥 탄자국 + 흙먼지 + 불똥만 (불릿 파티: 2026-11-17 사용자 — 총알 비는 빼고 바닥 탄 자국은 남김)</summary>
        public static void GroundHit(Vector3 ground, Color col)
        {
            var go = new GameObject("FX_GroundHit");
            go.transform.position = ground;
            var r = go.AddComponent<RainDropFx>();
            r.col = col; r.groundOnly = true;
        }

        // ---------- 14. 관통 · 튕김 ----------
        /// <summary>관통: 맞은 자리에서 탄도 방향으로 길쭉하게 뚫고 나가는 불꽃 꼬리 (가시 3개가 앞으로 늘어지며 빠져나감)</summary>
        public static void Pierce(Vector3 p, Vector3 dir, Color col)
        {
            if (!GunkataFx.Ready) return;
            dir = dir.sqrMagnitude > 1e-4f ? dir.normalized : Vector3.forward;
            for (int i = 0; i < 3; i++)
            {
                var d = (dir + Random.insideUnitSphere * 0.12f).normalized;
                GunkataFx.Streak(p + dir * (0.15f + i * 0.25f), d, Random.Range(1.2f, 1.9f) * (1f - i * 0.2f), 0.13f - i * 0.03f, i == 0 ? Color.Lerp(col, Color.white, 0.5f) : col, Random.Range(9f, 13f));
            }
            FxBurst.Spawn(p, 0.8f, FxBurst.Kind.Spark, col, 0.15f);
        }

        /// <summary>튕김: 꺾인 자리에서 지그재그로 튀는 불똥 (방향을 한 번 더 꺾으며) + 짧은 흰 번쩍</summary>
        public static void Ricochet(Vector3 p, Vector3 outDir, Color col)
        {
            if (!GunkataFx.Ready) return;
            outDir = outDir.sqrMagnitude > 1e-4f ? outDir.normalized : Vector3.forward;
            int n = Random.Range(3, 5);
            for (int i = 0; i < n; i++)
            {
                var d = Quaternion.Euler(Random.Range(-10f, 25f), Random.Range(-70f, 70f), 0) * outDir;
                var bend = Quaternion.Euler(0, Random.value < 0.5f ? -60f : 60f, 0) * d;
                GunkataFx.Streak(p, d, Random.Range(0.35f, 0.55f), 0.06f, col, Random.Range(6f, 9f));
                GunkataFx.Streak(p + d * 0.45f, bend, Random.Range(0.25f, 0.4f), 0.05f, Color.Lerp(col, Color.white, 0.4f), Random.Range(5f, 8f));
            }
            FxBurst.Spawn(p, 0.7f, FxBurst.Kind.Spark, Color.white, 0.1f);
        }

        // ---------- 10. 레벨 업 ----------
        public static void LevelUp(Transform player, Color col)
        {
            if (!player) return;
            Sfx.Play("levelup", 0.85f, 0f);
            var go = new GameObject("FX_LevelUp");
            go.transform.position = player.position;
            var l = go.AddComponent<LevelUpFx>();
            l.follow = player; l.col = col;
        }
    }

    public class RainDropFx : MonoBehaviour
    {
        public Color col;
        public bool groundOnly;                       // 하늘 줄 없이 바로 탄자국 (ExtraFx.GroundHit)
        const float Fall = 0.09f, Fade = 0.12f;
        static Texture2D holeTex;
        static Material holeMat;
        LineRenderer line;
        Vector3 top;
        float t;
        bool landed;

        void Start()
        {
            if (groundOnly) { Land(); Destroy(gameObject); return; }
            // 하늘 비스듬한 자리에서 (카메라 쪽 · 옆으로 조금씩 흔들어)
            top = transform.position + new Vector3(Random.Range(-1.2f, 1.2f), 7f, Random.Range(-1.6f, -0.6f));
            line = ExtraFx.Line(transform, 2, col, 0.09f);
            line.alignment = LineAlignment.View;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(col, 0), new GradientColorKey(Color.Lerp(col, Color.white, 0.6f), 1) },
                      new[] { new GradientAlphaKey(0f, 0), new GradientAlphaKey(1f, 1) });
            line.colorGradient = g;
            line.widthCurve = new AnimationCurve(new Keyframe(0, 0.3f), new Keyframe(1, 1f));
            line.SetPosition(0, top); line.SetPosition(1, top);
        }

        void Update()
        {
            t += Time.deltaTime;
            var ground = transform.position;
            if (!landed)
            {
                float k = Mathf.Clamp01(t / Fall);
                line.SetPosition(0, Vector3.Lerp(top, ground, Mathf.Max(0, k - 0.55f)));
                line.SetPosition(1, Vector3.Lerp(top, ground, k));
                if (k >= 1) { landed = true; Land(); }
                return;
            }
            float f = Mathf.Clamp01((t - Fall) / Fade);
            line.SetPosition(0, Vector3.Lerp(Vector3.Lerp(top, ground, 0.45f), ground, f));
            line.widthMultiplier = 0.09f * (1 - f);
            if (f >= 1) Destroy(gameObject);
        }

        void Land()
        {
            var p = transform.position;
            FxBurst.Spawn(p + Vector3.up * 0.2f, Random.Range(0.6f, 0.9f), FxBurst.Kind.Spark, col, 0.14f);
            FxBurst.Spawn(p + Vector3.up * 0.15f, Random.Range(0.9f, 1.2f), FxBurst.Kind.Dust, FxBurst.MistDust, 0.45f);
            // 작은 탄자국 (바닥에 눕힌 판, 1.4초 뒤 사라짐)
            if (!holeTex) holeTex = Resources.Load<Texture2D>("FX/bullet_hole");
            if (!holeTex) return;
            if (!holeMat) holeMat = new Material(Shader.Find("CatTang/Sprite") ?? Shader.Find("Sprites/Default")) { mainTexture = holeTex, renderQueue = 2985 };
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(q.GetComponent<Collider>());
            q.name = "BulletHole";
            q.transform.position = p + Vector3.up * 0.02f;
            q.transform.rotation = Quaternion.Euler(90, Random.Range(0f, 360f), 0);
            q.transform.localScale = Vector3.one * Random.Range(0.45f, 0.65f);
            var r = q.GetComponent<Renderer>();
            r.sharedMaterial = holeMat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            q.AddComponent<FadeOut>().life = 1.4f;
        }
    }

    /// <summary>life 초 뒤 0.4초에 걸쳐 투명해지며 사라짐 (_Color 알파)</summary>
    public class FadeOut : MonoBehaviour
    {
        public float life = 1f;
        float t;
        Renderer r;
        MaterialPropertyBlock mpb;
        void Start() { r = GetComponent<Renderer>(); mpb = new MaterialPropertyBlock(); }
        void Update()
        {
            t += Time.deltaTime;
            float a = 1f - Mathf.Clamp01((t - life) / 0.4f);
            if (a <= 0) { Destroy(gameObject); return; }
            mpb.SetColor("_Color", new Color(1, 1, 1, a));
            r.SetPropertyBlock(mpb);
        }
    }

    public class GroundWaveFx : MonoBehaviour
    {
        public float radius = 5f;
        public Color col;
        const float Sweep = 0.3f;
        float t;
        int step;
        LineRenderer edge;

        void Start()
        {
            edge = ExtraFx.Line(transform, 49, col, 0.35f);
            edge.loop = true;
            edge.alignment = LineAlignment.TransformZ;            // 바닥에 눕힘
            transform.rotation = Quaternion.Euler(90, 0, 0);
            CameraFollow.Shake(0.06f);
        }

        void Update()
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / Sweep), e = 1 - (1 - k) * (1 - k);
            float r = Mathf.Lerp(0.4f, radius, e);
            var c = transform.position;
            for (int i = 0; i < 49; i++)
            {
                float a = i / 48f * Mathf.PI * 2f;
                float wob = 1f + 0.05f * Mathf.Sin(a * 7f + t * 20f);
                edge.SetPosition(i, c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * r * wob);
            }
            var ec = col; ec.a = 1f - k * 0.5f; edge.startColor = edge.endColor = ec;
            edge.widthMultiplier = Mathf.Lerp(0.5f, 0.15f, k);
            // 띠를 따라 흙먼지가 뭉게뭉게 (0.05초마다 한 바퀴)
            if (t >= step * 0.05f && step < 7)
            {
                step++;
                int n = 8 + Mathf.RoundToInt(r * 2f);
                float a0 = Random.Range(0f, 360f);
                for (int i = 0; i < n; i++)
                {
                    float a = (a0 + i * 360f / n) * Mathf.Deg2Rad;
                    FxBurst.Spawn(c + new Vector3(Mathf.Cos(a), 0.15f, Mathf.Sin(a)) * r, Random.Range(0.9f, 1.4f), FxBurst.Kind.Dust, col, 0.45f);
                }
            }
            if (t > Sweep + 0.25f) Destroy(gameObject);
            else if (t > Sweep) { var fc = col; fc.a = 1 - (t - Sweep) / 0.25f; edge.startColor = edge.endColor = fc; }
        }
    }

    public class SandSwirlFx : MonoBehaviour
    {
        public float radius = 4f;
        public Color col;
        const int Arms = 4, Pts = 22;
        const float Life = 0.9f;
        readonly LineRenderer[] arms = new LineRenderer[Arms];
        float t, spin;

        void Start()
        {
            for (int k = 0; k < Arms; k++)
            {
                arms[k] = ExtraFx.Line(transform, Pts, col, 0.22f);
                arms[k].alignment = LineAlignment.TransformZ;
                arms[k].widthCurve = new AnimationCurve(new Keyframe(0, 0.3f), new Keyframe(0.6f, 1f), new Keyframe(1, 0.1f));
            }
            transform.rotation = Quaternion.Euler(90, 0, 0);
            spin = Random.Range(0f, 360f);
        }

        void Update()
        {
            t += Time.deltaTime;
            if (t >= Life) { Destroy(gameObject); return; }
            float k = t / Life;
            spin += 70f * (1 - k * 0.6f) * Time.deltaTime;         // 천천히, 점점 더 느리게 (시간이 느려지는 느낌)
            float grow = 1 - (1 - Mathf.Clamp01(t / 0.35f)) * (1 - Mathf.Clamp01(t / 0.35f));
            float alpha = (k < 0.15f ? k / 0.15f : k > 0.65f ? 1 - (k - 0.65f) / 0.35f : 1f) * 0.75f;
            var c = transform.position;
            for (int a = 0; a < Arms; a++)
            {
                var lr = arms[a];
                var ac = col; ac.a = alpha;
                lr.startColor = lr.endColor = ac;
                for (int i = 0; i < Pts; i++)
                {
                    float u = i / (Pts - 1f);
                    float ang = (spin + a * 360f / Arms + u * 140f) * Mathf.Deg2Rad;
                    float r = Mathf.Lerp(0.3f, radius, u) * grow;
                    lr.SetPosition(i, c + new Vector3(Mathf.Cos(ang), -0.02f * k, Mathf.Sin(ang)) * r);
                }
            }
        }
    }

    public class LevelUpFx : MonoBehaviour
    {
        public Transform follow;
        public Color col;
        const float Life = 1.1f;
        float t;
        LineRenderer pillar;
        Transform[] gems;
        float[] gemPhase;
        Transform word;
        Renderer wordR;
        MaterialPropertyBlock mpb;
        static Texture2D wordTex;

        void Start()
        {
            mpb = new MaterialPropertyBlock();
            pillar = ExtraFx.Line(transform, 2, col, 1f);
            pillar.alignment = LineAlignment.View;
            pillar.widthCurve = new AnimationCurve(new Keyframe(0, 1f), new Keyframe(1, 0.2f));
            var g = new Gradient();
            var hot = Color.Lerp(col, Color.white, 0.6f);
            g.SetKeys(new[] { new GradientColorKey(hot, 0), new GradientColorKey(col, 1) }, new[] { new GradientAlphaKey(1f, 0), new GradientAlphaKey(0.55f, 0.6f), new GradientAlphaKey(0f, 1) });
            pillar.colorGradient = g;
            // 경험치 보석 조각 (블렌더 + Codex 모델 Kit XpGem, 없으면 생략)
            var rm = RunManager.Instance;
            var gemPrefab = rm ? Kit.Shared(rm.Chapter, "XpGem") : null;
            int n = gemPrefab ? 6 : 0;
            gems = new Transform[n]; gemPhase = new float[n];
            for (int i = 0; i < n; i++)
            {
                var gi = Instantiate(gemPrefab, transform);
                foreach (var c in gi.GetComponentsInChildren<Collider>()) Destroy(c);
                foreach (var b in gi.GetComponentsInChildren<MonoBehaviour>()) Destroy(b);
                gems[i] = gi.transform;
                gemPhase[i] = i * 60f + Random.Range(-15f, 15f);
            }
            // 'LEVEL UP' 글자 (Codex 그림) — 카메라를 보는 판
            if (!wordTex) wordTex = Resources.Load<Texture2D>("UI/level_up_text");
            if (wordTex)
            {
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Destroy(q.GetComponent<Collider>());
                q.name = "Word";
                q.transform.SetParent(transform, false);
                wordR = q.GetComponent<Renderer>();
                wordR.sharedMaterial = new Material(Shader.Find("CatTang/Sprite") ?? Shader.Find("Sprites/Default")) { mainTexture = wordTex, renderQueue = 3500 };
                if (wordR.sharedMaterial.HasProperty("_ZTest")) wordR.sharedMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
                wordR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                word = q.transform;
            }
        }

        void Update()
        {
            t += Time.unscaledDeltaTime;
            if (t >= Life) { Destroy(gameObject); return; }
            if (follow) transform.position = follow.position;
            var p = transform.position;
            var cam = Camera.main;
            // 빛기둥: 0.15초에 4.5m 솟고, 뒤로 가늘어지며 사라짐
            float rise = Mathf.Clamp01(t / 0.15f);
            float fade = Mathf.Clamp01((t - 0.4f) / 0.5f);
            pillar.SetPosition(0, p);
            pillar.SetPosition(1, p + Vector3.up * 4.5f * (1 - (1 - rise) * (1 - rise)));
            pillar.widthMultiplier = Mathf.Lerp(1.5f, 0f, fade);
            // 보석 조각: 몸 둘레를 빙글빙글 돌며 위로 흩날리고 작아짐
            for (int i = 0; i < gems.Length; i++)
            {
                if (!gems[i]) continue;
                float u = Mathf.Clamp01(t / 0.9f);
                float a = (gemPhase[i] + t * 420f) * Mathf.Deg2Rad;
                float r = Mathf.Lerp(0.4f, 1.1f, u);
                gems[i].position = p + new Vector3(Mathf.Cos(a) * r, 0.3f + u * 2.8f + (i % 2) * 0.3f, Mathf.Sin(a) * r);
                gems[i].rotation = Quaternion.Euler(t * 300f, t * 520f + i * 40f, 0);
                gems[i].localScale = Vector3.one * Mathf.Lerp(2.2f, 0.3f, u * u);   // 보석 모델이 0.35m라 크게 (화면에서 읽히게)
            }
            // 글자: 머리 위 대시 줄보다 위에서 툭 커졌다 자리 잡고 살짝 떠오르다 사라짐
            if (word && cam)
            {
                float pop = t < 0.12f ? Mathf.Lerp(0.3f, 1.2f, t / 0.12f) : Mathf.Lerp(1.2f, 1f, Mathf.Clamp01((t - 0.12f) / 0.1f));
                float h = 0.48f * pop;                         // 글자 그림이 6.7:1이라 가로 약 3.2m
                word.position = p + Vector3.up * (3.1f + t * 0.5f);
                word.rotation = cam.transform.rotation;
                word.localScale = new Vector3(h * wordTex.width / wordTex.height, h, 1);
                mpb.SetColor("_Color", new Color(1, 1, 1, t > Life - 0.25f ? (Life - t) / 0.25f : 1f));
                wordR.SetPropertyBlock(mpb);
            }
        }

        void OnDestroy() { if (wordR) Destroy(wordR.sharedMaterial); }
    }
}

namespace CatTang
{
    /// <summary>고양이 머리 위에 연하게 뜨는 효과 아이콘 (2026-11-18 사용자: 종탑의 메아리 경직이 무슨 효과인지 알 수 있게 종 그림).
    /// 같은 그림은 하나만 두고 다시 발동하면 시간만 늘림. 머리 위 대시 줄보다 위, 카메라를 봄</summary>
    public class HeadIcon : MonoBehaviour
    {
        const float Life = 0.6f, Alpha = 0.55f, Size = 0.75f;
        static readonly System.Collections.Generic.Dictionary<string, HeadIcon> live = new();
        static readonly System.Collections.Generic.Dictionary<string, Texture2D> tex = new();
        Transform owner; Renderer r; MaterialPropertyBlock mpb; float born; string key;

        public static void Show(Transform owner, string sprite)
        {
            if (!owner) return;
            if (live.TryGetValue(sprite, out var h) && h) { h.born = Time.time; return; }
            if (!tex.TryGetValue(sprite, out var t) || !t) { t = Resources.Load<Texture2D>("UI/" + sprite); tex[sprite] = t; }
            if (!t) return;
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(q.GetComponent<Collider>());
            q.name = "HeadIcon_" + sprite;
            var r = q.GetComponent<Renderer>();
            r.sharedMaterial = new Material(Shader.Find("CatTang/Sprite") ?? Shader.Find("Sprites/Default")) { mainTexture = t, renderQueue = 3500 };
            if (r.sharedMaterial.HasProperty("_ZTest")) r.sharedMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            h = q.AddComponent<HeadIcon>();
            h.owner = owner; h.r = r; h.mpb = new MaterialPropertyBlock(); h.born = Time.time; h.key = sprite;
            live[sprite] = h;
            h.LateUpdate();
        }

        void LateUpdate()
        {
            float t = Time.time - born;
            if (!owner || t > Life) { Destroy(gameObject); return; }
            var cam = Camera.main;
            float pop = t < 0.08f ? Mathf.Lerp(0.6f, 1.1f, t / 0.08f) : Mathf.Lerp(1.1f, 1f, Mathf.Clamp01((t - 0.08f) / 0.1f));
            transform.position = owner.position + Vector3.up * (3.0f + t * 0.3f);
            if (cam) transform.rotation = cam.transform.rotation;
            transform.localScale = Vector3.one * Size * pop;
            float a = Alpha * (t > Life - 0.2f ? (Life - t) / 0.2f : 1f);
            mpb.SetColor("_Color", new Color(1, 1, 1, a));
            r.SetPropertyBlock(mpb);
        }

        void OnDestroy()
        {
            if (r) Destroy(r.sharedMaterial);
            if (key != null && live.TryGetValue(key, out var h) && h == this) live.Remove(key);
        }
    }
}
