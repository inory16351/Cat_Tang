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
        public static Gradient TailGradient()
        {
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.55f, 0.55f), 0.6f), new GradientColorKey(new Color(0.85f, 0.25f, 0.28f), 1f) },
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

        public static EyeTrail Attach(Enemy e)
        {
            if (!e || e.GetComponent<EyeTrail>()) return null;
            var t = e.gameObject.AddComponent<EyeTrail>();
            t.Build(e);
            return t;
        }

        void Build(Enemy e)
        {
            // 머리 높이 = 모델 렌더러 경계 위쪽 85%, 정면으로 몸 반지름 35%
            float h = 2f, r = 0.5f;
            var rs = GetComponentsInChildren<Renderer>();
            if (rs.Length > 0)
            {
                var b = rs[0].bounds; foreach (var x in rs) if (!(x is TrailRenderer) && !(x is LineRenderer)) b.Encapsulate(x.bounds);
                h = b.max.y - transform.position.y; r = Mathf.Max(b.extents.x, b.extents.z);
            }
            float y = Mathf.Max(0.8f, h * 0.84f), fwd = Mathf.Clamp(r * 0.35f, 0.15f, 0.6f), gap = Mathf.Clamp(h * 0.06f, 0.07f, 0.25f);
            trails = new TrailRenderer[2]; eyes = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                var go = new GameObject(i == 0 ? "EyeL" : "EyeR");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(i == 0 ? -gap : gap, y, fwd);
                var tr = go.AddComponent<TrailRenderer>();
                tr.sharedMaterial = LaserFx.StripMat();
                tr.textureMode = LineTextureMode.Stretch;
                tr.time = 0.001f;
                tr.minVertexDistance = 0.08f;
                tr.widthMultiplier = 0.2f;
                tr.widthCurve = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 0.7f));   // 끝이 0이 되면 리본 꼬리(제비꼬리)가 안 보임
                tr.numCapVertices = 2;
                tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                tr.receiveShadows = false;
                tr.colorGradient = LaserFx.TailGradient();
                tr.emitting = true;                     // 끄지 않음: 끄면 다음 점에서 폭이 0으로 뚝 끊김 (Unity 동작) → 대신 time을 줄여 꼬리가 눈으로 빨려 들어감
                trails[i] = tr; eyes[i] = go.transform; prevEye[i] = go.transform.position;
            }
            last = transform.position;
        }

        /// <summary>강제로 잠깐 꼬리를 켬 (회전 베기처럼 제자리에서 빠른 동작)</summary>
        public void Hot(float dur) => hotUntil = Mathf.Max(hotUntil, Time.time + dur);

        void LateUpdate()
        {
            if (trails == null) return;
            var p = transform.position;
            float moved = Effects.Flat(p - last).magnitude;
            float spd = moved / Mathf.Max(1e-4f, Time.deltaTime);
            last = p;
            bool on = spd > FastSpeed || moved > 1.2f || Time.time < hotUntil;
            if (on)
            {
                if (curTime < 0.01f)
                    for (int i = 0; i < 2; i++)
                        if (trails[i]) { trails[i].Clear(); trails[i].AddPosition(prevEye[i]); }   // 새로 시작: 지난 프레임 눈 자리부터 (점멸이면 옛 자리 → 새 자리가 곧은 레이저 줄)
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
