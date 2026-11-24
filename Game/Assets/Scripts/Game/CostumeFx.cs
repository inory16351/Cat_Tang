// 코스튬 효과 연출 · 동작 (2026-11-24 코스튬 기획 확정)
// - 장의사 정장: 처형할 때 체력 회복 → 잉크 터짐 + 혼 줄기가 고양이 가슴으로 빨려 듦 + 회복 고리 (ExecuteHeal)
// - 거울 망토: 극한 회피하면 범위 안 무작위 적 최대 3명 옆에 자객 잔상이 하나씩 피어나 꿰뚫고 벰 (ShadowStrike, 레퍼런스: 세피리아 혼령봉인연구회 제2형)
//     이동 전체 잔상(ShadowGhost) · 칼끝 리본(DashSlash: 궤적 시작점 → 멈춘 앞, 손 쪽 꼬리 길고 반대쪽 짧게 · 불규칙 찢김 · 속도선 · 광, 긴 꼬리부터 사라짐) · 타격 아크(HitArcs)
// - 모래 순례자 로브: 판정 시각 0 이하 탄이 맞힌 자리에 모래 소용돌이 (SandVortex: 둔화 + 지속 피해)
// - 노인의 잠옷: 체력 40% 미만이면 공격력 +30% → 켜질 때 푸른 펄스 + 몸 둘레 잔불 오라 (LowHpAura)
// 그림 = 블렌더로 구운 플립북 (Art/tools/blender_fx_costume.py → Resources/FX/anim/bk_cos_*), 없으면 기존 효과로 대신
using System.Collections;
using UnityEngine;

namespace CatTang
{
    public static class CostumeFx
    {
        public static readonly Color Soul = new(0.31f, 0.76f, 0.63f);    // 혼 줄기 민트 (4FC3A0)

        public static bool Flip(string set, Vector3 at, float size, float life, Vector3 drift = default) =>
            FxFlipbook.Spawn("bk_cos_" + set, at, Vector3.zero, size, life, false, drift);

        public static void ExecuteHeal(Vector3 from, Transform to)
        {
            if (!to) return;
            Flip("undertaker_ink", from + Vector3.up * 0.8f, 2.4f, 0.45f);
            var host = new GameObject("ExecuteHeal").AddComponent<FxHost>();
            host.StartCoroutine(HealStream(host.gameObject, from + Vector3.up * 0.8f, to));
        }

        static IEnumerator HealStream(GameObject host, Vector3 from, Transform to)
        {
            // 혼 줄기 3가닥: 휘어 들어오며 가슴에 닿음
            var bends = new[] { -1.1f, 0.2f, 1.2f };
            for (float t = 0; t < 0.4f && to; t += Time.deltaTime)
            {
                float u = t / 0.4f;
                var end = to.position + Vector3.up * 0.9f;
                var side = Vector3.Cross(Effects.Flat(end - from).normalized, Vector3.up);
                foreach (var b in bends)
                {
                    var at = Vector3.Lerp(from, end, u * u) + side * b * Mathf.Sin(u * Mathf.PI) * 0.9f + Vector3.up * Mathf.Sin(u * Mathf.PI) * 0.5f;
                    FxBurst.Spawn(at, 0.3f, FxBurst.Kind.Spark, Soul, 0.22f);
                }
                yield return null;
            }
            if (to && !Flip("undertaker_heal", to.position + Vector3.up * 0.9f, 2.4f, 0.4f, Vector3.zero))
                FxBurst.Spawn(to.position + Vector3.up * 0.9f, 1.3f, FxBurst.Kind.Converge, Soul, 0.35f);
            Object.Destroy(host);
        }
    }

    /// <summary>코루틴만 돌리는 빈 객체</summary>
    public class FxHost : MonoBehaviour { }

