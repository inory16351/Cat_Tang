// 발 바닥 맞추기 (2026-11-20 사용자: "시침 보스 공중에 떠 있는 것 같은데")
// 휴머노이드 애니메이션은 발을 애니메이터 오브젝트(프리팹의 "Mesh") 높이에 놓는데, 프리팹들이 T자세 기준으로 Mesh를 올려 둬서
// 걷고 서 있을 때 그만큼 뜸 (재 보니 시침 2.63m · 분침 2.13m · 집행관 2.36m · 기관차 거인 2.8m, 일반 적도 0.2~0.4m).
// 프리팹을 고치면 빌더 메뉴를 다시 돌릴 때 되돌아가므로, 꺼낼 때 한 번 잼: 대기 자세가 두 프레임 돈 뒤 스킨 메시를 구워
// 가장 낮은 정점(발바닥 · 옷자락 끝)이 적 뿌리 높이(바닥)에 오도록 Mesh를 내림. 그 뒤로는 손대지 않음 (도약 · 내려찍기 동작은 그대로)
using UnityEngine;

namespace CatTang
{
    public class GroundFeet : MonoBehaviour
    {
        Animator anim;
        int frames;

        /// <summary>model = 적의 "Model" 자식. 휴머노이드가 아니거나 이미 발 붙이기(FootGround)가 있으면 안 붙임</summary>
        public static void Add(GameObject model)
        {
            if (!model) return;
            var a = model.GetComponentInChildren<Animator>();
            if (!a || !a.avatar || !a.avatar.isHuman || a.GetComponent<FootGround>()) return;
            model.AddComponent<GroundFeet>().anim = a;
        }

        void LateUpdate()
        {
            if (++frames < 3) return;                     // 대기 자세가 실제로 돈 뒤
            enabled = false;
            if (!anim) return;
            var root = transform.parent ? transform.parent : transform;   // 적 뿌리 = 바닥 높이
            float low = float.MaxValue;
            var mesh = new Mesh();
            foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (!smr.enabled || !smr.sharedMesh) continue;
                smr.BakeMesh(mesh, true);
                var m = Matrix4x4.TRS(smr.transform.position, smr.transform.rotation, smr.transform.lossyScale);   // 구운 메시는 부모 배율이 빠져 나옴
                var v = mesh.vertices;
                int step = Mathf.Max(1, v.Length / 20000);
                for (int i = 0; i < v.Length; i += step) low = Mathf.Min(low, m.MultiplyPoint3x4(v[i]).y);
            }
            Destroy(mesh);
            if (low == float.MaxValue) return;
            float gap = low - root.position.y - transform.localPosition.y;   // Model 자체를 들어 올린 연출(도약)은 빼고 잼
            if (Mathf.Abs(gap) < 0.03f) return;
            var t = anim.transform;
            float ps = t.parent ? t.parent.lossyScale.y : 1f;
            t.localPosition -= Vector3.up * gap / Mathf.Max(1e-4f, ps);
        }
    }
}
