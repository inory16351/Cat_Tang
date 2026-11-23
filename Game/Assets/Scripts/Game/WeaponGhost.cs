// 무기 잔상 (2026-11-23 사용자: "템포와 크로노 분침에 잔상 표현 넣어서 스피드 강조")
// 무기 칼끝이 빠르게 움직이는 동안만 무기 실루엣 잔상(Afterimage)을 짧은 간격으로 남김 → 휘두르기 궤적이 겹쳐 보임.
// 등장 연출 · 전투 둘 다 같은 컴포넌트가 처리 (무기 Transform에 붙음)
using UnityEngine;

namespace CatTang
{
    public class WeaponGhost : MonoBehaviour
    {
        public Color color = new(1f, 0.25f, 0.3f, 0.8f);
        public float minSpeed = 7f;          // 칼끝 속도(m/s)가 이보다 빠를 때만
        public float gap = 0.035f;           // 잔상 간격 (초)
        public float life = 0.22f;
        public Vector3 tipLocal = new(0, 1.4f, 0);   // 무기 로컬 칼끝 (Kit 무기는 손잡이가 원점 · 긴 축 +Y)

        Vector3 lastTip; bool has; float next;

        /// <summary>적 모델에서 "Weapon_" + weapon 을 찾아 붙임</summary>
        public static WeaponGhost Attach(Enemy e, string weapon, Color c, float tipLen = 1.4f)
        {
            if (!e) return null;
            foreach (var t in e.GetComponentsInChildren<Transform>(true))
                if (t.name == "Weapon_" + weapon)
                {
                    var g = t.gameObject.GetComponent<WeaponGhost>();
                    if (!g) g = t.gameObject.AddComponent<WeaponGhost>();
                    g.color = c; g.tipLocal = new Vector3(0, tipLen, 0);
                    return g;
                }
            return null;
        }

        void OnEnable() => has = false;

        void LateUpdate()
        {
            var tip = transform.TransformPoint(tipLocal);
            float dt = Mathf.Max(Time.deltaTime, 1e-4f);
            if (has && Time.time >= next && (tip - lastTip).magnitude / dt >= minSpeed)
            {
                Afterimage.Spawn(transform, color, life, false);
                next = Time.time + gap;
            }
            lastTip = tip; has = true;
        }
    }
}
