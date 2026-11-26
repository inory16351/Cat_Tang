// 창 밖 소리 끄기 (환경설정, 2026-11-26): 창이 포커스를 잃으면 GameSettings.MuteUnfocused일 때만 소리를 멈춤, 돌아오면 다시.
// 옵션이 꺼져 있으면 뒤에서도 음악이 계속 나오게 runInBackground를 켜 둠 (ProjectSettings도 켜져 있음)
using UnityEngine;

namespace CatTang
{
    public class FocusMute : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            Application.runInBackground = true;
            if (FindFirstObjectByType<FocusMute>()) return;     // 도메인 리로드를 꺼 둬서 이미 있을 수 있음
            var go = new GameObject("FocusMute") { hideFlags = HideFlags.HideInHierarchy };
            DontDestroyOnLoad(go);
            go.AddComponent<FocusMute>();
        }

        void OnApplicationFocus(bool focus)
        {
            // 스토리 곡(Music.story)은 ignoreListenerPause라 pause로 안 멈춤 → 전체 볼륨도 0으로 내렸다 돌려놓음
            if (!focus && GameSettings.MuteUnfocused) { AudioListener.pause = true; AudioListener.volume = 0f; }
            else if (focus) { AudioListener.pause = false; AudioListener.volume = GameSettings.MasterVolume; }
        }
    }
}
