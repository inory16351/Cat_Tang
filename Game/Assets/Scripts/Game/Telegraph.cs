// 적 공격 예고: 바닥에 그림. 시간이 지나며 안쪽이 차오르고, 끝나면 사라짐.
// Ring = 원형 범위 (폭발 · 휘두르기), Lane = 직선 (돌진 경로), Aim = 가는 조준선 (사격)
// 가시성 (2026-11-17 사용자: 주황 흙바닥에 묻힘):
// - 색: 위험 = 진홍 분홍, 조준 = 보라 (주황 바닥의 보색 쪽)
// - 범위 전체에 어두운 반투명 그림자 판 → 바닥과 분리, 진행도만큼 색 판이 차오름
// - 밝은 빛 테두리 밑에 굵은 어두운 선을 덧댐 → 어떤 바닥에서도 테두리가 보임
using UnityEngine;

namespace CatTang
{
    public class Telegraph : MonoBehaviour, IPooled
    {
        enum Shape { Ring, Lane, Sector }
        Shape shape;
        LineRenderer outer, fill, back;
        MeshFilter areaBack, areaFill;
        Mesh backMesh, fillMesh;
        static Material darkMat;
        float born, life, radius, len, width, half;
        const int ArcN = 24;
        Color col;
        Transform follow;
        static Material mat;

        static readonly Color DangerPink = new(1f, 0.14f, 0.36f);        // 진홍 분홍
        static readonly Color DangerRed = new(1f, 0.06f, 0.08f);         // 4장 느와르: 흑백 위 새빨강 (2026-11-19 사용자)
        static readonly Color DangerMagenta = new(1f, 0.16f, 0.72f);    // 5장: 보라 · 금 우주 위 자홍 (2026-11-19 md 챕터 5 '적 공격 궤적은 자홍 · 마젠타')
        public static Color Danger => EnemyProjectile.Ch4 ? DangerRed : EnemyProjectile.Ch5 ? DangerMagenta : DangerPink;
        public static readonly Color Aim = new(0.82f, 0.38f, 1f);      // 보라
        static readonly Color Shadow = new(0.06f, 0.02f, 0.08f, 0.34f);
        static readonly Color Ink = new(0.05f, 0.01f, 0.07f, 0.9f);

        // 풀링 (2026-11-20): 모양별 완성품과 메시 작성 버퍼를 재사용한다.
        readonly System.Collections.Generic.List<Vector3> vertices = new(42);
        readonly System.Collections.Generic.List<int> triangles = new(120);
        readonly System.Collections.Generic.List<Color> colors = new(42);
        static readonly System.Func<GameObject> ringFactory = CreateRing, laneFactory = CreateLane, sectorFactory = CreateSector;
        static GameObject CreateRing() => Create(Shape.Ring);
        static GameObject CreateLane() => Create(Shape.Lane);
        static GameObject CreateSector() => Create(Shape.Sector);
        static GameObject Create(Shape shape)
        {
            var go = new GameObject("Telegraph_" + shape);
            var t = go.AddComponent<Telegraph>(); t.shape = shape;
            t.areaBack = t.NewArea("AreaShadow", out t.backMesh, 0.025f);
            t.areaFill = t.NewArea("AreaFill", out t.fillMesh, 0.03f);
            t.back = t.NewDarkLine("OuterInk"); t.outer = NewLine(t.transform, "Outer"); t.fill = NewLine(t.transform, "Fill");
            return go;
        }
        public void OnSpawned()
        {
            follow = null; grow = false; glint = true; glinted = false; laser = true;
            born = life = radius = len = width = half = 0f; col = Color.white;
            outer.loop = fill.loop = back.loop = false;
        }
        public void OnDespawned() { Enemy.ForgetPooledTelegraph(this); follow = null; vertices.Clear(); triangles.Clear(); colors.Clear(); }
        void OnDestroy() { if (backMesh) Destroy(backMesh); if (fillMesh) Destroy(fillMesh); }

        static Material DarkMat()
        {
            if (!darkMat) darkMat = new Material(Shader.Find("CatTang/Sprite") ?? Shader.Find("Sprites/Default")) { renderQueue = 2999 };
            return darkMat;
        }

