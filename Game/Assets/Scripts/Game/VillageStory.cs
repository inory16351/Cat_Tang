// 마을 이야기 (2026-11-18 사용자 흐름): 첫 판(바로 1장 전투) → 마을에서 깨어나 뻐꾸기와 대화
//   → 1장 클리어: 기억의 방(노드 성장) → 2장 클리어: 탄약 공방 · 의상실 섬 다리가 이어지고 대장장이 · 재봉사 등장 → 3장 클리어: 총포상 섬 · 보안관
// 깨어남 연출(VillageWake)이 끝난 뒤, 아직 안 본 장면을 순서대로. 본 장면은 슬롯에 기록(Meta.StorySeen)
using System.Collections;
using UnityEngine;

namespace CatTang
{
    public class VillageStory : MonoBehaviour
    {
        struct Beat
        {
            public string key; public int chapter; public string speaker; public string[] lines; public bool bridges;
            public Beat(string k, int c, string s, bool b, params string[] l) { key = k; chapter = c; speaker = s; bridges = b; lines = l; }
        }

        const string Cuckoo = "태엽 풀린 뻐꾸기", Smith = "모래시계 대장장이", Tailor = "실타래 재봉사", Sheriff = "빈 외투 보안관";

        static readonly Beat[] Beats =
        {
            new("intro", 0, Cuckoo, false,
                "…뻐꾹. 아니, 뻐… 꾹? 노래가 생각나지 않아.",
                "또 쓰러졌구나, 총잡이. 여긴 시계공이 잊어 가는 꿈의 끝자락, 녹아 사라지다 남은 마을이야.",
                "11시 59분이 끝나지 않는 한, 너는 몇 번이고 여기서 눈을 떠.",
                "남쪽 시간의 문으로 나서면 다시 1장부터. 장을 하나 넘을 때마다 끊긴 다리가 하나씩 이어질 거야."),
            new("ch1", 1, Cuckoo, false,
                "1장을 넘었구나. 여관 2층, 기억의 방 문이 열렸어.",
                "판에서 모은 시간의 조각으로 잊었던 감각을 되찾아. 다음엔 1장과 2장을 잇달아 넘어 봐."),
            new("ch2_smith", 2, Smith, true,
                "…모래가 다 새기 전에 왔군.",
                "새는 모래로 탄을 부어 주지. 쓸 만한 탄이 생기면 공방에 걸어 두겠다."),
            new("ch2_tailor", 2, Tailor, false,
                "실이 아직 남았네요. 다행이에요.",
                "해진 옷은 기워 드릴게요. 해낸 일이 많을수록 고를 옷도 늘어나요."),
            new("ch3", 3, Sheriff, true,
                "내 얼굴은 필름과 함께 바랬어. 하지만 총은 아직 기억하지.",
                "총포상을 다시 연다. 시작할 때 쥘 총을 골라 두게."),
        };

        static string SpotOf(string speaker) => speaker switch
        {
            Cuckoo => "NpcSpot_MemoryRoom", Smith => "NpcSpot_AmmoWorkshop", Tailor => "NpcSpot_Wardrobe", Sheriff => "NpcSpot_GunShop", _ => "",
        };

        public static void Begin() => new GameObject("VillageStory").AddComponent<VillageStory>();

        IEnumerator Start()
        {
            yield return null;
            while (FindFirstObjectByType<VillageWake>()) yield return null;     // 깨어남 연출이 끝날 때까지
            var ui = FindFirstObjectByType<GameUI>();
            var p = PlayerController.Instance;
            if (!ui || !p) { Destroy(gameObject); yield break; }
            foreach (var b in Beats)
            {
                if (Meta.ChaptersCleared < b.chapter || Meta.StorySeen(b.key)) continue;
                p.InputLocked = true;
                if (b.bridges)
                    foreach (var br in FindObjectsByType<IslandBridge>(FindObjectsSortMode.None))
                        if (br.reqChapter == b.chapter && br.Pending) yield return br.Reconnect();
                var spot = GameObject.Find(SpotOf(b.speaker));                   // 말하는 NPC를 비춤
                if (spot) CameraFollow.FocusOn(spot.transform.position + Vector3.back * 1.5f, 0.8f);
                bool done = false;
                ui.Talk(b.speaker, b.lines, () => done = true);
                while (!done) yield return null;
                CameraFollow.FocusOn(null, 0f);
                Meta.MarkStory(b.key);
                yield return new WaitForSecondsRealtime(0.25f);
            }
            // 대화 없이 열린 다리가 남았으면 (예: 장을 한 번에 여러 개 넘음) 마저 이음
            foreach (var br in FindObjectsByType<IslandBridge>(FindObjectsSortMode.None))
                if (br.Pending) { p.InputLocked = true; yield return br.Reconnect(); }
            p.InputLocked = false;
            Destroy(gameObject);
        }
    }
}
