// 데이터 테이블(CSV) 읽기. 원본: docs/tools/build_tables.py → Game/Assets/Data/Tables/*.csv
// 첫 줄은 "이름:타입" 헤더. 따옴표 안 쉼표 처리.
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace CatTang
{
    public class BulletDef
    {
        public int Id; public string Name; public string Grade; public int Damage;
        public string CondType; public int CondValue; public int Trait1, Trait2; public int OpSlots;
        public int Effect1, Effect2; public string Desc;
    }

    public class GunDef
    {
        public int Id; public string Name; public string Grade; public float DamageMul, FireInterval, ReloadTime, Range;
        public int SkillId, Effect1, Effect2; public string Desc;
    }

    public class EffectDef
    {
        public int Id; public string Trigger, Cond; public int CondValue; public float Chance;
        public string Target, Type; public float Value, Value2, Duration; public string Desc;
    }

    public class OpDef { public int Id; public string Name, OpType; public int Operand, DropWeight; }

    public class TraitDef { public int Id; public string Name, Desc; }

    public class MemoryNodeDef
    {
        public int Id; public string Name, Stat; public float ValuePerLevel; public int MaxLevel, BaseCost; public string Desc;
    }

    public class ChapterDef
    {
        public int Id; public string Name; public Color Floor, Wall; public string Prop;
        public float HpMul, DmgMul, ShooterRatio; public string BossName, BossPattern; public int BossHp; public string Desc;
    }

    public class CostumeDef
    {
        public int Id; public string Name, UnlockType; public int UnlockValue, Cost, Effect1, Effect2, Effect3; public string Desc;
    }

    public static class Tables
    {
        public static readonly List<ChapterDef> Chapters = new();
        public static readonly Dictionary<int, CostumeDef> Costumes = new();
        public static readonly Dictionary<int, BulletDef> Bullets = new();
        public static readonly Dictionary<int, GunDef> Guns = new();
        public static readonly Dictionary<int, EffectDef> Effects = new();
        public static readonly Dictionary<int, OpDef> Ops = new();
        public static readonly Dictionary<int, TraitDef> Traits = new();
        public static readonly List<MemoryNodeDef> MemoryNodes = new();
        public static readonly List<(int trait, int tier, int effect)> TraitTiers = new();
        static bool loaded;

        public static void Load()
        {
            if (loaded) return;
            loaded = true;
            foreach (var r in Read("Bullet"))
                Bullets[I(r, "Id")] = new BulletDef
                {
                    Id = I(r, "Id"), Name = r["Name"], Grade = r["Grade"], Damage = I(r, "Damage"), CondType = r["CondType"],
                    CondValue = I(r, "CondValue"), Trait1 = I(r, "Trait1"), Trait2 = I(r, "Trait2"), OpSlots = I(r, "OpSlots"),
                    Effect1 = I(r, "Effect1"), Effect2 = I(r, "Effect2"), Desc = r["Desc"]
                };
            foreach (var r in Read("Gun"))
                Guns[I(r, "Id")] = new GunDef
                {
                    Id = I(r, "Id"), Name = r["Name"], Grade = r["Grade"], DamageMul = F(r, "DamageMul"), FireInterval = F(r, "FireInterval"),
                    ReloadTime = F(r, "ReloadTime"), Range = F(r, "Range"), SkillId = I(r, "SkillId"), Effect1 = I(r, "Effect1"),
                    Effect2 = I(r, "Effect2"), Desc = r["Desc"]
                };
            foreach (var r in Read("Effect"))
                Effects[I(r, "Id")] = new EffectDef
                {
                    Id = I(r, "Id"), Trigger = r["Trigger"], Cond = r["Cond"], CondValue = I(r, "CondValue"), Chance = F(r, "Chance"),
                    Target = r["Target"], Type = r["Type"], Value = F(r, "Value"), Value2 = F(r, "Value2"), Duration = F(r, "Duration"), Desc = r["Desc"]
                };
            foreach (var r in Read("Op"))
                Ops[I(r, "Id")] = new OpDef { Id = I(r, "Id"), Name = r["Name"], OpType = r["OpType"], Operand = I(r, "Operand"), DropWeight = I(r, "DropWeight") };
            foreach (var r in Read("Trait"))
                Traits[I(r, "Id")] = new TraitDef { Id = I(r, "Id"), Name = r["Name"], Desc = r["Desc"] };
            foreach (var r in Read("Memory"))
                MemoryNodes.Add(new MemoryNodeDef
                {
                    Id = I(r, "Id"), Name = r["Name"], Stat = r["Stat"], ValuePerLevel = F(r, "ValuePerLevel"),
                    MaxLevel = I(r, "MaxLevel"), BaseCost = I(r, "BaseCost"), Desc = r["Desc"]
                });
            foreach (var r in Read("TraitTier"))
                TraitTiers.Add((I(r, "TraitId"), I(r, "Tier"), I(r, "EffectId")));
            foreach (var r in Read("Chapter"))
                Chapters.Add(new ChapterDef
                {
                    Id = I(r, "Id"), Name = r["Name"], Floor = Hex(r["Floor"]), Wall = Hex(r["Wall"]), Prop = r["Prop"],
                    HpMul = F(r, "HpMul"), DmgMul = F(r, "DmgMul"), ShooterRatio = F(r, "ShooterRatio"),
                    BossName = r["BossName"], BossPattern = r["BossPattern"], BossHp = I(r, "BossHp"), Desc = r["Desc"]
                });
            foreach (var r in Read("Costume"))
                Costumes[I(r, "Id")] = new CostumeDef
                {
                    Id = I(r, "Id"), Name = r["Name"], UnlockType = r["UnlockType"], UnlockValue = I(r, "UnlockValue"), Cost = I(r, "Cost"),
                    Effect1 = I(r, "Effect1"), Effect2 = I(r, "Effect2"), Effect3 = I(r, "Effect3"), Desc = r["Desc"]
                };
            Debug.Log($"[Tables] 챕터 {Chapters.Count} · 코스튬 {Costumes.Count}");
            Debug.Log($"[Tables] 탄 {Bullets.Count} · 총 {Guns.Count} · 효과 {Effects.Count} · 연산 {Ops.Count} · 기억 {MemoryNodes.Count}");
        }

        public static ChapterDef Chapter(int id) => Chapters.Find(c => c.Id == id) ?? (Chapters.Count > 0 ? Chapters[0] : null);

        static Color Hex(string h) => ColorUtility.TryParseHtmlString("#" + h, out var c) ? c : Color.gray;

        static int I(Dictionary<string, string> r, string k) =>
            r.TryGetValue(k, out var v) && int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out var x) ? x : 0;

        static float F(Dictionary<string, string> r, string k) =>
            r.TryGetValue(k, out var v) && float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out var x) ? x : 0f;

        static List<Dictionary<string, string>> Read(string name)
        {
            var list = new List<Dictionary<string, string>>();
            var path = Path.Combine(Application.dataPath, "Data/Tables", name + ".csv");
            if (!File.Exists(path)) { Debug.LogWarning("[Tables] 없음 " + path); return list; }
            var lines = File.ReadAllLines(path, Encoding.UTF8);
            if (lines.Length == 0) return list;
            var head = Split(lines[0]);
            for (int i = 0; i < head.Count; i++) head[i] = head[i].Split(':')[0].Trim('﻿');
            for (int l = 1; l < lines.Length; l++)
            {
                if (string.IsNullOrWhiteSpace(lines[l])) continue;
                var cells = Split(lines[l]);
                var row = new Dictionary<string, string>();
                for (int i = 0; i < head.Count; i++) row[head[i]] = i < cells.Count ? cells[i] : "";
                list.Add(row);
            }
            return list;
        }

        static List<string> Split(string line)
        {
            var res = new List<string>();
            var sb = new StringBuilder();
            bool q = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"') { if (q && i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; } else q = !q; }
                else if (c == ',' && !q) { res.Add(sb.ToString()); sb.Clear(); }
                else sb.Append(c);
            }
            res.Add(sb.ToString());
            return res;
        }
    }
}
