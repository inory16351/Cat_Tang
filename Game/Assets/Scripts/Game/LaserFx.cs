// 4장 (시계 장치 속, 느와르) 적 공격 = 레이저 느낌 (2026-11-19 사용자: 레이저 같은 느낌, 빠른 움직임 때 눈의 빨간 빛이 궤적으로 남음,
// 이펙트도 같은 느낌, 단 그림체는 다른 장과 같은 방식 — 툰 띠 + 잉크선 플립북, 플랫 + 브롤풍 셰이더와 이질적이지 않게)
// - 근접 베기: 부채꼴 예고(Telegraph.Sector)가 끝나는 순간 그 방향 · 반지름으로 레이저 호
//     = GPT 그림 시트 cx_c4slash (docs/images/fx_src/c4_laserslash.png → fx_sheet_cut.py --strict) + 블렌더로 구운 bk_c4slash (Art/tools/blender_bake_fx.py)
// - 돌진 · 직선 공격: 직선 예고(Telegraph.Lane)가 끝나는 순간 그 길을 따라 레이저 줄 (bk_c4strip 텍스처 LineRenderer)
// - 눈빛 꼬리 EyeTrail: 4장 적 머리에 붉은 눈 두 점, 빨리 움직일 때(점멸 · 돌진 · 회전 베기)만 레이저 꼬리 (TrailRenderer, bk_c4strip)
//   칼끝 · 눈에서 나오는 이동 궤적(눈빛 꼬리 · 돌진 자국 · 베기 리본)은 끝이 리본 꼬리(제비꼬리)로 끝남 (2026-11-19 사용자)
// - 탄 · 불똥은 EnemyProjectile (bk_c4orb · bk_c4needle · bk_c4spark)
using UnityEngine;

namespace CatTang
{
    public static class LaserFx
    {
        public static bool On => EnemyProjectile.Ch4;

        static Material stripMat;
        static Material stripMat5;
        /// <summary>5장 자홍 리본 꼬리 (블렌더 bk_c5strip). 없으면 4장 띠</summary>
        public static Material StripMat5()
        {
            if (stripMat5) return stripMat5;
            var tex = Resources.Load<Texture2D>("FX/anim/bk_c5strip_0");
            if (!tex) return StripMat();
            var sh = Shader.Find("CatTang/Sprite") ?? Shader.Find("Sprites/Default");
            stripMat5 = new Material(sh) { mainTexture = tex, renderQueue = 3003 };
            return stripMat5;
        }

        public static Material StripMat()
        {
            if (stripMat) return stripMat;
            var tex = Resources.Load<Texture2D>("FX/anim/bk_c4strip_0");
            var sh = Shader.Find("CatTang/Sprite") ?? Shader.Find("Sprites/Default");
            stripMat = new Material(sh) { mainTexture = tex, renderQueue = 3003 };
            return stripMat;
        }

        /// <summary>잠깐 뒤에 실행 (이펙트 박자 맞추기)</summary>
        public static void Later(float sec, System.Action act)
        {
            var go = new GameObject("FxLater");
            go.AddComponent<FxLater>().Init(sec, act);
        }

        /// <summary>길이 방향 색 (머리 → 꼬리): 머리 흰빛 그대로, 꼬리로 갈수록 붉고 옅어짐 — 끝은 리본 꼬리 모양이 남을 만큼만 옅게</summary>
        public static Gradient TailGradient() => TailGradient(new Color(0.85f, 0.25f, 0.28f));

