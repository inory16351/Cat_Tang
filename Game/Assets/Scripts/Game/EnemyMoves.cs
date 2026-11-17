// 일반 적 행동 다양화 (2026-11-17 사용자 확정)
// - 공격 여러 개 + 가중 랜덤: 거리마다 가중치로 고르고, 직전 공격은 가중치를 1/4로 (같은 공격만 반복하지 않게)
// - 무법자: 단발 조준 사격(예고 0.4~0.6초 흔들림) · 3점사(예고 0.6초 → 0.13초 간격 3발, 한 발마다 살짝 따라감)
//          움직임 = 6~9m를 유지하며 옆걸음(방향 1.2~2.8초마다 바뀜) + 3~5초마다 다른 각도 자리로 재배치
//          강화형: 조준당하면 구르기 대신 옆으로 대시(잔상) 후 한 발 — 몬스터 대시는 고양이 닷지 액션을 발동시키지 않음
// - 깡패: 일직선 돌진(기존) · 내리찍기(가까우면 원형 예고 0.6초 → 반경 2.6m + 밀어냄), 움직임 = ±30° 비스듬히 돌아 들어오고 가끔 멈칫
// - 폭탄병: 도약 자폭(기존) · 모래 폭탄 투척(자기는 살아서 고양이 자리에 포물선 투척, 착탄 예고 1초 · 반경 1.8m · 피해 60%, 던진 뒤 옆으로 물러남)
using System.Collections;
using UnityEngine;

namespace CatTang
{
    public partial class Enemy
    {
        bool busy;                 // 코루틴 공격 중 (이동 · 다른 공격 멈춤)
        int lastAtk = -1;
        float strafeSign = 1, nextStrafeSwitch, nextReposition, repoUntil, flankAngle, nextFlank, pauseUntil;
        Vector3 repoTarget;
        public bool Dashing { get; private set; }   // 몬스터 대시 중 (닷지 액션 예외 판정용)
        static readonly Color DashSil = new Color(0.55f, 0.32f, 0.75f, 0.85f), DashSilHot = new Color(0.85f, 0.6f, 1f, 0.85f);

        /// <summary>(공격 번호, 가중치) 중 하나. 직전 공격은 가중치 1/4</summary>
        int Pick(params (int id, float w)[] opts)
        {
            float total = 0;
            foreach (var o in opts) total += o.id == lastAtk ? o.w * 0.25f : o.w;
            if (total <= 0) return -1;
            float r = Random.value * total;
            foreach (var o in opts)
            {
                float w = o.id == lastAtk ? o.w * 0.25f : o.w;
                if (w <= 0) continue;
                if ((r -= w) <= 0) { lastAtk = o.id; return o.id; }
            }
            lastAtk = opts[opts.Length - 1].id;
            return lastAtk;
        }

        /// <summary>기다리기 (기절하면 예고를 지우고 false)</summary>
        IEnumerator Hold(float t, System.Action<bool> ok)
        {
            float end = Time.time + t / AtkMul;
            while (Time.time < end)
            {
                if (!health.Alive || health.Stunned) { if (tele) tele.Kill(); ok(false); yield break; }
                yield return null;
            }
            ok(true);
        }

        Vector3 ToP(PlayerController p) { var d = Effects.Flat(p.transform.position - transform.position); return d.sqrMagnitude > 1e-4f ? d.normalized : transform.forward; }

        void Shoot(Vector3 d, float dmgMul = 1f)
        {
            EnemyProjectile.Spawn(transform.position + Vector3.up * 0.9f + d * 0.7f, d, OutlawBulletSpeed, projectileDamage * dmgMul, 0, EnemyProjectile.Style.Revolver);
            GunkataFx.MuzzleFlash(transform.position + Vector3.up * 0.9f + d * 0.8f, d, 0.6f);
        }

