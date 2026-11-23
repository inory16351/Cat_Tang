// 마을 이야기 (2026-11-18 사용자 흐름): 첫 판(바로 1장 전투, StoryRun1) → 마을에서 깨어나 뻐꾸기와 대화 (대사 2026-11-23 스토리 기획서)
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
            // 판 1에서 쓰러진 뒤 첫 대화 (2026-11-23 스토리 기획서: 반복되는 시간 · 죽음 암시). "화자::대사"
            new("intro", 0, Cuckoo, false,
                "고양이#의문::……어떻게 된 거지. 분명 쓰러졌는데.",
                "태엽 풀린 뻐꾸기#쓸쓸::…뻐꾹. 아니, 뻐… 꾹? 노래가 생각나지 않아.",
                "태엽 풀린 뻐꾸기#다정::또 왔구나. 넌 늘 같은 시간에 돌아와.",
                "태엽 풀린 뻐꾸기#다정::여긴 바늘이 열두 시에 닿지 않는 곳이야. 그러니 몇 번을 쓰러져도 다시 일어나지.",
                "태엽 풀린 뻐꾸기#엄숙::……하지만 언젠가는 종이 울려야 해. 시계는 원래 그런 거니까.",
                // 2026-11-23 사용자 대사: 마을을 벗어나려는 고양이 → 뻐꾸기가 시간의 문으로 보냄 (끝나면 시간의 문에 화살표)
                "고양이#결의::난… 이 마을을 벗어나야 해…",
                "태엽 풀린 뻐꾸기#쓸쓸::…어째서? 같은 짓을 반복하는 거지?",
                "태엽 풀린 뻐꾸기#다정::정 그렇다면 저쪽으로 나가 봐. 하고 나서 후회하는 게 아무것도 하지 않는 것보단 나을 테니…"),
            // 장을 넘으면 기절해 꿈을 꾸고(StoryRun.Dreams) 깨어나 이 대화. 깨어남 혼잣말(StoryRun.WakeLine)이 첫 줄로 붙음
            // 1장 → 꿈1 (2026-11-23 사용자 대사) → 끝나면 기억의 방 문을 비추고 화살표 + 안내 창
            new("ch1", 1, Cuckoo, false,
                "태엽 풀린 뻐꾸기#쓸쓸::저런… 악몽을 꾼 모양이네… 그것보다 널 위해 방을 하나 마련해 두었으니 올라가 봐.",
                "고양이#의문::그것보다 저 시곗바늘은 대체 무슨 의미가 있는 거지?",
                "태엽 풀린 뻐꾸기#의미심장::…때가 되면 다 알게 될 거야."),
            // 2장 → 꿈2 '불 꺼진 시계탑' (노을 진 시계탑을 올려다보는 아이 · 천장 불빛이 지나가는 병원 복도 · 불 꺼진 문자판)
            new("ch2_smith", 2, Smith, true,
                "…모래가 다 새기 전에 왔군.",
                "불 꺼진 시계탑 꿈을 꿨나. 얼굴에 모래처럼 묻어 있어.",
                "고양이#의문::그 탑을 알아?",
                "알지. 종이 멈춘 뒤로는 아무도 올려다보지 않더군. 올려다보던 아이 하나만 빼고.",
                "새는 모래로 탄을 부어 주지. 쓸 만한 탄이 생기면 공방에 걸어 두겠다."),
            new("ch2_tailor", 2, Tailor, false,
                "실이 아직 남았네요. 다행이에요.",
                "하얀 복도 꿈이요? 불빛이 머리 위로 지나가는…… 저도 그 실을 잡아 본 적 있어요. 끝이 아주 하얬죠.",
                "해진 옷은 기워 드릴게요. 해낸 일이 많을수록 고를 옷도 늘어나요."),
            // 3장 → 꿈3 '빗속의 플랫폼' (떠나는 기차 창에 비친 손 · 빗물 흐르는 병실 창 · '곧 올게')
            new("ch3", 3, Sheriff, true,
                "내 얼굴은 필름과 함께 바랬어. 하지만 총은 아직 기억하지.",
                "빗속의 플랫폼이라. '곧 올게'…… 그 말을 지킨 사람은 별로 못 봤네.",
                "고양이#결의::지킬 거다. 누가 기다리고 있든.",
                "총포상을 다시 연다. 시작할 때 쥘 총을 골라 두게."),
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
            bool wake = StoryRun.PendingWake;
            StoryRun.PendingWake = false;
            bool anyBeat = false;
            foreach (var b in Beats) if (Meta.ChaptersCleared >= b.chapter && !Meta.StorySeen(b.key)) anyBeat = true;
            if (wake && !anyBeat)
            {
                p.InputLocked = true;
                bool woke = false;
                ui.Talk("고양이", new[] { StoryRun.WakeLine }, () => woke = true);
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
                yield return RunBeat(b, wake, ui, a => arrowAfter = a);
                wake = false;
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
        static IEnumerator RunBeat(Beat b, bool wake, GameUI ui, System.Action<System.Action> setArrow)
        {
            var spot = GameObject.Find(SpotOf(b.speaker));                       // 말하는 NPC를 비춤
            if (spot) CameraFollow.FocusOn(spot.transform.position + Vector3.back * 1.5f, 0.8f);
            var lines = b.lines;
            if (wake) { lines = new string[b.lines.Length + 1]; lines[0] = StoryRun.WakeLine; b.lines.CopyTo(lines, 1); }
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
            // 1장 꿈 뒤: 여관 2층 기억의 방 해금 → 화살표 + 안내 (2026-11-23 사용자). 그 뒤 장의 튜토리얼 · 해금 연출은 아직 안 만듦(사용자)
            if (b.key == "ch1")
            {
                var door = DoorOf(Meta.Facility.MemoryRoom);
                yield return RevealFacility(ui, Meta.Facility.MemoryRoom);
                if (door) setArrow(() => GuideArrow.Show(door.transform, "기억의 방"));
                var ov = StoryOverlay.I;
                if (ov) yield return ov.Hint("기억의 방", "시간의 조각으로 능력 강화\n더 강해진 기억으로 재도전", door ? ScreenFocus(door.transform.position + Vector3.up * 1.2f) : (Rect?)null);
                CameraFollow.FocusOn(null, 0f);
                yield return new WaitForSecondsRealtime(0.3f);
            }
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
                    yield return RunBeat(b, wake, ui, a => arrow = a);
                    if (p) p.InputLocked = false;
                    arrow?.Invoke();
                }
            Destroy(gameObject);
        }
    }
}
