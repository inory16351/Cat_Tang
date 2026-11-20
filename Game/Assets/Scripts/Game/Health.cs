// 체력 · 피격 · 상태 이상. 플레이어와 적 공용.
// 상태: 감속 · 기절 · 시간에 가둠(풀릴 때 이벤트) · 화상 · 방어력 감소 · 추억 표식(겹칠수록 받는 피해 증가) · 지연 피해 · 공격 속도 감소 · 넉백
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    public class Health : MonoBehaviour
    {
        public float maxHp = 100;
        public float hp;
        public bool invulnerable;
        /// <summary>체력이 이 아래로 안 내려감 (5장 보스: 체력 0 = 3페이즈 '마지막 1분' 버티기로 넘어가며 죽지 않음)</summary>
        public float minHp;
        public bool isPlayer;
        public bool dashing;                          // 플레이어 대시 중 (저스트 회피 판정)
        public event Action<Health> OnDeath;
        public event Action<float> OnDamaged;
        public event Action OnTimeLockEnd;            // 시간에 갇힌 상태가 풀릴 때
        public event Action OnJustDodge;              // 대시 중에 맞을 뻔함
        public event Action OnChanceDodge;            // 확률 회피 성공
        /// <summary>탄이 날아온 방향(탄 진행 방향)을 받아 막으면 true (2장 짐꾼 트렁크 방패). 고양이 사격이 맞기 전에 물어봄</summary>
        public Func<Vector3, bool> BlockFrom;
        /// <summary>탄 진행 방향 d로 맞았을 때 고양이 쪽으로 되튕기는지 (3장 유리 기사 거울 방패). true면 피해 없이 반사</summary>
        public Func<Vector3, bool> ReflectFrom;
        /// <summary>마지막으로 맞은 방향(맞힌 쪽 → 이 물체, 수평)과 시각. 공중 짐이 이 방향으로 날아감</summary>
        public Vector3 lastHitDir;
        public float lastHitAt = -99f;
        public void HitFrom(Vector3 dir) { dir.y = 0; if (dir.sqrMagnitude < 1e-6f) return; lastHitDir = dir.normalized; lastHitAt = Time.time; }

        public bool Alive => hp > 0;
        public float SlowMul => Time.time < slowUntil ? 1f - slowAmount : 1f;
        public bool Stunned => Time.time < stunUntil || TimeLocked || GlobalStop;
        public bool TimeLocked => Time.time < lockUntil;
        public bool Slowed => Time.time < slowUntil;
        public float AttackSpeedMul => Time.time < atkDownUntil ? 1f - atkDown : 1f;
        public int MarkStacks => Time.time < markUntil ? marks : 0;
        /// <summary>추억 표식이 다 쌓인 뒤 더 맞은 횟수 (6단계: 5번이면 터뜨림, 2026-11-18 사용자)</summary>
        public int MarkFullHits => Time.time < markUntil ? markFullHits : 0;
        public bool Burning => Time.time < burnUntil;
        public bool DefDowned => Time.time < defDownUntil;
        public bool AtkDowned => Time.time < atkDownUntil;
        /// <summary>상태 이상 (정지 · 둔화 · 가둠 · 화상 · 표식) 중 하나라도 걸려 있음</summary>
        public bool HasStatus => Stunned || Slowed || Burning || MarkStacks > 0;
        public bool IsBoss { get; set; }
        public static float GlobalStopUntil;
        bool GlobalStop => !isPlayer && Time.time < GlobalStopUntil;
        public float lastDamageWasDelayed;            // 마지막 피해가 지연 피해였던 시각

        float slowUntil, slowAmount, stunUntil, lockUntil, burnUntil, burnDps, defDownUntil, defDown, flashUntil, atkDownUntil, atkDown, markUntil, markAmp;
        int marks, markMax = 5, markFullHits;
        bool wasLocked;
        Vector3 knock;
        CharacterController cc;
        readonly List<(float at, float dmg)> delayed = new();
        Renderer[] rends;
        MaterialPropertyBlock mpb;
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        Color[] baseColors;
        Color[][] matColors;                          // 재질이 여러 개인 렌더러: 재질마다 원래 색 (키트 모델 = 메시 하나에 재질 5~10개)
        bool matTinted;                               // 재질별 블록을 씌워 둔 상태 (효과가 끝나면 지움)
        static MaterialPropertyBlock matMpb, emptyMpb;
        static readonly int BrawlActorId = Shader.PropertyToID("_BrawlActor");

        void Awake()
        {
            hp = maxHp;
            RefreshRenderers();
        }

        public void RefreshRenderers()
        {
            rends = GetComponentsInChildren<Renderer>();
            mpb = new MaterialPropertyBlock();
            baseColors = new Color[rends.Length];
            matColors = new Color[rends.Length][];
            for (int i = 0; i < rends.Length; i++)
            {
                baseColors[i] = rends[i].sharedMaterial && rends[i].sharedMaterial.HasProperty(BaseColorId)
                    ? rends[i].sharedMaterial.GetColor(BaseColorId) : Color.white;
                var mats = rends[i].sharedMaterials;
                if (mats.Length <= 1) continue;
                matColors[i] = new Color[mats.Length];
                for (int k = 0; k < mats.Length; k++)
                    matColors[i][k] = mats[k] && mats[k].HasProperty(BaseColorId) ? mats[k].GetColor(BaseColorId) : Color.white;
            }
            matTinted = false;
        }

        public void ResetHp(float max) { maxHp = max; hp = max; }

        /// <summary>피해 받기. 플레이어는 대시 중이면 저스트 회피, 확률 회피 판정. crit = 치명타 전용 숫자로 띄움.
        /// bonusFrac = 피해 중 정지된 적 추가 피해 몫 (0~1) → 그만큼은 초록 숫자로 따로 띄움</summary>
        /// <remarks>condFrac = 조건부 추가 피해 몫 (보스 · 표식 · 상태 이상 · 거리 · 판정 시각 등, 0~1) → 그만큼은 금빛 숫자로 따로 (2026-11-18 사용자)</remarks>
        public void TakeDamage(float dmg, bool fromDelayed = false, bool crit = false, float bonusFrac = 0, float condFrac = 0)
        {
            if (!Alive) return;
            if (isPlayer && dashing) { OnJustDodge?.Invoke(); return; }
            if (invulnerable) return;
            if (isPlayer && PlayerController.Instance)
            {
                var p = PlayerController.Instance;
                if (UnityEngine.Random.value < p.DodgeChance) { OnChanceDodge?.Invoke(); return; }
                dmg *= p.DamageTakenMul;
                if (p.Defense > 0.5f) FxBurst.Spawn(transform.position + Vector3.up * 0.9f, 1.4f + p.Defense * 0.01f, FxBurst.Kind.Shield, Color.white);
            }
            if (Time.time < defDownUntil) dmg *= 1f + defDown;
            if (MarkStacks > 0) dmg *= 1f + markAmp * MarkStacks;
            hp -= dmg;
            if (minHp > 0 && hp < minHp) hp = minHp;
            if (fromDelayed) lastDamageWasDelayed = Time.time;
            flashUntil = Time.time + 0.08f;
            if (isPlayer) { hurtUntil = Time.unscaledTime + HurtBlinkTime; Sfx.Play("player_hurt", 0.85f, 0.05f, 1); }
            OnDamaged?.Invoke(dmg);
            if (!isPlayer)
            {
                var at = transform.position + Vector3.up * 1.6f;
                DamageNumber.Spawn(at, dmg * (1 - bonusFrac - condFrac), crit);
                if (bonusFrac > 0) DamageNumber.SpawnBonus(at + Vector3.up * 0.8f, dmg * bonusFrac);
                if (condFrac > 0) DamageNumber.SpawnCond(at + Vector3.up * (bonusFrac > 0 ? 1.5f : 0.8f), dmg * condFrac);
            }
            if (hp <= 0) { hp = 0; OnDeath?.Invoke(this); }
        }

        public void Heal(float v) => hp = Mathf.Min(maxHp, hp + v);
        public void ApplySlow(float amount, float dur) { slowAmount = Time.time < slowUntil ? Mathf.Max(slowAmount, amount) : amount; slowUntil = Mathf.Max(slowUntil, Time.time + dur); }
        public void ApplyStun(float dur) => stunUntil = Mathf.Max(stunUntil, Time.time + dur);
        public void ApplyTimeLock(float dur) { lockUntil = Mathf.Max(lockUntil, Time.time + dur); wasLocked = true; }
        // 특성 타격 몸 반응 (2026-11-17 사용자: 머리 위 문양만이 아니라 맞은 적 몸 자체에 피드백) — GunkataFx.TraitHit가 부름
        Color hitTint; float hitTintFrom, hitTintUntil, hitTintStrength, hitTintPulse, flashAt = -1;

        /// <summary>몸을 색 c로 물들였다가 dur 동안 빠짐 (strength 0~1). pulse = 초당 깜빡임(0 = 없음, 끝으로 갈수록 빨라짐). delay 뒤 시작</summary>
        public void TraitTint(Color c, float dur, float strength, float pulse = 0, float delay = 0)
        {
            hitTint = c; hitTintFrom = Time.time + delay; hitTintUntil = hitTintFrom + dur; hitTintStrength = strength; hitTintPulse = pulse;
        }

        /// <summary>delay초 뒤에 몸이 하얗게 번쩍 (지연 폭발 순간)</summary>
        public void FlashLater(float delay) => flashAt = Time.time + delay;

        public void ApplyBurn(float dps, float dur) { burnDps = Mathf.Max(burnDps, dps); burnUntil = Time.time + dur; }
        public void ApplyDefenseDown(float v, float dur) { defDown = Mathf.Max(defDown, v); defDownUntil = Time.time + dur; }
        public void ApplyAttackSpeedDown(float v, float dur) { atkDown = Mathf.Max(atkDown, v); atkDownUntil = Time.time + dur; }
        /// <summary>추억 표식 1개 (최대 max). 반환 = 이번에 막 다 찼는지 (그때만 시계 이펙트, 2026-11-18 사용자: 매번 뜨면 정신없음)</summary>
        public bool AddMark(float amp, int max)
        {
            if (Time.time >= markUntil) { marks = 0; markFullHits = 0; }
            markAmp = amp; markMax = Mathf.Max(1, max);
            markUntil = Time.time + 6f;
            if (marks >= markMax) { markFullHits++; return false; }
            marks++;
            return marks >= markMax;
        }
        public void ClearMarks() { marks = 0; markFullHits = 0; }
        public void AddDelayed(float dmg, float after) => delayed.Add((Time.time + after, dmg));
        /// <summary>넉백. 보스는 거의 밀리지 않음 (방 밖으로 밀려 나가지 않게)</summary>
        public void Knockback(Vector3 v) => knock += IsBoss ? v * 0.1f : v;

        // 플레이어 피격 점멸: 0.45초 동안 빨갛게 깜빡임 (흰 기본 색 × 텍스처라 흰 번쩍임은 안 보여서 빨강으로)
        const float HurtBlinkTime = 0.45f;
        static readonly Color HurtRed = new Color(1f, 0.12f, 0.12f);
        float hurtUntil;
        public bool HurtFlashing => Time.unscaledTime < hurtUntil;

        void Update()
        {
            if (Alive && Time.time < burnUntil)
            {
                hp -= burnDps * Time.deltaTime;
                if (hp <= 0) { hp = 0; OnDeath?.Invoke(this); }
            }
            for (int i = delayed.Count - 1; i >= 0; i--)
                if (Time.time >= delayed[i].at) { var d = delayed[i].dmg; delayed.RemoveAt(i); TakeDamage(d, true); if (!Alive) return; }
            if (wasLocked && !TimeLocked) { wasLocked = false; OnTimeLockEnd?.Invoke(); }
            if (knock.sqrMagnitude > 0.0001f)
            {
                // 고양이는 CharacterController로 밀어 벽을 뚫고 방 밖으로 나가지 않게 (적은 Enemy.LateUpdate가 방 안으로 붙잡음)
                var step = knock * Time.deltaTime * 8f;
                if (isPlayer && !cc) cc = GetComponent<CharacterController>();
                if (isPlayer && cc && cc.enabled) cc.Move(step); else transform.position += step;
                knock = Vector3.Lerp(knock, Vector3.zero, Time.deltaTime * 8f);
            }
            if (flashAt > 0 && Time.time >= flashAt) { flashAt = -1; flashUntil = Time.time + 0.1f; }
            bool flash = !isPlayer && Time.time < flashUntil;
            float tintK = 0;
            if (Time.time >= hitTintFrom && Time.time < hitTintUntil)
            {
                float u = (Time.time - hitTintFrom) / Mathf.Max(0.01f, hitTintUntil - hitTintFrom);
                tintK = hitTintStrength * (1 - u * u);
                if (hitTintPulse > 0)
                {
                    float ph = hitTintPulse * (hitTintUntil - hitTintFrom) * (u + u * u);
                    tintK *= Mathf.Lerp(0.35f, 1f, Mathf.Pow(Mathf.Abs(Mathf.Sin(ph * Mathf.PI)), 2f));
                }
            }
            // 플레이어: 남은 시간 동안 약 0.07초 간격으로 빨강 ↔ 원래 색 (갈수록 옅어짐)
            float hurtK = 0;
            if (isPlayer && HurtFlashing)
            {
                float left = (hurtUntil - Time.unscaledTime) / HurtBlinkTime;
                bool on = Mathf.Repeat(Time.unscaledTime * 14f, 1f) < 0.55f;
                hurtK = on ? Mathf.Lerp(0.45f, 0.85f, left) : 0.1f * left;
            }
            if (mpb == null || rends == null || matColors == null) RefreshRenderers();       // Awake 전에 불린 경우 (비활성으로 만들어진 오브젝트 등)
            bool locked = TimeLocked || GlobalStop, stunned = Time.time < stunUntil;
            bool active = flash || locked || stunned || tintK > 0 || hurtK > 0;
            Color Fx(Color c)
            {
                if (locked) c = Color.Lerp(c, new Color(0.55f, 0.75f, 1f), 0.65f);
                else if (stunned) c = Color.Lerp(c, new Color(1f, 0.95f, 0.5f), 0.5f);
                if (tintK > 0) c = Color.Lerp(c, hitTint, tintK);
                if (hurtK > 0) c = Color.Lerp(c, HurtRed, hurtK);
                return flash ? Color.white : c;
            }
            for (int i = 0; i < rends.Length; i++)
            {
                if (!rends[i]) continue;
                if (matColors[i] != null)
                {
                    // 재질이 여러 개: 렌더러 단위로 _BaseColor를 덮으면 첫 재질 색 하나로 통째 칠해짐 (2026-11-18 분수 · 수정이 단색으로 보이던 원인)
                    // → 효과가 있을 때만 재질마다 블록을 씌우고, 끝나면 지워서 원래 재질 색으로
                    if (!active && !matTinted) continue;
                    matMpb ??= new MaterialPropertyBlock();
                    emptyMpb ??= new MaterialPropertyBlock();
                    rends[i].GetPropertyBlock(mpb);
                    float actor = mpb.GetFloat(BrawlActorId);
                    for (int k = 0; k < matColors[i].Length; k++)
                    {
                        if (!active) { rends[i].SetPropertyBlock(emptyMpb, k); continue; }
                        matMpb.Clear();
                        matMpb.SetColor(BaseColorId, Fx(matColors[i][k]));
                        if (actor > 0.5f) matMpb.SetFloat(BrawlActorId, actor);
                        rends[i].SetPropertyBlock(matMpb, k);
                    }
                    continue;
                }
                rends[i].GetPropertyBlock(mpb);
                mpb.SetColor(BaseColorId, Fx(baseColors[i]));
                rends[i].SetPropertyBlock(mpb);
            }
            matTinted = active;
        }
    }
}
