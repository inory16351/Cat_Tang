// 탑뷰 카메라: 플레이어를 부드럽게 따라가고 조준 방향으로 살짝 앞을 보여 줌
using UnityEngine;

namespace CatTang
{
    public class CameraFollow : MonoBehaviour
    {
        public Vector3 offset = new Vector3(0, 17f, -11f);
        public float smooth = 8f;

        void LateUpdate()
        {
            var p = PlayerController.Instance;
            if (!p) return;
            var target = p.transform.position + offset;
            transform.position = Vector3.Lerp(transform.position, target, 1 - Mathf.Exp(-smooth * Time.unscaledDeltaTime));
            transform.rotation = Quaternion.LookRotation(p.transform.position + Vector3.up * 0.5f - target, Vector3.up);
        }
    }
}
