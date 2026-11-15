// 마을: 전투 끔, 시설 이름표 띄우기, 마을 음악
using UnityEngine;

namespace CatTang
{
    public class VillageManager : MonoBehaviour
    {
        void Start()
        {
            Tables.Load();
            Time.timeScale = 1f;
            var p = PlayerController.Instance;
            if (p)
            {
                p.combatEnabled = false;
                p.health.ResetHp(p.MaxHp);
                FindFirstObjectByType<GameUI>()?.BindVillage(p);
            }
            foreach (var fd in FindObjectsByType<FacilityDoor>(FindObjectsSortMode.None))
            {
                bool open = Meta.FacilityUnlocked(fd.facility);
                string name = Meta.FacilityName[(int)fd.facility];
                string txt = open ? $"{name}  {Meta.FacilityLevel(fd.facility)}단계" : $"{name}\n<size=40>챕터 {Meta.FacilityUnlockChapter[(int)fd.facility]} 보스를 넘으면</size>";
                Interactable.WorldLabel(fd.transform, txt, new Vector3(0, 6.2f, 0), 0.07f, open ? UIKit.Gold : UIKit.Muted);
            }
            foreach (var g in FindObjectsByType<ChapterGate>(FindObjectsSortMode.None))
                Interactable.WorldLabel(g.transform, $"시간의 문\n<size=40>다음: {Mathf.Min(Meta.ChaptersCleared + 1, Meta.ChapterCount)}장</size>", new Vector3(0, 8.3f, 0), 0.075f, UIKit.Gold);
            Music.Play("bgm_village");
            if (Meta.ChaptersCleared == 0) FindFirstObjectByType<GameUI>()?.Toast("여관 앞 마을 — 남쪽 시간의 문으로 1장에 나서자");
        }
    }
}
