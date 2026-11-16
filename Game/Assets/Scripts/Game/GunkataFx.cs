// 건카타 총구 폭발 연출 (2D + 3D 섞음)
// - 2D 섬광(flash_0~2): 총구 앞에서 한 순간 번쩍이는 날카로운 가시 별 (잉크 외곽선, docs/tools/fx_flash.py)
// - 3D 불꽃 원뿔(FxMeshes.Flame): 총열 방향으로 쭉 뻗는 가시 꽃잎 불꽃. CatTang/ToonFire 셰이더로 4단 툰 색 + 잉크 외곽선,
//   식으며(흰 → 노랑 → 붉음 → 그을음) 3D 노이즈 구멍으로 녹아 사라짐
// - 3D 불덩이(FxMeshes.Ball): 원뿔 끝에서 울퉁불퉁 부풀었다 식는 폭발 구름
// - 빛줄기 · 붉은 흩뿌림(선) · 회색 연기(2D) · 몸 주위 소용돌이(선)
// 메시는 처음 쓸 때 코드로 한 번 구워서 공유함 (변형 3개씩)
using UnityEngine;

namespace CatTang
{
    public static class GunkataFx
    {
        static Material[] smokeMats, flashMats;
        static Material lineMat, fireMat;
        internal static Mesh quad;
        static readonly Color Ray = new Color(1f, 0.97f, 0.82f);
        static readonly Color Spray = new Color(1f, 0.36f, 0.2f, 0.9f);
        static readonly Color SwirlCol = new Color(0.22f, 0.13f, 0.11f, 0.85f);

        static bool inited;
        static bool Init()
        {
            if (inited) return fireMat != null;
            inited = true;
            var sh = Shader.Find("Sprites/Default");
            smokeMats = Load("FX/smoke_", 2, sh, 3000);
            flashMats = Load("FX/flash_", 3, sh, 3003);
            lineMat = new Material(sh) { renderQueue = 3001 };
            var fm = Resources.Load<Material>("FX/ToonFire");
            if (fm) fireMat = fm;
            else { var fire = Shader.Find("CatTang/ToonFire"); if (fire) fireMat = new Material(fire); }
            quad = new Mesh { name = "FxQuad" };
            quad.vertices = new[] { new Vector3(-0.5f, -0.5f, 0), new Vector3(0.5f, -0.5f, 0), new Vector3(-0.5f, 0.5f, 0), new Vector3(0.5f, 0.5f, 0) };
            quad.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
            quad.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            quad.RecalculateBounds();
            return fireMat != null;
        }

        static Material[] Load(string prefix, int n, Shader sh, int queue)
        {
            var list = new System.Collections.Generic.List<Material>();
            for (int i = 0; i < n; i++)
            {
                var t = Resources.Load<Texture2D>(prefix + i);
                if (t) list.Add(new Material(sh) { mainTexture = t, renderQueue = queue });
            }
            return list.ToArray();
        }

        /// <summary>총구 한 발. p = 총구, d = 바깥 방향(수평), full = 본체(폭발 구름 + 붉은 흩뿌림) / 분신(폭발만)
        /// 주인공 = 손으로 그린 2D 프레임(Codex 시트 FX/anim/blast) · 3D 불꽃 원뿔은 아래에 깔아 입체감만 더함</summary>
        /// <summary>true = 건카타 폭발을 Codex 손그림 프레임으로, false = 3D 불꽃 (2026-11-16 사용자: 3D가 더 나음)</summary>
        public static bool UseDrawnBlast = false;

