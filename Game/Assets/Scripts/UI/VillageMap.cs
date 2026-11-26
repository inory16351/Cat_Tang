// 마을 M 키 지도 (2026-11-19 사용자: 마을에서도 맵 만들어서 표시)
// - 판 지도(MapView)와 같은 양피지 판 · 고양이 머리 아이콘
// - 열린 곳만: 다리가 이어진 섬(둘레 모양 그대로 + 짙은 테) · 열린 다리 · 해금된 시설 · 시간의 문 (잠긴 곳은 안 그림)
// - M으로 열고 닫기, Esc로도 닫힘(마을 메뉴는 안 열림). 다른 창 · 대화 중엔 안 열림
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CatTang
{
    public class VillageMap : MonoBehaviour
    {
        public static bool IsOpen { get; private set; }
        static int closedFrame = -1;
        /// <summary>이 프레임 Esc를 지도가 썼는지 (GameUI가 마을 메뉴를 안 열게)</summary>
        public static bool EscUsed => IsOpen || closedFrame == Time.frameCount;

        Canvas canvas;
        RectTransform panel, content, cat;
        float scale;
        Vector2 worldMin;
        GameUI ui;
        Text hint;

        static readonly Color Ground = new(0.918f, 0.467f, 0.149f);          // 판 지도의 방 판 흙빛과 같게
        static readonly Color Rim = new(0.32f, 0.18f, 0.12f, 0.95f);
        static readonly Color Ink = new(0.35f, 0.2f, 0.12f);
        static readonly Color GoldDot = new(1f, 0.82f, 0.3f);

        void Awake()
        {
            UIKit.Init();
            var go = new GameObject("VillageMapCanvas", typeof(Canvas), typeof(CanvasScaler));
            go.transform.SetParent(transform, false);
            canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 60;
            var sc = go.GetComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1920, 1080);
            sc.matchWidthOrHeight = 1f;
            UiScaleFollow.Add(sc);   // 환경설정 UI 크기 (2026-11-26)
            var dim =UIKit.Stretch("Dim", (RectTransform)go.transform).gameObject.AddComponent<Image>();
            dim.color = new Color(0, 0, 0, 0.72f);
            dim.raycastTarget = false;
            panel = Rect("Panel", (RectTransform)go.transform, new Vector2(1560, 1030));
            var pi = panel.gameObject.AddComponent<Image>();
            pi.sprite = UIKit.Spr("map/map_panel");
            pi.preserveAspect = true;
            pi.raycastTarget = false;
            content = Rect("Content", panel, new Vector2(1240, 740));
            content.anchoredPosition = new Vector2(0, -24);
            var title = UIKit.Label("Title", panel, new Vector2(0.5f, 1f), new Vector2(0, -95), new Vector2(900, 60), 40, TextAnchor.MiddleCenter, true, Ink);
            title.text = "마을";
            hint = UIKit.Label("Hint", panel, new Vector2(0.5f, 0f), new Vector2(0, 70), new Vector2(900, 40), 24, TextAnchor.MiddleCenter, false, new Color(0.45f, 0.3f, 0.2f));
            hint.text = KeyBinds.Name(Act.Map) + " · 닫기";
            canvas.gameObject.SetActive(false);
            IsOpen = false;
        }

        void OnDestroy() => IsOpen = false;

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (IsOpen)
            {
                if (KeyBinds.Down(Act.Map) || (kb.escapeKey.wasPressedThisFrame && !KeyBinds.EscConsumed)) Show(false);
                else Refresh();
                return;
            }
            if (!KeyBinds.Down(Act.Map)) return;                             // 단축키 설정 (기본 M)
            ui ??= FindFirstObjectByType<GameUI>();
            var p = PlayerController.Instance;
            if ((ui && ui.ScreenOpen) || (p && p.InputLocked)) return;            // 시설 창 · 메뉴 · 대화 · 깨어나는 중
            Show(true);
        }

        void Show(bool on)
        {
            IsOpen = on;
            if (on && hint) hint.text = KeyBinds.Name(Act.Map) + " · 닫기";     // 키를 바꿨을 수 있음
            canvas.gameObject.SetActive(on);
            if (on) { Build(); Refresh(); Sfx.Play("ui_click", 0.6f, 0f); }
            else closedFrame = Time.frameCount;
        }

        // ---------- 그리기 ----------
        Vector2 ToMap(Vector3 w) => (new Vector2(w.x, w.z) - worldMin) * scale - content.sizeDelta / 2;

        void Build()
        {
            for (int i = content.childCount - 1; i >= 0; i--) Destroy(content.GetChild(i).gameObject);

            // 열린 곳만 (2026-11-19 사용자): 다리가 이어진 섬 · 열린 다리 · 해금된 시설만 그림. 범위도 열린 섬 기준이라 열린 곳이 적으면 크게 보임
            // 섬이 열렸는지 = 그 섬으로 가는 다리가 열렸는지 (Bridge_Ammo → Isle_Ammo), 가운데 섬은 늘 열림
            var bridges = FindObjectsByType<IslandBridge>(FindObjectsSortMode.None);
            bool IsleOpen(string isle)
            {
                foreach (var b in bridges) if ("Isle_" + b.name.Replace("Bridge_", "") == isle) return b.Open;
                return true;
            }
            bool OnOpenIsle(Transform t)                                            // 시설 · 문이 닫힌 섬 위에 있으면 안 그림
            {
                for (var p = t; p; p = p.parent) if (p.name.StartsWith("Isle_")) return IsleOpen(p.name);
                return true;
            }

            // 섬 둘레 (IsleOutline, 섬 로컬 x · z) → 월드
            var isles = new List<(string name, Vector3 center, Vector3[] ring)>();
            foreach (var o in FindObjectsByType<IsleOutline>(FindObjectsSortMode.None))
            {
                if (o.points == null || o.points.Length < 3 || !IsleOpen(o.name)) continue;
                var ring = new Vector3[o.points.Length];
                for (int i = 0; i < ring.Length; i++) ring[i] = o.transform.TransformPoint(new Vector3(o.points[i].x, 0, o.points[i].y));
                isles.Add((o.name, o.transform.position, ring));
            }
            if (isles.Count == 0) return;

            Vector2 mn = new(float.MaxValue, float.MaxValue), mx = new(float.MinValue, float.MinValue);
            foreach (var (_, _, ring) in isles)
                foreach (var w in ring) { mn = Vector2.Min(mn, new Vector2(w.x, w.z)); mx = Vector2.Max(mx, new Vector2(w.x, w.z)); }
            var span = mx - mn;
            // 너무 크게 확대되지 않게 (가운데 섬 하나만 열렸을 때) 한 변 최소 60m
            var pad = Vector2.Max(Vector2.zero, new Vector2(60, 60) - span) / 2;
            mn -= pad; mx += pad; span = mx - mn;
            scale = Mathf.Min(content.sizeDelta.x / Mathf.Max(1, span.x), content.sizeDelta.y / Mathf.Max(1, span.y)) * 0.9f;
            worldMin = mn - (content.sizeDelta / scale - span) / 2;

            // 열린 다리 (섬 밑에 깔아 양 끝이 섬 테두리 밑으로 파고들게)
            foreach (var b in bridges)
            {
                if (!b.Open) continue;
                var deck = b.transform.Find("Deck");
                float len = deck && deck.TryGetComponent<BoxCollider>(out var bc) ? bc.size.z : 10f;
                var a = ToMap(b.transform.position - b.transform.forward * len * 0.5f);
                var c = ToMap(b.transform.position + b.transform.forward * len * 0.5f);
                Bar("Bridge", a, c, Mathf.Max(18f, 3f * scale), Ground, Rim);
            }

            // 섬
            foreach (var (name, wc, ring) in isles)
            {
                var center = ToMap(wc);
                var tris = new List<Vector2>();                                          // 가운데에서 부채꼴
                for (int i = 0; i < ring.Length; i++)
                {
                    tris.Add(center);
                    tris.Add(ToMap(ring[i]));
                    tris.Add(ToMap(ring[(i + 1) % ring.Length]));
                }
                Poly("IsleRim_" + name, tris, center, 1f, 7f, Rim);                    // 테두리 = 바깥으로 7px 부풀린 같은 모양
                Poly("Isle_" + name, tris, center, 1f, 0f, Ground);
            }

            // 해금된 시설 · 시간의 문
            foreach (var fd in FindObjectsByType<FacilityDoor>(FindObjectsSortMode.None))
                if (Meta.FacilityUnlocked(fd.facility) && OnOpenIsle(fd.transform))
                    Marker(fd.transform.position, Meta.FacilityName[(int)fd.facility], GoldDot, Ink);
            foreach (var g in FindObjectsByType<ChapterGate>(FindObjectsSortMode.None))
                if (OnOpenIsle(g.transform)) Marker(g.transform.position, $"시간의 문\n<size=18>다음: {Mathf.Min(Meta.ChaptersCleared + 1, Meta.ChapterCount)}장</size>", new Color(0.55f, 0.85f, 1f), Ink);

            cat = Icon("map_cat", content, Vector2.zero, 74).rectTransform;
        }

        void Refresh()
        {
            var p = PlayerController.Instance;
            if (!p || !cat) return;
            cat.anchoredPosition = ToMap(p.transform.position) + Vector2.up * (8f + 6f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 5f)));
            cat.SetAsLastSibling();
        }

        void Marker(Vector3 world, string label, Color dot, Color ink)
        {
            var at = ToMap(world);
            var d = Rect("Dot", content, new Vector2(22, 22));
            d.anchoredPosition = at;
            d.localRotation = Quaternion.Euler(0, 0, 45);                        // 마름모
            var di = d.gameObject.AddComponent<Image>();
            di.color = dot; di.raycastTarget = false;
            var ol = d.gameObject.AddComponent<Outline>();
            ol.effectColor = Rim; ol.effectDistance = new Vector2(3, -3);
            var t = UIKit.Label("Name", content, new Vector2(0.5f, 0.5f), at + new Vector2(0, -38), new Vector2(240, 54), 24, TextAnchor.UpperCenter, true, ink);
            t.supportRichText = true;
            t.text = label;
            t.raycastTarget = false;
            var to = t.gameObject.AddComponent<Outline>();                         // 흙빛 섬 위에서도 읽히게 밝은 테
            to.effectColor = new Color(1f, 0.94f, 0.8f, 0.95f); to.effectDistance = new Vector2(2, -2);
        }

        void Bar(string name, Vector2 a, Vector2 b, float width, Color fill, Color edge)
        {
            var e = Rect(name + "Edge", content, new Vector2((b - a).magnitude + 8, width + 10));
            e.anchoredPosition = (a + b) / 2;
            e.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg);
            var ei = e.gameObject.AddComponent<Image>(); ei.color = edge; ei.raycastTarget = false;
            var r = Rect(name, content, new Vector2((b - a).magnitude, width));
            r.anchoredPosition = (a + b) / 2;
            r.localRotation = e.localRotation;
            var ri = r.gameObject.AddComponent<Image>(); ri.color = fill; ri.raycastTarget = false;
        }

        void Poly(string name, List<Vector2> tris, Vector2 center, float k, float grow, Color c)
        {
            var rt = Rect(name, content, content.sizeDelta);
            rt.gameObject.AddComponent<CanvasRenderer>();                          // 직접 만든 Graphic은 자동으로 안 붙음 (LobbyStrokes와 같음)
            var g = rt.gameObject.AddComponent<PolyGraphic>();
            g.tris = new List<Vector2>(tris.Count);
            foreach (var v in tris)
            {
                var d = v - center;
                g.tris.Add(center + d * k + (d.sqrMagnitude > 0.01f ? d.normalized * grow : Vector2.zero));
            }
            g.color = c;
            g.raycastTarget = false;
        }

        static RectTransform Rect(string name, RectTransform parent, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            return rt;
        }

        static Image Icon(string sprite, RectTransform parent, Vector2 pos, float size)
        {
            var rt = Rect(sprite, parent, new Vector2(size, size));
            rt.anchoredPosition = pos;
            var im = rt.gameObject.AddComponent<Image>();
            im.sprite = UIKit.Spr("map/" + sprite);
            im.preserveAspect = true;
            im.raycastTarget = false;
            return im;
        }

    }

    /// <summary>삼각형 목록(가운데 기준 UI 좌표)을 그대로 채워 그리는 그래픽 (마을 지도의 섬 모양)</summary>
    public class PolyGraphic : MaskableGraphic
    {
        public List<Vector2> tris;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (tris == null) return;
            var v = UIVertex.simpleVert;
            v.color = color;
            for (int i = 0; i + 2 < tris.Count; i += 3)
            {
                int s = vh.currentVertCount;
                for (int j = 0; j < 3; j++) { v.position = tris[i + j]; vh.AddVert(v); }
                vh.AddTriangle(s, s + 1, s + 2);
            }
        }
    }
}