    /// <summary>거울 망토: 극한 회피 한 번 = 범위 안 무작위 적 최대 3명, 한 대상엔 자객 잔상 하나씩 (엇박으로 동시에)</summary>
    // 2026-11-24 사용자: 대상에게 자객 잔상이 떠서 벤다 (세피리아 혼령봉인연구회 제2형) · 원형 · 돌진 선 없이 잔상으로 · 스쿼시 앤 스트레치 가감속
    //   피어남(디졸브로 차오름 · 납작 → 툭 솟음) → 움츠림(뒤로 살짝, 낮고 넓게) → 폭발 가속 돌진(앞뒤로 늘어남) → 반대편 도착 찌그러짐 · 반동 → 디졸브로 사라짐
    //   자세 모델 = Resources/Costumes/assassin_windup · assassin_slash.fbx (Hyper3D, Art/tools/blender_assassin_pose.py, 앞 = 로컬 +Z라고 봄)
    // 2026-11-24 사용자 (v5): 돌진이 순간이동처럼 보임 → 이동 전체에 잔상 (거리 간격으로 촘촘히, 프레임 사이도 메워서 끊김 없이)
    //   참격 = 단색 초승달(DashSlash): 사라질 땐 꼬리(그어지기 시작한 쪽)부터 먹혀 들어감
    // 2026-11-24 사용자 (v6): 심심함 → 참격을 돌진하는 몸 앞에 붙임 · 광 효과 · 톱니 대신 속도를 강조하는 불규칙 찢김(속도선) · 타격 아크(HitArcs) + 섬광 + 짧은 멈칫
    //   (전에는 Afterimage.SpawnAt에 몸 위치를 넘겨 뿌리 위치가 두 번 더해져 잔상이 엉뚱한 데 찍혔음 → 자체 잔상 ShadowGhost로 바꿈)
    public class ShadowStrike : MonoBehaviour
    {
        static readonly Color Shade = new(0.07f, 0.04f, 0.12f, 0.92f);     // 검은 몸 (Silhouette 셰이더 테 = 밝게)
        static readonly Color GhostShade = new(0.1f, 0.06f, 0.2f, 0.5f);
        public static readonly Color SlashColor = new(0.62f, 0.46f, 1f, 1f); // 참격 단색 (보라 — 밝은 바닥 · 블룸에서 하얗게 날아가지 않게)
        public const float Range = 9f;                                   // 플레이어 둘레 대상 범위
        public const int MaxTargets = 3;
        const float Stagger = 0.07f;                                     // 자객끼리 엇박
        const float Ant = 0.12f, Dash = 0.26f, Arr = 0.42f, End = 0.84f;  // 피어난 뒤 시각 (v5: 돌진 0.1 → 0.16초, 순간이동처럼 안 보이게)
        const float U = 1.8f;                                            // 블렌더 미리보기 단위 → Unity (주인공 ModelScale)
        const float Appear = 1.45f * U, Beyond = 1.3f * U, Back = 0.28f * U;
        const float GhostGap = 0.36f, GhostLife = 0.26f;                  // 잔상 거리 간격 (m) · 수명
        const float EatFrom = Arr + 0.03f, EatTime = 0.22f;              // 도착 뒤 잠깐 보여 준 뒤 긴 꼬리(궤적 시작점)부터 먹힘
        // 칼끝 길 (v7, 2026-11-24 사용자: 리본 꼬리가 궤적 시작점부터 멈추는 앞까지 이어짐 · 손 쪽 꼬리 길고 반대쪽 짧게)
        //   돌진 내내 칼 쥔 손 쪽 옆(몸 가까이, 살짝 뒤)에 붙어 따라옴 = 긴 꼬리 → 대상 근처부터 몸 앞을 돌아 베어 넘김 → 반대쪽에서 짧게 멈춤
        const float TipFrom = Dash - 0.005f, SweepFrom = Dash + 0.015f;   // 리본 시작(궤적 시작점) · 베기 시작 (대상 가까이)
        const float HandDeg = 105f, EndDeg = -60f, TipNear = 0.75f, TipFar = 1.3f, TipH = 0.62f * U;
        static float SweepU(float r) => 1 - Mathf.Pow(1 - Mathf.Clamp01((r - SweepFrom) / (Arr - SweepFrom)), 2.2f);   // 빠르게 베고 → 감속하며 넘어감
        static Vector3 TipAt(float r, Vector3 a, Vector3 b, Vector3 dir, float tilt)
        {
            float u = SweepU(r);
            float th = tilt * Mathf.Lerp(HandDeg, EndDeg, u) * Mathf.Deg2Rad;
            float rad = Mathf.Lerp(TipNear, TipFar, Mathf.SmoothStep(0, 1, u / 0.3f));
            var right = Vector3.Cross(Vector3.up, dir);
            return PosAt(Mathf.Min(r, Arr), a, b, dir) + Vector3.up * TipH + (right * Mathf.Sin(th) + dir * Mathf.Cos(th)) * rad;
        }
        /// <summary>굵기 비율: 긴 꼬리(손 쪽)는 가늘게 점점 굵어짐 → 앞을 도는 초승달이 가장 굵음 → 반대쪽 끝은 짧고 뾰족</summary>
        static float TipWidth(float r)
        {
            if (r < SweepFrom) return 0.04f + 0.2f * Mathf.Clamp01((r - TipFrom) / (SweepFrom - TipFrom));
            float u = SweepU(r);
            float w = Mathf.Lerp(0.24f, 1f, Mathf.SmoothStep(0, 1, u / 0.45f));
            if (u > 0.72f) w *= Mathf.Sqrt(Mathf.Max(0, 1 - Mathf.Pow((u - 0.72f) / 0.28f, 2)));
            return w;
        }
        // FBX는 블렌더 단위(주인공 키 약 0.98 기준) → 주인공 ModelScale 1.8 (인게임 확인: ×1.35면 적보다 커서 뺌)
        const float BodyScale = 1.8f;
        static Material mat, ghostMat;
        MaterialPropertyBlock mpb;

        public static void Launch(PlayerController p, float dmg)
        {
            if (!p) return;
            var pool = new System.Collections.Generic.List<Enemy>();
            foreach (var e in Enemy.All)
                if (e && e.health.Alive && (e.transform.position - p.transform.position).sqrMagnitude <= Range * Range) pool.Add(e);
            if (pool.Count == 0) return;
            if (p.model) Afterimage.SpawnAt(p.model, p.transform.position, p.model.rotation, Shade, 0.3f);   // 피한 자리의 그림자
            for (int i = 0; i < MaxTargets && pool.Count > 0; i++)
            {
                int k = Random.Range(0, pool.Count);                         // 대상마다 자객 하나 (겹치지 않게 뽑음)
                var s = new GameObject("ShadowStrike").AddComponent<ShadowStrike>();
                s.StartCoroutine(s.Run(pool[k], dmg, i * Stagger));
                pool.RemoveAt(k);
            }
        }

        static float DashU(float k) => 1 - Mathf.Pow(1 - k, 3.4f);        // 폭발 출발 → 길게 감속
        static float DashV(float k) => Mathf.Pow(1 - k, 2.6f);

        static Vector3 PosAt(float r, Vector3 a, Vector3 b, Vector3 dir)
        {
            if (r < Ant) return a;
            if (r < Dash) return a - dir * Back * Mathf.SmoothStep(0, 1, (r - Ant) / (Dash - Ant));
            if (r < Arr) return Vector3.LerpUnclamped(a - dir * Back, b, DashU((r - Dash) / (Arr - Dash)));
            float tau = r - Arr;                                         // 미끄러졌다 되돌아옴
            return b + dir * 0.22f * U * Mathf.Exp(-9 * tau) * Mathf.Sin(14 * tau + 0.6f);
        }

