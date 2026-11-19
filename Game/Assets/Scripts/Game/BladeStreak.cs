// 칼빛 줄기 (무기에 붙는 베기 궤적)
// 2026-11-19 사용자: "궤적이 칼날에 붙어야 함(몸 앞에 따로 뜬 호 말고), 4장은 섬광처럼" → "랫키호테 평타 모션에 붙은 이펙트 확인해 봐" → "3D니까 그 특성을 고려해서 사실적인 빨간 궤적"
// - 붙이는 법은 랫키호테 FoeRigLive.LabSlash(웹 main.js sampleBlade)와 같음: 휘두르는 동안 매 프레임 실제 무기의 칼끝 · 칼 밑동 자리를 기록
//   (애니메이터 · 몸 회전이 끝난 LateUpdate). 한 프레임에 크게 돌았으면 밑동 둘레 각도로 잘게 나눠 채움 (칼끝만 이으면 곧은 선으로 꺾임)
// - 겉모습은 3D: 2D 단계 띠 대신 칼이 실제로 쓸고 지나간 면(밑동 ~ 칼끝)을 그대로 띠 메시로 → CatTang/BladeTrail 셰이더(더하기 · HDR → 블룸)가
//   칼날 끝선에 흰 심지, 안쪽 · 꼬리로 붉게 옅어짐. 휘두르기 시작 순간이 가장 세고(섬광) 나이에 따라 빠르게 사그라듦
using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    public class BladeStreak : MonoBehaviour
    {
        struct Pt { public Vector3 tip, grip; public float at; }
        readonly List<Pt> pts = new();
        Transform weapon;
        Vector3 tipL, gripL;
        float end, life, born;
        bool open = true;
        Mesh mesh;
        MeshRenderer rend;
        MaterialPropertyBlock mpb;
        static Material mat;
        static readonly int IntensityId = Shader.PropertyToID("_Intensity"), ColorId = Shader.PropertyToID("_Color"), SeedId = Shader.PropertyToID("_Seed");

        // 4장 느와르: 빨강 (예고 DangerRed와 같은 계열). HDR 세기는 _Intensity
        public static readonly Color Ch4Base = new(1f, 0.06f, 0.08f);

        /// <summary>weapon의 칼끝 · 칼 밑동(무기 기준 좌표)을 swing초 동안 따라가며 칼빛을 그림</summary>
        public static BladeStreak Begin(Transform weapon, Vector3 tipLocal, Vector3 gripLocal, float swing, Color baseCol)
        {
            if (!weapon) return null;
            var go = new GameObject("BladeStreak");
            var s = go.AddComponent<BladeStreak>();
            s.weapon = weapon; s.tipL = tipLocal; s.gripL = gripLocal; s.life = swing; s.born = Time.time; s.end = Time.time + swing;
            s.mesh = new Mesh { name = "BladeStreak" };
            s.mesh.MarkDynamic();
            go.AddComponent<MeshFilter>().sharedMesh = s.mesh;
            s.rend = go.AddComponent<MeshRenderer>();
            if (!mat) { var sh = Shader.Find("CatTang/BladeTrail") ?? Shader.Find("Sprites/Default"); mat = new Material(sh); }
            s.rend.sharedMaterial = mat;
            s.rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            s.rend.receiveShadows = false;
            s.mpb = new MaterialPropertyBlock();
            s.mpb.SetColor(ColorId, baseCol);
            s.mpb.SetFloat(SeedId, Random.Range(0f, 100f));
            s.Push(Time.time);
            return s;
        }

        void Push(float now)
        {
            if (!weapon) return;
            Vector3 t = weapon.TransformPoint(tipL), g = weapon.TransformPoint(gripL);
            if (pts.Count > 0)
            {
                var q = pts[pts.Count - 1];
                Vector3 v0 = q.tip - q.grip, v1 = t - g;
                float ang = Vector3.Angle(v0, v1);
                int n = Mathf.Min(16, (int)(ang / 6f) + (int)((t - q.tip).magnitude / 0.2f));
                for (int i = 1; i <= n; i++)
                {
                    float f = (float)i / (n + 1);
                    var gp = Vector3.Lerp(q.grip, g, f);
                    var dir = Vector3.Slerp(v0.normalized, v1.normalized, f);
                    pts.Add(new Pt { grip = gp, tip = gp + dir * Mathf.Lerp(v0.magnitude, v1.magnitude, f), at = Mathf.Lerp(q.at, now, f) });
                }
            }
            pts.Add(new Pt { tip = t, grip = g, at = now });
            if (pts.Count > 200) pts.RemoveAt(0);
        }

        readonly List<Vector3> vs = new();
        readonly List<Vector2> uvs = new();
        readonly List<Color> cs = new();
        readonly List<int> tris = new();
        readonly List<Vector3> tipS = new(), gripS = new();
        readonly List<float> ageS = new();

        void LateUpdate()
        {
            float now = Time.time;
            if (open && (now > end || !weapon)) open = false;
            if (open) Push(now);
            float LIFE = Mathf.Max(0.2f, life * 1.5f);                                    // 한 점이 사라지기까지
            if (!open && (pts.Count == 0 || now - pts[pts.Count - 1].at >= LIFE)) { Destroy(gameObject); return; }
            // 살아 있는 점만 (꼬리부터 사라짐)
            tipS.Clear(); gripS.Clear(); ageS.Clear();
            foreach (var p in pts)
            {
                float age = (now - p.at) / LIFE;
                if (age >= 1 || age < 0) continue;
                tipS.Add(p.tip); gripS.Add(p.grip); ageS.Add(age);
            }
            mesh.Clear();
            int n = tipS.Count;
            if (n < 2) return;
            // 꺾임을 부드럽게 (양 끝은 그대로)
            for (int pass = 0; pass < 2; pass++)
                for (int i = 1; i < n - 1; i++)
                {
                    tipS[i] = (tipS[i - 1] + 2 * tipS[i] + tipS[i + 1]) / 4;
                    gripS[i] = (gripS[i - 1] + 2 * gripS[i] + gripS[i + 1]) / 4;
                }
            vs.Clear(); uvs.Clear(); cs.Clear(); tris.Clear();
            for (int i = 0; i < n; i++)
            {
                float u = n > 1 ? (float)i / (n - 1) : 1;                                // 0 꼬리 → 1 머리
                float fadeA = Mathf.Pow(1 - ageS[i], 1.6f);
                var c = new Color(1, 1, 1, fadeA);
                // 칼 밑동 쪽은 칼날 길이의 15%만 남겨 칼날 면에 붙임 (자루까지 다 칠하지 않게)
                var inner = Vector3.Lerp(gripS[i], tipS[i], 0.15f);
                vs.Add(inner); uvs.Add(new Vector2(u, 0)); cs.Add(c);
                vs.Add(tipS[i]); uvs.Add(new Vector2(u, 1)); cs.Add(c);
                if (i > 0)
                {
                    int a = (i - 1) * 2;
                    tris.Add(a); tris.Add(a + 2); tris.Add(a + 3);
                    tris.Add(a); tris.Add(a + 3); tris.Add(a + 1);
                }
            }
            mesh.SetVertices(vs); mesh.SetUVs(0, uvs); mesh.SetColors(cs); mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            // 섬광: 휘두르기 시작 0.06초가 가장 셈 → 휘두르는 동안 3 → 다 휘두른 뒤 빠르게 사그라듦
            float since = now - born;
            float intensity = Mathf.Lerp(6f, 3f, Mathf.Clamp01(since / 0.06f));
            if (!open) intensity *= Mathf.Clamp01(1 - (now - end) / LIFE);
            mpb.SetFloat(IntensityId, intensity);
            rend.SetPropertyBlock(mpb);
        }
    }
}
