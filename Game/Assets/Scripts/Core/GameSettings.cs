// 설정 (메인 로비 '설정', 2026-11-18): 전체 음량 · 음악 음량 · 전체 화면. 저장 슬롯과 상관없이 하나 (PlayerPrefs cattang.opt.*)
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

        public static bool Fullscreen
        {
            get => PlayerPrefs.GetInt(P + "fullscreen", 1) == 1;
            set { PlayerPrefs.SetInt(P + "fullscreen", value ? 1 : 0); Screen.fullScreen = value; PlayerPrefs.Save(); }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Apply()
        {
            AudioListener.volume = MasterVolume;
            Music.Volume = MusicVolume;
#if !UNITY_EDITOR
            Screen.fullScreen = Fullscreen;
#endif
        }
    }
}
