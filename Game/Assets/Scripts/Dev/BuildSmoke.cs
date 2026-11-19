// 빌드 점검 (2026-11-19): 실행 인자 -smoke 일 때만 동작. 평소 실행에는 아무 영향 없음.
// 판(Run)으로 바로 들어가 → 커서 · HUD가 만들어졌는지 → 경험치를 넣어 레벨 업 3택 창이 뜨는지 → 그동안 난 예외 수를 로그에 남기고 종료.
// 저장 데이터는 건드리지 않음 (판 시작은 읽기만, 끝나기 전에 끔).
// 실행: 1159.exe -smoke -logFile smoke.log  → "[Smoke]" 줄 확인
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CatTang
{
    public class BuildSmoke : MonoBehaviour
    {
        int errors;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-smoke") < 0) return;
            var go = new GameObject("BuildSmoke");
            DontDestroyOnLoad(go);
            go.AddComponent<BuildSmoke>();
        }

        void OnEnable() => Application.logMessageReceived += OnLog;
        void OnDisable() => Application.logMessageReceived -= OnLog;
        void OnLog(string msg, string stack, LogType t)
        {
            if (t == LogType.Exception || t == LogType.Error) errors++;
        }

        IEnumerator Start()
        {
            yield return SceneManager.LoadSceneAsync("Run");
            yield return new WaitForSecondsRealtime(3f);
            var ui = FindFirstObjectByType<GameUI>();
            Debug.Log($"[Smoke] GameUI={(ui != null)} AimCursor={(FindFirstObjectByType<AimCursor>() != null)} HudView={(FindFirstObjectByType<HudView>() != null)} " +
                      $"FX셰이더={(Shader.Find("CatTang/FX") != null)} 반전셰이더={(Shader.Find("CatTang/UIInvert") != null)} 오류={errors}");
            var rm = RunManager.Instance;
            if (rm) rm.AddXp(100000);
            bool opened = false;
            for (float t = 0; t < 5f && !opened; t += 0.25f)
            {
                yield return new WaitForSecondsRealtime(0.25f);
                opened = ui && ui.ScreenOpen;
            }
            Debug.Log($"[Smoke] RunManager={(rm != null)} 레벨업창={opened} 오류={errors}");
            Debug.Log("[Smoke] 끝");
            Application.Quit();
        }
    }
}
