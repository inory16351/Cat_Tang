// 불릿 파티 영역: 플레이어를 따라다니는 원형 트리거 콜라이더.
// - 판정: SphereCollider(트리거) + 키네마틱 Rigidbody. 안에 들어온 적(Health) 전부가 매 타 피해 대상.
// - 연출: 타마다 몸에서 금빛 탄피가 튀어 오름. (원 경계 이펙트 없음. 범위는 원 안에 떨어지는 총알 비로 보임)
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
        ParticleSystem casings, debris;
        static Material debrisMat;
        bool closed;

        static Material mat;
        static readonly Color Hot = new(1f, 0.96f, 0.78f);

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
            z.BuildDebris();
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
        /// <summary>한 타: 탄피 + 사방으로 튀는 파편 · 불티</summary>
        public void Pulse(int shot)
        {
            if (casings) casings.Emit(10);
            if (debris) debris.Emit(16);
        }

        /// <summary>마무리: 탄피 · 파편을 한 움큼 쏟고 닫힘</summary>
        public void Close()
        {
            if (closed) return;
            closed = true;
            if (casings) casings.Emit(30);
            if (debris) debris.Emit(40);
            Destroy(gameObject, 1.0f);
        }

        /// <summary>파편 · 불티: 검은 조각과 붉은 불티가 몸에서 사방 수평으로 튀어 나감 (속도 방향으로 늘어진 선)</summary>
        void BuildDebris()
        {
            if (!debrisMat) debrisMat = new Material(Shader.Find("Sprites/Default"));
            var g = new GameObject("Debris");
            g.transform.SetParent(transform, false);
            g.transform.localPosition = Vector3.up * 1.0f;
            g.transform.localRotation = Quaternion.Euler(90, 0, 0);          // 원 모양 방출을 바닥과 나란히
            debris = g.AddComponent<ParticleSystem>();
            debris.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = debris.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 12f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.11f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.16f, 0.09f, 0.08f), new Color(1f, 0.42f, 0.16f));
            main.gravityModifier = 0.8f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 500;
            var em = debris.emission; em.rateOverTime = 0;
            var sh = debris.shape; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 0.5f; sh.randomDirectionAmount = 0.15f;
            var col = debris.colorOverLifetime; col.enabled = true;
            var gr = new Gradient();
            gr.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                       new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 0.6f), new GradientAlphaKey(0, 1) });
            col.color = gr;
            var pr = g.GetComponent<ParticleSystemRenderer>();
            pr.sharedMaterial = debrisMat;
            pr.renderMode = ParticleSystemRenderMode.Stretch;
            pr.velocityScale = 0.03f;
            pr.lengthScale = 2f;
            pr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        void BuildCasings()
        {
            if (!mat)
            {
                mat = new Material(Shader.Find("Sprites/Default"));
                var tex = Resources.Load<Texture2D>("FX/casing");          // 툰 탄피 (docs/tools/fx_sprites.py)
                if (tex) mat.mainTexture = tex;
            }
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
            main.startColor = new ParticleSystem.MinMaxGradient(Color.white, Hot);   // 텍스처가 이미 놋쇠색
            main.gravityModifier = 2.2f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 400;
            var em = casings.emission; em.rateOverTime = 0;
            var sh = casings.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 55; sh.radius = 0.35f;
            var rot = casings.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-15f, 15f);
            var col = casings.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
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
