// 스타일 VFX 한 겹 (셰이더 CatTang/StylizedVfx, 2026-11-20 사용자가 준 블렌더 VFX 영상 기법):
// 블렌더로 만든 단순 메시(Resources/FX/Meshes5/fx5_*: 돔 · 고리 · 벽 고리 · 회오리 · 베기 궤적 · 나선 구 · 가시 폭발)에
// 흐르는 노이즈/보로노이 + 계단 색 + 나이 문턱(구멍 뚫리며 사라짐)을 입혀 띄움. 재질 하나는 단순하게, 화려함은 겹(Layer)과 타이밍으로.
// 쓰는 곳: 큰 동작 위주 (보스 패턴 · 균열 깨짐 · 거인 내려찍기 등). 작은 탄 · 불똥은 기존 플립북이 더 맞음 (사용자: 적절한 이펙트에만)
using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    public class StylizedFx : MonoBehaviour, IPooled
    {
        /// <summary>겉모습 한 벌 (색 · 무늬 · 흐름 · 띠 끝 깎기 · 테두리)</summary>
        public struct Look
        {
            public Color core, mid, edge, ink;
            public float pattern;                    // 0 노이즈 · 1 보로노이 · 2 줄무늬
            public Vector2 scale, scroll;
            public float band1, band2, taperV, taperU, fresnel;
            public float opacity;                    // 0 = 빛이 더해짐(에너지 · 베기) · 1 = 덮음(연기 · 장판)
            public float sweepTime, tailFade;        // 베기: 처음 sweepTime(수명 비율) 동안 U 방향으로 그어지고, 꼬리부터 흩어짐
            public Look With(Vector2? scale = null, Vector2? scroll = null, float? pattern = null, float? taperU = null, float? taperV = null, float? fresnel = null,
                             float? opacity = null, float? sweepTime = null, float? tailFade = null)
            {
                var l = this;
                if (opacity.HasValue) l.opacity = opacity.Value;
                if (sweepTime.HasValue) l.sweepTime = sweepTime.Value;
                if (tailFade.HasValue) l.tailFade = tailFade.Value;
                if (scale.HasValue) l.scale = scale.Value;
                if (scroll.HasValue) l.scroll = scroll.Value;
                if (pattern.HasValue) l.pattern = pattern.Value;
                if (taperU.HasValue) l.taperU = taperU.Value;
                if (taperV.HasValue) l.taperV = taperV.Value;
                if (fresnel.HasValue) l.fresnel = fresnel.Value;
                return l;
            }
        }

        static Look Make(Color core, Color mid, Color edge, float pattern, Vector2 scale, Vector2 scroll, float taperV = 0.22f, float taperU = 0f, float fresnel = 0f) =>
            new() { core = core, mid = mid, edge = edge, ink = new Color(0.07f, 0.03f, 0.12f, 1f), pattern = pattern, scale = scale, scroll = scroll, band1 = 0.42f, band2 = 0.17f, taperV = taperV, taperU = taperU, fresnel = fresnel, opacity = 0f };   // 기본 = 더해지는 빛 (2026-11-20 사용자: 덮는 색 덩어리는 그림 같음)   // 심지(흰빛)는 좁게 — 넓으면 하얗게 날아감

        // 5장 적 공격 = 자홍 (HDR 심지는 블룸에 빛남), 보스 시간 = 금, 허공 = 보라, 고양이 쪽 = 흰 금
        // 결 = 흐르는 방향(U)으로 길게 늘어진 줄 (U는 성기게 · V는 촘촘히) → 물감 얼룩이 아니라 빛이 흐르는 느낌
        public static readonly Look Magenta = Make(new Color(2.6f, 2.2f, 2.6f), new Color(1.9f, 0.3f, 1.3f), new Color(0.5f, 0.12f, 0.8f), 0, new Vector2(2.2f, 6f), new Vector2(-2.2f, 0));
        public static readonly Look Gold = Make(new Color(2.8f, 2.5f, 1.7f), new Color(2.1f, 1.3f, 0.35f), new Color(0.9f, 0.5f, 0.15f), 0, new Vector2(2.2f, 6f), new Vector2(-1.8f, 0));
        public static readonly Look Void = Make(new Color(1.3f, 1.0f, 2.0f), new Color(0.5f, 0.28f, 1.05f), new Color(0.12f, 0.06f, 0.3f), 0, new Vector2(8, 1.6f), new Vector2(-0.9f, 0.1f)).With(opacity: 0.75f);   // 노이즈 결 (길게 늘인 연기 결), 덮는 연기
        /// <summary>베기 궤적 결 (2026-11-20 사용자: 초승달이 그림 같음 → 낫이 지나간 방향으로 늘어진 빛 결, 더해지는 빛, 머리부터 그어져 꼬리부터 흩어짐)</summary>
        // (2026-11-20 사용자: 리본 형태 · 섬광에 가깝게) 무기가 없을 때 쓰는 초승달도 가늘고 짧게 번쩍이는 섬광 띠로
        public static readonly Look SlashStreak = Make(new Color(3.4f, 3.2f, 3.6f), new Color(2.2f, 0.35f, 1.5f), new Color(0.6f, 0.15f, 0.9f), 0, new Vector2(1.2f, 8f), new Vector2(-9f, 0), 0.44f, 0.1f).With(opacity: 0f, sweepTime: 0.25f, tailFade: 2.2f);
        public static readonly Look SlashCore = Make(new Color(3.6f, 3.6f, 3.8f), new Color(2.6f, 2.0f, 2.8f), new Color(1.4f, 0.6f, 1.8f), 0, new Vector2(1.0f, 4f), new Vector2(-12f, 0), 0.48f, 0.14f).With(opacity: 0f, sweepTime: 0.2f, tailFade: 3f);
        /// <summary>4장 지면 폭발 빨강 (2026-11-20 사용자: 연기 말고 레퍼런스 지면 폭발 모양, 색은 빨강): 흰 심지 → 빨강 → 짙은 검붉은 테두리, 위로 흐르는 불꽃 결</summary>
        public static readonly Look BlastRed = Make(new Color(3.2f, 2.5f, 2.3f), new Color(2.5f, 0.22f, 0.16f), new Color(0.55f, 0.03f, 0.05f), 0, new Vector2(3f, 4.5f), new Vector2(0f, -3.2f), 0.3f).With(opacity: 0.55f);
        public static readonly Look Glass = Make(new Color(2.2f, 2.3f, 2.6f), new Color(0.9f, 0.95f, 1.5f), new Color(0.45f, 0.4f, 0.8f), 1, new Vector2(8, 3), new Vector2(-0.8f, 0));

        static Material mat;
        static readonly Dictionary<string, Mesh> meshes = new();
        static readonly int AgeId = Shader.PropertyToID("_Age"), OffsetId = Shader.PropertyToID("_Offset"), AlphaId = Shader.PropertyToID("_Alpha");
        static readonly int CoreId = Shader.PropertyToID("_ColCore"), MidId = Shader.PropertyToID("_ColMid"), EdgeId = Shader.PropertyToID("_ColEdge"), InkId = Shader.PropertyToID("_ColInk");
        static readonly int PatId = Shader.PropertyToID("_Pattern"), ScaleId = Shader.PropertyToID("_Scale"), ScrollId = Shader.PropertyToID("_Scroll");
        static readonly int OpId = Shader.PropertyToID("_Opacity"), SwId = Shader.PropertyToID("_Sweep"), TfId = Shader.PropertyToID("_TailFade");
        static readonly int B1Id = Shader.PropertyToID("_Band1"), B2Id = Shader.PropertyToID("_Band2"), TvId = Shader.PropertyToID("_TaperV"), TuId = Shader.PropertyToID("_TaperU"), FrId = Shader.PropertyToID("_Fresnel");

        public static bool Ready => Mesh("ring") != null && Shader.Find("CatTang/StylizedVfx") != null;

        static Mesh Mesh(string name)
        {
            if (meshes.TryGetValue(name, out var m)) return m;
            var go = Resources.Load<GameObject>("FX/Meshes5/fx5_" + name);
            var mf = go ? go.GetComponentInChildren<MeshFilter>() : null;
            meshes[name] = m = mf ? mf.sharedMesh : null;
            return m;
        }

        Renderer rend; MaterialPropertyBlock mpb; float sweepT;
        float born, life, ageFrom, ageTo, spin, holdUntil, uDrift;
        Vector3 size0, size1;
        Transform follow; Vector3 followOff;
        System.Func<float, float> ageCurve;

        /// <summary>
        /// 한 겹 띄움. size0 → size1로 커지며(빠르게 시작해 느려짐), 나이는 ageFrom → ageTo로 (처음엔 천천히, 끝에서 빠르게 = 구멍이 확 번지며 사라짐).
        /// hold = 처음 이만큼은 나이를 멈춤 (유지되는 장판 · 정지장). spin = 초당 도는 각도(Y). uDrift = 초당 무늬가 U로 더 흐르는 양
        /// </summary>
        // 풀링 (2026-11-20): 메시와 재생 상태만 교체하고 렌더러와 재질 블록을 재사용한다.
        MeshFilter mf;
        static readonly System.Func<GameObject> factory = Create;
        static GameObject Create()
        {
            var go = new GameObject("StylizedFx");
            var fx = go.AddComponent<StylizedFx>();
            fx.mf = go.AddComponent<MeshFilter>();
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            fx.rend = r; fx.mpb = new MaterialPropertyBlock();
            return go;
        }
        public void OnSpawned() { follow = null; followOff = Vector3.zero; ageCurve = null; mpb.Clear(); }
        public void OnDespawned() { follow = null; followOff = Vector3.zero; ageCurve = null; }

        public static StylizedFx Spawn(string mesh, Vector3 pos, Quaternion rot, Vector3 size0, Vector3 size1, float life, Look look,
                                       float spin = 0f, float hold = 0f, float ageFrom = 0.02f, float ageTo = 1f, float uDrift = 0f, Transform follow = null)
        {
            var m = Mesh(mesh);
            var sh = Shader.Find("CatTang/StylizedVfx");
            if (!m || !sh) return null;
            if (!mat) mat = new Material(sh) { renderQueue = 3010 };
            var go = GameObjectPool.Get("StylizedFx", factory);
            go.transform.SetPositionAndRotation(pos, rot);
            go.transform.localScale = size0;
            var fx = go.GetComponent<StylizedFx>();
            fx.mf.sharedMesh = m;
            fx.rend.sharedMaterial = mat;
            fx.born = Time.time; fx.life = Mathf.Max(0.05f, life); fx.size0 = size0; fx.size1 = size1;
            fx.spin = spin; fx.holdUntil = Time.time + hold; fx.ageFrom = ageFrom; fx.ageTo = ageTo; fx.uDrift = uDrift;
            fx.follow = follow; if (follow) fx.followOff = pos - follow.position;
            fx.mpb.SetColor(CoreId, look.core); fx.mpb.SetColor(MidId, look.mid); fx.mpb.SetColor(EdgeId, look.edge); fx.mpb.SetColor(InkId, look.ink);
            fx.mpb.SetFloat(PatId, look.pattern); fx.mpb.SetVector(ScaleId, look.scale); fx.mpb.SetVector(ScrollId, look.scroll);
            fx.mpb.SetFloat(B1Id, look.band1); fx.mpb.SetFloat(B2Id, look.band2); fx.mpb.SetFloat(TvId, look.taperV); fx.mpb.SetFloat(TuId, look.taperU); fx.mpb.SetFloat(FrId, look.fresnel);
            fx.mpb.SetFloat(OpId, look.opacity); fx.mpb.SetFloat(TfId, look.tailFade); fx.sweepT = look.sweepTime;
            fx.mpb.SetVector(OffsetId, new Vector4(Random.value * 10f, Random.value * 10f, 0, 0));   // 겹마다 무늬가 다르게
            fx.Apply(0f);
            return fx;
        }

        public static StylizedFx Spawn(string mesh, Vector3 pos, float size0, float size1, float life, Look look, float spin = 0f, float hold = 0f) =>
            Spawn(mesh, pos, Quaternion.Euler(0, Random.Range(0f, 360f), 0), Vector3.one * size0, Vector3.one * size1, life, look, spin, hold);

        /// <summary>당장 사라지기 시작 (정지장 · 장판을 끝낼 때)</summary>
        public void Release(float fade = 0.3f) { holdUntil = 0; born = Time.time - life * 0.0f; life = Mathf.Max(0.05f, fade); ageFrom = Mathf.Max(ageFrom, 0.15f); }

        void Apply(float t)
        {
            float a = Mathf.Lerp(ageFrom, ageTo, t * t);                    // 끝에서 빠르게 번지며 사라짐
            // 베기: 처음 sweepT 동안 머리가 U 0 → 1로 그어짐(빠르게 출발해 감속), 그 뒤엔 다 보임. 나이는 그어진 뒤부터 오름
            float sw = sweepT > 0 ? 1f - Mathf.Pow(1f - Mathf.Clamp01(t / sweepT), 2.5f) : 1.2f;
            mpb.SetFloat(SwId, sweepT > 0 ? sw * 1.05f : 1.2f);
            if (sweepT > 0) a = Mathf.Lerp(ageFrom, ageTo, Mathf.Pow(Mathf.Clamp01((t - sweepT * 0.6f) / (1f - sweepT * 0.6f)), 1.6f));
            mpb.SetFloat(AgeId, a);
            if (uDrift != 0) mpb.SetVector(OffsetId, mpb.GetVector(OffsetId) + new Vector4(uDrift * Time.deltaTime, 0, 0, 0));
            rend.SetPropertyBlock(mpb);
        }

        void Update()
        {
            if (follow) transform.position = follow.position + followOff;
            if (spin != 0) transform.Rotate(0, spin * Time.deltaTime, 0, Space.World);
            if (Time.time < holdUntil) { Apply(0f); return; }
            float t = Mathf.Clamp01((Time.time - Mathf.Max(born, holdUntil)) / life);
            float g = 1 - (1 - t) * (1 - t) * (1 - t);                      // 크기: 빠르게 퍼지고 느려짐
            transform.localScale = Vector3.LerpUnclamped(size0, size1, g);
            Apply(t);
            if (t >= 1f) GameObjectPool.Release(gameObject);
        }

        // ================= 겹 프리셋 (여러 겹을 타이밍 · 색으로 겹친 완성 이펙트) =================

        /// <summary>충격파: 바닥 고리 2겹(보로노이 금 + 노이즈 몸색) + 짧은 벽 고리</summary>
        public static void Shockwave(Vector3 at, float radius, Look body, float life = 0.45f)
        {
            at.y = 0.05f;
            Spawn("ring", at, Quaternion.identity, Vector3.one * radius * 0.25f, Vector3.one * radius, life, body, 90f);
            Spawn("ring", at + Vector3.up * 0.02f, Quaternion.Euler(0, 40, 0), Vector3.one * radius * 0.15f, Vector3.one * radius * 0.85f, life * 0.8f, Glass, -140f, 0, 0.1f);
            Spawn("ring_tall", at, Quaternion.identity, new Vector3(radius * 0.3f, 0.4f, radius * 0.3f), new Vector3(radius * 0.95f, 1.2f, radius * 0.95f), life * 0.7f, body, 60f);
        }

        /// <summary>베기 궤적: 초승달 2겹 (몸색 넓게 + 흰 심지 좁게, 살짝 늦게) — dir = 휘두른 쪽 가운데, 반지름 = 사거리</summary>
        public static void Slash(Vector3 at, Vector3 dir, float radius, Look body, float height = 1.0f, bool flip = false, float life = 0.32f)
        {
            dir.y = 0; if (dir.sqrMagnitude < 1e-4f) dir = Vector3.forward;
            // fx5_slash_arc: 바닥에 누운 초승달(법선 +Y), 가운데가 −X → Y로 90° 돌려 가운데를 +Z(= dir)로. flip = 가운데 축으로 뒤집어 휘두르는 방향 반대
            var rot = Quaternion.LookRotation(dir.normalized) * Quaternion.Euler(0, 90f, 0) * (flip ? Quaternion.Euler(180f, 0, 0) : Quaternion.identity);
            var p = at + Vector3.up * height;
            // 몸(색) 결: 넓게, 색은 넘겨받은 겉모습의 몸 · 테두리 색을 씀 / 심지: 좁고 흰빛, 살짝 늦게 그어짐 (2026-11-20 사용자: 단색 초승달은 그림 같음)
            var streak = SlashStreak; streak.mid = body.mid; streak.edge = body.edge;
            Spawn("slash_arc", p, rot, new Vector3(radius, 1f, radius), new Vector3(radius * 1.06f, 1f, radius * 1.06f), Mathf.Min(life, 0.24f), streak, 0, 0, 0.0f);
            Spawn("slash_arc", p + Vector3.up * 0.04f, rot, new Vector3(radius * 0.98f, 1f, radius * 0.98f), new Vector3(radius * 1.02f, 1f, radius * 1.02f), Mathf.Min(life, 0.18f), SlashCore, 0, 0, 0.05f);
        }

        /// <summary>오라 돔: 프레넬 돔 + 바닥 고리 (hold초 유지 후 사라짐) — 정지장 · 보호막</summary>
        public static StylizedFx Dome(Vector3 at, float radius, float hold, Look body)
        {
            at.y = 0;
            var d = Spawn("dome", at, Quaternion.identity, Vector3.one * radius * 0.4f, Vector3.one * radius, 0.4f, body.With(fresnel: 1.6f, scale: new Vector2(5, 3), taperV: 0.08f), 25f, hold, 0.25f);
            Spawn("ring", at + Vector3.up * 0.04f, Quaternion.identity, Vector3.one * radius, Vector3.one * radius * 1.05f, 0.4f, body, -30f, hold);
            return d;
        }

        /// <summary>회오리 (스핀짓수 느낌): 3갈래 나선이 돌며 솟음 + 바닥 고리 — 모래폭풍 · 소환</summary>
        public static void Tornado(Vector3 at, float radius, float height, float hold, Look body)
        {
            at.y = 0;
            Spawn("swirl", at, Quaternion.identity, new Vector3(radius * 0.6f, height * 0.3f, radius * 0.6f), new Vector3(radius, height, radius), 0.5f, body.With(scroll: new Vector2(-2.5f, 0)), 420f, hold, 0.05f);
            Spawn("swirl", at, Quaternion.Euler(0, 60, 0), new Vector3(radius * 0.5f, height * 0.25f, radius * 0.5f), new Vector3(radius * 0.8f, height * 0.85f, radius * 0.8f), 0.45f, Glass.With(scroll: new Vector2(-3f, 0)), 520f, hold, 0.12f);
            Spawn("ring", at + Vector3.up * 0.04f, Quaternion.identity, Vector3.one * radius * 0.6f, Vector3.one * radius * 1.1f, 0.5f, body, 200f, hold);
        }

        /// <summary>
        /// 지면 폭발 (2026-11-20 사용자: 무거운 일격이 땅을 치는 순간 — 연기 덩이 대신 레퍼런스의 지면 폭발 모양):
        /// ① 순간 섬광 반구(0.14초) ② 바닥에서 솟는 불꽃 왕관(ring_tall, 위로 흐르는 결, 늦게 사그라듦) ③ 바닥에 납작하게 퍼지는 가시 꽃잎(spike_burst 눌림)
        /// ④ 바깥으로 번지는 충격 고리 2겹 ⑤ 위로 튀는 불티. size = 폭발 반지름(m)
        /// </summary>
        public static void GroundBlast(Vector3 at, float size, Look body)
        {
            at.y = 0.05f;
            var rot = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
            Spawn("dome", at, rot, new Vector3(size * 0.25f, size * 0.18f, size * 0.25f), new Vector3(size * 0.75f, size * 0.55f, size * 0.75f), 0.14f, body.With(pattern: 1, scale: new Vector2(4, 4), opacity: 0f));
            Spawn("ring_tall", at, rot, new Vector3(size * 0.35f, size * 0.25f, size * 0.35f), new Vector3(size * 0.85f, size * 1.15f, size * 0.85f), 0.42f, body, 40f, 0f, 0.02f, 1f);
            Spawn("ring_tall", at, rot * Quaternion.Euler(0, 37, 0), new Vector3(size * 0.2f, size * 0.15f, size * 0.2f), new Vector3(size * 0.55f, size * 1.5f, size * 0.55f), 0.32f, body.With(scale: new Vector2(2.2f, 6f), scroll: new Vector2(0, -5f)), -60f);
            Spawn("spike_burst", at + Vector3.up * 0.1f, rot, new Vector3(size * 0.3f, size * 0.08f, size * 0.3f), new Vector3(size * 1.15f, size * 0.22f, size * 1.15f), 0.3f, body.With(scroll: new Vector2(-4f, 0), taperV: 0f, taperU: 0f));
            Spawn("ring", at, rot, Vector3.one * size * 0.3f, Vector3.one * size * 1.5f, 0.38f, body.With(opacity: 0f), 90f);
            Spawn("ring", at + Vector3.up * 0.03f, rot * Quaternion.Euler(0, 50, 0), Vector3.one * size * 0.2f, Vector3.one * size * 1.1f, 0.3f, body.With(opacity: 0f, scale: new Vector2(6, 2)), -120f);
            FxBurst.Spawn(at + Vector3.up * size * 0.3f, size * 0.9f, FxBurst.Kind.Spark, body.mid);
        }

        /// <summary>가시 폭발 + 나선 구 섬광 — 깨짐 · 타격 순간</summary>
        public static void Burst(Vector3 at, float size, Look body)
        {
            Spawn("spike_burst", at, Quaternion.Euler(0, Random.Range(0f, 360f), 0), Vector3.one * size * 0.3f, Vector3.one * size, 0.3f, body.With(scroll: new Vector2(-4f, 0), taperV: 0f, taperU: 0f));
            Spawn("spiral_ball", at, Quaternion.Euler(Random.Range(0f, 360f), Random.Range(0f, 360f), 0), Vector3.one * size * 0.25f, Vector3.one * size * 0.55f, 0.25f, Glass.With(pattern: 2, scale: new Vector2(3, 1), scroll: new Vector2(-6f, 0), taperV: 0.05f, fresnel: 1f), 600f);
        }
    }
}

