// 사격 궤적: 리볼버 느낌의 거의 즉발 레이저 선. 탄마다 색 · 굵기가 다르고, 전설 탄은 흰 심지 + 색 테두리 두 겹.
using UnityEngine;

namespace CatTang
{
    public class Tracer : MonoBehaviour
    {
        LineRenderer lr, core;
        float born, life = 0.1f, width;
        static Material mat;

        public static void Spawn(Vector3 a, Vector3 b, bool awakened) => Spawn(a, b, awakened ? new Color(1f, 0.85f, 0.35f) : new Color(1f, 0.97f, 0.85f), awakened ? 0.16f : 0.09f, false);

        public static void Spawn(Vector3 a, Vector3 b, Color c, float width, bool doubleLine)
        {
            if (mat == null) mat = new Material(Shader.Find("Sprites/Default"));
            var go = new GameObject("Tracer");
            var t = go.AddComponent<Tracer>();
            t.lr = Line(go, a, b, c);
            if (doubleLine)
            {
                var g2 = new GameObject("Core");
                g2.transform.SetParent(go.transform, false);
                t.core = Line(g2, a, b, Color.white);
            }
            t.width = width;
            t.born = Time.time;
            t.life = doubleLine ? 0.16f : 0.1f;
            t.Update();
        }

        static LineRenderer Line(GameObject go, Vector3 a, Vector3 b, Color c)
        {
            var lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial = mat;
            lr.positionCount = 2;
            lr.SetPosition(0, a);
            lr.SetPosition(1, b);
            lr.startColor = c;
            lr.endColor = c;
            lr.numCapVertices = 2;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return lr;
        }

        void Update()
        {
            float k = (Time.time - born) / life;
            if (k >= 1) { Destroy(gameObject); return; }
            float w = width * (1 - k * k);
            lr.startWidth = w * 0.6f;
            lr.endWidth = w;
            if (core) { core.startWidth = w * 0.25f; core.endWidth = w * 0.4f; }
        }
    }
}
