// 스토리 흐름 (2026-11-23 스토리 기획서 Ver01, docs/1159_스토리_기획서_20261123_정승운_Ver01.pptx)
// - 판 1: 오프닝(꿈1 수채화) → 황야 거리에서 깨어남 "윽… 꿈인가…" → 몬스터 소환 "지금은 잠자리 같은 걸…" → 이동 · 사격 · 대시만 → 적이 몰려와 결국 쓰러짐 → 마을(뻐꾸기)
// - 판 2: 총알 시계(처음 쏠 때, HUD) · 시각 조건(첫 총탄을 얻으면 Tab 유도 → 인벤토리에서) · 불릿 파티(열기가 다 찼을 때, 고양이 독백)
// - 시너지: 처음 켜지면 Tab 유도 → 인벤토리 능력치 탭에서 (2026-11-23 사용자: "시각 조건 같은 건 탭 누르게 유도한 다음 아이템 창 UI에서 설명")
// - 장을 처음 넘으면: 클리어 → 기절 → 꿈(수채화 셀 애니메이션) → 마을에서 깨어남 (결과 창 대신)
// 본 장면은 슬롯에 기록 (Meta.StorySeen): opening(= 꿈1) · tut1 · tut2 · dream1~5 (장 n 클리어 = 꿈 n+1, StoryRun.DreamOf)
using System.Collections;
using UnityEngine;

namespace CatTang
{
    public static class StoryRun
    {
        public const string KeyOpening = "opening", KeyTut1 = "tut1", KeyTut2 = "tut2", KeySynergy = "tut_synergy";
        // 시너지 설명 (2026-11-23 사용자 컨펌)
        public const string SynergyText = "같은 특성의 탄을 함께 장전하면 시너지가 생겨요.\n2 · 4 · 6발을 모을 때마다 효과가 한 단계씩 강해져요.\n지금 켜진 시너지는 이곳에서 확인할 수 있어요.";
        public static bool Run1 => Meta.FirstRunPending && !Meta.StorySeen(KeyTut1);          // 새 슬롯의 첫 판만 (이미 판을 돈 예전 저장은 건너뜀)
        public static bool Run2 => Meta.StorySeen(KeyTut1) && !Meta.StorySeen(KeyTut2);
        // 꿈 배치 (2026-11-24 사용자): 꿈1 '멈춘 손' = 오프닝(현실 몽타주 대신) · 1장 클리어 = 꿈2 '불 꺼진 시계탑' · 2장 클리어 = 꿈3 (꿈3 이후 · 3장부터는 임시 — 스토리 맵으로 바뀔 예정)
        public static int DreamOf(int chapter) => chapter + 1;
        public static bool DreamPending(int chapter) { int n = DreamOf(chapter); return chapter >= 1 && n <= Dreams.Length && !Meta.StorySeen("dream" + n); }
        /// <summary>마을에서 깨어난 뒤 띄울 한 줄 (꿈을 보고 돌아오면 결과 창이 없으니 얻은 조각을 여기로)</summary>
        public static string PendingToast;
        /// <summary>꿈을 보고 마을에서 깨어남 → 그 장의 혼잣말(WakeLines) 뒤 NPC 대화 · 해금으로 이어짐 (VillageStory). 빈 문자열 = 깨어남 없음</summary>
        public static string PendingWake;
        // 장마다 다른 기절 · 깨어남 대사 (2026-11-24 사용자: 1장은 지금 대사, 2~5장은 사용자가 만들 때 적어 줌 → 그때까지 비워 둠 = 대사 없이 연출만)
        public static readonly string[] FaintLines = { "……또, 이 소리……", "", "", "", "" };
        public static readonly string[] WakeLines = { "고양이#두통::젠장… 또 이상한 꿈을 꿨군…", "고양이#두통::……또…꿈인가……", "", "", "" };   // 2장 (2026-11-24 사용자): 기절 대사 없음
        static string Of(string[] a, int chapter) => chapter >= 1 && chapter <= a.Length ? a[chapter - 1] : "";

        public static void OnRunStart(RunManager rm)
        {
            if (Run1) rm.gameObject.AddComponent<StoryRun1>();
            else if (Run2 || (Meta.StorySeen(KeyTut1) && !Meta.StorySeen(KeySynergy))) rm.gameObject.AddComponent<StoryHints>();   // 시너지는 켜질 때까지 판마다 기다림
        }

        /// <summary>판 1에선 쓰러져도 결과 창 없이 기절 연출 → 마을</summary>
        public static bool HandleDeath(RunManager rm)
        {
            var r1 = rm.GetComponent<StoryRun1>();
            if (!r1) return false;
            r1.Collapse();
            return true;
        }

        // 꿈 목록 (기획서 '꿈 목록'): 그림 = Resources/Story/Dreams/dream{n}/*.png, 없으면 조각 설명 자리 표시
        public static readonly (string title, string[] fragments)[] Dreams =
        {
            ("멈춘 손", new[] { "태엽을 감는 주름진 손", "같은 동작을 하는 작은 아이 손", "째깍 소리와 심장 소리가 엇갈림" }),
            ("불 꺼진 시계탑", new[] { "노을 진 시골 시계탑을 올려다보는 아이", "천장 조명이 빠르게 지나가는 병원 복도", "불 꺼진 시계탑의 문자판" }),
            ("빗속의 플랫폼", new[] { "떠나는 기차 창에 비친 손", "빗물이 흐르는 병실 창", "'곧 올게'라는 먹먹한 목소리" }),
            ("호숫가의 목소리", new[] { "빛에 가려진 여인의 사진", "엎질러진 물이 거꾸로 컵으로 되돌아감", "똑, 똑 떨어지는 링거 방울" }),
            ("11:59", new[] { "벽시계 두 바늘이 12시로 다가감", "흩어진 조각들이 한 병실로 모임", "노인의 손을 꼭 쥔 손" }),
        };

