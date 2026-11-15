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
        public System.Action apply;
    }

    public class RunManager : MonoBehaviour
    {
        public static RunManager Instance;
        public const int Stages = 4;

        public int chapter = 1;
        public int stageIndex;                        // 0~3 스테이지, 4 보스
        public int roomIndex;                         // 스테이지 안에서 지금 방
        public RewardKind pendingReward = RewardKind.Gold;
        public int shardsThisRun;
        public bool runOver, inCombat;
        public ChapterDef Chapter { get; private set; }
        public bool IsBossRoom => stageIndex >= Stages;
        public int RoomCount => layout != null ? layout.rooms.Count : 1;
        public Vector3 RoomCenter => current != null ? current.center : Vector3.zero;
        public Transform StageRoot => layout != null ? layout.root : null;

        RoomBuilder.StageLayout layout;
        RoomBuilder.RoomInfo current;
        readonly List<Enemy> alive = new();
        int wavesLeft;
        bool merchantSeen, stageDone;
        GameUI ui;
        PlayerController player;

        void Awake()
        {
            Instance = this;
            Tables.Load();
        }

        void Start()
        {
            player = PlayerController.Instance;
            ui = FindFirstObjectByType<GameUI>();
            chapter = Mathf.Clamp(Meta.SelectedChapter, 1, Meta.ChapterCount);
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
        void BuildStage(int index, RewardKind reward)
        {
            if (layout != null && layout.root) Destroy(layout.root.gameObject);
            foreach (var e in alive) if (e) Destroy(e.gameObject);
            alive.Clear();
            stageIndex = index;
            pendingReward = reward;
            stageDone = false;
            int rooms = IsBossRoom ? 1 : Random.Range(4, 6);
            layout = RoomBuilder.BuildStage(Chapter, rooms, chapter * 1000 + index * 37 + Random.Range(0, 999), IsBossRoom);
            current = null;
            roomIndex = 0;
            var r0 = layout.rooms[0];
            player.Teleport(r0.center - r0.farSide * (r0.size.y / 2 - 3) + Vector3.up * 0.3f);
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
            current = layout.rooms[i];
            current.entered = true;
            roomIndex = i;
            inCombat = true;
            foreach (var g in current.gates) g.SetActive(true);
            wavesLeft = IsBossRoom ? 1 : (stageIndex >= 2 ? 2 : Random.value < 0.5f ? 1 : 2);
            StartCoroutine(SpawnSoon());
        }

        IEnumerator SpawnSoon()
        {
            yield return new WaitForSeconds(0.6f);
            SpawnWave();
        }

        void SpawnWave()
        {
            wavesLeft--;
            var c = current.center;
            if (IsBossRoom)
            {
                alive.Add(EnemyFactory.Boss(c + new Vector3(0, 0, 4), Chapter));
                return;
            }
            int n = Mathf.Min(3 + stageIndex + (chapter - 1), 9);
            for (int k = 0; k < n; k++)
            {
                Vector3 pos;
                int tries = 0;
                do
                {
                    pos = c + new Vector3(Random.Range(-current.size.x / 2 + 2.5f, current.size.x / 2 - 2.5f), 0, Random.Range(-current.size.y / 2 + 2.5f, current.size.y / 2 - 2.5f));
                } while (Effects.Flat(pos - player.transform.position).magnitude < 5f && ++tries < 10);
                bool shooter = Random.value < Chapter.ShooterRatio;
                alive.Add(shooter ? EnemyFactory.Shooter(pos, Chapter) : EnemyFactory.Chaser(pos, Chapter));
            }
        }

        public void AddEnemy(Enemy e) { if (e) alive.Add(e); }

        public void OnEnemyKilled(Enemy e)
        {
            alive.Remove(e);
            alive.RemoveAll(x => x == null);
            if (e && e.kind != Enemy.Kind.Boss) player.inv.gold += Random.Range(2, 5);
            if (!inCombat || alive.Count > 0) return;
            if (wavesLeft > 0) { SpawnWave(); return; }
            RoomCleared();
        }

        void RoomCleared()
        {
            inCombat = false;
            current.cleared = true;
            foreach (var g in current.gates) g.SetActive(false);
            player.ResetCylinder();
            if (layout.rooms.Any(r => !r.cleared)) { ui?.Toast("다음 방으로"); return; }
            stageDone = true;
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
            bool merchant = Random.value < 0.35f || (!merchantSeen && stageIndex == Stages - 2);
            if (merchant) SpawnMerchant();
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
            return list;
        }

        RewardOption ItemOption(Item it)
        {
            string desc = it.kind switch
            {
                ItemKind.Bullet => $"{UIKit.GradeKo(it.bullet.Grade)} · 공격 {it.bullet.Damage} · 조건 {UIKit.CondKo(it.bullet)}\n" +
                                   string.Join("\n", new[] { it.bullet.Effect1, it.bullet.Effect2 }.Where(x => x != 0).Select(x => Tables.Effects.TryGetValue(x, out var e) ? e.Desc : "")),
                ItemKind.Gun => $"{UIKit.GradeKo(it.gun.Grade)} · 공격 ×{it.gun.DamageMul:0.00} · 연사 {it.gun.FireInterval:0.00}초\n" + UIKit.GunHandText(it.gun),
                _ => "탄에 끼워 판정 시각을 바꾼다"
            };
            var icon = it.kind switch { ItemKind.Bullet => UIKit.BulletIcon(it.bullet), ItemKind.Gun => UIKit.GunIcon(it.gun), _ => UIKit.Spr("op_token") };
            return new RewardOption { label = it.Name, desc = desc, icon = icon, apply = () => Give(it) };
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
            BuildStage(stageIndex + 1, d.reward == RewardKind.Boss ? RewardKind.Gold : d.reward);
        }

        // ---------- 끝 ----------
        void ChapterClear()
        {
            Meta.MarkChapterCleared(chapter);
            shardsThisRun += 15 * chapter;
            EndRun(true);
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