        public static void Burst(Vector3 p, Vector3 d, float scale, bool full)
        {
            if (!Init()) return;
            d = d.sqrMagnitude > 1e-4f ? d.normalized : Vector3.forward;
            var rot = Quaternion.LookRotation(d, Vector3.up) * Quaternion.Euler(0, 0, Random.Range(0f, 360f));
            bool drawn = UseDrawnBlast && FxFlipbook.Spawn("blast", p, d, 2.7f * scale * Random.Range(0.92f, 1.08f), 0.4f, true, d * 0.6f * scale);
            if (drawn)
            {
                // 3D 원뿔: 짧고 작게 (그림 밑에서 부피감)
                FxMesh.Spawn(FxMeshes.Flame(Random.Range(0, 3)), fireMat, p, rot, new Vector3(0.38f, 0.38f, 1.1f) * scale, 0.12f, 0, d * 0.8f * scale, FxMesh.Kind.Flame);
                if (full)
                {
                    var side = Vector3.Cross(Vector3.up, d).normalized;
                    FxFlipbook.Spawn("cloud", p + d * 1.5f * scale + side * Random.Range(-0.25f, 0.25f) * scale + Vector3.up * 0.15f, Vector3.zero, 1.7f * scale, 0.55f, false,
                        d * 1.2f * scale + Vector3.up * 0.4f, Random.Range(0f, 360f));
                }
            }
            else
            {
                // 3D 방식 (사용자 선택): 2D 가시 섬광 + 3D 가시 꽃잎 불꽃 원뿔 + (본체) 3D 불덩이 2개
                if (flashMats.Length > 0)
                    FxSprite.Spawn(flashMats[Random.Range(0, flashMats.Length)], p + d * 0.3f * scale, 1.7f * scale, 0.07f, 0, Vector3.zero, FxSprite.Style.Flash, d);
                FxMesh.Spawn(FxMeshes.Flame(Random.Range(0, 3)), fireMat, p, rot, new Vector3(0.5f, 0.5f, 1.5f) * scale, 0.18f, 0, d * 0.8f * scale, FxMesh.Kind.Flame);
                int n = full ? 2 : 0;
                var side = Vector3.Cross(Vector3.up, d).normalized;
                for (int i = 0; i < n; i++)
                {
                    var pos = p + d * (0.9f + 0.55f * i) * scale + side * Random.Range(-0.2f, 0.2f) * scale + Vector3.up * Random.Range(-0.05f, 0.2f);
                    float size = (0.55f - 0.15f * i) * scale * Random.Range(0.9f, 1.1f);
                    FxMesh.Spawn(FxMeshes.Ball(Random.Range(0, 3)), fireMat, pos, Random.rotation, Vector3.one * size, 0.32f, 0.02f + i * 0.03f, d * 1.4f * scale + Vector3.up * 0.5f, FxMesh.Kind.Ball);
                }
            }
            // 하얀 빛줄기: 총구에서 바깥으로 쭉
            FxLine.Ray(p, d, 3.0f * scale, 0.12f * scale, Ray, 0.07f);
            if (!full) return;
            // 붉은 흩뿌림: 부채꼴 가는 선
            for (int k = 0; k < 4; k++)
            {
                var dd = Quaternion.AngleAxis(Random.Range(-22f, 22f), Vector3.up) * d;
                dd.y = Random.Range(-0.08f, 0.12f);
                FxLine.Ray(p + dd * 0.4f * scale, dd.normalized, Random.Range(2.2f, 3.6f) * scale, 0.035f * scale, Spray, 0.13f);
            }
        }

        /// <summary>평소 사격 총구 화염 (작게): 손 그림 섬광 4프레임 + 아주 작은 3D 원뿔</summary>
        public static void MuzzleFlash(Vector3 p, Vector3 d, float scale)
        {
            if (!Init()) return;
            d = d.sqrMagnitude > 1e-4f ? d.normalized : Vector3.forward;
            if (!FxFlipbook.Spawn("flash", p, d, 1.5f * scale * Random.Range(0.9f, 1.1f), 0.12f, true))
                if (flashMats.Length > 0) FxSprite.Spawn(flashMats[Random.Range(0, flashMats.Length)], p + d * 0.2f * scale, 0.8f * scale, 0.06f, 0, Vector3.zero, FxSprite.Style.Flash, d);
            var rot = Quaternion.LookRotation(d, Vector3.up) * Quaternion.Euler(0, 0, Random.Range(0f, 360f));
            FxMesh.Spawn(FxMeshes.Flame(Random.Range(0, 3)), fireMat, p, rot, new Vector3(0.18f, 0.18f, 0.5f) * scale, 0.07f, 0, d * 0.5f * scale, FxMesh.Kind.Flame);
        }

        /// <summary>회색 연기 뭉치</summary>
        public static void Smoke(Vector3 at, float size, float life = 0.9f)
        {
            if (!Init() || smokeMats.Length == 0) return;
            FxSprite.Spawn(smokeMats[Random.Range(0, smokeMats.Length)], at, size, life, 0, Vector3.up * 0.5f + Random.insideUnitSphere * 0.4f, FxSprite.Style.Smoke);
        }

