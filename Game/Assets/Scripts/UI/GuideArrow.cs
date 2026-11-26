// 길 안내 화살표 (2026-11-23 사용자: "화면에 화살표로 전투 지역 문 표시하여 유도" · "여관 2층 기억의 방 해금 (화살표로 표기)")
// 대상 머리 위에서 통통 튀는 노란 화살표 + 이름. 대상이 화면 밖이면 화면 가장자리에 붙어 그쪽을 가리킴.
// 고양이가 대상 가까이(reach m) 가거나 Clear()를 부르면 사라짐. 보통 한 번에 하나, keep이면 여러 개 (2026-11-24: 기억의 방 · 의상실 동시 해금)
// 작은 표시(Marker, 2026-11-25 전투 끝 보상 문 안내): 화면 안이면 대상 위에 작은 화살표만(이름은 대상이 이미 달고 있음),
//   화면 밖이면 가장자리 화살표 + 이름. 같은 묶음(group)이 여러 개 화면 밖이면 가장자리 화살표 하나로 모아 그 무리 쪽을 가리킴 (겹침 방지).
//   가까이 가도 지워지지 않고 그동안만 숨김 — 문에 들어가거나(대상이 사라짐) ClearGroup으로 지움
using UnityEngine;
using UnityEngine.UI;

namespace CatTang
{
    public class GuideArrow : MonoBehaviour
    {
        static readonly System.Collections.Generic.List<GuideArrow> all = new();
        Transform target;
        float reach, height;
        RectTransform root, arrow, labelRt;
        Text label;
        Image img;
        // 작은 표시
        bool marker;
        string group, text, groupText;
        static GameUI gameUi;

        public static void Show(Transform target, string text, float height = 3.2f, float reach = 3f, bool keep = false)
        {
            if (!keep) Clear();
            if (!target) return;
            var go = new GameObject("GuideArrow");
            var inst = go.AddComponent<GuideArrow>();
            inst.target = target; inst.height = height; inst.reach = reach;
            inst.Build(text);
            all.Add(inst);
        }

        /// <summary>작은 표시 (다른 화살표를 지우지 않음). group = 같이 지우고 · 화면 밖에서 하나로 모을 묶음 이름,
        /// groupText = 묶음이 여럿 화면 밖일 때 가장자리 화살표 이름. hideNear = 고양이가 이만큼 가까우면 숨김</summary>
        public static void Marker(Transform target, string text, string group, string groupText, float height = 3.2f, float hideNear = 2.6f)
        {
            if (!target) return;
            var go = new GameObject("GuideMarker");
            var inst = go.AddComponent<GuideArrow>();
            inst.target = target; inst.height = height; inst.reach = hideNear;
            inst.marker = true; inst.group = group; inst.text = text; inst.groupText = groupText;
            inst.Build(text);
            inst.arrow.sizeDelta = new Vector2(50, 50);
            inst.label.fontSize = 26;
            all.Add(inst);
        }

        public static void Clear() { foreach (var a in all) if (a) Destroy(a.gameObject); all.Clear(); }

        /// <summary>그 묶음의 작은 표시만 지움 (다른 안내 화살표는 그대로)</summary>
        public static void ClearGroup(string group)
        {
            for (int i = all.Count - 1; i >= 0; i--)
            {
                var a = all[i];
                if (a && a.group != group) continue;
                if (a) Destroy(a.gameObject);
                all.RemoveAt(i);
            }
        }

        void Remove() { all.Remove(this); Destroy(gameObject); }
        /// <summary>그 대상을 썼으면(문 열기 · 시간의 문) 화살표 치움</summary>
        public static void Reached(Transform t) { for (int i = all.Count - 1; i >= 0; i--) if (all[i] && all[i].target == t) all[i].Remove(); }
        bool wasFar;
        public static bool Active => all.Count > 0;

