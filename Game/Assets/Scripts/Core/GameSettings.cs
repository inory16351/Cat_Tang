// 설정 (메인 로비 '설정', 2026-11-18 · 4탭 2026-11-26): 저장 슬롯과 상관없이 하나 (PlayerPrefs cattang.opt.*)
//   소리(전체 · 음악 · 효과음 · 창 밖 소리 끄기) · 화면(창 모드 · 해상도 · 수직 동기화 · 흔들림 · 번쩍임 줄이기 · UI 크기)
//   게임(언어 · 대사 자동 넘김 · 글자 속도 · 조준선 모양/크기 · 장별 보스 패턴 연습) · 조작(KeyBinds)
//   값이 바뀌면 Changed (UI 크기 · 조준선처럼 화면에 바로 반영할 곳이 구독)
using UnityEngine;

namespace CatTang
{
    public static class GameSettings
    {
        const string P = "cattang.opt.";

        public static float MasterVolume
        {
            get => PlayerPrefs.GetFloat(P + "master", 1f);
            set { PlayerPrefs.SetFloat(P + "master", Mathf.Clamp01(value)); AudioListener.volume = Mathf.Clamp01(value); PlayerPrefs.Save(); }
        }

        public static float MusicVolume
        {
            get => PlayerPrefs.GetFloat(P + "music", 0.55f);
            set { PlayerPrefs.SetFloat(P + "music", Mathf.Clamp01(value)); Music.SetVolume(value); PlayerPrefs.Save(); }
        }

        public static event System.Action Changed;
        static void Fire() { PlayerPrefs.Save(); Changed?.Invoke(); }

        /// <summary>창 모드: 0 = 전체 화면 · 1 = 창 · 2 = 테두리 없는 창 (예전 fullscreen 켬/끔 값을 처음 한 번 이어받음)</summary>
        public static int WindowMode
        {
            get => PlayerPrefs.GetInt(P + "window", PlayerPrefs.GetInt(P + "fullscreen", 1) == 1 ? 0 : 1);
            set { PlayerPrefs.SetInt(P + "window", Mathf.Clamp(value, 0, 2)); ApplyScreen(); Fire(); }
        }

        public static FullScreenMode ScreenMode => WindowMode switch { 1 => FullScreenMode.Windowed, 2 => FullScreenMode.FullScreenWindow, _ => FullScreenMode.ExclusiveFullScreen };

        /// <summary>예전 코드 호환: 창 모드가 '창'이 아니면 전체 화면</summary>
        public static bool Fullscreen
        {
            get => WindowMode != 1;
            set => WindowMode = value ? 0 : 1;
        }

        static void ApplyScreen()
        {
            var r = Resolution;
            int w = r.x > 0 ? r.x : Screen.currentResolution.width, h = r.y > 0 ? r.y : Screen.currentResolution.height;
            if (WindowMode == 1 && r.x <= 0) { w = Screen.width; h = Screen.height; }
            Screen.SetResolution(w, h, ScreenMode);
        }

        /// <summary>효과음 크기 (Sfx.Volume, 기본 0.8)</summary>
        public static float SfxVolume
        {
            get => PlayerPrefs.GetFloat(P + "sfx", 0.8f);
            set { PlayerPrefs.SetFloat(P + "sfx", Mathf.Clamp01(value)); Sfx.Volume = Mathf.Clamp01(value); PlayerPrefs.Save(); }
        }

        /// <summary>해상도 (가로 × 세로, 0이면 지금 모니터 그대로). 전체 화면이면 그 해상도로 전체 화면</summary>
        public static Vector2Int Resolution
        {
            get => new(PlayerPrefs.GetInt(P + "resW", 0), PlayerPrefs.GetInt(P + "resH", 0));
            set
            {
                PlayerPrefs.SetInt(P + "resW", value.x); PlayerPrefs.SetInt(P + "resH", value.y); PlayerPrefs.Save();
                ApplyScreen(); Changed?.Invoke();
            }
        }

        /// <summary>수직 동기화</summary>
        public static bool VSync
        {
            get => PlayerPrefs.GetInt(P + "vsync", 1) == 1;
            set { PlayerPrefs.SetInt(P + "vsync", value ? 1 : 0); QualitySettings.vSyncCount = value ? 1 : 0; PlayerPrefs.Save(); }
        }