        public static IEnumerator FaintAndDream(RunManager rm, int chapter, int gain)
        {
            var p = PlayerController.Instance;
            var ov = StoryOverlay.I;
            if (p) { p.InputLocked = true; p.health.invulnerable = true; }
            // 클리어: 슬로우 → 두통 → 쓰러짐
            Time.timeScale = 0.3f;
            Music.Duck(0.2f, 0.8f);
            yield return new WaitForSecondsRealtime(0.8f);
            ov.SetCaption(Of(FaintLines, chapter));
            yield return ov.Headache(0.85f, 1.6f);
            if (Sfx.Has("story_flatline")) Sfx.Play("story_flatline", 0.8f, 0f);
            CameraFollow.FocusOn(p ? p.transform.position : (Vector3?)null, 0.6f);
            yield return ov.FadeWhite(0, 1, 1.4f);
            ov.SetCaption("");
            Time.timeScale = 1f;
            Music.Duck(0f, 0.5f);
            int n = DreamOf(chapter);
            var d = Dreams[n - 1];
            yield return ov.PlayDream(n, d.title, d.fragments, authored: DreamCuts.For(n));
            Meta.MarkStory("dream" + n);
            PendingToast = $"시간의 조각 +{gain}";
            PendingWake = Of(WakeLines, chapter);
            CameraFollow.FocusOn(null, 0f);
            ov.RevealAfterLoad(0.8f, 1.2f);
            RunManager.ReturnToVillage();
        }
    }

    /// <summary>판 1: 오프닝 몽타주 → 깨어남 → 첫 전투 → 몰려오는 적 → 쓰러짐</summary>
    public class StoryRun1 : MonoBehaviour
    {
        RunManager rm;
        PlayerController p;
        bool collapsing, swarmStarted;
        string controls = "WASD  이동     좌클릭  사격     Space  대시";

        // 현실 몽타주 (2026-11-23 사용자: "시간선 더 무작위로 섞고 한 컷에 이미지 여러개를 조각 내 잘라붙여서 … 컷 전환이나 카메라 무빙도 훨씬 더 긴박하게 … 유리컵 … 떨어트리는거",
        //   "한컷에 여러 장면 디졸브 … 예지몽 · 회상 연출 참고", "긴박한 줌 달리 · 빠른 컷 전환 · 정신없는 카메라 무빙 · 역동적인 앵글",
        //   "오브젝트 변화가 이미지 상에 일어나고 뎁스가 배경이랑 맞아야", "책상에서 떨어지는 물컵 → 떨어지는 장면 확대 → 깨지는 장면, 컨티뉴이티", "컷을 많이 넣어서 완성도 높게, 이미지 많이 써도")
        // 그림 = Resources/Story/Opening/*.png (크롬 ChatGPT, 하데스풍)
        // 구성: 현재(병실 · 들것) ↔ 과거(시계탑 · 아이 · 기차) ↔ 미래(11:59)가 뒤섞임. 사이사이 노인의 눈 초근접 0.3초 = 이 모든 걸 보는 사람
        //   과거 = 채도 빠짐 + 가장자리 흐림 · 겹쳐 찍기 / 예지 = 0.2초 끼어들기 + 번쩍 / 긴박 = 크래시 줌 · 달리 줌(현기증) · 핸드헬드 · 더치 앵글
        //   물컵(동작 이어 붙이기): 손이 뻗어 컵을 건드림 → 컵이 기울어 떨어짐(가까이) → 바닥으로 떨어지는 컵(바닥 높이) → 바닥에서 깨짐 → 거꾸로 감겨 모두 되돌아감
        // 같은 평면(책상 위 컵 · 그림자 · 손)은 배경과 같은 depth · 같은 흐림 · 같은 밤빛 색으로 (그림에 붙어 있어 보이게)
        static MLayer L(string img, float depth = 1f, float b0 = 0, float b1 = -1) => new MLayer { img = img, depth = depth, blur0 = b0, blur1 = b1 < 0 ? b0 : b1 };
        static MShot M(float dur, string desc, MTrans tin, float tinDur, params MLayer[] layers) => new MShot { dur = dur, desc = desc, tin = tin, tinDur = tinDur, layers = layers };
        /// <summary>과거(회상) 표식: 채도 빠짐 + 가장자리 흐림 + 바랜 색</summary>
        static MLayer Past(MLayer l, float sat = 0.55f, float edge = 0.022f) { l.sat = sat; l.edge = edge; l.tint = new Color(1f, 0.93f, 0.82f); return l; }
        /// <summary>겹쳐 찍기: a~b 동안 반투명하게 떠올랐다 가라앉는 다른 시간의 장면</summary>
        static MLayer Ghost(MLayer l, float peak, float a, float b) { l.fade = new[] { new Vector2(0, 0), new Vector2(a, peak), new Vector2(b, peak), new Vector2(1, 0) }; return l; }
        static MLayer FadeOut(MLayer l, float at) { l.fade = new[] { new Vector2(0, 1), new Vector2(at, 0) }; return l; }
        static MLayer Edge(MLayer l, float edge) { l.edge = edge; return l; }
        static (float, string, float, float) Q(float u, string sfx, float shake = 0, float flash = 0) => (u, sfx, shake, flash);

        // 11:59 벽시계: 시계판(바늘 없음) + 바늘 세 개 따로 (축 = 시계판 가운데). 초침이 째깍거리다 멈칫
        static MLayer Hand(string img, float height, float pivotY, float r0, float r1, float tick = 0) =>
            new MLayer { img = img, height = height, pivot = new Vector2(0.5f, pivotY), at = new Vector2(0, 56), r0 = r0, r1 = r1, tick = tick };
        static MLayer[] Clock(float sec0, float sec1, float tick) => new[]
        {
            L("clock_face", 1f, 0.006f, 0f),
            Hand("hand_hour", 350f, 0.172f, 0.5f, 0.5f),
            Hand("hand_minute", 480f, 0.15f, 6f, 6f),
            Hand("hand_second", 540f, 0.178f, sec0, sec1, tick),
        };