        /// <summary>작은 불꽃 한 점 (장전 철컥 등): 섬광 없이 작은 불덩이만</summary>
        public static void Spark(Vector3 at, float size)
        {
            if (!Init()) return;
            FxMesh.Spawn(FxMeshes.Ball(Random.Range(0, 3)), fireMat, at, Random.rotation, Vector3.one * size, 0.22f, 0, Vector3.up * 0.3f, FxMesh.Kind.Ball);
        }

        /// <summary>섬광 한 장만 (장전 철컥 반짝 등)</summary>
        public static void Flash(Vector3 at, Vector3 dir, float size, float life = 0.08f)
        {
            if (!Init() || flashMats.Length == 0) return;
            FxSprite.Spawn(flashMats[Random.Range(0, flashMats.Length)], at, size, life, 0, Vector3.zero, FxSprite.Style.Flash, dir);
        }

        /// <summary>몸 주위를 기울어진 원으로 도는 어두운 곡선 (spin = 도/초)</summary>
        public static void Swirl(Vector3 center, float radius, float spin, float life = 0.35f)
        {
            if (!Init()) return;
            FxLine.Arc(center, radius, spin, SwirlCol, life);
        }

        internal static Material LineMat { get { Init(); return lineMat; } }
        internal static Mesh Quad { get { Init(); return quad; } }
    }

    /// <summary>2D 프레임 애니메이션 (Codex 시트를 잘라 만든 FX/anim/이름_0~N). 카메라를 보고, 그림의 +x(오른쪽)를 화면에 비친 방향 d에 맞춤.
    /// pivotLeft = 그림 왼쪽 가운데가 원점(총구)</summary>
    public class FxFlipbook : MonoBehaviour
    {
        static readonly System.Collections.Generic.Dictionary<string, Material[]> sets = new();
        static readonly int ColorId = Shader.PropertyToID("_Color");
        Material[] frames;
        MeshRenderer mr;
        MaterialPropertyBlock mpb;
        float born, life, size;
        Vector3 origin, dir, drift;
        bool pivotLeft;
        float roll;

        public static int Count(string set) => Load(set).Length;

        static Material[] Load(string set)
        {
            if (sets.TryGetValue(set, out var m)) return m;
            var sh = Shader.Find("Sprites/Default");
            var list = new System.Collections.Generic.List<Material>();
            for (int i = 0; i < 32; i++)
            {
                var t = Resources.Load<Texture2D>("FX/anim/" + set + "_" + i);
                if (!t) break;
                list.Add(new Material(sh) { mainTexture = t, renderQueue = 3004 });
            }
            return sets[set] = list.ToArray();
        }

        public static bool Spawn(string set, Vector3 pos, Vector3 dir, float size, float life, bool pivotLeft, Vector3 drift = default, float roll = 0)
        {
            var fr = Load(set);
            if (fr.Length == 0) return false;
            var go = new GameObject("Flip_" + set);
            go.AddComponent<MeshFilter>().sharedMesh = GunkataFx.Quad;
            var f = go.AddComponent<FxFlipbook>();
            f.mr = go.AddComponent<MeshRenderer>();
            f.mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            f.mr.receiveShadows = false;
            f.mpb = new MaterialPropertyBlock();
            f.frames = fr; f.origin = pos; f.dir = dir; f.size = size; f.life = life; f.pivotLeft = pivotLeft; f.drift = drift; f.roll = roll;
            f.born = Time.time;
            f.Pose(0);
            return true;
        }

        void LateUpdate()
        {
            float t = (Time.time - born) / life;
            if (t >= 1) { Destroy(gameObject); return; }
            origin += drift * Time.deltaTime;
            Pose(t);
        }

        void Pose(float t)
        {
            int i = Mathf.Min(frames.Length - 1, Mathf.FloorToInt(t * frames.Length));
            mr.sharedMaterial = frames[i];
            var cam = Camera.main;
            float ang = roll;
            Vector3 right = Vector3.right;
            if (cam)
            {
                if (dir.sqrMagnitude > 1e-4f)
                {
                    var v = cam.transform.InverseTransformDirection(dir);
                    ang = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
                }
                var rot = cam.transform.rotation * Quaternion.Euler(0, 0, ang);
                transform.rotation = rot;
                right = rot * Vector3.right;
            }
            transform.localScale = Vector3.one * size;
            transform.position = pivotLeft ? origin + right * size * 0.5f : origin;
            // 마지막 20%는 살짝 옅어짐
            var c = Color.white; c.a = t < 0.8f ? 1 : 1 - (t - 0.8f) / 0.2f;
            mpb.SetColor(ColorId, c);
            mr.SetPropertyBlock(mpb);
        }
    }

