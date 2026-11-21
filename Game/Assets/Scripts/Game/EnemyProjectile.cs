// Enemy projectiles use emissive Blender meshes, with separate glow and flight trails.
using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    public class EnemyProjectile : MonoBehaviour, IPooled
    {
        public enum Style { Orb, Revolver, Pellet, Sniper, Needle, Numeral, Ticket }

        /// <summary>살아 있는 적 탄 (신호수+ 정지 구역이 훑음)</summary>
        public static readonly List<EnemyProjectile> All = new();
        /// <summary>고양이에게 맞았을 때 추가 효과 (차장+ 검표 펀치: 대시 충전 멈춤)</summary>
        public System.Action<PlayerController> OnHitPlayer;
        /// <summary>엄폐물 · 화약통을 뚫고 지나감 (벽엔 막힘)</summary>
        public bool pierceCover;
        /// <summary>이 시각까지 제자리에 멈춤 (정지 구역). 풀리면 원래 방향으로 다시 날아감</summary>
        public float frozenUntil;
        /// <summary>고양이 탄에 맞으면 부서짐 (2장 중간 보스 빨간불의 느린 표 탄)</summary>
        public bool shootable;
        /// <summary>이미 거울 조각에 한 번 튕김 (3장)</summary>
        public bool mirrored;
        /// <summary>벽 · 거울 조각에 튕기는 남은 횟수 (3장 거울 사수 반사탄 = 1). 튕길 때 OnBounced</summary>
        public int bounces;
        public System.Action<EnemyProjectile, Vector3> OnBounced;
        /// <summary>유도: 초당 이만큼(°) 고양이 쪽으로 꺾음 (3장 빛가루 법사 구슬)</summary>
        public float homing;
        /// <summary>방향을 새로 정함 (멈춰 있던 구슬을 쏠 때)</summary>
        public void Retarget(Vector3 d) { d = Effects.Flat(d); if (d.sqrMagnitude > 1e-6f) { dir = d.normalized; Face(); } }
        /// <summary>조용히 사라짐</summary>
        public void Vanish() { if (enabled) Pop(transform.position, true); }
        public float SpeedNow { get => speed; set => speed = value; }

        /// <summary>고양이 탄 한 줄(from → to, 반지름 radius)에 걸린 부서지는 적 탄을 깨뜨림. 깬 수를 돌려줌</summary>
        /// <summary>반경 안 적 탄을 지움 (3장 분수 샘 물 폭발). 지운 개수</summary>
        public static int WashAway(Vector3 at, float radius)
        {
            int n = 0;
            foreach (var p in All.ToArray())
            {
                if (!p || !p.enabled || Effects.Flat(p.transform.position - at).magnitude > radius) continue;
                p.Pop(p.transform.position, true);
                n++;
            }
            return n;
        }

        public static int ShootDown(Vector3 from, Vector3 to, float radius = 0.7f)
        {
            int n = 0;
            var seg = Effects.Flat(to - from);
            float len = seg.magnitude;
            if (len < 0.01f) return 0;
            var d = seg / len;
            foreach (var p in All.ToArray())
            {
                if (!p || !p.shootable || !p.enabled) continue;
                var rel = Effects.Flat(p.transform.position - from);
                float along = Vector3.Dot(rel, d);
                if (along < -0.3f || along > len + 0.3f || (rel - d * along).magnitude > radius) continue;
                FxBurst.Spawn(p.transform.position, 1.3f, FxBurst.Kind.Spark, new Color(1f, 0.85f, 0.5f));
                p.Pop(p.transform.position, true);
                n++;
            }
            return n;
        }
        public Vector3 Dir => dir;

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        Vector3 dir, launchOffset;
        float speed, damage, dieAt, rangeLeft, born, releaseAt;
        bool wasHeld;
        Style style;
        Transform body, halo;
        MeshRenderer bodyRenderer;
        MaterialPropertyBlock bodyProperties;
        Color tint;
        Vector2 haloSize;
        static Material coreMat, glowMat, trailMat, numeralMat;
        static readonly Mesh[] artMeshes = new Mesh[7];
        static readonly Dictionary<Color, Mesh> glowMeshes = new();
        static readonly int TintId = Shader.PropertyToID("_Tint"), PhaseId = Shader.PropertyToID("_Phase");

        static bool Ch3 => RunManager.Instance && RunManager.Instance.Chapter != null && RunManager.Instance.Chapter.Id == 3;
        public static bool Ch4 => RunManager.Instance && RunManager.Instance.Chapter != null && RunManager.Instance.Chapter.Id == 4;
        public static bool Ch5 => RunManager.Instance && RunManager.Instance.Chapter != null && RunManager.Instance.Chapter.Id == 5;

        /// <summary>탄 맞음 · 튕김 불똥: 3장은 구운 유리 섬광, 그 밖엔 2D 불똥</summary>
        static void Spark(Vector3 at, float size, Color c)
        {
            if (Ch3 && FxFlipbook.Spawn("bk_c3spark", at, Vector3.zero, size * 1.3f, 0.4f, false)) return;
            if (Ch4 && FxFlipbook.Spawn("bk_c4spark", at, Vector3.zero, size * 1.3f, 0.4f, false)) return;
            if (Ch5) { Enemy.Fx5("c5spark", at, size * 1.2f, 0.35f); return; }
            FxBurst.Spawn(at, size, FxBurst.Kind.Spark, c);
        }

        static Color ColorOf(int chapter) => chapter switch
        {
            1 => new Color(1f, 0.52f, 0.22f),
            2 => new Color(1f, 0.67f, 0.28f),
            3 => new Color(0.56f, 0.90f, 1f),
            4 => BladeStreak.Ch4Base,
            5 => Enemy.Ch5Magenta,
            _ => new Color(1f, 0.28f, 0.42f),
        };

        static Mesh BodyMesh(Style style)
        {
            int index = (int)style;
            if (artMeshes[index]) return artMeshes[index];
            var asset = Resources.Load<GameObject>("FX/EnemyProjectiles/" + style);
            var filter = asset ? asset.GetComponentInChildren<MeshFilter>() : null;
            return artMeshes[index] = filter && filter.sharedMesh ? filter.sharedMesh : FxMeshes.Ball(0);
        }

        internal static MeshRenderer BuildBody(Transform parent, Style style)
        {
            if (!coreMat)
                coreMat = new Material(Shader.Find("CatTang/EnemyProjectile") ?? Shader.Find("Universal Render Pipeline/Unlit"));
            var go = new GameObject("EmissiveBody");
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = BodyMesh(style);
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = coreMat;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return renderer;
        }

        internal static void TintBody(MeshRenderer renderer, Color color, MaterialPropertyBlock properties)
        {
            properties.SetColor(TintId, color);
            properties.SetFloat(PhaseId, (renderer.GetInstanceID() & 255) * 0.37f);
            renderer.SetPropertyBlock(properties);
        }

        static Material Glow()
        {
            if (glowMat) return glowMat;
            var tex = new Texture2D(64, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                {
                    float dx = (x + 0.5f) / 32f - 1, dy = (y + 0.5f) / 32f - 1;
                    float a = Mathf.Clamp01(1 - Mathf.Sqrt(dx * dx + dy * dy));
                    tex.SetPixel(x, y, new Color(1, 1, 1, a * a));
                }
            tex.Apply();
            var sh = Shader.Find("CatTang/AdditiveGlow") ?? Shader.Find("Sprites/Default");
            glowMat = new Material(sh) { mainTexture = tex };
            if (glowMat.HasProperty("_Intensity")) glowMat.SetFloat("_Intensity", 0.83f);
            return glowMat;
        }

        static Material TrailMaterial()
        {
            if (trailMat) return trailMat;
            trailMat = new Material(Glow()) { mainTexture = Texture2D.whiteTexture };
            if (trailMat.HasProperty("_Intensity")) trailMat.SetFloat("_Intensity", 1.46f);
            return trailMat;
        }

        internal static TrailRenderer BuildTrail(Transform parent, Color color, float width, float life)
        {
            var go = new GameObject("Trail");
            go.transform.SetParent(parent, false);
            var trail = go.AddComponent<TrailRenderer>();
            trail.sharedMaterial = TrailMaterial();
            trail.time = life;
            trail.widthMultiplier = width;
            trail.widthCurve = new AnimationCurve(new Keyframe(0, 1), new Keyframe(0.54f, 0.46f), new Keyframe(1, 0));
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.Lerp(color, Color.white, 0.34f), 0), new GradientColorKey(color, 1) },
                new[] { new GradientAlphaKey(0.76f, 0), new GradientAlphaKey(0.38f, 0.46f), new GradientAlphaKey(0f, 1) });
            trail.colorGradient = gradient;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.minVertexDistance = 0.04f;
            return trail;
        }

        internal static Transform BuildGlow(Transform parent, Color tint)
        {
            var go = new GameObject("Halo");
            go.transform.SetParent(parent, false);
            if (!glowMeshes.TryGetValue(tint, out var mesh) || !mesh)
            {
                mesh = Instantiate(GunkataFx.Quad);
                mesh.name = "Projectile Halo";
                mesh.colors = new[] { tint, tint, tint, tint };
                glowMeshes[tint] = mesh;
            }
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = Glow();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return go.transform;
        }

        /// <summary>range > 0이면 그 거리만큼 날아가면 사라짐 (산탄 사거리). hour는 숫자 탄의 시각(1~12)</summary>
        // 풀링 (2026-11-20): 장별 외형과 스타일별 완성 탄을 보관해 자식과 꼬리를 다시 만들지 않는다.
        static readonly string[][] poolKeys = MakeKeys();
        static readonly string[] hours = { "", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12" };
        static string[][] MakeKeys()
        {
            var keys = new string[6][];
            for (int ch = 0; ch < keys.Length; ch++)
            {
                keys[ch] = new string[7];
                for (int i = 0; i < 7; i++) keys[ch][i] = "EnemyBullet_" + ch + "_" + i;
            }
            return keys;
        }
        static string Key(Style style)
        {
            int chapter = RunManager.Instance && RunManager.Instance.Chapter != null ? RunManager.Instance.Chapter.Id : 0;
            return poolKeys[Mathf.Clamp(chapter, 0, 5)][(int)style];
        }
        TrailRenderer trail;
        float trailWidth, elasticity;
        TextMesh number;
        static float nextAccentAt;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetAccent() => nextAccentAt = 0f;

        public static void Prewarm(Style style, int count, int maxCreate = int.MaxValue) => GameObjectPool.Prewarm(Key(style), factory, count, maxCreate);
        public void OnSpawned()
        {
            OnHitPlayer = null; OnBounced = null;
            pierceCover = shootable = mirrored = false; frozenUntil = homing = 0f; bounces = 0;
            dir = launchOffset = Vector3.zero; speed = damage = dieAt = rangeLeft = born = releaseAt = 0f;
            wasHeld = false;
            TintBody(bodyRenderer, tint, bodyProperties);
            trail.widthMultiplier = trailWidth;
            trail.emitting = false; trail.Clear();
        }
        public void OnDespawned()
        {
            if (frozenUntil == float.MaxValue) Enemy.ForgetPooledProjectile(this);
            OnHitPlayer = null; OnBounced = null;
            trail.emitting = false; trail.Clear();
        }
        public static EnemyProjectile Spawn(Vector3 pos, Vector3 dir, float speed, float damage, float range = 0, Style style = Style.Orb, int hour = 0)
        {
            dir.y = 0;
            dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.forward;
            var root = GameObjectPool.Get(Key(style), factory);
            root.transform.position = pos;
            var p = root.GetComponent<EnemyProjectile>();
            p.dir = dir; p.speed = speed; p.damage = damage;
            p.born = p.releaseAt = Time.time; p.dieAt = Time.time + 6f;
            p.rangeLeft = range > 0 ? range : float.MaxValue;
            if (p.number) p.number.text = hours[Mathf.Clamp(hour, 1, 12)];
            p.Face();
            p.halo.localScale = new Vector3(p.haloSize.x, p.haloSize.y, 1f);
            p.trail.Clear(); p.trail.emitting = true;
            p.Accent();
            return p;
        }

        void Accent()
        {
            if ((style != Style.Orb && style != Style.Sniper) || Time.time < nextAccentAt) return;
            nextAccentAt = Time.time + 0.16f;
            EffectShadowLight.Projectile(body, tint);
        }

        static readonly System.Func<string, GameObject> factory = Create;
        static GameObject Create(string key)
        {
            var style = (Style)(key[key.Length - 1] - '0');
            var root = new GameObject(key);
            var color = ColorOf(key[key.Length - 3] - '0');
            var body = BuildBody(root.transform, style);
            var size = body.GetComponent<MeshFilter>().sharedMesh.bounds.size;
            float width = style == Style.Numeral ? size.x : Mathf.Max(size.x, size.y);
            float length = style == Style.Numeral ? size.y : size.z;
            var halo = BuildGlow(root.transform, new Color(color.r, color.g, color.b, 0.76f));
            TextMesh number = null;

            if (style == Style.Numeral)
            {
                var label = new GameObject("Numeral");
                label.transform.SetParent(body.transform, false);
                label.transform.localPosition = new Vector3(0f, 0f, -0.098f);
                number = label.AddComponent<TextMesh>();
                var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                number.font = font;
                if (!numeralMat) numeralMat = new Material(font.material) { renderQueue = 3006 };
                var renderer = label.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = numeralMat;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                number.text = hours[1];
                number.fontSize = 64;
                number.characterSize = 0.055f;
                number.fontStyle = FontStyle.Bold;
                number.anchor = TextAnchor.MiddleCenter;
                number.color = new Color(1f, 0.97f, 0.90f);
            }

            float tailLife = style switch { Style.Sniper => 0.20f, Style.Pellet => 0.095f, _ => 0.15f };
            var tr = BuildTrail(root.transform, color, Mathf.Clamp(width * 0.63f, 0.16f, 0.36f), tailLife);

            var projectile = root.AddComponent<EnemyProjectile>();
            projectile.style = style;
            projectile.body = body.transform;
            projectile.bodyRenderer = body;
            projectile.bodyProperties = new MaterialPropertyBlock();
            projectile.tint = color;
            projectile.halo = halo;
            projectile.haloSize = new Vector2(length * 1.55f, width * 2.25f);
            projectile.trail = tr;
            projectile.trailWidth = tr.widthMultiplier;
            projectile.elasticity = style switch
            {
                Style.Numeral => 0f, Style.Orb => 0.39f, Style.Pellet => 0.20f,
                Style.Sniper => 0.30f, Style.Needle => 0.32f, Style.Ticket => 0.23f, _ => 0.24f,
            };
            projectile.number = number;
            return root;
        }

        internal void LaunchVisualFrom(Vector3 muzzle)
        {
            launchOffset = muzzle - transform.position;
            Face(visualOffset: launchOffset);
            trail.Clear();
        }

        void Face(Vector3 visualOffset = default, float roll = 0f)
        {
            var at = transform.position + visualOffset;
            body.position = at;
            trail.transform.position = at;
            var cam = Camera.main;
            var heading = dir.sqrMagnitude > 0.001f ? dir : Vector3.forward;
            body.rotation = style == Style.Numeral && cam ? cam.transform.rotation
                : Quaternion.LookRotation(heading) * Quaternion.AngleAxis(roll, Vector3.forward);
            halo.position = at;
            if (!cam) return;
            var fwd = cam.transform.forward;
            var right = style == Style.Numeral ? cam.transform.right : Vector3.ProjectOnPlane(heading, fwd);
            if (right.sqrMagnitude < 0.0001f) right = cam.transform.right;
            right.Normalize();
            halo.rotation = Quaternion.LookRotation(fwd, Vector3.Cross(fwd, right));
            halo.position = at - fwd * 0.03f;
        }

        void LateUpdate()
        {
            float now = Time.time;
            float age = now - born;
            bool held = now < frozenUntil;
            if (held != wasHeld)
            {
                if (!held) { releaseAt = now; Accent(); }
                trail.emitting = !held;
                wasHeld = held;
            }
            float release = now - releaseAt;
            // Stretch along the flight axis, then rebound slightly before the silhouette settles.
            float charge = Mathf.Lerp(-0.52f, 1f, WeaponSwingRig.Ease(release / 0.032f));
            float rebound = Mathf.Sin(Mathf.PI * Mathf.InverseLerp(0.076f, 0.20f, release)) * 0.26f;
            float stretch = held ? -0.40f * elasticity
                : (charge * (1f - WeaponSwingRig.Ease((release - 0.032f) / 0.105f)) - rebound) * elasticity;
            float pulse = style == Style.Orb ? 1f + (held ? 0.09f : 0.025f) * Mathf.Sin(age * 23f) : 1f;
            float along = 1f + stretch;
            float across = 1f - stretch * 0.55f;
            // Blend from the rendered muzzle onto the existing collision path.
            float converge = Mathf.Max(0.13f, Mathf.Abs(Vector3.Dot(launchOffset, dir)) / Mathf.Max(1f, speed) * 1.70f);
            var offset = launchOffset * (1f - WeaponSwingRig.Ease(age / converge));
            if (held) offset += Vector3.right * (Mathf.Sin(now * 70f) * 0.02f);
            float roll = style switch { Style.Orb => age * 125f, Style.Ticket => Mathf.Sin(age * 24f) * 15f, _ => 0f };
            Face(offset, roll);
            body.localScale = new Vector3(across, across, along) * pulse;
            halo.localScale = new Vector3(haloSize.x * along, haloSize.y * across, 1f) * pulse;
            trail.widthMultiplier = trailWidth * across;
        }

        void Update()
        {
            if (Time.time < frozenUntil)
            {
                // 정지 구역: 제자리에서 살짝 떨림, 사라지는 시각도 멈춘 만큼 미룸
                dieAt += Time.deltaTime;
                return;
            }
            float step = speed * Time.deltaTime;
            if (homing > 0)
            {
                var pt = PlayerController.Instance;
                if (pt && pt.health.Alive)
                {
                    var want = Effects.Flat(pt.transform.position - transform.position);
                    if (want.sqrMagnitude > 0.01f) dir = Vector3.RotateTowards(dir, want.normalized, homing * Mathf.Deg2Rad * Time.deltaTime, 0);
                }
            }
            // 3장 떠다니는 거울 조각: 한 번 튕겨 냄 (판에 닿은 자리에서 꺾여 남은 거리를 감)
            if (!mirrored && MirrorShard.All.Count > 0)
            {
                var ms = MirrorShard.Raycast(transform.position, dir, step, null, out float mt, out var mp);
                if (ms)
                {
                    mirrored = true;
                    dir = ms.Reflect(dir);
                    transform.position = mp + dir * 0.05f;
                    launchOffset = Vector3.zero;
                    releaseAt = Time.time;
                    rangeLeft -= mt;
                    ms.OnBounce(mp, tint, false);
                    Face();
                    if (bounces > 0) { bounces--; OnBounced?.Invoke(this, mp); }
                    return;
                }
            }
            if (Physics.Raycast(transform.position, dir, out var hit, step + 0.2f, ~0, QueryTriggerInteraction.Ignore))
            {
                var hp = hit.collider.GetComponentInParent<Health>();
                if (hp == null && bounces > 0)
                {
                    // 반사탄: 벽에 한 번 튕김
                    bounces--;
                    var n = Effects.Flat(hit.normal);
                    dir = Effects.Flat(Vector3.Reflect(dir, n.sqrMagnitude > 1e-4f ? n.normalized : -dir)).normalized;
                    transform.position = hit.point + dir * 0.15f + Vector3.up * (transform.position.y - hit.point.y);
                    launchOffset = Vector3.zero;
                    releaseAt = Time.time;
                    Spark(hit.point, 0.8f, tint);
                    Face();
                    OnBounced?.Invoke(this, hit.point);
                    return;
                }
                if (hp == null) { Pop(hit.point); return; }                                                     // 벽
                // 엄폐물 · 화약통은 적 탄을 막음 (맞으면 깎임). 관통 탄은 깎기만 하고 지나감
                if (!hp.isPlayer && !hp.GetComponent<Enemy>() && !hp.GetComponent<PlayerOnlyTarget>() && !pierceCover) { hp.HitFrom(dir); hp.TakeDamage(damage); Pop(hit.point); return; }
            }
            transform.position += dir * step;
            rangeLeft -= step;
            if (rangeLeft <= 0) { Pop(transform.position, true); return; }
            var pl = PlayerController.Instance;
            if (pl && pl.health.Alive)
            {
                var d = pl.transform.position + Vector3.up * 0.9f - transform.position;
                float hitR = style == Style.Pellet ? 0.45f : 0.55f;
                if (d.sqrMagnitude < hitR * hitR)
                {
                    float before = pl.health.hp;
                    pl.health.TakeDamage(damage);
                    if (OnHitPlayer != null && pl.health.hp < before) OnHitPlayer(pl);   // 실제로 맞았을 때만 (회피 · 저스트 회피 제외)
                    Pop(transform.position);
                    return;
                }
            }
            if (Time.time > dieAt) Pop(transform.position, true);
        }

        /// <summary>사라짐: 맞으면 탄 색 불꽃, 사거리 끝이면 조용히. 꼬리는 남겨 흐려지게</summary>
        void Pop(Vector3 at, bool quiet = false)
        {
            if (!enabled) return;
            if (!quiet) Spark(at, style == Style.Pellet ? 0.7f : style == Style.Orb ? 1.4f : 1.0f, tint);
            body.gameObject.SetActive(false); halo.gameObject.SetActive(false);
            enabled = false;
            All.Remove(this);
            trail.emitting = false;
            GameObjectPool.ReleaseAfter(gameObject, trail.time);
        }
    }
}
