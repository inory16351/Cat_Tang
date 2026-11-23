// 이동 잔상 (2026-11-23 사용자: "분침 이동에 잔상 이동 표현 넣어서 스피드 액션감 좀 더 살려")
// 적 뿌리가 minSpeed보다 빠르게 움직이는 동안 몸 실루엣 잔상(Afterimage)을 줄지어 남김 — 빠를수록 촘촘하고 길게.
// 분침 템포: 돌진(초속 20) · 순간 이동 · 등장 연출의 낙하 · 자리 바꾸기에서 붉은 잔상 줄 + 가끔 흰 잔상(번쩍)으로 속도감
using UnityEngine;

namespace CatTang
{
    public class MotionGhost : MonoBehaviour
    {
        public Transform model;
        public Color color = new(1f, 0.2f, 0.26f, 0.7f);
        public Color flash = new(1f, 1f, 1f, 0.45f);
        public float minSpeed = 6.5f;        // 걷기(4.2)는 빼고 달리기 · 돌진만
        public float life = 0.28f;

        Vector3 last; bool has; float next; int n;

        public static MotionGhost Attach(Enemy e, Color c, float minSpeed = 6.5f)
        {
            if (!e) return null;
            var m = e.transform.Find("Model");
            if (!m) return null;
            var g = e.gameObject.AddComponent<MotionGhost>();
            g.model = m; g.color = c; g.minSpeed = minSpeed;
            return g;
        }

        void LateUpdate()
        {
            var p = transform.position;
            float dt = Time.deltaTime;
            if (!has || dt <= 0f) { last = p; has = true; return; }
            float speed = (p - last).magnitude / dt;
            last = p;
            if (!model || speed < minSpeed || Time.time < next) return;
            // 빠를수록 간격은 좁게 (6.5 m/s → 0.06초, 20 m/s → 0.025초), 잔상 수명은 조금 길게
            float k = Mathf.InverseLerp(minSpeed, 20f, speed);
            next = Time.time + Mathf.Lerp(0.06f, 0.025f, k);
            bool white = ++n % 4 == 0;
            Afterimage.Spawn(model, white ? flash : color, Mathf.Lerp(life, life * 1.5f, k) * (white ? 0.6f : 1f), false);
        }
    }
}
