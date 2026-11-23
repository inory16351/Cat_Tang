// 오프닝 몽타주 재생기 (2026-11-23 사용자: "디졸브랑 깨진 유리 같은 연출로 그림 조각 나눠서 무작위로 섞는 방식으로 엇갈린 시간과 꿈 같은 연출",
//   "배경이랑 캐릭터 / 오브젝트 따로 … 애니메이션", "뎁스도 좀 넣어, 연출에 따라 심도도 조절",
//   "시간선 더 무작위로 섞고 한 컷에 이미지 여러개를 조각 내 잘라붙여서 … 컷 전환이나 카메라 무빙도 훨씬 더 긴박하게 … 유리컵 … 떨어트리는거",
//   "한컷에 여러 장면 디졸브 … 예지몽이나 과거의 회상장면을 어떻게 연출하는지를 찾아보고 참고")
// 참고한 회상 · 꿈 연출:
// - 겹쳐 찍기(이중 노출): 한 컷 안에서 다른 시간의 장면이 반투명하게 떠올랐다 가라앉음 → 레이어 fade 키
// - 곤 사토시(파프리카 · 퍼펙트 블루): 모양을 맞춘 매치 컷으로 장면이 이어짐 → Zoom 전환 + 같은 자리 원형(시계판 · 심전도 · 시계탑)
// - 레퀴엠 포 어 드림 '힙합 몽타주': 아주 짧은 동작 조각을 같은 소리와 함께 되풀이 → 박자(cue) · 반복 컷
// - 이터널 선샤인: 기억이 지워질 때 대상이 사라지고 빛이 꺼짐 · 장면이 무너짐 → 레이어 fade-out · 어두워짐
// - 꿈 표식: 가장자리 흐림 · 채도 빠짐 · 디졸브 → 셰이더 _Edge · _Sat
// - 컷 = 레이어 여러 장 (배경 판 · 대상 오려 낸 그림 · 부품). 레이어마다 시차(depth) · 흐림(초점 이동) · 자체 움직임(떨어짐 · 되감김)
// - 콜라주 컷: 화면을 유리 조각처럼 나눠 조각마다 다른 기억 그림이 끼워지고 깜빡이며 바뀜
// - 전환: 끊기 · 디졸브 · 깨진 유리 · 조각 섞기 · 하얀 번쩍 · 휙(옆으로 쓸림) · 줌 관통(매치 컷)
// 그림 = Resources/Story/Opening/<이름>.png (크롬 ChatGPT, 하데스풍). 없으면 장면 설명 자리 표시
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CatTang
{
    public enum MTrans { Cut, Dissolve, Shatter, Mix, Flash, Whip, Zoom, Paint }   // Paint = 물감이 뒤덮었다 걷히며 넘어감

    public class MLayer
    {
        public string img;
        public float depth = 1f;                  // 카메라 이동 · 줌을 얼마나 따라가나 (배경 0.4 · 대상 1.2 …)
        public float blur0, blur1;                // 흐림 시작 → 끝 (0 = 또렷, 0.012 = 꽤 흐림)
        public Vector2 p0, p1;                    // 자체 이동 (px)
        public float z0 = 1, z1 = 1, r0, r1;      // 자체 줌 · 회전
        public float tick;                        // 째깍: 초당 몇 번 끊어서 회전 (시곗바늘)
        public float wave;                        // 왕복: 초당 몇 번 (r0 · p0 = 가운데, r1 · p1 = 한쪽 끝) — 진자 · 59초에서 떨리는 초침
        public float waveDamp;                    // 왕복이 컷 끝으로 갈수록 줄어드는 정도 (0 = 그대로, 1 = 끝에서 멈춤)
        public float paint;                       // 물감 섞임 (0~1): 오려 낸 부품 가장자리가 번지고 안료가 고이고 종이 결이 비침 (수채화 꿈)
        public float reveal0 = 2, reveal1 = 2;    // 그려짐: 물감 경계 위치 (그림 기준 0~1, 방향 revealDir) 시작 → 끝. 2 = 다 칠해짐 (카메라 이동과 같은 진행)
        public Vector2 revealDir = new(1, 0);
        public float revealSoft = 0.12f;
        public Vector2 pivot = new(0.5f, 0.5f);
        public float height;                      // 0 = 화면 덮기, 아니면 높이 px (부품)
        public Vector2 at;                        // 부품 자리 (화면 가운데 기준 px)
        public float alpha = 1f;
        public Vector2[] fade;                    // 겹쳐 찍기: (진행 0~1, 불투명도) 키. 없으면 alpha 고정
        public int motion;                        // 0 = 부드럽게 · 1 = 떨어짐(가속) · 2 = 떨어졌다 멈칫 → 거꾸로 감겨 되돌아감(시간 역행)
        public float edge;                        // 가장자리 흐림 (꿈 · 회상 표식)
        public float sat = 1f;                    // 채도 (0 = 흑백, 회상)
        public Color tint = Color.white;
        public float breakAt = -1;                // 오브젝트가 이 진행(0~1)에서 깨져 조각이 바닥으로 흩어짐 (진행으로 계산 → 거꾸로 재생하면 조각이 모여 붙음)
        public int seed = 7;
        public bool crumble;                      // 깨짐 방식: false = 바닥에 부딪혀 미끄러짐(물컵) · true = 금이 가며 무너져 아래로 쏟아짐(시계탑)
        public int breakCols = 4, breakRows = 5;
        public bool drops = true;                 // 물방울 조각
    }

    public class MShot
    {
        public string desc;
        public float dur;
        public MLayer[] layers;
        public Vector2 pan0, pan1;
        public float zoom0 = 1, zoom1 = 1, rot0, rot1, shake;
        public string sfx;
        public MTrans tin = MTrans.Cut;
        public float tinDur = 0.5f;
        public Color tint = new(0.08f, 0.07f, 0.1f);
        public int ease;                          // 0 = 부드럽게 · 1 = 크래시(처음에 확 → 천천히) · 2 = 일정
        public string[] collage;                  // 콜라주: 조각마다 다른 기억 그림
        public int cols = 6, rows = 4;
        public float spread = 1f;                 // 콜라주 조각이 벌어짐(+) / 흩어졌다 모임(-)
        public float swap = 0.5f;                 // 조각이 다른 그림으로 바뀌는 확률 (깜빡임마다)
        public (float u, string sfx, float shake, float flash)[] cues;   // 컷 안의 박자: 소리 · 충격 흔들림 · 하얀 번쩍
        public float dolly = 1f;                  // 달리 줌(현기증): 배경(depth<1)만 이 배율까지 커지고(>1) / 작아지고(<1) 대상은 그대로
        public float hand;                        // 핸드헬드: 정신없이 흔들리는 카메라 (px, 회전 = hand × 0.12°)
        public bool reverse;                      // 거꾸로 재생 (시간 역행: 깨진 조각이 모이고 컵이 책상으로 되돌아감)
    }

    public class StoryMontage : MonoBehaviour
    {
        RectTransform root, cam;
        Image back;
        Text placeholder;
        Material blurBase;
        readonly List<(Image im, MLayer l, Material m)> live = new();
        float kick;                               // 박자 충격 흔들림 (빠르게 줄어듦)

        public static StoryMontage Create(Transform parent)
        {
            var rt = UIKit.Stretch("Montage", parent);
            var m = rt.gameObject.AddComponent<StoryMontage>();
            m.root = rt;
            m.back = rt.gameObject.AddComponent<Image>(); m.back.color = Color.black; m.back.raycastTarget = false;
            m.cam = UIKit.MakeRect("Cam", rt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1920, 1080));
            m.placeholder = UIKit.Label("Placeholder", rt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1400, 300), 34, TextAnchor.MiddleCenter, false, new Color(1, 1, 1, 0.35f));
            var sh = Shader.Find("CatTang/UIBlur");
            m.blurBase = sh ? new Material(sh) : null;
            // 화면 전체 비네트: 배경 · 따로 뽑은 오브젝트(물컵 등)가 같은 가장자리 어둠을 받아 한 그림처럼 섞임
            var vig = UIKit.Stretch("Vignette", rt).gameObject.AddComponent<RawImage>();
            vig.texture = VignetteTex(); vig.raycastTarget = false;
            return m;
        }

        static Texture2D vigTex;
        static Texture2D VignetteTex()
        {
            if (vigTex) return vigTex;
            const int W = 320, H = 180;
            vigTex = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[W * H];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float dx = (x + 0.5f) / W - 0.5f, dy = ((y + 0.5f) / H - 0.5f) * 0.85f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) / 0.6f;
                    float a = Mathf.SmoothStep(0.45f, 1.05f, d) * 0.72f;
                    px[y * W + x] = new Color32(4, 5, 10, (byte)(a * 255));
                }
            vigTex.SetPixels32(px); vigTex.Apply(false, true);
            return vigTex;
        }

        static readonly Dictionary<string, Sprite> cache = new();
        static Sprite Load(string folder, string n)
        {
            if (string.IsNullOrEmpty(n)) return null;
            if (cache.TryGetValue(folder + n, out var s)) return s;
            var t = Resources.Load<Texture2D>(folder + n);
            s = t ? Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f)) : null;
            cache[folder + n] = s;
            return s;
        }

        void Build(MShot s, string folder)
        {
            foreach (Transform c in cam) Destroy(c.gameObject);
            foreach (var x in live) if (x.m) Destroy(x.m);
            live.Clear();
            breaks.Clear();
            back.color = s.tint;
            BuildCollage(s, folder);                  // 콜라주가 맨 뒤, 그 위에 레이어(오려 낸 대상 등)
            bool any = shards.Count > 0;
            if (s.layers != null)
                foreach (var l in s.layers)
                {
                    var sp = Load(folder, l.img);
                    if (!sp) continue;
                    any = true;
                    float aspect = sp.rect.width / sp.rect.height;
                    Vector2 size;
                    if (l.height <= 0) { size = aspect > 1920f / 1080f ? new Vector2(1080 * aspect, 1080) : new Vector2(1920, 1920 / aspect); size *= 1.12f; }   // 화면 덮기 + 이동 여유
                    else size = new Vector2(l.height * aspect, l.height);
                    var rt = UIKit.MakeRect(l.img, cam, new Vector2(0.5f, 0.5f), l.at, size, l.pivot);
                    var im = rt.gameObject.AddComponent<Image>(); im.sprite = sp; im.raycastTarget = false;
                    var mat = blurBase ? new Material(blurBase) : null;
                    if (mat) im.material = mat;
                    live.Add((im, l, mat));
                    if (l.breakAt >= 0) BuildBreak(im, sp.texture, l);
                }
            placeholder.text = any ? "" : s.desc;
        }

        /// <summary>레이어 자체 움직임 곡선. 2 = 떨어짐(0~0.42) → 바닥에서 떨며 멈칫(~0.6) → 거꾸로 빨려 올라감(~1)</summary>
        static float Motion(int m, float u, float e)
        {
            if (m == 1) return u * u;
            if (m == 3) return u < 0.45f ? 0.04f * (u / 0.45f) : 0.04f + 0.96f * Mathf.Pow((u - 0.45f) / 0.55f, 2);   // 버티다(살짝 밀림) → 떨어짐
            if (m == 4) return u < 0.3f ? 0 : Mathf.Pow((u - 0.3f) / 0.7f, 2);                                         // 멈춰 있다 → 떨어짐
            if (m == 2)
            {
                if (u < 0.42f) { float k = u / 0.42f; return k * k; }
                if (u < 0.6f) return 1f;
                float r = (u - 0.6f) / 0.4f; return 1f - (1 - Mathf.Pow(1 - r, 3));
            }
            return e;
        }

        static float Ease(int mode, float u) => mode == 1 ? 1 - Mathf.Pow(1 - u, 4) : mode == 2 ? u : u * u * (3 - 2 * u);

        static float Keys(Vector2[] k, float u, float def)
        {
            if (k == null || k.Length == 0) return def;
            if (u <= k[0].x) return k[0].y;
            for (int i = 1; i < k.Length; i++)
                if (u <= k[i].x) return Mathf.Lerp(k[i - 1].y, k[i].y, Mathf.SmoothStep(0, 1, (u - k[i - 1].x) / Mathf.Max(0.0001f, k[i].x - k[i - 1].x)));
            return k[^1].y;
        }

        // ---------- 오브젝트 깨짐: 그림 자체를 조각내 바닥으로 미끄러뜨림 ----------
        class Piece { public ShardGraphic g; public Vector2 slide; public float hop, spin; }
        readonly Dictionary<Image, List<Piece>> breaks = new();

        void BuildBreak(Image im, Texture tex, MLayer l)
        {
            var rng = new System.Random(l.seed);
            float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
            var list = new List<Piece>();
            var st = Random.state; Random.InitState(l.seed);
            var polys = Cracks(l.breakCols, l.breakRows, 0.9f);
            Random.state = st;
            foreach (var poly in polys)
            {
                var c = (poly[0] + poly[1] + poly[2]) / 3f;
                var g = NewShard(im.rectTransform, tex, poly);
                if (l.crumble)
                {
                    // 무너짐: 바깥으로 살짝 터지고 위쪽 조각일수록 늦게 · 크게 떨어짐
                    list.Add(new Piece { g = g, slide = new Vector2((c.x - 0.5f) * R(200f, 700f), R(-60f, 160f)), hop = -(1.1f - c.y) * R(0.02f, 0.25f), spin = R(-120f, 120f) });
                    continue;
                }
                var dir = new Vector2(c.x - 0.5f + R(-0.3f, 0.3f), R(-0.35f, 0.25f));
                list.Add(new Piece { g = g, slide = dir.normalized * R(150f, 900f), hop = R(150f, 900f), spin = R(-540f, 540f) });
            }
            // 물방울: 작고 하얀 조각
            for (int i = 0; i < (l.drops && !l.crumble ? 18 : 0); i++)
            {
                var o = new Vector2(R(0.35f, 0.65f), R(0.2f, 0.5f)); float s = R(0.015f, 0.04f);
                var g = NewShard(im.rectTransform, null, new[] { o, o + new Vector2(s, s * 0.4f), o + new Vector2(s * 0.3f, s) });
                g.color = new Color(0.75f, 0.88f, 1f, 0.85f);
                list.Add(new Piece { g = g, slide = new Vector2(R(-1f, 1f), R(-0.4f, 0.3f)) * R(300f, 1300f), hop = R(300f, 1300f), spin = R(-200f, 200f) });
            }
            foreach (var p in list) p.g.gameObject.SetActive(false);
            breaks[im] = list;
        }

        static ShardGraphic NewShard(RectTransform parent, Texture tex, Vector2[] poly)
        {
            var c = Vector2.zero; foreach (var v in poly) c += v; c /= poly.Length;
            var rt = UIKit.Stretch("Piece", parent); rt.pivot = c;
            rt.gameObject.AddComponent<CanvasRenderer>();
            var g = rt.gameObject.AddComponent<ShardGraphic>();
            g.tex = tex; g.poly = poly; g.raycastTarget = false;
            return g;
        }

        void PoseBreak(Image im, MLayer l, float t, float breakT)
        {
            if (!breaks.TryGetValue(im, out var list)) return;
            float tb = t - breakT;
            bool broken = tb >= 0;
            var c = im.color; if (broken) { c.a = 0; im.color = c; }
            var inv = Quaternion.Inverse(im.rectTransform.localRotation);
            float sc = Mathf.Max(0.01f, im.rectTransform.localScale.x);
            foreach (var p in list)
            {
                if (p.g.gameObject.activeSelf != broken) p.g.gameObject.SetActive(broken);
                if (!broken) continue;
                if (l.crumble)
                {
                    // hop(음수) = 이 조각이 떨어지기 시작하는 지연(초). 그 전엔 금 간 채 떨림
                    float td = tb + p.hop;
                    var prt = p.g.rectTransform;
                    if (td < 0) { prt.anchoredPosition = Random.insideUnitCircle * 3f / sc; prt.localRotation = Quaternion.identity; continue; }
                    var wf = p.slide * td + new Vector2(0, -0.5f * 3200f * td * td);
                    prt.anchoredPosition = (Vector2)(inv * (Vector3)wf) / sc;
                    prt.localRotation = Quaternion.Euler(0, 0, p.spin * td);
                    continue;
                }
                float k = 1 - Mathf.Exp(-3.5f * tb);
                float hop = Mathf.Max(0, p.hop * tb - 0.5f * 4200f * tb * tb);       // 튀어 올랐다 바닥에 내려앉음
                var w = p.slide * k * 0.6f + new Vector2(0, hop);
                var rt = p.g.rectTransform;
                rt.anchoredPosition = (Vector2)(inv * (Vector3)w) / sc;
                rt.localRotation = Quaternion.Euler(0, 0, p.spin * k);
            }
        }

        void Pose(MShot s, float u)
        {
            if (s.reverse) u = 1 - u;
            float e = Ease(s.ease, u);
            var pan = Vector2.Lerp(s.pan0, s.pan1, e);
            float zoom = Mathf.Lerp(s.zoom0, s.zoom1, e);
            kick = Mathf.MoveTowards(kick, 0, Time.unscaledDeltaTime * 70f);
            float sh = s.shake + kick;
            var j = sh > 0 ? Random.insideUnitCircle * sh : Vector2.zero;
            float hr = 0;
            if (s.hand > 0)
            {
                // 핸드헬드: 느린 흔들림 + 빠른 떨림을 겹친 펄린 노이즈 (카메라맨이 뛰면서 찍는 느낌)
                float T = Time.unscaledTime;
                j += new Vector2(Mathf.PerlinNoise(T * 3.1f, 0.3f) - 0.5f + (Mathf.PerlinNoise(T * 11f, 5f) - 0.5f) * 0.35f,
                                 Mathf.PerlinNoise(1.7f, T * 2.7f) - 0.5f + (Mathf.PerlinNoise(9f, T * 13f) - 0.5f) * 0.35f) * 2f * s.hand;
                hr = (Mathf.PerlinNoise(T * 2.3f, 7.7f) - 0.5f) * 2f * s.hand * 0.12f;
            }
            cam.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(s.rot0, s.rot1, e) + hr + (kick > 0 ? Random.Range(-0.08f, 0.08f) * kick : 0));
            cam.anchoredPosition = Vector2.zero;
            float dz = Mathf.Lerp(1f, s.dolly, e);
            foreach (var (im, l, m) in live)
            {
                if (!im) continue;
                var rt = im.rectTransform;
                float le = l.wave > 0 ? Mathf.Sin(u * s.dur * l.wave * Mathf.PI * 2f) * (1 - l.waveDamp * u) : Motion(l.motion, u, e);
                // 시차: 카메라 이동 · 줌을 depth만큼 (가까운 레이어일수록 많이 움직임)
                float lz = Mathf.Lerp(l.z0, l.z1, e) * (1 + (zoom - 1) * l.depth);
                if (l.depth < 1f) lz *= Mathf.Pow(dz, Mathf.Clamp01((1f - l.depth) / 0.6f));   // 달리 줌: 배경만 늘어나거나 줄어듦
                rt.anchoredPosition = l.at * lz + (pan + j) * l.depth + Vector2.LerpUnclamped(l.p0, l.p1, le);   // 부품 자리도 같이 커짐 (시곗바늘이 시계판 축에 붙어 있게)
                if (l.motion == 2 && u >= 0.42f && u < 0.6f) rt.anchoredPosition += Random.insideUnitCircle * 4f;   // 바닥에 닿아 멈칫 떨림
                rt.localScale = new Vector3(lz, lz, 1);
                float r = l.tick > 0 ? Mathf.Lerp(l.r0, l.r1, Mathf.Floor(u * s.dur * l.tick) / Mathf.Max(1, s.dur * l.tick)) : Mathf.LerpUnclamped(l.r0, l.r1, le);
                rt.localRotation = Quaternion.Euler(0, 0, r);
                if (m)
                {
                    m.SetFloat("_Blur", Mathf.Lerp(l.blur0, l.blur1, e));
                    m.SetFloat("_Edge", l.edge);
                    m.SetFloat("_Sat", l.sat);
                    m.SetFloat("_Paint", l.paint);
                    m.SetFloat("_Reveal", Mathf.Lerp(l.reveal0, l.reveal1, e));
                    m.SetVector("_RevealDir", l.revealDir);
                    m.SetFloat("_RevealSoft", l.revealSoft);
                }
                var c = l.tint; c.a *= Keys(l.fade, u, l.alpha);
                im.color = c;
                if (l.breakAt >= 0) PoseBreak(im, l, u * s.dur, l.breakAt * s.dur);
            }
            placeholder.rectTransform.anchoredPosition = pan * 0.3f;
            PoseCollage(s, u, e, pan + j, zoom);
        }

        // ---------- 콜라주: 한 화면의 조각마다 다른 기억 ----------
        // 조각 = 테두리 번짐 + 살짝 흐림 + 잔상(바로 전 그림이 뒤에서 늦게 따라오며 옅게 남음) → 조각끼리 이어 붙은 듯 섞임
        class CShard { public ShardGraphic g, ghost; public Texture[] pool; public Vector2 drift, ghostPos; public float spin, nextSwap, depth, ghostA; public Vector2 uvOff; }
        readonly List<CShard> shards = new();
        Material collageMat;

        void BuildCollage(MShot s, string folder)
        {
            shards.Clear();
            if (s.collage == null || s.collage.Length == 0) return;
            var pool = new List<Texture>();
            foreach (var n in s.collage) { var sp = Load(folder, n); if (sp) pool.Add(sp.texture); }
            if (pool.Count == 0) return;
            var arr = pool.ToArray();
            if (!collageMat && blurBase) { collageMat = new Material(blurBase); collageMat.SetFloat("_Blur", 0.0025f); }
            int i = 0;
            var polys = Cracks(s.cols, s.rows, 0.85f);
            // 바닥: 첫 그림을 크게 흐려 깔아 조각 사이 번진 틈을 메움 (검은 줄 대신 같은 기억의 색이 비침)
            var bed = UIKit.Stretch("CBed", cam).gameObject.AddComponent<RawImage>();
            bed.texture = arr[Random.Range(0, arr.Length)]; bed.raycastTarget = false; bed.color = new Color(1, 1, 1, 0.75f);
            if (blurBase) { var bm = new Material(blurBase); bm.SetFloat("_Blur", 0.02f); bed.material = bm; Destroy(bm, s.dur + 2f); }
            // 잔상 조각들을 먼저 만들어 뒤에 깔기
            var ghosts = new List<ShardGraphic>();
            foreach (var poly in polys)
            {
                var gr = UIKit.Stretch("CG", cam); gr.pivot = (poly[0] + poly[1] + poly[2]) / 3f;
                gr.gameObject.AddComponent<CanvasRenderer>();
                var gg = gr.gameObject.AddComponent<ShardGraphic>();
                gg.poly = poly; gg.raycastTarget = false; gg.feather = 0.35f; gg.color = new Color(1, 1, 1, 0);
                if (collageMat) gg.material = collageMat;
                ghosts.Add(gg);
            }
            int gi = 0;
            foreach (var poly in polys)
            {
                var c = (poly[0] + poly[1] + poly[2]) / 3f;
                var rt = UIKit.Stretch("C", cam); rt.pivot = c;
                rt.gameObject.AddComponent<CanvasRenderer>();
                var g = rt.gameObject.AddComponent<ShardGraphic>();
                g.poly = poly; g.raycastTarget = false;
                g.feather = 0.16f;
                if (collageMat) g.material = collageMat;
                g.tex = arr[(i++ + Random.Range(0, 2)) % arr.Length];
                g.uvZoom = Random.Range(1.4f, 1.8f);                 // 조각 안 그림이 가장자리를 넘지 않게 (확대 + 어긋남 ≤ 0.5)
                g.uvOff = new Vector2(Random.Range(-0.08f, 0.08f), Random.Range(-0.07f, 0.07f));
                var gh = ghosts[gi++]; gh.tex = g.tex; gh.uvZoom = g.uvZoom * 0.94f; gh.uvOff = g.uvOff;
                var dir = (c - new Vector2(0.5f, 0.5f));
                shards.Add(new CShard
                {
                    g = g, ghost = gh, pool = arr, uvOff = g.uvOff,
                    drift = dir * Random.Range(60f, 220f) + Random.insideUnitCircle * 30f,
                    spin = Random.Range(-10f, 10f), nextSwap = Random.Range(0.08f, 0.4f), depth = Random.Range(0.5f, 1.7f)
                });
            }
        }

        void PoseCollage(MShot s, float u, float e, Vector2 pan, float zoom)
        {
            if (shards.Count == 0) return;
            float t = u * s.dur;
            // spread > 0: 붙어 있다가 벌어짐 · < 0: 흩어진 조각이 모여 한 장면이 됨
            float k = s.spread >= 0 ? e * s.spread : (1 - e) * -s.spread;
            foreach (var c in shards)
            {
                if (!c.g) continue;
                var rt = c.g.rectTransform;
                rt.anchoredPosition = c.drift * k + pan * c.depth;                       // 조각마다 다른 깊이로 떠다님
                rt.localRotation = Quaternion.Euler(0, 0, c.spin * k);
                float z = 1 + (zoom - 1) * c.depth;
                rt.localScale = new Vector3(z, z, 1);
                c.g.uvOff = c.uvOff + new Vector2(Mathf.Sin(t * 0.9f + c.depth * 3) * 0.025f, Mathf.Cos(t * 0.7f + c.spin) * 0.02f);
                if (t >= c.nextSwap)
                {
                    c.nextSwap = t + Random.Range(0.1f, 0.4f);
                    if (Random.value < s.swap)
                    {
                        // 바뀌기 전 그림은 잔상으로 넘김 (옅게 남아 늦게 따라오다 사라짐)
                        if (c.ghost) { c.ghost.tex = c.g.tex; c.ghostA = 0.55f; c.ghost.SetMaterialDirty(); }
                        c.g.tex = c.pool[Random.Range(0, c.pool.Length)];   // 다른 시간의 조각으로 바뀜
                    }
                    c.g.color = new Color(1, 1, 1, Random.Range(0.75f, 1f));
                    c.g.SetMaterialDirty();
                }
                c.g.SetVerticesDirty();
                if (c.ghost)
                {
                    // 잔상: 조각 위치를 늦게 따라감 + 조금 크게 + 점점 옅어짐 (최소 0.18은 남겨 조각 사이를 메움)
                    var target = rt.anchoredPosition;
                    c.ghostPos = Vector2.Lerp(c.ghostPos == Vector2.zero ? target : c.ghostPos, target, Time.unscaledDeltaTime * 5f);
                    var grt = c.ghost.rectTransform;
                    grt.anchoredPosition = c.ghostPos;
                    grt.localRotation = rt.localRotation;
                    grt.localScale = rt.localScale * 1.07f;
                    c.ghostA = Mathf.MoveTowards(c.ghostA, 0.18f, Time.unscaledDeltaTime * 1.2f);
                    c.ghost.color = new Color(1, 1, 1, c.ghostA);
                    c.ghost.uvOff = c.g.uvOff * 0.9f;
                    c.ghost.SetVerticesDirty();
                }
            }
        }

        // ---------- 화면 잔상 · 글리치 (2026-11-23 사용자: "잔상 효과나 글리치 조금씩 넣어서 죽음을 앞둔 사람의 무의식을 좀 더 긴박하게") ----------
        // 잔상: 0.08초마다 화면을 GPU로 떠 두고, 직전 화면을 살짝 크게 · 옅게 겹침 → 움직임마다 꼬리가 남고 스스로 되먹임돼 서서히 사라짐
        // 글리치: 센 박자(흔들림 ≥ 14)나 무작위 간격으로 0.1~0.25초 — 가로 띠가 옆으로 찢겨 밀리고 빨강/청록으로 갈라짐
        public float echo = 0.2f, glitch = 1f;   // 꿈(수채화)은 glitch 0
        public float vignette = 1f;              // 화면 비네트 세기 (수채화 꿈은 종이가 밝아 약하게)
        // 물감 글리치 (2026-11-23 사용자: "디졸브 동시랑 함께 화면에 어울리는 글리치도 무작위로"): 수채화 꿈용
        //   띠 · 블록이 밀리되 가장자리가 흐리고(블러 재질) · 아래로 번져 늘어지고(물감이 흐르듯) · 갈라지는 색은 장미빛/보랏빛 안료 · 안료색으로 물듦
        public bool paintGlitch;
        Material glitchBlur;
        RawImage echoImg, splitR, splitC; readonly List<RawImage> strips = new();
        RenderTexture rtA, rtB; bool fxOn, glitchSplit = true; float glitchLeft, glitchNext = 0.6f; int glitchCount = 7;
        Rect FlipRect(Rect r) => SystemInfo.graphicsUVStartsAtTop ? new Rect(r.x, r.y + r.height, r.width, -r.height) : r;

        void EnsureFx()
        {
            if (echoImg) return;
            echoImg = UIKit.Stretch("Echo", root).gameObject.AddComponent<RawImage>(); echoImg.raycastTarget = false;
            splitR = UIKit.Stretch("SplitR", root).gameObject.AddComponent<RawImage>(); splitR.raycastTarget = false;
            splitC = UIKit.Stretch("SplitC", root).gameObject.AddComponent<RawImage>(); splitC.raycastTarget = false;
            for (int i = 0; i < 12; i++) { var s =UIKit.MakeRect("Strip", root, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1920, 40)).gameObject.AddComponent<RawImage>(); s.raycastTarget = false; strips.Add(s); }
            // 비네트는 맨 위로
            var vig = root.Find("Vignette"); if (vig) vig.SetAsLastSibling();
            foreach (var g in new[] { echoImg, splitR, splitC }) g.enabled = false;
            foreach (var s in strips) s.enabled = false;
        }

        IEnumerator Fx()
        {
            EnsureFx();
            int w = Screen.width, h = Screen.height;
            rtA = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB); rtB = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);   // sRGB: 리니어 프로젝트에서 잔상 · 글리치가 회색으로 바래지 않게
            float nextGrab = 0;
            while (fxOn)
            {
                yield return new WaitForEndOfFrame();
                if (!fxOn) break;
                float now = Time.unscaledTime;
                if (now >= nextGrab)
                {
                    nextGrab = now + 0.08f;
                    (rtA, rtB) = (rtB, rtA);                           // rtB = 직전 화면 (잔상용) · rtA = 지금 화면
                    ScreenCapture.CaptureScreenshotIntoRenderTexture(rtA);
                }
                var full = FlipRect(new Rect(0, 0, 1, 1));
                // 잔상
                echoImg.enabled = echo > 0;
                echoImg.texture = rtB; echoImg.uvRect = full;
                echoImg.color = new Color(1, 1, 1, echo);
                echoImg.rectTransform.localScale = Vector3.one * 1.025f;
                // 글리치
                // 무작위 글리치 (2026-11-23 사용자: "좀 더 무작위로 여러군데"): 잦고 짧은 잔떨림(1~3프레임) + 가끔 긴 찢김
                //   종류: 띠(가로 전체) · 블록(화면 일부만 사각으로 어긋남) · 색 갈라짐은 절반 확률. 매번 다른 자리 · 다른 개수
                glitchNext -= Time.unscaledDeltaTime;
                if (glitch > 0 && glitchNext <= 0)
                {
                    bool big = Random.value < 0.35f;
                    glitchLeft = big ? Random.Range(0.12f, 0.26f) : Random.Range(0.03f, 0.08f);
                    glitchSplit = Random.value < (big ? 0.8f : 0.4f);
                    glitchCount = big ? Random.Range(5, strips.Count + 1) : Random.Range(1, 4);
                    glitchNext = Random.Range(0.25f, 1.1f) / glitch;
                }
                bool on = glitchLeft > 0;
                glitchLeft -= Time.unscaledDeltaTime;
                splitR.enabled = splitC.enabled = on && glitchSplit;
                if (paintGlitch && !glitchBlur && blurBase) { glitchBlur = new Material(blurBase); glitchBlur.SetFloat("_Blur", 0.004f); }
                var gm = paintGlitch ? glitchBlur : null;
                if (on && glitchSplit)
                {
                    float o = paintGlitch ? Random.Range(6f, 18f) : Random.Range(5f, 26f);
                    splitR.texture = splitC.texture = rtA; splitR.uvRect = splitC.uvRect = full;
                    splitR.material = splitC.material = gm;
                    if (paintGlitch)
                    {
                        // 안료가 어긋나 번짐: 장미빛은 옆 · 아래로, 보랏빛은 반대쪽 · 위로 (물에 번진 판화처럼)
                        splitR.color = new Color(0.98f, 0.5f, 0.58f, 0.3f); splitR.rectTransform.anchoredPosition = new Vector2(o, -Random.Range(3f, 14f));
                        splitC.color = new Color(0.55f, 0.45f, 0.95f, 0.26f); splitC.rectTransform.anchoredPosition = new Vector2(-o, Random.Range(2f, 9f));
                    }
                    else
                    {
                        splitR.color = new Color(1f, 0.15f, 0.2f, 0.32f); splitR.rectTransform.anchoredPosition = new Vector2(o, Random.Range(-4f, 4f));
                        splitC.color = new Color(0.1f, 0.95f, 1f, 0.28f); splitC.rectTransform.anchoredPosition = new Vector2(-o, Random.Range(-4f, 4f));
                    }
                }
                for (int si = 0; si < strips.Count; si++)
                {
                    var s = strips[si];
                    s.enabled = on && si < glitchCount && Random.value < 0.85f;
                    if (!s.enabled) continue;
                    bool block = Random.value < 0.55f;                                     // 블록: 화면 일부만
                    float y = Random.Range(0.03f, 0.95f), hh = block ? Random.Range(0.03f, 0.16f) : Random.Range(0.006f, 0.06f);
                    float x = block ? Random.Range(0f, 0.75f) : 0f, ww = block ? Random.Range(0.12f, 0.45f) : 1f;
                    ww = Mathf.Min(ww, 1f - x);
                    s.texture = rtA; s.uvRect = FlipRect(new Rect(x, y, ww, hh));
                    s.material = gm;
                    if (paintGlitch)
                    {
                        // 물감 띠: 덜 밀리고 · 아래로 흘러내리며 세로로 늘어짐(번짐) · 안료색으로 물듦 · 반투명
                        float smear = Random.Range(1f, 1.9f);
                        s.rectTransform.sizeDelta = new Vector2(1920 * ww, 1080 * hh * smear);
                        float shiftP = block ? Random.Range(-80f, 80f) : Random.Range(-55f, 55f);
                        s.rectTransform.anchoredPosition = new Vector2((x + ww * 0.5f - 0.5f) * 1920 + shiftP, (y + hh * 0.5f - 0.5f) * 1080 - 1080 * hh * (smear - 1) * 0.5f - Random.Range(0f, 18f));
                        float rp = Random.value;
                        s.color = rp < 0.3f ? new Color(1f, 0.72f, 0.75f, 0.82f) : rp < 0.55f ? new Color(0.78f, 0.7f, 1f, 0.8f) : rp < 0.7f ? new Color(1f, 0.88f, 0.65f, 0.8f) : new Color(1, 1, 1, 0.88f);
                        continue;
                    }
                    s.rectTransform.sizeDelta = new Vector2(1920 * ww, 1080 * hh);
                    float shift = block ? Random.Range(-140f, 140f) : Random.Range(-90f, 90f);
                    s.rectTransform.anchoredPosition = new Vector2((x + ww * 0.5f - 0.5f) * 1920 + shift, (y + hh * 0.5f - 0.5f) * 1080 + (block ? Random.Range(-12f, 12f) : 0));
                    float r = Random.value;
                    s.color = r < 0.2f ? new Color(0.75f, 1f, 1f, 0.95f) : r < 0.32f ? new Color(1f, 0.6f, 0.65f, 0.95f) : Color.white;
                }
            }
            foreach (var g in new[] { echoImg, splitR, splitC }) if (g) g.enabled = false;
            foreach (var s in strips) if (s) s.enabled = false;
            rtA.Release(); rtB.Release(); Destroy(rtA); Destroy(rtB);
        }

        /// <summary>센 박자에 글리치 터뜨리기</summary>
        public void Glitch(float dur = 0.18f) { glitchLeft = Mathf.Max(glitchLeft, dur); glitchSplit = true; glitchCount = strips.Count; }

        public IEnumerator Play(MShot[] shots, string folder)
        {
            root.gameObject.SetActive(true);
            fxOn = true; StartCoroutine(Fx());
            var vigT = root.Find("Vignette"); if (vigT) vigT.GetComponent<RawImage>().color = new Color(1, 1, 1, vignette);
            Texture2D prev = null;
            foreach (var s in shots)
            {
                // 앞 장면을 찍어 둠 (전환 조각 · 디졸브용)
                if (s.tin != MTrans.Cut && s.tin != MTrans.Flash)
                {
                    yield return new WaitForEndOfFrame();
                    if (prev) Destroy(prev);
                    prev = Grab();
                    if (IsBlack(prev)) { Destroy(prev); prev = null; }   // 캡처가 까맣게 찍히면(다른 캡처와 겹친 프레임) 전환 없이 컷 — 까만 화면이 번쩍이지 않게
                }
                Build(s, folder);
                Pose(s, 0);
                if (!string.IsNullOrEmpty(s.sfx) && Sfx.Has(s.sfx)) Sfx.Play(s.sfx, 0.85f, 0f);
                if (s.tin == MTrans.Flash) StartCoroutine(FlashIn(s.tinDur));
                else if (s.tin != MTrans.Cut && prev)
                {
                    Texture2D next = null;
                    if (s.tin == MTrans.Mix) { yield return new WaitForEndOfFrame(); next = Grab(); }
                    StartCoroutine(s.tin switch
                    {
                        MTrans.Dissolve => Dissolve(prev, s.tinDur),
                        MTrans.Shatter => Shatter(prev, s.tinDur),
                        MTrans.Whip => Whip(prev, s.tinDur),
                        MTrans.Zoom => ZoomThrough(prev, s.tinDur),
                        MTrans.Paint => PaintWipe(prev, s.tinDur),
                        _ => Mix(prev, next, s.tinDur),
                    });
                }
                int cue = 0;
                for (float t = 0; t < s.dur; t += Time.unscaledDeltaTime)
                {
                    float u = Mathf.Clamp01(t / s.dur);
                    while (s.cues != null && cue < s.cues.Length && u >= s.cues[cue].u)
                    {
                        var c = s.cues[cue++];
                        if (!string.IsNullOrEmpty(c.sfx) && Sfx.Has(c.sfx)) Sfx.Play(c.sfx, 0.9f, 0f);
                        kick = Mathf.Max(kick, c.shake);
                        if (c.flash > 0) StartCoroutine(FlashIn(c.flash));
                        if (glitch > 0 && c.shake >= 14) Glitch(0.12f + c.shake * 0.006f);
                    }
                    Pose(s, u);
                    yield return null;
                }
            }
            fxOn = false;
            if (prev) Destroy(prev);
            foreach (Transform c in cam) Destroy(c.gameObject);
            shards.Clear();
            placeholder.text = "";
        }

        /// <summary>화면 캡처를 sRGB 텍스처로 (리니어 프로젝트에서 캡처 값이 sRGB인데 리니어로 표시돼 하얗게 바래던 문제)</summary>
        static bool IsBlack(Texture2D t)
        {
            if (!t) return true;
            float sum = 0;
            for (int y = 1; y <= 3; y++) for (int x = 1; x <= 3; x++) { var c = t.GetPixel(t.width * x / 4, t.height * y / 4); sum += c.r + c.g + c.b; }
            return sum < 0.15f;
        }

        static Texture2D Grab()
        {
            var raw = ScreenCapture.CaptureScreenshotAsTexture();
            var t = new Texture2D(raw.width, raw.height, TextureFormat.RGBA32, false, false);
            t.SetPixels32(raw.GetPixels32()); t.Apply(false, false);
            Destroy(raw);
            return t;
        }

        // ---------- 전환 ----------
        IEnumerator FlashIn(float dur)
        {
            var w = UIKit.Stretch("Flash", root).gameObject.AddComponent<Image>(); w.raycastTarget = false;
            for (float t = 0; t < dur; t += Time.unscaledDeltaTime) { w.color = new Color(1, 1, 1, 1 - t / dur); yield return null; }
            Destroy(w.gameObject);
        }

        IEnumerator Dissolve(Texture2D a, float dur)
        {
            var img = UIKit.Stretch("Dissolve", root).gameObject.AddComponent<RawImage>(); img.texture = a; img.raycastTarget = false;
            for (float t = 0; t < dur; t += Time.unscaledDeltaTime)
            {
                float u = t / dur;
                img.color = new Color(1, 1, 1, 1 - u * u);
                img.rectTransform.localScale = Vector3.one * (1 + 0.04f * u);   // 꿈처럼 살짝 번지며 사라짐
                yield return null;
            }
            Destroy(img.gameObject);
        }

        /// <summary>물감 덮기: 앞 장면 위로 물감 방울들이 번져 화면을 뒤덮고(앞 절반) → 다른 모양으로 걷히며 다음 장면이 드러남(뒤 절반)</summary>
        static Shader paintWipeSh;
        IEnumerator PaintWipe(Texture2D a, float dur)
        {
            if (!paintWipeSh) paintWipeSh = Shader.Find("CatTang/PaintWipe");
            if (!paintWipeSh) { yield return Dissolve(a, dur); yield break; }
            var img = UIKit.Stretch("PaintWipe", root).gameObject.AddComponent<RawImage>(); img.texture = a; img.raycastTarget = false;
            var mat = new Material(paintWipeSh); img.material = mat;
            mat.SetFloat("_Seed", Random.Range(0f, 50f));
            // 꿈마다 같은 안료 (장미 · 보라 · 황토), 가끔 붉은 피 같은 물감
            if (Random.value < 0.3f) { mat.SetColor("_ColA", new Color(0.7f, 0.24f, 0.28f)); mat.SetColor("_ColB", new Color(0.48f, 0.3f, 0.44f)); }
            if (Sfx.Has("story_dream")) Sfx.Play("story_dream", 0.25f, 0f);
            for (float t = 0; t < dur; t += Time.unscaledDeltaTime)
            {
                float u = t / dur;
                // 덮을 땐 빠르게 퍼지고(가속) · 걷힐 땐 처음에 확 → 천천히
                float p = u < 0.5f ? Mathf.Pow(u / 0.5f, 0.7f) : 1f + (1 - Mathf.Pow(1 - (u - 0.5f) / 0.5f, 2));
                mat.SetFloat("_Prog", p);
                img.rectTransform.localScale = Vector3.one * (1 + 0.03f * u);
                yield return null;
            }
            Destroy(img.gameObject); Destroy(mat);
        }

        /// <summary>휙: 앞 장면이 흐려지며 옆으로 빠르게 쓸려 나감 (카메라를 휙 돌린 느낌)</summary>
        IEnumerator Whip(Texture2D a, float dur)
        {
            var img = UIKit.Stretch("Whip", root).gameObject.AddComponent<RawImage>(); img.texture = a; img.raycastTarget = false;
            var mat = blurBase ? new Material(blurBase) : null; if (mat) img.material = mat;
            float dir = Random.value < 0.5f ? -1 : 1;
            for (float t = 0; t < dur; t += Time.unscaledDeltaTime)
            {
                float u = t / dur;
                img.rectTransform.anchoredPosition = new Vector2(dir * 2400f * u * u, Random.Range(-6f, 6f));
                if (mat) mat.SetFloat("_Blur", 0.035f * Mathf.Sqrt(u));
                img.color = new Color(1, 1, 1, 1 - u * u);
                yield return null;
            }
            Destroy(img.gameObject); if (mat) Destroy(mat);
        }

        /// <summary>줌 관통(매치 컷): 앞 장면 가운데로 빨려 들어가며 다음 장면에 겹쳐 사라짐 — 같은 자리 원형끼리 이어 붙임</summary>
        IEnumerator ZoomThrough(Texture2D a, float dur)
        {
            var img = UIKit.Stretch("ZoomThrough", root).gameObject.AddComponent<RawImage>(); img.texture = a; img.raycastTarget = false;
            var mat = blurBase ? new Material(blurBase) : null; if (mat) img.material = mat;
            for (float t = 0; t < dur; t += Time.unscaledDeltaTime)
            {
                float u = t / dur;
                img.rectTransform.localScale = Vector3.one * (1 + 1.6f * u * u);
                if (mat) { mat.SetFloat("_Blur", 0.02f * u); mat.SetFloat("_Edge", 0.03f * u); }
                img.color = new Color(1, 1, 1, 1 - u);
                yield return null;
            }
            Destroy(img.gameObject); if (mat) Destroy(mat);
        }

        /// <summary>깨진 유리: 화면을 들쭉날쭉한 삼각 조각으로 나눔 (정규 좌표 0~1)</summary>
        static List<Vector2[]> Cracks(int cols, int rows, float jitter)
        {
            var p = new Vector2[cols + 1, rows + 1];
            for (int x = 0; x <= cols; x++)
                for (int y = 0; y <= rows; y++)
                {
                    float jx = x == 0 || x == cols ? 0 : Random.Range(-jitter, jitter) / cols;
                    float jy = y == 0 || y == rows ? 0 : Random.Range(-jitter, jitter) / rows;
                    p[x, y] = new Vector2((float)x / cols + jx, (float)y / rows + jy);
                }
            var list = new List<Vector2[]>();
            for (int x = 0; x < cols; x++)
                for (int y = 0; y < rows; y++)
                {
                    var a = p[x, y]; var b = p[x + 1, y]; var c = p[x + 1, y + 1]; var d = p[x, y + 1];
                    if (Random.value < 0.5f) { list.Add(new[] { a, b, c }); list.Add(new[] { a, c, d }); }
                    else { list.Add(new[] { a, b, d }); list.Add(new[] { b, c, d }); }
                }
            return list;
        }

        ShardGraphic Shard(Texture tex, Vector2[] poly, string n)
        {
            var c = Vector2.zero; foreach (var v in poly) c += v; c /= poly.Length;
            var rt = UIKit.Stretch(n, root);
            rt.pivot = c;
            if (!rt.GetComponent<CanvasRenderer>()) rt.gameObject.AddComponent<CanvasRenderer>();
            var g = rt.gameObject.AddComponent<ShardGraphic>();
            g.tex = tex; g.poly = poly; g.raycastTarget = false;
            g.SetVerticesDirty();
            return g;
        }

        IEnumerator Shatter(Texture2D a, float dur)
        {
            var hit = new Vector2(Random.Range(0.35f, 0.65f), Random.Range(0.35f, 0.65f));
            var list = new List<(ShardGraphic g, Vector2 dir, float spin, float delay)>();
            foreach (var poly in Cracks(9, 6, 0.8f))
            {
                var g = Shard(a, poly, "Shard");
                var c = (poly[0] + poly[1] + poly[2]) / 3f;
                var d = c - hit;
                list.Add((g, d.normalized * Random.Range(500f, 1100f) + Vector2.down * 200f, Random.Range(-160f, 160f), d.magnitude * 0.25f));
            }
            if (Sfx.Has("e_glass")) Sfx.Play("e_glass", 0.6f, 0f);
            for (float t = 0; t < dur + 0.25f; t += Time.unscaledDeltaTime)
            {
                foreach (var (g, dir, spin, delay) in list)
                {
                    float u = Mathf.Clamp01((t - delay) / dur);
                    var rt = g.rectTransform;
                    rt.anchoredPosition = dir * u * u + (u > 0 ? Vector2.zero : Random.insideUnitCircle * 2f);   // 금이 간 채 떨리다 흩어짐
                    rt.localRotation = Quaternion.Euler(0, 0, spin * u);
                    rt.localScale = Vector3.one * (1 - 0.25f * u);
                    g.color = new Color(1, 1, 1, 1 - u);
                }
                yield return null;
            }
            foreach (var s in list) Destroy(s.g.gameObject);
        }

        /// <summary>조각 섞기: 같은 자리의 조각이 앞 장면 · 다음 장면 사이를 무작위로 깜빡이며 어긋났다가, 점점 다음 장면으로 맞춰짐 (엇갈린 시간)</summary>
        IEnumerator Mix(Texture2D a, Texture2D b, float dur)
        {
            var list = new List<(ShardGraphic g, Vector2 off, float rot, float settle)>();
            foreach (var poly in Cracks(7, 4, 0.9f))
            {
                var g = Shard(Random.value < 0.5f ? a : b, poly, "Mix");
                g.feather = 0.12f;                                              // 조각 테두리 번짐 (서로 섞이게)
                list.Add((g, Random.insideUnitCircle * 40f, Random.Range(-6f, 6f), Random.Range(0.35f, 1f)));
            }
            float nextFlip = 0;
            for (float t = 0; t < dur; t += Time.unscaledDeltaTime)
            {
                float u = t / dur;
                bool flip = t >= nextFlip;
                if (flip) nextFlip = t + Random.Range(0.06f, 0.14f);
                foreach (var (g, off, rot, settle) in list)
                {
                    float k = Mathf.Clamp01(u / settle);                       // 이 조각이 다음 장면으로 자리 잡는 정도
                    if (flip && k < 1f && Random.value < 0.35f) { g.tex = g.tex == a ? b : a; g.SetMaterialDirty(); }
                    if (k >= 1f && g.tex != b) { g.tex = b; g.SetMaterialDirty(); }
                    var rt = g.rectTransform;
                    rt.anchoredPosition = off * (1 - k);
                    rt.localRotation = Quaternion.Euler(0, 0, rot * (1 - k));
                    g.color = new Color(1, 1, 1, k >= 1f ? 1 - Mathf.Clamp01((u - settle) * 4f) : 1);
                }
                yield return null;
            }
            foreach (var s in list) Destroy(s.g.gameObject);
            if (b) Destroy(b);
        }
    }

    /// <summary>텍스처의 한 다각형 조각을 그 자리 그대로 그림 (정규 좌표 = UV). 콜라주용으로 화면 비율 맞춤 · 확대 · 어긋남</summary>
    public class ShardGraphic : MaskableGraphic
    {
        public Texture tex;
        public Vector2[] poly;
        public float uvZoom = 1f;
        public Vector2 uvOff;
        public override Texture mainTexture => tex ? tex : s_WhiteTexture;

        public float feather;                     // 가장자리 번짐: 조각 테두리가 투명하게 스러져 옆 조각과 섞임 (0 = 날카로운 유리 조각)

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (poly == null || poly.Length < 3) return;
            var r = rectTransform.rect;
            var col = color;
            // 텍스처를 화면 비율에 맞춰 덮기 (늘어나지 않게)
            var sc = Vector2.one;
            if (tex && r.height > 0)
            {
                float ra = r.width / r.height, ta = (float)tex.width / tex.height;
                sc = ta > ra ? new Vector2(ra / ta, 1) : new Vector2(1, ta / ra);
            }
            void Vert(Vector2 p, Color32 c)
            {
                var pos = new Vector2(r.xMin + p.x * r.width, r.yMin + p.y * r.height);
                var uv = new Vector2(0.5f, 0.5f) + Vector2.Scale(p - new Vector2(0.5f, 0.5f), sc) / uvZoom + uvOff;
                vh.AddVert(pos, c, uv);
            }
            int n = poly.Length;
            if (feather <= 0)
            {
                foreach (var p in poly) Vert(p, col);
                for (int i = 1; i + 1 < n; i++) vh.AddTriangle(0, i, i + 1);
                return;
            }
            // 안쪽(불투명) 다각형 + 바깥 띠(투명으로 스러짐). 바깥은 원래 테두리보다 살짝 넓혀 조각 사이 틈 없이 겹침
            var cen = Vector2.zero; foreach (var p in poly) cen += p; cen /= n;
            var clear = col; clear.a = 0;
            foreach (var p in poly) Vert(cen + (p - cen) * (1 - feather), col);            // 0..n-1
            foreach (var p in poly) Vert(cen + (p - cen) * (1 + feather * 0.8f), clear);  // n..2n-1
            for (int i = 1; i + 1 < n; i++) vh.AddTriangle(0, i, i + 1);
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                vh.AddTriangle(i, j, n + j);
                vh.AddTriangle(i, n + j, n + i);
            }
        }
    }
}
