// 탄환 시계: 실린더 12칸 = 인벤토리. 오른총 = 홀수 칸, 왼총 = 짝수 칸. 발사는 1 → 12 (되감는 실린더 효과면 12 → 1).
// 판정 시각 = 놓인 시각에 연산을 위 슬롯부터 적용 (+ 이웃 탄의 판정 +1 효과). 나누어떨어지지 않으면 반올림(.5는 0에서 멀어지게).
// 깨어난 탄(자리 조건 충족)만 특성 · 고유 효과, 못 깨어나면 공격력만. 기본 탄 '탄환'은 조건 없음 = 늘 잠듦.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    [Serializable]
    public class Slot
    {
        public BulletDef bullet;
        public List<OpDef> ops = new();
        public float permanentBonus;          // 영구 피해 성장 (처치로 쌓임)
    }

    public class Revolver
    {
        public readonly Slot[] slots = new Slot[13];          // 1~12 사용
        public GunDef rightGun, leftGun;
        public int next = 1;
        public int shotsThisCycle;                             // 이번 바퀴에 쏜 수 (12면 장전)
        public bool reversed;                                  // 12 → 1로 쏨
        public bool reloading;
        public float heat;
        public const float HeatMax = 3f;

        public Revolver()
        {
            for (int i = 1; i <= 12; i++) slots[i] = new Slot();
        }

        public GunDef GunFor(int slot) => slot % 2 == 1 ? rightGun : leftGun;
        public int FirstSlot => reversed ? 12 : 1;

        public static int ApplyOps(int v, List<OpDef> ops)
        {
            foreach (var op in ops)
            {
                switch (op.OpType)
                {
                    case "Add": v += op.Operand; break;
                    case "Sub": v -= op.Operand; break;
                    case "Mul": v *= op.Operand; break;
                    case "Div":
                        if (op.Operand == 0) break;
                        double q = (double)v / op.Operand;
                        v = (int)(Math.Sign(q) * Math.Floor(Math.Abs(q) + 0.5));
                        break;
                }
            }
            return v;
        }

        /// <summary>이웃 효과를 뺀 판정 시각</summary>
        public int BaseJudge(int slot) => ApplyOps(slot, slots[slot].ops);
        bool BaseAwake(int slot) { var b = slots[slot].bullet; return b != null && Meets(b.CondType, b.CondValue, BaseJudge(slot)); }

        /// <summary>판정 시각 (이웃 탄의 '판정 +N' 효과 포함: 이웃이 스스로 깨어 있을 때)</summary>
        public int JudgeTime(int slot)
        {
            int v = BaseJudge(slot);
            foreach (int n in new[] { slot == 1 ? 12 : slot - 1, slot == 12 ? 1 : slot + 1 })
            {
                var nb = slots[n].bullet;
                if (nb == null || !BaseAwake(n)) continue;
                foreach (var id in new[] { nb.Effect1, nb.Effect2 })
                    if (id != 0 && Tables.Effects.TryGetValue(id, out var e) && e.Type == "JudgeTimeAdd" && e.Target == "Neighbors") v += Mathf.RoundToInt(e.Value);
            }
            return v;
        }

        public static bool Meets(string cond, int value, int t)
        {
            switch (cond)
            {
                case "Odd": return t % 2 != 0;
                case "Even": return t % 2 == 0;
                case "Exact": return t == value;
                case "GreaterThan": return t > value;
                case "LessThan": return t < value;
                case "MultipleOf": return value != 0 && t % value == 0;
                case "Negative": return t < 0;
                case "Zero": return t == 0;
                case "Prime":
                    if (t < 2) return false;
                    for (int d = 2; d * d <= t; d++) if (t % d == 0) return false;
                    return true;
                default: return false;                         // None = 기본 탄
            }
        }

        public bool Awakened(int slot)
        {
            var b = slots[slot].bullet;
            return b != null && Meets(b.CondType, b.CondValue, JudgeTime(slot));
        }

        public int UnawakenedCount()
        {
            int c = 0;
            for (int i = 1; i <= 12; i++) if (slots[i].bullet != null && !Awakened(i)) c++;
            return c;
        }

        // ---------- 특성 ----------
        /// <summary>전역 패시브 값 합 (코스튬 + 특성 단계 효과 중 Type이 같은 것)</summary>
        public float GlobalPassive(string type)
        {
            float v = PlayerController.CostumeStat(type);
            foreach (var e in ActiveTraitEffects()) if (e.Type == type && e.Trigger == "Passive") v += e.Value;
            return v;
        }

        /// <summary>깨어난 탄에서 센 특성 개수 (+ 특성 수 보너스, 미련 6단계면 잠든 탄의 첫 특성도)</summary>
        public Dictionary<int, int> TraitCounts()
        {
            var d = new Dictionary<int, int>();
            bool unawakenedFirst = false;
            for (int i = 1; i <= 12; i++)
            {
                if (!Awakened(i)) continue;
                var b = slots[i].bullet;
                foreach (var id in new[] { b.Effect1, b.Effect2 })
                    if (id != 0 && Tables.Effects.TryGetValue(id, out var e) && e.Type == "TraitCountBonus")
                        d[(int)e.Value2] = (d.TryGetValue((int)e.Value2, out var c0) ? c0 : 0) + Mathf.RoundToInt(e.Value);
                foreach (var t in new[] { b.Trait1, b.Trait2 })
                    if (t != 0) d[t] = d.TryGetValue(t, out var c) ? c + 1 : 1;
            }
            foreach (var e in TierEffects(d)) if (e.Type == "UnawakenedTraitCount") unawakenedFirst = true;
            if (unawakenedFirst)
                for (int i = 1; i <= 12; i++)
                {
                    var b = slots[i].bullet;
                    if (b == null || Awakened(i) || b.Trait1 == 0) continue;
                    d[b.Trait1] = d.TryGetValue(b.Trait1, out var c) ? c + 1 : 1;
                }
            return d;
        }

        public int TierThresholdReduce => Mathf.RoundToInt(PlayerController.CostumeStat("TraitThresholdReduce"));

        public int TierOf(int count)
        {
            int r = TierThresholdReduce;
            return count >= 6 - r ? 6 : count >= 4 - r ? 4 : count >= 2 - r ? 2 : 0;
        }

        IEnumerable<EffectDef> TierEffects(Dictionary<int, int> counts)
        {
            foreach (var (trait, tier, effect) in Tables.TraitTiers)
            {
                if (!counts.TryGetValue(trait, out var c) || TierOf(c) < tier) continue;
                if (Tables.Effects.TryGetValue(effect, out var e)) yield return e;
            }
        }

        /// <summary>지금 켜져 있는 특성 단계 효과 (모든 탄에 적용)</summary>
        public List<EffectDef> ActiveTraitEffects()
        {
            var counts = TraitCounts();
            var list = new List<EffectDef>();
            foreach (var e in TierEffects(counts)) list.Add(e);
            return list;
        }

        public void AddHeat(float v) => heat = Mathf.Clamp(heat + v, 0, HeatMax);
    }
}
