// 이펙트 프리팹의 조각 (FxEffect 참고). 클래스 이름 = 파일 이름이어야 프리팹에 저장됨
using UnityEngine;

namespace CatTang
{
    [RequireComponent(typeof(MeshRenderer))]
    public class FxPart : MonoBehaviour, IPooled
    {
        public FxMesh.Kind kind = FxMesh.Kind.Core;
        [Tooltip("수명(초)")] public float life = 0.2f;
        [Tooltip("나타나기 전 지연(초)")] public float delay = 0f;
        [Tooltip("이동 속도 (루트 기준 방향 · m/초, 루트 크기를 곱함). 점점 느려짐 (Tracer는 그대로)")] public Vector3 drift;
        [Tooltip("회전 속도(도/초)")] public float spin;
        public Vector3 spinAxis = Vector3.forward;
        [Tooltip("카메라를 봄 (납작한 모양: +Z가 카메라 쪽, 위가 화면 위)")] public bool billboard;
        [Tooltip("크기 · 수명 · 방향을 이만큼 무작위로 흔듦 (0~1)")] [Range(0, 1)] public float jitter = 0.15f;
        [Tooltip("카메라 보는 조각을 화면에서 무작위로 돌림")] public bool randomRoll = true;
        [Tooltip("탄 색을 입힘 (false = 재질 색 그대로, 예: 치명타 금빛)")] public bool useTint = true;
        [Tooltip("블렌더로 구운 플립북 (Resources/FX/anim/이름_0~N, 예: bk_fire). 지정하면 카메라를 보는 판에 수명 동안 프레임을 차례로 재생")]
        public string flipSet = "";
        [Tooltip("플립북: 수명 동안 커지는 비율 (1 = 그대로)")] public float flipGrow = 1.15f;
        [Tooltip("플립북: 전체 불투명도 (1 = 그대로, 0.7 = 반투명하게 비쳐 보임 — 추억 시계)")] [Range(0, 1)] public float opacity = 1f;
        [Tooltip("붙어 버팀: 톡 나타난 뒤 수명 끝(마지막 15%)까지 크기 · 열을 그대로 유지하다 확 오그라듦 (지연 충전 폭탄 · 미련 불씨)")] public bool hold;
        [Tooltip("붙어 버틸 때 열 (0 = 짙은 탄 색 · 1 = 하얀 심지)")] [Range(0, 1)] public float holdHeat = 0.5f;
        [Tooltip("깜빡임(초당 횟수, 0 = 없음): 크기가 툭툭 튀고 하얗게 달아오름. 끝으로 갈수록 빨라짐")] public float blink;
        [Tooltip("플립북을 카메라 대신 바닥에 눕힘 (위에서 내려다보고 구운 그림: 발 디딤 흙먼지 고리 · 돌진 자국). 발 · 몸이 판을 제대로 가림. 그림 위쪽 = 루트 앞(+Z)")] public bool flat;
        [Tooltip("카메라 보는 플립북: 그림의 어느 높이(아래 0 ~ 위 1)가 이 조각 자리에 오는지. 0.3 = 땅에 닿는 점이 그림 아래 30%인 그림 (솟는 먼지)")] [Range(0, 1)] public float pivotY = 0.5f;
        [Tooltip("카메라 보는 플립북: 그림의 어느 가로 자리(왼 0 ~ 오른 1)가 이 조각 자리에 오는지. 돌진 꼬리 = 0.85 (오른쪽 새로 생긴 끝이 몸 뒤)")] [Range(0, 1)] public float pivotX = 0.5f;
        [Tooltip("카메라 보는 판을 화면 자리는 그대로 두고 카메라 쪽으로 당김(m, 루트 크기를 곱함) — 판 아래가 바닥에 묻히지 않게")] public float liftToCamera;
        [Tooltip("카메라 보는 플립북의 그림 +X(오른쪽)를 화면에 비친 루트 앞(+Z) 방향에 맞춤 (돌진 꼬리: 그림은 오른쪽으로 달려 나가고 꼬리가 왼쪽)")] public bool alignToRoot;

        MeshRenderer mr;
        MaterialPropertyBlock mpb;
        Vector3 baseScale, worldDrift, baseLocal, driftAcc;
        Quaternion baseRot;
        float born, roll, lifeK = 1;
        bool started;

        public float EndTime => delay + life * 1.2f;

        Material[] frames;
        static readonly int ColorId = Shader.PropertyToID("_Color");

        // 풀링 (2026-11-20): 최초 자세를 보관해 줄어든 크기와 누적 회전을 다음 재생에 넘기지 않는다.
        Vector3 originalPosition, originalScale;
        Quaternion originalRotation;
        MeshFilter mf;
        bool cached;
        void Awake() => CachePose();

