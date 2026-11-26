// 마우스에 붙어 다니는 툴팁: 보상 카드 · 상점 카드에 마우스를 올리면 그 탄 · 총의 효과를 커서 옆에 띄움.
// 화면 끝에 닿으면 반대쪽(왼쪽 · 위)으로 뒤집어 잘리지 않게. 시간이 멈춘 화면(timeScale 0)에서도 움직임.
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CatTang
{
    public struct TipContent
    {
        public Sprite icon;
        public string title, body;
        public Color titleColor;
    }

    public class CursorTip : MonoBehaviour
    {
        const float W = 460, Pad = 20, IconSize = 72;
        // 고유명사 설명 상자 (세피리아 아이템 툴팁처럼 본 툴팁 옆에 작게 쌓음, 2026-11-18)
        const float SideW = 360, SidePad = 12, SideGap = 8;
        const int SideMax = 4;
        readonly System.Collections.Generic.List<(RectTransform box, Text title, Text body)> side = new();
        bool sideLeft;
        static CursorTip inst;
        RectTransform rt, canvasRt;
        Image icon;
        Text title, body;
        Func<TipContent> source;
        GameObject owner;

        /// <summary>go에 마우스를 올리면 make()로 만든 내용을 커서 옆에 띄움</summary>
        public static void Attach(GameObject go, Func<TipContent> make)
        {
            if (!go.TryGetComponent<CursorTipTarget>(out var h)) h = go.AddComponent<CursorTipTarget>();
            h.make = make;
            var g = go.GetComponent<Graphic>();
            if (g) g.raycastTarget = true;
        }

        public static void Show(GameObject from, Func<TipContent> make)
        {
            var canvas = from.GetComponentInParent<Canvas>();
            if (!canvas) return;
            canvas = canvas.rootCanvas;
            if (!inst || inst.canvasRt != (RectTransform)canvas.transform) Build((RectTransform)canvas.transform);
            inst.owner = from;
            inst.source = make;
            inst.Fill();
            inst.gameObject.SetActive(true);
            inst.transform.SetAsLastSibling();
            inst.Follow();
        }

        public static void Hide(GameObject from = null)
        {
            if (!inst) return;
            if (from && inst.owner != from) return;
            inst.owner = null;
            inst.source = null;
            inst.gameObject.SetActive(false);
        }

        static void Build(RectTransform canvasRt)
        {
            if (inst) Destroy(inst.gameObject);
            var panel = UIKit.Panel("CursorTip", canvasRt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(W, 200), false, "panel_plain", 1.4f);
            panel.color = new Color(0.12f, 0.08f, 0.15f, 0.97f);
            panel.raycastTarget = false;
            inst = panel.gameObject.AddComponent<CursorTip>();
            inst.rt = panel.rectTransform;
            inst.rt.pivot = new Vector2(0, 1);
            inst.canvasRt = canvasRt;
            // 테두리 스프라이트 안쪽이 반투명이라 뒤 글자가 비침 → 안쪽에 불투명 바탕을 한 겹 깔아 둠
            var bg = UIKit.Stretch("Bg", inst.rt).gameObject.AddComponent<Image>();
            ((RectTransform)bg.transform).offsetMin = new Vector2(6, 6);
            ((RectTransform)bg.transform).offsetMax = new Vector2(-6, -6);
            bg.color = new Color(0.1f, 0.07f, 0.13f, 1f);
            bg.raycastTarget = false;
            bg.transform.SetAsFirstSibling();
            inst.icon = UIKit.MakeRect("Icon", inst.rt, new Vector2(0, 1), new Vector2(Pad, -Pad), new Vector2(IconSize, IconSize), new Vector2(0, 1)).gameObject.AddComponent<Image>();
            inst.icon.preserveAspect = true; inst.icon.raycastTarget = false;
            inst.title = UIKit.Label("Title", inst.rt, new Vector2(0, 1), new Vector2(Pad + IconSize + 14, -Pad), new Vector2(W - Pad * 2 - IconSize - 14, IconSize), 26, TextAnchor.MiddleLeft, true, UIKit.Gold);
            inst.title.rectTransform.pivot = new Vector2(0, 1);
            UIKit.NoFit(inst.title, 18);
            inst.body = UIKit.Label("Body", inst.rt, new Vector2(0, 1), new Vector2(Pad, -Pad - IconSize - 12), new Vector2(W - Pad * 2, 100), 19, TextAnchor.UpperLeft);
            inst.body.rectTransform.pivot = new Vector2(0, 1);
            inst.body.lineSpacing = 1.12f;
            UIKit.NoFit(inst.body);
            for (int i = 0; i < SideMax; i++) inst.side.Add(MakeSide(inst.rt, i));
            panel.gameObject.SetActive(false);
        }

        static (RectTransform, Text, Text) MakeSide(RectTransform parent, int i)
        {
            var box = UIKit.Panel("Term" + i, parent, new Vector2(0, 1), Vector2.zero, new Vector2(SideW, 80), false, "panel_plain", 1.1f);
            box.color = new Color(0.1f, 0.12f, 0.17f, 0.97f);
            box.raycastTarget = false;
            var brt = box.rectTransform;
            brt.anchorMin = brt.anchorMax = brt.pivot = new Vector2(0, 1);
            var t = UIKit.Label("Title", brt, new Vector2(0, 1), new Vector2(SidePad, -SidePad + 2), new Vector2(SideW - SidePad * 2, 26), 19, TextAnchor.MiddleLeft, true, Terms.LinkColor);
            t.rectTransform.pivot = new Vector2(0, 1);
            UIKit.NoFit(t, 14);
            var b = UIKit.Label("Body", brt, new Vector2(0, 1), new Vector2(SidePad, -SidePad - 26), new Vector2(SideW - SidePad * 2, 60), 15, TextAnchor.UpperLeft);
            b.rectTransform.pivot = new Vector2(0, 1);
            b.lineSpacing = 1.08f;
            UIKit.NoFit(b);
            box.gameObject.SetActive(false);
            return (brt, t, b);
        }

        /// <summary>툴팁 글 속 고유명사의 설명 상자를 채우고 쌓음 (자리는 Follow가 좌우로 정함)</summary>
        void FillSide(TipContent c)
        {
            var terms = Terms.Find((c.title ?? "") + " " + (c.body ?? ""), SideMax);
            terms.Remove(c.title);
            float y = 0;
            for (int i = 0; i < side.Count; i++)
            {
                var (box, t, b) = side[i];
                bool on = i < terms.Count;
                box.gameObject.SetActive(on);
                if (!on) continue;
                if (Terms.TraitBox(terms[i], out var tt, out var tb)) { t.text = tt; b.text = tb; }   // 특성: 롤체식 단계 표시
                else { t.text = terms[i]; b.text = Terms.Desc(terms[i]) ?? ""; }
                float bh = b.preferredHeight + 6;
                b.rectTransform.sizeDelta = new Vector2(SideW - SidePad * 2, bh);
                float h = SidePad + 26 + bh + SidePad;
                box.sizeDelta = new Vector2(SideW, h);
                box.anchoredPosition = new Vector2(0, -y);       // x는 Follow가 정함
                y += h + SideGap;
            }
        }

        void Fill()
        {
            var c = source != null ? source() : default;
            icon.sprite = c.icon;
            icon.enabled = c.icon;
            float tx = c.icon ? Pad + IconSize + 14 : Pad;
            title.rectTransform.anchoredPosition = new Vector2(tx, -Pad);
            title.rectTransform.sizeDelta = new Vector2(W - Pad - tx, IconSize);
            title.text = c.title;
            title.color = c.titleColor.a > 0 ? c.titleColor : UIKit.Gold;
            body.text = c.body ?? "";
            float bh = body.preferredHeight + 10;          // 줄 간격 · 크기 태그 오차 여유
            body.rectTransform.sizeDelta = new Vector2(W - Pad * 2, bh);
            rt.sizeDelta = new Vector2(W, Pad + IconSize + 12 + bh + Pad);
            FillSide(c);
        }

        void Update()
        {
            // 띄운 카드가 사라졌으면(화면 닫힘) 같이 숨김
            if (!owner || !owner.activeInHierarchy) { Hide(); return; }
            Follow();
        }

        void Follow()
        {
            var m = Mouse.current;
            if (m == null) return;
            Vector2 sp = m.position.ReadValue();
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, sp, null, out var lp)) return;
            var cr = canvasRt.rect;
            var size = rt.sizeDelta;
            // 기본: 커서 오른쪽 아래. 넘치면 왼쪽 · 위로 뒤집고, 그래도 넘치면 화면 안으로 밀어 넣음
            float x = lp.x + 26, y = lp.y - 26;
            if (x + size.x > cr.xMax - 8) x = lp.x - 26 - size.x;
            // 설명 상자는 본 툴팁 오른쪽에, 화면 밖이면 왼쪽에
            bool anySide = side.Count > 0 && side[0].box.gameObject.activeSelf;
            sideLeft = anySide && x + size.x + SideGap + SideW > cr.xMax - 8;
            if (sideLeft && x - SideGap - SideW < cr.xMin + 8) x = cr.xMin + 8 + SideW + SideGap;
            if (y - size.y < cr.yMin + 8) y = lp.y + 26 + size.y;
            x = Mathf.Clamp(x, cr.xMin + 8, Mathf.Max(cr.xMin + 8, cr.xMax - 8 - size.x));
            y = Mathf.Clamp(y, Mathf.Min(cr.yMax - 8, cr.yMin + 8 + size.y), cr.yMax - 8);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x, y);
            foreach (var (box, _, _) in side)
                if (box.gameObject.activeSelf) box.anchoredPosition = new Vector2(sideLeft ? -SideGap - SideW : size.x + SideGap, box.anchoredPosition.y);
        }
    }

    /// <summary>툴팁을 띄울 UI 조각 (마우스 올림 / 내림 감지)</summary>
    public class CursorTipTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Func<TipContent> make;
        public void OnPointerEnter(PointerEventData e) { if (make != null) CursorTip.Show(gameObject, make); }
        public void OnPointerExit(PointerEventData e) => CursorTip.Hide(gameObject);
        void OnDisable() => CursorTip.Hide(gameObject);
    }
}
