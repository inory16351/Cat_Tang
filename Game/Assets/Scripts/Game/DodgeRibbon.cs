// 극한 회피 리본 궤적 (2026-11-17 사용자: 극한 회피 이펙트 — 몸 감싸는 고리 대신 리본 궤적)
// - 대시를 시작한 자리에서 지금 자리까지, 고양이 몸을 휘감으며 지나가는 하늘색 리본 두 가닥(서로 반 바퀴 어긋남)
// - 머리가 0.16초 동안 끝까지 그려지고 → 꼬리가 0.28초 동안 따라 들어가며 사라짐. 두께는 머리 쪽이 굵고 꼬리로 가늘게
// - 실제 시간(unscaled)으로 돌아 슬로우 중에도 일정, Vivid 레이어라 회색 화면에서도 제 색 (극한 회피 잔상과 같은 규칙)
// - FX_DodgePerfect 프리팹(작은 고리 섬광 + 불티)과 같이 나옴. 예전 큰 고리 조각(GptDodge)은 뺌
using UnityEngine;

namespace CatTang
{
    public class DodgeRibbon : MonoBehaviour
    {
        const int N = 48;
        const float Draw = 0.16f, Fade = 0.28f;
        static Material mat;

        LineRenderer lr;
        readonly Vector3[] path = new Vector3[N];
        float t;

        /// <summary>from(대시 시작) → to(지금) 사이를 휘감는 리본 두 가닥</summary>
        public static void Spawn(Vector3 from, Vector3 to, Color col)
        {
            if (!mat)
            {
                var sh = Shader.Find("CatTang/Sprite") ?? Shader.Find("CatTang/AdditiveGlow");   // 알파 섞기: 밝은 모래 위에서도 하늘색이 하얗게 날아가지 않게
                if (!sh) return;
                mat = new Material(sh) { renderQueue = 3060 };
            }
            for (int k = 0; k < 2; k++) Make(from, to, col, k * 180f, k == 0 ? 1f : 0.7f);
        }

        static void Make(Vector3 from, Vector3 to, Color col, float phase, float widthMul)
        {
            var go = new GameObject("DodgeRibbon");
            var r = go.AddComponent<DodgeRibbon>();
            r.lr = go.AddComponent<LineRenderer>();
            r.lr.sharedMaterial = mat;
            r.lr.useWorldSpace = true;
            r.lr.positionCount = 0;
            r.lr.numCapVertices = 4;
            r.lr.alignment = LineAlignment.View;
            r.lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.lr.receiveShadows = false;
            r.lr.widthCurve = new AnimationCurve(new Keyframe(0, 0.02f), new Keyframe(0.7f, 0.22f * widthMul), new Keyframe(1, 0.3f * widthMul));
            var hot = Color.Lerp(col, Color.white, 0.55f);
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(col, 0), new GradientColorKey(col, 0.6f), new GradientColorKey(hot, 1) },
                      new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(0.9f, 0.35f), new GradientAlphaKey(1, 1) });
            r.lr.colorGradient = g;
            // 경로: 시작 → 지금으로 가며 몸 둘레를 한 바퀴 반 감고 허리 → 가슴 높이로 올라옴, 끝은 몸에 붙음
            var flat = to - from; flat.y = 0;
            var fwd = flat.sqrMagnitude > 0.01f ? flat.normalized : Vector3.forward;
            var side = Vector3.Cross(Vector3.up, fwd);
            for (int i = 0; i < N; i++)
            {
                float u = i / (N - 1f);
                float a = (phase + u * 540f) * Mathf.Deg2Rad;
                float rad = Mathf.Lerp(0.25f, 0.85f, Mathf.Sin(u * Mathf.PI)) * (1 - 0.6f * u * u);   // 가운데에서 가장 크게 휘고 끝은 몸으로
                var c = Vector3.Lerp(from, to, Mathf.SmoothStep(0, 1, u)) + Vector3.up * Mathf.Lerp(0.45f, 1.25f, u);
                r.path[i] = c + (side * Mathf.Cos(a) + fwd * Mathf.Sin(a) * 0.6f) * rad;
            }
            VividLayer.SetAll(go);
        }

        void Update()
        {
            t += Time.unscaledDeltaTime;
            // 머리가 그려지는 끝(head)과 꼬리(tail)를 0~1로 → 그 사이 점만 보여 줌
            float head = Mathf.Clamp01(t / Draw);
            float tail = Mathf.Clamp01((t - Draw * 0.6f) / Fade);
            if (tail >= 1f) { Destroy(gameObject); return; }
            int i0 = Mathf.FloorToInt(tail * (N - 1)), i1 = Mathf.CeilToInt((1 - (1 - head) * (1 - head)) * (N - 1));
            int n = Mathf.Max(0, i1 - i0 + 1);
            lr.positionCount = n;
            for (int i = 0; i < n; i++) lr.SetPosition(i, path[i0 + i]);
        }
    }
}