        // ---- 물컵 장면 (bedside_bg · floor_bg 기준 자리) ----
        static readonly Vector2 GlassAt = new(129, -230);              // bedside_hand 상판 위, 떨리는 손끝 바로 오른쪽 아래 (컵 밑동)
        static readonly Color Night = new(0.52f, 0.63f, 0.82f, 0.9f);  // 밤 병실 빛: 어둡고 푸르게 + 살짝 비침 (배경 그림의 컵처럼 보이게)
        const float TableD = 0.6f, TableBlur = 0.003f;
        // 손이 그려진 협탁 판 (손을 따로 붙이지 않고 배경 그림 안에서 뻗음). 없으면 빈 협탁
        static MLayer TableBg() => Edge(L("bedside_hand", TableD, TableBlur), 0.024f);   // 손이 그려진 협탁 판 하나로 모든 협탁 컷을 이음 (구도 연속)
        static MLayer TableHand() => new MLayer();                                          // (예전: 손 따로 붙이기 → 배경과 안 어울려 판에 그려 넣음)
        static MLayer GlassOnTable(int motion = 0, Vector2 p1 = default, float r1 = 0) =>
            new MLayer { img = "glass", height = 330, pivot = new Vector2(0.5f, 0.03f), at = GlassAt, depth = TableD, blur0 = TableBlur, blur1 = TableBlur, tint = Night, motion = motion, p1 = p1, r1 = r1 };
        static MLayer Shadow(Vector2[] fade = null) =>
            new MLayer { img = "shadow_soft", height = 95, at = GlassAt + new Vector2(8, 2), depth = TableD, alpha = 0.8f, fade = fade };
        static readonly Vector2 FloorHit = new(300, -200);            // 떨어지는 컵 밑동이 닿는 자리 = floor_impact(①)의 컵 밑동 (컷이 이어지게)

        // ---- 시계탑 (child_run_bg 오른쪽, 원본 그림의 탑 자리) ----
        const float RunD = 0.9f, TowerH = 1080f;
        static readonly Vector2 TowerAt = new(690, -420);              // 탑 밑동
        static readonly Vector2 TowerPan = new(-900, -229);            // 줌 1.8에서 탑 쪽으로 (판 가장자리가 화면 안으로 들어오지 않는 한계 ≈ -980)
        static MLayer Tower(float r0 = 0, float r1 = 0) =>
            Past(new MLayer { img = "tower_obj", height = TowerH, pivot = new Vector2(0.5f, 0f), at = TowerAt, depth = RunD, r0 = r0, r1 = r1, blur0 = 0.003f, blur1 = 0.003f });
        static MShot Knock(float dur, bool rev) => Pan(new MShot
        {
            dur = dur, desc = "건드려 기울어 떨어지는 컵 (가까이)", tin = MTrans.Cut, ease = 2, reverse = rev, hand = 5,
            layers = new[] { TableBg(), TableHand(), Shadow(new[] { new Vector2(0, 0.8f), new Vector2(0.35f, 0.8f), new Vector2(0.55f, 0) }),
                             GlassOnTable(3, new Vector2(230, -1500), -100) },
            cues = rev ? null : new[] { Q(0.05f, "e4_tick_1", 4) },
        }, new Vector2(-498, 167), new Vector2(-498, 240), 1.9f, 1.9f, 4, 9);
        static MShot FallClose(float dur, bool rev) => Pan(new MShot
        {
            dur = dur, desc = "바닥으로 떨어지는 컵 (바닥 높이)", tin = rev ? MTrans.Mix : MTrans.Cut, tinDur = 0.22f, ease = 2, reverse = rev, hand = 9,   // 거꾸로: 깨진 조각 그림이 조각 섞기로 다시 컵이 됨
            layers = new[] { Edge(L("floor_bg", 0.5f, 0.012f), 0.03f), Edge(L("floor_empty", 0.5f, 0.012f), 0.03f),   // floor_empty = 깨진 장면과 같은 구도의 빈 바닥
                             new MLayer { img = "glass", height = 440, pivot = new Vector2(0.5f, 0.03f), at = FloorHit + new Vector2(-140, 1150), p1 = new Vector2(140, -1150), r0 = -110, r1 = -31, motion = 1, depth = 0.9f, tint = Night, blur0 = 0.006f, blur1 = 0.002f } },
        }, Vector2.zero, Vector2.zero, 1.0f, 1.0f, -12, -9);   // 끝 프레임 = 단계 ① 첫 프레임과 같은 카메라 (줌 1 · 회전 -9)
        // 깨짐은 단계별로 따로 그린 그림(같은 바닥 · 같은 각도): ① 닿는 순간 금 → ② 터지며 물 튐 → ③ 흩어진 조각 (이미지를 찢지 않음)
        //   셀 애니처럼 단계 그림을 뚝뚝 넘김. 카메라 줌은 단계를 가로질러 이어짐 (z0 → z1이 다음 단계 z0)
        static MShot Stage(string img, string desc, float dur, bool rev, float z0, float z1, float hand, (float, string, float, float)[] cues = null) => Pan(new MShot
        {
            dur = dur, desc = desc, tin = MTrans.Cut, ease = 2, reverse = rev, hand = hand,
            layers = new[] { Edge(L(img, 0.5f, 0.002f), 0.026f) }, cues = cues,
        }, Vector2.zero, Vector2.zero, z0, z1, -9, -8.5f);
        static MShot[] Break(bool rev) => rev
            ? new[]
            {
                Stage("floor_shatter", "흩어진 조각 (거꾸로)", 0.26f, true, 1.06f, 1.18f, 5, new[] { Q(0f, "story_rewind", 8) }),
                Stage("floor_burst", "터짐 (거꾸로)", 0.09f, true, 1.03f, 1.06f, 6),
                Stage("floor_impact", "닿는 순간 (거꾸로)", 0.09f, true, 1.0f, 1.03f, 6),
            }
            : new[]
            {
                Stage("floor_impact", "바닥에 닿는 순간 · 금", 0.12f, false, 1.0f, 1.03f, 2, new[] { Q(0f, "e_glass", 26, 0.1f) }),
                Stage("floor_burst", "터지며 물이 튐", 0.16f, false, 1.03f, 1.06f, 3, new[] { Q(0f, "heart", 12) }),
                Stage("floor_shatter", "흩어진 조각", 0.9f, false, 1.06f, 1.18f, 2),
            };

        // 시계탑 깨짐 단계 (과거 표식 · 카메라 줌은 단계를 가로질러 이어짐)
        static MShot TowerStage(string img, string desc, float dur, float z0, float z1, float hand, (float, string, float, float)[] cues) => Pan(new MShot
        {
            dur = dur, desc = desc, tin = MTrans.Cut, ease = 2, hand = hand,
            layers = new[] { Past(L(img, 1f, 0.002f)) }, cues = cues,
        }, Vector2.zero, Vector2.zero, z0, z1, 7, 9);

