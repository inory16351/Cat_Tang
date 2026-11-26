// 4장 최종 보스: 시침 · 분침 쌍둥이 (2026-11-20, 기획 docs/Cat_Tang_레벨_디자인.md '시침 · 분침 쌍둥이 (챕터 4)')
// 모델 = Hyper3D Resources/Enemies/Ch4_HourHand (대검) · Ch4_MinuteHand (레이피어). 경기장 = 꼭대기 문자판 원형 지름 40m (Ch4Kit.ArenaR)
// - 체력 따로 (시침 5000 · 분침 4180). 한쪽이 쓰러지면 남은 쪽 분노(2페이즈): 이속 · 패턴 간격 1.2배 + 쓰러진 쪽 패턴 하나 물려받음
//   (남은 쪽이 시침 = 분침 돌진 2번 / 분침 = 시각 낙인)
// - 패턴 박자: 번갈아 발동 (한쪽 패턴이 끝나야 다른 쪽 예고). 둘 다 살아 있고 한쪽이 50% 이하면, 상대 예고가 시작될 때 30% 확률로 약한 패턴(시각 낙인 · 바늘 부채)을 겹침
// - 상시 탄막 별빛 낙하 (2초 / 2페이즈 1.5초), 고유 기믹 3종 20초 쿨 (순서대로 연속 명중 · 뒤섞인 문자판 · 멈춘 톱니 돌리기) — TwinDirector
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    /// <summary>쌍둥이 지휘: 차례 · 겹침 · 상시 별빛 낙하 · 고유 기믹. 보스방에 하나</summary>
    public class TwinDirector : MonoBehaviour
    {
        public static TwinDirector Inst;
        public Ch4TwinBrain hour, minute;
        public bool inGimmick;
        public Ch4TwinBrain lastStarter;
        float nextGimmick, nextStar;
        string lastGimmick;
        int needHour; Health needTarget;                     // 순서 표적 판정
        readonly Dictionary<Health, int> gearHour = new();
        readonly Dictionary<Health, FloatGear> floatOf = new();         // 과녁 → 공중 톱니 (맞으면 돌며 사라짐)
        readonly HashSet<Health> gearDone = new();
        readonly List<GameObject> spawned = new();
        public const float GimmickFirst = 20f, GimmickEvery = 20f, Stun = 4f;
        public const float StarEvery = 2f, StarEvery2 = 1.5f, StarR = 1.2f, StarDmg = 15f;

        public bool BothAlive => hour && hour.Alive && minute && minute.Alive;
        public bool Phase2 => !BothAlive;
        Vector3 C => RunManager.Instance ? RunManager.Instance.RoomCenter : Vector3.zero;
        static PlayerController P => PlayerController.Instance;

        public static void Make(Ch4TwinBrain h, Ch4TwinBrain m)
        {
            if (Inst) Destroy(Inst.gameObject);
            var go = new GameObject("TwinDirector");
            Inst = go.AddComponent<TwinDirector>();
            Inst.hour = h; Inst.minute = m;
            Inst.nextGimmick = Time.time + GimmickFirst;
            Inst.nextStar = Time.time + 3f;
            if (P) P.OnFired += Inst.OnPlayerFired;
        }

        void OnDestroy()
        {
            if (P) P.OnFired -= OnPlayerFired;
            foreach (var g in spawned) if (g) Destroy(g);
            if (Inst == this) Inst = null;
        }

        /// <summary>b가 지금 패턴을 시작해도 되나 (번갈아 · 기믹 중 금지)</summary>
        public bool CanStart(Ch4TwinBrain b)
        {
            if (inGimmick) return false;
            var other = b == hour ? minute : hour;
            if (!other || !other.Alive) return true;
            if (other.Busy) return false;
            return lastStarter != b || other.ReadyAt > Time.time + 0.5f;        // 상대가 한동안 못 쓰면 연속도 허용
        }

        public void Started(Ch4TwinBrain b, bool weak)
        {
            if (!weak) lastStarter = b;
            // 겹침: 둘 다 살아 있고 한쪽이 50% 이하 → 상대 예고 시작 때 30%로 약한 패턴
            var other = b == hour ? minute : hour;
            if (weak || !BothAlive || other.Busy) return;
            if ((hour.HpFrac <= 0.5f || minute.HpFrac <= 0.5f) && Random.value < 0.3f) other.StartWeak();
        }

        void Update()
        {
            if (!hour && !minute) { Destroy(gameObject); return; }
            if ((!hour || !hour.Alive) && (!minute || !minute.Alive)) { Destroy(gameObject); return; }
            var p = P;
            if (!p || !p.health.Alive) return;
            if (BossIntro.Playing) { nextStar = Mathf.Max(nextStar, Time.time + 1f); return; }
            if (Time.time >= nextStar)
            {
                nextStar = Time.time + (Phase2 ? StarEvery2 : StarEvery);
                StartCoroutine(StarFall(Effects.Flat(p.transform.position) + Quaternion.Euler(0, Random.Range(0f, 360f), 0) * Vector3.forward * Random.Range(0f, 6f)));
            }
            if (!inGimmick && Time.time >= nextGimmick && (!hour || !hour.Busy) && (!minute || !minute.Busy)) StartCoroutine(Gimmick());
        }

        IEnumerator StarFall(Vector3 at)
        {
            at = Clamp(at);
            Telegraph.Ring(at, StarR, 1f, Telegraph.Danger);
            yield return new WaitForSeconds(1f);
            FxBurst.Spawn(at + Vector3.up * 0.4f, 1.4f, FxBurst.Kind.Spark, new Color(1f, 0.95f, 0.8f));
            Sfx.Play("e5_star_burst", 0.45f, 0.1f, 2);
            var p = P;
            if (p && p.health.Alive && Effects.Flat(p.transform.position - at).magnitude <= StarR) p.health.TakeDamage(StarDmg);
        }

        Vector3 Clamp(Vector3 at)
        {
            var d = Effects.Flat(at - C);
            if (d.magnitude > Ch4Kit.ArenaR - 1.5f) at = C + d.normalized * (Ch4Kit.ArenaR - 1.5f);
            return at;
        }

        // ---------- 고유 기믹 (20초 쿨, 직전 제외 무작위) ----------
        IEnumerator Gimmick()
        {
            inGimmick = true;
            var all = new List<string> { "seq", "shuffle", "gears" };
            all.Remove(lastGimmick);
            lastGimmick = all[Random.Range(0, all.Count)];
            yield return lastGimmick == "seq" ? Sequence() : lastGimmick == "shuffle" ? Shuffled() : Gears();
            needHour = 0; needTarget = null;
            foreach (var g in spawned) if (g) Destroy(g);
            spawned.Clear(); floatOf.Clear();
            inGimmick = false;
            nextGimmick = Time.time + GimmickEvery;
        }

        void StunBoth() { foreach (var b in new[] { hour, minute }) if (b && b.Alive) b.Hp.ApplyStun(Stun); }

        static void HurtFrac(float frac)
        {
            var p = P;
            if (p && p.health.Alive) p.health.TakeDamage(p.health.maxHp * frac);
        }

        void OnPlayerFired(int slot, bool awake)
        {
            if (!inGimmick) return;
            if (needTarget && Time.time - needTarget.lastHitAt <= 0.001f)
            {
                if (slot == needHour) { if (floatOf.TryGetValue(needTarget, out var fg) && fg) fg.Solve(); needTarget = null; }   // 맞음 → 다음 표적
                else FxBurst.Spawn(needTarget.transform.position + Vector3.up * 1.2f, 0.8f, FxBurst.Kind.Spark, Color.white);   // 튕김
            }
            foreach (var kv in gearHour)
            {
                var h = kv.Key;
                if (!h || gearDone.Contains(h) || Time.time - h.lastHitAt > 0.001f) continue;
                if (slot == kv.Value) { gearDone.Add(h); if (floatOf.TryGetValue(h, out var fg) && fg) fg.Solve(); }
                else FxBurst.Spawn(h.transform.position + Vector3.up, 0.8f, FxBurst.Kind.Spark, Color.white);
            }
        }

        void Retreat(float r)
        {
            int i = 0;
            foreach (var b in new[] { hour, minute })
            {
                if (!b || !b.Alive) continue;
                var at = C + Quaternion.Euler(0, i * 180f + 30f, 0) * Vector3.forward * r;
                b.transform.position = new Vector3(at.x, b.transform.position.y, at.z);
                Enemy.Fx3("Stardust", at + Vector3.up * 2f, 1.4f);
                i++;
            }
        }

        // ① 순서대로 연속 명중: 문자판 둘레 숫자 3개(2페이즈 4개)가 차례로 빛남 → 12초 안에 빛난 순서대로 그 숫자 시각 탄으로
        IEnumerator Sequence()
        {
            BossFx.Toast("순서대로 연속 명중 — 빛난 순서대로, 그 숫자 시각 탄으로!");
            Retreat(Ch4Kit.ArenaR - 2.5f);
            int n = Phase2 ? 4 : 3;
            var hours = new List<int>();
            while (hours.Count < n) { int h = Random.Range(1, 13); if (!hours.Contains(h)) hours.Add(h); }
            float ringR = Ch4Kit.ArenaR * 0.62f;
            var targets = new List<Health>();
            foreach (var h in hours)
            {
                var at = C + new Vector3(Mathf.Sin(h * 30f * Mathf.Deg2Rad), 0, Mathf.Cos(h * 30f * Mathf.Deg2Rad)) * ringR;
                // 2026-11-26 사용자: 배지만 뜨면 무엇을 맞히는지 모름 → 공중 톱니(블렌더) + 배지 아래 화살표, 맞으면 돌며 사라짐
                var fg = FloatGear.Make(at, h, new Color(1f, 0.85f, 0.4f), out var th);
                spawned.Add(fg.gameObject);
                targets.Add(th); floatOf[th] = fg;
                Telegraph.Ring(at, 1.1f, 0.6f, Telegraph.Aim);
                Sfx.Play("e4_bell", 0.6f, 0f, 2);
                yield return new WaitForSeconds(0.6f);
            }
            float end = Time.time + 12f;
            int done = 0;
            while (done < n && Time.time < end && (hour && hour.Alive || minute && minute.Alive))
            {
                needHour = hours[done]; needTarget = targets[done];
                while (needTarget && Time.time < end) yield return null;
                if (!needTarget) done++;                                                // 톱니는 FloatGear.Solve가 돌리며 지움
            }
            needTarget = null;
            if (done >= n) { BossFx.Toast("성공 — 크로노 & 템포 기절!"); StunBoth(); yield return new WaitForSeconds(0.5f); }
            else
            {
                // 실패: 남은 표적마다 바늘 낙하
                for (int i = done; i < n; i++)
                {
                    var p = P; if (!p) break;
                    var at = Effects.Flat(p.transform.position);
                    Telegraph.Ring(at, 1.5f, 0.6f, Telegraph.Danger);
                    yield return new WaitForSeconds(0.6f);
                    CameraFollow.Shake(0.2f);
                    if (P && Effects.Flat(P.transform.position - at).magnitude <= 1.5f) HurtFrac(0.15f);
                }
            }
        }

        // ② 뒤섞인 문자판: 바닥 숫자 12칸이 2초 동안 뒤섞임 → 6초 뒤 내 발사 시각 숫자 칸 위에 서 있어야 안전. 쌍둥이는 바깥을 돌며 느린 바늘 탄
        IEnumerator Shuffled()
        {
            BossFx.Toast("뒤섞인 문자판 — 내 발사 시각 숫자 칸 위에 서 있어라!");
            float ringR = Ch4Kit.ArenaR * 0.55f;
            var nums = new List<int>(); for (int i = 1; i <= 12; i++) nums.Add(i);
            // 칸 숫자 = 바닥에 스스슥 새겨지는 블렌더 3D 숫자 (2026-11-26 사용자: 그림 배지는 바닥과 안 어울림). 12시 칸부터 시계 방향으로 차례로
            var floorNums = new List<FloorNumeral>();
            for (int k = 0; k < 12; k++)
            {
                var fn = FloorNumeral.Make(SectorPos(k, ringR), nums[k], 3.3f, k * 0.04f);   // 지름 3.3m (2026-11-26 사용자: 더 크게)
                fn.transform.SetParent(transform, true);                                // 지휘가 사라지면 같이 (패턴 도중 끝나도 안 남음)
                floorNums.Add(fn);
            }
            for (int k = 0; k < 12; k++)                                                // 칸 경계
                Telegraph.Lane(C, Quaternion.Euler(0, k * 30f + 15f, 0) * Vector3.forward, Ch4Kit.ArenaR - 1f, 0.12f, 8f, Telegraph.Aim).NoGlint();
            // 2초 동안 섞임 (숫자를 바꿔 쓰며 깜빡)
            float t0 = Time.time;
            while (Time.time - t0 < 2f)
            {
                int a = Random.Range(0, 12), b = Random.Range(0, 12);
                (nums[a], nums[b]) = (nums[b], nums[a]);
                if (floorNums[a]) floorNums[a].SetNumber(nums[a]);
                if (floorNums[b]) floorNums[b].SetNumber(nums[b]);
                Sfx.Play("e4_tick", 0.35f, 0.2f, 2);
                yield return new WaitForSeconds(0.12f);
            }
            // 6초: 쌍둥이 바깥 둘레를 돌며 느린 바늘 탄
            float end = Time.time + 6f, nextShot = 0;
            float ang = 0;
            while (Time.time < end)
            {
                ang += 40f * Time.deltaTime;
                int i = 0;
                foreach (var tb in new[] { hour, minute })
                {
                    if (!tb || !tb.Alive) continue;
                    var at = C + Quaternion.Euler(0, ang + i * 180f, 0) * Vector3.forward * (Ch4Kit.ArenaR - 2f);
                    tb.transform.position = Vector3.Lerp(tb.transform.position, new Vector3(at.x, tb.transform.position.y, at.z), 6f * Time.deltaTime);
                    i++;
                }
                if (Time.time >= nextShot)
                {
                    nextShot = Time.time + 0.7f;
                    foreach (var tb in new[] { hour, minute })
                        if (tb && tb.Alive && P)
                        {
                            var d = Effects.Flat(P.transform.position - tb.transform.position).normalized;
                            EnemyProjectile.Spawn(tb.transform.position + Vector3.up * 1.4f + d, d, 6f, 15f, 30f, EnemyProjectile.Style.Needle, 0);
                        }
                }
                yield return null;
            }
            // 판정
            var p = P;
            if (p && p.health.Alive)
            {
                var d = Effects.Flat(p.transform.position - C);
                float a = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg; if (a < 0) a += 360f;
                int k = Mathf.RoundToInt(a / 30f) % 12;                                // 칸 k = 12시 방향부터 시계 방향
                int stand = nums[k];
                bool center = d.magnitude < 2f;
                if (!center && stand == p.rev.next) { BossFx.Toast("안전!"); FxBurst.Spawn(p.transform.position + Vector3.up, 1.2f, FxBurst.Kind.Gold, Color.white); }
                else { CameraFollow.Shake(0.25f); HurtFrac(0.35f); BossFx.Toast($"실패 — {stand}시 칸 (발사 시각 {p.rev.next}시)"); }
            }
            yield return new WaitForSeconds(0.6f);
            for (int k = 0; k < floorNums.Count; k++) if (floorNums[k]) floorNums[k].Erase(k * 0.03f);   // 쓸려 지워짐
        }

        Vector3 SectorPos(int k, float r) => C + Quaternion.Euler(0, k * 30f, 0) * Vector3.forward * r;

        // ③ 멈춘 톱니 돌리기: 고양이에서 6~14m에 톱니 3개(서로 다른 시각), 자기 시각 탄에만 돌아감, 10초
        IEnumerator Gears()
        {
            BossFx.Toast("멈춘 톱니 — 톱니 숫자와 같은 시각 탄으로 돌려라!");
            var p = P;
            if (!p) yield break;
            gearHour.Clear(); gearDone.Clear();
            var hours = new List<int>();
            while (hours.Count < 3) { int h = Random.Range(1, 13); if (!hours.Contains(h)) hours.Add(h); }
            var placed = new List<Vector3>();
            var gearT = new List<Transform>();
            for (int k = 0, tries = 0; k < 3 && tries < 200; tries++)
            {
                var at = Effects.Flat(p.transform.position) + Quaternion.Euler(0, Random.Range(0f, 360f), 0) * Vector3.forward * Random.Range(6f, 14f);
                if (Effects.Flat(at - C).magnitude > Ch4Kit.ArenaR - 2f) continue;
                bool ok = true; foreach (var o in placed) if ((o - at).magnitude < 4f) ok = false;
                if (!ok) continue;
                placed.Add(at);
                // 공중 톱니(블렌더) + 배지 아래 화살표 (2026-11-26 사용자), 맞는 시각 탄이면 돌며 사라짐
                var fg = FloatGear.Make(at + Vector3.up * 6f, hours[k], new Color(1f, 0.85f, 0.4f), out var h);
                spawned.Add(fg.gameObject);
                gearHour[h] = hours[k]; floatOf[h] = fg;
                gearT.Add(fg.transform);
                k++;
            }
            // 내려옴
            for (float t = 0; t < 0.5f; t += Time.deltaTime)
            {
                for (int i = 0; i < gearT.Count; i++) gearT[i].position = Vector3.Lerp(placed[i] + Vector3.up * 6f, placed[i], t / 0.5f);
                yield return null;
            }
            for (int i = 0; i < gearT.Count; i++) gearT[i].position = placed[i];
            CameraFollow.Shake(0.15f);
            float end = Time.time + 10f;
            while (Time.time < end && gearDone.Count < gearHour.Count)
            {
                yield return null;
            }
            int left = gearHour.Count - gearDone.Count;
            if (left == 0) { BossFx.Toast("문자판이 멈췄다 — 크로노 & 템포 기절!"); StunBoth(); }
            else { CameraFollow.Shake(0.25f); HurtFrac(0.12f * left); }
            gearHour.Clear(); gearDone.Clear(); floatOf.Clear();
            yield return new WaitForSeconds(0.5f);
        }
    }

    /// <summary>쌍둥이 한쪽 두뇌 (isHour = 시침)</summary>
    public class Ch4HourBrain : Ch4TwinBrain { protected override bool IsHourHand => true; }
    public class Ch4MinuteBrain : Ch4TwinBrain { protected override bool IsHourHand => false; }

    public abstract class Ch4TwinBrain : BossBrain
    {
        protected abstract bool IsHourHand { get; }          // AddComponent 때 Awake → Setup이 바로 불려서 필드 대신 자식 클래스로 나눔
        bool isHour => IsHourHand;
        public bool rage;                                     // 2페이즈 (상대가 쓰러짐)
        public Health Hp => hp;
        public bool Alive => hp && hp.Alive;
        public bool Busy => busy;
        public float ReadyAt => nextPatternAt;
        public float HpFrac => hp ? hp.hp / Mathf.Max(1f, hp.maxHp) : 0;
        public override float NearDist => isHour ? 6f : 5f;
        public override float PatternGap => (isHour ? 1.4f : 1.0f) / (rage ? 1.2f : 1f);
        public override string DisplayName => isHour ? "크로노" : "템포";
        TwinDirector D => TwinDirector.Inst;

        // 수치 (기획서)
        const float SlamTell = 0.9f, SlamLen = 12f, SlamW = 2.5f, SlamDmg = 32f, SlamStuck = 1.2f;
        const float SpinTell = 0.8f, SpinR = 4.5f, SpinDmg = 30f;
        const float BrandR = 2f, BrandGap = 0.4f, BrandTell = 1f, BrandDmg = 28f;
        const float StabTell = 0.5f, StabLen = 3f, StabW = 1.2f, StabTime = 1.2f, StabTick = 0.2f, StabDmg = 8f;
        const float DashTell = 0.6f, DashSpeed = 20f, DashDmg = 27f;
        const float FanSpeed = 12f, FanDmg = 18f;
        const float PincerTell = 1.2f, PincerDmg = 34f;

        protected override void Setup()
        {
            if (isHour)
            {
                Add("시침 내려찍기", Reach.Near, Slam, null, "");     // 몸 클립 대신 WeaponSwingRig가 대검 · 몸 회전을 직접
                Add("대검 회전", Reach.Near, Spin, null, "");
                Add("시각 낙인", Reach.Far, Brand);
                Add("분침 돌진 (물려받음)", Reach.Far, () => Dash(2), () => rage);
            }
            else
            {
                Add("60초 찌르기", Reach.Near, Stab);
                Add("분침 돌진", Reach.Far, () => Dash(3));
                Add("바늘 부채", Reach.Far, Fan);
                Add("시각 낙인 (물려받음)", Reach.Any, Brand, () => rage);
            }
            Add("열두 시 협공", Reach.Any, Pincer, () => D && D.BothAlive);
        }

        protected override System.Func<IEnumerator> Forced() => null;

        // 차례 · 기믹: 지휘가 허락할 때만 패턴 (BossBrain.Tick 앞에서 다음 패턴 시각을 미룸)
        protected override void Always(PlayerController p, Vector3 dir, float dist)
        {
            if (D && D.Phase2 && !rage && Alive) { rage = true; self.speed *= 1.2f; BossFx.Toast(DisplayName + " — 쓰러진 바늘을 흡수했다!"); }
            if (!busy && D && !D.CanStart(this)) nextPatternAt = Mathf.Max(nextPatternAt, Time.time + 0.1f);
            if (D && D.inGimmick && !busy) nextPatternAt = Mathf.Max(nextPatternAt, Time.time + 0.5f);
        }

        protected override void Move(PlayerController p, Vector3 dir, float dist, float spd)
        {
            if (D && D.inGimmick) return;
            if (isHour) { if (dist > NearDist * 0.7f) transform.position += dir * spd * Time.deltaTime; return; }
            // 분침: 고양이 둘레를 빠르게 돌며 6~8m 유지
            var side = Vector3.Cross(Vector3.up, dir);
            var radial = dist < 6f ? -dir : dist > 8f ? dir : Vector3.zero;
            transform.position += (side * 0.9f + radial) .normalized * spd * Time.deltaTime;
        }

        bool startedNotified;
        void LateUpdate()
        {
            // 패턴이 막 시작되면 지휘에 알림 (차례 기록 · 겹침 추첨)
            if (busy && !startedNotified) { startedNotified = true; if (D) D.Started(this, weakRunning); }
            if (!busy) { startedNotified = false; weakRunning = false; }
        }

        bool weakRunning;
        /// <summary>겹침용 약한 패턴 (시각 낙인 / 바늘 부채)</summary>
        public void StartWeak()
        {
            if (busy || !Alive) return;
            weakRunning = true;
            busy = true;
            StartCoroutine(WeakRun());
        }
        IEnumerator WeakRun()
        {
            yield return isHour ? Brand() : Fan();
            busy = false;
            nextPatternAt = Time.time + T(PatternGap);
        }

        Vector3 C => RunManager.Instance ? RunManager.Instance.RoomCenter : Vector3.zero;
        Vector3 ClampA(Vector3 at)
        {
            var d = Effects.Flat(at - C);
            if (d.magnitude > Ch4Kit.ArenaR - 1.5f) at = C + d.normalized * (Ch4Kit.ArenaR - 1.5f);
            return at;
        }

        // ---------- 시침 ----------
        WeaponSwingRig rig;
        WeaponSwingRig Rig => rig ? rig : rig = GetComponent<WeaponSwingRig>();

        // 대검이 떨어지며 가속하고, 칼끝의 마지막 자세에서 충격과 땅 파동이 시작된다.
        IEnumerator Slam()
        {
            var d = ToPlayer();
            var origin = transform.position;
            float tell = T(SlamTell);
            Telegraph.Lane(origin, d, SlamLen, SlamW, tell, Telegraph.Danger).NoLaser();
            transform.rotation = Quaternion.LookRotation(d);
            float act = Mathf.Min(0.32f, tell * 0.49f), pause = Mathf.Min(0.12f, tell * 0.2f);
            var r = Rig;
            bool contacted = false;

            void Contact()
            {
                if (contacted || !hp.Alive || hp.Stunned) return;
                contacted = true;
                var tip = r && !r.BodyOnly ? r.TipWorld : origin + d * 5f;
                if (r) r.Impact(tip, 1.6f, StylizedFx.BlastRed); else CameraFollow.Shake(0.28f);
                Upheaval.Spawn(tip, 1.5f, Random.Range(0f, 360f), 0.9f);
                Sfx.Play("e5_giant_slam", 1f, 0.05f, 2);
                var p = P;
                if (p && p.health.Alive && Telegraph.InLane(p.transform.position, origin, d, SlamLen, SlamW))
                    p.health.TakeDamage(SlamDmg);
                SlamCrack(origin, d);
            }

            if (r) r.Play(new WeaponSwingRig.Swing
            {
                axis = Vector3.right, from = new Vector3(0.18f, 0.6f, -0.78f), sweep = 172f,
                wind = Mathf.Max(0f, tell - act - pause), windHold = pause, act = act,
                hold = T(SlamStuck) * 0.8f, rec = 0.55f, leanBack = 17f, leanFwd = 19f, lunge = 0.9f,
                streak = 1f, reachDrive = 0.7f, accelerateToImpact = true, onHit = Contact,
            });
            float end = Time.time + tell;
            while (Time.time < end) { Face(d, 10f); yield return null; }
            if (r && r.Swinging && !contacted && hp.Alive && !hp.Stunned) yield return null;
            if (!contacted) Contact();
            yield return Wait(T(SlamStuck));
        }

        void SlamCrack(Vector3 origin, Vector3 d)
        {
            // 날아가는 파동 = 땅을 타고 가는 전용 파동 (2026-11-20 사용자: 칼빛 재활용 말고 돌진 이펙트처럼 땅에 붙어서) + 머리가 지나가는 자리마다 땅 솟음
            MeleeReachFx.Lane(origin, d, SlamLen, SlamW, 0.37f, BladeStreak.Ch4Base);
            GroundWave.Spawn(origin + d * 1.25f, origin + d * SlamLen, SlamW, 0.42f, StylizedFx.BlastRed.With(opacity: 0.72f, scale: new Vector2(1.4f, 6f), scroll: new Vector2(-6f, 0)), 1.7f);
        }

        // 대검 회전: 대검을 오른쪽 뒤로 감고 웅크림 → 1.5바퀴 (몸이 돌고 칼은 원심력에 끌려 늦게 따라옴) → 칼빛이 원을 그림
        IEnumerator Spin()
        {
            float act = 0.65f, hold = 0.12f, tell = T(SpinTell);
            var r = Rig;
            // 반지름 = 대검 칼빛이 닿는 거리 (예고 · 판정 같은 값, 조종기가 없으면 SpinR)
            float spinR = r ? r.EffectRadius(1f) : SpinR;
            if (spinR < 1f) spinR = SpinR;
            Telegraph.Ring(transform.position, spinR, tell, Telegraph.Danger, transform);
            if (r) r.Play(new WeaponSwingRig.Swing
            {
                axis = new Vector3(0, 1f, 0.12f), from = new Vector3(0.75f, -0.12f, -0.65f), sweep = -110f,
                wind = Mathf.Max(0.15f, tell - hold), windHold = hold, act = act, actPow = 2.2f,
                hold = 0.1f, rec = 0.5f, bodyFollow = 0f, leanBack = 11f, leanFwd = 13f, streak = 1f, pivotTurn = true, reachDrive = 0.78f,
            });
            yield return Wait(tell);
            Sfx.Play("e4_whirl", 1f, 0.05f, 2);
            float spun = 0;
            for (float t = 0; t < act; t += Time.deltaTime)
            {
                float u = Mathf.Min(1, (t + Time.deltaTime) / act), to = -540f * WeaponSwingRig.Heavy(u);   // 반시계 1.5바퀴, 발과 몸이 함께 가속
                transform.Rotate(0, to - spun, 0); spun = to;
                yield return null;
            }
            MeleeReachFx.Impact(transform.position, transform.forward, spinR, 180f, BladeStreak.Ch4Base);
            if (r) r.Impact(transform.position, 1.1f, StylizedFx.BlastRed);
            HitCircle(transform.position, spinR, SpinDmg);
            yield return Wait(T(0.5f));
        }

        // 시각 낙인: 고양이 둘레에 원 3개가 0.4초 간격으로 예고 1초 → 차례로 폭발
        IEnumerator Brand()
        {
            var p = P;
            if (!p) yield break;
            var pts = new List<Vector3>();
            for (int i = 0; i < 3; i++)
            {
                var at = ClampA(Effects.Flat(p.transform.position) + Quaternion.Euler(0, i * 120f + Random.Range(-30f, 30f), 0) * Vector3.forward * Random.Range(0.5f, 3f));
                pts.Add(at);
                Telegraph.Ring(at, BrandR, T(BrandTell), Telegraph.Danger);
                yield return Wait(T(BrandGap));
            }
            yield return Wait(T(BrandTell) - T(BrandGap) * 2);
            foreach (var at in pts)
            {
                FxBurst.Spawn(at + Vector3.up * 0.4f, 2f, FxBurst.Kind.Explosion, new Color(1f, 0.3f, 0.3f));
                Sfx.Play("e4_bell", 0.55f, 0.1f, 2);
                HitCircle(at, BrandR, BrandDmg);
                yield return Wait(T(BrandGap));
            }
        }

        // ---------- 분침 ----------
        IEnumerator Stab()
        {
            var d = ToPlayer();
            Telegraph.Lane(transform.position, d, StabLen, StabW, T(StabTell), Telegraph.Danger, transform);
            yield return Wait(T(StabTell));
            float end = Time.time + StabTime, next = 0;
            int hits = 0;
            while (Time.time < end && hits < 6)
            {
                Face(d, 14f);
                if (Time.time >= next)
                {
                    next = Time.time + StabTick;
                    Sfx.Play("e4_stab", 0.4f, 0.15f, 3);
                    var p = P;
                    if (p && p.health.Alive && Telegraph.InLane(p.transform.position, transform.position, d, StabLen, StabW)) { p.health.TakeDamage(StabDmg); hits++; }
                }
                yield return null;
            }
            yield return Wait(T(0.3f));
        }

        IEnumerator Dash(int n)
        {
            for (int i = 0; i < n; i++)
            {
                var p = P;
                if (!p) yield break;
                var d = ToPlayer();
                float len = Mathf.Min(Ch4Kit.ArenaR, Dist() + 4f);
                Telegraph.Lane(transform.position, d, len, 1.4f, T(DashTell), Telegraph.Danger);
                float end = Time.time + T(DashTell);
                while (Time.time < end) { Face(d, 14f); yield return null; }
                Sfx.Play("e5_giant_dash", 0.7f, 0.1f, 2);
                var trail = GetComponent<EyeTrail>(); if (trail) trail.Hot(0.4f);
                bool hit = false;
                for (float m = 0; m < len; m += DashSpeed * Time.deltaTime)
                {
                    var to = ClampA(transform.position + d * DashSpeed * Time.deltaTime);
                    transform.position = new Vector3(to.x, transform.position.y, to.z);
                    var pl = P;
                    if (!hit && pl && pl.health.Alive && Effects.Flat(pl.transform.position - transform.position).magnitude < 1.4f) { pl.health.TakeDamage(DashDmg); hit = true; }
                    yield return null;
                }
                yield return Wait(T(0.25f));
            }
        }

        IEnumerator Fan()
        {
            var d = ToPlayer();
            Telegraph.Sector(transform.position, d, 8f, 25f, T(0.5f), Telegraph.Danger);
            yield return Wait(T(0.5f));
            for (int i = 0; i < 7; i++) Shoot(Quaternion.Euler(0, -25f + i * (50f / 6f), 0) * d, FanSpeed, FanDmg, 1.4f, 18f, EnemyProjectile.Style.Needle);
            Sfx.Play("e4_toss", 0.6f, 0.1f, 2);
            yield return Wait(T(0.4f));
        }

        // ---------- 협공 (둘 다 살아 있을 때) ----------
        // 열두 시 협공: 쌍둥이가 12시 · 6시 자리로 순간 이동 → 고양이를 지나는 X자 두 줄 예고 1.2초 → 동시에 가로질러 돌진
        IEnumerator Pincer()
        {
            var d = D;
            if (!d || !d.BothAlive) yield break;
            var other = isHour ? d.minute : d.hour;
            var p = P;
            if (!p) yield break;
            var pp = Effects.Flat(p.transform.position);
            var c = C;
            float r = Ch4Kit.ArenaR - 2f;
            float tilt = Random.Range(-35f, 35f);
            var aFrom = c + Quaternion.Euler(0, tilt, 0) * Vector3.forward * r;
            var bFrom = c + Quaternion.Euler(0, tilt + 180f, 0) * Vector3.forward * r;
            var aDir = Effects.Flat(pp - aFrom).normalized; var bDir = Effects.Flat(pp - bFrom).normalized;
            Enemy.Fx3("Stardust", transform.position + Vector3.up * 2f, 1.4f);
            Enemy.Fx3("Stardust", other.transform.position + Vector3.up * 2f, 1.4f);
            transform.position = new Vector3(aFrom.x, transform.position.y, aFrom.z);
            other.transform.position = new Vector3(bFrom.x, other.transform.position.y, bFrom.z);
            transform.rotation = Quaternion.LookRotation(aDir); other.transform.rotation = Quaternion.LookRotation(bDir);
            float lenA = (pp - aFrom).magnitude + 8f, lenB = (pp - bFrom).magnitude + 8f;
            Telegraph.Lane(aFrom, aDir, lenA, 1.8f, PincerTell, Telegraph.Danger);
            Telegraph.Lane(bFrom, bDir, lenB, 1.8f, PincerTell, Telegraph.Danger);
            other.HoldFor(PincerTell + 1.2f);
            yield return Wait(PincerTell);
            Sfx.Play("e5_giant_dash", 0.9f, 0f, 2);
            bool hit = false;
            float t = 0, dur = Mathf.Max(lenA, lenB) / 24f;
            while (t < dur)
            {
                t += Time.deltaTime;
                if (t * 24f < lenA) { var to = ClampA(transform.position + aDir * 24f * Time.deltaTime); transform.position = new Vector3(to.x, transform.position.y, to.z); }
                if (other && t * 24f < lenB) { var to = ClampA(other.transform.position + bDir * 24f * Time.deltaTime); other.transform.position = new Vector3(to.x, other.transform.position.y, to.z); }
                var pl = P;
                if (!hit && pl && pl.health.Alive && (Effects.Flat(pl.transform.position - transform.position).magnitude < 1.5f || other && Effects.Flat(pl.transform.position - other.transform.position).magnitude < 1.5f)) { pl.health.TakeDamage(PincerDmg); hit = true; }
                yield return null;
            }
            yield return Wait(T(0.5f));
        }

        /// <summary>협공 동안 상대 두뇌를 붙잡아 둠 (그 사이 자기 패턴 X)</summary>
        public void HoldFor(float s)
        {
            if (busy) return;
            busy = true;
            StartCoroutine(Release(s));
        }
        IEnumerator Release(float s) { yield return new WaitForSeconds(s); busy = false; nextPatternAt = Time.time + T(PatternGap); }
    }
}
