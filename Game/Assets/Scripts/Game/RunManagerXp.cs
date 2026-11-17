// 경험치 · 레벨 업 (2026-11-17 사용자: 세피리아처럼 경험치를 모으면 3개 선택지 중 하나)
// - 적을 처치하면 경험치 보석(XpOrb)이 튀어나와 고양이에게 빨려 옴 → 닿으면 경험치
//   일반 2 · 강화형 3 · 중간 보스 20 (최종 보스는 장이 끝나므로 없음)
// - 필요 경험치 = 12 + 6 × (레벨 - 1). 넘친 경험치는 다음 레벨로 이어짐, 한꺼번에 여러 레벨이 오르면 차례로 고름
// - 레벨 업 창(게임 멈춤): 체력 회복 / 골드 / 시간의 조각 / 총탄 / 총 / 방어구 강화 중 서로 다른 3개
//   다른 창(스테이지 보상 · 상점 등)이 열려 있으면 닫힌 뒤에 뜸
// 경험치 · 레벨은 한 판 동안 유지 (장을 넘어도 이어짐), 새 판이면 1부터
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CatTang
{
    public partial class RunManager
    {
        public int level = 1, xp;
        int pendingLevels;
        public int XpNeed => 12 + 6 * (level - 1);
        public float XpProgress => Mathf.Clamp01(xp / (float)Mathf.Max(1, XpNeed));
        public const float HealRatio = 0.3f;

        /// <summary>처치한 적에서 경험치 보석을 튀어나오게 함</summary>
        void DropXp(Enemy e)
        {
            int value = e.kind == Enemy.Kind.Boss ? (IsMidBossRoom ? 20 : 0) : e.elite ? 3 : 2;
            if (value <= 0) return;
            int orbs = Mathf.Min(value, 10);
            for (int i = 0; i < orbs; i++)
                XpOrb.Spawn(e.transform.position + Vector3.up * 0.8f, value / orbs + (i < value % orbs ? 1 : 0));
        }

        /// <summary>경험치 더하기 (보석이 고양이에게 닿을 때)</summary>
        public void AddXp(int n)
        {
            if (runOver || n <= 0) return;
            xp += n;
            while (xp >= XpNeed)
            {
                xp -= XpNeed;
                level++;
                pendingLevels++;
                if (player)
                {
                    GunkataFx.Flare(player.transform.position + Vector3.up * 1.2f, 2.2f, new Color(0.45f, 0.95f, 1f));
                    FxBurst.Spawn(player.transform.position + Vector3.up, 2.4f, FxBurst.Kind.Gold, new Color(0.5f, 0.95f, 1f));
                }
            }
        }

        bool levelUpOpen;

        /// <summary>쌓인 레벨 업이 있고 다른 창이 없으면 3택 창을 띄움 (Update에서 매 프레임)</summary>
        void TryLevelUp()
        {
            if (pendingLevels <= 0 || levelUpOpen || runOver || !ui || !player || !player.health.Alive || ui.ScreenOpen) return;
            levelUpOpen = true;
            int shownLevel = level - pendingLevels + 1;
            ui.ShowRewardPick($"레벨 업!  Lv {shownLevel} — 하나 고르기", LevelUpOptions(), () =>
            {
                pendingLevels--;
                levelUpOpen = false;
            });
        }

        enum LvReward { Heal, Gold, Shard, Bullet, Gun, Armor }

        List<RewardOption> LevelUpOptions()
        {
            bool hurt = player.health.hp < player.health.maxHp * 0.95f;
            var pool = new List<(LvReward k, float w)>
            {
                (LvReward.Heal, hurt ? 2.2f : 0.4f), (LvReward.Gold, 2f), (LvReward.Shard, 1.5f),
                (LvReward.Bullet, 2.4f), (LvReward.Gun, 1.1f), (LvReward.Armor, 1.4f),
            };
            var picks = new List<LvReward>();
            while (picks.Count < 3 && pool.Count > 0)
            {
                float total = pool.Sum(x => x.w), r = Random.value * total;
                int idx = 0;
                for (; idx < pool.Count - 1; idx++) { r -= pool[idx].w; if (r <= 0) break; }
                picks.Add(pool[idx].k);
                pool.RemoveAt(idx);
            }
            var list = new List<RewardOption>();
            foreach (var k in picks)
            {
                switch (k)
                {
                    case LvReward.Heal:
                    {
                        int amt = Mathf.RoundToInt(player.health.maxHp * HealRatio);
                        list.Add(new RewardOption
                        {
                            label = "체력 회복", desc = $"체력 {amt} 회복\n(최대 체력의 {HealRatio * 100:0}%)", icon = UIKit.Spr("heal_icon") ?? UIKit.Spr("close"),
                            apply = () =>
                            {
                                player.health.Heal(amt);
                                FxBurst.Spawn(player.transform.position + Vector3.up, 2f, FxBurst.Kind.Gold, new Color(1f, 0.35f, 0.4f));
                            },
                        });
                        break;
                    }
                    case LvReward.Gold:
                    {
                        int n = Mathf.Max(10, Mathf.RoundToInt(FixedAmount(RewardKind.Gold) * 0.5f));
                        list.Add(new RewardOption { label = $"골드 +{n}", desc = "상인에게서 총탄 · 총 · 하트를 산다", icon = UIKit.Spr("coin"), apply = () => player.inv.gold += n });
                        break;
                    }
                    case LvReward.Shard:
                    {
                        int n = Mathf.Max(3, Mathf.RoundToInt(FixedAmount(RewardKind.Shard) * 0.6f));
                        list.Add(new RewardOption { label = $"시간의 조각 +{n}", desc = "마을에서 영구 강화에 쓴다", icon = UIKit.Spr("shard"), apply = () => shardsThisRun += n });
                        break;
                    }
                    case LvReward.Bullet:
                        list.Add(ItemOption(Item.Of(RandomBullet())));
                        break;
                    case LvReward.Gun:
                        list.Add(ItemOption(Item.Of(RandomGun())));
                        break;
                    case LvReward.Armor:
                        list.Add(new RewardOption
                        {
                            label = "방어구 강화", desc = $"{player.armorLevel + 1}단계: 최대 체력 +{PlayerController.ArmorHpPerLevel:0} / 방어력 +{PlayerController.ArmorDefPerLevel:0}",
                            icon = UIKit.Spr("armor"), apply = ArmorUp,
                        });
                        break;
                }
            }
            return list;
        }
    }
}