        /// <summary>흰 심지 → tail 색 꼬리 (5장 거울 고양이 = 자홍)</summary>
        public static Gradient TailGradient(Color tail)
        {
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.Lerp(Color.white, tail, 0.6f), 0.6f), new GradientColorKey(tail, 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.9f, 0.6f), new GradientAlphaKey(0.55f, 1f) });
            return g;
        }

        /// <summary>레이저 베기 호: at = 휘두르는 몸, dir = 가운데 방향, radius = 사거리</summary>
        public static void Slash(Vector3 at, Vector3 dir, float radius)
        {
            dir.y = 0;
            if (dir.sqrMagnitude < 1e-4f) dir = Vector3.forward;
            dir.Normalize();
            var p = new Vector3(at.x, 1.0f, at.z) + dir * radius * 0.35f;
            float size = Mathf.Max(2f, radius * 2.1f);
            FxFlipbook.Spawn("bk_c4slash", p, dir, size * 0.95f, 0.34f, false);
            FxFlipbook.Spawn("cx_c4slash", p + dir * 0.1f, dir, size, 0.4f, false);
            CameraFollow.Shake(0.03f);
            // 궤적 종결: 리본 꼬리가 칼끝을 따라잡는 끝 무렵 칼끝 자리에 작은 불똥 (구운 베기 프레임과 같은 박자)
            var tip = new Vector3(at.x, 1.0f, at.z) + Quaternion.Euler(0, 60f, 0) * dir * radius * 0.9f;
            Later(0.26f, () => FxFlipbook.Spawn("bk_c4spark", tip, Vector3.zero, 0.8f, 0.25f, false));
        }

        /// <summary>돌진 · 직선 공격이 지나간 자리에 남는 레이저 줄 (0.35초에 가늘어지며 사라짐)</summary>
        public static void Streak(Vector3 from, Vector3 dir, float len, float width)
        {
            dir.y = 0;
            if (dir.sqrMagnitude < 1e-4f) return;
            var go = new GameObject("LaserStreak"); VividLayer.Set(go);
            var lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial = StripMat();
            lr.textureMode = LineTextureMode.Stretch;
            lr.useWorldSpace = true;
            lr.numCapVertices = 0;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.positionCount = 2;
            var a = new Vector3(from.x, 0.9f, from.z);
            lr.SetPosition(0, a + dir.normalized * len);   // 텍스처 U 0 = 머리(도착 쪽), 1 = 리본 꼬리(출발 쪽)
            lr.SetPosition(1, a);
            go.AddComponent<LaserFade>().Init(lr, Mathf.Clamp(width * 0.35f, 0.25f, 0.9f), 0.35f);
            FxFlipbook.Spawn("bk_c4spark", a + dir.normalized * len, Vector3.zero, 1.6f, 0.35f, false);
        }
    }

    /// <summary>
    /// 돌진 자국 마무리 (궤적 종결: 꼬리가 머리 쪽으로 따라 들어가며 사라짐 — 한꺼번에 가늘어지지 않음).
    /// 머리(도착 쪽)는 0.08초 그대로 빛나다가, 리본 꼬리(출발 쪽)가 머리로 빨려 들어가고, 길이 방향으로 머리 흰빛 → 꼬리 옅은 빨강.
    /// 다 들어가면 머리 자리에 작은 불똥
    /// </summary>
    public class LaserFade : MonoBehaviour
    {
        LineRenderer lr; float w0, life, born; Vector3 head, tail;
        public void Init(LineRenderer l, float width, float dur)
        {
            lr = l; w0 = width; life = dur; born = Time.time;
            head = lr.GetPosition(0); tail = lr.GetPosition(1);
            lr.widthMultiplier = w0;
            lr.widthCurve = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 0.75f));
            lr.colorGradient = LaserFx.TailGradient();
        }
        void Update()
        {
            float t = (Time.time - born) / life;
            if (t >= 1) { FxFlipbook.Spawn("bk_c4spark", head, Vector3.zero, 0.9f, 0.25f, false); Destroy(gameObject); return; }
            float k = Mathf.Clamp01((t - 0.22f) / 0.78f);
            k = k * k * (3 - 2 * k);                                   // 부드럽게 출발 · 도착
            lr.SetPosition(1, Vector3.Lerp(tail, head, k));
            lr.widthMultiplier = w0 * (1 - 0.35f * k);
        }
    }

    /// <summary>
    /// 눈빛 꼬리 (4장 적): 머리 앞쪽에 붉은 눈 두 점(TrailRenderer 기준점). 빠르게 움직일 때만(초속 4.5m 넘음 · 순간 점멸) 레이저 꼬리를 그림.
    /// 점멸은 한 프레임에 크게 옮겨지므로 그 사이가 곧은 레이저 줄로 이어짐 (초침 자객의 째깍 점멸이 그대로 보임)
    /// </summary>
    public class EyeTrail : MonoBehaviour
    {
        public const float FastSpeed = 4.5f;
        TrailRenderer[] trails;
        Transform[] eyes;
        Vector3 last;
        float hotUntil, curTime;
        Vector3[] prevEye = new Vector3[2];
        bool wasOn;
        public const float TrailTime = 0.22f, RetractTime = 0.16f;

        Color? tint;
        float eyeY = -1f, eyeFwd = -1f, eyeGap = -1f;

        public static EyeTrail Attach(Enemy e) => Attach(e, null);

        /// <summary>tint = 꼬리 색 (없으면 4장 빨강). 눈 자리를 알면 y · fwd · gap(몸 기준 m)으로 직접 (2등신 고양이처럼 머리가 큰 몸)</summary>
        public static EyeTrail Attach(Enemy e, Color? tint, float y = -1f, float fwd = -1f, float gap = -1f)
        {
            if (!e || e.GetComponent<EyeTrail>()) return null;
            var t = e.gameObject.AddComponent<EyeTrail>();
            t.tint = tint; t.eyeY = y; t.eyeFwd = fwd; t.eyeGap = gap;
            t.Build(e);
            return t;
        }

        // 2026-11-22 사용자: "눈에 붙는 레이저 궤적 — 타격처럼 단색에 광효과, 눈 위치 정확히 파악해서 눈에 붙여"
        // → 휴머노이드면 머리 뼈에 붙여 동작(회전 베기 · 찌르기)을 그대로 따라감. 단색 심지(알파 섞기 — 밝은 바닥에서도 또렷) + 넓은 빛 번짐(빛 더하기) 두 겹
        public static readonly Color Ch4Red = new(1f, 0.1f, 0.14f);
        static Material coreMat, glowMat;

        /// <summary>폭 방향으로 가운데가 꽉 차고 가장자리만 부드러운 띠 (soft = 빛 번짐용 넓은 감쇠)</summary>
        static Texture2D Strip(bool soft)
        {
            const int W = 8, H = 64;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            for (int y = 0; y < H; y++)
            {
                float v = Mathf.Abs(y / (H - 1f) * 2f - 1f);                     // 0 = 가운데, 1 = 가장자리
                float a = soft ? Mathf.Pow(1f - v, 2.2f) : Mathf.Clamp01((1f - v) / 0.35f);
                for (int x = 0; x < W; x++) tex.SetPixel(x, y, new Color(1, 1, 1, a));
            }
            tex.Apply();
            return tex;
        }
        static Material CoreMat()
        {
            if (coreMat) return coreMat;
            var sh = Shader.Find("CatTang/Sprite") ?? Shader.Find("Sprites/Default");
            return coreMat = new Material(sh) { mainTexture = Strip(false), renderQueue = 3004 };
        }
        static Material GlowMat()
        {
            if (glowMat) return glowMat;
            var sh = Shader.Find("CatTang/AdditiveGlow") ?? Shader.Find("Sprites/Default");
            glowMat = new Material(sh) { mainTexture = Strip(true), renderQueue = 3005 };
            if (glowMat.HasProperty("_Intensity")) glowMat.SetFloat("_Intensity", 2.2f);
            return glowMat;
        }
        static Gradient Solid(Color c, float a0, float a1)
        {
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(c, 0f), new GradientColorKey(c, 1f) }, new[] { new GradientAlphaKey(a0, 0f), new GradientAlphaKey(a1, 1f) });
            return g;
        }

        void Build(Enemy e)
        {
            var col = tint ?? Ch4Red;
            // 눈 자리: 휴머노이드면 머리 뼈 기준 (리그에 눈 뼈가 있으면 그것, 없으면 목 → 머리 길이로 크기를 잡아 앞 · 위로). 아니면 렌더러 경계로 짐작
            var an = GetComponentInChildren<Animator>();
            bool human = an && an.isHuman && eyeY <= 0;
            Transform head = human ? an.GetBoneTransform(HumanBodyBones.Head) : null;
            var at = new Vector3[2];
            Transform parent = transform;
            float width = 0.14f;
            if (head)
            {
                parent = head; faceHead = head; placeAt = Time.frameCount + 2;   // 실제 얼굴 측정은 스킨이 한 번 돈 뒤(LateUpdate)로 미룸 — 만든 프레임엔 구운 메시가 아직 안 굽혀 있음
                {
                    // 메시를 못 읽으면 키 비례로 짐작 (머리 뼈 = 목 밑동 근처)
                    float hh = Mathf.Max(0.5f, head.position.y - transform.position.y);
                    for (int i = 0; i < 2; i++) at[i] = head.position + transform.up * hh * EyeUp + transform.forward * hh * EyeFwd + transform.right * hh * EyeGap * (i == 0 ? -1 : 1);
                    width = Mathf.Clamp(hh * 0.025f, 0.06f, 0.3f);
                }
            }
            else
            {
                float h = 2f, r = 0.5f;
                var rs = GetComponentsInChildren<Renderer>();
                if (rs.Length > 0)
                {
                    var bb = rs[0].bounds; foreach (var x in rs) if (!(x is TrailRenderer) && !(x is LineRenderer)) bb.Encapsulate(x.bounds);
                    h = bb.max.y - transform.position.y; r = Mathf.Max(bb.extents.x, bb.extents.z);
                }
                float y = Mathf.Max(0.8f, h * 0.84f), fwd = Mathf.Clamp(r * 0.35f, 0.15f, 0.6f), gap = Mathf.Clamp(h * 0.06f, 0.07f, 0.25f);
                if (eyeY > 0) y = eyeY;
                if (eyeFwd > 0) fwd = eyeFwd;
                if (eyeGap > 0) gap = eyeGap;
                for (int i = 0; i < 2; i++) at[i] = transform.TransformPoint(new Vector3(i == 0 ? -gap : gap, y, fwd));
            }
            trails = new TrailRenderer[4]; eyes = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                var go = new GameObject(i == 0 ? "EyeL" : "EyeR");
                go.transform.SetParent(parent, true);
                go.transform.position = at[i];
                trails[i] = MakeTrail(go, CoreMat(), width, Solid(col, 1f, 0.75f));
                var glow = new GameObject("Glow"); glow.transform.SetParent(go.transform, false);
                trails[i + 2] = MakeTrail(glow, GlowMat(), width * 3.6f, Solid(col, 0.55f, 0.2f));
                eyes[i] = go.transform; prevEye[i] = go.transform.position;
            }
            last = transform.position;
        }
        /// <summary>메시를 못 읽을 때 머리 뼈 기준 눈 자리 (머리 뼈 높이 배수): 위 · 앞 · 좌우 간격</summary>
        public static float EyeUp = 0.08f, EyeFwd = 0.07f, EyeGap = 0.035f;

        /// <summary>
        /// 실제 얼굴에서 눈 자리 찾기: 스킨 메시를 구워 머리 뼈(와 그 아래 뼈)에 주로 묶인 정점만 모아, 몸 기준 좌우 · 위 · 앞 축으로
        /// 백분위 범위를 잡음 (투구 장식 · 후광처럼 튀어나온 부분에 안 끌려가게). 눈 높이 = 머리 아래에서 45%, 깊이 = 그 높이 띠에서 가장 앞 얼굴 면, 간격 = 머리 폭 18%
        /// </summary>
        static readonly System.Collections.Generic.Dictionary<int, (Vector3 l, Vector3 r, float w)> faceCache = new();
        bool FaceFromMesh(Transform head, Vector3[] at, out float width)
        {
            var smr0 = GetComponentInChildren<SkinnedMeshRenderer>();
            int key = smr0 && smr0.sharedMesh ? smr0.sharedMesh.GetInstanceID() : 0;
            if (key != 0 && faceCache.TryGetValue(key, out var f))           // 같은 모델은 한 번만 굽기 (머리 뼈 로컬 좌표라 크기가 달라도 그대로)
            {
                at[0] = head.TransformPoint(f.l); at[1] = head.TransformPoint(f.r);
                width = f.w * head.lossyScale.y;
                return true;
            }
            bool ok = FaceFromMeshBake(head, at, out width);
            if (ok && key != 0) faceCache[key] = (head.InverseTransformPoint(at[0]), head.InverseTransformPoint(at[1]), width / Mathf.Max(1e-4f, head.lossyScale.y));
            return ok;
        }
        /// <summary>확인용: 아무 모델에서나 눈 두 점을 찾음 (적 컴포넌트 없이)</summary>
        public static bool FindEyes(GameObject go, Color? tint, out Vector3 l, out Vector3 r)
        {
            l = r = Vector3.zero;
            var an = go.GetComponentInChildren<Animator>();
            var head = an && an.isHuman ? an.GetBoneTransform(HumanBodyBones.Head) : null;
            if (!head) return false;
            var t = go.AddComponent<EyeTrail>(); t.tint = tint;
            var at = new Vector3[2];
            bool ok = t.FaceFromMeshBake(head, at, out _);
            Destroy(t);
            l = at[0]; r = at[1];
            return ok;
        }

        /// <summary>눈다운 덩어리: 너무 넓지 않고(고글 · 큰 눈까지) 세로로 긴 줄(투구 깃털 줄무늬)이 아님</summary>
        static bool EyeLike(float w, float h, float H) => w < H * 0.12f && h < H * 0.12f && h < w * 2.2f + H * 0.01f;

        /// <summary>점들을 격자 칸 이웃끼리 묶은 덩어리: 가운데 · 좌우 폭 · 가장 앞 깊이</summary>
        static System.Collections.Generic.List<(Vector3 c, float w, float h, float zMax)> Blobs(System.Collections.Generic.List<Vector3> ps, float cell)
        {
            var cells = new System.Collections.Generic.Dictionary<(int, int), System.Collections.Generic.List<Vector3>>();
            foreach (var p in ps)
            {
                var k = (Mathf.FloorToInt(p.x / cell), Mathf.FloorToInt(p.y / cell));
                if (!cells.TryGetValue(k, out var l)) cells[k] = l = new System.Collections.Generic.List<Vector3>();
                l.Add(p);
            }
            var seen = new System.Collections.Generic.HashSet<(int, int)>();
            var outL = new System.Collections.Generic.List<(Vector3, float, float, float)>();
            foreach (var start in cells.Keys)
            {
                if (seen.Contains(start)) continue;
                var stack = new System.Collections.Generic.Stack<(int, int)>(); stack.Push(start); seen.Add(start);
                Vector3 sum = Vector3.zero; int n = 0; float x0 = float.MaxValue, x1 = float.MinValue, y0 = float.MaxValue, y1 = float.MinValue, zm = float.MinValue;
                while (stack.Count > 0)
                {
                    var k = stack.Pop();
                    foreach (var p in cells[k]) { sum += p; n++; x0 = Mathf.Min(x0, p.x); x1 = Mathf.Max(x1, p.x); y0 = Mathf.Min(y0, p.y); y1 = Mathf.Max(y1, p.y); zm = Mathf.Max(zm, p.z); }
                    for (int dx = -1; dx <= 1; dx++) for (int dy = -1; dy <= 1; dy++)
                    {
                        var nk = (k.Item1 + dx, k.Item2 + dy);
                        if (cells.ContainsKey(nk) && seen.Add(nk)) stack.Push(nk);
                    }
                }
                if (n >= 3) outL.Add((sum / n, x1 - x0, y1 - y0, zm));
            }
            return outL;
        }

        // 텍스처 픽셀 (읽기 불가 텍스처라 GPU로 512칸에 옮겨 읽음, 텍스처마다 한 번)
        static readonly System.Collections.Generic.Dictionary<int, Color32[]> texCache = new();
        const int TexN = 512;
        static Color32[] ReadTex(Texture t)
        {
            if (!t) return null;
            if (texCache.TryGetValue(t.GetInstanceID(), out var c)) return c;
            var rt = RenderTexture.GetTemporary(TexN, TexN, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var prev = RenderTexture.active;
            Graphics.Blit(t, rt);
            RenderTexture.active = rt;
            var tex = new Texture2D(TexN, TexN, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, TexN, TexN), 0, 0); tex.Apply();
            RenderTexture.active = prev; RenderTexture.ReleaseTemporary(rt);
            c = tex.GetPixels32(); Destroy(tex);
            return texCache[t.GetInstanceID()] = c;
        }

        /// <summary>
        /// 실제 얼굴에서 눈 찾기 (2026-11-22): 스킨 메시를 구워 머리 뼈 둘레 정점마다 텍스처 색을 봄 → 눈빛 색(4장 빨강 · 5장 자홍)이고 얼굴 앞면에 있는 점들 중
        /// 가장 위 무리(목도리 · 망토 같은 아래쪽 빨강은 버림)를 좌우로 나눠 그 가운데 = 눈. 칠한 눈이 없으면 머리 뼈 높이의 얼굴 앞면에 키 비례 간격으로
        /// </summary>
        bool FaceFromMeshBake(Transform head, Vector3[] at, out float width)
        {
            width = 0.14f;
            Vector3 rt = transform.right, up = transform.up, fw = transform.forward;
            var pts = new System.Collections.Generic.List<Vector3>(); var eye = new System.Collections.Generic.List<bool>();
            Color.RGBToHSV(tint ?? Ch4Red, out float hue, out _, out _);
            float minY = float.MaxValue, maxY = float.MinValue;
            foreach (var sm in GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (!sm.sharedMesh) continue;
                var baked = new Mesh();
                try { sm.BakeMesh(baked, true); } catch { continue; }
                var v = baked.vertices; var uv = baked.uv;
                var mat = sm.sharedMaterial;
                var pix = mat ? ReadTex(mat.HasProperty("_BaseMap") ? mat.GetTexture("_BaseMap") : mat.mainTexture) : null;
                var m = sm.transform.localToWorldMatrix;
                for (int i = 0; i < v.Length; i++)
                {
                    var wp = m.MultiplyPoint3x4(v[i]);
                    minY = Mathf.Min(minY, wp.y); maxY = Mathf.Max(maxY, wp.y);
                    var p = wp - head.position;
                    pts.Add(new Vector3(Vector3.Dot(p, rt), Vector3.Dot(p, up), Vector3.Dot(p, fw)));
                    bool e = false;
                    if (pix != null && i < uv.Length)
                    {
                        float u = Mathf.Repeat(uv[i].x, 1f), w = Mathf.Repeat(uv[i].y, 1f);
                        var c = (Color)pix[Mathf.Min(TexN - 1, (int)(w * TexN)) * TexN + Mathf.Min(TexN - 1, (int)(u * TexN))];
                        Color.RGBToHSV(c, out float h, out float s, out float val);
                        float dh = Mathf.Abs(h - hue); dh = Mathf.Min(dh, 1f - dh);
                        e = s > 0.55f && val > 0.4f && dh < 0.09f;   // 빨강 ~ 분홍 눈까지 (거울 고양이)
                    }
                    eye.Add(e);
                }
                Destroy(baked);
            }
            float H = maxY - minY;
            if (pts.Count < 30 || H < 0.3f) return false;                       // 아직 안 굽혀졌거나 이상한 메시: 버림 (저장 안 함)
            // 머리 둘레 (머리 뼈 기준 위아래 · 좌우 키의 12%) 에서 얼굴 앞면 깊이
            float front = float.MinValue;
            for (int i = 0; i < pts.Count; i++) { var p = pts[i]; if (Mathf.Abs(p.x) < H * 0.05f && Mathf.Abs(p.y) < H * 0.1f && p.z < H * 0.15f) front = Mathf.Max(front, p.z); }
            if (front == float.MinValue) return false;
            var cand = new System.Collections.Generic.List<Vector3>();
            for (int i = 0; i < pts.Count; i++)
            {
                var p = pts[i];
                if (eye[i] && Mathf.Abs(p.x) < H * 0.18f && p.y > -H * 0.14f && p.y < H * 0.16f && p.z > front - H * 0.25f && p.z > -H * 0.1f) cand.Add(p);   // 두건 · 철창 속 눈은 얼굴 앞면보다 깊이 들어가 있음 → 머리 앞쪽 절반이면 됨
            }
            float gap0 = H * 0.035f;
            // 후보를 덩어리로 묶음 (격자 칸 이웃끼리) → 눈 = 같은 높이 · 얼굴 가운데를 사이에 두고 마주 보는 작은 덩어리 한 쌍.
            // 목도리 · 망토 · 투구 장식 빨강은 넓거나 짝이 없어서 빠짐. 짝이 없으면 가운데의 작은 한 덩어리(외눈 · 투구 눈구멍 띠)를 좌우로 나눔
            var blobs = Blobs(cand, H * 0.012f);
            (Vector3 c, float w, float h, float zMax) best1 = default, best2 = default; float bestScore = float.MaxValue; bool pair = false;
            for (int a = 0; a < blobs.Count; a++)
                for (int b = 0; b < blobs.Count; b++)
                {
                    var A = blobs[a]; var B = blobs[b];
                    if (A.c.x >= B.c.x || !EyeLike(A.w, A.h, H) || !EyeLike(B.w, B.h, H)) continue;
                    float dy = Mathf.Abs(A.c.y - B.c.y), sep = B.c.x - A.c.x, sym = Mathf.Abs(A.c.x + B.c.x) * 0.5f;
                    if (dy > H * 0.025f || sep < H * 0.02f || sep > H * 0.24f || sym > H * 0.05f) continue;   // 고글 · 큰 눈은 간격도 넓음
                    float score = dy * 2f + sym - (A.c.y + B.c.y) * 0.3f;          // 높은 쌍 우선 (눈은 목도리 위)
                    if (score < bestScore) { bestScore = score; best1 = A; best2 = B; pair = true; }
                }
            // 가운데 외눈(마녀 · 사수) · 투구 눈구멍 띠: 짝이 없거나, 짝보다 확실히 높으면 그쪽 (짝은 옷깃 장식일 수 있음)
            (Vector3 c, float w, float h, float zMax) single = default; bool hasSingle = false;
            foreach (var A in blobs)
                if (A.w < H * 0.12f && A.h < H * 0.1f && A.w > H * 0.015f && A.c.y < H * 0.1f && Mathf.Abs(A.c.x) < H * 0.04f && (!hasSingle || A.c.y > single.c.y)) { single = A; hasSingle = true; }   // 뿔 끝 빨강 같은 높은 장식 제외
            if (hasSingle && (!pair || single.c.y > (best1.c.y + best2.c.y) * 0.5f + H * 0.03f)) { pair = false; best1 = single; bestScore = 0; }
            if (pair || bestScore == 0)
            {
                Vector3 L, R;
                if (pair) { L = best1.c; R = best2.c; L.z = best1.zMax; R.z = best2.zMax; }
                else { float g = Mathf.Max(best1.w * 0.3f, H * 0.02f); L = best1.c - Vector3.right * g; R = best1.c + Vector3.right * g; L.z = R.z = best1.zMax; }
                L.z += H * 0.004f; R.z += H * 0.004f;
                at[0] = head.position + rt * L.x + up * L.y + fw * L.z;
                at[1] = head.position + rt * R.x + up * R.y + fw * R.z;
            }
            else
            {
                // 칠한 눈이 없음: 머리 뼈 높이의 얼굴 앞면 (이 모델들은 머리 뼈가 대개 눈 높이)
                float ez = float.MinValue;
                foreach (var p in pts) if (Mathf.Abs(p.x) < H * 0.03f && Mathf.Abs(p.y) < H * 0.015f && p.z < H * 0.15f) ez = Mathf.Max(ez, p.z);
                if (ez == float.MinValue) ez = front;
                for (int i = 0; i < 2; i++) at[i] = head.position + rt * gap0 * (i == 0 ? -1 : 1) + fw * (ez + H * 0.004f);
            }
            width = Mathf.Clamp(H * 0.016f, 0.04f, 0.3f);
            return true;
        }

        static TrailRenderer MakeTrail(GameObject go, Material m, float width, Gradient g)
        {
            var tr = go.AddComponent<TrailRenderer>();
            tr.sharedMaterial = m;
            tr.textureMode = LineTextureMode.Stretch;
            tr.time = 0.001f;
            tr.minVertexDistance = 0.05f;
            tr.widthMultiplier = width;
            tr.widthCurve = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 0.55f));
            tr.numCapVertices = 3;
            tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            tr.receiveShadows = false;
            tr.colorGradient = g;
            tr.emitting = true;                     // 끄지 않음: 끄면 다음 점에서 폭이 0으로 뚝 끊김 (Unity 동작) → 대신 time을 줄여 꼬리가 눈으로 빨려 들어감
            return tr;
        }

        /// <summary>강제로 잠깐 꼬리를 켬 (회전 베기처럼 제자리에서 빠른 동작)</summary>
        public void Hot(float dur) => hotUntil = Mathf.Max(hotUntil, Time.time + dur);

        Transform faceHead; int placeAt = -1;
        /// <summary>스킨이 돈 뒤 실제 얼굴 메시로 눈 두 점 · 폭을 다시 잡음 (한 번)</summary>
        void PlaceOnFace()
        {
            placeAt = -1;
            if (!faceHead || eyes == null) return;
            var at = new Vector3[2];
            if (!FaceFromMesh(faceHead, at, out float width)) return;
            for (int i = 0; i < 2; i++) if (eyes[i]) { eyes[i].position = at[i]; prevEye[i] = at[i]; }
            for (int i = 0; i < trails.Length; i++) if (trails[i]) { trails[i].widthMultiplier = i < 2 ? width : width * 3.6f; trails[i].Clear(); }
        }

        void LateUpdate()
        {
            if (trails == null) return;
            if (placeAt >= 0 && Time.frameCount >= placeAt) PlaceOnFace();
            var p = transform.position;
            float moved = Effects.Flat(p - last).magnitude;
            float spd = moved / Mathf.Max(1e-4f, Time.deltaTime);
            last = p;
            bool on = spd > FastSpeed || moved > 1.2f || Time.time < hotUntil;
            if (on)
            {
                if (curTime < 0.01f)
                    for (int i = 0; i < trails.Length; i++)
                        if (trails[i]) { trails[i].Clear(); trails[i].AddPosition(prevEye[i % 2]); }   // 새로 시작: 지난 프레임 눈 자리부터 (점멸이면 옛 자리 → 새 자리가 곧은 레이저 줄)
                curTime = TrailTime;
            }
            else
            {
                // 궤적 종결: 꼬리 길이(시간)를 부드럽게 줄여 꼬리가 눈 쪽으로 따라 들어감, 다 들어가면 꼬리 끝에 작은 불똥
                if (wasOn && trails[0] && trails[0].positionCount > 1)
                    FxFlipbook.Spawn("bk_c4spark", trails[0].GetPosition(0), Vector3.zero, 0.55f, 0.22f, false);
                curTime = Mathf.MoveTowards(curTime, 0f, TrailTime / RetractTime * Time.deltaTime);
            }
            wasOn = on;
            foreach (var t in trails) if (t) t.time = Mathf.Max(0.001f, curTime);
            for (int i = 0; i < 2; i++) if (eyes[i]) prevEye[i] = eyes[i].position;
        }
    }

    public class FxLater : MonoBehaviour
    {
        float at; System.Action act;
        public void Init(float sec, System.Action a) { at = Time.time + sec; act = a; }
        void Update() { if (Time.time < at) return; act?.Invoke(); Destroy(gameObject); }
    }
}
