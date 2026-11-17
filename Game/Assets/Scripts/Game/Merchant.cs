// Interactable.cs에서 분리 (씬에 저장하려면 파일 이름 = 클래스 이름)
using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    /// <summary>방에 확률로 서 있는 상인</summary>
    public class Merchant : Interactable
    {
        public List<Item> stock = new();
        public List<bool> sold = new();
        public int rerolls;

        public override void Interact(PlayerController p) => FindFirstObjectByType<GameUI>()?.OpenShop(this);

        public void Restock(PlayerController p)
        {
            stock.Clear(); sold.Clear();
            for (int i = 0; i < 3; i++) stock.Add(Item.Of(RunManager.RandomBullet()));
            for (int i = 0; i < 2; i++) stock.Add(Item.Of(PlayerController.RandomOp()));
            stock.Add(Item.Of(RunManager.RandomGun()));
            foreach (var _ in stock) sold.Add(false);
        }

        public int RerollCost => 30 + 15 * rerolls;
        // 하트 (2026-11-17 사용자: 상인도 하트 판매). 하나 = 최대 체력 HeartPickup.HealPct, 상인 한 명당 3개
        public const int HeartCost = 25;
        public int heartsLeft = 3;
    }
}
