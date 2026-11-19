// 4장 '시계 장치 속' 일반 적 6종 (레벨 디자인 md '챕터 4' · '수치 기준 (챕터 4)', 2026-11-19)
// 체력 · 피해는 md 값 그대로 (챕터 배율 안 곱함). 강화형 체력 1.5배는 EnemyFactory.MakeElite.
// 동작은 스킬마다 전용 모션 상태(Editor/EnemyMotions.cs → EnemyModelBuilder.Moves)를 이름으로 재생 (2026-11-19 사용자: 모션 재사용 말고 스킬마다 다르게, 액션감 있게)
//
// - 태엽 병정(ClockSoldier): 8~10m 유지, 3초마다 1발(예고 0.5 · 10m/초 · 15) [Shot]. 병정 2마리 이상이면 8초마다 일제 사격:
//   한 마리가 북 [Drum], 나머지는 견착 [Volley] 0.8초 → 전부 3연발(0.15초 간격). 강화형 착검 돌격: 6초 쿨 · 예고 0.6 · 6m · 18 [Bayonet]
// - 종지기(BellRinger): 9m 유지, 5초마다 종 → 음파 링(예고 0.6 · 5m/초 · 반경 10 · 폭 0.5 · 16, 대시로 넘음) [Ring]. 강화형 2겹(0.6초 간격) [RingDouble]
// - 오르골 무희(MusicDancer): 8m 유지, 7초마다 3초 회전(이속 0.8) → 두 갈래 나선 탄 0.12초마다(6m/초 · 15) [Rise → Spin]. 강화형 마무리 12방향 [Finale]
// - 초침 자객(TickAssassin): 매 초 '째깍'(모든 자객이 같은 박자)에 2.5m 점멸 [Blink]. 2.5m 안이면 예고 0.5 → 전방 2.5m · 폭 1m 찌르기 20 [Stab] → 1초 빈틈 [Breath]
//   강화형 뒤 찌르기: 고양이 뒤 1.5m로 점멸 → 예고 0.6 → 반 바퀴 돌며 역수 찌르기 [BackStab]
// - 진자 기사(PendulumKnight): 이속 2.8, 1.2초마다 좌 · 우 번갈아 낫(전방 3m · 한쪽 120° · 예고 0.5 · 18) [SwingL / SwingR]. 강화형 회전 베기 8초 쿨 · 예고 0.8 · 반경 3 · 20 [Whirl]
// - 시계 수리공(Repairman): 10m 유지(도망), 4초마다 8m 안 적 태엽 감기(예고 0.6, 3초 이속 · 공속 1.3배 + 최대 체력 15%) [Wind],
//   3초마다 렌치 곡사(원 예고 0.8 · 반경 1.2 · 15) [Toss]. 강화형: 한 번에 2마리, 죽을 때 8m 안 적 전부 감기
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    public partial class Enemy
    {
        // ---------- 수치 (md 챕터 4) ----------
        public const float SoldierNear = 8f, SoldierFar = 10f, SoldierInterval = 3f, SoldierAim = 0.5f, SoldierBullet = 10f;
        public const float VolleyEvery = 8f, VolleyDrum = 0.8f, VolleyGap = 0.15f, BayonetCd = 6f, BayonetWindup = 0.6f, BayonetDist = 6f, BayonetTime = 0.34f;
        public const float RingerKeep = 9f, RingerInterval = 5f, RingerWindup = 0.6f, RingSpeed = 5f, RingMax = 10f, RingWidth = 0.5f, RingSecond = 0.6f;
        public const float DancerKeep = 8f, DancerInterval = 7f, DancerSpin = 3f, DancerSpinSpeed = 0.8f, DancerShotGap = 0.12f, DancerBullet = 6f, DancerArmTurn = 140f, DancerRise = 0.4f;
        public const float TickBlink = 2.5f, TickCrouch = 0.18f, StabRange = 2.5f, StabWidth = 1f, StabWindup = 0.5f, StabOpen = 1f, BackStabDist = 1.5f, BackStabWindup = 0.6f;
        public const float PKSwingGap = 1.2f, PKRange = 3f, PKHalf = 60f, PKWindup = 0.5f, PKEngage = 5.5f, WhirlCd = 8f, WhirlWindup = 0.8f, WhirlRadius = 3f;
        public const float RepairKeep = 10f, WindEvery = 4f, WindReach = 8f, WindWindup = 0.6f, WindDur = 3f, WindBoost = 1.3f, WindHeal = 0.15f;
        public const float TossEvery = 3f, TossRelease = 0.3f, TossFlight = 0.5f, TossRadius = 1.2f;
        public const float SoldierShotDmg = 15f, SoldierBayonetDmg = 18f, RingDmg = 16f, DancerDmg = 15f, StabDmg = 20f, PKDmg = 18f, WhirlDmg = 20f, TossDmg = 15f;

        // 조준선 색: 붉은 계열이지만 '공격이 나가는 레이저 줄'(LaserFx.Streak, 빨강 g < 0.3)은 안 띄움 — 탄이 날아가는 공격이라서
        static readonly Color AimRed = new(1f, 0.38f, 0.42f);

        // 태엽 감기 버프 (시계 수리공이 걸어 줌)
        float windUntil;
        bool Wound => Time.time < windUntil;

        // 일제 사격: 방 안 병정끼리 같은 번호로 묶음
        static int volleyId;
        static float volleyStart = -99f, nextVolleyAt;
        static Enemy volleyDrummer;
        int myVolley;
        float nextBayonet;
        bool backhand;                              // 진자 기사: 다음 휘두름이 오른쪽(true)인지
        float nextTick;
        bool tickReady;

        static void Spark(Vector3 at, float size = 0.9f) => FxFlipbook.Spawn("bk_c4spark", at, Vector3.zero, size, 0.25f, false);

        // 무기 칼빛 (BladeStreak): 무기 기준 칼끝 · 손잡이 쪽 점 (Kit_W_* 명세: 원점 = 손잡이, 긴 축 +Y)
        static readonly Dictionary<string, (Vector3 tip, Vector3 grip)> BladePts = new()
        {
            { "PendulumScythe", (new Vector3(0.3f, 1.6f, 0), new Vector3(0, 0.85f, 0)) },   // 진자 원판 · 낫날 바깥 ~ 자루 가운데
            { "PendulumScythePlus", (new Vector3(0.35f, 1.9f, 0), new Vector3(0, 1.0f, 0)) },   // 강화 기사: 원판 2.05 · 낫날 1.1 (무기 자체 좌표, 플레이로는 아직 안 봄)
            { "NeedleDagger", (new Vector3(0, 0.75f, 0), new Vector3(0, 0.05f, 0)) },
            { "Hammer", (new Vector3(0, 0.4f, 0), new Vector3(0, 0.05f, 0)) },
            { "Rifle", (new Vector3(0, 0.95f, 0), new Vector3(0, 0.3f, 0)) },
        };
        readonly Dictionary<string, Transform> weaponCache = new();
        Transform WeaponT(string name)
        {
            if (weaponCache.TryGetValue(name, out var t) && t) return t;
            t = null;
            if (model) foreach (var c in model.GetComponentsInChildren<Transform>(true)) if (c.name == "Weapon_" + name) { t = c; break; }
            weaponCache[name] = t;
            return t;
        }
        /// <summary>무기에 붙은 칼빛을 swing초 동안 (휘두르는 박자에 맞춰 부름)</summary>
        void Streak(string weapon, float swing)
        {
            var w = WeaponT(weapon + "Plus");          // 강화형 전용 무기(예: 강화 기사의 큰 양날 낫)가 있으면 그것
            if (w) weapon += "Plus"; else w = WeaponT(weapon);
            if (w && BladePts.TryGetValue(weapon, out var bp)) BladeStreak.Begin(w, bp.tip, bp.grip, swing, BladeStreak.Ch4Base);
        }

        /// <summary>죽을 때 4장 처리 (Die에서 부름)</summary>
        void OnDieCh4()
        {
            if (kind == Kind.Repairman && elite)
                foreach (var o in All.ToArray())
                    if (o && o != this && o.health.Alive && Effects.Flat(o.transform.position - transform.position).magnitude <= WindReach) WindUp(o, transform.position);
            if (volleyDrummer == this) volleyDrummer = null;
        }

        // ================= 태엽 병정 =================
        static int AliveSoldiers()
        {
            int n = 0;
            foreach (var e in All) if (e && e.kind == Kind.ClockSoldier && e.health.Alive) n++;
            return n;
        }

        void SoldierUpdate(PlayerController p, Vector3 dir, float dist, float spd)
        {
            // 일제 사격에 합류 (북이 울린 지 0.5초 안이면 하던 걸 마치고 바로 견착)
            if (myVolley != volleyId && Time.time - volleyStart < 0.5f && !busy) { myVolley = volleyId; StartCoroutine(SoldierVolley(p, volleyDrummer == this)); return; }
            if (busy) return;
            // 북 치기: 병정이 둘 이상이고 8초가 지났으면 이 병정이 북을 침
            if (Time.time >= nextVolleyAt && Time.time - volleyStart > VolleyEvery * 0.5f && AliveSoldiers() >= 2 && dist < 16f)
            {
                volleyId++; volleyStart = Time.time; volleyDrummer = this;
                nextVolleyAt = Time.time + VolleyEvery;
                myVolley = volleyId;
                StartCoroutine(SoldierVolley(p, true));
                return;
            }
            if (elite && Time.time >= nextBayonet && dist > 2f && dist < BayonetDist + 1f) { StartCoroutine(SoldierBayonet(p)); return; }
            KeepRange(p, dir, dist, spd, SoldierNear, SoldierFar);
            if (Time.time >= nextAttack && dist < 15f) StartCoroutine(SoldierShot(p));
        }

        IEnumerator SoldierShot(PlayerController p)
        {
            busy = true;
            Anim("Shot");                                                      // 어깨 견착 → 숨 참고 조준 → 반동으로 총구가 튀어 오름
            var d = ToP(p);
            float end = Time.time + SoldierAim / AtkMul;
            bool ok = true;
            while (Time.time < end)
            {
                if (!health.Alive || health.Stunned) { ok = false; break; }
                d = Vector3.Slerp(d, ToP(p), 5f * Time.deltaTime).normalized;
                if (tele) tele.Kill();
                tele = Telegraph.Lane(transform.position + d * 0.6f, d, 11f, 0.14f, 0.05f, AimRed);
                Face(d);
                yield return null;
            }
            if (tele) tele.Kill();
            if (ok) FireRifle(d);
            nextAttack = Time.time + SoldierInterval * Random.Range(0.85f, 1.15f) / AtkMul;
            yield return new WaitForSeconds(0.25f);                            // 반동에서 돌아오는 짧은 틈
            busy = false;
        }

        void FireRifle(Vector3 d)
        {
            var muzzle = transform.position + Vector3.up * 1.15f + d * 1.1f;
            EnemyProjectile.Spawn(muzzle, d, SoldierBullet, projectileDamage, 18f, EnemyProjectile.Style.Sniper);
            Spark(muzzle, 0.7f);
            Sfx.Play("e4_rifle", 0.45f, 0.08f, 4);
        }

        /// <summary>일제 사격: 북 치는 병정은 왼손으로 허리춤 북을 두 번 → 견착, 나머지는 처음부터 견착해 0.8초 겨눔 → 다 같이 3연발</summary>
        IEnumerator SoldierVolley(PlayerController p, bool drummer)
        {
            busy = true;
            Anim(drummer ? "Drum" : "Volley");
            float fireAt = volleyStart + VolleyDrum;
            var d = ToP(p);
            if (drummer)
                for (int b = 0; b < 2; b++)
                    LaserFx.Later(0.12f + b * 0.3f, () => { if (this && health.Alive) { Telegraph.Ring(transform.position, 1.4f, 0.18f, AimRed).NoGlint(); CameraFollow.Shake(0.03f); Sfx.Play("e4_drum", 0.7f, 0.04f, 2); } });
            // 조준선: 처음엔 따라가다 마지막 0.3초는 고정 → 겹치지 않는 틈으로 빠질 수 있게
            while (Time.time < fireAt)
            {
                if (!health.Alive || health.Stunned) { if (tele) tele.Kill(); busy = false; yield break; }
                if (fireAt - Time.time > 0.3f) d = Vector3.Slerp(d, ToP(p), 6f * Time.deltaTime).normalized;
                if (tele) tele.Kill();
                tele = Telegraph.Lane(transform.position + d * 0.6f, d, 13f, 0.22f, 0.05f, fireAt - Time.time > 0.3f ? AimRed : Telegraph.Danger);
                Face(d);
                yield return null;
            }
            if (tele) tele.Kill();
            for (int i = 0; i < 3; i++)
            {
                if (!health.Alive || health.Stunned) break;
                FireRifle(d);
                yield return new WaitForSeconds(VolleyGap);
            }
            yield return new WaitForSeconds(0.35f);
            nextAttack = Time.time + SoldierInterval * Random.Range(0.6f, 1f) / AtkMul;
            busy = false;
        }

        /// <summary>강화형 착검 돌격: 몸을 낮춰 소총을 앞으로 겨눔(0.6초) → 6m 일직선 찌르며 돌진 → 찌른 자세로 미끄러지며 멈춤</summary>
        IEnumerator SoldierBayonet(PlayerController p)
        {
            busy = true;
            nextBayonet = Time.time + BayonetCd;
            Anim("Bayonet");
            var d = ToP(p);
            tele = Telegraph.Lane(transform.position, d, BayonetDist + 0.8f, 1.2f, BayonetWindup / AtkMul, Telegraph.Danger);
            bool ok = false;
            yield return Hold(BayonetWindup, r => ok = r);
            if (ok)
            {
                Dashing = true;
                Sfx.Play("e4_bayonet", 0.6f);
                Streak("Rifle", BayonetTime);
                var from = transform.position;
                var to = from + d * BayonetDist;
                var rm = RunManager.Instance; if (rm) to = rm.ClampToRoom(to, 1.2f);
                bool hit = false;
                for (float t = 0; t < 1f; t += Time.deltaTime / BayonetTime)
                {
                    transform.position = Vector3.Lerp(from, to, 1 - (1 - t) * (1 - t));
                    float mv = Effects.Flat(transform.position - from).magnitude;
                    if (!hit && p && p.health.Alive && Telegraph.InLane(p.transform.position, from, d, BayonetDist + 0.8f, 1.2f, mv - 0.55f, mv + 0.9f))   // 예고 선(폭 1.2)과 같게
                    { hit = true; p.health.TakeDamage(contactDamage); p.health.Knockback(d * 5f); }
                    yield return null;
                }
                Dashing = false;
                PropHit.Area(transform.position + d, 1.2f, contactDamage);
                FxBurst.Spawn(transform.position + Vector3.up * 0.15f, 1.2f, FxBurst.Kind.Dust, FxBurst.MistDust);
                yield return new WaitForSeconds(0.5f);                         // 찌른 자세 그대로 빈틈
            }
            nextAttack = Mathf.Max(nextAttack, Time.time + 1f);
            busy = false;
        }

        // ================= 종지기 =================
        void RingerUpdate(PlayerController p, Vector3 dir, float dist, float spd)
        {
            if (busy) return;
            KeepRange(p, dir, dist, spd, RingerKeep - 1f, RingerKeep + 1f);
            if (Time.time >= nextAttack && dist < 14f) StartCoroutine(RingerBell(p));
        }

        /// <summary>망치를 머리 위로 크게 젖힘(0.6초) → 왼손 종을 내려침 = 그 순간 음파 링. 강화형은 두 번 연타 (0.6초 뒤 두 번째 링)</summary>
        IEnumerator RingerBell(PlayerController p)
        {
            busy = true;
            Anim(elite ? "RingDouble" : "Ring");
            Face(ToP(p));
            tele = Telegraph.Ring(transform.position, 1.3f, RingerWindup / AtkMul, Telegraph.Danger, transform);
            bool ok = false;
            yield return Hold(RingerWindup, r => ok = r);
            int rings = elite ? 2 : 1;
            for (int i = 0; ok && i < rings; i++)
            {
                if (i > 0) { yield return Hold(RingSecond, r => ok = r); if (!ok) break; }
                Streak("Hammer", 0.12f);
                var bell = transform.position + Vector3.up * 1.1f + Effects.Flat(transform.forward) * 0.45f;
                RippleRing.Spawn(transform.position, RingSpeed, projectileDamage, RingMax, RingWidth, Telegraph.Danger, 0.8f);
                Sfx.Play("e4_bell", 0.65f, 0.05f, 2);
                Spark(bell, 1.3f);
                CameraFollow.Shake(0.05f);
            }
            yield return new WaitForSeconds(0.35f);
            nextAttack = Time.time + RingerInterval * Random.Range(0.85f, 1.15f) / AtkMul;
            busy = false;
        }

        // ================= 오르골 무희 =================
        void DancerUpdate(PlayerController p, Vector3 dir, float dist, float spd)
        {
            if (busy) return;
            KeepRange(p, dir, dist, spd, DancerKeep - 1f, DancerKeep + 1f);
            if (Time.time >= nextAttack && dist < 15f) StartCoroutine(DancerSpinRun(p));
        }

        /// <summary>발끝으로 올라서며 두 팔을 모음(0.4초) → 3초 피루엣(몸이 돌고 두 팔은 둥글게) 두 갈래 나선 탄 → 강화형은 두 팔 활짝 펼치며 12방향</summary>
        IEnumerator DancerSpinRun(PlayerController p)
        {
            busy = true;
            Anim("Rise");
            tele = Telegraph.Ring(transform.position, 1.6f, DancerRise / AtkMul, AimRed, transform);
            bool ok = false;
            yield return Hold(DancerRise, r => ok = r);
            if (ok)
            {
                Anim("Spin");
                AnimBusy(true);
                Sfx.Play("e4_spin", 0.45f, 0.03f, 2);
                float dur = DancerSpin;
                StartCoroutine(SpinBody(dur, dur * 1.5f));
                var trail = GetComponent<EyeTrail>(); if (trail) trail.Hot(dur);
                float arm = Random.Range(0f, 360f), nextShot = 0f, sign = Random.value < 0.5f ? 1 : -1;
                for (float t = 0; t < dur; t += Time.deltaTime)
                {
                    if (!health.Alive || health.Stunned) break;
                    arm += DancerArmTurn * sign * Time.deltaTime;
                    if (t >= nextShot)
                    {
                        nextShot += DancerShotGap / AtkMul;
                        for (int k = 0; k < 2; k++)
                        {
                            var d = Quaternion.Euler(0, arm + k * 180f, 0) * Vector3.forward;
                            EnemyProjectile.Spawn(transform.position + Vector3.up * 1.1f + d * 0.7f, d, DancerBullet, projectileDamage, 14f, EnemyProjectile.Style.Orb);
                        }
                    }
                    // 도는 동안 아주 느리게 거리를 맞춤
                    if (p) { var to = Effects.Flat(p.transform.position - transform.position); float m = to.magnitude; if (m > 0.1f) transform.position += to / m * (m > DancerKeep ? 1 : -1) * DancerSpinSpeed * health.SlowMul * Time.deltaTime; }
                    yield return null;
                }
                AnimBusy(false);
                if (elite && health.Alive && !health.Stunned)
                {
                    Anim("Finale");
                    Sfx.Play("e4_finale", 0.6f);
                    for (int k = 0; k < 12; k++)
                    {
                        var d = Quaternion.Euler(0, k * 30f + arm, 0) * Vector3.forward;
                        EnemyProjectile.Spawn(transform.position + Vector3.up * 1.1f + d * 0.7f, d, DancerBullet, projectileDamage, 14f, EnemyProjectile.Style.Orb);
                    }
                    Spark(transform.position + Vector3.up * 1.6f, 1.4f);
                    yield return new WaitForSeconds(0.6f);
                }
                else yield return new WaitForSeconds(0.3f);
            }
            nextAttack = Time.time + DancerInterval * Random.Range(0.85f, 1.15f) / AtkMul;
            busy = false;
        }

        // ================= 초침 자객 =================
        void AssassinUpdate(PlayerController p, Vector3 dir, float dist, float spd)
        {
            if (busy) return;
            Face(dir);
            // 모든 자객이 같은 박자: 매 정수 초가 '째깍'. 0.18초 전에 웅크림
            if (nextTick < Time.time) { nextTick = Mathf.Ceil(Time.time + 0.05f); tickReady = false; }
            if (!tickReady && Time.time >= nextTick - TickCrouch / AtkMul) { tickReady = true; Anim("Blink"); }
            if (Time.time < nextTick) return;
            nextTick = Mathf.Ceil(Time.time + 0.05f);
            tickReady = false;
            if (dist <= StabRange + 0.2f && Time.time >= nextAttack) { StartCoroutine(AssassinStab(p, ToP(p), false)); return; }
            // 째깍: 고양이 쪽으로 2.5m 점멸 (붙어 버리지 않게 1.4m 앞에서 멈춤)
            float step = Mathf.Min(TickBlink, Mathf.Max(0f, dist - 1.4f));
            if (step > 0.2f) BlinkTo(transform.position + dir * step);
        }

        void BlinkTo(Vector3 to)
        {
            var rm = RunManager.Instance; if (rm) to = rm.ClampToRoom(to, 1.2f);
            if (model) Afterimage.Spawn(model, new Color(1f, 0.15f, 0.2f, 0.7f), 0.3f, false);
            Sfx.Play("e4_tick", 0.4f, 0.03f, 2);                                   // 째깍 (모든 자객이 같은 박자라 한 번에 겹쳐 울림 → 최대 2)
            Spark(transform.position + Vector3.up * 1.2f, 0.6f);
            transform.position = new Vector3(to.x, transform.position.y, to.z);   // 한 프레임에 옮김 → 눈빛 꼬리가 곧은 레이저 줄로 남음
        }

        /// <summary>찌르기: 단검을 뒤로 당김(예고 0.5) → 쭉 뻗음. 강화형은 고양이 뒤로 점멸해 반 바퀴 돌며 역수로 한 번 더 → 숨 고르기 1초 빈틈</summary>
        IEnumerator AssassinStab(PlayerController p, Vector3 d, bool back)
        {
            busy = true;
            Anim(back ? "BackStab" : "Stab");
            float windup = back ? BackStabWindup : StabWindup;
            transform.rotation = Quaternion.LookRotation(d);
            if (back) HoldYaw(180f);                                                // 등을 돌린 채 역수로 쥐고 웅크림
            tele = Telegraph.Lane(transform.position, d, StabRange, StabWidth, windup / AtkMul, Telegraph.Danger, transform).NoLaser();
            bool ok = false;
            yield return Hold(windup, r => ok = r);
            if (ok) Streak("NeedleDagger", back ? 0.18f : 0.12f);
            if (back) { if (ok) StartCoroutine(SpinBody(0.14f, 1f, 180f)); else HoldYaw(null); }   // 반 바퀴 채찍처럼 돌며 찌름
            if (ok) Sfx.Play("e4_stab", 0.55f);
            if (ok && p && p.health.Alive)
            {
                if (Telegraph.InLane(p.transform.position, transform.position, d, StabRange, StabWidth)) p.health.TakeDamage(contactDamage);   // 예고 선과 똑같이 (예전엔 앞뒤 0.3 · 옆 0.35 여유)
                PropHit.Area(transform.position + d * StabRange * 0.6f, 0.9f, contactDamage);
                Spark(transform.position + Vector3.up * 1.1f + d * StabRange, 0.9f);
            }
            if (ok && elite && !back && p && health.Alive)
            {
                // 고양이 뒤 1.5m로 점멸 → 고양이 쪽을 보며 한 번 더
                var pd = Effects.Flat(p.transform.position - transform.position);
                var behind = p.transform.position + (pd.sqrMagnitude > 1e-4f ? pd.normalized : transform.forward) * BackStabDist;
                yield return new WaitForSeconds(0.15f);
                BlinkTo(behind);
                yield return AssassinStab(p, ToP(p), true);
                yield break;
            }
            if (ok)
            {
                Anim("Breath");                                                  // 어깨로 숨 쉬는 빈틈
                yield return Hold(StabOpen, _ => { });
            }
            nextAttack = Time.time + 0.4f / AtkMul;
            busy = false;
        }

        // ================= 진자 기사 =================
        void PKnightUpdate(PlayerController p, Vector3 dir, float dist, float spd)
        {
            if (busy) return;
            if (elite && Time.time >= nextSpecial && dist < WhirlRadius + 0.4f) { StartCoroutine(PKnightWhirl(p)); return; }
            if (dist > 1.6f) transform.position += dir * spd * Time.deltaTime;
            Face(dir);
            if (dist < PKEngage && Time.time >= nextAttack) StartCoroutine(PKnightSwing(p));
        }

        /// <summary>낫을 한쪽으로 크게 젖힘(예고 0.5, 상체 비틀기) → 반대쪽으로 큰 호. 휘두르는 동안에도 천천히 전진, 다음은 반대쪽</summary>
        IEnumerator PKnightSwing(PlayerController p)
        {
            busy = true;
            bool right = backhand;
            backhand = !backhand;
            Anim(right ? "SwingR" : "SwingL");
            var fwd = ToP(p);
            transform.rotation = Quaternion.LookRotation(fwd);
            var center = Quaternion.Euler(0, right ? PKHalf : -PKHalf, 0) * fwd;
            tele = Telegraph.Sector(transform.position, center, PKRange, PKHalf, PKWindup / AtkMul, Telegraph.Danger, transform).NoLaser();
            float end = Time.time + PKWindup / AtkMul;
            bool ok = true;
            while (Time.time < end)
            {
                if (!health.Alive || health.Stunned) { ok = false; if (tele) tele.Kill(); break; }
                transform.position += fwd * speed * 0.35f * health.SlowMul * Time.deltaTime;
                yield return null;
            }
            if (ok) { Sfx.Play("e4_scythe", 0.6f, 0.06f, 2); Streak("PendulumScythe", 0.2f); }
            if (ok && p && p.health.Alive)
            {
                var to = Effects.Flat(p.transform.position - transform.position);
                if (to.magnitude <= PKRange && Vector3.Angle(center, to) <= PKHalf) { p.health.TakeDamage(contactDamage); p.health.Knockback(center * 4f); }
                PropHit.Area(transform.position + center * PKRange * 0.6f, PKRange * 0.6f, contactDamage);
            }
            // 휘두른 뒤 남은 시간 = 다음 휘두름까지 (1.2초 주기), 그 사이 반대쪽 · 뒤가 빈틈
            float rest = Mathf.Max(0.1f, PKSwingGap - PKWindup) / AtkMul;
            for (float t = 0; t < rest; t += Time.deltaTime) { if (p) Face(ToP(p)); yield return null; }
            nextAttack = Time.time;
            busy = false;
        }

        /// <summary>강화형 회전 베기: 낫을 등 뒤로 감고 웅크림(예고 0.8) → 한 바퀴</summary>
        IEnumerator PKnightWhirl(PlayerController p)
        {
            busy = true;
            nextSpecial = Time.time + WhirlCd;
            Anim("Whirl");
            tele = Telegraph.Ring(transform.position, WhirlRadius, WhirlWindup / AtkMul, Telegraph.Danger, transform);
            bool ok = false;
            yield return Hold(WhirlWindup, r => ok = r);
            if (ok)
            {
                StartCoroutine(SpinBody(0.32f, 1f));
                Sfx.Play("e4_whirl", 0.7f);
                var trail = GetComponent<EyeTrail>(); if (trail) trail.Hot(0.4f);
                if (p && p.health.Alive && Effects.Flat(p.transform.position - transform.position).magnitude <= WhirlRadius) p.health.TakeDamage(WhirlDmg);
                Streak("PendulumScythe", 0.34f);                                    // 한 바퀴 내내 낫에 붙은 칼빛 (예전: 몸 둘레 레이저 호 3장)
                PropHit.Area(transform.position, WhirlRadius, WhirlDmg);
                yield return new WaitForSeconds(0.7f);
            }
            nextAttack = Time.time + 0.4f;
            busy = false;
        }

        // ================= 시계 수리공 =================
        void RepairUpdate(PlayerController p, Vector3 dir, float dist, float spd)
        {
            if (busy) return;
            KeepRange(p, dir, dist, spd, RepairKeep - 1f, RepairKeep + 2f);
            if (Time.time >= nextSpecial && WindTargets(elite ? 2 : 1).Count > 0) { StartCoroutine(RepairWind()); return; }
            if (Time.time >= nextAttack && dist < 14f) StartCoroutine(RepairToss(p));
        }

        List<Enemy> WindTargets(int max)
        {
            var list = new List<Enemy>();
            foreach (var o in All)
                if (o && o != this && o.health.Alive && o.kind != Kind.Boss && !o.Wound && Effects.Flat(o.transform.position - transform.position).magnitude <= WindReach) list.Add(o);
            list.Sort((a, b) => Effects.Flat(a.transform.position - transform.position).sqrMagnitude.CompareTo(Effects.Flat(b.transform.position - transform.position).sqrMagnitude));
            if (list.Count > max) list.RemoveRange(max, list.Count - max);
            return list;
        }

        /// <summary>렌치를 대상 쪽으로 뻗어 손목을 돌림(0.6초, 연결 선이 이어짐) → 대상이 3초 빨라지고 체력 15% 회복</summary>
        IEnumerator RepairWind()
        {
            busy = true;
            nextSpecial = Time.time + WindEvery;
            var targets = WindTargets(elite ? 2 : 1);
            Anim("Wind");
            if (targets.Count > 0) Face(Effects.Flat(targets[0].transform.position - transform.position));
            var links = new List<WindLink>();
            foreach (var t in targets) links.Add(WindLink.Make(transform, t.transform, WindWindup + WindDur));
            bool ok = false;
            yield return Hold(WindWindup, r => ok = r);
            if (ok) { foreach (var t in targets) if (t && t.health.Alive) WindUp(t, transform.position); Sfx.Play("e4_wind", 0.55f); }
            else { foreach (var l in links) if (l) Destroy(l.gameObject); }
            yield return new WaitForSeconds(0.25f);
            busy = false;
        }

        static void WindUp(Enemy t, Vector3 from)
        {
            t.windUntil = Time.time + WindDur;
            t.health.Heal(t.health.maxHp * WindHeal);
            Spark(t.transform.position + Vector3.up * 1.6f, 1.1f);
        }

        /// <summary>렌치 곡사: 몸을 뒤로 젖혔다(0.3초) 위로 던짐 → 0.5초 날아가 원 자리에 떨어짐 (원 예고 0.8초 = 젖힘 + 비행)</summary>
        IEnumerator RepairToss(PlayerController p)
        {
            busy = true;
            Anim("Toss");
            var at = p.transform.position; at.y = transform.position.y;
            Telegraph.Ring(at, TossRadius, (TossRelease + TossFlight) / AtkMul, Telegraph.Danger);
            for (float w = 0; w < TossRelease / AtkMul; w += Time.deltaTime) { Face(Effects.Flat(at - transform.position)); yield return null; }
            if (health.Alive && !health.Stunned)
            {
                var prefab = Resources.Load<GameObject>("Kit/Ch4/W_Wrench");
                var go = prefab ? Instantiate(prefab) : GameObject.CreatePrimitive(PrimitiveType.Cube);
                foreach (var c in go.GetComponentsInChildren<Collider>()) Destroy(c);
                if (!prefab) go.transform.localScale = new Vector3(0.12f, 0.12f, 0.6f);
                ThrownWrench.Launch(go, transform.position + Vector3.up * 1.8f, at, TossFlight / AtkMul, TossRadius, projectileDamage);
                Sfx.Play("e4_toss", 0.5f);
            }
            yield return new WaitForSeconds(0.25f);
            nextAttack = Time.time + TossEvery * Random.Range(0.85f, 1.15f) / AtkMul;
            busy = false;
        }
    }

    /// <summary>던진 렌치: 빙글빙글 돌며 포물선으로 날아가 원 자리에 꽂힘 → 피해 + 레이저 불똥. 던진 적이 죽어도 끝까지 감</summary>
    public class ThrownWrench : MonoBehaviour
    {
        Vector3 from, to; float flight, radius, damage, t;

        public static void Launch(GameObject g, Vector3 from, Vector3 to, float flight, float radius, float damage)
        {
            var w = g.AddComponent<ThrownWrench>();
            w.from = from; w.to = to; w.flight = Mathf.Max(0.05f, flight); w.radius = radius; w.damage = damage;
            g.transform.position = from;
        }

        void Update()
        {
            t += Time.deltaTime / flight;
            if (t < 1f)
            {
                transform.position = Vector3.Lerp(from, to, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 2.6f;
                transform.Rotate(new Vector3(900f, 0, 0) * Time.deltaTime, Space.Self);
                return;
            }
            var p = PlayerController.Instance;
            if (p && p.health.Alive && Effects.Flat(p.transform.position - to).magnitude <= radius) p.health.TakeDamage(damage);
            FxFlipbook.Spawn("bk_c4spark", to + Vector3.up * 0.3f, Vector3.zero, 1.6f, 0.3f, false);
            Sfx.Play("e4_clank", 0.6f);
            FxBurst.Spawn(to + Vector3.up * 0.1f, 1.2f, FxBurst.Kind.Dust, FxBurst.MistDust);
            PropHit.Area(to, radius, damage);
            CameraFollow.Shake(0.04f);
            Destroy(gameObject);
        }
    }

    /// <summary>태엽 감기 연결 선: 수리공 렌치 끝 ↔ 대상 가슴, 레이저 띠 (흰 심지 → 붉은 꼬리). 둘 중 하나가 없어지면 사라짐</summary>
    public class WindLink : MonoBehaviour
    {
        Transform a, b; float until; LineRenderer lr;

        public static WindLink Make(Transform a, Transform b, float dur)
        {
            var go = new GameObject("WindLink");
            var w = go.AddComponent<WindLink>();
            w.a = a; w.b = b; w.until = Time.time + dur;
            w.lr = go.AddComponent<LineRenderer>();
            w.lr.sharedMaterial = LaserFx.StripMat();
            w.lr.textureMode = LineTextureMode.Stretch;
            w.lr.colorGradient = LaserFx.TailGradient();
            w.lr.widthMultiplier = 0.12f;
            w.lr.positionCount = 2;
            w.lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            w.lr.receiveShadows = false;
            return w;
        }

        void LateUpdate()
        {
            if (!a || !b || Time.time > until) { Destroy(gameObject); return; }
            lr.SetPosition(0, a.position + Vector3.up * 1.3f + a.forward * 0.5f);
            lr.SetPosition(1, b.position + Vector3.up * 1.2f);
            lr.widthMultiplier = 0.1f + 0.04f * Mathf.Sin(Time.time * 20f);       // 태엽이 감기듯 떨림
        }
    }
}
