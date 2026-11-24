// 꿈 컷 목록 (2026-11-23 사용자: "2번째 막도 비슷한 느낌인데 화풍을 수채화 느낌으로",
//   "첫번째 스토리 애니메이션처럼 긴박한 화면 연출과 여러가지 역동적인 이동, 셀 애니메이션식 구성으로 액션 구성, 뎁스 구분, 다양한 효과와 많은 컷 전환",
//   "심장에 피 퍼지는걸 물감으로 표현 … 점점 줌 확확 넣으면서 점점 피가 물감처럼 퍼져 나가는걸 시계 바늘의 이동과 함께")
// 오프닝 몽타주와 같은 문법(컷 많이 · 겹쳐 찍기 · 달리/크래시 줌 · 핸드헬드 · 더치 앵글 · 변화는 단계별로 그린 그림을 셀처럼 뚝뚝)을
// 수채화 꿈에 맞게: 종이빛 바탕 · 번지는 디졸브 · 잔상은 은은하게 · 디지털 글리치 대신 물감 번짐(가장자리 흐림)
// 뎁스: 배경 판(depth < 1, 흐림) / 오브젝트(매달린 시계 · 시곗바늘 — 따로 그려 오려 냄, depth ≥ 1, 물감 섞임 셰이더)
// 2026-11-23 사용자: 전환 · 무빙 더 빠르고 정신없게 · 유리 깨짐(조각) 연출 X · 톱니(수레바퀴) X · 그림책 아이는 실루엣 · 디졸브와 함께 물감 글리치 무작위
// 그림 = Resources/Story/Dreams/dream{n}/*.png (크롬 ChatGPT 수채화). 그림이 없는 컷은 자리 표시
//
// 꿈1 '멈춘 손': 태엽을 감는 주름진 손 ↔ 같은 동작의 작은 아이 손 (같은 구도 · 매치 컷) · 째깍 소리와 심장 소리가 엇갈림
//   → 바늘 없는 문자판에 따로 얹은 바늘이 째깍 · 박동마다 줌이 확확 들어가며 심장의 피가 물감처럼 번짐 → 초침이 59초에서 떨며 멈춤
//   → 아이 손이 노인 손 위에 겹쳐 같은 시계를 쥠 → 녹아내리는 11:59 (셀 단계) — 아이와 노인이 같은 사람일지도, 정도만 암시
using UnityEngine;

namespace CatTang
{
    public static partial class DreamCuts
    {
        public static MShot[] For(int chapter) => chapter == 1 ? Dream1 : chapter == 2 ? Dream2 : null;

        static readonly Color Paper = new(0.93f, 0.9f, 0.84f);
        static readonly Vector2 O = Vector2.zero;

        // ---- 도우미 (수채화: 기본 가장자리 번짐 · 바랜 색) ----
        static MLayer W(string img, float depth = 1f, float b0 = 0.003f, float b1 = -1, float edge = 0.026f) =>
            new MLayer { img = img, depth = depth, blur0 = b0, blur1 = b1 < 0 ? b0 : b1, edge = edge, sat = 0.95f };
        static MLayer Ghost(MLayer l, float peak, float a, float b) { l.fade = new[] { new Vector2(0, 0), new Vector2(a, peak), new Vector2(b, peak), new Vector2(1, 0) }; return l; }
        /// <summary>a~b 동안 서서히 번져 올라와 끝까지 남음 (다음 단계 그림이 물감처럼 퍼져 나옴)</summary>
        static MLayer Bleed(MLayer l, float a, float b) { l.fade = new[] { new Vector2(0, 0), new Vector2(a, 0), new Vector2(b, 1), new Vector2(1, 1) }; return l; }
        /// <summary>그려짐: 카메라 이동과 같은 진행으로 물감 경계가 dir 쪽으로 번져 나가며 칠해짐 (안 칠한 곳은 연한 밑그림)</summary>
        static MLayer Painted(MLayer l, float r0, float r1, Vector2 dir, float soft = 0.14f) { l.reveal0 = r0; l.reveal1 = r1; l.revealDir = dir; l.revealSoft = soft; return l; }
        static (float, string, float, float) Q(float u, string sfx, float shake = 0, float flash = 0) => (u, sfx, shake, flash);
        static MShot S(float dur, string desc, MTrans tin, float tinDur, MLayer[] layers, Vector2 p0, Vector2 p1, float z0, float z1,
                       float r0 = 0, float r1 = 0, float hand = 2, float dolly = 1, int ease = 0, (float, string, float, float)[] cues = null, string sfx = null) =>
            new MShot
            {
                dur = dur, desc = desc, tin = tin, tinDur = tinDur, layers = layers, tint = Paper,
                pan0 = p0, pan1 = p1, zoom0 = z0, zoom1 = z1, rot0 = r0, rot1 = r1, hand = hand, dolly = dolly, ease = ease, cues = cues, sfx = sfx,
            };

        // ---- 오브젝트 (오려 낸 부품) ----
        static MLayer Part(string img, float height, Vector2 pivot, Vector2 at, float depth, float r0, float r1, float blur = 0.0015f, float paint = 0.85f) =>
            new MLayer { img = img, height = height, pivot = pivot, at = at, depth = depth, r0 = r0, r1 = r1, blur0 = blur, blur1 = blur, sat = 0.95f, paint = paint };   // 물감 섞임: 오려 낸 가장자리가 바탕에 번져 듦
        /// <summary>사슬에 매달린 회중시계: 화면 위 가장자리에 매달려 진자처럼 흔들림</summary>
        static MLayer Pendulum(float x, float h, float amp, float hz, float depth = 1.15f, float blur = 0.0015f)
        { var l = Part("watch_hang", h, new Vector2(0.5f, 1f), new Vector2(x, 545), depth, 0, amp, Mathf.Max(blur, 0.003f), 1f); l.wave = hz; return l; }