    /// <summary>코드로 구운 3D 이펙트 메시 (처음 쓸 때 한 번 만들어 공유)</summary>
    public static class FxMeshes
    {
        static Mesh[] flames, balls;

        /// <summary>불꽃 원뿔: 원점(총구)에서 +z로 뻗음. 좁은 뿌리 → 넓게 퍼지다 끝이 가시 꽃잎으로 갈라짐. 길이 1, 반지름 약 0.5</summary>
        public static Mesh Flame(int v)
        {
            if (flames == null) { flames = new Mesh[3]; for (int i = 0; i < 3; i++) flames[i] = BuildFlame(101 + i * 17); }
            return flames[Mathf.Abs(v) % 3];
        }

        /// <summary>울퉁불퉁한 불덩이: 지름 약 1</summary>
        public static Mesh Ball(int v)
        {
            if (balls == null) { balls = new Mesh[3]; for (int i = 0; i < 3; i++) balls[i] = BuildBall(7 + i * 13); }
            return balls[Mathf.Abs(v) % 3];
        }

        public static Mesh BuildFlame(int seed)
        {
            var rng = new System.Random(seed);
            float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
            const int U = 18, K = 64;
            int petals = 6 + rng.Next(3);
            float phase = R(0, Mathf.PI * 2);
            var petalLen = new float[petals];
            for (int i = 0; i < petals; i++) petalLen[i] = R(0.55f, 1f);
            var verts = new Vector3[(U + 1) * K + 1];
            for (int k = 0; k < K; k++)
            {
                float th = k * Mathf.PI * 2 / K;
                // 꽃잎: 각도마다 가장 가까운 꽃잎 중심까지의 거리 → 뾰족한 봉우리
                float pa = (th + phase) / (Mathf.PI * 2) * petals;
                int pi = Mathf.FloorToInt(pa) % petals;
                float fr = pa - Mathf.Floor(pa);
                float peak = 1 - Mathf.Abs(fr - 0.5f) * 2;                 // 0(골) ~ 1(봉우리)
                float sp = Mathf.Pow(peak, 2.2f);
                float zEnd = 0.62f + 0.38f * sp * petalLen[pi];             // 봉우리는 길게 뻗음
                float wid = 0.7f + 0.45f * sp;
                for (int u = 0; u <= U; u++)
                {
                    float t = u / (float)U;
                    float prof = Mathf.Sin(Mathf.PI * Mathf.Pow(t, 0.62f));  // 0 → 1 → 0
                    float r = 0.5f * prof * wid * (1 + 0.08f * Mathf.Sin(th * 3 + t * 9 + phase));
                    r += 0.12f * sp * t * t * prof;                          // 꽃잎 끝이 바깥으로 살짝 벌어짐
                    verts[u * K + k] = new Vector3(Mathf.Cos(th) * r, Mathf.Sin(th) * r, t * zEnd);
                }
            }
            int baseIdx = (U + 1) * K;
            verts[baseIdx] = new Vector3(0, 0, -0.02f);
            var tris = new System.Collections.Generic.List<int>();
            for (int u = 0; u < U; u++)
                for (int k = 0; k < K; k++)
                {
                    int a = u * K + k, b = u * K + (k + 1) % K, c = (u + 1) * K + k, d = (u + 1) * K + (k + 1) % K;
                    tris.Add(a); tris.Add(b); tris.Add(c);
                    tris.Add(b); tris.Add(d); tris.Add(c);
                }
            for (int k = 0; k < K; k++) { tris.Add(baseIdx); tris.Add((k + 1) % K); tris.Add(k); }
            var m = new Mesh { name = "FxFlame" + seed, vertices = verts, triangles = tris.ToArray() };
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        public static Mesh BuildBall(int seed)
        {
            var rng = new System.Random(seed);
            float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
            // 혹 8~10개: 방향 · 크기
            int n = 8 + rng.Next(3);
            var dirs = new Vector3[n]; var amp = new float[n];
            for (int i = 0; i < n; i++) { dirs[i] = new Vector3(R(-1, 1), R(-1, 1), R(-1, 1)).normalized; amp[i] = R(0.18f, 0.38f); }
            const int LAT = 20, LON = 32;
            var verts = new Vector3[(LAT + 1) * LON];
            for (int a = 0; a <= LAT; a++)
                for (int b = 0; b < LON; b++)
                {
                    float th = Mathf.PI * a / LAT, ph = Mathf.PI * 2 * b / LON;
                    var nrm = new Vector3(Mathf.Sin(th) * Mathf.Cos(ph), Mathf.Cos(th), Mathf.Sin(th) * Mathf.Sin(ph));
                    float r = 0.36f;
                    for (int i = 0; i < n; i++) r += amp[i] * Mathf.Pow(Mathf.Max(0, Vector3.Dot(nrm, dirs[i])), 5f);
                    verts[a * LON + b] = nrm * r;
                }
            var tris = new System.Collections.Generic.List<int>();
            for (int a = 0; a < LAT; a++)
                for (int b = 0; b < LON; b++)
                {
                    int i0 = a * LON + b, i1 = a * LON + (b + 1) % LON, i2 = (a + 1) * LON + b, i3 = (a + 1) * LON + (b + 1) % LON;
                    tris.Add(i0); tris.Add(i1); tris.Add(i2);
                    tris.Add(i1); tris.Add(i3); tris.Add(i2);
                }
            var m = new Mesh { name = "FxBall" + seed, vertices = verts, triangles = tris.ToArray() };
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }
    }