namespace CatTang
{
    /// <summary>
    /// 섬광 리본 (2026-11-20 사용자: 베기 궤적은 리본 형태로, 섬광에 가깝게): 휘두르는 동안 무기의 안쪽 점 · 끝 점을 매 프레임 기록해 띠 메시를 만들고
    /// CatTang/StylizedVfx로 그림 — U = 기록 순서(0 오래된 꼬리 → 1 지금 머리), V = 안쪽 → 끝. 머리 바로 뒤가 흰 섬광, 꼬리부터 흩어짐.
    /// 기록이 끝나면 fade초 만에 나이가 0 → 1 (번쩍하고 사라짐). 더해지는 빛(불투명도 0)이라 겹칠수록 밝음
    /// </summary>
    public class FlashRibbon : MonoBehaviour, IPooled
    {
        Transform weapon; Vector3 inner, outer; float recordUntil, fade, fadeStart = -1; float maxLen;
        readonly List<Vector3> a = new(72), b = new(72);
        readonly List<Vector3> vertices = new(128);
        readonly List<Vector2> uvs = new(128);
        readonly List<int> triangles = new(378);
        Mesh mesh; MeshRenderer mr; MaterialPropertyBlock mpb; StylizedFx.Look look;
        static Material mat;
        static readonly int AgeId = Shader.PropertyToID("_Age"), SwId = Shader.PropertyToID("_Sweep");

