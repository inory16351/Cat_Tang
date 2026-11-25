// 시계바늘 수리 퀘스트 (ClockQuest.cs) — 씬에 저장하려면 파일 이름 = 클래스 이름
using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    /// <summary>숨은 물건 (줍기). 퀘스트가 시작되면 켜짐, 주우면 꺼짐 — ClockQuest.Refresh</summary>
    public class MementoPickup : Interactable
    {
        public string id;
        float t0;
        Transform model;

        protected override void OnEnable() { base.OnEnable(); prompt = "줍기"; radius = 2.4f; model = transform.Find("Model"); t0 = Random.value * 6f; }
        public override bool Available => !ClockQuest.Busy && ClockQuest.Active && ClockQuest.Started && !ClockQuest.Got(id);
        public override void Interact(PlayerController p) { if (Available && ClockQuest.I) ClockQuest.I.StartCoroutine(ClockQuest.Pickup(this)); }   // 주우면 이 물건이 꺼지므로 퀘스트 쪽에서 돌림

        void Update()
        {
            if (!model) return;
            float t = Time.time + t0;
            model.localPosition = new Vector3(0, 0.9f + Mathf.Sin(t * 1.6f) * 0.15f, 0);
            model.localRotation = Quaternion.Euler(-35f, t * 40f, 0);
        }
    }
}
