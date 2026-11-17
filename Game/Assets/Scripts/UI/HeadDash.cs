// 고양이 머리 위 남은 대시 '<<<' (2026-11-17 사용자: 머리 위에 <<<로 남은 대시 횟수, 머리 위 패턴 UI는 그 위에 / 같은 날: 좀 작게, 비네트에 맞게)
// - 그림: Codex(docs/images/ui_src/prompts/head_dash.txt → docs/tools/ui_dash_cut.py → Resources/UI/head_dash.png) '<' 한 칸을 겹쳐 '<<<'
// - 화면 UI가 아니라 월드에 띄운 빌보드 스프라이트 → 후처리(극한 회피 회색 · 피격 붉은 비네트 · 색수차)를 화면과 똑같이 받음.
//   벽에 가려지지 않게 ZTest Always, 카메라 거리와 상관없이 화면에서 늘 같은 크기(1080 기준 HeightPx)
// - 찬 칸 = 밝은 청록빛 흰색, 차오르는 칸 = 어두운 회색에서 진행만큼 밝아짐(검표 펀치 잠김이면 붉게 깜빡), 빈 칸 = 흐린 어두운 색. 다 차는 순간 톡 튐
// - 고양이를 따라다니는 화면 배지(BigLabel 현상금 표식 등)는 TryAbove로 이 줄 바로 위에 올라감
using UnityEngine;

namespace CatTang
{
    public class HeadDash : MonoBehaviour
    {
        public const float HeightPx = 30f;               // 줄 높이 (1080 기준 화면 px)
        const float StepPx = 17f;                        // 칸 간격 (겹쳐서 <<<)
        const float LiftM = 0.3f;                        // 머리 끝에서 띄우는 높이 (m)
        const int MaxPips = 5;
        static readonly Color Full = new(0.78f, 0.95f, 1f, 0.95f);
        static readonly Color Charging = new(0.55f, 0.6f, 0.66f, 0.85f);
        static readonly Color Empty = new(0.16f, 0.15f, 0.2f, 0.5f);
        static readonly Color Locked = new(1f, 0.25f, 0.4f, 0.95f);

        static HeadDash instance;
        static float headY = -1f;                        // 고양이 발 → 머리 끝 높이 (처음 한 번 잼)

        GameUI ui;
        readonly SpriteRenderer[] pips = new SpriteRenderer[MaxPips];
        readonly float[] pop = new float[MaxPips];
        readonly bool[] wasFull = new bool[MaxPips];
        float spriteH = 1f;
        bool shown = true;

        public static void Create(GameUI owner)
        {
            if (instance) { instance.ui = owner; return; }
            var spr = UIKit.Spr("head_dash");
            if (!spr) { Debug.LogWarning("[HeadDash] Resources/UI/head_dash.png 없음 — 머리 위 대시 표시 안 함"); return; }
            instance = new GameObject("HeadDash").AddComponent<HeadDash>();
            instance.ui = owner;
            instance.Build(spr);
        }

        void Build(Sprite spr)
        {
            var sh = Shader.Find("CatTang/Sprite") ?? Shader.Find("Sprites/Default");
            var mat = new Material(sh) { renderQueue = 3500 };                // 다른 반투명 이펙트 위
            if (mat.HasProperty("_ZTest")) mat.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
            spriteH = Mathf.Max(0.01f, spr.bounds.size.y);
            for (int i = 0; i < MaxPips; i++)
            {
                var g = new GameObject("Pip" + i);
                g.transform.SetParent(transform, false);
                var sr = g.AddComponent<SpriteRenderer>();
                sr.sprite = spr;
                sr.sharedMaterial = mat;
                sr.sortingOrder = 100 + i;
                sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                sr.receiveShadows = false;
                pips[i] = sr;
            }
        }

