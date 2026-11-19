// 개발용: 게임 화면(UI 포함, ScreenCapture)을 일정 간격(실시간)으로 N장 PNG로 저장 — 로비 연출 GIF 만들기용
using System.Collections;
using UnityEngine;

namespace CatTang.Dev
{
    public class UiCapture : MonoBehaviour
    {
        public static void Begin(int count, float interval, string folder, int width = 720)
        {
            var go = new GameObject("UiCapture");
            DontDestroyOnLoad(go);
            go.AddComponent<UiCapture>().StartCoroutine(go.GetComponent<UiCapture>().Run(count, interval, folder, width));
        }

        /// <summary>로비 등장 연출을 시각 고정(LobbyScreen.DebugAge)으로 한 장씩 찍음 — 캡처가 느려도 GIF 타이밍이 정확</summary>
        public static void BeginLobby(int count, float step, string folder, int from = 0)
        {
            var go = new GameObject("UiCapture");
            go.AddComponent<UiCapture>().StartCoroutine(go.GetComponent<UiCapture>().RunLobby(count, step, folder, from));
        }

        IEnumerator RunLobby(int count, float step, string folder, int from)
        {
            System.IO.Directory.CreateDirectory(folder);
            for (int i = from; i < count; i++)
            {
                LobbyScreen.DebugAge = i * step;
                yield return null; yield return null;
                yield return new WaitForEndOfFrame();
                var full = ScreenCapture.CaptureScreenshotAsTexture();
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(folder, $"f_{i:000}.png"), full.EncodeToPNG());
                System.IO.File.AppendAllText(System.IO.Path.Combine(folder, "times.txt"), (i * step).ToString("F4", System.Globalization.CultureInfo.InvariantCulture) + "\n");
                Destroy(full);
            }
            LobbyScreen.DebugAge = -1f;
            Debug.Log("[UiCapture] 로비 끝 " + folder);
            Destroy(gameObject);
        }

        IEnumerator Run(int count, float interval, string folder, int width)
        {
            System.IO.Directory.CreateDirectory(folder);
            float next = Time.unscaledTime;
            for (int i = 0; i < count; i++)
            {
                while (Time.unscaledTime < next) yield return null;
                next += interval;
                yield return new WaitForEndOfFrame();
                float at = Time.unscaledTime;
                var full = ScreenCapture.CaptureScreenshotAsTexture();      // 크기 줄이기는 바깥(파이썬)에서 — 블릿하면 알파 · 색공간이 섞임
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(folder, $"f_{i:000}.png"), full.EncodeToPNG());
                System.IO.File.AppendAllText(System.IO.Path.Combine(folder, "times.txt"), at.ToString("F4", System.Globalization.CultureInfo.InvariantCulture) + "\n");
                Destroy(full);
            }
            Debug.Log("[UiCapture] 끝 " + folder);
            Destroy(gameObject);
        }
    }
}
