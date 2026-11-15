// 플레이어: WASD 이동 · 마우스 조준 · 좌클릭 사격 · Space 대시(최대 3칸, 1칸당 2초, 실루엣 잔상) · R 완벽 장전 · 우클릭 총 스킬
// Q 불릿 파티(열기 3칸) · E 상호작용. 탄 · 총 · 특성 · 코스튬 효과는 Effects로 모아 발동.
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CatTang
{
    [RequireComponent(typeof(CharacterController), typeof(Health))]
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance;

        [Header("이동")]
        public float moveSpeed = 6.5f;
        public float dashDistance = 5f, dashTime = 0.18f;
        public int dashMax = 3;
        public float dashRecharge = 2f;
        [Header("참조")]
        public Transform model;
        public Transform rightMuzzle, leftMuzzle;
        public float baseMaxHp = 100;
        [Header("모드")]
        public bool combatEnabled = true;

        public Revolver rev = new Revolver();
        public Inventory inv = new Inventory();
        public Health health;
        public event Action<int, bool> OnFired;
        public event Action<float> OnReloadStart;
        public event Action<bool> OnReloadEnd;
        public event Action OnUltimate;

        CharacterController cc;
        Vector3 aimDir = Vector3.forward;
        float aimDist = 8f;
        float nextFireTime, nextSkill, reloadStart, reloadDur, dashCharges = 3, dashTimer;
        bool dashing, perfectBuff, ultimateActive;
        // 효과 상태
        float nextShotDamageAdd, nextShotSpeedAdd, tempDodge, tempDodgeUntil, silhouetteFollowUntil, globalStopReady, lastJustDodge;
        int critShots;
        public bool InputLocked { get; set; }
        public float SkillCooldownLeft => Mathf.Max(0, nextSkill - Time.time);
        public float DashCharges => dashCharges;
        public float DashRechargeProgress => dashCharges >= dashMax ? 1 : dashTimer / DashRechargeTime;
        public float ReloadProgress => rev.reloading ? Mathf.Clamp01((Time.time - reloadStart) / reloadDur) : 0;
        public Interactable Focus { get; private set; }

        void Awake()
        {
            Instance = this;
            cc = GetComponent<CharacterController>();
            health = GetComponent<Health>();
            health.isPlayer = true;
            health.OnJustDodge += JustDodge;
            health.OnChanceDodge += ChanceDodge;
            Tables.Load();
        }

        // ---------- 능력치 ----------
        public float MaxHp => (baseMaxHp + Meta.Stat("MaxHp")) * (1 + CostumeStat("MaxHpAdd"));
        public float DamageMul => 1 + Meta.Stat("DamageMul");
        public float AttackSpeedBonus => CostumeStat("AttackSpeedAdd");
        public float ReloadTime
        {
            get
            {
                float a = rev.rightGun != null ? rev.rightGun.ReloadTime : 1.2f, b = rev.leftGun != null ? rev.leftGun.ReloadTime : 1.2f;
                return Mathf.Max(a, b) * (1 + Meta.Stat("ReloadTime"));
            }
        }
        public float DashRechargeTime => dashRecharge * (1 + Meta.Stat("DodgeCooldown"));
        public float HeatPerHit => (1f / 12f) * (1 + Meta.Stat("HeatGain"));
        public float DamageTakenMul => 1 + CostumeStat("DamageTakenAdd");
        public float DodgeChance => Mathf.Clamp01(Effects.GlobalPassive(this, "DodgeChance") + (Time.time < tempDodgeUntil ? tempDodge : 0));
        public float ControlDurationMul => 1 + Effects.GlobalPassive(this, "ControlDurationAdd");
        public float JustDodgeWindow => 0.08f + Effects.GlobalPassive(this, "JustDodgeWindowAdd");

        public static float CostumeStat(string type)
        {
            if (!Tables.Costumes.TryGetValue(Meta.SelectedCostume, out var c)) return 0;
            float v = 0;
            foreach (var id in new[] { c.Effect1, c.Effect2, c.Effect3 })
                if (id != 0 && Tables.Effects.TryGetValue(id, out var e) && e.Type == type && e.Trigger == "Passive") v += e.Value;
            return v;
        }

        /// <summary>판 시작: 시작 총 · 코스튬 · 기본 탄 '탄환' 12발</summary>
        public void SetupRun()
        {
            rev = new Revolver
            {
                rightGun = Tables.Guns.TryGetValue(Meta.StartGunRight, out var g1) ? g1 : Tables.Guns[2001],
                leftGun = Tables.Guns.TryGetValue(Meta.StartGunLeft, out var g2) ? g2 : Tables.Guns[2001]
            };
            inv = new Inventory();
            var basic = Tables.Bullets.TryGetValue(1000, out var bb) ? bb : null;
            for (int i = 1; i <= 12; i++) rev.slots[i].bullet = basic;
            if (Tables.Costumes.TryGetValue(Meta.SelectedCostume, out var c))
                foreach (var id in new[] { c.Effect1, c.Effect2, c.Effect3 })
                {
                    if (id == 0 || !Tables.Effects.TryGetValue(id, out var e)) continue;
                    if (e.Type == "GrantBullet" && Tables.Bullets.TryGetValue((int)e.Value, out var gb)) inv.Add(Item.Of(gb));
                    if (e.Type == "GrantRandomOp") inv.Add(Item.Of(RandomOp()));
                }
            if (Meta.StartWithOp) inv.Add(Item.Of(RandomOp()));
            health.ResetHp(MaxHp);
            rev.next = rev.FirstSlot;
            rev.shotsThisCycle = 0;
            dashCharges = dashMax;
        }

        public static OpDef RandomOp()
        {
            int total = 0;
            foreach (var o in Tables.Ops.Values) total += o.DropWeight;
            int r = UnityEngine.Random.Range(0, total);
            foreach (var o in Tables.Ops.Values) { r -= o.DropWeight; if (r < 0) return o; }
            foreach (var o in Tables.Ops.Values) return o;
            return null;
        }

        public void Teleport(Vector3 p)
        {
            cc.enabled = false;
            transform.position = p;
            cc.enabled = true;
        }

        void Update()
        {
            if (dashCharges < dashMax)
            {
                dashTimer += Time.deltaTime;
                if (dashTimer >= DashRechargeTime) { dashTimer = 0; dashCharges = Mathf.Min(dashMax, dashCharges + 1); }
            }
            else dashTimer = 0;

            if (transform.position.y < -3f)
                Teleport(RunManager.Instance ? RunManager.Instance.RoomCenter + Vector3.up * 0.3f : new Vector3(0, 0.3f, 0));

            UpdateFocus();
            if (!health.Alive || InputLocked || ultimateActive || !Application.isFocused) return;
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if (kb == null || mouse == null) return;

            var cam = Camera.main;
            if (cam)
            {
                var ray = cam.ScreenPointToRay(mouse.position.ReadValue());
                var plane = new Plane(Vector3.up, new Vector3(0, transform.position.y + 0.9f, 0));
                if (plane.Raycast(ray, out float d))
                {
                    var dir = ray.GetPoint(d) - transform.position;
                    dir.y = 0;
                    if (dir.sqrMagnitude > 0.01f) { aimDir = dir.normalized; aimDist = Mathf.Max(2.5f, dir.magnitude); }
                }
            }

            var mv = new Vector3((kb.dKey.isPressed ? 1 : 0) - (kb.aKey.isPressed ? 1 : 0), 0, (kb.wKey.isPressed ? 1 : 0) - (kb.sKey.isPressed ? 1 : 0));
            if (mv.sqrMagnitude > 1) mv.Normalize();
            var faceDir = combatEnabled ? aimDir : (mv.sqrMagnitude > 0.01f ? mv : (model ? model.forward : aimDir));
            if (model && faceDir.sqrMagnitude > 0.001f) model.rotation = Quaternion.Slerp(model.rotation, Quaternion.LookRotation(faceDir), 20f * Time.deltaTime);

            if (!dashing)
            {
                cc.Move((mv * moveSpeed + Vector3.down * 9f) * Time.deltaTime);
                if (kb.spaceKey.wasPressedThisFrame && dashCharges >= 1)
                    StartCoroutine(Dash(mv.sqrMagnitude > 0.01f ? mv.normalized : faceDir));
            }

            if (kb.eKey.wasPressedThisFrame && Focus) Focus.Interact(this);
            if (!combatEnabled) return;

            if (rev.reloading)
            {
                if (kb.rKey.wasPressedThisFrame) TryPerfectReload();
                if (Time.time >= reloadStart + reloadDur) FinishReload(false);
            }
            else if (mouse.leftButton.isPressed && Time.time >= nextFireTime) FireNext();

            if (mouse.rightButton.wasPressedThisFrame && Time.time >= nextSkill) StartCoroutine(BackstepShot());
            if (kb.qKey.wasPressedThisFrame && rev.heat >= Revolver.HeatMax) StartCoroutine(BulletParty());
        }

        void UpdateFocus()
        {
            Interactable best = null;
            float bd = 999;
            foreach (var it in Interactable.All)
            {
                if (!it || !it.isActiveAndEnabled || !it.Available) continue;
                var d3 = it.transform.position - transform.position;
                d3.y = 0;
                float d = d3.magnitude;
                if (d < it.radius && d < bd) { bd = d; best = it; }
            }
            Focus = best;
        }

        // ---------- 대시 (실루엣 잔상) ----------
        IEnumerator Dash(Vector3 dir)
        {
            dashing = true;
            dashCharges -= 1;
            health.invulnerable = true;
            health.dashing = true;
            float t = 0, ghostT = 0;
            Afterimage.Spawn(model, new Color(0.7f, 0.55f, 1f, 0.75f));
            while (t < dashTime)
            {
                cc.Move(dir * (dashDistance / dashTime) * Time.deltaTime);
                t += Time.deltaTime;
                ghostT += Time.deltaTime;
                if (ghostT >= 0.035f) { ghostT = 0; Afterimage.Spawn(model, new Color(0.7f, 0.55f, 1f, 0.6f)); }
                yield return null;
            }
            dashing = false;
            Afterimage.Spawn(model, new Color(1f, 0.85f, 0.45f, 0.55f));
            yield return new WaitForSeconds(JustDodgeWindow);
            health.dashing = false;
            health.invulnerable = false;
        }

        void JustDodge()
        {
            if (Time.time - lastJustDodge < 0.5f) return;
            lastJustDodge = Time.time;
            FxBurst.Spawn(transform.position + Vector3.up, 2f, FxBurst.Kind.Gold, new Color(0.7f, 0.55f, 1f));
            foreach (var e in Effects.Global(this, "OnJustDodge"))
            {
                if (e.Type == "InstantReload" && rev.reloading) FinishReload(false);
                if (e.Type == "CritNextShots") critShots += Mathf.RoundToInt(e.Value);
            }
        }

        void ChanceDodge()
        {
            FxBurst.Spawn(transform.position + Vector3.up, 1.6f, FxBurst.Kind.Gold, new Color(0.4f, 0.95f, 0.85f));
            foreach (var e in Effects.Global(this, "OnChanceDodge"))
                if (e.Type == "BulletRefund" && Effects.Roll(e)) Refund();
        }

        /// <summary>탄 회수: 방금 쏜 칸을 되돌림</summary>
        void Refund()
        {
            if (rev.reloading || rev.shotsThisCycle <= 0) return;
            rev.shotsThisCycle--;
            rev.next = rev.reversed ? Mathf.Min(12, rev.next + 1) : Mathf.Max(1, rev.next - 1);
            OnReloadEnd?.Invoke(false);
        }

        // ---------- 사격 ----------
        void FireNext()
        {
            int s = rev.next;
            var gun = rev.GunFor(s);
            bool refund = false;
            if (rev.slots[s].bullet != null)
            {
                float atkSpeed = AttackSpeedBonus + Effects.Passive(this, s, "AttackSpeedAdd") + nextShotSpeedAdd;
                nextShotSpeedAdd = 0;
                nextFireTime = Time.time + (gun != null ? gun.FireInterval : 0.3f) / Mathf.Max(0.2f, 1 + atkSpeed);
                refund = Shoot(s, aimDir, s % 2 == 1 ? rightMuzzle : leftMuzzle, 1f);
            }
            else
            {
                nextFireTime = Time.time + 0.06f;
                OnFired?.Invoke(s, false);
            }
            if (refund) { FxBurst.Spawn(transform.position + Vector3.up * 1.2f, 1.2f, FxBurst.Kind.Gold, new Color(0.4f, 0.95f, 0.85f)); return; }
            rev.shotsThisCycle++;
            rev.next = rev.reversed ? rev.next - 1 : rev.next + 1;
            if (rev.shotsThisCycle >= 12 || rev.next < 1 || rev.next > 12) StartReload();
        }

        /// <summary>칸 s의 기본 피해 (패시브 효과 포함, 대상 무관)</summary>
        public float ShotDamage(int s)
        {
            var slot = rev.slots[s];
            var b = slot.bullet;
            var gun = rev.GunFor(s);
            bool awake = rev.Awakened(s);
            int judge = rev.JudgeTime(s);
            float add = Effects.Passive(this, s, "DamageAdd");
            add += awake ? Effects.Passive(this, s, "AwakenedDamageAdd") : Effects.Passive(this, s, "UnawakenedDamageAdd");
            add += Effects.Passive(this, s, "DamagePerUnawakened") * rev.UnawakenedCount();
            if (judge < 0) add += Effects.Passive(this, s, "DamageByNegativeJudge") * -judge;
            float dmg = ((b != null ? b.Damage : 8) + slot.permanentBonus) * (gun != null ? gun.DamageMul : 1f) * DamageMul * (1 + add);
            if (s == 12) dmg *= 1.5f + CostumeStat("Slot12BonusMul");
            return dmg;
        }

        /// <summary>한 발: 거의 즉발 히트스캔 + 레이저 궤적. ghost = 실루엣 · 추가 사격 (발사 효과 없음). 반환: 탄 회수 여부</summary>
        public bool Shoot(int s, Vector3 dir, Transform muzzle, float mul, float targetDist = -1f, bool ghost = false, Vector3? fromPos = null)
        {
            var b = rev.slots[s].bullet;
            var gun = rev.GunFor(s);
            bool awake = rev.Awakened(s);
            float dmg = ShotDamage(s) * mul;
            bool refund = false;
            if (!ghost)
            {
                dmg *= 1 + nextShotDamageAdd;
                nextShotDamageAdd = 0;
                if (perfectBuff) { dmg *= 1.5f; perfectBuff = false; }
            }
            bool crit = Effects.Passive(this, s, "CritChance") > UnityEngine.Random.value || (!ghost && critShots > 0);
            if (!ghost && critShots > 0) critShots--;
            if (crit) dmg *= 2f;

            Vector3 basePos = fromPos ?? transform.position;
            Vector3 origin = muzzle && !fromPos.HasValue ? muzzle.position : basePos + Vector3.up * 0.9f;
            origin.y = basePos.y + 0.9f;
            var castFrom = basePos + Vector3.up * 0.9f;
            castFrom.y = transform.position.y + 0.9f;

            // 발사 효과
            if (!ghost)
                foreach (var e in Effects.For(this, s, "OnFire"))
                {
                    if (!Effects.CondOk(e, this, s, null) || !Effects.Roll(e)) continue;
                    switch (e.Type)
                    {
                        case "SilhouetteShot": StartCoroutine(SilhouetteShot(s, dir, e.Value)); break;
                        case "ExtraShot":
                            for (int k = 0; k < Mathf.Max(1, Mathf.RoundToInt(e.Value)); k++)
                                Shoot(s, Quaternion.Euler(0, (k % 2 == 0 ? 7 : -7) * (k / 2 + 1), 0) * dir, muzzle, e.Value2 > 0 ? e.Value2 : 0.5f, targetDist, true);
                            break;
                        case "DamageAdd": if (e.Target == "NextBullet") nextShotDamageAdd += e.Value; break;
                        case "NextBulletAttackSpeedAdd": nextShotSpeedAdd += e.Value; break;
                        case "Stun": Effects.AreaStun(transform.position, e.Value2 > 0 ? e.Value2 : 3f, Mathf.Max(0.3f, e.Duration) * ControlDurationMul); break;
                        case "DodgeChance": tempDodge = e.Value; tempDodgeUntil = Time.time + e.Duration; break;
                        case "BulletRefund": refund = true; break;
                        case "GlobalTimeStop":
                            if (Time.time >= globalStopReady)
                            {
                                Health.GlobalStopUntil = Time.time + Mathf.Max(0.5f, e.Duration) * ControlDurationMul;
                                globalStopReady = Time.time + (e.Value2 > 0 ? e.Value2 : 20f);
                                FxBurst.Spawn(transform.position + Vector3.up, 9f, FxBurst.Kind.Gold, new Color(0.6f, 0.75f, 1f));
                            }
                            break;
                    }
                }
            if (!ghost && Time.time < silhouetteFollowUntil) StartCoroutine(SilhouetteShot(s, dir, 0.3f));

            // 관통 · 튕김 · 유도
            int pierce = 0, ricochet = 0;
            int judge = rev.JudgeTime(s);
            foreach (var e in Effects.For(this, s, "OnHit")) { if (e.Type == "Pierce") pierce += Mathf.RoundToInt(e.Value); if (e.Type == "Ricochet") ricochet += Mathf.RoundToInt(e.Value); if (e.Type == "RicochetByJudgeTime" && Mathf.Abs(judge) >= e.Value) ricochet += 1; }
            foreach (var e in Effects.For(this, s, "Passive")) if (e.Type == "Ricochet") ricochet += Mathf.RoundToInt(e.Value);
            bool homing = Effects.Passive(this, s, "Homing") > 0;
            float range = gun != null ? gun.Range : 12f;

            var aimPoint = basePos + dir * (targetDist > 0 ? targetDist : aimDist);
            aimPoint.y = castFrom.y;
            var toAim = aimPoint - castFrom;
            if (toAim.sqrMagnitude > 0.25f) dir = toAim.normalized;
            if (homing)
            {
                Enemy best = null; float bestScore = 2.5f;
                foreach (var en in Enemy.All)
                {
                    if (!en || !en.health.Alive) continue;
                    var to = Effects.Flat(en.transform.position - castFrom);
                    float along = Vector3.Dot(to, dir);
                    if (along < 0 || along > range) continue;
                    float lateral = (to - dir * along).magnitude;
                    if (lateral < bestScore) { bestScore = lateral; best = en; }
                }
                if (best) dir = Effects.Flat(best.transform.position - castFrom).normalized;
            }

            var hits = Physics.SphereCastAll(castFrom, 0.3f, dir, range, ~0, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));
            Vector3 end = castFrom + dir * range;
            var hitSet = new HashSet<Health>();
            int pierced = 0;
            Color col = BulletLook.Shown(b, awake);
            foreach (var h in hits)
            {
                if (h.collider.transform.IsChildOf(transform)) continue;
                var hp = h.collider.GetComponentInParent<Health>();
                if (hp == null) { end = h.distance > 0 ? h.point : castFrom + dir * 0.5f; break; }
                if (!hp.Alive || hp.isPlayer || hitSet.Contains(hp)) continue;
                hitSet.Add(hp);
                ApplyHit(s, hp, dmg, h.point, h.distance, dir, col, crit);
                end = h.point;
                if (pierced >= pierce) break;
                pierced++;
                foreach (var e in Effects.For(this, s, "OnPierce")) if (e.Type == "PierceDamageGrowth") dmg *= 1 + e.Value;
            }
            Tracer.Spawn(origin, end, col, BulletLook.Width(b, awake) * (ghost ? 0.7f : 1f), awake && b != null && b.Grade == "Legendary");

            // 튕김: 맞은 자리에서 가장 가까운 다른 적으로
            Vector3 from = end;
            float stack = 0;
            for (int r = 0; r < ricochet && hitSet.Count > 0; r++)
            {
                var next = Effects.Nearest(from, 9f, hitSet);
                if (!next) break;
                foreach (var e in Effects.For(this, s, "OnRicochet")) if (e.Type == "DamageAddStack") stack += e.Value;
                var to = next.transform.position + Vector3.up * 0.9f;
                Tracer.Spawn(from, to, col, BulletLook.Width(b, awake) * 0.8f, false);
                hitSet.Add(next.health);
                ApplyHit(s, next.health, dmg * (1 + stack), to, Vector3.Distance(transform.position, to), (to - from).normalized, col, crit);
                from = to;
            }

            if (muzzle || !fromPos.HasValue) FxBurst.Spawn(origin + dir * 0.3f, ghost ? 0.5f : 0.7f, FxBurst.Kind.Muzzle, col);
            if (!ghost) OnFired?.Invoke(s, awake);
            return refund;
        }

        void ApplyHit(int s, Health target, float dmg, Vector3 point, float dist, Vector3 dir, Color col, bool crit)
        {
            var slotBullet = rev.slots[s].bullet;
            // 맞기 전 배율
            foreach (var e in Effects.For(this, s, "OnHit"))
            {
                if (!Effects.CondOk(e, this, s, target)) continue;
                if (e.Type == "DamageAdd") dmg *= 1 + e.Value;
                if (e.Type == "DamageByDistance") dmg *= 1 + Mathf.Min(e.Value * dist, e.Value2 > 0 ? e.Value2 : 1f);
            }
            if (target.Stunned) dmg *= 1 + Effects.Passive(this, s, "DamageVsStoppedAdd");
            bool wasLocked = target.TimeLocked;
            target.TakeDamage(dmg);
            rev.AddHeat(HeatPerHit);
            FxBurst.Spawn(point, crit ? 1.6f : 1.1f, FxBurst.Kind.Hit, col);
            bool killed = !target.Alive;
            var en = target.GetComponent<Enemy>();

            foreach (var e in Effects.For(this, s, "OnHit"))
            {
                if (killed && e.Type != "Explosion" && e.Type != "HeatGain" && e.Type != "Split" && e.Type != "TrailSilhouetteShot") continue;
                if (!Effects.CondOk(e, this, s, target) || !Effects.Roll(e)) continue;
                float ctl = ControlDurationMul;
                switch (e.Type)
                {
                    case "TimeLock":
                        target.ApplyTimeLock(Mathf.Max(0.3f, e.Duration) * ctl);
                        if (en) en.WatchTimeLock(this);
                        if (e.Value > 0) target.ApplyDefenseDown(e.Value, Mathf.Max(0.3f, e.Duration) * ctl);
                        break;
                    case "Stun": target.ApplyStun(Mathf.Max(0.3f, e.Duration) * ctl); break;
                    case "Slow": target.ApplySlow(e.Value, Mathf.Max(0.5f, e.Duration) * ctl); break;
                    case "Burn": target.ApplyBurn(Mathf.Max(1f, dmg * e.Value * 4f), Mathf.Max(1f, e.Duration)); break;
                    case "DefenseDown": target.ApplyDefenseDown(e.Value, Mathf.Max(1f, e.Duration)); break;
                    case "EnemyAttackSpeedDown": target.ApplyAttackSpeedDown(e.Value, Mathf.Max(1f, e.Duration) * ctl); break;
                    case "Knockback": target.Knockback(dir * Mathf.Max(0.3f, e.Value) * 2f); break;
                    case "MemoryMark": target.AddMark(e.Value, e.Value2 > 0 ? (int)e.Value2 : 10); break;
                    case "MarkBurst":
                        Effects.Explosion(target.transform.position, 2f, dmg * e.Value, new Color(1f, 0.55f, 0.85f));
                        target.ClearMarks();
                        break;
                    case "Execute":
                        if (target.Alive) { target.TakeDamage(target.hp + 1); foreach (var x in Effects.Global(this, "OnExecute")) if (x.Type == "HeatGain") rev.AddHeat(x.Value); }
                        break;
                    case "DelayedDamage":
                        if (e.Value2 > 0) StartCoroutine(DelayedBlast(target.transform.position, e.Value2, dmg * e.Value, Mathf.Max(0.2f, e.Duration)));
                        else target.AddDelayed(dmg * e.Value, Mathf.Max(0.2f, e.Duration));
                        break;
                    case "Explosion": Effects.Explosion(point, e.Value2 > 0 ? e.Value2 : 2.5f, dmg * e.Value, col); break;
                    case "HeatGain": rev.AddHeat(e.Value); break;
                    case "Split":
                        for (int k = 0; k < Mathf.Max(1, Mathf.RoundToInt(e.Value)); k++)
                        {
                            var sd = Quaternion.Euler(0, UnityEngine.Random.Range(-60f, 60f), 0) * dir;
                            var tgt = Effects.Nearest(point + sd * 3f, 6f, new HashSet<Health> { target });
                            var to = tgt ? tgt.transform.position + Vector3.up * 0.9f : point + sd * 5f;
                            Tracer.Spawn(point, to, col, 0.06f, false);
                            if (tgt) tgt.health.TakeDamage(dmg * (e.Value2 > 0 ? e.Value2 : 0.4f));
                        }
                        break;
                    case "TrailSilhouetteShot": StartCoroutine(TrailSilhouette(s, point, e.Value, Mathf.Max(0.3f, e.Duration))); break;
                }
            }
            if (killed)
                foreach (var e in Effects.For(this, s, "OnKill"))
                {
                    bool ok = e.Cond == "TargetTimeLocked" ? wasLocked : e.Cond == "KilledByDelayed" ? false : Effects.CondOk(e, this, s, null);
                    if (!ok || !Effects.Roll(e)) continue;
                    if (e.Type == "Explosion") Effects.Explosion(target.transform.position, e.Value2 > 0 ? e.Value2 : 2f, dmg * e.Value, col);
                    if (e.Type == "HeatGain") rev.AddHeat(e.Value);
                    if (e.Type == "PermanentDamageGrowth") rev.slots[s].permanentBonus = Mathf.Min(e.Value2 > 0 ? e.Value2 : 30, rev.slots[s].permanentBonus + e.Value);
                }
        }

        /// <summary>시간에 갇힌 적이 풀릴 때 (Enemy가 알려 줌)</summary>
        public void TimeLockEnded(Vector3 at)
        {
            foreach (var e in Effects.Global(this, "OnTimeLockEnd"))
                if (e.Type == "Explosion") Effects.Explosion(at, e.Value2 > 0 ? e.Value2 : 3f, 10 * DamageMul * Mathf.Max(0.3f, e.Value), new Color(0.55f, 0.8f, 1f));
        }

        /// <summary>지연 피해로 적이 죽었을 때</summary>
        public void DelayedKill(Vector3 at)
        {
            foreach (var e in Effects.Global(this, "OnKill"))
                if (e.Cond == "KilledByDelayed" && e.Type == "Explosion") Effects.Explosion(at, e.Value2 > 0 ? e.Value2 : 3f, 12 * DamageMul * e.Value, new Color(1f, 0.6f, 0.25f));
        }

        IEnumerator DelayedBlast(Vector3 at, float radius, float dmg, float after)
        {
            yield return new WaitForSeconds(after);
            Effects.Explosion(at, radius, dmg, new Color(1f, 0.6f, 0.25f));
        }

        IEnumerator SilhouetteShot(int s, Vector3 dir, float mul)
        {
            yield return new WaitForSeconds(0.07f);
            var side = Vector3.Cross(Vector3.up, dir).normalized * (UnityEngine.Random.value < 0.5f ? 1.1f : -1.1f);
            var pos = transform.position + side - dir * 0.4f;
            Afterimage.SpawnAt(model, pos, new Color(0.75f, 0.55f, 1f, 0.7f), 0.5f);
            Shoot(s, dir, null, mul, -1, true, pos);
            foreach (var e in Effects.Global(this, "OnSilhouette"))
                if (e.Type == "SilhouetteFollow") silhouetteFollowUntil = Time.time + Mathf.Max(1f, e.Duration);
        }

        IEnumerator TrailSilhouette(int s, Vector3 at, float mul, float after)
        {
            var pos = Effects.Flat(at) + Vector3.up * 0.05f;
            Afterimage.SpawnAt(model, pos, new Color(0.75f, 0.55f, 1f, 0.6f), after + 0.3f);
            yield return new WaitForSeconds(after);
            var tgt = Effects.Nearest(pos, 12f);
            if (!tgt) yield break;
            var d = Effects.Flat(tgt.transform.position - pos).normalized;
            Shoot(s, d, null, mul, Vector3.Distance(pos, tgt.transform.position), true, pos);
        }

        // ---------- 장전 ----------
        void StartReload()
        {
            rev.reloading = true;
            reloadDur = ReloadTime;
            reloadStart = Time.time;
            foreach (var e in Effects.Global(this, "OnReload"))
            {
                if (!Effects.Roll(e)) continue;
                switch (e.Type)
                {
                    case "HeatGain": rev.AddHeat(e.Value); break;
                    case "TimeSlowField": Effects.AreaSlow(transform.position, e.Value2 > 0 ? e.Value2 : 4f, e.Value, Mathf.Max(1f, e.Duration) * ControlDurationMul); break;
                    case "ClockBurst": StartCoroutine(ClockBurst(e.Value > 0 ? e.Value : 0.5f)); break;
                    case "ReverseCylinder": rev.reversed = !rev.reversed; break;
                }
            }
            OnReloadStart?.Invoke(reloadDur);
        }

        IEnumerator ClockBurst(float mul)
        {
            for (int k = 1; k <= 12; k++)
            {
                if (rev.slots[k].bullet == null) continue;
                var d = Quaternion.Euler(0, k * 30, 0) * Vector3.forward;
                Shoot(k, d, null, mul, 8f, true, transform.position);
                yield return new WaitForSeconds(0.02f);
            }
        }

        void TryPerfectReload()
        {
            float p = ReloadProgress;
            if (p > 0.45f && p < 0.65f) FinishReload(true);
            else reloadStart += 0.25f;
        }

        void FinishReload(bool perfect)
        {
            rev.reloading = false;
            rev.shotsThisCycle = 0;
            rev.next = rev.FirstSlot;
            if (perfect)
            {
                perfectBuff = true;
                FxBurst.Spawn(transform.position + Vector3.up, 2.2f, FxBurst.Kind.Gold, new Color(1f, 0.85f, 0.4f));
                foreach (var e in Effects.Global(this, "OnPerfectReload"))
                    if (e.Type == "Stun") Effects.AreaStun(transform.position, e.Value2 > 0 ? e.Value2 : 5f, Mathf.Max(0.3f, e.Duration) * ControlDurationMul);
            }
            OnReloadEnd?.Invoke(perfect);
        }

        public void ResetCylinder()
        {
            if (rev.reloading) FinishReload(false);
            else if (rev.shotsThisCycle != 0) { rev.shotsThisCycle = 0; rev.next = rev.FirstSlot; OnReloadEnd?.Invoke(false); }
        }

        // ---------- 총 스킬 (프로토타입: 백스텝 샷) ----------
        IEnumerator BackstepShot()
        {
            nextSkill = Time.time + 6f;
            foreach (var e in Effects.Global(this, "OnSkill")) if (e.Type == "HeatGain") rev.AddHeat(e.Value);
            var back = -aimDir;
            float t = 0;
            health.invulnerable = true;
            Afterimage.Spawn(model, new Color(1f, 0.85f, 0.45f, 0.6f));
            while (t < 0.15f) { cc.Move(back * (4f / 0.15f) * Time.deltaTime); t += Time.deltaTime; yield return null; }
            health.invulnerable = false;
            for (int i = 0; i < 2; i++)
            {
                Shoot(Mathf.Clamp(rev.next, 1, 12), aimDir, i == 0 ? rightMuzzle : leftMuzzle, 0.8f, -1, true);
                yield return new WaitForSeconds(0.08f);
            }
        }

        // ---------- 궁극기: 불릿 파티 ----------
        IEnumerator BulletParty()
        {
            ultimateActive = true;
            rev.heat = 0;
            health.invulnerable = true;
            OnUltimate?.Invoke();
            Time.timeScale = 0.5f;
            var start = transform.position;
            for (int s = 1; s <= 12; s++)
            {
                Enemy target = Effects.Nearest(transform.position, 16f);
                float best = target ? Vector3.Distance(target.transform.position, transform.position) : 8f;
                var dir = target ? (target.transform.position - transform.position) : Quaternion.Euler(0, s * 30, 0) * Vector3.forward;
                dir.y = 0;
                dir.Normalize();
                if (model) model.rotation = Quaternion.LookRotation(dir);
                if (rev.slots[s].bullet != null)
                {
                    Shoot(s, dir, s % 2 == 1 ? rightMuzzle : leftMuzzle, 1.6f, best, true);
                    Afterimage.SpawnAt(model, start, new Color(1f, 0.85f, 0.4f, 0.6f), 0.4f);        // 금빛 잔상도 함께 쏨
                    Shoot(s, Effects.Flat((target ? target.transform.position : start + dir * 6) - start).normalized, null, 0.6f, -1, true, start);
                }
                yield return new WaitForSecondsRealtime(0.07f);
            }
            Time.timeScale = 1f;
            health.invulnerable = false;
            ultimateActive = false;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            Time.timeScale = 1f;
        }
    }
}