        /// <summary>weapon 기준 inner · outer 두 점을 dur초 동안 따라 그림 → fade초 섬광으로 사라짐. look = 색 (Magenta · Gold 등, 결 모양은 섬광 고정)</summary>
        // 풀링 (2026-11-20): 리본 메시와 기록 버퍼를 보관하고 다음 기록 전에 비운다.
        static readonly System.Func<GameObject> factory = Create;
        static GameObject Create()
        {
            var go = new GameObject("FlashRibbon");
            var f = go.AddComponent<FlashRibbon>();
            f.mesh = new Mesh { name = "FlashRibbon" }; f.mesh.MarkDynamic();
            go.AddComponent<MeshFilter>().sharedMesh = f.mesh;
            f.mr = go.AddComponent<MeshRenderer>(); f.mr.sharedMaterial = mat;
            f.mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; f.mr.receiveShadows = false;
            f.mpb = new MaterialPropertyBlock();
            return go;
        }
        public void OnSpawned()
        {
            weapon = null; fadeStart = -1f; recordUntil = 0f; maxLen = 0f; look = default;
            a.Clear(); b.Clear(); vertices.Clear(); uvs.Clear(); triangles.Clear(); mesh.Clear(); mpb.Clear();
        }
        public void OnDespawned() { weapon = null; a.Clear(); b.Clear(); vertices.Clear(); uvs.Clear(); triangles.Clear(); mesh.Clear(); }

