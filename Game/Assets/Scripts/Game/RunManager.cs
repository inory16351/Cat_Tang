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

    public class RunManager : MonoBehaviour
    {
        public static RunManager Instance;
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
        public Vector2 RoomSize => current != null ? current.size : new Vector2(24, 20);
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
            frontier = Mathf.Min(Meta.ChapterCount, clearedAtStart + 1);
            chapter = 1;
            Chapter = Tables.Chapter(chapter);
            player.combatEnabled = true;
            player.SetupRun();
            player.health.OnDeath -= OnPlayerDeath;
            player.health.OnDeath += OnPlayerDeath;
            ui?.OnRunStart(player);
            Music.Play("bgm_stage");
            BuildStage(0, RewardKind.Gold);
        }

        // ---------- 스테이지 ----------
        bool HasMidBoss => chapter == 1;                 // 지금은 1장만 중간 보스가 있음

        void BuildMidBoss()
        {
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
            Music.Play("bgm_boss");
            ui?.Toast("중간 보스 — 태엽 현상금 사냥꾼");
        }

        void BuildStage(int index, RewardKind reward) => BuildStage(index, reward, null);

        // 술집(중간 보스) 뒷문 너머에 이어 지은 스테이지 3: 첫 방에 들어가면 지나온 스테이지 2 · 술집을 지움
        RoomBuilder.StageLayout oldLayout;

        /// <param name="attachOrigin">있으면 순간이동 없이 그 자리(술집 뒷문 너머)에 이어 지음</param>
        void BuildStage(int index, RewardKind reward, Vector3? attachOrigin)
        {
            IsMidBossRoom = false;
            if (attachOrigin.HasValue) oldLayout = layout;
            else if (layout != null && layout.root) Destroy(layout.root.gameObject);
            foreach (var e in alive) if (e) Destroy(e.gameObject);
            alive.Clear();
            stageIndex = index;
            pendingReward = reward;
            stageDone = false;
            int rooms = IsBossRoom ? 1 : Random.Range(4, 6);
            // 스테이지 2 마지막 방 뒤에 술집 홀(중간 보스)을 붙여 지음 (2026-11-17 사용자: 건물로 걸어 들어가 이어지게)
            bool saloon = index == 1 && HasMidBoss && !midBossDone;
            layout = RoomBuilder.BuildStage(Chapter, rooms, chapter * 1000 + index * 37 + Random.Range(0, 999), IsBossRoom, null,
                attachOrigin ?? Vector3.zero, attachOrigin.HasValue, saloon);
            var hallRoom = layout.rooms.Find(r => r.midBoss);
            if (hallRoom != null) { if (hallRoom.entranceGate) hallRoom.entranceGate.SetActive(true); if (hallRoom.backGate) hallRoom.backGate.SetActive(true); }
            current = null;
            roomIndex = 0;
            var r0 = layout.rooms[0];
            if (!attachOrigin.HasValue) player.Teleport(r0.center - r0.farSide * (r0.size.y / 2 - 3) + Vector3.up * 0.3f);
            if (IsBossRoom) Music.Play("bgm_boss");
            ui?.Toast(IsBossRoom ? Chapter.BossName : $"{chapter}장 · 스테이지 {index + 1}/{Stages} — 방 {rooms}개");
        }

        void Update()
        {
            if (runOver || layout == null || inCombat || stageDone) return;
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
            current = layout.rooms[i];
            current.entered = true;
            roomIndex = i;
            inCombat = true;
            foreach (var g in current.gates) g.SetActive(true);
            if (current.midBoss)
            {
                IsMidBossRoom = true;                     // 술집 홀: 들어서면 문이 닫히고 중간 보스
                Music.Play("bgm_boss");
                ui?.Toast("중간 보스 — 태엽 현상금 사냥꾼");
            }
            wavesLeft = IsBossRoom || IsMidBossRoom ? 1 : (stageIndex >= 2 ? 3 : Random.value < 0.5f ? 2 : 3);   // 방마다 웨이브 2~3번 (1장 기획서)
            StartCoroutine(SpawnSoon());
        }

        IEnumerator SpawnSoon()
        {
            yield return new WaitForSeconds(0.3f);
            yield return SpawnWave();
        }

        bool spawning;
        public const float WavePreview = 1f;              // 다음 웨이브 예고 (1장 기획서: 1초)

        IEnumerator SpawnWave()
        {
            wavesLeft--;
            var c = current.center;
            if (IsMidBossRoom)
            {
                alive.Add(EnemyFactory.MidBoss(c + current.farSide * 3f, Chapter));
                yield break;
            }
            if (IsBossRoom)
            {
                alive.Add(EnemyFactory.Boss(c + new Vector3(0, 0, 4), Chapter));
                yield break;
            }
            spawning = true;
            // 웨이브당 적 (1장 기획서): 스테이지 1은 4~6마리 → 스테이지 4는 6~9마리. 뒤 장은 +1씩
            int lo = 4 + Mathf.RoundToInt(stageIndex * 2f / 3f), hi = 6 + stageIndex;
            int n = Mathf.Min(Random.Range(lo, hi + 1) + (chapter - 1), 11);
            var spots = new List<Vector3>();
            for (int k = 0; k < n; k++)
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
            foreach (var pos in spots) alive.Add(EnemyFactory.RandomMinion(pos, Chapter, elite: midBossDone && stageIndex >= 2));   // 중간 보스 뒤 하수인은 전부 강화형
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
            if (e && e.kind != Enemy.Kind.Boss) player.inv.gold += Random.Range(2, 5);
            if (!inCombat || alive.Count > 0 || spawning) return;
            if (wavesLeft > 0) { StartCoroutine(SpawnWave()); return; }
            RoomCleared();
        }

        void RoomCleared()
        {
            inCombat = false;
            current.cleared = true;
            foreach (var g in current.gates) g.SetActive(false);
            player.ResetCylinder();
            if (current.midBoss)
            {
                // 술집에서 이김: 뒷문이 열리고 그 앞에 다음 보상 문 → 고르면 뒷문 너머에 스테이지 3이 이어 지어짐
                midBossDone = true;
                stageDone = true;
                ui?.Toast("태엽 현상금 사냥꾼을 쓰러뜨렸다 (+30 골드) — 뒷문이 열렸다");
                player.inv.gold += 30;
                SpawnDoors();
                return;
            }
            if (layout.rooms.Any(r => !r.cleared && !r.midBoss)) { ui?.Toast("다음 방으로"); return; }
            stageDone = true;
            if (IsMidBossRoom)
            {
                midBossDone = true;
                ui?.Toast("태엽 현상금 사냥꾼을 쓰러뜨렸다 (+30 골드)");
                player.inv.gold += 30;
                BuildStage(2, afterMidBoss);
                return;
            }
            if (IsBossRoom) { ui?.ShowRewardPick("최종 보상 — 하나 고르기", BossOptions(), ChapterClear); return; }
            if (pendingReward == RewardKind.Gold || pendingReward == RewardKind.Shard || pendingReward == RewardKind.Armor) { GrantFixed(pendingReward); AfterReward(); return; }
            ui?.ShowRewardPick($"스테이지 보상 — {RoomDoor.NameOf(pendingReward)}", Options(pendingReward), AfterReward);
        }

        /// <summary>골드 · 시간의 조각 방: 고르기 없이 확정 지급. 양은 스테이지 진행도(챕터 · 스테이지)에 비례 (수치 임시)</summary>
        public int FixedAmount(RewardKind k)
        {
            float scale = (1 + 0.5f * stageIndex) * (1 + 0.3f * (chapter - 1));
            return Mathf.RoundToInt((k == RewardKind.Gold ? 40f : 6f) * scale);
        }

        void GrantFixed(RewardKind k)
        {
            if (k == RewardKind.Armor)
            {
                // 방어구 단계 +1: 단계표대로 최대 체력 · 방어력이 오르고 오른 체력만큼 회복
                player.armorLevel++;
                player.health.maxHp = player.MaxHp;
                player.health.Heal(PlayerController.ArmorHpPerLevel);
                FxBurst.Spawn(player.transform.position + Vector3.up, 2.4f, FxBurst.Kind.Gold, new Color(0.7f, 0.85f, 1f));
                ui?.Toast($"방어구 강화 — {player.armorLevel}단계 (체력 +{PlayerController.ArmorHpPerLevel:0} · 방어력 +{PlayerController.ArmorDefPerLevel:0})");
                return;
            }
            int n = FixedAmount(k);
            if (k == RewardKind.Gold) player.inv.gold += n; else shardsThisRun += n;
            FxBurst.Spawn(player.transform.position + Vector3.up, 2.2f, FxBurst.Kind.Gold, k == RewardKind.Gold ? new Color(1f, 0.82f, 0.3f) : new Color(0.6f, 0.85f, 1f));
            ui?.Toast($"스테이지 보상 — {RoomDoor.NameOf(k)} +{n}");
        }

        void AfterReward()
        {
            bool merchant = Random.value < 0.15f || (!merchantSeen && stageIndex == Stages - 2);   // 상인 15% · 챕터당 최소 1번 (1장 기획서)
            if (merchant) SpawnMerchant();
            // 스테이지 2: 문 고르기 대신 술집 문이 열림 (걸어 들어가면 중간 보스)
            var hall = layout?.rooms.Find(r => r.midBoss && !r.cleared);
            if (hall != null)
            {
                if (hall.entranceGate) hall.entranceGate.SetActive(false);
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
            var strong = Tables.Bullets.Values.Where(b => (b.Grade == "Epic" || b.Grade == "Legendary") && Meta.GradeInPool(b.Grade)).ToList();
            if (strong.Count == 0) strong = Tables.Bullets.Values.Where(b => b.Grade == "Rare").ToList();
            var bulletPick = strong[Random.Range(0, strong.Count)];
            list.Add(ItemOption(Item.Of(bulletPick)));
            var guns = Tables.Guns.Values.Where(g => g.Grade != "Common").ToList();
            list.Add(ItemOption(Item.Of(guns[Random.Range(0, guns.Count)])));
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
                ItemKind.Gun => $"{UIKit.GradeKo(it.gun.Grade)} · 공격 ×{it.gun.DamageMul:0.00} · 연사 {it.gun.FireInterval:0.00}초\n<i>{it.gun.Desc}</i>",
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

        void SpawnMerchant()
        {
            merchantSeen = true;
            var r = current;
            var side = Vector3.Cross(Vector3.up, r.farSide).normalized;
            var go = RoomBuilder.MerchantNpc(r.root, side * (r.size.x / 2 - 4f) - r.farSide * 3f);
            var m = go.AddComponent<Merchant>();
            m.prompt = "상인";
            m.radius = 2.6f;
            m.Restock(player);
            ui?.Toast("떠돌이 상인이 있다 (E)");
        }

        void SpawnDoors()
        {
            var kinds = new List<RewardKind>();
            if (stageIndex == Stages - 1) kinds.Add(RewardKind.Boss);
            else
            {
                var pool = new List<(RewardKind k, float w)> { (RewardKind.Gold, 3), (RewardKind.Bullet, 3.2f), (RewardKind.Op, 2.5f), (RewardKind.Shard, 2), (RewardKind.Gun, 1.3f), (RewardKind.Armor, 1.8f) };
                int n = Random.value < 0.5f ? 2 : 3;
                while (kinds.Count < n)
                {
                    float total = pool.Sum(p => p.w), roll = Random.value * total;
                    for (int i = 0; i < pool.Count; i++) { roll -= pool[i].w; if (roll <= 0) { kinds.Add(pool[i].k); pool.RemoveAt(i); break; } }
                }
            }
            var r = current;
            var fwd = r.farSide;
            var side = Vector3.Cross(Vector3.up, fwd).normalized;
            float depth = Mathf.Abs(fwd.z) > 0.5f ? r.size.y / 2 : r.size.x / 2;
            float span = (Mathf.Abs(fwd.z) > 0.5f ? r.size.x : r.size.y) - 8;
            for (int i = 0; i < kinds.Count; i++)
            {
                float t = kinds.Count == 1 ? 0 : -span / 2 + span * i / (kinds.Count - 1);
                var local = fwd * (depth - 1.6f) + side * t;
                var d = RoomBuilder.Door(r.root, local, kinds[i]);
                d.transform.localRotation = Quaternion.LookRotation(fwd);
                d.prompt = kinds[i] == RewardKind.Boss ? "최종 보스에게" : RoomDoor.NameOf(kinds[i]) + " 스테이지로";
            }
        }

        public void EnterDoor(RoomDoor d)
        {
            if (inCombat || runOver) return;
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
                BuildStage(stageIndex + 1, d.reward == RewardKind.Boss ? RewardKind.Gold : d.reward, hall.center + new Vector3(0, 0, RoomBuilder.CellDepth));
                ui?.Toast("뒷문 너머로");
                return;
            }
            BuildStage(stageIndex + 1, d.reward == RewardKind.Boss ? RewardKind.Gold : d.reward);
        }

        // ---------- 끝 ----------
        void ChapterClear()
        {
            Meta.MarkChapterCleared(chapter);
            shardsThisRun += 15 * chapter;
            if (chapter >= frontier) { EndRun(true); return; }
            // 다음 장으로 이어감: 체력 30% 회복 · 골드 · 빌드 그대로
            ui?.CloseAll();
            chapter++;
            Chapter = Tables.Chapter(chapter);
            player.health.Heal(player.health.maxHp * 0.3f);
            ui?.Toast((string.IsNullOrEmpty(bossPick) ? "" : $"{bossPick} 획득 · ") + $"{chapter - 1}장 돌파 — {chapter}장 · {Chapter?.Name}");
            bossPick = null;
            Music.Play("bgm_stage");
            BuildStage(0, RewardKind.Gold);
        }

        void OnPlayerDeath(Health h) => EndRun(false);

        void EndRun(bool win)
        {
            if (runOver) return;
            runOver = true;
            int gain = Mathf.RoundToInt(shardsThisRun * (1 + Meta.Stat("ShardGain") + PlayerController.CostumeStat("MemoryShardGainAdd")));
            shardsThisRun = gain;
            Meta.Shards += gain;
            ui?.ShowResults(win, gain, chapter);
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
            var pool = Tables.Bullets.Values.Where(b => b.Id != 1000 && Meta.GradeInPool(b.Grade)).ToList();
            float total = pool.Sum(b => weights[b.Grade]), r = Random.value * total;
            foreach (var b in pool) { r -= weights[b.Grade]; if (r <= 0) return b; }
            return pool[0];
        }

        public static GunDef RandomGun()
        {
            var weights = new Dictionary<string, float> { { "Common", 55 }, { "Rare", 30 }, { "Epic", 12 }, { "Legendary", 3 } };
            var pool = Tables.Guns.Values.Where(g => g.Id != 2001).ToList();      // 기본 총(효과 없음)은 드랍 안 함
            float total = pool.Sum(g => weights[g.Grade]), r = Random.value * total;
            foreach (var g in pool) { r -= weights[g.Grade]; if (r <= 0) return g; }
            return pool[0];
        }
    }
}
