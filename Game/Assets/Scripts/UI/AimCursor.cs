// 조준 커서 '표적지' (2026-11-17 사용자: 표적지 느낌 마우스 포인터)
// - 그림: Codex(docs/images/ui_src/prompts/cursor_target.txt → docs/tools/ui_cursor_cut.py → Resources/UI/cursor_target.png)
//   서부 사격장 종이 표적: 고리 3개(선만, 사이는 비어 전장이 보임) + 가운데 빨간 점 + 12 · 3 · 6 · 9시 눈금
// - 전투 중(창이 안 열려 있고 고양이가 살아 있을 때)엔 윈도 커서를 숨기고 이걸 마우스 자리에 그림. 창 · 마을에선 원래 커서
// - 쏠 때마다 툭 커졌다 살짝 비틀리며 돌아옴(반동), 장전 중엔 천천히 돌며 흐려짐(실린더 도는 느낌)
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
        Image img;
        PlayerController bound;
        float kick, kickRoll, spin;
        bool shown;

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
            instance.img.sprite = spr;
            instance.img.preserveAspect = true;
            instance.img.raycastTarget = false;
            var sh = g.AddComponent<Shadow>(); sh.effectColor = new Color(0.05f, 0.03f, 0.08f, 0.55f); sh.effectDistance = new Vector2(2, -3);
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
            if (!show) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform, mouse.position.ReadValue(), null, out var lp);
            rt.anchoredPosition = lp;
            float dt = Time.unscaledDeltaTime;
            kick = Mathf.MoveTowards(kick, 0, dt * 6f);
            bool reloading = p.rev != null && p.rev.reloading;
            spin += (reloading ? 220f : 0f) * dt;
            if (!reloading) spin = Mathf.MoveTowardsAngle(spin, 0, dt * 400f);
            rt.localScale = Vector3.one * (1f + 0.28f * kick * kick);
            rt.localRotation = Quaternion.Euler(0, 0, kickRoll * kick - spin);
            img.color = new Color(1, 1, 1, reloading ? 0.6f : 1f);
        }
    }
}
