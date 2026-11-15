// 개발용: 카메라 화면을 일정 간격(실시간)으로 N장 PNG로 저장 (연출 확인용)
using System.Collections;
using UnityEngine;

namespace CatTang.Dev
{
    public class FrameCapture : MonoBehaviour
    {
        public static void Begin(int count, float interval, string folder, int size = 640, Transform follow = null, Vector3? offset = null)
        {
            var go = new GameObject("FrameCapture");
            var fc = go.AddComponent<FrameCapture>();
            fc.StartCoroutine(fc.Run(count, interval, folder, size, follow, offset ?? new Vector3(0, 7f, -6f)));
        }

        IEnumerator Run(int count, float interval, string folder, int size, Transform follow, Vector3 offset)
        {
            System.IO.Directory.CreateDirectory(folder);
            var cam = Camera.main;
            var rt = new RenderTexture(size, size * 9 / 16, 24);
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            Camera shot = null;
            if (follow)
            {
                shot = new GameObject("CaptureCam").AddComponent<Camera>();
                shot.CopyFrom(cam);
                shot.fieldOfView = 34;
            }
            for (int i = 0; i < count; i++)
            {
                yield return new WaitForEndOfFrame();
                var c = shot ? shot : cam;
                if (shot) { shot.transform.position = follow.position + offset; shot.transform.LookAt(follow.position + Vector3.up * 0.8f); }
                c.targetTexture = rt; c.Render(); c.targetTexture = null;
                RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); tex.Apply(); RenderTexture.active = null;
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(folder, $"f{i:00}.png"), tex.EncodeToPNG());
                yield return new WaitForSecondsRealtime(interval);
            }
            if (shot) Destroy(shot.gameObject);
            Destroy(rt);
            Destroy(gameObject);
        }
    }
}
