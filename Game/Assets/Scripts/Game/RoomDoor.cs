// Interactable.cs에서 분리 (씬에 저장하려면 파일 이름 = 클래스 이름)
using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    /// <summary>방을 비우면 열리는 문. 문 위 아이콘 = 그 방을 비우면 얻는 보상</summary>
    public class RoomDoor : Interactable
    {
        public RewardKind reward;

        public static string IconOf(RewardKind k) => k switch
        {
            RewardKind.Gold => "UI/coin", RewardKind.Shard => "UI/shard", RewardKind.Op => "UI/op_token", RewardKind.Armor => "UI/armor",
            RewardKind.Gun => "Icons/guns/gun_2001", RewardKind.Bullet => "Icons/bullets/bullet_1013", _ => "UI/close"
        };

        public static string NameOf(RewardKind k) => k switch
        {
            RewardKind.Gold => "골드", RewardKind.Gun => "총", RewardKind.Bullet => "총탄", RewardKind.Op => "연산",
            RewardKind.Shard => "시간의 조각", RewardKind.Armor => "방어구 강화", _ => "최종 보스"
        };

        public override void Interact(PlayerController p) => RunManager.Instance.EnterDoor(this);
    }
}
