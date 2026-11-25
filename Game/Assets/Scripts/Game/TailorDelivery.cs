// 시계바늘 수리 퀘스트 (ClockQuest.cs) — 씬에 저장하려면 파일 이름 = 클래스 이름
using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    /// <summary>재봉사 앞: 들고 있는 물건이 있으면 "건네기"</summary>
    public class TailorDelivery : Interactable
    {
        protected override void OnEnable() { base.OnEnable(); prompt = "재봉사에게 건네기"; radius = 3.2f; }
        public override bool Available => !ClockQuest.Busy && ClockQuest.Active && ClockQuest.Carrying;
        public override void Interact(PlayerController p)
        {
            var ui = FindFirstObjectByType<GameUI>();
            if (ui && Available) ui.StartCoroutine(ClockQuest.Deliver(ui));
        }
    }
}
