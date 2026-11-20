// 돌진 꼬리 v2 (2026-11-20 사용자: 돌진 경로 뒤에 몬스터 컨셉 이펙트 → "이어 붙인 느낌 · 이미지 느낌이 남아 있으니 확실하게 VFX로")
// 구운 그림을 길 위에 도장처럼 찍던 방식(이음매가 보임 · 판 그림)을 버리고, 실시간 3D 두 겹 + 입자로:
//  ① 바닥 자국(Wake): 돌진한 실제 경로를 따라 이어진 띠 메시 한 장 (CatTang/StylizedVfx: 흐르는 노이즈 · 보로노이 · 줄무늬 + 계단 색 + 잉크 띠).
//     점마다 '지난 시간'을 U에 넣어(U 1 = 막 지나간 머리, 0 = 오래된 꼬리) 셰이더의 꼬리 흩어짐(_TailFade)이 오래된 쪽부터 구멍 뚫리며 사라짐 → 끊김 없는 한 줄기
//  ② 솟는 벽(Wall): 같은 경로를 바닥에서 카메라 위쪽으로 밀어 올린 띠 (항상 카메라를 향해 두께가 보임) — 모래 바람 · 증기 · 물마루 · 허공의 높이
//  ③ 3D 입자: 블렌더 이펙트 메시(구름 덩어리 · 톱니 · 유리 · 불티 · 물방울)를 거리마다 뿜음. 게임 오브젝트 없는 입자 풀(DashFx)로 그림
// 돌진 알아채기: 모든 적(EnemyFactory.Root)에 붙어 평소 이속 1.9배(최소 5.5m/초) 넘게 바라보는 쪽으로 달리면 돌진. 밀려남 · 순간 이동(한 프레임 3m+)은 뺌
// 스타일: 1장 sand / 2장 steam / 3장 water / 4장 laser / 5장 rift / 거울 고양이 mirror
using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    public class DashTrail : MonoBehaviour
    {
        // ---------------- 스타일 표 ----------------
        public struct Debris
        {
            public string[] meshes; public FxMesh.Kind kind; public Color tint;
            public float perMeter, sizeMin, sizeMax, back, up, side, life; public bool gravity; public float spin;
        }
        public class Style
        {
            public StylizedFx.Look ground, wall;
            public float width = 1.2f, wallH = 0.9f, tailFade = 1.3f, life = 0.6f;
            public Color puffTint; public float puffPerMeter, puffMin, puffMax, puffLife = 0.7f;
            public Debris[] debris = System.Array.Empty<Debris>();
        }

        static StylizedFx.Look L(Color core, Color mid, Color edge, Color ink, float pattern, Vector2 scale, Vector2 scroll, float opacity,
                                 float b1 = 0.5f, float b2 = 0.3f, float taperV = 0.3f) =>
            new StylizedFx.Look { core = core, mid = mid, edge = edge, ink = ink, pattern = pattern, scale = scale, scroll = scroll, opacity = opacity,
                                  band1 = b1, band2 = b2, taperV = taperV, taperU = 0f, fresnel = 0f };
        static Color H(string hex, float k = 1f) { ColorUtility.TryParseHtmlString("#" + hex, out var c); return new Color(c.r * k, c.g * k, c.b * k, 1); }

        static readonly Dictionary<string, Style> Styles = new()
        {
            // 1장 서부 사막: 바닥에 끌린 모래 자국 + 몸 뒤로 일어나는 모래 바람 벽 + 모래 구름 덩어리 · 황동 톱니 · 모래알
            ["sand"] = new Style
            {
                ground = L(H("F8E6B8"), H("D9A35B"), H("9C6232"), H("3B2414"), 0, new Vector2(5f, 2.2f), new Vector2(-1.2f, 0), 1f, 0.55f, 0.33f, 0.35f),
                wall = L(H("FBEBC2"), H("E2B271"), H("A86A36"), H("3B2414"), 0, new Vector2(4f, 1.6f), new Vector2(-2.2f, -0.6f), 1f, 0.55f, 0.32f, 0.42f),
                width = 1.25f, wallH = 0.95f, tailFade = 1.25f, life = 0.65f,
                puffTint = new Color(0.86f, 0.64f, 0.36f), puffPerMeter = 1.6f, puffMin = 0.45f, puffMax = 0.85f,
                debris = new[]
                {
                    new Debris { meshes = new[] { "fx_gear" }, kind = FxMesh.Kind.Ball, tint = new Color(0.95f, 0.7f, 0.25f), perMeter = 0.22f, sizeMin = 0.28f, sizeMax = 0.42f, back = 3f, up = 4.5f, side = 1.5f, life = 0.9f, gravity = true, spin = 600f },
                    new Debris { meshes = new[] { "fx_ball_0", "fx_ball_1" }, kind = FxMesh.Kind.Ball, tint = new Color(0.8f, 0.55f, 0.28f), perMeter = 2.2f, sizeMin = 0.12f, sizeMax = 0.2f, back = 3.5f, up = 3f, side = 2f, life = 0.6f, gravity = true },
                },
            },
            // 2장 멈춘 역: 증기 벽이 크게 일고 흰 증기 덩어리, 바닥을 긁는 레일 불티 · 석탄 조각
            ["steam"] = new Style
            {
                ground = L(H("FFFFFF"), H("D6D9E2"), H("9CA1B3"), H("2C2A3A"), 0, new Vector2(5f, 2f), new Vector2(-1.5f, 0), 0.85f, 0.55f, 0.32f, 0.4f),
                wall = L(H("FFFFFF"), H("E6E8EF"), H("A3A8B8"), H("2C2A3A"), 0, new Vector2(3.5f, 1.4f), new Vector2(-1.6f, -0.9f), 1f, 0.5f, 0.28f, 0.45f),
                width = 1.3f, wallH = 1.35f, tailFade = 1.1f, life = 0.75f,
                puffTint = new Color(0.93f, 0.94f, 0.98f), puffPerMeter = 1.8f, puffMin = 0.6f, puffMax = 1.1f, puffLife = 0.85f,
                debris = new[]
                {
                    new Debris { meshes = new[] { "fx_spike_0" }, kind = FxMesh.Kind.Spike, tint = new Color(1f, 0.55f, 0.12f), perMeter = 2.5f, sizeMin = 0.09f, sizeMax = 0.15f, back = 7f, up = 1.6f, side = 2.5f, life = 0.35f, gravity = true },
                    new Debris { meshes = new[] { "fx_shard_0", "fx_shard_1" }, kind = FxMesh.Kind.Ball, tint = new Color(0.42f, 0.38f, 0.46f), perMeter = 0.6f, sizeMin = 0.18f, sizeMax = 0.28f, back = 3f, up = 3.5f, side = 1.5f, life = 0.7f, gravity = true, spin = 500f },
                },
            },
            // 3장 거울 호수: 줄무늬 물결 자국 + 몸 뒤로 말려 오르는 물마루 벽 + 물거품 · 물방울 · 장미 · 하늘 반짝임
            ["water"] = new Style
            {
                ground = L(H("F4FDFF"), H("9EE3F5"), H("4FA9D2"), H("1D3B5A"), 0, new Vector2(1.6f, 6f), new Vector2(-3f, 0), 0.9f, 0.58f, 0.36f, 0.35f),   // 길을 따라 길게 늘어진 물결 결
                wall = L(H("FFFFFF"), H("BFEFFF"), H("5FB6DC"), H("1D3B5A"), 0, new Vector2(1.8f, 2.4f), new Vector2(-2.6f, -1.2f), 1f, 0.6f, 0.36f, 0.4f),
                width = 1.2f, wallH = 1.0f, tailFade = 1.3f, life = 0.6f,
                puffTint = new Color(0.86f, 0.97f, 1f), puffPerMeter = 0.8f, puffMin = 0.26f, puffMax = 0.42f, puffLife = 0.5f,
                debris = new[]
                {
                    new Debris { meshes = new[] { "fx_water_drop" }, kind = FxMesh.Kind.Spike, tint = new Color(0.6f, 0.9f, 1f), perMeter = 5f, sizeMin = 0.12f, sizeMax = 0.2f, back = 3.5f, up = 5f, side = 2.2f, life = 0.6f, gravity = true },
                    new Debris { meshes = new[] { "fx_sparkle" }, kind = FxMesh.Kind.Core, tint = new Color(1f, 0.72f, 0.88f), perMeter = 0.35f, sizeMin = 0.25f, sizeMax = 0.35f, back = 1.2f, up = 2f, side = 1f, life = 0.45f, spin = 200f },
                    new Debris { meshes = new[] { "fx_sparkle" }, kind = FxMesh.Kind.Core, tint = new Color(0.75f, 0.94f, 1f), perMeter = 0.35f, sizeMin = 0.22f, sizeMax = 0.32f, back = 1.2f, up = 2f, side = 1f, life = 0.45f, spin = -200f },
                },
            },
            // 4장 느와르: 바닥을 긋는 빨간 레이저 자국(더해지는 빛, 빠르게 흐르는 줄) + 낮은 레이저 벽 + 회색 연기 조금 · 은빛 톱니 · 바늘 · 빨간 불티
            ["laser"] = new Style
            {
                ground = L(new Color(2.4f, 1.3f, 1.3f), new Color(2.0f, 0.12f, 0.14f), new Color(0.7f, 0.02f, 0.05f), new Color(0.2f, 0f, 0.02f), 0, new Vector2(1.2f, 8f), new Vector2(-12f, 0), 0f, 0.66f, 0.26f, 0.45f),   // 길 따라 그어진 레이저 결 (심지는 가늘게 — 넓으면 하얗게 날아감)
                wall = L(new Color(2.0f, 0.9f, 0.9f), new Color(1.6f, 0.1f, 0.12f), new Color(0.5f, 0.02f, 0.04f), new Color(0.2f, 0f, 0.02f), 0, new Vector2(1.0f, 5f), new Vector2(-14f, 0), 0f, 0.72f, 0.32f, 0.5f),
                width = 0.95f, wallH = 0.32f, tailFade = 1.15f, life = 0.6f,
                puffTint = new Color(0.32f, 0.32f, 0.36f), puffPerMeter = 0.3f, puffMin = 0.22f, puffMax = 0.36f, puffLife = 0.45f,   // 느와르: 검은 그을음 조금만
                debris = new[]
                {
                    new Debris { meshes = new[] { "fx_gear" }, kind = FxMesh.Kind.Ball, tint = new Color(0.9f, 0.9f, 0.95f), perMeter = 0.18f, sizeMin = 0.25f, sizeMax = 0.38f, back = 3f, up = 4f, side = 1.2f, life = 0.8f, gravity = true, spin = 700f },
                    new Debris { meshes = new[] { "fx_shard_0", "fx_shard_1" }, kind = FxMesh.Kind.Ball, tint = new Color(0.92f, 0.92f, 0.96f), perMeter = 1.2f, sizeMin = 0.2f, sizeMax = 0.3f, back = 4f, up = 3f, side = 2f, life = 0.55f, gravity = true, spin = 800f },
                    new Debris { meshes = new[] { "fx_spike_0" }, kind = FxMesh.Kind.Spike, tint = new Color(1f, 0.12f, 0.12f), perMeter = 2.8f, sizeMin = 0.09f, sizeMax = 0.14f, back = 9f, up = 0.8f, side = 2f, life = 0.3f, gravity = false },
                },
            },
            // 5장 무너지는 시간: 보로노이 금 간 허공 자국(속 보라 · 자홍 테) + 보라 허공 벽 + 보라 허공 덩어리 · 자홍 유리 조각 · 금 불티
            ["rift"] = new Style
            {
                ground = L(new Color(2.3f, 1.6f, 2.5f), new Color(1.9f, 0.3f, 1.3f), new Color(0.35f, 0.1f, 0.6f), new Color(0.08f, 0.03f, 0.15f), 1, new Vector2(6f, 2.2f), new Vector2(-1.4f, 0), 0.65f, 0.55f, 0.3f, 0.32f),
                wall = L(new Color(1.6f, 1.0f, 2.0f), new Color(0.6f, 0.25f, 1.1f), new Color(0.15f, 0.06f, 0.32f), new Color(0.05f, 0.02f, 0.1f), 0, new Vector2(4f, 1.5f), new Vector2(-1.8f, -0.8f), 0.75f, 0.55f, 0.3f, 0.42f),
                width = 1.2f, wallH = 0.85f, tailFade = 1.3f, life = 0.6f,
                puffTint = new Color(0.45f, 0.2f, 0.78f), puffPerMeter = 1.1f, puffMin = 0.35f, puffMax = 0.65f, puffLife = 0.6f,
                debris = new[]
                {
                    new Debris { meshes = new[] { "fx_glass_0", "fx_glass_1" }, kind = FxMesh.Kind.Ball, tint = new Color(1f, 0.4f, 0.85f), perMeter = 1.8f, sizeMin = 0.18f, sizeMax = 0.3f, back = 3.5f, up = 3.5f, side = 2f, life = 0.6f, gravity = true, spin = 700f },
                    new Debris { meshes = new[] { "fx_spike_0" }, kind = FxMesh.Kind.Spike, tint = new Color(1f, 0.78f, 0.3f), perMeter = 1.0f, sizeMin = 0.08f, sizeMax = 0.12f, back = 6f, up = 1.5f, side = 2f, life = 0.35f, gravity = false },
                },
            },
            // 거울 고양이: 금 간 거울 같은 은빛 자국(보로노이, 빛) + 벽 없음 · 은빛 거울 조각이 깨진 반사처럼 흩어짐 · 흰 반짝임
            ["mirror"] = new Style
            {
                ground = L(new Color(2.6f, 2.7f, 3.0f), new Color(1.1f, 1.2f, 1.6f), new Color(0.4f, 0.45f, 0.7f), new Color(0.1f, 0.12f, 0.2f), 1, new Vector2(7f, 2f), new Vector2(-1f, 0), 0.35f, 0.5f, 0.28f, 0.35f),
                wall = default, width = 1.1f, wallH = 0f, tailFade = 1.2f, life = 0.6f,
                puffPerMeter = 0f,
                debris = new[]
                {
                    new Debris { meshes = new[] { "fx_glass_0", "fx_glass_1", "fx_shard_0" }, kind = FxMesh.Kind.Ball, tint = new Color(0.86f, 0.9f, 1f), perMeter = 6f, sizeMin = 0.16f, sizeMax = 0.32f, back = 3f, up = 3f, side = 2f, life = 0.6f, gravity = true, spin = 800f },
                    new Debris { meshes = new[] { "fx_sparkle" }, kind = FxMesh.Kind.Core, tint = new Color(0.95f, 0.97f, 1f), perMeter = 1.0f, sizeMin = 0.3f, sizeMax = 0.42f, back = 1f, up = 1.5f, side = 1f, life = 0.4f, spin = 300f },
                },
            },
        };

        // ---------------- 붙이기 · 돌진 알아채기 ----------------
        public string style;
        public float size = 1f;
        Enemy enemy;
        Transform model;
        Style st;
        Vector3 last, lastEmit;
        float speed, sinceDash = 9f, puffAcc;
        float[] debrisAcc;
        bool dashing;

        public static void Attach(Enemy e) { if (e && !e.GetComponent<DashTrail>()) e.gameObject.AddComponent<DashTrail>(); }

        static string StyleOf(Enemy e)
        {
            if (e.GetComponent<Ch5MidBoss>()) return "mirror";
            int ch = e.chapter != null ? e.chapter.Id : 1;
            return ch switch { 2 => "steam", 3 => "water", 4 => "laser", >= 5 => "rift", _ => "sand" };
        }

        void Start()
        {
            enemy = GetComponent<Enemy>();
            model = transform.Find("Model");
            last = transform.position;
            if (string.IsNullOrEmpty(style) && enemy) style = StyleOf(enemy);
            st = Styles.TryGetValue(style ?? "", out var s) ? s : Styles["sand"];
            debrisAcc = new float[st.debris.Length];
            var col = GetComponent<CapsuleCollider>();
            if (col) size = Mathf.Clamp(col.radius * 1.5f, 0.8f, 2.8f);
        }

        void LateUpdate()
        {
            float dt = Mathf.Max(Time.deltaTime, 1e-4f);
            var p = transform.position;
            var d = p - last; d.y = 0;
            last = p;
            float dist = d.magnitude;
            if (dist > 3f) { EndDash(); return; }                                  // 순간 이동 · 점멸
            speed = Mathf.Lerp(speed, dist / dt, 1 - Mathf.Exp(-14f * dt));
            float need = Mathf.Max(5.5f, (enemy ? enemy.speed : 3f) * 1.9f);
            var dir = dist > 1e-5f ? d / dist : Flat(transform.forward);
            float face = Vector3.Dot(Flat(transform.forward), dir);
            if (model) face = Mathf.Max(face, Vector3.Dot(Flat(model.forward), dir));
            bool now = speed > need && face > 0.25f;
            sinceDash += dt;
            if (now && !dashing && sinceDash > 0.25f) BeginDash(p, dir);
            if (!now && dashing) EndDash();
            if (dashing)
            {
                sinceDash = 0;
                wake?.Add(p, Time.time);
                Emit(p, dir, dist);
            }
        }

        static Vector3 Flat(Vector3 v) { v.y = 0; return v.sqrMagnitude > 1e-6f ? v.normalized : Vector3.forward; }

        // ---------------- 자국 (풀: 적마다 자국 둘을 돌려 씀) ----------------
        Wake wake;
        readonly Wake[] wakes = new Wake[2];
        int wakeIdx;

        void BeginDash(Vector3 p, Vector3 dir)
        {
            dashing = true;
            wakeIdx = (wakeIdx + 1) % wakes.Length;
            wakes[wakeIdx] ??= Wake.Make(transform, st, size);
            wake = wakes[wakeIdx];
            wake.Restart(p, Time.time);
            lastEmit = p;
            // 박차고 나가는 순간: 발밑에서 사방으로 한 번 크게
            var side = Vector3.Cross(Vector3.up, dir);
            if (st.puffPerMeter >= 1f)
                for (int i = 0; i < 4; i++)
                {
                    var o = Quaternion.Euler(0, Random.Range(-80f, 80f), 0) * -dir;
                    DashFx.Emit(DashFx.M("fx_ball_" + Random.Range(0, 3)), FxMesh.Kind.Ball, p + Vector3.up * 0.25f * size + side * Random.Range(-0.4f, 0.4f) * size,
                        (o * Random.Range(3f, 6f) + Vector3.up * Random.Range(0.5f, 2f)) * size, Vector3.one * Random.Range(st.puffMin, st.puffMax) * size * 1.1f,
                        st.puffLife * 0.9f, st.puffTint, false, 5f, Random.Range(-150f, 150f));
                }
            for (int k = 0; k < st.debris.Length; k++) debrisAcc[k] = 1f;            // 첫 조각 바로
        }

        void EndDash()
        {
            dashing = false;
            wake?.Stop();
            wake = null;
        }

        // ---------------- 입자 (지나간 거리만큼 고르게) ----------------
        void Emit(Vector3 p, Vector3 dir, float dist)
        {
            var side = Vector3.Cross(Vector3.up, dir);
            if (st.puffPerMeter > 0)
            {
                puffAcc += dist * st.puffPerMeter;
                while (puffAcc >= 1f)
                {
                    puffAcc -= 1f;
                    float along = Random.Range(0f, dist);
                    var at = p - dir * (along + 0.35f * size) + side * Random.Range(-0.5f, 0.5f) * st.width * size + Vector3.up * Random.Range(0.15f, 0.45f) * size;
                    var vel = (-dir * Random.Range(1.2f, 2.6f) + side * Random.Range(-1.2f, 1.2f) + Vector3.up * Random.Range(0.6f, 1.8f)) * size;
                    DashFx.Emit(DashFx.M("fx_ball_" + Random.Range(0, 3)), FxMesh.Kind.Ball, at, vel, Vector3.one * Random.Range(st.puffMin, st.puffMax) * size,
                        st.puffLife * Random.Range(0.8f, 1.15f), st.puffTint, false, 3.5f, Random.Range(-120f, 120f));
                }
            }
            for (int k = 0; k < st.debris.Length; k++)
            {
                ref var db = ref st.debris[k];
                debrisAcc[k] += dist * db.perMeter;
                while (debrisAcc[k] >= 1f)
                {
                    debrisAcc[k] -= 1f;
                    var at = p - dir * Random.Range(0.2f, 0.6f) * size + side * Random.Range(-0.4f, 0.4f) * st.width * size + Vector3.up * Random.Range(0.1f, 0.4f) * size;
                    var vel = (-dir * db.back * Random.Range(0.6f, 1.2f) + side * Random.Range(-db.side, db.side) + Vector3.up * db.up * Random.Range(0.5f, 1.1f)) * Mathf.Sqrt(size);
                    var mesh = DashFx.M(db.meshes[Random.Range(0, db.meshes.Length)]);
                    float s = Random.Range(db.sizeMin, db.sizeMax) * size;
                    var sz = db.kind == FxMesh.Kind.Spike ? new Vector3(s, s, s * 4f) : Vector3.one * s;
                    DashFx.Emit(mesh, db.kind, at, vel, sz, db.life * Random.Range(0.8f, 1.2f), db.tint, db.gravity, db.gravity ? 0.6f : 3f, db.spin * Random.Range(0.6f, 1.2f));
                }
            }
        }

        void OnDestroy()
        {
            // 적이 사라져도 남은 자국은 스스로 다 흩어진 뒤 지워짐
            foreach (var w in wakes)
            {
                if (!w) continue;
                if (w.gameObject.activeSelf) w.Orphan(); else Destroy(w.gameObject);   // 꺼져 쉬던 자국은 바로 지움
            }
        }

        // ================= 자국 띠 (바닥 + 솟는 벽) =================
        class Wake : MonoBehaviour
        {
            const int MaxPts = 96;
            const float Step = 0.22f;                       // 점 간격(m)
            static Material mat;
            static readonly int AgeId = Shader.PropertyToID("_Age"), SwId = Shader.PropertyToID("_Sweep"), TfId = Shader.PropertyToID("_TailFade"), AlphaId = Shader.PropertyToID("_Alpha"), OffsetId = Shader.PropertyToID("_Offset");

            Style st; float size, groundY;
            readonly Vector3[] pos = new Vector3[MaxPts];
            readonly float[] time = new float[MaxPts];
            int count; bool live, orphan;
            Vector3 head; float headTime;
            Strip ground, wall;

            class Strip
            {
                public Mesh mesh; public MeshRenderer mr; public MaterialPropertyBlock mpb;
                public readonly List<Vector3> v = new(MaxPts * 2 + 2);
                public readonly List<Vector2> uv = new(MaxPts * 2 + 2);
                public readonly List<int> tri = new(MaxPts * 6);
            }

            public static Wake Make(Transform owner, Style st, float size)
            {
                var go = new GameObject("DashWake");
                var w = go.AddComponent<Wake>();
                w.st = st; w.size = size;
                w.ground = w.MakeStrip("Ground", st.ground);
                if (st.wallH > 0) w.wall = w.MakeStrip("Wall", st.wall);
                VividLayer.SetAll(go);
                go.SetActive(false);
                return w;
            }

            Strip MakeStrip(string name, StylizedFx.Look look)
            {
                var sh = Shader.Find("CatTang/StylizedVfx");
                if (!mat && sh) mat = new Material(sh) { renderQueue = 3006, name = "DashWake" };
                var g = new GameObject(name);
                g.transform.SetParent(transform, false);
                var s = new Strip { mesh = new Mesh { name = "DashWake_" + name }, mpb = new MaterialPropertyBlock() };
                s.mesh.MarkDynamic();
                g.AddComponent<MeshFilter>().sharedMesh = s.mesh;
                s.mr = g.AddComponent<MeshRenderer>();
                s.mr.sharedMaterial = mat;
                s.mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                s.mr.receiveShadows = false;
                s.mpb.SetColor("_ColCore", look.core); s.mpb.SetColor("_ColMid", look.mid); s.mpb.SetColor("_ColEdge", look.edge); s.mpb.SetColor("_ColInk", look.ink);
                s.mpb.SetFloat("_Pattern", look.pattern); s.mpb.SetVector("_Scale", look.scale); s.mpb.SetVector("_Scroll", look.scroll);
                s.mpb.SetFloat("_Band1", look.band1); s.mpb.SetFloat("_Band2", look.band2); s.mpb.SetFloat("_Ink", 0.08f);
                s.mpb.SetFloat("_TaperV", look.taperV); s.mpb.SetFloat("_TaperU", 0f); s.mpb.SetFloat("_Fresnel", 0f);
                s.mpb.SetFloat("_Opacity", look.opacity); s.mpb.SetFloat(TfId, st.tailFade); s.mpb.SetFloat(SwId, 1.0f); s.mpb.SetFloat(AlphaId, 1f); s.mpb.SetFloat(AgeId, 0.02f);
                return s;
            }

            public void Restart(Vector3 p, float t)
            {
                gameObject.SetActive(true);
                count = 0; live = true; groundY = p.y;
                AddPoint(p, t);
                head = p; headTime = t;
                foreach (var s in new[] { ground, wall }) if (s != null) s.mpb.SetVector(OffsetId, new Vector4(Random.value * 10f, Random.value * 10f));
            }

            public void Add(Vector3 p, float t)
            {
                head = p; headTime = t;
                // 점 간격마다 고르게 (한 프레임에 멀리 가면 사이를 채움 → 꺾임 없이 매끈)
                var lastP = pos[count - 1];
                float dd = Vector3.Distance(Flat(lastP), Flat(p));
                if (dd < Step) return;
                int n = Mathf.Min(8, Mathf.FloorToInt(dd / Step));
                float lastT = time[count - 1];
                for (int i = 1; i <= n; i++) { float k = i / (float)n; AddPoint(Vector3.Lerp(lastP, p, k), Mathf.Lerp(lastT, t, k)); }
            }

            static Vector3 Flat(Vector3 v) { v.y = 0; return v; }

            void AddPoint(Vector3 p, float t)
            {
                if (count == MaxPts) { System.Array.Copy(pos, 1, pos, 0, MaxPts - 1); System.Array.Copy(time, 1, time, 0, MaxPts - 1); count--; }
                pos[count] = p; time[count] = t; count++;
            }

            public void Stop() { live = false; }
            public void Orphan() { live = false; orphan = true; }

            void LateUpdate()
            {
                float now = Time.time, L = st.life;
                // 다 흩어진 점은 앞에서 버림 (하나는 남겨 이어지게)
                int drop = 0;
                while (drop < count - 1 && now - time[drop + 1] > L) drop++;
                if (drop > 0) { System.Array.Copy(pos, drop, pos, 0, count - drop); System.Array.Copy(time, drop, time, 0, count - drop); count -= drop; }
                bool any = count > 0 && now - (live ? now : time[count - 1]) < L;
                if (!any)
                {
                    if (orphan) Destroy(gameObject); else gameObject.SetActive(false);
                    return;
                }
                var cam = Camera.main;
                var up = cam ? cam.transform.up : Vector3.up;
                Build(ground, false, up, now, L);
                if (wall != null) Build(wall, true, up, now, L);
            }

            void Build(Strip s, bool isWall, Vector3 camUp, float now, float L)
            {
                s.v.Clear(); s.uv.Clear(); s.tri.Clear();
                int n = count + (live ? 1 : 0);
                if (n < 2) { s.mesh.Clear(); return; }
                for (int i = 0; i < n; i++)
                {
                    Vector3 p = i < count ? pos[i] : head;
                    float t = i < count ? time[i] : headTime;
                    Vector3 prev = i > 0 ? (i - 1 < count ? pos[i - 1] : head) : p;
                    Vector3 next = i + 1 < n ? (i + 1 < count ? pos[i + 1] : head) : p;
                    var dir = Flat(next - prev); if (dir.sqrMagnitude < 1e-6f) dir = Vector3.forward; dir.Normalize();
                    float u = Mathf.Clamp01(1f - (now - t) / L);                // 1 = 막 지나간 머리, 0 = 다 흩어질 꼬리
                    float headIn = Mathf.Clamp01((count - 1 - i + (live ? 1 : 0)) / 3f + 0.35f);   // 몸 바로 뒤는 조금 좁게 시작 (몸에서 뿜어 나오는 느낌)
                    var basePt = new Vector3(p.x, groundY + (isWall ? 0.06f : 0.04f), p.z);
                    if (!isWall)
                    {
                        float hw = st.width * size * 0.5f * (0.35f + 0.65f * Mathf.Sqrt(u)) * (0.6f + 0.4f * headIn) * (1f + 0.35f * (1f - u));   // 꼬리로 갈수록 퍼졌다 가늘어짐
                        var side = Vector3.Cross(Vector3.up, dir) * hw;
                        s.v.Add(basePt - side); s.v.Add(basePt + side);
                    }
                    else
                    {
                        float hh = st.wallH * size * (0.15f + 0.85f * u) * (0.55f + 0.45f * headIn);
                        s.v.Add(basePt); s.v.Add(basePt + camUp * hh);
                    }
                    s.uv.Add(new Vector2(u, 0)); s.uv.Add(new Vector2(u, 1));
                    if (i < n - 1) { int q = i * 2; s.tri.Add(q); s.tri.Add(q + 1); s.tri.Add(q + 2); s.tri.Add(q + 1); s.tri.Add(q + 3); s.tri.Add(q + 2); }
                }
                s.mesh.Clear();
                s.mesh.SetVertices(s.v); s.mesh.SetUVs(0, s.uv); s.mesh.SetTriangles(s.tri, 0);
                s.mesh.RecalculateNormals(); s.mesh.RecalculateBounds();
                s.mr.SetPropertyBlock(s.mpb);
            }

            void OnDestroy()
            {
                if (ground != null && ground.mesh) Destroy(ground.mesh);
                if (wall != null && wall.mesh) Destroy(wall.mesh);
            }
        }
    }
}
