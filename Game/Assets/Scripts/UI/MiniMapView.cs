// 오른쪽 아래 상시 미니맵 (2026-11-25 사용자: LoL처럼 지금 방만 작게 늘 보이게)
// - 놋쇠 리벳 틀(UI/slot) 안에 지금 방 판(map/map_room)을 방 크기에 맞춰 축척해 그림 + 막힌 곳(방 모양 깎임) + 나무 문(닫힘 = 짙은 나무 · 열림 = 금빛)
// - 적 = 빨간 점 (강화형은 조금 크게 · 보스는 map_boss 아이콘), 보상 문 = map_door + 위에 보상 아이콘, 고양이 = map_cat + 조준 방향 바늘(merchant_arrow)
// - 카메라가 바라보는 쪽 = 미니맵 위 (카메라 수평 방향만큼 판을 돌림)
// - 전투 방이 아닐 때 숨김: 스토리 맵 · 연출(StoryHold) · 보스 등장(BossIntro) · 판 끝 · 방 밖 멀리(스토리 맵 자리).
//   HUD 루트 밑이라 대화 중 HUD 숨김 · 마을(장전 HUD 없음)도 그대로 따라감. 다른 연출이 끄려면 MiniMapView.Suppressed
// - 성능: 점 · 문 · 막힌 곳은 풀에서 재사용(매 프레임 할당 없음), 자기 Canvas로 묶어 점이 움직여도 HUD 전체를 다시 굽지 않음
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CatTang
{
    public class MiniMapView : MonoBehaviour
    {
        /// <summary>튜토리얼 · 연출이 잠시 미니맵을 끄고 싶을 때 true</summary>
        public static bool Suppressed;
        public static MiniMapView Instance { get; private set; }

        // 기준 해상도(1920×1080, 높이 맞춤) 단위
        const float Margin = 28f;              // 화면 오른쪽 끝에서
        const float Bottom = 64f;              // 화면 아래에서 (밑에 '총 스킬' 글자 한 줄)
        const float MaxSize = 250f, MinSize = 170f;
        const float CenterHalf = 450f;         // 가운데 장전 HUD + 오른쪽 '열기' 글자가 차지하는 반폭 → 좁은 화면에선 그만큼 미니맵을 줄임
        const float Inset = 24f;               // 틀(slot 그림)의 놋쇠 띠 두께
        const float Pad = 12f;                 // 방 판 둘레 여백 (문 막대 · 아이콘이 틀에 안 먹히게)
        const float RoomMargin = 4f;           // 방 밖 이만큼(m)까지의 적도 가장자리에 붙여 보여 줌
        const float FarHide = 18f;             // 어느 방에서도 이만큼(m) 넘게 떨어져 있으면 숨김 (스토리 맵 자리 등)

        static readonly Color EnemyRed = new(0.95f, 0.16f, 0.13f), EliteRed = new(1f, 0.32f, 0.12f);
        static readonly Color GateShut = new(0.3f, 0.16f, 0.08f), GateOpen = new(1f, 0.82f, 0.3f);
        static readonly Color Block = new(0.32f, 0.18f, 0.12f, 0.92f);

        RectTransform root, parentRt, inner, content, plateRt, cat, needle;
        Canvas canvas;
        float lastParentW = -1;

        RoomBuilder.RoomInfo shown;
        float scale = 1, yaw, builtYaw = float.NaN;
        Quaternion upright = Quaternion.identity;

        readonly List<Image> blocks = new();
        int blockCount;

        readonly List<Image> gates = new();
        readonly List<GameObject> gateObjs = new();
        readonly List<Collider> gateBoxes = new();

        readonly List<RectTransform> dots = new();
        readonly List<Image> dotImgs = new();
        int dotsOn;
        readonly List<RectTransform> bossIcons = new();
        int bossOn;

        class DoorMark { public RectTransform rt; public Image icon; public RewardKind kind = (RewardKind)(-1); }
        readonly List<DoorMark> doorMarks = new();
        readonly List<RoomDoor> doorBuf = new();
        readonly List<RoomDoor> doorOf = new();                          // doorMarks와 같은 순서
        int doorsOn;
        float doorScanAt;

        static Sprite dotSprite;

        /// <summary>HudView.Build에서 부름: HUD 루트 오른쪽 아래에 붙임</summary>
        public static MiniMapView Create(RectTransform parent)
        {
            var rt = UIKit.MakeRect("MiniMap", parent, new Vector2(1, 0), new Vector2(-Margin, Bottom), new Vector2(MaxSize, MaxSize), new Vector2(1, 0));
            var mm = rt.gameObject.AddComponent<MiniMapView>();
            mm.Build(rt, parent);
            return mm;
        }

        void Build(RectTransform rt, RectTransform parent)
        {
            root = rt; parentRt = parent;
            canvas = rt.gameObject.AddComponent<Canvas>();                 // 하위 캔버스: 점이 움직여도 HUD 전체를 다시 굽지 않게
            // 틀: 놋쇠 리벳 칸 (인벤토리 칸과 같은 그림)
            var frame = UIKit.Img("Frame", rt, "slot", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, true, 2.4f);
            Fill(frame.rectTransform, 0);
            var shd = frame.gameObject.AddComponent<Shadow>(); shd.effectColor = new Color(0.05f, 0.03f, 0.08f, 0.75f); shd.effectDistance = new Vector2(3, -3);
            inner = UIKit.MakeRect("Inner", rt, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Fill(inner, Inset);
            inner.gameObject.AddComponent<RectMask2D>();
            content = UIKit.MakeRect("Content", inner, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            plateRt = UIKit.MakeRect("Room", content, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one);
            plate = plateRt.gameObject.AddComponent<Image>();
            plate.sprite = UIKit.Spr("map/map_room");
            plate.raycastTarget = false;
            // 고양이: 바라보는 방향 바늘(둥근 메달 가운데를 축으로 돌림) + 위에 머리 (머리는 늘 똑바로)
            needle = UIKit.MakeRect("Facing", content, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(46, 46), new Vector2(87f / 256f, 0.5f));
            var ni = needle.gameObject.AddComponent<Image>();
            ni.sprite = UIKit.Spr("merchant_arrow"); ni.raycastTarget = false;
            cat = UIKit.MakeRect("Cat", content, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(30, 30));
            var ci = cat.gameObject.AddComponent<Image>();
            ci.sprite = UIKit.Spr("map/map_cat"); ci.preserveAspect = true; ci.raycastTarget = false;
            Relayout();
            canvas.enabled = false;
        }

        static void Fill(RectTransform rt, float inset)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(inset, inset); rt.offsetMax = new Vector2(-inset, -inset);
        }

        void OnEnable() => Instance = this;
        void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>화면 비율에 맞춰 크기 · 자리: 가운데 장전 HUD와 안 겹치게 남는 폭만큼 (기준 250, 좁으면 170까지)</summary>
        void Relayout()
        {
            float w = parentRt ? parentRt.rect.width : 1920f;
            lastParentW = w;
            float free = w / 2f - CenterHalf - Margin;
            float s = Mathf.Clamp(free, MinSize, MaxSize);
            root.sizeDelta = new Vector2(s, s);
            builtYaw = float.NaN;                                          // 축척 다시
        }

        Image plate;
        int plateChapter = 1;

        void LateUpdate()
        {
            if (parentRt && !Mathf.Approximately(parentRt.rect.width, lastParentW)) Relayout();
            var rm = RunManager.Instance;
            var p = PlayerController.Instance;
            var room = Visible(rm, p) ? PickRoom(rm, p.transform.position) : null;
            if (room == null) { if (canvas.enabled) canvas.enabled = false; return; }
            if (!canvas.enabled) canvas.enabled = true;

            // 방 판 색 = 장별 바닥 색 (1장 주황 원본 · 2~5장은 같은 그림을 그 장 바닥 색으로 다시 칠한 것)
            if (rm.chapter != plateChapter)
            {
                plateChapter = rm.chapter;
                plate.sprite = (plateChapter >= 2 ? UIKit.Spr("map/map_room_ch" + plateChapter) : null) ?? UIKit.Spr("map/map_room");
            }
            UpdateYaw();
            if (room != shown || !Mathf.Approximately(builtYaw, yaw)) BuildRoom(room);
            UpdateGates();
            UpdateDoors(rm);
            UpdateEnemies();
            UpdateCat(p);
        }

        static bool Visible(RunManager rm, PlayerController p) =>
            !Suppressed && rm && p && !rm.runOver && !RunManager.StoryHold && !BossIntro.Playing
            && rm.Layout != null && rm.Layout.rooms.Count > 0;

        /// <summary>보여 줄 방: 고양이가 서 있는 방 → 지금 전투 방 → 마지막으로 보여 준 방 (복도에선 그대로). 어느 방에서도 멀면 null</summary>
        RoomBuilder.RoomInfo PickRoom(RunManager rm, Vector3 pos)
        {
            var rooms = rm.Layout.rooms;
            var cur = rm.CurrentRoom;
            if (cur != null && cur.Contains(pos, 0f)) return cur;
            bool near = false;
            for (int i = 0; i < rooms.Count; i++)
            {
                var r = rooms[i];
                if (r.Contains(pos, 0f)) return r;
                if (!near && r.Contains(pos, -FarHide)) near = true;
            }
            if (!near) return null;
            if (shown != null && rooms.Contains(shown)) return shown;
            return cur != null && rooms.Contains(cur) ? cur : rooms[0];
        }

        void UpdateYaw()
        {
            var cam = Camera.main;
            if (!cam) return;
            var f = cam.transform.forward;
            if (f.x * f.x + f.z * f.z < 1e-4f) f = cam.transform.up;       // 바로 위에서 내려다보면 화면 위쪽 = 카메라 up
            float y = Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;
            if (float.IsNaN(builtYaw) || Mathf.Abs(Mathf.DeltaAngle(y, yaw)) > 1.5f) yaw = Mathf.Round(y);   // 흔들림 · 미세한 떨림은 무시
        }

        // ---------- 방 (방이 바뀌거나 카메라 방향이 바뀔 때만) ----------
        void BuildRoom(RoomBuilder.RoomInfo room)
        {
            shown = room;
            builtYaw = yaw;
            content.localRotation = Quaternion.Euler(0, 0, yaw);           // 월드 +z(카메라 앞) → 화면 위
            upright = Quaternion.Euler(0, 0, -yaw);
            var area = inner.rect.size - new Vector2(Pad, Pad) * 2;
            float c = Mathf.Abs(Mathf.Cos(yaw * Mathf.Deg2Rad)), s = Mathf.Abs(Mathf.Sin(yaw * Mathf.Deg2Rad));
            float sx = Mathf.Max(1f, room.size.x), sz = Mathf.Max(1f, room.size.y);
            scale = Mathf.Min(area.x / (c * sx + s * sz), area.y / (s * sx + c * sz));
            plateRt.sizeDelta = new Vector2(sx, sz) * scale;
            // 막힌 곳
            blockCount = 0;
            foreach (var b in room.blocks)
            {
                if (blockCount >= blocks.Count)
                {
                    var bi = UIKit.MakeRect("Block", plateRt, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one).gameObject.AddComponent<Image>();
                    bi.color = Block; bi.raycastTarget = false;
                    blocks.Add(bi);
                }
                var im = blocks[blockCount++];
                im.enabled = true;
                im.rectTransform.anchoredPosition = (b.center - new Vector2(room.center.x, room.center.z)) * scale;
                im.rectTransform.sizeDelta = new Vector2(b.width, b.height) * scale;
            }
            for (int i = blockCount; i < blocks.Count; i++) blocks[i].enabled = false;
            // 나무 문 (방 사이 길목)
            gateObjs.Clear(); gateBoxes.Clear();
            foreach (var g in room.gates) if (g) { gateObjs.Add(g); gateBoxes.Add(g.GetComponent<Collider>()); }
            for (int i = 0; i < gateObjs.Count; i++)
            {
                if (i >= gates.Count)
                {
                    var gi = UIKit.MakeRect("Gate", content, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one).gameObject.AddComponent<Image>();
                    gi.raycastTarget = false;
                    var ol = gi.gameObject.AddComponent<Outline>(); ol.effectColor = new Color(0.08f, 0.04f, 0.02f, 0.9f); ol.effectDistance = new Vector2(1.5f, -1.5f);
                    gates.Add(gi);
                }
                var d = gateObjs[i].transform.position - room.center;
                bool side = Mathf.Abs(d.x) / (sx / 2) > Mathf.Abs(d.z) / (sz / 2);   // 동 · 서 벽이면 세로 막대
                float len = Mathf.Max(10f, WoodGate.Width * scale), thick = 6f;
                var grt = gates[i].rectTransform;
                grt.sizeDelta = side ? new Vector2(thick, len) : new Vector2(len, thick);
                // 벽선 위에 딱 붙임
                var at = new Vector2(d.x, d.z) * scale;
                if (side) at.x = Mathf.Sign(d.x) * sx / 2 * scale; else at.y = Mathf.Sign(d.z) * sz / 2 * scale;
                grt.anchoredPosition = at;
                gates[i].enabled = true;
            }
            for (int i = gateObjs.Count; i < gates.Count; i++) gates[i].enabled = false;
            // 똑바로 서야 하는 것들
            cat.localRotation = upright;
            foreach (var m in doorMarks) m.rt.localRotation = upright;
            foreach (var b in bossIcons) b.localRotation = upright;
            doorScanAt = 0;                                                 // 문 다시 찾기
            // 그리는 순서: 판 → 문 막대 → 보상 문 → 적 → 고양이
            needle.SetAsLastSibling(); cat.SetAsLastSibling();
        }

        void UpdateGates()
        {
            for (int i = 0; i < gateObjs.Count; i++)
            {
                var g = gateObjs[i];
                var box = gateBoxes[i];
                bool shut = g && g.activeSelf && (!box || box.enabled);
                gates[i].color = shut ? GateShut : GateOpen;               // 같은 색이면 Graphic이 알아서 안 다시 그림
            }
        }

        Vector2 ToMap(Vector3 w) => new Vector2(w.x - shown.center.x, w.z - shown.center.z) * scale;

        Vector2 ClampIn(Vector2 v, float pad)
        {
            float hx = shown.size.x / 2 * scale + pad, hz = shown.size.y / 2 * scale + pad;
            return new Vector2(Mathf.Clamp(v.x, -hx, hx), Mathf.Clamp(v.y, -hz, hz));
        }

        bool Near(Vector3 w, float margin)
        {
            var d = w - shown.center;
            return Mathf.Abs(d.x) < shown.size.x / 2 + margin && Mathf.Abs(d.z) < shown.size.y / 2 + margin;
        }

        // ---------- 보상 문 (방을 비운 뒤 생김, 0.5초마다 찾음) ----------
        void UpdateDoors(RunManager rm)
        {
            if (Time.unscaledTime >= doorScanAt)
            {
                doorScanAt = Time.unscaledTime + 0.5f;
                doorBuf.Clear();
                if (shown.root && (shown.cleared || !rm.inCombat)) shown.root.GetComponentsInChildren(false, doorBuf);
                int n = 0;
                for (int i = 0; i < doorBuf.Count; i++)
                {
                    var d = doorBuf[i];
                    if (!d) continue;
                    var m = n < doorMarks.Count ? doorMarks[n] : NewDoorMark();
                    if (n < doorOf.Count) doorOf[n] = d; else doorOf.Add(d);
                    n++;
                    if (m.kind != d.reward) SetDoorIcon(m, d.reward);
                    m.rt.anchoredPosition = ClampIn(ToMap(d.transform.position), 0);
                    if (!m.rt.gameObject.activeSelf) m.rt.gameObject.SetActive(true);
                }
                for (int i = n; i < doorsOn; i++) doorMarks[i].rt.gameObject.SetActive(false);
                doorsOn = n;
            }
            // 고른 문이 사라졌으면 바로 숨김
            for (int i = 0; i < doorsOn; i++)
                if (!doorOf[i] && doorMarks[i].rt.gameObject.activeSelf) doorMarks[i].rt.gameObject.SetActive(false);
        }

        DoorMark NewDoorMark()
        {
            var rt = UIKit.MakeRect("Door", content, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(22, 28));
            var di = rt.gameObject.AddComponent<Image>();
            di.sprite = UIKit.Spr("map/map_door"); di.preserveAspect = true; di.raycastTarget = false;
            var irt = UIKit.MakeRect("Reward", rt, new Vector2(0.5f, 0.5f), new Vector2(0, -1), new Vector2(17, 17));   // 문 가운데(열쇠 구멍 자리)에 보상 아이콘 — 위로 띄우면 먼 벽 쪽 문에서 틀에 잘림
            var ii = irt.gameObject.AddComponent<Image>();
            ii.preserveAspect = true; ii.raycastTarget = false;
            var ol = irt.gameObject.AddComponent<Shadow>(); ol.effectColor = new Color(0, 0, 0, 0.8f); ol.effectDistance = new Vector2(1.5f, -1.5f);
            rt.localRotation = upright;
            rt.SetSiblingIndex(needle.GetSiblingIndex());                   // 고양이 밑
            var m = new DoorMark { rt = rt, icon = ii };
            doorMarks.Add(m);
            return m;
        }

        static void SetDoorIcon(DoorMark m, RewardKind k)
        {
            m.kind = k;
            m.icon.sprite = k == RewardKind.Boss ? UIKit.Spr("map/map_boss") : UIKit.Icon(RoomDoor.IconOf(k));
            m.icon.enabled = m.icon.sprite;
        }

        // ---------- 적 (매 프레임, 풀 재사용) ----------
        void UpdateEnemies()
        {
            int n = 0, nb = 0;
            var all = Enemy.All;
            for (int i = 0; i < all.Count; i++)
            {
                var e = all[i];
                if (!e || !e.isActiveAndEnabled) continue;
                var h = e.health;
                if (h && !h.Alive) continue;
                var w = e.transform.position;
                if (!Near(w, RoomMargin)) continue;
                var at = ClampIn(ToMap(w), 2f);
                if (h && h.IsBoss)
                {
                    var b = nb < bossIcons.Count ? bossIcons[nb] : NewBossIcon();
                    if (nb >= bossOn) b.gameObject.SetActive(true);
                    b.anchoredPosition = at;
                    nb++;
                    continue;
                }
                var dot = n < dots.Count ? dots[n] : NewDot();
                if (n >= dotsOn) dotImgs[n].enabled = true;
                dot.anchoredPosition = at;
                float sz = e.elite ? 12f : 9f;
                dot.sizeDelta = new Vector2(sz, sz);
                dotImgs[n].color = e.elite ? EliteRed : EnemyRed;
                n++;
            }
            for (int i = n; i < dotsOn; i++) dotImgs[i].enabled = false;
            for (int i = nb; i < bossOn; i++) bossIcons[i].gameObject.SetActive(false);
            dotsOn = n; bossOn = nb;
        }

        RectTransform NewDot()
        {
            var rt = UIKit.MakeRect("Enemy", content, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(9, 9));
            var im = rt.gameObject.AddComponent<Image>();
            im.sprite = DotSprite(); im.raycastTarget = false; im.color = EnemyRed;
            rt.SetSiblingIndex(needle.GetSiblingIndex());
            dots.Add(rt); dotImgs.Add(im);
            return rt;
        }

        RectTransform NewBossIcon()
        {
            var rt = UIKit.MakeRect("Boss", content, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(30, 30));
            var im = rt.gameObject.AddComponent<Image>();
            im.sprite = UIKit.Spr("map/map_boss"); im.preserveAspect = true; im.raycastTarget = false;
            rt.localRotation = upright;
            rt.SetSiblingIndex(needle.GetSiblingIndex());
            rt.gameObject.SetActive(false);
            bossIcons.Add(rt);
            return rt;
        }

        /// <summary>적 점: 흰 원 + 짙은 테두리 (색은 Image.color로 입힘). 한 번만 만듦</summary>
        static Sprite DotSprite()
        {
            if (dotSprite) return dotSprite;
            const int N = 32;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "MiniMapDot" };
            var px = new Color32[N * N];
            float r = N / 2f - 0.5f;
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float d = Mathf.Sqrt((x - r) * (x - r) + (y - r) * (y - r));
                    float a = Mathf.Clamp01(r - d + 0.5f);                    // 바깥 가장자리 부드럽게
                    float k = Mathf.Clamp01((r - 5f) - d + 0.5f);             // 안쪽 = 흰색, 바깥 띠 = 짙은 테두리
                    byte v = (byte)Mathf.Lerp(30, 255, k);
                    px[y * N + x] = new Color32(v, (byte)Mathf.Lerp(14, 255, k), (byte)Mathf.Lerp(10, 255, k), (byte)(a * 255));
                }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            dotSprite = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 100);
            return dotSprite;
        }

        // ---------- 고양이 ----------
        void UpdateCat(PlayerController p)
        {
            var at = ClampIn(ToMap(p.transform.position), 4f);
            cat.anchoredPosition = at;
            needle.anchoredPosition = at;
            var a = p.AimDir;
            if (a.x * a.x + a.z * a.z < 1e-4f) a = p.transform.forward;
            needle.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(a.z, a.x) * Mathf.Rad2Deg);   // 그림 바늘이 +x(오른쪽)를 가리킴
        }
    }
}
