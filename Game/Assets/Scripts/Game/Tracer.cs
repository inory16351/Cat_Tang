// 사격 궤적 (빛 탄도): 빛을 더하는 셰이더(CatTang/AdditiveGlow)로 세 겹. 색은 빨강 쪽 (탄 색 30%)
// - 번짐(halo): 가는 빨간 빛 띠. 가장 오래 남아 굵기가 줄며 사라짐
// - 심지(core): 가늘고 하얗게 달군 빨간 선. 짧게 번쩍
// - 탄두(head): 밝은 짧은 줄이 총구 → 목표로 0.05초 만에 날아감 (속도감)
// 탄마다 색 · 굵기가 다르고, 전설 · 공명 탄(doubleLine)은 번짐이 더 넓고 오래 감.
using UnityEngine;

namespace CatTang
{
    public class Tracer : MonoBehaviour
    {
        LineRenderer halo, core, head;
        float born, life, width;
        Vector3 a, b;
        Color col;
        static Material glowMat, softMat;
        const float HeadTime = 0.05f, HeadLen = 2.2f;

        public static void Spawn(Vector3 a, Vector3 b, bool awakened) => Spawn(a, b, awakened ? new Color(1f, 0.85f, 0.35f) : new Color(1f, 0.97f, 0.85f), awakened ? 0.16f : 0.09f, false);

        static void EnsureMats()
        {
            if (glowMat) return;
            var sh = Shader.Find("CatTang/AdditiveGlow");
            if (!sh) sh = Shader.Find("Sprites/Default");
            // 가로(길이) 방향은 그대로, 세로(굵기) 방향으로 가운데가 밝은 부드러운 띠
            Texture2D Band(float sharp)
            {
                var t = new Texture2D(4, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                for (int y = 0; y < 64; y++)
                {
                    float v = (y + 0.5f) / 64f * 2 - 1;
                    float al = Mathf.Exp(-v * v * sharp);
                    for (int x = 0; x < 4; x++) t.SetPixel(x, y, new Color(1, 1, 1, al));
                }
                t.Apply();
                return t;
            }
            softMat = new Material(sh) { mainTexture = Band(3.5f) };
            glowMat = new Material(sh) { mainTexture = Band(9f) };
            if (softMat.HasProperty("_Intensity")) { softMat.SetFloat("_Intensity", 1.6f); glowMat.SetFloat("_Intensity", 2.6f); }
        }

        public static void Spawn(Vector3 a, Vector3 b, Color c, float width, bool doubleLine)
        {
            EnsureMats();
            var go = new GameObject("Tracer"); VividLayer.Set(go);
            var t = go.AddComponent<Tracer>();
            t.a = a; t.b = b; t.col = c;
            t.width = width;
            t.halo = Line(go, "Halo", softMat, a, b);
            t.core = Line(go, "Core", glowMat, a, b);
            t.head = Line(go, "Head", glowMat, a, a);
            t.born = Time.time;
            t.life = doubleLine ? 0.24f : 0.17f;
            if (doubleLine) t.width *= 1.3f;
            t.Update();
        }

        static LineRenderer Line(GameObject parent, string name, Material m, Vector3 a, Vector3 b)
        {
            var go = new GameObject(name); VividLayer.Set(go);
            go.transform.SetParent(parent.transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial = m;
            lr.positionCount = 2;
            lr.SetPosition(0, a);
            lr.SetPosition(1, b);
            lr.numCapVertices = 3;
            lr.textureMode = LineTextureMode.Stretch;
            lr.alignment = LineAlignment.View;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            return lr;
        }

        // 탄도 색은 빨강 쪽으로 (2026-11-16 사용자: 일반 사격을 얇고 빨갛게). 탄 색은 30%만 남겨 탄 종류는 살짝 구분
        static readonly Color Red = new Color(0.95f, 0.12f, 0.08f);
        const float TintKeep = 0.3f;

        void Update()
        {
            float age = Time.time - born;
            float k = age / life;
            if (k >= 1) { Destroy(gameObject); return; }
            var tint = Color.Lerp(Red, col, TintKeep);
            var hot = Color.Lerp(tint, Color.white, 0.55f);       // 심지 · 탄두: 하얗게 달군 빨강

            // 번짐: 가늘게 → 더 가늘어지며 옅어짐. 총구 쪽이 조금 더 굵음
            float hk = 1 - k;
            halo.startWidth = width * 3.0f * (0.6f + 0.4f * hk);
            halo.endWidth = width * 2.0f * (0.6f + 0.4f * hk);
            var hc = tint; hc.a = 0.9f * hk * hk;
            halo.startColor = hc; halo.endColor = new Color(hc.r, hc.g, hc.b, hc.a * 0.7f);

            // 심지: 앞 40% 동안만
            float ck = Mathf.Clamp01(age / (life * 0.45f));
            core.enabled = ck < 1;
            if (core.enabled)
            {
                float w = width * 0.75f * (1 - ck);
                core.startWidth = w * 0.8f; core.endWidth = w;
                var cc = hot; cc.a = 1 - ck * ck;
                core.startColor = cc; core.endColor = cc;
            }

            // 탄두: 총구 → 목표로 날아가는 밝은 짧은 줄, 도착하면 꺼짐
            float tk = age / HeadTime;
            head.enabled = tk < 1.15f;
            if (head.enabled)
            {
                float len = (b - a).magnitude;
                var dir = len > 1e-4f ? (b - a) / len : Vector3.forward;
                float tip = Mathf.Min(len, len * Mathf.Clamp01(tk));
                float tail = Mathf.Max(0, tip - HeadLen);
                head.SetPosition(0, a + dir * tail);
                head.SetPosition(1, a + dir * tip);
                head.startWidth = width * 0.4f;
                head.endWidth = width * 1.6f;
                head.startColor = new Color(hot.r, hot.g, hot.b, 0);
                head.endColor = Color.Lerp(hot, Color.white, 0.5f);
            }
        }
    }
}
