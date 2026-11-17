// 이펙트 프리팹의 조각 (FxEffect 참고). 클래스 이름 = 파일 이름이어야 프리팹에 저장됨
using UnityEngine;

namespace CatTang
{
    [RequireComponent(typeof(MeshRenderer))]
    public class FxPart : MonoBehaviour
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

        MeshRenderer mr;
        MaterialPropertyBlock mpb;
        Vector3 baseScale, worldDrift;
        Quaternion baseRot;
        float born, roll, lifeK = 1;
        bool started;

        public float EndTime => delay + life * 1.2f;

        Material[] frames;
        static readonly int ColorId = Shader.PropertyToID("_Color");

        public void Begin(Color? tint)
        {
            mr = GetComponent<MeshRenderer>();
            mpb = new MaterialPropertyBlock();
            if (!string.IsNullOrEmpty(flipSet))
            {
                frames = FxFlipbook.Frames(flipSet);
                var mf = GetComponent<MeshFilter>();
                if (mf) mf.sharedMesh = GunkataFx.Quad;
                if (frames.Length > 0) mr.sharedMaterial = frames[0];
                billboard = true;
                // 구운 그림은 제 색이 있으므로 탄 색은 절반만 섞음
                mpb.SetColor(ColorId, tint.HasValue && useTint ? Color.Lerp(Color.white, tint.Value, 0.5f) : Color.white);
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
                transform.position += worldDrift * Time.deltaTime;
                if (kind != FxMesh.Kind.Tracer) worldDrift *= Mathf.Exp(-6f * Time.deltaTime);
                if (spin != 0) baseRot = baseRot * Quaternion.AngleAxis(spin * Time.deltaTime, spinAxis);
            }
            Pose(t);
        }

        void Pose(float t)
        {
            mr.enabled = t >= 0;
            if (t < 0) { transform.localScale = Vector3.zero; return; }
            var cam0 = Camera.main;
            if (frames != null && frames.Length > 0)
            {
                // 플립북: 프레임을 차례로, 조금씩 커짐
                mr.sharedMaterial = frames[Mathf.Min(frames.Length - 1, (int)(t * frames.Length))];
                transform.localScale = baseScale * Mathf.Lerp(1f, flipGrow, t);
                if (cam0) transform.rotation = Quaternion.LookRotation(cam0.transform.forward, cam0.transform.up) * Quaternion.Euler(0, 0, roll);
                mr.SetPropertyBlock(mpb);
                return;
            }
            FxMesh.Evaluate(kind, baseScale, t, out var sc, out float heat, out float dis);
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
