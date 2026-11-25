// 3장 거울 호수 바닥의 실시간 반사 (2026-11-18 사용자: 모래사장 말고 거울 느낌)
// 메인 카메라 옆에 반사 카메라를 하나 두고, 바닥 면(y = PlaneY)을 기준으로 뒤집은 시점에서 세상을 그려
// 전역 텍스처 _CatTangMirrorTex(알파 = 물체가 있는 곳)로 넘김 → 바닥 셰이더 CatTang/MirrorFloor가 화면 좌표로 섞음.
// - 비스듬한 자르기 면(oblique)으로 바닥 아래 · 바닥 자체는 안 그림, 뒤집힌 시점이라 그리는 동안 GL.invertCulling
// - 반 해상도, 후처리 · 그림자 끔 (값싸게). 3장 거울 방에서만 켬 (RoomBuilder.BuildStage → Ensure)
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CatTang
{
    [RequireComponent(typeof(Camera))]
    public class MirrorReflection : MonoBehaviour
    {
        public static float PlaneY = 0.0f;
        const float ClipOffset = 0.05f;                       // 바닥(0.01) · 바닥 조각(0.02)은 반사에서 뺌
        static readonly int TexId = Shader.PropertyToID("_CatTangMirrorTex");
        static readonly int OnId = Shader.PropertyToID("_CatTangMirrorOn");

        Camera src, refl;
        RenderTexture rt;

        /// <summary>메인 카메라에 반사를 켜고 끔</summary>
        public static void Ensure(bool on)
        {
            var cam = Camera.main;
            if (!cam) { Shader.SetGlobalFloat(OnId, 0); return; }
            var m = cam.GetComponent<MirrorReflection>();
            if (on && !m) m = cam.gameObject.AddComponent<MirrorReflection>();
            if (m) m.enabled = on;
            if (!on) Shader.SetGlobalFloat(OnId, 0);
        }

        void OnEnable()
        {
            src = GetComponent<Camera>();
            RenderPipelineManager.beginCameraRendering += BeginCam;
            RenderPipelineManager.endCameraRendering += EndCam;
        }

        void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= BeginCam;
            RenderPipelineManager.endCameraRendering -= EndCam;
            if (refl) refl.enabled = false;
            Shader.SetGlobalFloat(OnId, 0);
        }

        void OnDestroy()
        {
            if (refl) Destroy(refl.gameObject);
            if (rt) { rt.Release(); Destroy(rt); }
        }

        void Setup()
        {
            int w = Mathf.Max(64, Screen.width / 2), h = Mathf.Max(64, Screen.height / 2);
            if (!rt || rt.width != w || rt.height != h)
            {
                if (rt) { rt.Release(); Destroy(rt); }
                rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { name = "MirrorReflection" };
                if (refl) refl.targetTexture = rt;
            }
            if (!refl)
            {
                var go = new GameObject("MirrorReflectionCam") { hideFlags = HideFlags.DontSave };
                refl = go.AddComponent<Camera>();
                var data = go.AddComponent<UniversalAdditionalCameraData>();
                data.renderPostProcessing = false;
                data.renderShadows = false;
                data.requiresColorTexture = false;
                data.requiresDepthTexture = false;
                refl.targetTexture = rt;
            }
        }

        void LateUpdate()
        {
            if (!src) return;
            Setup();
            refl.CopyFrom(src);
            refl.targetTexture = rt;
            refl.clearFlags = CameraClearFlags.SolidColor;
            refl.backgroundColor = new Color(0, 0, 0, 0);
            refl.depth = src.depth - 1;
            refl.cullingMask = (src.cullingMask | (1 << MirrorLayers.ReflectOnly)) & ~(1 << MirrorLayers.MainOnly) & ~(1 << 5);   // UI 레이어 뺌 · 반사에만 비치는 것(스토리 맵 3 끝의 그녀) 넣음
            refl.enabled = true;

            // 바닥 면 기준 반사 행렬
            float d = -PlaneY;
            var R = new Matrix4x4();
            R.m00 = 1; R.m01 = 0; R.m02 = 0; R.m03 = 0;
            R.m10 = 0; R.m11 = -1; R.m12 = 0; R.m13 = -2 * d;
            R.m20 = 0; R.m21 = 0; R.m22 = 1; R.m23 = 0;
            R.m30 = 0; R.m31 = 0; R.m32 = 0; R.m33 = 1;
            refl.worldToCameraMatrix = src.worldToCameraMatrix * R;
            var p = src.transform.position; p.y = 2 * PlaneY - p.y;
            refl.transform.position = p;

            // 비스듬한 자르기 면: 바닥 위만 그림
            var wtc = refl.worldToCameraMatrix;
            var cpos = wtc.MultiplyPoint(new Vector3(0, PlaneY + ClipOffset, 0));
            var cnorm = wtc.MultiplyVector(Vector3.up).normalized;
            var plane = new Vector4(cnorm.x, cnorm.y, cnorm.z, -Vector3.Dot(cpos, cnorm));
            refl.projectionMatrix = src.CalculateObliqueMatrix(plane);

            Shader.SetGlobalTexture(TexId, rt);
            Shader.SetGlobalFloat(OnId, 1);
        }

        static readonly int PassId = Shader.PropertyToID("_CatTangMirrorPass");
        // 뒤집힌 시점: 면 방향이 반대라 면 가리기를 뒤집고, 툰 외곽선 껍질은 끔 (CatTangToon의 _CatTangMirrorPass)
        void BeginCam(ScriptableRenderContext ctx, Camera cam) { if (cam != refl) return; GL.invertCulling = true; Shader.SetGlobalFloat(PassId, 1); }
        void EndCam(ScriptableRenderContext ctx, Camera cam) { if (cam != refl) return; GL.invertCulling = false; Shader.SetGlobalFloat(PassId, 0); }
    }
}
