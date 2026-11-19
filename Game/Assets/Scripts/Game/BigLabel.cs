// 보스 기믹 숫자 배지 (2026-11-17 사용자: 고양이 머리 위 · 보스 얼굴 숫자 가시성 키우기)
// 월드 TextMesh(얇고 지형에 가려짐) 대신 화면 UI 배지: 어두운 원판 + 금테 + 굵은 외곽선 숫자, 월드 대상을 따라다님
// - 글이 바뀌면 툭 커졌다 돌아오고(펀치), 위험 색이면 빠르게 맥동
// - text 비우면 숨김. TextMesh와 같은 모양(text · color · gameObject)이라 기존 기믹 코드를 거의 그대로 씀
// - 고양이를 따라다니는 배지는 offset 높이 대신 머리 위 대시 줄(HeadDash) 바로 위에 붙음
using UnityEngine;
using UnityEngine.UI;

namespace CatTang
{
    public class BigLabel : MonoBehaviour
    {
        static Canvas canvas;
        public Transform target;
        Vector3 offset, worldPos;
        RectTransform rt;
        Text label;
        Image disk, ring;
        string current = "";
        Color col = Color.white;
        float punch, sizeMul = 1f;
        bool hadTarget;

        public string text
        {
            get => current;
            set
            {
                value ??= "";
                if (value == current) return;
                if (value.Length > 0) punch = 1f;
                current = value;
                if (label) label.text = value;
            }
        }

        public Color color { get => col; set { col = value; if (label) label.color = value; if (ring) ring.color = value; } }

        /// <summary>월드 대상을 따라다니는 화면 UI 캔버스 (BigLabel · HeadDash 같이 씀)</summary>
        public static Canvas Canvas()
        {
            if (canvas) return canvas;
            UIKit.Init();
            var go = new GameObject("BigLabelCanvas", typeof(Canvas), typeof(CanvasScaler));
            canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40;
            var sc = go.GetComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1920, 1080);
            sc.matchWidthOrHeight = 1f;
            return canvas;
        }

        /// <summary>parent(없으면 월드 고정) 위 offset에 배지. size = 1이 기본(고양이 머리 위), 보스 얼굴은 1.4 정도</summary>
        public static BigLabel Show(Transform parent, Vector3 offset, string text, Color c, float size = 1f)
        {
            var go = new GameObject("BigLabel", typeof(RectTransform));
            go.transform.SetParent(Canvas().transform, false);
            var b = go.AddComponent<BigLabel>();
            b.target = parent; b.hadTarget = parent; b.offset = offset; b.worldPos = offset; b.sizeMul = size;
            b.rt = (RectTransform)go.transform;
            b.rt.sizeDelta = new Vector2(150, 150) * size;
            b.ring = Img(go.transform, "Ring", 1.12f * size, c);
            b.disk = Img(go.transform, "Disk", 1f * size, new Color(0.09f, 0.05f, 0.05f, 0.9f));
            var lgo = new GameObject("Num", typeof(RectTransform));
            lgo.transform.SetParent(go.transform, false);
            var lrt = (RectTransform)lgo.transform;
            lrt.sizeDelta = new Vector2(300, 200) * size;
            b.label = lgo.AddComponent<Text>();
            b.label.font = UIKit.Title ? UIKit.Title : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            b.label.fontSize = Mathf.RoundToInt(82 * size);
            b.label.alignment = TextAnchor.MiddleCenter;
            b.label.horizontalOverflow = HorizontalWrapMode.Overflow;
            b.label.verticalOverflow = VerticalWrapMode.Overflow;
            b.label.lineSpacing = 0.8f;
            b.label.supportRichText = true;
            b.label.raycastTarget = false;
            var o = lgo.AddComponent<Outline>(); o.effectColor = new Color(0.05f, 0.02f, 0.02f, 1f); o.effectDistance = new Vector2(4, -4) * size;
            var o2 = lgo.AddComponent<Outline>(); o2.effectColor = new Color(0.05f, 0.02f, 0.02f, 1f); o2.effectDistance = new Vector2(-4, 4) * size;
            var sh = lgo.AddComponent<Shadow>(); sh.effectColor = new Color(0, 0, 0, 0.6f); sh.effectDistance = new Vector2(0, -7) * size;
            b.color = c;
            b.text = text;
            b.LateUpdate();
            return b;
        }

        static Image Img(Transform parent, string name, float scale, Color c)
        {
            var g = new GameObject(name, typeof(RectTransform));
            g.transform.SetParent(parent, false);
            ((RectTransform)g.transform).sizeDelta = new Vector2(150, 150) * scale;
            var im = g.AddComponent<Image>();
            im.sprite = UIKit.Spr("slot_round");
            im.color = c;
            im.raycastTarget = false;
            return im;
        }

        void LateUpdate()
        {
            if (hadTarget && !target) { Destroy(gameObject); return; }      // 따라가던 보스 · 고양이가 사라지면 같이
            var cam = Camera.main;
            bool show = current.Length > 0 && cam;
            Vector3 wp = target ? target.position + offset : worldPos;
            Vector3 sp = cam ? cam.WorldToScreenPoint(wp) : Vector3.zero;
            if (sp.z < 0) show = false;
            foreach (Transform c in transform) c.gameObject.SetActive(show);
            if (!show) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform, sp, null, out var lp);
            // 고양이 머리 위 배지(현상금 표식 등)는 머리 위 대시 '<<<' 줄 바로 위에 (2026-11-17 사용자)
            var pc = PlayerController.Instance;
            if (target && pc && target == pc.transform && HeadDash.TryAbove(150f * 1.12f * sizeMul, out var above)) lp = above;
            // 화면 밖으로 나가지 않게 가장자리 안쪽에 붙잡음 (키 큰 보스 얼굴이 화면 위로 잘리지 않게)
            var half = ((RectTransform)canvas.transform).rect.size / 2f;
            float m = 95f * sizeMul;
            lp.x = Mathf.Clamp(lp.x, -half.x + m, half.x - m);
            lp.y = Mathf.Clamp(lp.y, -half.y + m, half.y - m * 1.2f);
            rt.anchoredPosition = lp;
            // 펀치(글이 바뀔 때) + 위험 색이면 빠른 맥동
            punch = Mathf.MoveTowards(punch, 0, Time.unscaledDeltaTime * 4f);
            bool danger = col.g < 0.5f;
            float pulse = danger ? 1f + 0.08f * Mathf.Sin(Time.unscaledTime * 18f) : 1f;
            rt.localScale = Vector3.one * (1f + 0.35f * punch * punch) * pulse;
        }
    }
}
