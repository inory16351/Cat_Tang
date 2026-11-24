// 판이 끝나도 남는 것 (PlayerPrefs 저장, 저장 슬롯 3개 — 2026-11-18 사용자: 메인 로비에서 슬롯 고르기)
// - 시간의 조각, 기억(능력치) 단계, 챕터 클리어
// - 해금 (2026-11-18 사용자: 시설 레벨 없앰, 여타 로그라이크처럼 새 아이템 해금이 주요 성장):
//   총포상 · 탄약 공방 = 해금 묶음(Unlock 표)을 조각으로 사면 묶음 안 아이템이 드랍 · 상점 · 시작 총에 풀림 (세피리아식)
//   의상실 = 코스튬마다 업적 조건을 채우면 조각으로 구매
//   기억의 방 = 능력치 노드, 챕터를 넘을수록 노드가 더 열림
using System;
using UnityEngine;

namespace CatTang
{
    public static class Meta
    {
        // ---------- 저장 슬롯 ----------
        public const int SlotCount = 3;
        const string Root = "cattang.";
        public static int ActiveSlot
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(Root + "slot", 1), 1, SlotCount);
            set { PlayerPrefs.SetInt(Root + "slot", Mathf.Clamp(value, 1, SlotCount)); PlayerPrefs.Save(); }
        }
        // 슬롯을 지우면 세대(gen)를 올려 새 키 묶음을 씀 (PlayerPrefs는 키 목록을 못 얻음)
        static int Gen(int s) => PlayerPrefs.GetInt($"{Root}s{s}.gen", 0);
        static string SlotPrefix(int s) => $"{Root}s{s}g{Gen(s)}.";
        static string P => SlotPrefix(ActiveSlot);

        public static bool SlotUsed(int s) => PlayerPrefs.GetInt(SlotPrefix(s) + "used", 0) == 1;
        public static int SlotChapters(int s) => PlayerPrefs.GetInt(SlotPrefix(s) + "chapters", 0);
        public static int SlotShards(int s) => PlayerPrefs.GetInt(SlotPrefix(s) + "shards", 0);
        public static int SlotRuns(int s) => PlayerPrefs.GetInt(SlotPrefix(s) + "runs", 0);
        public static string SlotLastPlayed(int s) => PlayerPrefs.GetString(SlotPrefix(s) + "last", "");

        /// <summary>슬롯을 골라 시작 (빈 슬롯이면 새로 만듦)</summary>
        public static void BeginSlot(int s)
        {
            ActiveSlot = s;
            if (!SlotUsed(s)) PlayerPrefs.SetInt(P + "used", 1);
            Touch();
        }

        public static void DeleteSlot(int s)
        {
            PlayerPrefs.SetInt($"{Root}s{s}.gen", Gen(s) + 1);
            PlayerPrefs.Save();
        }

        /// <summary>마지막 플레이 시각 기록 + 저장</summary>
        static void Touch()
        {
            PlayerPrefs.SetString(P + "last", DateTime.Now.ToString("yyyy.MM.dd HH:mm"));
            PlayerPrefs.Save();
        }

        // ---------- 보스 등장 연출 (첫 만남은 스킵 불가) ----------
        public static bool IntroSeen(string id) => PlayerPrefs.GetInt(P + "intro." + id, 0) == 1;
        public static void MarkIntroSeen(string id) { PlayerPrefs.SetInt(P + "intro." + id, 1); PlayerPrefs.Save(); }

        public static void RunEnded()
        {
            PlayerPrefs.SetInt(P + "runs", SlotRuns(ActiveSlot) + 1);
            Touch();
        }

        // ---------- 시간의 조각 ----------
        public static int Shards
        {
            get => PlayerPrefs.GetInt(P + "shards", 0);
            set { PlayerPrefs.SetInt(P + "shards", Mathf.Max(0, value)); Touch(); }
        }

        // ---------- 기억 (능력치 노드) ----------
        // 기억의 방 갈래 트리 (2026-11-23 사용자 확정): 화력 · 생존 · 손재주 3갈래 × 4층. 층 n은 장을 n개 넘으면 열림,
        //   위층 노드는 아래층(Req 노드, 없으면 같은 갈래 아래층 아무거나)을 1단계 이상 찍어야. 다 찍으면 판 시작 전력 ~+35% + 기술 노드 4개
        //   (기술 노드 = 생존 보험 · 보상 선택폭, 판의 규칙을 바꾸는 1회 해금)
        public static int Level(int nodeId)
        {
            int lv = PlayerPrefs.GetInt(P + "memory." + nodeId, 0);
            var n = Tables.MemoryNodes.Find(x => x.Id == nodeId);
            return n != null ? Mathf.Min(lv, n.MaxLevel) : lv;            // 예전 저장(최대 5단계)은 새 최대 단계로 자름
        }
        public static int CostOf(MemoryNodeDef n) => n.IsTech ? n.BaseCost : n.BaseCost * (Level(n.Id) + 1);
        public static bool FloorOpen(int floor) => ChaptersCleared >= floor;
        public static bool ReqMet(MemoryNodeDef n)
        {
            if (n.Floor <= 1) return true;
            if (n.Req > 0) return Level(n.Req) >= 1;
            foreach (var m in Tables.MemoryNodes)
                if (m.Branch == n.Branch && m.Floor == n.Floor - 1 && Level(m.Id) >= 1) return true;
            return false;
        }
        public static bool NodeAvailable(MemoryNodeDef n) => FloorOpen(n.Floor) && ReqMet(n);
        /// <summary>기술 노드를 해금했나 (7101 넓어진 선택 · 7102 한 번 더 · 7103 다시 뽑기 · 7104 보스의 선물)</summary>
        public static bool HasTech(int id) => Level(id) >= 1;
        public const int TechWiderChoice = 7101, TechLastStand = 7102, TechReroll = 7103, TechBossGift = 7104;

        public static bool TryBuy(MemoryNodeDef n)
        {
            int lv = Level(n.Id), cost = CostOf(n);
            if (lv >= n.MaxLevel || !NodeAvailable(n) || Shards < cost) return false;
            PlayerPrefs.SetInt(P + "memory." + n.Id, lv + 1);
            Shards -= cost;
            return true;
        }

        public static float Stat(string stat)
        {
            float sum = 0;
            foreach (var n in Tables.MemoryNodes)
                if (!n.IsTech && n.Stat == stat) sum += n.ValuePerLevel * Level(n.Id);
            return sum;
        }

        // ---------- 챕터 ----------
        public const int ChapterCount = 5;
        public static int ChaptersCleared
        {
            get => PlayerPrefs.GetInt(P + "chapters", 0);
            set { PlayerPrefs.SetInt(P + "chapters", Mathf.Clamp(value, 0, ChapterCount)); Touch(); }
        }
        public static void MarkChapterCleared(int chapter) { if (chapter > ChaptersCleared) ChaptersCleared = chapter; }
        public static int SelectedChapter = 1;                       // 마을 → 판 시작 때 넘김

        // ---------- 마을 시설 (레벨 없음, 챕터로 열림) ----------
        public enum Facility { MemoryRoom, GunShop, AmmoWorkshop, Wardrobe }
        public static readonly string[] FacilityName = { "기억의 방", "총포상", "탄약 공방", "의상실" };
        public static readonly string[] FacilityNpc = { "태엽 풀린 뻐꾸기", "빈 외투 보안관", "모래시계 대장장이", "실타래 재봉사" };
        // 2026-11-18 사용자: 장(맵)을 넘을 때마다 마을 섬 · NPC가 하나씩 열림 — 1장 = 기억의 방(노드 성장) · 의상실(2026-11-24 사용자: 2장 → 1장) · 2장 = 탄약 공방 · 3장 = 총포상
        public static readonly int[] FacilityUnlockChapter = { 1, 3, 2, 1 };    // 그 챕터 보스를 넘으면 열림
        public static bool FacilityUnlocked(Facility f) => ChaptersCleared >= FacilityUnlockChapter[(int)f];

        // ---------- 마을 이야기 (본 대화 기록) ----------
        public static bool StorySeen(string key) => PlayerPrefs.GetInt(P + "story." + key, 0) == 1;
        public static void MarkStory(string key) { PlayerPrefs.SetInt(P + "story." + key, 1); Touch(); }
        /// <summary>이 슬롯에서 판을 한 번도 안 했음 → 새로 시작하면 마을을 거치지 않고 바로 1장 전투부터</summary>
        public static bool FirstRunPending => SlotRuns(ActiveSlot) == 0 && ChaptersCleared == 0;

        // ---------- 해금 묶음 (총포상 · 탄약 공방) ----------
        // 2026-11-18 사용자: 지금 있는 총탄 · 총은 기본 해금, 묶음 구매는 앞으로 새로 만들 것만 (Unlock.csv는 비워 둠)
        public static bool GroupOwned(UnlockGroupDef g) => g.Cost <= 0 || PlayerPrefs.GetInt(P + "group." + g.Id, 0) == 1;
        public static bool GroupAvailable(UnlockGroupDef g) => ChaptersCleared >= g.ReqChapter;

        public static bool TryBuyGroup(UnlockGroupDef g)
        {
            if (GroupOwned(g) || !GroupAvailable(g) || Shards < g.Cost) return false;
            PlayerPrefs.SetInt(P + "group." + g.Id, 1);
            Shards -= g.Cost;
            return true;
        }

        /// <summary>아이템이 풀렸는지: 그 아이템이 든 묶음을 하나라도 가졌으면. 어느 묶음에도 없는 아이템은 처음부터 풀림</summary>
        public static bool ItemUnlocked(int id)
        {
            bool listed = false;
            foreach (var g in Tables.UnlockGroups)
            {
                if (!g.Items.Contains(id)) continue;
                listed = true;
                if (GroupOwned(g)) return true;
            }
            return !listed;
        }
        public static bool BulletUnlocked(int id) => id == 1000 || ItemUnlocked(id);
        public static bool GunUnlocked(int id) => id == 2001 || ItemUnlocked(id);

        public static int StartGunRight { get => PlayerPrefs.GetInt(P + "startgun.r", 2001); set { PlayerPrefs.SetInt(P + "startgun.r", value); Touch(); } }
        public static int StartGunLeft { get => PlayerPrefs.GetInt(P + "startgun.l", 2001); set { PlayerPrefs.SetInt(P + "startgun.l", value); Touch(); } }

        // ---------- 업적 카운터 (코스튬 해금 조건) ----------
        public static int Count(string key) => PlayerPrefs.GetInt(P + "count." + key, 0);
        /// <summary>판 도중 자주 불려서 저장(Save)은 판 끝 · 조각 변화 때 같이 됨</summary>
        public static void AddCount(string key, int n = 1) => PlayerPrefs.SetInt(P + "count." + key, Count(key) + n);
        public const string CountExecute = "execute", CountJustDodge = "justdodge", CountTraitTier6 = "tier6", CountLowJudgeKill = "lowjudgekill", CountTier6Boss = "tier6boss";

        // ---------- 의상실: 조건을 채우면 구매 ----------
        public static bool CostumeUnlocked(int id) => id == 4001 || PlayerPrefs.GetInt(P + "costume." + id, 0) == 1;

        /// <summary>코스튬 업적 진행 (지금 값, 목표)</summary>
        public static (int now, int goal) CostumeProgress(CostumeDef c) => c.UnlockType switch
        {
            "ClearZone" or "KillBoss" => (Mathf.Min(ChaptersCleared, c.UnlockValue), c.UnlockValue),
            "ExecuteCount" => (Mathf.Min(Count(CountExecute), c.UnlockValue), c.UnlockValue),
            "JustDodgeCount" => (Mathf.Min(Count(CountJustDodge), c.UnlockValue), c.UnlockValue),
            "TraitTier6" => (Mathf.Min(Count(CountTraitTier6), 1), 1),
            "LowJudgeKill" => (Mathf.Min(Count(CountLowJudgeKill), c.UnlockValue), c.UnlockValue),
            "Tier6BossKill" => (Mathf.Min(Count(CountTier6Boss), c.UnlockValue), c.UnlockValue),
            _ => (1, 1)
        };
        public static bool CostumeCondMet(CostumeDef c) { var (n, g) = CostumeProgress(c); return n >= g; }

        public static string CostumeCondText(CostumeDef c) => c.UnlockType switch
        {
            "ClearZone" => $"{c.UnlockValue}장 첫 클리어",
            "KillBoss" => $"{c.UnlockValue}장 보스 처치",
            "ExecuteCount" => $"누적 처형 {c.UnlockValue}회",
            "JustDodgeCount" => $"누적 극한 회피 {c.UnlockValue}회",
            "TraitTier6" => "특성 6단계 1번 달성",
            "LowJudgeKill" => $"판정 시각 0 이하 탄으로 처치 {c.UnlockValue}회",
            "Tier6BossKill" => $"특성 6단계 2개 이상으로 보스 처치 {c.UnlockValue}회",
            _ => "처음부터"
        };

        public static bool TryUnlockCostume(CostumeDef c)
        {
            if (CostumeUnlocked(c.Id) || !FacilityUnlocked(Facility.Wardrobe) || !CostumeCondMet(c) || Shards < c.Cost) return false;
            PlayerPrefs.SetInt(P + "costume." + c.Id, 1);
            Shards -= c.Cost;
            return true;
        }
        public static int SelectedCostume { get => PlayerPrefs.GetInt(P + "costume.sel", 4001); set { PlayerPrefs.SetInt(P + "costume.sel", value); Touch(); } }

        /// <summary>지금 슬롯만 처음으로</summary>
        public static void ResetAll() => DeleteSlot(ActiveSlot);
    }
}
