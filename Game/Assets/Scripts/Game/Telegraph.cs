// 적 공격 예고: 바닥에 빛 선으로 그림 (CatTang/AdditiveGlow). 시간이 지나며 안쪽이 차오르고, 끝나면 사라짐.
// Ring = 원형 범위 (폭발 · 휘두르기), Lane = 직선 (돌진 경로), Aim = 가는 조준선 (사격)
using UnityEngine;

namespace CatTang
{
    public class Telegraph : MonoBehaviour
    {
        enum Shape { Ring, Lane, Sector }
        Shape shape;
        LineRenderer outer, fill;
        float born, life, radius, len, width, half;
        const int ArcN = 24;
        Color col;
        Transform follow;
        static Material mat;

        public static readonly Color Danger = new(1f, 0.28f, 0.18f);
        public static readonly Color Aim = new(1f, 0.75f, 0.3f);

        static LineRenderer NewLine(Transform parent, string name)
        {
            if (!mat)
            {
                var sh = Shader.Find("CatTang/AdditiveGlow") ?? Shader.Find("Sprites/Default");
                mat = new Material(sh);
                if (mat.HasProperty("_Intensity")) mat.SetFloat("_Intensity", 1.8f);
            }
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial = mat;
            lr.useWorldSpace = true;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.numCapVertices = 2;
            lr.alignment = LineAlignment.TransformZ;    // 바닥에 눕힘 (아래 회전으로 Z가 위를 봄)
            go.transform.rotation = Quaternion.Euler(90, 0, 0);
            return lr;
        }

        /// <summary>원형 범위 예고 (follow가 있으면 그 위치를 따라감)</summary>
        public static Telegraph Ring(Vector3 at, float radius, float dur, Color c, Transform follow = null)
        {
            var t = new GameObject("Telegraph_Ring").AddComponent<Telegraph>();
            t.transform.position = at;
            t.shape = Shape.Ring; t.radius = radius; t.life = dur; t.col = c; t.follow = follow;
            t.Init();
            return t;
        }

        /// <summary>직선 예고: from에서 dir로 len만큼, 폭 width</summary>
        public static Telegraph Lane(Vector3 from, Vector3 dir, float len, float width, float dur, Color c, Transform follow = null)
        {
            var t = new GameObject("Telegraph_Lane").AddComponent<Telegraph>();
            t.transform.position = from;
            dir.y = 0;
            t.transform.rotation = Quaternion.LookRotation(dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.forward);
            t.shape = Shape.Lane; t.len = len; t.width = width; t.life = dur; t.col = c; t.follow = follow;
            t.Init();
            return t;
        }

        /// <summary>부채꼴 예고: dir 방향을 가운데로 양쪽 halfAngle도, 반지름 radius</summary>
        public static Telegraph Sector(Vector3 at, Vector3 dir, float radius, float halfAngle, float dur, Color c, Transform follow = null)
        {
            var t = new GameObject("Telegraph_Sector").AddComponent<Telegraph>();
            t.transform.position = at;
            dir.y = 0;
            t.transform.rotation = Quaternion.LookRotation(dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.forward);
            t.shape = Shape.Sector; t.radius = radius; t.half = halfAngle; t.life = dur; t.col = c; t.follow = follow;
            t.Init();
            return t;
        }

        /// <summary>방향 바꾸기 (조준선이 대상을 쫓을 때)</summary>
        public void SetDir(Vector3 dir) { dir.y = 0; if (dir.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(dir.normalized); }
        public void SetColor(Color c) => col = c;

        void Init()
        {
            born = Time.time;
            outer = NewLine(transform, "Outer");
            fill = NewLine(transform, "Fill");
            if (shape == Shape.Ring)
            {
                outer.loop = fill.loop = true;
                outer.positionCount = fill.positionCount = 40;
                outer.widthMultiplier = 0.09f;
                fill.widthMultiplier = 0.22f;
            }
            else if (shape == Shape.Sector)
            {
                outer.loop = fill.loop = true;
                outer.positionCount = fill.positionCount = ArcN + 2;
                outer.widthMultiplier = 0.09f;
                fill.widthMultiplier = 0.18f;
            }
            else
            {
                outer.positionCount = 5;
                outer.loop = true;
                outer.widthMultiplier = 0.07f;
                fill.positionCount = 2;
                fill.widthMultiplier = width;
            }
            Redraw(0);
        }

        /// <summary>바로 지우기 (공격이 취소됐을 때)</summary>
        public void Kill() { if (this) Destroy(gameObject); }

        void Update()
        {
            float k = Mathf.Clamp01((Time.time - born) / Mathf.Max(0.01f, life));
            if (follow) transform.position = follow.position;
            Redraw(k);
            if (Time.time - born >= life) Destroy(gameObject);
        }

        void Redraw(float k)
        {
            float pulse = 0.65f + 0.35f * Mathf.Sin((Time.time - born) * 28f);
            var edge = col * (0.55f + 0.45f * k * pulse); edge.a = 1;
            var inner = col * (0.25f + 0.35f * k); inner.a = 1;
            outer.startColor = outer.endColor = edge;
            fill.startColor = fill.endColor = inner;
            Vector3 o = transform.position + Vector3.up * 0.05f;
            if (shape == Shape.Ring)
            {
                float rf = Mathf.Max(0.05f, radius * k);
                for (int i = 0; i < 40; i++)
                {
                    float a = i / 40f * Mathf.PI * 2;
                    var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                    outer.SetPosition(i, o + d * radius);
                    fill.SetPosition(i, o + d * rf);
                }
            }
            else if (shape == Shape.Sector)
            {
                float rf = Mathf.Max(0.05f, radius * k);
                outer.SetPosition(0, o);
                fill.SetPosition(0, o);
                for (int i = 0; i <= ArcN; i++)
                {
                    float a = Mathf.Lerp(-half, half, i / (float)ArcN);
                    var d = Quaternion.AngleAxis(a, Vector3.up) * transform.forward;
                    outer.SetPosition(i + 1, o + d * radius);
                    fill.SetPosition(i + 1, o + d * rf);
                }
            }
            else
            {
                var f = transform.forward; var s = transform.right * width / 2;
                outer.SetPosition(0, o - s);
                outer.SetPosition(1, o - s + f * len);
                outer.SetPosition(2, o + s + f * len);
                outer.SetPosition(3, o + s);
                outer.SetPosition(4, o - s);
                fill.SetPosition(0, o);
                fill.SetPosition(1, o + f * Mathf.Max(0.05f, len * k));
            }
        }
    }
}
