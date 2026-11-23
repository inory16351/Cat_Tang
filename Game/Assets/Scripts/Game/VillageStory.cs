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
                "고양이#씩::그럼 나갈 방법을 찾으면 되겠군.",
                "태엽 풀린 뻐꾸기::남쪽 시간의 문으로 나서면 다시 1장부터. 장을 하나 넘을 때마다 끊긴 다리가 하나씩 이어질 거야."),
            // 장을 넘으면 기절해 꿈을 꾸고(StoryRun.Dreams) 깨어나 이 대화 — 꿈 내용에 반응하되 설명하지 않고 암시만 (2026-11-23 사용자: "NPC 대화도 연결")
            // 1장 → 꿈1 '멈춘 손' (태엽 감는 주름진 손 · 같은 동작의 작은 손 · 째깍과 심장 소리가 엇갈림)
            new("ch1", 1, Cuckoo, false,
                "고양이#두통::……또 꿈이다. 누군가 시계 태엽을 감고 있었어.",
                "태엽 풀린 뻐꾸기#다정::꿈을 꿨구나. 여기선 다들 꿈을 꿔. 잊어버린 걸 꿈이 대신 기억해 주거든.",
                "고양이#의문::손이 둘이었어. 주름진 손이랑, 아주 작은 손. 똑같은 동작으로 태엽을 감더군.",
                "태엽 풀린 뻐꾸기#쓸쓸::……째깍 소리였니, 심장 소리였니. 어느 쪽이 먼저 멈췄어?",
                "고양이#의문::모르겠다. 둘이 자꾸 엇갈려서 들렸어.",
                "태엽 풀린 뻐꾸기#엄숙::그 꿈, 버리지 말고 간직해. 조각이 모이면 그 손이 누구 건지 알게 될 테니까.",
                "태엽 풀린 뻐꾸기#다정::1장을 넘었으니 여관 2층, 기억의 방 문이 열렸어.",
                "태엽 풀린 뻐꾸기::판에서 모은 시간의 조각으로 잊었던 감각을 되찾아. 다음엔 1장과 2장을 잇달아 넘어 봐."),
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

        public static void Begin() => new GameObject("VillageStory").AddComponent<VillageStory>();

        IEnumerator Start()
        {
            yield return null;
            while (FindFirstObjectByType<VillageWake>()) yield return null;     // 깨어남 연출이 끝날 때까지
            var ui = FindFirstObjectByType<GameUI>();
            var p = PlayerController.Instance;
            if (!ui || !p) { Destroy(gameObject); yield break; }
            if (!string.IsNullOrEmpty(StoryRun.PendingToast)) { ui.Toast(StoryRun.PendingToast); StoryRun.PendingToast = null; }   // 꿈을 보고 돌아옴 (결과 창 대신)
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
