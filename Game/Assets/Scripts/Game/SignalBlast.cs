// 2장 신호수가 던지는 신호등 (레벨 디자인 md: 노랑 1.5초 → 빨강 폭발 반경 2m · 피해 12)
// 손에서 포물선으로 0.35초 날아가 바닥에 박힘 → 노란 원 + 노란 등 1.5초 (끝 0.35초는 빨강으로 바뀌며 빠르게 깜빡임) → 폭발.
// 던진 신호수가 죽어도 이미 던진 신호등은 터짐 (방 일부를 잠깐 봉쇄). 강화형이면 터진 자리에 정지 구역(StopZone)
using UnityEngine;

namespace CatTang
{
    public class SignalBlast : MonoBehaviour
    {
        public const float Flight = 0.35f, RedTime = 0.35f;
        public static readonly Color Yellow = new(1f, 0.82f, 0.15f);
        public static readonly Color Red = new(1f, 0.12f, 0.15f);

        Vector3 from, to;
        float born, damage, radius = Enemy.SignalRadius, fuse = Enemy.SignalFuse;
        bool stopZone, landed;
        Telegraph ring;
        Light glow;
        Material lampMat;
        Transform lamp;

        /// <summary>radius · fuse를 안 주면 기획서 기본값(반경 2m · 노랑 1.5초). 건널목 차단 · 따라오는 신호는 작고 빠르게</summary>
        public static SignalBlast Throw(Vector3 from, Vector3 to, float damage, bool stopZone, float radius = Enemy.SignalRadius, float fuse = Enemy.SignalFuse)
        {
            var go = new GameObject("SignalBlast");
            go.transform.position = from;
            var s = go.AddComponent<SignalBlast>();
            s.from = from; s.to = to; s.damage = damage; s.stopZone = stopZone; s.born = Time.time; s.radius = radius; s.fuse = fuse;
            // 예고 원은 던지는 순간부터 착지 자리에 (노랑, 끝나기 직전 반짝임은 Telegraph가 띄움)
            s.ring = Telegraph.Ring(to, radius, Flight + fuse, Yellow);
            s.Build();
            return s;
        }

        void Build()
        {
            // 작은 신호등: 검은 기둥 + 둥근 등 (재질은 인스턴스 — 색이 바뀜)
            lamp = new GameObject("Lamp").transform;
            lamp.SetParent(transform, false);
            var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(post.GetComponent<Collider>());
            post.transform.SetParent(lamp, false);
            post.transform.localPosition = new Vector3(0, 0.22f, 0);
            post.transform.localScale = new Vector3(0.14f, 0.22f, 0.14f);
            post.GetComponent<Renderer>().sharedMaterial = RoomBuilder.Mat(new Color(0.16f, 0.13f, 0.12f), 2f);
            var bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(bulb.GetComponent<Collider>());
            bulb.transform.SetParent(lamp, false);
            bulb.transform.localPosition = new Vector3(0, 0.55f, 0);
            bulb.transform.localScale = Vector3.one * 0.34f;
            lampMat = new Material(RoomBuilder.Mat(Yellow, 2f));
            bulb.GetComponent<Renderer>().sharedMaterial = lampMat;
            var lg = new GameObject("Glow");
            lg.transform.SetParent(lamp, false);
            lg.transform.localPosition = new Vector3(0, 0.9f, 0);
            glow = lg.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.color = Yellow;
            glow.range = radius * 2f;
            glow.intensity = 2.5f;
            glow.shadows = LightShadows.None;
        }

        void Update()
        {
            float age = Time.time - born;
            if (!landed)
            {
                float t = Mathf.Clamp01(age / Flight);
                transform.position = Vector3.Lerp(from, to, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 2.2f;
                lamp.localRotation = Quaternion.Euler(t * 540f, 0, 0);
                if (t < 1f) return;
                landed = true;
                transform.position = to;
                lamp.localRotation = Quaternion.identity;
                FxBurst.Spawn(to + Vector3.up * 0.1f, 0.9f, FxBurst.Kind.Dust, new Color(0.7f, 0.62f, 0.55f));
            }
            float left = Flight + fuse - age;
            bool red = left <= RedTime;
            // 노랑은 느리게, 빨강은 빠르게 깜빡임
            float blink = 0.5f + 0.5f * Mathf.Sin(age * (red ? 55f : 9f));
            var c = red ? Red : Yellow;
            if (lampMat) lampMat.SetColor("_BaseColor", Color.Lerp(c * 0.55f, c * 1.3f, blink));
            if (glow) { glow.color = c; glow.intensity = Mathf.Lerp(1.2f, red ? 5f : 3f, blink); }
            if (red && ring) ring.SetColor(Telegraph.Danger);
            if (left > 0) return;
            Explode();
        }

        void Explode()
        {
            var at = transform.position;
            GunkataFx.Explosion(at, radius, false, Red);
            CameraFollow.Shake(0.1f);
            var p = PlayerController.Instance;
            if (p && p.health.Alive && Effects.Flat(p.transform.position - at).magnitude <= radius) p.health.TakeDamage(damage);
            PropHit.Area(at, radius, damage);
            if (stopZone) StopZone.Spawn(at, Enemy.SignalRadius, Enemy.StopZoneLife);
            Destroy(gameObject);
        }

        void OnDestroy()
        {
            if (lampMat) Destroy(lampMat);
        }
    }
}
