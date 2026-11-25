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
                VillageWake.Play(p);                                 // 연기 속에서 깨어남 (로비에서 · 판이 끝나고 들어올 때마다)
                VillageStory.Begin();                                // 깨어난 뒤 아직 안 본 이야기 (뻐꾸기 첫 대화 · 장마다 열리는 섬)
            }
            RefreshFacilityLabels();
            foreach (var g in FindObjectsByType<ChapterGate>(FindObjectsSortMode.None))
                Interactable.WorldLabel(g.transform, $"시간의 문\n<size=40>다음: {Mathf.Min(Meta.ChaptersCleared + 1, Meta.ChapterCount)}장</size>", new Vector3(0, 8.3f, 0), 0.075f, UIKit.Gold);
            if (!GetComponent<VillageMap>()) gameObject.AddComponent<VillageMap>();   // M 키 마을 지도
            Music.Play("bgm_village");
        }

        /// <summary>시설 문 위 이름표 (퀘스트로 시설이 열리면 다시 부름)</summary>
        public static void RefreshFacilityLabels()
        {
            foreach (var fd in FindObjectsByType<FacilityDoor>(FindObjectsSortMode.None))
            {
                var old = fd.transform.Find("Label");
                if (old) Destroy(old.gameObject);
                bool open = Meta.FacilityUnlocked(fd.facility);
                string name = Meta.FacilityName[(int)fd.facility];
                string sub = open ? Meta.FacilityNpc[(int)fd.facility]
                           : Meta.FacilityQuestLocked(fd.facility) ? Meta.FacilityNpc[(int)fd.facility] + "의 부탁"
                           : $"챕터 {Meta.FacilityUnlockChapter[(int)fd.facility]} 보스를 넘으면";
                Interactable.WorldLabel(fd.transform, $"{name}\n<size=40>{sub}</size>", new Vector3(0, 6.2f, 0), 0.07f, open ? UIKit.Gold : UIKit.Muted);
            }
        }
    }
}