        // ---------- 무법자 ----------
        void OutlawThink(PlayerController p, Vector3 dir, float dist, float spd)
        {
            if (busy) return;
            // 움직임: 거리 유지 + 옆걸음, 가끔 다른 각도 자리로 재배치
            if (Time.time >= nextStrafeSwitch) { strafeSign = Random.value < 0.5f ? -1 : 1; nextStrafeSwitch = Time.time + Random.Range(1.2f, 2.8f); }
            Vector3 move;
            if (Time.time < repoUntil)
            {
                var to = Effects.Flat(repoTarget - transform.position);
                move = to.magnitude > 0.3f ? to.normalized * 1.35f : Vector3.zero;
            }
            else
            {
                float want = dist > 9 ? 1 : dist < 6 ? -1 : 0;
                var side = Vector3.Cross(Vector3.up, dir) * strafeSign;
                move = dir * want + side * 0.75f;
                if (Time.time >= nextReposition)
                {
                    nextReposition = Time.time + Random.Range(3f, 5f);
                    float ang = Random.Range(50f, 110f) * (Random.value < 0.5f ? -1 : 1);
                    repoTarget = p.transform.position - (Quaternion.Euler(0, ang, 0) * dir) * Random.Range(6.5f, 8.5f);
                    var rm = RunManager.Instance; if (rm) repoTarget = rm.ClampToRoom(repoTarget, 1.5f);
                    repoUntil = Time.time + 1.1f;
                }
            }
            if (move.sqrMagnitude > 1f) move.Normalize();
            transform.position += move * spd * Time.deltaTime;
            Face(dir);
            if (Time.time >= nextAttack && dist < 14f)
            {
                int a = Pick((0, dist > 9 ? 3f : 2f), (1, dist < 9 ? 2.5f : 1.5f));
                StartCoroutine(a == 0 ? OutlawSingle(p, dir, dist) : OutlawBurst(p, dir, dist));
            }
        }

        IEnumerator OutlawSingle(PlayerController p, Vector3 dir, float dist)
        {
            busy = true;
            float aim = OutlawAimTime * Random.Range(0.8f, 1.2f);
            Anim("Attack");
            tele = Telegraph.Lane(transform.position + dir * 0.6f, dir, Mathf.Min(dist + 2f, 12f), 0.14f, aim, Telegraph.Aim);
            bool ok = true;
            float end = Time.time + aim / AtkMul;
            while (Time.time < end) { if (!health.Alive || health.Stunned) { ok = false; if (tele) tele.Kill(); break; } Face(dir); yield return null; }
            if (ok) Shoot(dir);
            nextAttack = Time.time + shootInterval * Random.Range(0.8f, 1.25f) / AtkMul;
            busy = false;
        }

        IEnumerator OutlawBurst(PlayerController p, Vector3 dir, float dist)
        {
            busy = true;
            Anim("Attack");
            tele = Telegraph.Lane(transform.position + dir * 0.6f, dir, Mathf.Min(dist + 2f, 12f), 0.22f, 0.6f, Telegraph.Aim);
            bool ok = false;
            yield return Hold(0.6f, r => ok = r);
            var d = dir;
            for (int i = 0; ok && i < 3; i++)
            {
                if (!health.Alive || health.Stunned) break;
                d = Vector3.Slerp(d, ToP(p), 0.35f);                                // 한 발마다 살짝 따라감
                Shoot(Quaternion.Euler(0, Random.Range(-4f, 4f), 0) * d, 0.75f);
                Face(d);
                yield return new WaitForSeconds(0.13f);
            }
            yield return new WaitForSeconds(0.3f);
            nextAttack = Time.time + shootInterval * Random.Range(1f, 1.4f) / AtkMul;
            busy = false;
        }

