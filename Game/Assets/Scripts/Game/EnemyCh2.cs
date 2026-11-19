// 2장 '멈춘 기차역' 일반 적 3종 (레벨 디자인 md 2장 수치 그대로, 2026-11-17)
// 기본 공격은 기획서 값 그대로, 1장처럼 단조롭지 않게 공격 여러 개 + 가중 랜덤(직전 공격 ×0.25, EnemyMoves.Pick) + 움직임 다듬기 (2026-11-17 사용자: 세피리아처럼)
//
// - 태엽 차장(Conductor): 7~10m 유지 · 옆걸음 · 재배치. 고양이가 4.5m 안으로 붙으면 뒤로 깡충 물러나며 바로 흩뿌리기
//   ① 승차권 3갈래(부채 30°, 예고 0.6초, 탄속 9, 피해 10)  ② 연속 검표: 따라오는 조준선 0.5초 → 움직임을 앞질러 3연발
//   ③ 승차권 흩뿌리기(가까울 때): 5장 60° 느린 탄(7.5), 피해 80%
//   강화형 검표 펀치: 6초 쿨, 예고 0.6초 → 엄폐물을 뚫는 큰 승차권, 맞으면 2초간 대시 충전 멈춤
// - 트렁크 짐꾼(Porter): 트렁크를 든 채 무겁게 걸어옴(정면 120° 탄은 피해 33%만). 몸 돌리는 속도가 느리고, 몸 방향과 걷는 방향이 크게 어긋나면
//   제자리에서 돌아섬(뒷걸음 미끄러짐 X). 등 뒤를 잡히면 가끔 발을 굴러 빠르게 돌아섬
//   ① 내리찍기(가까움): 예고 0.7초 부채꼴 90° · 2.5m · 피해 15 → 1.2초 빈틈(숙인 채, 방패 없음)
//   ② 방패 돌진(중간 거리): 예고 0.5초 직선 → 트렁크를 앞세우고 5m 돌진, 피해 12 + 크게 밀림, 끝나고 0.5초 빈틈
//   강화형 트렁크 던지기: 8초 쿨, 예고 0.8초 → 직선 10m, 피해 15. 손에서 트렁크가 사라지고 3초 뒤 새 트렁크를 꺼냄(그동안 방패 없음)
// - 신호수(Signal): 8m 유지. 고양이가 3.8m 안으로 붙으면 발밑에 신호등을 떨구고 도망
//   ① 신호등 2개(고양이 자리 + 둘레, 노랑 1.5초 → 빨강 폭발 반경 2m, 피해 12)
//   ② 건널목 차단: 고양이 앞을 가로지르는 줄로 4개(반경 1.3) → 방 일부를 잠깐 막음
//   ③ 따라오는 신호: 0.45초 간격으로 고양이를 쫓아 3개(반경 1.6)
//   강화형 정지 구역: ①과 발밑 신호등이 터진 자리에 반경 2m · 3초 정지 구역(적 탄이 멈췄다가 다시 날아감)
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    public partial class Enemy
    {
        // ---------- 수치 (레벨 디자인 md '수치 기준 (챕터 2)' + 다양화 추가분) ----------
        public const float ConductorNear = 7f, ConductorFar = 10f, TicketInterval = 2.5f, TicketAim = 0.6f, TicketSpeed = 9f, TicketFan = 30f;
        public const float PunchCooldown = 6f, PunchAim = 0.6f, PunchSpeed = 15f, PunchDashLock = 2f;
        public const float ConductorHopRange = 4.5f, ConductorHopDist = 2.8f, ConductorHopCooldown = 3.5f;
        public const float PorterShieldHalf = 60f, PorterTurn = 150f, SlamWindup = 0.7f, SlamHalf = 45f, SlamRange = 2.5f, SlamOpen = 1.2f, PorterCooldown = 2.2f;
        public const float BashWindup = 0.5f, BashDist = 5f, BashSpeed = 12f, BashOpen = 0.5f;
        public const float TrunkCooldown = 8f, TrunkAim = 0.8f, TrunkRange = 10f, TrunkSpeed = 16f, TrunkOpen = 3f;
        public const float SignalKeep = 8f, SignalInterval = 4f, SignalFuse = 1.5f, SignalRadius = 2f, StopZoneLife = 3f, SignalFleeRange = 3.8f;

        float nextPunch, nextTrunk, shieldDownUntil, porterOpenUntil, nextHop, nextShuffle, shuffleUntil, nextFlee;

        /// <summary>짐꾼이 트렁크를 내린 빈틈</summary>
        bool PorterOpen => kind == Kind.Porter && Time.time < porterOpenUntil;
        public bool ShieldUp => kind == Kind.Porter && Time.time >= shieldDownUntil && !trunkHidden;

        /// <summary>Health.BlockFrom: 탄 진행 방향 d가 짐꾼 정면 ±60° 안에서 날아오면 방패에 걸림 (피해 33%만, PlayerController.ShieldDamageFrac)</summary>
        public bool PorterBlocks(Vector3 d)
        {
            if (!ShieldUp || !health.Alive) return false;
            var from = -Effects.Flat(d);
            return from.sqrMagnitude > 1e-4f && Vector3.Angle(Effects.Flat(transform.forward), from) <= PorterShieldHalf;
        }

        /// <summary>고양이가 지금 움직이는 방향 · 속도를 보고 t초 뒤 자리 (앞질러 쏘기)</summary>
        static Vector3 Lead(PlayerController p, float t)
        {
            var cc = p.GetComponent<CharacterController>();
            var v = cc ? Effects.Flat(cc.velocity) : Vector3.zero;
            return p.transform.position + v * t;
        }

        // ================= 태엽 차장 =================
        void ConductorUpdate(PlayerController p, Vector3 dir, float dist, float spd)
        {
            if (busy) return;
            if (elite && Time.time >= nextPunch && dist < 13f && Time.time >= nextAttack - 1.2f)
            {
                StartCoroutine(ConductorPunch(p));
                return;
            }
            // 바짝 붙으면 뒤로 깡충 물러나 흩뿌리기 (원거리 적이 그냥 맞고 있지 않게)
            if (dist < ConductorHopRange && Time.time >= nextHop)
            {
                StartCoroutine(ConductorHop(p, dir));
                return;
            }
            KeepRange(p, dir, dist, spd, ConductorNear, ConductorFar);
            if (Time.time >= nextAttack && dist < 15f)
            {
                int a = Pick((0, dist >= 6f ? 3f : 1.5f), (1, dist >= 6f ? 2.2f : 1.2f), (2, dist < 6f ? 3f : 0.3f));
                StartCoroutine(a == 0 ? ConductorTickets(p, dir, dist) : a == 1 ? ConductorChain(p, dist) : ConductorScatter(p, dir, 0.45f));
            }
        }

        void Ticket(Vector3 d, float speed, float dmgMul = 1f)
        {
            EnemyProjectile.Spawn(transform.position + Vector3.up * 0.9f + d * 0.7f, d, speed, projectileDamage * dmgMul, 0, EnemyProjectile.Style.Ticket);
        }

        IEnumerator ConductorTickets(PlayerController p, Vector3 dir, float dist)
        {
            busy = true;
            Anim("Attack");
            // 세 갈래 조준선 (가운데 + 양쪽 15°) → 0.6초 뒤 동시에 발사
            var lanes = new List<Telegraph>();
            float len = Mathf.Min(dist + 2f, 13f);
            for (int i = -1; i <= 1; i++)
            {
                var d = Quaternion.Euler(0, i * TicketFan / 2, 0) * dir;
                var t = Telegraph.Lane(transform.position + d * 0.6f, d, len, 0.14f, TicketAim / AtkMul, Telegraph.Aim);
                if (i != 0) t.NoGlint();                     // 반짝임은 가운데 하나만
                lanes.Add(t);
            }
            bool ok = true;
            float end = Time.time + TicketAim / AtkMul;
            while (Time.time < end)
            {
                if (!health.Alive || health.Stunned) { ok = false; break; }
                Face(dir);
                yield return null;
            }
            if (!ok) foreach (var t in lanes) if (t) t.Kill();
            if (ok)
            {
                for (int i = -1; i <= 1; i++) Ticket(Quaternion.Euler(0, i * TicketFan / 2, 0) * dir, TicketSpeed);
                GunkataFx.MuzzleFlash(transform.position + Vector3.up * 0.9f + dir * 0.8f, dir, 0.55f);
                yield return new WaitForSeconds(0.25f);
            }
            nextAttack = Time.time + TicketInterval * Random.Range(0.9f, 1.15f) / AtkMul;
            busy = false;
        }

        /// <summary>연속 검표: 고양이를 따라가는 조준선 0.5초 → 0.18초 간격 3연발, 한 발마다 고양이 움직임을 앞질러 겨눔</summary>
        IEnumerator ConductorChain(PlayerController p, float dist)
        {
            busy = true;
            Anim("Attack");
            var dir = ToP(p);
            tele = Telegraph.Lane(transform.position + dir * 0.6f, dir, Mathf.Min(dist + 2f, 13f), 0.18f, 0.5f / AtkMul, Telegraph.Aim);
            float end = Time.time + 0.5f / AtkMul;
            bool ok = true;
            while (Time.time < end)
            {
                if (!health.Alive || health.Stunned) { ok = false; if (tele) tele.Kill(); break; }
                dir = Vector3.Slerp(dir, ToP(p), 8f * Time.deltaTime).normalized;
                if (tele) { tele.SetDir(dir); tele.transform.position = transform.position + dir * 0.6f; }
                Face(dir);
                yield return null;
            }
            for (int i = 0; ok && i < 3; i++)
            {
                if (!health.Alive || health.Stunned) break;
                var to = Effects.Flat(Lead(p, Effects.Flat(p.transform.position - transform.position).magnitude / TicketSpeed * 0.7f) - transform.position);
                var d = to.sqrMagnitude > 1e-4f ? to.normalized : dir;
                Ticket(Quaternion.Euler(0, Random.Range(-3f, 3f), 0) * d, TicketSpeed + 1f, 0.8f);
                GunkataFx.MuzzleFlash(transform.position + Vector3.up * 0.9f + d * 0.8f, d, 0.45f);
                Face(d);
                yield return new WaitForSeconds(0.18f);
            }
            yield return new WaitForSeconds(0.25f);
            nextAttack = Time.time + TicketInterval * Random.Range(1f, 1.3f) / AtkMul;
            busy = false;
        }

        /// <summary>승차권 흩뿌리기: 5장 60° 느린 탄 (가까울 때 · 깡충 뒤)</summary>
        IEnumerator ConductorScatter(PlayerController p, Vector3 dir, float aim)
        {
            busy = true;
            Anim("Attack");
            dir = ToP(p);
            tele = Telegraph.Sector(transform.position, dir, 4.5f, 30f, aim / AtkMul, Telegraph.Aim);
            bool ok = false;
            yield return Hold(aim, r => ok = r);
            if (ok)
            {
                dir = ToP(p);
                for (int i = -2; i <= 2; i++) Ticket(Quaternion.Euler(0, i * 15f, 0) * dir, 7.5f, 0.8f);
                GunkataFx.MuzzleFlash(transform.position + Vector3.up * 0.9f + dir * 0.8f, dir, 0.6f);
                yield return new WaitForSeconds(0.2f);
            }
            nextAttack = Time.time + TicketInterval * Random.Range(0.9f, 1.2f) / AtkMul;
            busy = false;
        }

        /// <summary>뒤로 깡충: 고양이 반대쪽(조금 비스듬히)으로 0.25초 물러난 뒤 흩뿌리기</summary>
        IEnumerator ConductorHop(PlayerController p, Vector3 dir)
        {
            busy = true; Dashing = true;
            nextHop = Time.time + ConductorHopCooldown;
            var away = Quaternion.Euler(0, Random.Range(-35f, 35f), 0) * -dir;
            var from = transform.position;
            var to = from + away * ConductorHopDist;
            var rm = RunManager.Instance; if (rm) to = rm.ClampToRoom(to, 1.2f);
            FxBurst.Spawn(from + Vector3.up * 0.15f, 1f, FxBurst.Kind.Dust, FxBurst.MistDust);
            for (float t = 0; t < 1f; t += Time.deltaTime / 0.25f)
            {
                float k = 1 - (1 - t) * (1 - t);
                transform.position = Vector3.Lerp(from, to, k);
                if (model) model.localPosition = Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.45f;
                Face(dir);
                yield return null;
            }
            if (model) model.localPosition = Vector3.zero;
            Dashing = false;
            FxBurst.Spawn(transform.position + Vector3.up * 0.1f, 0.8f, FxBurst.Kind.Dust, FxBurst.MistDust);
            busy = false;
            if (health.Alive && !health.Stunned) StartCoroutine(ConductorScatter(p, ToP(p), 0.35f));
        }

        IEnumerator ConductorPunch(PlayerController p)
        {
            busy = true;
            nextPunch = Time.time + PunchCooldown / AtkMul;
            Anim("Attack");
            var dir = ToP(p);
            // 굵은 진홍 조준선이 고양이를 따라가다 마지막 0.15초엔 고정 (펀치는 엄폐물을 뚫음)
            tele = Telegraph.Lane(transform.position + dir * 0.6f, dir, 14f, 0.3f, PunchAim / AtkMul, Telegraph.Danger);
            float end = Time.time + PunchAim / AtkMul;
            bool ok = true;
            while (Time.time < end)
            {
                if (!health.Alive || health.Stunned) { ok = false; if (tele) tele.Kill(); break; }
                if (end - Time.time > 0.15f) { dir = Vector3.Slerp(dir, ToP(p), 6f * Time.deltaTime).normalized; if (tele) { tele.SetDir(dir); tele.transform.position = transform.position + dir * 0.6f; } }
                Face(dir);
                yield return null;
            }
            if (ok)
            {
                var b = EnemyProjectile.Spawn(transform.position + Vector3.up * 0.9f + dir * 0.7f, dir, PunchSpeed, projectileDamage, 0, EnemyProjectile.Style.Ticket);
                b.transform.localScale = Vector3.one * 1.35f;
                b.pierceCover = true;
                b.OnHitPlayer = pl =>
                {
                    pl.dashLockUntil = Mathf.Max(pl.dashLockUntil, Time.time + PunchDashLock);
                    GunkataFx.Flare(pl.transform.position + Vector3.up * 1.1f, 1.3f, new Color(1f, 0.3f, 0.5f));
                    FxBurst.Spawn(pl.transform.position + Vector3.up * 1.0f, 1.2f, FxBurst.Kind.Spark, new Color(1f, 0.35f, 0.55f));
                };
                GunkataFx.MuzzleFlash(transform.position + Vector3.up * 0.9f + dir * 0.8f, dir, 0.8f);
                CameraFollow.Shake(0.05f);
                yield return new WaitForSeconds(0.3f);
            }
            nextAttack = Mathf.Max(nextAttack, Time.time + 0.9f);
            busy = false;
        }

        // ================= 트렁크 짐꾼 =================
        Transform trunkBone;
        SkinnedMeshRenderer trunkSkin;
        bool trunkHidden, trunkLooked;

        /// <summary>트렁크 뼈 · 렌더러 (PorterRig가 만든 가슴 밑 'Trunk' 뼈 + 'TrunkSkin'). 없으면 null → 대신 키트 짐을 던짐</summary>
        Transform Trunk()
        {
            if (trunkLooked) return trunkBone;
            trunkLooked = true;
            if (!model) return null;
            foreach (var t in model.GetComponentsInChildren<Transform>(true)) if (t.name == "Trunk") { trunkBone = t; break; }
            foreach (var r in model.GetComponentsInChildren<SkinnedMeshRenderer>(true)) if (r.name == "TrunkSkin") { trunkSkin = r; break; }
            return trunkBone;
        }

        /// <summary>손에 든 트렁크를 그대로 구워 떼어 냄 (날아갈 물체). 트렁크 렌더러는 끔</summary>
        GameObject DetachTrunk()
        {
            Trunk();
            trunkHidden = true;
            if (!trunkSkin || !trunkBone) return null;
            var mesh = new Mesh { name = "ThrownTrunk" };
            trunkSkin.BakeMesh(mesh, true);
            // 회전 중심 = 트렁크 가운데: 구운 메시(렌더러 공간)를 트렁크 뼈 기준으로 옮김
            var center = trunkSkin.transform.InverseTransformPoint(trunkBone.position);
            var vs = mesh.vertices;
            for (int i = 0; i < vs.Length; i++) vs[i] -= center;
            mesh.vertices = vs;
            mesh.RecalculateBounds();
            var go = new GameObject("ThrownTrunk");
            go.transform.SetPositionAndRotation(trunkBone.position, trunkSkin.transform.rotation);
            go.transform.localScale = trunkSkin.transform.lossyScale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterials = trunkSkin.sharedMaterials;
            go.AddComponent<DestroyMesh>().mesh = mesh;
            trunkSkin.enabled = false;
            return go;
        }

        /// <summary>새 트렁크를 꺼냄: 0.25초 동안 튀며 커짐 + 먼지</summary>
        IEnumerator ShowTrunk()
        {
            var t = Trunk();
            trunkHidden = false;
            if (!t || !trunkSkin) yield break;
            trunkSkin.enabled = true;
            FxBurst.Spawn(t.position, 1.1f, FxBurst.Kind.Dust, FxBurst.MistDust);
            for (float k = 0; k < 1f && t; k += Time.deltaTime / 0.25f)
            {
                float e = 1 + 2.2f * Mathf.Pow(k - 1, 3) + 1.2f * Mathf.Pow(k - 1, 2);       // 살짝 튀며 커짐
                t.localScale = Vector3.one * Mathf.Max(0.001f, e);
                yield return null;
            }
            if (t) t.localScale = Vector3.one;
            ShieldClank();
        }

        /// <summary>천천히 몸을 돌림 (방패라 빨리 못 돌아섬 → 옆 · 뒤로 돌아 쏠 틈)</summary>
        void FaceSlow(Vector3 dir, float degPerSec)
        {
            if (dir.sqrMagnitude < 0.0001f) return;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dir), degPerSec * Time.deltaTime);
        }

        void PorterUpdate(PlayerController p, Vector3 dir, float dist, float spd)
        {
            if (trunkHidden && Time.time >= shieldDownUntil && !busy) StartCoroutine(ShowTrunk());
            if (busy) return;
            var fwd = Effects.Flat(transform.forward).normalized;
            float off = Vector3.Angle(fwd, dir);
            // 등 뒤를 잡히면 가끔 발을 굴러 빠르게 돌아섬 (2.5~4초에 한 번 → 그 사이가 뒤를 칠 기회)
            if (off > 100f && Time.time >= nextShuffle)
            {
                nextShuffle = Time.time + Random.Range(2.5f, 4f);
                shuffleUntil = Time.time + 0.45f;
                FxBurst.Spawn(transform.position + Vector3.up * 0.1f, 1.2f, FxBurst.Kind.Dust, FxBurst.MistDust);
                CameraFollow.Shake(0.03f);
            }
            float turn = Time.time < shuffleUntil ? 340f : PorterTurn;

            // 움직임: ±25° 비스듬히 다가오다 가끔 버티고 섬. 몸 방향과 걷는 방향이 60° 넘게 어긋나면 거의 제자리에서 돌아섬 (미끄러지지 않게)
            if (Time.time >= nextFlank)
            {
                flankAngle = Random.Range(-25f, 25f);
                nextFlank = Time.time + Random.Range(1.4f, 2.6f);
                if (Random.value < 0.22f) pauseUntil = Time.time + Random.Range(0.35f, 0.6f);
            }
            var md = dist > 3.5f ? Quaternion.Euler(0, flankAngle, 0) * dir : dir;
            FaceSlow(dist > 6f ? md : dir, turn);
            float align = Vector3.Angle(Effects.Flat(transform.forward), md);
            float walk = align > 60f ? 0.15f : Mathf.Lerp(1f, 0.5f, align / 60f);
            if (Time.time >= pauseUntil && dist > 1.9f) transform.position += md * Mathf.Min(spd * walk * Time.deltaTime, dist - 1.9f);

            if (elite && Time.time >= nextTrunk && dist > 4f && dist < TrunkRange - 0.5f && off < 40f && Time.time >= nextAttack - 1f)
            {
                StartCoroutine(PorterThrow(p));
                return;
            }
            if (Time.time < nextAttack || off > 45f) return;
            bool close = dist < SlamRange + 0.4f, mid = dist > 3.5f && dist < BashDist + 2.5f;
            if (!close && !mid) return;
            int a = Pick((0, close ? 3f : 0f), (1, mid ? 2.5f : 0.4f));
            StartCoroutine(a == 0 ? PorterSlam(p) : PorterBash(p));
        }

        IEnumerator PorterSlam(PlayerController p)
        {
            busy = true;
            Anim("Special");                                   // 전용 클립: 젖혀 들어 올림 → 0.7초에 허리를 숙여 바닥을 쾅 → 숙인 채 → 일어남
            var fwd = Effects.Flat(transform.forward).normalized;
            tele = Telegraph.Sector(transform.position, fwd, SlamRange, SlamHalf, SlamWindup / AtkMul, Telegraph.Danger);
            bool ok = false;
            yield return Hold(SlamWindup, r => ok = r);
            if (ok)
            {
                var hitAt = transform.position + fwd * 1.5f;
                if (p && p.health.Alive)
                {
                    var d = Effects.Flat(p.transform.position - transform.position);
                    if (d.magnitude <= SlamRange && Vector3.Angle(fwd, d) <= SlamHalf)
                    {
                        p.health.TakeDamage(contactDamage);
                        p.health.Knockback((d.sqrMagnitude > 1e-4f ? d.normalized : fwd) * 6f);
                    }
                }
                FxBurst.Spawn(hitAt + Vector3.up * 0.15f, SlamRange * 1.3f, FxBurst.Kind.Dust, FxBurst.MistDust);
                PropHit.Area(hitAt, SlamRange * 0.8f, contactDamage);
                GunkataFx.HitImpact(hitAt + Vector3.up * 0.2f, Vector3.down, new Color(1f, 0.7f, 0.35f), false, 1.2f);
                CameraFollow.Shake(0.16f);
                // 빈틈: 트렁크를 바닥에 박은 채 (방패 꺼짐), 제자리에서 다시 들어 올림
                shieldDownUntil = porterOpenUntil = Time.time + SlamOpen;
                yield return Hold(SlamOpen, _ => { });
                ShieldClank();
                yield return new WaitForSeconds(0.3f);             // 클립이 일어서는 동안
            }
            nextAttack = Time.time + PorterCooldown * Random.Range(0.85f, 1.2f) / AtkMul;
            busy = false;
        }

        /// <summary>방패 돌진: 0.5초 웅크려 예고 → 트렁크를 앞세워 5m 돌진(맞으면 피해 80% + 크게 밀림) → 0.5초 빈틈</summary>
        IEnumerator PorterBash(PlayerController p)
        {
            busy = true;
            var dir = ToP(p);
            Anim("Attack");
            var laneFrom = transform.position;
            tele = Telegraph.Lane(laneFrom, dir, BashDist + 0.8f, 1.8f, BashWindup / AtkMul, Telegraph.Danger);
            float end = Time.time + BashWindup / AtkMul;
            bool ok = true;
            while (Time.time < end)
            {
                if (!health.Alive || health.Stunned) { ok = false; if (tele) tele.Kill(); break; }
                FaceSlow(dir, 500f);
                yield return null;
            }
            if (ok)
            {
                bool hit = false;
                float run = 0, ghost = 0;
                var bashProps = new HashSet<Health>();
                var rm = RunManager.Instance;
                FxBurst.Spawn(transform.position + Vector3.up * 0.1f, 1.4f, FxBurst.Kind.Dust, FxBurst.MistDust);
                Dashing = true;
                while (run < BashDist && health.Alive && !health.Stunned)
                {
                    float step = BashSpeed * health.SlowMul * Time.deltaTime;
                    var before = transform.position;
                    transform.position += dir * step;
                    if (rm) transform.position = rm.ClampToRoom(transform.position, 1.2f);
                    float moved = (transform.position - before).magnitude;
                    run += step;
                    ghost += Time.deltaTime;
                    PropHit.Area(transform.position + dir * 0.9f, 1.3f, contactDamage, bashProps);
                    if (model && ghost > 0.05f) { ghost = 0; Afterimage.Spawn(model, DashSil * new Color(1, 1, 1, 0.5f), 0.25f, false); }
                    if (!hit && p && p.health.Alive && Telegraph.InLane(p.transform.position, laneFrom, dir, BashDist + 0.8f, 1.8f, run - 0.95f, run + 0.95f))   // 예고 선과 같게
                    {
                        hit = true;
                        p.health.TakeDamage(contactDamage * 0.8f);
                        p.health.Knockback(dir * 9f);
                        GunkataFx.HitImpact(p.transform.position + Vector3.up, dir, new Color(1f, 0.7f, 0.35f), false, 1.1f);
                        CameraFollow.Shake(0.14f);
                    }
                    if (moved < step * 0.3f) { CameraFollow.Shake(0.1f); FxBurst.Spawn(transform.position + dir * 1f + Vector3.up, 1.2f, FxBurst.Kind.Spark, new Color(1f, 0.85f, 0.55f)); break; }   // 벽에 쾅
                    yield return null;
                }
                Dashing = false;
                FxBurst.Spawn(transform.position + Vector3.up * 0.1f, 1.6f, FxBurst.Kind.Dust, FxBurst.MistDust);
                shieldDownUntil = porterOpenUntil = Time.time + BashOpen;
                yield return Hold(BashOpen, _ => { });
            }
            nextAttack = Time.time + PorterCooldown * Random.Range(0.9f, 1.3f) / AtkMul;
            busy = false;
        }

        /// <summary>트렁크를 다시 들어 올림: 쇳소리 불꽃</summary>
        void ShieldClank()
        {
            if (!health.Alive) return;
            var at = transform.position + Effects.Flat(transform.forward).normalized * 0.8f + Vector3.up * 1.2f;
            FxBurst.Spawn(at, 0.8f, FxBurst.Kind.Spark, new Color(1f, 0.85f, 0.55f));
        }

        IEnumerator PorterThrow(PlayerController p)
        {
            busy = true;
            nextTrunk = Time.time + TrunkCooldown / AtkMul;
            Anim("Throw");                                     // 전용 클립: 뒤로 젖혀 비틂 → 0.8초에 앞으로 내던짐
            var dir = ToP(p);
            tele = Telegraph.Lane(transform.position + dir * 0.5f, dir, TrunkRange, 1.4f, TrunkAim / AtkMul, Telegraph.Danger);
            bool ok = true;
            float end = Time.time + TrunkAim / AtkMul;
            while (Time.time < end)
            {
                if (!health.Alive || health.Stunned) { ok = false; if (tele) tele.Kill(); break; }
                FaceSlow(dir, 400f);
                yield return null;
            }
            if (ok)
            {
                // 손에 든 트렁크가 그대로 떨어져 나가 날아감 → 3초 무방비 (새 트렁크를 꺼낼 때까지)
                var t = Trunk();
                var from = t ? t.position : transform.position + dir * 0.9f + Vector3.up * 1.1f;
                var thrown = DetachTrunk();
                shieldDownUntil = Time.time + TrunkOpen;
                // 짐꾼이 죽어도 트렁크는 끝까지 날아가게 RunManager가 돌림
                MonoBehaviour host = RunManager.Instance ? RunManager.Instance : this;
                host.StartCoroutine(TrunkFlight(thrown, from, dir, contactDamage));
                CameraFollow.Shake(0.06f);
                yield return new WaitForSeconds(0.45f);
            }
            nextAttack = Mathf.Max(nextAttack, Time.time + 1.2f);
            busy = false;
        }

        /// <summary>날아가는 트렁크: 손에서 떼어 낸 트렁크(없으면 2장 키트 '멈춘 짐')를 빙글빙글 돌리며 직선 10m. 고양이에 닿으면 피해 + 밀림, 끝에서 부서짐</summary>
        IEnumerator TrunkFlight(GameObject go, Vector3 from, Vector3 dir, float dmg)
        {
            var baseRot = go ? go.transform.rotation : Quaternion.LookRotation(dir);
            var prefab = go ? null : Kit.Prefab(chapter, "FloatLuggage");
            if (go) { }
            else if (prefab)
            {
                go = Instantiate(prefab);
                foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>()) Destroy(mb);     // 폭발물 테두리 · 낙하 동작 떼어 냄
                foreach (var c in go.GetComponentsInChildren<Collider>()) Destroy(c);
                go.transform.localScale *= 0.85f;
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(go.GetComponent<Collider>());
                go.transform.localScale = new Vector3(0.9f, 0.6f, 0.5f);
            }
            float travelled = 0;
            var pos = from;
            float startY = from.y;
            while (travelled < TrunkRange && go)
            {
                float step = TrunkSpeed * Time.deltaTime;
                // 벽에 부딪히면 거기서 부서짐
                if (Physics.Raycast(pos, dir, out var wh, step + 0.3f, ~0, QueryTriggerInteraction.Ignore) && !wh.collider.GetComponentInParent<Enemy>() && !wh.collider.GetComponentInParent<PlayerController>())
                {
                    pos = wh.point - dir * 0.3f;
                    break;
                }
                pos += dir * step;
                travelled += step;
                pos.y = Mathf.Lerp(startY, 1.0f, Mathf.Clamp01(travelled / 2f));          // 손 높이에서 몸통 높이로
                go.transform.position = pos;
                go.transform.rotation = Quaternion.AngleAxis(travelled * 70f, Vector3.Cross(Vector3.up, dir)) * baseRot;   // 앞으로 굴러가듯 회전
                var pl = PlayerController.Instance;
                if (pl && pl.health.Alive && Telegraph.InLane(pl.transform.position, pos - dir * 0.5f, dir, 1.0f, 1.4f))   // 트렁크가 지금 덮은 구간 · 예고 폭 1.4
                {
                    pl.health.TakeDamage(dmg);
                    pl.health.Knockback(dir * 7f);
                    GunkataFx.HitImpact(pl.transform.position + Vector3.up, dir, new Color(1f, 0.6f, 0.3f), false, 1.1f);
                    CameraFollow.Shake(0.12f);
                    break;
                }
                yield return null;
            }
            if (go) Destroy(go);
            var at = new Vector3(pos.x, 0, pos.z);
            PropHit.Area(at, 1.3f, dmg);
            FxBurst.Spawn(at + Vector3.up * 0.4f, 1.6f, FxBurst.Kind.Dust, FxBurst.MistDust);
            FxBurst.Spawn(at + Vector3.up * 0.6f, 1.0f, FxBurst.Kind.Spark, new Color(1f, 0.8f, 0.5f));
        }

        // ================= 신호수 =================
        void SignalUpdate(PlayerController p, Vector3 dir, float dist, float spd)
        {
            if (busy) return;
            // 바짝 붙으면 발밑에 신호등을 떨구고 도망
            if (dist < SignalFleeRange && Time.time >= nextFlee)
            {
                StartCoroutine(SignalFlee(p, dir));
                return;
            }
            KeepRange(p, dir, dist, spd, SignalKeep - 1f, SignalKeep + 1f);
            if (Time.time >= nextAttack && dist < 14f)
            {
                int a = Pick((0, 3f), (1, dist > 5f ? 2f : 0.8f), (2, 1.6f));
                StartCoroutine(a == 0 ? SignalPair(p) : a == 1 ? SignalCrossing(p) : SignalChase(p));
            }
        }

        Vector3 Hand => transform.position + Vector3.up * 1.3f + transform.forward * 0.3f;

        /// <summary>던지기 예비 동작 (던지기 클립 0.34초에 손을 떠남). 끊기면 false</summary>
        IEnumerator WindThrow(Vector3 at, System.Action<bool> ok)
        {
            Anim("Attack");
            float release = ThrowRelease / AtkMul;
            for (float w = 0; w < release; w += Time.deltaTime)
            {
                if (!health.Alive || health.Stunned) { ok(false); yield break; }
                Face(Effects.Flat(at - transform.position));
                yield return null;
            }
            ok(true);
        }

        Vector3 InRoom(Vector3 v) { var rm = RunManager.Instance; v.y = transform.position.y; return rm ? rm.ClampToRoom(v, 1f) : v; }

        void EndSignal(float mul = 1f)
        {
            nextAttack = Time.time + SignalInterval * mul * Random.Range(0.9f, 1.15f) / AtkMul;
            strafeSign = -strafeSign;
            busy = false;
        }

        /// <summary>① 신호등 2개: 고양이 자리 + 둘레 2.5~3.5m</summary>
        IEnumerator SignalPair(PlayerController p)
        {
            busy = true;
            var a = InRoom(p.transform.position);
            var b = InRoom(a + Quaternion.Euler(0, Random.Range(0f, 360f), 0) * Vector3.forward * Random.Range(2.5f, 3.5f));
            bool ok = false;
            yield return WindThrow(a, r => ok = r);
            if (ok)
            {
                SignalBlast.Throw(Hand, a, projectileDamage, elite);
                SignalBlast.Throw(Hand, b, projectileDamage, elite);
                yield return new WaitForSeconds(0.3f);
            }
            EndSignal();
        }

        /// <summary>② 건널목 차단: 신호수 → 고양이 방향에 수직인 줄로 4개 (고양이가 가던 쪽을 조금 앞질러), 차례로 떨어짐</summary>
        IEnumerator SignalCrossing(PlayerController p)
        {
            busy = true;
            var c = Lead(p, 0.5f);
            var across = Vector3.Cross(Vector3.up, ToP(p)).normalized;
            bool ok = false;
            yield return WindThrow(c, r => ok = r);
            if (ok)
            {
                float shift = Random.Range(-0.6f, 0.6f);
                for (int i = 0; i < 4; i++)
                {
                    SignalBlast.Throw(Hand, InRoom(c + across * ((i - 1.5f + shift) * 1.7f)), projectileDamage, false, 1.3f, SignalFuse - 0.1f);
                    yield return new WaitForSeconds(0.07f);
                }
                yield return new WaitForSeconds(0.25f);
            }
            EndSignal(1.1f);
        }

        /// <summary>③ 따라오는 신호: 0.45초 간격으로 고양이가 갈 자리에 3개</summary>
        IEnumerator SignalChase(PlayerController p)
        {
            busy = true;
            bool ok = false;
            yield return WindThrow(p.transform.position, r => ok = r);
            for (int i = 0; ok && i < 3; i++)
            {
                if (!health.Alive || health.Stunned || !p) break;
                if (i > 0) Anim("Attack");
                Face(ToP(p));
                SignalBlast.Throw(Hand, InRoom(Lead(p, 0.35f + SignalBlast.Flight)), projectileDamage, false, 1.6f, 1.2f);
                yield return new WaitForSeconds(0.45f);
            }
            EndSignal(1.15f);
        }

        /// <summary>발밑에 신호등을 떨구고 고양이 반대쪽(비스듬히)으로 0.9초 도망</summary>
        IEnumerator SignalFlee(PlayerController p, Vector3 dir)
        {
            busy = true;
            nextFlee = Time.time + Random.Range(4.5f, 6f);
            SignalBlast.Throw(transform.position + Vector3.up * 0.8f, InRoom(transform.position), projectileDamage, elite, SignalRadius, 1.2f);
            var away = (Quaternion.Euler(0, Random.Range(-40f, 40f), 0) * -dir).normalized;
            for (float t = 0; t < 0.9f; t += Time.deltaTime)
            {
                if (!health.Alive || health.Stunned) break;
                transform.position += away * speed * 1.5f * health.SlowMul * Time.deltaTime;
                Face(away);
                yield return null;
            }
            nextAttack = Mathf.Max(nextAttack, Time.time + 0.6f);
            busy = false;
        }
    }
}