        public static FlashRibbon Begin(Transform weapon, Vector3 innerLocal, Vector3 outerLocal, float dur, StylizedFx.Look color, float fade = 0.16f)
        {
            if (!weapon) return null;
            var sh = Shader.Find("CatTang/StylizedVfx");
            if (!sh) return null;
            if (!mat) mat = new Material(sh) { renderQueue = 3012 };
            var go = GameObjectPool.Get("FlashRibbon", factory);
            var f = go.GetComponent<FlashRibbon>();
            f.weapon = weapon; f.inner = innerLocal; f.outer = outerLocal; f.recordUntil = Time.time + dur; f.fade = fade;
            // 섬광 결: 길이 방향으로 길게 늘어진 줄무늬(가로 결 촘촘) · 머리 흰빛 넓게 · 띠 가장자리 뾰족
            f.mpb.SetColor("_ColCore", new Color(3.4f, 3.3f, 3.6f));
            f.mpb.SetColor("_ColMid", color.mid * 1.3f);
            f.mpb.SetColor("_ColEdge", color.edge);
            f.mpb.SetColor("_ColInk", color.edge * 0.5f);
            f.mpb.SetFloat("_Pattern", 0); f.mpb.SetVector("_Scale", new Vector4(1.2f, 7f)); f.mpb.SetVector("_Scroll", new Vector4(-9f, 0));
            f.mpb.SetFloat("_Band1", 0.3f); f.mpb.SetFloat("_Band2", 0.12f); f.mpb.SetFloat("_Ink", 0.02f);
            f.mpb.SetFloat("_TaperV", 0.38f); f.mpb.SetFloat("_TaperU", 0.08f); f.mpb.SetFloat("_Fresnel", 0);
            f.mpb.SetFloat("_Opacity", 0f); f.mpb.SetFloat("_TailFade", 1.4f); f.mpb.SetFloat("_Alpha", 1f);
            f.mpb.SetVector("_Offset", new Vector4(Random.value * 10f, Random.value * 10f));
            f.Sample();
            return f;
        }