        // ---- 문자판 + 따로 얹은 바늘 (dial_face · bleed1~3 모두 같은 구도, 축 = 화면 (-1, -5)) ----
        // 바늘 각도: 반시계 + (11:59 → 분침 6° · 시침 0.5°), 초침 s초 = 360 - 6s
        static readonly Vector2 Pin = new(-1, -5);
        static float Sec(float s) => 360f - 6f * s;
        static MLayer[] Hands(float s0, float s1, float tick = 0, float tremble = 0)
        {
            var sec = Part("hand_second", 427, new Vector2(0.5f, 0.18f), Pin, 1f, Sec(s0), tremble > 0 ? Sec(s0) + 1.6f : Sec(s1), 0.0015f, 0.35f);
            sec.tick = tick;
            if (tremble > 0) sec.wave = tremble;      // 59초에서 넘어가지 못하고 떨림
            return new[]
            {
                Part("hand_hour", 235, new Vector2(0.5f, 0.085f), Pin, 1f, 0.5f, 0.5f, 0.0015f, 0.5f),
                Part("hand_minute", 344, new Vector2(0.5f, 0.07f), Pin, 1f, 6f, 6f, 0.0015f, 0.5f),
                sec,
            };
        }
        /// <summary>문자판 판 + (다음 단계가 번져 올라옴) + 바늘</summary>
        static MLayer[] Dial(string baseImg, string nextImg, float a, float b, float s0, float s1, float tick = 0, float tremble = 0)
        {
            var hands = Hands(s0, s1, tick, tremble);
            var list = new System.Collections.Generic.List<MLayer> { W(baseImg, 1f, 0.0015f, 0.0015f, 0.02f) };
            if (nextImg != null) list.Add(Bleed(W(nextImg, 1f, 0.0015f, 0.0015f, 0.02f), a, b));
            list.AddRange(hands);
            return list.ToArray();
        }

        // 째깍(노인) ↔ 쿵(아이): 같은 구도의 손이 박자마다 바뀌며 점점 빨라짐 (셀처럼 뚝뚝)
        static MShot Wind(bool old, float dur, float rot, float z) =>
            S(dur, old ? "태엽을 감는 주름진 손" : "같은 동작의 작은 손", MTrans.Cut, 0,
              new[] { W(old ? "wind_old" : "wind_child", 1f, 0.002f) }, O, O, z, z + 0.05f, rot, rot, 4, 1, 1,
              new[] { Q(0f, old ? "e4_tick_1" : "heart", 5) });
        static MShot Crown(bool old, float dur, float rot) =>
            S(dur, old ? "태엽 꼭지를 돌리는 주름진 손끝" : "같은 꼭지를 돌리는 작은 손끝", MTrans.Cut, 0,
              new[] { W(old ? "crown_old" : "crown_child", 1f, 0.002f) }, O, O, 1.14f, 1.22f, rot, rot, 5, 1, 1,
              new[] { Q(0f, old ? "e4_tick_2" : "heart", 7) });
        // 레퀴엠식 끼어들기: 0.14초 · 더치 앵글
        static MShot Flick(string img, float rot, float z, string sfx, string ghost = null) =>
            S(0.14f, "끼어드는 기억: " + img, MTrans.Cut, 0, ghost == null ? new[] { W(img, 1f, 0.003f) } : new[] { W(img, 1f, 0.003f), Ghost(W(ghost, 1.15f, 0.008f), 0.35f, 0f, 1f) },
              O, O, z, z + 0.06f, rot, rot, 6, 1, 2, new[] { Q(0f, sfx, 6) });
        // 박동 컷: 줌이 확 들어가며(크래시) 다음 단계 피가 번져 오르고 초침이 한 칸 넘어감
        static MShot Beat(string baseImg, string nextImg, float s0, float s1, float z0, float z1, float dur, float shake, float flash, float rot) =>
            S(dur, "박동: 번지는 피와 초침", MTrans.Cut, 0, Dial(baseImg, nextImg, 0.05f, 0.85f, s1, s1),   // 컷 = 박동 = 초침이 한 칸 넘어가는 순간
              O, O, z0, z1, rot, rot * 0.6f, 3, 1, 1, new[] { Q(0f, "heart", shake, flash), Q(0.02f, "e4_tick_1", 0) });

        static readonly Vector2 R = new(1, 0), Up = new(0, 1);   // 그려짐 방향 (UI uv: 아래 = 0)

        // 빠른 끼어들기 (2026-11-23 사용자: "전환이랑 무빙도 더 빠르고 정신없게"): 휙 쓸리는 카메라 + 겹쳐 비치는 다른 시간 + 센 박자엔 물감 글리치
        static MShot Dart(string img, string ghost, float dur, float rot0, float rot1, float z0, float z1, Vector2 p0, Vector2 p1, string sfx, float shake = 8, MTrans tin = MTrans.Cut) =>
            S(dur, "휙 지나가는 기억: " + img, tin, 0.1f, ghost == null ? new[] { W(img, 1f, 0.003f) } : new[] { W(img, 1f, 0.003f), Ghost(W(ghost, 1.2f, 0.01f), 0.4f, 0f, 1f) },
              p0, p1, z0, z1, rot0, rot1, 9, 1, 1, new[] { Q(0f, sfx, shake) });