        LineRenderer NewDarkLine(string name)
        {
            var lr = NewLine(transform, name);
            lr.sharedMaterial = DarkMat();
            return lr;
        }

        MeshFilter NewArea(string name, out Mesh mesh, float y)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0, y, 0);
            var mf = go.AddComponent<MeshFilter>();
            mesh = new Mesh { name = name };
            mesh.MarkDynamic();
            mf.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = DarkMat();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return mf;
        }

        /// <summary>로컬 평면 모양 메시: 원(반지름 r) · 부채꼴(반지름 r, ±half°, +Z가 가운데) · 직선(폭 w, 길이 l, +Z 방향)</summary>
        void ShapeMesh(Mesh m, float r, float l, Color c)
        {
            var v = vertices; var t = triangles;
            v.Clear(); t.Clear(); colors.Clear();
            if (shape == Shape.Lane)
            {
                float hw = width / 2;
                v.Add(new Vector3(-hw, 0, 0)); v.Add(new Vector3(hw, 0, 0)); v.Add(new Vector3(-hw, 0, l)); v.Add(new Vector3(hw, 0, l));
                t.Add(0); t.Add(2); t.Add(1); t.Add(2); t.Add(3); t.Add(1);
            }
            else
            {
                v.Add(Vector3.zero);
                int n = shape == Shape.Ring ? 40 : ArcN;
                for (int i = 0; i <= n; i++)
                {
                    float a = shape == Shape.Ring ? i * 360f / n : Mathf.Lerp(-half, half, i / (float)n);
                    v.Add(Quaternion.AngleAxis(a, Vector3.up) * Vector3.forward * r);
                }
                for (int i = 1; i <= n; i++) { t.Add(0); t.Add(i); t.Add(i + 1); }
            }
            for (int i = 0; i < v.Count; i++) colors.Add(c);
            m.Clear();
            m.SetVertices(v);
            m.SetTriangles(t, 0);
            m.SetColors(colors);
            m.RecalculateBounds();
        }

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
            var t = GameObjectPool.Get("Telegraph_Ring", ringFactory).GetComponent<Telegraph>();
            t.transform.position = at;
            t.shape = Shape.Ring; t.radius = radius; t.life = dur; t.col = c; t.follow = follow;
            t.Init();
            return t;
        }

        /// <summary>직선 예고: from에서 dir로 len만큼, 폭 width</summary>
        public static Telegraph Lane(Vector3 from, Vector3 dir, float len, float width, float dur, Color c, Transform follow = null)
        {
            var t = GameObjectPool.Get("Telegraph_Lane", laneFactory).GetComponent<Telegraph>();
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
            var t = GameObjectPool.Get("Telegraph_Sector", sectorFactory).GetComponent<Telegraph>();
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

        bool grow, glint = true, glinted, laser = true;
        /// <summary>4장: 끝날 때 레이저 베기 호 · 레이저 줄을 띄우지 않음 (무기 칼빛 BladeStreak가 칼날에 붙어 대신 그림)</summary>
        public Telegraph NoLaser() { laser = false; return this; }

        /// <summary>
        /// 직선 예고(Lane)와 똑같은 판정 (2026-11-19 사용자: 실제 타격 범위와 예고를 같게): from에서 dir로 [0, len] · 폭 width 안에 점(몸 중심)이 있으면 참.
        /// 돌진은 lo~hi = 지금 몸이 덮고 있는 구간(나아간 거리 ± 몸 반지름)만 — 몸이 지나간 뒤 남은 길에 들어가도 맞지 않음
        /// </summary>
        public static bool InLane(Vector3 pt, Vector3 from, Vector3 dir, float len, float width, float lo = 0f, float hi = float.MaxValue)
        {
            dir.y = 0; if (dir.sqrMagnitude < 1e-6f) return false; dir.Normalize();
            var rel = Effects.Flat(pt - from);
            float along = Vector3.Dot(rel, dir);
            return along >= Mathf.Max(0f, lo) && along <= Mathf.Min(len, hi) && (rel - dir * along).magnitude <= width * 0.5f;
        }
        /// <summary>끝나기 직전 예고 섬광을 띄우지 않음 (공격이 아닌 표시: 폭탄병 착지 조준 등)</summary>
        public Telegraph NoGlint() { glint = false; return this; }
        /// <summary>원 자체가 작게 시작해 끝날 때 반경까지 커짐 (폭탄병 점멸: 다 커지는 순간 폭발). 깜빡임도 점점 빨라짐</summary>
        public Telegraph Grow() { grow = true; Redraw(0); return this; }

        void Init()
        {
            born = Time.time;
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
        public void Kill() { if (this) GameObjectPool.Release(gameObject); }

        void Update()
        {
            float k = Mathf.Clamp01((Time.time - born) / Mathf.Max(0.01f, life));
            if (follow) transform.position = follow.position;
            Redraw(k);
            // 공격 직전 예고 섬광 (젠레스 존 제로식 주황 십자 반짝임): 끝나기 0.15초 전 = 이때 회피하면 극한 회피
            if (glint && !glinted && life > 0.25f && life - (Time.time - born) <= 0.15f)
            {
                glinted = true;
                GunkataFx.WarnGlint(transform.position + Vector3.up * 1.6f);
            }
            if (Time.time - born >= life)
            {
                // 4장 (느와르): 적 예고가 끝나는 순간 = 공격이 나가는 순간 → 레이저 베기 호 · 레이저 줄 (LaserFx)
                if (laser && LaserFx.On && col.r > 0.8f && col.g < 0.3f)
                {
                    if (shape == Shape.Sector) LaserFx.Slash(transform.position, transform.forward, radius);
                    else if (shape == Shape.Lane) LaserFx.Streak(transform.position, transform.forward, len, width);
                }
                GameObjectPool.Release(gameObject);
            }
        }

        void Redraw(float k)
        {
            float age = Time.time - born;
            float pulse = 0.65f + 0.35f * Mathf.Sin(grow ? age * Mathf.Lerp(18f, 60f, k) : age * 28f);
            var edge = col * (1.0f + 0.5f * k * pulse); edge.a = 1;          // 처음부터 밝게 (가시성)
            var inner = col * (0.6f + 0.5f * k); inner.a = 1;
            outer.startColor = outer.endColor = edge;
            fill.startColor = fill.endColor = inner;
            Vector3 o = transform.position + Vector3.up * 0.05f;
            if (shape == Shape.Ring)
            {
                // 커지기: 바깥 원이 반경의 15%에서 끝에 100%로 (처음엔 빠르게, 끝에서 살짝 느려지며 꽉 참), 안쪽 선이 바로 뒤를 따라옴
                float ro = grow ? radius * Mathf.Lerp(0.15f, 1f, 1 - (1 - k) * (1 - k)) : radius;
                float rf = grow ? Mathf.Max(0.05f, ro * Mathf.Lerp(0.5f, 0.92f, k)) : Mathf.Max(0.05f, radius * k);
                for (int i = 0; i < 40; i++)
                {
                    float a = i / 40f * Mathf.PI * 2;
                    var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                    outer.SetPosition(i, o + d * ro);
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
            // 어두운 덧선 (테두리 밑) + 그림자 판 · 차오르는 색 판
            back.loop = outer.loop;
            back.positionCount = outer.positionCount;
            for (int i = 0; i < outer.positionCount; i++) back.SetPosition(i, outer.GetPosition(i) - Vector3.up * 0.005f);
            back.widthMultiplier = outer.widthMultiplier * 2.6f + 0.06f;
            back.startColor = back.endColor = Ink;
            float rOuter = shape == Shape.Ring && grow ? radius * Mathf.Lerp(0.15f, 1f, 1 - (1 - k) * (1 - k)) : radius;
            ShapeMesh(backMesh, rOuter, len, Shadow);
            var fc = col; fc.a = Mathf.Lerp(0.32f, 0.55f, k) * (0.85f + 0.15f * pulse);
            ShapeMesh(fillMesh, shape == Shape.Ring ? (grow ? rOuter * Mathf.Lerp(0.5f, 0.95f, k) : radius * k) : radius * k, len * k, fc);
        }
    }
}
