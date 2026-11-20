// 떠다니는 적 (2026-11-20 사용자: "막보 떠다니는 거면 떠다니는 느낌을 줘, 왜 걸어가냐") — 시간의 주인
// 모델(Model)을 바닥에서 height만큼 띄우고 느린 숨처럼 위아래 둥실 (주기가 다른 사인 두 개), 이동하면 그쪽으로 앞으로 기울고 돌면 옆으로 기욺 (관성처럼 늦게 따라감)
// Enemy.Animate가 매 프레임 모델 위치 · 기울기를 0으로 되돌리므로 LateUpdate에서 다시 놓음
using UnityEngine;

namespace CatTang
{
    public class HoverFloat : MonoBehaviour
    {
        public float height = 0.7f, bob = 0.32f, period = 2.8f, maxTilt = 15f;
        Transform m;
        Quaternion baseRot;
        Vector3 lastPos, vel;
        float t, tiltF, tiltS;

        public static HoverFloat Attach(Enemy e)
        {
            if (!e) return null;
            var h = e.GetComponent<HoverFloat>();
            if (!h) h = e.gameObject.AddComponent<HoverFloat>();
            return h;
        }

        void Start()
        {
            m = transform.Find("Model");
            if (m) baseRot = m.localRotation;
            lastPos = transform.position;
            t = Random.value * period;
        }

        void LateUpdate()
        {
            if (!m) return;
            float dt = Mathf.Max(1e-4f, Time.deltaTime);
            var v = Effects.Flat(transform.position - lastPos) / dt;
            lastPos = transform.position;
            if (v.magnitude > 40f) v = Vector3.zero;                                  // 순간 이동은 무시
            vel = Vector3.Lerp(vel, v, 1 - Mathf.Exp(-4f * dt));
            var local = transform.InverseTransformDirection(vel);
            tiltF = Mathf.Lerp(tiltF, Mathf.Clamp(local.z * 3.2f, -maxTilt, maxTilt), 1 - Mathf.Exp(-3f * dt));
            tiltS = Mathf.Lerp(tiltS, Mathf.Clamp(-local.x * 3.2f, -maxTilt, maxTilt), 1 - Mathf.Exp(-3f * dt));
            t += dt;
            float w = Mathf.PI * 2 / period;
            float y = height + bob * Mathf.Sin(w * t) + bob * 0.3f * Mathf.Sin(w * 2.3f * t + 1.1f);
            m.localPosition = Vector3.up * y;
            m.localRotation = Quaternion.Euler(tiltF + 2f * Mathf.Sin(w * t + 0.8f), 0, tiltS + 2.5f * Mathf.Sin(w * 0.7f * t)) * baseRot;
        }
    }
}