        void Build(string text)
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 40;   // HUD 위 · 대화/안내 창 아래
            var sc = gameObject.AddComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080); sc.matchWidthOrHeight = 0.5f;
            UiScaleFollow.Add(sc);   // 환경설정 UI 크기 (2026-11-26)
            root = (RectTransform)transform;
            arrow = UIKit.MakeRect("Arrow", root, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(70, 70));
            img = arrow.gameObject.AddComponent<Image>(); img.sprite = ArrowSprite(); img.raycastTarget = false;
            label = UIKit.Label("Label", root, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420, 50), 30, TextAnchor.MiddleCenter, true, new Color(1f, 0.84f, 0.29f));
            label.text = text; label.raycastTarget = false;
            var ol = label.gameObject.AddComponent<Outline>(); ol.effectColor = new Color(0.08f, 0.06f, 0.1f, 0.9f); ol.effectDistance = new Vector2(2, -2);
            labelRt = label.rectTransform;
        }

        /// <summary>대상 위 자리를 이 캔버스 좌표로. off = 화면 밖(또는 카메라 뒤)</summary>
        bool Project(Camera cam, Transform t, float h, out Vector2 local)
        {
            var sp = cam.WorldToScreenPoint(t.position + Vector3.up * h);
            bool behind = sp.z < 0;
            if (behind) sp = new Vector3(Screen.width - sp.x, Screen.height - sp.y, 0);   // 뒤에 있으면 반대로
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, sp, null, out local);
            var half = root.rect.size * 0.5f - new Vector2(80, 80);
            return behind || Mathf.Abs(local.x) > half.x || Mathf.Abs(local.y) > half.y;
        }

        bool NearPlayer(PlayerController p) => p && target && Vector3.Distance(p.transform.position, target.position) < reach;

        void SetVisible(bool on) { img.enabled = label.enabled = on; }

        void LateUpdate()
        {
            var p = PlayerController.Instance;
            var cam = Camera.main;
            if (!target) { Remove(); return; }
            // 가까이 가면 사라짐 — 단, 시작부터 가까이 있었으면(깨어난 자리 옆 문) 한 번 멀어졌다 돌아올 때만. 문 · 시간의 문을 쓰면 바로 사라짐(Reached)
            // 작은 표시는 지우지 않고 가까이 있는 동안만 숨김
            if (p && !marker)
            {
                float d = Vector3.Distance(p.transform.position, target.position);
                if (d > reach + 1.5f) wasFar = true;
                if (wasFar && d < reach) { Remove(); return; }
            }
            if (!cam) return;
            // 대화 · 안내 창 중엔 숨김 (작은 표시는 보상 · 상점 · 보관함 창이 열려 있을 때도)
            bool hide = StoryOverlay.HintOpen || (p && p.InputLocked);
            if (marker && !hide)
            {
                if (!gameUi) gameUi = FindFirstObjectByType<GameUI>();
                hide = (gameUi && gameUi.ScreenOpen) || NearPlayer(p);
            }
            SetVisible(!hide);
            if (hide) return;

            bool off = Project(cam, target, height, out var local);
            var half = root.rect.size * 0.5f - new Vector2(80, 80);
            float bob = Mathf.Sin(Time.unscaledTime * 5f) * 12f;
            if (!off)
            {
                arrow.localRotation = Quaternion.identity;                       // 아래를 가리킴
                if (marker)
                {
                    // 작은 표시: 대상 위 작은 화살표만 (이름은 문이 이미 달고 있음)
                    arrow.anchoredPosition = local + new Vector2(0, 30 + bob * 0.6f);
                    label.enabled = false;
                    return;
                }
                arrow.anchoredPosition = local + new Vector2(0, 40 + bob);
                labelRt.anchoredPosition = local + new Vector2(0, 110 + bob);
                return;
            }
            string shown = text;
            if (marker)
            {
                // 같은 묶음 중 화면 밖인 것들: 맨 앞 하나만 그려서 무리 가운데 쪽을 가리킴
                GuideArrow leader = null; int n = 0; Vector2 sum = Vector2.zero;
                foreach (var a in all)
                {
                    if (!a || !a.marker || a.group != group || !a.target || a.NearPlayer(p)) continue;
                    if (!a.Project(cam, a.target, a.height, out var al)) continue;
                    if (!leader) leader = a;
                    sum += al; n++;
                }
                if (leader && leader != this) { SetVisible(false); return; }
                if (n > 1) { local = sum / n; shown = groupText; }
            }
            // 화면 가장자리: 가운데에서 대상 쪽으로 뻗은 선이 테두리에 닿는 자리
            var dir = local.sqrMagnitude < 1 ? Vector2.down : local.normalized;
            float k = Mathf.Min(dir.x != 0 ? half.x / Mathf.Abs(dir.x) : float.MaxValue, dir.y != 0 ? half.y / Mathf.Abs(dir.y) : float.MaxValue);
            var edge = dir * k;
            arrow.anchoredPosition = edge + dir * bob * 0.6f;
            arrow.localRotation = Quaternion.FromToRotation(Vector3.down, dir);
            labelRt.anchoredPosition = edge - dir * 70f;
            if (marker && label.text != shown) label.text = shown;
        }

        static Sprite arrowSprite;
        /// <summary>아래를 가리키는 노란 삼각 화살표 (짙은 테두리)</summary>
        static Sprite ArrowSprite()
        {
            if (arrowSprite) return arrowSprite;
            const int N = 96;
            var t = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[N * N];
            var fill = new Color(1f, 0.84f, 0.29f); var line = new Color(0.1f, 0.07f, 0.12f);
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    // 위가 넓고 아래가 뾰족한 삼각형: 위 변 y=0.86, 꼭지 (0.5, 0.08)
                    float u = (x + 0.5f) / N, v = (y + 0.5f) / N;
                    float w = Mathf.InverseLerp(0.08f, 0.86f, v) * 0.42f;            // 높이에 따른 반폭
                    float d = Mathf.Min(w - Mathf.Abs(u - 0.5f), 0.86f - v);          // 안쪽 거리 (양수 = 안)
                    d = Mathf.Min(d, (v - 0.08f) * 0.5f);
                    Color c = d > 0.05f ? fill : d > 0f ? line : new Color(0, 0, 0, 0);
                    if (d > 0f && d < 0.012f) c.a = d / 0.012f;
                    px[y * N + x] = c;
                }
            t.SetPixels32(px); t.Apply(false, true);
            arrowSprite = Sprite.Create(t, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f));
            return arrowSprite;
        }
    }
}
