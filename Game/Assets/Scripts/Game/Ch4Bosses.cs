// 4장 보스 (2026-11-20)
// 중간 보스 진자 집행관 (Hyper3D 모델 Resources/Enemies/Ch4_Executioner, 원화 Art/tripo_input/ch4_boss/):
// - 이동 = 전광석화 점멸 (2026-11-20 사용자: 지직거리는 글리치와 함께 중간중간 사라졌다 나타나다 갑자기 고양이 앞에 나타나 공격,
//   많이 이동하기보다 흔들리면서 궤적을 헷갈리게). 평소 천천히 다가오며 계속 지직거리고 가끔 옆으로 툭 튐.
//   점멸 묶음 = 짧은 옆걸음(1~2.5m) 3~5번 좌우 번갈아 + 엉뚱한 자리에 가짜 잔상(붉은 / 청록으로 갈라진 몸).
//   마지막엔 고양이 바로 앞(1.8m) 자리에 아주 짧은 글리치 예고(0.3초) → 나타나자마자 X자 베기 (대시로 피할 틈은 남김).
// - 패턴 (4장 기획서 14쪽): 진자 십자 베기(가까움) · 단두대 내려찍기(가까움) · 톱니 원반 던지기(멂) · 초침 자객 호출(거리 무관).
//   고유 기믹(진자 운동 · 판결의 수 · 끊어진 추)은 아직 안 넣음.
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
                Show(model, Random.value > 0.25f);
                if (t >= next)
                {
                    next = t + 0.05f;
                    var side = model.parent ? model.parent.right : Vector3.right;
                    Afterimage.SpawnAt(model, model.position + side * Random.Range(0.15f, 0.45f), Red, 0.12f);
                    Afterimage.SpawnAt(model, model.position - side * Random.Range(0.15f, 0.45f), Cyan, 0.12f);
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
        public override string DisplayName => "진자 집행관";

        // 전광석화 점멸
        public const float ChainEveryMin = 2.2f, ChainEveryMax = 3.6f;   // 패턴 사이 점멸 이동 간격
        public const float BlinkGlitch = 0.15f, BlinkGap = 0.16f;        // 사라지기 전 지직거림 · 나타난 뒤 다음 점멸까지
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
        Transform model;

        protected override void Setup()
        {
            Add("진자 십자 베기", Reach.Near, CrossSlash);
            Add("단두대 내려찍기", Reach.Near, Guillotine);
            Add("톱니 원반 던지기", Reach.Far, Discs);
            Add("초침 자객 호출", Reach.Any, Summon, () => Enemy.All.Count - 1 <= 2);
            nextChain = Time.time + 1.2f;
        }

        void Start() { model = transform.Find("Model"); }

        // 평소: 천천히 다가오며 몸이 계속 지직거리고, 가끔 옆으로 툭 튐 (궤적을 헷갈리게)
        protected override void Move(PlayerController p, Vector3 dir, float dist, float spd)
        {
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
            if (Time.time >= nextChain) return BlinkChain;
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
            Telegraph.Sector(at, dir, AmbushRange, AmbushHalf, T(AmbushTell), Telegraph.Danger);
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
            FxBurst.Spawn(transform.position + Vector3.up * 1.4f + dir * 1.6f, 2.2f, FxBurst.Kind.Slash, new Color(1f, 0.3f, 0.3f));
            var p = P;
            if (p && p.health.Alive)
            {
                var to = Effects.Flat(p.transform.position - transform.position);
                if (to.magnitude <= range && Vector3.Angle(dir, to) <= half) { p.health.TakeDamage(dmg); p.health.Knockback(dir * 5f); }
            }
            PropHit.Area(transform.position + dir * range * 0.5f, range * 0.5f, dmg);
            yield return null;
        }

        // ---------- 패턴 ----------
        // 진자 십자 베기: 두 칼날을 X자로, 전방 부채꼴 100° · 4m · 예고 0.7 · 피해 24
        IEnumerator CrossSlash()
        {
            var d = ToPlayer();
            Telegraph.Sector(transform.position, d, CrossRange, CrossHalf, T(CrossTell), Telegraph.Danger);
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
    }
}
