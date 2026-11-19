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
            FindFirstObjectByType<GameUI>()?.OpenFacility(facility);
        }

        void Update()
        {
            bool open = Meta.FacilityUnlocked(facility);
            if (lockedVisual && lockedVisual.activeSelf == open) lockedVisual.SetActive(!open);
            if (openVisual && openVisual.activeSelf != open) openVisual.SetActive(open);
        }
    }
}
