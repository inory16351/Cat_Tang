// Samples the moving blade after animation. The mesh keeps the full sweep width,
// with an HDR outer edge and a soft halo aligned to the unextended blade.
using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    [DefaultExecutionOrder(50)]
    public class BladeStreak : MonoBehaviour
    {
        struct Pt { public Vector3 tip, grip; public float at, energy; }
        readonly List<Pt> pts = new();
        Transform weapon;
        Vector3 tipL, gripL, glowTipL, glowGripL;
        float end, life, born, extend, firstAt = -1, dRef, maxLen;
        bool open = true, drawRibbon;
        Mesh mesh;
        Color baseCol;
        Renderer[] bladeRenderers;
        int[] bladeSlots;
        MaterialPropertyBlock bladeProperties;
        static readonly Dictionary<Transform, BladeStreak> glowingWeapons = new();
        static readonly int GlowId = Shader.PropertyToID("_SwingGlow"),
            GlowStartId = Shader.PropertyToID("_SwingGlowStart"),
            GlowEndId = Shader.PropertyToID("_SwingGlowEnd");
        static Material mat;
        static readonly int ColorId = Shader.PropertyToID("_Color"), SeedId = Shader.PropertyToID("_Seed");

        internal static Material RibbonMaterial
        {
            get
            {
                if (mat) return mat;
                var shader = Shader.Find("CatTang/SlashRibbon") ?? Shader.Find("Sprites/Default");
                mat = new Material(shader);
                var flow = Resources.Load<Texture2D>("FX/Tex/slash_flow");
                if (flow)
                {
                    flow.wrapModeU = TextureWrapMode.Repeat;
                    mat.mainTexture = flow;
                }
                return mat;
            }
        }

        // 4장 느와르: 빨강 (예고 DangerRed와 같은 계열)
        public static readonly Color Ch4Base = new(1f, 0.06f, 0.08f);


        /// <summary>weapon의 칼끝 · 칼 밑동(무기 기준 좌표)을 swing초 동안 따라가며 칼빛을 그림</summary>
        public static BladeStreak Begin(Transform weapon, Vector3 tipLocal, Vector3 gripLocal, float swing, Color baseCol, float extend = 1.3f, Renderer attachedSkin = null,
                                        Vector3? glowTip = null, Vector3? glowGrip = null, bool drawRibbon = true)
        {
            if (!weapon) return null;
            var go = new GameObject("BladeStreak");
            var s = go.AddComponent<BladeStreak>();
            s.weapon = weapon; s.tipL = tipLocal; s.gripL = gripLocal; s.life = swing; s.born = Time.time; s.end = Time.time + swing; s.extend = extend;
            s.glowTipL = glowTip ?? tipLocal;
            s.glowGripL = glowGrip ?? gripLocal;
            s.drawRibbon = drawRibbon;
            s.mesh = new Mesh { name = "BladeStreak" };
            s.mesh.MarkDynamic();
            go.AddComponent<MeshFilter>().sharedMesh = s.mesh;
            var rend = go.AddComponent<MeshRenderer>();
            rend.sharedMaterial = RibbonMaterial;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
            s.baseCol = baseCol;
            var ribbonProperties = new MaterialPropertyBlock();
            ribbonProperties.SetColor(ColorId, baseCol);
            ribbonProperties.SetFloat(SeedId, Random.value);
            rend.SetPropertyBlock(ribbonProperties);
            s.bladeRenderers = attachedSkin ? new[] { attachedSkin } : weapon.GetComponentsInChildren<Renderer>();
            if (s.bladeRenderers.Length > 0)
            {
                s.bladeProperties = new MaterialPropertyBlock();
                s.bladeSlots = new int[s.bladeRenderers.Length];
                for (int i = 0; i < s.bladeRenderers.Length; i++)
                    s.bladeSlots[i] = s.bladeRenderers[i].sharedMaterials.Length;
                glowingWeapons[weapon] = s;
            }
            s.Push(Time.time);
            EffectShadowLight.Blade(weapon, s.glowGripL, s.glowTipL, baseCol, swing);
            return s;
        }

        void Push(float now)
        {
            if (!weapon) return;
            Vector3 g = weapon.TransformPoint(gripL), t = g + (weapon.TransformPoint(tipL) - g) * extend;
            if (firstAt < 0) { firstAt = now; dRef = (t - g).magnitude; }
            float energy = 0f;
            if (pts.Count > 0)
            {
                var q = pts[pts.Count - 1];
                Vector3 v0 = q.tip - q.grip, v1 = t - g;
                float dt = now - q.at;
                if (dt <= 0f) return;
                float ang = Vector3.Angle(v0, v1);
                // Angular speed plus translation keeps short blades and dashes readable.
                energy = Mathf.InverseLerp(25f, 360f, (ang + (g - q.grip).magnitude * 32f) / dt);
                int n = Mathf.Min(24, (int)(ang / 4f) + (int)((t - q.tip).magnitude / 0.15f));
                for (int i = 1; i <= n; i++)
                {
                    float f = (float)i / (n + 1);
                    var gp = Vector3.Lerp(q.grip, g, f);
                    var dir = Vector3.Slerp(v0.normalized, v1.normalized, f);
                    pts.Add(new Pt { grip = gp, tip = gp + dir * Mathf.Lerp(v0.magnitude, v1.magnitude, f), at = Mathf.Lerp(q.at, now, f), energy = Mathf.Lerp(q.energy, energy, f) });
                }
            }
            pts.Add(new Pt { tip = t, grip = g, at = now, energy = energy });
            if (pts.Count > 400) pts.RemoveRange(0, pts.Count - 400);
        }

        readonly List<Vector3> vs = new();
        readonly List<Color> cs = new();
        readonly List<Vector2> uvs = new();
        readonly List<int> tris = new();
        readonly List<Vector3> P = new(), V = new();
        readonly List<float> cum = new();
        readonly List<Vector2> heatAge = new(), bandHeatAge = new();
        readonly List<Vector3> SP = new(), SV = new();

        void LateUpdate()
        {
            float now = Time.time;
            if (open && (now > end || !weapon || !weapon.gameObject.activeInHierarchy))
            {
                open = false;
                ClearBladeGlow();
            }
            if (open) Push(now);
            float lifeTime = life * 0.75f + 0.2f;
            if (!open && now - pts[pts.Count - 1].at >= lifeTime)
            {
                Destroy(gameObject);
                return;
            }
            vs.Clear(); cs.Clear(); uvs.Clear(); tris.Clear();
            if (open)
            {
                float windIn = Mathf.SmoothStep(0f, 1f, (now - born) / 0.045f);
                float windOut = Mathf.SmoothStep(0f, 1f, (end - now) / 0.075f);
                float glow = windIn * windOut * Mathf.Lerp(0.42f, 1f, pts[pts.Count - 1].energy);
                SetBladeGlow(glow);
                BladeHalo(glow);
            }
            if (drawRibbon) BuildBand(now, lifeTime);
            else if (!open) { Destroy(gameObject); return; }
            mesh.Clear();
            mesh.SetVertices(vs); mesh.SetColors(cs); mesh.SetUVs(0, uvs); mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
        }

        void BuildBand(float now, float lifeTime)
        {
            // 살아 있는 점: 칼끝 선 p와 폭 벡터 v(칼끝 → 밑동, 길이 = 이 점의 폭)
            P.Clear(); V.Clear(); heatAge.Clear();
            foreach (var p in pts)
            {
                float age = (now - p.at) / lifeTime;
                if (age >= 1 || age < 0) continue;
                var d = p.grip - p.tip;
                float dl = Mathf.Min(d.magnitude, dRef * 1.1f);
                float width = dl * 0.85f * Mathf.Lerp(0.80f, 1f, p.energy) * Mathf.Pow(1 - age, 0.35f) * Mathf.Min(1f, (p.at - firstAt) / 0.02f + 0.2f);
                P.Add(p.tip); V.Add(d.normalized * width);
                heatAge.Add(new Vector2(p.energy, age));
            }
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
            SP.Clear(); SV.Clear(); bandHeatAge.Clear();
            int j = 0;
            for (int k = 0; k < K; k++)
            {
                float q = (float)k / (K - 1), dd = q * L;
                while (j < n - 2 && cum[j + 1] < dd) j++;
                float span = cum[j + 1] - cum[j], u = span > 1e-6f ? Mathf.Clamp01((dd - cum[j]) / span) : 0;
                float cap = q > 0.92f ? Mathf.Sqrt(Mathf.Max(0, 1 - Mathf.Pow((q - 0.92f) / 0.08f, 2))) * 0.9f + 0.1f : 1f;
                float prof = Mathf.Pow(q, 1.1f) * cap * shrink;
                SP.Add(Vector3.Lerp(P[j], P[j + 1], u)); SV.Add(Vector3.Lerp(V[j], V[j + 1], u) * prof);
                bandHeatAge.Add(Vector2.Lerp(heatAge[j], heatAge[j + 1], u));
            }
            Band(halo: true);
            Band(halo: false);
        }

        void Band(bool halo)
        {
            int baseV = vs.Count, K = SP.Count;
            for (int k = 0; k < K; k++)
            {
                float q = (float)k / (K - 1);
                var sample = bandHeatAge[k];
                var c = new Color(sample.x, sample.y, halo ? 0.5f : 0f, 1);
                vs.Add(halo ? SP[k] - SV[k] * 0.17f : SP[k]); cs.Add(c); uvs.Add(new Vector2(q, 0));
                vs.Add(SP[k] + SV[k] * (halo ? 0.27f : 1f)); cs.Add(c); uvs.Add(new Vector2(q, 1));
                if (k > 0)
                {
                    int a = baseV + (k - 1) * 2;
                    tris.Add(a); tris.Add(a + 2); tris.Add(a + 3);
                    tris.Add(a); tris.Add(a + 3); tris.Add(a + 1);
                }
            }
        }

        void BladeHalo(float glow)
        {
            if (glow <= 0f) return;
            var cam = Camera.main;
            if (!cam) return;
            Vector3 g = weapon.TransformPoint(glowGripL), t = weapon.TransformPoint(glowTipL);
            Vector3 axis = t - g, view = cam.transform.position - (g + t) * 0.5f;
            if (axis.sqrMagnitude < 1e-6f) return;
            var side = Vector3.Cross(axis, view).normalized * axis.magnitude * 0.075f;
            var offset = view.normalized * 0.015f;
            var c = new Color(glow, 0, 1, 1);
            int a = vs.Count;
            vs.Add(g - side + offset); vs.Add(g + side + offset);
            vs.Add(t - side + offset); vs.Add(t + side + offset);
            uvs.Add(Vector2.zero); uvs.Add(Vector2.up);
            uvs.Add(Vector2.right); uvs.Add(Vector2.one);
            for (int i = 0; i < 4; i++) cs.Add(c);
            tris.Add(a); tris.Add(a + 2); tris.Add(a + 3);
            tris.Add(a); tris.Add(a + 3); tris.Add(a + 1);
        }

        void SetBladeGlow(float amount)
        {
            if (bladeProperties == null || !glowingWeapons.TryGetValue(weapon, out var owner) || owner != this) return;
            Vector3 start = weapon.TransformPoint(glowGripL), end = weapon.TransformPoint(glowTipL);
            var glow = baseCol;
            glow.a = amount;
            for (int i = 0; i < bladeRenderers.Length; i++)
            {
                var blade = bladeRenderers[i];
                if (!blade) continue;
                blade.GetPropertyBlock(bladeProperties);
                ApplyGlow(glow, start, end);
                blade.SetPropertyBlock(bladeProperties);
                if (bladeSlots[i] <= 1) continue;
                for (int slot = 0; slot < bladeSlots[i]; slot++)
                {
                    blade.GetPropertyBlock(bladeProperties, slot);
                    if (bladeProperties.isEmpty) continue;
                    ApplyGlow(glow, start, end);
                    blade.SetPropertyBlock(bladeProperties, slot);
                }
            }
        }

        void ApplyGlow(Color color, Vector3 start, Vector3 end)
        {
            bladeProperties.SetColor(GlowId, color);
            bladeProperties.SetVector(GlowStartId, start);
            bladeProperties.SetVector(GlowEndId, end);
        }

        void ClearBladeGlow()
        {
            if (bladeProperties == null || !glowingWeapons.TryGetValue(weapon, out var owner) || owner != this) return;
            if (weapon) SetBladeGlow(0f);
            glowingWeapons.Remove(weapon);
        }

        void OnDestroy()
        {
            ClearBladeGlow();
            if (mesh) Destroy(mesh);
        }
    }
}
