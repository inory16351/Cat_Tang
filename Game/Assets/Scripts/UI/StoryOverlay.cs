// 스토리 연출 화면 (2026-11-23 스토리 기획서 Ver01): 오프닝 몽타주 · 꿈(수채화 셀 애니메이션) · 두통 · 하얀 번쩍임 · 튜토리얼 안내
// 맨 위 캔버스 하나에 그림. 그림 자리는 Resources/Story/... — 그림이 아직 없으면 장면 설명을 흐리게 띄우는 자리 표시로 대신함 (그림은 크롬 ChatGPT로 만들 예정)
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CatTang
{
    public class StoryOverlay : MonoBehaviour
    {
        static StoryOverlay inst;
        public static StoryOverlay I
        {
            get
            {
                if (inst) return inst;
                UIKit.Init();
                var go = new GameObject("StoryOverlay");
                inst = go.AddComponent<StoryOverlay>();
                inst.Build();
                return inst;
            }
        }

        Canvas canvas;
        RectTransform root, shotRt;
        Image black, shot, shot2, white, ache, hintPanel, blocker;
        /// <summary>튜토리얼 안내 창이 떠 있음 (GameUI가 Tab · Esc를 무시)</summary>
        public static bool HintOpen { get; private set; }
        Text caption, placeholder, hintTitle, hintBody, hintKey;

        void Build()
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900;
            var sc = gameObject.AddComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080); sc.matchWidthOrHeight = 0.5f;
            root = (RectTransform)transform;
            black = Full("Black", Color.black);
            shotRt = UIKit.MakeRect("Shot", root, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1920, 1080));
            shot = shotRt.gameObject.AddComponent<Image>(); shot.preserveAspect = false; shot.raycastTarget = false;
            var s2 = UIKit.MakeRect("Shot2", root, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1920, 1080));
            shot2 = s2.gameObject.AddComponent<Image>(); shot2.raycastTarget = false;
            placeholder = UIKit.Label("Placeholder", root, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1400, 300), 34, TextAnchor.MiddleCenter, false, new Color(1, 1, 1, 0.35f));
            ache = Full("Ache", new Color(0.35f, 0.02f, 0.06f, 0));
            ache.sprite = Vignette(); ache.type = Image.Type.Simple;
            white = Full("White", new Color(1, 1, 1, 0));
            caption = UIKit.Label("Caption", root, new Vector2(0.5f, 0), new Vector2(0, 400), new Vector2(1400, 80), 30, TextAnchor.MiddleCenter, false, UIKit.Cream);   // 총알 시계 HUD(아래 가운데) 위로 비킴
            blocker = Full("HintBlocker", new Color(0, 0, 0, 0));                 // 안내가 떠 있는 동안 아래 화면(인벤토리 등) 클릭을 막음
            hintPanel = UIKit.Panel("Hint", root, new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(980, 380), true);
            hintTitle = UIKit.Banner("", hintPanel.transform, new Vector2(0.5f, 1), new Vector2(0, 6), 520);
            hintBody = UIKit.Label("Body", hintPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0, -10), new Vector2(860, 220), 28, TextAnchor.MiddleCenter, false, UIKit.Cream);
            hintKey = UIKit.Label("Key", hintPanel.transform, new Vector2(0.5f, 0), new Vector2(0, 34), new Vector2(600, 40), 22, TextAnchor.MiddleCenter, false, new Color(1, 1, 1, 0.6f));
            hintKey.text = "E · 클릭  계속";
            foreach (var g in GetComponentsInChildren<Graphic>(true)) g.raycastTarget = false;
            gameObject.AddComponent<GraphicRaycaster>();
            blocker.raycastTarget = true; blocker.gameObject.SetActive(false);
            Clear();
            DontDestroyOnLoad(gameObject);
        }

        Image Full(string n, Color c)
        {
            var rt = UIKit.Stretch(n, root);
            var im = rt.gameObject.AddComponent<Image>(); im.color = c; im.raycastTarget = false;
            return im;
        }

        /// <summary>가장자리가 짙은 둥근 그림자 (두통 · 기절 때 화면 가장자리)</summary>
        static Sprite vig;
        static Sprite Vignette()
        {
            if (vig) return vig;
            const int N = 128;
            var t = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float dx = (x + 0.5f) / N * 2 - 1, dy = (y + 0.5f) / N * 2 - 1;
                    float d = Mathf.Sqrt(dx * dx * 0.8f + dy * dy);
                    t.SetPixel(x, y, new Color(1, 1, 1, Mathf.SmoothStep(0.45f, 1.15f, d)));
                }
            t.Apply();
            return vig = Sprite.Create(t, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f));
        }

        /// <summary>둥근 부드러운 물감 덩어리 (꿈 자리 표시용)</summary>
        static Sprite blob;
        static Sprite Blob()
        {
            if (blob) return blob;
            const int N = 128;
            var t = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float dx = (x + 0.5f) / N * 2 - 1, dy = (y + 0.5f) / N * 2 - 1;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) + 0.08f * Mathf.Sin(Mathf.Atan2(dy, dx) * 5);
                    t.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(1 - d) * Mathf.Clamp01(1 - d)));
                }
            t.Apply();
            return blob = Sprite.Create(t, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f));
        }

        public void Clear()
        {
            black.color = new Color(0, 0, 0, 0);
            shot.enabled = false; shot2.enabled = false;
            placeholder.text = ""; caption.text = "";
            ache.color = new Color(ache.color.r, ache.color.g, ache.color.b, 0);
            white.color = new Color(1, 1, 1, 0);
            hintPanel.gameObject.SetActive(false);
            blocker.gameObject.SetActive(false);
            foreach (Transform c in root) if (c.name == "DreamBlob") Destroy(c.gameObject);
        }

        StoryMontage montage;
        /// <summary>오프닝 몽타주 재생기 (레이어 · 심도 · 깨진 유리 / 조각 섞기 전환) — 검은 바탕 바로 위</summary>
        public StoryMontage Montage()
        {
            if (!montage) { montage = StoryMontage.Create(root); montage.transform.SetSiblingIndex(1); }
            montage.gameObject.SetActive(true);
            return montage;
        }
        public void HideMontage() { if (montage) montage.gameObject.SetActive(false); }

        public void SetBlack(float a) => black.color = new Color(0, 0, 0, a);
        public void SetWhite(float a) => white.color = new Color(1, 1, 1, a);
        public void SetAche(float a) => ache.color = new Color(ache.color.r, ache.color.g, ache.color.b, a);
        public void SetCaption(string t) => caption.text = t ?? "";

        public IEnumerator FadeBlack(float from, float to, float dur)
        {
            for (float t = 0; t < dur; t += Time.unscaledDeltaTime) { SetBlack(Mathf.Lerp(from, to, t / dur)); yield return null; }
            SetBlack(to);
        }

        /// <summary>장면을 바꾼 뒤 검은 화면을 걷어 냄 — 이 화면(장면이 바뀌어도 남음)에서 돌려야 함 (부른 쪽은 장면과 함께 사라짐)</summary>
        public void RevealAfterLoad(float delay = 0.6f, float dur = 1.1f) => StartCoroutine(RevealRun(delay, dur));
        IEnumerator RevealRun(float delay, float dur)
        {
            SetBlack(1);
            yield return new WaitForSecondsRealtime(delay);
            yield return FadeBlack(1, 0, dur);
        }

        public IEnumerator FadeWhite(float from, float to, float dur)
        {
            for (float t = 0; t < dur; t += Time.unscaledDeltaTime) { SetWhite(Mathf.Lerp(from, to, t / dur)); yield return null; }
            SetWhite(to);
        }

        /// <summary>두통: 가장자리 붉은 그림자가 박동처럼 두 번 조였다 풀림</summary>
        public IEnumerator Headache(float strength = 0.75f, float dur = 1.4f)
        {
            for (float t = 0; t < dur; t += Time.unscaledDeltaTime)
            {
                float u = t / dur;
                SetAche(strength * Mathf.Abs(Mathf.Sin(u * Mathf.PI * 2)) * (1 - u * 0.5f));
                yield return null;
            }
            SetAche(0);
        }

        static float devNext;
        static bool Next()
        {
            if (GameUI.DevAutoAdvance && Time.unscaledTime >= devNext) { devNext = Time.unscaledTime + 2f; return true; }
            var kb = Keyboard.current; var ms = Mouse.current;
            return (kb != null && (kb.eKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)) || (ms != null && ms.leftButton.wasPressedThisFrame);
        }

        // 스포트라이트: 설명하는 HUD만 밝게 남기고 나머지를 어둡게 + 금빛 테두리가 맥동 (창이 HUD를 가리지 않게 위로 비킴)
        // 노란 테두리 + 안내 창과 비춘 곳을 잇는 노란 선 (2026-11-23 사용자: "노란색과 선으로 연결해서 좀더 알기 쉽게 · 등장도 연출")
        static readonly Color Yellow = new(1f, 0.84f, 0.18f);
        const float DimA = 0.8f, RingW = 5f, LineW = 5f;
        Image[] dims, ring; Image line, dotA, dotB; CanvasGroup hintCg;

        Image TopLeft(string n, Color c, Vector2 pivot)
        {
            var im = UIKit.MakeRect(n, root, new Vector2(0, 1), Vector2.zero, Vector2.zero, pivot).gameObject.AddComponent<Image>();
            im.color = c; im.raycastTarget = false;
            return im;
        }

        void EnsureSpot()
        {
            if (dims != null) return;
            dims = new Image[4]; ring = new Image[4];
            for (int i = 0; i < 4; i++) dims[i] = TopLeft("Dim", new Color(0.02f, 0.02f, 0.05f, DimA), new Vector2(0, 1));
            for (int i = 0; i < 4; i++) ring[i] = TopLeft("Ring", Yellow, new Vector2(0, 1));
            line = TopLeft("Line", Yellow, new Vector2(0, 0.5f));
            dotA = TopLeft("DotA", Yellow, new Vector2(0.5f, 0.5f)); dotA.sprite = Dot();
            dotB = TopLeft("DotB", Yellow, new Vector2(0.5f, 0.5f)); dotB.sprite = Dot();
            hintCg = hintPanel.gameObject.AddComponent<CanvasGroup>(); hintCg.blocksRaycasts = false;
            hintPanel.transform.SetAsLastSibling();
            SpotOff();
        }

        void SpotOff()
        {
            foreach (var d in dims) d.gameObject.SetActive(false);
            foreach (var r in ring) r.gameObject.SetActive(false);
            line.gameObject.SetActive(false); dotA.gameObject.SetActive(false); dotB.gameObject.SetActive(false);
        }

        static void Box(Image im, float x, float y, float w, float h) { var rt = im.rectTransform; rt.anchoredPosition = new Vector2(x, -y); rt.sizeDelta = new Vector2(Mathf.Max(0, w), Mathf.Max(0, h)); }
        static void Alpha(Image im, float a) { var c = im.color; c.a = a; im.color = c; }

        /// <summary>f만 밝히고 나머지를 dim만큼 어둡게 + 노란 테두리 (ringA = 테두리 진하기)</summary>
        void SpotRect(Rect f, float dim, float ringA)
        {
            float W = root.rect.width, H = root.rect.height; const float b = RingW;
            foreach (var d in dims) { d.gameObject.SetActive(true); Alpha(d, dim); }
            foreach (var r in ring) { r.gameObject.SetActive(true); Alpha(r, ringA); }
            Box(dims[0], 0, 0, W, f.yMin);                                   // 위
            Box(dims[1], 0, f.yMax, W, H - f.yMax);                          // 아래
            Box(dims[2], 0, f.yMin, f.xMin, f.height);                       // 왼쪽
            Box(dims[3], f.xMax, f.yMin, W - f.xMax, f.height);              // 오른쪽
            Box(ring[0], f.xMin - b, f.yMin - b, f.width + b * 2, b);
            Box(ring[1], f.xMin - b, f.yMax, f.width + b * 2, b);
            Box(ring[2], f.xMin - b, f.yMin, b, f.height);
            Box(ring[3], f.xMax, f.yMin, b, f.height);
        }

        /// <summary>a(창 가장자리) → b(비춘 곳 가장자리) 노란 선을 u만큼 뻗음 (왼쪽 위 원점 좌표)</summary>
        void LineTo(Vector2 a, Vector2 b, float u, float alpha)
        {
            var d = b - a; float len = d.magnitude;
            bool on = len > 4 && u > 0;
            line.gameObject.SetActive(on); dotA.gameObject.SetActive(on); dotB.gameObject.SetActive(on && u >= 1);
            if (!on) return;
            var rt = line.rectTransform;
            rt.anchoredPosition = new Vector2(a.x, -a.y);
            rt.sizeDelta = new Vector2(len * u, LineW);
            rt.localRotation = Quaternion.Euler(0, 0, -Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            dotA.rectTransform.anchoredPosition = new Vector2(a.x, -a.y); dotA.rectTransform.sizeDelta = Vector2.one * 14;
            dotB.rectTransform.anchoredPosition = new Vector2(b.x, -b.y);
            Alpha(line, alpha); Alpha(dotA, alpha); Alpha(dotB, alpha);
        }

        /// <summary>속이 찬 동그라미 (선 끝점)</summary>
        static Sprite dot;
        static Sprite Dot()
        {
            if (dot) return dot;
            const int N = 64;
            var t = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float dx = (x + 0.5f) / N * 2 - 1, dy = (y + 0.5f) / N * 2 - 1;
                    t.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01((1 - Mathf.Sqrt(dx * dx + dy * dy)) * N * 0.5f)));
                }
            t.Apply();
            return dot = Sprite.Create(t, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f));
        }

        static float EaseOutBack(float u) { const float c = 1.70158f; float v = u - 1; return 1 + (c + 1) * v * v * v + c * v * v; }
        static float EaseOut(float u) => 1 - (1 - u) * (1 - u) * (1 - u);

        /// <summary>다른 캔버스의 UI들을 감싸는 자리 (이 화면 기준, 왼쪽 위 원점) — 화면비가 달라도 실제 UI에 맞음</summary>
        public Rect FocusOf(float pad, params RectTransform[] targets)
        {
            float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
            var cs = new Vector3[4];
            foreach (var t in targets)
            {
                if (!t) continue;
                var cv = t.GetComponentInParent<Canvas>(); var cam = cv && cv.renderMode != RenderMode.ScreenSpaceOverlay ? cv.worldCamera : null;
                t.GetWorldCorners(cs);
                foreach (var c in cs)
                {
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(root, RectTransformUtility.WorldToScreenPoint(cam, c), null, out var l);
                    float x = l.x + root.rect.width * 0.5f, y = root.rect.height * 0.5f - l.y;
                    x0 = Mathf.Min(x0, x); x1 = Mathf.Max(x1, x); y0 = Mathf.Min(y0, y); y1 = Mathf.Max(y1, y);
                }
            }
            if (x0 > x1) return new Rect(0, 0, 0, 0);
            return Rect.MinMaxRect(x0 - pad, y0 - pad, x1 + pad, y1 + pad);
        }

        /// <summary>안내 창 자리: 비춘 곳을 가리지 않게 위 · 아래 · 오른쪽 · 왼쪽 중 들어가는 곳 (anchoredPosition, 가운데 기준)</summary>
        Vector2 PanelSpot(Rect? focus)
        {
            if (!focus.HasValue) return new Vector2(0, 40);
            var f = focus.Value; float W = root.rect.width, H = root.rect.height; const float pw = 980, ph = 380, gap = 110;   // 노란 선이 보이게 떨어뜨림
            Vector2 C(float cx, float cy) => new Vector2(Mathf.Clamp(cx, pw / 2 + 20, W - pw / 2 - 20) - W / 2, H / 2 - Mathf.Clamp(cy, ph / 2 + 20, H - ph / 2 - 20));
            if (f.yMin - gap - ph >= 20) return C(W / 2, f.yMin - gap - ph / 2);                 // 위
            if (f.yMax + gap + ph <= H - 20) return C(W / 2, f.yMax + gap + ph / 2);             // 아래
            if (f.xMax + gap + pw <= W - 20) return C(f.xMax + gap + pw / 2, f.center.y);       // 오른쪽
            if (f.xMin - gap - pw >= 20) return C(f.xMin - gap - pw / 2, f.center.y);           // 왼쪽
            return f.center.y > H / 2 ? new Vector2(0, 230) : new Vector2(0, -230);
        }

        /// <summary>튜토리얼 안내: 실제 UI(targets)를 비춤 — 인벤토리처럼 다른 화면 위에서도 씀</summary>
        public IEnumerator HintAt(string title, string body, params RectTransform[] targets) => Hint(title, body, FocusOf(10, targets));

        /// <summary>튜토리얼 안내: 시간을 멈추고 창. focus = 설명하는 HUD 자리(1920×1080 기준, 왼쪽 위 원점) — 그곳만 밝히고 창은 비켜 섬. E · 클릭으로 닫힘</summary>
        public IEnumerator Hint(string title, string body, Rect? focus = null)
        {
            float ts = Time.timeScale;
            Time.timeScale = 0f;
            var p = PlayerController.Instance; bool locked = p && p.InputLocked;
            if (p) p.InputLocked = true;
            HintOpen = true;
            EnsureSpot();
            blocker.gameObject.SetActive(true);
            blocker.transform.SetAsLastSibling(); hintPanel.transform.SetAsLastSibling();
            hintPanel.gameObject.SetActive(true);
            hintTitle.text = title; hintBody.text = body;
            Sfx.Play("ui_click_1", 0.6f);
            var rt = (RectTransform)hintPanel.transform;
            Vector2 pos = PanelSpot(focus);
            float W = root.rect.width, H = root.rect.height;
            // 선 양 끝: 창 가장자리(비춘 곳에 가장 가까운 점) → 비춘 곳 가장자리
            Vector2 a = Vector2.zero, b = Vector2.zero, from = Vector2.zero; bool link = false;
            if (focus.HasValue)
            {
                var f = focus.Value;
                var pr = new Rect(W / 2 + pos.x - 490, H / 2 - pos.y - 190, 980, 380);
                a = new Vector2(Mathf.Clamp(f.center.x, pr.xMin, pr.xMax), Mathf.Clamp(f.center.y, pr.yMin, pr.yMax));
                b = new Vector2(Mathf.Clamp(a.x, f.xMin, f.xMax), Mathf.Clamp(a.y, f.yMin, f.yMax));
                link = !f.Contains(a) && (b - a).magnitude > 12;
                var toPanel = pr.center - f.center; from = toPanel.sqrMagnitude > 1 ? new Vector2(toPanel.x, -toPanel.y).normalized * -70f : Vector2.zero;   // 비춘 곳 쪽에서 미끄러져 나옴
            }
            // ① 어두워지며 노란 테두리가 바깥에서 조여 들어옴 · 창이 튀어나옴
            const float T1 = 0.32f;
            for (float t = 0; t < T1; t += Time.unscaledDeltaTime)
            {
                float u = Mathf.Clamp01(t / T1), e = EaseOut(u);
                if (focus.HasValue)
                {
                    var f = focus.Value; float g = Mathf.Lerp(90f, 0f, e);
                    SpotRect(Rect.MinMaxRect(f.xMin - g, f.yMin - g, f.xMax + g, f.yMax + g), DimA * e, e);
                }
                float pu = Mathf.Clamp01((t - 0.06f) / (T1 - 0.06f));
                hintCg.alpha = Mathf.Clamp01(pu * 1.6f);
                rt.localScale = Vector3.one * Mathf.LerpUnclamped(0.8f, 1f, EaseOutBack(pu));
                rt.anchoredPosition = pos + from * (1 - EaseOut(pu));
                yield return null;
            }
            hintCg.alpha = 1; rt.localScale = Vector3.one; rt.anchoredPosition = pos;
            if (focus.HasValue) SpotRect(focus.Value, DimA, 1);
            // ② 창에서 비춘 곳으로 노란 선이 뻗고 끝점이 톡
            if (link)
            {
                for (float t = 0; t < 0.2f; t += Time.unscaledDeltaTime) { LineTo(a, b, EaseOut(t / 0.2f), 1); yield return null; }
                LineTo(a, b, 1, 1);
                Sfx.Play("e4_tick_1", 0.5f);
                for (float t = 0; t < 0.18f; t += Time.unscaledDeltaTime) { dotB.rectTransform.sizeDelta = Vector2.one * Mathf.LerpUnclamped(6f, 22f, EaseOutBack(t / 0.18f)); yield return null; }
            }
            yield return new WaitForSecondsRealtime(0.2f);
            while (!Next())
            {
                float s = Mathf.Sin(Time.unscaledTime * 6f);
                hintKey.color = new Color(1, 1, 1, 0.35f + 0.3f * Mathf.Sin(Time.unscaledTime * 5f));
                if (focus.HasValue) foreach (var r in ring) Alpha(r, 0.65f + 0.35f * s);
                if (link) { Alpha(line, 0.75f + 0.25f * s); dotB.rectTransform.sizeDelta = Vector2.one * (22f + 4f * s); }
                yield return null;
            }
            // 닫힘: 빠르게 걷힘
            Sfx.Play("ui_close", 0.4f);
            for (float t = 0; t < 0.14f; t += Time.unscaledDeltaTime)
            {
                float k = 1 - t / 0.14f;
                hintCg.alpha = k; rt.localScale = Vector3.one * Mathf.Lerp(0.94f, 1f, k);
                if (focus.HasValue) SpotRect(focus.Value, DimA * k, k);
                if (link) LineTo(a, b, 1, k);
                yield return null;
            }
            hintPanel.gameObject.SetActive(false);
            hintCg.alpha = 1; rt.localScale = Vector3.one;
            blocker.gameObject.SetActive(false);
            SpotOff();
            HintOpen = false;
            var ui = FindFirstObjectByType<GameUI>();
            bool screen = ui && ui.ScreenOpen;                // 인벤토리 위에서 띄웠으면 계속 멈춘 채로
            if (p) p.InputLocked = locked || screen;
            Time.timeScale = screen ? 0f : ts <= 0 ? 1f : ts;
        }

        // ---------- 오프닝 몽타주 ----------
        public struct Shot
        {
            public string img, desc;          // Resources/Story/Opening/<img>  · 그림이 없을 때 보일 장면 설명
            public float dur;
            public Vector2 pan0, pan1;        // 화면 이동 (px)
            public float zoom0, zoom1, rot0, rot1, shake;
            public string sfx;
            public Color tint;                // 그림이 없을 때 바탕색
        }

        public IEnumerator PlayShots(Shot[] shots, string folder)
        {
            SetBlack(1);
            foreach (var s in shots)
            {
                var spr = string.IsNullOrEmpty(s.img) ? null : Resources.Load<Sprite>(folder + s.img);
                shot.enabled = true;
                shot.sprite = spr;
                shot.color = spr ? Color.white : s.tint;
                placeholder.text = spr ? "" : s.desc;
                if (!string.IsNullOrEmpty(s.sfx) && Sfx.Has(s.sfx)) Sfx.Play(s.sfx, 0.85f, 0f);
                for (float t = 0; t < s.dur; t += Time.unscaledDeltaTime)
                {
                    float u = Mathf.Clamp01(t / s.dur), e = u * u * (3 - 2 * u);
                    var j = s.shake > 0 ? Random.insideUnitCircle * s.shake : Vector2.zero;
                    shotRt.anchoredPosition = Vector2.Lerp(s.pan0, s.pan1, e) + j;
                    shotRt.localScale = Vector3.one * Mathf.Lerp(s.zoom0 <= 0 ? 1 : s.zoom0, s.zoom1 <= 0 ? 1 : s.zoom1, e);
                    shotRt.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(s.rot0, s.rot1, e));
                    placeholder.rectTransform.anchoredPosition = shotRt.anchoredPosition * 0.3f;
                    yield return null;
                }
            }
            shot.enabled = false; placeholder.text = "";
        }

        // ---------- 꿈: 수채화 셀 애니메이션 ----------
        /// <summary>Resources/Story/Dreams/dream{n}/ 의 그림을 이름순으로 번지듯 겹쳐 넘김. 그림이 없으면 물감 번짐 + 기억 조각 설명 자리 표시</summary>
        public IEnumerator PlayDream(int n, string title, string[] fragments, float each = 4.5f, MShot[] authored = null, bool shotEnd = false)
        {
            Clear();
            var frames = Resources.LoadAll<Texture2D>("Story/Dreams/dream" + n);   // 이름만 씀 (재생은 StoryMontage가 텍스처로)
            System.Array.Sort(frames, (a, b) => string.CompareOrdinal(a.name, b.name));
            var paper = new Color(0.93f, 0.9f, 0.84f);
            black.color = paper;
            if (authored == null && Sfx.Has("story_dream")) Sfx.Play("story_dream", 0.7f, 0f);
            if (authored != null && frames.Length > 0)
            {
                // 짜 둔 컷 목록 (꿈1 '멈춘 손' 등): 오프닝처럼 컷 많고 역동적이되 수채화 — 잔상은 은은하게, 디지털 글리치 대신 번짐
                // 효과음 없이 전용 곡 story_dream{n}만 (2026-11-23 사용자), 재생 중 인게임 곡은 멈춤
                var mt = Montage();
                mt.echo = 0.2f; mt.glitch = 0.6f; mt.glitchCueMin = 18f; mt.paintGlitch = true;   // 글리치 적당히 (2026-11-23 사용자: 너무 많음) mt.vignette = 0.35f;   // 물감 글리치 무작위 (2026-11-23 사용자)
                mt.gradeChance = 0.35f; mt.gradeGlitch = 0.3f;                                    // 배색 변주: 디졸브 · 글리치 조각에 세피아 · 음화 · 청사진 무작위
                mt.sfx = false; mt.mixTrans = 0.6f;                                                // 꿈 전체: 긴 컷 전환을 물감 덮기 ↔ 조각 섞기 + 글리치로 (2026-11-23 사용자)
                Music.Story("story_dream" + n);
                yield return mt.Play(authored, "Story/Dreams/dream" + n + "/");
                Music.StoryEnd(1.2f);
                mt.vignette = 1f; mt.paintGlitch = false; mt.glitch = 1f; mt.gradeChance = 0f; mt.gradeGlitch = 0f; mt.sfx = true; mt.mixTrans = 0f; mt.glitchCueMin = 14f;
                HideMontage();
            }
            else if (frames.Length > 0)
            {
                // 수채화 프레임을 몽타주 재생기로: 천천히 떠밀리며 초점이 잡혀 들어오고(흐림 → 또렷), 장면 사이는 번지는 디졸브와
                // 조각이 엇갈리는 섞기를 번갈아 (2026-11-23 사용자: 엇갈린 시간과 꿈 같은 연출)
                var shots = new MShot[frames.Length];
                for (int i = 0; i < frames.Length; i++)
                {
                    bool odd = i % 2 == 1;
                    shots[i] = new MShot
                    {
                        dur = each, tint = paper,
                        layers = new[] { new MLayer { img = frames[i].name, depth = 1f, blur0 = 0.012f, blur1 = 0.0015f } },
                        pan0 = new Vector2(odd ? 24 : -24, 10), pan1 = new Vector2(odd ? -24 : 24, -10),
                        zoom0 = 1.04f, zoom1 = 1.12f,
                        tin = i == 0 ? MTrans.Dissolve : odd ? MTrans.Mix : MTrans.Dissolve, tinDur = i == 0 ? 1.4f : odd ? 1.1f : 1.3f,
                    };
                }
                var mt = Montage();
                mt.echo = 0.12f; mt.glitch = 0f;          // 꿈(수채화): 잔상만 은은하게, 글리치 없음
                yield return mt.Play(shots, "Story/Dreams/dream" + n + "/");
                HideMontage();
            }
            else
            {
                // 자리 표시: 번지는 물감 덩어리 + 조각 설명 (그림이 생기면 이 경로는 안 씀)
                var cols = new[] { new Color(0.55f, 0.66f, 0.82f), new Color(0.85f, 0.62f, 0.55f), new Color(0.7f, 0.6f, 0.8f), new Color(0.62f, 0.78f, 0.7f) };
                foreach (var frag in fragments)
                {
                    for (int k = 0; k < 4; k++)
                    {
                        var b = UIKit.MakeRect("DreamBlob", root, new Vector2(0.5f, 0.5f), new Vector2(Random.Range(-700, 700), Random.Range(-350, 350)), Vector2.one * Random.Range(500, 900));
                        b.SetSiblingIndex(1);
                        var im = b.gameObject.AddComponent<Image>(); im.sprite = Blob(); im.raycastTarget = false;
                        var c = cols[Random.Range(0, cols.Length)]; im.color = new Color(c.r, c.g, c.b, 0);
                        StartCoroutine(Bleed(im, each * 1.6f));
                    }
                    placeholder.text = frag;
                    placeholder.color = new Color(0.25f, 0.22f, 0.3f, 0);
                    for (float t = 0; t < each; t += Time.unscaledDeltaTime)
                    {
                        float a = Mathf.Min(Mathf.Clamp01(t / 1.2f), Mathf.Clamp01((each - t) / 1.0f));
                        placeholder.color = new Color(0.25f, 0.22f, 0.3f, a * 0.85f);
                        yield return null;
                    }
                }
                placeholder.text = "";
            }
            // 꿈 제목 카드는 뺌 (2026-11-23 사용자: "멈춘 손 없애고") — 깨어나서 "젠장… 또 이상한 꿈을 꿨군…"으로 이어짐 (VillageStory)
            if (shotEnd)
            {
                // 오프닝으로 틀 때 (2026-11-24 사용자): 늘어진 '삐' → 총성 한 발 → 암전 (그림 없이 소리만) → 판 1 깨어남
                if (Sfx.Has("story_flatline")) Sfx.Play("story_flatline", 0.8f, 0f);
                yield return FadeWhite(0, 1, 1.6f);
                Sfx.Play("shot_1", 1f, 0f);
                Clear();
                SetWhite(0);
                SetBlack(1);
                yield return new WaitForSecondsRealtime(1.0f);
                yield break;
            }
            yield return FadeWhite(0, 1, 1.2f);
            Clear();
            SetBlack(1);
        }

        IEnumerator Bleed(Image im, float life)
        {
            var rt = im.rectTransform; var s0 = rt.localScale; var c = im.color;
            for (float t = 0; t < life; t += Time.unscaledDeltaTime)
            {
                if (!im) yield break;
                float u = t / life;
                rt.localScale = s0 * Mathf.Lerp(0.6f, 1.4f, Mathf.Sqrt(u));
                im.color = new Color(c.r, c.g, c.b, 0.55f * Mathf.Sin(u * Mathf.PI));
                yield return null;
            }
            if (im) Destroy(im.gameObject);
        }
    }
}