        /// <summary>강화형 무법자: 조준당하면 옆으로 대시(잔상 · 먼지) 후 한 발. 피해가 없는 이동이라 고양이 닷지 액션과 무관</summary>
        IEnumerator OutlawDash(PlayerController p, Vector3 dir)
        {
            busy = true; Dashing = true;
            var side = Vector3.Cross(Vector3.up, dir) * (Random.value < 0.5f ? 1 : -1);
            var from = transform.position;
            var to = from + side * RollDist;
            var rm = RunManager.Instance; if (rm) to = rm.ClampToRoom(to, 1.2f);
            FxBurst.Spawn(from + Vector3.up * 0.15f, 1.1f, FxBurst.Kind.Dust, new Color(0.92f, 0.8f, 0.55f));
            // 고양이 대시처럼 실루엣: 출발 자리 진한 잔상 → 지나가며 옅은 잔상 → 도착 자리 밝은 잔상 (몬스터 색 = 보랏빛, 일반 레이어)
            if (model) Afterimage.Spawn(model, DashSil, 0.35f, false);
            float t = 0, ghost = 0;
            while (t < 1f)
            {
                t += Time.deltaTime / 0.22f;
                ghost += Time.deltaTime;
                transform.position = Vector3.Lerp(from, to, 1 - (1 - Mathf.Clamp01(t)) * (1 - Mathf.Clamp01(t)));
                if (model && ghost >= 0.035f) { ghost = 0; Afterimage.Spawn(model, DashSil * new Color(1, 1, 1, 0.8f), 0.35f, false); }
                yield return null;
            }
            if (model) Afterimage.Spawn(model, DashSilHot * new Color(1, 1, 1, 0.6f), 0.35f, false);
            Dashing = false;
            yield return new WaitForSeconds(0.08f);
            if (health.Alive && !health.Stunned) { var d = ToP(p); Face(d); Shoot(d); }
            nextAttack = Mathf.Max(nextAttack, Time.time + 0.8f);
            busy = false;
        }

        // ---------- 깡패 ----------
        void BruteThink(PlayerController p, Vector3 dir, float dist, float spd)
        {
            if (busy) return;
            // 움직임: ±30° 비스듬히 돌아 들어오고 가끔 멈칫
            if (Time.time >= nextFlank)
            {
                flankAngle = Random.Range(-30f, 30f);
                nextFlank = Time.time + Random.Range(1.5f, 3f);
                if (Random.value < 0.2f) pauseUntil = Time.time + Random.Range(0.25f, 0.45f);
            }
            if (Time.time >= pauseUntil && dist > 1.5f)
            {
                var md = dist > 3.5f ? Quaternion.Euler(0, flankAngle, 0) * dir : dir;
                transform.position += md * Mathf.Min(spd * Time.deltaTime, dist - 1.5f);
            }
            Face(dir);
            if (Time.time >= nextAttack && dist < BruteRange)
            {
                int a = Pick((0, dist < 3.4f ? 0.5f : 2.5f), (1, dist < 3.4f ? 3f : 0f));
                if (a == 1) StartCoroutine(BruteSlam(p));
                else
                {
                    act = Act.Windup;                           // 기존 일직선 돌진 (BruteUpdate의 Windup → Charge → Recover)
                    lockDir = dir;
                    actUntil = Time.time + BruteWindup / AtkMul;
                    Anim("Attack");
                    tele = Telegraph.Lane(transform.position, dir, BruteChargeDist + 0.8f, 1.6f, actUntil - Time.time, Telegraph.Danger);
                }
            }
        }

        public const float SlamRadius = 2.6f;

