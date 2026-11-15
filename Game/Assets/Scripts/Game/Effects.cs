// 효과 시스템: 효과(Effect 테이블)를 출처별로 모아 발동 시점(Trigger)에 실행.
// 출처: 깨어난 탄 · 그 칸을 쏜 총(OwnBullets/자기) · 다른 손 총의 전역 효과 · 특성 단계 효과(모든 탄) · 코스튬
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

        /// <summary>칸 s를 쏠 때 적용되는 효과 (trigger가 같은 것)</summary>
        public static IEnumerable<EffectDef> For(PlayerController p, int s, string trigger)
        {
            var rev = p.rev;
            var b = rev.slots[s].bullet;
            if (b != null && rev.Awakened(s))
                foreach (var e in Ids(b.Effect1, b.Effect2)) if (e.Trigger == trigger && e.Target != "Neighbors") yield return e;
            var own = rev.GunFor(s);
            var other = s % 2 == 1 ? rev.leftGun : rev.rightGun;
            if (own != null) foreach (var e in Ids(own.Effect1, own.Effect2)) if (e.Trigger == trigger) yield return e;
            if (other != null && other != own) foreach (var e in Ids(other.Effect1, other.Effect2)) if (e.Trigger == trigger && e.Target != "OwnBullets") yield return e;
            foreach (var e in rev.ActiveTraitEffects()) if (e.Trigger == trigger) yield return e;
            foreach (var e in Costume()) if (e.Trigger == trigger) yield return e;
        }

        /// <summary>칸과 무관한 시점(장전 · 회피 · 스킬)의 효과: 깨어난 탄 전부 + 양손 총 + 특성 + 코스튬</summary>
        public static IEnumerable<EffectDef> Global(PlayerController p, string trigger)
        {
            var rev = p.rev;
            for (int s = 1; s <= 12; s++)
            {
                var b = rev.slots[s].bullet;
                if (b == null || !rev.Awakened(s)) continue;
                foreach (var e in Ids(b.Effect1, b.Effect2)) if (e.Trigger == trigger) yield return e;
            }
            var guns = new HashSet<GunDef>();
            if (rev.rightGun != null) guns.Add(rev.rightGun);
            if (rev.leftGun != null) guns.Add(rev.leftGun);
            foreach (var g in guns) foreach (var e in Ids(g.Effect1, g.Effect2)) if (e.Trigger == trigger) yield return e;
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
                case "TargetSlowed": return target && target.Slowed;
                case "TargetTimeLocked": return target && target.TimeLocked;
                case "TargetBoss": return target && target.IsBoss;
                case "TargetHpBelow": return target && target.hp <= target.maxHp * e.CondValue / 100f;
                case "JudgeEven": return judge % 2 == 0;
                case "JudgeNegative": return judge < 0;
                case "JudgeOver": return judge > e.CondValue;
                case "JudgeAtMost": return judge <= e.CondValue;
                default: return false;
            }
        }

        public static bool Roll(EffectDef e) => e.Chance >= 1f || Random.value < e.Chance;

        /// <summary>칸 s 사격의 패시브 합 (대상 없는 조건만)</summary>
        public static float Passive(PlayerController p, int s, string type)
        {
            float v = 0;
            foreach (var e in For(p, s, "Passive")) if (e.Type == type && CondOk(e, p, s, null)) v += e.Value;
            return v;
        }

        /// <summary>칸과 무관한 전역 패시브 합 (특성 · 코스튬 · 총 · 깨어난 탄 전부)</summary>
        public static float GlobalPassive(PlayerController p, string type)
        {
            float v = 0;
            foreach (var e in Global(p, "Passive")) if (e.Type == type && e.Target != "OwnBullets" && e.Target != "Neighbors") v += e.Value;
            return v;
        }

        // ---------- 범위 효과 ----------
        public static void Explosion(Vector3 at, float radius, float dmg, Color tint)
        {
            FxBurst.Spawn(at + Vector3.up * 0.8f, radius * 1.6f, FxBurst.Kind.Explosion, tint);
            foreach (var en in Enemy.All.ToArray())
                if (en && en.health.Alive && Flat(en.transform.position - at).magnitude < radius) en.health.TakeDamage(dmg);
        }

        public static void AreaStun(Vector3 at, float radius, float dur)
        {
            FxBurst.Spawn(at + Vector3.up, radius * 1.4f, FxBurst.Kind.Gold, new Color(1f, 0.9f, 0.5f));
            foreach (var en in Enemy.All) if (en && Flat(en.transform.position - at).magnitude < radius) en.health.ApplyStun(dur);
        }

        public static void AreaSlow(Vector3 at, float radius, float amount, float dur)
        {
            FxBurst.Spawn(at + Vector3.up, radius * 1.8f, FxBurst.Kind.Gold, new Color(0.55f, 0.75f, 1f));
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

    /// <summary>탄마다 다른 색 (첫 특성 기준, 기본 탄은 크림색). 잠든 탄은 흐리게.</summary>
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
                _ => new Color(0.65f, 0.72f, 0.9f)       // 미련
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
