// 시계바늘 수리 퀘스트 (ClockQuest.cs) — 씬에 저장하려면 파일 이름 = 클래스 이름
using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    /// <summary>반딧불 길: 자식 점들(Waypoint)을 따라 작은 빛이 물건 쪽으로 천천히 흘러감</summary>
    public class FireflyTrail : MonoBehaviour
    {
        public string id;
        public int count = 34;
        readonly List<Vector3> pts = new();
        readonly List<Transform> flies = new();
        readonly List<float> phase = new();
        float length;
        static Texture2D dot;
        static Material mat;

        public Vector3 End => transform.childCount > 0 ? transform.GetChild(transform.childCount - 1).position : transform.position;

        void OnEnable()
        {
            pts.Clear();
            foreach (Transform c in transform) if (c.name.StartsWith("Waypoint")) pts.Add(c.position);
            if (pts.Count < 2) return;
            length = 0; for (int i = 1; i < pts.Count; i++) length += Vector3.Distance(pts[i - 1], pts[i]);
            if (!mat)
            {
                dot = new Texture2D(32, 32, TextureFormat.RGBA32, false);
                for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 15.5f)) / 15.5f;
                    float a = Mathf.Clamp01(1 - d); a = a * a;
                    dot.SetPixel(x, y, new Color(1, 1, 1, a));
                }
                dot.Apply();
                mat = new Material(Shader.Find("Sprites/Default")) { mainTexture = dot };
            }
            if (flies.Count == 0)
                for (int i = 0; i < count; i++)
                {
                    var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    Destroy(q.GetComponent<Collider>());
                    q.name = "Firefly";
                    q.transform.SetParent(transform, false);
                    var r = q.GetComponent<MeshRenderer>();
                    r.sharedMaterial = mat;
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    var mpb = new MaterialPropertyBlock();
                    mpb.SetColor("_Color", Color.Lerp(new Color(1f, 0.95f, 0.35f), new Color(0.7f, 1f, 0.35f), Random.value));
                    r.SetPropertyBlock(mpb);
                    q.AddComponent<Billboard>();
                    flies.Add(q.transform);
                    phase.Add(Random.value);
                }
        }

        Vector3 At(float d)
        {
            for (int i = 1; i < pts.Count; i++)
            {
                float seg = Vector3.Distance(pts[i - 1], pts[i]);
                if (d <= seg) return Vector3.Lerp(pts[i - 1], pts[i], seg < 1e-4f ? 0 : d / seg);
                d -= seg;
            }
            return pts[pts.Count - 1];
        }

        void Update()
        {
            if (pts.Count < 2) return;
            float t = Time.time;
            for (int i = 0; i < flies.Count; i++)
            {
                float u = Mathf.Repeat(phase[i] + t * 0.035f, 1f);             // 물건 쪽으로 천천히 흐름
                var p = At(u * length);
                float w = i * 1.37f;
                p += new Vector3(Mathf.Sin(t * 1.3f + w) * 0.6f, 0.9f + Mathf.Sin(t * 0.9f + w * 2f) * 0.45f, Mathf.Cos(t * 1.1f + w) * 0.6f);
                flies[i].position = p;
                float blink = 0.5f + 0.5f * Mathf.Sin(t * 3.1f + w * 3f);
                flies[i].localScale = Vector3.one * (0.32f + 0.26f * blink) * (u > 0.92f ? 1.4f : 1f);
            }
        }
    }
}
