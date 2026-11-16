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
        public int armorLevel;                       // 방어구 단계 (방어구 강화 방에서 +1)
        public KataPoser poser;
        Animator anim;
        public Inventory inv = new Inventory();
        public Health health;
        public event Action<int, bool> OnFired;
        public event Action<float> OnReloadStart;
        public event Action<bool> OnReloadEnd;
        public event Action OnUltimate;
        /// <summary>닷지 액션 발동 (치명타 보정 시간)</summary>
        public event Action<float> OnDodgeAction;

        CharacterController cc;
        Vector3 aimDir = Vector3.forward;
        public Vector3 AimDir => aimDir;            // 강화형 무법자가 조준당했는지 볼 때 씀
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
        public bool UltimateActive => ultimateActive;
        // 실루엣 색 (주인공 붉은 스카프에 맞춘 붉은 계열): 대시 잔상 · 분신 사격 = 진홍, 대시 끝 · 백스텝 = 밝은 붉은빛, 닷지 = 분홍빛 붉은색
        static readonly Color SilCrimson = new Color(0.92f, 0.16f, 0.2f, 0.75f);
        static readonly Color SilHot = new Color(1f, 0.42f, 0.28f, 1f);
        static readonly Color SilDodge = new Color(1f, 0.32f, 0.42f, 0.85f);
        // 닷지 액션: 적의 공격을 대시로 직접 피하면 → 주변 시간이 느려지고(0.2배, 실시간 0.3초 + 0.3초 동안 회복) 짧은 시간 치명타 보정
        public const float DodgeCritMul = 1.5f, DodgeCritTime = 2.5f, DodgeSlowScale = 0.2f, DodgeSlowHold = 0.3f, DodgeSlowRecover = 0.3f;
        float dodgeCritUntil;
        public float DodgeCritLeft => Mathf.Max(0, dodgeCritUntil - Time.time);

        void Awake()
        {
            Instance = this;
            cc = GetComponent<CharacterController>();
            health = GetComponent<Health>();
            health.isPlayer = true;
            health.OnJustDodge += JustDodge;
            health.OnChanceDodge += ChanceDodge;
            Tables.Load();
            if (model) { poser = model.GetComponent<KataPoser>(); anim = model.GetComponentInChildren<Animator>(); }
            if (!GetComponent<ReloadMotion>()) gameObject.AddComponent<ReloadMotion>();     // 스피드로더 장전 동작
        }

        // ---------- 능력치 ----------
        // 방어구 단계표 (단계마다): 최대 체력 +15, 방어력 +4
        public const float ArmorHpPerLevel = 15f, ArmorDefPerLevel = 4f;
        public float MaxHp => (baseMaxHp + Meta.Stat("MaxHp") + armorLevel * ArmorHpPerLevel) * (1 + CostumeStat("MaxHpAdd"));
        public float Defense => Mathf.Max(0, armorLevel * ArmorDefPerLevel + Meta.Stat("Defense") + Effects.GlobalPassive(this, "DefenseAdd"));
        /// <summary>치명타 확률 = 기본 5% + 양손 총 합 + 효과 + 기억</summary>
        public float CritChance => 0.05f + (rev.rightGun != null ? rev.rightGun.CritChance : 0) + (rev.leftGun != null ? rev.leftGun.CritChance : 0)
                                   + Effects.GlobalPassive(this, "CritChanceAdd") + Meta.Stat("CritChance");
        /// <summary>칸 s를 쏠 때 실제 치명타 확률: (기본 + 그 탄 효과) × 닷지 배율, 최대 100%</summary>
        public float CritChanceFor(int s) => Mathf.Clamp01((CritChance + Effects.Passive(this, s, "CritChance")) * (Time.time < dodgeCritUntil ? DodgeCritMul : 1f));
        /// <summary>치명타 피해 배율 = 기본 150% + 양손 총 합 + 효과 + 기억</summary>
        public float CritDamage => 1.5f + (rev.rightGun != null ? rev.rightGun.CritDamage : 0) + (rev.leftGun != null ? rev.leftGun.CritDamage : 0)
                                   + Effects.GlobalPassive(this, "CritDamageAdd") + Meta.Stat("CritDamage");
        /// <summary>확률형 효과 발동률 배율</summary>
        public float ProcMul => 1 + Effects.GlobalPassive(this, "ProcChanceAdd") + Meta.Stat("ProcChance");
        public float MoveSpeedMul => Mathf.Max(0.3f, 1 + Effects.GlobalPassive(this, "MoveSpeedAdd") + Meta.Stat("MoveSpeed"));
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
        public float DashRechargeTime => dashRecharge * Mathf.Max(0.3f, 1 + Meta.Stat("DodgeCooldown") + Effects.GlobalPassive(this, "DodgeCooldown"));
        public float HeatPerHit => (1f / 12f) * (1 + Meta.Stat("HeatGain"));
        public float DamageTakenMul => (1 + CostumeStat("DamageTakenAdd")) * 100f / (100f + Defense);
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
            armorLevel = 0;
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
            if (model)
            {
                // 불릿 파티: 타마다 몸 중심을 축으로 홱 틀어 새 방향에 딱 멈춤 (계속 빙글빙글 돌지 않음)
                if (ultimateActive) UltTurnStep();
                // 궁이 끝나면 몸 중심 회전으로 생긴 작은 어긋남을 제자리로
                else if (model.localPosition.sqrMagnitude > 1e-6f)
                    model.localPosition = Vector3.MoveTowards(model.localPosition, Vector3.zero, 2f * Time.deltaTime);
            }
            if (!health.Alive || InputLocked || !Application.isFocused) return;
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
            if (model && !ultimateActive && faceDir.sqrMagnitude > 0.001f) model.rotation = Quaternion.Slerp(model.rotation, Quaternion.LookRotation(faceDir), 20f * Time.deltaTime);

            if (anim) anim.SetFloat("Speed", dashing ? 1f : mv.magnitude);
            if (poser && !ultimateActive) poser.SetAimWeight(combatEnabled ? 1f : 0f);
            if (!dashing)
            {
                cc.Move((mv * moveSpeed * MoveSpeedMul * (ultimateActive ? UltMoveMul : 1f) + Vector3.down * 9f) * Time.deltaTime);
                if (kb.spaceKey.wasPressedThisFrame && dashCharges >= 1)
                    StartCoroutine(Dash(mv.sqrMagnitude > 0.01f ? mv.normalized : faceDir));
            }
            if (ultimateActive) return;          // 불릿 파티 중엔 이동 · 대시만 (사격은 궁이 알아서)

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
            Afterimage.Spawn(model, SilCrimson);
            while (t < dashTime)
            {
                cc.Move(dir * (dashDistance / dashTime) * Time.deltaTime);
                t += Time.deltaTime;
                ghostT += Time.deltaTime;
                if (ghostT >= 0.035f) { ghostT = 0; Afterimage.Spawn(model, SilCrimson * new Color(1, 1, 1, 0.8f)); }
                yield return null;
            }
            dashing = false;
            Afterimage.Spawn(model, SilHot * new Color(1, 1, 1, 0.6f));
            yield return new WaitForSeconds(JustDodgeWindow);
            health.dashing = false;
            health.invulnerable = ultimateActive;         // 불릿 파티 중 대시는 무적을 풀지 않음
        }

        void JustDodge()
        {
            if (Time.time - lastJustDodge < 0.5f) return;
            lastJustDodge = Time.time;
            GunkataFx.DodgeBurst(transform.position, new Color(0.72f, 0.5f, 1f), true);   // 닷지 액션: 바닥 고리 두 겹 · 몸 감싸는 고리 · 사방 빛살 (3D)
            // 닷지 액션: 치명타 보정 + 주변 시간 느려짐
            dodgeCritUntil = Time.time + DodgeCritTime;
            Afterimage.Spawn(model, SilDodge);
            CameraFollow.Zoom(ultimateActive ? 0.8f : 0.9f);
            if (!ultimateActive) StartCoroutine(DodgeSlow());
            OnDodgeAction?.Invoke(DodgeCritTime);
            foreach (var e in Effects.Global(this, "OnJustDodge"))
            {
                if (e.Type == "InstantReload" && rev.reloading) FinishReload(false);
                if (e.Type == "CritNextShots") critShots += Mathf.RoundToInt(e.Value);
            }
        }

        IEnumerator DodgeSlow()
        {
            if (Time.timeScale < 0.99f) yield break;           // 멈춤 화면 · 다른 연출 중이면 건드리지 않음
            Time.timeScale = DodgeSlowScale;
            yield return new WaitForSecondsRealtime(DodgeSlowHold);
            for (float t = 0; t < DodgeSlowRecover; t += Time.unscaledDeltaTime)
            {
                if (Time.timeScale == 0f) yield break;           // 그 사이 멈춤 화면이 열림
                Time.timeScale = Mathf.Lerp(DodgeSlowScale, 1f, t / DodgeSlowRecover);
                yield return null;
            }
            if (Time.timeScale > 0f) Time.timeScale = 1f;
            if (!ultimateActive) CameraFollow.Zoom(1f);
        }

        void ChanceDodge()
        {
            GunkataFx.DodgeBurst(transform.position, new Color(0.35f, 0.95f, 0.85f), false);
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
                if (poser) poser.Recoil(s % 2 == 1);              // 쏜 손 반동
            }
            else
            {
                nextFireTime = Time.time + 0.06f;
                OnFired?.Invoke(s, false);
            }
            if (refund) { GunkataFx.Flare(transform.position + Vector3.up * 1.2f, 1.2f, new Color(0.4f, 0.95f, 0.85f)); return; }
            rev.shotsThisCycle++;
            rev.next = rev.reversed ? rev.next - 1 : rev.next + 1;
            if (rev.shotsThisCycle >= 12 || rev.next < 1 || rev.next > 12) StartReload();
        }

        /// <summary>발사 효과(OnFire): 활성화된 탄 · 양손 총 · 특성 · 코스튬. 평소 사격과 불릿 파티가 같이 씀. 반환 = 탄 회수</summary>
        bool FireEffects(int s, Vector3 dir, Transform muzzle, float targetDist)
        {
            bool refund = false;
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
                            GunkataFx.Flare(transform.position + Vector3.up, 9f, new Color(0.6f, 0.75f, 1f));
                        }
                        break;
                }
            }
            return refund;
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
            bool crit = CritChanceFor(s) > UnityEngine.Random.value || (!ghost && critShots > 0);
            if (!ghost && critShots > 0) critShots--;
            if (crit) dmg *= CritDamage;

            Vector3 basePos = fromPos ?? transform.position;
            Vector3 origin = muzzle && !fromPos.HasValue ? muzzle.position : basePos + Vector3.up * 0.9f;
            origin.y = basePos.y + 0.9f;
            var castFrom = basePos + Vector3.up * 0.9f;
            castFrom.y = transform.position.y + 0.9f;

            // 발사 효과
            if (!ghost) refund = FireEffects(s, dir, muzzle, targetDist);
            if (!ghost && Time.time < silhouetteFollowUntil) StartCoroutine(SilhouetteShot(s, dir, 0.3f));

            // 관통 · 튕김 · 유도
            int pierce = 0, ricochet = 0;
            int judge = rev.JudgeTime(s);
            foreach (var e in Effects.For(this, s, "OnHit")) { if (e.Type == "Pierce") pierce += Mathf.RoundToInt(e.Value); if (e.Type == "Ricochet") ricochet += Mathf.RoundToInt(e.Value); if (e.Type == "RicochetByJudgeTime" && Mathf.Abs(judge) >= e.Value) ricochet += 1; }
            foreach (var e in Effects.For(this, s, "Passive")) if (e.Type == "Ricochet" && (!e.Borrowed || Effects.Roll(e))) ricochet += Mathf.RoundToInt(e.Value);
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
                GunkataFx.HitImpact(h.point, dir, col, false, 1f, true);          // 관통 불꽃 (3D)
                foreach (var e in Effects.For(this, s, "OnPierce")) if (e.Type == "PierceDamageGrowth") dmg *= 1 + e.Value;
            }
            bool resonance = !ghost && awake && b != null && b.Id != 1000;
            Tracer.Spawn(origin, end, col, BulletLook.Width(b, awake) * (ghost ? 0.7f : resonance ? 1.6f : 1f), resonance || (awake && b != null && b.Grade == "Legendary"));
            if (resonance)
            {
                // 공명 사격: 금빛 섬광 + 맞은 자리 탄 색 폭발(피해 30%) + 화면 흔들림
                GunkataFx.Flare(origin + dir * 0.5f, 1.8f, col);
                if (hitSet.Count > 0) Effects.Explosion(end, 2f, dmg * 0.3f, col);
                CameraFollow.Shake(b.Grade == "Legendary" ? 0.22f : b.Grade == "Epic" ? 0.16f : 0.1f);
            }

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
                GunkataFx.HitImpact(from, (to - from).normalized, col, false, 1f, true);
                hitSet.Add(next.health);
                ApplyHit(s, next.health, dmg * (1 + stack), to, Vector3.Distance(transform.position, to), (to - from).normalized, col, crit);
                from = to;
            }

            if (muzzle || !fromPos.HasValue)
            {
                // 리볼버 총구 섬광 (가는 빨간 3D + 실린더 틈 가스). 총구에서 쏠 때만 총을 넘겨 실린더 · 공이 자리를 잡음
                GunkataFx.MuzzleFlash(origin, Effects.Flat(dir).normalized, ghost ? 0.6f : 0.85f, muzzle && !fromPos.HasValue ? muzzle.parent : null);
            }
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
            // 피해 유형별 증폭 (탄 고유 효과)
            if (target.IsBoss) dmg *= 1 + Effects.GlobalPassive(this, "BossDamageAdd");
            if (target.HasStatus) dmg *= 1 + Effects.GlobalPassive(this, "StatusDamageAdd");
            bool wasLocked = target.TimeLocked;
            target.TakeDamage(dmg);
            rev.AddHeat(HeatPerHit);
            // 타격 이펙트 (3D 툰 불꽃, 탄 색): 섬광 덩어리 + 튀는 가시 + 충격 고리, 치명타는 금빛 네 갈래 별 + 큰 고리
            GunkataFx.HitImpact(point, dir, col, crit);
            if (crit) CameraFollow.Shake(0.07f);
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
                    case "Knockback": target.Knockback(dir * Mathf.Max(0.3f, e.Value) * 2f); FxBurst.Spawn(target.transform.position + Vector3.up * 0.3f, 1.2f, FxBurst.Kind.Dust, Color.white); break;
                    case "MemoryMark": target.AddMark(e.Value, e.Value2 > 0 ? (int)e.Value2 : 10); break;
                    case "MarkBurst":
                        Effects.Explosion(target.transform.position, 2f, dmg * e.Value, new Color(1f, 0.55f, 0.85f));
                        target.ClearMarks();
                        break;
                    case "Execute":
                        FxBurst.Spawn(target.transform.position + Vector3.up * 0.9f, 2.2f, FxBurst.Kind.Slash, Color.white); CameraFollow.Shake(0.1f);
                        if (target.Alive) { target.TakeDamage(target.hp + 1); foreach (var x in Effects.Global(this, "OnExecute")) if (x.Type == "HeatGain") rev.AddHeat(x.Value); }
                        break;
                    case "DelayedDamage":
                        FxBurst.Spawn(target.transform.position + Vector3.up * 0.9f, e.Value2 > 0 ? e.Value2 * 1.4f : 1.6f, FxBurst.Kind.Converge, Color.white, Mathf.Max(0.2f, e.Duration));
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
            Afterimage.SpawnAt(model, pos, SilCrimson, 0.5f);
            Shoot(s, dir, null, mul, -1, true, pos);
            foreach (var e in Effects.Global(this, "OnSilhouette"))
                if (e.Type == "SilhouetteFollow") silhouetteFollowUntil = Time.time + Mathf.Max(1f, e.Duration);
        }

        IEnumerator TrailSilhouette(int s, Vector3 at, float mul, float after)
        {
            var pos = Effects.Flat(at) + Vector3.up * 0.05f;
            Afterimage.SpawnAt(model, pos, SilCrimson * new Color(1, 1, 1, 0.8f), after + 0.3f);
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
                GunkataFx.Flare(transform.position + Vector3.up, 2.2f, new Color(1f, 0.85f, 0.4f));
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
            Afterimage.Spawn(model, SilHot * new Color(1, 1, 1, 0.65f));
            while (t < 0.15f) { cc.Move(back * (4f / 0.15f) * Time.deltaTime); t += Time.deltaTime; yield return null; }
            health.invulnerable = false;
            for (int i = 0; i < 2; i++)
            {
                Shoot(Mathf.Clamp(rev.next, 1, 12), aimDir, i == 0 ? rightMuzzle : leftMuzzle, 0.8f, -1, true);
                if (poser) poser.Recoil(i == 0);
                yield return new WaitForSeconds(0.08f);
            }
        }

        // ---------- 궁극기: 불릿 파티 (건카타) ----------
        // 사미라 궁 느낌: 게임은 그대로 흐르고, 플레이어는 무적 상태로 WASD 이동(대시 가능)하면서 약 2초 동안
        // 타마다 몸 중심을 축으로 제각각 각도로 홱 틀어(NextUltTurn) 새 방향에 박히고, 건카타 8자세로 오른손 → 왼손 연사 + 상체 반동.
        // 매 타마다 금빛 실루엣 분신 3~5개가 무작위 자리로 대시해 조금씩 어긋난 순간에 총구 폭발. 한 타 안의 총구 방향은 모두 다름 (ClaimDir).
        // 마무리는 별 모양 일제 사격 없이 마지막 탕-탕 한 번 후 연기 속에 끝.
        // 플레이어를 따라다니는 원형 콜라이더(BulletPartyZone) 안의 적 전부가 12번 피해. 범위는 원 안에 떨어지는 총알 비로 보임.
        static readonly int[] KataSeq = { 0, 3, 1, 5, 2, 4, 6, 7, 0, 3, 1, 7 };
        public const float UltDuration = 2.0f;        // 지속 시간 (12타)
        public const float UltRadius = 6.5f;          // 원형 콜라이더 반지름 (일반 방 24×20의 가운데 넓은 원)
        public const float UltTickMul = 0.9f;         // 한 타당 피해 배율 (칸 피해 기준, 원 안의 모든 적에게)
        public const float UltMoveMul = 0.9f;         // 궁 중 이동 속도 배율
        public const float UltSpinSpeed = 900f;       // 몸 주위 소용돌이 곡선이 도는 속도 (도/초)
        public const int UltClones = 4;               // 한 타에 본체와 동시에 쏘는 실루엣 분신 수
        public const float UltCloneDist = 1.6f;       // 분신이 본체에서 떨어진 거리 (m)
        const int UltRainPerTick = 7;                // 한 타에 원 안 아무 데나 떨어지는 총알 비 수
        static readonly Color UltGold = new Color(1f, 0.82f, 0.38f);
        static readonly Color UltGhost = new Color(1f, 0.68f, 0.22f, 0.95f);    // 궁 분신: 금빛 주황 실루엣 (총구 섬광 ToonFire 노랑~주황에 맞춤)

        const float UltTurnTime = 0.07f;              // 한 번 트는 시간 (살짝 지나쳤다 딱 멈춤)
        const float UltAimDelay = 0.07f;              // 틀고 자세 잡은 뒤 쏘기까지
        const float UltDoubleTap = 0.06f;             // 오른손 → 왼손 연사 간격
        const float UltDirGap = 26f;                  // 한 타 안에서 총구 방향끼리 최소 벌어짐 (도)
        float ultYawFrom, ultYawTo, ultTurnT = 1, ultTurnSign = 1;
        readonly List<Vector3> ultDirs = new();       // 이번 타에 이미 쏜 총구 방향들

        /// <summary>다음에 홱 틀 각 (+ = 시계): 70~220° 제각각, 대체로 좌우를 엇갈리지만 가끔 같은 쪽으로 연달아 꺾음</summary>
        float NextUltTurn()
        {
            if (UnityEngine.Random.value < 0.7f) ultTurnSign = -ultTurnSign;
            return ultTurnSign * UnityEngine.Random.Range(70f, 220f);
        }

        /// <summary>이번 타의 다른 총구들과 UltDirGap° 이상 떨어진 방향을 고름 (원하는 방향에서 가장 가까운 빈 자리, 시도 쪽은 무작위)</summary>
        Vector3 ClaimDir(Vector3 d)
        {
            float sign = UnityEngine.Random.value < 0.5f ? -1f : 1f;
            var pick = d;
            for (int step = 0; step < 28; step++)
            {
                float off = step == 0 ? 0 : ((step + 1) / 2) * 13f * (step % 2 == 1 ? sign : -sign);
                var c = Quaternion.AngleAxis(off, Vector3.up) * d;
                bool free = true;
                foreach (var u in ultDirs) if (Vector3.Angle(Effects.Flat(c), Effects.Flat(u)) < UltDirGap) { free = false; break; }
                if (free) { pick = c; break; }
            }
            ultDirs.Add(pick);
            return pick;
        }

        void UltTurn(float delta)
        {
            if (!model) return;
            ultYawFrom = model.eulerAngles.y;
            ultYawTo = ultYawFrom + delta;
            ultTurnT = 0;
        }

        void UltTurnStep()
        {
            if (ultTurnT >= 1) return;
            ultTurnT = Mathf.Min(1, ultTurnT + Time.deltaTime / UltTurnTime);
            // ease-out-back: 빠르게 돌아 목표를 살짝 지나쳤다가 제자리에 박힘
            const float c1 = 1.70158f, c3 = c1 + 1;
            float x = ultTurnT - 1;
            float e = 1 + c3 * x * x * x + c1 * x * x;
            RotateModelAroundBody(Quaternion.Euler(0, Mathf.LerpUnclamped(ultYawFrom, ultYawTo, e), 0));
        }

        IEnumerator BulletParty()
        {
            ultimateActive = true;
            rev.heat = 0;
            health.invulnerable = true;
            OnUltimate?.Invoke();
            var zone = BulletPartyZone.Spawn(transform, UltRadius);
            CameraFollow.Zoom(0.8f);
            CameraFollow.Shake(0.15f);
            yield return new WaitForFixedUpdate();             // 콜라이더가 첫 판정을 잡을 때까지
            float tick = UltDuration / 12f;
            for (int i = 0; i < 12 && health.Alive; i++)
            {
                UltTurn(i == 0 ? 0 : NextUltTurn());
                if (poser) poser.Snap(KataSeq[i], 0.06f);
                yield return new WaitForSeconds(UltAimDelay);  // 홱 틀어 자세가 잡힌 뒤 쏨
                UltVolley(i + 1, i, zone ? zone.Targets() : new List<Health>(), zone);
                rev.heat = 0;                                 // 궁 피해로는 열기가 차지 않음
                yield return new WaitForSeconds(Mathf.Max(0.02f, tick - UltAimDelay));
            }
            // 마무리: 별 모양 일제 사격 없이 마지막으로 한 번 더 홱 틀어 탕-탕 쏘고 연기 속에 끝
            if (zone) zone.Close();
            UltTurn(NextUltTurn());
            if (poser) poser.Snap(0, 0.05f);
            yield return new WaitForSeconds(UltAimDelay);
            ultDirs.Clear();
            BodyBlast(true, 1.15f, true);
            if (poser) { poser.Recoil(true); poser.BodyKick(1f); }
            yield return new WaitForSeconds(UltDoubleTap);
            BodyBlast(false, 1.15f, true);
            if (poser) { poser.Recoil(false); poser.BodyKick(0.8f); }
            // 끝: 연기와 소용돌이만 남아 흩어짐
            var center = (model ? model.position : transform.position) + Vector3.up * 0.9f;
            for (int k = 0; k < 5; k++) GunkataFx.Swirl(center, UnityEngine.Random.Range(1.2f, 2.3f), -UltSpinSpeed * 0.6f, 0.6f);
            for (int k = 0; k < 3; k++) GunkataFx.Smoke(center + UnityEngine.Random.insideUnitSphere * 0.8f, UnityEngine.Random.Range(2.2f, 3f), 1.2f);
            CameraFollow.Shake(0.22f);
            yield return new WaitForSeconds(0.25f);
            if (poser) poser.Release(0.15f);
            CameraFollow.Zoom(1f);
            rev.heat = 0;
            health.invulnerable = false;
            ultimateActive = false;
        }

        /// <summary>불릿 파티 한 타: 본체 + 분신 동시 총구 폭발, 원 안의 모든 적에게 피해 (궤적 없음)</summary>
        void UltVolley(int s, int i, List<Health> foes, BulletPartyZone zone)
        {
            var b = rev.slots[s].bullet;
            bool awake = rev.Awakened(s);
            Color col = BulletLook.Shown(b, awake);
            var c = transform.position;
            // 활성화된 총탄 · 특성 · 총 · 코스튬의 발사 효과도 그대로 (분신 사격 · 추가 사격 · 기절 · 시간 정지 등).
            // 맞힌 효과(OnHit) · 처치 효과(OnKill)는 아래 ApplyHit가 적용
            var near = Effects.Nearest(c, UltRadius);
            var fireDir = near ? Effects.Flat(near.transform.position - c).normalized : BarrelDir(i % 2 == 0);
            FireEffects(s, fireDir, i % 2 == 0 ? rightMuzzle : leftMuzzle, near ? Vector3.Distance(c, near.transform.position) : -1f);
            float baseDmg = ShotDamage(s) * UltTickMul * (1 + nextShotDamageAdd);
            nextShotDamageAdd = 0;

            if (zone) zone.Pulse(s);
            ultDirs.Clear();                              // 이번 타의 총구 방향은 모두 제각각 (ClaimDir)
            // 본체: 오른손 → 왼손 연사 (탕-탕). 쏠 때마다 팔이 튀고 상체가 뒤로 젖혀짐
            BodyBlast(true, 1f, i % 2 == 0);
            if (poser) { poser.Recoil(true); poser.BodyKick(1f); }
            StartCoroutine(UltLeftTap(i % 2 == 1));
            // 분신: 타마다 수 · 자리 · 거리 · 쏘는 순간이 제각각 (고른 간격 없이)
            CloneStrikes(UltClones + UnityEngine.Random.Range(-1, 2), foes, 0.16f);
            // 가운데 쌓이는 회색 연기 + 몸 주위를 도는 어두운 소용돌이 (본체와 같은 방향으로 → 회전감)
            var mid = (model ? model.position : c) + Vector3.up * 0.9f;
            GunkataFx.Smoke(mid + UnityEngine.Random.insideUnitSphere * 0.7f, UnityEngine.Random.Range(1.6f, 2.4f));
            for (int k = 0; k < 2; k++)
                GunkataFx.Swirl(mid + Vector3.up * UnityEngine.Random.Range(-0.4f, 0.4f), UnityEngine.Random.Range(1.0f, 1.9f), -UltSpinSpeed * 0.9f);
            // 총알 비: 원 안 아무 데나 탄착 (3D로 구운 플립북: 섬광 · 튀는 불똥 · 고리 · 먼지). 범위가 보임
            if (zone)
                for (int k = 0; k < UltRainPerTick; k++)
                {
                    var g = zone.RandomPoint();
                    if (GunkataFx.Impact(g, UnityEngine.Random.Range(0.7f, 1.05f))) continue;
                    // 구운 그림이 없을 때 (Cat_Tang/Bake Impact FX 전): 셰이더 탄착
                    var p = g + Vector3.up * 0.25f;
                    FxBurst.Spawn(p, UnityEngine.Random.Range(0.7f, 1.1f), FxBurst.Kind.Spark, UltGold);
                    if (k % 3 == 0) FxBurst.Spawn(p, 1.0f, FxBurst.Kind.Dust, Color.white);
                }
            // 원 안의 적 전부 적중
            foreach (var h in foes)
            {
                if (!h || !h.Alive) continue;
                var to = h.transform.position + Vector3.up * 0.9f;
                var dir = Effects.Flat(to - c);
                dir = dir.sqrMagnitude > 0.001f ? dir.normalized : transform.forward;
                bool crit = CritChanceFor(s) > UnityEngine.Random.value;
                ApplyHit(s, h, crit ? baseDmg * CritDamage : baseDmg, to, Vector3.Distance(c, to), dir, col, crit);
                // 한 적에게 여러 발이 쏟아지는 느낌: 몸 주변 탄착 두 개 + 금빛 X
                for (int k = 0; k < 2; k++)
                    FxBurst.Spawn(to + UnityEngine.Random.insideUnitSphere * 0.5f, 0.8f, FxBurst.Kind.Spark, col);
                if (i % 3 == 0) FxBurst.Spawn(to, 1.4f, FxBurst.Kind.Slash, UltGold);
            }
            CameraFollow.Shake(foes.Count > 0 ? 0.1f : 0.06f);
        }

        IEnumerator UltLeftTap(bool smoke)
        {
            yield return new WaitForSeconds(UltDoubleTap);
            if (!ultimateActive) yield break;
            BodyBlast(false, 1f, smoke);
            if (poser) { poser.Recoil(false); poser.BodyKick(0.7f); }
        }

        /// <summary>분신 n개 (사미라 궁 · 불릿 파티 느낌): 몸에서 대시하듯 잔상을 끌며 둘레로 뛰쳐나가, 제각각 방향으로 쏘고 금방 사라짐.
        /// 대칭 없이: 자리(서로 40° 이상만 떨어진 무작위 각) · 거리 · 바라보는 쪽 · 대시 시간(= 쏘는 순간) · 섬광 크기가 매번 다름.
        /// 적이 있으면 한 분신은 그 적 쪽을 봄. 총구 방향은 ClaimDir로 이번 타의 다른 총구와 겹치지 않음</summary>
        void CloneStrikes(int n, List<Health> foes, float life)
        {
            if (!model) return;
            var basePos = transform.position;
            var center = model.position;
            var rootOff = model.position - transform.position;
            Vector3 muzR = rightMuzzle ? rightMuzzle.position - center : Vector3.zero, muzL = leftMuzzle ? leftMuzzle.position - center : Vector3.zero;
            Vector3 barR = BarrelDir(true), barL = BarrelDir(false);
            var fwd0 = Effects.Flat(model.forward).normalized;
            var angles = new List<float>();
            for (int k = 0; k < n; k++)
            {
                float ang = UnityEngine.Random.Range(0f, 360f);
                for (int tries = 0; tries < 20; tries++)
                {
                    bool ok = true;
                    foreach (var o in angles) if (Mathf.Abs(Mathf.DeltaAngle(o, ang)) < 40f) { ok = false; break; }
                    if (ok) break;
                    ang = UnityEngine.Random.Range(0f, 360f);
                }
                angles.Add(ang);
                var dir = Quaternion.Euler(0, ang, 0) * Vector3.forward;
                var spot = basePos + dir * UnityEngine.Random.Range(0.7f, 2.0f);
                Health tgt = k == 0 && foes != null && foes.Count > 0 ? foes[UnityEngine.Random.Range(0, foes.Count)] : null;
                Quaternion facing = tgt ? Quaternion.LookRotation(Effects.Flat(tgt.transform.position - spot).normalized, Vector3.up)
                    : Quaternion.LookRotation(Quaternion.Euler(0, UnityEngine.Random.Range(-55f, 55f), 0) * dir, Vector3.up);
                float dash = UnityEngine.Random.Range(0.07f, 0.14f);          // 분신마다 도착 · 사격 순간이 어긋남
                Afterimage.DashIn(model, basePos, spot, facing, UltGhost, dash, life);
                var rot = Quaternion.Euler(0, Quaternion.LookRotation(facing * Vector3.forward).eulerAngles.y - Quaternion.LookRotation(fwd0).eulerAngles.y, 0);
                StartCoroutine(CloneFire(spot + rootOff, rot, muzR, muzL, barR, barL, UnityEngine.Random.Range(0.65f, 0.95f), dash));
            }
        }

        IEnumerator CloneFire(Vector3 center, Quaternion rot, Vector3 muzR, Vector3 muzL, Vector3 barR, Vector3 barL, float scale, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (!ultimateActive) yield break;
            Vector3 pR = center + rot * muzR, pL = center + rot * muzL;
            var dR = OutwardDir(pR, rot * barR, center);
            var dL = OutwardDir(pL, rot * barL, center);
            if (muzR != Vector3.zero && muzL != Vector3.zero) Splay(ref dR, ref dL, UnityEngine.Random.Range(25f, 70f), UnityEngine.Random.Range(25f, 70f));
            if (muzR != Vector3.zero) BlastDir(pR, ClaimDir(dR), scale, false);
            if (muzL != Vector3.zero) BlastDir(pL, ClaimDir(dL), scale, false);
        }

        /// <summary>한 몸의 두 총구가 70°보다 가까운 쪽을 보면 둘의 가운데에서 오른총은 오른쪽으로 hR°, 왼총은 왼쪽으로 hL° 벌림
        /// (벌어지는 각이 좌우 따로라 대칭이 아님)</summary>
        static void Splay(ref Vector3 dR, ref Vector3 dL, float hR, float hL)
        {
            float ang = Vector3.SignedAngle(dR, dL, Vector3.up);       // + = 왼총이 오른총보다 시계 방향(오른쪽)
            if (Mathf.Abs(ang) >= 70f) return;
            var mean = dR + dL;
            mean = mean.sqrMagnitude > 1e-4f ? mean.normalized : dR;
            float s = Mathf.Abs(ang) < 5f ? -1f : Mathf.Sign(ang);     // 거의 겹치면 오른총을 오른쪽으로
            dR = Quaternion.AngleAxis(-s * hR, Vector3.up) * mean;
            dL = Quaternion.AngleAxis(s * hL, Vector3.up) * mean;
        }

        Vector3 BarrelDir(bool right)
        {
            var g = poser ? (right ? poser.gunR : poser.gunL) : null;
            return g ? g.forward : (model ? model.forward : transform.forward);
        }

        /// <summary>본체 총구 폭발 (right = 오른총). 두 총이 같은 쪽을 보면 벌려서 쏨</summary>
        void BodyBlast(bool right, float scale, bool smoke)
        {
            if (!model) return;
            var c = model.position;
            var dR = rightMuzzle ? OutwardDir(rightMuzzle.position, BarrelDir(true), c) : Vector3.zero;
            var dL = leftMuzzle ? OutwardDir(leftMuzzle.position, BarrelDir(false), c) : Vector3.zero;
            if (rightMuzzle && leftMuzzle) Splay(ref dR, ref dL, 35f, 55f);
            var m = right ? rightMuzzle : leftMuzzle;
            if (m) BlastDir(m.position, ClaimDir(right ? dR : dL), scale, true, smoke);
        }

        /// <summary>화염 방향 = 항상 바깥. 총열이 이미 바깥(몸 중심 → 총구 쪽)을 향하면 총열 방향, 아니면 몸 중심에서 총구로 뻗는 방향.
        /// 위 · 아래로 쏘는 자세도 위에서 보면 짧은 토막이 되지 않게 수평으로 눕힘</summary>
        Vector3 OutwardDir(Vector3 p, Vector3 barrel, Vector3 center)
        {
            var radial = Effects.Flat(p - center);
            var flat = Effects.Flat(barrel);
            if (radial.sqrMagnitude < 0.01f) radial = flat;
            radial = radial.sqrMagnitude > 1e-4f ? radial.normalized : transform.forward;
            if (flat.sqrMagnitude > 0.04f && Vector3.Dot(flat.normalized, radial) > 0.5f) return flat.normalized;
            return radial;
        }

        /// <summary>총구 폭발 한 번 (d = 이미 정한 수평 방향): 3D 섬광 (+ 본체는 비스듬한 가시 · 불똥 · 연기)</summary>
        void BlastDir(Vector3 p, Vector3 d, float scale, bool full, bool smoke = false)
        {
            GunkataFx.Burst(p, d, scale, full);
            if (smoke) GunkataFx.Smoke(p + d * 0.5f + Vector3.up * 0.2f, 1.4f * scale, 0.7f);
        }

        /// <summary>회전축 = 몸 중심 (골반 바로 아래). 모델 원점이 아니라 캐릭터 몸을 축으로 돌림</summary>
        Vector3 BodyPivot()
        {
            var h = poser ? poser.Hips : null;
            var p = h ? h.position : model.position;
            p.y = model.position.y;
            return p;
        }

        void RotateModelAroundBody(Quaternion next)
        {
            var pivot = BodyPivot();
            var delta = next * Quaternion.Inverse(model.rotation);
            var off = model.position - pivot;
            model.rotation = next;
            model.position = pivot + delta * off;
        }


        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            Time.timeScale = 1f;
        }
    }
}
