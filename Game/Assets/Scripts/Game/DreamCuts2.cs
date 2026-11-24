// 꿈2 '불 꺼진 시계탑' (2026-11-24 사용자 확정): 구아슈 · 주제 '고칠 수 없는 시계' — 왜 죽음을 거부하는지
//   어린 날 멈춘 시계탑을 고치는 어른의 손 → 불이 다시 켜지고 탑 위에 그림책 고양이(아이의 상상) = "멈춘 시계는 고치면 다시 간다"
//   → 같은 자리의 늙은 손은 고치지 못하고, 마을 창불이 병원 창으로 바뀌며 하나씩 꺼짐 · 등불이 사그라듦 (= 의식이 꺼져 감)
//   → 어둠이 닥쳐 오고, 섬광 속에 고양이 실루엣 · 어둠의 존재 시점에서 총을 쏘며 물러나는 고양이 → 눈동자까지 핸드헬드로 확확 (= 시간과 사투)
// 연출 (사용자): 오프닝 · 꿈1과 겹치는 구도 금지(올려다보는 시계탑 · 병원 복도 · 태엽 손 X) · 컷마다 배색을 확 다르게 · 시간대가 다른 컷을 불규칙하게 섞음
//   · 오브젝트(손 · 시곗바늘)와 배경을 따로 · 같은 구도의 셀을 많이 이어 자연스러운 셀 애니메이션 · 아주 거친 붓터치 컷
//   · 어둠 장면은 거친 붓터치의 흑백 대비 + 느린 셔터(MShot.shutter: 앞 그림이 끌리며 번짐) · 눈동자 줌은 직선 X, 어둠의 시점 핸드헬드
// 그림 = Resources/Story/Dreams/dream2/*.png (크롬 ChatGPT 구아슈, 원본 Art/story_src/dream2/)
//   배색 판본 <이름>_<배색>.png = docs/tools/dream2_palettes.py (2026-11-24 사용자: 배색이 다 비슷함 → 컷마다 다른 두 색으로, 셀 묶음은 같은 배색), 곡 = Resources/Audio/story_dream2 (크롬 Gemini, 61초)
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CatTang
{
    public static partial class DreamCuts
    {
        // ---- 구아슈 도우미: 수채화보다 또렷 · 가장자리 번짐 약하게 ----
        static MLayer Gq(string img, float depth = 1f, float b0 = 0.0012f, float b1 = -1) =>
            new MLayer { img = img, depth = depth, blur0 = b0, blur1 = b1 < 0 ? b0 : b1, edge = 0.012f, sat = 1f };

        static MShot C2(float dur, string desc, MLayer[] layers, Vector2 p0, Vector2 p1, float z0, float z1, float r0, float r1, float hand,
                        (float, string, float, float)[] cues = null, MTrans tin = MTrans.Cut, float tinDur = 0f, int ease = 2, float shutter = 0, MGrade g = null)
        {
            var s = S(dur, desc, tin, tinDur, layers, p0, p1, z0, z1, r0, r1, hand, 1, ease, cues);
            s.shutter = shutter; s.grade = g;
            return s;
        }
        static MShot C1(float dur, string desc, string img, Vector2 p0, Vector2 p1, float z0, float z1, float r0, float r1, float hand,
                        (float, string, float, float)[] cues = null, MTrans tin = MTrans.Cut, float tinDur = 0f, int ease = 2, float shutter = 0, MGrade g = null) =>
            C2(dur, desc, new[] { Gq(img) }, p0, p1, z0, z1, r0, r1, hand, cues, tin, tinDur, ease, shutter, g);

        /// <summary>셀 연속: 하나의 카메라 이동(p0→p1 · z · r)을 셀마다 이어 받아 그림만 뚝뚝 바뀜 (같은 구도로 그린 셀)</summary>
        static IEnumerable<MShot> Cels(string desc, string[] imgs, float[] durs, Vector2 p0, Vector2 p1, float z0, float z1, float r0, float r1, float hand,
                                       string sfx = "e4_tick_1", float shake = 3, float shutter = 0, MGrade[] grades = null)
        {
            float total = 0; foreach (var d in durs) total += d;
            float acc = 0;
            for (int i = 0; i < imgs.Length; i++)
            {
                float a = acc / total, b = (acc + durs[i]) / total; acc += durs[i];
                yield return C1(durs[i], desc + " (셀 " + (i + 1) + ")", imgs[i], Vector2.Lerp(p0, p1, a), Vector2.Lerp(p0, p1, b), Mathf.Lerp(z0, z1, a), Mathf.Lerp(z0, z1, b),
                                Mathf.Lerp(r0, r1, a), Mathf.Lerp(r0, r1, b), hand, new[] { Q(0f, sfx, shake) }, shutter: shutter, g: grades != null && i < grades.Length ? grades[i] : null);
            }
        }
        static float[] Even(int n, float each) { var d = new float[n]; for (int i = 0; i < n; i++) d[i] = each; return d; }

        /// <summary>시간대가 다른 기억이 0.1초 남짓 끼어듦 (불규칙하게 섞음)</summary>
        static MShot Cut2(string img, float dur, float rot, float z, MGrade g = null, float shake = 10, float flash = 0, float shutter = 0) =>
            C1(dur, "끼어드는 기억: " + img, img, new(Random2(img) * 60, 0), new(-Random2(img) * 40, 0), z, z + 0.07f, rot, rot * 1.2f, 8, new[] { Q(0f, "heart", shake, flash) }, shutter: shutter, g: g);
        static float Random2(string k) { int h = 0; foreach (var c in k) h = h * 31 + c; return ((h & 0xff) / 255f) * 2 - 1; }   // 이름으로 고정된 -1~1 (재생마다 같게)

        /// <summary>볼트를 쥔 손 (손까지 한 장으로 그린 그림 — 2026-11-24 사용자: 오려 붙인 손이 부자연스러움 → 비네트 · 빛이 맞게 새로)</summary>
        static MShot Bolt(string img, float dur, float z, float rot, string sfx = "e4_tick_2", float shake = 6, MGrade g = null) =>
            C1(dur, "볼트를 돌리는 손: " + img, img, new(24, 0), new(-24, 0), z, z + 0.06f, rot, rot * 0.6f, 4, new[] { Q(0f, sfx, shake) }, g: g);
        /// <summary>늙은 손의 떨림: 같은 구도의 두 셀(a · b)을 잘게 번갈아 + 프레이밍이 미세하게 튐</summary>
        static IEnumerable<MShot> Tremble(string a, string b, int n, float each, float z, float rot)
        {
            for (int i = 0; i < n; i++)
                yield return C1(each, "떨리는 늙은 손 (셀 " + (i % 2 + 1) + ")", i % 2 == 0 ? a : b, new((i % 2 == 0 ? 6 : -6), (i % 3) - 1), new((i % 2 == 0 ? -4 : 4), 0),
                                z + 0.01f * i, z + 0.01f * i + 0.01f, rot + (i % 2 == 0 ? 0.6f : -0.6f), rot, 6, new[] { Q(0f, i == 0 ? "heart" : "e4_tick_1", 4) });
        }

        /// <summary>추적 원테이크 한 숏 (9초) — 2026-11-24 사용자 v2: 카메라 무빙이 없어 보이고 동작이 너무 빠름 →
        /// 고양이 동작을 늦추고(셀 약 0.21초) 총을 쏘는 셀마다 카메라가 반동으로 튐 · 고양이는 좌우로 피하며 물러나고 카메라는 한 박자 늦게 따라 꺾임 ·
        /// 어둠 터널 배경은 끊임없이 앞으로 빨려 듦(전진) · 카메라가 고양이의 후퇴보다 빨라 처음엔 천천히 → 점점 급하게 다가가 그대로 눈동자까지 · 느린 셔터 유지</summary>
        static MShot Chase()
        {
            const float Dur = 9f, CatEnd = 0.86f;               // 고양이 셀은 0.86까지, 그 뒤 눈 → 눈동자
            const string Seq = "abcdef";
            const int N = 37;                                    // 9 × 0.86 / 0.21
            var order = new List<string>(); var at = new List<float>();
            var cues = new List<(float, string, float, float)>();
            for (int i = 0; i < N; i++)
            {
                char c = Seq[i % 6];
                float u = CatEnd * i / N;
                order.Add("chase_cat_" + c); at.Add(u);
                // 쏘는 셀마다 카메라 반동 (섬광은 큰 발사 d · f에만 약하게 — 매번 터지면 화면이 하얗게 날아감)
                if (c != 'c') cues.Add(Q(u, "shot_1", c == 'f' ? 15 : c == 'd' ? 14 : 9, c == 'f' || c == 'd' ? 0.045f : 0f));
            }
            cues.Add(Q(0.88f, "heart", 16)); cues.Add(Q(0.95f, "shot_1", 17, 0.08f));
            // 따로 뽑은 티가 안 나게 (2026-11-24 사용자: 비네트 맞춰야): 가장자리를 배경 쪽으로 녹이고(물감 섞임) · 배경과 같은 흐림 · 흰 곳을 배경 명도로 낮춤
            var cat = Part("chase_cat_a", 560, new Vector2(0.5f, 0.5f), Vector2.zero, 1f, 0, 0, 0.0022f, 0.45f);
            cat.tint = new Color(0.9f, 0.9f, 0.93f); cat.edge = 0.006f;
            cat.cels = order.ToArray(); cat.celAt = at.ToArray();
            // 보이는 크기 = 카메라가 따라잡는 정도: 처음엔 거의 그대로 → 점점 급하게 (얼굴이 화면을 채움)
            cat.scaleKeys = new[] { new Vector2(0, 0.46f), new Vector2(0.3f, 0.56f), new Vector2(0.55f, 0.72f), new Vector2(0.7f, 0.95f), new Vector2(0.8f, 1.45f), new Vector2(0.86f, 2.2f), new Vector2(0.9f, 2.8f) };
            // 좌우로 피하며 물러남 (끝에선 얼굴이 화면 가운데 오게 아래로)
            cat.path = new[] { new Vector3(0, 0, 0), new Vector3(0.14f, -230, 10), new Vector3(0.29f, 210, -10), new Vector3(0.44f, -190, 0), new Vector3(0.58f, 160, -20),
                               new Vector3(0.7f, -90, -60), new Vector3(0.8f, 20, -130), new Vector3(0.88f, 0, -175), new Vector3(1f, 0, -180) };
            cat.fade = new[] { new Vector2(0, 1), new Vector2(0.86f, 1), new Vector2(0.9f, 0), new Vector2(1, 0) };
            var tend = Part("chase_tendrils", 1300, new Vector2(0.5f, 0.5f), Vector2.zero, 1.3f, 0, 0, 0.0045f, 0.5f);   // 카메라 앞이라 초점 밖으로 흐림
            tend.tint = new Color(0.85f, 0.85f, 0.88f);
            tend.scaleKeys = new[] { new Vector2(0, 1.35f), new Vector2(0.25f, 1.15f), new Vector2(0.5f, 1.28f), new Vector2(0.75f, 1.02f), new Vector2(1, 0.9f) };   // 어둠이 숨 쉬듯 조여 옴
            tend.fade = new[] { new Vector2(0, 0.85f), new Vector2(0.82f, 1), new Vector2(0.9f, 0.35f), new Vector2(1, 0.2f) };
            // 터널 배경 5장 (2026-11-24 사용자: 이동에 맞춰 배경도 바뀌어야 · 화각이 같아서 줌 느낌이 안 남)
            //   광각 터널 → 왼쪽으로 꺾임 → 오른쪽으로 꺾임 → 좁아진 망원 → 아주 가까운 소용돌이. 카메라가 꺾이는 때에 맞춰 번지듯 넘어가고,
            //   장마다 나타난 뒤 계속 커지며(전진) 살짝 돌아감. 달리 줌(dolly)으로 배경만 더 당겨 화각이 좁아지는 느낌
            string[] bgImg = { "chase_bg", "chase_bg2", "chase_bg3", "chase_bg4", "chase_bg5" };
            float[] bgIn = { 0f, 0.18f, 0.33f, 0.53f, 0.72f }, bgOut = { 0.24f, 0.39f, 0.59f, 0.78f, 1.01f };
            var bgs = new List<MLayer>();
            for (int i = 0; i < bgImg.Length; i++)
            {
                var l = Gq(bgImg[i], 0.7f, 0.0015f, 0.005f);
                float i0 = bgIn[i], i1 = Mathf.Min(1, bgIn[i] + 0.06f), o0 = bgOut[i] - 0.06f, o1 = bgOut[i];
                l.fade = i == 0 ? new[] { new Vector2(0, 1), new Vector2(o0, 1), new Vector2(o1, 0) }
                       : i == bgImg.Length - 1 ? new[] { new Vector2(0, 0), new Vector2(i0, 0), new Vector2(i1, 1), new Vector2(1, 1) }
                       : new[] { new Vector2(0, 0), new Vector2(i0, 0), new Vector2(i1, 1), new Vector2(o0, 1), new Vector2(o1, 0) };
                l.scaleKeys = new[] { new Vector2(0, 1f), new Vector2(i0, 1f), new Vector2(Mathf.Min(1, o1), i == bgImg.Length - 1 ? 1.8f : 1.4f) };
                l.r0 = i % 2 == 0 ? -4 : 4; l.r1 = -l.r0 * 2;
                bgs.Add(l);
            }
            var eyeA = Gq("eye_a", 0f); eyeA.z0 = 1.05f; eyeA.z1 = 1.35f; eyeA.fade = new[] { new Vector2(0, 0), new Vector2(0.84f, 0), new Vector2(0.89f, 1), new Vector2(0.93f, 1), new Vector2(0.96f, 0) };
            var eyeB = Gq("eye_b", 0f); eyeB.z0 = 1.0f; eyeB.z1 = 1.5f; eyeB.fade = new[] { new Vector2(0, 0), new Vector2(0.92f, 0), new Vector2(0.96f, 1), new Vector2(1, 1) };
            var shot = S(Dur, "어둠의 시점 원테이크: 쏘며 물러나는 고양이를 쫓아 눈동자까지", MTrans.Cut, 0, bgs.Concat(new[] { cat, tend, eyeA, eyeB }).ToArray(),
                         Vector2.zero, Vector2.zero, 1, 1, 0, 0, 18, 1, 2, cues.ToArray());
            // 카메라: 고양이 좌우 피하기를 한 박자 늦게 따라 꺾임 (반대로 밀려 배경이 크게 흐름) + 계속 조금씩 줌 인 → 끝에서 확
            shot.camPath = new[]
            {
                new Vector4(0, 0, 0, 1.0f), new Vector4(0.1f, 40, 10, 1.03f), new Vector4(0.2f, 190, -10, 1.06f), new Vector4(0.35f, -170, 10, 1.1f),
                new Vector4(0.5f, 160, -10, 1.15f), new Vector4(0.64f, -130, 20, 1.22f), new Vector4(0.75f, 70, 40, 1.32f), new Vector4(0.84f, -10, 60, 1.5f),
                new Vector4(0.92f, 0, 20, 1.75f), new Vector4(1f, 0, 0, 2.0f),
            };
            shot.rotPath = new[] { new Vector2(0, -6), new Vector2(0.18f, 9), new Vector2(0.34f, -11), new Vector2(0.5f, 10), new Vector2(0.66f, -9), new Vector2(0.8f, 6), new Vector2(0.9f, -3), new Vector2(1, 2) };
            shot.shutter = 0.62f;
            shot.dolly = 1.7f;                                   // 배경만 더 당겨짐 → 화각이 좁아지며 다가가는 느낌
            shot.tint = Color.black;
            return shot;
        }

        /// <summary>추적 원테이크 v3 (2026-11-24 사용자: 오려 붙이면 따로 노는 티를 못 없앰 → 배경과 고양이가 한 그림에 같이 있는 컷을 카메라 움직임대로 많이 뽑아 이어 붙임).
        /// 어둠의 시점으로 도망치는 고양이를 쫓음: 연속 프레임 run_01 → run_01b(중간) → run_02 … → run_12(눈) 를 한 레이어에서 셀로 넘기고,
        /// 그 위에 멈추지 않는 핸드헬드 · 조금씩 밀고 들어가는 카메라 + 느린 셔터(앞 프레임이 끌림)로 이음 → 끝은 눈동자 익스트림 클로즈업(eye_b)</summary>
        static MShot RunChase()
        {
            // v4 (2026-11-24 사용자): 얼굴 · 표정이 보이면 안 됨 → 강한 붓터치 실루엣(sil_01~14). 멀리선 눈도 없음 →
            //   가슴 위로 다가왔을 때 처음 눈에 작은 섬광 → 별처럼 번짐 → 그 빛 속으로 들어가 마지막 장에서만 눈동자 디테일
            var names = new List<string>();
            foreach (var br in new[] { "bridge_a", "bridge_b", "bridge_c" })   // 풍향계 위 고양이가 어둠 속으로 뛰어내림 → 터널로 (v5, 앞 컷과 이어지게)
                if (Resources.Load<Texture2D>("Story/Dreams/dream2/" + br)) names.Add(br);
            // 재생 순서 (2026-11-24 사용자: 2b가 2보다 멀리 그려져 끊겨 보임 → 2b 뺌)
            string[] order =
            {
                "sil_01", "sil_02", "sil_02c", "sil_03", "sil_04", "sil_05", "sil_06", "sil_06b", "sil_07", "sil_07b",   // 2b는 뺌 (2026-11-24 사용자: 갑자기 멀어지는 컷)
                "sil_08", "sil_08b", "sil_09", "sil_09b", "sil_10", "sil_11", "sil_12", "sil_12b", "sil_13", "sil_13b", "sil_13c", "sil_13d", "sil_14",   // 12b · 13b~d = 눈이 확대되는 중간 컷 (2026-11-24 사용자)
            };
            foreach (var k in order)
                if (Resources.Load<Texture2D>("Story/Dreams/dream2/" + k)) names.Add(k);
            if (names.Count == 0) return Chase();                       // 그림이 없으면 예전 원테이크
            const float CelEnd = 1f;                                      // 마지막 장(눈동자)까지 그림이 직접
            var at = new float[names.Count];
            for (int i = 0; i < at.Length; i++) at[i] = 0.92f * Mathf.Pow(i / (float)(at.Length - 1), 1.12f);   // 뒤로 갈수록 한 장이 조금씩 짧아짐 (가속), 마지막 장은 0.92부터 끝까지
            float dur = 0.4f * names.Count + 0.6f;                         // 마지막 눈동자는 조금 더 머묾
            var run = Gq(names[0], 1f, 0.0012f, 0.0025f);
            run.cels = names.ToArray(); run.celAt = at;
            
            // 사격 · 섬광 프레임에 반동 (이름 순서 기준 — 3 · 5 · 8 · 12 키 프레임)
            var cues = new List<(float, string, float, float)>();
            for (int i = 0; i < names.Count; i++)
            {
                string n = names[i];
                // 총을 쏘는 프레임: 느린 셔터 화면에 맞춰 피치를 낮추고 늘인 총성 (꿈 몽타주는 효과음을 끄지만 '!'는 냄)
                if (n == "sil_03" || n == "sil_05" || n == "sil_07b") cues.Add(Q(at[i], "!story_shot_slow", 15, 0.05f));   // 섬광이 터지는 프레임
                else if (n == "sil_10" || n == "sil_12") cues.Add(Q(at[i], "heart", 12, 0.03f));                // 눈에 섬광이 처음 · 크게
                else cues.Add(Q(at[i], "heart", 5));
            }
            var shot = S(dur, "어둠의 시점 원테이크: 도망치는 고양이를 쫓아 눈동자까지 (연속 프레임)", MTrans.Cut, 0, new[] { run }, Vector2.zero, Vector2.zero, 1, 1, 0, 0, 14, 1, 2, cues.ToArray());
            // 카메라: 프레임 안의 움직임에 더해 멈추지 않고 조금씩 밀고 들어가며 좌우로 흔들림
            shot.camPath = new[]
            {
                new Vector4(0, 0, 0, 1.04f), new Vector4(0.2f, 40, -10, 1.08f), new Vector4(0.4f, -50, 10, 1.12f), new Vector4(0.6f, 40, -10, 1.17f),
                new Vector4(0.8f, -20, 10, 1.24f), new Vector4(0.9f, 0, 0, 1.32f), new Vector4(1f, 0, 0, 1.45f),
            };
            shot.rotPath = new[] { new Vector2(0, -3), new Vector2(0.25f, 4), new Vector2(0.5f, -4), new Vector2(0.75f, 3), new Vector2(1, 0) };
            shot.shutter = 0.6f;
            shot.tint = Color.black;
            return shot;
        }

        static MShot[] Dream2
        {
            get
            {
                var L = new List<MShot>();
                void A(params MShot[] s) => L.AddRange(s);
                void AA(IEnumerable<MShot> s) => L.AddRange(s);
                var Z = Vector2.zero;

                // v5 (2026-11-24 사용자: 초반 반복이 많음 · 수리공 장면 템포가 김 · 기둥 위 고양이가 의미 없이 반복 · 추적 씬과 앞 컷이 안 이어짐)
                //   → 반복 컷 덜고 길이 늘리기(곡 맞춤) 없앰 · 풍향계 고양이는 어둠이 탑을 삼킬 때 한 번 → 뛰어내려 어둠 속으로 → 그대로 추적 원테이크

                // ══ 1. 어린 날: 등불을 든 아이가 탑 안 나선계단을 오름 (부감 · 코발트 + 레몬)
                A(C1(1.2f, "탑 안 나선계단 (부감)", "spiral_a", new(0, -40), new(0, -20), 1.2f, 1.13f, -16, -11, 1.5f, new[] { Q(0.3f, "e4_tick_1", 1) }, MTrans.Dissolve, 0.8f));
                AA(Cels("등불을 들고 계단을 오르는 아이", new[] { "spiral_b", "spiral_c", "spiral_d", "spiral_e", "spiral_f" }, Even(5, 0.3f), new(0, -20), new(0, 20), 1.13f, 1.2f, -11, 18, 2));
                A(Cut2("lantern_e", 0.12f, 9, 1.25f, MGrade.Negative, 12),                          // 끝: 꺼진 등불 (미래가 끼어듦)
                  Cut2("xp_spiral_cy", 0.16f, -7, 1.12f, null, 14));                               // 거친 붓터치: 소용돌이가 된 계단

                // ══ 2. 거대한 문자판의 작은 수리공 (청록 + 산호) ↔ 볼트를 돌리는 손: 어른 · 청년 · 노인 (짧게)
                AA(Cels("거대한 문자판과 비계 위의 작은 수리공", new[] { "scaffold_a", "scaffold_b", "scaffold_c" }, new[] { 0.7f, 0.35f, 0.35f }, new(200, 0), new(-110, 10), 1.06f, 1.14f, 1, -2, 2, "e4_tick_2", 2));
                A(Bolt("bolt_adult", 0.4f, 1.06f, -3),
                  Bolt("bolt_young_cc", 0.22f, 1.12f, 4));
                AA(Tremble("bolt_old_a", "bolt_old_b", 4, 0.08f, 1.18f, 5));
                A(Cut2("xp_hands_vp", 0.14f, -10, 1.15f, null, 16));
                // 불이 들어오고 바늘이 다시 감 → 머리 위 종이 울림 (진홍 + 황토)
                AA(Cels("문자판에 불이 들어오고 바늘이 다시 감", new[] { "scaffold_d_iy", "scaffold_d2_iy", "scaffold_d3_iy" }, new[] { 0.35f, 0.2f, 0.3f }, new(-120, 10), new(-170, 20), 1.14f, 1.3f, -2, -4, 3, "e4_tick_1", 6));
                AA(Cels("머리 위에서 흔들리는 거대한 종", new[] { "bell_a", "bell_b", "bell_c", "bell_b_cy", "bell_c_pl" }, new[] { 0.5f, 0.32f, 0.32f, 0.28f, 0.28f },   // 2026-11-24 사용자: 종 흔들림이 너무 빠름 → 약 2배 느리게
                        Z, new(0, -20), 1.08f, 1.2f, 0, 6, 6, "e4_bell_2", 8));
                A(Cut2("xp_bell", 0.16f, 8, 1.1f, null, 18, 0.06f));
                A(C1(0.55f, "종이 울려 빛의 고리가 퍼짐", "bell_d_og", new(0, -20), new(0, -40), 1.2f, 1.45f, 6, 9, 5, new[] { Q(0f, "e4_bell_2", 18, 0.15f) }, ease: 1));

                // ══ 3. 불이 꺼져 감: 마을 창불 → 병원 창 ↔ 늙은 손의 등불 (자홍 + 비리디언) — 컷이 점점 짧아짐
                A(C1(1.0f, "밤 마을의 창불", "windows_a_bg", new(0, 40), new(0, 10), 1.05f, 1.12f, 0, 1, 1.5f, new[] { Q(0.2f, "heart", 2), Q(0.7f, "heart", 2) }, MTrans.Dissolve, 0.3f));
                A(C1(0.5f, "등불을 쥔 늙은 손", "lantern_a", new(20, 0), new(-10, 0), 1.06f, 1.1f, 2, 1, 3, new[] { Q(0f, "heart", 4) }));
                A(C1(0.5f, "창불이 꺼지기 시작함", "windows_b_bg", new(0, 10), new(0, 0), 1.12f, 1.16f, 1, 0, 2, new[] { Q(0f, "heart", 5) }));
                A(C1(0.4f, "줄어드는 불꽃", "lantern_b_uo", new(-10, 0), new(-20, 0), 1.1f, 1.15f, 1, -1, 4, new[] { Q(0f, "heart", 6) }));
                AA(Tremble("bolt_old_a_cc", "bolt_old_b_cc", 4, 0.08f, 1.2f, -4));
                A(C1(0.4f, "집들이 늘어나 병원 건물로", "windows_c_og", new(0, 0), new(0, -10), 1.16f, 1.22f, 0, -2, 3, new[] { Q(0f, "heart", 7) }));
                A(C1(0.34f, "깜빡이는 불꽃", "lantern_c_cy", new(-20, 0), new(-30, 0), 1.15f, 1.22f, -1, -3, 5, new[] { Q(0f, "heart", 8) }));
                A(Cut2("xp_lantern_bg", 0.14f, -9, 1.12f, null, 16));
                A(C1(0.32f, "거의 다 꺼진 병원 창", "windows_d_mt", new(0, -10), new(0, -20), 1.22f, 1.3f, -2, -4, 4, new[] { Q(0f, "heart", 9) }));
                A(C1(0.3f, "남은 불씨", "lantern_d_og", new(-30, 0), new(-36, 0), 1.22f, 1.3f, -3, -5, 6, new[] { Q(0f, "heart", 10) }));
                // 불규칙하게 섞인 시간 (짧게)
                A(Cut2("spiral_c_og", 0.11f, -13, 1.4f, null, 12),
                  Bolt("bolt_adult_vp", 0.11f, 1.3f, 8, "heart", 12, MGrade.Negative),
                  Cut2("xp_twin_cy", 0.16f, 11, 1.1f, null, 14),                                   // 거친 붓터치: 아이와 노인이 겹친 하나의 형체
                  Cut2("scaffold_b_pl", 0.1f, -8, 1.3f, null, 12),
                  Cut2("bell_c_og", 0.1f, -12, 1.4f, null, 14));
                A(C1(0.8f, "단 하나 남은 창", "windows_e", new(0, -20), new(0, -60), 1.3f, 1.55f, -4, -2, 3, new[] { Q(0f, "heart", 6), Q(0.6f, "heart", 3) }, ease: 1));
                A(C1(0.8f, "꺼진 등불 · 피어오르는 연기", "lantern_e", new(-36, 0), new(-36, 30), 1.3f, 1.36f, -5, -5, 2, new[] { Q(0.1f, "heart", 3) }));
                // 늙은 손이 놓친 렌치가 나선계단 한가운데 어둠으로 떨어짐 (부감)
                AA(Cels("어둠으로 떨어지는 렌치", new[] { "fall_a", "fall_b_cc", "fall_c_og" }, new[] { 0.25f, 0.25f, 0.6f }, Z, Z, 1.05f, 1.45f, 0, 50, 3, "e4_tick_2", 8));
                A(Cut2("xp_grip_vp", 0.18f, 8, 1.2f, null, 18, 0.06f));                           // 거친 붓터치: 금 간 무언가를 움켜쥔 손

                // ══ 4. 어둠이 닥쳐 옴: 검은 해일이 탑을 삼킴 → 탑 꼭대기 풍향계 위의 고양이(여기서 한 번)
                AA(Cels("탑을 삼키는 어둠의 해일", new[] { "ink_a", "ink_b_cc", "ink_c_bg", "ink_d" }, new[] { 0.6f, 0.45f, 0.4f, 0.6f }, new(0, -30), new(0, 30), 1.04f, 1.24f, 0, -4, 6, "heart", 10));
                A(Cut2("xp_abstract", 0.16f, -12, 1.15f, null, 20, 0.08f));
                A(C1(0.9f, "어둠에 둘러싸인 풍향계 위의 고양이", "vane_c", new(60, -30), new(0, 10), 1.12f, 1.3f, -2, 2, 3, new[] { Q(0f, "heart", 8), Q(0.6f, "heart", 6) }, MTrans.Dissolve, 0.18f, g: MGrade.Ink));

                // ══ 5. 고양이가 어둠 속으로 뛰어내리고 → 어둠의 시점으로 쫓아 눈동자까지 (한 테이크)
                A(RunChase());

                // ══ 6. 눈동자 익스트림 클로즈업에서 섬광 속으로 하얗게
                A(new MShot { dur = 1.2f, tint = Color.white, tin = MTrans.Dissolve, tinDur = 0.5f });
                return L.ToArray();
            }
        }
    }
}