        void Sample()
        {
            if (!weapon) return;
            a.Add(weapon.TransformPoint(inner)); b.Add(weapon.TransformPoint(outer));
            if (a.Count > 64) { a.RemoveAt(0); b.RemoveAt(0); }
        }

        void LateUpdate()
        {
            if (Time.time < recordUntil && weapon)
            {
                // 큰 동작이 한 프레임에 크게 지나가면 사이를 채움 (끊긴 다각형이 보이지 않게)
                var na = weapon.TransformPoint(inner); var nb = weapon.TransformPoint(outer);
                var la = a[a.Count - 1]; var lb = b[b.Count - 1];
                int steps = Mathf.Clamp(Mathf.CeilToInt(Vector3.Distance(lb, nb) / 0.35f), 1, 6);
                for (int i = 1; i <= steps; i++) { float t = i / (float)steps; a.Add(Vector3.Lerp(la, na, t)); b.Add(Vector3.Lerp(lb, nb, t)); }
                while (a.Count > 64) { a.RemoveAt(0); b.RemoveAt(0); }
            }
            else if (fadeStart < 0) fadeStart = Time.time;
            Build();
            float age = fadeStart < 0 ? 0.02f : Mathf.Clamp01((Time.time - fadeStart) / fade);
            mpb.SetFloat(AgeId, fadeStart < 0 ? 0.02f : Mathf.Lerp(0.05f, 1f, age * age));
            mpb.SetFloat(SwId, 1.0f);
            mr.SetPropertyBlock(mpb);
            if (fadeStart >= 0 && age >= 1f) GameObjectPool.Release(gameObject);
        }

        void Build()
        {
            int n = a.Count;
            if (n < 2) return;
            vertices.Clear(); uvs.Clear(); triangles.Clear();
            for (int i = 0; i < n; i++)
            {
                vertices.Add(a[i]); vertices.Add(b[i]);
                float u = i / (float)(n - 1);
                uvs.Add(new Vector2(u, 0)); uvs.Add(new Vector2(u, 1));
                if (i < n - 1)
                {
                    int q = i * 2;
                    triangles.Add(q); triangles.Add(q + 1); triangles.Add(q + 2);
                    triangles.Add(q + 1); triangles.Add(q + 3); triangles.Add(q + 2);
                }
            }
            mesh.Clear(); mesh.SetVertices(vertices); mesh.SetUVs(0, uvs); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
        }

        void OnDestroy() { if (mesh) Destroy(mesh); }
    }
}