    /// <summary>3D 불꽃 메시 하나: 확 뻗거나 부풀고 → 식으며 → 노이즈 구멍으로 녹아 사라짐</summary>
    public class FxMesh : MonoBehaviour
    {
        public enum Kind { Flame, Ball }
        static readonly int HeatId = Shader.PropertyToID("_Heat"), DissolveId = Shader.PropertyToID("_Dissolve"), SeedId = Shader.PropertyToID("_Seed");
        MeshRenderer mr;
        MaterialPropertyBlock mpb;
        float born, life, delay, spin;
        Vector3 size, drift, spinAxis;
        Kind kind;

        public static void Spawn(Mesh mesh, Material mat, Vector3 pos, Quaternion rot, Vector3 size, float life, float delay, Vector3 drift, Kind kind)
        {
            if (!mesh || !mat) return;
            var go = new GameObject(kind == Kind.Flame ? "FxFlame" : "FxBall");
            go.transform.SetPositionAndRotation(pos, rot);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var f = go.AddComponent<FxMesh>();
            f.mr = go.AddComponent<MeshRenderer>();
            f.mr.sharedMaterial = mat;
            f.mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            f.mr.receiveShadows = false;
            f.mpb = new MaterialPropertyBlock();
            f.mpb.SetFloat(SeedId, Random.Range(0f, 100f));
            f.born = Time.time; f.life = life; f.delay = delay; f.size = size; f.drift = drift; f.kind = kind;
            f.spin = kind == Kind.Flame ? Random.Range(-240f, 240f) : Random.Range(-120f, 120f);
            f.spinAxis = kind == Kind.Flame ? Vector3.forward : Random.onUnitSphere;
            f.Pose(-1);
        }

        void LateUpdate()
        {
            float t = (Time.time - born - delay) / life;
            if (t >= 1) { Destroy(gameObject); return; }
            if (t >= 0)
            {
                transform.position += drift * Time.deltaTime;
                drift *= Mathf.Exp(-6f * Time.deltaTime);
                transform.rotation = transform.rotation * Quaternion.AngleAxis(spin * Time.deltaTime, spinAxis);
            }
            Pose(t);
        }

