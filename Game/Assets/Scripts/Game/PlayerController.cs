// 플레이어: WASD 이동 · 마우스 조준 · 좌클릭 사격 · Space 대시(최대 3칸, 1칸당 2초, 실루엣 잔상) · 우클릭 총 스킬
// Q 불릿 파티(열기 3칸) · E 상호작용. 탄 · 총 · 특성 · 코스튬 효과는 Effects로 모아 발동.
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CatTang
{
    [RequireComponent(typeof(CharacterController), typeof(Health))]
    public partial class PlayerController : MonoBehaviour
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
        public event Action OnReloadEnd;
        /// <summary>칸 s 탄의 공격력이 영구로 올랐을 때 (칸, 오른 양) — 레테의 물결</summary>
        public event Action<int, int> OnSlotPowerUp;
        public event Action OnUltimate;
        /// <summary>닷지 액션 발동 (치명타 보정 시간)</summary>
        public event Action<float> OnDodgeAction;

        CharacterController cc;
        Vector3 aimDir = Vector3.forward;
        public Vector3 AimDir => aimDir;            // 강화형 무법자가 조준당했는지 볼 때 씀
        float aimDist = 8f;
        public float AimDist => aimDist;
        /// <summary>다음에 쏠 칸을 쏘는 총의 사거리 (조준 커서가 사거리 밖을 표시할 때 씀)</summary>
        public float NextRange { get { var g = rev != null ? rev.GunFor(rev.next) : null; return g != null ? g.Range : 12f; } }
        float nextFireTime, nextSkill, reloadStart, reloadDur, dashCharges = 3, dashTimer;
        bool dashing, ultimateActive;
        AudioSource ultSpin;                                   // 건카타 회전음 (판이 도중에 끝나도 OnDestroy에서 끔)
        Vector3 dashFrom;
        // 효과 상태
        float nextShotDamageAdd, nextShotSpeedAdd, tempDodge, tempDodgeUntil, silhouetteFollowUntil, globalStopReady, lastJustDodge;
        int critShots;
        public bool InputLocked { get; set; }
        public float SkillCooldownLeft => Mathf.Max(0, nextSkill - Time.time);
        public float DashCharges => dashCharges;
        public float DashRechargeProgress => dashCharges >= dashMax ? 1 : dashTimer / DashRechargeTime;
        /// <summary>이 시각까지 대시 충전이 멈춤 (2장 차장+ 검표 펀치)</summary>
        [HideInInspector] public float dashLockUntil;
        public bool DashLocked => Time.time < dashLockUntil;
        // 3장 신기루 마술사 '진짜 찾기': 섞는 동안 바늘이 바뀌지 않게 잠깐 사격 봉인
        [HideInInspector] public float fireLockUntil;
        public bool FireLocked => Time.time < fireLockUntil;
        public float ReloadProgress => rev.reloading ? Mathf.Clamp01((Time.time - reloadStart) / reloadDur) : 0;
        public Interactable Focus { get; private set; }
        public bool UltimateActive => ultimateActive;
        // 실루엣 색 (주인공 붉은 스카프에 맞춘 붉은 계열): 대시 잔상 · 분신 사격 = 진홍, 대시 끝 · 백스텝 = 밝은 붉은빛, 닷지 = 분홍빛 붉은색
        static readonly Color SilHot = new Color(1f, 0.42f, 0.28f, 1f);
        static readonly Color SilDodge = new Color(0.45f, 0.75f, 1f, 0.85f);     // 극한 회피 잔상: 젠레스 존 제로식 파란빛
        // 2026-11-17 사용자: 대시 · 회상 실루엣을 하늘색 계열로 (가시성). 대시 = 옅은 하늘색 꼬리, 회상 실루엣 = 더 진하고 고양이보다 조금 크게(1.15배)
        static readonly Color SilDash = new Color(0.52f, 0.86f, 1f, 0.72f);
        static readonly Color SilDashEnd = new Color(0.78f, 0.95f, 1f, 0.55f);
        static readonly Color SilRecall = new Color(0.5f, 0.88f, 1f, 0.92f);
        const float RecallScale = 1.15f;                // (몸이 원래 크기로 고쳐져서 예전 실루엣보다 이미 약 2배)
        // 닷지 액션: 적의 공격을 대시로 직접 피하면 → 주변 시간이 느려지고(0.2배, 실시간 0.3초 + 0.3초 동안 회복) 짧은 시간 치명타 보정
        public const float DodgeCritMul = 1.5f, DodgeCritTime = 2.5f, DodgeSlowScale = 0.2f, DodgeSlowHold = 0.45f, DodgeSlowRecover = 0.35f;
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
            health.OnDamaged += Hurt;
            Tables.Load();
            WearCostumeModel();
            if (model) { poser = model.GetComponent<KataPoser>(); anim = model.GetComponentInChildren<Animator>(); }
            if (model) StartCoroutine(KeepVivid());
            if (!GetComponent<ReloadMotion>()) gameObject.AddComponent<ReloadMotion>();     // 스피드로더 장전 동작
        }

        /// <summary>입은 코스튬의 모델(Resources/Costumes/&lt;ModelKey&gt;, Hyper3D · CostumeModelBuilder)로 기본 모델을 바꿔 끼움. 없으면 기본 모델 그대로</summary>
        void WearCostumeModel()
        {
            if (!model || !Tables.Costumes.TryGetValue(Meta.SelectedCostume, out var c) || string.IsNullOrEmpty(c.ModelKey)) return;
            var pf = Resources.Load<GameObject>("Costumes/" + c.ModelKey);
            if (!pf) return;
            var m = Instantiate(pf, model.parent);
            m.name = model.name;
            m.transform.SetLocalPositionAndRotation(model.localPosition, model.localRotation);
            m.transform.localScale = model.localScale;
            model.gameObject.SetActive(false);
            Destroy(model.gameObject);
            model = m.transform;
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
        public float DamageMul => (1 + Meta.Stat("DamageMul")) * (1 + LowHpDamageBonus);
        /// <summary>코스튬 노인의 잠옷: 체력이 CondValue% 미만이면 공격력 +Value (2026-11-24 확정: 40% · +30%)</summary>
        public float LowHpDamageBonus
        {
            get
            {
                if (!health || !Tables.Costumes.TryGetValue(Meta.SelectedCostume, out var c)) return 0;
                float v = 0;
                foreach (var id in new[] { c.Effect1, c.Effect2, c.Effect3 })
                    if (id != 0 && Tables.Effects.TryGetValue(id, out var e) && e.Type == "LowHpDamageAdd" && health.hp < health.maxHp * e.CondValue / 100f) v += e.Value;
                return v;
            }
        }
        public float AttackSpeedBonus => CostumeStat("AttackSpeedAdd");
        public float ReloadTime
        {
            get
            {
                float a = rev.rightGun != null ? rev.rightGun.ReloadTime : 1.2f, b = rev.leftGun != null ? rev.leftGun.ReloadTime : 1.2f;
                return Mathf.Max(a, b) * (1 + Meta.Stat("ReloadTime")) * (1 + CostumeStat("ReloadTimeAdd"));   // 코스튬 시계공의 앞치마: 장전 속도 -10%
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

        /// <summary>판 시작: 양손 기본 총(2026-11-25 사용자: 시작 총 고르기 없앰) · 코스튬 · 기본 탄 '탄환' 12발</summary>
        public void SetupRun()
        {
            rev = new Revolver { rightGun = Tables.Guns[2001], leftGun = Tables.Guns[2001] };
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
            health.ResetHp(MaxHp);
            LowHpAura.Attach(this);                          // 코스튬 노인의 잠옷
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
            if (dashCharges < dashMax && Time.time >= dashLockUntil)   // 차장+ 검표 펀치에 맞으면 잠깐 충전 멈춤
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
                if (Time.time >= reloadStart + reloadDur) FinishReload();   // 완벽 장전(R)은 2026-11-17 사용자 지시로 없앰
            }
            else if (mouse.leftButton.isPressed && Time.time >= nextFireTime && !FireLocked) FireNext();

            if (mouse.rightButton.wasPressedThisFrame && Time.time >= nextSkill) StartCoroutine(UseSkill());   // 새 총(랜턴 · 기관차)이면 그 총 스킬, 아니면 백스텝 샷 (PlayerSkills.cs)
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
            Sfx.Play("dash", 0.7f);
            dashFrom = transform.position;                // 극한 회피 리본이 여기서부터 휘감음
            dashCharges -= 1;
            health.invulnerable = true;
            health.dashing = true;
            float t = 0, ghostT = 0;
            Afterimage.Spawn(model, SilDash);
            while (t < dashTime)
            {
                cc.Move(dir * (dashDistance / dashTime) * Time.deltaTime);
                t += Time.deltaTime;
                ghostT += Time.deltaTime;
                if (ghostT >= 0.035f) { ghostT = 0; Afterimage.Spawn(model, SilDash * new Color(1, 1, 1, 0.8f)); }
                yield return null;
            }
            dashing = false;
            Afterimage.Spawn(model, SilDashEnd);
            yield return new WaitForSeconds(JustDodgeWindow);
            health.dashing = false;
            health.invulnerable = ultimateActive;         // 불릿 파티 중 대시는 무적을 풀지 않음
        }

        void JustDodge()
        {
            if (Time.time - lastJustDodge < 0.5f) return;
            lastJustDodge = Time.time;
            Sfx.Play("justdodge", 0.9f, 0f);
            Meta.AddCount(Meta.CountJustDodge);                 // 코스튬 거울 망토 해금 조건
            // 닷지 액션 (젠레스 존 제로 극한 회피 참고): 순간 파란 섬광 → 화면이 회색으로 빠지며 슬로우, 몸 주위로 파란 반짝임이 차례로 터짐
            GunkataFx.DodgeBurst(transform.position, new Color(0.45f, 0.75f, 1f), true);
            DodgeRibbon.Spawn(dashFrom, transform.position, new Color(0.45f, 0.85f, 1f));   // 2026-11-17 사용자: 고리 대신 리본 궤적
            ScreenFx.PerfectDodge(ultimateActive ? 0.25f : DodgeSlowHold, DodgeSlowRecover);
            // 닷지 액션: 치명타 보정 + 주변 시간 느려짐
            dodgeCritUntil = Time.time + DodgeCritTime;
            Afterimage.Spawn(model, SilDodge);
            CameraFollow.Zoom(ultimateActive ? 0.8f : 0.9f);
            if (!ultimateActive) StartCoroutine(DodgeSlow());
            OnDodgeAction?.Invoke(DodgeCritTime);
            foreach (var e in Effects.Global(this, "OnJustDodge"))
            {
                if (e.Type == "InstantReload" && rev.reloading) FinishReload();
                if (e.Type == "CritNextShots") critShots += Mathf.RoundToInt(e.Value);
                if (e.Type == "ShadowStrike") ShadowStrike.Launch(this, ShotDamage(rev.next, out _) * e.Value);   // 코스튬 거울 망토: 그림자 잔상이 무작위 적을 벰
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

        /// <summary>고양이 모델(총 · 코스튬이 나중에 붙어도)을 Vivid 레이어로: 후처리 뒤에 그려져 회색 · 붉은 화면에서도 제 색</summary>
        IEnumerator KeepVivid()
        {
            var wait = new WaitForSecondsRealtime(0.5f);
            while (model) { VividLayer.SetAll(model.gameObject); yield return wait; }
        }

        // ---------- 피격 피드백 (2026-11-16 사용자: 맞았을 때 확실하게) ----------
        // 하얀 번쩍 → 붉은 비네트 · 색수차(ScreenFx) + 아주 짧은 히트스톱 + 강한 흔들림 + 몸에 붉은 타격 이펙트
        static readonly Color HurtFx = new Color(1f, 0.18f, 0.16f);

        void Hurt(float dmg)
        {
            float power = Mathf.Clamp01(dmg / Mathf.Max(1f, health.maxHp * 0.15f));
            ScreenFx.PlayerHit(0.55f + 0.45f * power);
            CameraFollow.Shake(0.22f + 0.2f * power);
            var at = (model ? model.position : transform.position) + Vector3.up * 1.0f;
            var cam = Camera.main;
            var toCam = cam ? (cam.transform.position - at).normalized : Vector3.back;
            FxEffect.Spawn("FX_HitCrit", at, Quaternion.LookRotation(-toCam), 0.9f, HurtFx);
            if (!ultimateActive) StartCoroutine(HitStop(0.05f + 0.04f * power));
        }

        /// <summary>히트스톱: 맞는 순간 아주 잠깐 멈춤 (실제 시간). 다른 연출(멈춤 화면 · 슬로우) 중이면 건드리지 않음</summary>
        IEnumerator HitStop(float realSeconds)
        {
            if (Time.timeScale < 0.99f) yield break;
            Time.timeScale = 0.04f;
            yield return new WaitForSecondsRealtime(realSeconds);
            if (Time.timeScale > 0f && Time.timeScale < 0.05f) Time.timeScale = 1f;
        }

        void ChanceDodge()
        {
            ExtraFx.ChanceDodge(model, transform.position, new Color(0.35f, 0.95f, 0.85f));   // 2026-11-17 사용자: 옆으로 빗겨나는 잔상
            foreach (var e in Effects.Global(this, "OnChanceDodge"))
                if (e.Type == "BulletRefund" && Effects.Roll(e)) Refund();
        }

        /// <summary>탄 회수: 방금 쏜 칸을 되돌림</summary>
        void Refund()
        {
            if (rev.reloading || rev.shotsThisCycle <= 0) return;
            rev.shotsThisCycle--;
            rev.next = rev.reversed ? Mathf.Min(12, rev.next + 1) : Mathf.Max(1, rev.next - 1);
            OnReloadEnd?.Invoke();
        }

        // ---------- 사격 ----------
        void FireNext()
        {
            int s = rev.next;
            var gun = rev.GunFor(s);
            bool refund = false;
            if (rev.slots[s].bullet != null && !rev.lost[s])
            {
                float atkSpeed = AttackSpeedBonus + Effects.Passive(this, s, "AttackSpeedAdd") + nextShotSpeedAdd;
                nextShotSpeedAdd = 0;
                nextFireTime = Time.time + (gun != null ? gun.FireInterval : 0.3f) / Mathf.Max(0.2f, 1 + atkSpeed);
                refund = Shoot(s, aimDir, s % 2 == 1 ? rightMuzzle : leftMuzzle, 1f);
                Sfx.Play("shot", 0.75f, 0.08f, 2);
                if (poser) poser.Recoil(s % 2 == 1);              // 쏜 손 반동
            }
            else
            {
                nextFireTime = Time.time + (rev.lost[s] ? 0.18f : 0.06f);
                if (rev.lost[s]) FxBurst.Spawn(transform.position + Vector3.up * 1.2f + aimDir * 0.8f, 0.6f, FxBurst.Kind.Dust, new Color(1f, 0.75f, 0.9f));   // 거울 고래에게 삼켜진 칸: 헛방
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
                    case "Stun":
                        Effects.AreaStun(transform.position, e.Value2 > 0 ? e.Value2 : 3f, Mathf.Max(0.3f, e.Duration) * ControlDurationMul);
                        HeadIcon.Show(transform, "bell_icon");   // 종탑의 메아리: 머리 위 연한 종 (무슨 효과인지 보이게)
                        break;
                    case "DodgeChance": tempDodge = e.Value; tempDodgeUntil = Time.time + e.Duration; break;
                    case "BulletRefund": refund = true; break;
                    case "GlobalTimeStop":
                        if (Time.time >= globalStopReady)
                        {
                            Health.GlobalStopUntil = Time.time + Mathf.Max(0.5f, e.Duration) * ControlDurationMul;
                            globalStopReady = Time.time + (e.Value2 > 0 ? e.Value2 : 20f);
                            GunkataFx.Flare(transform.position + Vector3.up, 9f, new Color(0.6f, 0.75f, 1f), true);
                        }
                        break;
                }
            }
            return refund;
        }

        /// <summary>칸 s의 기본 피해 (패시브 효과 포함, 대상 무관)</summary>
        public float ShotDamage(int s) => ShotDamage(s, out _);

        /// <summary>condMul = 조건이 붙은 패시브(판정 시각 · 12시 칸 · 비활성화 수 · 음수 판정)가 곱해 준 배율 → 늘어난 몫을 금빛 숫자로 따로 띄울 때 씀</summary>
        public float ShotDamage(int s, out float condMul)
        {
            var slot = rev.slots[s];
            var b = slot.bullet;
            var gun = rev.GunFor(s);
            bool awake = rev.Awakened(s);
            int judge = rev.JudgeTime(s);
            float add = Effects.Passive(this, s, "DamageAdd");
            float condAdd = Effects.PassiveConditional(this, s, "DamageAdd");
            add += awake ? Effects.Passive(this, s, "AwakenedDamageAdd") : Effects.Passive(this, s, "UnawakenedDamageAdd");
            float perUn = Effects.Passive(this, s, "DamagePerUnawakened") * rev.UnawakenedCount();
            add += perUn; condAdd += perUn;
            if (judge < 0) { float nj = Effects.Passive(this, s, "DamageByNegativeJudge") * -judge; add += nj; condAdd += nj; }
            condMul = condAdd > 0 ? (1 + add) / Mathf.Max(0.05f, 1 + add - condAdd) : 1f;
            // 영구 성장은 처치마다 소수로 쌓이고 정수만큼만 들어감 (레테의 물결: 처치당 0.1 → 10명당 +1, 2026-11-18 사용자)
            float dmg = ((b != null ? b.Damage : 8) + Mathf.Floor(slot.permanentBonus + 1e-4f)) * (gun != null ? gun.DamageMul : 1f) * DamageMul * (1 + add);
            return dmg;
        }

        // 이번 발사의 특성 목록 (프레임마다 한 번 계산): [0] = 대표 = 모은 개수 가장 많은 특성(동률이면 랜덤),
        // 이어서 세트가 켜진(2단계 이상) 다른 특성들, 마지막으로 이 탄 자신의 특성(아직 없으면)
        int shotTraitsFrame = -1; BulletDef shotTraitsBullet;
        readonly List<int> shotTraits = new();
        List<int> ShotTraits(BulletDef b)
        {
            if (shotTraitsFrame == Time.frameCount && shotTraitsBullet == b) return shotTraits;
            shotTraitsFrame = Time.frameCount; shotTraitsBullet = b;
            shotTraits.Clear();
            var counts = rev.TraitCounts();
            int best = 0, ties = 0, top = 0;
            foreach (var kv in counts)
            {
                if (kv.Key == 0 || rev.TierOf(kv.Value) <= 0) continue;
                if (kv.Value > best) { best = kv.Value; top = kv.Key; ties = 1; }
                else if (kv.Value == best && UnityEngine.Random.Range(0, ++ties) == 0) top = kv.Key;   // 동률: 고르게 랜덤
            }
            if (top != 0) shotTraits.Add(top);
            if (best > 0 && rev.TierOf(best) >= 6 && Meta.Count(Meta.CountTraitTier6) == 0) Meta.AddCount(Meta.CountTraitTier6);   // 코스튬 기억의 스카프 해금 조건
            foreach (var kv in counts)
                if (kv.Key != 0 && kv.Key != top && rev.TierOf(kv.Value) > 0) shotTraits.Add(kv.Key);
            if (b != null)
                foreach (var t in new[] { b.Trait1, b.Trait2 })
                    if (t != 0 && !shotTraits.Contains(t)) shotTraits.Add(t);
            return shotTraits;
        }

        /// <summary>한 발: 거의 즉발 히트스캔 + 레이저 궤적. ghost = 실루엣 · 추가 사격 (발사 효과 없음). 반환: 탄 회수 여부</summary>
        public bool Shoot(int s, Vector3 dir, Transform muzzle, float mul, float targetDist = -1f, bool ghost = false, Vector3? fromPos = null)
        {
            var b = rev.slots[s].bullet;
            var gun = rev.GunFor(s);
            bool awake = rev.Awakened(s);
            float dmg = ShotDamage(s) * mul;
            bool refund = false;
            bool boosted = !ghost && nextShotDamageAdd > 0.001f;   // DEAR[ ] 등 '다음 탄 피해 +': 궤적을 굵고 하얗게 달궈 보여 줌 (2026-11-18 사용자)
            if (!ghost)
            {
                dmg *= 1 + nextShotDamageAdd;
                nextShotDamageAdd = 0;
            }
            // 치명타는 맞는 적마다 따로 굴림 (2026-11-19 사용자: 관통탄 첫 적이 치명타면 뒤 적도 전부 치명타였음).
            // dmg = 치명타 전 피해. 확정 치명타(critShots)는 첫 적에게만
            bool firstCrit = !ghost && critShots > 0;
            if (firstCrit) critShots--;
            int critRolls = 0;
            bool shotCrit = false;   // 첫 적의 치명타 = 이 사격의 치명타 → 공명 · 은빛 메아리 범위 피해에도 배율 (예전 그대로, 2026-11-19 사용자)
            bool RollCrit()
            {
                bool first = critRolls++ == 0;
                bool c = (first && firstCrit) || CritChanceFor(s) > UnityEngine.Random.value;
                if (first) shotCrit = c;
                return c;
            }

            Vector3 basePos = fromPos ?? transform.position;
            Vector3 origin = muzzle && !fromPos.HasValue ? muzzle.position : basePos + Vector3.up * 0.9f;
            origin.y = basePos.y + 0.9f;
            var castFrom = basePos + Vector3.up * 0.9f;
            castFrom.y = transform.position.y + 0.9f;

            // 발사 효과
            if (!ghost) refund = FireEffects(s, dir, muzzle, targetDist);
            if (!ghost && Time.time < silhouetteFollowUntil) StartCoroutine(SilhouetteShot(s, dir, 0.3f));
            int specialMark = specialCount;   // 이 뒤로 특수 효과가 터지면 공명 범위 피해 연출을 뺌

            // 관통 · 튕김 · 유도
            int pierce = 0, ricochet = 0;
            int judge = rev.JudgeTime(s);
            foreach (var e in Effects.For(this, s, "OnHit")) { if (e.Type == "Pierce") pierce += Mathf.RoundToInt(e.Value); if (e.Type == "Ricochet") ricochet += Mathf.RoundToInt(e.Value); if (e.Type == "RicochetByJudgeTime" && Mathf.Abs(judge) >= e.Value) ricochet += 1; }
            foreach (var e in Effects.For(this, s, "Passive")) if (e.Type == "Ricochet" && (!e.Borrowed || Effects.Roll(e))) ricochet += Mathf.RoundToInt(e.Value);
            var train = ghost ? null : TrainOf(s);                                  // 마지막 기차: 모든 적 관통 · 관통마다 피해 성장 · 끝 공명 폭발 (PlayerSkills.cs)
            if (train != null) pierce = 999;
            bool homing = Effects.Passive(this, s, "Homing") > 0;
            float range = gun != null ? gun.Range : 12f;

            var aimPoint = basePos + dir * (targetDist > 0 ? targetDist : aimDist);
            aimPoint.y = castFrom.y;
            var toAim = aimPoint - castFrom;
            if (toAim.sqrMagnitude > 0.25f) dir = toAim.normalized;
            var aimedDir = dir;
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
            bool homed = homing && Vector3.Angle(aimedDir, dir) > 3f;   // 따라가서 꺾였음 → 궤적을 조준 방향에서 휘어 들어가는 곡선으로 (2026-11-18 사용자)

            Vector3 end = castFrom + dir * range;
            var hitSet = new HashSet<Health>();
            int pierced = 0;
            float growth = 1f;
            var growMarks = new List<(int seg, Vector3 at, float growth)>();   // 세월 등 관통 피해 성장: 뚫은 자리부터 궤적이 굵어짐
            Color col = BulletLook.Shown(b, awake);
            // 탄도 구간: 3장 떠다니는 거울 조각에 닿으면 한 번 꺾여 남은 사거리를 감 (2026-11-18). bends = 꺾인 자리들
            var bends = new List<Vector3>();
            Vector3 segFrom = castFrom, segDir = dir;
            float segRange = range;
            MirrorShard lastMirror = null;
            for (int seg = 0; seg < 2; seg++)
            {
                float mt = segRange; Vector3 mp = default;
                var mirror = seg == 0 && MirrorShard.All.Count > 0 ? MirrorShard.Raycast(segFrom, segDir, segRange, lastMirror, out mt, out mp) : null;
                float reach = mirror ? mt : segRange;
                var hits = Physics.SphereCastAll(segFrom, 0.3f, segDir, reach, ~0, QueryTriggerInteraction.Ignore);
                Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));
                end = segFrom + segDir * reach;
                bool stopped = false;
                foreach (var h in hits)
                {
                    if (h.collider.transform.IsChildOf(transform)) continue;
                    var hp = h.collider.GetComponentInParent<Health>();
                    if (hp == null) { end = h.distance > 0 ? h.point : segFrom + segDir * 0.5f; stopped = true; break; }
                    if (!hp.Alive || hp.isPlayer || hitSet.Contains(hp)) continue;
                    if (hp.ReflectFrom != null && hp.ReflectFrom(segDir))
                    {
                        // 거울 방패 (3장 유리 기사): 피해 없이 고양이 쪽으로 되튕김
                        end = h.point;
                        Enemy.Fx3("MirrorFlash", h.point, 0.9f);
                        var back = Effects.Flat(transform.position - h.point);
                        EnemyProjectile.Spawn(new Vector3(h.point.x, transform.position.y + 0.9f, h.point.z), back.sqrMagnitude > 1e-4f ? back.normalized : -segDir,
                            Enemy.KnightReflectSpeed, Enemy.KnightReflectDamage, 0, EnemyProjectile.Style.Needle);
                        stopped = true;
                        break;
                    }
                    if (hp.BlockFrom != null && hp.BlockFrom(segDir))
                    {
                        // 방패에 걸림 (짐꾼 트렁크): 피해 33%만 들어가고 탄이 멈춤 + 쇳빛 불꽃, 관통 · 튕김 없음
                        // (2026-11-18 사용자: 완전 차단은 불쾌 → 33%)
                        end = h.point;
                        GunkataFx.HitImpact(h.point, -segDir, new Color(1f, 0.85f, 0.55f), false, 0.8f, true);
                        FxBurst.Spawn(h.point, 0.9f, FxBurst.Kind.Spark, new Color(1f, 0.9f, 0.6f));
                        hitSet.Add(hp);
                        bool sc = RollCrit();
                        ApplyHit(s, hp, dmg * ShieldDamageFrac * (sc ? CritDamage : 1f), h.point, h.distance, segDir, col, sc);
                        stopped = true;
                        break;
                    }
                    hitSet.Add(hp);
                    hp.HitFrom(segDir);                                                // 공중 짐 등이 맞은 방향으로 날아감
                    bool hc = RollCrit();
                    ApplyHit(s, hp, dmg * (hc ? CritDamage : 1f), h.point, range - segRange + h.distance, segDir, col, hc);
                    end = h.point;
                    if (pierced >= pierce) { stopped = true; break; }
                    pierced++;
                    ExtraFx.Pierce(h.point, segDir, col);                              // 관통: 탄도 방향으로 뚫고 나가는 불꽃 꼬리 (2026-11-17)
                    float grow = train != null ? 1 + train.Value : 1f;
                    foreach (var e in Effects.For(this, s, "OnPierce"))
                    {
                        if (e.Type == "PierceDamageGrowth") grow *= 1 + e.Value;
                        // 은빛 메아리: 꿰뚫은 자리에서 은빛 메아리 폭발 (2026-11-18 사용자)
                        if (e.Type == "Explosion" && Effects.Roll(e)) PierceEcho(h.point, segDir, e.Value2 > 0 ? e.Value2 : 1.5f, dmg * e.Value * (shotCrit ? CritDamage : 1f));
                    }
                    if (grow > 1.001f) { dmg *= grow; growth *= grow; growMarks.Add((seg, h.point, growth)); }
                }
                if (stopped || !mirror) break;
                // 거울에서 꺾임: 판이 15° 돌아가고 남은 사거리로 다시 날아감
                mirror.OnBounce(mp, col, true);
                bends.Add(mp);
                segRange -= mt;
                segFrom = mp;
                segDir = mirror.Reflect(segDir);
                lastMirror = mirror;
                end = segFrom + segDir * segRange;
            }
            // 꺾인 탄도: 총구 → 거울 → 끝 (거울이 없으면 총구 → 끝 한 줄)
            var legStart = new List<Vector3> { origin };
            foreach (var bp in bends) legStart.Add(bp);
            for (int li = 0; li < legStart.Count; li++)
            {
                var la = li == 0 ? castFrom : legStart[li];
                EnemyProjectile.ShootDown(la, li + 1 < legStart.Count ? legStart[li + 1] : end);   // 부서지는 적 탄 (2장 중간 보스 빨간불 표 탄)
            }
            bool resonance = !ghost && awake && b != null && b.Id != 1000;
            // 특성 탄도 (2026-11-17 사용자): 날아가는 탄(궤적 색 · 덩어리)은 플레이어가 가장 많이 모은 특성(동률이면 랜덤),
            // 모은 특성이 없으면 이 탄의 특성. 궤적 번짐은 이 탄 자신의 특성 색 (대표와 다를 때)
            var shown = ShotTraits(b);
            int trait = shown.Count > 0 ? shown[0] : 0;
            int own = b != null ? b.Trait1 : 0;
            var trailCol = trait != 0 ? GunkataFx.TraitColor(trait, col) : col;
            Color? halo2 = own != 0 && own != trait ? GunkataFx.TraitColor(own, col) : (Color?)null;
            float baseW = BulletLook.Width(b, awake);
            float w0 = baseW * (ghost ? 0.7f : resonance ? 1.6f : 1f) * (boosted ? 1.55f : 1f) * (train != null ? 2.2f : 1f);
            if (boosted) trailCol = Color.Lerp(trailCol, Color.white, 0.45f);
            bool wide = resonance || boosted || (awake && b != null && b.Grade == "Legendary");
            for (int li = 0; li < legStart.Count; li++)
            {
                var la = legStart[li];
                var lb = li + 1 < legStart.Count ? legStart[li + 1] : end;
                if (li == 0 && homed)
                {
                    // 자동 추적: 조준한 방향으로 나가다 표적 쪽으로 휘어 들어가는 곡선 (판정은 그대로 곧은 선)
                    float len = Vector3.Distance(la, lb);
                    var ctrl = la + aimedDir * Mathf.Min(len * 0.55f, 4f);
                    var pts = new Vector3[10];
                    for (int k = 0; k < pts.Length; k++)
                    {
                        float u = k / (pts.Length - 1f);
                        pts[k] = (1 - u) * (1 - u) * la + 2 * (1 - u) * u * ctrl + u * u * lb;
                    }
                    Tracer.SpawnPath(pts, trailCol, w0, wide, trait != 0, halo2);
                }
                else Tracer.Spawn(la, lb, trailCol, w0, wide, trait != 0, halo2);
                // 특성 덩어리(GunkataFx.TraitBolt)는 안 날림 (2026-11-18 사용자: 추가 피해 · 효과가 없는 탄엔 이펙트 X). 궤적 색만 특성 색
            }
            // 관통 피해 성장 (세월): 뚫은 자리부터 그 구간 끝까지 더 굵고 밝은 궤적을 덧그림, 많이 뚫을수록 굵게 (2026-11-18 사용자)
            foreach (var g in growMarks)
            {
                var lb = g.seg + 1 < legStart.Count ? legStart[g.seg + 1] : end;
                Tracer.Spawn(g.at, lb, Color.Lerp(trailCol, Color.white, 0.3f), baseW * Mathf.Clamp(0.6f + (g.growth - 1f) * 1.2f, 0.6f, 2.2f), true);
            }
            if (resonance)
            {
                // 공명 사격: 섬광 + 맞은 자리 범위 피해 30% + 화면 흔들림.
                // 범위 피해 연출은 다른 특수 이펙트를 안 가리게 (2026-11-18 사용자): 폭발 · 연기 대신 작고 짧은 섬광,
                // 이번 사격에서 다른 특수 효과(관통 폭발 · 가둠 · 처형 · 치명타 · 지연 피해 등)가 터졌으면 연출 · 흔들림은 빼고 피해만
                bool special = specialCount != specialMark;
                GunkataFx.Flare(origin + dir * 0.5f, 1.8f, col);
                // 2026-11-25: 반경 · 피해는 미련 특성 · 탄 · 랜턴 장판으로 늘고 둔화 · 원혼이 붙음 (PlayerSkills.Resonance)
                if (train != null)
                {
                    // 마지막 기차: 맞았든 안 맞았든 끝에서 큰 공명 폭발 (기적 소리 고리)
                    StartCoroutine(TrainRun(origin, end));
                    if (!FxEffect.Spawn("FX_TrainEnd", end, Quaternion.LookRotation(Effects.Flat(dir).sqrMagnitude > 1e-4f ? Effects.Flat(dir).normalized : Vector3.forward), train.Value2 > 0 ? train.Value2 / 4f : 1f, col))
                        GunkataFx.Explosion(end, train.Value2 > 0 ? train.Value2 : 4f, true, col);
                    Resonance(s, end, dmg * (shotCrit ? CritDamage : 1f), col, false, train.Value2 > 0 ? train.Value2 : 4f);
                    CameraFollow.Shake(0.3f);
                }
                else if (hitSet.Count > 0) Resonance(s, end, dmg * (shotCrit ? CritDamage : 1f), col, !special);
                if (!special && train == null) CameraFollow.Shake(b.Grade == "Legendary" ? 0.22f : b.Grade == "Epic" ? 0.16f : 0.1f);
            }

            // 튕김: 맞은 자리에서 가장 가까운 다른 적으로
            // 튕긴 탄은 치명타를 물려받지 않고 튕길 때마다 따로 굴림 (2026-11-18 사용자: 앞 탄이 치명타면 뒤도 다 치명타였음)
            Vector3 from = end;
            float stack = 0;
            float bounceDmg = dmg;                                                   // dmg는 이미 치명타 전 피해
            for (int r = 0; r < ricochet && hitSet.Count > 0; r++)
            {
                var next = Effects.Nearest(from, 9f, hitSet);
                if (!next) break;
                foreach (var e in Effects.For(this, s, "OnRicochet")) if (e.Type == "DamageAddStack") stack += e.Value;
                bool rcrit = CritChanceFor(s) > UnityEngine.Random.value;
                var to = next.transform.position + Vector3.up * 0.9f;
                Tracer.Spawn(from, to, col, BulletLook.Width(b, awake) * 0.8f, false);
                ExtraFx.Ricochet(from, (to - from).normalized, col);              // 튕김: 지그재그 불똥 + 흰 번쩍 (2026-11-17)
                hitSet.Add(next.health);
                next.health.HitFrom(to - from);
                ApplyHit(s, next.health, bounceDmg * (1 + stack) * (rcrit ? CritDamage : 1f), to, Vector3.Distance(transform.position, to), (to - from).normalized, col, rcrit);
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

        /// <summary>은빛 메아리 관통 폭발 색 (은빛 · 옅은 하늘빛)</summary>
        public static readonly Color EchoSilver = new(0.8f, 0.88f, 1f);

        /// <summary>은빛 메아리 (2026-11-18 사용자): 꿰뚫은 자리에서 은빛 폭발 + 메아리 — 그림(cx_pierce_echo, Codex) + 블렌더 3D(fx_echo_star · fx_echo_blade)
        /// 프리팹 FX_PierceEcho. 반경 radius 범위 피해</summary>
        void PierceEcho(Vector3 at, Vector3 dir, float radius, float dmg)
        {
            specialCount++;
            var f = Effects.Flat(dir);
            var rot = Quaternion.LookRotation(f.sqrMagnitude > 1e-4f ? f.normalized : Vector3.forward, Vector3.up);
            if (!FxEffect.Spawn("FX_PierceEcho", at, rot, radius / 1.5f, EchoSilver)) GunkataFx.Explosion(at, radius, false, EchoSilver);
            Effects.AreaDamage(at, radius, dmg);
        }

        /// <summary>이번 사격에서 특수 효과 이펙트가 터진 횟수 (관통 폭발 · 가둠 · 처형 · 치명타 · 지연 피해 · 폭발 · 표식 · 분열 등). 공명 범위 피해 연출을 뺄지 판단</summary>
        int specialCount;

        /// <summary>공명 범위 피해: 반경 안 적에게 피해 + 맞은 자리 작고 짧은 섬광 (2026-11-18 사용자 확정, 연기 · 폭발 · 연결선 없음). show = false면 피해만</summary>
        public void ResonanceBlast(Vector3 at, float radius, float dmg, Color col, bool show)
        {
            if (show) GunkataFx.SmallFlash(at, 1.2f, col);
            Effects.AreaDamage(at, radius, dmg);
        }

        /// <summary>방패(짐꾼 트렁크 정면)에 맞았을 때 들어가는 피해 비율</summary>
        public const float ShieldDamageFrac = 0.33f;

        void ApplyHit(int s, Health target, float dmg, Vector3 point, float dist, Vector3 dir, Color col, bool crit)
        {
            // 맞기 전 배율. 조건부 추가 피해(보스 · 표식 · 상태 이상 · 거리 · 판정 시각 등)는 condMul에 모아 늘어난 몫을 금빛 숫자로 따로 띄움 (2026-11-18 사용자)
            ShotDamage(s, out float condMul);
            foreach (var e in Effects.For(this, s, "OnHit"))
            {
                if (!Effects.CondOk(e, this, s, target)) continue;
                float m = 1f;
                if (e.Type == "DamageAdd") m = 1 + e.Value;
                if (e.Type == "DamageByDistance") m = 1 + Mathf.Min(e.Value * dist, e.Value2 > 0 ? e.Value2 : 1f);
                dmg *= m; condMul *= m;
            }
            // 정지된 적 추가 피해 (영원 6개 등): 늘어난 몫은 초록 숫자로 따로 띄움 (2026-11-17 사용자)
            float stoppedFrac = 0;
            if (target.Stunned)
            {
                float vs = Effects.Passive(this, s, "DamageVsStoppedAdd");
                dmg *= 1 + vs;
                if (vs > 0) stoppedFrac = vs / (1 + vs);
            }
            // 피해 유형별 증폭 (탄 고유 효과)
            if (target.IsBoss) { float m = 1 + Effects.GlobalPassive(this, "BossDamageAdd"); dmg *= m; condMul *= m; }
            if (target.HasStatus) { float m = 1 + Effects.GlobalPassive(this, "StatusDamageAdd"); dmg *= m; condMul *= m; }
            // 몫 나누기: 전체 = 기본 × 조건(C) × 정지(S) → 정지 몫 = (S-1)/S (초록), 조건 몫 = (C-1)/(C·S) (금빛)
            float condFrac = condMul > 1.001f ? (condMul - 1) / condMul * (1 - stoppedFrac) : 0f;
            bool wasLocked = target.TimeLocked;
            target.TakeDamage(dmg, false, crit, stoppedFrac, condFrac);
            rev.AddHeat(HeatPerHit);
            // 타격 이펙트: 평소엔 일반 타격(탄 색)만. 특성 타격(FX_Trait_*)을 맞을 때마다 띄우지 않음
            // (2026-11-18 사용자: 추가 피해 · 효과가 실제로 생길 때만 이펙트). 특성 연출은 실제로 발동한 자리에서만:
            // 망각 = 시간 가둠(OblivionFx.Lock) · 종말 = 처형(ExecutionFx.Rend) · 추억 = 표식이 다 참(MemoryClock) · 지연 = 지연 피해 예고 · 치명타 = 해골
            Sfx.Play(crit ? "crit" : "hit", crit ? 0.85f : 0.5f, 0.1f, crit ? 2 : 3);
            if (crit) { GunkataFx.CritSkull(target.transform, point); specialCount++; }   // 치명타 = 해골 · 불꽃 (2026-11-18 사용자, 예전 종말 처형 연출)
            else GunkataFx.HitImpact(point, dir, col, false, GunkataFx.HitScale);
            if (crit) CameraFollow.Shake(0.07f);
            if (crit)                                                                // 레일 위의 불꽃: 치명타 자리 폭발 (2026-11-25)
                foreach (var e in Effects.For(this, s, "OnCrit"))
                    if (e.Type == "Explosion" && Effects.Roll(e)) RailSpark(point, dir, e.Value2 > 0 ? e.Value2 : 2f, dmg * e.Value);
            bool killed = !target.Alive;
            var en = target.GetComponent<Enemy>();
            if (killed && rev.JudgeTime(s) <= 0) Meta.AddCount(Meta.CountLowJudgeKill);   // 코스튬 모래 순례자 로브 해금 조건

            var onHit = new List<EffectDef>(Effects.For(this, s, "OnHit"));
            // 추억 표식: 단계(2 · 4 · 6) 효과가 같이 켜져도 한 발에 하나만, 가장 높은 표식당 피해로 (예전엔 한 발에 2~3개 쌓였음)
            EffectDef bestMark = null;
            foreach (var e in onHit) if (e.Type == "MemoryMark" && (bestMark == null || e.Value > bestMark.Value)) bestMark = e;
            foreach (var e in onHit)
            {
                if (e.Type == "MemoryMark" && e != bestMark) continue;
                if (killed && e.Type != "Explosion" && e.Type != "HeatGain" && e.Type != "Split" && e.Type != "TrailSilhouetteShot") continue;
                if (!Effects.CondOk(e, this, s, target) || !Effects.Roll(e)) continue;
                float ctl = ControlDurationMul;
                // 제 이펙트가 있는 특수 효과 → 공명 범위 피해 연출은 뺌 (추억 표식은 다 찼을 때만, 아래에서)
                if (e.Type == "TimeLock" || e.Type == "Execute" || e.Type == "DelayedDamage" || e.Type == "Explosion" || e.Type == "MarkBurst"
                    || e.Type == "Split" || e.Type == "TrailSilhouetteShot" || e.Type == "Knockback" || e.Type == "Stun") specialCount++;
                switch (e.Type)
                {
                    case "TimeLock":
                        target.ApplyTimeLock(Mathf.Max(0.3f, e.Duration) * ctl);
                        if (en) en.WatchTimeLock(this);
                        OblivionFx.Lock(target.transform);   // 얼음 감옥 (6단계면 얼음 꽃) — 갇힌 동안 유지, 풀리면 깨짐 (2026-11-18 사용자)
                        if (e.Value > 0) target.ApplyDefenseDown(e.Value, Mathf.Max(0.3f, e.Duration) * ctl);
                        break;
                    case "Stun": target.ApplyStun(Mathf.Max(0.3f, e.Duration) * ctl); break;
                    case "Slow": target.ApplySlow(e.Value, Mathf.Max(0.5f, e.Duration) * ctl); break;
                    case "Burn": target.ApplyBurn(Mathf.Max(1f, dmg * e.Value * 4f), Mathf.Max(1f, e.Duration)); break;
                    case "DefenseDown": target.ApplyDefenseDown(e.Value, Mathf.Max(1f, e.Duration)); break;
                    case "EnemyAttackSpeedDown": target.ApplyAttackSpeedDown(e.Value, Mathf.Max(1f, e.Duration) * ctl); break;
                    case "Knockback": target.Knockback(dir * Mathf.Max(0.3f, e.Value) * 2f); FxBurst.Spawn(target.transform.position + Vector3.up * 0.3f, 1.2f, FxBurst.Kind.Dust, Color.white); break;
                    case "MemoryMark":
                        if (target.AddMark(e.Value, e.Value2 > 0 ? (int)e.Value2 : 5)) { MemoryClock.Ring(target.transform, point); specialCount++; }   // 표식이 다 찼을 때만 시계가 울림
                        break;
                    case "MarkBurst":
                        // 터뜨림: 회중시계가 깨지는 연출 (2026-11-18 사용자) + 범위 피해
                        MemoryClock.Shatter(target.transform, point);
                        Effects.AreaDamage(target.transform.position, 2f, dmg * e.Value);
                        target.ClearMarks();
                        break;
                    case "SandVortex": SandVortex.Spawn(point, dmg * e.Value, e.Value2, Mathf.Max(0.5f, e.Duration)); break;   // 코스튬 모래 순례자 로브
                    case "Execute":
                        if (!target.Alive) break;
                        // 보스는 처형 대신 그 확률로 피해 +100% (기획서 9141/9142, 2026-11-18 전엔 보스도 즉사였음)
                        // 연출 (2026-11-18 사용자): 다키스트 던전 '상처 찢기'처럼 빨간 X자 상처 + 검은 블랙아웃 (보스 추가 피해는 옅고 짧게)
                        ExecutionFx.Rend(target.transform, target.IsBoss);
                        if (target.IsBoss) { target.TakeDamage(dmg * Mathf.Max(1f, e.Value)); break; }
                        Meta.AddCount(Meta.CountExecute);              // 코스튬 장의사 정장 해금 조건
                        target.TakeDamage(target.hp + 1);
                        foreach (var x in Effects.Global(this, "OnExecute"))
                        {
                            if (x.Type == "HeatGain") rev.AddHeat(x.Value);
                            if (x.Type == "Heal") { health.Heal(x.Value); CostumeFx.ExecuteHeal(target.transform.position, transform); }   // 코스튬 장의사 정장
                        }
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
                    case "LockTransfer": LockTransfer(target, e.Value > 0 ? e.Value : 4f, Mathf.Max(0.3f, e.Duration) * ctl); break;   // 환승역 (2026-11-25)
                }
            }
            if (killed)
                foreach (var e in Effects.For(this, s, "OnKill"))
                {
                    bool ok = e.Cond == "TargetTimeLocked" ? wasLocked : e.Cond == "KilledByDelayed" ? false : Effects.CondOk(e, this, s, null);
                    if (!ok || !Effects.Roll(e)) continue;
                    if (e.Type == "Explosion") Effects.Explosion(target.transform.position, e.Value2 > 0 ? e.Value2 : 2f, dmg * e.Value, col);
                    if (e.Type == "SmokeRing") SmokeRing(target.transform.position, e.Value2 > 0 ? e.Value2 : 3f, dmg * e.Value);   // 연기 고리 (2026-11-25)
                    if (e.Type == "DamageAdd" && e.Target == "NextBullet") nextShotDamageAdd += e.Value;
                    if (e.Type == "HeatGain") rev.AddHeat(e.Value);
                    if (e.Type == "PermanentDamageGrowth")
                    {
                        var sl = rev.slots[s];
                        float before = Mathf.Floor(sl.permanentBonus + 1e-4f);
                        sl.permanentBonus = Mathf.Min(e.Value2 > 0 ? e.Value2 : 10, sl.permanentBonus + e.Value);
                        float after = Mathf.Floor(sl.permanentBonus + 1e-4f);
                        if (after > before) OnSlotPowerUp?.Invoke(s, Mathf.RoundToInt(after - before));   // 레테의 물결: HUD 그 칸이 번쩍 + '공격력 +1' (2026-11-18 사용자)
                    }
                }
        }

        /// <summary>시간에 갇힌 적이 풀릴 때 (Enemy가 알려 줌)</summary>
        public void TimeLockEnded(Vector3 at)
        {
            // 6단계 시간 파편 폭발: 겉모습은 얼음 꽃이 깨지는 연출(OblivionFx IceBloom)이 맡고 여기선 피해만 (얼음 연출을 못 쓰면 예전 폭발)
            foreach (var e in Effects.Global(this, "OnTimeLockEnd"))
                if (e.Type == "Explosion")
                {
                    float r = e.Value2 > 0 ? e.Value2 : 3f, dmg = 10 * DamageMul * Mathf.Max(0.3f, e.Value);
                    if (OblivionFx.Ready) Effects.AreaDamage(at, r, dmg);
                    else Effects.Explosion(at, r, dmg, new Color(0.55f, 0.8f, 1f));
                }
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
            var side = Vector3.Cross(Vector3.up, dir).normalized * (UnityEngine.Random.value < 0.5f ? 1.4f : -1.4f);   // 커진 만큼 고양이와 덜 겹치게
            var pos = transform.position + side - dir * 0.4f;
            Afterimage.SpawnAt(model, pos, SilRecall, 0.6f, RecallScale);
            Shoot(s, dir, null, mul, -1, true, pos);
            foreach (var e in Effects.Global(this, "OnSilhouette"))
                if (e.Type == "SilhouetteFollow") silhouetteFollowUntil = Time.time + Mathf.Max(1f, e.Duration);
        }

        IEnumerator TrailSilhouette(int s, Vector3 at, float mul, float after)
        {
            var pos = Effects.Flat(at) + Vector3.up * 0.05f;
            Afterimage.SpawnAt(model, pos, SilRecall * new Color(1, 1, 1, 0.85f), after + 0.3f, RecallScale);
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
            Sfx.Play("reload", 0.7f, 0.03f);
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

        void FinishReload()
        {
            rev.reloading = false;
            Sfx.Play("reload_end", 0.75f, 0.03f);
            rev.shotsThisCycle = 0;
            rev.next = rev.FirstSlot;
            // 장전이 끝날 때 효과 (예: 총 2008 '마지막 종소리' 9310 — 예전 완벽 장전 효과를 2026-11-17 이 트리거로 옮김)
            foreach (var e in Effects.Global(this, "OnReloadEnd"))
                if (e.Type == "Stun") Effects.AreaStun(transform.position, e.Value2 > 0 ? e.Value2 : 5f, Mathf.Max(0.3f, e.Duration) * ControlDurationMul);
            OnReloadEnd?.Invoke();
        }

        public void ResetCylinder()
        {
            if (rev.reloading) FinishReload();
            else if (rev.shotsThisCycle != 0) { rev.shotsThisCycle = 0; rev.next = rev.FirstSlot; OnReloadEnd?.Invoke(); }
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
        // 플레이어를 따라다니는 원형 콜라이더(BulletPartyZone) 안의 적 전부가 12번 피해. 범위는 원 안 바닥 탄자국으로 보임.
        static readonly int[] KataSeq = { 0, 3, 1, 5, 2, 4, 6, 7, 0, 3, 1, 7 };
        public const float UltDuration = 2.0f;        // 지속 시간 (12타)
        public const float UltRadius = 6.5f;          // 원형 콜라이더 반지름 (일반 방 24×20의 가운데 넓은 원)
        public const float UltTickMul = 0.9f;         // 한 타당 피해 배율 (칸 피해 기준, 원 안의 모든 적에게)
        public const float UltMoveMul = 0.9f;         // 궁 중 이동 속도 배율
        public const float UltSpinSpeed = 900f;       // 몸 주위 소용돌이 곡선이 도는 속도 (도/초)
        public const int UltClones = 4;               // 한 타에 본체와 동시에 쏘는 실루엣 분신 수
        const int UltHolesPerTick = 7;               // 한 타에 원 안 아무 데나 생기는 바닥 탄자국 수
        public const float UltCloneDist = 1.6f;       // 분신이 본체에서 떨어진 거리 (m)
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
            ultSpin = Sfx.Loop("gunkata_spin", 0.7f);            // 건카타 도는 내내 이어지는 회전음 (2026-11-19 사용자)
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
            Sfx.StopLoop(ultSpin, 0.5f); ultSpin = null;
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
            // 총알 비(하늘 줄)는 뺌 (2026-11-17 사용자: 특성도 없는데 비가 내림) — 바닥 탄자국 · 흙먼지만 원 안 아무 데나 (범위가 보임)
            if (zone)
                for (int k = 0; k < UltHolesPerTick; k++)
                    ExtraFx.GroundHit(zone.RandomPoint(), UltGold);
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
            Sfx.Play("shot", 0.8f, 0.1f, 2);                   // 건카타 한 타마다 총소리
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
            if (ultSpin) Sfx.StopLoop(ultSpin, 0.2f);
            if (Instance == this) Instance = null;
            Time.timeScale = 1f;
        }
    }
}
