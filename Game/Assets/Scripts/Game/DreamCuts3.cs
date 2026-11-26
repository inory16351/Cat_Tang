// 꿈3 '11:59의 기원' (내부 키 dream5 · 4장 클리어 · 2026-11-25 사용자 확정, Art/tools/specs/ch4_story_spec.md "2. 꿈3")
//   흑백 목판화(리노컷) + 빨강 한 점(실 · 숄 · 분침) · 자막 없음 · 곡 = Resources/Audio/story_dream3 (Gemini 원곡 56.7초를 29.7초로 편집, Art/tools/story_dream3_cut.py)
//   그녀가 떠난 밤 시계공은 공방 시계를 하나씩 멈췄고(옛 풍습) 마지막 괘종시계의 빨간 분침을 11:59에서 손가락으로 붙잡음 → 그때부터 그의 시간도 멈춤
//   → 계절이 지나도 그대로 → 지금 병상 머리맡 시계도 11:59 → 그림책 고양이가 그 바늘 위에서 멈춤을 지킴 → 빨간 바늘 극접사 · 째깍 · 암전
// v1 (2026-11-25): 컷을 이어 뽑아 셀 애니메이션, 약 57초
// v2 (2026-11-26 사용자: "영상 템포가 너무 느리고 반복적으로 비슷한 배색과 장면이 나오다 보니 지루함", "시간도 다른 꿈처럼 30초 정도로") →
//   약 30초 · 박자(약 0.4 / 0.8초)에 맞춘 하드컷 · 구간마다 판을 바꿈: A 도입 크림 → B 빠른 교차(크림 ↔ 흑백 음화 _neg, 박자마다) → C 11:59 빨강 바탕 한 장
//   → D 시간 흐름 밤 회색(_night, 여기만 디졸브) → E 끝 크림. 시계 얼굴 정면은 11:59 순간 · 병상 · 극접사에만 (진자는 벽 그림자 · 시계는 바닥 그림자 · 톱니 · 태엽 열쇠로)
//   · 카메라 문법을 섞음: 급줌 · 빠른 기울기 패닝 · 초점 이동 · 정지 프레임 · 분할 화면 · 거꾸로 도는 톱니 · 옆으로 미끄러지는 트래킹 · 줌 관통 — 느린 푸시인 반복 X, 스와이프 X
//   · 선 떨림(Boil): 같은 그림을 살짝 다르게 뽑은(또는 _j = 스크립트로 살짝 일그러뜨린) 2장을 초당 12번 교대 (비 창 · 빨강 11:59 · 빈 의자 · 고양이 · 극접사)
// 배색 판본 = docs/tools/dream5_palettes.py (_neg 흑백 음화 · _night 밤 회색, 빨강은 빨강 그대로 · split_hands 좌우 분할 합성), 빨강 바탕 red_1159는 ChatGPT로 따로
// 그림 = Resources/Story/Dreams/dream5/*.jpg (Claude 내장 브라우저 ChatGPT 리노컷 1672x941 → 1450 JPG, 원본 Art/story_src/dream5/, 정리 docs/tools/dream5_import.py)
//   그림이 아직 없는 컷은 재생기가 컷 설명(desc)을 자리 표시로 띄움. v1 그림 중 wall · season · chairs · veil_a/b · pend · minute_a 는 v2 컷에서 뺌(파일은 남김)
using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    public static partial class DreamCuts
    {
        /// <summary>꿈 n의 전용 곡 이름 (Resources/Audio/). 꿈3(내부 dream5)만 이름이 다름</summary>
        public static string SongOf(int n) => n == 5 ? "story_dream3" : n == 6 ? "story_dream4" : n == 7 ? "" : "story_dream" + n;   // 7 = 엔딩 병실: 곡 없이 심전도만

        /// <summary>꿈마다 재생기 효과 조정 (PlayDream이 기본값을 넣은 뒤). 꿈3 리노컷: 무작위 배색(세피아 · 청사진) · 물감 글리치 · 물감 덮기 전환을 끔 — 흑백 + 빨강 유지</summary>
        public static void Tune(int n, StoryMontage mt)
        {
            if ((n != 5 && n != 6 && n != 7) || !mt) return;
            mt.echo = 0.24f;            // 잔상은 조금 더 (몽환)
            mt.glitch = 0f;             // 색 갈라짐 글리치 X (빨강/청록 · 장미/보라가 섞임) → 음화 끼어들기 · 느린 셔터로
            mt.paintGlitch = false;
            mt.gradeChance = 0f; mt.gradeGlitch = 0f;
            mt.mixTrans = 0f;           // 전환은 컷 목록에 적은 대로 (디졸브 · 줌 관통 · 끊기)
        }

        static readonly Color LinoPaper = new(0.92f, 0.9f, 0.86f);
        /// <summary>흑백 음화: 두 색 판화로 밝기만 뒤집음 (MGrade.Negative는 빨강을 청록으로 뒤집음)</summary>
        static MGrade InkNeg => new() { duo = 1f, duoA = new Color(0.95f, 0.93f, 0.88f), duoB = new Color(0.03f, 0.03f, 0.04f), contrast = 1.3f };

        // ---- 리노컷 도우미: 또렷한 칼자국 · 가장자리 번짐 약하게 · 채도 그대로(빨강 살림) ----
        static MLayer Lp(string img, float depth = 1f, float b0 = 0.0008f, float b1 = -1) =>
            new MLayer { img = img, depth = depth, blur0 = b0, blur1 = b1 < 0 ? b0 : b1, edge = 0.008f, sat = 1f };

        /// <summary>필름 결: 먼지 · 긁힘 판 세 장을 초당 약 12번 뒤섞어 넘김 (카메라를 따라가지 않음)</summary>
        static MLayer Grain(float dur, string seedKey)
        {
            int n = Mathf.Max(1, Mathf.CeilToInt(dur * 12f));
            var cels = new string[n];
            int h = 0; foreach (var c in seedKey) h = h * 31 + c;
            var rnd = new System.Random(h);
            int prev = -1;
            for (int i = 0; i < n; i++) { int k; do k = rnd.Next(3); while (k == prev); prev = k; cels[i] = "grain_" + (char)('a' + k); }
            return new MLayer { img = cels[0], cels = cels, depth = 0f, alpha = 0.55f, sat = 1f };
        }

        static MShot L1(float dur, string desc, string img, Vector2 p0, Vector2 p1, float z0, float z1, float r0, float r1, float hand,
                        (float, string, float, float)[] cues = null, MTrans tin = MTrans.Cut, float tinDur = 0f, int ease = 2, float shutter = 0,
                        MGrade g = null, float b0 = 0.0008f, float b1 = -1, bool dark = false)
        {
            var s = S(dur, desc, tin, tinDur, new[] { Lp(img, 1f, b0, b1), Grain(dur, img + desc) }, p0, p1, z0, z1, r0, r1, hand, 1, ease, cues);
            s.shutter = shutter; s.grade = g;
            s.tint = dark ? Color.black : LinoPaper;
            return s;
        }

        /// <summary>셀 연속: 카메라 이동(p · z · r)이 셀을 넘어 이어지고 그림만 바뀜. between = 셀 사이 전환(끊기 · 디졸브), 첫 셀은 tin</summary>
        static IEnumerable<MShot> LCels(string desc, string[] imgs, float[] durs, Vector2 p0, Vector2 p1, float z0, float z1, float r0, float r1, float hand,
                                        float shutter = 0, MTrans tin = MTrans.Cut, float tinDur = 0, MTrans between = MTrans.Cut, float betweenDur = 0,
                                        (int cel, (float, string, float, float)[] cues)[] cues = null, int ease = 2, float b0 = 0.0008f, float b1 = -1)
        {
            float total = 0; foreach (var d in durs) total += d;
            float acc = 0;
            for (int i = 0; i < imgs.Length; i++)
            {
                float a = acc / total, b = (acc + durs[i]) / total; acc += durs[i];
                (float, string, float, float)[] cc = null;
                if (cues != null) foreach (var (cel, q) in cues) if (cel == i) cc = q;
                yield return L1(durs[i], desc + " (셀 " + (i + 1) + ")", imgs[i], Vector2.Lerp(p0, p1, a), Vector2.Lerp(p0, p1, b), Mathf.Lerp(z0, z1, a), Mathf.Lerp(z0, z1, b),
                                Mathf.Lerp(r0, r1, a), Mathf.Lerp(r0, r1, b), hand, cc, i == 0 ? tin : between, i == 0 ? tinDur : betweenDur, ease, shutter,
                                b0: i == 0 ? b0 : 0.0008f, b1: i == 0 ? b1 : -1);
            }
        }

        /// <summary>0.1초 기억 끼어들기: 다른 컷 그림이 더치 앵글 · 흑백 음화로 번쩍</summary>
        static MShot LFlash(string img, float dur, float rot, float z, MGrade g = null, float shake = 8) =>
            L1(dur, "끼어드는 기억: " + img, img, new(Random2(img) * 50, 0), new(-Random2(img) * 30, 0), z, z + 0.06f, rot, rot * 1.2f, 6, new[] { Q(0f, null, shake) }, g: g ?? InkNeg);

        static MShot Dark(float dur, (float, string, float, float)[] cues = null, MTrans tin = MTrans.Cut, float tinDur = 0) =>
            new MShot { dur = dur, tint = Color.black, cues = cues, tin = tin, tinDur = tinDur };

        static readonly Color NightInk = new(0.035f, 0.05f, 0.085f), RedGround = new(0.78f, 0.1f, 0.12f);

        /// <summary>선 떨림 · 루프: 그림 여러 장을 초당 fps번 차례로 교대하는 한 컷 (셀은 한 레이어에서 넘김 — 카메라는 끊기지 않음)</summary>
        static MShot Boil(float dur, string desc, string[] imgs, Vector2 p0, Vector2 p1, float z0, float z1, float r0, float r1, float hand,
                          (float, string, float, float)[] cues = null, MTrans tin = MTrans.Cut, float tinDur = 0f, int ease = 2, float shutter = 0,
                          float b0 = 0.0008f, float b1 = -1, Color? tint = null, float fps = 12f)
        {
            var s = L1(dur, desc, imgs[0], p0, p1, z0, z1, r0, r1, hand, cues, tin, tinDur, ease, shutter, null, b0, b1);
            int n = Mathf.Max(imgs.Length, Mathf.RoundToInt(dur * fps));
            var cels = new string[n];
            for (int i = 0; i < n; i++) cels[i] = imgs[i % imgs.Length];
            s.layers[0].cels = cels;
            if (tint.HasValue) s.tint = tint.Value;
            return s;
        }
        static MShot Tinted(MShot s, Color c) { s.tint = c; return s; }

        static MShot[] Dream5
        {
            get
            {
                var L = new List<MShot>();
                void A(params MShot[] s) => L.AddRange(s);
                var Z = Vector2.zero;

                // ══ A 도입 0~3.6초 (크림 판): 어둠 · 째깍 → 비 오는 창 (선 떨림) — 급줌 아웃으로 열고 빠르게 기울며 창에 비친 노인
                A(Dark(0.8f, new[] { Q(0.1f, "!e4_tick_1", 1), Q(0.55f, "!e4_tick_2", 1) }),
                  Boil(1.3f, "비 오는 공방 창 (선 떨림 · 급줌 아웃)", new[] { "rain_a", "rain_b" }, new(0, 20), new(-20, 0), 1.45f, 1.12f, -4, -1, 1.5f, tin: MTrans.Dissolve, tinDur: 0.3f, ease: 1, shutter: 0.25f),
                  Boil(1.5f, "창에 비친 노인 (선 떨림 · 초점 이동 · 빠른 기울기)", new[] { "rain_b", "rain_c" }, new(-20, 0), new(40, -10), 1.12f, 1.2f, -1, 2.5f, 2, b0: 0.006f, b1: 0.0008f, ease: 0, shutter: 0.2f));

                // ══ B 빠른 교차 3.6~14.0초 (박자 0.4 · 0.8초마다 하드컷, 크림 ↔ 흑백 음화)
                A(L1(0.8f, "풀리는 빨간 실 (빠른 기울기 패닝)", "hands_b", new(60, 0), new(-40, 0), 1.18f, 1.24f, 4, -2, 2, new[] { Q(0f, null, 6) }, ease: 1),
                  L1(0.4f, "실이 빠진 손목 (음화 · 정지)", "hands_c_neg", new(-20, 0), new(-20, 0), 1.3f, 1.3f, -3, -3, 0, new[] { Q(0f, null, 8) }),
                  L1(0.4f, "늘어진 손", "hands_c", new(30, -10), new(10, -10), 1.36f, 1.42f, 2, 3, 1.5f, ease: 1));
                A(Boil(1.6f, "벽에 비친 진자 그림자 왕복 (루프)", new[] { "pshadow_a", "pshadow_b" }, new(-30, 0), new(30, 0), 1.12f, 1.18f, 6, 8, 2, shutter: 0.5f, fps: 5f),
                  L1(0.4f, "그림자 손이 진자를 붙잡음 (음화 · 급줌)", "pshadow_c_neg", Z, new(0, -10), 1.5f, 1.22f, 7, 7, 1, new[] { Q(0f, null, 12, 0.04f) }, ease: 1));
                // 위에서 본 바닥: 시계 그림자가 하나씩 사라짐 — 카메라가 내려다보며 돎
                A(L1(0.4f, "바닥의 시계 그림자 (위에서 · 돌며)", "floor_a", Z, Z, 1.3f, 1.32f, 0, -6, 1, new[] { Q(0f, null, 5) }),
                  L1(0.4f, "그림자가 사라짐 (음화)", "floor_b_neg", Z, Z, 1.33f, 1.35f, -6, -12, 1, new[] { Q(0f, null, 6) }),
                  L1(0.6f, "마지막 그림자", "floor_c", Z, Z, 1.36f, 1.38f, -12, -16, 1, new[] { Q(0f, null, 7) }),
                  L1(0.2f, "끼어드는 비 창 (음화)", "rain_c_neg", new(40, 0), new(20, 0), 1.3f, 1.34f, -8, -9, 6, new[] { Q(0f, null, 10) }));
                A(L1(1.0f, "태엽 열쇠를 내려놓는 손 (초점 이동)", "key_hand", Z, new(-10, 0), 1.06f, 1.1f, 0, 0, 1, b0: 0.009f, b1: 0.0008f, ease: 0),
                  L1(0.4f, "천 덮인 거울 (음화 · 패닝)", "veil_c_neg", new(80, 0), new(-60, 0), 1.18f, 1.2f, -3, -3, 2, new[] { Q(0f, null, 6) }));
                {
                    // 멈춘 톱니 극접사: 톱니가 거꾸로 조금 돌다 멈춤 (레이어 회전) — 화면 밖이 안 비게 크게
                    var g = L1(1.0f, "멈춘 톱니 극접사 (거꾸로 돌다 멈춤)", "gear_ecu", Z, Z, 1.28f, 1.32f, 0, 0, 0.5f, new[] { Q(0.7f, null, 9) }, ease: 1);
                    g.layers[0].r0 = 0; g.layers[0].r1 = 9f;
                    A(g);
                }
                A(L1(1.0f, "분할: 진자를 잡은 손 | 분침을 누른 손가락 (정지)", "split_hands", Z, Z, 1.04f, 1.04f, 0, 0, 0, new[] { Q(0f, null, 10) }),
                  L1(0.4f, "분할 (음화)", "split_hands_neg", Z, Z, 1.06f, 1.06f, 0, 0, 0, new[] { Q(0f, null, 12, 0.04f) }),
                  L1(0.4f, "11:59로 밀리는 분침 (음화 · 급줌)", "minute_b_neg", new(420, -60), new(380, -40), 1.6f, 1.45f, -2, -2, 1, new[] { Q(0f, null, 8) }, ease: 1),
                  L1(0.2f, "끼어드는 고양이 (음화)", "cat_a_neg", new(200, 0), new(180, 0), 1.35f, 1.4f, 9, 10, 6, new[] { Q(0f, null, 10) }),
                  L1(0.8f, "분침을 눌러 붙잡는 손가락 (급줌)", "minute_c", new(200, -40), new(260, -120), 1.15f, 1.5f, -1, 2, 2, new[] { Q(0f, null, 10), Q(0.6f, null, 6) }, ease: 1, shutter: 0.25f));

                // ══ C 11:59 순간 14.0~14.95초: 빨강 바탕 + 검은 선 한 장 (화면 전체 빨강은 여기 한 번) · 한 박자 정지 프레임 + 섬광
                A(Boil(0.8f, "11:59 (빨강 바탕 · 정지 프레임)", new[] { "red_1159", "red_1159_b" }, Z, Z, 1.06f, 1.06f, 0, 0, 0, new[] { Q(0f, "!e4_tick_2", 16, 0.12f) }, tint: RedGround),
                  Dark(0.15f));

                // ══ D 시간 흐름 14.95~21.15초 (밤 회색 판, 여기만 디졸브): 창틀과 바닥에 드는 빛만으로 계절 타임랩스 (옆으로 미끄러지는 트래킹)
                //   → 노인의 등 너머 빈 의자의 빨간 숄 → 병상 머리맡 11:59 (줌 관통)
                foreach (var s in LCels("창빛 계절 타임랩스", new[] { "light_a_night", "light_b_night", "light_c_night", "light_d_night", "light_e_night", "light_f_night", "light_g_night" }, Even(7, 0.4f),
                                        new(-60, 0), new(60, 0), 1.08f, 1.08f, 0, 0, 0.5f, tin: MTrans.Dissolve, tinDur: 0.3f, between: MTrans.Dissolve, betweenDur: 0.2f))
                    A(Tinted(s, NightInk));
                A(Boil(1.6f, "노인의 등 너머 빈 의자의 빨간 숄 (선 떨림 · 초점 이동)", new[] { "back_chair_night", "back_chair_j_night" }, new(30, 0), new(-10, 0), 1.1f, 1.14f, -2, 1, 1,
                       tin: MTrans.Dissolve, tinDur: 0.4f, b0: 0.007f, b1: 0.0008f, ease: 0, tint: NightInk),
                  Tinted(L1(1.0f, "지금, 병상 (줌 관통 → 물러남)", "bed_a_night", Z, new(0, 10), 1.3f, 1.12f, 0, -1, 1, tin: MTrans.Zoom, tinDur: 0.35f, ease: 1), NightInk),
                  Tinted(L1(0.8f, "머리맡 시계도 11:59 (급줌)", "bed_b_night", new(0, -120), new(30, -280), 1.1f, 1.45f, 0, 0, 1.5f, new[] { Q(0.1f, null, 8) }, ease: 1), NightInk));

                // ══ E 끝 21.15~29.7초 (크림 판): 고양이가 바늘 위로 (급줌 시작 → 느리게) → 빨간 바늘 극접사 떨림 → 째깍 → 암전
                A(L1(0.5f, "바늘로 뛰어드는 고양이 (급줌)", "cat_a", new(300, 0), new(220, -40), 1.55f, 1.3f, 3, 3, 3, new[] { Q(0f, null, 12, 0.05f) }, ease: 1),
                  L1(0.5f, "바늘에 내려앉음", "cat_b", new(-150, -130), new(-170, -180), 1.35f, 1.45f, 2, 2, 3, new[] { Q(0f, null, 12) }, ease: 1),
                  Boil(2.2f, "바늘 위에서 멈춤을 지키는 고양이 (선 떨림)", new[] { "cat_c", "cat_c_j" }, new(-170, -220), new(-180, -300), 1.45f, 1.6f, 2, 0.5f, 2.5f, new[] { Q(0f, null, 8) }, ease: 0, shutter: 0.3f),
                  L1(1.85f, "빨간 바늘 극접사 (초점 이동 · 옆으로 미끄러짐)", "ecu_a", new(40, 0), new(-20, 0), 1.06f, 1.08f, 0, 0, 0.8f, b0: 0.006f, b1: 0.0008f, ease: 0),
                  Boil(1.25f, "바늘이 떨림 (선 떨림)", new[] { "ecu_a", "ecu_b" }, new(-20, 0), new(-24, -6), 1.08f, 1.12f, 0, 0.5f, 2.5f, new[] { Q(0.2f, null, 4), Q(0.6f, null, 6) }, shutter: 0.3f),
                  Dark(2.25f, new[] { Q(0f, "!e4_tick_1", 10) }));                                 // 합계 29.7초 = 곡 story_dream3 (29.7초), 째깍 = 곡의 마지막 째깍(약 27.45초)
                return L.ToArray();
            }
        }
    }
}
