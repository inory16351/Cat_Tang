// 돌진 꼬리 3D 입자 (2026-11-20 사용자: 이어 붙인 느낌 · 이미지 느낌 없이 확실한 VFX + 최적화 = 오브젝트 풀링 고려)
// 게임 오브젝트를 만들지 않는 입자 풀: 미리 잡아 둔 배열(MaxParticles)에 입자를 채우고, 매 프레임 Graphics.DrawMesh로 그림.
//   → Instantiate / Destroy · 컴포넌트 · 트랜스폼 비용 0, 실행 중 메모리 할당 0 (MaterialPropertyBlock 하나를 돌려 씀 — DrawMesh가 그 순간 값을 복사)
// 메시 = 블렌더 이펙트 메시(Resources/FX/Meshes: fx_ball 구름 덩어리 · fx_shard · fx_glass 유리 · fx_spike 불티 · fx_water_drop · fx_sparkle · fx_gear 톱니)
// 재질 = CatTang/ToonFire (툰 색 띠 + 잉크 외곽선 + 노이즈 구멍으로 녹음), 색은 FxTint로 입자마다. 크기 · 열 · 녹음 곡선은 FxMesh.Evaluate와 같음
using UnityEngine;

namespace CatTang
{
    public class DashFx : MonoBehaviour
    {
        public const int MaxParticles = 600;

        struct P
        {
            public bool alive, gravity;
            public Mesh mesh;
            public FxMesh.Kind kind;
            public Vector3 pos, vel, size, spinAxis;
            public Quaternion rot;
            public float born, life, delay, spin, drag, seed;
            public Color tint;
        }

        static DashFx inst;
        static Material mat;
        readonly P[] ps = new P[MaxParticles];
        int cursor, aliveCount;
        MaterialPropertyBlock mpb;
        public static int Alive => inst ? inst.aliveCount : 0;

        static DashFx Get()
        {
            if (inst) return inst;
            var go = new GameObject("DashFx (입자 풀)");
            DontDestroyOnLoad(go);
            inst = go.AddComponent<DashFx>();
            inst.mpb = new MaterialPropertyBlock();
            if (!mat)
            {
                var src = Resources.Load<Material>("FX/ToonFire");
                mat = src ? new Material(src) { name = "DashFx_ToonFire" } : null;
                if (mat) { mat.enableInstancing = true; if (mat.HasProperty("_OutlineWidth")) mat.SetFloat("_OutlineWidth", 1.3f); }
            }
            return inst;
        }

        static readonly System.Collections.Generic.Dictionary<string, Mesh> meshes = new();
        public static Mesh M(string name)
        {
            if (meshes.TryGetValue(name, out var m) && m) return m;
            m = Resources.Load<Mesh>("FX/Meshes/" + name);
            meshes[name] = m;
            return m;
        }

        /// <summary>입자 하나. 빈 칸을 돌아가며 찾고, 가득 차면 가장 오래된 칸(커서 자리)을 덮어씀 → 절대 새로 할당하지 않음</summary>
        public static void Emit(Mesh mesh, FxMesh.Kind kind, Vector3 pos, Vector3 vel, Vector3 size, float life, Color tint,
                                bool gravity = false, float drag = 4f, float spin = 0f, float delay = 0f, Quaternion? rot = null)
        {
            if (!mesh) return;
            var s = Get();
            if (!mat) return;
            int idx = -1;
            for (int k = 0; k < MaxParticles; k++)
            {
                int i = (s.cursor + k) % MaxParticles;
                if (!s.ps[i].alive) { idx = i; break; }
            }
            if (idx < 0) idx = s.cursor;                      // 가득 참: 가장 오래 묵었을 자리를 재사용
            s.cursor = (idx + 1) % MaxParticles;
            if (!s.ps[idx].alive) s.aliveCount++;
            s.ps[idx] = new P
            {
                alive = true, gravity = gravity, mesh = mesh, kind = kind, pos = pos, vel = vel, size = size, tint = tint,
                rot = rot ?? Random.rotation, spinAxis = Random.onUnitSphere, spin = spin,
                born = Time.time, life = Mathf.Max(0.05f, life), delay = delay, drag = drag, seed = Random.Range(0f, 100f)
            };
        }

        void LateUpdate()
        {
            if (!mat) return;
            float now = Time.time, dt = Time.deltaTime;
            int layer = VividLayer.Id >= 0 ? VividLayer.Id : 0;
            for (int i = 0; i < MaxParticles; i++)
            {
                if (!ps[i].alive) continue;
                ref var p = ref ps[i];
                float t = (now - p.born - p.delay) / p.life;
                if (t >= 1f) { p.alive = false; aliveCount--; continue; }
                if (t < 0f) continue;
                // 움직임: 공기 저항 · (조각) 중력과 바닥 튕김 · 회전
                p.vel *= Mathf.Exp(-p.drag * dt);
                if (p.gravity)
                {
                    p.vel.y -= 16f * dt;
                    if (p.pos.y < 0.03f && p.vel.y < 0) { p.pos.y = 0.03f; p.vel = new Vector3(p.vel.x * 0.55f, -p.vel.y * 0.32f, p.vel.z * 0.55f); p.spin *= 0.5f; }
                }
                p.pos += p.vel * dt;
                if (p.spin != 0) p.rot = Quaternion.AngleAxis(p.spin * dt, p.spinAxis) * p.rot;
                if (p.kind == FxMesh.Kind.Spike && p.vel.sqrMagnitude > 0.01f) p.rot = Quaternion.LookRotation(p.vel);   // 불티는 날아가는 쪽으로 길게
                FxMesh.Evaluate(p.kind, p.size, t, out var sc, out float heat, out float dis);
                mpb.Clear();
                FxTint.Apply(mpb, p.tint);
                mpb.SetFloat(FxMesh.HeatId, heat);
                mpb.SetFloat(FxMesh.DissolveId, dis);
                mpb.SetFloat(FxMesh.SeedId, p.seed);
                Graphics.DrawMesh(p.mesh, Matrix4x4.TRS(p.pos, p.rot, sc), mat, layer, null, 0, mpb, false, false, false);
            }
        }
    }
}
