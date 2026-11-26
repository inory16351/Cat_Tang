// 4장 보스 (2026-11-20)
// 중간 보스 진자 집행관 (Hyper3D 모델 Resources/Enemies/Ch4_Executioner, 원화 Art/tripo_input/ch4_boss/):
// - 이동 = 전광석화 점멸 (2026-11-20 사용자: 지직거리는 글리치와 함께 중간중간 사라졌다 나타나다 갑자기 고양이 앞에 나타나 공격,
//   많이 이동하기보다 흔들리면서 궤적을 헷갈리게). 평소 천천히 다가오며 계속 지직거리고 가끔 옆으로 툭 튐.
//   점멸 묶음 = 짧은 옆걸음(1~2.5m) 3~5번 좌우 번갈아 + 엉뚱한 자리에 가짜 잔상(붉은 / 청록으로 갈라진 몸).
//   마지막엔 고양이 바로 앞(1.8m) 자리에 아주 짧은 글리치 예고(0.3초) → 나타나자마자 X자 베기 (대시로 피할 틈은 남김).
// - 패턴 (4장 기획서 14쪽): 진자 십자 베기(가까움) · 단두대 내려찍기(가까움) · 톱니 원반 던지기(멂) · 초침 자객 호출(거리 무관).
// - 고유 기믹 (기획서 11 ~ 13쪽): 진자 운동(12초 뒤 첫 발동 · 25초마다, 왼쪽 / 오른쪽 번갈아 8번, 7번째 엇박) ·
//   판결의 수(25초 뒤 · 25초마다, 진자 운동과 8초 이상 떨어뜨림, 숫자 n · 5초 카운트다운 → 발사 시각 = n 무죄) ·
//   끊어진 추(체력 66 / 33% 확정, 추 3개 · 자기 시각 탄에만 멈춤 · 12초)
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    /// <summary>지직거리는 글리치: 모델을 마구 떨고 깜빡이며 붉은 · 청록 잔상으로 갈라짐</summary>
    public static class Glitch
    {
        public static readonly Color Red = new(1f, 0.15f, 0.2f, 0.8f);
        public static readonly Color Cyan = new(0.3f, 0.95f, 1f, 0.75f);

        static Renderer[] Rends(Transform model) => model ? model.GetComponentsInChildren<Renderer>(true) : new Renderer[0];

        public static void Show(Transform model, bool on)
        {
            foreach (var r in Rends(model)) if (r && !(r is TrailRenderer)) r.enabled = on;
        }

        /// <summary>dur 동안 지직거림 (모델 떨림 + 깜빡임 + 색 갈라진 잔상). 끝나면 모델 자리 · 보임 원래대로</summary>
        public static IEnumerator Shake(Transform model, float dur, float amp = 0.28f)
        {
            if (!model) yield break;
            var basePos = model.localPosition;
            float next = 0;
            for (float t = 0; t < dur; t += Time.deltaTime)
            {
                if (!model) yield break;
                var j = new Vector3(Random.Range(-amp, amp), Random.Range(-amp, amp) * 0.4f, Random.Range(-amp, amp));
                model.localPosition = basePos + j;
                Show(model, Random.value > 0.25f * GameSettings.FlashScale);   // 번쩍임 줄이기: 깜빡임 덜 (2026-11-26)
                if (t >= next)
                {
                    next = t + 0.05f;
                    var side = model.parent ? model.parent.right : Vector3.right;
                    var r = Red; r.a *= GameSettings.FlashScale; var cy = Cyan; cy.a *= GameSettings.FlashScale;   // 번쩍임 줄이기: 장식 글리치만 옅게 (예고 잔상 Ghost는 그대로)
                    Afterimage.SpawnAt(model, model.position + side * Random.Range(0.15f, 0.45f), r, 0.12f);
                    Afterimage.SpawnAt(model, model.position - side * Random.Range(0.15f, 0.45f), cy, 0.12f);
                }
                yield return null;
            }
            if (!model) yield break;
            model.localPosition = basePos;
            Show(model, true);
        }

        /// <summary>그 자리에 지직거리는 잔상만 남김 (사라진 자리 · 나타날 자리 예고)</summary>
        public static void Ghost(Transform model, Vector3 at, float life = 0.25f)
        {
            if (!model) return;
            var side = model.parent ? model.parent.right : Vector3.right;
            Afterimage.SpawnAt(model, at + side * 0.3f, Red, life);
            Afterimage.SpawnAt(model, at - side * 0.3f, Cyan, life);
            Afterimage.SpawnAt(model, at, new Color(1f, 1f, 1f, 0.5f), life * 0.6f);
        }
    }

    public class Ch4MidBoss : BossBrain
    {
        public override float NearDist => 5f;
        public override float PatternGap => 0.9f;
        public override string DisplayName => "기로틴";

        // 전광석화 점멸
        public const float ChainEveryMin = 3.0f, ChainEveryMax = 4.5f;   // 패턴 사이 점멸 이동 간격
        public const float BlinkGlitch = 0.2f, BlinkGap = 0.32f;         // 사라지기 전 지직거림 · 나타난 뒤 다음 점멸까지 (2026-11-20 사용자: 사이 간격이 너무 빠름 → 0.16 → 0.32)
        public const float BlinkFar = 7f;                                // (2026-11-20 사용자) 고양이가 이보다 멀 때만 전광석화로 다가옴, 가까우면 그냥 걸음
        // (2026-11-20 사용자: 많이 이동하기보다 흔들리면서 궤적을 헷갈리게 하다가 갑자기 나타나 공격) → 짧은 옆걸음 점멸 + 가짜 잔상
        public const float StepMin = 1.0f, StepMax = 2.5f, StepIn = 0.9f;    // 옆으로 튀는 거리 · 한 번에 다가오는 거리
        public const float CreepSpeed = 0.45f;                               // 평소 이속 배율 (천천히 다가오며 지직거림)
        public const float JitterEvery = 0.9f;                               // 평소 지직 · 짧은 옆 튐 간격
        public const float AmbushDist = 1.8f, AmbushTell = 0.3f, AmbushChance = 0.8f;
        public const float AmbushRange = 4f, AmbushHalf = 55f, AmbushDmg = 24f;
        // 패턴 수치 (기획서 14쪽)
        public const float CrossTell = 0.7f, CrossRange = 4f, CrossHalf = 50f, CrossDmg = 24f;
        public const float DropTell = 0.8f, DropR = 1.5f, DropDmg = 26f, WaveLen = 8f, WaveW = 1.5f, WaveDmg = 22f;
        public const float DiscSpeed = 9f, DiscDmg = 22f;

        float nextChain, nextJitter;
        // 고유 기믹
        public const float SwingFirst = 12f, SwingEvery = 25f, SwingTell = 0.6f, SwingDmg = 22f;
        static readonly float[] SwingGaps = { 1.2f, 1.1f, 1f, 0.9f, 0.85f, 0.8f, 0.7f, 0.7f };
        public const float VerdictFirst = 25f, VerdictEvery = 25f, VerdictCount = 5f, VerdictStun = 2f, VerdictFail = 0.25f;
        public const float WeightTime = 12f, WeightDmg = 22f, WeightFail = 0.1f, WeightStun = 3f;
        float nextSwing, nextVerdict;
        bool inGimmick;
        readonly HashSet<int> weightDone = new();
        readonly Dictionary<Health, int> weightHour = new();
        readonly HashSet<Health> weightStopped = new();
        Transform model;

        protected override void Setup()
        {
            Add("진자 십자 베기", Reach.Near, CrossSlash);
            Add("단두대 내려찍기", Reach.Near, Guillotine);
            Add("톱니 원반 던지기", Reach.Far, Discs);
            Add("초침 자객 호출", Reach.Any, Summon, () => Enemy.All.Count - 1 <= 2);
            nextChain = Time.time + 1.2f;
            nextSwing = Time.time + SwingFirst;
            nextVerdict = Time.time + VerdictFirst;
        }

        void Start() { model = transform.Find("Model"); if (P) P.OnFired += OnPlayerFired; }

        // 칼빛 리본: 이 모델은 무기가 몸 메시에 붙어 있어 무기 뼈가 없음 → 오른손 뼈 기준, 칼끝 = 아래팔 → 손 방향으로 1.5m 늘인 자리 (플레이로 각도 확인 필요)
        Transform hand; Vector3 tipLocal, gripLocal;
        void BladeRibbon(float swing)
        {
            if (!hand)
            {
                var anim = model ? model.GetComponentInChildren<Animator>() : null;
                if (!anim || !anim.isHuman) return;
                hand = anim.GetBoneTransform(HumanBodyBones.RightHand);
                var fore = anim.GetBoneTransform(HumanBodyBones.RightLowerArm);
                if (!hand || !fore) return;
                var along = (hand.position - fore.position).normalized;
                tipLocal = hand.InverseTransformPoint(hand.position + along * 1.5f);
                gripLocal = hand.InverseTransformPoint(hand.position + along * 0.35f);
            }
            BladeStreak.Begin(hand, tipLocal, gripLocal, swing, BladeStreak.Ch4Base);
        }
        protected override void OnDisable() { base.OnDisable(); if (P) P.OnFired -= OnPlayerFired; Glitch.Show(model, true); }

        // 평소: 천천히 다가오며 몸이 계속 지직거리고, 가끔 옆으로 툭 튐 (궤적을 헷갈리게)
        protected override void Move(PlayerController p, Vector3 dir, float dist, float spd)
        {
            if (inGimmick) return;
            // 가까우면(전광석화 거리 안) 지직거림 · 옆 튐 없이 그냥 걸어서 다가옴
            if (dist <= BlinkFar) { if (dist > NearDist * 0.7f) transform.position += dir * spd * Time.deltaTime; return; }
            if (dist > NearDist * 0.7f) transform.position += dir * spd * CreepSpeed * Time.deltaTime;
            if (Time.time < nextJitter) return;
            nextJitter = Time.time + T(JitterEvery) * Random.Range(0.6f, 1.4f);
            if (!model) model = transform.Find("Model");
            if (Random.value < 0.45f)
            {
                var side = Vector3.Cross(Vector3.up, dir) * (Random.value < 0.5f ? 1f : -1f) * Random.Range(0.6f, 1.4f);
                Glitch.Ghost(model, transform.position, 0.2f);
                var to = Clamp(transform.position + side);
                transform.position = new Vector3(to.x, transform.position.y, to.z);
                Sfx.Play("e4m_glitch", 0.3f, 0.15f, 1);
            }
            StartCoroutine(Glitch.Shake(model, Random.Range(0.08f, 0.16f), 0.2f));
        }

        protected override System.Func<IEnumerator> Forced()
        {
            // 끊어진 추(체력 구간 확정) > 판결의 수 · 진자 운동(쿨) > 전광석화
            foreach (int th in new[] { 66, 33 })
                if (!weightDone.Contains(th) && hp.hp <= hp.maxHp * th / 100f) { weightDone.Add(th); return () => G(Weights()); }
            if (Time.time >= nextVerdict) { nextVerdict = Time.time + VerdictEvery; nextSwing = Mathf.Max(nextSwing, Time.time + 8f); return () => G(Verdict()); }
            if (Time.time >= nextSwing) { nextSwing = Time.time + SwingEvery; nextVerdict = Mathf.Max(nextVerdict, Time.time + 8f); return () => G(PendulumSwing()); }
            if (Time.time >= nextChain && Dist() > BlinkFar) return BlinkChain;   // 멀 때만 전광석화
            return null;
        }

        Vector3 Clamp(Vector3 to) => RunManager.Instance ? RunManager.Instance.ClampToRoom(to, 2f) : to;

        /// <summary>한 번 점멸: 지직거림 → 사라짐(자리에 잔상) → to에 나타남(잠깐 지직)</summary>
        IEnumerator BlinkTo(Vector3 to, float glitch)
        {
            if (!model) model = transform.Find("Model");
            Sfx.Play("e4m_glitch", 0.55f, 0.12f, 2);
            yield return Glitch.Shake(model, glitch);
            Glitch.Ghost(model, transform.position, 0.3f);
            Glitch.Show(model, false);
            var trail = GetComponent<EyeTrail>(); if (trail) trail.Hot(0.15f);
            to = Clamp(to);
            transform.position = new Vector3(to.x, transform.position.y, to.z);
            if (P) transform.rotation = Quaternion.LookRotation(ToPlayer());
            Sfx.Play("e4m_blink", 0.6f, 0.1f, 2);
            Glitch.Show(model, true);
            StartCoroutine(Glitch.Shake(model, 0.08f, 0.18f));
        }

        // 전광석화: 짧은 옆걸음 점멸 3~5번(좌우 번갈아, 조금씩 다가옴) + 엉뚱한 자리에 가짜 잔상 → 어디로 오는지 헷갈리게 하다가 고양이 앞 기습
        IEnumerator BlinkChain()
        {
            nextChain = Time.time + T(Random.Range(ChainEveryMin, ChainEveryMax));
            var p = P;
            if (!p) yield break;
            int n = Random.Range(3, 6);
            float side = Random.value < 0.5f ? 1f : -1f;
            for (int i = 0; i < n; i++)
            {
                var toP = Effects.Flat(p.transform.position - transform.position);
                if (toP.sqrMagnitude < 0.01f) toP = transform.forward;
                toP.Normalize();
                var lat = Vector3.Cross(Vector3.up, toP) * side;
                side = -side;
                var to = transform.position + lat * Random.Range(StepMin, StepMax) + toP * (toP.sqrMagnitude > 0 && Dist() > 3.5f ? StepIn : 0f);
                // 가짜: 반대쪽 · 고양이 둘레 엉뚱한 자리에 잔상만 깜빡
                Glitch.Ghost(model, transform.position - lat * Random.Range(1.5f, 3f), 0.22f);
                if (Random.value < 0.5f) Glitch.Ghost(model, p.transform.position + Quaternion.Euler(0, Random.Range(0f, 360f), 0) * Vector3.forward * Random.Range(3f, 5f), 0.18f);
                yield return BlinkTo(to, T(BlinkGlitch) * Random.Range(0.6f, 1.1f));
                yield return Wait(T(BlinkGap) * Random.Range(0.6f, 1.3f));
            }
            if (Random.value < AmbushChance) yield return Ambush();
        }

        /// <summary>고양이 바로 앞에 나타나 X자 베기. 나타날 자리에 0.3초 글리치 예고</summary>
        IEnumerator Ambush()
        {
            var p = P;
            if (!p) yield break;
            // 고양이가 보고 있는(쏘는) 쪽 앞, 없으면 보스 쪽
            var face = p.AimDir; face.y = 0;
            if (face.sqrMagnitude < 0.01f) face = Effects.Flat(transform.position - p.transform.position);
            var at = Clamp(p.transform.position + face.normalized * AmbushDist);
            var dir = Effects.Flat(p.transform.position - at);
            if (dir.sqrMagnitude < 0.01f) dir = -face;
            dir.Normalize();
            Glitch.Ghost(model, at, T(AmbushTell));
            Telegraph.Sector(at, dir, AmbushRange, AmbushHalf, T(AmbushTell), Telegraph.Danger).NoLaser();
            Sfx.Play("e4m_glitch", 0.7f, 0.05f, 2);
            yield return Glitch.Shake(model, T(AmbushTell) * 0.6f);
            Glitch.Show(model, false);
            yield return Wait(T(AmbushTell) * 0.4f);
            transform.position = new Vector3(at.x, transform.position.y, at.z);
            transform.rotation = Quaternion.LookRotation(dir);
            Glitch.Show(model, true);
            Sfx.Play("e4m_blink", 0.7f, 0.05f, 2);
            if (self) self.Anim("Attack");
            yield return Slash(dir, AmbushRange, AmbushHalf, AmbushDmg);
            yield return Wait(T(0.5f));                                       // 기습 뒤 빈틈
        }

        IEnumerator Slash(Vector3 dir, float range, float half, float dmg)
        {
            Sfx.Play("e4_scythe", 0.8f, 0.05f, 2);
            CameraFollow.Shake(0.12f);
            MeleeReachFx.Sweep(transform.position, dir, range, half, BladeStreak.Ch4Base);
            BladeRibbon(0.35f);   // (2026-11-20 사용자) 4장 적처럼 무기 궤적 = 레이저 리본 (예전: 판 그림 FxBurst.Slash)
            var p = P;
            if (p && p.health.Alive)
            {
                var to = Effects.Flat(p.transform.position - transform.position);
                if (to.magnitude <= range && Vector3.Angle(dir, to) <= half) { p.health.TakeDamage(dmg, hitStop: 0.036f); p.health.Knockback(dir * 5f); }
            }
            PropHit.Area(transform.position + dir * range * 0.5f, range * 0.5f, dmg);
            yield return null;
        }

        // ---------- 패턴 ----------
        // 진자 십자 베기: 두 칼날을 X자로, 전방 부채꼴 100° · 4m · 예고 0.7 · 피해 24
        IEnumerator CrossSlash()
        {
            var d = ToPlayer();
            Telegraph.Sector(transform.position, d, CrossRange, CrossHalf, T(CrossTell), Telegraph.Danger).NoLaser();
            float end = Time.time + T(CrossTell);
            while (Time.time < end) { Face(d, 12f); yield return null; }
            yield return Slash(d, CrossRange, CrossHalf, CrossDmg);
            yield return Wait(T(0.4f));
        }

        // 단두대 내려찍기: 고양이 자리 원 0.8초 → 점멸로 그 위에 나타나 내려찍음(26) → 앞뒤 직선 충격파(22)
        IEnumerator Guillotine()
        {
            var p = P;
            if (!p) yield break;
            var at = Clamp(Effects.Flat(p.transform.position));
            var fwd = Effects.Flat(at - transform.position);
            if (fwd.sqrMagnitude < 0.01f) fwd = transform.forward;
            fwd.Normalize();
            Telegraph.Ring(at, DropR, T(DropTell), Telegraph.Danger);
            Telegraph.Lane(at, fwd, WaveLen, WaveW, T(DropTell) + T(0.2f), Telegraph.Danger).NoGlint();
            Telegraph.Lane(at, -fwd, WaveLen, WaveW, T(DropTell) + T(0.2f), Telegraph.Danger).NoGlint();
            yield return Glitch.Shake(model, T(DropTell) * 0.5f);
            Glitch.Show(model, false);
            yield return Wait(T(DropTell) * 0.5f);
            transform.position = new Vector3(at.x, transform.position.y, at.z);
            Glitch.Show(model, true);
            if (self) self.Anim("Attack");
            CameraFollow.Shake(0.22f);
            Sfx.Play("e5_giant_slam", 0.9f, 0.05f, 2);
            FxBurst.Spawn(at + Vector3.up * 0.3f, 2.6f, FxBurst.Kind.Dust);
            HitCircle(at, DropR, DropDmg);
            yield return Wait(T(0.2f));
            var pl = P;
            if (pl && pl.health.Alive)
                foreach (var d in new[] { fwd, -fwd })
                    if (Telegraph.InLane(pl.transform.position, at, d, WaveLen, WaveW)) { pl.health.TakeDamage(WaveDmg); break; }
            yield return Wait(T(0.6f));
        }

        // 톱니 원반 던지기: 40° 부채꼴 3개 · 9m/초 · 피해 22
        IEnumerator Discs()
        {
            var d = ToPlayer();
            Telegraph.Sector(transform.position, d, 6f, 20f, T(0.5f), Telegraph.Danger);
            float end = Time.time + T(0.5f);
            while (Time.time < end) { Face(d, 12f); yield return null; }
            if (self) self.Anim("Attack");
            for (int i = -1; i <= 1; i++) Shoot(Quaternion.Euler(0, i * 20f, 0) * d, DiscSpeed, DiscDmg, 1.4f, 16f, EnemyProjectile.Style.Numeral, 0);
            Sfx.Play("e4_toss", 0.7f, 0.05f, 2);
            yield return Wait(T(0.5f));
        }

        // 초침 자객 호출: 2명 · 필드 최대 4명 · 필드 몬스터 2명 이하일 때만
        IEnumerator Summon()
        {
            yield return Glitch.Shake(model, T(0.4f));
            int room = 4 - (Enemy.All.Count - 1);
            for (int i = 0; i < Mathf.Min(2, room); i++)
            {
                var pos = Clamp(transform.position + transform.right * (i == 0 ? -3f : 3f));
                Telegraph.Ring(pos, 0.7f, 0.6f, Telegraph.Aim);
                var e = EnemyFactory.Assassin4(pos, self.chapter ?? Tables.Chapter(4));
                RunManager.Instance?.AddEnemy(e);
            }
            yield return Wait(T(0.4f));
        }
        // ================= 고유 기믹 =================
        IEnumerator G(IEnumerator inner)
        {
            inGimmick = true;
            float pausedAt = Time.time;
            yield return inner;
            float paused = Time.time - pausedAt;                                 // 기믹 동안 다른 쿨은 멈춤
            nextSwing += paused; nextVerdict += paused; nextChain = Time.time + 1.5f;
            hp.invulnerable = false;
            inGimmick = false;
        }

        Vector3 RoomC => RunManager.Instance ? RunManager.Instance.RoomCenter : transform.position;
        Vector2 RoomSize => RunManager.Instance && RunManager.Instance.CurrentRoom != null ? RunManager.Instance.CurrentRoom.size : new Vector2(28, 24);

        // ① 진자 운동: 천장 진자가 방을 왼쪽 / 오른쪽으로 번갈아 8번 쓸어 냄 (간격 1.2 → 0.7초, 7번째 엇박 = 같은 쪽 두 번).
        //    쓸 쪽 바닥 0.6초 붉게 예고 → 방 절반 피해 22. 집행관은 제자리에서 칼날로 박자를 맞춤 (공격 가능)
        IEnumerator PendulumSwing()
        {
            BossFx.Toast("진자 운동 — 진자가 쓸고 가는 반대쪽으로!");
            var c = RoomC; var size = RoomSize;
            bool left = Random.value < 0.5f;
            for (int i = 0; i < 8; i++)
            {
                if (i == 6) left = !left;                                        // 7번째 엇박: 같은 쪽 두 번
                float sx = left ? -1f : 1f;
                var half = c + new Vector3(sx * size.x / 4f, 0, 0);
                Telegraph.Lane(half - new Vector3(0, 0, size.y / 2f), Vector3.forward, size.y, size.x / 2f, SwingTell, Telegraph.Danger).NoGlint();
                Sfx.Play("e4_tick", 0.6f, 0f, 2);
                if (self) self.Anim(left ? "SwingL" : "SwingR");
                yield return Wait(SwingTell);
                Sfx.Play("e4_scythe", 0.8f, 0.05f, 2);
                CameraFollow.Shake(0.15f);
                FxBurst.Spawn(half + Vector3.up * 1.5f, 3f, FxBurst.Kind.Slash, new Color(1f, 0.3f, 0.3f));
                var p = P;
                if (p && p.health.Alive && Mathf.Sign(p.transform.position.x - c.x) == sx) p.health.TakeDamage(SwingDmg);
                left = !left;
                yield return Wait(Mathf.Max(0.1f, SwingGaps[i] - SwingTell * 0.5f));
            }
        }

        // ② 판결의 수: 고양이 머리 위 숫자 n(2~11) → 5초 카운트다운 → 0일 때 발사 시각 = n 이면 무죄(단두대가 집행관 쪽, 2초 기절), 아니면 유죄(고양이 자리 단두대, 최대 체력 25%)
        IEnumerator Verdict()
        {
            var p = P;
            if (!p) yield break;
            int n = Random.Range(2, 12);
            BossFx.Toast($"판결의 수 — 0이 될 때 발사 시각을 {n}시로 맞춰라!");
            var tag = Label(p.transform, new Vector3(0, 3.2f, 0), n.ToString(), new Color(1f, 0.25f, 0.25f), 1.4f, BigLabel.Frame.Mark);
            var count = Label(transform, new Vector3(0, 5.2f, 0), "5", Color.white, 1.2f, BigLabel.Frame.Timer);
            for (int s = 5; s > 0; s--)
            {
                if (count) count.text = s.ToString();
                Sfx.Play("e4_tick", 0.7f, 0f, 2);
                yield return Wait(1f);
            }
            if (tag) Destroy(tag.gameObject);
            if (count) Destroy(count.gameObject);
            p = P;
            if (!p) yield break;
            bool innocent = p.rev.next == n;
            var at = innocent ? Effects.Flat(transform.position) : Effects.Flat(p.transform.position);
            Telegraph.Ring(at, 1.6f, 0.35f, innocent ? Telegraph.Aim : Telegraph.Danger);
            yield return Wait(0.35f);
            CameraFollow.Shake(0.25f);
            Sfx.Play("e5_giant_slam", 0.9f, 0f, 2);
            FxBurst.Spawn(at + Vector3.up, 2.4f, FxBurst.Kind.Slash, innocent ? Color.white : new Color(1f, 0.25f, 0.25f));
            if (innocent) { BossFx.Toast("무죄 — 단두대가 집행관에게!"); hp.ApplyStun(VerdictStun); }
            else { BossFx.Toast($"유죄 — 발사 시각 {p.rev.next}시"); if (Effects.Flat(p.transform.position - at).magnitude <= 1.6f) p.health.TakeDamage(p.health.maxHp * VerdictFail); }
            yield return Wait(0.3f);
        }

        // ③ 끊어진 추: 추 3개(서로 다른 시각 1~12), 흔들림 반경 2m → 3초마다 +1m (최대 6m), 닿으면 22.
        //    추는 자기 시각 탄에만 멈춤 (다른 탄은 튕김). 12초 뒤 남은 추 하나당 최대 체력 10%, 다 멈추면 집행관 3초 기절. 진행 동안 집행관은 멈추고 무적
        IEnumerator Weights()
        {
            BossFx.Toast("끊어진 추 — 추의 숫자와 같은 시각 탄으로 멈춰라!");
            hp.invulnerable = true;
            var c = RoomC; var size = RoomSize;
            var hours = new List<int>();
            while (hours.Count < 3) { int h = Random.Range(1, 13); if (!hours.Contains(h)) hours.Add(h); }
            weightHour.Clear(); weightStopped.Clear();
            var anchors = new List<Vector3>(); var axes = new List<Vector3>(); var bodies = new List<Transform>(); var phases = new List<float>();
            for (int i = 0; i < 3; i++)
            {
                var a = c + new Vector3((i - 1) * size.x * 0.28f, 0, Random.Range(-size.y * 0.15f, size.y * 0.15f));
                var go = new GameObject("BrokenWeight_" + hours[i]);
                go.transform.position = a;
                Own(go);
                Ch3Fx.Prim(PrimitiveType.Sphere, "Weight", go.transform, Vector3.up * 1.1f, Vector3.one * 1.6f, new Color(0.25f, 0.25f, 0.28f));
                var h = Ch3Fx.Target(go, 0.9f, 2f);
                weightHour[h] = hours[i];
                Label(go.transform, new Vector3(0, 2.6f, 0), hours[i].ToString(), new Color(1f, 0.85f, 0.4f), 1.2f, BigLabel.Frame.Hour);
                anchors.Add(a); axes.Add(Quaternion.Euler(0, Random.Range(0f, 180f), 0) * Vector3.right); bodies.Add(go.transform); phases.Add(Random.Range(0f, 6.28f));
            }
            float start = Time.time, end = start + WeightTime;
            var lastTouch = new float[3];
            while (Time.time < end && weightStopped.Count < 3)
            {
                float r = Mathf.Min(6f, 2f + Mathf.Floor((Time.time - start) / 3f));
                var p = P;
                for (int i = 0; i < 3; i++)
                {
                    var b = bodies[i];
                    if (!b) continue;
                    var h = b.GetComponent<Health>();
                    if (weightStopped.Contains(h)) continue;
                    b.position = anchors[i] + axes[i] * Mathf.Sin((Time.time - start) * 2.2f + phases[i]) * r;
                    if (p && p.health.Alive && Time.time - lastTouch[i] > 0.8f && Effects.Flat(p.transform.position - b.position).magnitude < 1.3f) { lastTouch[i] = Time.time; p.health.TakeDamage(WeightDmg); }
                }
                yield return null;
            }
            int left = 3 - weightStopped.Count;
            hp.invulnerable = false;
            if (left == 0) { BossFx.Toast("추가 다 멈췄다 — 집행관 기절!"); hp.ApplyStun(WeightStun); }
            else
            {
                CameraFollow.Shake(0.3f);
                Sfx.Play("e4_whirl", 0.9f, 0f, 2);
                var p = P;
                if (p && p.health.Alive) p.health.TakeDamage(p.health.maxHp * WeightFail * left);
            }
            foreach (var b in bodies) if (b) Destroy(b.gameObject);
            weightHour.Clear(); weightStopped.Clear();
        }

        void OnPlayerFired(int slot, bool awake)
        {
            if (weightHour.Count == 0) return;
            foreach (var kv in weightHour)
            {
                var h = kv.Key;
                if (!h || weightStopped.Contains(h) || Time.time - h.lastHitAt > 0.001f) continue;
                if (slot == kv.Value) { weightStopped.Add(h); Sfx.Play("e4_clank", 0.8f, 0.05f, 2); FxBurst.Spawn(h.transform.position + Vector3.up, 1.4f, FxBurst.Kind.Gold, Color.white); }
                else FxBurst.Spawn(h.transform.position + Vector3.up, 0.8f, FxBurst.Kind.Spark, Color.white);
            }
        }
    }
}
