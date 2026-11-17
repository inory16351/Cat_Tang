// 체력 회복 하트 (2026-11-17 사용자: 회복 수단이 너무 없음 → 적 처치 시 낮은 확률로 하트, 상인도 판매)
// - 모델: 블렌더 + Codex(Art/tools/blender_kit_heart.py) → Resources/Kit/Ch1/Heart.prefab. 없으면 빨간 공.
// - 적 처치: 일반 6% · 강화형 10% · 중간 보스 확정. 튀어나와 떨어진 뒤 떠서 돔.
// - 고양이가 가까이 오면(1.6m) 빨려 들어와 최대 체력의 HealPct 회복. 체력이 가득이면 안 빨려 오고 남아 있음.
// - 스테이지 루트에 붙어서 스테이지가 바뀌면 같이 사라짐.
using UnityEngine;

namespace CatTang
{
    public class HeartPickup : MonoBehaviour
    {
        public const float HealPct = 0.2f;          // 하트 하나 = 최대 체력 20%
        public const float DropChance = 0.06f, EliteDropChance = 0.1f;
        const float Magnet = 1.6f, Grab = 0.55f;
        const float Size = 1.4f;                     // 위에서 내려다보면 0.6m 하트가 작아 보여 키움
        static readonly Color Red = new(1f, 0.3f, 0.35f);

        Transform model;
        Vector3 baseScale;
        Vector3 vel;
        float t, groundY, spinSpeed;
        bool landed, pulling;

        public static void TryDrop(Enemy e, bool midBoss)
        {
            if (!e) return;
            float chance = midBoss ? 1f : e.elite ? EliteDropChance : DropChance;
            if (Random.value < chance) Drop(e.transform.position);
        }

        public static HeartPickup Drop(Vector3 at)
        {
            var go = new GameObject("Heart");
            var root = RunManager.Instance ? RunManager.Instance.StageRoot : null;
            if (root) go.transform.SetParent(root, true);
            go.transform.position = new Vector3(at.x, at.y + 1.2f, at.z);
            var h = go.AddComponent<HeartPickup>();
            h.groundY = at.y;
            var dir = Random.insideUnitCircle.normalized;
            h.vel = new Vector3(dir.x * 2.2f, 6f, dir.y * 2.2f);
            h.spinSpeed = Random.Range(110f, 150f);
            var prefab = Kit.Prefab(RunManager.Instance ? RunManager.Instance.Chapter : null, "Heart");
            GameObject m;
            if (prefab) m = Instantiate(prefab, go.transform, false);
            else
            {
                m = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(m.GetComponent<Collider>());
                m.transform.SetParent(go.transform, false);
                m.transform.localScale = Vector3.one * 0.5f;
                m.GetComponent<Renderer>().sharedMaterial = RoomBuilder.Mat(Red);
            }
            foreach (var c in m.GetComponentsInChildren<Collider>()) Destroy(c);
            m.name = "Model";
            m.transform.localScale *= Size;
            h.model = m.transform;
            h.baseScale = m.transform.localScale;
            h.AddGlow();
            return h;
        }

        // ---------- 빛남 (2026-11-17 사용자: 살짝 빛나게) ----------
        // 뒤에 붉은 후광(카메라를 보는 부드러운 원, 맥동) + 바닥에 붉은 빛(작은 점광원) + 가끔 반짝임
        Transform halo;
        Material haloMat;
        Light glowLight;
        float nextSparkle;
        static Texture2D haloTex;

