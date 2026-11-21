// 땅을 타고 날아가는 파동 + 땅 솟음 (2026-11-20 사용자: "날아가는 파동 따로 만들어, 재활용 말고 돌진 이펙트 느낌으로 땅에 붙어서" · "땅 솟아오르는 형태도 블렌더로, 잠깐 남는 형태로")
// GroundWave: 머리가 길을 따라 미끄러지며(처음 빠르게 → 끝에서 감속) 지나간 바닥에 띠를 그음 — 돌진 꼬리(DashTrail.Wake)와 같은 방식:
//   점마다 지난 시간을 U에 넣어 CatTang/StylizedVfx가 꼬리부터 흩어지게, 폭은 머리에서 가장 넓고 꼬리로 갈수록 퍼졌다 가늘어짐.
//   머리가 지나가는 자리마다 Upheaval(블렌더 바위 무더기, Art/tools/blender_fx_upheaval.py)이 좌우 번갈아 솟음
// Upheaval: 땅 밑에서 0.1초 만에 튀어 오름(살짝 넘침) → hold초 남음(갈라진 틈의 빨간빛은 식어 감) → 가라앉아 사라짐
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    public class GroundWave : MonoBehaviour
    {
        const float Step = 0.25f;
        static Material mat;
        Vector3 a, b; float dur, width, life, born, rockEvery;
        StylizedFx.Look look;
        readonly List<Vector3> pts = new(); readonly List<float> times = new();
        Vector3 head; bool live = true;
        float nextRock; int rockSide = 1;
        Mesh mesh; MeshRenderer mr; MaterialPropertyBlock mpb;
        readonly List<Vector3> v = new(); readonly List<Vector2> uv = new(); readonly List<int> tri = new();

        /// <summary>from → to 로 dur초 동안 땅을 타고 가는 파동. width = 파동 폭(예고 길 폭과 같게), rockEvery = 땅 솟음 간격(m, 0 = 없음)</summary>
        public static GroundWave Spawn(Vector3 from, Vector3 to, float width, float dur, StylizedFx.Look look, float rockEvery = 1.8f)
        {
            var go = new GameObject("GroundWave");
            var w = go.AddComponent<GroundWave>();
            from.y = to.y = 0.05f;
            w.a = from; w.b = to; w.width = width; w.dur = dur; w.look = look; w.life = 0.55f; w.born = Time.time; w.rockEvery = rockEvery;
            w.head = from; w.pts.Add(from); w.times.Add(Time.time); w.nextRock = 0f;
            w.Init();
            return w;
        }

        void Init()
        {
            var sh = Shader.Find("CatTang/StylizedVfx");
            if (!mat && sh) mat = new Material(sh) { renderQueue = 3006, name = "GroundWave" };
            mesh = new Mesh { name = "GroundWave" }; mesh.MarkDynamic();
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            mr = gameObject.AddComponent<MeshRenderer>(); mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
            mpb = new MaterialPropertyBlock();
            mpb.SetColor("_ColCore", look.core); mpb.SetColor("_ColMid", look.mid); mpb.SetColor("_ColEdge", look.edge); mpb.SetColor("_ColInk", look.ink);
            mpb.SetFloat("_Pattern", look.pattern); mpb.SetVector("_Scale", look.scale); mpb.SetVector("_Scroll", look.scroll);
            mpb.SetFloat("_Band1", look.band1); mpb.SetFloat("_Band2", look.band2); mpb.SetFloat("_Ink", 0.08f);
            mpb.SetFloat("_TaperV", look.taperV); mpb.SetFloat("_TaperU", 0f); mpb.SetFloat("_Fresnel", 0f);
            mpb.SetFloat("_Opacity", look.opacity); mpb.SetFloat("_TailFade", 1.3f); mpb.SetFloat("_Sweep", 1f); mpb.SetFloat("_Alpha", 1f); mpb.SetFloat("_Age", 0.02f);
            mpb.SetVector("_Offset", new Vector4(Random.value * 10f, Random.value * 10f));
        }

        void Update()
        {
            float now = Time.time;
            if (live)
            {
                float t = Mathf.Clamp01((now - born) / dur);
                float k = 1 - Mathf.Pow(1 - t, 2.2f);
                var p = Vector3.Lerp(a, b, k);
                var last = pts[pts.Count - 1];
                float dd = Vector3.Distance(last, p);
                if (dd >= Step || (t >= 1f && dd > 0.001f))
                {
                    int n = Mathf.Clamp(Mathf.FloorToInt(dd / Step), 1, 10);
                    float lt = times[times.Count - 1];
                    for (int i = 1; i <= n; i++) { float f = i / (float)n; pts.Add(Vector3.Lerp(last, p, f)); times.Add(Mathf.Lerp(lt, now, f)); }
                }
                head = p;
                // 땅 솟음: 머리가 지나가는 자리마다 좌우 번갈아
                float walked = Vector3.Distance(a, p);
                if (rockEvery > 0 && walked >= nextRock)
                {
                    var dir = (b - a).normalized; var side = Vector3.Cross(Vector3.up, dir);
                    Upheaval.Spawn(p + side * rockSide * width * 0.18f, width * Random.Range(0.42f, 0.55f), Random.Range(0f, 360f), 0.75f);
                    rockSide = -rockSide; nextRock += rockEvery;
                }
                if (t >= 1) live = false;
            }
            int drop = 0;
            while (drop < pts.Count - 1 && now - times[drop + 1] > life) drop++;
            if (drop > 0) { pts.RemoveRange(0, drop); times.RemoveRange(0, drop); }
            if (!live && now - times[times.Count - 1] > life) { Destroy(gameObject); return; }
            Build(now);
        }

        void Build(float now)
        {
            v.Clear(); uv.Clear(); tri.Clear();
            int n = pts.Count + (live ? 1 : 0);
            if (n < 2) { mesh.Clear(); return; }
            for (int i = 0; i < n; i++)
            {
                var p = i < pts.Count ? pts[i] : head;
                float t = i < times.Count ? times[i] : now;
                var prev = i > 0 ? (i - 1 < pts.Count ? pts[i - 1] : head) : p;
                var next = i + 1 < n ? (i + 1 < pts.Count ? pts[i + 1] : head) : p;
                var dir = next - prev; dir.y = 0; if (dir.sqrMagnitude < 1e-6f) dir = (b - a); dir.Normalize();
                float u = Mathf.Clamp01(1f - (now - t) / life);
                float hw = width * 0.5f * (0.35f + 0.65f * Mathf.Sqrt(u)) * (1f + 0.3f * (1f - u));
                var side = Vector3.Cross(Vector3.up, dir) * hw;
                v.Add(p - side); v.Add(p + side);
                uv.Add(new Vector2(u, 0)); uv.Add(new Vector2(u, 1));
                if (i < n - 1) { int q = i * 2; tri.Add(q); tri.Add(q + 1); tri.Add(q + 2); tri.Add(q + 1); tri.Add(q + 3); tri.Add(q + 2); }
            }
            mesh.Clear(); mesh.SetVertices(v); mesh.SetUVs(0, uv); mesh.SetTriangles(tri, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            mr.SetPropertyBlock(mpb);
        }

        void OnDestroy() { if (mesh) Destroy(mesh); }
    }

    /// <summary>블렌더로 구운 땅 솟음 바위 무더기 (Resources/FX/Upheaval/fx_upheaval_0~2, 반지름 1m 기준)</summary>
    public class Upheaval : MonoBehaviour
    {
        static GameObject[] src;
        static Material rock, rockTop;
        Material glow;
        float radius, hold;

        public static Upheaval Spawn(Vector3 at, float radius, float yaw, float hold = 0.75f)
        {
            if (src == null)
            {
                src = new GameObject[3];
                for (int i = 0; i < 3; i++) src[i] = Resources.Load<GameObject>("FX/Upheaval/fx_upheaval_" + i);
                var toon = Shader.Find("CatTang/Toon");
                if (toon)
                {
                    rock = new Material(toon) { name = "UpheavalRock" }; rock.SetColor("_BaseColor", new Color(0.36f, 0.32f, 0.33f)); rock.SetColor("_ShadeColor", new Color(0.24f, 0.2f, 0.23f)); rock.SetColor("_SecondShadeColor", new Color(0.14f, 0.11f, 0.14f));
                    rockTop = new Material(toon) { name = "UpheavalTop" }; rockTop.SetColor("_BaseColor", new Color(0.62f, 0.56f, 0.54f)); rockTop.SetColor("_ShadeColor", new Color(0.42f, 0.36f, 0.37f)); rockTop.SetColor("_SecondShadeColor", new Color(0.26f, 0.2f, 0.22f));
                }
            }
            var pf = src[Random.Range(0, 3)];
            if (!pf) return null;
            var go = Instantiate(pf);
            go.name = "Upheaval";
            at.y = 0;
            go.transform.SetPositionAndRotation(at + Vector3.down * radius, Quaternion.Euler(0, yaw, 0));
            go.transform.localScale = Vector3.one * radius;
            var u = go.AddComponent<Upheaval>();
            u.radius = radius; u.hold = hold;
            var unlit = Shader.Find("Universal Render Pipeline/Unlit");
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                var ms = r.sharedMaterials;
                for (int i = 0; i < ms.Length; i++)
                {
                    string n = ms[i] ? ms[i].name : "";
                    if (n.Contains("Glow")) { u.glow = new Material(unlit); u.glow.SetColor("_BaseColor", new Color(3.2f, 0.25f, 0.18f)); ms[i] = u.glow; }
                    else if (n.Contains("Top")) ms[i] = rockTop ? rockTop : ms[i];
                    else ms[i] = rock ? rock : ms[i];
                }
                r.sharedMaterials = ms;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }
            u.StartCoroutine(u.Run(at));
            return u;
        }

        IEnumerator Run(Vector3 at)
        {
            var down = at + Vector3.down * radius * 1.05f;
            // 튀어 오름 (살짝 넘쳤다 자리 잡음)
            for (float t = 0; t < 0.11f; t += Time.deltaTime)
            {
                float x = t / 0.11f, c = 1.70158f;
                float k = 1 + (c + 1) * Mathf.Pow(x - 1, 3) + c * Mathf.Pow(x - 1, 2);   // ease-out back
                transform.position = Vector3.LerpUnclamped(down, at, k);
                yield return null;
            }
            transform.position = at;
            // 남음: 틈의 빨간빛이 식음
            for (float t = 0; t < hold; t += Time.deltaTime)
            {
                if (glow) glow.SetColor("_BaseColor", Color.Lerp(new Color(3.2f, 0.25f, 0.18f), new Color(0.35f, 0.04f, 0.05f), t / hold));
                yield return null;
            }
            // 가라앉음
            for (float t = 0; t < 0.4f; t += Time.deltaTime)
            {
                float k = t / 0.4f; k *= k;
                transform.position = Vector3.Lerp(at, down, k);
                yield return null;
            }
            Destroy(gameObject);
        }

        void OnDestroy() { if (glow) Destroy(glow); }
    }
}
