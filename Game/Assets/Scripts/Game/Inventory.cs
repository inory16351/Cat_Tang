// 판 안의 소지품: 실린더 12칸(Revolver) · 양손 총 · 보관함 12칸 · 골드
// 보관함 칸에는 탄(연산 포함) · 연산 · 총 중 하나가 들어감. 교체는 전투가 없을 때만.
using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    public enum ItemKind { Bullet, Op, Gun }

    public class Item
    {
        public ItemKind kind;
        public BulletDef bullet;
        public List<OpDef> ops = new();          // 탄에 끼운 연산 (위 슬롯부터)
        public OpDef op;
        public GunDef gun;

        public static Item Of(BulletDef b) => new Item { kind = ItemKind.Bullet, bullet = b };
        public static Item Of(OpDef o) => new Item { kind = ItemKind.Op, op = o };
        public static Item Of(GunDef g) => new Item { kind = ItemKind.Gun, gun = g };

        public string Name => kind switch { ItemKind.Bullet => bullet.Name, ItemKind.Op => "연산 " + op.Name, _ => gun.Name };
        public string Grade => kind switch { ItemKind.Bullet => bullet.Grade, ItemKind.Gun => gun.Grade, _ => "Common" };

        public int SellPrice => kind switch
        {
            ItemKind.Bullet => Shop.PriceOf(this) / 2,
            ItemKind.Op => Shop.PriceOf(this) / 2,
            _ => Shop.PriceOf(this) / 2
        };
    }

    public class Inventory
    {
        public const int StorageSize = 12;
        public readonly Item[] storage = new Item[StorageSize];
        public int gold;

        public bool HasSpace => FirstEmpty() >= 0;
        public int FirstEmpty() { for (int i = 0; i < StorageSize; i++) if (storage[i] == null) return i; return -1; }

        /// <summary>보관함에 넣기. 가득 차면 골드로 바꿈 (false)</summary>
        public bool Add(Item it)
        {
            int i = FirstEmpty();
            if (i < 0) { gold += it.SellPrice; return false; }
            storage[i] = it;
            return true;
        }
    }

    public static class Shop
    {
        public static int PriceOf(Item it)
        {
            switch (it.kind)
            {
                case ItemKind.Bullet: return it.bullet.Grade switch { "Common" => 40, "Rare" => 70, "Epic" => 110, _ => 170 };
                case ItemKind.Op: return it.op.OpType == "Mul" || it.op.OpType == "Div" ? 60 : 35 + 10 * it.op.Operand;
                default: return it.gun.Grade switch { "Common" => 90, "Rare" => 140, "Epic" => 200, _ => 280 };
            }
        }
    }
}
