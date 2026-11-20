// 육중한 발걸음 흙먼지 (2026-11-20 사용자: 거인 · 5장 중간 보스 · 시침 같은 육중한 캐릭터가 이동할 때 흙먼지, 블렌더로 구워서
//   → "땅에 닿는 순간에 퍼져야" → "한 점이 아니라 발이 닿는 면적에서부터" → "발 콜라이더가 닿는 면적을 입체적으로 추적해 그 끝점에서부터 자연스럽게 퍼지게")
//
// 발 접지 면 추적:
//  1) 시작할 때 스킨 메시를 한 번 구워(BakeMesh) 발목 ↔ 발끝 선분 가까이 · 발목 아래에 있는 정점을 발마다 골라 '발 뼈 기준 좌표'로 저장 (= 발 모양 그대로의 점 구름, 발 콜라이더)
//  2) 매 프레임 발 뼈로 그 점들을 월드로 옮겨 가장 낮은 점의 높이를 잼. 흔들림 폭을 스스로 재서 발이 들렸다(폭 45% 위) → 가장 낮은 점이 바닥(폭 15% 아래)에 닿는 프레임 = 접지
//  3) 접지 순간 실제로 바닥에 닿아 있는 점들(가장 낮은 점에서 발 길이 12% 안)을 바닥에 눌러 볼록 껍질 = 발자국 윤곽
//  4) 윤곽 둘레를 같은 간격으로 6~14점 → 점마다 흙먼지 덩이(FX_DustClump, bk_dustclump)를 놓고 그 자리의 바깥 법선 방향으로 굴려 보냄 + 발자국 가운데 솟는 뭉게(FX_FootDust, bk_dustpuff)
//  발 뼈가 없거나 메시가 없으면: 보폭마다 · 뼈 높이로 대신 (작은 원 둘레에서 퍼짐)
using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    public class HeavyStep : MonoBehaviour
    {
        [Tooltip("먼지 세기 배율 (발 크기는 메시에서 잼)")] public float size = 1f;
        [Tooltip("발 뼈가 없을 때 보폭(m)")] public float stride = 2.4f;
        [Tooltip("이보다 느리면 먼지 없음 (m/초)")] public float minSpeed = 0.6f;
        public Color tint = Color.white;
        /// <summary>개발용: 접지 순간 발자국 윤곽을 선으로 0.6초 보여 줌</summary>
        public static bool ShowFootprint;

        class Foot
        {
            public Transform bone, toe;
            public Vector3[] pts;              // 발 뼈 기준 좌표의 발 정점들 (발 콜라이더)
            public float lo, hi, lastStep;
            public bool lifted;
        }

        readonly List<Foot> feet = new();
        float footLen = 0.8f;
        Vector3 lastPos;
        float travelled, speed;
        int side;

        /// <summary>적에 붙임. 먼지 색 = 장 바닥 색을 밝게 (구운 그림이 베이지라 절반만 섞임)</summary>
        public static HeavyStep Attach(Enemy e, float size, float stride = 2.4f)
        {
            if (!e) return null;
            var h = e.gameObject.AddComponent<HeavyStep>();
            h.size = size;
            h.stride = stride;
            var floor = e.chapter != null ? e.chapter.Floor : new Color(0.6f, 0.5f, 0.4f);
            h.tint = Color.Lerp(floor, new Color(0.95f, 0.9f, 0.82f), 0.55f);
            return h;
        }

        void Start()
        {
            lastPos = transform.position;
            Transform[] bones = null, toeBones = null;
            var anim = GetComponentInChildren<Animator>();
            if (anim && anim.isHuman)
            {
                var l = anim.GetBoneTransform(HumanBodyBones.LeftFoot);
                var r = anim.GetBoneTransform(HumanBodyBones.RightFoot);
                if (l && r) { bones = new[] { l, r }; toeBones = new[] { anim.GetBoneTransform(HumanBodyBones.LeftToes), anim.GetBoneTransform(HumanBodyBones.RightToes) }; }
            }
            if (bones == null)
            {
                var found = new List<Transform>();
                foreach (var t in GetComponentsInChildren<Transform>(true))
                {
                    var n = t.name.ToLowerInvariant();
                    if ((n.Contains("foot") || n.Contains("ankle")) && !n.Contains("toe") && !n.Contains("end") && !n.Contains("ik")) found.Add(t);
                    if (found.Count >= 2) break;
                }
                if (found.Count >= 2) { bones = found.ToArray(); toeBones = new Transform[2]; }
            }
            if (bones == null) return;

            // 발 길이: 발목 ↔ 발가락 × 2.1 (발가락 뼈가 없으면 정강이 길이의 40%)
            if (toeBones[0]) footLen = Vector3.Distance(bones[0].position, toeBones[0].position) * 2.1f;
            else if (bones[0].parent) footLen = Vector3.Distance(bones[0].position, bones[0].parent.position) * 0.4f;
            footLen = Mathf.Max(0.15f, footLen);

            var clouds = CollectFootPoints(bones, toeBones);
            for (int i = 0; i < bones.Length; i++)
            {
                var f = new Foot { bone = bones[i], toe = toeBones[i], pts = clouds[i], lastStep = -9 };
                f.lo = f.hi = Lowest(f) - transform.position.y;
                feet.Add(f);
            }
        }

        /// <summary>스킨 메시를 지금 자세로 구워 발마다 발 근처 정점을 골라 발 뼈 기준 좌표로 (없으면 발목 한 점)</summary>
        Vector3[][] CollectFootPoints(Transform[] bones, Transform[] toeBones)
        {
            var lists = new List<Vector3>[bones.Length];
            for (int i = 0; i < bones.Length; i++) lists[i] = new List<Vector3>();
            var a = new Vector3[bones.Length]; var b = new Vector3[bones.Length];
            for (int i = 0; i < bones.Length; i++)
            {
                a[i] = bones[i].position;
                var tip = toeBones[i] ? toeBones[i].position + (toeBones[i].position - bones[i].position) * 0.6f
                                      : bones[i].position + Effects.Flat(transform.forward) * footLen * 0.6f;
                b[i] = tip;
            }
            var baked = new Mesh();
            foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (!smr.enabled || !smr.sharedMesh) continue;
                smr.BakeMesh(baked, true);
                var m = Matrix4x4.TRS(smr.transform.position, smr.transform.rotation, smr.transform.lossyScale);   // 구운 메시는 부모 배율이 빠져 나옴
                var v = baked.vertices;
                int step = Mathf.Max(1, v.Length / 30000);
                for (int k = 0; k < v.Length; k += step)
                {
                    var w = m.MultiplyPoint3x4(v[k]);
                    int best = -1; float bd = float.MaxValue;
                    for (int i = 0; i < bones.Length; i++)
                    {
                        if (w.y > a[i].y + footLen * 0.15f) continue;                  // 발목보다 위 = 정강이
                        float d = DistToSegment(w, a[i], b[i]);
                        if (d < footLen * 0.55f && d < bd) { bd = d; best = i; }
                    }
                    if (best >= 0) lists[best].Add(bones[best].InverseTransformPoint(w));
                }
            }
            Destroy(baked);
            var res = new Vector3[bones.Length][];
            for (int i = 0; i < bones.Length; i++)
            {
                var l = lists[i];
                // 너무 많으면 고르게 솎음 (매 프레임 가장 낮은 점을 찾으므로 200개 안팎)
                if (l.Count > 200) { var t = new List<Vector3>(); float s = l.Count / 200f; for (float k = 0; k < l.Count; k += s) t.Add(l[(int)k]); l = t; }
                res[i] = l.Count >= 4 ? l.ToArray() : new[] { Vector3.zero };
            }
            return res;
        }

        static float DistToSegment(Vector3 p, Vector3 a, Vector3 b)
        {
            var ab = b - a; float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / Mathf.Max(1e-6f, ab.sqrMagnitude));
            return Vector3.Distance(p, a + ab * t);
        }

        static float Lowest(Foot f)
        {
            float y = float.MaxValue;
            foreach (var q in f.pts) y = Mathf.Min(y, f.bone.TransformPoint(q).y);
            return y;
        }

        void LateUpdate()
        {
            float dt = Mathf.Max(Time.deltaTime, 1e-4f);
            var p = transform.position;
            var d = Effects.Flat(p - lastPos);
            lastPos = p;
            speed = Mathf.Lerp(speed, d.magnitude / dt, 1 - Mathf.Exp(-10f * dt));
            bool moving = speed > minSpeed;

            if (feet.Count > 0)
            {
                foreach (var f in feet)
                {
                    if (!f.bone) continue;
                    float h = Lowest(f) - p.y;
                    f.lo = Mathf.Min(f.lo + 0.2f * dt, h);
                    f.hi = Mathf.Max(f.hi - 0.2f * dt, h);
                    float swing = f.hi - f.lo;
                    if (swing < 0.04f * footLen) continue;
                    if (h > f.lo + swing * 0.45f) f.lifted = true;
                    else if (f.lifted && h < f.lo + swing * 0.15f)
                    {
                        f.lifted = false;                                              // 가장 낮은 점이 바닥에 닿은 그 프레임
                        if (moving && Time.time - f.lastStep > 0.18f) { f.lastStep = Time.time; Land(f, p.y); }
                    }
                }
                return;
            }
            // 발 뼈가 없을 때만: 보폭마다 좌우 번갈아, 작은 원 둘레에서
            if (moving) travelled += d.magnitude; else travelled = 0;
            if (moving && travelled >= stride)
            {
                travelled = 0;
                side = side == 1 ? -1 : 1;
                var dir = d.sqrMagnitude > 1e-8f ? d.normalized : transform.forward;
                var c = p + Vector3.Cross(Vector3.up, dir) * side * 0.35f * size;
                var ring = new List<Vector2>();
                for (int k = 0; k < 8; k++) { float a = k * Mathf.PI / 4; ring.Add(new Vector2(c.x + Mathf.Cos(a) * 0.3f * size, c.z + Mathf.Sin(a) * 0.45f * size)); }
                Emit(ring, p.y);
            }
        }

        /// <summary>접지: 지금 바닥에 닿아 있는 발 점들 → 바닥 평면의 볼록 껍질(발자국) → 둘레에서 퍼짐</summary>
        void Land(Foot f, float groundY)
        {
            var world = new Vector3[f.pts.Length];
            float minY = float.MaxValue;
            for (int i = 0; i < world.Length; i++) { world[i] = f.bone.TransformPoint(f.pts[i]); minY = Mathf.Min(minY, world[i].y); }
            float band = Mathf.Max(0.03f, footLen * 0.12f);
            var flat = new List<Vector2>();
            foreach (var w in world) if (w.y < minY + band) flat.Add(new Vector2(w.x, w.z));
            if (flat.Count < 3)
            {
                // 닿은 점이 너무 적으면(뾰족한 발끝 등) 발 점 전체의 아래 절반으로
                flat.Clear();
                foreach (var w in world) if (w.y < minY + footLen * 0.3f) flat.Add(new Vector2(w.x, w.z));
            }
            var hull = Hull(flat);
            if (hull.Count < 3)
            {
                var c = f.bone.position;
                hull.Clear();
                for (int k = 0; k < 8; k++) { float a = k * Mathf.PI / 4; hull.Add(new Vector2(c.x + Mathf.Cos(a) * footLen * 0.3f, c.z + Mathf.Sin(a) * footLen * 0.3f)); }
            }
            Emit(hull, groundY);
        }

        /// <summary>발자국 윤곽(반시계 볼록 다각형) 둘레를 같은 간격으로 → 점마다 바깥으로 굴러 나가는 흙먼지 덩이 + 가운데 솟는 뭉게</summary>
        void Emit(List<Vector2> hull, float groundY)
        {
            float per = 0; var cen = Vector2.zero;
            for (int i = 0; i < hull.Count; i++) { per += Vector2.Distance(hull[i], hull[(i + 1) % hull.Count]); cen += hull[i]; }
            cen /= hull.Count;
            int n = Mathf.Clamp(Mathf.RoundToInt(per / (footLen * 0.26f)), 8, 16);
            float step = per / n, walk = Random.Range(0f, step);
            float clump = footLen * 1.05f * size;
            int e = 0; float edgeStart = 0;
            for (int k = 0; k < n; k++)
            {
                float s = walk + k * step;
                while (e < hull.Count && edgeStart + Vector2.Distance(hull[e], hull[(e + 1) % hull.Count]) < s) { edgeStart += Vector2.Distance(hull[e], hull[(e + 1) % hull.Count]); e++; }
                var a = hull[e % hull.Count]; var b = hull[(e + 1) % hull.Count];
                float len = Mathf.Max(1e-5f, Vector2.Distance(a, b));
                var pt = Vector2.Lerp(a, b, Mathf.Clamp01((s - edgeStart) / len));
                var edgeN = new Vector2(b.y - a.y, a.x - b.x).normalized;           // 반시계 껍질의 바깥 법선
                var radial = (pt - cen).sqrMagnitude > 1e-6f ? (pt - cen).normalized : edgeN;
                var outN = (edgeN * 0.65f + radial * 0.35f).normalized;
                var at = new Vector3(pt.x, groundY, pt.y);
                FxEffect.Spawn("FX_DustClump", at, Quaternion.LookRotation(new Vector3(outN.x, 0, outN.y)), clump * Random.Range(0.85f, 1.15f), tint);
            }
            FxEffect.Spawn("FX_FootDust", new Vector3(cen.x, groundY, cen.y), Quaternion.identity, footLen / 0.84f * 1.0f * size, tint);
            if (ShowFootprint) DrawHull(hull, groundY);
        }

        static List<Vector2> Hull(List<Vector2> p)
        {
            var r = new List<Vector2>();
            if (p.Count < 3) return r;
            p.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
            static float Cross(Vector2 o, Vector2 a, Vector2 b) => (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);
            var h = new Vector2[p.Count * 2]; int k = 0;
            for (int i = 0; i < p.Count; i++) { while (k >= 2 && Cross(h[k - 2], h[k - 1], p[i]) <= 0) k--; h[k++] = p[i]; }
            for (int i = p.Count - 2, t = k + 1; i >= 0; i--) { while (k >= t && Cross(h[k - 2], h[k - 1], p[i]) <= 0) k--; h[k++] = p[i]; }
            for (int i = 0; i < k - 1; i++) r.Add(h[i]);
            return r;
        }

        static void DrawHull(List<Vector2> hull, float y)
        {
            var go = new GameObject("FootprintDebug");
            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = hull.Count; lr.loop = true; lr.widthMultiplier = 0.04f;
            lr.material = new Material(Shader.Find("Sprites/Default")); lr.startColor = lr.endColor = new Color(0.2f, 1f, 0.4f);
            for (int i = 0; i < hull.Count; i++) lr.SetPosition(i, new Vector3(hull[i].x, y + 0.06f, hull[i].y));
            VividLayer.Set(go);
            Destroy(go, 0.6f);
        }
    }
}