        // ---- 배색 변주 (2026-11-23 사용자: "배색이랑 질감이 동일해서 … 세피아 필터나 색반전 같은 것도, 배색 다른 컷들도 중간중간, 디졸브 컷에도 섞어서") ----
        // 보라 · 황토 수채화 사이에 끼우는 것: 컷 배색(G) · 빠져나가는 장면 배색(T) · 박자 음화 번쩍(Neg 큐 = 음수 flash) · 컷 안 잠깐 뒤집힘(F)
        //   다른 배색으로 따로 그린 컷(크롬 ChatGPT): night_ws(남색 밤 작업대) · ink_wind(먹 + 주홍 초침) · teal_corridor(녹청 병원 복도)
        //   · gold_child(주황 · 코발트, 거대한 문자판 앞 아이) · chalk_watch(검은 종이에 흰 분필 회중시계) · cyan_heart(청사진 심장 시계)
        static MShot G(MShot s, MGrade g) { s.grade = g; return s; }
        static MShot T(MShot s, MGrade g) { s.tinGrade = g; return s; }
        static MShot F(MShot s, params (float u, float dur, MGrade g)[] f) { s.flips = f; return s; }
        static MLayer LG(MLayer l, MGrade g) { l.grade = g; return l; }
        static (float, string, float, float) Neg(float u, string sfx, float shake, float dur = 0.14f) => (u, sfx, shake, -dur);
        // 다른 배색 컷 한 장: 짧게 · 크게 흔들리며
        static MShot Alien(string img, string desc, float dur, float rot0, float rot1, float z0, float z1, Vector2 p0, Vector2 p1, string sfx, float shake, MTrans tin = MTrans.Cut, float tinDur = 0.1f, string ghost = null, MGrade ghostGrade = null) =>
            S(dur, desc, tin, tinDur, ghost == null ? new[] { W(img, 1f, 0.003f) } : new[] { W(img, 1f, 0.003f), LG(Ghost(W(ghost, 1.15f, 0.01f), 0.3f, 0f, 1f), ghostGrade) },
              p0, p1, z0, z1, rot0, rot1, 7, 1, 1, new[] { Q(0f, sfx, shake) });

        // 움직이는 건 셀 프레임 · 오려 낸 부품으로 (2026-11-23 사용자: "움직이는 오브젝트는 연속된 동작 전부 이미지로 만들거나 오브젝트 따로 뽑아서 애니메이션 방식으로, 컷 엄청 많이")
        //   ink_wind → _b → _c : 태엽 꼭지 · 손가락 · 주홍 초침이 한 단계씩 (셀)
        //   cyan_heart ↔ _b : 이완 ↔ 수축 (박동마다 뚝뚝)
        //   gold_child → _b → _c : 거대 문자판 바늘이 12시로 → 빛이 터지며 아이가 빛에 삼켜짐
        //   chalk_bg + chalk_watch_obj : 분필 회중시계만 오려 내 진자로 흔들림 (축 = 사슬 꼭대기)
        static readonly string[] InkCels = { "ink_wind", "ink_wind_b", "ink_wind_c" };
        static MShot Ink(int cel, float dur, float rot, float z, string sfx = "e4_tick_2", float shake = 9, MGrade g = null) =>
            G(S(dur, "먹 · 주홍 초침 태엽 (셀 " + (cel + 1) + ")", MTrans.Cut, 0, new[] { W(InkCels[cel % 3], 1f, 0.002f) }, O, O, z, z + 0.04f, rot, rot, 5, 1, 1,
              new[] { Q(0f, sfx, shake) }), g);
        static MShot Heart(bool squeeze, float dur, float rot, float z, float shake, MGrade g = null) =>
            G(S(dur, squeeze ? "청사진 심장 · 수축" : "청사진 심장 · 이완", MTrans.Cut, 0, new[] { W(squeeze ? "cyan_heart_b" : "cyan_heart", 1f, 0.002f) }, O, O, z, z + (squeeze ? -0.03f : 0.05f), rot, rot, 4, 1, 1,
              new[] { Q(0f, "heart", shake) }), g);
        static MShot Child(int cel, float dur, Vector2 p0, Vector2 p1, float z0, float z1, (float, string, float, float)[] cues, MTrans tin = MTrans.Cut) =>
            S(dur, "거대한 문자판 앞의 작은 아이 (셀 " + (cel + 1) + ")", tin, 0.12f,
              new[] { W(cel == 0 ? "gold_child" : cel == 1 ? "gold_child_b" : "gold_child_c", 0.75f, 0.004f, 0.002f) }, p0, p1, z0, z1, 1.5f, -1.5f, 4, 1.06f, 2, cues);
        /// <summary>검은 종이 + 오려 낸 분필 회중시계가 사슬 꼭대기를 축으로 흔들림 (wave)</summary>
        static MShot Chalk(float dur, float hz, float amp, float z0, float z1, float rot, (float, string, float, float)[] cues)
        {
            var watch = Part("chalk_watch_obj", 1067, new Vector2(40f / 730f, 1f), new Vector2(134, 605), 1.1f, -26f, -26f + amp, 0.0015f, 0f);
            watch.wave = hz;
            var s = S(dur, "분필 회중시계 진자", MTrans.Cut, 0, new[] { W("chalk_bg", 0.7f, 0.004f), watch }, O, new(0, -20), z0, z1, rot, rot * 0.5f, 4, 1, 2, cues);
            s.tint = Color.black;
            return s;
        }

