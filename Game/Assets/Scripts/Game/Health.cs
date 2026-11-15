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
        public bool isPlayer;
        public bool dashing;                          // 플레이어 대시 중 (저스트 회피 판정)
        public event Action<Health> OnDeath;
        public event Action<float> OnDamaged;
        public event Action OnTimeLockEnd;            // 시간에 갇힌 상태가 풀릴 때
        public event Action OnJustDodge;              // 대시 중에 맞을 뻔함
        public event Action OnChanceDodge;            // 확률 회피 성공

        public bool Alive => hp > 0;
        public float SlowMul => Time.time < slowUntil ? 1f - slowAmount : 1f;
        public bool Stunned => Time.time < stunUntil || TimeLocked || GlobalStop;
        public bool TimeLocked => Time.time < lockUntil;
        public bool Slowed => Time.time < slowUntil;
        public float AttackSpeedMul => Time.time < atkDownUntil ? 1f - atkDown : 1f;
        public int MarkStacks => Time.time < markUntil ? marks : 0;
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
        int marks, markMax = 10;
        bool wasLocked;
        Vector3 knock;
        readonly List<(float at, float dmg)> delayed = new();
        Renderer[] rends;
        MaterialPropertyBlock mpb;
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        Color[] baseColors;

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
            for (int i = 0; i < rends.Length; i++)
                baseColors[i] = rends[i].sharedMaterial && rends[i].sharedMaterial.HasProperty(BaseColorId)
                    ? rends[i].sharedMaterial.GetColor(BaseColorId) : Color.white;
        }

        public void ResetHp(float max) { maxHp = max; hp = max; }

        /// <summary>피해 받기. 플레이어는 대시 중이면 저스트 회피, 확률 회피 판정.</summary>
        public void TakeDamage(float dmg, bool fromDelayed = false)
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
            if (fromDelayed) lastDamageWasDelayed = Time.time;
            flashUntil = Time.time + 0.08f;
            OnDamaged?.Invoke(dmg);
            if (!isPlayer) DamageNumber.Spawn(transform.position + Vector3.up * 1.6f, dmg);
            if (hp <= 0) { hp = 0; OnDeath?.Invoke(this); }
        }

        public void Heal(float v) => hp = Mathf.Min(maxHp, hp + v);
        public void ApplySlow(float amount, float dur) { slowAmount = Time.time < slowUntil ? Mathf.Max(slowAmount, amount) : amount; slowUntil = Mathf.Max(slowUntil, Time.time + dur); }
        public void ApplyStun(float dur) => stunUntil = Mathf.Max(stunUntil, Time.time + dur);
        public void ApplyTimeLock(float dur) { lockUntil = Mathf.Max(lockUntil, Time.time + dur); wasLocked = true; }
        public void ApplyBurn(float dps, float dur) { burnDps = Mathf.Max(burnDps, dps); burnUntil = Time.time + dur; }
        public void ApplyDefenseDown(float v, float dur) { defDown = Mathf.Max(defDown, v); defDownUntil = Time.time + dur; }
        public void ApplyAttackSpeedDown(float v, float dur) { atkDown = Mathf.Max(atkDown, v); atkDownUntil = Time.time + dur; }
        public void AddMark(float amp, int max) { if (Time.time >= markUntil) marks = 0; markAmp = amp; markMax = Mathf.Max(1, max); marks = Mathf.Min(markMax, marks + 1); markUntil = Time.time + 6f; }
        public void ClearMarks() => marks = 0;
        public void AddDelayed(float dmg, float after) => delayed.Add((Time.time + after, dmg));
        public void Knockback(Vector3 v) => knock += v;

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
                transform.position += knock * Time.deltaTime * 8f;
                knock = Vector3.Lerp(knock, Vector3.zero, Time.deltaTime * 8f);
            }
            bool flash = Time.time < flashUntil;
            for (int i = 0; i < rends.Length; i++)
            {
                if (!rends[i]) continue;
                rends[i].GetPropertyBlock(mpb);
                Color c = baseColors[i];
                if (TimeLocked || GlobalStop) c = Color.Lerp(c, new Color(0.55f, 0.75f, 1f), 0.65f);
                else if (Time.time < stunUntil) c = Color.Lerp(c, new Color(1f, 0.95f, 0.5f), 0.5f);
                else if (MarkStacks > 0) c = Color.Lerp(c, new Color(1f, 0.5f, 0.85f), 0.15f + 0.04f * MarkStacks);
                mpb.SetColor(BaseColorId, flash ? Color.white : c);
                rends[i].SetPropertyBlock(mpb);
            }
        }
    }
}
