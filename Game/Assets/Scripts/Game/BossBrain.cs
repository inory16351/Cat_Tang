// 보스 두뇌 공통: 패턴 고르기 · 페이즈 · 코루틴 패턴 실행
// 패턴 고르기 (모든 보스 공통, 2026-11-18 사용자 확정): 거리에 맞는 패턴 + 거리 무관 패턴을 동일 확률로 무작위 발동, 직전 패턴은 제외.
// 패턴마다 추가 조건(예: 하수인 호출은 필드 몬스터 2명 이하일 때만)을 걸 수 있음.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    public enum Reach { Near, Far, Any }

    public class BossPattern
    {
        public string id;
        public Reach reach;
        public System.Func<bool> allowed;           // 추가 조건 (없으면 늘 가능)
        public System.Func<IEnumerator> run;
        public string anim = "Attack";             // 패턴 시작 때 애니메이터 트리거
    }

    public static class PatternPicker
    {
        /// <summary>후보 = 거리에 맞는 패턴 + 거리 무관 패턴 (조건 통과, 직전 패턴 제외). 동일 확률. 후보가 없으면 직전 패턴도 허용</summary>
        public static BossPattern Pick(List<BossPattern> all, bool near, string last)
        {
            var c = Candidates(all, near, last);
            if (c.Count == 0) c = Candidates(all, near, null);
            return c.Count == 0 ? null : c[Random.Range(0, c.Count)];
        }

        static List<BossPattern> Candidates(List<BossPattern> all, bool near, string exclude)
        {
            var list = new List<BossPattern>();
            foreach (var p in all)
            {
                if (p.id == exclude) continue;
                if (p.reach == Reach.Near && !near) continue;
                if (p.reach == Reach.Far && near) continue;
                if (p.allowed != null && !p.allowed()) continue;
                list.Add(p);
            }
            return list;
        }
    }

    /// <summary>Enemy(kind = Boss)에 붙는 패턴 두뇌. Enemy.Update가 Tick을 부름</summary>
    public abstract class BossBrain : MonoBehaviour
    {
        protected Enemy self;
        protected Health hp;
        protected readonly List<BossPattern> patterns = new();
        protected string lastPattern;
        protected bool busy;                       // 패턴 실행 중 (이동 멈춤 여부는 패턴이 정함)
        protected bool moveWhileBusy;
        protected float nextPatternAt;
        Coroutine running;

        public abstract float NearDist { get; }
        public abstract float PatternGap { get; }  // 패턴 사이 간격 (초)
        public virtual string DisplayName => "보스";

        /// <summary>체력 50% 이하부터 공격 속도와 패턴 간격 1.2배 (1장 기획서)</summary>
        public float PhaseMul => hp && hp.hp <= hp.maxHp * 0.5f ? 1.2f : 1f;
        /// <summary>시간 계열 값에 곱함 (빨라질수록 짧아짐)</summary>
        public float T(float seconds) => seconds / (PhaseMul * Mathf.Max(0.2f, hp ? hp.AttackSpeedMul : 1f));

        protected PlayerController P => PlayerController.Instance;

        protected virtual void Awake()
        {
            self = GetComponent<Enemy>();
            hp = GetComponent<Health>();
            Setup();
            nextPatternAt = Time.time + 1.5f;
        }

        protected abstract void Setup();
        /// <summary>등장 연출 뒤 첫 패턴까지 최소 s초</summary>
        public void HoldPatterns(float s) => nextPatternAt = Mathf.Max(nextPatternAt, Time.time + s);
        protected void Add(string id, Reach reach, System.Func<IEnumerator> run, System.Func<bool> allowed = null, string anim = "Attack") =>
            patterns.Add(new BossPattern { id = id, reach = reach, run = run, allowed = allowed, anim = anim });

        /// <summary>추첨과 별개로 끼어드는 확정 패턴 (예: 체력 75 · 50 · 25% 시각 패턴). 있으면 그 코루틴을 돌려줌</summary>
        protected virtual System.Func<IEnumerator> Forced() => null;
        /// <summary>패턴과 별개로 매 프레임 (상시 탄막 · 기믹 타이머)</summary>
        protected virtual void Always(PlayerController p, Vector3 dir, float dist) { }
        protected virtual void Move(PlayerController p, Vector3 dir, float dist, float spd)
        {
            if (dist > NearDist * 0.6f) transform.position += dir * spd * Time.deltaTime;
        }

        public void Tick(PlayerController p, Vector3 dir, float dist, float spd)
        {
            Always(p, dir, dist);
            if (!busy || moveWhileBusy) Move(p, dir, dist, spd);
            if (!busy) Face(dir);
            if (busy || Time.time < nextPatternAt) return;

            var forced = Forced();
            if (forced != null) { Begin(forced(), null); return; }
            var pick = PatternPicker.Pick(patterns, dist <= NearDist, lastPattern);
            if (pick != null) { if (self) self.Anim(pick.anim); Begin(pick.run(), pick.id); }
        }

        void Begin(IEnumerator routine, string id)
        {
            if (id != null) lastPattern = id;
            busy = true;
            moveWhileBusy = false;
            running = StartCoroutine(Wrap(routine));
        }

        IEnumerator Wrap(IEnumerator routine)
        {
            yield return routine;
            busy = false;
            nextPatternAt = Time.time + T(PatternGap);
        }

        /// <summary>기절 · 시간 정지 동안 기다림 (패턴 코루틴 안에서 yield return Hold())</summary>
        protected IEnumerator Wait(float seconds)
        {
            float end = Time.time + seconds;
            while (Time.time < end)
            {
                if (hp.Stunned) end += Time.deltaTime;
                yield return null;
            }
        }

        protected void Face(Vector3 dir, float speed = 6f)
        {
            dir.y = 0;
            if (dir.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), speed * Time.deltaTime);
        }

        protected Vector3 ToPlayer()
        {
            var d = P ? P.transform.position - transform.position : transform.forward;
            d.y = 0;
            return d.sqrMagnitude > 0.0001f ? d.normalized : transform.forward;
        }

        protected float Dist() => P ? Effects.Flat(P.transform.position - transform.position).magnitude : 99f;

        protected void Shoot(Vector3 dir, float speed, float dmg, float height = 1.4f, float range = 0, EnemyProjectile.Style style = EnemyProjectile.Style.Orb, int hour = 0)
        {
            dir.y = 0;
            EnemyProjectile.Spawn(transform.position + Vector3.up * height + dir.normalized * 1.4f, dir, speed, dmg, range, style, hour);
        }

        /// <summary>플레이어가 원 안이면 피해</summary>
        protected static bool HitCircle(Vector3 at, float radius, float dmg)
        {
            var p = PlayerController.Instance;
            if (!p || !p.health.Alive) return false;
            if (Effects.Flat(p.transform.position - at).magnitude > radius) return false;
            p.health.TakeDamage(dmg);
            return true;
        }

        // 패턴이 띄운 표시(고양이 머리 위 표식 · 바닥 숫자 등). 보스가 패턴 도중 죽으면 코루틴이 멈춰 지우는 줄까지 못 가므로 여기서 정리
        readonly List<GameObject> owned = new();

        /// <summary>BigLabel.Show와 같지만 보스가 사라질 때 같이 지움</summary>
        protected BigLabel Label(Transform parent, Vector3 offset, string text, Color c, float size = 1f, BigLabel.Frame frame = BigLabel.Frame.Plain)
        {
            var b = BigLabel.Show(parent, offset, text, c, size, frame);
            owned.RemoveAll(g => !g);
            if (b) owned.Add(b.gameObject);
            return b;
        }

        /// <summary>패턴 중 만든 물체를 보스가 사라질 때 같이 지우도록 등록</summary>
        protected GameObject Own(GameObject g) { owned.RemoveAll(x => !x); if (g) owned.Add(g); return g; }

        protected virtual void OnDisable()
        {
            if (running != null) StopCoroutine(running);
            foreach (var g in owned) if (g) Destroy(g);
            owned.Clear();
        }
    }
}
