// 3장 거울 마을 · 스토리 맵 3 거울 (2026-11-25 사용자: 거울에 '진짜 마을'이 비침)
// 거울 유리(재질 이름 MirrorGlass…)에 전용 카메라가 그린 그림을 띄움. 카메라는 메인 카메라와 같은 각도로 정한 자리(viewCenter, 또는 고양이)를 보고,
// 투영을 좌우로 뒤집어 그림 → 거울처럼 좌우가 바뀐 모습. ReflectOnly 레이어(거울에만 비치는 것: 노인 집 물건 · 노인 실루엣)는 보이고,
// MainOnly 레이어(거울엔 안 비치는 것: 거울 앞에 선 NPC · 광장의 고양이)는 뺌. 고양이가 가까이(range) 있을 때만 그림 (값싸게)
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CatTang
{
    public static class MirrorLayers
    {
        public const int ReflectOnly = 9, MainOnly = 10;
        /// <summary>메인 카메라는 거울 전용(ReflectOnly)을 안 그림</summary>
        public static void HideFromMain()
        {
            var cam = Camera.main;
            if (cam) cam.cullingMask &= ~(1 << ReflectOnly);
        }
        public static void SetLayer(GameObject go, int layer) { foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer; }
    }

    public class MirrorView : MonoBehaviour
    {
        static readonly int PassId = Shader.PropertyToID("_CatTangMirrorPass");
        static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");

        [Tooltip("거울이 비추는 자리 (월드). followPlayer면 무시")] public Vector3 viewCenter;
        public bool followPlayer;
        [Tooltip("메인 카메라 거리 대비 배율 (클수록 넓게)")] public float viewScale = 1.6f;
        [Tooltip("고양이가 이 거리 안에 있을 때만 그림")] public float range = 26f;
        public int texHeight = 640;
        [Tooltip("그림 위에 덧입히는 거울빛")] public Color tint = new(0.86f, 0.93f, 1f);
        [Tooltip("좌우 반전 (물웅덩이 고양이처럼 그림 자체가 이미 뒤집힌 것이면 끔)")] public bool flip = true;
        [Tooltip("진짜 거울처럼 유리 자리에서 고양이를 마주 봄 (광장 큰 거울)")] public bool faceFromGlass;
        public float faceFov = 34f;
        [System.NonSerialized] public int excludeMask;                     // 잠깐 거울에서 뺄 레이어 (광장: 고양이 모델이 있는 Vivid 레이어)
        [Tooltip("카메라 위치를 직접 정함 (스토리 맵: 수면 속에서 올려다봄 등). 비면 메인 카메라 각도")] public Transform customEye;

        Camera cam;
        RenderTexture rt;
        Renderer glass;
        int slot = -1;
        Material mat;
        float aspect = 0.73f;
        public bool Broken { get; private set; }

        void Start()
        {
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                var ms = r.sharedMaterials;
                for (int i = 0; i < ms.Length; i++)
                    if (ms[i] && ms[i].name.StartsWith("MirrorGlass")) { glass = r; slot = i; break; }
                if (glass) break;
            }
            if (!glass) { Debug.LogWarning($"[MirrorView] {name}: MirrorGlass 재질 없음"); enabled = false; return; }
            // 유리 크기로 그림 비율
            var mf = glass.GetComponent<MeshFilter>();
            if (mf && mf.sharedMesh && mf.sharedMesh.subMeshCount > slot)
            {
                var sm = mf.sharedMesh.GetSubMesh(slot);
                var b = sm.bounds;
                float w = Mathf.Max(b.size.x, 0.01f), h = Mathf.Max(Mathf.Max(b.size.y, b.size.z), 0.01f);
                if (b.size.y < 0.05f) h = Mathf.Max(b.size.z, 0.01f);           // 바닥에 누운 유리 (물웅덩이)
                aspect = Mathf.Clamp(w / h, 0.3f, 3f);
            }
            int th = texHeight, tw = Mathf.RoundToInt(th * aspect);
            rt = new RenderTexture(tw, th, 24, RenderTextureFormat.ARGB32) { name = "Mirror_" + name };
            var go = new GameObject("MirrorCam_" + name) { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(transform, false);
            cam = go.AddComponent<Camera>();
            var data = go.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = false;
            data.renderShadows = false;
            cam.targetTexture = rt;
            cam.enabled = false;
            mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            mat.SetTexture(BaseMapId, rt);
            mat.SetColor("_BaseColor", tint);
            var mats = glass.materials;
            mats[slot] = mat;
            glass.materials = mats;
            RenderPipelineManager.beginCameraRendering += BeginCam;
            RenderPipelineManager.endCameraRendering += EndCam;
        }

        void OnDestroy()
        {
            RenderPipelineManager.beginCameraRendering -= BeginCam;
            RenderPipelineManager.endCameraRendering -= EndCam;
            if (rt) { rt.Release(); Destroy(rt); }
            if (mat) Destroy(mat);
        }

        /// <summary>유리를 깸: 카메라 끄고 유리 칸을 어두운 거울 뒷판 색으로</summary>
        public void Break()
        {
            Broken = true;
            if (cam) cam.enabled = false;
            if (mat) { mat.SetTexture(BaseMapId, null); mat.SetColor("_BaseColor", new Color(0.16f, 0.15f, 0.26f)); }
        }

        /// <summary>유리 한가운데 (월드)</summary>
        public Vector3 GlassCenter => glass ? glass.bounds.center : transform.position + Vector3.up * 2f;

        void LateUpdate()
        {
            var main = Camera.main;
            var p = PlayerController.Instance;
            if (Broken || !main || !cam) return;
            bool near = !p || (Effects.Flat(p.transform.position - transform.position).magnitude < range);
            cam.enabled = near;
            if (!near) return;
            if (customEye)
            {
                cam.transform.SetPositionAndRotation(customEye.position, customEye.rotation);
            }
            else if (faceFromGlass && p)
            {
                var eye = GlassCenter + Vector3.up * 0.2f;
                var look = p.transform.position + Vector3.up * 1.0f;
                cam.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(look - eye));
            }
            else
            {
                Vector3 center = followPlayer && p ? p.transform.position + Vector3.up * 0.9f : viewCenter;
                // 메인 카메라가 지금 비추는 곳(바닥과 만나는 점)에서 카메라까지의 거리 · 방향을 그대로 씀
                var fwd = main.transform.forward;
                float dist = 20f;
                if (fwd.y < -0.05f) dist = (main.transform.position.y - center.y) / -fwd.y;
                cam.transform.SetPositionAndRotation(center - fwd * dist * viewScale, main.transform.rotation);
            }
            cam.fieldOfView = faceFromGlass ? faceFov : customEye ? 30f : main.fieldOfView;
            cam.nearClipPlane = 0.3f; cam.farClipPlane = 300f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = main.backgroundColor;
            cam.cullingMask = (main.cullingMask | (1 << MirrorLayers.ReflectOnly)) & ~(1 << MirrorLayers.MainOnly) & ~(1 << 5) & ~excludeMask;
            cam.aspect = aspect;
            cam.ResetProjectionMatrix();
            if (flip) cam.projectionMatrix = Matrix4x4.Scale(new Vector3(-1, 1, 1)) * cam.projectionMatrix;   // 좌우 반전
        }

        void BeginCam(ScriptableRenderContext ctx, Camera c) { if (c != cam) return; GL.invertCulling = flip; Shader.SetGlobalFloat(PassId, 1); }
        void EndCam(ScriptableRenderContext ctx, Camera c) { if (c != cam) return; GL.invertCulling = false; Shader.SetGlobalFloat(PassId, 0); }
    }
}
