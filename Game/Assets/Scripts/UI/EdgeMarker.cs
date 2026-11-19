// 화면 밖 표시 (상인 공통 규칙, 2026-11-18): 붙은 물체가 화면 밖이면 화면 가장자리에 그쪽을 가리키는 화살표 + 아이콘.
// 화면 안으로 들어오면 숨김. 물체가 사라지면 같이 사라짐.
using UnityEngine;
using UnityEngine.UI;

namespace CatTang
{
    public class EdgeMarker : MonoBehaviour
    {
        static Canvas canvas;
        RectTransform root, arrow;
        Image icon;
        float iconOffset;
        public float height = 2.0f;                       // 물체 위 어디를 가리킬지 (m)
        const float Margin = 70f;                          // 화면 가장자리에서 띄우는 거리 (기준 해상도 px)

        public void Init(string iconRes)
        {
            if (!canvas)
            {
                var cgo = new GameObject("EdgeMarkers", typeof(Canvas), typeof(CanvasScaler));
                canvas = cgo.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 5;                   // HUD 창들보다 아래
                var sc = cgo.GetComponent<CanvasScaler>();
                sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                sc.referenceResolution = new Vector2(1920, 1080);
                sc.matchWidthOrHeight = 1f;
            }
            root = new GameObject("Edge_" + name, typeof(RectTransform)).GetComponent<RectTransform>();
            root.SetParent(canvas.transform, false);
            root.anchorMin = root.anchorMax = Vector2.zero;
            root.sizeDelta = new Vector2(96, 96);
            var arrowSpr = UIKit.Spr("merchant_arrow");
            arrow = new GameObject("Arrow", typeof(RectTransform)).GetComponent<RectTransform>();
            arrow.SetParent(root, false);
            arrow.sizeDelta = new Vector2(110, 110);
            if (arrowSpr)
            {
                var im = arrow.gameObject.AddComponent<Image>();
                im.sprite = arrowSpr; im.preserveAspect = true; im.raycastTarget = false;
            }
            else
            {
                // 화살표 그림이 아직 없을 때: 글자 삼각형
                var t = arrow.gameObject.AddComponent<Text>();
                t.text = "▶"; t.font = UIKit.BodyBold; t.fontSize = 64; t.alignment = TextAnchor.MiddleRight;
                t.color = UIKit.Gold; t.raycastTarget = false;
                arrow.gameObject.AddComponent<Outline>().effectColor = new Color(0.1f, 0.07f, 0.13f);
            }
            var tex = Resources.Load<Texture2D>(iconRes);
            var ig = new GameObject("Icon", typeof(RectTransform)).GetComponent<RectTransform>();
            ig.SetParent(root, false);
            ig.sizeDelta = new Vector2(arrowSpr ? 38 : 52, arrowSpr ? 38 : 52);
            iconOffset = arrowSpr ? -20f : 0f;                          // 화살표 그림의 둥근 메달 가운데 (그림 가운데보다 왼쪽)
            icon = ig.gameObject.AddComponent<Image>();
            if (tex) icon.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            icon.preserveAspect = true; icon.raycastTarget = false;
            root.gameObject.SetActive(false);
        }

        void LateUpdate()
        {
            if (!root) return;
            var cam = Camera.main;
            if (!cam) { root.gameObject.SetActive(false); return; }
            var v = cam.WorldToViewportPoint(transform.position + Vector3.up * height);
            bool behind = v.z < 0;
            if (behind) { v.x = 1 - v.x; v.y = 1 - v.y; }
            bool onScreen = !behind && v.x > 0.02f && v.x < 0.98f && v.y > 0.02f && v.y < 0.98f;
            root.gameObject.SetActive(!onScreen);
            if (onScreen) return;
            var cs = ((RectTransform)canvas.transform).sizeDelta;           // 캔버스 크기 (기준 해상도 기준)
            var c = new Vector2(0.5f, 0.5f);
            var d = new Vector2(v.x, v.y) - c;
            if (d.sqrMagnitude < 1e-6f) d = Vector2.down;
            // 가운데에서 그 방향으로 화면 사각형에 닿는 점
            var half = new Vector2(0.5f - Margin / cs.x, 0.5f - Margin / cs.y);
            float k = Mathf.Min(Mathf.Abs(d.x) > 1e-5f ? half.x / Mathf.Abs(d.x) : float.MaxValue, Mathf.Abs(d.y) > 1e-5f ? half.y / Mathf.Abs(d.y) : float.MaxValue);
            var p = c + d * k;
            root.anchoredPosition = new Vector2(p.x * cs.x, p.y * cs.y);
            float ang = Mathf.Atan2(d.y * cs.y, d.x * cs.x) * Mathf.Rad2Deg;
            arrow.localRotation = Quaternion.Euler(0, 0, ang);
            icon.rectTransform.anchoredPosition = (Vector2)(arrow.localRotation * new Vector3(iconOffset, 0, 0));
            float pulse = 1 + 0.08f * Mathf.Sin(Time.unscaledTime * 6f);
            root.localScale = Vector3.one * pulse;
        }

        void OnDestroy() { if (root) Destroy(root.gameObject); }
        void OnDisable() { if (root) root.gameObject.SetActive(false); }
    }
}
