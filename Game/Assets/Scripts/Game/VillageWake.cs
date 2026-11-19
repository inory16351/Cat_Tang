// 마을에 들어올 때마다 (메인 로비에서 · 판이 끝나고) 고양이가 연기 속에서 깨어나는 연출 (2026-11-18 사용자)
// 흐릿한 잿빛 안개 화면 → 걷히며 땅에 누운 고양이 + 둘레에 피어오르는 연기 → 몸을 일으키고 연기가 바깥으로 흩어짐 → 카메라가 물러나며 조작 시작
// 시계공의 무의식 속에서 다시 눈을 뜨는 순간. 입력은 연출 동안 잠금
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CatTang
{
    public class VillageWake : MonoBehaviour
    {
        public const float HazeTime = 1.3f, LieTime = 0.6f, RiseTime = 0.55f;
        static readonly Color Haze = new(0.78f, 0.74f, 0.8f);       // 옅은 잿빛 라벤더 안개

        PlayerController p;
        Image haze;
        float lie = 1f;                                               // 1 = 누움, 0 = 섬
        float yaw;
        bool running;

        public static void Play(PlayerController p)
        {
            if (!p) return;
            var go = new GameObject("VillageWake");
            var w = go.AddComponent<VillageWake>();
            w.p = p;
            w.StartCoroutine(w.Run());
        }

        IEnumerator Run()
        {
            running = true;
            p.InputLocked = true;
            yaw = p.model ? p.model.eulerAngles.y : 180f;
            CameraFollow.SnapZoom(0.42f);
            BuildHaze();

            // 1) 안개 화면이 걷히는 동안 누운 몸 둘레로 연기가 계속 피어오름
            float t = 0, nextPuff = 0;
            while (t < HazeTime + LieTime)
            {
                t += Time.unscaledDeltaTime;
                haze.color = new Color(Haze.r, Haze.g, Haze.b, 1f - Mathf.SmoothStep(0, 1, t / HazeTime));
                if (t >= nextPuff)
                {
                    nextPuff = t + 0.09f;
                    var r = Random.insideUnitCircle * 1.3f;
                    Mist(p.transform.position + new Vector3(r.x, 0.25f, r.y), Random.Range(1.4f, 2.2f), Random.Range(1.1f, 1.6f));
                }
                yield return null;
            }

            // 2) 몸을 일으킴 + 연기가 바깥으로 흩어짐 + 카메라가 물러남
            for (int i = 0; i < 10; i++)
            {
                float a = i / 10f * Mathf.PI * 2;
                var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                Mist(p.transform.position + d * 0.9f + Vector3.up * 0.3f, 2.2f, 1.2f);
            }
            CameraFollow.Zoom(1f);
            t = 0;
            while (t < RiseTime)
            {
                t += Time.unscaledDeltaTime;
                lie = 1f - Mathf.SmoothStep(0, 1, t / RiseTime);
                yield return null;
            }
            lie = 0;
            if (haze) Destroy(haze.canvas.gameObject);
            yield return new WaitForSecondsRealtime(0.25f);
            p.InputLocked = false;
            running = false;
            Destroy(gameObject);
        }

        void LateUpdate()
        {
            if (!running || !p || !p.model) return;
            // 누운 자세: 옆으로 쓰러져 땅에 붙음 → 일어나며 살짝 뜀
            float hop = Mathf.Sin((1f - lie) * Mathf.PI) * 0.18f * (lie < 1f ? 1f : 0f);
            p.model.rotation = Quaternion.Euler(0, yaw, 0) * Quaternion.Euler(0, 0, 82f * lie);
            p.model.localPosition = new Vector3(0, 0.22f * lie + hop, 0);
        }

        void BuildHaze()
        {
            var cgo = new GameObject("WakeHaze", typeof(Canvas), typeof(CanvasScaler));
            cgo.transform.SetParent(transform, false);
            var c = cgo.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 900;
            var img = new GameObject("Haze", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            img.transform.SetParent(cgo.transform, false);
            var rt = img.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            img.raycastTarget = false;
            img.color = Haze;
            haze = img;
        }

        void OnDestroy()
        {
            if (running && p) { p.InputLocked = false; if (p.model) p.model.localPosition = Vector3.zero; }
        }
    
        /// <summary>깨어남 연기 = 적이 사라질 때 검은 안개(bk_mist)와 같은 회보라 (2026-11-19 사용자: 흰 연기 X). 그림이 없으면 예전 연기</summary>
        static void Mist(Vector3 at, float size, float life)
        {
            if (!FxFlipbook.Spawn("bk_mist", at, Vector3.zero, size * 1.1f, life * 1.1f, false, Vector3.up * 0.6f + Random.insideUnitSphere * 0.3f, Random.Range(0f, 360f)))
                GunkataFx.Smoke(at, size, life);
        }
    }
}