        void AddGlow()
        {
            if (!haloTex)
            {
                const int n = 64;
                haloTex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float d = new Vector2(x - n / 2f + 0.5f, y - n / 2f + 0.5f).magnitude / (n / 2f);
                        float a = Mathf.Clamp01(1 - d);
                        haloTex.SetPixel(x, y, new Color(1, 1, 1, a * a * a));
                    }
                haloTex.Apply();
            }
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(q.GetComponent<Collider>());
            q.name = "Halo";
            q.transform.SetParent(transform, false);
            q.transform.localScale = Vector3.one * 1.5f;
            haloMat = new Material(Shader.Find("Sprites/Default")) { mainTexture = haloTex, color = new Color(1f, 0.35f, 0.4f, 0.55f) };
            var r = q.GetComponent<MeshRenderer>();
            r.sharedMaterial = haloMat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            q.AddComponent<Billboard>();
            halo = q.transform;
            var lg = new GameObject("Glow");
            lg.transform.SetParent(transform, false);
            lg.transform.localPosition = Vector3.up * 0.2f;
            glowLight = lg.AddComponent<Light>();
            glowLight.type = LightType.Point;
            glowLight.color = new Color(1f, 0.35f, 0.4f);
            glowLight.range = 2.6f;
            glowLight.intensity = 1.6f;
            glowLight.shadows = LightShadows.None;
        }

        void Glow()
        {
            float k = 0.5f + 0.5f * Mathf.Sin(t * 4f);
            if (halo)
            {
                halo.localPosition = model.localPosition;
                halo.localScale = Vector3.one * Mathf.Lerp(1.3f, 1.7f, k);
                haloMat.color = new Color(1f, 0.35f, 0.4f, Mathf.Lerp(0.35f, 0.6f, k));
            }
            if (glowLight) glowLight.intensity = Mathf.Lerp(1.1f, 2f, k);
            if (t > nextSparkle)
            {
                nextSparkle = t + Random.Range(0.6f, 1.1f);
                FxBurst.Spawn(transform.position + model.localPosition + Random.insideUnitSphere * 0.35f, 0.45f, FxBurst.Kind.Spark, new Color(1f, 0.75f, 0.75f));
            }
        }

        void OnDestroy() { if (haloMat) Destroy(haloMat); }

        void Update()
        {
            float dt = Time.deltaTime;
            t += dt;
            var p = PlayerController.Instance;
            // 회전 · 둥실
            model.Rotate(0, spinSpeed * dt, 0, Space.World);
            Glow();
            if (!landed)
            {
                vel.y -= 20f * dt;
                transform.position += vel * dt;
                float floor = groundY + 0.75f;
                if (transform.position.y <= floor && vel.y < 0)
                {
                    transform.position = new Vector3(transform.position.x, floor, transform.position.z);
                    if (vel.y < -3f) { vel = new Vector3(vel.x * 0.4f, -vel.y * 0.35f, vel.z * 0.4f); }
                    else
                    {
                        landed = true;
                        FxBurst.Spawn(new Vector3(transform.position.x, groundY + 0.1f, transform.position.z), 1f, FxBurst.Kind.Dust, new Color(0.92f, 0.8f, 0.55f));
                    }
                }
                return;
            }
            model.localPosition = Vector3.up * Mathf.Sin(t * 3f) * 0.12f;
            float pulse = 1f + Mathf.Sin(t * 6f) * 0.06f;
            model.localScale = baseScale * pulse;

            if (!p || !p.health.Alive) return;
            bool hurt = p.health.hp < p.health.maxHp;
            var target = p.transform.position + Vector3.up * 0.9f;
            var d = target - transform.position;
            float flat = new Vector2(d.x, d.z).magnitude;
            if (hurt && (pulling || flat < Magnet))
            {
                pulling = true;
                transform.position += d.normalized * Mathf.Min(d.magnitude, (6f + t * 0.5f) * dt * 2f);
                if (d.magnitude < Grab) Collect(p);
            }
            else pulling = false;
        }

        void Collect(PlayerController p)
        {
            float amount = p.health.maxHp * HealPct;
            p.health.Heal(amount);
            FxBurst.Spawn(p.transform.position + Vector3.up * 1f, 2f, FxBurst.Kind.Gold, Red);
            FindFirstObjectByType<GameUI>()?.Toast($"체력 +{Mathf.RoundToInt(amount)}");
            Destroy(gameObject);
        }
    }
}
