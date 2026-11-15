// 판이 끝나도 남는 것 (PlayerPrefs 저장)
// - 기억 조각, 기억(성장 노드) 단계
// - 챕터 클리어, 마을 시설 해금 · 레벨
// - 총포상: 해금한 총 · 시작 총 / 탄약 공방: 탄 풀 / 의상실: 해금 코스튬 · 선택
using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    public static class Meta
    {
        const string P = "cattang.";

        // ---------- 기억 조각 ----------
        public static int Shards
        {
            get => PlayerPrefs.GetInt(P + "shards", 0);
            set { PlayerPrefs.SetInt(P + "shards", Mathf.Max(0, value)); PlayerPrefs.Save(); }
        }

        // ---------- 기억 (성장 노드) ----------
        public static int Level(int nodeId) => PlayerPrefs.GetInt(P + "memory." + nodeId, 0);
        public static int CostOf(MemoryNodeDef n) => n.BaseCost * (Level(n.Id) + 1);

        /// <summary>기억의 방 레벨이 오르면 찍을 수 있는 노드가 늘어남 (1: 앞 3개, 2: 5개, 3: 전부)</summary>
        public static bool NodeAvailable(MemoryNodeDef n)
        {
            int idx = Tables.MemoryNodes.IndexOf(n);
            int lv = FacilityLevel(Facility.MemoryRoom);
            int open = lv >= 3 ? 99 : lv == 2 ? 5 : 3;
            return idx < open;
        }

        public static bool TryBuy(MemoryNodeDef n)
        {
            int lv = Level(n.Id);
            if (lv >= n.MaxLevel || !NodeAvailable(n) || Shards < CostOf(n)) return false;
            Shards -= CostOf(n);
            PlayerPrefs.SetInt(P + "memory." + n.Id, lv + 1);
            PlayerPrefs.Save();
            return true;
        }

        public static float Stat(string stat)
        {
            float sum = 0;
            foreach (var n in Tables.MemoryNodes)
                if (n.Stat == stat) sum += n.ValuePerLevel * Level(n.Id);
            return sum;
        }

        // ---------- 챕터 ----------
        public const int ChapterCount = 5;
        public static int ChaptersCleared
        {
            get => PlayerPrefs.GetInt(P + "chapters", 0);
            set { PlayerPrefs.SetInt(P + "chapters", Mathf.Clamp(value, 0, ChapterCount)); PlayerPrefs.Save(); }
        }
        public static void MarkChapterCleared(int chapter) { if (chapter > ChaptersCleared) ChaptersCleared = chapter; }
        public static int SelectedChapter = 1;                       // 마을 → 판 시작 때 넘김

        // ---------- 마을 시설 ----------
        public enum Facility { MemoryRoom, GunShop, AmmoWorkshop, Wardrobe }
        public static readonly string[] FacilityName = { "기억의 방", "총포상", "탄약 공방", "의상실" };
        public static readonly int[] FacilityUnlockChapter = { 0, 1, 2, 3 };    // 그 챕터 보스를 넘으면 열림
        public const int FacilityMaxLevel = 3;

        public static bool FacilityUnlocked(Facility f) => ChaptersCleared >= FacilityUnlockChapter[(int)f];
        public static int FacilityLevel(Facility f) =>
            FacilityUnlocked(f) ? Mathf.Max(1, PlayerPrefs.GetInt(P + "facility." + (int)f, 1)) : 0;
        public static int FacilityUpgradeCost(Facility f) => 30 * FacilityLevel(f);

        public static bool TryUpgradeFacility(Facility f)
        {
            int lv = FacilityLevel(f);
            if (lv <= 0 || lv >= FacilityMaxLevel || Shards < FacilityUpgradeCost(f)) return false;
            Shards -= FacilityUpgradeCost(f);
            PlayerPrefs.SetInt(P + "facility." + (int)f, lv + 1);
            PlayerPrefs.Save();
            return true;
        }

        // ---------- 총포상 ----------
        public static bool GunUnlocked(int id) => id == 2001 || PlayerPrefs.GetInt(P + "gun." + id, 0) == 1;
        public static int GunUnlockCost(GunDef g) => g.Grade switch { "Common" => 15, "Rare" => 30, "Epic" => 50, _ => 80 };
        public static bool TryUnlockGun(GunDef g)
        {
            if (GunUnlocked(g.Id) || !FacilityUnlocked(Facility.GunShop) || Shards < GunUnlockCost(g)) return false;
            // 총포상 레벨에 따라 살 수 있는 등급: 1 일반 · 2 희귀까지 · 3 전부
            int lv = FacilityLevel(Facility.GunShop);
            if ((g.Grade == "Rare" && lv < 2) || ((g.Grade == "Epic" || g.Grade == "Legendary") && lv < 3)) return false;
            Shards -= GunUnlockCost(g);
            PlayerPrefs.SetInt(P + "gun." + g.Id, 1);
            PlayerPrefs.Save();
            return true;
        }
        public static int StartGunRight { get => PlayerPrefs.GetInt(P + "startgun.r", 2001); set { PlayerPrefs.SetInt(P + "startgun.r", value); PlayerPrefs.Save(); } }
        public static int StartGunLeft { get => PlayerPrefs.GetInt(P + "startgun.l", 2001); set { PlayerPrefs.SetInt(P + "startgun.l", value); PlayerPrefs.Save(); } }

        // ---------- 탄약 공방: 드랍 · 상점 탄 풀 ----------
        /// <summary>기본: 일반 · 희귀. 공방 1: +영웅, 2: +전설, 3: 판 시작 때 연산 1개</summary>
        public static bool GradeInPool(string grade)
        {
            int lv = FacilityLevel(Facility.AmmoWorkshop);
            return grade switch { "Common" => true, "Rare" => true, "Epic" => lv >= 1, "Legendary" => lv >= 2, _ => false };
        }
        public static bool StartWithOp => FacilityLevel(Facility.AmmoWorkshop) >= 3;

        // ---------- 의상실 ----------
        public static bool CostumeUnlocked(int id) => id == 4001 || PlayerPrefs.GetInt(P + "costume." + id, 0) == 1;
        public static bool TryUnlockCostume(int id, int cost)
        {
            if (CostumeUnlocked(id) || !FacilityUnlocked(Facility.Wardrobe) || Shards < cost) return false;
            Shards -= cost;
            PlayerPrefs.SetInt(P + "costume." + id, 1);
            PlayerPrefs.Save();
            return true;
        }
        public static int SelectedCostume { get => PlayerPrefs.GetInt(P + "costume.sel", 4001); set { PlayerPrefs.SetInt(P + "costume.sel", value); PlayerPrefs.Save(); } }

        public static void ResetAll()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
        }
    }
}