        void Pose(float t)
        {
            mr.enabled = t >= 0;
            if (t < 0) { transform.localScale = Vector3.zero; return; }
            Vector3 sc; float heat, dis;
            if (kind == Kind.Flame)
            {
                // 0.25 동안 확 뻗고(길이 0.25 → 1.1, 굵기 0.5 → 1) 조금 더 늘어나며 식고 녹음
                float e = Mathf.Clamp01(t / 0.25f); e = 1 - (1 - e) * (1 - e) * (1 - e);
                float len = Mathf.Lerp(0.25f, 1.1f, e) + 0.15f * t;
                float wid = Mathf.Lerp(0.5f, 1f, e) * (1 - 0.25f * t);
                sc = new Vector3(size.x * wid, size.y * wid, size.z * len);
                heat = 1 - Mathf.Clamp01((t - 0.3f) / 0.7f);
                dis = Mathf.SmoothStep(0, 1, Mathf.Clamp01((t - 0.3f) / 0.7f));
            }
            else
            {
                // 0.15 동안 튀어나오듯 부풀고(살짝 넘쳤다 돌아옴) 천천히 커지며 그을린 색으로 식고 녹음
                float e = Mathf.Clamp01(t / 0.15f);
                float pop = e < 1 ? Mathf.Lerp(0.3f, 1.12f, Mathf.Sin(e * Mathf.PI * 0.5f)) : 1.12f - 0.12f * Mathf.Clamp01((t - 0.15f) / 0.15f);
                sc = size * pop * (1 + 0.3f * t);
                heat = 1 - Mathf.Clamp01((t - 0.25f) / 0.6f);
                dis = Mathf.SmoothStep(0, 1, Mathf.Clamp01((t - 0.35f) / 0.65f));
            }
            transform.localScale = sc;
            mpb.SetFloat(HeatId, heat);
            mpb.SetFloat(DissolveId, dis);
            mr.SetPropertyBlock(mpb);
        }
    }

    /// <summary>카메라를 보는 스프라이트 한 장 (불덩이 / 연기)</summary>
    public class FxSprite : MonoBehaviour
    {
        public enum Style { Fire, Smoke, Flash }
        static readonly int ColorId = Shader.PropertyToID("_Color");
        MeshRenderer mr;
        MaterialPropertyBlock mpb;
        float born, life, delay, size, roll, rollSpeed;
        Vector3 drift;
        Style style;
        Vector3 dir;

        public static void Spawn(Material mat, Vector3 pos, float size, float life, float delay, Vector3 drift, Style style, Vector3 dir = default)
        {
            var go = new GameObject(style == Style.Smoke ? "Smoke" : style == Style.Flash ? "Flash" : "FirePuff");
            go.transform.position = pos;
            go.AddComponent<MeshFilter>().sharedMesh = GunkataFx.quad;
            var s = go.AddComponent<FxSprite>();
            s.mr = go.AddComponent<MeshRenderer>();
            s.mr.sharedMaterial = mat;
            s.mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            s.mr.receiveShadows = false;
            s.mpb = new MaterialPropertyBlock();
            s.born = Time.time; s.life = life; s.delay = delay; s.size = size; s.drift = drift; s.style = style;
            s.dir = dir;
            s.roll = style == Style.Flash ? 0 : Random.Range(0f, 360f);
            s.rollSpeed = style == Style.Flash ? 0 : Random.Range(-90f, 90f);
            s.Pose(0);
        }

        void LateUpdate()
        {
            float t = (Time.time - born - delay) / life;
            if (t >= 1) { Destroy(gameObject); return; }
            transform.position += drift * Time.deltaTime;
            drift *= Mathf.Exp(-4f * Time.deltaTime);
            roll += rollSpeed * Time.deltaTime;
            Pose(t);
        }

        void Pose(float t)
        {
            mr.enabled = t >= 0;
            if (t < 0) return;
            var cam = Camera.main;
            if (cam && style == Style.Flash)
            {
                // 섬광 그림의 +x(긴 가시) = 화면에 비친 총구 방향
                var v = cam.transform.InverseTransformDirection(dir);
                roll = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
            }
            if (cam) transform.rotation = cam.transform.rotation * Quaternion.Euler(0, 0, roll);
            float sc; Color c;
            if (style == Style.Flash)
            {
                // 크게 번쩍 → 살짝 커지며 끝에서 사라짐
                sc = Mathf.Lerp(0.75f, 1.15f, t);
                c = Color.white;
                c.a = t < 0.6f ? 1 : 1 - (t - 0.6f) / 0.4f;
            }
            else if (style == Style.Fire)
            {
                // 0.06초 만에 확 부풀고(0.35→1) 천천히 더 커지며 → 하얗게 뜨겁다가 그을린 갈색으로 식으며 사라짐
                float pop = t < 0.2f ? 1 - (1 - t / 0.2f) * (1 - t / 0.2f) : 1;
                sc = Mathf.Lerp(0.35f, 1f, pop) * (1 + 0.15f * t);
                float cool = Mathf.Clamp01((t - 0.3f) / 0.5f);
                c = Color.Lerp(Color.white, new Color(0.55f, 0.38f, 0.33f), cool);
                c.a = t < 0.6f ? 1 : 1 - (t - 0.6f) / 0.4f;
            }
            else
            {
                // 연기: 부풀며 진해졌다가 옅어짐
                sc = Mathf.Lerp(0.7f, 1.5f, 1 - (1 - t) * (1 - t));
                c = Color.white;
                c.a = t < 0.15f ? t / 0.15f * 0.8f : 0.8f * (1 - (t - 0.15f) / 0.85f);
            }
            transform.localScale = Vector3.one * size * sc;
            mpb.SetColor(ColorId, c);
            mr.SetPropertyBlock(mpb);
        }
    }

