// 적: 추격형(몸통 박치기) · 사격형(느린 탄) · 보스(챕터별 패턴)
// 보스 패턴: Spiral(12방향 + 바늘) · Ring(24방향 고리 엇갈림) · Mirror(거울 분신 소환) · Gear(쌍바늘 + 8방향) · Master(전부 섞음)
using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    [RequireComponent(typeof(Health))]
    public class Enemy : MonoBehaviour
    {
        public enum Kind { Chaser, Shooter, Boss }
        public static readonly List<Enemy> All = new();

        public Kind kind = Kind.Chaser;
        public float speed = 3.2f;
        public float contactDamage = 10;
        public float shootInterval = 1.8f;
        public float projectileDamage = 8;
        public string bossPattern = "Spiral";
        public ChapterDef chapter;

        [HideInInspector] public Health health;
        float nextAttack, spin, nextHandHit, nextSummon;
        int volley;
        Transform hand, hand2;
        bool dead;

        void Awake()
        {
            health = GetComponent<Health>();
            health.OnDeath += Die;
        }

        void Start()
        {
            hand = transform.Find("Hand");
            hand2 = transform.Find("Hand2");
            health.ResetHp(health.maxHp);
        }

        void OnEnable() { All.Add(this); nextAttack = Time.time + Random.Range(0.6f, 1.4f); }
        void OnDisable() { All.Remove(this); }

        void Update()
        {
            var p = PlayerController.Instance;
            if (!health.Alive || p == null || !p.health.Alive || health.Stunned) return;
            Vector3 to = p.transform.position - transform.position;
            to.y = 0;
            float dist = to.magnitude;
            Vector3 dir = dist > 0.01f ? to / dist : Vector3.forward;
            float spd = speed * health.SlowMul;

            switch (kind)
            {
                case Kind.Chaser:
                    if (dist > 1.0f) transform.position += dir * Mathf.Min(spd * Time.deltaTime, dist - 1.0f);
                    transform.rotation = Quaternion.LookRotation(dir);
                    if (dist < 1.25f && Time.time >= nextAttack) { p.health.TakeDamage(contactDamage); nextAttack = Time.time + 0.8f / Mathf.Max(0.2f, health.AttackSpeedMul); }
                    break;
                case Kind.Shooter:
                    float want = dist > 9 ? 1 : dist < 6 ? -1 : 0;
                    transform.position += dir * want * spd * Time.deltaTime;
                    transform.rotation = Quaternion.LookRotation(dir);
                    if (Time.time >= nextAttack)
                    {
                        EnemyProjectile.Spawn(transform.position + Vector3.up * 0.9f + dir * 0.7f, dir, 7f, projectileDamage);
                        nextAttack = Time.time + shootInterval / Mathf.Max(0.2f, health.AttackSpeedMul);
                    }
                    break;
                case Kind.Boss:
                    BossUpdate(p, dir, dist, spd);
                    break;
            }
        }

        // 넉백 · 이동 뒤에 방 밖으로 나가지 않게 붙잡음 (보스는 몸이 커서 벽에서 더 띄움)
        void LateUpdate()
        {
            var rm = RunManager.Instance;
            if (!rm || !health.Alive) return;
            var p = rm.ClampToRoom(transform.position, kind == Kind.Boss ? 3f : 1.2f);
            if ((p - transform.position).sqrMagnitude > 0.000001f) transform.position = p;
        }

        void Fan(int count, float offset, float spd)
        {
            for (int i = 0; i < count; i++)
            {
                var d = Quaternion.Euler(0, i * 360f / count + offset, 0) * Vector3.forward;
                EnemyProjectile.Spawn(transform.position + Vector3.up * 1.2f + d * 1.6f, d, spd, projectileDamage);
            }
        }

        void BossUpdate(PlayerController p, Vector3 dir, float dist, float spd)
        {
            bool phase2 = health.hp < health.maxHp * 0.5f;
            if (dist > 2.6f) transform.position += dir * spd * 0.5f * Time.deltaTime;
            spin += (phase2 ? 150f : 90f) * Time.deltaTime;
            if (hand) hand.localRotation = Quaternion.Euler(0, spin, 0);
            if (hand2) hand2.localRotation = Quaternion.Euler(0, -spin * 1.3f + 180, 0);
            foreach (var h in new[] { hand, hand2 })
            {
                if (!h || dist > 4.4f) continue;
                var hd = h.forward; hd.y = 0;
                var td = p.transform.position - transform.position; td.y = 0;
                if (Vector3.Angle(hd, td) < 14f && Time.time >= nextHandHit) { p.health.TakeDamage(contactDamage); nextHandHit = Time.time + 0.6f; }
            }

            string pat = bossPattern;
            if (pat == "Master") pat = new[] { "Spiral", "Ring", "Gear", "Mirror" }[(volley / 3) % 4];
            if (Time.time < nextAttack) return;
            volley++;
            float interval = shootInterval * (phase2 ? 0.7f : 1f);
            switch (pat)
            {
                case "Ring":
                    Fan(24, (volley % 2) * 7.5f, 5f);
                    if (phase2) Fan(12, 15, 7f);
                    break;
                case "Mirror":
                    Fan(12, spin * 0.2f, 5.5f);
                    if (Time.time >= nextSummon && chapter != null)
                    {
                        for (int i = -1; i <= 1; i += 2)
                            RunManager.Instance?.AddEnemy(EnemyFactory.Shooter(transform.position + transform.right * i * 3f, chapter));
                        nextSummon = Time.time + 9f;
                    }
                    break;
                case "Gear":
                    Fan(8, spin * 0.5f, 6f);
                    Fan(8, spin * 0.5f + 22.5f, 4.5f);
                    break;
                default:
                    Fan(12, spin * 0.2f, 5.5f);
                    if (phase2) Fan(12, spin * 0.2f + 15, 6.5f);
                    break;
            }
            nextAttack = Time.time + interval / Mathf.Max(0.2f, health.AttackSpeedMul);
        }

        bool watching;
        /// <summary>시간에 가둠이 풀릴 때 플레이어 효과(해제 폭발)를 부르도록 등록</summary>
        public void WatchTimeLock(PlayerController p)
        {
            if (watching) return;
            watching = true;
            health.OnTimeLockEnd += () => { if (p) p.TimeLockEnded(transform.position); };
        }

        void Die(Health h)
        {
            if (dead) return;
            dead = true;
            if (Mathf.Approximately(health.lastDamageWasDelayed, Time.time)) PlayerController.Instance?.DelayedKill(transform.position);
            FxBurst.Spawn(transform.position + Vector3.up, kind == Kind.Boss ? 6f : 2.2f, FxBurst.Kind.Explosion);
            RunManager.Instance?.OnEnemyKilled(this);
            Destroy(gameObject);
        }
    }
}
