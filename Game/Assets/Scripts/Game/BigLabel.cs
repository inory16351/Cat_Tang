// 보스 기믹 숫자 배지 (2026-11-17 사용자: 고양이 머리 위 · 보스 얼굴 숫자 가시성 키우기)
// 월드 TextMesh(얇고 지형에 가려짐) 대신 화면 UI 배지: 어두운 원판 + 금테 + 굵은 외곽선 숫자, 월드 대상을 따라다님
// - 글이 바뀌면 툭 커졌다 돌아오고(펀치), 위험 색이면 빠르게 맥동
// - text 비우면 숨김. TextMesh와 같은 모양(text · color · gameObject)이라 기존 기믹 코드를 거의 그대로 씀
// - 고양이를 따라다니는 배지는 offset 높이 대신 머리 위 대시 줄(HeadDash) 바로 위에 붙음
// - 2026-11-26 사용자: 너무 커서 다른 요소를 가림 → 전체 크기 줄이고(큰 size일수록 덜 커지게 눌러 담음),
//   패턴 종류별 테두리 그림(Frame): Hour 시각 숫자 · Timer 카운트다운 · Mark 고양이 위험 표식 · Boss 보스 머리 위 · Gear 톱니 돌리기
//   그림 = Resources/UI/badge/badge_*.png (Hour~Boss = ChatGPT, Gear = 블렌더 Art/tools/blender_ui_gear_badge.py). 그림이 없으면 예전 원판 + 테
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CatTang
{
    public class BigLabel : MonoBehaviour
    {
        public enum Frame { Plain, Hour, Timer, Mark, Boss, Gear }
        const float Base = 100f;          // size 1일 때 원판 지름 (예전 150)
        static float Squash(float size) => 0.55f + 0.35f * size;   // 1 → 0.9 · 1.5 → 1.08 · 2.2 → 1.32
        static string SpriteOf(Frame f) => f switch
        {
            Frame.Hour => "badge/badge_hour", Frame.Timer => "badge/badge_timer", Frame.Mark => "badge/badge_mark",
            Frame.Boss => "badge/badge_boss", Frame.Gear => "badge/badge_gear", _ => null
        };

        static Canvas canvas;
        public Transform target;
        Vector3 offset, worldPos;
        RectTransform rt;
        Text label;
        Image disk, ring, arrow;
        float lift, armDist;
        Image pointer;                                              // 화면 밖 표시: 배지 둘레를 도는 삼각 포인터
        Text subLabel;                                              // 배지 옆 작은 글 (남은 초 등)
        bool hideOff;                                               // 화면 밖 · HUD 뒤면 숨김 (바닥 칸 숫자)
        bool indicator;
        const float IndScale = 0.85f;                               // 화면 밖 표시는 조금 작게
        string current = "";
        Color col = Color.white;
        float punch, sizeMul = 1f, edge = 1.12f, numDy;
        bool hadTarget, framed;

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

        public Color color { get => col; set { col = value; if (label) label.color = value; if (ring && !framed) ring.color = value; if (arrow) arrow.color = value; if (pointer) pointer.color = value; if (subLabel) subLabel.color = value; } }

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
        public static BigLabel Show(Transform parent, Vector3 offset, string text, Color c, float size = 1f, Frame frame = Frame.Plain)
        {
            var go = new GameObject("BigLabel", typeof(RectTransform));
            go.transform.SetParent(Canvas().transform, false);
            var b = go.AddComponent<BigLabel>();
            size = Squash(size);
            b.target = parent; b.hadTarget = parent; b.offset = offset; b.worldPos = offset; b.sizeMul = size;
            b.rt = (RectTransform)go.transform;
            b.rt.sizeDelta = new Vector2(Base, Base) * size;
            var spr = SpriteOf(frame) is string sn ? UIKit.Spr(sn) : null;
            if (spr)
            {
                // 테두리 그림 한 장(가운데 어두운 원 포함, 그림 색 그대로) — 그림 지름 = 원판의 1.3배 (테가 바깥으로 둘림)
                b.framed = true;
                var fr = Img(go.transform, "Frame", 1.3f * size, Color.white);
                fr.sprite = spr; fr.preserveAspect = true;
                b.edge = 1.3f;
                if (frame == Frame.Timer) b.numDy = -0.039f * Base * 1.3f * size;   // 초시계는 위 꼭지만큼 어두운 원이 아래로 내려가 있음 (그림에서 잼)
            }
            else
            {
                b.ring = Img(go.transform, "Ring", 1.12f * size, c);
                b.disk = Img(go.transform, "Disk", 1f * size, new Color(0.09f, 0.05f, 0.05f, 0.9f));
                b.edge = 1.12f;
            }
            var lgo = new GameObject("Num", typeof(RectTransform));
            lgo.transform.SetParent(go.transform, false);
            var lrt = (RectTransform)lgo.transform;
            lrt.sizeDelta = new Vector2(2f * Base, 1.33f * Base) * size;
            lrt.anchoredPosition = new Vector2(0, b.numDy);
            b.label = lgo.AddComponent<LText>();
            b.label.font = UIKit.Title ? UIKit.Title : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            b.label.fontSize = Mathf.RoundToInt(56 * size);
            b.label.alignment = TextAnchor.MiddleCenter;
            b.label.horizontalOverflow = HorizontalWrapMode.Overflow;
            b.label.verticalOverflow = VerticalWrapMode.Overflow;
            b.label.lineSpacing = 0.8f;
            b.label.supportRichText = true;
            b.label.raycastTarget = false;
            var o = lgo.AddComponent<Outline>(); o.effectColor = new Color(0.05f, 0.02f, 0.02f, 1f); o.effectDistance = new Vector2(3, -3) * size;
            var o2 = lgo.AddComponent<Outline>(); o2.effectColor = new Color(0.05f, 0.02f, 0.02f, 1f); o2.effectDistance = new Vector2(-3, 3) * size;
            var sh = lgo.AddComponent<Shadow>(); sh.effectColor = new Color(0, 0, 0, 0.6f); sh.effectDistance = new Vector2(0, -5) * size;
            b.color = c;
            b.text = text;
            if (b.framed) b.WithArrow();                               // 2026-11-26 사용자: 배지 아래 화살표로 어느 자리 · 물체의 숫자인지 (메이플 보스 숫자 표기 참고)
            b.LateUpdate();
            return b;
        }

        /// <summary>배지 아래에 아래를 가리키는 화살표 (어느 물체의 숫자인지 보이게, 2026-11-26 사용자). 화살표 끝 = 배지 기준 위치(대상 위 offset)</summary>
        public BigLabel WithArrow()
        {
            var spr = UIKit.Spr("badge/badge_arrow");
            if (!spr || arrow) return this;
            float ah = Base * 0.62f * sizeMul;                      // 화살표 길이 (짧게: 탑뷰라 메이플처럼 길 필요 없음)
            arrow = Img(transform, "Arrow", 1f, col);
            arrow.sprite = spr; arrow.preserveAspect = true;
            var art = (RectTransform)arrow.transform;
            art.sizeDelta = new Vector2(ah * 0.5f, ah);
            art.SetAsFirstSibling();                                   // 배지 뒤로 (위 끝이 테 아래로 살짝 숨음)
            float half = Base * edge * 0.5f * sizeMul;
            armDist = half + ah * 0.5f - Base * 0.08f * sizeMul;       // 배지 가운데 → 화살표 가운데
            art.anchoredPosition = new Vector2(0, -armDist);
            lift = half + ah - Base * 0.08f * sizeMul;                 // 배지 가운데 → 화살촉 끝 (화살촉 끝 = 대상 위 offset 자리)
            var ps = UIKit.Spr("badge/badge_pointer");
            if (ps)
            {
                pointer = Img(transform, "Pointer", 1f, col);
                pointer.sprite = ps; pointer.preserveAspect = true; pointer.enabled = false;
                ((RectTransform)pointer.transform).sizeDelta = new Vector2(0.66f, 0.58f) * Base * sizeMul;          // 굵게 (작으면 안 보임)
                pointer.transform.SetAsFirstSibling();
            }
            return this;
        }

        // ---------- 화살표 배지 자리 (2026-11-26 사용자: 가시성 · 체력바와 겹침 → 일반적인 게임 표시 방식으로) ----------
        // 흔한 방식: ① 화면 안 + HUD에 안 걸림 = 물체 바로 위(막대 화살표 ↓)
        //           ② 화면 밖 또는 HUD(체력바 · 리볼버 · 미니맵 …)에 가려지는 자리 = HUD를 피한 타원 안전 영역 테두리로 끌어옴,
        //              배지는 똑바로 두고 굵은 삼각 포인터만 배지 둘레를 돌아 물체 쪽을 가리킴, 여럿이면 서로 비킴
        // HUD 자리 (1920×1080 화면 기준, 캔버스 높이 1080 · 너비는 화면비에 따라)
        static readonly (float x0, float y0, float w, float h, int anchor)[] Hud =
        {
            (0, 0, -1, 178, 0),          // 위 띠: 방 정보 · 보스 체력바 (너비 = 전체, 체력바 틀 아래 끝 ≈ 165 + 여유)
            (0, 0, 480, 240, 0),         // 왼쪽 위 체력 · 대시
            (0, 0, 230, 170, 1),         // 오른쪽 위 금화 · 시간 조각
            (-470, 0, 940, 265, 2),      // 아래 가운데 리볼버 · 시계 (x0 = 가운데 기준)
            (0, 0, 290, 300, 3),         // 오른쪽 아래 미니맵
            (0, 0, 450, 120, 4),         // 왼쪽 아래 조작 안내
        };
        const int MiniMapHud = 4;                                     // Hud[4] = 미니맵 (전투 중 반투명이면 비키지 않음)
        static int placedFrame = -1;
        static readonly List<Vector2> placed = new();

        /// <summary>HUD 사각형 (캔버스 좌표, 가운데 = 0)</summary>
        static Rect HudRect(int i, Vector2 half)
        {
            var (x0, y0, w, h, a) = Hud[i];
            float W = half.x * 2f;
            if (w < 0) w = W;
            return a switch
            {
                0 => new Rect(-half.x + x0, half.y - y0 - h, w, h),           // 왼쪽 위
                1 => new Rect(half.x - w, half.y - h, w, h),                  // 오른쪽 위
                2 => new Rect(x0, -half.y, w, h),                             // 아래 가운데
                3 => new Rect(half.x - w, -half.y, w, h),                     // 오른쪽 아래
                _ => new Rect(-half.x, -half.y, w, h),                        // 왼쪽 아래
            };
        }

        static bool HitsHud(Rect r, Vector2 half)
        {
            if (r.xMin < -half.x + 8 || r.xMax > half.x - 8 || r.yMin < -half.y + 8 || r.yMax > half.y - 8) return true;
            for (int i = 0; i < Hud.Length; i++) if (!(i == MiniMapHud && MiniMapView.Faded)) if (HudRect(i, half).Overlaps(r)) return true;
            return false;
        }

        /// <summary>점이 화면 안이고 HUD에 가려지지 않았는지 (= 물체가 보임)</summary>
        static bool Visible(Vector2 p, Vector2 half, float pad)
        {
            if (Mathf.Abs(p.x) > half.x - pad || Mathf.Abs(p.y) > half.y - pad) return false;
            for (int i = 0; i < Hud.Length; i++) if (!(i == MiniMapHud && MiniMapView.Faded)) { var h = HudRect(i, half); h.xMin -= pad; h.yMin -= pad; h.xMax += pad; h.yMax += pad; if (h.Contains(p)) return false; }
            return true;
        }

        void PlaceWithArrow(Vector2 tip, Vector2 half, float m)
        {
            var art = (RectTransform)arrow.transform;
            // ① 물체가 화면에 보이면 언제나 물체에 붙음 (2026-11-26 사용자). 경계에서 깜빡이지 않게 들어올 땐 안쪽 20px, 나갈 땐 0px
            if (Visible(tip, half, indicator ? 20f : 0f))
            {
                indicator = false;
                arrow.enabled = true; if (pointer) pointer.enabled = false;
                var up = tip + new Vector2(0, lift);
                bool fitsUp = !HitsHud(new Rect(up.x - m, tip.y, m * 2f, lift + m), half);
                bool down = !fitsUp;                                     // 위가 HUD(체력바 등)에 걸리면 물체 아래로 (화살표 ↑)
                var center = down ? tip - new Vector2(0, lift) : up;
                center.x = Mathf.Clamp(center.x, -half.x + m, half.x - m);
                rt.anchoredPosition = center;
                art.anchoredPosition = new Vector2(0, down ? armDist : -armDist);
                art.localRotation = down ? Quaternion.Euler(0, 0, 180) : Quaternion.identity;
                return;
            }
            // ② 화면 밖(또는 HUD 뒤): 화면 끝에 바짝 붙여 물체 쪽을 가리킴 (문 안내처럼). HUD에 걸리면 화면 안쪽으로만 비킴
            indicator = true;
            float r = Base * edge * 0.5f * sizeMul * IndScale;
            float ptr = pointer ? ((RectTransform)pointer.transform).sizeDelta.y * IndScale * 0.6f : 0f;   // 포인터가 화면 밖으로 안 나가게
            float ex = half.x - r - ptr - 6f, ey = half.y - r - ptr - 6f;
            var d = tip; if (d.sqrMagnitude < 1f) d = Vector2.up;
            d.Normalize();
            float kx = Mathf.Abs(d.x) > 1e-4f ? ex / Mathf.Abs(d.x) : float.MaxValue;
            float ky = Mathf.Abs(d.y) > 1e-4f ? ey / Mathf.Abs(d.y) : float.MaxValue;
            var pos = d * Mathf.Min(kx, ky);
            float rr = r + ptr * 0.9f;                                       // 포인터까지 HUD에 안 걸리게
            pos = PushOutOfHud(pos, rr, half);
            if (placedFrame != Time.frameCount) { placedFrame = Time.frameCount; placed.Clear(); }
            for (int k = 0; k < 8; k++)                                   // 이미 놓인 표시와 겹치면 테두리를 따라 비킴
            {
                Vector2 hit = Vector2.zero; bool clash = false;
                foreach (var q in placed) if ((q - pos).magnitude < r * 2.1f) { clash = true; hit = q; break; }
                if (!clash) break;
                var along = Mathf.Abs(Mathf.Abs(pos.x) - ex) < 2f ? Vector2.up : Vector2.right;   // 세로 테두리면 위아래, 가로 테두리면 좌우로
                float sgn = Vector2.Dot(pos - hit, along) >= 0 ? 1f : -1f;
                pos += along * sgn * r * 2.1f;
                pos.x = Mathf.Clamp(pos.x, -ex, ex); pos.y = Mathf.Clamp(pos.y, -ey, ey);
                pos = PushOutOfHud(pos, rr, half);
            }
            placed.Add(pos);
            rt.anchoredPosition = pos;
            arrow.enabled = false;
            if (pointer)
            {
                pointer.enabled = true;
                var pd = (tip - pos).normalized;
                if (pd.sqrMagnitude < 0.5f) pd = Vector2.down;
                var prt = (RectTransform)pointer.transform;
                float pulse = 1f + 0.12f * Mathf.Sin(Time.unscaledTime * 7f);
                prt.anchoredPosition = pd * (r / IndScale + prt.sizeDelta.y * 0.32f * pulse);
                prt.localRotation = Quaternion.FromToRotation(Vector3.down, new Vector3(pd.x, pd.y, 0));
            }
        }

        /// <summary>표시(반지름 r)가 HUD 사각형과 겹치면 화면 안쪽으로 가장 조금 밀어냄</summary>
        static Vector2 PushOutOfHud(Vector2 p, float r, Vector2 half)
        {
            for (int pass = 0; pass < 3; pass++)
            {
                bool moved = false;
                for (int i = 0; i < Hud.Length; i++) if (!(i == MiniMapHud && MiniMapView.Faded))
                {
                    var h = HudRect(i, half);
                    if (!new Rect(p.x - r, p.y - r, r * 2, r * 2).Overlaps(h)) continue;
                    // 화면 안쪽 방향의 두 축 중 덜 밀어도 되는 쪽
                    float toX = p.x < 0 ? h.xMax + r - p.x : h.xMin - r - p.x;  // 가운데 쪽으로
                    float toY = p.y < 0 ? h.yMax + r - p.y : h.yMin - r - p.y;
                    bool xInward = Mathf.Sign(toX) == -Mathf.Sign(p.x) || p.x == 0;
                    bool yInward = Mathf.Sign(toY) == -Mathf.Sign(p.y) || p.y == 0;
                    if (h.width >= half.x * 1.9f) xInward = false;              // 전체 너비 띠는 위아래로만
                    if (Mathf.Sign(p.x + toX) != Mathf.Sign(p.x)) xInward = false;     // 가운데를 건너가면 안 됨 (아래 가운데 리볼버 등)
                    if (Mathf.Sign(p.y + toY) != Mathf.Sign(p.y)) yInward = false;
                    if (!xInward && !yInward) yInward = true;
                    if (xInward && (!yInward || Mathf.Abs(toX) < Mathf.Abs(toY))) p.x += toX; else p.y += toY;
                    moved = true;
                }
                if (!moved) break;
            }
            return p;
        }

        /// <summary>배지 오른쪽 옆 작은 글 (예: 남은 초). 빈 글이면 숨김 — 배지 안에는 핵심 숫자 하나만 (2026-11-26 사용자: 두 줄이 배지 밖으로 넘침)</summary>
        public string sub
        {
            get => subLabel ? subLabel.text : "";
            set
            {
                if (!subLabel)
                {
                    if (string.IsNullOrEmpty(value)) return;
                    var go = new GameObject("Sub", typeof(RectTransform));
                    go.transform.SetParent(transform, false);
                    var srt = (RectTransform)go.transform;
                    srt.pivot = new Vector2(0f, 0.5f);
                    srt.sizeDelta = new Vector2(200, 50) * sizeMul;
                    srt.anchoredPosition = new Vector2(Base * edge * 0.5f * sizeMul + 6f, 0);
                    subLabel = go.AddComponent<LText>();
                    subLabel.font = label.font;
                    subLabel.fontSize = Mathf.RoundToInt(32 * sizeMul);
                    subLabel.alignment = TextAnchor.MiddleLeft;
                    subLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
                    subLabel.raycastTarget = false;
                    var o = go.AddComponent<Outline>(); o.effectColor = new Color(0.05f, 0.02f, 0.02f, 1f); o.effectDistance = new Vector2(2.5f, -2.5f);
                    var o2 = go.AddComponent<Outline>(); o2.effectColor = new Color(0.05f, 0.02f, 0.02f, 1f); o2.effectDistance = new Vector2(-2.5f, 2.5f);
                }
                subLabel.text = value ?? "";
                subLabel.color = col;
            }
        }

        /// <summary>배지 왼쪽 위 작은 순서 동그라미 (예: 봉인 1 · 2 · 3). 배지 안 숫자와 겹치지 않게 테 위에 걸침</summary>
        public BigLabel Corner(string text, Color c)
        {
            float d = Base * 0.42f * sizeMul;
            var chip = new GameObject("Corner", typeof(RectTransform));
            chip.transform.SetParent(transform, false);
            var crt = (RectTransform)chip.transform;
            crt.sizeDelta = new Vector2(d, d);
            float rr = Base * edge * 0.5f * sizeMul * 0.72f;
            crt.anchoredPosition = new Vector2(-rr, rr);
            var ringImg = Img(chip.transform, "Ring", 1f, c); ((RectTransform)ringImg.transform).sizeDelta = new Vector2(d, d);
            var disk = Img(chip.transform, "Disk", 1f, new Color(0.09f, 0.05f, 0.05f, 1f)); ((RectTransform)disk.transform).sizeDelta = new Vector2(d, d) * 0.8f;
            var tgo = new GameObject("Num", typeof(RectTransform));
            tgo.transform.SetParent(chip.transform, false);
            ((RectTransform)tgo.transform).sizeDelta = new Vector2(d, d) * 1.4f;
            var t = tgo.AddComponent<LText>();
            t.font = label.font; t.fontSize = Mathf.RoundToInt(d * 0.62f); t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
            t.color = c; t.text = text; t.raycastTarget = false;
            var o = tgo.AddComponent<Outline>(); o.effectColor = new Color(0.05f, 0.02f, 0.02f, 1f); o.effectDistance = new Vector2(2, -2);
            return this;
        }

        /// <summary>떠올랐다 사라지는 글 (배지 · 화살표 없음): 표적이 아닌 알림용 (예: 5장 ±초). world 위에서 1.4초 동안 70px 떠오르며 끝 30%에 흐려짐</summary>
        public static void Popup(Vector3 world, string text, Color c, float size = 1f)
        {
            var go = new GameObject("BigLabelPopup", typeof(RectTransform));
            go.transform.SetParent(Canvas().transform, false);
            var t = go.AddComponent<LText>();
            t.font = UIKit.Title ? UIKit.Title : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = Mathf.RoundToInt(72 * size); t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
            t.text = text; t.color = c; t.raycastTarget = false;
            var o = go.AddComponent<Outline>(); o.effectColor = new Color(0.05f, 0.02f, 0.02f, 1f); o.effectDistance = new Vector2(3, -3) * size;
            var o2 = go.AddComponent<Outline>(); o2.effectColor = new Color(0.05f, 0.02f, 0.02f, 1f); o2.effectDistance = new Vector2(-3, 3) * size;
            go.AddComponent<PopupRise>().Init(world, t);
        }

        class PopupRise : MonoBehaviour
        {
            Vector3 world; Text t; float born; RectTransform rt;
            public void Init(Vector3 w, Text text) { world = w; t = text; born = Time.unscaledTime; rt = (RectTransform)transform; LateUpdate(); }
            void LateUpdate()
            {
                float k = (Time.unscaledTime - born) / 1.4f;
                if (k >= 1f || !Camera.main) { Destroy(gameObject); return; }
                var sp = Camera.main.WorldToScreenPoint(world);
                RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform, sp, null, out var lp);
                rt.anchoredPosition = lp + new Vector2(0, 70f * (1f - (1f - k) * (1f - k)));
                rt.localScale = Vector3.one * (k < 0.12f ? Mathf.Lerp(1.4f, 1f, k / 0.12f) : 1f);
                var c = t.color; c.a = k < 0.7f ? 1f : 1f - (k - 0.7f) / 0.3f; t.color = c;
            }
        }

        /// <summary>화살표 빼기 (바닥 칸 숫자처럼 물체가 아닌 자리 표시: 화면 밖 칸마다 가장자리 화살표가 쌓이면 어수선함)</summary>
        public BigLabel NoArrow()
        {
            if (arrow) Destroy(arrow.gameObject);
            if (pointer) Destroy(pointer.gameObject);
            arrow = null; pointer = null; lift = 0;
            hideOff = true;                                             // 물체가 아닌 자리 표시라 화면 밖이면 가장자리로 끌어오지 않고 숨김
            return this;
        }

        static Image Img(Transform parent, string name, float scale, Color c)
        {
            var g = new GameObject(name, typeof(RectTransform));
            g.transform.SetParent(parent, false);
            ((RectTransform)g.transform).sizeDelta = new Vector2(Base, Base) * scale;
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
            bool onPlayer = target && pc && target == pc.transform;
            var half = ((RectTransform)canvas.transform).rect.size / 2f;
            float m = Base * edge * 0.5f * sizeMul + 6f;
            if (hideOff && (!Visible(lp, half, 0f) || HitsHud(new Rect(lp.x - m, lp.y - m, m * 2f, m * 2f), half))) { foreach (Transform c in transform) c.gameObject.SetActive(false); return; }   // 칸 숫자가 HUD에 걸치면 숨김
            if (arrow)
            {
                if (onPlayer && HeadDash.TryAbove(0f, out var dashTop)) lp = dashTop;      // 화살촉 끝 = 머리 위 대시 줄 바로 위
                PlaceWithArrow(lp, half, m);
            }
            else
            {
                if (onPlayer && HeadDash.TryAbove(Base * edge * sizeMul, out var above)) lp = above;
                // 화면 밖으로 나가지 않게 가장자리 안쪽에 붙잡음 (키 큰 보스 얼굴이 화면 위로 잘리지 않게)
                lp.x = Mathf.Clamp(lp.x, -half.x + m, half.x - m);
                lp.y = Mathf.Clamp(lp.y, -half.y + m, half.y - m * 1.2f);
                rt.anchoredPosition = lp;
            }
            // 펀치(글이 바뀔 때) + 위험 색이면 빠른 맥동
            punch = Mathf.MoveTowards(punch, 0, Time.unscaledDeltaTime * 4f);
            bool danger = col.g < 0.5f;
            float pulse = danger ? 1f + 0.08f * Mathf.Sin(Time.unscaledTime * 18f) : 1f;
            rt.localScale = Vector3.one * (1f + 0.35f * punch * punch) * pulse * (indicator ? IndScale : 1f);
        }
    }
}
