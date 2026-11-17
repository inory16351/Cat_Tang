// 발 붙이기: 애니메이션 뒤(LateUpdate) 가장 낮은 발 뼈가 기본 자세 높이에 오도록 모델을 위아래로 맞춤.
// 휴머노이드 클립을 다른 체형(짐꾼)에 돌려 쓰면 무릎을 굽힌 만큼 몸이 떠 보임 → 디딤발 기준으로 내려 줌.
// 빌더(PorterRig)가 프리팹의 "Mesh"(애니메이터가 있는 오브젝트)에 붙이고 발 뼈 · 기본 높이를 넣어 둠.
using UnityEngine;

namespace CatTang
{
    public class FootGround : MonoBehaviour
    {
        public Transform[] feet;
        public float restY;              // 기본 자세에서 가장 낮은 발 뼈 높이 (부모 공간)
        Vector3 basePos;
        float offset;
        bool init;

        void LateUpdate()
        {
            if (feet == null || feet.Length == 0 || !transform.parent) return;
            if (!init) { basePos = transform.localPosition; init = true; }
            float low = float.MaxValue;
            foreach (var f in feet) if (f) low = Mathf.Min(low, transform.parent.InverseTransformPoint(f.position).y);
            if (low == float.MaxValue) return;
            offset += restY - low;
            offset = Mathf.Clamp(offset, -2f, 2f);
            transform.localPosition = basePos + Vector3.up * offset;
        }
    }
}
