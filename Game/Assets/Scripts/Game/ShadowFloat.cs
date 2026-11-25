// 스토리 맵 그림자 인물 = 떠다니는 유령 (2026-11-25 사용자: 다리 대신 아래로 길게 늘어져 녹아 사라지는 꼬리, 미끄러지듯 떠다님 — 1장 광장 · 2장 승강장)
//   꼬리 디졸브는 셰이더(CatTang/GhostFade, 정점 알파). 불꽃은 뺌 (2026-11-25 사용자)
//   모델(자식 "Mesh")만 띄워 흔듦: 땅에서 살짝 떠 천천히 위아래로 출렁 · 숨 쉬듯 미세하게 부풂
//   움직일 땐 가는 쪽으로 숙이고 조금 더 떠오르며, 방향을 틀면 몸이 바깥쪽으로 기움. 루트 위치 · 방향은 Walk 코루틴이 잡음
using UnityEngine;

namespace CatTang
{
    public class ShadowFloat : MonoBehaviour
    {
        public float hover = 0.05f, bob = 0.06f, bobSpeed = 1.7f;  // 뜬 높이 · 출렁임 (초당 라디안) — 꼬리가 땅 아래까지 길어 몸은 이미 떠 있음
        public float lean = 9f, bank = 7f, rise = 0.08f;          // 움직일 때 숙임 · 기울기 · 더 떠오름

        Transform body;
        Vector3 bodyPos0, bodyScale0;
        Quaternion bodyRot0;
        Vector3 lastPos;
        float lastYaw, moving, turn, seed;

        void Awake()
        {
            body = transform.Find("Mesh") ?? (transform.childCount > 0 ? transform.GetChild(0) : null);
            if (body) { bodyPos0 = body.localPosition; bodyRot0 = body.localRotation; bodyScale0 = body.localScale; }
            lastPos = transform.position;
            lastYaw = transform.eulerAngles.y;
            seed = Random.value * 10f;
        }

        void LateUpdate()
        {
            if (!body) return;
            float dt = Mathf.Max(Time.deltaTime, 1e-4f);
            var d = transform.position - lastPos; d.y = 0;
            lastPos = transform.position;
            moving = Mathf.MoveTowards(moving, d.magnitude / dt > 0.2f ? 1f : 0f, dt * 3f);   // 미끄러지듯 천천히 섞음
            float yaw = transform.eulerAngles.y;
            turn = Mathf.Lerp(turn, Mathf.Clamp(Mathf.DeltaAngle(lastYaw, yaw) / dt / 90f, -1f, 1f), dt * 4f);
            lastYaw = yaw;

            float t = Time.time * bobSpeed + seed;
            float y = hover + Mathf.Sin(t) * bob + rise * moving;
            body.localPosition = bodyPos0 + Vector3.up * y;
            body.localRotation = Quaternion.Euler(lean * moving + Mathf.Sin(t * 0.5f) * 1.5f, 0, -turn * bank + Mathf.Sin(t * 0.7f) * 1.2f) * bodyRot0;
            float breathe = Mathf.Sin(t * 0.9f) * 0.012f;
            body.localScale = Vector3.Scale(bodyScale0, new Vector3(1f - breathe * 0.5f, 1f + breathe, 1f - breathe * 0.5f));
        }
    }
}
