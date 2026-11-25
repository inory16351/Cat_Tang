// 효과 시스템: 효과(Effect 테이블)를 출처별로 모아 발동 시점(Trigger)에 실행.
// 출처: 활성화된 탄 · 그 칸을 쏜 총의 그 손 효과(OwnBullets/자기) · 다른 손 총의 그 손 전역 효과 · 특성 단계 효과(모든 탄) · 코스튬
using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    public static class Effects
    {
        static IEnumerable<EffectDef> Ids(params int[] ids)
        {
            foreach (var id in ids)
                if (id != 0 && Tables.Effects.TryGetValue(id, out var e)) yield return e;
        }

        static readonly Dictionary<int, EffectDef> slot12Free = new();

        /// <summary>'12시 칸에 있을 때' 조건은 그 탄의 자리로 판정 → 조건을 이미 채운 복사본</summary>
        static EffectDef NoCond(EffectDef e)
        {
            if (slot12Free.TryGetValue(e.Id, out var c)) return c;
            c = new EffectDef { Id = e.Id, Trigger = e.Trigger, Cond = "None", CondValue = 0, Chance = e.Chance, Target = e.Target, Type = e.Type, Value = e.Value, Value2 = e.Value2, Duration = e.Duration, Desc = e.Desc };
            slot12Free[e.Id] = c;
            return c;
        }

        /// <summary>탄 고유 효과 설명 + 발동 확률 (2026-11-18 사용자: 확률 발동이면 UI에 표기). 탄 효과는 그 탄을 쏠 때만 (For 참고)</summary>
        public static string BulletEffectDesc(EffectDef e, string grade)
        {
            bool perShot = e.Trigger == "OnFire" || e.Trigger == "OnHit" || e.Trigger == "OnPierce" || e.Trigger == "OnRicochet" || e.Trigger == "OnKill";
            if (!perShot || e.Target == "Neighbors" || e.Cond == "Slot12" || e.Chance >= 1f) return e.Desc;
            return $"{e.Desc} <size=15><color=#9FB3C8>(확률 {Mathf.RoundToInt(e.Chance * 100f)}%)</color></size>";
        }

        /// <summary>칸 s를 쏠 때 적용되는 효과 (trigger가 같은 것).
        /// 탄 고유 효과는 그 탄(칸 s)이 활성화되어 있고 그 탄을 쏠 때만 한 번 (2026-11-18 사용자: 예전 '활성화된 탄 효과를 모든 사격에 겹쳐 적용
        /// + 직접 쏠 때 2배' 규칙은 없앰). 총 · 특성 단계 · 코스튬 효과는 그대로 모든 사격에</summary>
        public static IEnumerable<EffectDef> For(PlayerController p, int s, string trigger)
        {
            var rev = p.rev;
            if (s >= 1 && s <= 12)
            {
                var b = rev.slots[s].bullet;
                if (b != null && rev.Awakened(s))
                    foreach (var e in Ids(b.Effect1, b.Effect2))
                    {
                        if (e.Trigger != trigger || e.Target == "Neighbors") continue;
                        if (e.Cond == "Slot12") { if (s != 12) continue; yield return NoCond(e); continue; }
                        yield return e;
                    }
            }
            bool rightSide = s % 2 == 1;
            var own = rightSide ? rev.rightGun : rev.leftGun;
            var other = rightSide ? rev.leftGun : rev.rightGun;
            if (own != null) foreach (var e in Ids(own.HandEffects(rightSide))) if (e.Trigger == trigger) yield return e;
            if (other != null) foreach (var e in Ids(other.HandEffects(!rightSide))) if (e.Trigger == trigger && e.Target != "OwnBullets") yield return e;
            foreach (var e in rev.ActiveTraitEffects()) if (e.Trigger == trigger) yield return e;
            foreach (var e in Costume()) if (e.Trigger == trigger) yield return e;
        }

        /// <summary>칸과 무관한 시점(장전 · 회피 · 스킬)의 효과: 활성화된 탄 전부 + 양손 총 + 특성 + 코스튬</summary>
        public static IEnumerable<EffectDef> Global(PlayerController p, string trigger)
        {
            var rev = p.rev;
            for (int s = 1; s <= 12; s++)
            {
                var b = rev.slots[s].bullet;
                if (b == null || !rev.Awakened(s)) continue;
                foreach (var e in Ids(b.Effect1, b.Effect2)) if (e.Trigger == trigger) yield return e;
            }
            if (rev.rightGun != null) foreach (var e in Ids(rev.rightGun.HandEffects(true))) if (e.Trigger == trigger) yield return e;
            if (rev.leftGun != null) foreach (var e in Ids(rev.leftGun.HandEffects(false))) if (e.Trigger == trigger) yield return e;
            foreach (var e in rev.ActiveTraitEffects()) if (e.Trigger == trigger) yield return e;
            foreach (var e in Costume()) if (e.Trigger == trigger) yield return e;
        }

        static IEnumerable<EffectDef> Costume()
        {
            if (!Tables.Costumes.TryGetValue(Meta.SelectedCostume, out var c)) yield break;
            foreach (var e in Ids(c.Effect1, c.Effect2, c.Effect3)) yield return e;
        }

        public static bool CondOk(EffectDef e, PlayerController p, int s, Health target)
        {
            int judge = s >= 1 && s <= 12 ? p.rev.JudgeTime(s) : 0;
            switch (e.Cond)
            {
                case "None": case "": return true;
                case "Slot12": return s == 12;
                case "TargetMarked": return target && target.MarkStacks > 0;
                case "MarkStacksAtLeast": return target && target.MarkStacks >= e.CondValue;
                case "MarkFullHitsAtLeast": return target && target.MarkFullHits >= e.CondValue;
                case "TargetSlowed": return target && target.Slowed;
                case "TargetTimeLocked": return target && target.TimeLocked;
                case "TargetBoss": return target && target.IsBoss;
                case "TargetHpBelow": return target && target.hp <= target.maxHp * e.CondValue / 100f * (e.Type == "Execute" ? 1 + GlobalPassive(p, "ExecuteRangeAdd") : 1f);   // 코스튬 장의사 정장: 처형 범위 +20%
                case "JudgeEven": return judge % 2 == 0;
                case "JudgeNegative": return judge < 0;
                case "JudgeOver": return judge > e.CondValue;
                case "JudgeAtMost": return judge <= e.CondValue;
                default: return false;
            }
        }

        static float ProcMul(PlayerController p) => p ? p.ProcMul : 1f;
        public static bool Roll(EffectDef e) => e.Chance >= 1f || Random.value < e.Chance * ProcMul(PlayerController.Instance);

        /// <summary>칸 s 사격의 패시브 합 (대상 없는 조건만)</summary>
        public static float Passive(PlayerController p, int s, string type)
        {
            float v = 0;
            foreach (var e in For(p, s, "Passive")) if (e.Type == type && CondOk(e, p, s, null)) v += e.Borrowed ? e.Value * e.Chance : e.Value;
            return v;
        }

        /// <summary>Passive 중 조건이 붙은 것만 (판정 시각 짝수 · 음수 · 12 초과 등) — 조건부 추가 피해 금빛 숫자용</summary>
        public static float PassiveConditional(PlayerController p, int s, string type)
        {
            float v = 0;
            foreach (var e in For(p, s, "Passive"))
                if (e.Type == type && e.Cond != "None" && e.Cond != "" && CondOk(e, p, s, null)) v += e.Borrowed ? e.Value * e.Chance : e.Value;
            return v;
        }

        /// <summary>칸과 무관한 전역 패시브 합 (특성 · 코스튬 · 총 · 활성화된 탄 전부)</summary>
        public static float GlobalPassive(PlayerController p, string type)
        {
            float v = 0;
            foreach (var e in Global(p, "Passive")) if (e.Type == type && e.Target != "OwnBullets" && e.Target != "Neighbors") v += e.Value;
            return v;
        }

        // ---------- 범위 효과 ----------
        /// <summary>범위 피해만 (그림은 부른 쪽이 따로 — 추억 시계 깨짐)</summary>
        public static void AreaDamage(Vector3 at, float radius, float dmg)
        {
            var pl = PlayerController.Instance;
            if (pl) dmg *= 1 + GlobalPassive(pl, "AreaDamageAdd");
            foreach (var en in Enemy.All.ToArray())
                if (en && en.health.Alive && Flat(en.transform.position - at).magnitude < radius) en.health.TakeDamage(dmg);
        }

        public static void Explosion(Vector3 at, float radius, float dmg, Color tint)
        {
            var pl = PlayerController.Instance;
            if (pl) dmg *= 1 + GlobalPassive(pl, "AreaDamageAdd");
            GunkataFx.Explosion(at, radius, false, tint);   // 3D 탄 색 폭발
            foreach (var en in Enemy.All.ToArray())
                if (en && en.health.Alive && Flat(en.transform.position - at).magnitude < radius) en.health.TakeDamage(dmg);
        }

        public static void AreaStun(Vector3 at, float radius, float dur)
        {
            // 2026-11-17 사용자: 바닥을 쓸고 가는 금빛 흙먼지 띠 + 닿은 적마다 짧은 번쩍
            var gold = new Color(1f, 0.82f, 0.4f);
            ExtraFx.GroundWave(at, radius, gold);
            foreach (var en in Enemy.All)
                if (en && Flat(en.transform.position - at).magnitude < radius)
                {
                    en.health.ApplyStun(dur);
                    FxBurst.Spawn(en.transform.position + Vector3.up * 1.2f, 1.6f, FxBurst.Kind.Spark, gold);
                }
        }

        public static void AreaSlow(Vector3 at, float radius, float amount, float dur)
        {
            ExtraFx.SandSwirl(at, radius, new Color(0.45f, 0.65f, 1f));   // 2026-11-17 사용자: 바닥 푸른 모래 소용돌이
            foreach (var en in Enemy.All) if (en && Flat(en.transform.position - at).magnitude < radius) en.health.ApplySlow(amount, dur);
        }

        public static Vector3 Flat(Vector3 v) { v.y = 0; return v; }

        public static Enemy Nearest(Vector3 from, float maxDist, ICollection<Health> exclude = null)
        {
            Enemy best = null;
            float bd = maxDist;
            foreach (var e in Enemy.All)
            {
                if (!e || !e.health.Alive || (exclude != null && exclude.Contains(e.health))) continue;
                float d = Flat(e.transform.position - from).magnitude;
                if (d < bd) { bd = d; best = e; }
            }
            return best;
        }
    }

    /// <summary>탄마다 다른 색 (첫 특성 기준, 기본 탄은 크림색). 비활성화된 탄은 흐리게.</summary>
    public static class BulletLook
    {
        public static Color Of(BulletDef b)
        {
            if (b == null || b.Trait1 == 0) return new Color(1f, 0.95f, 0.82f);
            return b.Trait1 switch
            {
                3001 => new Color(0.75f, 0.55f, 1f),     // 회상
                3002 => new Color(1f, 0.55f, 0.8f),      // 추억
                3003 => new Color(0.55f, 0.85f, 1f),     // 망각
                3004 => new Color(0.4f, 0.95f, 0.85f),   // 되감기
                3005 => new Color(1f, 0.35f, 0.3f),      // 종말
                3006 => new Color(1f, 0.6f, 0.25f),      // 지연
                3007 => new Color(0.65f, 0.5f, 1f),      // 예지
                3008 => new Color(1f, 0.85f, 0.35f),     // 영원
                _ => new Color(0.44f, 0.49f, 1f)         // 미련 (2026-11-25 사용자: 푸른 원혼빛, 망각 · 영원과 구분되게 남보라 쪽)
            };
        }

        public static Color Shown(BulletDef b, bool awake) => awake ? Of(b) : Color.Lerp(Of(b), new Color(0.9f, 0.88f, 0.85f), 0.65f);

        public static float Width(BulletDef b, bool awake)
        {
            if (b == null) return 0.08f;
            float w = b.Grade switch { "Rare" => 0.11f, "Epic" => 0.14f, "Legendary" => 0.18f, _ => 0.09f };
            return awake ? w * 1.35f : w * 0.85f;
        }
    }
}