        internal void CachePose()
        {
            if (cached) return;
            cached = true;
            originalPosition = transform.localPosition; originalScale = transform.localScale; originalRotation = transform.localRotation;
            mr = GetComponent<MeshRenderer>(); mf = GetComponent<MeshFilter>();
            mpb = new MaterialPropertyBlock();
        }

        void ResetPose()
        {
            transform.localPosition = originalPosition; transform.localRotation = originalRotation; transform.localScale = originalScale;
        }

        public void OnSpawned() { CachePose(); ResetPose(); started = false; driftAcc = Vector3.zero; frames = null; mpb.Clear(); }
        public void OnDespawned() { CachePose(); started = false; driftAcc = Vector3.zero; ResetPose(); }

        public void Begin(Color? tint)
        {
            CachePose(); ResetPose();
            mpb.Clear(); frames = null;
            if (!string.IsNullOrEmpty(flipSet))
            {
                frames = FxFlipbook.Frames(flipSet);
                if (mf) mf.sharedMesh = GunkataFx.Quad;
                if (frames.Length > 0) mr.sharedMaterial = frames[0];
                billboard = !flat;
                // 구운 그림은 제 색이 있으므로 탄 색은 절반만 섞음
                var fc = tint.HasValue && useTint ? Color.Lerp(Color.white, tint.Value, 0.5f) : Color.white;
                fc.a *= opacity;
                mpb.SetColor(ColorId, fc);
            }
            else
            {
                mpb.SetFloat(FxMesh.SeedId, Random.Range(0f, 100f));
                if (tint.HasValue && useTint) FxTint.Apply(mpb, tint.Value);
            }
            float j = jitter;
            baseScale = transform.localScale * Random.Range(1 - j, 1 + j);
            lifeK = Random.Range(1 - j * 0.5f, 1 + j * 0.5f);
            baseRot = transform.localRotation;
            if (!billboard && j > 0) baseRot = Quaternion.Euler(Random.Range(-25f, 25f) * j, Random.Range(-25f, 25f) * j, 0) * baseRot;
            roll = randomRoll ? Random.Range(0f, 360f) : 0f;
            var root = transform.parent ? transform.parent : transform;
            worldDrift = root.TransformDirection(drift) * root.lossyScale.x * Random.Range(1 - j, 1 + j);
            born = Time.time;
            baseLocal = transform.localPosition;
            driftAcc = Vector3.zero;
            started = true;
            Pose(-1);
        }

        void LateUpdate()
        {
            if (!started) return;
            float t = (Time.time - born - delay) / Mathf.Max(0.01f, life * lifeK);
            if (t >= 1) { mr.enabled = false; started = false; return; }
            if (t >= 0)
            {
                driftAcc += worldDrift * Time.deltaTime;
                if (kind != FxMesh.Kind.Tracer) worldDrift *= Mathf.Exp(-6f * Time.deltaTime);
                if (spin != 0) baseRot = baseRot * Quaternion.AngleAxis(spin * Time.deltaTime, spinAxis);
            }
            Pose(t);
        }