        static float HeadY(PlayerController p)
        {
            if (headY > 0) return headY;
            float top = 0;
            foreach (var r in (p.model ? p.model : p.transform).GetComponentsInChildren<Renderer>())
                if (r is SkinnedMeshRenderer || r is MeshRenderer) top = Mathf.Max(top, r.bounds.max.y - p.transform.position.y);
            headY = top > 0.5f ? Mathf.Clamp(top, 1f, 3f) : 1.8f;
            return headY;
        }

        static Vector3 HeadPoint(PlayerController p) => p.transform.position + Vector3.up * (HeadY(p) + LiftM);

        /// <summary>1080 기준 px 하나가 머리 위치에서 월드 몇 m인지</summary>
        static float MetersPerPx(Camera cam, Vector3 at)
        {
            float d = Vector3.Dot(at - cam.transform.position, cam.transform.forward);
            float worldH = cam.orthographic ? cam.orthographicSize * 2f : 2f * Mathf.Max(0.1f, d) * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            return worldH / 1080f;
        }

        /// <summary>머리 위 대시 줄 가운데의 화면 UI(BigLabel 캔버스) 로컬 좌표. 고양이 · 카메라가 없으면 false</summary>
        public static bool TryAnchor(out Vector2 local)
        {
            local = Vector2.zero;
            var p = PlayerController.Instance;
            var cam = Camera.main;
            if (!p || !cam) return false;
            var sp = cam.WorldToScreenPoint(HeadPoint(p));
            if (sp.z < 0) return false;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)BigLabel.Canvas().transform, sp, null, out local);
            local.y += HeightPx / 2;
            return true;
        }

        /// <summary>고양이를 따라다니는 배지(지름 diameter px)를 놓을 자리: 대시 줄 바로 위</summary>
        public static bool TryAbove(float diameter, out Vector2 local)
        {
            if (!TryAnchor(out local)) return false;
            local.y += HeightPx / 2 + diameter / 2 + 4f;
            return true;
        }

        void LateUpdate()
        {
            var p = PlayerController.Instance;
            var cam = Camera.main;
            bool show = p && cam && p.health && p.health.Alive && p.gameObject.activeInHierarchy && !(ui && ui.ScreenOpen);
            if (!show)
            {
                if (shown) foreach (var sr in pips) sr.enabled = false;
                shown = false;
                return;
            }
            shown = true;

            // 카메라를 보는 빌보드, 줄 아래 가운데가 머리 위 점. 화면 크기 일정
            var head = HeadPoint(p);
            float m = MetersPerPx(cam, head);
            transform.rotation = cam.transform.rotation;
            transform.position = head + cam.transform.up * (HeightPx / 2 * m);
            float glyphScale = HeightPx * m / spriteH;

            int n = Mathf.Clamp(p.dashMax, 1, MaxPips);
            float dc = p.DashCharges, dp = p.DashRechargeProgress;
            float x0 = -StepPx * (n - 1) / 2f;
            for (int i = 0; i < MaxPips; i++)
            {
                var sr = pips[i];
                bool on = i < n;
                sr.enabled = on;
                if (!on) continue;
                bool full = dc >= i + 1;
                bool filling = !full && i == Mathf.FloorToInt(dc);
                Color c = full ? Full : filling ? Color.Lerp(Empty, Charging, dp) : Empty;
                if (filling && p.DashLocked) c = Color.Lerp(Charging, Locked, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 18f));
                sr.color = c;
                if (full && !wasFull[i]) pop[i] = 1f;
                wasFull[i] = full;
                pop[i] = Mathf.MoveTowards(pop[i], 0, Time.unscaledDeltaTime * 4f);
                var t = sr.transform;
                t.localPosition = new Vector3((x0 + StepPx * i) * m, 0, -0.001f * i);   // 오른쪽 칸이 앞에 겹침
                t.localScale = Vector3.one * glyphScale * ((full ? 1f : 0.86f) + 0.3f * pop[i] * pop[i]);
            }
        }
    }
}
