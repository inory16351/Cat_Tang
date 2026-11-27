// 한 판(챕터): 스테이지 4개(전투 방 4~5개가 복도로 이어짐) + 최종 보스.
// 방에 들어가면 문이 닫히고 웨이브, 다 잡으면 열림. 스테이지의 방을 모두 비우면 보상 여러 개 중 하나를 고르고,
// 문 2~3개(다음 스테이지 보상 갈래 표시) 중 하나로 나아감. 상인은 스테이지 끝 방에 확률로 등장(챕터당 최소 1번).
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CatTang
{
    public class RewardOption
    {
        public string label, desc;
        public Sprite icon;
        public Item item;                               // 탄 · 총 · 연산이면 그 아이템 (마우스 올림 툴팁용)
        public System.Action apply;
        /// <summary>창을 하나 더 거치는 보상(총 → 교체 창): 그 창이 닫힌 뒤에 done을 불러야 다음 단계로 넘어감</summary>
        public System.Action<System.Action> applyThen;
    }

    public partial class RunManager : MonoBehaviour
    {
        public static RunManager Instance;
        /// <summary>스토리 연출 중엔 방에 들어가도 웨이브를 부르지 않음 (StoryRun1)</summary>
        public static bool StoryHold;
        /// <summary>방을 비웠을 때 (스토리 · 튜토리얼이 들음)</summary>
        public event System.Action RoomClearedEvent;
        public const int Stages = 4;

        public int chapter = 1;
        // 한 판 = 한 목숨 (세피리아식): 늘 1장부터 시작해 보스를 넘으면 마을로 안 돌아가고 다음 장으로 이어짐.
        // 이번 판의 끝(frontier) = 지금까지 넘은 장 + 1. 새 장을 넘으면 판이 끝나고 다음 판은 한 장 더 멀리 갈 수 있음
        public int clearedAtStart, frontier;
        public bool AllCleared => Meta.ChaptersCleared >= Meta.ChapterCount;
        public int stageIndex;                        // 0~3 스테이지, 4 보스
        public int roomIndex;                         // 스테이지 안에서 지금 방
        public RewardKind pendingReward = RewardKind.Gold;
        public int shardsThisRun;
        public bool runOver, inCombat;
        public ChapterDef Chapter { get; private set; }
        public bool IsBossRoom => stageIndex >= Stages && !IsMidBossRoom;
        // 중간 보스 방: 스테이지 2와 3 사이 (1장 기획서). 이 방을 비우면 보상 없이 바로 스테이지 3으로
        public bool IsMidBossRoom { get; private set; }
        bool midBossDone;
        RewardKind afterMidBoss;
        public Vector2 RoomSize => current != null ? current.size : RoomBuilder.RoomSize;
        public int RoomCount => layout != null ? layout.rooms.Count : 1;
        public Vector3 RoomCenter => current != null ? current.center : Vector3.zero;
        public Transform StageRoot => layout != null ? layout.root : null;

        RoomBuilder.StageLayout layout;
        RoomBuilder.RoomInfo current;
        public RoomBuilder.StageLayout Layout => layout;           // M 키 지도용
        public RoomBuilder.RoomInfo CurrentRoom => current;
        readonly List<Enemy> alive = new();
        int wavesLeft;
        bool merchantSeen, stageDone;
        GameUI ui;
        PlayerController player;

        void Awake()
        {
            Instance = this;
            Tables.Load();
            gameObject.AddComponent<MapView>();                     // M 키 전체 지도
        }

        void Start()
        {
            player = PlayerController.Instance;
            ui = FindFirstObjectByType<GameUI>();
            clearedAtStart = Meta.ChaptersCleared;
            lastStandLeft = Meta.HasTech(Meta.TechLastStand);   // 기억의 방 기술 노드 (판마다 1번)
            RerollLeft = Meta.HasTech(Meta.TechReroll);
            frontier = Mathf.Min(Meta.ChapterCount, clearedAtStart + 1);
            chapter = 1;
            Chapter = Tables.Chapter(chapter);
            player.combatEnabled = true;
            player.SetupRun();
            player.health.OnDeath -= OnPlayerDeath;
            player.health.OnDeath += OnPlayerDeath;
            ui?.OnRunStart(player);
            Music.PlayFor("stage", chapter);
            BuildStage(0, RewardKind.Gold);
            StoryRun.OnRunStart(this);                   // 판 1 오프닝 · 판 2 안내 (2026-11-23 스토리 기획서)
        }

        // ---------- 스테이지 ----------
        bool HasMidBoss => chapter <= 5;                 // 1장 술집 · 2장 대합실 · 3장 거울 극장 · 4장 진자 홀 · 5장 거울 대전
        string MidBossName => BossIntro.Def(chapter, true)?.nameKo ?? "중간 보스";   // 이름 2026-11-21 사용자 확정

        void BuildMidBoss()
        {
            ClearDoorGuide();
            if (layout != null && layout.root) Destroy(layout.root.gameObject);
            foreach (var e in alive) if (e) Destroy(e.gameObject);
            alive.Clear();
            IsMidBossRoom = true;
            stageDone = false;
            // 술집 앞 큰길 30×14m: 양옆 건물(긴 벽) = 돌진 태클이 부딪히는 벽
            layout = RoomBuilder.BuildStage(Chapter, 1, chapter * 1000 + 555, true, new Vector2(30, 14));
            current = null;
            roomIndex = 0;
            var r0 = layout.rooms[0];
            player.Teleport(r0.center - r0.farSide * (r0.size.y / 2 - 2.5f) + Vector3.up * 0.3f);
            Music.PlayFor("midboss", chapter);
            ui?.Toast("중간 보스 — " + MidBossName);
        }

        void BuildStage(int index, RewardKind reward) => BuildStage(index, reward, null);

        // 술집(중간 보스) 뒷문 너머에 이어 지은 스테이지 3: 첫 방에 들어가면 지나온 스테이지 2 · 술집을 지움
        RoomBuilder.StageLayout oldLayout;

        /// <param name="attachOrigin">있으면 순간이동 없이 그 자리(술집 뒷문 너머)에 이어 지음</param>
        void BuildStage(int index, RewardKind reward, Vector3? attachOrigin)
        {
            IsMidBossRoom = false;
            ClearDoorGuide();
            if (attachOrigin.HasValue) oldLayout = layout;
            else if (layout != null && layout.root) Destroy(layout.root.gameObject);
            foreach (var e in alive) if (e) Destroy(e.gameObject);
            alive.Clear();
            stageIndex = index;
            pendingReward = reward;
            stageDone = false;
            inCombat = false; spawning = false;   // 새 스테이지: 이전 방 전투 상태가 남아 방에 못 들어가는 일 없게
            int rooms = IsBossRoom ? 1 : Random.Range(4, 6);
            // 스테이지 2 마지막 방 뒤에 술집 홀(중간 보스)을 붙여 지음 (2026-11-17 사용자: 건물로 걸어 들어가 이어지게)
            bool saloon = index == 1 && HasMidBoss && !midBossDone;
            layout = RoomBuilder.BuildStage(Chapter, rooms, chapter * 1000 + index * 37 + Random.Range(0, 999), IsBossRoom, null,
                attachOrigin ?? Vector3.zero, attachOrigin.HasValue, saloon);
            var hallRoom = layout.rooms.Find(r => r.midBoss);
            if (hallRoom != null) { WoodGate.Close(hallRoom.entranceGate, false); WoodGate.Close(hallRoom.backGate, false); }
            current = null;
            roomIndex = 0;
            var r0 = layout.rooms[0];
            float back = IsBossRoom && Ch2Kit.Is(Chapter) ? Ch2Kit.BossArenaDiameter / 2 - 3 : IsBossRoom && Ch3Kit.Is(Chapter) ? Ch3Boss.ArenaR - 4 : IsBossRoom && Ch4Kit.Is(Chapter) ? Ch4Kit.ArenaR - 4 : IsBossRoom && Ch5Kit.Is(Chapter) ? Ch5Kit.ArenaR - 4 : r0.size.y / 2 - 3;   // 2장 보스: 원형 경계 안에서 시작
            if (!attachOrigin.HasValue) player.Teleport(r0.center - r0.farSide * back + Vector3.up * 0.3f);
            Music.PlayFor(IsBossRoom ? "boss" : "stage", chapter);   // 중간 보스 뒤 스테이지 3에서 맵 곡으로 돌아옴
            ui?.Toast(IsBossRoom ? Chapter.BossName : $"{chapter}장 · 스테이지 {index + 1}/{Stages} — 방 {rooms}개");
            // 풀링 (2026-11-20): 해당 장에서 쓰는 작은 연출만 프레임마다 최대 네 개씩 미리 만든다.
            if (poolWarmup != null) StopCoroutine(poolWarmup);
            poolWarmup = StartCoroutine(PrewarmStagePools());
        }

        Coroutine poolWarmup;
        static readonly EnemyProjectile.Style[][] warmStyles =
        {
            new[] { EnemyProjectile.Style.Orb, EnemyProjectile.Style.Revolver, EnemyProjectile.Style.Pellet, EnemyProjectile.Style.Sniper, EnemyProjectile.Style.Needle, EnemyProjectile.Style.Numeral },
            new[] { EnemyProjectile.Style.Orb, EnemyProjectile.Style.Ticket, EnemyProjectile.Style.Needle, EnemyProjectile.Style.Numeral },
            new[] { EnemyProjectile.Style.Orb, EnemyProjectile.Style.Pellet, EnemyProjectile.Style.Needle, EnemyProjectile.Style.Ticket },
            new[] { EnemyProjectile.Style.Orb, EnemyProjectile.Style.Sniper, EnemyProjectile.Style.Needle, EnemyProjectile.Style.Numeral },
            new[] { EnemyProjectile.Style.Orb, EnemyProjectile.Style.Needle, EnemyProjectile.Style.Revolver, EnemyProjectile.Style.Sniper, EnemyProjectile.Style.Numeral }
        };

        IEnumerator PrewarmStagePools()
        {
            int ch = chapter;
            yield return null;
            for (int count = 4; count <= 12; count += 4)
            {
                if (chapter != ch || runOver) yield break;
                FxEffect.Prewarm("FX_Hit", count, 4); yield return null;
            }
            for (int count = 2; count <= 6; count += 2)
            {
                if (chapter != ch || runOver) yield break;
                FxEffect.Prewarm("FX_HitCrit", count, 2); yield return null;
            }
            foreach (var style in warmStyles[Mathf.Clamp(ch - 1, 0, warmStyles.Length - 1)])
                for (int count = 4; count <= 16; count += 4)
                {
                    if (chapter != ch || runOver) yield break;
                    EnemyProjectile.Prewarm(style, count, 4); yield return null;
                }
            for (int count = 4; count <= 12; count += 4)
            {
                if (chapter != ch || runOver) yield break;
                DamageNumber.Prewarm(count, 4); yield return null;
                FxBurst.Prewarm(FxBurst.Kind.Spark, count, 4); yield return null;
                FxBurst.Prewarm(FxBurst.Kind.Hit, count, 4); yield return null;
            }
            poolWarmup = null;
        }

        void Update()
        {
            TryLevelUp();                                                   // 쌓인 레벨 업이 있고 다른 창이 없으면 3택 창 (RunManagerXp)
            if (runOver || layout == null || inCombat || stageDone || StoryHold) return;
            for (int i = 0; i < layout.rooms.Count; i++)
            {
                var r = layout.rooms[i];
                if (r.entered || !r.Contains(player.transform.position)) continue;
                EnterRoom(i);
                break;
            }
        }

        void EnterRoom(int i)
        {
            if (oldLayout != null) { if (oldLayout.root) Destroy(oldLayout.root.gameObject); oldLayout = null; }   // 지나온 술집 · 스테이지 2 정리
            ClearDoorGuide();                                             // 다음 방 안내는 그 방에 들어서면 지움
            current = layout.rooms[i];
            current.entered = true;
            roomIndex = i;
            inCombat = true;
            foreach (var g in current.gates) WoodGate.Close(g);          // 위에서 쾅 닫힘
            if (current.midBoss)
            {
                IsMidBossRoom = true;                     // 술집 홀: 들어서면 문이 닫히고 중간 보스
                Music.PlayFor("midboss", chapter);
                ui?.Toast("중간 보스 — " + MidBossName);
            }
            wavesLeft = IsBossRoom || IsMidBossRoom ? 1 : (stageIndex >= 2 ? 3 : Random.value < 0.5f ? 2 : 3);   // 방마다 웨이브 2~3번 (1장 기획서)
            StartCoroutine(SpawnSoon());
        }

        IEnumerator SpawnSoon()
        {
            var room = current;
            yield return new WaitForSeconds(0.3f);
            // 2026-11-23: 기다리는 사이 스테이지가 새로 지어져 다른 방(보스방)에 들어가 있으면 그 방 웨이브를 또 부르지 않음
            //  (보스가 두 번 나와 크로노 · 템포가 두 쌍이 됐음 — 등장 연출 녹화 도구가 방 입장 직후 스테이지를 바꿀 때)
            if (room != current) yield break;
            yield return SpawnWave();
        }

        bool spawning;
        public const float WavePreview = 1f;              // 다음 웨이브 예고 (1장 기획서: 1초)

        IEnumerator SpawnWave()
        {
            // 웨이브가 예약된 사이 스테이지가 새로 지어지면(current = null) 사라진 방을 건드리지 않고 멈춤
            if (current == null || runOver) yield break;
            wavesLeft--;
            var c = current.center;
            if (IsMidBossRoom)
            {
                var mb = EnemyFactory.MidBoss(c + current.farSide * 3f, Chapter);
                alive.Add(mb);
                yield return BossIntro.Play(mb, chapter, true);
                yield break;
            }
            if (IsBossRoom)
            {
                var bb = EnemyFactory.Boss(c + new Vector3(0, 0, 4), Chapter);
                alive.Add(bb);
                yield return BossIntro.Play(bb, chapter, false);
                yield break;
            }
            spawning = true;
            // 웨이브당 적 (1장 기획서): 스테이지 1은 4~6마리 → 스테이지 4는 6~9마리. 뒤 장은 +1씩
            int lo = 4 + Mathf.RoundToInt(stageIndex * 2f / 3f), hi = 6 + stageIndex;
            int n = Mathf.Min(Random.Range(lo, hi + 1) + (chapter - 1), 11);
            var spots = new List<Vector3>();
            // 2장: 굳은 승객 몇 명이 금 가며 깨지고 그 자리에서 나옴 (웨이브의 60%까지, 모자라면 바닥 원)
            var wake = FrozenPassenger.Pick(current, player.transform.position, 4.5f);
            int fromPeople = Mathf.Min(wake.Count, Mathf.CeilToInt(n * 0.6f));
            for (int k = 0; k < fromPeople; k++)
            {
                wake[k].Awaken(WavePreview);
                spots.Add(wake[k].transform.position);
            }
            for (int k = fromPeople; k < n; k++)
            {
                Vector3 pos;
                int tries = 0;
                do
                {
                    pos = c + new Vector3(Random.Range(-current.size.x / 2 + 2.5f, current.size.x / 2 - 2.5f), 0, Random.Range(-current.size.y / 2 + 2.5f, current.size.y / 2 - 2.5f));
                } while ((Effects.Flat(pos - player.transform.position).magnitude < 5f || current.Blocked(pos, 1.2f)) && ++tries < 14);
                spots.Add(pos);
                Telegraph.Ring(pos, 0.7f, WavePreview, Telegraph.Aim);
            }
            yield return new WaitForSeconds(WavePreview);
            spawning = false;
            if (runOver || current == null) yield break;
            foreach (var pos in spots) alive.Add(EnemyFactory.RandomMinion(pos, Chapter, elite: (midBossDone || !HasMidBoss) && stageIndex >= 2));   // 중간 보스 뒤 하수인은 전부 강화형 (중간 보스가 아직 없는 장은 스테이지 3부터)
        }

        public void AddEnemy(Enemy e) { if (e) alive.Add(e); }

        /// <summary>지금 전투 방 안으로 위치를 붙잡음 (inset = 벽에서 띄울 거리, 몸 반지름 + 여유). 방이 없으면 그대로</summary>
        public Vector3 ClampToRoom(Vector3 p, float inset)
        {
            if (current == null) return p;
            float hx = Mathf.Max(0.5f, current.size.x / 2 - inset), hz = Mathf.Max(0.5f, current.size.y / 2 - inset);
            p.x = Mathf.Clamp(p.x, current.center.x - hx, current.center.x + hx);
            p.z = Mathf.Clamp(p.z, current.center.z - hz, current.center.z + hz);
            // 방 모양으로 깎인 곳(막힌 덩어리) 밖으로 밀어냄
            foreach (var b in current.blocks)
            {
                var e = new Rect(b.x - inset, b.y - inset, b.width + inset * 2, b.height + inset * 2);
                if (!e.Contains(new Vector2(p.x, p.z))) continue;
                // 바깥 벽에 붙은 면 쪽으로는 밀지 않음 (방 밖으로 튕겨 나가던 문제)
                float big = 1e6f;
                float dl = e.xMin >= current.center.x - hx ? p.x - e.xMin : big, dr = e.xMax <= current.center.x + hx ? e.xMax - p.x : big;
                float dd = e.yMin >= current.center.z - hz ? p.z - e.yMin : big, du = e.yMax <= current.center.z + hz ? e.yMax - p.z : big;
                float m = Mathf.Min(Mathf.Min(dl, dr), Mathf.Min(dd, du));
                if (m >= big) continue;
                if (m == dl) p.x = e.xMin; else if (m == dr) p.x = e.xMax; else if (m == dd) p.z = e.yMin; else p.z = e.yMax;
            }
            return p;
        }

        public void OnEnemyKilled(Enemy e)
        {
            alive.Remove(e);
            alive.RemoveAll(x => x == null);
            if (e && e.kind != Enemy.Kind.Boss && !StoryHold) player.inv.gold += Random.Range(2, 5);        // 스토리 맵에선 골드 없음 (2026-11-26 사용자)
            if (e && (e.kind != Enemy.Kind.Boss || IsMidBossRoom)) HeartPickup.TryDrop(e, e.kind == Enemy.Kind.Boss);   // 하트: 일반 2% · 강화형 3% · 중간 보스 확정
            if (e && e.kind == Enemy.Kind.Boss && IsMidBossRoom) Ch2Quest.TryDropSand(e.transform.position);          // 2장 클리어 뒤 첫 중간 보스: 흐르는 모래 확정 (Ch2Quest)
            if (e) DropXp(e);                                                                           // 경험치 보석 (RunManagerXp)
            if (!inCombat || alive.Count > 0 || spawning) return;
            if (wavesLeft > 0) { StartCoroutine(SpawnWave()); return; }
            RoomCleared();
        }

        void RoomCleared()
        {
            inCombat = false;
            RoomClearedEvent?.Invoke();
            current.cleared = true;
            foreach (var g in current.gates)
            {
                if (g == current.entryGate) continue;                    // 지나온 술집 · 복도는 이미 지웠으니 들어온 문은 닫힌 채로
                if (current.midBoss && g == current.backGate) continue;  // 뒷문은 보상 문을 골라 스테이지 3을 지은 뒤에 열림 (그 전엔 복도 끝이 허공)
                WoodGate.Break(g);                                       // 덜컹 → 조각 날림
            }
            player.ResetCylinder();
            if (current.midBoss)
            {
                // 술집에서 이김: 뒷문이 열리고 그 앞에 다음 보상 문 → 고르면 뒷문 너머에 스테이지 3이 이어 지어짐
                midBossDone = true;
                stageDone = true;
                ui?.Toast(MidBossName + "을(를) 쓰러뜨렸다 (+30 골드) — 다음 스테이지를 고르면 뒷문이 열림");
                player.inv.gold += 30;
                SpawnDoors();
                return;
            }
            bool lastRoom = !layout.rooms.Any(r => !r.cleared && !r.midBoss);
            if (!IsMidBossRoom && !IsBossRoom) TryMerchant(current, lastRoom);
            if (!lastRoom)
            {
                if (!merchantHere) ui?.Toast("다음 방으로");
                ShowNextRoomGuide();                                     // 다음 방 쪽 열린 문 안내 + 그 길을 막는 소품 부수기
                return;
            }
            stageDone = true;
            if (IsMidBossRoom)
            {
                midBossDone = true;
                ui?.Toast(MidBossName + "을(를) 쓰러뜨렸다 (+30 골드)");
                player.inv.gold += 30;
                BuildStage(2, afterMidBoss);
                return;
            }
            if (IsBossRoom)
            {
                // 기억의 방 '보스의 선물': 고르고 남은 것 중에서 하나 더 (다시 뽑으면 새로 뽑은 것 중에서)
                if (Meta.HasTech(Meta.TechBossGift))
                {
                    List<RewardOption> shown = null; RewardOption picked = null;
                    List<RewardOption> Track(List<RewardOption> l) { shown = l; foreach (var o in l) { var me = o; var a = o.apply; var at = o.applyThen; o.apply = a == null ? null : () => { picked = me; a(); }; o.applyThen = at == null ? null : d => { picked = me; at(d); }; } return l; }
                    ui?.ShowRewardPick("최종 보상 — 하나 고르기", Track(BossOptions()), () =>
                    {
                        var rest = shown?.FindAll(o => o != picked);
                        if (rest == null || rest.Count == 0) { ChapterClear(); return; }
                        ui.ShowRewardPick("보스의 선물 · 추가 선택", rest, ChapterClear);
                    }, () => Track(BossOptions()));
                    return;
                }
                ui?.ShowRewardPick("최종 보상 — 하나 고르기", BossOptions(), ChapterClear, BossOptions);
                return;
            }
            if (pendingReward == RewardKind.Gold || pendingReward == RewardKind.Shard || pendingReward == RewardKind.Armor) { GrantFixed(pendingReward); AfterReward(); return; }
            var k = pendingReward;
            ui?.ShowRewardPick($"스테이지 보상 — {RoomDoor.NameOf(pendingReward)}", Options(pendingReward), AfterReward, () => Options(k));
        }

        /// <summary>골드 · 시간의 조각 방: 고르기 없이 확정 지급. 양은 스테이지 진행도(챕터 · 스테이지)에 비례 (수치 임시)</summary>
        public int FixedAmount(RewardKind k)
        {
            float scale = (1 + 0.5f * stageIndex) * (1 + 0.3f * (chapter - 1));
            return Mathf.RoundToInt((k == RewardKind.Gold ? 40f : 6f) * scale);
        }

        void GrantFixed(RewardKind k)
        {
            if (k == RewardKind.Armor) { ArmorUp(); return; }
            int n = FixedAmount(k);
            if (k == RewardKind.Gold) player.inv.gold += n; else shardsThisRun += n;
            FxBurst.Spawn(player.transform.position + Vector3.up, 2.2f, FxBurst.Kind.Gold, k == RewardKind.Gold ? new Color(1f, 0.82f, 0.3f) : new Color(0.6f, 0.85f, 1f));
            ui?.Toast($"스테이지 보상 — {RoomDoor.NameOf(k)} +{n}");
        }

        /// <summary>방어구 단계 +1: 단계표대로 최대 체력 · 방어력이 오르고 오른 체력만큼 회복 (스테이지 보상 · 레벨 업 보상 공용)</summary>
        void ArmorUp()
        {
            player.armorLevel++;
            player.health.maxHp = player.MaxHp;
            player.health.Heal(PlayerController.ArmorHpPerLevel);
            FxBurst.Spawn(player.transform.position + Vector3.up, 2.4f, FxBurst.Kind.Gold, new Color(0.7f, 0.85f, 1f));
            ui?.Toast($"방어구 강화 — {player.armorLevel}단계 (체력 +{PlayerController.ArmorHpPerLevel:0} · 방어력 +{PlayerController.ArmorDefPerLevel:0})");
        }

        void AfterReward()
        {
            // 스테이지 2: 문 고르기 대신 술집 문이 열림 (걸어 들어가면 중간 보스)
            var hall = layout?.rooms.Find(r => r.midBoss && !r.cleared);
            if (hall != null)
            {
                WoodGate.Break(hall.entranceGate);
                stageDone = false;
                ui?.Toast("술집 문이 열렸다 — 들어가면 중간 보스");
                return;
            }
            SpawnDoors();
        }

        // ---------- 보상 선택지 ----------
        List<RewardOption> Options(RewardKind k)
        {
            var list = new List<RewardOption>();
            switch (k)
            {
                case RewardKind.Bullet:
                    foreach (var b in Distinct(() => RandomBullet(), 3)) { var bb = b; list.Add(ItemOption(Item.Of(bb))); }
                    break;
                case RewardKind.Op:
                    foreach (var o in Distinct(() => PlayerController.RandomOp(), 3)) { var oo = o; list.Add(ItemOption(Item.Of(oo))); }
                    break;
                case RewardKind.Gun:
                    foreach (var gun in Distinct(() => RandomGun(), 3)) { var gg = gun; list.Add(ItemOption(Item.Of(gg))); }
                    break;
            }
            return list;
        }

        List<RewardOption> BossOptions()
        {
            var list = new List<RewardOption>();
            var strong = Tables.Bullets.Values.Where(b => (b.Grade == "Epic" || b.Grade == "Legendary") && Meta.BulletUnlocked(b.Id)).ToList();
            if (strong.Count == 0) strong = Tables.Bullets.Values.Where(b => b.Grade == "Rare" && Meta.BulletUnlocked(b.Id)).ToList();
            if (strong.Count == 0) strong = new List<BulletDef> { RandomBullet() };
            var bulletPick = strong[Random.Range(0, strong.Count)];
            list.Add(ItemOption(Item.Of(bulletPick)));
            var guns = Tables.Guns.Values.Where(g => g.Grade != "Common" && Meta.GunUnlocked(g.Id)).ToList();   // 해금한 총만 (총포상 묶음)
            list.Add(ItemOption(Item.Of(guns.Count > 0 ? guns[Random.Range(0, guns.Count)] : RandomGun())));
            int s = 20 + 10 * chapter;
            list.Add(new RewardOption { label = $"시간의 조각 +{s}", desc = "마을에서 영구 강화에 쓴다", icon = UIKit.Spr("shard"), apply = () => shardsThisRun += s });
            // 고른 보상 이름을 기억해 다음 장 안내 · 결과 화면에 띄움
            foreach (var o in list)
            {
                var opt = o;
                var a = opt.apply; var at = opt.applyThen;
                if (a != null) opt.apply = () => { bossPick = opt.label; a(); };
                if (at != null) opt.applyThen = done => { bossPick = opt.label; at(done); };
            }
            return list;
        }

        public string bossPick;                          // 이번 보스 보상으로 고른 것 (결과 화면 표시)

        RewardOption ItemOption(Item it)
        {
            string desc = it.kind switch
            {
                ItemKind.Bullet => $"{UIKit.GradeKo(it.bullet.Grade)} · 공격 {it.bullet.Damage} · 조건 {UIKit.CondKo(it.bullet)}\n" +
                                   string.Join("\n", new[] { it.bullet.Effect1, it.bullet.Effect2 }.Where(x => x != 0).Select(x => Tables.Effects.TryGetValue(x, out var e) ? e.Desc : "")),
                // 총의 손별 효과는 길어서 카드엔 요약만, 자세한 건 마우스 올림 툴팁으로
                ItemKind.Gun => $"{UIKit.GradeKo(it.gun.Grade)} · 공격 ×{it.gun.DamageMul:0.00} · 사거리 {it.gun.Range:0}m\n<i>{it.gun.Desc}</i>",
                _ => "탄에 끼워 판정 시각을 바꾼다"
            } + "\n<size=15><color=#8A6A4A>마우스를 올리면 자세히</color></size>";
            var icon = it.kind switch { ItemKind.Bullet => UIKit.BulletIcon(it.bullet), ItemKind.Gun => UIKit.GunIcon(it.gun), _ => UIKit.Spr("op_token") };
            var o = new RewardOption { label = it.Name, desc = desc, icon = icon, item = it };
            // 총은 보관함에 안 들어가고 교체 창을 거침 → 그 창에서 손을 고른 뒤에 다음 단계(문 · 다음 장)로
            if (it.kind == ItemKind.Gun) o.applyThen = done => { if (ui) ui.ShowGunPick(it.gun, done); else done?.Invoke(); };
            else o.apply = () => Give(it);
            return o;
        }

        static List<T> Distinct<T>(System.Func<T> make, int n)
        {
            var list = new List<T>();
            for (int tries = 0; list.Count < n && tries < 40; tries++)
            {
                var x = make();
                if (x != null && !list.Contains(x)) list.Add(x);
            }
            return list;
        }

        void Give(Item it)
        {
            if (it.kind == ItemKind.Gun) { ui?.ShowGunPick(it.gun, null); return; }   // 총은 보관함에 안 들어감: 교체 창
            bool ok = player.inv.Add(it);
            ui?.Toast(ok ? $"{it.Name} — 보관함에 넣음 (Tab으로 배치)" : $"보관함이 가득 차 {it.Name}을(를) 골드로 바꿈");
        }

        // ---------- 상인 (상인 공통 규칙, 2026-11-18) ----------
        // 방을 비울 때마다 15%. 스테이지 1 · 2에서 한 번도 못 만났으면 스테이지 2 마지막 방(중간 보스 직전)에 확정.
        bool merchantHere;                               // 이번에 비운 방에 상인이 나왔는지 (토스트 겹침 방지)

        /// <summary>판 1 튜토리얼 중: 경험치 · 상인 없이 전투만 (StoryRun1)</summary>
        public bool StoryCalm => GetComponent<StoryRun1>();

        void TryMerchant(RoomBuilder.RoomInfo r, bool lastRoom)
        {
            merchantHere = false;
            if (r == null || !r.root || StoryCalm) return;
            bool guarantee = !merchantSeen && stageIndex == 1 && lastRoom;
            if (!guarantee && Random.value >= 0.15f) return;
            SpawnMerchant(r);
        }

        void SpawnMerchant(RoomBuilder.RoomInfo r)
        {
            merchantSeen = merchantHere = true;
            Sfx.Play("merchant", 0.8f, 0f);
            var go = RoomBuilder.MerchantNpc(r.root, MerchantSpot(r));
            var m = go.AddComponent<Merchant>();
            m.prompt = "상인";
            m.radius = 2.8f;
            m.Restock(player);
            go.AddComponent<EdgeMarker>().Init("UI/coin");                 // 화면 밖이면 가장자리 화살표
            ui?.Toast((chapter >= 4 ? "오르골이 울리며 상인이 나타났다" : chapter >= 3 ? "거울 너머에 상인이 나타났다" : "상인이 나타났다") + $" ({KeyBinds.Name(Act.Interact)})");
        }

        /// <summary>상인 자리: 방 벽을 따라 3.5m 안쪽 후보 중 화면 안 · 고양이와 4~10m · 막힌 곳 · 소품과 안 겹치는 곳 (없으면 조건을 풀어 가장 가까운 곳). 반환 = 방 기준 위치</summary>
        Vector3 MerchantSpot(RoomBuilder.RoomInfo r)
        {
            var cam = Camera.main;
            var pp = player ? player.transform.position : r.center;
            var nextGates = NextRoomGates(r);
            float hx = r.size.x / 2 - 3.5f, hz = r.size.y / 2 - 3.5f;
            var cands = new List<Vector3>();
            for (float x = -hx; x <= hx + 0.01f; x += 2f) { cands.Add(new Vector3(x, 0, -hz)); cands.Add(new Vector3(x, 0, hz)); }
            for (float z = -hz + 2f; z <= hz - 1.99f; z += 2f) { cands.Add(new Vector3(-hx, 0, z)); cands.Add(new Vector3(hx, 0, z)); }
            Vector3 best = -r.farSide * hz; float bestScore = float.MaxValue;
            foreach (var c in cands)
            {
                var w = r.center + c;
                if (r.Blocked(w, 1.8f)) continue;
                float score = 0;
                if (Physics.CheckSphere(w + Vector3.up * 1.4f, 1.3f, ~0, QueryTriggerInteraction.Ignore)) score += 1000;   // 소품 · 벽과 겹침
                if (cam)
                {
                    var v = cam.WorldToViewportPoint(w + Vector3.up * 1.5f);
                    bool seen = v.z > 0 && v.x > 0.12f && v.x < 0.88f && v.y > 0.32f && v.y < 0.85f;   // 아래 30%는 실린더 HUD가 가림
                    if (!seen) score += 200;
                }
                float d = Vector3.Distance(Effects.Flat(w), Effects.Flat(pp));
                if (d < 4f) score += 300;                                    // 고양이 바로 옆에서 솟지 않게
                score += Mathf.Abs(d - 7f);                                  // 7m 쯤이 가장 좋음
                // 다음 방으로 가는 열린 문 앞 · 문 → 방 가운데 통로는 비움 (문 안내 · 길 트기와 안 겹치게)
                foreach (var g in nextGates)
                {
                    var gp = Effects.Flat(g.transform.position); var fw = Effects.Flat(w);
                    var seg = Effects.Flat(r.center) - gp;
                    float u = seg.sqrMagnitude > 0.01f ? Mathf.Clamp01(Vector3.Dot(fw - gp, seg) / seg.sqrMagnitude) : 0f;
                    if (Vector3.Distance(fw, gp) < DoorPathClear.DoorRadius + 1.5f || Vector3.Distance(fw, gp + seg * u) < DoorPathClear.LaneHalf + 2f) score += 500;
                }
                if (score < bestScore) { bestScore = score; best = c; }
            }
            return best;
        }

        void SpawnDoors()
        {
            var kinds = new List<RewardKind>();
            if (stageIndex == Stages - 1) kinds.Add(RewardKind.Boss);
            else
            {
                var pool = new List<(RewardKind k, float w)> { (RewardKind.Gold, 3), (RewardKind.Bullet, 3.2f), (RewardKind.Op, 2.5f * (1 + PlayerController.CostumeStat("OpDropRateAdd"))), (RewardKind.Shard, 2), (RewardKind.Gun, 1.3f), (RewardKind.Armor, 1.8f) };
                int n = Random.value < 0.5f ? 2 : 3;
                while (kinds.Count < n)
                {
                    float total = pool.Sum(p => p.w), roll = Random.value * total;
                    for (int i = 0; i < pool.Count; i++) { roll -= pool[i].w; if (roll <= 0) { kinds.Add(pool[i].k); pool.RemoveAt(i); break; } }
                }
            }
            var r = current;
            if (r == null || !r.root) return;
            var fwd = r.farSide;
            var side = Vector3.Cross(Vector3.up, fwd).normalized;
            float depth = Mathf.Abs(fwd.z) > 0.5f ? r.size.y / 2 : r.size.x / 2;
            float span = (Mathf.Abs(fwd.z) > 0.5f ? r.size.x : r.size.y) - 8;
            var doors = new List<Transform>();
            for (int i = 0; i < kinds.Count; i++)
            {
                float t = kinds.Count == 1 ? 0 : -span / 2 + span * i / (kinds.Count - 1);
                var local = fwd * (depth - 1.6f) + side * t;
                var d = RoomBuilder.Door(r.root, local, kinds[i]);
                d.transform.localRotation = Quaternion.LookRotation(fwd);
                d.prompt = kinds[i] == RewardKind.Boss ? "최종 보스에게" : RoomDoor.NameOf(kinds[i]) + " 스테이지로";
                doors.Add(d.transform);
            }
            DoorPathClear.Clear(r, doors);                                   // 문 둘레 · 문 → 방 가운데 통로를 막는 소품 · 엄폐물 · 떠다니는 잔해를 부숨
            ShowDoorGuide(doors, kinds);
        }

        // ---------- 문 안내 (2026-11-25 사용자: 전투가 끝나면 화살표로 문 방향 안내) ----------
        // 문마다 작은 화살표: 화면 안이면 문 아이콘 위에 작게, 화면 밖이면 가장자리 화살표 + 이름(여러 문이 다 밖이면 하나로 모아 '보상 문 N개').
        // 문에 들어가거나 새 스테이지 · 중간 보스 방 · 스토리 맵으로 넘어가면 지움 (문이 지워져도 저절로 사라짐)
        const string DoorGuideGroup = "RunDoors";

        void ShowDoorGuide(List<Transform> doors, List<RewardKind> kinds)
        {
            GuideArrow.ClearGroup(DoorGuideGroup);
            int n = doors.Count;
            for (int i = 0; i < n; i++)
            {
                if (!doors[i]) continue;
                string label = kinds[i] == RewardKind.Boss ? "최종 보스" : RoomDoor.NameOf(kinds[i]) + " 문";
                GuideArrow.Marker(doors[i], label, DoorGuideGroup, $"보상 문 {n}개", 5f);   // 문 아이콘(4.1m) 위
            }
        }

        static void ClearDoorGuide() => GuideArrow.ClearGroup(DoorGuideGroup);

        /// <summary>이 방에서 아직 안 깬 다음 방으로 이어지는 나무 문들 (들어온 문 · 이미 깬 방 쪽 · 술집 입구 · 뒷문 제외)</summary>
        List<GameObject> NextRoomGates(RoomBuilder.RoomInfo r)
        {
            var list = new List<GameObject>();
            if (r == null || layout == null) return list;
            foreach (var g in r.gates)
            {
                if (!g || g == r.entryGate || g == r.backGate || g == r.entranceGate) continue;
                var dir = Effects.Flat(g.transform.position - r.center);
                if (dir.sqrMagnitude < 0.01f) continue;
                dir.Normalize();
                // 문 너머 이웃 방: 문 쪽 방향에 있는 가장 가까운 방
                RoomBuilder.RoomInfo next = null; float bestD = float.MaxValue;
                foreach (var o in layout.rooms)
                {
                    if (o == r) continue;
                    var to = Effects.Flat(o.center - r.center);
                    float dist = to.magnitude;
                    if (dist < 0.1f || Vector3.Dot(to / dist, dir) < 0.9f || dist >= bestD) continue;
                    bestD = dist; next = o;
                }
                if (next != null && !next.cleared && !next.midBoss) list.Add(g);
            }
            return list;
        }

        /// <summary>중간 방을 비움: 다음 방으로 이어진 열린 문을 작은 화살표로 가리키고, 그 문 둘레 · 문 → 방 가운데 통로를 막는 소품을 부숨</summary>
        void ShowNextRoomGuide()
        {
            ClearDoorGuide();
            var gates = NextRoomGates(current);
            if (gates.Count == 0 || current == null || !current.root) return;
            var ts = gates.ConvertAll(g => g.transform);
            DoorPathClear.Clear(current, ts);
            if (StoryCalm) return;                                       // 판 1 튜토리얼: 방을 비우면 바로 몰려오는 연출이라 안내 없음
            foreach (var t in ts) GuideArrow.Marker(t, "다음 방", DoorGuideGroup, "다음 방", 3.2f, 3f);
        }

        public void EnterDoor(RoomDoor d)
        {
            if (inCombat || runOver) return;
            ClearDoorGuide();
            Sfx.Play("door", 0.8f);
            ui?.CloseAll();
            // 스테이지 2를 끝내고 문을 고르면 먼저 중간 보스 방 (고른 문의 보상은 스테이지 3으로 넘김)
            if (stageIndex == 1 && !midBossDone && HasMidBoss)
            {
                afterMidBoss = d.reward;
                BuildMidBoss();
                return;
            }
            var hall = layout?.rooms.Find(r => r.midBoss && r.cleared);
            if (hall != null)
            {
                // 술집 뒷문 너머에 다음 스테이지를 이어 지음 (순간이동 없음). 고르지 않은 문들은 치움
                foreach (var other in FindObjectsByType<RoomDoor>(FindObjectsSortMode.None)) Destroy(other.gameObject);
                BuildStage(stageIndex + 1, d.reward == RewardKind.Boss ? RewardKind.Gold : d.reward, hall.center + new Vector3(0, 0, RoomBuilder.BackAttachDistance(Chapter, hall)));
                WoodGate.Break(hall.backGate);                           // 스테이지 3이 지어진 뒤에 뒷문 열림
                ui?.Toast("뒷문이 열렸다 — 뒷문 너머로");
                return;
            }
            // 처음 만나는 1장 보스 문: 꿈 맵 '어린 날 시계탑 광장'을 한 번 거쳐 보스 방으로 (2026-11-25 사용자, StoryMap)
            if (d.reward == RewardKind.Boss && StoryMap.Pending(chapter))
            {
                foreach (var other in FindObjectsByType<RoomDoor>(FindObjectsSortMode.None)) Destroy(other.gameObject);
                StoryMap.Play(this, () => BuildStage(stageIndex + 1, RewardKind.Gold));
                return;
            }
            // 처음 만나는 2장 보스 문: 기억 맵 '배웅하던 승강장'을 한 번 거쳐 로코모스 방으로 (2026-11-25 사용자, StoryPlatform)
            if (d.reward == RewardKind.Boss && StoryPlatform.Pending(chapter))
            {
                foreach (var other in FindObjectsByType<RoomDoor>(FindObjectsSortMode.None)) Destroy(other.gameObject);
                StoryPlatform.Play(this, () => BuildStage(stageIndex + 1, RewardKind.Gold));
                return;
            }
            // 처음 만나는 3장 보스 문: 기억 맵 '거울 같은 호숫가'를 한 번 거쳐 모비 글래스 방으로 (2026-11-25 사용자, StoryLake)
            if (d.reward == RewardKind.Boss && StoryLake.Pending(chapter))
            {
                foreach (var other in FindObjectsByType<RoomDoor>(FindObjectsSortMode.None)) Destroy(other.gameObject);
                StoryLake.Play(this, () => BuildStage(stageIndex + 1, RewardKind.Gold));
                return;
            }
            // 처음 만나는 4장 보스 문: 기억 맵 '시계를 멈춘 공방'을 한 번 거쳐 크로노 & 템포 방으로 (2026-11-25 사용자, StoryWorkshop)
            if (d.reward == RewardKind.Boss && StoryWorkshop.Pending(chapter))
            {
                foreach (var other in FindObjectsByType<RoomDoor>(FindObjectsSortMode.None)) Destroy(other.gameObject);
                StoryWorkshop.Play(this, () => BuildStage(stageIndex + 1, RewardKind.Gold));
                return;
            }
            // 처음 만나는 5장 보스 문: 기억 맵 '11:59의 병실'을 한 번 거쳐 시간의 주인 방으로 (2026-11-26 사용자, StoryWard)
            if (d.reward == RewardKind.Boss && StoryWard.Pending(chapter))
            {
                foreach (var other in FindObjectsByType<RoomDoor>(FindObjectsSortMode.None)) Destroy(other.gameObject);
                StoryWard.Play(this, () => BuildStage(stageIndex + 1, RewardKind.Gold));
                return;
            }
            // 보스 패턴 연습 (환경설정 게임 탭, 장별 체크): 그 장 스토리 맵을 이미 봤으면 판마다 그 장 보스 문 첫 진입 때 한 번,
            //   스토리 맵을 연습 모드로 → 끝나면 바로 보스 방 (2026-11-27, StoryPractice)
            if (d.reward == RewardKind.Boss && (practicedMask & (1 << chapter)) == 0 && StoryPractice.Pending(chapter))
            {
                practicedMask |= 1 << chapter;
                foreach (var other in FindObjectsByType<RoomDoor>(FindObjectsSortMode.None)) Destroy(other.gameObject);
                StoryPractice.Play(this, chapter, () => BuildStage(stageIndex + 1, RewardKind.Gold));
                return;
            }
            BuildStage(stageIndex + 1, d.reward == RewardKind.Boss ? RewardKind.Gold : d.reward);
        }

        /// <summary>스토리 맵에 들어감: 판 방 판정 · 방 가두기를 멈춤 (보스 방을 지으면 다시 평소대로)</summary>
        public void EnterStoryMap()
        {
            ClearDoorGuide();
            StoryHold = true;
            current = null;
            inCombat = false;
        }

        // ---------- 끝 ----------
        void ChapterClear()
        {
            Meta.MarkChapterCleared(chapter);
            shardsThisRun += 15 * chapter;
            // 장을 처음 넘으면: 결과 창 대신 기절 → 꿈 → 마을 (2026-11-23 스토리 기획서)
            if (chapter >= frontier && StoryRun.FaintPending(chapter)) { int gain = EndRunQuiet(); StartCoroutine(StoryRun.FaintAndDream(this, chapter, gain)); return; }
            if (chapter >= frontier) { EndRun(true); return; }
            // 다음 장으로 이어감: 체력 30% 회복 · 골드 · 빌드 그대로
            ui?.CloseAll();
            chapter++;
            Chapter = Tables.Chapter(chapter);
            merchantSeen = false;                        // 상인 보장은 챕터마다
            player.health.Heal(player.health.maxHp * 0.3f);
            ui?.Toast((string.IsNullOrEmpty(bossPick) ? "" : $"{bossPick} 획득 · ") + $"{chapter - 1}장 돌파 — {chapter}장 · {Chapter?.Name}");
            bossPick = null;
            Music.PlayFor("stage", chapter);
            BuildStage(0, RewardKind.Gold);
        }

        void OnPlayerDeath(Health h)
        {
            if (StoryRun.HandleDeath(this)) return;   // 판 1은 결과 창 없이 기절 연출
            // 기억의 방 '한 번 더': 판마다 한 번, 체력 30%로 버팀 + 1.5초 무적
            if (lastStandLeft && !runOver)
            {
                lastStandLeft = false;
                h.hp = Mathf.Max(1f, h.maxHp * 0.3f);
                StartCoroutine(LastStandFx(h));
                return;
            }
            EndRun(false);
        }

        bool lastStandLeft;
        int practicedMask;   // 이번 판에 보스 패턴 연습을 이미 한 장 (비트 = 장 번호)
        /// <summary>기억의 방 '다시 뽑기' (판마다 1번) — GameUI 보상 창이 확인하고 씀</summary>
        public bool RerollLeft;

        IEnumerator LastStandFx(Health h)
        {
            h.invulnerable = true;
            ui?.Toast("<color=#FFD54A>한 번 더 · 부활</color>");
            if (Sfx.Has("e4_bell_2")) Sfx.Play("e4_bell_2", 0.8f, 0f);
            FxBurst.Spawn(h.transform.position + Vector3.up * 0.9f, 2.4f, FxBurst.Kind.Shield, new Color(1f, 0.85f, 0.4f));
            Time.timeScale = 0.35f;
            yield return new WaitForSecondsRealtime(0.5f);
            if (!runOver) Time.timeScale = 1f;
            yield return new WaitForSecondsRealtime(1.0f);
            if (h) h.invulnerable = false;
        }

        /// <summary>이번 판에서 지금까지 얻은 시간의 조각 (기억 · 코스튬 보너스 포함) — 판이 끝날 때 받는 양</summary>
        public int ShardGainPreview => Mathf.RoundToInt(shardsThisRun * (1 + Meta.Stat("ShardGain") + PlayerController.CostumeStat("MemoryShardGainAdd")));

        /// <summary>전투 포기 (ESC 확인 창의 '예', 2026-11-19 사용자): 판 끝 처리 + 얻은 조각만 챙겨 결과 창 없이 마을로</summary>
        public void GiveUp()
        {
            if (runOver) return;
            runOver = true;
            Meta.Shards += ShardGainPreview;
            Meta.RunEnded();
            ReturnToVillage();
        }

        void EndRun(bool win)
        {
            if (runOver) return;
            runOver = true;
            if (!win) StoryRun.PendingDeath = (chapter, IsBossRoom ? 2 : IsMidBossRoom ? 1 : 0);   // 마을에서 뻐꾸기가 쓰러진 곳에 한 줄
            int gain = ShardGainPreview;
            shardsThisRun = gain;
            Meta.Shards += gain;
            Meta.RunEnded();
            ui?.ShowResults(win, gain, chapter);
        }

        /// <summary>결과 창 없이 판을 끝냄 (꿈 연출로 이어질 때): 조각을 챙기고 얻은 양을 돌려줌</summary>
        public int EndRunQuiet()
        {
            if (runOver) return 0;
            runOver = true;
            int gain = ShardGainPreview;
            shardsThisRun = gain;
            Meta.Shards += gain;
            Meta.RunEnded();
            return gain;
        }

        public static void ReturnToVillage()
        {
            Time.timeScale = 1f;
            SceneManager.LoadSceneAsync("Village");
        }

        // ---------- 무작위 (기본 탄 '탄환'은 나오지 않음) ----------
        public static BulletDef RandomBullet()
        {
            var weights = new Dictionary<string, float> { { "Common", 60 }, { "Rare", 28 }, { "Epic", 9 }, { "Legendary", 3 } };
            var pool = Tables.Bullets.Values.Where(b => b.Id != 1000 && Meta.BulletUnlocked(b.Id)).ToList();   // 탄약 공방에서 해금한 묶음만
            float total = pool.Sum(b => weights[b.Grade]), r = Random.value * total;
            foreach (var b in pool) { r -= weights[b.Grade]; if (r <= 0) return b; }
            return pool[0];
        }

        public static GunDef RandomGun()
        {
            var weights = new Dictionary<string, float> { { "Common", 55 }, { "Rare", 30 }, { "Epic", 12 }, { "Legendary", 3 } };
            var pool = Tables.Guns.Values.Where(g => g.Id != 2001 && Meta.GunUnlocked(g.Id)).ToList();      // 기본 총(효과 없음)은 드랍 안 함, 총포상에서 해금한 총만
            if (pool.Count == 0) pool = Tables.Guns.Values.Where(g => g.Id != 2001 && g.Grade == "Common").ToList();   // 아직 하나도 없으면 일반 총
            float total = pool.Sum(g => weights[g.Grade]), r = Random.value * total;
            foreach (var g in pool) { r -= weights[g.Grade]; if (r <= 0) return g; }
            return pool[0];
        }
    }
}