        void Pose(float t)
        {
            // 자리: 루트 기준 처음 자리 + 흘러간 만큼 (루트가 대상을 따라다녀도 맞음) + 그림 기준점 · 카메라 쪽 당김
            transform.localPosition = baseLocal;
            transform.position += driftAcc;
            mr.enabled = t >= 0;
            if (t < 0) { transform.localScale = Vector3.zero; return; }
            var cam0 = Camera.main;
            if (frames != null && frames.Length > 0)
            {
                // 플립북: 프레임을 차례로, 조금씩 커짐
                mr.sharedMaterial = frames[Mathf.Min(frames.Length - 1, (int)(t * frames.Length))];
                transform.localScale = baseScale * Mathf.Lerp(1f, flipGrow, t);
                if (flat) transform.rotation = Quaternion.Euler(0, (transform.parent ? transform.parent.eulerAngles.y : 0) + roll, 0) * Quaternion.Euler(90, 0, 0);
                else if (cam0)
                {
                    float r = roll;
                    if (alignToRoot && transform.parent)
                    {
                        var f = transform.parent.forward;
                        r += Mathf.Atan2(Vector3.Dot(f, cam0.transform.up), Vector3.Dot(f, cam0.transform.right)) * Mathf.Rad2Deg;
                    }
                    transform.rotation = Quaternion.LookRotation(cam0.transform.forward, cam0.transform.up) * Quaternion.Euler(0, 0, r);
                    if (pivotX != 0.5f || pivotY != 0.5f || liftToCamera != 0)
                    {
                        // 그림 기준점(돌린 그림의 가로 · 세로)이 조각 자리에 오게 + 카메라 쪽 당김 (화면 자리 그대로)
                        float k = transform.parent ? transform.parent.lossyScale.x : 1f;
                        float sz = baseScale.y * k * Mathf.Lerp(1f, flipGrow, Mathf.Max(0f, t));
                        var p0 = transform.position;
                        transform.position = p0 + transform.right * (0.5f - pivotX) * sz + transform.up * (0.5f - pivotY) * sz
                            + (cam0.transform.position - p0).normalized * liftToCamera * k;
                    }
                }
                mr.SetPropertyBlock(mpb);
                return;
            }
            FxMesh.Evaluate(kind, baseScale, t, out var sc, out float heat, out float dis);
            if (hold)
            {
                float pop = t < 0.12f ? Mathf.Lerp(0.3f, 1.15f, Mathf.Sin(t / 0.12f * Mathf.PI * 0.5f)) : Mathf.Lerp(1.15f, 1f, Mathf.Clamp01((t - 0.12f) / 0.1f));
                float end = t > 0.85f ? 1 - (t - 0.85f) / 0.15f : 1;
                sc = baseScale * pop * end;
                heat = holdHeat; dis = 0;
            }
            if (blink > 0)
            {
                // 끝으로 갈수록 빨라지는 깜빡임 (위상 = 적분한 주파수)
                float ph = blink * life * lifeK * (t + t * t);
                float b = Mathf.Pow(Mathf.Abs(Mathf.Sin(ph * Mathf.PI)), 6f);
                sc *= 1 + 0.35f * b;
                heat = Mathf.Max(heat, b);
            }
            transform.localScale = sc;
            var cam = Camera.main;
            if (billboard && cam) transform.rotation = Quaternion.LookRotation(-cam.transform.forward, cam.transform.up) * Quaternion.Euler(0, 0, roll) * baseRot;
            else transform.localRotation = baseRot;
            mpb.SetFloat(FxMesh.HeatId, heat);
            mpb.SetFloat(FxMesh.DissolveId, dis);
            mr.SetPropertyBlock(mpb);
        }
    }

    /// <summary>Vivid 레이어 (2026-11-17 사용자: 닷지 액션 별도 렌더 레이어): URP 렌더러가 이 레이어를 후처리가 끝난 뒤 따로 그림
    /// (PC_Renderer의 "Vivid Opaque/Transparent (후처리 뒤)" 기능, 일반 패스에서는 제외) → 극한 회피 회색 화면 · 피격 붉은 화면에서도 고양이와 고양이 쪽 이펙트는 제 색</summary>
    public static class VividLayer
    {
        static int id = -2;
        public static int Id { get { if (id == -2) id = LayerMask.NameToLayer("Vivid"); return id; } }
        public static void Set(GameObject g) { if (g && Id >= 0) g.layer = Id; }
        public static void SetAll(GameObject g) { if (!g || Id < 0) return; foreach (var t in g.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = Id; }
    }

    /// <summary>툰 불꽃 재질에 탄 색 입히기 (FxMesh · FxPart 공용)</summary>
    public static class FxTint
    {
        static readonly int Core = Shader.PropertyToID("_ColCore"), Hot = Shader.PropertyToID("_ColHot"), Mid = Shader.PropertyToID("_ColMid"),
            Edge = Shader.PropertyToID("_ColEdge"), Ash = Shader.PropertyToID("_ColAsh"), Outline = Shader.PropertyToID("_OutlineColor"), Intensity = Shader.PropertyToID("_Intensity");

        /// <summary>흰 심지는 옅은 탄 색, 뜨거운 층 → 탄 색 → 짙은 테 → 그을음, 외곽선은 탄 색의 짙은 잉크. 어두운 색도 불꽃처럼 밝기를 올림</summary>
        public static void Apply(MaterialPropertyBlock mpb, Color c)
        {
            Color.RGBToHSV(c, out float h, out float s, out float v);
            v = Mathf.Max(v, 0.85f);
            mpb.SetColor(Core, Color.HSVToRGB(h, s * 0.3f, 1f));
            mpb.SetColor(Hot, Color.HSVToRGB(h, s * 0.7f, 1f));
            mpb.SetColor(Mid, Color.HSVToRGB(h, s, v));
            mpb.SetColor(Edge, Color.HSVToRGB(h, Mathf.Min(1f, s * 1.1f), v * 0.62f));
            mpb.SetColor(Ash, Color.HSVToRGB(h, s * 0.6f, v * 0.28f));
            mpb.SetColor(Outline, Color.HSVToRGB(h, s * 0.7f, 0.13f));
            mpb.SetFloat(Intensity, 1.45f);
        }
    }
}
