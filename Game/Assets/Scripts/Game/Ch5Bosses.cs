// 5장 보스 '시간의 주인' (레벨 디자인 md '보스 상세 — 시간의 주인 (챕터 5)', 2026-11-20)
// - 1페이즈: 남은 시간 게이지 240초 (매초 줄어듦, 0이면 즉사). 낫에 맞으면 체력 대신 게이지가 깎임(시간 깎기), 모래시계 픽업(20초마다 2개)으로 되찾음
//   기믹(쿨 25초): ① 죽음의 시각 (얼굴 시계가 가리킨 시각 탄으로 맞히면 4초 기절, 다른 탄은 흡수돼 게이지 −3) ③ 12칸 시계 숫자 인장 (시각 탄을 쏜 상태로 그 인장 위 · 순서대로)
// - 2페이즈(체력 50%): 앞 장 보스 환영 소환 (녹은 시계 거인 · 멈춘 기관차 거인 · 거울 고래 · 시침 분침 쌍둥이, 체력 1500, 20초 뒤 퇴장), 보스는 가장자리에서 감시 빔 · 시간 역류
// - 3페이즈(체력 0): 죽지 않고 '마지막 1분' 버티기. 경기장이 15초마다 2m씩 무너짐, 모래시계 뒤집기(−5초) · 종소리 12번(울림에 맞춰 극한 회피 = −1초). 0초면 클리어
// 패턴: 대낫 베기 · 유령 돌진 베기(2026-11-20 사용자: 낫을 크게 뒤로 들었다가 앞으로 베어 넘기며 유령처럼 돌진, 가감속, 시계 카운트를 깎음) · 시간 정지장 · 종말의 탄 · 시계 바늘 창 · 모래폭풍
// 남은 시간은 보스 이름 줄(GameUI 보스 바)에 함께 표시 (DisplayName)
// 강력한 느낌 (사용자): 블렌더 부품(후광 문자판 · 어깨 갑옷 · 왕관 · 망토 조각, 빌더가 뼈에 붙임) + 아우라(StylizedFx 겹 + 공전하는 모래시계 · 톱니)
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    public class Ch5Boss : BossBrain
    {
        public override float NearDist => 6f;
        public override float PatternGap => phase == 3 ? 0.75f : phase == 2 ? 1.0f : 1.1f;
        public override string DisplayName => phase == 3 ? $"시간의 주인 · 마지막 {Mathf.CeilToInt(Mathf.Max(0, survive))}초"
                                                       : $"시간의 주인 · 남은 시간 {(int)(gauge / 60)}:{(int)(gauge % 60):00}";

        // ---------- 수치 (md) ----------
        public const float GaugeStart = 240f, HourglassEvery = 20f, HourglassGain = 20f, GimmickFirst = 12f, GimmickEvery = 25f, StunTime = 4f;
        public const float ReapRange = 6f, ReapWindup = 0.9f, ReapCut = 10f;
        public const float GhostWind = 1.1f, GhostDash = 11f, GhostDashTime = 0.42f, GhostWidth = 3.2f, GhostCut = 15f;
        public const float StopR = 4f, StopDur = 3f, StopSlow = 0.6f;
        public const float DoomSpeed = 5f, DoomHoming = 60f, DoomLife = 5f, DoomDmg = 34f;
        public const float SpearAim = 1f, SpearSpeed = 20f, SpearDmg = 36f;
        public const float SandR = 4f, SandDur = 3f, SandDps = 10f;
        public const float BeamEvery = 3f, BeamAim = 0.8f, BeamDmg = 28f, RewindDmg = 25f;
        public const float GhostHp = 1500f, GhostLife = 20f;
        public const float SurviveTime = 60f, ShrinkEvery = 15f, ShrinkStep = 2f, ArenaMin = 9f, FlipEvery = 15f, BellEvery = 20f, BellDmg = 30f;

        public static readonly Color Gold = new(1f, 0.78f, 0.3f), Magenta = new(1f, 0.18f, 0.7f), Violet = new(0.5f, 0.3f, 0.95f);

        int phase = 1;
        float gauge = GaugeStart, survive = SurviveTime, arenaR = Ch5Kit.ArenaR;
        float nextHourglass, nextGimmick, nextGhost, nextShrink, nextFlip, nextBell, nextEcho;
        // 잔상 (맵 패턴): 1페이즈 6~7초 · 2페이즈 9초 · 3페이즈 4.5초마다 한 마리, 무작위 방향에서 → 바닥 예고 없이 유령 돌진 베기 (맞으면 −8초)
        public const float EchoFirst = 8f, EchoTurn = 1.1f, EchoLock = 0.25f, EchoDash = 10f, EchoDashTime = 0.32f, EchoCut = 8f;
        float EchoEvery => phase == 3 ? 4.5f : phase == 2 ? 9f : Random.Range(6f, 7f);
        bool inGimmick, gimmickHit;
        int needHour; string lastGimmick;
        int lastFiredSlot; float lastFiredAt = -9f;
        readonly List<BossGhost> ghosts = new();
        readonly List<string> ghostOrder = new();
        readonly List<(float t, Vector3 p)> trail = new();          // 시간 역류: 고양이 지난 3초 자리
        Transform model;
        LineRenderer edge;                                          // 3페이즈 무너지는 경계 원
        float bellDodgeWindow;

        Vector3 C => Ch3Fx.ArenaC();

        // 낫 섬광 리본 (2026-11-20 사용자: 리본 형태 · 섬광에 가깝게): 낫 기준 안쪽(자루 위) · 날 끝 (Kit_W_TimeScythe: 원점 = 손잡이, 자루 +Y, 날은 −X)
        static readonly Vector3 ScytheInner = new(0f, 1.3f, 0f), ScytheOuter = new(-1.45f, 1.95f, 0f);
        Transform scythe;
        Transform Scythe() { if (!scythe && model) foreach (var t in model.GetComponentsInChildren<Transform>(true)) if (t.name == "Weapon_TimeScythe") { scythe = t; break; } return scythe; }
        // 대낫을 코드로 쥐는 조종기 (2026-11-20 사용자: 최종 보스 낫 휘두르기도 크게 · 가감속 · 랫키호테 칼빛 붙이는 방식)
        WeaponSwingRig srig;
        WeaponSwingRig SRig()
        {
            if (!srig && self) { srig = WeaponSwingRig.Get(self, "TimeScythe", new Color(1f, 0.18f, 0.72f), true); if (srig) { srig.streakTip = ScytheOuter; srig.streakGrip = ScytheInner; srig.streakExtend = 1.25f; } }
            return srig;
        }
        void ScytheFlash(float dur) { if (!FlashRibbon.Begin(Scythe(), ScytheInner, ScytheOuter, dur, StylizedFx.Magenta, 0.18f)) StylizedFx.Slash(transform.position, Effects.Flat(transform.forward), ReapRange, StylizedFx.Magenta, 1.4f); }

        protected override void Setup()
        {
            Add("대낫 베기", Reach.Near, Reap180, () => phase != 2, "Reap");
            Add("유령 돌진 베기", Reach.Any, GhostReap, () => phase != 2, "ReapWind");
            Add("시간 정지장", Reach.Near, TimeStop, () => phase != 2, "Cast");
            Add("종말의 탄", Reach.Far, DoomOrbs, () => phase != 2, "Cast");
            Add("시계 바늘 창", Reach.Far, HandSpear, () => phase != 2, "Spear");
            Add("모래폭풍", Reach.Any, Sandstorm, () => phase != 2, "Cast");
            Add("최후의 째깍", Reach.Any, FinalTick, () => phase == 3, "Reap");
            Add("감시 빔", Reach.Any, WatchBeam, () => phase == 2, "Spear");
            Add("시간 역류", Reach.Any, Rewind, () => phase == 2, "Cast");
            nextHourglass = Time.time + 6f;
            nextGimmick = Time.time + GimmickFirst;
            nextEcho = Time.time + EchoFirst;
        }

        void Start()
        {
            hp.minHp = 1f;                                           // 체력 0 = 3페이즈로 (죽지 않음)
            model = transform.Find("Model");
            if (P) { P.OnFired += OnFired; P.health.OnJustDodge += OnJustDodge; }
            BossAura.Attach(this);
            BossFx.Toast("시간의 주인 — 남은 시간이 0이 되기 전에!");
            Sfx.Play("e5b_summon", 0.8f);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (P) { P.OnFired -= OnFired; P.health.OnJustDodge -= OnJustDodge; }
            foreach (var g in ghosts) if (g) Destroy(g.gameObject);
            if (edge) Destroy(edge.gameObject);
        }

        void OnFired(int slot, bool awake)
        {
            lastFiredSlot = slot; lastFiredAt = Time.time;
            if (needHour == 0 || gimmickHit) return;
            if (Time.time - hp.lastHitAt > 0.001f) return;          // 이번 탄이 보스에 맞은 게 아님
            if (slot == needHour) gimmickHit = true;
            else { CutTime(3f, false); FxBurst.Spawn(transform.position + Vector3.up * 6f, 1.2f, FxBurst.Kind.Spark, Magenta); }   // 흡수 → 시간 −3
        }

        void OnJustDodge()
        {
            if (phase == 3 && Time.time < bellDodgeWindow) { survive -= 1f; bellDodgeWindow = 0; FxBurst.Spawn(P.transform.position + Vector3.up, 1.6f, FxBurst.Kind.Gold, Gold); }
        }

        // ---------- 시간 깎기 ----------
        /// <summary>고양이가 실제로 맞았는지 (대시 저스트 회피 · 확률 회피 · 무적이면 false): 피해 1을 넣어 보고 체력이 줄었는지로 판정</summary>
        static bool Landed(PlayerController p)
        {
            if (!p || !p.health.Alive) return false;
            float before = p.health.hp;
            p.health.TakeDamage(1f);
            return p.health.hp < before;
        }

        /// <summary>남은 시간을 깎음 (3페이즈엔 버틸 시간이 늘어남 = 되감김). show = 고양이 머리 위 −n초</summary>
        void CutTime(float sec, bool show = true)
        {
            if (phase == 3) survive = Mathf.Min(SurviveTime, survive + sec * 0.3f);
            else gauge = Mathf.Max(0f, gauge - sec);
            if (show && P) { var b = Label(null, P.transform.position + Vector3.up * 3f, $"-{sec:0}초", Magenta, 1.2f); if (b) Destroy(b.gameObject, 1.1f); }
            Sfx.Play("e5b_time_cut", 0.7f);
            CameraFollow.Shake(0.08f);
        }

        // ---------- 이동 ----------
        protected override void Move(PlayerController p, Vector3 dir, float dist, float spd)
        {
            if (phase == 2)
            {
                // 가장자리에서 감시: 고양이 반대쪽 가장자리로 미끄러지듯
                var want = C - Effects.Flat(p.transform.position - C).normalized * (arenaR - 4f);
                transform.position = Vector3.MoveTowards(transform.position, new Vector3(want.x, transform.position.y, want.z), 5f * Time.deltaTime);
                return;
            }
            float s = phase == 3 ? 4.5f : 3.5f;
            if (dist > NearDist * 0.7f) transform.position += dir * s * hp.SlowMul * Time.deltaTime;
            KeepIn();
        }

        void KeepIn()
        {
            var rel = Effects.Flat(transform.position - C);
            if (rel.magnitude > arenaR - 2.5f) transform.position = C + rel.normalized * (arenaR - 2.5f) + Vector3.up * transform.position.y;
        }

        // ---------- 상시 ----------
        protected override void Always(PlayerController p, Vector3 dir, float dist)
        {
            if (!p) return;
            trail.Add((Time.time, p.transform.position));
            trail.RemoveAll(x => Time.time - x.t > 3f);
            if (phase < 3)
            {
                if (!hp.Stunned) gauge -= Time.deltaTime;
                if (gauge <= 0f && p.health.Alive) { BossFx.Toast("시간이 다 됐다…"); p.health.invulnerable = false; p.health.TakeDamage(p.health.maxHp * 10f); }
                if (Time.time >= nextHourglass) { nextHourglass = Time.time + HourglassEvery; for (int i = 0; i < 2; i++) HourglassPickup.Spawn(RandomSpot(5f), this); }
            }
            if (Time.time >= nextEcho && !hp.Stunned) { nextEcho = Time.time + EchoEvery; StartCoroutine(Echo()); }   // 잔상 맵 패턴 (보스 패턴과 따로 돎)
            if (phase == 1 && hp.hp <= hp.maxHp * 0.5f) EnterPhase2();
            if (phase == 2 && hp.hp <= hp.minHp + 0.5f) EnterPhase3();
            if (phase == 3)
            {
                survive -= Time.deltaTime;
                if (Time.time >= nextShrink && arenaR > ArenaMin) { nextShrink = Time.time + ShrinkEvery; StartCoroutine(Shrink()); }
                DrawEdge();
                var rel = Effects.Flat(p.transform.position - C);
                if (rel.magnitude > arenaR - 0.3f) { p.health.TakeDamage(10f * Time.deltaTime * 8f); p.health.Knockback(-rel.normalized * 6f); }
                if (survive <= 0f) Win();
            }
        }

        Vector3 RandomSpot(float minFromBoss)
        {
            for (int k = 0; k < 20; k++)
            {
                var at = C + Quaternion.Euler(0, Random.Range(0f, 360f), 0) * Vector3.forward * Random.Range(3f, arenaR - 3f);
                if (Effects.Flat(at - transform.position).magnitude > minFromBoss) return at;
            }
            return C;
        }

        // ---------- 기믹 스케줄 ----------
        protected override System.Func<IEnumerator> Forced()
        {
            if (inGimmick) return null;
            if (phase == 1 && Time.time >= nextGimmick)
            {
                lastGimmick = lastGimmick == "hour" ? "seal" : "hour";
                return () => Gimmick(lastGimmick == "hour" ? DeathHour() : NumberSeals(), GimmickEvery);
            }
            if (phase == 2 && Time.time >= nextGhost && ghosts.TrueForAll(g => !g))
                return () => Gimmick(SummonGhost(), 0f);
            if (phase == 3)
            {
                if (Time.time >= nextFlip) { nextFlip = Time.time + FlipEvery; return () => Gimmick(HourglassFlip(), 0f); }
                if (Time.time >= nextBell) { nextBell = Time.time + BellEvery; return () => Gimmick(Bell12(), 0f); }
            }
            return null;
        }

        IEnumerator Gimmick(IEnumerator inner, float cd)
        {
            inGimmick = true;
            yield return inner;
            inGimmick = false;
            if (cd > 0) nextGimmick = Time.time + cd;
        }

        void Stun(float dur)
        {
            hp.ApplyStun(dur);
            StylizedFx.Shockwave(transform.position, 6f, StylizedFx.Gold, 0.6f);
            Sfx.Play("e5b_seal", 0.7f);
        }

        // ================= 잔상 (맵 패턴) =================
        /// <summary>
        /// (2026-11-20 사용자: 일정 시간마다 보라 잔상이 나와 때림 — 발탄처럼 모션 · 이펙트, 끊는 기믹 아님, 한 마리씩, 지속적으로 등장하는 맵 패턴, 텔레그래프 없이 유령을 보고 피함)
        /// 고양이 둘레 무작위 방향(보스 쪽 ±45° 제외) · 6~9m 허공 균열에서 보라 유령 잔상 하나가 튀어나와 낫을 수평으로 감은 채 고양이 쪽으로 몸을 돌림(1.1초)
        /// → 베기 0.25초 전에 방향 고정 → 10m 유령 돌진으로 수평 베어 넘김(가감속 · 잔상 · 낫 섬광 리본). 맞으면 시간 −8초. 바닥 예고 없음
        /// </summary>
        IEnumerator Echo()
        {
            var p = P; if (!p || !model) yield break;
            var bossAng = Mathf.Atan2(transform.position.x - p.transform.position.x, transform.position.z - p.transform.position.z) * Mathf.Rad2Deg;
            float ang = Random.Range(0f, 360f);
            for (int tries = 0; tries < 20 && Mathf.Abs(Mathf.DeltaAngle(ang, bossAng)) < 45f; tries++) ang = Random.Range(0f, 360f);
            var at = Effects.Flat(p.transform.position) + Quaternion.Euler(0, ang, 0) * Vector3.forward * Random.Range(6f, 9f);
            var rel = Effects.Flat(at - C); if (rel.magnitude > arenaR - 2f) at = C + rel.normalized * (arenaR - 2f);
            var body = Own(Instantiate(model.gameObject));
            body.transform.SetPositionAndRotation(at, Quaternion.LookRotation(Effects.Flat(p.transform.position - at)));
            body.transform.localScale = model.lossyScale * 0.8f;
            // 자세가 읽히게 장식 부품(후광 · 왕관 · 어깨 · 망토 조각)은 빼고 몸 + 낫만
            foreach (var tr in body.GetComponentsInChildren<Transform>(true)) if (tr && tr.name.StartsWith("Armor_")) Destroy(tr.gameObject);
            foreach (var c in body.GetComponentsInChildren<Collider>()) Destroy(c);
            foreach (var mb in body.GetComponentsInChildren<MonoBehaviour>()) if (mb) mb.enabled = false;
            BossGhost.MakeGhostly(body, new Color(0.62f, 0.32f, 1f), 0.45f);
            var an = body.GetComponentInChildren<Animator>();
            if (an) an.CrossFadeInFixedTime("ReapWind", 0.1f);
            Enemy.Fx5("c5rift", at + Vector3.up * 2f, 2.4f, 0.4f);                   // 허공 균열에서 튀어나옴
            StylizedFx.Tornado(at, 1.8f, 4.5f, 0.15f, StylizedFx.Void);
            Sfx.Play("e5_rift_emerge", 0.55f, 0.05f, 2);
            // 몸을 돌리며 감음 (마지막 0.25초는 방향 고정)
            var d = Effects.Flat(p.transform.position - at).normalized;
            for (float t = 0; t < EchoTurn && body; t += Time.deltaTime)
            {
                if (p && t < EchoTurn - EchoLock) d = Vector3.Slerp(d, Effects.Flat(p.transform.position - body.transform.position).normalized, 6f * Time.deltaTime).normalized;
                body.transform.rotation = Quaternion.LookRotation(d);
                yield return null;
            }
            if (!body) yield break;
            // 유령 돌진 베기
            if (an) an.CrossFadeInFixedTime("ReapDash", 0.05f);
            Sfx.Play("e5b_reap_dash", 0.65f, 0.05f, 2);
            Transform w = null; foreach (var tr in body.GetComponentsInChildren<Transform>(true)) if (tr.name == "Weapon_TimeScythe") { w = tr; break; }
            FlashRibbon.Begin(w, ScytheInner, ScytheOuter, EchoDashTime + 0.06f, StylizedFx.Magenta, 0.16f);
            var from = body.transform.position; var to = from + d * EchoDash;
            bool landed = false; float nextImg = 0;
            for (float t = 0; t < 1f && body; t += Time.deltaTime / EchoDashTime)
            {
                float k = t * t * t * (t * (t * 6 - 15) + 10);                      // 가속 → 감속
                body.transform.position = Vector3.Lerp(from, to, k);
                if (Time.time >= nextImg) { nextImg = Time.time + 0.04f; Afterimage.Spawn(body.transform, new Color(0.8f, 0.3f, 1f, 0.5f), 0.28f, false); }
                if (!landed && p && Telegraph.InLane(p.transform.position, from, d, EchoDash + 1f, GhostWidth * 0.8f, k * EchoDash - 1.8f, k * EchoDash + 2f))
                {
                    landed = true;
                    if (Landed(p)) { CutTime(EchoCut); p.health.Knockback(d * 7f); }
                }
                yield return null;
            }
            if (!body) yield break;
            StylizedFx.Shockwave(body.transform.position, 2.6f, StylizedFx.Void, 0.3f);
            // 베어 넘긴 자세로 잠깐 머물다 흩어짐
            yield return new WaitForSeconds(0.25f);
            if (body) { Enemy.Fx5("c5spark", body.transform.position + Vector3.up * 2f, 1.6f, 0.3f); Destroy(body); }
        }

        // ================= 1페이즈 기믹 =================
        static string Roman(int h) => new[] { "", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X", "XI", "XII" }[h];

        /// <summary>① 죽음의 시각: 얼굴 시계가 한 시각을 가리킴 → 8초 안에 그 시각 탄으로 보스를 맞히면 4초 기절. 다른 탄은 흡수 (시간 −3)</summary>
        IEnumerator DeathHour()
        {
            var p = P; if (!p) yield break;
            int h; do h = Random.Range(1, 13); while (h == p.rev.next);
            needHour = h; gimmickHit = false;
            BossFx.Toast($"죽음의 시각 — {h}시 탄으로 얼굴 시계를 쏴라!");
            Sfx.Play("e5b_bell", 0.6f);
            var lb = Label(transform, new Vector3(0, 7f, 0), Roman(h), Gold, 2.2f);
            var dome = StylizedFx.Dome(transform.position, 3.2f, 8f, StylizedFx.Gold);
            float end = Time.time + 8f;
            while (Time.time < end && !gimmickHit && hp.Alive) { if (lb) lb.text = $"{Roman(h)}  {Mathf.CeilToInt(end - Time.time)}"; yield return null; }
            needHour = 0;
            if (lb) Destroy(lb.gameObject);
            if (dome) dome.Release();
            if (gimmickHit) { BossFx.Toast("시계가 깨졌다 — 시간의 주인이 멈췄다!"); StylizedFx.Burst(transform.position + Vector3.up * 5.5f, 4f, StylizedFx.Gold); Stun(StunTime); yield return Wait(StunTime); }
            else { BossFx.Toast("시각을 놓쳤다 — 시간이 깎인다"); CutTime(15f); }
        }

        /// <summary>③ 12칸 시계 숫자 인장: 바닥에 숫자 인장 3개 → 순서대로, 그 숫자 시각 탄을 쏜 뒤(1.5초 안) 인장 위에 서야 봉인. 15초. 성공 4초 기절 · 실패 남은 인장당 최대 체력 10%</summary>
        IEnumerator NumberSeals()
        {
            var p = P; if (!p) yield break;
            var nums = new List<int>();
            while (nums.Count < 3) { int n = Random.Range(1, 13); if (!nums.Contains(n)) nums.Add(n); }
            var spots = new List<Vector3>(); var marks = new List<GameObject>();
            for (int i = 0; i < 3; i++)
            {
                var at = RandomSpot(4f); at.y = 0;
                spots.Add(at);
                var m = Own(new GameObject("Seal_" + nums[i]));
                m.transform.position = at;
                Label(m.transform, new Vector3(0, 1.6f, 0), $"{i + 1}. {Roman(nums[i])}", i == 0 ? Gold : new Color(0.75f, 0.75f, 0.85f), 1.4f);
                marks.Add(m);
                StylizedFx.Spawn("ring", at + Vector3.up * 0.05f, Quaternion.identity, Vector3.one * 1.6f, Vector3.one * 1.6f, 0.3f, StylizedFx.Gold, 40f, 15f, 0.05f);
            }
            BossFx.Toast("숫자 인장 — 그 시각 탄을 쏘고 인장 위에 서라 (순서대로)");
            int done = 0; float end = Time.time + 15f;
            while (Time.time < end && done < 3 && hp.Alive)
            {
                var at = spots[done];
                bool on = Effects.Flat(p.transform.position - at).magnitude <= 1.6f;
                if (on && lastFiredSlot == nums[done] && Time.time - lastFiredAt < 1.5f)
                {
                    StylizedFx.Shockwave(at, 2.4f, StylizedFx.Gold, 0.4f);
                    Sfx.Play("e5b_seal", 0.6f);
                    if (marks[done]) Destroy(marks[done]);
                    done++;
                    if (done < 3 && marks[done]) foreach (var b in FindObjectsByType<BigLabel>(FindObjectsSortMode.None)) if (b.target == marks[done].transform) b.color = Gold;
                }
                yield return null;
            }
            foreach (var m in marks) if (m) Destroy(m);
            if (done >= 3) { BossFx.Toast("봉인 — 시간의 주인이 묶였다!"); Stun(StunTime); yield return Wait(StunTime); }
            else { BossFx.Toast("봉인 실패"); if (p.health.Alive) p.health.TakeDamage(p.health.maxHp * 0.1f * (3 - done)); }
        }

        // ================= 패턴 =================
        /// <summary>대낫 베기: 낫을 옆으로 감음(예고 0.9) → 전방 180° 6m 큰 호. 맞으면 체력 대신 시간 −10초</summary>
        IEnumerator Reap180()
        {
            var d = ToPlayer();
            transform.rotation = Quaternion.LookRotation(d);
            Sfx.Play("e5b_reap_wind", 0.5f);
            // 낫을 오른쪽 뒤로 크게 감고(몸을 젖힘) → 멈칫 → 240° 수평 호로 베고 감속. 예고 · 판정 = 칼빛이 쓸고 가는 부채꼴 (조종기가 없으면 예전 180° · ReapRange)
            var rig = SRig();
            float wu = T(ReapWindup), act = 0.3f, wh = 0.08f;   // (사용자: 조금 느리게)
            Vector3 rc = d; float rHalf = 90f, rR = ReapRange;
            if (rig)
            {
                var sw = new WeaponSwingRig.Swing
                {
                    axis = Vector3.up, from = new Vector3(0.82f, -0.04f, -0.57f), sweep = -250f,   // 낫을 눕혀 수평으로 크게 (사용자: 눕혀서 크게)
                    wind = Mathf.Max(0.15f, wu - act - wh), windHold = wh, act = act, actPow = 2.4f, hold = 0.25f, rec = 0.6f,
                    bodyFollow = rig.BodyOnly ? 0.75f : 0.15f, leanBack = 10f, leanFwd = 14f, lunge = 0.8f, streak = 1f,
                };
                if (!rig.BodyOnly) (rc, rHalf, rR) = rig.Sector(sw, d);
                rig.Play(sw);
            }
            var t = Telegraph.Sector(transform.position, rc, rR, rHalf, wu, Telegraph.Danger, transform).NoLaser();
            yield return Wait(wu);
            if (t) t.Kill();
            if (rig) rig.Impact(transform.position + d * rR * 0.55f, 1.3f, StylizedFx.Magenta); else ScytheFlash(0.22f);
            ReaperFx.BigSlash(transform.position, d, rR, true);   // (2026-11-20 사용자) 최종 보스 포스: 잉크 연기 · 깃털 · 불티 겹 베기
            Sfx.Play("e5b_reap_dash", 0.7f, 0.05f, 2);
            var p = P;
            if (p)
            {
                var to = Effects.Flat(p.transform.position - transform.position);
                if (WeaponSwingRig.InSector(p.transform.position, transform.position, rc, rHalf, rR) && Landed(p)) CutTime(ReapCut);
            }
            PropHit.Area(transform.position + d * 3f, 3f, 40f);
            yield return Wait(T(0.5f));
        }

        /// <summary>
        /// 유령 돌진 베기 (2026-11-20 사용자): 낫을 머리 뒤로 크게 들어 올림 — 처음엔 천천히, 꼭대기에서 잠깐 멈칫(가속을 모으는 예비 동작) →
        /// 앞으로 베어 넘기며 몸이 반투명 유령이 되어 11m 일직선 돌진 (속도 = 천천히 출발 → 한가운데 최고 → 끝에서 미끄러지며 감속) →
        /// 지나간 자리에 자홍 초승달 궤적, 맞으면 시간 −15초. 모션 클립 Ch5M_BossReapWind · Ch5M_BossReapDash도 같은 가감속 박자
        /// </summary>
        IEnumerator GhostReap()
        {
            var p = P; if (!p) yield break;
            var d = ToPlayer();
            float wind = T(GhostWind);
            // 길 폭 = 낫 칼빛이 쓸고 가는 폭 (돌진하며 낫이 몸 둘레를 크게 돎 → 칼빛 반지름 × 2). 예고 · 판정 같은 값
            var gr = SRig();
            float gw = gr && !gr.BodyOnly ? gr.EffectRadius(1.1f) * 2f : GhostWidth;
            var from = transform.position;
            var tele = Telegraph.Lane(from, d, GhostDash + 1f, gw, wind, Telegraph.Danger);
            Sfx.Play("e5b_reap_wind", 0.7f);
            var aura = StylizedFx.Spawn("swirl", from, Quaternion.identity, new Vector3(2f, 1.5f, 2f), new Vector3(3.2f, 4.5f, 3.2f), 0.3f, StylizedFx.Void, 300f, wind, 0.1f);
            // 예고 동안 방향은 앞 75%만 따라감 (마지막 순간엔 고정 → 옆으로 피할 틈)
            for (float t = 0; t < wind; t += Time.deltaTime)
            {
                if (hp.Stunned) { if (tele) tele.Kill(); if (aura) aura.Release(); yield break; }
                if (t < wind * 0.75f && p)
                {
                    d = Vector3.Slerp(d, ToPlayer(), 3f * Time.deltaTime).normalized;
                    if (tele) tele.Kill();
                    tele = Telegraph.Lane(transform.position, d, GhostDash + 1f, gw, wind - t, Telegraph.Danger);
                }
                transform.rotation = Quaternion.LookRotation(d);
                yield return null;
            }
            if (tele) tele.Kill();
            if (aura) aura.Release(0.2f);
            // 돌진: 유령 (몸 반투명 + 잔상 + 자홍 초승달), 가감속 = smootherstep
            self.Anim("ReapDash");
            Sfx.Play("e5b_reap_dash", 0.85f, 0.04f, 2);
            from = transform.position;
            var to = from + d * GhostDash;
            var rm = RunManager.Instance;
            var rel = Effects.Flat(to - C); if (rel.magnitude > arenaR - 2.5f) to = C + rel.normalized * (arenaR - 2.5f);
            float len = Effects.Flat(to - from).magnitude;
            Ghostly(true);
            // 돌진하며 오른쪽 뒤에서 앞을 지나 왼쪽 뒤까지 280° 베어 넘김 (돌진과 같은 시간, 처음 빠르게 → 끝에서 미끄러지며 감속)
            var rig = SRig();
            if (rig) rig.Play(new WeaponSwingRig.Swing
            {
                axis = Vector3.up, from = new Vector3(0.88f, -0.04f, -0.47f), sweep = -280f,
                wind = 0.06f, windHold = 0f, act = T(GhostDashTime), actPow = 2f, hold = 0.2f, rec = 0.5f,
                bodyFollow = rig.BodyOnly ? 0.6f : 0.1f, leanBack = 6f, leanFwd = 18f, streak = 1.1f,
            });
            else ScytheFlash(T(GhostDashTime) + 0.12f);                          // 조종기가 없으면 예전 섬광 리본
            bool hit = false; float nextImg = 0;
            for (float t = 0; t < 1f; t += Time.deltaTime / T(GhostDashTime))
            {
                float k = t * t * t * (t * (t * 6 - 15) + 10);                      // smootherstep: 가속 → 감속
                transform.position = Vector3.Lerp(from, to, k);
                if (Time.time >= nextImg && model) { nextImg = Time.time + 0.035f; Afterimage.Spawn(model, new Color(0.8f, 0.3f, 1f, 0.55f), 0.32f, false); }
                float mv = k * len;
                if (!hit && p && p.health.Alive && Telegraph.InLane(p.transform.position, from, d, GhostDash + 1f, gw, mv - 2f, mv + 2.5f))
                {
                    hit = true;
                    if (Landed(p)) { CutTime(GhostCut); p.health.Knockback(d * 9f); StylizedFx.Burst(p.transform.position + Vector3.up, 2.5f, StylizedFx.Magenta); }
                }
                yield return null;
            }
            transform.position = to;
            ReaperFx.DashEnd(to, d);                                                 // 멈춘 자리 잉크 연기 폭발 · 깃털 · 불티 (예전: Void 충격 고리 + 흔들림 0.15)
            PropHit.Area(to, 3f, 40f);
            Ghostly(false);
            yield return Wait(T(0.6f));                                              // 베어 넘긴 자세로 미끄러져 멈춘 빈틈
        }

        Renderer[] rends; Material[][] solidMats; Material ghostMat;
        /// <summary>돌진 동안 몸을 반투명 유령으로 (CatTang/MirrorGhost 셰이더, 보랏빛 · 금 테두리)</summary>
        void Ghostly(bool on)
        {
            if (!model) return;
            if (rends == null)
            {
                rends = model.GetComponentsInChildren<Renderer>(true);
                solidMats = new Material[rends.Length][];
                for (int i = 0; i < rends.Length; i++) solidMats[i] = rends[i].sharedMaterials;
                var sh = Shader.Find("CatTang/MirrorGhost");
                if (sh)
                {
                    ghostMat = new Material(sh);
                    ghostMat.SetColor("_Tint", new Color(0.7f, 0.4f, 1f));
                    ghostMat.SetFloat("_TintAmount", 0.6f);
                    ghostMat.SetFloat("_Alpha", 0.45f);
                    ghostMat.SetColor("_RimColor", Gold);
                }
            }
            if (!ghostMat) return;
            for (int i = 0; i < rends.Length; i++)
            {
                if (!rends[i] || rends[i] is ParticleSystemRenderer || rends[i] is LineRenderer || rends[i] is TrailRenderer) continue;
                if (on)
                {
                    var mats = new Material[solidMats[i].Length];
                    for (int k = 0; k < mats.Length; k++)
                    {
                        mats[k] = new Material(ghostMat);
                        var src = solidMats[i][k];
                        if (src && src.HasProperty("_BaseMap")) mats[k].SetTexture("_BaseMap", src.GetTexture("_BaseMap"));
                    }
                    rends[i].sharedMaterials = mats;
                }
                else rends[i].sharedMaterials = solidMats[i];
            }
        }

        /// <summary>시간 정지장: 고양이 둘레 반경 4m 금빛 돔 3초 — 안에선 60% 느려짐</summary>
        IEnumerator TimeStop()
        {
            var p = P; if (!p) yield break;
            var at = Effects.Flat(p.transform.position);
            Telegraph.Ring(at, StopR, T(0.6f), Telegraph.Danger).Grow();
            yield return Wait(T(0.6f));
            Sfx.Play("e5b_stop", 0.7f);
            var dome = StylizedFx.Dome(at, StopR, StopDur, StylizedFx.Void.With(pattern: 1, opacity: 0.35f));   // 해로운 장판 = 보라 (금은 이로운 것만, 2026-11-20 사용자: 금빛 고리가 뭘 뜻하는지 헷갈림)
            float end = Time.time + StopDur;
            while (Time.time < end)
            {
                if (p && Effects.Flat(p.transform.position - at).magnitude <= StopR) p.health.ApplySlow(StopSlow, 0.15f);
                yield return null;
            }
            if (dome) dome.Release();
        }

        /// <summary>종말의 탄: 큰 보랏빛 구슬 3개 (5m/초, 초당 60° 유도, 5초)</summary>
        IEnumerator DoomOrbs()
        {
            Sfx.Play("e5b_doom_orb", 0.7f);
            var hand = transform.position + Vector3.up * 4f + transform.forward * 1.5f;
            StylizedFx.Spawn("spiral_ball", hand, Quaternion.identity, Vector3.one * 0.5f, Vector3.one * 2.2f, 0.6f, StylizedFx.Void.With(pattern: 2, scale: new Vector2(3, 1), scroll: new Vector2(-5f, 0), fresnel: 1.2f), 400f);
            yield return Wait(T(0.6f));
            for (int i = -1; i <= 1; i++)
            {
                var dir = Quaternion.Euler(0, i * 35f, 0) * ToPlayer();
                var o = EnemyProjectile.Spawn(transform.position + Vector3.up * 1.4f + dir * 2f, dir, DoomSpeed, DoomDmg, DoomSpeed * DoomLife, EnemyProjectile.Style.Orb);
                o.homing = DoomHoming;
            }
            yield return Wait(T(0.4f));
        }

        /// <summary>시계 바늘 창: 조준선 1초 → 20m/초 바늘 창</summary>
        IEnumerator HandSpear()
        {
            var d = ToPlayer();
            Telegraph tl = null;
            for (float t = 0; t < T(SpearAim); t += Time.deltaTime)
            {
                if (t < T(SpearAim) * 0.7f) d = Vector3.Slerp(d, ToPlayer(), 6f * Time.deltaTime).normalized;
                if (tl) tl.Kill();
                tl = Telegraph.Lane(transform.position + d, d, 22f, 0.5f, 0.05f, t < T(SpearAim) * 0.7f ? new Color(1f, 0.5f, 0.86f) : Telegraph.Danger);
                transform.rotation = Quaternion.LookRotation(d);
                yield return null;
            }
            if (tl) tl.Kill();
            Sfx.Play("e5b_spear", 0.7f);
            Shoot(d, SpearSpeed, SpearDmg, 1.6f, 30f, EnemyProjectile.Style.Sniper);
            StylizedFx.Spawn("spike_burst", transform.position + Vector3.up * 1.6f + d * 2f, Quaternion.LookRotation(d), Vector3.one * 0.6f, Vector3.one * 2f, 0.25f, StylizedFx.Magenta);
            yield return Wait(T(0.4f));
        }

        /// <summary>모래폭풍: 보스 둘레 반경 4m 금빛 회오리 3초, 초당 10 + 밀어냄</summary>
        IEnumerator Sandstorm()
        {
            Telegraph.Ring(transform.position, SandR, T(0.7f), Telegraph.Danger, transform);
            yield return Wait(T(0.7f));
            Sfx.Play("e5b_sand", 0.7f);
            StylizedFx.Tornado(transform.position, SandR, 6f, SandDur, StylizedFx.Magenta);
            float end = Time.time + SandDur;
            moveWhileBusy = true;
            while (Time.time < end)
            {
                var p = P;
                if (p && p.health.Alive)
                {
                    var rel = Effects.Flat(p.transform.position - transform.position);
                    if (rel.magnitude <= SandR) { p.health.TakeDamage(SandDps * Time.deltaTime); p.health.Knockback(rel.normalized * 4f * Time.deltaTime * 10f); }
                }
                yield return null;
            }
            moveWhileBusy = false;
        }

        /// <summary>최후의 째깍 (3페이즈): 12방향 낫 파동 4번 (0.5초 간격, 30)</summary>
        IEnumerator FinalTick()
        {
            for (int w = 0; w < 4; w++)
            {
                Sfx.Play("e4_tick", 0.6f);
                StylizedFx.Shockwave(transform.position, 3f, StylizedFx.Magenta, 0.3f);
                for (int k = 0; k < 12; k++)
                {
                    var dir = Quaternion.Euler(0, k * 30f + w * 15f, 0) * Vector3.forward;
                    Shoot(dir, 9f, BellDmg, 1.2f, 30f, EnemyProjectile.Style.Needle);
                }
                yield return Wait(T(0.5f));
            }
        }

        // ================= 2페이즈 =================
        void EnterPhase2()
        {
            phase = 2;
            BossFx.Toast("시간의 주인이 지난 시간을 불러낸다!");
            Sfx.Play("e5b_phase", 0.8f);
            CameraFollow.Shake(0.3f);
            StylizedFx.Shockwave(transform.position, 12f, StylizedFx.Void, 0.8f);
            ghostOrder.Clear();
            ghostOrder.AddRange(new[] { "melt", "loco", "whale", "twins" });
            for (int i = ghostOrder.Count - 1; i > 0; i--) { int j = Random.Range(0, i + 1); (ghostOrder[i], ghostOrder[j]) = (ghostOrder[j], ghostOrder[i]); }
            nextGhost = Time.time + 1.5f;
        }

        IEnumerator SummonGhost()
        {
            if (ghostOrder.Count == 0) ghostOrder.AddRange(new[] { "melt", "loco", "whale", "twins" });
            var kind = ghostOrder[0]; ghostOrder.RemoveAt(0);
            self.Anim("Cast");
            Sfx.Play("e5b_summon", 0.8f);
            var at = C + Quaternion.Euler(0, Random.Range(0f, 360f), 0) * Vector3.forward * 4f;
            StylizedFx.Tornado(at, 3f, 7f, 0.6f, StylizedFx.Void);
            yield return Wait(0.8f);
            var g = BossGhost.Spawn(kind, at, this);
            if (g) ghosts.Add(g);
            nextGhost = Time.time + 2f;
        }

        /// <summary>감시 빔: 가장자리에서 조준 0.8초 → 고양이 쪽으로 빔 (28)</summary>
        IEnumerator WatchBeam()
        {
            var d = ToPlayer();
            var from = transform.position + Vector3.up * 0.1f;
            var tl = Telegraph.Lane(from, d, arenaR * 2f, 1.2f, T(BeamAim), Telegraph.Danger);
            yield return Wait(T(BeamAim));
            if (tl) tl.Kill();
            Sfx.Play("e5b_beam", 0.6f);
            StarLink.Make(from, from + d * arenaR * 2f, 120f, 1.2f, BeamDmg);
            yield return Wait(T(BeamEvery - BeamAim));
        }

        /// <summary>시간 역류: 고양이가 지난 3초 동안 밟은 길을 되감아 그 자리마다 금빛 원 → 0.8초 뒤 피해 (25)</summary>
        IEnumerator Rewind()
        {
            var pts = new List<Vector3>();
            for (int i = 0; i < trail.Count; i += 3) pts.Add(Effects.Flat(trail[i].p));
            Sfx.Play("e5b_stop", 0.5f);
            foreach (var at in pts) Telegraph.Ring(at, 1.1f, 0.8f, Telegraph.Danger).NoGlint();
            yield return Wait(0.8f);
            var p = P;
            bool hit = false;
            foreach (var at in pts)
            {
                StylizedFx.Spawn("ring", at + Vector3.up * 0.05f, 0.4f, 1.2f, 0.3f, StylizedFx.Magenta);
                if (!hit && p && Effects.Flat(p.transform.position - at).magnitude <= 1.1f) { hit = true; p.health.TakeDamage(RewindDmg); }
            }
            yield return Wait(T(0.6f));
        }

        // ================= 3페이즈 =================
        void EnterPhase3()
        {
            phase = 3;
            foreach (var g in ghosts) if (g) g.Leave();
            ghosts.Clear();
            hp.invulnerable = true;                       // 체력 0: 버티기만 (총은 막힘)
            BossFx.Toast("마지막 1분 — 버텨라!");
            Sfx.Play("e5b_phase", 1f);
            CameraFollow.Shake(0.4f);
            StylizedFx.Shockwave(transform.position, 16f, StylizedFx.Magenta, 1f);
            nextShrink = Time.time + ShrinkEvery; nextFlip = Time.time + 6f; nextBell = Time.time + 12f;
            var go = new GameObject("CollapseEdge");
            edge = go.AddComponent<LineRenderer>();
            edge.sharedMaterial = LaserFx.StripMat5();
            edge.loop = true; edge.positionCount = 72; edge.widthMultiplier = 0.5f;
            edge.colorGradient = LaserFx.TailGradient(Magenta);
            edge.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        void DrawEdge()
        {
            if (!edge) return;
            for (int i = 0; i < 72; i++)
            {
                float a = i * Mathf.PI * 2f / 72f;
                edge.SetPosition(i, C + new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * arenaR + Vector3.up * (0.15f + 0.1f * Mathf.Sin(Time.time * 4f + i)));
            }
        }

        IEnumerator Shrink()
        {
            float from = arenaR, to = Mathf.Max(ArenaMin, arenaR - ShrinkStep);
            Sfx.Play("e5_rift_shatter", 0.8f);
            for (int i = 0; i < 10; i++) Enemy.Fx5("c5rift", C + Quaternion.Euler(0, i * 36f + Random.Range(0, 30f), 0) * Vector3.forward * from + Vector3.up, 2f, 0.5f);
            for (float t = 0; t < 1f; t += Time.deltaTime / 1.2f) { arenaR = Mathf.Lerp(from, to, t); yield return null; }
            arenaR = to;
        }

        /// <summary>모래시계 뒤집기: 모래시계 3개 5초 — 쏴서 뒤집으면 −5초씩, 남기면 +5초씩</summary>
        IEnumerator HourglassFlip()
        {
            BossFx.Toast("모래시계를 쏴서 뒤집어라!");
            var list = new List<Health>();
            var objs = new List<GameObject>();
            var pf = Resources.Load<GameObject>("Kit/Ch5/BP_Orbit_Hourglass");
            for (int i = 0; i < 3; i++)
            {
                var at = RandomSpot(4f); at.y = 0;
                var go = Own(pf ? Instantiate(pf) : GameObject.CreatePrimitive(PrimitiveType.Cylinder));
                foreach (var c in go.GetComponentsInChildren<Collider>()) Destroy(c);
                go.transform.position = at + Vector3.up * 1.4f;
                go.transform.localScale *= pf ? 2.6f : 0.8f;
                var holder = Own(new GameObject("HourglassTarget"));
                holder.transform.position = at;
                list.Add(Ch3Fx.Target(holder, 0.9f, 2.6f));
                objs.Add(go); objs.Add(holder);
                StylizedFx.Spawn("ring", at + Vector3.up * 0.05f, Quaternion.identity, Vector3.one * 1.1f, Vector3.one * 1.1f, 0.3f, StylizedFx.Gold, 60f, 5f);
            }
            var flipped = new HashSet<Health>();
            float end = Time.time + 5f;
            while (Time.time < end && flipped.Count < 3)
            {
                for (int i = 0; i < 3; i++)
                {
                    var h = list[i];
                    if (!h || flipped.Contains(h) || Time.time - h.lastHitAt > 0.05f) continue;
                    flipped.Add(h);
                    survive -= 5f;
                    Sfx.Play("e5b_hourglass", 0.7f);
                    var vis = objs[i * 2];
                    if (vis) StartCoroutine(FlipVisual(vis.transform));
                    StylizedFx.Burst(h.transform.position + Vector3.up * 1.4f, 2.2f, StylizedFx.Gold);
                }
                yield return null;
            }
            int missed = 3 - flipped.Count;
            if (missed > 0) { survive += 5f * missed; BossFx.Toast($"놓친 모래시계 {missed}개 — 시간이 되감긴다 (+{5 * missed}초)"); }
            foreach (var o in objs) if (o) Destroy(o);
        }

        IEnumerator FlipVisual(Transform t)
        {
            var r0 = t.rotation;
            for (float k = 0; k < 1f && t; k += Time.deltaTime / 0.35f) { t.rotation = r0 * Quaternion.Euler(0, 0, 180f * k); yield return null; }
        }

        /// <summary>종소리 12번: 0.5초 간격으로 울림 → 매번 12방향 박자 탄막(30). 울림 순간 ±0.25초 안에 극한 회피하면 −1초</summary>
        IEnumerator Bell12()
        {
            BossFx.Toast("종소리 12번 — 울림에 맞춰 회피!");
            for (int i = 0; i < 12; i++)
            {
                Sfx.Play("e5b_bell", 0.5f, 0.02f, 3);
                bellDodgeWindow = Time.time + 0.25f;
                StylizedFx.Shockwave(transform.position, 4f, StylizedFx.Magenta, 0.35f);
                for (int k = 0; k < 12; k++)
                {
                    var dir = Quaternion.Euler(0, k * 30f + i * 7.5f, 0) * Vector3.forward;
                    Shoot(dir, 7f, BellDmg, 1.2f, 30f, EnemyProjectile.Style.Numeral, (i % 12) + 1);
                }
                yield return Wait(0.5f);
            }
        }

        /// <summary>모래시계 픽업: 남은 시간 되찾기</summary>
        public void Regain(float sec)
        {
            gauge = Mathf.Min(GaugeStart, gauge + sec);
            if (P) { var b = Label(null, P.transform.position + Vector3.up * 3f, $"+{sec:0}초", Gold, 1.2f); if (b) Destroy(b.gameObject, 1.1f); }
        }

        void Win()
        {
            if (!hp.Alive) return;
            BossFx.Toast("자정 — 시간의 감옥이 멈췄다");
            StylizedFx.Shockwave(transform.position, 20f, StylizedFx.Gold, 1.2f);
            hp.minHp = 0; hp.invulnerable = false;
            hp.TakeDamage(hp.maxHp * 100f);
        }
    }

    /// <summary>
    /// 보스 아우라 (2026-11-20 사용자: 보스가 더 강력한 느낌): 발밑에 천천히 도는 보랏빛 보로노이 고리 + 금빛 고리, 3초마다 몸을 휘감는 옅은 회오리,
    /// 몸 둘레를 도는 블렌더 모래시계 3개 · 부서진 톱니 2개 (Kit/Ch5/BP_Orbit_*). 보스가 사라지면 같이 사라짐
    /// </summary>
    public class BossAura : MonoBehaviour
    {
        Transform owner; readonly List<Transform> orbit = new(); float nextSwirl, ang;
        StylizedFx ring1, ring2;

        public static void Attach(Component boss)
        {
            var go = new GameObject("BossAura");
            var a = go.AddComponent<BossAura>();
            a.owner = boss.transform;
            foreach (var (name, n) in new[] { ("BP_Orbit_Hourglass", 3), ("BP_Orbit_Gear", 2) })
            {
                var pf = Resources.Load<GameObject>("Kit/Ch5/" + name);
                if (!pf) continue;
                for (int i = 0; i < n; i++)
                {
                    var o = Instantiate(pf, go.transform);
                    foreach (var c in o.GetComponentsInChildren<Collider>()) Destroy(c);
                    o.transform.localScale *= 2.2f;
                    a.orbit.Add(o.transform);
                }
            }
        }

        void Update()
        {
            if (!owner) { Destroy(gameObject); return; }
            var c = owner.position;
            ang += 38f * Time.deltaTime;
            for (int i = 0; i < orbit.Count; i++)
            {
                float a = (ang + i * 360f / Mathf.Max(1, orbit.Count)) * Mathf.Deg2Rad;
                orbit[i].position = c + new Vector3(Mathf.Sin(a) * 4.2f, 3.2f + Mathf.Sin(Time.time * 1.6f + i) * 0.6f, Mathf.Cos(a) * 4.2f);
                orbit[i].Rotate(25f * Time.deltaTime, 60f * Time.deltaTime, 0, Space.Self);
            }
            // 발밑 고리 2겹 (계속 유지: 끝나면 다시)
            if (!ring1) ring1 = StylizedFx.Spawn("ring", c + Vector3.up * 0.05f, Quaternion.identity, Vector3.one * 3.6f, Vector3.one * 3.6f, 0.5f, StylizedFx.Void.With(scroll: new Vector2(-0.4f, 0)), 18f, 9999f, 0.32f);
            if (!ring2) ring2 = StylizedFx.Spawn("ring", c + Vector3.up * 0.07f, Quaternion.identity, Vector3.one * 2.4f, Vector3.one * 2.4f, 0.5f, StylizedFx.Magenta.With(scroll: new Vector2(0.6f, 0), pattern: 1, scale: new Vector2(10, 1.5f)), -30f, 9999f, 0.38f);
            if (ring1) ring1.transform.position = c + Vector3.up * 0.05f;
            if (ring2) ring2.transform.position = c + Vector3.up * 0.07f;
            if (Time.time >= nextSwirl)
            {
                nextSwirl = Time.time + 3f;
                StylizedFx.Spawn("swirl", c, Quaternion.Euler(0, Random.Range(0f, 360f), 0), new Vector3(2.2f, 3f, 2.2f), new Vector3(3f, 6.5f, 3f), 1.6f, StylizedFx.Void.With(scroll: new Vector2(-1.2f, 0)), 160f, 0f, 0.45f);
            }
        }

        void OnDestroy() { if (ring1) Destroy(ring1.gameObject); if (ring2) Destroy(ring2.gameObject); }
    }

    /// <summary>남은 시간 되찾기 모래시계 (1 · 2페이즈, 20초마다 2개): 고양이가 1.4m 안에 오면 +20초</summary>
    public class HourglassPickup : MonoBehaviour
    {
        Ch5Boss boss; float born;
        public static void Spawn(Vector3 at, Ch5Boss boss)
        {
            at.y = 0;
            var pf = Resources.Load<GameObject>("Kit/Ch5/BP_Orbit_Hourglass");
            var go = pf ? Instantiate(pf) : GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            foreach (var c in go.GetComponentsInChildren<Collider>()) Destroy(c);
            go.transform.position = at + Vector3.up * 1.2f;
            go.transform.localScale *= pf ? 1.8f : 0.5f;
            var h = go.AddComponent<HourglassPickup>();
            h.boss = boss; h.born = Time.time;
            StylizedFx.Spawn("ring", at + Vector3.up * 0.05f, Quaternion.identity, Vector3.one * 0.4f, Vector3.one * 1.3f, 0.4f, StylizedFx.Gold, 90f);
        }

        void Update()
        {
            if (!boss) { Destroy(gameObject); return; }
            transform.Rotate(0, 90f * Time.deltaTime, 0, Space.World);
            transform.position += Vector3.up * Mathf.Sin(Time.time * 3f) * 0.004f;
            var p = PlayerController.Instance;
            if (p && Effects.Flat(p.transform.position - transform.position).magnitude <= 1.4f)
            {
                boss.Regain(Ch5Boss.HourglassGain);
                Sfx.Play("e5b_hourglass", 0.7f);
                StylizedFx.Burst(transform.position, 2f, StylizedFx.Gold);
                Destroy(gameObject);
            }
            else if (Time.time - born > 18f) Destroy(gameObject);
        }
    }

    /// <summary>
    /// 지난 장 보스 환영 (2페이즈): 반투명 보랏빛 유령 모습으로 나와 그 보스의 대표 기믹 하나를 되풀이, 체력 1500 · 20초 뒤 사라짐
    /// melt = 녹은 시계 거인(녹은 웅덩이 비) · loco = 멈춘 기관차 거인(경기장 가로지르는 돌진) · whale = 거울 고래(물결 고리) · twins = 시침 분침 쌍둥이(도는 바늘 두 줄)
    /// </summary>
    public class BossGhost : MonoBehaviour
    {
        string kind; Ch5Boss boss; Health hp; float born, next; Vector3 home;

        static readonly Dictionary<string, (string res, float h, string name)> Models = new()
        {
            { "melt", ("Enemies/Ch1_Boss", 6f, "녹은 시계 거인의 환영") },
            { "loco", ("Enemies/Ch2_Boss", 6.5f, "멈춘 기관차 거인의 환영") },
            { "whale", ("Kit/Ch3/Ch3_Whale", 5f, "거울 고래의 환영") },
            { "twins", ("Enemies/Ch4_Knight", 4.5f, "시침 · 분침 쌍둥이의 환영") },
        };

        /// <summary>몸을 반투명 유령으로 (CatTang/MirrorGhost): tint 색 · alpha. 다시 부르면 색만 바꿈 (잔상 카운터 창 = 금빛)</summary>
        public static void MakeGhostly(GameObject body, Color tint, float alpha)
        {
            var sh = Shader.Find("CatTang/MirrorGhost");
            if (!sh) return;
            foreach (var r in body.GetComponentsInChildren<Renderer>())
            {
                if (r is ParticleSystemRenderer || r is LineRenderer || r is TrailRenderer) continue;
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    var src = mats[i];
                    var m = src && src.shader == sh ? src : new Material(sh);
                    if (src && src.shader != sh && src.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", src.GetTexture("_BaseMap"));
                    m.SetColor("_Tint", tint); m.SetFloat("_TintAmount", 0.65f); m.SetFloat("_Alpha", alpha); m.SetColor("_RimColor", Ch5Boss.Gold);
                    mats[i] = m;
                }
                r.sharedMaterials = mats;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        public static BossGhost Spawn(string kind, Vector3 at, Ch5Boss boss)
        {
            var go = new GameObject("BossGhost_" + kind);
            go.transform.position = at;
            var g = go.AddComponent<BossGhost>();
            g.kind = kind; g.boss = boss; g.born = Time.time; g.next = Time.time + 1.5f; g.home = at;
            var (res, h, name) = Models[kind];
            var pf = Resources.Load<GameObject>(res);
            GameObject body = pf ? Instantiate(pf, go.transform) : GameObject.CreatePrimitive(PrimitiveType.Capsule);
            if (!pf) { body.transform.SetParent(go.transform, false); body.transform.localScale = new Vector3(2f, h / 2f, 2f); body.transform.localPosition = Vector3.up * h / 2f; }
            foreach (var c in body.GetComponentsInChildren<Collider>()) Destroy(c);
            foreach (var mb in body.GetComponentsInChildren<MonoBehaviour>()) if (mb) mb.enabled = false;   // 원래 보스 스크립트 · 애니 코드는 끔 (모습만)
            var sh = Shader.Find("CatTang/MirrorGhost");
            if (sh)
                foreach (var r in body.GetComponentsInChildren<Renderer>())
                {
                    var mats = r.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++)
                    {
                        var m = new Material(sh);
                        if (mats[i] && mats[i].HasProperty("_BaseMap")) m.SetTexture("_BaseMap", mats[i].GetTexture("_BaseMap"));
                        m.SetColor("_Tint", new Color(0.62f, 0.42f, 1f)); m.SetFloat("_TintAmount", 0.65f); m.SetFloat("_Alpha", 0.55f); m.SetColor("_RimColor", Ch5Boss.Gold);
                        mats[i] = m;
                    }
                    r.sharedMaterials = mats;
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            // 고양이 탄만 맞는 과녁 + 진짜 체력 (3장 과녁처럼 무한 체력이 아님)
            var col = go.AddComponent<CapsuleCollider>();
            col.radius = 1.6f; col.height = h; col.center = Vector3.up * h / 2f;
            go.AddComponent<PlayerOnlyTarget>();
            g.hp = go.AddComponent<Health>();
            g.hp.maxHp = Ch5Boss.GhostHp; g.hp.ResetHp(Ch5Boss.GhostHp);
            g.hp.OnDeath += _ => { if (!g) return; Sfx.Play("e5_phantom_break", 0.8f); StylizedFx.Burst(g.transform.position + Vector3.up * 2f, 4f, StylizedFx.Void); Destroy(g.gameObject); };
            BossFx.Toast(name + "이 나타났다");
            return g;
        }

        public void Leave() { StylizedFx.Tornado(transform.position, 2.5f, 6f, 0.2f, StylizedFx.Void); Destroy(gameObject); }

        void Update()
        {
            if (!boss) { Destroy(gameObject); return; }
            if (Time.time - born > Ch5Boss.GhostLife) { Leave(); return; }
            transform.position = home + Vector3.up * (0.3f + Mathf.Sin(Time.time * 1.5f) * 0.2f);
            var p = PlayerController.Instance;
            if (!p || Time.time < next) return;
            var at = Effects.Flat(p.transform.position);
            switch (kind)
            {
                case "melt":       // 고양이 둘레에 녹은 웅덩이 3개 비
                    next = Time.time + 2.6f;
                    for (int i = 0; i < 3; i++)
                    {
                        var d = at + Quaternion.Euler(0, Random.Range(0f, 360f), 0) * Vector3.forward * Random.Range(0f, 3f);
                        StartCoroutine(Drop(d));
                    }
                    break;
                case "loco":       // 경기장을 가로지르는 돌진 줄 (1초 예고)
                    next = Time.time + 3.2f;
                    StartCoroutine(Charge(at));
                    break;
                case "whale":      // 물결 고리
                    next = Time.time + 2.2f;
                    VoidRipple.Spawn(transform.position, 5f, 18f, 16f, 0.7f, 1.5f);   // 5장 허공 파동 (예전 3장 선 고리 대신)
                    break;
                default:           // 쌍둥이: 고양이 쪽 십자 바늘 두 줄
                    next = Time.time + 2.4f;
                    StartCoroutine(Hands(at));
                    break;
            }
        }

        IEnumerator Drop(Vector3 at)
        {
            Telegraph.Ring(at, 1.4f, 0.9f, Telegraph.Danger);
            yield return new WaitForSeconds(0.9f);
            var p = PlayerController.Instance;
            if (p && Effects.Flat(p.transform.position - at).magnitude <= 1.4f) p.health.TakeDamage(20f);
            MeltPuddle.Spawn(at, 1.4f, 3f, 0.3f, new Color(0.6f, 0.4f, 0.9f));
        }

        IEnumerator Charge(Vector3 target)
        {
            var d = Effects.Flat(target - transform.position).normalized;
            var from = transform.position - d * 6f;
            Telegraph.Lane(from, d, 30f, 2.6f, 1f, Telegraph.Danger);
            yield return new WaitForSeconds(1f);
            var p = PlayerController.Instance;
            if (p && Telegraph.InLane(p.transform.position, from, d, 30f, 2.6f)) { p.health.TakeDamage(30f); p.health.Knockback(d * 8f); }
            StylizedFx.Slash(target, d, 5f, StylizedFx.Void, 1f);
        }

        IEnumerator Hands(Vector3 at)
        {
            float a0 = Random.Range(0f, 90f);
            for (int k = 0; k < 2; k++) { var d = Quaternion.Euler(0, a0 + k * 90f, 0) * Vector3.forward; Telegraph.Lane(at - d * 9f, d, 18f, 1.2f, 0.9f, Telegraph.Danger); }
            yield return new WaitForSeconds(0.9f);
            var p = PlayerController.Instance;
            for (int k = 0; k < 2; k++)
            {
                var d = Quaternion.Euler(0, a0 + k * 90f, 0) * Vector3.forward;
                if (p && Telegraph.InLane(p.transform.position, at - d * 9f, d, 18f, 1.2f)) { p.health.TakeDamage(24f); break; }
            }
            StylizedFx.Shockwave(at, 3f, StylizedFx.Magenta, 0.3f);
        }
    }
}
