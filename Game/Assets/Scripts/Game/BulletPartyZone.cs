// 불릿 파티 영역: 플레이어를 따라다니는 원형 트리거 콜라이더.
// - 판정: SphereCollider(트리거) + 키네마틱 Rigidbody. 안에 들어온 적(Health) 전부가 매 타 피해 대상.
// - 연출 (바닥 표시 없이 공중 이펙트만으로 사거리를 보여 줌):
//   타마다 원 경계를 따라 탄착 불꽃 고리가 한 바퀴 튀고(조금씩 돌아감), 몸에서 금빛 탄피가 튀어 오름.
//   열릴 때는 경계에 금빛 섬광 고리, 닫힐 때는 경계 전체가 폭발하며 사라짐.
using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    [RequireComponent(typeof(SphereCollider), typeof(Rigidbody))]
    public class BulletPartyZone : MonoBehaviour
    {
        public float radius = 11f;
        Transform follow;
        Rigidbody rb;
        readonly HashSet<Health> inside = new();
        ParticleSystem casings;
        bool closed;

        static Material mat;
        static readonly Color Gold = new(1f, 0.8f, 0.32f), Hot = new(1f, 0.96f, 0.78f);
        const int EdgePoints = 20;          // 경계 고리 이펙트 수 (반지름 6.5m 기준 약 2m 간격)

        public static BulletPartyZone Spawn(Transform follow, float radius)
        {
            var go = new GameObject("BulletPartyZone");
            go.transform.position = follow.position;
            var col = go.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = radius;
            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.sleepThreshold = 0;          // 플레이어가 가만히 서 있어도 들어오는 적을 계속 감지
            var z = go.AddComponent<BulletPartyZone>();
            z.follow = follow;
            z.radius = radius;
            z.rb = rb;
            z.BuildCasings();
            z.EdgeRing(0, FxBurst.Kind.Gold, 1.6f, Gold, 0.35f);
            return z;
        }

        // ---------- 판정 ----------
        void OnTriggerEnter(Collider c) => Track(c);
        void OnTriggerStay(Collider c) => Track(c);
        void OnTriggerExit(Collider c)
        {
            var h = c.GetComponentInParent<Health>();
            if (h) inside.Remove(h);
        }
        void Track(Collider c)
        {
            var h = c.GetComponentInParent<Health>();
            if (h && !h.isPlayer && h.Alive) inside.Add(h);
        }

        /// <summary>지금 원 안에 있는 살아 있는 적 (복사본)</summary>
        public List<Health> Targets()
        {
            inside.RemoveWhere(h => !h || !h.Alive);
            return new List<Health>(inside);
        }

        /// <summary>원 안 아무 점 (바닥 높이)</summary>
        public Vector3 RandomPoint(float minFrac = 0.15f)
        {
            float a = Random.Range(0f, Mathf.PI * 2), r = radius * Mathf.Sqrt(Random.Range(minFrac * minFrac, 1f));
            return transform.position + new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r);
        }

        // ---------- 연출 ----------
        /// <summary>한 타: 경계 불꽃 고리 (타마다 조금씩 돌아 회전하는 느낌) + 탄피</summary>
        public void Pulse(int shot)
        {
            EdgeRing(shot * 5f, FxBurst.Kind.Spark, 1.25f, Hot, 0.55f);
            if (shot % 2 == 0) EdgeRing(shot * 5f + 360f / EdgePoints * 0.5f, FxBurst.Kind.Muzzle, 1.1f, Gold, 1.0f);
            if (casings) casings.Emit(10);
        }

        /// <summary>마무리: 경계 전체가 터지며 닫힘</summary>
        public void Close()
        {
            if (closed) return;
            closed = true;
            EdgeRing(0, FxBurst.Kind.Explosion, 2.2f, Gold, 0.6f);
            EdgeRing(360f / EdgePoints * 0.5f, FxBurst.Kind.Gold, 1.8f, Hot, 1.0f);
            if (casings) casings.Emit(30);
            Destroy(gameObject, 1.0f);
        }

        /// <summary>원 경계를 따라 공중 이펙트를 한 바퀴</summary>
        void EdgeRing(float offsetDeg, FxBurst.Kind kind, float size, Color tint, float height)
        {
            var c = transform.position;
            for (int i = 0; i < EdgePoints; i++)
            {
                float a = (offsetDeg + i * 360f / EdgePoints) * Mathf.Deg2Rad;
                var p = c + new Vector3(Mathf.Cos(a) * radius, height + Random.Range(-0.15f, 0.25f), Mathf.Sin(a) * radius);
                FxBurst.Spawn(p, size * Random.Range(0.85f, 1.15f), kind, tint);
            }
        }

        void BuildCasings()
        {
            if (!mat) mat = new Material(Shader.Find("Sprites/Default"));
            var c = new GameObject("Casings");
            c.transform.SetParent(transform, false);
            c.transform.localPosition = Vector3.up * 1.1f;
            c.transform.localRotation = Quaternion.Euler(-90, 0, 0);
            casings = c.AddComponent<ParticleSystem>();
            casings.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = casings.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 8f);
            main.startSize3D = true;
            main.startSizeX = new ParticleSystem.MinMaxCurve(0.045f, 0.06f);   // 길쭉한 탄피 모양
            main.startSizeY = new ParticleSystem.MinMaxCurve(0.11f, 0.15f);
            main.startSizeZ = 1f;
            main.startRotation = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
            main.startColor = new ParticleSystem.MinMaxGradient(Gold, Hot);
            main.gravityModifier = 2.2f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 400;
            var em = casings.emission; em.rateOverTime = 0;
            var sh = casings.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 55; sh.radius = 0.35f;
            var rot = casings.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-15f, 15f);
            var col = casings.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Hot, 0), new GradientColorKey(Gold, 1) },
                      new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 0.7f), new GradientAlphaKey(0, 1) });
            col.color = g;
            var pr = c.GetComponent<ParticleSystemRenderer>();
            pr.sharedMaterial = mat;
            pr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // ---------- 따라가기 ----------
        void Follow()
        {
            if (!follow) return;
            var p = follow.position;
            if (rb) rb.MovePosition(p);
            transform.position = p;
        }

        void FixedUpdate() => Follow();

        void LateUpdate()
        {
            if (!follow) { Destroy(gameObject); return; }
            if (!closed) Follow();          // 닫힌 뒤엔 그 자리에 남아 탄피만 떨어짐
        }
    }
}