        /// <summary>x = 앞뒤 늘어남, y = 위아래 (옆은 부피 유지 비슷하게)</summary>
        static Vector2 SquashAt(float r)
        {
            if (r < Ant) { float k = r / Ant; return new Vector2(1, 0.8f + 0.2f * (1 - Mathf.Pow(1 - k, 3)) + 0.08f * Mathf.Sin(Mathf.PI * k)); }
            if (r < Dash) { float k = Mathf.SmoothStep(0, 1, (r - Ant) / (Dash - Ant)); return new Vector2(1 + 0.08f * k, 1 - 0.2f * k); }
            if (r < Arr) { float v = DashV((r - Dash) / (Arr - Dash)); return new Vector2(1 + 0.6f * v, 1 - 0.15f * v); }
            float w = Mathf.Exp(-11 * (r - Arr)) * Mathf.Cos(24 * (r - Arr));
            return new Vector2(1 - 0.25f * w, 1 - 0.28f * w);
        }

        static void Pose(Transform t, Vector3 pos, Quaternion rot, Vector2 sq)
        {
            float side = 1 / Mathf.Sqrt(Mathf.Max(0.2f, sq.x * sq.y));
            t.SetPositionAndRotation(pos, rot);
            t.localScale = new Vector3(side, sq.y, sq.x) * BodyScale;      // 로컬 Z = 앞뒤
        }

        Transform Body(string key)
        {
            var pf = Resources.Load<GameObject>("Costumes/" + key);
            if (!pf) return null;
            if (!mat)
            {
                mat = Resources.Load<Material>("Costumes/AssassinGhost");        // 빌드에 셰이더가 들어가게 재질로 참조
                if (!mat) { var sh = Shader.Find("CatTang/AssassinGhost") ?? Shader.Find("CatTang/Silhouette"); mat = new Material(sh) { renderQueue = 3050 }; }
            }
            if (!ghostMat) { ghostMat = new Material(mat) { renderQueue = 3040 }; ghostMat.SetFloat("_ZWrite", 0); }
            var go = Instantiate(pf, transform);
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                var ms = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < ms.Length; i++) ms[i] = mat;
                r.sharedMaterials = ms;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            go.SetActive(false);
            return go.transform;
        }

        /// <summary>디졸브: 0 = 다 보임, 1 = 다 사라짐 (경계 띠 보라 광)</summary>
        void Dissolve(Transform t, float d)
        {
            if (!t) return;
            mpb ??= new MaterialPropertyBlock();
            mpb.SetFloat("_Dissolve", Mathf.Clamp01(d));
            foreach (var r in t.GetComponentsInChildren<Renderer>()) r.SetPropertyBlock(mpb);
        }