        static MShot[] Dream1 => new[]
        {
            // ══ 1. 꿈이 열림: 종이에 물감 한 방울 → 시계공 작업대가 카메라를 따라 그려짐 → 흔들리는 회중시계
            S(1.1f, "번지는 물감 방울", MTrans.Dissolve, 0.7f, new[] { W("ink_bloom", 1f, 0.01f, 0.0f), Ghost(W("dial_face", 1.1f, 0.012f), 0.2f, 0.3f, 0.9f) }, O, O, 1.25f, 1.0f, 0, 4, 2, 1, 1,
              new[] { Q(0.4f, "e4_tick_1", 1) }),
            S(1.8f, "해 질 녘 작업대가 그려지며 흔들리는 회중시계", MTrans.Zoom, 0.25f, new[]
              {
                  Painted(W("ws_bg", 0.55f, 0.007f, 0.004f), 0.1f, 1.3f, R, 0.18f),
                  Pendulum(-120, 820, 18, 0.7f),
                  Ghost(W("book_boy", 0.8f, 0.012f), 0.2f, 0.45f, 0.95f),
                  Ghost(W("wind_old", 1.1f, 0.014f), 0.14f, 0.1f, 0.4f),
              }, new(300, 0), new(-260, 20), 1.25f, 1.36f, -2, -6, 4, 1.15f, 0,
              new[] { Q(0.1f, "e4_tick_1", 2), Q(0.45f, "e4_tick_2", 2), Q(0.8f, "e4_tick_1", 2) }),
            S(0.35f, "흔들리는 시계 크래시 줌", MTrans.Cut, 0, new[] { W("ws_bg", 0.55f, 0.012f), Pendulum(-120, 820, 18, 0.7f) },
              new(-260, 20), new(150, -330), 1.3f, 2.3f, -6, 12, 10, 1, 1, new[] { Q(0f, "e4_bell_2", 16, 0.06f) }),
            // 같은 작업대, 한밤의 남색 — 노을빛이 꺼지고 등불만 남음
            T(Alien("night_ws", "한밤의 작업대 (남색 · 등불 하나)", 0.55f, -3, 2, 1.12f, 1.26f, new(140, 0), new(-90, 10), "e4_tick_2", 6, MTrans.Dissolve, 0.16f, "ws_bg", MGrade.Negative), MGrade.Cyanotype),
            G(Dart("dial_face", "wind_child", 0.16f, 14, 8, 1.6f, 1.75f, new(80, 0), new(-60, 0), "e4_tick_2", 10), MGrade.Negative),
            Dart("book_old", null, 0.13f, -12, -16, 1.3f, 1.4f, new(-90, 20), new(70, -10), "heart", 9),

            // ══ 2. 태엽 감는 손: 노인 ↔ 아이 (같은 구도 매치 컷, 점점 빨라짐) · 컷마다 다른 시간이 겹쳐 비침
            S(1.0f, "태엽을 감는 주름진 손", MTrans.Dissolve, 0.18f, new[] { W("wind_old", 1f, 0.004f, 0f), Ghost(W("wind_child", 1.08f, 0.006f), 0.45f, 0.3f, 0.7f), Ghost(W("book_old", 0.7f, 0.012f), 0.18f, 0.05f, 0.45f) },
              new(-40, 0), new(45, -16), 1.04f, 1.2f, 3, -3, 5, 1, 1, new[] { Q(0.15f, "e4_tick_1", 3), Q(0.55f, "e4_tick_2", 3) }),
            // 노인 손은 바랜 사진(세피아) · 먹 · 음화로 번갈아, 아이 손은 원래 색 → 같은 구도인데 시간의 색이 엇갈림
            Wind(false, 0.32f, 5, 1.06f), G(Wind(true, 0.28f, -6, 1.1f), MGrade.Sepia),
            Crown(true, 0.22f, -8), Dart("book_page1", "crown_child", 0.12f, 10, 14, 1.5f, 1.6f, new(200, 0), new(80, 0), "e4_tick_1", 12),
            // 먹 · 주홍 초침 셀: 꼭지가 한 단계씩 감기고 초침이 한 칸씩 (째깍마다 한 장)
            Ink(0, 0.1f, -10, 1.1f, "e4_tick_1"), Ink(1, 0.1f, -10, 1.12f), Ink(2, 0.12f, -10, 1.14f, "e4_tick_1", 11),
            Crown(false, 0.2f, 9), Wind(false, 0.17f, 11, 1.14f), G(Wind(true, 0.15f, -12, 1.18f), MGrade.Ink),
            G(Dart("ecg_wash", "wind_old", 0.12f, -14, -10, 1.35f, 1.45f, new(-120, 0), new(60, 0), "heart", 14), MGrade.Verdigris),
            G(Crown(true, 0.13f, -13), MGrade.Negative), Crown(false, 0.12f, 14),
            Ink(0, 0.07f, 14, 1.5f, "e4_tick_1", 12), Ink(1, 0.07f, 14, 1.54f), Ink(2, 0.08f, 14, 1.58f, "e4_tick_1", 12, MGrade.Negative),
            Wind(false, 0.11f, 15, 1.22f), G(Wind(true, 0.11f, -16, 1.26f), MGrade.OldNegative),

            // ══ 3. 아이 — 작업대에서 지켜보던 눈 → 밤에 펼치던 그림책 (실루엣)
            T(S(1.1f, "작업대에서 노인의 손을 지켜보는 아이", MTrans.Paint, 0.6f, new[] { W("boy_watch", 0.5f, 0.004f), Ghost(W("book_boy", 0.9f, 0.01f), 0.35f, 0.35f, 0.9f), LG(Ghost(W("hands_overlap", 1.1f, 0.014f), 0.14f, 0.05f, 0.4f), MGrade.Negative) },
              new(70, 0), new(-50, 10), 1.18f, 1.22f, -2, 3, 4, 0.78f, 1, new[] { Q(0.3f, "e4_tick_2", 2), Q(0.7f, "e4_tick_1", 2) }), MGrade.Sepia),
            Dart("wind_child", "book_boy", 0.14f, 12, 6, 1.3f, 1.42f, new(100, 0), new(-40, 0), "heart", 10),
            // 주황 · 코발트: 거대한 문자판 앞의 작은 아이 (따로 그린 다른 배색 컷) — 줌 관통으로 그림책에 이어짐
            //   셀 3장: 바늘이 12시로 모임 → 12시 → 빛이 터지며 아이가 빛에 삼켜짐. 카메라는 세 컷을 이어 한 번에 올려다봄
            Child(0, 0.32f, new(0, 70), new(0, 30), 1.05f, 1.1f, new[] { Q(0f, "e4_bell_2", 8), Q(0.6f, "e4_tick_1", 3) }, MTrans.Whip),
            Child(1, 0.26f, new(0, 30), new(0, 0), 1.1f, 1.16f, new[] { Q(0f, "e4_tick_2", 10) }),
            Child(2, 0.34f, new(0, 0), new(0, -40), 1.16f, 1.3f, new[] { Neg(0f, "e4_bell_2", 16, 0.14f) }),
            // 동화책 (2026-11-23 사용자: "동화 부분은 배색이 너무 비슷함, 글리치랑 물감 트랜지션 적절하게 섞어"):
            //   긴 파노라마 · 대결 컷을 쪼개 구간마다 배색을 바꾸고(원색 → 청록 돌림 → 달밤 / 달밤 → 황토 판화)
            //   구간 사이는 물감 덮기(Paint)와 조각 섞기(Mix) + 글리치 박자(흔들림 ≥ 14)를 번갈아 → 책장이 넘어갈 때마다 다른 판본으로 찍힌 듯
            T(F(S(1.2f, "이불 속에서 그림책을 펼친 아이 (실루엣)", MTrans.Paint, 0.7f, new[] { W("book_boy", 0.8f, 0.004f, 0f), LG(Ghost(W("book_page1", 1.1f, 0.01f), 0.3f, 0.4f, 0.9f), MGrade.Negative), Ghost(W("dial_face", 1.2f, 0.014f), 0.12f, 0f, 0.35f) },
              new(-90, 20), new(60, -20), 1.06f, 1.26f, -4, 3, 4, 1.12f, 1, new[] { Q(0.25f, "e4_tick_1", 2), Q(0.55f, "e4_tick_2", 15), Q(0.8f, "e4_tick_1", 2) }),
              (0.55f, 0.1f, MGrade.Cyanotype)), MGrade.OldNegative),
            G(S(0.32f, "그림책 표지로 크래시 줌 (달밤)", MTrans.Mix, 0.2f, new[] { W("book_boy", 0.8f, 0.006f) }, new(60, -20), new(-980, 420), 1.3f, 2.4f, 4, -8, 9, 1, 1,   // 표지의 고양이 실루엣 쪽으로
              new[] { Q(0f, "intro_whoosh_1", 16) }), MGrade.Moon),
            // 파노라마 ①: 원래 노을빛으로 그려지기 시작
            S(0.75f, "펼친 그림책 ① 노을", MTrans.Zoom, 0.2f, new[]
              {
                  Painted(W("book_page1", 1f, 0.003f, 0.002f), 0.45f, 0.85f, R, 0.16f),
                  Ghost(W("ws_bg", 0.7f, 0.014f), 0.16f, 0.05f, 0.6f),
              }, new(360, 30), new(130, 15), 1.45f, 1.49f, 3, 1, 4, 1, 2,
              new[] { Q(0.1f, "e4_tick_1", 2), Q(0.6f, "e4_tick_2", 2) }),
            // ② 물감이 덮었다 걷히면 같은 책장이 청록으로 (노을 → 청록 하늘)
            T(G(S(0.6f, "펼친 그림책 ② 청록", MTrans.Paint, 0.34f, new[]
              {
                  Painted(W("book_page1", 1f, 0.003f, 0.002f), 0.8f, 1.05f, R, 0.16f),
                  LG(Ghost(W("book_boy", 0.8f, 0.014f), 0.18f, 0.2f, 1f), MGrade.Negative),
              }, new(130, 15), new(-120, 0), 1.49f, 1.52f, 1, -1, 4, 1, 2,
              new[] { Q(0.15f, "e4_tick_2", 2), Q(0.7f, "e4_tick_1", 15) }), MGrade.Shift(150)), MGrade.Sepia),
            // ③ 조각이 엇갈리며(글리치) 달밤 판본으로 — 오른쪽 고양이 실루엣
            G(S(0.65f, "펼친 그림책 ③ 달밤의 고양이", MTrans.Mix, 0.26f, new[]
              {
                  Painted(W("book_page1", 1f, 0.003f, 0.002f), 1.0f, 1.25f, R, 0.16f),
                  Ghost(W("ws_bg", 0.7f, 0.014f), 0.14f, 0.3f, 1f),
              }, new(-120, 0), new(-360, -20), 1.52f, 1.56f, -1, -3, 4, 1, 2,
              new[] { Q(0f, "e4_tick_2", 15), Q(0.85f, "shot_1", 8) }), MGrade.Moon),
            G(Dart("book_old", "book_page1", 0.12f, -10, -14, 1.4f, 1.5f, new(-60, 0), new(60, 0), "heart", 14), MGrade.Verdigris),
            // 무법자와 대결: 악당 쪽이 먼저 그려지고(달밤) → 물감이 덮고 걷히며 고양이 쪽은 황토 판화로
            G(S(0.8f, "무법자들 (그려지며 훑음 · 달밤)", MTrans.Mix, 0.22f, new[]
              {
                  Painted(W("book_page2", 1f, 0.003f, 0.002f), 0.2f, 0.75f, R, 0.16f),
                  Ghost(W("book_boy", 0.8f, 0.012f), 0.18f, 0.4f, 1f),
              }, new(380, 10), new(10, 5), 1.5f, 1.55f, -4, 0, 5, 1, 2,
              new[] { Q(0f, "e4_tick_2", 15), Q(0.55f, "e4_tick_1", 3) }), MGrade.Moon),
            T(G(S(0.8f, "맞서는 고양이 (그려지며 훑음 · 황토 판화)", MTrans.Paint, 0.3f, new[]
              {
                  Painted(W("book_page2", 1f, 0.003f, 0.002f), 0.7f, 1.25f, R, 0.16f),
                  LG(Ghost(W("wind_child", 1.15f, 0.012f), 0.2f, 0.3f, 1f), MGrade.Negative),
              }, new(10, 5), new(-360, 0), 1.55f, 1.6f, 0, 4, 5, 1, 2,
              new[] { Q(0.2f, "e4_tick_1", 3), Q(0.75f, "e4_tick_2", 14) }), MGrade.Ochre), MGrade.Moon),
            // 탕! — 고양이 ↔ 악당 (셀처럼 끊어서, 물감 글리치)
            // 총성마다 화면이 음화로 뒤집힘 · 악당 컷은 핏빛 두 색 · 다시 고양이는 먹 판화 · 마지막 쌍권총은 청사진 → 바랜 음화
            S(0.2f, "총을 뽑은 고양이 실루엣", MTrans.Cut, 0, new[] { W("book_page2", 1f, 0.002f) }, new(-360, 40), new(-430, 70), 2.0f, 2.4f, 8, 12, 9, 1, 1, new[] { Neg(0f, "shot_1", 20, 0.12f) }),
            G(S(0.16f, "무법자들", MTrans.Cut, 0, new[] { W("book_page2", 1f, 0.002f) }, new(560, 40), new(610, 50), 1.9f, 2.05f, -12, -14, 10, 1, 1, new[] { Q(0f, "shot_1", 16, 0.06f) }), MGrade.Blood),
            G(S(0.16f, "총을 뽑은 고양이 실루엣", MTrans.Cut, 0, new[] { W("book_page2", 1f, 0.002f) }, new(-400, 60), new(-420, 60), 2.3f, 2.5f, -6, -9, 10, 1, 1, new[] { Q(0f, "shot_1", 18, 0.08f) }), MGrade.Ink),
            F(G(S(0.4f, "쌍권총 고양이 실루엣", MTrans.Mix, 0.14f, new[] { W("book_page2", 1f, 0.002f), Ghost(W("wind_child", 1.2f, 0.01f), 0.25f, 0.2f, 1f) },
              new(-380, 50), new(-320, 30), 2.5f, 1.95f, 6, 0, 6, 1, 1, new[] { Q(0f, "shot_1", 22, 0.12f), Q(0.5f, "e4_tick_1", 15) }), MGrade.Cyanotype), (0.5f, 0.2f, MGrade.OldNegative)),

            // ══ 4. 현재: 같은 그림책이 낡은 채 병상 협탁에 — 어린 시간과 겹침
            T(S(1.3f, "병상 협탁의 낡은 그림책", MTrans.Paint, 0.65f, new[] { W("book_old", 0.8f, 0.004f, 0.002f), LG(Ghost(W("book_boy", 1.05f, 0.01f), 0.35f, 0.05f, 0.45f), MGrade.Sepia), Ghost(W("ecg_wash", 0.9f, 0.012f), 0.25f, 0.5f, 0.95f) },
              new(60, -20), new(-50, 10), 1.12f, 1.26f, 2, -2, 3, 0.84f, 1, new[] { Q(0.15f, "heart", 3), Q(0.6f, "heart", 3) }), MGrade.Negative),
            Dart("ecg_wash", "book_page2", 0.13f, -10, -6, 1.35f, 1.45f, new(140, 0), new(20, 0), "heart", 14),
            // 녹청 형광등 복도 (따로 그린 다른 배색 컷): 실려 가며 올려다본 천장
            S(0.7f, "실려 가며 올려다본 녹청 복도", MTrans.Whip, 0.1f, new[] { W("teal_corridor", 0.9f, 0.004f, 0.01f), LG(Ghost(W("book_old", 1.15f, 0.012f), 0.2f, 0.3f, 1f), MGrade.Negative) },
              new(0, 200), new(0, -220), 1.15f, 1.35f, -9, -4, 10, 1.15f, 2, new[] { Q(0f, "heart", 10), Q(0.4f, "heart", 8), Neg(0.75f, "heart", 16, 0.1f) }),
            Dart("wind_old", "book_old", 0.12f, 12, 16, 1.3f, 1.4f, new(-80, 0), new(40, 0), "e4_tick_1", 10),
            // 바늘 없는 문자판 + 따로 얹은 바늘: 초침 55 → 57 째깍
            S(1.1f, "회중시계: 째깍이는 초침", MTrans.Zoom, 0.2f, Dial("dial_face", null, 0, 0, 55, 57, 2f / 1.1f * 1.01f),
              O, O, 1.0f, 1.14f, 0, -3, 2.5f, 1, 1, new[] { Q(0f, "e4_tick_1", 3), Q(0.5f, "e4_tick_2", 3), Q(0.98f, "e4_tick_1", 3) }),
            // 레퀴엠식 끼어들기 (현재 · 과거 · 그림책 무작위, 겹쳐 비침)
            //   (배색도 컷마다 뒤섞임: 음화 · 세피아 · 청사진 · 따로 그린 분필 회중시계 · 녹청 복도)
            Flick("wind_child", 9, 1.3f, "heart", "book_page1"), G(Flick("book_old", -11, 1.35f, "e4_tick_2"), MGrade.Negative), G(Flick("boy_watch", 7, 1.28f, "heart", "ecg_wash"), MGrade.Sepia),
            Chalk(0.4f, 1.3f, 30, 1.04f, 1.14f, -5, new[] { Q(0f, "e4_tick_1", 6), Q(0.5f, "e4_tick_2", 6) }),
            G(Flick("book_page2", -13, 1.6f, "shot_1"), MGrade.Blood), Flick("crown_old", 12, 1.4f, "e4_tick_1", "wind_child"), G(Flick("book_boy", -8, 1.5f, "heart"), MGrade.Cyanotype),
            Flick("teal_corridor", 11, 1.3f, "heart", "dial_face"),
            G(Flick("hands_overlap", 10, 1.4f, "e4_tick_2", "book_old"), MGrade.OldNegative), Flick("ws_bg", -14, 1.5f, "heart", "dial_face"), Flick("night_ws", 8, 1.35f, "e4_tick_2"),

            // ══ 5. 박동마다 줌이 확확 들어가며 심장의 피가 물감처럼 번지고 초침이 한 칸씩
            //   박동 = 한 번씩 화면이 음화로 뒤집힘(Neg) · 박동 사이 끼어드는 컷은 청사진 · 핏빛
            Beat("dial_face", "bleed1", 57, 58, 1.0f, 1.14f, 0.5f, 10, 0.05f, -3),
            G(Dart("book_page2", "bleed1", 0.1f, 14, 10, 1.7f, 1.8f, new(-300, 40), new(-340, 40), "shot_1", 16), MGrade.Blood),
            F(Beat("bleed1", "bleed2", 58, 59, 1.18f, 1.36f, 0.48f, 14, 0.08f, 4), (0.55f, 0.07f, MGrade.Negative)),
            G(Dart("wind_child", "bleed2", 0.1f, -12, -16, 1.4f, 1.5f, new(60, 0), new(-60, 0), "heart", 14), MGrade.Cyanotype),
            S(0.6f, "59초에서 떨리는 초침", MTrans.Cut, 0, Dial("bleed2", null, 0, 0, 59, 59, 0, 12f), O, O, 1.4f, 1.55f, -6, 6, 8, 1, 1,
              new[] { Neg(0f, "heart", 16, 0.1f), Q(0.55f, "heart", 10) }),
            // 청사진 심장 시계 (따로 그린 다른 배색 컷)
            // 청사진 심장 셀: 이완 ↔ 수축이 박동마다 뚝뚝 (점점 빨라짐)
            Heart(false, 0.14f, 8, 1.2f, 12), Heart(true, 0.1f, 8, 1.24f, 18), Heart(false, 0.11f, 9, 1.28f, 10), Heart(true, 0.09f, 10, 1.32f, 20, MGrade.Negative),
            Beat("bleed2", "bleed3", 59, 59, 1.6f, 1.9f, 0.55f, 20, 0.1f, -8),
            G(S(0.24f, "시계 뒤로 번져 오는 심장 (근접)", MTrans.Cut, 0, new[] { W("heart_clock2", 1f, 0.002f), Ghost(W("book_page2", 1.2f, 0.012f), 0.2f, 0f, 1f) }, O, O, 1.2f, 1.5f, 10, 14, 10, 1, 1, new[] { Q(0f, "heart", 20, 0.08f) }), MGrade.Negative),
            Heart(true, 0.08f, -12, 1.6f, 20), Heart(false, 0.08f, -13, 1.66f, 12), Heart(true, 0.08f, -14, 1.72f, 22, MGrade.Blood),
            S(0.3f, "하나로 번진 시계와 심장 (근접)", MTrans.Cut, 0, new[] { W("heart_clock3", 1f, 0.002f), Ghost(W("book_old", 1.1f, 0.014f), 0.15f, 0f, 1f) }, O, O, 1.5f, 2.0f, -10, -15, 11, 1, 1, new[] { Q(0f, "heart", 24, 0.1f) }),
            S(1.1f, "피로 넘쳐흐르는 시계", MTrans.Cut, 0, Dial("bleed3", null, 0, 0, 59, 59, 0, 8f), O, O, 2.0f, 1.0f, 8, 0, 5, 1, 1,
              new[] { Q(0f, "heart", 18, 0.06f), Q(0.5f, "heart", 8), Q(0.9f, "heart", 4) }),
            // 심전도가 물에 번져 흘러내림 — 카메라를 따라 그려지며 (박동이 느려짐)
            T(S(1.0f, "물에 번지는 심전도", MTrans.Paint, 0.6f, new[] { Painted(W("ecg_wash", 1f, 0.002f, 0.004f), 0.2f, 1.3f, R, 0.2f), Ghost(W("bleed3", 1.1f, 0.012f), 0.25f, 0.05f, 0.55f), Ghost(W("book_old", 0.8f, 0.012f), 0.16f, 0.45f, 1f) },
              new(240, 0), new(-240, -10), 1.2f, 1.28f, 5, 1, 4, 1, 2, new[] { Q(0.1f, "heart", 6), Q(0.6f, "heart", 3) }), MGrade.Blood),

            // ══ 6. 엇갈리는 기억이 휙휙 → 그림책 속 고양이가 책장 밖으로 걸어 나옴 (고양이는 어디서 왔나)
            G(Dart("book_boy", "wind_old", 0.12f, 10, 14, 1.3f, 1.4f, new(-90, 0), new(30, 0), "e4_tick_1", 10, MTrans.Flash), MGrade.Sepia),
            Ink(1, 0.06f, -8, 1.3f, "e4_tick_2", 12), Ink(2, 0.06f, -8, 1.33f, "e4_tick_1", 12),
            G(Dart("book_page2", "dial_face", 0.11f, -14, -10, 1.6f, 1.7f, new(-340, 30), new(-300, 30), "shot_1", 16), MGrade.Negative),
            Child(1, 0.07f, new(0, 20), new(0, 10), 1.3f, 1.34f, new[] { Q(0f, "heart", 12) }), Child(2, 0.08f, new(0, 10), new(0, -10), 1.36f, 1.42f, new[] { Q(0f, "e4_tick_1", 12) }),
            G(Dart("boy_watch", "bleed2", 0.11f, 9, 5, 1.3f, 1.42f, new(80, 0), new(-40, 0), "heart", 12), MGrade.Verdigris),
            Chalk(0.16f, 3f, 34, 1.3f, 1.4f, -10, new[] { Q(0f, "e4_tick_1", 12) }),
            G(Dart("wind_child", "book_old", 0.11f, -12, -16, 1.35f, 1.45f, new(-60, 0), new(60, 0), "e4_tick_2", 12), MGrade.Cyanotype),
            T(S(0.9f, "펼친 그림책 (다시)", MTrans.Dissolve, 0.18f, new[] { W("book_page1", 1f, 0.004f, 0.002f), Ghost(W("book_old", 0.9f, 0.012f), 0.25f, 0.05f, 0.55f), Ghost(W("hands_overlap", 1.1f, 0.014f), 0.15f, 0.5f, 1f) },
              new(-200, 10), new(-280, 0), 1.35f, 1.48f, 2, -2, 3, 1, 1, new[] { Q(0.3f, "heart", 3) }), MGrade.OldNegative),
            S(2.2f, "책장 밖으로 걸어 나오는 고양이", MTrans.Zoom, 0.25f, new[]
              {
                  Painted(W("book_rise", 1f, 0.004f, 0.0f), 0.0f, 1.25f, Up, 0.2f),   // 아래(책장)에서 위(떠오른 고양이)로 번져 그려짐
                  Ghost(W("book_page1", 1.05f, 0.01f), 0.35f, 0f, 0.3f),
                  Ghost(W("hands_overlap", 0.8f, 0.014f), 0.18f, 0.55f, 1f),
              }, new(0, -100), new(0, 70), 1.15f, 1.34f, -2, 2, 2.5f, 1.12f, 0,
              new[] { Q(0.1f, "story_dream", 1), Q(0.5f, "heart", 4), Q(0.88f, "e4_bell_2", 8, 0.05f) }),
            S(0.3f, "떠오른 고양이로 크래시 줌", MTrans.Cut, 0, new[] { W("book_rise", 1f, 0.003f) }, new(0, 70), new(-60, -330), 1.34f, 2.2f, 2, -7, 8, 1, 1,
              new[] { Neg(0f, "shot_1", 18, 0.14f) }),

            // ══ 7. 아이 손이 노인 손 위에 겹쳐 같은 시계를 쥠 → 녹아내리는 11:59 (셀 단계)
            //   빠져나가는 크래시 줌은 세피아로 바래며 · 겹친 손 사이로 음화 한 번 · 녹는 시계는 먹 → 원래 색으로 돌아와 끝
            T(F(S(1.9f, "노인 손 위에 겹친 아이 손", MTrans.Paint, 0.8f, new[] { W("hands_overlap", 1f, 0.006f, 0.0f), LG(Ghost(W("book_old", 0.7f, 0.012f), 0.25f, 0.05f, 0.45f), MGrade.Sepia), LG(Ghost(W("boy_watch", 0.6f, 0.012f), 0.2f, 0.4f, 0.8f), MGrade.Negative) },
              new(0, -14), new(0, 14), 1.02f, 1.16f, 0, 0, 1.5f, 1.08f, 0, new[] { Q(0.15f, "heart", 2), Q(0.6f, "heart", 2) }), (0.6f, 0.08f, MGrade.Negative)), MGrade.Sepia),
            G(Dart("book_rise", "hands_overlap", 0.12f, 8, 4, 1.25f, 1.32f, new(40, 0), new(-30, 0), "heart", 8), MGrade.Cyanotype),
            T(G(S(0.55f, "녹기 시작하는 11:59", MTrans.Dissolve, 0.2f, new[] { W("melt_a", 1f, 0.003f), Ghost(W("bleed3", 1.1f, 0.012f), 0.18f, 0f, 0.6f) }, new(0, 20), new(0, 10), 1.04f, 1.08f, 0, 0, 2, 1, 0,
              new[] { Q(0.3f, "e4_tick_1", 2) }), MGrade.Ink), MGrade.Negative),
            S(0.35f, "녹아내리는 11:59", MTrans.Cut, 0, new[] { W("melt_b", 1f, 0.003f), Ghost(W("book_page1", 1.1f, 0.014f), 0.15f, 0f, 1f) }, new(0, 10), new(0, 0), 1.08f, 1.12f, 0, 0, 2, 1, 2,
              new[] { Neg(0f, "heart", 6, 0.1f) }),
            S(1.8f, "녹아 흘러내린 11:59", MTrans.Cut, 0, new[] { W("melt_1159", 1f, 0.003f, 0.007f), Ghost(W("book_rise", 1.1f, 0.014f), 0.14f, 0.35f, 1f) }, new(0, 0), new(0, -40), 1.12f, 1.24f, 0, 0, 1, 1, 0,
              new[] { Q(0.05f, "heart", 3), Q(0.6f, "e4_tick_2", 1) }),
            new MShot { dur = 0.9f, tint = Paper, tin = MTrans.Dissolve, tinDur = 0.8f },
        };
    }
}
