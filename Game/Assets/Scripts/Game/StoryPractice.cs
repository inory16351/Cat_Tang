// 보스 패턴 연습 (2026-11-26 사용자): 환경설정 게임 탭 '보스 패턴 연습'에서 그 장을 체크하면, 그 장 스토리 맵을 이미 봤을 때
//   판마다 그 장 보스 문에 처음 들어갈 때 한 번, 스토리 맵을 '연습 모드'로 다시 돌림 → 끝나면 바로 보스 방.
//   - 대사 · 도착/끝 연출 · 카메라 비추기는 건너뛰고 기믹 연습 구간만 순서대로 (조작 안내 토스트는 남김) — 각 스토리 맵의 practice 분기
//   - 화면 위 '보스 패턴 연습' 표시 + Enter 길게(1.2초) = 언제든 연습 끝내기 → 바로 보스 방 (Esc는 일시정지 창)
//   - 기록(Meta.MarkStory) · 골드 · 경험치 · 하트 없음 (StoryHold + Active), 연습 중엔 쓰러지지 않음:
//     체력이 1 밑으로 안 내려가고 25% 아래로 떨어지면 다시 채움 → 끝나면 들어올 때 체력으로 되돌림
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CatTang
{
    public class StoryPractice : MonoBehaviour
    {
        public const float HoldTime = 1.2f;
        const float RefillBelow = 0.25f;

        /// <summary>지금 연습 모드 스토리 맵 안 (하트 드롭 등 보상 막기)</summary>
        public static bool Active { get; private set; }

        static string KeyFor(int chapter) => chapter switch
        {
            1 => StoryMap.Key, 2 => StoryPlatform.Key, 3 => StoryLake.Key, 4 => StoryWorkshop.Key, 5 => StoryWard.Key, _ => null,
        };

        /// <summary>설정이 켜져 있고 그 장 스토리 맵을 이미 봤으면 연습 가능 (판마다 한 번은 RunManager가 셈)</summary>
        public static bool Pending(int chapter)
        {
            var k = KeyFor(chapter);
            return k != null && GameSettings.BossPracticeCh(chapter) && Meta.StorySeen(k);
        }

        /// <summary>그 장 스토리 맵을 연습 모드로 시작</summary>
        public static void Play(RunManager rm, int chapter, Action done)
        {
            switch (chapter)
            {
                case 1: StoryMap.Play(rm, done, true); break;
                case 2: StoryPlatform.Play(rm, done, true); break;
                case 3: StoryLake.Play(rm, done, true); break;
                case 4: StoryWorkshop.Play(rm, done, true); break;
                case 5: StoryWard.Play(rm, done, true); break;
                default: done?.Invoke(); break;
            }
        }

        /// <summary>스토리 맵이 연습 모드로 시작할 때 붙임. quit = 'Enter 길게'로 끝낼 때 맵이 할 일 (보통 Finish)</summary>
        public static StoryPractice Begin(MonoBehaviour map, Action quit)
        {
            var sp = map.gameObject.AddComponent<StoryPractice>();
            sp.quit = quit;
            return sp;
        }

        Action quit;
        PlayerController p;
        float savedHp, savedMinHp, hold, introT;
        bool stopped, quitting;
        GameObject canvasGo;
        CanvasGroup intro;
        Image holdFill;
        GameUI ui;

        void Awake()
        {
            Active = true;
            p = PlayerController.Instance;
            ui = FindFirstObjectByType<GameUI>();
            if (p && p.health)
            {
                savedHp = p.health.hp;
                savedMinHp = p.health.minHp;
                p.health.minHp = 1f;                                   // 연습 중엔 쓰러지지 않음
            }
            BuildUi();
        }

        void BuildUi()
        {
            UIKit.Init();
            canvasGo = new GameObject("StoryPracticeCanvas", typeof(Canvas), typeof(CanvasScaler));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 45;                                      // HUD 위 · 대화/일시정지 창 아래
            var sc = canvasGo.GetComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1920, 1080);
            sc.matchWidthOrHeight = 1f;
            UiScaleFollow.Add(sc);   // 환경설정 UI 크기 (2026-11-26)
            var root = (RectTransform)canvasGo.transform;

            // 늘 떠 있는 작은 표시: 화면 위 (방 이름 아래 · 토스트 위)
            UIKit.Label("PracticeTitle", root, new Vector2(0.5f, 1), new Vector2(0, -80), new Vector2(700, 34), 26, TextAnchor.MiddleCenter, true, UIKit.Gold).text = "보스 패턴 연습";
            UIKit.Label("PracticeHint", root, new Vector2(0.5f, 1), new Vector2(0, -106), new Vector2(700, 26), 19, TextAnchor.MiddleCenter, false, UIKit.Cream).text = "Enter 길게 — 연습 끝내기";
            var barBg = UIKit.MakeRect("HoldBar", root, new Vector2(0.5f, 1), new Vector2(0, -122), new Vector2(240, 5)).gameObject.AddComponent<Image>();
            barBg.color = new Color(0, 0, 0, 0.45f);
            barBg.raycastTarget = false;
            holdFill = UIKit.Stretch("Fill", barBg.transform).gameObject.AddComponent<Image>();
            holdFill.color = UIKit.Gold;
            holdFill.raycastTarget = false;
            holdFill.rectTransform.pivot = new Vector2(0, 0.5f);
            holdFill.rectTransform.localScale = new Vector3(0, 1, 1);

            // 들어올 때 한 번 크게
            var big = UIKit.Stretch("Intro", root);
            intro = big.gameObject.AddComponent<CanvasGroup>();
            intro.blocksRaycasts = false; intro.interactable = false;
            UIKit.Label("Big", big, new Vector2(0.5f, 0.5f), new Vector2(0, 70), new Vector2(1200, 90), 72, TextAnchor.MiddleCenter, true, UIKit.Gold).text = "보스 패턴 연습";
            UIKit.Label("Sub", big, new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(1200, 40), 28, TextAnchor.MiddleCenter, false, UIKit.Cream).text = "대화 없이 패턴만 — 언제든 Enter를 길게 눌러 끝내고 보스 방으로";
            introT = 0;
        }

        void Update()
        {
            if (stopped) return;
            // 들어올 때 큰 글: 2.4초 보이고 사라짐
            if (intro)
            {
                introT += Time.unscaledDeltaTime;
                intro.alpha = introT < 2.4f ? 1f : Mathf.Clamp01(1f - (introT - 2.4f) / 0.6f);
                if (introT > 3.0f) { Destroy(intro.gameObject); intro = null; }
            }
            // 쓰러지지 않게: 많이 깎이면 다시 채움
            if (p && p.health && p.health.hp <= p.health.maxHp * RefillBelow)
            {
                p.health.hp = p.health.maxHp;
                FxBurst.Spawn(p.transform.position + Vector3.up, 1.4f, FxBurst.Kind.Gold, Color.white);
                ui?.Toast("연습 — 체력을 다시 채웠어요");
            }
            // Enter 길게 = 끝내기 (일시정지 · 창이 열린 동안엔 세지 않음)
            var kb = Keyboard.current;
            bool down = kb != null && (kb.enterKey.isPressed || kb.numpadEnterKey.isPressed) && Time.timeScale > 0f && !quitting;
            hold = down ? hold + Time.unscaledDeltaTime : Mathf.Max(0, hold - Time.unscaledDeltaTime * 3f);
            if (holdFill) holdFill.rectTransform.localScale = new Vector3(Mathf.Clamp01(hold / HoldTime), 1, 1);
            if (hold >= HoldTime && !quitting)
            {
                quitting = true;
                ui?.Toast("연습 끝 — 보스 방으로");
                quit?.Invoke();
            }
        }

        /// <summary>연습 끝 (맵의 Finish가 보스 방을 짓기 전에 부름): 체력 되돌림 · 표시 치움</summary>
        public void Stop()
        {
            if (stopped) return;
            stopped = true;
            quitting = true;
            Active = false;
            if (p && p.health)
            {
                p.health.minHp = savedMinHp;
                if (p.health.Alive) p.health.hp = Mathf.Min(p.health.maxHp, savedHp);
            }
            if (canvasGo) Destroy(canvasGo);
        }

        void OnDestroy() => Stop();
    }
}