        IEnumerator BruteSlam(PlayerController p)
        {
            busy = true;
            Anim("Special");
            tele = Telegraph.Ring(transform.position, SlamRadius, 0.6f / AtkMul, Telegraph.Danger, transform);
            bool ok = false;
            yield return Hold(0.6f, r => ok = r);
            if (ok && p && p.health.Alive)
            {
                var d = Effects.Flat(p.transform.position - transform.position);
                if (d.magnitude <= SlamRadius) { p.health.TakeDamage(contactDamage); p.health.Knockback((d.sqrMagnitude > 1e-4f ? d.normalized : transform.forward) * 7f); }
                FxBurst.Spawn(transform.position + Vector3.up * 0.15f, SlamRadius * 1.6f, FxBurst.Kind.Dust, new Color(0.85f, 0.7f, 0.5f));
                GunkataFx.HitImpact(transform.position + Vector3.up * 0.3f, Vector3.down, new Color(1f, 0.6f, 0.3f), false, 1.4f);
                CameraFollow.Shake(0.18f);
            }
            yield return new WaitForSeconds(0.55f);
            nextAttack = Time.time + BruteCooldown * Random.Range(0.85f, 1.2f) / AtkMul;
            busy = false;
        }

        // ---------- 폭탄병 ----------
        public const float SandBombRadius = 1.8f, SandBombFlight = 1f;

        /// <summary>폭탄병 공격 고르기: 도약 자폭(기존) / 모래 폭탄 투척. 둘 다 안 되면 false</summary>
        bool BomberPick(PlayerController p, Vector3 dir, float dist)
        {
            if (Time.time < nextAttack || dist > 9f) return false;
            int a = Pick((0, dist < BomberLeapRange ? 2.2f : 0f), (1, dist > 3.5f ? 2f : 0.4f));
            if (a == 1) { StartCoroutine(BomberThrow(p)); return true; }
            if (a == 0) { StartLeap(dir, dist); return true; }
            return false;
        }

        void StartLeap(Vector3 dir, float dist)
        {
            var target = transform.position + dir * Mathf.Min(dist, BomberLeapRange);
            var rm = RunManager.Instance;
            leapTo = rm ? rm.ClampToRoom(target, 1.2f) : target;
            leapTo.y = transform.position.y;
            act = Act.Windup;
            actUntil = Time.time + BomberCrouch / AtkMul;
            Anim("Special");
            tele = Telegraph.Ring(leapTo, 0.6f, (actUntil - Time.time) + BomberAir, Telegraph.Aim).NoGlint();
        }

        IEnumerator BomberThrow(PlayerController p)
        {
            busy = true;
            var at = p.transform.position; at.y = transform.position.y;
            Anim("Attack");
            Telegraph.Ring(at, SandBombRadius, SandBombFlight, Telegraph.Danger);
            // 던지는 폭탄: 화약통 키트를 작게, 없으면 구
            GameObject bomb = null;
            var kit = Kit.Prefab(chapter, "Keg");
            if (kit) { bomb = Instantiate(kit); bomb.transform.localScale *= 0.35f; foreach (var c in bomb.GetComponentsInChildren<Collider>()) Destroy(c); }
            else { bomb = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(bomb.GetComponent<Collider>()); bomb.transform.localScale = Vector3.one * 0.35f; }
            var from = transform.position + Vector3.up * 1.2f;
            for (float t = 0; t < 1f; t += Time.deltaTime / SandBombFlight)
            {
                if (!bomb) break;
                bomb.transform.position = Vector3.Lerp(from, at, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 3f;
                bomb.transform.Rotate(new Vector3(360f, 0, 180f) * Time.deltaTime);
                // 던진 뒤 옆으로 물러남
                var away = Effects.Flat(transform.position - p.transform.position).normalized;
                transform.position += (away + Vector3.Cross(Vector3.up, away) * strafeSign) .normalized * speed * 0.6f * Time.deltaTime;
                yield return null;
            }
            if (bomb) Destroy(bomb);
            if (p && p.health.Alive && Effects.Flat(p.transform.position - at).magnitude <= SandBombRadius) p.health.TakeDamage(contactDamage * 0.6f);
            GunkataFx.Explosion(at, SandBombRadius, true);
            nextAttack = Time.time + Random.Range(2.2f, 3f) / AtkMul;
            strafeSign = -strafeSign;
            busy = false;
        }
    }
}
