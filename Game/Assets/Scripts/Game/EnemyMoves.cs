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
            KeepRange(p, dir, dist, spd, 6f, 9f);
            if (Time.time >= nextAttack && dist < 14f)
            {
                int a = Pick((0, dist > 9 ? 3f : 2f), (1, dist < 9 ? 2.5f : 1.5f));
                StartCoroutine(a == 0 ? OutlawSingle(p, dir, dist) : OutlawBurst(p, dir, dist));
            }
        }

        /// <summary>원거리 적 움직임: near~far 거리를 유지하며 옆걸음, 3~5초마다 다른 각도 자리로 재배치 (무법자 · 2장 차장 · 신호수)</summary>
        void KeepRange(PlayerController p, Vector3 dir, float dist, float spd, float near, float far)
        {
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
                float want = dist > far ? 1 : dist < near ? -1 : 0;
                var side = Vector3.Cross(Vector3.up, dir) * strafeSign;
                move = dir * want + side * 0.75f;
                if (Time.time >= nextReposition)
                {
                    nextReposition = Time.time + Random.Range(3f, 5f);
                    float ang = Random.Range(50f, 110f) * (Random.value < 0.5f ? -1 : 1);
                    repoTarget = p.transform.position - (Quaternion.Euler(0, ang, 0) * dir) * Random.Range(near + 0.5f, far - 0.5f);
                    var rm = RunManager.Instance; if (rm) repoTarget = rm.ClampToRoom(repoTarget, 1.5f);
                    repoUntil = Time.time + 1.1f;
                }
            }
            if (move.sqrMagnitude > 1f) move.Normalize();
            transform.position += move * spd * Time.deltaTime;
            // 걷는 방향을 바라봄 (옆걸음을 앞걸음 모션으로 미끄러지지 않게, 2026-11-17 사용자). 공격 예고 때만 고양이를 봄
            Face(move.sqrMagnitude > 0.04f ? move.normalized : dir);
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
            FxBurst.Spawn(from + Vector3.up * 0.15f, 1.1f, FxBurst.Kind.Dust, FxBurst.MistDust);
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
            var md = dist > 3.5f ? Quaternion.Euler(0, flankAngle, 0) * dir : dir;
            if (Time.time >= pauseUntil && dist > 1.5f)
                transform.position += md * Mathf.Min(spd * Time.deltaTime, dist - 1.5f);
            Face(Time.time >= pauseUntil ? md : dir);                 // 비스듬히 돌 때도 걷는 방향을 봄
            if (Time.time >= nextAttack && dist < BruteRange)
            {
                int a = Pick((0, dist < 3.4f ? 0.5f : 2.5f), (1, dist < 3.4f ? 3f : 0f));
                if (a == 1) StartCoroutine(BruteSlam(p));
                else
                {
                    act = Act.Windup;                           // 기존 일직선 돌진 (BruteUpdate의 Windup → Charge → Recover)
                    lockDir = dir;
                    actUntil = Time.time + BruteWindup / AtkMul;
                    Anim("Attack");                             // 발 구르기 클립 (Ch1_BruteStomp: 몸 숙이고 한 발로 땅 두 번 긁기)
                    StartCoroutine(StompDust(actUntil));
                    tele = Telegraph.Lane(transform.position, dir, BruteChargeDist + 0.8f, 1.6f, actUntil - Time.time, Telegraph.Danger);
                }
            }
        }

        /// <summary>돌진 예고 동안 발 구르기: 발 구를 때마다 발밑 흙먼지 + 작은 흔들림 (발탄 돌진 전 발 구르기 느낌)</summary>
        IEnumerator StompDust(float until)
        {
            float[] beats = { 0.2f, 0.4f, 0.55f };
            float start = Time.time, len = Mathf.Max(0.1f, until - start);
            foreach (var b in beats)
            {
                float at = start + len * b / 0.6f;
                while (Time.time < at) { if (act != Act.Windup) yield break; yield return null; }
                var side = Vector3.Cross(Vector3.up, lockDir).normalized;
                FxBurst.Spawn(transform.position + side * 0.35f - lockDir * 0.3f + Vector3.up * 0.1f, 1.3f, FxBurst.Kind.Dust, FxBurst.MistDust);
                CameraFollow.Shake(0.04f);
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
                FxBurst.Spawn(transform.position + Vector3.up * 0.15f, SlamRadius * 1.6f, FxBurst.Kind.Dust, FxBurst.MistDust);
                PropHit.Area(transform.position, SlamRadius, contactDamage);
                GunkataFx.HitImpact(transform.position + Vector3.up * 0.3f, Vector3.down, new Color(1f, 0.6f, 0.3f), false, 1.4f);
                CameraFollow.Shake(0.18f);
            }
            yield return new WaitForSeconds(0.55f);
            nextAttack = Time.time + BruteCooldown * Random.Range(0.85f, 1.2f) / AtkMul;
            busy = false;
        }

        // ---------- 폭탄병 ----------
        public const float SandBombRadius = 1.8f, SandBombFlight = 0.8f, ThrowRelease = 0.34f;

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
            // 던지기 동작 (Ch1_BomberThrow): 팔을 젖혔다가 0.34초에 손을 떠남 → 그때까지 제자리에서 고양이를 바라봄
            // (2026-11-17 사용자: 모션 없이 폭발하는 것 같음 — 예전엔 자폭 떨기 클립으로 바로 던졌음)
            Anim("Throw");
            float release = ThrowRelease / AtkMul;
            Telegraph.Ring(at, SandBombRadius, release + SandBombFlight, Telegraph.Danger);
            for (float w = 0; w < release; w += Time.deltaTime)
            {
                if (!p) break;
                Face(Effects.Flat(at - transform.position));
                yield return null;
            }
            // 던지는 폭탄: 블렌더 모래 폭탄(Kit/Ch1/SandBomb — 모래주머니 + 모래시계 뚜껑 + 불꽃 심지), 없으면 화약통을 작게, 그것도 없으면 구
            GameObject bomb = null;
            var sb = Kit.Prefab(chapter, "SandBomb");
            var kit = sb ? sb : Kit.Prefab(chapter, "Keg");
            if (kit) { bomb = Instantiate(kit); if (!sb) bomb.transform.localScale *= 0.35f; else bomb.transform.localScale *= 1.25f; foreach (var c in bomb.GetComponentsInChildren<Collider>()) Destroy(c); }
            else { bomb = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(bomb.GetComponent<Collider>()); bomb.transform.localScale = Vector3.one * 0.35f; bomb.GetComponent<Renderer>().sharedMaterial = RoomBuilder.Mat(new Color(0.85f, 0.6f, 0.3f), 2f); }   // 최후 대비: 회색 기본 재질 말고 모래색
            var from = transform.position + Vector3.up * 1.4f + transform.forward * 0.3f;   // 손이 앞으로 휘둘러진 자리
            // 날아가 터지는 건 폭탄이 스스로 함 (2026-11-18 사용자: 던진 도중 폭탄병이 죽으면 폭탄이 공중에 멈춰 남던 문제 → 끝까지 날아가 터짐)
            ThrownBomb.Launch(bomb, from, at, SandBombFlight, SandBombRadius, contactDamage * 0.6f);
            for (float t = 0; t < 1f; t += Time.deltaTime / SandBombFlight)
            {
                // 던진 뒤 옆으로 물러남
                if (p) { var away = Effects.Flat(transform.position - p.transform.position).normalized; transform.position += (away + Vector3.Cross(Vector3.up, away) * strafeSign).normalized * speed * 0.6f * Time.deltaTime; }
                yield return null;
            }
            nextAttack = Time.time + Random.Range(2.2f, 3f) / AtkMul;
            strafeSign = -strafeSign;
            busy = false;
        }
    }

    /// <summary>던진 모래 폭탄: 포물선으로 날아가 착지 자리에서 터짐. 던진 적과 따로 움직여 그 적이 죽어도 끝까지 감</summary>
    public class ThrownBomb : MonoBehaviour
    {
        Vector3 from, to; float flight, radius, damage, t;

        public static void Launch(GameObject bomb, Vector3 from, Vector3 to, float flight, float radius, float damage)
        {
            if (!bomb) return;
            var b = bomb.AddComponent<ThrownBomb>();
            b.from = from; b.to = to; b.flight = Mathf.Max(0.05f, flight); b.radius = radius; b.damage = damage;
            bomb.transform.position = from;
        }

        void Update()
        {
            t += Time.deltaTime / flight;
            if (t < 1f)
            {
                transform.position = Vector3.Lerp(from, to, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 3f;
                transform.Rotate(new Vector3(360f, 0, 180f) * Time.deltaTime);
                return;
            }
            var p = PlayerController.Instance;
            if (p && p.health.Alive && Effects.Flat(p.transform.position - to).magnitude <= radius) p.health.TakeDamage(damage);
            GunkataFx.Explosion(to, radius, true);
            PropHit.Area(to, radius, damage);
            Destroy(gameObject);
        }
    }
}