        // 째깍 반복 컷 (레퀴엠식): 0.16초 · 더치 앵글 · 째깍 소리
        static MShot Tk(string desc, float rot, float z, string sfx, params MLayer[] ls) => Pan(new MShot { dur = 0.16f, desc = desc, tin = MTrans.Cut, layers = ls, hand = 6 }, Vector2.zero, Vector2.zero, z, z + 0.06f, rot, rot, 2, sfx);
        // 노인의 눈 초근접 (사이사이 끼어듦)
        // 눈동자 속 시계: 시계 비친 눈(eye_ecu)에서 시계 반사만 동그랗게 오려(eye_clock_iris) 시계 없는 눈(eye_plain)의 눈동자 자리에 맞춰 얹음
        //   (두 그림의 눈 크기 · 자리가 달라 통째로 겹치면 눈이 두 개로 보임 → 반사만 맞춤). 자리 = 눈동자 가운데 (842, 367)/1450×815 → 화면 (174, 60), 지름 237
        static MLayer EyeClock(bool emerge) => new MLayer
        {
            img = "eye_clock_iris", height = 237, at = new Vector2(174, 60), blur0 = 0.002f, blur1 = 0.002f,
            fade = emerge ? new[] { new Vector2(0, 0), new Vector2(0.2f, 0), new Vector2(0.85f, 1) } : null,   // 두 번째 눈: 눈동자에 시계가 서서히 떠오름
        };
        static MShot Eye(float dur, float rot, string sfx, int clock = 2) => Pan(new MShot { dur = dur, desc = "노인의 눈 (초근접)", tin = MTrans.Cut, ease = 1, hand = 4,
            layers = clock == 0 ? new[] { Edge(L("eye_plain", 1f, 0.002f), 0.02f) }
                   : new[] { Edge(L("eye_plain", 1f, 0.002f), 0.02f), EyeClock(clock == 1) },
            cues = new[] { Q(0f, sfx, 8, 0.08f) } }, Vector2.zero, Vector2.zero, 1.0f, 1.35f, rot, rot);

