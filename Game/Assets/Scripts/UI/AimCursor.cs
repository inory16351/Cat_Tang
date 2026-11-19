// 조준 커서 (2026-11-17 사용자: 표적지 → "안 어울림" → HUD와 같은 놋쇠 조준기로 다시)
// - 그림: Codex(docs/images/ui_src/prompts/cursor_target.txt, 참고 ref_hud.png → docs/tools/ui_cursor_cut.py → Resources/UI/cursor_target.png)
//   놋쇠 쐐기 4개가 12 · 3 · 6 · 9시에서 안쪽을 가리킴(가운데는 비어 전장이 보임) + 가운데 진홍 마름모 점, 굵은 검은 외곽선. 고리 없음
//   (예전 종이 표적 그림 · 프롬프트는 docs/images/ui_src/old/cursor_target_v1.png · prompts/old_cursor_target_v1.txt)
// - 전투 중(창이 안 열려 있고 고양이가 살아 있을 때)엔 윈도 커서를 숨기고 이걸 마우스 자리에 그림. 창 · 마을에선 원래 커서
// - 쏠 때마다 툭 커졌다 살짝 비틀리며 돌아옴(반동), 장전 중엔 천천히 돌며 흐려짐(실린더 도는 느낌)
// - FPS 조준선처럼 밑 화면 색을 반전 (2026-11-17 사용자: 커서가 밑 색과 비슷해 안 보임 → 영역별로 확실히 구분되게):
//   안쪽 칠(cursor_target_fill, 흰색)을 반전 셰이더(CatTang/UIInvert, 재질 Resources/UI/CursorInvert)로 → 픽셀마다 밑색 반전,
//   그 위에 같은 칠을 밝히기 셰이더(CatTang/UIBoost, 재질 CursorBoost, 2배 곱하기)로 한 번 더 → 반전 색이 밝고 선명하게 · 흰색 쪽으로,
//   외곽선(cursor_target_edge, 검정)을 그냥 그려 반전된 안쪽과 배경을 확실히 가르고, 가운데 마름모(cursor_target_dot)는 반전 없이 늘 빨강.
//   (그전엔 밑 색을 읽어 대비 색으로 통째로 바꿨음 — 픽셀 단위가 아니라서 바꿈)
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CatTang
{
    public class AimCursor : MonoBehaviour
    {
        const float Size = 84f;                          // 1080 기준 px (64는 선이 가늘어 잘 안 보였음)
        static AimCursor instance;

        GameUI ui;
        Canvas canvas;
        RectTransform rt;
        Image img, boost, edge, dot;
        PlayerController bound;
        float kick, kickRoll, spin;
        bool shown;

        static Material invertMat, boostMat;

        public static void Create(GameUI owner)
        {
            if (instance) { instance.ui = owner; return; }
            var spr = UIKit.Spr("cursor_target");
            if (!spr) { Debug.LogWarning("[AimCursor] Resources/UI/cursor_target.png 없음 — 윈도 커서 그대로"); return; }
            var go = new GameObject("AimCursorCanvas", typeof(Canvas), typeof(CanvasScaler));
            var c = go.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 200;                          // 다른 UI보다 위
            var sc = go.GetComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1920, 1080);
            sc.matchWidthOrHeight = 1f;
            instance = go.AddComponent<AimCursor>();
            instance.ui = owner;
            instance.canvas = c;
            var g = new GameObject("Target", typeof(RectTransform));
            g.transform.SetParent(go.transform, false);
            instance.rt = (RectTransform)g.transform;
            instance.rt.sizeDelta = Vector2.one * Size;
            instance.img = g.AddComponent<Image>();
            instance.img.preserveAspect = true;
            instance.img.raycastTarget = false;
            var fill = UIKit.Spr("cursor_target_fill");
            var edgeSpr = UIKit.Spr("cursor_target_edge");
            var dotSpr = UIKit.Spr("cursor_target_dot");
            if (!invertMat) invertMat = Resources.Load<Material>("UI/CursorInvert");
            if (!boostMat) boostMat = Resources.Load<Material>("UI/CursorBoost");
            if (fill && edgeSpr && invertMat)
            {
                instance.img.sprite = fill;
                instance.img.material = invertMat;
                // 층 순서: 반전 → 밝히기 → 검은 테 → 빨간 점
                if (boostMat) instance.boost = instance.Layer(g.transform, "Boost", fill, boostMat);
                instance.edge = instance.Layer(g.transform, "Edge", edgeSpr, null);
                if (dotSpr) instance.dot = instance.Layer(g.transform, "Dot", dotSpr, null);
            }
            else
            {
                // 반전 그림이 없으면 원래 놋쇠 그림 + 그림자
                instance.img.sprite = spr;
                var sh = g.AddComponent<Shadow>(); sh.effectColor = new Color(0.05f, 0.03f, 0.08f, 0.55f); sh.effectDistance = new Vector2(2, -3);
            }
        }

        /// <summary>커서 위에 겹치는 층 (같은 크기, 그림 · 재질만 다름)</summary>
        Image Layer(Transform parent, string name, Sprite spr, Material mat)
        {
            var e = new GameObject(name, typeof(RectTransform));
            e.transform.SetParent(parent, false);
            var r = (RectTransform)e.transform;
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
            var im = e.AddComponent<Image>();
            im.sprite = spr;
            im.preserveAspect = true;
            im.raycastTarget = false;
            if (mat) im.material = mat;
            return im;
        }

        void Fired(int slot, bool ok) { kick = 1f; kickRoll = Random.Range(-14f, 14f); }

        void Bind(PlayerController p)
        {
            if (bound == p) return;
            if (bound) bound.OnFired -= Fired;
            bound = p;
            if (p) p.OnFired += Fired;
        }

        void OnDestroy()
        {
            if (bound) bound.OnFired -= Fired;
            Cursor.visible = true;
        }

        void LateUpdate()
        {
            var p = PlayerController.Instance;
            Bind(p);
            var mouse = Mouse.current;
            bool show = p && mouse != null && p.health && p.health.Alive && p.combatEnabled && !(ui && ui.ScreenOpen);
            if (show != shown)
            {
                shown = show;
                rt.gameObject.SetActive(show);
                Cursor.visible = !show;
            }
            if (!show) { if (rangeMark) rangeMark.gameObject.SetActive(false); return; }
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform, mouse.position.ReadValue(), null, out var lp);
            rt.anchoredPosition = lp;
            float dt = Time.unscaledDeltaTime;
            kick = Mathf.MoveTowards(kick, 0, dt * 6f);
            bool reloading = p.rev != null && p.rev.reloading;
            spin += (reloading ? 220f : 0f) * dt;
            if (!reloading) spin = Mathf.MoveTowardsAngle(spin, 0, dt * 400f);
            rt.localScale = Vector3.one * (1f + 0.28f * kick * kick);
            rt.localRotation = Quaternion.Euler(0, 0, kickRoll * kick - spin);
            // 사거리 (2026-11-18 사용자: 총마다 사거리가 달라짐): 커서가 다음 총의 사거리 밖이면 커서를 흐리게 하고,
            // 조준 방향의 사거리 끝 자리에 작은 빨간 점을 찍어 탄이 거기까지만 간다는 걸 보여 줌
            bool outOfRange = p.AimDist > p.NextRange + 0.3f;
            float al = reloading ? 0.6f : outOfRange ? 0.45f : 1f;
            img.color = new Color(1, 1, 1, al);     // 반전 재질: 알파 = 반전 세기 (장전 중 · 사거리 밖이면 흐리게)
            var a = new Color(1, 1, 1, al);
            if (boost) boost.color = a;
            if (edge) edge.color = a;
            if (dot) dot.color = a;
            RangeMark(p, outOfRange);
        }

        RectTransform rangeMark;

        void RangeMark(PlayerController p, bool on)
        {
            var cam = Camera.main;
            if (on && cam)
            {
                if (!rangeMark)
                {
                    var spr = UIKit.Spr("cursor_target_dot");
                    if (!spr) return;
                    var g = new GameObject("RangeEnd", typeof(RectTransform));
                    g.transform.SetParent(canvas.transform, false);
                    rangeMark = (RectTransform)g.transform;
                    rangeMark.sizeDelta = Vector2.one * Size * 0.75f;
                    var im = g.AddComponent<Image>();
                    im.sprite = spr; im.preserveAspect = true; im.raycastTarget = false;
                }
                var world = p.transform.position + Vector3.up * 0.9f + p.AimDir * p.NextRange;
                var sp = cam.WorldToScreenPoint(world);
                if (sp.z > 0 && RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform, sp, null, out var lp))
                {
                    rangeMark.anchoredPosition = lp;
                    rangeMark.gameObject.SetActive(true);
                    return;
                }
            }
            if (rangeMark) rangeMark.gameObject.SetActive(false);
        }
    }
}
