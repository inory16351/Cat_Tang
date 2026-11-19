// 이펙트 미리보기 녹화 (GIF용 프레임): 게임 시간을 프레임마다 고정 간격으로 흘려(Time.captureDeltaTime) 화면을 PNG로 저장.
// 캡처가 느려도 이펙트는 정확히 fps대로 진행됨. 화면은 메인 카메라를 직접 1920×1080 렌더 텍스처에 그려 찍음
// (Game 창이 가려져 있어도 됨, 화면 UI(HUD)는 안 찍힘). 실제 시간(WaitForSecondsRealtime)을 쓰는 히트스톱은 녹화 중엔 짧게 지나감.
// 사용(에디터 플레이 중, MCP execute_code): FxRecorder.Record(폴더, 프레임 수, fps, 시작할 때 부를 함수, 그 프레임 수만큼 기다린 뒤 시작)
using System.Collections;
using System.IO;
using UnityEngine;

namespace CatTang
{
    public class FxRecorder : MonoBehaviour
    {
        public static bool Busy { get; private set; }
        public static string LastInfo = "";

        public static void Record(string dir, int frames, float fps, System.Action start, int warmup = 2)
        {
            if (Busy) return;
            var go = new GameObject("FxRecorder");
            DontDestroyOnLoad(go);
            go.AddComponent<FxRecorder>().StartCoroutine(go.GetComponent<FxRecorder>().Run(dir, frames, fps, start, warmup));
        }

        IEnumerator Run(string dir, int frames, float fps, System.Action start, int warmup)
        {
            Busy = true;
            Directory.CreateDirectory(dir);
            foreach (var f in Directory.GetFiles(dir, "f_*.png")) File.Delete(f);
            float prevScale = Time.timeScale;
            Time.timeScale = 1f;
            Time.captureDeltaTime = 1f / fps;
            for (int i = 0; i < warmup; i++) yield return null;
            try { start?.Invoke(); } catch (System.Exception e) { Debug.LogException(e); }   // 시작 함수가 터져도 녹화는 계속 (멈춰 Busy로 남지 않게)
            var rt = RenderTexture.GetTemporary(1920, 1080, 24, RenderTextureFormat.ARGB32);
            var tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            for (int i = 0; i < frames; i++)
            {
                yield return null;
                var cam = Camera.main;
                if (!cam) continue;
                var prev = cam.targetTexture;
                cam.targetTexture = rt;
                cam.Render();
                cam.targetTexture = prev;
                var act = RenderTexture.active;
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
                tex.Apply();
                RenderTexture.active = act;
                File.WriteAllBytes(Path.Combine(dir, $"f_{i:000}.png"), tex.EncodeToPNG());
            }
            RenderTexture.ReleaseTemporary(rt);
            Destroy(tex);
            Time.captureDeltaTime = 0f;
            Time.timeScale = prevScale;
            LastInfo = $"{frames} frames → {dir}";
            Busy = false;
            Destroy(gameObject);
        }
    }
}