        static MShot[] Montage => new[]
        {
            new MShot { dur = 1.2f, sfx = "story_heartbeat", tint = Color.black },
            // ── 태엽 감는 손 (현재) + 같은 동작의 아이 손 (과거, 겹쳐 찍기)
            Pan(new MShot { dur = 1.6f, desc = "태엽을 감는 주름진 손 + 겹친 아이 손", tin = MTrans.Dissolve, tinDur = 0.7f, hand = 3,
                layers = new[] { L("bench_bg", 0.5f, 0.012f, 0.016f), L("hands_old_fg", 1.25f, 0.004f, 0f), Ghost(Past(L("hands_child", 1.05f, 0.006f)), 0.45f, 0.3f, 0.6f) },
                cues = new[] { Q(0.2f, "e4_tick_1", 2), Q(0.6f, "e4_tick_2", 2) } }, Vector2.zero, new Vector2(-40, 12), 1.0f, 1.16f),
            // 회중시계 초근접 크래시 줌
            Pan(new MShot { dur = 0.45f, desc = "회중시계 초근접", tin = MTrans.Cut, ease = 1, hand = 4,
                layers = new[] { L("bench_bg", 0.5f, 0.024f), new MLayer { img = "watch", height = 820, at = new Vector2(0, -30), depth = 1f, tint = new Color(1f, 0.95f, 0.88f) } },
                cues = new[] { Q(0f, "e4_tick_2", 6) } }, Vector2.zero, Vector2.zero, 1.0f, 1.3f, 6, 9),
            // 예지: 11:59 시계 0.18초
            Pan(new MShot { dur = 0.18f, desc = "11:59 (끼어듦)", tin = MTrans.Cut, layers = Clock(54, 54, 0), cues = new[] { Q(0f, "e4_bell_1", 12, 0.12f) } }, Vector2.zero, Vector2.zero, 1.5f, 1.6f, -8, -8),
            // ── 들것: 천장 형광등이 휙휙 지나감 (돌아가는 더치 앵글 + 달리 줌 + 핸드헬드)
            Pan(new MShot { dur = 1.1f, desc = "들것에서 올려다본 천장", tin = MTrans.Zoom, tinDur = 0.3f, ease = 1, hand = 12, dolly = 1.25f,
                layers = new[] { Edge(L("ceiling", 0.5f, 0.004f, 0.01f), 0.03f), Ghost(Past(L("tower_clock", 0.8f, 0.01f)), 0.22f, 0.3f, 0.7f), Ghost(L("eye_plain", 1.2f, 0.012f), 0.18f, 0.6f, 0.9f) },
                cues = new[] { Q(0f, "heart", 6), Q(0.45f, "heart", 6), Q(0.85f, "intro_whoosh_1", 4) } }, Vector2.zero, new Vector2(0, -40), 1.0f, 1.3f, -12, 10),
            Eye(0.32f, 6, "heart", 0),                        // 첫 눈: 아직 아무것도 비치지 않음
            Pan(new MShot { dur = 0.75f, desc = "실려 가는 시점: 병원 복도", tin = MTrans.Cut, ease = 1, hand = 14,
                layers = new[] { L("gurney", 1f, 0f, 0.008f), Ghost(L("ecg", 1f, 0.01f), 0.3f, 0.15f, 0.7f), Ghost(Past(L("hands_child", 1.3f, 0.012f)), 0.2f, 0.4f, 0.8f) },
                cues = new[] { Q(0f, "heart", 8) } }, new Vector2(0, -60), new Vector2(0, 90), 1.0f, 1.4f, 10, -6, 4),
            Pan(new MShot { dur = 0.4f, desc = "천장 (뒤집힌 각도)", tin = MTrans.Cut, hand = 16, layers = new[] { Edge(L("ceiling", 0.5f, 0.008f), 0.03f), Ghost(L("floor_shatter", 1f, 0.01f), 0.25f, 0.2f, 0.8f) } }, Vector2.zero, Vector2.zero, 1.35f, 1.5f, 168, 186),
            // ── 과거: 눈동자(원) → 시계탑 시계(원) 매치 컷, 줌 관통
            Eye(0.55f, -4, "e4_tick_1", 1),                   // 두 번째 눈: 눈동자에 시계가 떠오름
            Pan(new MShot { dur = 1.3f, desc = "노을 진 시계탑을 올려다보는 아이", tin = MTrans.Zoom, tinDur = 0.4f, hand = 3,
                layers = new[] { Past(L("tower", 1f, 0.008f, 0f)), Ghost(Past(L("water", 0.8f, 0.01f)), 0.3f, 0.25f, 0.6f) } }, new Vector2(0, -120), new Vector2(0, 110), 1.2f, 1.04f, 8, 3),
            // 올려다본 시계탑 시계 — 달리 줌(현기증): 하늘은 멀어지고 시계는 그대로
            Pan(new MShot { dur = 0.9f, desc = "올려다본 시계탑 시계 (달리 줌)", tin = MTrans.Zoom, tinDur = 0.35f, hand = 3, dolly = 0.78f,
                layers = new[] { Past(L("tower_clock", 0.4f, 0.006f)), Ghost(L("eye_ecu", 1f, 0.01f), 0.25f, 0.35f, 0.75f) }, cues = new[] { Q(0.1f, "e4_bell_2", 6) } }, Vector2.zero, Vector2.zero, 1.35f, 1.35f, -4, 4),
            // 시계탑 = 따로 뽑은 오브젝트(tower_obj)를 탑 없는 광장 판(child_run_bg)에 같은 depth로 세움 → 달려감 → 크래시 줌 → 금 가며 흔들림 → 무너짐 → 화면째 깨지며 다음 컷
            Pan(new MShot { dur = 0.8f, desc = "시계탑으로 달려가는 아이", tin = MTrans.Dissolve, tinDur = 0.3f, hand = 8,
                layers = new[] { Past(L("child_run_bg", RunD, 0.003f, 0f)), Tower(), }, cues = new[] { Q(0.1f, "intro_whoosh_2", 3) } }, new Vector2(-140, 0), new Vector2(60, 20), 1.1f, 1.2f, -6, -3),
            Pan(new MShot { dur = 0.5f, desc = "시계탑 크래시 줌 · 금이 감", tin = MTrans.Cut, ease = 1, hand = 10,
                layers = new[] { Past(L("child_run_bg", RunD, 0.006f)), Tower(0, -4) },
                cues = new[] { Q(0f, "e4_bell_2", 16, 0.1f), Q(0.55f, "e4_bell_1", 10) } }, new Vector2(60, 20), TowerPan, 1.2f, 1.8f, -3, 6),
            // 시계탑이 유리처럼 깨짐: 단계별로 따로 그린 그림을 뚝뚝 넘김 (물컵과 같은 방식, 이미지를 찢지 않음)
            //   ① 유리 같은 금이 시계판에서 퍼짐 → ② 산산이 박살 나며 빛이 터짐 → 그 화면이 그대로 유리처럼 깨지며 조각난 콜라주 컷으로
            TowerStage("tower_break1", "금이 퍼지는 시계탑", 0.32f, 1.0f, 1.06f, 12, new[] { Q(0f, "e5b_bell", 16), Q(0.5f, "e4_bell_1", 10) }),
            TowerStage("tower_break2", "유리처럼 박살 나는 시계탑", 0.3f, 1.06f, 1.14f, 18, new[] { Q(0f, "explosion_1", 30, 0.22f) }),
            // ── 콜라주 1: 조각마다 다른 시간 — 벌어지며 깜빡임
            Pan(new MShot { dur = 1.3f, desc = "엇갈린 기억 조각", tin = MTrans.Shatter, tinDur = 0.5f, hand = 6,
                collage = new[] { "tower_break2", "tower_break1", "hands_old", "hands_child", "tower", "gurney", "ecg", "train", "ceiling", "eye_ecu", "child_run", "floor_burst", "bedside_hand", "room_bg" },
                cols = 6, rows = 4, spread = 1.3f, swap = 0.6f,
                cues = new[] { Q(0.12f, "e4_tick_1", 3), Q(0.35f, "e4_tick_2", 4), Q(0.6f, "e4_tick_1", 6), Q(0.85f, "e4_tick_2", 9) } }, Vector2.zero, new Vector2(30, -20), 1.05f, 1.3f, -4, 5),
            // ── 의사: 달리 줌(현기증) — 방이 늘어나며 의사만 그대로, 숨 막힘
            Pan(new MShot { dur = 1.4f, desc = "소리 없는 의사의 입 (달리 줌)", tin = MTrans.Cut, hand = 2, dolly = 1.7f,
                layers = new[] { L("room_bg", 0.4f, 0.016f), Ghost(L("ecg", 0.6f, 0.012f), 0.22f, 0.2f, 0.6f), Ghost(Past(L("tower", 0.5f, 0.014f)), 0.2f, 0.55f, 0.95f), L("doctor", 1f, 0.004f, 0f) },
                cues = new[] { Q(0.1f, "story_heartbeat", 3) } }, Vector2.zero, Vector2.zero, 1.05f, 1.05f, -5, -9),
            Eye(0.26f, 10, "e4_tick_2"),
            // ── 물컵 (동작 이어 붙이기)
            //  ① 넓게: 떨리는 손이 컵을 향해 뻗음 (컵 · 그림자 · 손 = 책상과 같은 depth · 흐림 · 빛)
            Pan(new MShot { dur = 1.3f, desc = "떨리는 손이 협탁 위 물컵으로", tin = MTrans.Dissolve, tinDur = 0.35f, hand = 3,
                layers = new[] { TableBg(), TableHand(), Shadow(), GlassOnTable() },
                cues = new[] { Q(0.3f, "heart", 3), Q(0.75f, "heart", 4) } }, new Vector2(-60, 0), new Vector2(-120, 30), 1.0f, 1.25f, -2, -4),
            //  ② 가까이: 손끝이 닿고 컵이 기울어 떨어짐
            Knock(0.85f, false),
            //  ③ 바닥 높이: 떨어지는 컵
            FallClose(0.5f, false),
            //  ④ 바닥에서 깨짐
            Break(false)[0], Break(false)[1], Break(false)[2],
            //  ⑤ 거꾸로: 단계 그림을 거꾸로 뚝뚝 → 컵이 솟구쳐 → 책상으로 되돌아감 (시간 역행)
            Break(true)[0], Break(true)[1], Break(true)[2],
            FallClose(0.28f, true),
            Knock(0.4f, true),
            Pan(new MShot { dur = 0.35f, desc = "제자리로 돌아온 컵", tin = MTrans.Cut, hand = 2, layers = new[] { TableBg(), Shadow(), GlassOnTable() },
                cues = new[] { Q(0.05f, "e4_tick_1", 10, 0.1f) } }, new Vector2(-120, 30), new Vector2(-120, 30), 1.25f, 1.3f, -4, -4),
            // ── 떠나는 기차 (과거) — 휙 · 맞잡은 손이 겹침
            Pan(new MShot { dur = 0.8f, desc = "떠나는 기차 창에 비친 손", tin = MTrans.Dissolve, tinDur = 0.3f, hand = 6,
                layers = new[] { Past(L("train", 1f, 0f, 0.004f), 0.45f), Ghost(L("holding", 1.1f, 0.008f), 0.4f, 0.3f, 0.75f), Ghost(L("ceiling", 0.8f, 0.012f), 0.18f, 0.5f, 0.95f) } }, new Vector2(260, 0), new Vector2(-260, 0), 1.15f, 1.2f, 5, 2),
            // ── 손을 꼭 쥐는 손 — 거꾸로 달리 줌(방이 좁혀짐) · 아이 손이 겹쳐 떠오름
            Pan(new MShot { dur = 1.3f, desc = "노인의 손을 꼭 쥐는 손", tin = MTrans.Dissolve, tinDur = 0.3f, hand = 3, dolly = 0.75f,
                layers = new[] { L("room_bg", 0.4f, 0.02f), L("holding", 1f, 0f), Ghost(Past(L("hands_child", 1.3f, 0.01f)), 0.35f, 0.35f, 0.8f) },
                cues = new[] { Q(0.2f, "heart", 3) } }, Vector2.zero, Vector2.zero, 1.15f, 1.15f, 3, -2),
            // ── 레퀴엠식 째깍 반복: 0.16초 조각이 째깍마다 뚝뚝 (현재 · 과거 · 미래 무작위)
            Tk("째깍: 태엽 감는 손", -8, 1.35f, "e4_tick_1", L("bench_bg", 0.5f, 0.016f), L("hands_old_fg", 1.25f)),
            Tk("째깍: 천장", 14, 1.3f, "e4_tick_2", L("ceiling", 1f, 0.006f), Ghost(L("floor_burst", 1.2f, 0.01f), 0.3f, 0f, 1f)),
            Tk("째깍: 시계탑", -12, 1.4f, "e4_tick_1", Past(L("tower_clock", 1f))),
            Tk("째깍: 심전도", 9, 1.25f, "e4_tick_2", L("ecg", 1f)),
            Tk("째깍: 눈", -5, 1.3f, "e4_tick_1", L("eye_plain", 1f), EyeClock(false), Ghost(Past(L("tower_clock", 1.2f, 0.01f)), 0.3f, 0f, 1f)),
            Tk("째깍: 물컵", 7, 1.2f, "e4_tick_2", TableBg(), Shadow(), GlassOnTable()),
            Tk("째깍: 아이", -14, 1.3f, "e4_tick_1", Past(L("child_run", 1f))),
            Tk("째깍: 의사", 12, 1.45f, "e4_tick_2", L("room_bg", 0.4f, 0.018f), L("doctor", 1.1f)),
            // ── 콜라주 2: 흩어진 조각이 모여 하나의 시계가 됨 → 줌 관통
            Pan(new MShot { dur = 1.3f, desc = "모여드는 조각", tin = MTrans.Mix, tinDur = 0.25f, hand = 5,
                collage = new[] { "clock_face", "ecg", "tower_clock", "water", "room_bg", "eye_ecu", "floor_bg" },
                cols = 5, rows = 3, spread = -1.4f, swap = 0.7f, ease = 1,
                cues = new[] { Q(0.3f, "e4_tick_1", 4), Q(0.6f, "e4_tick_2", 6), Q(0.9f, "e4_bell_2", 12) } }, Vector2.zero, Vector2.zero, 1.3f, 1.0f, 10, 0),
            Pan(new MShot { dur = 2.3f, desc = "11:59의 벽시계", tin = MTrans.Zoom, tinDur = 0.35f, hand = 1.5f, layers = Clock(36, 6, 2.2f),
                cues = new[] { Q(0.86f, "story_flatline", 4) } }, Vector2.zero, Vector2.zero, 1.0f, 1.55f, 0, -4, sfx: "e4_tick_1"),
            new MShot { dur = 0.5f, sfx = "shot_1", tin = MTrans.Flash, tinDur = 0.45f, shake = 12, tint = Color.white },
            new MShot { dur = 1.0f, tint = Color.black },
        };

