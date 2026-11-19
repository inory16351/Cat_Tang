// 로비 '선 하나하나가 그어지는' 연출 (2026-11-18 사용자: 마스크로 드러내지 말고 선 하나하나가 그려지는 느낌)
// 획 경로 파일(Resources/UI/Lobby/strokes_*.txt, docs/tools/ui_lobby_vector.py)을 읽어 진행도(0~1)만큼 실제로 선을 그음:
//  획마다 시작 시각 · 길이가 있고, 진행 중인 획은 경로를 따라 펜 끝까지만 그려짐. 획 양 끝은 살짝 가늘게(손으로 그은 느낌)
// 좌표는 key.png(1920x1080) 픽셀 → 이 그래픽(그림과 같은 크기, art 아래) 안 좌표
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace CatTang
{
    public class LobbyStrokes : MaskableGraphic
    {
        struct Stroke { public float t0, dur, w, len; public Vector2[] p; public float[] cum; }

        readonly List<Stroke> strokes = new();
        float progress = -1f;
        public float widthMul = 1f;

        public static LobbyStrokes Create(RectTransform parent, string res, Color col, float widthMul)
        {
            var ta = Resources.Load<TextAsset>("UI/Lobby/" + res);
            if (!ta) return null;
            var go = new GameObject(res, typeof(RectTransform), typeof(CanvasRenderer));   // CanvasRenderer가 없으면 아무것도 안 그려짐
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            var g = go.AddComponent<LobbyStrokes>();
            g.color = col; g.widthMul = widthMul; g.raycastTarget = false;
            g.Parse(ta.text);
            return g;
        }

        void Parse(string text)
        {
            var ci = CultureInfo.InvariantCulture;
            foreach (var line in text.Split('\n'))
            {
                var v = line.Split(' ');
                if (v.Length < 7) continue;
                var s = new Stroke { t0 = float.Parse(v[0], ci), dur = Mathf.Max(1e-4f, float.Parse(v[1], ci)), w = float.Parse(v[2], ci) };
                int n = (v.Length - 3) / 2;
                s.p = new Vector2[n]; s.cum = new float[n];
                for (int i = 0; i < n; i++)
                {
                    s.p[i] = new Vector2(float.Parse(v[3 + i * 2], ci), float.Parse(v[4 + i * 2], ci));
                    if (i > 0) s.cum[i] = s.cum[i - 1] + Vector2.Distance(s.p[i - 1], s.p[i]);
                }
                s.len = s.cum[n - 1];
                strokes.Add(s);
            }
        }

        /// <summary>진행도 0~1 (바뀔 때만 메시를 다시 만듦)</summary>
        public void SetProgress(float p)
        {
            p = Mathf.Clamp01(p);
            if (Mathf.Abs(p - progress) < 1e-5f) return;
            progress = p;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (progress <= 0) return;
            var r = rectTransform.rect;
            float sx = r.width / 1920f, sy = r.height / 1080f;
            Vector2 Map(Vector2 q) => new(r.xMin + q.x * sx, r.yMax - q.y * sy);
            Color32 c = color;
            int verts = 0;
            foreach (var s in strokes)
            {
                if (progress <= s.t0) continue;
                float f = Mathf.Clamp01((progress - s.t0) / s.dur);
                float need = s.len * f;
                for (int i = 1; i < s.p.Length; i++)
                {
                    if (s.cum[i - 1] >= need) break;
                    var a = s.p[i - 1]; var b = s.p[i];
                    float segLen = s.cum[i] - s.cum[i - 1];
                    if (s.cum[i] > need && segLen > 0) b = Vector2.Lerp(a, b, (need - s.cum[i - 1]) / segLen);
                    // 양 끝이 가는 붓 모양 (전체 길이 기준)
                    float ta = s.len > 0 ? s.cum[i - 1] / s.len : 0, tb = s.len > 0 ? Mathf.Min(need, s.cum[i]) / s.len : 1;
                    float wa = s.w * widthMul * Taper(ta) * sx * 0.5f, wb = s.w * widthMul * Taper(tb) * sx * 0.5f;
                    var A = Map(a); var B = Map(b);
                    var d = B - A;
                    float l = d.magnitude;
                    if (l < 1e-3f) continue;
                    d /= l;
                    var nrm = new Vector2(-d.y, d.x);
                    // 이음매 틈이 안 보이게 앞뒤로 살짝 늘림
                    A -= d * wa * 0.6f; B += d * wb * 0.6f;
                    if (verts + 4 > 64000) return;
                    vh.AddVert(A + nrm * wa, c, Vector2.zero);
                    vh.AddVert(A - nrm * wa, c, Vector2.zero);
                    vh.AddVert(B - nrm * wb, c, Vector2.zero);
                    vh.AddVert(B + nrm * wb, c, Vector2.zero);
                    vh.AddTriangle(verts, verts + 1, verts + 2);
                    vh.AddTriangle(verts, verts + 2, verts + 3);
                    verts += 4;
                }
            }
        }

        static float Taper(float t) => Mathf.Lerp(0.45f, 1f, Mathf.Clamp01(Mathf.Min(t, 1 - t) * 6f));
    }
}