        /// <summary>보스 패턴 연습 (2026-11-26 사용자): 장마다 체크. 켜면 그 장 보스 문 앞 스토리 맵을 이미 봤어도 대화 없이 패턴 튜토리얼만 다시 함</summary>
        public static bool BossPracticeCh(int chapter) => PlayerPrefs.GetInt(P + "bossPractice" + chapter, PlayerPrefs.GetInt(P + "bossPractice", 0)) == 1;
        public static void SetBossPracticeCh(int chapter, bool on) { PlayerPrefs.SetInt(P + "bossPractice" + chapter, on ? 1 : 0); Fire(); }

        /// <summary>화면 흔들림 세기 0~1 (기본 1). 카메라 흔들림에 곱함</summary>
        public static float ShakeScale
        {
            get => PlayerPrefs.GetFloat(P + "shake", 1f);
            set { PlayerPrefs.SetFloat(P + "shake", Mathf.Clamp01(value)); Fire(); }
        }

        /// <summary>번쩍임 · 화면 효과 줄이기 (피격 번쩍 · 글리치 · 잔상을 약하게)</summary>
        public static bool ReduceFlash
        {
            get => PlayerPrefs.GetInt(P + "reduceFlash", 0) == 1;
            set { PlayerPrefs.SetInt(P + "reduceFlash", value ? 1 : 0); Fire(); }
        }

        /// <summary>번쩍임 줄이기가 켜졌을 때 번쩍 · 글리치 세기에 곱할 값</summary>
        public static float FlashScale => ReduceFlash ? 0.3f : 1f;

        /// <summary>UI 크기 0.8~1.3 (HUD · 대화창 · 글자, CanvasScaler 기준 해상도를 나눔)</summary>
        public static float UiScale
        {
            get => PlayerPrefs.GetFloat(P + "uiScale", 1f);
            set { PlayerPrefs.SetFloat(P + "uiScale", Mathf.Clamp(value, 0.8f, 1.3f)); Fire(); }
        }

        /// <summary>대사 자동 넘김</summary>
        public static bool DialogueAuto
        {
            get => PlayerPrefs.GetInt(P + "dlgAuto", 0) == 1;
            set { PlayerPrefs.SetInt(P + "dlgAuto", value ? 1 : 0); Fire(); }
        }

        /// <summary>글자 속도: 0 느림 · 1 보통 · 2 빠름 · 3 바로</summary>
        public static int TextSpeed
        {
            get => PlayerPrefs.GetInt(P + "textSpeed", 1);
            set { PlayerPrefs.SetInt(P + "textSpeed", Mathf.Clamp(value, 0, 3)); Fire(); }
        }

        /// <summary>글자 나오는 시간 배율 (보통 = 1, 바로 = 0)</summary>
        public static float TextDelayScale => TextSpeed switch { 0 => 1.6f, 2 => 0.5f, 3 => 0f, _ => 1f };

        /// <summary>조준선 모양 0~2 (0 = 기본 표적지)</summary>
        public static int CursorStyle
        {
            get => PlayerPrefs.GetInt(P + "cursorStyle", 0);
            set { PlayerPrefs.SetInt(P + "cursorStyle", Mathf.Clamp(value, 0, CursorStyles - 1)); Fire(); }
        }
        public const int CursorStyles = 3;

        /// <summary>조준선 크기 0.6~1.6</summary>
        public static float CursorSize
        {
            get => PlayerPrefs.GetFloat(P + "cursorSize", 1f);
            set { PlayerPrefs.SetFloat(P + "cursorSize", Mathf.Clamp(value, 0.6f, 1.6f)); Fire(); }
        }

        /// <summary>창 밖에 있으면 소리 끄기 (기본 켬)</summary>
        public static bool MuteUnfocused
        {
            get => PlayerPrefs.GetInt(P + "muteUnfocused", 1) == 1;
            set { PlayerPrefs.SetInt(P + "muteUnfocused", value ? 1 : 0); if (!value) AudioListener.pause = false; Fire(); }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Apply()
        {
            AudioListener.volume = MasterVolume;
            Music.Volume = MusicVolume;
            Sfx.Volume = SfxVolume;
            QualitySettings.vSyncCount = VSync ? 1 : 0;
#if !UNITY_EDITOR
            ApplyScreen();
#endif
        }
    }
}