        static MShot Pan(MShot s, Vector2 p0, Vector2 p1, float z0, float z1, float r0 = 0, float r1 = 0, float shake = 0, string sfx = null)
        {
            s.pan0 = p0; s.pan1 = p1; s.zoom0 = z0; s.zoom1 = z1; s.rot0 = r0; s.rot1 = r1; s.shake = shake; s.sfx = sfx;
            return s;
        }

        void Awake()
        {
            rm = GetComponent<RunManager>();
            RunManager.StoryHold = true;                 // 연출이 끝날 때까지 방에 들어가도 웨이브가 안 나옴
            rm.RoomClearedEvent += OnRoomCleared;
        }

        void OnDestroy()
        {
            if (rm) rm.RoomClearedEvent -= OnRoomCleared;
            RunManager.StoryHold = false;
        }

        /// <summary>현실 몽타주: 잔상 + 글리치 · 효과음 없이 전용 곡 story_opening만, 재생 중 인게임 곡 멈춤 (2026-11-23 사용자)</summary>
        public static IEnumerator PlayOpening(StoryOverlay ov)
        {
            Music.Story("story_opening", 0.2f);
            var mt = ov.Montage(); mt.echo = 0.22f; mt.glitch = 1f; mt.sfx = false;
            yield return mt.Play(Montage, "Story/Opening/");
            mt.sfx = true;
            Music.StoryEnd(1.5f);
            ov.HideMontage();
        }