    /// <summary>선 이펙트: 빛줄기(Ray) · 소용돌이 곡선(Arc)</summary>
    public class FxLine : MonoBehaviour
    {
        LineRenderer lr;
        float born, life, len, width, radius, spin, startAng;
        Vector3 origin, dir;
        Quaternion tilt;
        Color col;
        bool arc;
        const int ArcPts = 18;

        static FxLine Make(Color c, float life, int points)
        {
            var go = new GameObject("FxLine");
            var f = go.AddComponent<FxLine>();
            f.lr = go.AddComponent<LineRenderer>();
            f.lr.sharedMaterial = GunkataFx.LineMat;
            f.lr.positionCount = points;
            f.lr.numCapVertices = 2;
            f.lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            f.lr.receiveShadows = false;
            f.col = c; f.life = life; f.born = Time.time;
            return f;
        }

        /// <summary>총구에서 바깥으로 뻗는 가는 빛줄기 (뿌리 굵게 → 끝 뾰족)</summary>
        public static void Ray(Vector3 from, Vector3 dir, float len, float width, Color c, float life)
        {
            var f = Make(c, life, 2);
            f.origin = from; f.dir = dir.normalized; f.len = len; f.width = width;
            f.Pose(0);
        }

        /// <summary>몸 주위를 기울어진 원으로 도는 곡선 (150° 호, 꼬리로 갈수록 가늘게)</summary>
        public static void Arc(Vector3 center, float radius, float spin, Color c, float life)
        {
            var f = Make(c, life, ArcPts);
            f.arc = true;
            f.origin = center; f.radius = radius; f.spin = spin;
            f.startAng = Random.Range(0f, 360f);
            f.tilt = Quaternion.Euler(Random.Range(-28f, 28f), Random.Range(0f, 360f), Random.Range(-28f, 28f));
            f.width = Random.Range(0.12f, 0.2f);
            f.Pose(0);
        }

        void LateUpdate()
        {
            float t = (Time.time - born) / life;
            if (t >= 1) { Destroy(gameObject); return; }
            Pose(t);
        }

        void Pose(float t)
        {
            var c = col;
            c.a *= t < 0.5f ? 1 : 1 - (t - 0.5f) / 0.5f;
            if (!arc)
            {
                float grow = 1 - (1 - Mathf.Clamp01(t / 0.35f)) * (1 - Mathf.Clamp01(t / 0.35f));
                lr.SetPosition(0, origin);
                lr.SetPosition(1, origin + dir * len * Mathf.Lerp(0.4f, 1f, grow));
                lr.startWidth = width * (1 - t * 0.7f);
                lr.endWidth = 0;
                lr.startColor = c;
                var e = c; e.a *= 0.3f; lr.endColor = e;
                return;
            }
            // 소용돌이: 반지름이 조금 커지며 빠르게 돎
            float r = radius * (1 + 0.3f * t);
            float a0 = startAng + spin * (Time.time - born);
            for (int i = 0; i < ArcPts; i++)
            {
                float a = (a0 - Mathf.Sign(spin) * i * 150f / (ArcPts - 1)) * Mathf.Deg2Rad;   // 꼬리는 도는 방향 뒤쪽
                lr.SetPosition(i, origin + tilt * new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r));
            }
            lr.startWidth = width;
            lr.endWidth = 0;
            lr.startColor = c;
            var tail = c; tail.a = 0; lr.endColor = tail;
        }
    }
}
