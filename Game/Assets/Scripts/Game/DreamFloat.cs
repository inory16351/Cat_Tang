// 마을의 떠 있는 것들 (2026-11-18 사용자: 곳곳에 몽환적 · 부서진 시간 느낌) — 공중에 멈춘 톱니 · 깨진 시계판 · 계단 조각 · 조각 섬이
// 아주 천천히 오르내리고 돎. 빌더(VillageKitBuilder)가 씬에 놓을 때 붙임. 시간 멈춤 느낌이 나게 느리게
using UnityEngine;

namespace CatTang
{
    public class DreamFloat : MonoBehaviour
    {
        public float bob = 0.35f;          // 오르내리는 폭 (m)
        public float period = 7f;          // 한 번 오르내리는 데 걸리는 초
        public float spin;                 // 초당 회전 (도, Y축)
        public float tilt = 2f;            // 살짝 기우뚱 (도)
        Vector3 basePos; Quaternion baseRot; float phase;

        void Start()
        {
            basePos = transform.localPosition;
            baseRot = transform.localRotation;
            phase = (transform.position.x * 0.37f + transform.position.z * 0.61f) % 6.28f;   // 저마다 다른 박자
        }

        void Update()
        {
            float t = Time.time * Mathf.PI * 2f / Mathf.Max(0.1f, period) + phase;
            transform.localPosition = basePos + Vector3.up * Mathf.Sin(t) * bob;
            transform.localRotation = baseRot * Quaternion.Euler(Mathf.Sin(t * 0.7f) * tilt, Time.time * spin, Mathf.Cos(t * 0.5f) * tilt);
        }
    }
}
