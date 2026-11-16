// 적 (1장 기획서 기준 '시간의 하수인' 3종) · 보스(챕터별 패턴)
// - 태엽 무법자(Outlaw): 6~9m 거리를 두고, 2초마다 조준선 0.5초 예고 후 리볼버 1발
// - 시계추 깡패(Brute): 따라오다 7m 안이면 멈춰 0.6초 돌진 경로 예고 → 일직선 돌진, 부딪히면 피해
// - 모래시계 폭탄병(Bomber): 빠르게 달려와 붙으면 0.8초 점멸(반경 2m 예고) 후 자폭
// 보스 패턴: Spiral(12방향 + 바늘) · Ring(24방향 고리 엇갈림) · Mirror(거울 분신 소환) · Gear(쌍바늘 + 8방향) · Master(전부 섞음)
using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    [RequireComponent(typeof(Health))]
    public class Enemy : MonoBehaviour
    {
        public enum Kind { Outlaw, Brute, Bomber, Boss }
        public static readonly List<Enemy> All = new();

        public Kind kind = Kind.Outlaw;
        public float speed = 3.2f;
        public float contactDamage = 10;
        public float shootInterval = 1.8f;
        public float projectileDamage = 8;
        public string bossPattern = "Spiral";
        public ChapterDef chapter;
        public float chapterDmg = 1f;                 // 챕터 피해 배율 (보스 패턴 피해에 곱함)
        [HideInInspector] public BossBrain brain;     // 있으면 보스 패턴을 이 두뇌가 맡음 (1장 보스 · 중간 보스)

        // 1장 기획서 수치 (피해 · 체력은 EnemyFactory에서 챕터 배율과 함께 넣음)
        public const float OutlawAimTime = 0.5f, OutlawBulletSpeed = 9f;
        public const float BruteWindup = 0.6f, BruteChargeSpeed = 13f, BruteChargeDist = 8f, BruteRange = 7f, BruteCooldown = 2.2f, BruteRecover = 0.7f;
        public const float BomberFuse = 0.8f, BomberRadius = 2f, BomberTrigger = 1.3f;

        [HideInInspector] public Health health;
        float nextAttack, spin, nextHandHit, nextSummon;
        int volley;
        Transform hand, hand2, model;
        bool dead;

        // 일반 적 공격 상태
        enum Act { Move, Windup, Charge, Recover, Fuse }
        Act act = Act.Move;
        float actUntil, charged;
        Vector3 lockDir;
        bool chargeHit;
        Telegraph tele;
        float bobT;

        void Awake()
        {
            health = GetComponent<Health>();
            health.OnDeath += Die;
        }

        void Start()
        {
            hand = transform.Find("Hand");
            hand2 = transform.Find("Hand2");
            model = transform.Find("Model");
            health.ResetHp(health.maxHp);
        }

        void OnEnable() { All.Add(this); nextAttack = Time.time + Random.Range(0.6f, 1.4f); }
        void OnDisable() { All.Remove(this); if (tele) tele.Kill(); }

        float AtkMul => Mathf.Max(0.2f, health.AttackSpeedMul);

        void Update()
        {
            var p = PlayerController.Instance;
            if (!health.Alive || p == null || !p.health.Alive) return;
            if (health.Stunned) { CancelAct(); return; }
            Vector3 to = p.transform.position - transform.position;
            to.y = 0;
            float dist = to.magnitude;
            Vector3 dir = dist > 0.01f ? to / dist : transform.forward;
            float spd = speed * health.SlowMul;
            Vector3 before = transform.position;

            switch (kind)
            {
                case Kind.Outlaw: OutlawUpdate(p, dir, dist, spd); break;
                case Kind.Brute: BruteUpdate(p, dir, dist, spd); break;
                case Kind.Bomber: BomberUpdate(p, dir, dist, spd); break;
                case Kind.Boss:
                    if (!brain) brain = GetComponent<BossBrain>();
                    if (brain) brain.Tick(p, dir, dist, spd); else BossUpdate(p, dir, dist, spd);
                    break;
            }
            Animate((transform.position - before).magnitude / Mathf.Max(0.0001f, Time.deltaTime));
        }

        void Face(Vector3 dir) { if (dir.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 12f * Time.deltaTime); }

        void CancelAct()
        {
            if (tele) tele.Kill();
            if (act == Act.Fuse) return;                      // 폭탄병 점멸은 기절해도 이어짐 (불붙은 심지)
            act = Act.Move;
        }

        // ---------- 태엽 무법자 ----------
        void OutlawUpdate(PlayerController p, Vector3 dir, float dist, float spd)
        {
            if (act == Act.Windup)
            {
                Face(lockDir);
                if (Time.time < actUntil) return;
                EnemyProjectile.Spawn(transform.position + Vector3.up * 0.9f + lockDir * 0.7f, lockDir, OutlawBulletSpeed, projectileDamage);
                GunkataFx.MuzzleFlash(transform.position + Vector3.up * 0.9f + lockDir * 0.8f, lockDir, 0.6f);
                act = Act.Move;
                nextAttack = Time.time + shootInterval / AtkMul;
                return;
            }
            float want = dist > 9 ? 1 : dist < 6 ? -1 : 0;
            transform.position += dir * want * spd * Time.deltaTime;
            Face(dir);
            if (Time.time >= nextAttack && dist < 14f)
            {
                act = Act.Windup;
                lockDir = dir;
                actUntil = Time.time + OutlawAimTime;
                tele = Telegraph.Lane(transform.position + dir * 0.6f, dir, Mathf.Min(dist + 2f, 12f), 0.14f, OutlawAimTime, Telegraph.Aim);
            }
        }

        // ---------- 시계추 깡패 ----------
        void BruteUpdate(PlayerController p, Vector3 dir, float dist, float spd)
        {
            switch (act)
            {
                case Act.Windup:
                    Face(lockDir);
                    if (Time.time >= actUntil) { act = Act.Charge; charged = 0; chargeHit = false; }
                    return;
                case Act.Charge:
                {
                    float step = BruteChargeSpeed * health.SlowMul * Time.deltaTime;
                    var before = transform.position;
                    transform.position += lockDir * step;
                    var rm = RunManager.Instance;
                    if (rm) transform.position = rm.ClampToRoom(transform.position, 1.2f);
                    float moved = (transform.position - before).magnitude;
                    charged += step;
                    if (!chargeHit && Effects.Flat(p.transform.position - transform.position).magnitude < 1.3f)
                    {
                        chargeHit = true;
                        p.health.TakeDamage(contactDamage);
                        p.health.Knockback(lockDir * 6f);
                    }
                    if (charged >= BruteChargeDist || moved < step * 0.3f)   // 다 달렸거나 벽에 막힘
                    {
                        act = Act.Recover;
                        actUntil = Time.time + BruteRecover;
                    }
                    return;
                }
                case Act.Recover:
                    if (Time.time >= actUntil) { act = Act.Move; nextAttack = Time.time + BruteCooldown / AtkMul; }
                    return;
            }
            if (dist > 1.2f) transform.position += dir * Mathf.Min(spd * Time.deltaTime, dist - 1.2f);
            Face(dir);
            if (Time.time >= nextAttack && dist < BruteRange)
            {
                act = Act.Windup;
                lockDir = dir;
                actUntil = Time.time + BruteWindup / AtkMul;
                tele = Telegraph.Lane(transform.position, dir, BruteChargeDist + 0.8f, 1.6f, actUntil - Time.time, Telegraph.Danger);
            }
        }

        // ---------- 모래시계 폭탄병 ----------
        void BomberUpdate(PlayerController p, Vector3 dir, float dist, float spd)
        {
            if (act == Act.Fuse)
            {
                float left = actUntil - Time.time;
                // 점멸: 점점 빨라지는 깜빡임 (몸을 부풀렸다 줄임)
                float blink = Mathf.Sin((BomberFuse - left) * Mathf.Lerp(18f, 60f, 1 - left / BomberFuse));
                if (model) model.localScale = modelScale * (1f + 0.12f * Mathf.Max(0, blink));
                if (left > 0) return;
                Explode(p);
                return;
            }
            if (dist > 0.9f) transform.position += dir * Mathf.Min(spd * Time.deltaTime, dist - 0.9f);
            Face(dir);
            if (dist < BomberTrigger)
            {
                act = Act.Fuse;
                actUntil = Time.time + BomberFuse;
                tele = Telegraph.Ring(transform.position, BomberRadius, BomberFuse, Telegraph.Danger, transform);
            }
        }

        void Explode(PlayerController p)
        {
            if (dead) return;
            if (p && p.health.Alive && Effects.Flat(p.transform.position - transform.position).magnitude <= BomberRadius)
                p.health.TakeDamage(contactDamage);
            selfDestruct = true;
            health.hp = 0;
            Die(health);
        }

        bool selfDestruct;
        Vector3 modelScale = Vector3.one;
        bool modelScaleSet;

        // 리깅 없는 모델용 걷기 흔들림: 움직일 때 통통 튀고 좌우로 기우뚱
        void Animate(float moveSpeed)
        {
            if (!model) return;
            if (!modelScaleSet) { modelScale = model.localScale; modelScaleSet = true; }
            float k = Mathf.Clamp01(moveSpeed / Mathf.Max(0.5f, speed));
            bobT += Time.deltaTime * (kind == Kind.Bomber ? 16f : kind == Kind.Brute ? 8f : 10f) * Mathf.Max(0.25f, k);
            float lean = act == Act.Charge ? 18f : act == Act.Windup && kind == Kind.Brute ? -10f : 0f;
            model.localPosition = new Vector3(0, Mathf.Abs(Mathf.Sin(bobT)) * 0.12f * k, 0);
            model.localRotation = Quaternion.Euler(lean, 0, Mathf.Sin(bobT) * 6f * k) * modelBaseRot;
            if (act != Act.Fuse) model.localScale = modelScale;
        }

        Quaternion modelBaseRot = Quaternion.identity;
        public void SetModel(Transform m) { model = m; modelBaseRot = m.localRotation; modelScale = m.localScale; modelScaleSet = true; }

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
                            RunManager.Instance?.AddEnemy(EnemyFactory.Outlaw(transform.position + transform.right * i * 3f, chapter));
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
            if (tele) tele.Kill();
            if (kind == Kind.Bomber && selfDestruct) FxBurst.Spawn(transform.position + Vector3.up * 0.5f, BomberRadius * 1.4f, FxBurst.Kind.Explosion);
            if (Mathf.Approximately(health.lastDamageWasDelayed, Time.time)) PlayerController.Instance?.DelayedKill(transform.position);
            FxBurst.Spawn(transform.position + Vector3.up, kind == Kind.Boss ? 6f : 2.2f, FxBurst.Kind.Explosion);
            RunManager.Instance?.OnEnemyKilled(this);
            Destroy(gameObject);
        }
    }
}