        IEnumerator Start()
        {
            p = PlayerController.Instance;
            var ov = StoryOverlay.I;
            ov.SetBlack(1);
            if (p) p.InputLocked = true;
            yield return null;
            // 1. 오프닝 = 꿈1 '멈춘 손' (2026-11-24 사용자: 현실 몽타주 대신, 제목 카드 없음, 끝에 '삐' → 총성 → 암전 소리만)
            //    현실 몽타주(PlayOpening · Story/Opening · story_opening)는 보관만 함
            if (!Meta.StorySeen(StoryRun.KeyOpening))
            {
                var d1 = StoryRun.Dreams[0];
                yield return ov.PlayDream(1, d1.title, d1.fragments, authored: DreamCuts.For(1), shotEnd: true);
                Meta.MarkStory("dream1");
                Meta.MarkStory(StoryRun.KeyOpening);
            }
            // 2. 깨어남: 쓰러진 고양이 옆 낮은 곳에서 플레이 시점으로 올라감
            var cam = Camera.main;
            yield return null;                               // CameraFollow가 플레이 시점을 한 번 잡게
            Vector3 endPos = cam ? cam.transform.position : Vector3.zero; Quaternion endRot = cam ? cam.transform.rotation : Quaternion.identity;
            if (cam && p)
            {
                CameraFollow.Hold = true;
                var look = p.transform.position + Vector3.up * 0.6f;
                var startPos = p.transform.position + new Vector3(1.4f, 0.35f, -1.2f);
                var startRot = Quaternion.LookRotation(look - startPos);
                cam.transform.SetPositionAndRotation(startPos, startRot);
                StartCoroutine(ov.FadeBlack(1, 0, 1.6f));
                yield return new WaitForSecondsRealtime(1.2f);
                for (float t = 0; t < 2.4f; t += Time.unscaledDeltaTime)
                {
                    float u = t / 2.4f, e = u * u * (3 - 2 * u);
                    cam.transform.SetPositionAndRotation(Vector3.Lerp(startPos, endPos, e), Quaternion.Slerp(startRot, endRot, e));
                    yield return null;
                }
                CameraFollow.Hold = false;
            }
            else yield return ov.FadeBlack(1, 0, 1f);
            StartCoroutine(ov.Headache(0.7f, 1.4f));
            yield return Talk("고양이#두통::윽…… 꿈인가……. 머리가 아프군…….");
            // 3. 소환: 웨이브를 풀어 줌 → 적이 솟으면 대사
            Music.Duck(1f, 1f);
            RunManager.StoryHold = false;
            float wait = 0;
            while (Enemy.All.Count == 0 && wait < 6f) { wait += Time.unscaledDeltaTime; yield return null; }
            yield return new WaitForSecondsRealtime(0.5f);
            Time.timeScale = 0f;
            yield return Talk("고양이#결의::……지금은 잠자리 같은 걸 신경 쓸 때가 아니다. 우선 급한 불부터 해결하자고.");
            Time.timeScale = 1f;
            if (p) p.InputLocked = false;
            ov.SetCaption(controls);
        }

        static IEnumerator Talk(params string[] lines)
        {
            var ui = FindFirstObjectByType<GameUI>();
            if (!ui) yield break;
            bool done = false;
            ui.Talk("고양이", lines, () => done = true);
            while (!done) yield return null;
        }

        /// <summary>첫 방을 비우면 사방에서 적이 몰려옴 → 버티다 결국 쓰러짐 (일정 시간이 지나면 시간이 멎으며 강제로)</summary>
        void OnRoomCleared()
        {
            if (swarmStarted) return;
            swarmStarted = true;
            StartCoroutine(Swarm());
        }

        IEnumerator Swarm()
        {
            var ov = StoryOverlay.I;
            ov.SetCaption("");
            yield return new WaitForSeconds(0.6f);
            if (!p) yield break;
            StartCoroutine(ov.Headache(0.6f, 1.2f));
            for (int wave = 0; wave < 3 && !collapsing; wave++)
            {
                int n = 8 + wave * 4;
                for (int i = 0; i < n; i++)
                {
                    var dir = Quaternion.Euler(0, 360f * i / n + Random.Range(-10f, 10f), 0) * Vector3.forward;
                    var at = rm.ClampToRoom(p.transform.position + dir * Random.Range(6f, 9f), 1.5f);
                    var e = EnemyFactory.RandomMinion(at, rm.Chapter, elite: wave > 0);
                    rm.AddEnemy(e);
                }
                CameraFollow.Shake(0.3f);
                yield return new WaitForSeconds(4.5f);
            }
            if (!collapsing) Collapse();                 // 끝까지 버텨도 시간이 멎으며 쓰러짐
        }

        public void Collapse()
        {
            if (collapsing) return;
            collapsing = true;
            StartCoroutine(CollapseRun());
        }

        IEnumerator CollapseRun()
        {
            var ov = StoryOverlay.I;
            ov.SetCaption("");
            if (p) { p.InputLocked = true; p.health.invulnerable = true; }
            Time.timeScale = 0.25f;
            Music.Duck(0.15f, 0.6f);
            yield return ov.Headache(0.9f, 1.3f);
            if (Sfx.Has("story_flatline")) Sfx.Play("story_flatline", 0.8f, 0f);
            yield return ov.FadeWhite(0, 1, 1.2f);
            Time.timeScale = 1f;
            yield return new WaitForSecondsRealtime(0.4f);
            ov.SetWhite(0); ov.SetBlack(1);
            Meta.MarkStory(StoryRun.KeyTut1);
            Meta.RunEnded();
            ov.RevealAfterLoad(0.8f, 1.0f);
            RunManager.ReturnToVillage();
        }
    }

    /// <summary>판 2 튜토리얼 (2026-11-23 사용자 문구 · 흐름): 총알 시계(HUD) → 시각 조건(Tab → 인벤토리, 두 장) · 불릿 파티는 고양이 독백 → 시너지(Tab → 인벤토리 능력치 탭)</summary>
    public class StoryHints : MonoBehaviour
    {
        static bool busy;                                   // 안내 · 독백이 겹치지 않게
        int bulletsAtStart;                                 // 판을 시작할 때 이미 가진 탄 (코스튬 지급 등) — 이보다 늘면 '총탄 획득'

        void OnDestroy() => busy = false;

        /// <summary>기본 '탄환'(1000)이 아닌 탄 수: 보관함 + 실린더</summary>
        static int BulletCount(PlayerController p)
        {
            int n = 0;
            foreach (var it in p.inv.storage) if (it != null && it.kind == ItemKind.Bullet && it.bullet.Id != 1000) n++;
            for (int i = 1; i <= 12; i++) { var b = p.rev.slots[i].bullet; if (b != null && b.Id != 1000) n++; }
            return n;
        }

