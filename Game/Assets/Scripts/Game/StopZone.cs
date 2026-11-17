// 2장 신호수+ '정지 구역' (레벨 디자인 md: 터진 자리 반경 2m · 3초, 들어온 탄이 멈췄다가 끝날 때 원래 방향으로 다시 날아감)
// 적 탄(EnemyProjectile)만 대상: 고양이 탄은 즉발(SphereCast)이라 멈출 몸이 없음.
// 표시 = 옅은 청록 원(3초 동안 차오름 = 남은 시간) + 가운데 천천히 도는 시곗바늘 두 개
using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    public class StopZone : MonoBehaviour
    {
        public static readonly Color Tint = new(0.55f, 0.85f, 1f);

        float radius, until;
        readonly HashSet<EnemyProjectile> held = new();
        Transform hands;

        public static StopZone Spawn(Vector3 at, float radius, float life)
        {
            var go = new GameObject("StopZone");
            go.transform.position = at;
            var z = go.AddComponent<StopZone>();
            z.radius = radius;
            z.until = Time.time + life;
            Telegraph.Ring(at, radius, life, Tint).NoGlint();
            GunkataFx.Flare(at + Vector3.up * 0.8f, radius * 0.9f, Tint);
            z.BuildHands();
            return z;
        }

        void BuildHands()
        {
            // 바닥에 누운 시곗바늘 (긴 것 · 짧은 것)
            hands = new GameObject("Hands").transform;
            hands.SetParent(transform, false);
            hands.localPosition = Vector3.up * 0.06f;
            var m = RoomBuilder.Mat(Tint, 0f);
            for (int i = 0; i < 2; i++)
            {
                var h = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(h.GetComponent<Collider>());
                h.transform.SetParent(hands, false);
                float len = radius * (i == 0 ? 0.8f : 0.5f);
                h.transform.localRotation = Quaternion.Euler(0, i * 120f, 0);
                h.transform.localPosition = h.transform.localRotation * new Vector3(0, 0, len / 2);
                h.transform.localScale = new Vector3(0.09f, 0.02f, len);
                h.GetComponent<Renderer>().sharedMaterial = m;
                h.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        void Update()
        {
            if (hands) hands.Rotate(0, 25f * Time.deltaTime, 0);      // 거의 멈춘 듯 느리게
            var c = transform.position;
            foreach (var b in EnemyProjectile.All)
            {
                if (!b || held.Contains(b)) continue;
                if (Effects.Flat(b.transform.position - c).magnitude > radius) continue;
                held.Add(b);
                b.frozenUntil = until;
                FxBurst.Spawn(b.transform.position, 0.5f, FxBurst.Kind.Spark, Tint);
            }
            if (Time.time < until) return;
            // 풀림: 멈췄던 탄이 원래 방향으로 다시 날아감 (EnemyProjectile이 frozenUntil이 지나면 알아서 움직임)
            foreach (var b in held) if (b) FxBurst.Spawn(b.transform.position, 0.6f, FxBurst.Kind.Spark, Tint);
            Destroy(gameObject);
        }
    }
}
