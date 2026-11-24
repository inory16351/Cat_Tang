// 길 안내 화살표 (2026-11-23 사용자: "화면에 화살표로 전투 지역 문 표시하여 유도" · "여관 2층 기억의 방 해금 (화살표로 표기)")
// 대상 머리 위에서 통통 튀는 노란 화살표 + 이름. 대상이 화면 밖이면 화면 가장자리에 붙어 그쪽을 가리킴.
// 고양이가 대상 가까이(reach m) 가거나 Clear()를 부르면 사라짐. 보통 한 번에 하나, keep이면 여러 개 (2026-11-24: 기억의 방 · 의상실 동시 해금)
using UnityEngine;
using UnityEngine.UI;

namespace CatTang
{
    public class GuideArrow : MonoBehaviour
    {
        static readonly System.Collections.Generic.List<GuideArrow> all = new();
        Transform target;
        float reach, height;
        RectTransform root, arrow, labelRt;
        Text label;
        Image img;

        public static void Show(Transform target, string text, float height = 3.2f, float reach = 3f, bool keep = false)
        {
            if (!keep) Clear();
            if (!target) return;
            var go = new GameObject("GuideArrow");
            var inst = go.AddComponent<GuideArrow>();
            inst.target = target; inst.height = height; inst.reach = reach;
            inst.Build(text);
            all.Add(inst);
        }

        public static void Clear() { foreach (var a in all) if (a) Destroy(a.gameObject); all.Clear(); }
        void Remove() { all.Remove(this); Destroy(gameObject); }
        /// <summary>그 대상을 썼으면(문 열기 · 시간의 문) 화살표 치움</summary>
        public static void Reached(Transform t) { for (int i = all.Count - 1; i >= 0; i--) if (all[i] && all[i].target == t) all[i].Remove(); }
        bool wasFar;
        public static bool Active => all.Count > 0;

        void Build(string text)
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 40;   // HUD 위 · 대화/안내 창 아래
            var sc = gameObject.AddComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080); sc.matchWidthOrHeight = 0.5f;
            root = (RectTransform)transform;
            arrow = UIKit.MakeRect("Arrow", root, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(70, 70));
            img = arrow.gameObject.AddComponent<Image>(); img.sprite = ArrowSprite(); img.raycastTarget = false;
            label = UIKit.Label("Label", root, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420, 50), 30, TextAnchor.MiddleCenter, true, new Color(1f, 0.84f, 0.29f));
            label.text = text; label.raycastTarget = false;
            var ol = label.gameObject.AddComponent<Outline>(); ol.effectColor = new Color(0.08f, 0.06f, 0.1f, 0.9f); ol.effectDistance = new Vector2(2, -2);
            labelRt = label.rectTransform;
        }

        void LateUpdate()
        {
            var p = PlayerController.Instance;
            var cam = Camera.main;
            if (!target) { Remove(); return; }
            // 가까이 가면 사라짐 — 단, 시작부터 가까이 있었으면(깨어난 자리 옆 문) 한 번 멀어졌다 돌아올 때만. 문 · 시간의 문을 쓰면 바로 사라짐(Reached)
            if (p)
            {
                float d = Vector3.Distance(p.transform.position, target.position);
                if (d > reach + 1.5f) wasFar = true;
                if (wasFar && d < reach) { Remove(); return; }
            }
            if (!cam) return;
            // 대화 · 안내 창 중엔 숨김
            bool hide = StoryOverlay.HintOpen || (p && p.InputLocked);
            img.enabled = label.enabled = !hide;
            if (hide) return;

            var world = target.position + Vector3.up * height;
            var sp = cam.WorldToScreenPoint(world);
            bool behind = sp.z < 0;
            if (behind) sp = new Vector3(Screen.width - sp.x, Screen.height - sp.y, 0);   // 뒤에 있으면 반대로
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, sp, null, out var local);
            var half = root.rect.size * 0.5f - new Vector2(80, 80);
            bool off = behind || Mathf.Abs(local.x) > half.x || Mathf.Abs(local.y) > half.y;
            float bob = Mathf.Sin(Time.unscaledTime * 5f) * 12f;
            if (!off)
            {
                arrow.anchoredPosition = local + new Vector2(0, 40 + bob);
                arrow.localRotation = Quaternion.identity;                       // 아래를 가리킴
                labelRt.anchoredPosition = local + new Vector2(0, 110 + bob);
            }
            else
            {
                // 화면 가장자리: 가운데에서 대상 쪽으로 뻗은 선이 테두리에 닿는 자리
                var dir = local.sqrMagnitude < 1 ? Vector2.down : local.normalized;
                float k = Mathf.Min(dir.x != 0 ? half.x / Mathf.Abs(dir.x) : float.MaxValue, dir.y != 0 ? half.y / Mathf.Abs(dir.y) : float.MaxValue);
                var edge = dir * k;
                arrow.anchoredPosition = edge + dir * bob * 0.6f;
                arrow.localRotation = Quaternion.FromToRotation(Vector3.down, dir);
                labelRt.anchoredPosition = edge - dir * 70f;
            }
        }

        static Sprite arrowSprite;
        /// <summary>아래를 가리키는 노란 삼각 화살표 (짙은 테두리)</summary>
        static Sprite ArrowSprite()
        {
            if (arrowSprite) return arrowSprite;
            const int N = 96;
            var t = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[N * N];
            var fill = new Color(1f, 0.84f, 0.29f); var line = new Color(0.1f, 0.07f, 0.12f);
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    // 위가 넓고 아래가 뾰족한 삼각형: 위 변 y=0.86, 꼭지 (0.5, 0.08)
                    float u = (x + 0.5f) / N, v = (y + 0.5f) / N;
                    float w = Mathf.InverseLerp(0.08f, 0.86f, v) * 0.42f;            // 높이에 따른 반폭
                    float d = Mathf.Min(w - Mathf.Abs(u - 0.5f), 0.86f - v);          // 안쪽 거리 (양수 = 안)
                    d = Mathf.Min(d, (v - 0.08f) * 0.5f);
                    Color c = d > 0.05f ? fill : d > 0f ? line : new Color(0, 0, 0, 0);
                    if (d > 0f && d < 0.012f) c.a = d / 0.012f;
                    px[y * N + x] = c;
                }
            t.SetPixels32(px); t.Apply(false, true);
            arrowSprite = Sprite.Create(t, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f));
            return arrowSprite;
        }
    }
}
