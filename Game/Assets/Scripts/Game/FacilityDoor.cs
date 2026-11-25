// Interactable.cs에서 분리 (씬에 저장하려면 파일 이름 = 클래스 이름)
using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    /// <summary>마을 시설 입구</summary>
    public class FacilityDoor : Interactable
    {
        public Meta.Facility facility;
        public GameObject lockedVisual, openVisual;
        public override bool Available => true;
        public override void Interact(PlayerController p)
        {
            if (!Meta.FacilityUnlocked(facility))
            {
                Sfx.Play("ui_deny", 0.7f, 0f);
                FindFirstObjectByType<GameUI>()?.Toast($"챕터 {Meta.FacilityUnlockChapter[(int)facility]} 보스를 넘으면 열림");
                return;
            }
            GuideArrow.Reached(transform);
            var ui = FindFirstObjectByType<GameUI>();
            if (!ui) return;
            // 의상실을 처음 쓰면 재봉사 첫 대화 뒤에 열림 (2026-11-24 사용자)
            if (facility == Meta.Facility.Wardrobe && !Meta.StorySeen(VillageStory.KeyTailorMeet))
            {
                ui.StartCoroutine(VillageStory.TailorMeet(ui, () => ui.StartCoroutine(WardrobeQuest(ui))));
                return;
            }
            // 시계바늘 수리 퀘스트 (2026-11-25): 재봉사가 퀘스트를 주고 · 들고 온 물건을 받은 뒤 의상실
            if (facility == Meta.Facility.Wardrobe && ClockQuest.Active && (!ClockQuest.Started || ClockQuest.Carrying))
            {
                ui.StartCoroutine(WardrobeQuest(ui));
                return;
            }
            ui.OpenFacility(facility);
        }

        System.Collections.IEnumerator WardrobeQuest(GameUI ui)
        {
            if (ClockQuest.Active && !ClockQuest.Started) yield return ClockQuest.StartQuest(ui);
            else if (ClockQuest.Active && ClockQuest.Carrying) yield return ClockQuest.Deliver(ui);
            else ui.OpenFacility(facility);
        }

        void Update()
        {
            bool open = Meta.FacilityUnlocked(facility);
            if (lockedVisual && lockedVisual.activeSelf == open) lockedVisual.SetActive(!open);
            if (openVisual && openVisual.activeSelf != open) openVisual.SetActive(open);
        }
    }
}
