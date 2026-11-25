// 마을 이야기 (2026-11-18 사용자 흐름): 첫 판(바로 1장 전투, StoryRun1) → 마을에서 깨어나 뻐꾸기와 대화 (대사 2026-11-23 스토리 기획서)
//   → 1장 클리어: 기억의 방 · 의상실(재봉사) → 2장 클리어: 대장장이 섬 다리 → 퀘스트(Ch2Quest)로 탄약 공방 → 보안관 섬 · 결투 → 총포상
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
            // 판 1에서 쓰러진 뒤 첫 대화 (2026-11-23 스토리 기획서: 반복되는 시간 · 죽음 암시). "화자::대사"
            new("intro", 0, Cuckoo, false,
                "고양이#의문::......어떻게 된 거지.",
                "고양이#의문::분명 쓰러졌는데.",
                "태엽 풀린 뻐꾸기#쓸쓸::...뻐꾹. 아니, 뻐... 꾹?",
                "태엽 풀린 뻐꾸기#쓸쓸::노래가 생각나지 않아.",
                "태엽 풀린 뻐꾸기#다정::또 왔구나.",
                "태엽 풀린 뻐꾸기#다정::넌 늘 같은 시간에 돌아와.",
                "태엽 풀린 뻐꾸기#다정::여긴 바늘이 열두 시에 닿지 않는 곳이야.",
                "태엽 풀린 뻐꾸기#다정::그러니 몇 번을 쓰러져도 다시 일어나지.",
                "태엽 풀린 뻐꾸기#엄숙::......하지만 언젠가는 종이 울려야 해.",
                "태엽 풀린 뻐꾸기#엄숙::시계는 원래 그런 거니까.",
                // 2026-11-23 사용자 대사: 마을을 벗어나려는 고양이 → 뻐꾸기가 시간의 문으로 보냄 (끝나면 시간의 문에 화살표)
                "고양이#결의::난... 이 마을을 벗어나야 해...",
                "태엽 풀린 뻐꾸기#쓸쓸::...어째서? 같은 짓을 반복하는 거지?",
                "태엽 풀린 뻐꾸기#다정::정 그렇다면 저쪽으로 나가 봐.",
                "태엽 풀린 뻐꾸기#다정::하고 나서 후회하는 게 아무것도 하지 않는 것보단 나을 테니..."),
            // 장을 넘으면 기절해 꿈을 꾸고(StoryRun.Dreams) 깨어나 이 대화. 깨어남 혼잣말(StoryRun.WakeLines, 장마다 다름)이 첫 줄로 붙음
            // 1장 → 꿈2 '불 꺼진 시계탑' (2026-11-24 사용자: 꿈1은 오프닝으로) · 대사 2026-11-23 사용자 → 끝나면 기억의 방 문을 비추고 화살표 + 안내 창
            // 2026-11-25 사용자: 시곗바늘 수리 퀘스트 도입 — 고양이가 멈춘 바늘을 먼저 발견 → 뻐꾸기가 다른 사람들에게 방법을 물으라 함 (ClockQuest)
            new("ch1", 1, Cuckoo, false,
                "고양이#의문::......바늘이 멈췄군.",
                "태엽 풀린 뻐꾸기#놀람::어디까지 갔었던 거야?",
                "태엽 풀린 뻐꾸기#쓸쓸::한참 찾았다고...",
                "태엽 풀린 뻐꾸기#쓸쓸::네가 쓰러진 뒤로 저렇게 멈춰 버렸어...",
                "태엽 풀린 뻐꾸기#다정::다른 사람들을 만나서 방법을 알아봐.",
                "태엽 풀린 뻐꾸기#다정::그리고 널 위해 방을 하나 마련해 두었으니 올라가 봐."),
            // 2장 클리어 (2026-11-25 사용자 대사): 꿈 없이 블랙아웃 → 깨어남(StoryRun.WakeLines 첫 줄) → 다시 멈춘 바늘 → 뻐꾸기
            //   → 끝나면 대장장이 섬 다리가 이어지고 대장장이에게 화살표 (퀘스트 = Ch2Quest)
            new("ch2", 2, Cuckoo, false,
                "고양이#두통::난 분명... 그 기차역에 있었는데...",
                "태엽 풀린 뻐꾸기#다정::일어났구나...멀리까지 간 모양이던데...",
                "태엽 풀린 뻐꾸기#다정::그나저나 또 새로운 손님들이 찾아왔어.",
                "태엽 풀린 뻐꾸기#다정::시계를 고칠 수 있을지도 몰라.",
                "고양이#의문::(또 고장이 난건가? 무슨일인지 영문을 모르겠군)",
                "태엽 풀린 뻐꾸기#쓸쓸::그렇게 의심스런 눈초리로 쳐다보진마",
                "태엽 풀린 뻐꾸기#엄숙::결국 이 시계가 멈추는 이유는...",
                "태엽 풀린 뻐꾸기#쓸쓸::아니야.. 차차 알게 되겠지",
                "고양이::...."),
            // 3장부터는 마을 퀘스트 없음 (2026-11-25 사용자: 보스 맵 전 스토리 맵만). 총포상 · 보안관은 2장 퀘스트 결투에서 열림
        };

        static string SpotOf(string speaker) => speaker switch
        {
            Cuckoo => "NpcSpot_MemoryRoom", Smith => "NpcSpot_AmmoWorkshop", Tailor => "NpcSpot_Wardrobe", Sheriff => "NpcSpot_GunShop", _ => "",
        };

        static Meta.Facility? FacilityOf(string speaker) => speaker switch
        {
            Cuckoo => Meta.Facility.MemoryRoom, Smith => Meta.Facility.AmmoWorkshop, Tailor => Meta.Facility.Wardrobe, Sheriff => Meta.Facility.GunShop, _ => null,
        };

        static FacilityDoor DoorOf(Meta.Facility f)
        {
            foreach (var d in FindObjectsByType<FacilityDoor>(FindObjectsSortMode.None)) if (d.facility == f) return d;
            return null;
        }

        /// <summary>열린 시설 문을 비추고 "마을에 '…'이(가) 열렸다" (카메라는 문에 둔 채 — 이어지는 안내 창이 문을 가리킴)</summary>
        static IEnumerator RevealFacility(GameUI ui, Meta.Facility f)
        {
            var door = DoorOf(f);
            if (door) CameraFollow.FocusOn(door.transform.position + Vector3.back * 1.5f, 0.7f);
            yield return new WaitForSecondsRealtime(0.8f);
            if (Sfx.Has("ui_buy")) Sfx.Play("ui_buy", 0.8f, 0f);
            ui.Toast($"<color=#FFD54A>{Meta.FacilityName[(int)f]} 해금</color>");
            yield return new WaitForSecondsRealtime(1.2f);
        }

        /// <summary>카메라를 잠깐 그쪽으로 (길 안내 전에 어디인지 보여 줌)</summary>
        static IEnumerator Look(Vector3 at, float hold)
        {
            CameraFollow.FocusOn(at + Vector3.back * 1.5f, 0.7f);
            yield return new WaitForSecondsRealtime(0.7f + hold);
            CameraFollow.FocusOn(null, 0f);
            yield return new WaitForSecondsRealtime(0.3f);
        }

        /// <summary>월드 위치 → 안내 창 비춤 자리 (1920×1080, 왼쪽 위 원점)</summary>
        static Rect? ScreenFocus(Vector3 world)
        {
            var cam = Camera.main;
            if (!cam) return null;
            var sp = cam.WorldToScreenPoint(world);
            if (sp.z < 0) return null;
            float x = sp.x * 1920f / Screen.width, y = (Screen.height - sp.y) * 1080f / Screen.height;
            return new Rect(x - 150, y - 170, 300, 340);
        }

        public static void Begin() => new GameObject("VillageStory").AddComponent<VillageStory>();

        IEnumerator Start()
        {
            yield return null;
            while (FindFirstObjectByType<VillageWake>()) yield return null;     // 깨어남 연출이 끝날 때까지
            var ui = FindFirstObjectByType<GameUI>();
            var p = PlayerController.Instance;
            if (!ui || !p) { Destroy(gameObject); yield break; }
            if (!string.IsNullOrEmpty(StoryRun.PendingToast)) { ui.Toast(StoryRun.PendingToast); StoryRun.PendingToast = null; }   // 꿈을 보고 돌아옴 (결과 창 대신)
            // 꿈에서 깨어난 혼잣말 "젠장… 또 이상한 꿈을 꿨군…" (2026-11-23 사용자) → 이어질 대화가 있으면 그 첫 줄로 붙이고, 없으면 혼잣말만
            string wake = string.IsNullOrEmpty(StoryRun.PendingWake) ? null : StoryRun.PendingWake;
            StoryRun.PendingWake = null;
            bool anyBeat = false;
            foreach (var b in Beats) if (Meta.ChaptersCleared >= b.chapter && !Meta.StorySeen(b.key)) anyBeat = true;
            if (wake != null && !anyBeat)
            {
                p.InputLocked = true;
                bool woke = false;
                ui.Talk("고양이", new[] { wake }, () => woke = true);
                while (!woke) yield return null;
            }
            System.Action arrowAfter = null;                                        // 조작이 풀린 뒤 띄울 길 안내 화살표
            foreach (var b in Beats)
            {
                if (Meta.ChaptersCleared < b.chapter || Meta.StorySeen(b.key)) continue;
                p.InputLocked = true;
                if (b.bridges)
                    foreach (var br in FindObjectsByType<IslandBridge>(FindObjectsSortMode.None))
                        if (br.reqChapter == b.chapter && br.Pending) yield return br.Reconnect();
                yield return RunBeat(b, wake, ui, a => arrowAfter += a);
                wake = null;
                Meta.MarkStory(b.key);
            }
            // 대화 없이 열린 다리가 남았으면 (예: 장을 한 번에 여러 개 넘음) 마저 이음
            foreach (var br in FindObjectsByType<IslandBridge>(FindObjectsSortMode.None))
                if (br.Pending) { p.InputLocked = true; yield return br.Reconnect(); }
            p.InputLocked = false;
            arrowAfter?.Invoke();
            Destroy(gameObject);
        }

        /// <summary>대화 한 묶음 + 그 뒤 연출(길 안내 · 해금 안내). 기록(MarkStory)은 부른 쪽에서</summary>
        static IEnumerator RunBeat(Beat b, string wake, GameUI ui, System.Action<System.Action> setArrow)
        {
            // 1장 꿈 뒤 · 2장 블랙아웃 뒤: 먼저 멈춘 광장 시곗바늘을 비춤 (ClockQuest가 고장 상태로 바꿔 둠)
            if (b.key == "ch1" || b.key == "ch2") { var orb = GameObject.Find("ClockHand_Orbit"); if (orb) yield return Look(orb.transform.position + Vector3.down * 2.5f, 1.2f); }
            var spot = GameObject.Find(SpotOf(b.speaker));                       // 말하는 NPC를 비춤
            if (spot) CameraFollow.FocusOn(spot.transform.position + Vector3.back * 1.5f, 0.8f);
            var lines = b.lines;
            if (wake != null) { lines = new string[b.lines.Length + 1]; lines[0] = wake; b.lines.CopyTo(lines, 1); }
            bool done = false;
            ui.Talk(b.speaker, lines, () => done = true);
            while (!done) yield return null;
            CameraFollow.FocusOn(null, 0f);
            yield return new WaitForSecondsRealtime(0.25f);
            // 첫 대화 뒤: 시간의 문으로 유도 (2026-11-23 사용자: "화면에 화살표로 전투 지역 문 표시하여 유도")
            if (b.key == "intro")
            {
                var gate = FindFirstObjectByType<ChapterGate>();
                if (gate) { yield return Look(gate.transform.position, 1.1f); setArrow(() => GuideArrow.Show(gate.transform, "시간의 문")); }
            }
            // 1장 꿈 뒤 (2026-11-24 사용자): 기억의 방 해금 → 뻐꾸기 "손님이…" → 의상실 섬 다리 · 의상실 해금 → 화살표 둘.
            //   시설 설명(튜토리얼)은 둘 중 먼저 들어간 곳에서 (GameUI.FacilityTutorial)
            if (b.key == "ch1")
            {
                var mem = DoorOf(Meta.Facility.MemoryRoom);
                yield return RevealFacility(ui, Meta.Facility.MemoryRoom);
                var cuckoo = GameObject.Find(SpotOf(Cuckoo));
                if (cuckoo) CameraFollow.FocusOn(cuckoo.transform.position + Vector3.back * 1.5f, 0.8f);
                bool said = false;
                ui.Talk(Cuckoo, new[] { "태엽 풀린 뻐꾸기#의미심장::아 그러고보니 손님이 온 모양이던데.....", "태엽 풀린 뻐꾸기#의미심장::한번 찾아가봐." }, () => said = true);
                while (!said) yield return null;
                CameraFollow.FocusOn(null, 0f);
                foreach (var br in FindObjectsByType<IslandBridge>(FindObjectsSortMode.None))
                    if (br.reqChapter == 1 && br.Pending) yield return br.Reconnect();
                var ward = DoorOf(Meta.Facility.Wardrobe);
                yield return RevealFacility(ui, Meta.Facility.Wardrobe);
                setArrow(() =>
                {
                    if (mem) GuideArrow.Show(mem.transform, "기억의 방", keep: true);
                    if (ward) GuideArrow.Show(ward.transform, "의상실", keep: true);
                });
                CameraFollow.FocusOn(null, 0f);
                yield return new WaitForSecondsRealtime(0.3f);
            }
            // 2장 블랙아웃 뒤: "새로운 손님" = 대장장이 섬 다리가 이어짐 → 대장장이에게 화살표
            if (b.key == "ch2")
            {
                foreach (var br in FindObjectsByType<IslandBridge>(FindObjectsSortMode.None))
                    if (br.reqChapter == 2 && br.Pending) yield return br.Reconnect();
                CameraFollow.FocusOn(null, 0f);
                var smith = DoorOf(Meta.Facility.AmmoWorkshop);
                if (smith) { yield return Look(smith.transform.position, 0.9f); setArrow(() => GuideArrow.Show(smith.transform, "대장장이", keep: true)); }
            }
        }

        /// <summary>의상실 문을 처음 쓸 때 재봉사 첫 대화 (2026-11-24 사용자 대사) → 끝나면 의상실 열림</summary>
        public const string KeyTailorMeet = "tailor_meet";
        public static IEnumerator TailorMeet(GameUI ui, System.Action then)
        {
            var p = PlayerController.Instance;
            if (p) p.InputLocked = true;
            var spot = GameObject.Find(SpotOf(Tailor));
            if (spot) CameraFollow.FocusOn(spot.transform.position + Vector3.back * 1.5f, 0.8f);
            bool done = false;
            ui.Talk(Tailor, new[]
            {
                "아 어서와.",
                "이번이 몇번째 만남인지는 모르겠지만....",
                "고양이#의문::....난 그런 기억이 없는데",
                "아 그런 모양이군.",
                "하지만 그런건 이 곳에선 중요하지 않지.",
                "아무튼 골라봐.",
                "무법자에게도 패션은 언제나 중요한 법이지.",
            }, () => done = true);
            while (!done) yield return null;
            Meta.MarkStory(KeyTailorMeet);
            CameraFollow.FocusOn(null, 0f);
            if (p) p.InputLocked = false;
            then?.Invoke();
        }

        /// <summary>개발용: 대화 묶음을 기록 없이 재생 (저장 안 건드림). 플레이 중 마을에서 VillageStory.Preview("intro" | "ch1")</summary>
        public static void Preview(string key, bool wake = false)
        {
            var go = new GameObject("VillageStoryPreview");
            go.AddComponent<VillageStory>().enabled = false;   // Start(실제 흐름)는 안 돌림
            var host = go.GetComponent<VillageStory>();
            host.StartCoroutine(host.PreviewRun(key, wake));
        }

        IEnumerator PreviewRun(string key, bool wake)
        {
            var ui = FindFirstObjectByType<GameUI>(); var p = PlayerController.Instance;
            foreach (var b in Beats)
                if (b.key == key)
                {
                    if (p) p.InputLocked = true;
                    System.Action arrow = null;
                    yield return RunBeat(b, wake ? StoryRun.WakeLines[0] : null, ui, a => arrow += a);
                    if (p) p.InputLocked = false;
                    arrow?.Invoke();
                }
            Destroy(gameObject);
        }
    }
}
