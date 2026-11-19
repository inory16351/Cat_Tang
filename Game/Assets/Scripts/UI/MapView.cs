// M 키 전체 지도 (2026-11-17 사용자): 지금 스테이지의 방 구조 전체 + 현재 위치
// - GPT로 뽑은 양피지 판(Resources/UI/map/map_panel) 위에 방 타일(map_room)을 실제 배치 비율대로 그림
// - 복도 선 · 막힌 곳(방 모양 깎임) 어둡게 · 술집(중간 보스) · 보스 방 아이콘, 보상 문 위치(map_door)
// - 들어간 방은 밝게, 안 간 방은 흐리게, 지금 방은 금테. 고양이 머리(map_cat)가 현재 위치를 따라다님
// - M으로 열고 닫기(Esc로도 닫힘). 게임은 멈추지 않음
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CatTang
{
    public class MapView : MonoBehaviour
    {
        Canvas canvas;
        RectTransform panel, content, cat;
        Text title;
        bool open;
        static MapView live;
        static int closedFrame = -1;
        /// <summary>이 프레임 Esc를 지도가 썼는지 (GameUI가 전투 포기 창을 안 열게)</summary>
        public static bool EscUsed => (live && live.open) || closedFrame == Time.frameCount;
        object builtFor;                       // 어떤 레이아웃으로 그렸는지 (바뀌면 다시 그림)
        float scale;
        Vector2 worldMin;
        readonly List<(RoomBuilder.RoomInfo room, Image plate, Outline line)> plates = new();
        readonly List<RectTransform> doorIcons = new();
        readonly List<(Image bar, RoomBuilder.RoomInfo a, RoomBuilder.RoomInfo b)> links = new();
        // 방 판(map_room) 바닥 가운데 색 — 통로 띠를 방 판과 같은 색으로 이어 붙임 (2026-11-17 사용자: 세피리아처럼 색으로 연결)
        static readonly Color Floor = new(0.918f, 0.467f, 0.149f);

        static readonly Color Visited = Color.white, Unvisited = new Color(0.62f, 0.55f, 0.48f, 0.75f), Gold = new Color(1f, 0.82f, 0.3f);

        void Awake()
        {
            UIKit.Init();
            var go = new GameObject("MapCanvas", typeof(Canvas), typeof(CanvasScaler));
            go.transform.SetParent(transform, false);
            canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 60;
            var sc = go.GetComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1920, 1080);
            sc.matchWidthOrHeight = 1f;
            // 어둡게 깔기
            var dim = UIKit.Stretch("Dim", (RectTransform)go.transform).gameObject.AddComponent<Image>();
            dim.color = new Color(0, 0, 0, 0.72f);
            dim.raycastTarget = false;
            panel = Rect("Panel", (RectTransform)go.transform, new Vector2(1560, 1030));
            var pi = panel.gameObject.AddComponent<Image>();
            pi.sprite = UIKit.Spr("map/map_panel");
            pi.preserveAspect = true;
            pi.raycastTarget = false;
            content = Rect("Content", panel, new Vector2(1240, 760));
            content.anchoredPosition = new Vector2(0, -20);
            title = UIKit.Label("Title", panel, new Vector2(0.5f, 1f), new Vector2(0, -95), new Vector2(900, 60), 40, TextAnchor.MiddleCenter, true, new Color(0.35f, 0.2f, 0.12f));
            var hint = UIKit.Label("Hint", panel, new Vector2(0.5f, 0f), new Vector2(0, 70), new Vector2(900, 40), 24, TextAnchor.MiddleCenter, false, new Color(0.45f, 0.3f, 0.2f));
            hint.text = "M · 닫기";
            canvas.gameObject.SetActive(false);
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

        static Image Icon(string sprite, RectTransform parent, Vector2 pos, float size, Color c)
        {
            var rt = Rect(sprite, parent, new Vector2(size, size));
            rt.anchoredPosition = pos;
            var im = rt.gameObject.AddComponent<Image>();
            im.sprite = UIKit.Spr("map/" + sprite);
            im.preserveAspect = true;
            im.color = c;
            im.raycastTarget = false;
            return im;
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && (kb.mKey.wasPressedThisFrame || (open && kb.escapeKey.wasPressedThisFrame))) Toggle(!open && kb.mKey.wasPressedThisFrame);
            if (!open) return;
            var rm = RunManager.Instance;
            if (!rm || rm.Layout == null) return;
            if (builtFor != rm.Layout) Build(rm);
            Refresh(rm);
        }

        void OnEnable() => live = this;

        void Toggle(bool on)
        {
            if (open && !on) closedFrame = Time.frameCount;
            open = on;
            canvas.gameObject.SetActive(on);
            builtFor = null;                 // 열 때마다 새로 (문 · 방 상태 반영)
        }

        Vector2 ToMap(Vector3 w) => (new Vector2(w.x, w.z) - worldMin) * scale - content.sizeDelta / 2;

        void Build(RunManager rm)
        {
            foreach (Transform c in content) Destroy(c.gameObject);
            plates.Clear(); doorIcons.Clear(); links.Clear();
            var rooms = rm.Layout.rooms;
            if (rooms.Count == 0) return;
            // 전체 범위 → 판 안에 맞춤
            Vector2 mn = new(float.MaxValue, float.MaxValue), mx = new(float.MinValue, float.MinValue);
            foreach (var r in rooms)
            {
                mn = Vector2.Min(mn, new Vector2(r.center.x - r.size.x / 2, r.center.z - r.size.y / 2));
                mx = Vector2.Max(mx, new Vector2(r.center.x + r.size.x / 2, r.center.z + r.size.y / 2));
            }
            var span = mx - mn;
            scale = Mathf.Min(content.sizeDelta.x / Mathf.Max(1, span.x), content.sizeDelta.y / Mathf.Max(1, span.y)) * 0.92f;
            worldMin = mn - (content.sizeDelta / scale - span) / 2;
            // 복도 (방 순서대로 가운데끼리)
            for (int i = 1; i < rooms.Count; i++)
            {
                // 방 판 가장자리끼리만 (판 위로 선이 비치지 않게)
                var ra = rooms[i - 1]; var rb = rooms[i];
                var dir = rb.center - ra.center; dir.y = 0;
                bool horiz = Mathf.Abs(dir.x) > Mathf.Abs(dir.z);
                var step = horiz ? new Vector3(Mathf.Sign(dir.x), 0, 0) : new Vector3(0, 0, Mathf.Sign(dir.z));
                var a = ToMap(ra.center + step * ((horiz ? ra.size.x : ra.size.y) / 2 - 0.5f));
                var b = ToMap(rb.center - step * ((horiz ? rb.size.x : rb.size.y) / 2 - 0.5f));
                // 문 폭만큼 굵은 띠, 양 끝은 방 판 밑으로 살짝 파고들어 하나로 이어져 보이게
                var line = Rect("Link", content, new Vector2((b - a).magnitude, Mathf.Max(22f, 5f * scale)));
                line.anchoredPosition = (a + b) / 2;
                line.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg);
                var li = line.gameObject.AddComponent<Image>();
                li.color = Floor;
                li.raycastTarget = false;
                links.Add((li, ra, rb));
            }
            // 방 판
            foreach (var r in rooms)
            {
                var rt = Rect("Room", content, new Vector2(r.size.x, r.size.y) * scale);
                rt.anchoredPosition = ToMap(r.center);
                var im = rt.gameObject.AddComponent<Image>();
                im.sprite = UIKit.Spr(r.midBoss ? "map/map_room" : "map/map_room");
                im.color = r.midBoss ? new Color(0.78f, 0.55f, 0.38f) : Visited;
                im.raycastTarget = false;
                var ol = rt.gameObject.AddComponent<Outline>();
                ol.effectColor = Gold; ol.effectDistance = new Vector2(5, -5); ol.enabled = false;
                // 막힌 곳 (방 모양 깎임)
                foreach (var b in r.blocks)
                {
                    var br = Rect("Block", rt, new Vector2(b.width, b.height) * scale);
                    br.anchoredPosition = (b.center - new Vector2(r.center.x, r.center.z)) * scale;
                    var bi = br.gameObject.AddComponent<Image>();
                    bi.color = new Color(0.32f, 0.18f, 0.12f, 0.92f);
                    bi.raycastTarget = false;
                }
                if (r.midBoss) Icon("map_saloon", rt, Vector2.zero, Mathf.Min(rt.sizeDelta.x, rt.sizeDelta.y) * 0.75f, Color.white);
                else if (rm.IsBossRoom) Icon("map_boss", rt, Vector2.zero, Mathf.Min(rt.sizeDelta.x, rt.sizeDelta.y) * 0.6f, Color.white);
                plates.Add((r, im, ol));
            }
            // 보상 문
            foreach (var d in FindObjectsByType<RoomDoor>(FindObjectsSortMode.None))
                doorIcons.Add(Icon("map_door", content, ToMap(d.transform.position), 54, Color.white).rectTransform);
            // 현재 위치 (맨 위)
            cat = Icon("map_cat", content, Vector2.zero, 74, Color.white).rectTransform;
            title.text = rm.IsBossRoom ? $"{rm.chapter}장 · 보스" : $"{rm.chapter}장 · 스테이지 {rm.stageIndex + 1}/{RunManager.Stages}";
            builtFor = rm.Layout;
        }

        void Refresh(RunManager rm)
        {
            foreach (var (room, plate, line) in plates)
            {
                bool here = room == rm.CurrentRoom || room.Contains(PlayerController.Instance ? PlayerController.Instance.transform.position : Vector3.zero, 0f);
                plate.color = room.midBoss ? (room.entered ? new Color(0.85f, 0.62f, 0.42f) : new Color(0.6f, 0.45f, 0.36f, 0.85f)) : (room.entered || here ? Visited : Unvisited);
                line.enabled = here;
            }
            foreach (var (bar, a, b) in links)
            {
                bool seen = a.entered || b.entered || a == rm.CurrentRoom || b == rm.CurrentRoom;
                var t = seen ? Visited : Unvisited;
                bar.color = new Color(Floor.r * t.r, Floor.g * t.g, Floor.b * t.b, t.a);
            }
            var p = PlayerController.Instance;
            if (p && cat)
            {
                cat.anchoredPosition = ToMap(p.transform.position) + Vector2.up * (8f + 6f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 5f)));
                cat.SetAsLastSibling();
            }
        }
    }
}
