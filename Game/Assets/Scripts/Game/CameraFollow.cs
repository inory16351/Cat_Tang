// 탑뷰 카메라: 플레이어를 부드럽게 따라가고 조준 방향으로 살짝 앞을 보여 줌
using UnityEngine;

namespace CatTang
{
    public class CameraFollow : MonoBehaviour
    {
        public Vector3 offset = new Vector3(0, 17f, -11f);
        public float smooth = 8f;
        static float shake, shakeUntil, zoom = 1f, zoomTarget = 1f;

        /// <summary>카메라 당기기 (1 = 기본, 0.5 = 절반 거리). 궁극기 연출용</summary>
        public static void Zoom(float target) => zoomTarget = target;

        /// <summary>화면 흔들림 (세기, 0.15초)</summary>
        public static void Shake(float amount) { shake = Mathf.Max(amount, Time.unscaledTime < shakeUntil ? shake : 0); shakeUntil = Time.unscaledTime + 0.15f; }

        void LateUpdate()
        {
            var p = PlayerController.Instance;
            if (!p) return;
            zoom = Mathf.Lerp(zoom, zoomTarget, 1 - Mathf.Exp(-10f * Time.unscaledDeltaTime));
            var target = p.transform.position + offset * zoom;
            transform.position = Vector3.Lerp(transform.position, target, 1 - Mathf.Exp(-smooth * Time.unscaledDeltaTime));
            transform.rotation = Quaternion.LookRotation(p.transform.position + Vector3.up * 0.5f - target, Vector3.up);
            if (Time.unscaledTime < shakeUntil) transform.position += Random.insideUnitSphere * shake;
        }
    }
}