        IEnumerator Start()
        {
            busy = false;
            var p = PlayerController.Instance;
            if (!p) yield break;
            bulletsAtStart = BulletCount(p);
            if (!Meta.StorySeen(StoryRun.KeySynergy)) StartCoroutine(SynergyWatch(p));
            if (!Meta.StorySeen(StoryRun.KeyTut2)) yield return Basics(p);
        }

        public const string KeyClock = "tut2_clock", KeyCond = "tut2_cond", KeyHeat = "tut2_heat";
        // Tab 유도 문구 (2026-11-23 사용자 컨펌)
        public const string TabPrompt = "<color=#FFD54A>Tab</color> 키를 눌러 인벤토리를 여세요.";

        IEnumerator Basics(PlayerController p)
        {
            var ov = StoryOverlay.I;
            // 1. 처음 쏘면: 총알 시계 (HUD를 비춤)
            if (!Meta.StorySeen(KeyClock))
            {
                while (p.rev.shotsThisCycle == 0 || p.InputLocked || busy) yield return null;
                yield return new WaitForSeconds(0.25f);
                busy = true;
                yield return ov.Hint("총알 시계", "각 실린더의 칸은 시계를 의미합니다.\n총을 쏠 때마다 시간은 흘러가고,\n흘러가는 시간에 따라 특별한 효과를\n받을 수 있을지도 몰라요!", new Rect(690, 750, 540, 280));   // 실린더 두 개 + 시계판
                busy = false;
                Meta.MarkStory(KeyClock);
            }
            // 2 · 3은 따로 기다림 (Tab을 안 눌러도 열기 안내는 나옴)
            bool condDone = Meta.StorySeen(KeyCond), heatDone = Meta.StorySeen(KeyHeat);
            if (!condDone) StartCoroutine(Run(Cond(p), () => condDone = true));
            if (!heatDone) StartCoroutine(Run(Heat(p), () => heatDone = true));
            while (!condDone || !heatDone) yield return null;
            Meta.MarkStory(StoryRun.KeyTut2);
        }

        static IEnumerator Run(IEnumerator body, System.Action done) { yield return body; done(); }

        /// <summary>Tab을 누르게 유도 → 인벤토리가 열리면 그 화면 위에서 안내 (다른 안내와 겹치지 않게 차례를 기다림)</summary>
        IEnumerator InInventory(System.Func<InventoryScreen, IEnumerator> body)
        {
            var ov = StoryOverlay.I;
            var ui = FindFirstObjectByType<GameUI>();
            if (!ui) yield break;
            while (!ui.InventoryUI || busy)
            {
                if (!busy) ov.SetCaption(ui.InventoryUI ? "" : TabPrompt);
                yield return null;
            }
            busy = true;
            ov.SetCaption("");
            yield return new WaitForSecondsRealtime(0.35f);   // 인벤토리가 다 그려진 뒤
            if (ui.InventoryUI) yield return body(ui.InventoryUI);
            busy = false;
        }

        /// <summary>2. 첫 총탄을 얻으면 (2026-11-23 사용자): Tab → 인벤토리에서 시각 조건 (탄환 시계 → 얻은 탄의 설명 칸)</summary>
        IEnumerator Cond(PlayerController p)
        {
            var ov = StoryOverlay.I;
            while (BulletCount(p) <= bulletsAtStart) yield return new WaitForSecondsRealtime(0.2f);
            yield return new WaitForSecondsRealtime(0.3f);
            yield return InInventory(inv => CondPages(ov, inv));
            Meta.MarkStory(KeyCond);
        }

        static IEnumerator CondPages(StoryOverlay ov, InventoryScreen inv)
        {
            inv.ShowFeaturedBullet();
            yield return ov.HintAt("시각 조건", "탄마다 활성화되는 시각 조건이 있습니다.\n조건을 만족시키면 총탄의 신비한 힘이 깨어납니다.", inv.ClockRects);
            if (inv) yield return ov.HintAt("시각 조건", "각 조건과 현재 적용된 시간은 이곳에서 확인 가능해요.\n조건을 활성화시켜 더욱 강력한 힘을\n적들에게 선보이세요.", inv.InfoRect);
        }

        /// <summary>3. 열기가 처음 다 차는 순간: 고양이 독백 → Q 안내(느린 시간) → 쓰고 나면 한마디</summary>
        IEnumerator Heat(PlayerController p)
        {
            var ov = StoryOverlay.I;
            while (p.rev.heat < Revolver.HeatMax || busy || p.InputLocked) yield return null;
            busy = true;
            ov.SetCaption("");
            var ui = FindFirstObjectByType<GameUI>();
            if (ui) { bool done = false; ui.Talk("고양이", new[] { "고양이#씩::흠, 지금이다……. 본때를 보여주지!" }, () => done = true); while (!done) yield return null; }
            float ts = Time.timeScale; Time.timeScale = 0.3f;
            ov.SetCaption("<color=#FFD54A>Q</color> 키를 누르세요.");
            while (!p.UltimateActive && p.rev.heat >= Revolver.HeatMax) yield return null;
            ov.SetCaption("");
            Time.timeScale = ts <= 0.3f ? 1f : ts;
            while (p.UltimateActive) yield return null;
            yield return new WaitForSeconds(0.3f);
            if (ui) { bool done = false; ui.Talk("고양이", new[] { "고양이#씩::하핫, 아직 실력이 녹슬지 않았군!" }, () => done = true); while (!done) yield return null; }
            busy = false;
            Meta.MarkStory(KeyHeat);
        }

        /// <summary>시너지: 같은 특성이 처음 첫 단계에 닿으면 Tab → 인벤토리 능력치 탭(특성 목록)을 비추며 설명</summary>
        IEnumerator SynergyWatch(PlayerController p)
        {
            while (true)
            {
                bool on = false;
                foreach (var kv in p.rev.TraitCounts()) if (p.rev.TierOf(kv.Value) >= 1) { on = true; break; }
                if (on) break;
                yield return new WaitForSecondsRealtime(0.3f);
            }
            yield return InInventory(inv => SynergyPage(StoryOverlay.I, inv));
            Meta.MarkStory(StoryRun.KeySynergy);
        }

        static IEnumerator SynergyPage(StoryOverlay ov, InventoryScreen inv)
        {
            inv.ShowStatsTab();
            yield return ov.HintAt("시너지", StoryRun.SynergyText, inv.InfoRect);
        }
    }
}
