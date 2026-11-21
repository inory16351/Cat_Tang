using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    // A low, moving impact edge describes the reach of the hit while the weapon trail follows the blade.
    public sealed class MeleeReachFx : MonoBehaviour, IPooled
    {
        enum Shape { Sweep, Impact, Lane, Thrust, GroundRing, RisingArcs }
        const string PoolKey = "MeleeReachFx";
        const int Segments = 48;
        const int SlamArcCount = 6;
        internal const float SlamWaveTravel = 0.18f;
        static readonly System.Func<GameObject> factory = Create;
        readonly List<Vector3> points = new(Segments * 2 + 2);
        readonly List<Vector2> uv = new(Segments * 2 + 2);
        readonly List<Color> colors = new(Segments * 2 + 2);
        readonly List<int> indices = new(Segments * 6);
        MaterialPropertyBlock properties;
        static readonly int TintId = Shader.PropertyToID("_Color"), SeedId = Shader.PropertyToID("_Seed");
        Mesh mesh;
        MeshRenderer meshRenderer;
        Shape shape;
        float reach, halfSpan, travel, lifetime, born, thrustStart, ringStart, arcHeight;
        bool reverse, followDash;

        public static void Sweep(Vector3 at, Vector3 dir, float radius, float halfAngle, Color color, bool reverse = false)
            => Spawn(Shape.Sweep, at, dir, radius, halfAngle, 0.095f, 0.29f, color, reverse);

        public static void Impact(Vector3 at, Vector3 dir, float radius, float halfAngle, Color color)
            => Spawn(Shape.Impact, at, dir, radius, halfAngle, 0.12f, 0.32f, color, false);

        public static void SlamShockwave(Vector3 center, float bodyRadius, float radius, Color color, bool heavy)
        {
            var dust = StylizedFx.Gold.With(opacity: 0.2f, scale: new Vector2(8f, 3f));
            dust.core = new Color(0.42f, 0.28f, 0.15f);
            dust.mid = new Color(0.29f, 0.19f, 0.11f);
            dust.edge = new Color(0.15f, 0.10f, 0.07f);
            StylizedFx.Spawn("ring", center + Vector3.up * 0.04f, bodyRadius, radius, 0.26f, dust);

            Spawn(Shape.GroundRing, center, Vector3.forward, radius,
                180f, SlamWaveTravel, 0.32f, color * (heavy ? 1.60f : 1.40f), false, ringStart: bodyRadius);
            var arcColor = Color.Lerp(color, new Color(1f, 0.82f, 0.52f), 0.36f) * 1.32f;
            Spawn(Shape.RisingArcs, center, Quaternion.AngleAxis(15f, Vector3.up) * Vector3.forward, radius,
                180f, SlamWaveTravel, 0.30f, arcColor, false, ringStart: bodyRadius, arcHeight: heavy ? 0.84f : 0.69f);
            Spawn(Shape.GroundRing, center, Vector3.forward, Mathf.Lerp(bodyRadius, radius, 0.82f),
                180f, 0.22f, 0.32f, color * 0.74f, false, ringStart: bodyRadius, delay: 0.06f);
        }

        public static void Lane(Vector3 at, Vector3 dir, float length, float width, float travelTime, Color color, bool followDash = false)
            => Spawn(Shape.Lane, at, dir, length, width * 0.5f, travelTime, travelTime + 0.21f, color, false, followDash);

        public static void Thrust(Vector3 at, Vector3 dir, float start, float length, float width, float travelTime, Color color)
            => Spawn(Shape.Thrust, at, dir, length, width * 0.5f, travelTime, travelTime + 0.14f, color, false, thrustStart: start);

        static void Spawn(Shape shape, Vector3 at, Vector3 direction, float reach, float halfSpan,
                          float travel, float lifetime, Color color, bool reverse, bool followDash = false,
                          float thrustStart = 0f, float ringStart = 0f, float delay = 0f, float arcHeight = 0f)
        {
            if (reach <= 0f || halfSpan <= 0f) return;
            var effect = GameObjectPool.Get(PoolKey, factory).GetComponent<MeleeReachFx>();
            var forward = Effects.Flat(direction);
            effect.transform.SetPositionAndRotation(shape == Shape.Thrust ? at : at + Vector3.up * 0.09f,
                Quaternion.LookRotation(forward.sqrMagnitude > 0.0001f ? forward : Vector3.forward));
            effect.shape = shape;
            effect.reach = reach;
            effect.halfSpan = shape == Shape.Lane || shape == Shape.Thrust ? halfSpan : Mathf.Min(halfSpan, 180f);
            effect.travel = Mathf.Max(0.04f, travel);
            effect.lifetime = lifetime;
            effect.reverse = reverse;
            effect.followDash = followDash;
            effect.thrustStart = thrustStart;
            effect.ringStart = ringStart;
            effect.arcHeight = arcHeight;
            effect.born = Time.time + delay;
            effect.properties.SetColor(TintId, color);
            effect.properties.SetFloat(SeedId, Random.value);
            effect.meshRenderer.SetPropertyBlock(effect.properties);
            if (delay <= 0f) effect.Draw(0f);
        }

        static GameObject Create()
        {
            var go = new GameObject(PoolKey);
            var effect = go.AddComponent<MeleeReachFx>();
            effect.properties = new MaterialPropertyBlock();
            effect.mesh = new Mesh { name = PoolKey };
            effect.mesh.MarkDynamic();
            go.AddComponent<MeshFilter>().sharedMesh = effect.mesh;
            effect.meshRenderer = go.AddComponent<MeshRenderer>();
            effect.meshRenderer.sharedMaterial = BladeStreak.RibbonMaterial;
            effect.meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            effect.meshRenderer.receiveShadows = false;
            for (int i = 0; i < Segments; i++)
            {
                int v = i * 2;
                effect.indices.Add(v);
                effect.indices.Add(v + 2);
                effect.indices.Add(v + 3);
                effect.indices.Add(v);
                effect.indices.Add(v + 3);
                effect.indices.Add(v + 1);
            }
            return go;
        }

        public void OnSpawned() { }
        public void OnDespawned() => mesh.Clear();
        void OnDestroy() { if (mesh) Destroy(mesh); }

        void Update()
        {
            float elapsed = Time.time - born;
            if (elapsed < 0f) return;
            if (elapsed >= lifetime) { GameObjectPool.Release(gameObject); return; }
            Draw(elapsed);
        }

        void Draw(float elapsed)
        {
            float u = Mathf.Clamp01(elapsed / travel);
            float front = followDash ? u * u * u * (u * (u * 6f - 15f) + 10f) : 1f - (1f - u) * (1f - u);
            float age = Mathf.Clamp01((elapsed - travel * 0.55f) / (lifetime - travel * 0.55f));
            float ringRadius = Mathf.Lerp(ringStart, reach, front);
            points.Clear();
            uv.Clear();
            colors.Clear();
            for (int i = 0; i <= Segments; i++)
            {
                float q = (float)i / Segments;
                float crest = Mathf.Sin(q * Mathf.PI);
                bool fullRing = shape == Shape.Impact && halfSpan >= 180f;
                float taper = fullRing ? 1f : Mathf.SmoothStep(0.32f, 1f, crest * 2.2f);
                float energy = shape == Shape.Sweep ? Mathf.Lerp(0.46f, 0.97f, q) : 0.70f;
                if (shape == Shape.GroundRing)
                {
                    var ray = Quaternion.AngleAxis(360f * q, Vector3.up) * Vector3.forward;
                    float roll = 0.5f + 0.5f * Mathf.Cos(q * 6f * Mathf.PI);
                    float width = Mathf.Min(ringRadius, (0.23f + 0.25f * front) * (0.78f + 0.22f * roll) * (1f - 0.62f * age));
                    float lip = (0.04f + Mathf.Sin(Mathf.PI * u) * 0.14f) * (0.80f + 0.20f * roll);
                    points.Add(ray * ringRadius + Vector3.up * lip);
                    points.Add(ray * (ringRadius - width));
                    taper = 1f;
                    energy = Mathf.Lerp(0.83f, 1f, roll);
                }
                else if (shape == Shape.RisingArcs)
                {
                    // Each crest stays rooted on the expanding edge and curls back as it rises.
                    float sectorPhase = q * SlamArcCount;
                    int sector = Mathf.Min(Mathf.FloorToInt(sectorPhase), SlamArcCount - 1);
                    float section = Mathf.InverseLerp(0.08f, 0.87f, sectorPhase - sector);
                    float arch = Mathf.Max(0f, Mathf.Sin(Mathf.PI * section));
                    float pop = Mathf.Clamp01((elapsed - (sector % 3) * 0.02f) / 0.23f);
                    float lift = arcHeight * arch * Mathf.Max(0f, Mathf.Sin(Mathf.PI * Mathf.Pow(pop, 0.68f)));
                    var ray = Quaternion.AngleAxis(360f * q, Vector3.up) * Vector3.forward;
                    var curl = Quaternion.AngleAxis(lift * 7f, Vector3.up) * ray;
                    points.Add(curl * Mathf.Max(0f, ringRadius - lift * 0.37f) + Vector3.up * lift);
                    points.Add(ray * ringRadius);
                    taper = 1f;
                    energy = Mathf.Lerp(0.65f, 1f, arch);
                }
                else if (shape == Shape.Thrust)
                {
                    // Render a pointed streak along the blade's travel axis.
                    float tip = Mathf.Lerp(thrustStart, reach, WeaponSwingRig.Ease(u));
                    float tail = Mathf.Max(0f, tip - reach * 0.80f) + Mathf.Min(tip * 0.50f, 0.58f) * age;
                    float width = halfSpan * Mathf.SmoothStep(0.02f, 1f, Mathf.Min(q / 0.73f, (1f - q) / 0.27f));
                    var cross = Vector3.up * width * (1f - age * 0.55f);
                    var center = Vector3.forward * Mathf.Lerp(tail, tip, q);
                    points.Add(center - cross);
                    points.Add(center + cross);
                    taper = Mathf.Lerp(0.37f, 1f, q);
                    energy = 1f;
                }
                else if (shape == Shape.Lane)
                {
                    float z = reach * front;
                    float lip = Mathf.Min(z, (1f - crest) * Mathf.Min(halfSpan * 0.22f, 0.55f));
                    float tail = Mathf.Min(z - lip, Mathf.Clamp(halfSpan * 0.52f, 0.25f, 1.4f));
                    float x = Mathf.Lerp(-halfSpan, halfSpan, q);
                    points.Add(new Vector3(x, 0.07f + crest * Mathf.Min(halfSpan * 0.16f, 0.42f), z - lip));
                    points.Add(new Vector3(x, 0f, z - lip - tail));
                }
                else
                {
                    float fraction = shape == Shape.Sweep ? q * front : q;
                    float angle = Mathf.Lerp(-halfSpan, halfSpan, reverse ? 1f - fraction : fraction);
                    var ray = Quaternion.AngleAxis(angle, Vector3.up) * Vector3.forward;
                    float radius = shape == Shape.Sweep ? reach * Mathf.Lerp(0.83f, 1f, front) : reach * front;
                    float depth = Mathf.Min(radius, Mathf.Clamp(reach * 0.17f, 0.32f, 1.18f) * taper);
                    float lift = shape == Shape.Sweep ? Mathf.Min(reach * 0.10f, 0.52f) * taper : 0.13f;
                    points.Add(ray * radius + Vector3.up * lift);
                    points.Add(ray * (radius - depth));
                }
                uv.Add(new Vector2(q, 0f));
                uv.Add(new Vector2(q, 1f));
                var sample = new Color(energy * taper, Mathf.Clamp01(age + (1f - taper) * 0.4f), 0f, 1f);
                colors.Add(sample);
                colors.Add(sample);
            }
            mesh.Clear();
            mesh.SetVertices(points);
            mesh.SetUVs(0, uv);
            mesh.SetColors(colors);
            mesh.SetTriangles(indices, 0);
        }
    }
}