        IEnumerator Run(Enemy target, float dmg, float delay)
        {
            if (delay > 0) yield return new WaitForSeconds(delay);
            if (!target || !target.health.Alive) { Destroy(gameObject); yield break; }
            var tp = target.transform.position; tp.y = 0;
            var side = Quaternion.Euler(0, Random.Range(0f, 360f), 0) * Vector3.forward;
            var a = tp + side * Appear; var b = tp - side * Beyond;
            var dir = (b - a).normalized;
            var rot = Quaternion.LookRotation(dir);
            var wind = Body("assassin_windup"); var slash = Body("assassin_slash");
            float tilt = Random.value < 0.5f ? 1 : -1;
            var rib = DashSlash.Make(SlashColor, Random.Range(0, 99999), dir, TipFrom, Arr,
                rr => TipAt(rr, a, b, dir, tilt), rr => PosAt(Mathf.Min(rr, Arr), a, b, dir) + Vector3.up * TipH, TipWidth,
                rr => rr < Dash ? 0 : DashV(Mathf.Clamp01((rr - Dash) / (Arr - Dash))));
            float flashAt = -1;
            Sfx.Play("dash", 0.35f, 0.15f);
            bool hit = false;
            float prevR = 0; var lastGhost = PosAt(0, a, b, dir);
            for (float r = 0; r < End; r += Time.deltaTime)
            {
                var pos = PosAt(r, a, b, dir); var sq = SquashAt(r);
                bool dashing = r >= Dash;
                // 이동 전체 잔상: 지난 프레임 ~ 이번 프레임 사이 길을 잘게 나눠 GhostGap마다 그 순간 자세를 남김 (빠른 구간도 끊김 없이 이어짐)
                if (prevR < Arr + 0.15f)
                {
                    float span = (PosAt(Mathf.Min(r, Arr + 0.15f), a, b, dir) - PosAt(prevR, a, b, dir)).magnitude;
                    int sub = Mathf.Clamp(Mathf.CeilToInt(span / 0.06f), 1, 120);
                    for (int s = 1; s <= sub; s++)
                    {
                        float rs = Mathf.Lerp(prevR, Mathf.Min(r, Arr + 0.15f), s / (float)sub);
                        var ps = PosAt(rs, a, b, dir);
                        if ((ps - lastGhost).sqrMagnitude < GhostGap * GhostGap) continue;
                        lastGhost = ps;
                        var bd = rs >= Dash ? slash : wind;
                        if (!bd) continue;
                        Pose(bd, ps, rot, SquashAt(rs));
                        float al = rs < Dash ? 0.4f : Mathf.Lerp(0.55f, 0.3f, Mathf.Clamp01((rs - Dash) / (Arr - Dash)));   // 출발 쪽 진하게
                        ShadowGhost.Spawn(bd, ghostMat, new Color(GhostShade.r, GhostShade.g, GhostShade.b, al), GhostLife);
                    }
                }
                prevR = r;
                if (wind) { wind.gameObject.SetActive(!dashing); if (!dashing) { Pose(wind, pos, rot, sq); Dissolve(wind, 0.95f * Mathf.Pow(1 - Mathf.Clamp01(r / 0.16f), 2)); } }   // 디졸브로 차오름
                if (slash)
                {
                    slash.gameObject.SetActive(dashing);
                    if (dashing) { Pose(slash, pos, rot, sq); Dissolve(slash, 0.95f * Mathf.SmoothStep(0, 1, (r - (End - 0.22f)) / 0.22f)); }   // 디졸브로 사라짐
                }
                // 참격 리본: 칼끝이 지난 자리 (궤적 시작점 → 지금 칼끝), 도착 뒤 긴 꼬리부터 먹힘
                if (rib)
                {
                    float flash = flashAt < 0 ? 0 : 1 - Mathf.Clamp01((r - flashAt) / 0.12f);
                    rib.Set(r, Mathf.SmoothStep(0, 1, (r - EatFrom) / EatTime), flash);
                }
                if (!hit && dashing && Vector3.Dot(pos - tp, dir) >= 0)       // 몸이 대상을 지나는 순간
                {
                    hit = true;
                    if (target && target.health.Alive)
                    {
                        var hp = target.transform.position + Vector3.up * 0.75f * U;   // 타격 지점 = 대상 몸 가운데
                        target.health.TakeDamage(dmg);
                        flashAt = r;
                        HitArcs.Spawn(hp, SlashColor, 1.9f, dir);                      // 타격 아크 + 섬광 + 빛
                        CostumeFx.Flip("mirror_slash", hp, 3.2f * U, 0.3f);
                        Sfx.Play("crit", 0.55f, 0.1f);
                    }
                }
                yield return null;
            }
            if (rib) Destroy(rib.gameObject);
            Destroy(gameObject);
        }
    }

    /// <summary>거울 망토 돌진 잔상 한 장: 그 순간 자세를 굳힌 복제, 알파가 줄며 디졸브로 걷힘</summary>
    public class ShadowGhost : MonoBehaviour
    {
        readonly System.Collections.Generic.List<Renderer> rends = new();
        readonly System.Collections.Generic.List<Mesh> baked = new();
        MaterialPropertyBlock mpb;
        float born, life; Color color;

        public static void Spawn(Transform body, Material m, Color c, float life)
        {
            var g = new GameObject("ShadowGhost").AddComponent<ShadowGhost>();
            g.born = Time.time; g.life = life; g.color = c; g.mpb = new MaterialPropertyBlock();
            foreach (var mf in body.GetComponentsInChildren<MeshFilter>(true))
            {
                if (!mf.sharedMesh) continue;
                g.Part(mf.transform, mf.sharedMesh, m);
            }
            foreach (var smr in body.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (!smr.sharedMesh) continue;
                var bm = new Mesh(); smr.BakeMesh(bm, true); g.baked.Add(bm);
                g.Part(smr.transform, bm, m);
            }
            g.Apply(0);
        }

        void Part(Transform src, Mesh mesh, Material m)
        {
            var p = new GameObject("Part");
            p.transform.SetParent(transform, false);
            p.transform.SetPositionAndRotation(src.position, src.rotation);
            p.transform.localScale = src.lossyScale;
            p.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = p.AddComponent<MeshRenderer>();
            r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rends.Add(r);
        }

        void Apply(float k)
        {
            mpb.SetColor("_Color", new Color(color.r, color.g, color.b, color.a * Mathf.Pow(1 - k, 1.3f)));
            mpb.SetFloat("_Dissolve", 0.45f * k * k);
            foreach (var r in rends) if (r) r.SetPropertyBlock(mpb);
        }

        void Update()
        {
            float k = (Time.time - born) / life;
            if (k >= 1) { Destroy(gameObject); return; }
            Apply(k);
        }

        void OnDestroy() { foreach (var m in baked) if (m) Destroy(m); }
    }

    /// <summary>돌진 참격 리본 (2026-11-24 사용자: 타격 이펙트 느낌 · 광 효과 · 톱니 말고 속도를 강조하는 불규칙 찢김 + 아크 ·
    /// 리본 꼬리가 궤적 시작점부터 돌진이 멈추는 앞까지 자연스럽게 이어지고, 칼 쥔 손 쪽 꼬리는 길게 · 베어 넘긴 반대쪽 꼬리는 짧게).
    /// 칼끝이 지난 자리를 월드에 고정해 그림: 돌진 내내 손 쪽 옆을 따라 가늘고 긴 꼬리 → 대상 근처부터 몸 앞을 돌아 굵은 초승달 → 반대쪽에서 짧고 뾰족하게 끝.
    /// 바깥 날(칼끝 길)은 매끈, 안쪽은 여러 겹 노이즈 + 무작위 V자 찢김 · 떨어져 나간 조각, 찢긴 자리에서 돌진 반대쪽으로 뻗는 속도선(그 자리를 지날 때 속도만큼).
    /// 그림 = 단색 몸(알파) + 더하는 빛(번짐 + 바깥 날 심지). 타격 순간 하얗게 번쩍. 사라짐 = 긴 꼬리(궤적 시작점)부터 칼끝 쪽으로 먹혀 들어감</summary>
    public class DashSlash : MonoBehaviour
    {
        const int K = 170;
        const float Width = 0.75f, Glow = 0.28f;
        static Material bodyMat, glowMat;
        Mesh body, glow;
        MeshRenderer bodyR, glowR;
        MaterialPropertyBlock bodyMpb, glowMpb;
        Color color; Vector3 back;
        Vector3[] P, In; float[] S, T, Wf, Spd;                          // 길이 고르게 뽑은 칼끝 점 · 안쪽 방향 · 길이 비율 · 시각 · 굵기 비율 · 그때 속도
        struct Notch { public float q, depth, half; }
        struct Shred { public float q0, q1, off, thick; }
        struct Streak { public float q, root, len, w, side; }
        Notch[] notches; Shred[] shreds; Streak[] streaks; float seed;
        readonly System.Collections.Generic.List<Vector3> bv = new(), gv = new();
        readonly System.Collections.Generic.List<Color> bc = new(), gc = new();
        readonly System.Collections.Generic.List<int> bt = new(), gt = new();

        /// <param name="tip">시각 → 칼끝 (월드)</param><param name="center">시각 → 몸 가운데 (안쪽 방향 기준)</param>
        /// <param name="width">시각 → 굵기 비율 0~1</param><param name="speed">시각 → 돌진 속도 0~1</param>
        public static DashSlash Make(Color c, int seed, Vector3 dashDir, float r0, float r1,
            System.Func<float, Vector3> tip, System.Func<float, Vector3> center, System.Func<float, float> width, System.Func<float, float> speed)
        {
            if (!bodyMat) { var sh = Shader.Find("CatTang/Sprite") ?? Shader.Find("Sprites/Default"); bodyMat = new Material(sh) { renderQueue = 3060 }; }
            if (!glowMat) { var sh = Shader.Find("CatTang/AdditiveGlow") ?? Shader.Find("CatTang/Sprite"); glowMat = new Material(sh) { renderQueue = 3070 }; }
            var d = new GameObject("DashSlash").AddComponent<DashSlash>();
            d.color = c; d.back = -dashDir;
            d.body = d.Part("Body", bodyMat, out d.bodyR); d.glow = d.Part("Glow", glowMat, out d.glowR);
            d.bodyMpb = new MaterialPropertyBlock(); d.glowMpb = new MaterialPropertyBlock();
            var rnd = new System.Random(seed);
            float R(float lo, float hi) => lo + (float)rnd.NextDouble() * (hi - lo);
            d.seed = R(0, 100);
            // 찢김 자리 · 깊이 · 폭이 모두 제각각 (톱니처럼 고르게 반복되지 않게) — 굵은 초승달 쪽(뒤쪽 절반)에 몰아 둠
            d.notches = new Notch[rnd.Next(4, 7)];
            for (int i = 0; i < d.notches.Length; i++) d.notches[i] = new Notch { q = R(0.4f, 0.95f), depth = R(0.35f, 0.9f), half = R(0.008f, 0.035f) };
            d.shreds = new Shred[rnd.Next(3, 6)];
            for (int i = 0; i < d.shreds.Length; i++) { float q0 = R(0.35f, 0.88f); d.shreds[i] = new Shred { q0 = q0, q1 = q0 + R(0.03f, 0.1f), off = R(0.06f, 0.22f), thick = R(0.05f, 0.13f) }; }
            d.streaks = new Streak[rnd.Next(12, 18)];
            for (int i = 0; i < d.streaks.Length; i++)
                d.streaks[i] = new Streak { q = R(0.15f, 0.97f), root = R(0.1f, 1.05f), len = R(0.35f, 1f), w = R(0.025f, 0.09f), side = R(-0.2f, 0.2f) };
            d.Sample(r0, r1, tip, center, width, speed);
            return d;
        }

        Mesh Part(string n, Material m, out MeshRenderer r)
        {
            var g = new GameObject(n); g.transform.SetParent(transform, false);
            var mesh = new Mesh { name = "DashSlash" + n }; mesh.MarkDynamic();
            g.AddComponent<MeshFilter>().sharedMesh = mesh;
            r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return mesh;
        }

        void Sample(float r0, float r1, System.Func<float, Vector3> tip, System.Func<float, Vector3> center, System.Func<float, float> width, System.Func<float, float> speed)
        {
            // 시각으로 촘촘히 → 누적 길이 → 길이 기준 K점 (가감속이 커도 점이 한쪽에 몰리지 않게)
            int n = 900;
            var dp = new Vector3[n + 1]; var dl = new float[n + 1];
            for (int i = 0; i <= n; i++)
            {
                dp[i] = tip(Mathf.Lerp(r0, r1, i / (float)n));
                if (i > 0) dl[i] = dl[i - 1] + (dp[i] - dp[i - 1]).magnitude;
            }
            float total = Mathf.Max(0.01f, dl[n]);
            P = new Vector3[K]; In = new Vector3[K]; S = new float[K]; T = new float[K]; Wf = new float[K]; Spd = new float[K];
            int j = 0;
            for (int k = 0; k < K; k++)
            {
                float want = total * k / (K - 1);
                while (j < n - 1 && dl[j + 1] < want) j++;
                float u = Mathf.Clamp01((want - dl[j]) / Mathf.Max(1e-5f, dl[j + 1] - dl[j]));
                float t = Mathf.Lerp(r0, r1, (j + u) / n);
                P[k] = Vector3.Lerp(dp[j], dp[j + 1], u); S[k] = want / total; T[k] = t;
                var inw = center(t) - P[k]; inw.y = 0;
                In[k] = inw.sqrMagnitude > 1e-4f ? inw.normalized : Vector3.Cross(Vector3.up, -back);
                Wf[k] = width(t); Spd[k] = speed(t);
            }
        }

        /// <summary>안쪽 가장자리까지의 비율 (1 = 다 참): 여러 겹 노이즈 + 무작위 V자 찢김</summary>
        float Inner(float q)
        {
            float f = 0.6f + 0.4f * (0.65f * Mathf.PerlinNoise(q * 9f + seed, 0.3f) + 0.35f * Mathf.PerlinNoise(q * 31f + seed * 1.7f, 2.1f));
            foreach (var n in notches)
            {
                float d = Mathf.Abs(q - n.q) / n.half;
                if (d < 1) f *= 1 - n.depth * (1 - d) * (1 - d * 0.3f);
            }
            return f;
        }

        static void Quad(System.Collections.Generic.List<int> t, int a, int b, int c, int d) { t.Add(a); t.Add(b); t.Add(c); t.Add(b); t.Add(d); t.Add(c); }

        /// <param name="r">지금 시각 (칼끝이 지난 점까지만)</param><param name="eat">0~1 긴 꼬리부터 먹힌 정도</param><param name="flash">0~1 타격 섬광</param>
        public void Set(float r, float eat, float flash)
        {
            bv.Clear(); bc.Clear(); bt.Clear(); gv.Clear(); gc.Clear(); gt.Clear();
            int head = -1;
            while (head < K - 1 && T[head + 1] <= r) head++;
            if (head >= 2 && eat < 1)
            {
                float cut = -0.06f + 1.16f * eat;
                var white = Color.white;
                var core = new Color(Mathf.Lerp(1, color.r, 0.35f), Mathf.Lerp(1, color.g, 0.35f), 1, 0.85f);
                var clear = new Color(color.r, color.g, color.b, 0);
                // ---- 몸 + 빛
                int prevB = -1, prevG = -1;
                for (int k = 0; k <= head; k++)
                {
                    float q = S[k];
                    float jag = (Mathf.PerlinNoise(q * 14f + seed * 3, 5.5f) - 0.5f) * 0.08f;
                    float m = eat > 0 ? Mathf.Clamp01((q - cut + jag) / 0.05f) : 1;      // 먹히는 끝은 불규칙하게 뾰족
                    if (k == head && head < K - 1) m *= 0.85f;                         // 지금 칼끝
                    if (m <= 0.001f) { prevB = prevG = -1; continue; }
                    float w = Width * Wf[k] * m, inn = Inner(q);
                    var o = P[k]; var i_ = P[k] + In[k] * (w * inn);
                    int vb = bv.Count; bv.Add(o); bv.Add(i_); bc.Add(white); bc.Add(white);
                    if (prevB >= 0) Quad(bt, prevB, prevB + 1, vb, vb + 1);
                    prevB = vb;
                    // 빛: 바깥 번짐(0) → 바깥 날 심지 → 날 안 → 안쪽 가장자리 → 안쪽 번짐(0)
                    float gw = Glow * (0.4f + 0.6f * Mathf.Clamp01(Wf[k] * 1.5f)) * m;
                    int vg = gv.Count;
                    gv.Add(o - In[k] * gw); gc.Add(clear);
                    gv.Add(o - In[k] * 0.015f); gc.Add(core);
                    gv.Add(o + In[k] * Mathf.Min(0.06f, w * 0.3f)); gc.Add(core);
                    gv.Add(i_); gc.Add(new Color(color.r, color.g, color.b, 0.3f));
                    gv.Add(i_ + In[k] * gw); gc.Add(clear);
                    if (prevG >= 0) for (int jj = 0; jj < 4; jj++) Quad(gt, prevG + jj, prevG + jj + 1, vg + jj, vg + jj + 1);
                    prevG = vg;
                }
                // ---- 떨어져 나간 조각: 안쪽 가장자리 밖에 끊겨 떠 있는 가는 띠
                foreach (var s in shreds)
                {
                    int prev = -1;
                    for (int jj = 0; jj <= 8; jj++)
                    {
                        float q = Mathf.Lerp(s.q0, s.q1, jj / 8f);
                        int k = Mathf.Clamp(Mathf.RoundToInt(q * (K - 1)), 0, K - 1);
                        if (k > head || q < cut + 0.04f || q > 1) { prev = -1; continue; }
                        float w = Width * Wf[k], r0 = w * (Inner(q) + s.off);
                        float th = s.thick * Width * Mathf.Sin(Mathf.PI * jj / 8f);
                        int v = bv.Count; bv.Add(P[k] + In[k] * r0); bv.Add(P[k] + In[k] * (r0 + th)); bc.Add(white); bc.Add(white);
                        if (prev >= 0) Quad(bt, prev, prev + 1, v, v + 1);
                        prev = v;
                    }
                }
                // ---- 속도선: 찢긴 자리에서 돌진 반대쪽으로 뻗음, 길이 = 그 자리를 지날 때 속도 × 제각각
                foreach (var s in streaks)
                {
                    int k = Mathf.Clamp(Mathf.RoundToInt(s.q * (K - 1)), 0, K - 1);
                    if (k > head || s.q < cut + 0.02f) continue;
                    float w = Width * Wf[k];
                    var root = P[k] + In[k] * (w * Inner(s.q) * s.root);
                    float fresh = Mathf.Clamp01((r - T[k]) / 0.04f);
                    float L = s.len * (0.25f + 2.4f * Spd[k]) * (0.3f + 0.7f * fresh) * Mathf.Clamp01(Wf[k] * 3f + 0.3f);
                    var dirS = (back + Vector3.Cross(Vector3.up, back) * s.side).normalized;
                    var sideV = Vector3.Cross(Vector3.up, dirS);
                    int v0 = bv.Count, g0 = gv.Count;
                    for (int jj = 0; jj <= 4; jj++)
                    {
                        float u = jj / 4f, ww = s.w * (1 - u) * (1 - u * 0.4f);
                        var p = root + dirS * (L * u);
                        var cc = new Color(1, 1, 1, 1 - u * u);
                        bv.Add(p + sideV * ww); bv.Add(p - sideV * ww); bc.Add(cc); bc.Add(cc);
                        gv.Add(p + sideV * (ww + 0.08f)); gv.Add(p); gv.Add(p - sideV * (ww + 0.08f));
                        gc.Add(clear); gc.Add(new Color(color.r, color.g, color.b, 0.45f * (1 - u))); gc.Add(clear);
                        if (jj > 0)
                        {
                            Quad(bt, v0 + (jj - 1) * 2, v0 + (jj - 1) * 2 + 1, v0 + jj * 2, v0 + jj * 2 + 1);
                            int a0 = g0 + (jj - 1) * 3, a1 = g0 + jj * 3;
                            Quad(gt, a0, a0 + 1, a1, a1 + 1); Quad(gt, a0 + 1, a0 + 2, a1 + 1, a1 + 2);
                        }
                    }
                }
            }
            Fill(body, bv, bc, bt); Fill(glow, gv, gc, gt);
            bodyMpb.SetColor("_Color", Color.Lerp(color, Color.white, flash * 0.6f)); bodyR.SetPropertyBlock(bodyMpb);
            glowMpb.SetFloat("_Intensity", 0.55f + 1.1f * flash); glowR.SetPropertyBlock(glowMpb);
        }

        static void Fill(Mesh m, System.Collections.Generic.List<Vector3> v, System.Collections.Generic.List<Color> c, System.Collections.Generic.List<int> t)
        {
            m.Clear();
            if (t.Count == 0) return;                                        // 빈 메시에 경계 계산하면 경고
            m.SetVertices(v); m.SetColors(c); m.SetTriangles(t, 0); m.RecalculateBounds();
        }

        void OnDestroy() { if (body) Destroy(body); if (glow) Destroy(glow); }
    }

    /// <summary>타격 아크 (2026-11-24 사용자: 아크를 넣어 타격 이펙트임을 표시): 맞은 자리를 둘러 화면을 향한 가는 빛 호 여러 개가 툭 튀어 퍼지며 가늘어짐
    /// + 가운데 섬광(둥근 빛, 짧게) + 진행 방향으로 찢어지는 섬광 가시 + 주변 빛</summary>
    public class HitArcs : MonoBehaviour
    {
        const float Life = 0.22f;
        static Material mat;
        Mesh mesh; float born, size; Color color; Vector3 right, up, fwd;
        struct A { public float ang, span, r0, grow, w, delay; }
        A[] arcs;
        readonly System.Collections.Generic.List<Vector3> v = new();
        readonly System.Collections.Generic.List<Color> c = new();
        readonly System.Collections.Generic.List<int> t = new();

        public static void Spawn(Vector3 at, Color col, float size, Vector3 dashDir)
        {
            if (!mat) { var sh = Shader.Find("CatTang/AdditiveGlow") ?? Shader.Find("CatTang/Sprite"); mat = new Material(sh) { renderQueue = 3075 }; mat.SetFloat("_Intensity", 1.4f); }
            var go = new GameObject("HitArcs");
            go.transform.position = at;
            var h = go.AddComponent<HitArcs>();
            h.born = Time.time; h.size = size; h.color = col;
            var cam = Camera.main;
            h.right = cam ? cam.transform.right : Vector3.right; h.up = cam ? cam.transform.up : Vector3.up;
            var sd = Vector3.ProjectOnPlane(dashDir, cam ? cam.transform.forward : Vector3.up);
            h.fwd = sd.sqrMagnitude > 1e-4f ? sd.normalized : h.right;
            h.mesh = new Mesh { name = "HitArcs" }; h.mesh.MarkDynamic();
            go.AddComponent<MeshFilter>().sharedMesh = h.mesh;
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            int n = Random.Range(4, 7);
            h.arcs = new A[n];
            float baseAng = Random.Range(0f, 360f);
            for (int i = 0; i < n; i++)
                h.arcs[i] = new A { ang = (baseAng + 360f * i / n + Random.Range(-25f, 25f)) * Mathf.Deg2Rad, span = Random.Range(40f, 105f) * Mathf.Deg2Rad,
                                    r0 = Random.Range(0.28f, 0.5f), grow = Random.Range(0.45f, 0.95f), w = Random.Range(0.05f, 0.11f), delay = Random.Range(0f, 0.04f) };
            EffectShadowLight.Flash(at, col, 4f, 3.5f, 0.14f);
            CombatHitStop.Play(0.04f);                                       // 맞는 순간 아주 짧게 멈칫
        }

        Vector3 P(float ang, float rad) => (right * Mathf.Cos(ang) + up * Mathf.Sin(ang)) * rad;

        void Update()
        {
            float age = Time.time - born;
            if (age >= Life) { Destroy(gameObject); return; }
            v.Clear(); c.Clear(); t.Clear();
            var col0 = new Color(color.r, color.g, color.b, 0);
            // 가운데 섬광: 둥근 빛 (앞 40%만)
            float fk = age / (Life * 0.4f);
            if (fk < 1)
            {
                float rad = size * 0.28f * (0.6f + 0.4f * (1 - (1 - fk) * (1 - fk))) * (1 - fk * 0.5f);
                int c0 = v.Count; v.Add(Vector3.zero); c.Add(new Color(1, 1, 1, 1 - fk));
                for (int j = 0; j <= 20; j++) { v.Add(P(Mathf.PI * 2 * j / 20, rad)); c.Add(col0); }
                for (int j = 0; j < 20; j++) { t.Add(c0); t.Add(c0 + 1 + j); t.Add(c0 + 2 + j); }
                // 돌진 방향으로 길게 찢어지는 섬광 가시 (속도)
                float L = size * (0.8f + 0.7f * fk), W = size * 0.05f * (1 - fk);
                var sideV = Vector3.Cross(fwd, Vector3.Cross(right, up)).normalized;
                int s0 = v.Count;
                v.Add(-fwd * L * 0.45f); v.Add(sideV * W); v.Add(-sideV * W); v.Add(fwd * L);
                c.Add(col0); c.Add(new Color(1, 1, 1, 1 - fk)); c.Add(new Color(1, 1, 1, 1 - fk)); c.Add(col0);
                t.Add(s0); t.Add(s0 + 1); t.Add(s0 + 2); t.Add(s0 + 1); t.Add(s0 + 3); t.Add(s0 + 2);
            }
            // 아크: 툭 튀어 퍼지며(빠르게 → 느리게) 가늘어지고 끝이 뾰족
            foreach (var a in arcs)
            {
                float k = Mathf.Clamp01((age - a.delay) / (Life - a.delay));
                if (k <= 0 || k >= 1) continue;
                float e = 1 - Mathf.Pow(1 - k, 3);
                float rad = size * (a.r0 + a.grow * e), w = size * a.w * (1 - k);
                float span = a.span * (0.6f + 0.4f * e);
                int s0 = v.Count;
                for (int j = 0; j <= 12; j++)
                {
                    float u = j / 12f, ang = a.ang - span / 2 + span * u, ww = w * Mathf.Sin(Mathf.PI * u);
                    var mid = P(ang, rad);
                    var nrm = P(ang, 1);
                    v.Add(mid + nrm * ww); v.Add(mid); v.Add(mid - nrm * ww);
                    float al = 1 - k * k;
                    c.Add(new Color(color.r, color.g, color.b, 0)); c.Add(new Color(Mathf.Lerp(1, color.r, k), Mathf.Lerp(1, color.g, k), 1, al)); c.Add(new Color(color.r, color.g, color.b, 0));
                    if (j > 0) { int p0 = s0 + (j - 1) * 3, p1 = s0 + j * 3; Q(p0, p0 + 1, p1, p1 + 1); Q(p0 + 1, p0 + 2, p1 + 1, p1 + 2); }
                }
            }
            mesh.Clear();
            if (t.Count == 0) return;
            mesh.SetVertices(v); mesh.SetColors(c); mesh.SetTriangles(t, 0); mesh.RecalculateBounds();
        }

        void Q(int a, int b, int c_, int d) { t.Add(a); t.Add(b); t.Add(c_); t.Add(b); t.Add(d); t.Add(c_); }

        void OnDestroy() { if (mesh) Destroy(mesh); }
    }

    /// <summary>모래 순례자 로브: 맞힌 자리에 남는 모래 소용돌이 — 안의 적 둔화 + 초당 피해</summary>
    public class SandVortex : MonoBehaviour
    {
        public const float Radius = 1.6f, Tick = 0.25f, MaxAlive = 6;
        static int alive;
        float dps, slow, life;

        public static void Spawn(Vector3 at, float dps, float slow, float life)
        {
            if (alive >= MaxAlive) return;                          // 연사로 겹겹이 쌓이지 않게
            at.y = 0.05f;
            var v = new GameObject("SandVortex").AddComponent<SandVortex>();
            v.transform.position = at;
            v.dps = dps; v.slow = slow > 0 ? slow : 0.3f; v.life = life;
            alive++;
            v.StartCoroutine(v.Run());
        }

        IEnumerator Run()
        {
            // 그림 = 한 벌(sand_full 40장): 명중 광 고리 → 바람 줄기가 솟아 휘감음 → 위로 풀려 흩어짐 — 수명 전체에 맞춰 한 번 틂
            var c = transform.position + Vector3.up * 0.7f;           // 굽기 가운데 높이와 같게
            bool art = CostumeFx.Flip("sand_full", c, 4.4f, life);
            float t = 0, loop = 0;
            while (t < life)
            {
                if (!art && (loop -= Tick) <= 0)
                {
                    loop = 0.55f;
                    GunkataFx.Smoke(transform.position + Random.insideUnitSphere * 0.6f, 1.4f, 0.8f);
                }
                foreach (var e in Enemy.All)
                {
                    if (!e || !e.health.Alive) continue;
                    if (Effects.Flat(e.transform.position - transform.position).sqrMagnitude > Radius * Radius) continue;
                    e.health.ApplySlow(slow, Tick + 0.1f);
                    e.health.TakeDamage(dps * Tick);
                }
                t += Tick;
                yield return new WaitForSeconds(Tick);
            }
            Destroy(gameObject);
        }

        void OnDestroy() => alive--;
    }

    /// <summary>노인의 잠옷: 체력이 낮아 공격력이 오른 동안 몸 둘레 오라, 켜질 때 한 번 펄스</summary>
    public class LowHpAura : MonoBehaviour
    {
        PlayerController p;
        bool on;
        float loop;
        static readonly Color Ember = new(0.56f, 0.77f, 1f);

        public static void Attach(PlayerController p)
        {
            var old = p.GetComponent<LowHpAura>();
            if (old) Destroy(old);
            if (!HasEffect()) return;
            p.gameObject.AddComponent<LowHpAura>().p = p;
        }

        static bool HasEffect()
        {
            if (!Tables.Costumes.TryGetValue(Meta.SelectedCostume, out var c)) return false;
            foreach (var id in new[] { c.Effect1, c.Effect2, c.Effect3 })
                if (id != 0 && Tables.Effects.TryGetValue(id, out var e) && e.Type == "LowHpDamageAdd") return true;
            return false;
        }

        void Update()
        {
            if (!p) return;
            bool now = p.LowHpDamageBonus > 0 && p.health.Alive;
            var c = transform.position + Vector3.up * 0.9f;
            // 켜질 때: 들숨(빛이 모여듦) → 날숨(기의 물결이 온화하게 퍼짐) 24장 · 1.1초 — 굽기 가운데 0.6 높이 · 크기 5
            if (now && !on && !CostumeFx.Flip("pajama_pulse", transform.position + Vector3.up * 0.6f, 5f, 1.1f))
                FxBurst.Spawn(c, 2.6f, FxBurst.Kind.Shield, Ember, 0.5f);
            if (!now) loop = 0;
            on = now;
            if (on && (loop -= Time.deltaTime) <= 0)
            {
                loop = 0.5f;                                     // 오라는 반 바퀴씩 다시 띄워 몸을 따라감
                if (!CostumeFx.Flip("pajama_aura", c, 2.6f, 0.5f))
                    FxBurst.Spawn(c + Random.insideUnitSphere * 0.4f, 0.5f, FxBurst.Kind.Spark, Ember, 0.4f);
            }
        }
    }
}
