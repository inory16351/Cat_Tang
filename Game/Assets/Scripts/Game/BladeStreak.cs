// 칼빛 줄기 (무기에 붙는 베기 궤적)
// 2026-11-19 사용자: "궤적이 칼날에 붙어야 함(몸 앞에 따로 뜬 호 말고), 4장은 섬광처럼" → "3D니까 그 특성을 고려해서 사실적인 빨간 궤적"
// v2 (2026-11-20 사용자: "랫키호테 평타 이펙트랑 모션 분석해서 3D 구현") — 랫키호테 웹 main.js sampleBlade + fx.js drawStreaks 그대로 3D로:
// - 붙이기: 휘두르는 동안 매 프레임(애니메이터 · 몸 회전이 끝난 LateUpdate) 실제 무기의 칼끝 · 칼 밑동을 기록, 크게 돌았으면 밑동 둘레 각도로 잘게 나눠 채움.
//   칼끝은 밑동에서 extend배(기본 1.3, 랫키호테 FX_BIG 1.5)로 늘려 실제 칼보다 크게 쓸어 보이게
// - 수명 = 휘두르는 시간 × 0.75 + 0.2초. 점마다 나이에 따라 폭이 (1 − 나이)^0.35로 가늘어짐 → 투명해지지 않고 '가늘어지며' 사라짐
// - 모양: 꺾임 5번 다듬기 → 길이 따라 다시 뽑기 → 머리(막 지나간 곳)가 가장 굵고 꼬리는 바늘 끝 (prof = q^1.1 · 머리 둥근 마개),
//   꼬리가 머리를 따라잡으면 전체가 같이 가늘어짐 (shrink)
// - 색: 휘두르는 동안 · 끝난 뒤 0.06초 = 통째로 흰색 → 0.15초까지 4겹 띠 (어둠 · 바탕 · 밝음 · 흰 심지 — 심지는 띠 가운데 줄) → 그 뒤 식은 색 2겹
//   v3 (2026-11-20 사용자: "블렌더로 깔끔하게, 두께는 그대로 색 · 텍스처만 레퍼런스로"): 띠 겹 메시 대신 한 장 + CatTang/SlashRibbon 셰이더가
//   블렌더로 구운 결 텍스처(slash_flow)로 줄무늬를 흘리고 툰 단계(흰 심지 → 뜨거운 색 → 바탕 → 어두운 테두리)로 칠함. 끝나면 결대로 찢겨 사라짐
//   + 휘두르는 동안 칼날 위에 레이저 심지 띠 (사용자: "레이저가 무기 위에 붙어 무기보다 이펙트가 먼저 보이게") — 카메라 쪽으로 살짝 띄워 칼날을 덮음
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
        float end, life, born, extend, firstAt = -1, dRef, maxLen;
        bool open = true;
        Mesh mesh;
        Color baseCol;
        static Material mat;
        MaterialPropertyBlock mpb;
        MeshRenderer rend;
        static readonly int ColorId = Shader.PropertyToID("_Color"), SeedId = Shader.PropertyToID("_Seed");

        // 4장 느와르: 빨강 (예고 DangerRed와 같은 계열)
        public static readonly Color Ch4Base = new(1f, 0.06f, 0.08f);


        /// <summary>weapon의 칼끝 · 칼 밑동(무기 기준 좌표)을 swing초 동안 따라가며 칼빛을 그림</summary>
        public static BladeStreak Begin(Transform weapon, Vector3 tipLocal, Vector3 gripLocal, float swing, Color baseCol, float extend = 1.3f)
        {
            if (!weapon) return null;
            var go = new GameObject("BladeStreak");
            var s = go.AddComponent<BladeStreak>();
            s.weapon = weapon; s.tipL = tipLocal; s.gripL = gripLocal; s.life = swing; s.born = Time.time; s.end = Time.time + swing; s.extend = extend;
            s.mesh = new Mesh { name = "BladeStreak" };
            s.mesh.MarkDynamic();
            go.AddComponent<MeshFilter>().sharedMesh = s.mesh;
            var rend = s.rend = go.AddComponent<MeshRenderer>();
            if (!mat) { var sh = Shader.Find("CatTang/SlashRibbon") ?? Shader.Find("Sprites/Default"); mat = new Material(sh); var tex = Resources.Load<Texture2D>("FX/Tex/slash_flow"); if (tex) { tex.wrapMode = TextureWrapMode.Repeat; mat.mainTexture = tex; } }
            rend.sharedMaterial = mat;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
            s.baseCol = baseCol;
            s.mpb = new MaterialPropertyBlock();
            s.mpb.SetColor(ColorId, baseCol); s.mpb.SetFloat(SeedId, Random.value);
            rend.SetPropertyBlock(s.mpb);
            s.Push(Time.time);
            return s;
        }

        void Push(float now)
        {
            if (!weapon) return;
            Vector3 g = weapon.TransformPoint(gripL), t = g + (weapon.TransformPoint(tipL) - g) * extend;
            if (firstAt < 0) { firstAt = now; dRef = (t - g).magnitude; }
            if (pts.Count > 0)
            {
                var q = pts[pts.Count - 1];
                Vector3 v0 = q.tip - q.grip, v1 = t - g;
                float ang = Vector3.Angle(v0, v1);
                int n = Mathf.Min(24, (int)(ang / 4f) + (int)((t - q.tip).magnitude / 0.15f));
                for (int i = 1; i <= n; i++)
                {
                    float f = (float)i / (n + 1);
                    var gp = Vector3.Lerp(q.grip, g, f);
                    var dir = Vector3.Slerp(v0.normalized, v1.normalized, f);
                    pts.Add(new Pt { grip = gp, tip = gp + dir * Mathf.Lerp(v0.magnitude, v1.magnitude, f), at = Mathf.Lerp(q.at, now, f) });
                }
            }
            pts.Add(new Pt { tip = t, grip = g, at = now });
            if (pts.Count > 400) pts.RemoveAt(0);
        }

        readonly List<Vector3> vs = new();
        readonly List<Color> cs = new();
        readonly List<Vector2> uvs = new();
        readonly List<int> tris = new();
        readonly List<Vector3> P = new(), V = new();
        readonly List<float> cum = new();
        readonly List<Vector3> SP = new(), SV = new();

        void LateUpdate()
        {
            float now = Time.time;
            if (open && (now > end || !weapon)) open = false;
            if (open) Push(now);
            float LIFE = life * 0.75f + 0.2f;
            if (!open && (pts.Count == 0 || now - pts[pts.Count - 1].at >= LIFE)) { Destroy(gameObject); return; }
            // 살아 있는 점: 칼끝 선 p와 폭 벡터 v(칼끝 → 밑동, 길이 = 이 점의 폭)
            P.Clear(); V.Clear();
            foreach (var p in pts)
            {
                float age = (now - p.at) / LIFE;
                if (age >= 1 || age < 0) continue;
                var d = p.grip - p.tip;
                float dl = Mathf.Min(d.magnitude, dRef * 1.1f);
                float width = dl * 0.85f * Mathf.Pow(1 - age, 0.35f) * Mathf.Min(1f, (p.at - firstAt) / 0.02f + 0.2f);
                P.Add(p.tip); V.Add(d.normalized * width);
            }
            mesh.Clear();
            int n = P.Count;
            if (n < 2) return;
            for (int pass = 0; pass < 5; pass++)
                for (int i = 1; i < n - 1; i++) { P[i] = (P[i - 1] + 2 * P[i] + P[i + 1]) / 4; V[i] = (V[i - 1] + 2 * V[i] + V[i + 1]) / 4; }
            // 길이 따라 K점으로 다시 뽑기
            cum.Clear(); cum.Add(0);
            for (int i = 1; i < n; i++) cum.Add(cum[i - 1] + (P[i] - P[i - 1]).magnitude);
            float L = cum[n - 1];
            if (L < 1e-4f) return;
            maxLen = Mathf.Max(maxLen, L);
            float shrink = Mathf.Min(1f, L / (maxLen * 0.75f));
            const int K = 48;
            SP.Clear(); SV.Clear();
            int j = 0;
            for (int k = 0; k < K; k++)
            {
                float q = (float)k / (K - 1), dd = q * L;
                while (j < n - 2 && cum[j + 1] < dd) j++;
                float span = cum[j + 1] - cum[j], u = span > 1e-6f ? Mathf.Clamp01((dd - cum[j]) / span) : 0;
                float cap = q > 0.92f ? Mathf.Sqrt(Mathf.Max(0, 1 - Mathf.Pow((q - 0.92f) / 0.08f, 2))) * 0.9f + 0.1f : 1f;
                float prof = Mathf.Pow(q, 1.1f) * cap * shrink;
                SP.Add(Vector3.Lerp(P[j], P[j + 1], u)); SV.Add(Vector3.Lerp(V[j], V[j + 1], u) * prof);
            }
            vs.Clear(); cs.Clear(); uvs.Clear(); tris.Clear();
            float fade = open ? 0 : now - end;
            float heat = open ? 1f : Mathf.Clamp01(1 - fade / 0.16f);                 // 끝난 뒤 0.16초에 식음
            float gone = open ? 0f : Mathf.Clamp01((fade - 0.04f) / (LIFE * 0.7f));    // 그다음 결대로 찢겨 사라짐
            Band(new Color(heat, gone, 0, 1));
            if (open && weapon) Beam(now);
            mesh.SetVertices(vs); mesh.SetColors(cs); mesh.SetUVs(0, uvs); mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
        }

        void Band(Color c)
        {
            int baseV = vs.Count, K = SP.Count;
            for (int k = 0; k < K; k++)
            {
                float q = (float)k / (K - 1);
                vs.Add(SP[k]); cs.Add(c); uvs.Add(new Vector2(q, 0));
                vs.Add(SP[k] + SV[k]); cs.Add(c); uvs.Add(new Vector2(q, 1));
                if (k > 0)
                {
                    int a = baseV + (k - 1) * 2;
                    tris.Add(a); tris.Add(a + 2); tris.Add(a + 3);
                    tris.Add(a); tris.Add(a + 3); tris.Add(a + 1);
                }
            }
        }

        // 칼날 레이저: 지금 칼 밑동 → 칼끝(늘린 길이)을 따라 카메라를 보는 띠, 칼날을 덮게 카메라 쪽으로 띄움. 폭 = 칼날 길이의 12%, 끝은 뾰족
        void Beam(float now)
        {
            var cam = Camera.main; if (!cam) return;
            Vector3 g = weapon.TransformPoint(gripL), t = g + (weapon.TransformPoint(tipL) - g) * extend;
            var axis = t - g; float L = axis.magnitude; if (L < 1e-3f) return;
            var mid = (g + t) * 0.5f;
            var toCam = (cam.transform.position - mid).normalized;
            var side = Vector3.Cross(axis / L, toCam).normalized;
            var lift = toCam * Mathf.Min(0.6f, L * 0.12f);
            float w = L * 0.12f * Mathf.Clamp01((now - born) / 0.03f + 0.3f);
            var c = new Color(1, 0, 1, 1);
            int baseV = vs.Count; const int K = 10;
            for (int k = 0; k < K; k++)
            {
                float q = (float)k / (K - 1);
                float taper = Mathf.Sin(Mathf.Lerp(0.25f, 1f, q) * Mathf.PI * 0.5f) * (q > 0.85f ? Mathf.Sqrt(1 - Mathf.Pow((q - 0.85f) / 0.15f, 2)) : 1f);
                var p = Vector3.Lerp(g, t, q) + lift;
                vs.Add(p - side * w * taper * 0.5f); cs.Add(c); uvs.Add(new Vector2(q, 0));
                vs.Add(p + side * w * taper * 0.5f); cs.Add(c); uvs.Add(new Vector2(q, 1));
                if (k > 0)
                {
                    int a = baseV + (k - 1) * 2;
                    tris.Add(a); tris.Add(a + 2); tris.Add(a + 3);
                    tris.Add(a); tris.Add(a + 3); tris.Add(a + 1);
                }
            }
        }
    }
}
