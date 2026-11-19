// 탑뷰 카메라: 플레이어를 부드럽게 따라가고 조준 방향으로 살짝 앞을 보여 줌
using UnityEngine;

namespace CatTang
{
    public class CameraFollow : MonoBehaviour
    {
        public Vector3 offset = new Vector3(0, 17f, -11f);
        public float smooth = 8f;
        static float shake, shakeUntil, zoom = 1f, zoomTarget = 1f;
        static Vector3? focus;                                        // 연출: 플레이어 대신 이 자리를 봄 (마을 다리 이어짐 등)

        /// <summary>플레이어 대신 한 자리를 비춤 (null = 다시 플레이어). zoom = 그동안 거리 배율 (0이면 그대로)</summary>
        public static void FocusOn(Vector3? at, float zoomMul) { focus = at; if (zoomMul > 0) zoomTarget = zoomMul; else zoomTarget = 1f; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { focus = null; zoom = zoomTarget = 1f; }       // 도메인 리로드를 꺼 둬서 플레이 사이에 남지 않게

        /// <summary>카메라 당기기 (1 = 기본, 0.5 = 절반 거리). 궁극기 연출용</summary>
        public static void Zoom(float target) => zoomTarget = target;
        /// <summary>바로 그 거리로 (보간 없이)</summary>
        public static void SnapZoom(float z) { zoom = zoomTarget = z; foreach (var c in FindObjectsByType<CameraFollow>(FindObjectsSortMode.None)) c.Snap(); }
        void Snap() { var p = PlayerController.Instance; if (p) transform.position = p.transform.position + offset * zoom; }

        /// <summary>화면 흔들림 (세기, 0.15초)</summary>
        public static void Shake(float amount) { shake = Mathf.Max(amount, Time.unscaledTime < shakeUntil ? shake : 0); shakeUntil = Time.unscaledTime + 0.15f; }

        void LateUpdate()
        {
            var p = PlayerController.Instance;
            if (!p) return;
            zoom = Mathf.Lerp(zoom, zoomTarget, 1 - Mathf.Exp(-10f * Time.unscaledDeltaTime));
            var look = focus ?? p.transform.position;
            var target = look + offset * zoom;
            transform.position = Vector3.Lerp(transform.position, target, 1 - Mathf.Exp(-(focus.HasValue ? 3f : smooth) * Time.unscaledDeltaTime));
            transform.rotation = Quaternion.LookRotation(look + Vector3.up * 0.5f - target, Vector3.up);
            if (Time.unscaledTime < shakeUntil) transform.position += Random.insideUnitSphere * shake;
        }
    }
}
