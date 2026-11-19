// 적 탄: 손그림 이펙트 스프라이트(Resources/FX/bullets, docs/images/fx_src/enemy_bullets.png → docs/tools/fx_bullets_cut.py)
// 스프라이트는 카메라를 보고 눕되, 화면에서 날아가는 방향으로 머리(오른쪽)가 향하도록 돌림.
// 뒤에 약한 가산 빛 번짐 + 짧은 꼬리. 적 탄은 보라 ~ 붉은 계열이라 플레이어의 금빛 궤적과 구분됨.
// - Orb      보라 에너지 구체 (보스 탄막)       - Revolver 무법자 리볼버 탄
// - Pellet   산탄 알갱이                        - Sniper   수배 저격 관통탄 (긴 창)
// - Needle   초침 탄 (시곗바늘)                 - Numeral  12방향 숫자 탄 (시계판 + 아라비아 숫자, 2026-11-18 사용자: 로마 숫자 X)
using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    public class EnemyProjectile : MonoBehaviour
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

        Vector3 dir;
        float speed, damage, dieAt, rangeLeft, born, haloSize, length;
        Style style;
        Transform sprite, halo;
        static Material glowMat, numeralMat;
        static readonly Dictionary<Style, Material> spriteMats = new();
        Material[] flip;                                   // 3장: 블렌더로 구운 3D 탄 프레임 (반복)
        MeshRenderer sprR;

        // 3장 (거울 세계) 일반 몬스터 탄 · 불똥 = 블렌더로 구운 3D 플립북 (2026-11-18 사용자: 2D 말고 블렌더로 구워서)
        //   구슬 → bk_c3orb (깎은 장미빛 보석 + 빛 고리), 산탄 → bk_c3shard (구르는 유리 조각), 맞음 · 튕김 → bk_c3spark (Art/tools/blender_bake_fx.py)
        static bool Ch3 => RunManager.Instance && RunManager.Instance.Chapter != null && RunManager.Instance.Chapter.Id == 3;

        // 4장 (시계 장치 속, 느와르) = 빨간 계열 구운 3D (2026-11-19 사용자: 흑백 대비, 적 공격 궤적은 빨강): 구슬 → bk_c4orb (달아오른 톱니),
        //   산탄 · 바늘 · 리볼버 · 저격 → bk_c4needle (빨간 시곗바늘), 맞음 · 튕김 → bk_c4spark (톱니 조각 · 빨간 고리)
        public static bool Ch4 => RunManager.Instance && RunManager.Instance.Chapter != null && RunManager.Instance.Chapter.Id == 4;

        static Material[] Ch3Frames(Style s)
        {
            if (Ch4)
            {
                string set4 = s == Style.Orb ? "bk_c4orb" : s == Style.Numeral || s == Style.Ticket ? null : "bk_c4needle";
                if (set4 != null) { var f4 = FxFlipbook.Frames(set4); if (f4.Length > 0) return f4; }
                return null;
            }
            if (!Ch3) return null;
            string set = s == Style.Orb ? "bk_c3orb" : s == Style.Pellet ? "bk_c3shard" : null;
            if (set == null) return null;
            var fr = FxFlipbook.Frames(set);
            return fr.Length > 0 ? fr : null;
        }

        /// <summary>탄 맞음 · 튕김 불똥: 3장은 구운 유리 섬광, 그 밖엔 2D 불똥</summary>
        static void Spark(Vector3 at, float size, Color c)
        {
            if (Ch3 && FxFlipbook.Spawn("bk_c3spark", at, Vector3.zero, size * 1.3f, 0.4f, false)) return;
            if (Ch4 && FxFlipbook.Spawn("bk_c4spark", at, Vector3.zero, size * 1.3f, 0.4f, false)) return;
            FxBurst.Spawn(at, size, FxBurst.Kind.Spark, c);
        }

        static string TexName(Style s) => s switch
        {
            Style.Revolver => "revolver", Style.Pellet => "pellet", Style.Sniper => "sniper",
            Style.Needle => "needle", Style.Numeral => "dial", Style.Ticket => "ticket", _ => "orb",
        };

        // 화면에서의 길이(m)
        static float LengthOf(Style s) => s switch
        {
            Style.Revolver => 1.5f, Style.Pellet => 0.7f, Style.Sniper => 3.6f,
            Style.Needle => 2.0f, Style.Numeral => 1.6f, Style.Ticket => 1.5f, _ => 1.9f,
        };

        static Color ColorOf(Style s) => Ch4 && s != Style.Numeral ? new Color(1f, 0.1f, 0.12f) : s switch
        {
            Style.Revolver => new Color(1f, 0.25f, 0.35f),
            Style.Pellet => new Color(1f, 0.3f, 0.3f),
            Style.Sniper => new Color(1f, 0.2f, 0.4f),
            Style.Needle => new Color(0.75f, 0.4f, 1f),
            Style.Numeral => new Color(0.9f, 0.5f, 1f),
            Style.Ticket => new Color(1f, 0.3f, 0.55f),
            _ => new Color(0.8f, 0.35f, 1f),
        };

        static Material Glow()
        {
            if (glowMat) return glowMat;
            var tex = new Texture2D(64, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                {
                    float dx = (x + 0.5f) / 32f - 1, dy = (y + 0.5f) / 32f - 1;
                    float a = Mathf.Clamp01(1 - Mathf.Sqrt(dx * dx + dy * dy));
                    tex.SetPixel(x, y, new Color(1, 1, 1, a * a * a));
                }
            tex.Apply();
            var sh = Shader.Find("CatTang/AdditiveGlow") ?? Shader.Find("Sprites/Default");
            glowMat = new Material(sh) { mainTexture = tex };
            if (glowMat.HasProperty("_Intensity")) glowMat.SetFloat("_Intensity", 1.1f);
            return glowMat;
        }

        static Material SpriteMat(Style s, out Texture2D tex)
        {
            tex = Resources.Load<Texture2D>("FX/bullets/" + TexName(s));
            if (spriteMats.TryGetValue(s, out var m) && m) return m;
            m = new Material(Shader.Find("Sprites/Default")) { mainTexture = tex, renderQueue = 3005 };
            spriteMats[s] = m;
            return m;
        }

        static Mesh quad;
        static Mesh Quad()
        {
            if (quad) return quad;
            quad = new Mesh { name = "BulletQuad" };
            quad.vertices = new[] { new Vector3(-0.5f, -0.5f), new Vector3(0.5f, -0.5f), new Vector3(-0.5f, 0.5f), new Vector3(0.5f, 0.5f) };
            quad.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
            quad.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            quad.colors = new[] { Color.white, Color.white, Color.white, Color.white };
            quad.RecalculateBounds();
            return quad;
        }

        static GameObject QuadObj(string name, Transform parent, Material m, Color tint)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var mf = go.AddComponent<MeshFilter>();
            var mesh = Instantiate(Quad());
            var c = new Color[4]; for (int i = 0; i < 4; i++) c[i] = tint;
            mesh.colors = c;
            mf.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = m;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return go;
        }

        /// <summary>range > 0이면 그 거리만큼 날아가면 사라짐 (산탄 사거리). hour는 숫자 탄의 시각(1~12)</summary>
        public static EnemyProjectile Spawn(Vector3 pos, Vector3 dir, float speed, float damage, float range = 0, Style style = Style.Orb, int hour = 0)
        {
            dir.y = 0;
            dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.forward;
            var root = new GameObject("EnemyBullet_" + style);
            root.transform.position = pos;
            var c = ColorOf(style);
            float len = LengthOf(style);
            var flip = Ch3Frames(style);
            if (flip != null) len = style == Style.Orb ? 1.7f : 1.0f;      // 구운 그림은 정사각 캔버스 (내용이 가운데 약 2/3)

            // 빛 번짐 (뒤)
            float hs = len * (style == Style.Sniper ? 0.55f : style == Style.Pellet ? 1.6f : style == Style.Numeral ? 0.7f : 1.05f);
            var halo = QuadObj("Halo", root.transform, Glow(), c * 0.8f);

            // 손그림 탄 스프라이트
            var mat = SpriteMat(style, out var tex);
            float aspect = flip != null ? 1f : tex ? tex.height / (float)tex.width : 0.3f;
            var spr = QuadObj("Sprite", root.transform, flip != null ? flip[0] : mat, Color.white);
            spr.transform.localScale = new Vector3(len, len * aspect, 1);

            if (style == Style.Numeral)
            {
                var t = new GameObject("Numeral");
                t.transform.SetParent(spr.transform, false);
                // 시계판은 스프라이트 오른쪽 끝 쪽에 있음
                t.transform.localPosition = new Vector3(0.24f, 0, -0.01f);
                var tm = t.AddComponent<TextMesh>();
                var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                tm.font = font;
                if (!numeralMat) numeralMat = new Material(font.material) { renderQueue = 3006 };    // 스프라이트(3005) 위에
                t.GetComponent<MeshRenderer>().sharedMaterial = numeralMat;
                tm.text = Mathf.Clamp(hour, 1, 12).ToString();
                tm.fontSize = 64; tm.characterSize = 0.075f; tm.fontStyle = FontStyle.Bold;
                tm.anchor = TextAnchor.MiddleCenter; tm.color = new Color(0.22f, 0.03f, 0.3f);
                t.transform.localScale = new Vector3(1f / len, 1f / (len * aspect), 1) * 0.55f;
            }

            // 짧은 꼬리 (스프라이트에도 꼬리가 그려져 있어 약하게)
            var tr = root.AddComponent<TrailRenderer>();
            tr.sharedMaterial = Glow();
            tr.time = style switch { Style.Sniper => 0.25f, Style.Pellet => 0.06f, _ => 0.12f };
            tr.widthMultiplier = len * (style == Style.Sniper ? 0.1f : 0.22f);
            tr.widthCurve = AnimationCurve.Linear(0, 1, 1, 0);
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(c, 0), new GradientColorKey(c, 1) },
                      new[] { new GradientAlphaKey(0.55f, 0), new GradientAlphaKey(0f, 1) });
            tr.colorGradient = g;
            tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            tr.minVertexDistance = 0.05f;

            var p = root.AddComponent<EnemyProjectile>();
            p.dir = dir;
            p.speed = speed;
            p.damage = damage;
            p.style = style;
            p.sprite = spr.transform;
            p.flip = flip;
            p.sprR = spr.GetComponent<MeshRenderer>();
            p.halo = halo.transform;
            p.haloSize = hs;
            p.length = len;
            p.born = Time.time;
            p.dieAt = Time.time + 6f;
            p.rangeLeft = range > 0 ? range : float.MaxValue;
            p.Face();
            return p;
        }

        // 카메라를 보고 눕되, 화면에서 진행 방향으로 머리가 향하게
        void Face()
        {
            var cam = Camera.main;
            if (!cam) return;
            var fwd = cam.transform.forward;
            var right = flip != null && style == Style.Orb ? cam.transform.right : Vector3.ProjectOnPlane(dir, fwd);   // 구운 보석 구슬은 고리가 늘 같은 쪽으로 기울게 세워 둠
            if (right.sqrMagnitude < 0.0001f) right = cam.transform.right;
            right.Normalize();
            var up = Vector3.Cross(fwd, right);
            var rot = Quaternion.LookRotation(fwd, up);
            sprite.rotation = rot;
            halo.rotation = rot;
            // 빛 번짐은 탄 몸통(앞쪽) 중심에
            halo.position = transform.position + right * length * (style == Style.Sniper ? 0.3f : 0.15f) - fwd * 0.02f;
        }

        void LateUpdate()
        {
            Face();
            float pulse = 1f + 0.1f * Mathf.Sin((Time.time - born) * 32f);
            if (halo) halo.localScale = Vector3.one * haloSize * pulse;
            // 구체 · 시계판은 살짝 출렁, 바늘은 떨림
            if (flip != null) { if (sprR) sprR.sharedMaterial = flip[(int)((Time.time - born) * 20f) % flip.Length]; }
            else if (style == Style.Orb && sprite) sprite.localScale = new Vector3(length, sprite.localScale.y, 1) * (1f + 0.06f * Mathf.Sin((Time.time - born) * 40f));
        }

        void Update()
        {
            if (Time.time < frozenUntil)
            {
                // 정지 구역: 제자리에서 살짝 떨림, 사라지는 시각도 멈춘 만큼 미룸
                dieAt += Time.deltaTime;
                if (sprite) sprite.localPosition = new Vector3(Mathf.Sin(Time.time * 70f) * 0.02f, 0, 0);
                return;
            }
            if (sprite && sprite.localPosition != Vector3.zero) sprite.localPosition = Vector3.zero;
            float step = speed * Time.deltaTime;
            if (homing > 0)
            {
                var pt = PlayerController.Instance;
                if (pt && pt.health.Alive)
                {
                    var want = Effects.Flat(pt.transform.position - transform.position);
                    if (want.sqrMagnitude > 0.01f) { dir = Vector3.RotateTowards(dir, want.normalized, homing * Mathf.Deg2Rad * Time.deltaTime, 0); Face(); }
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
                    rangeLeft -= mt;
                    ms.OnBounce(mp, ColorOf(style), false);
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
                    Spark(hit.point, 0.8f, ColorOf(style));
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
            if (!quiet) Spark(at, style == Style.Pellet ? 0.7f : style == Style.Orb ? 1.4f : 1.0f, ColorOf(style));
            var tr = GetComponent<TrailRenderer>();
            foreach (Transform ch in transform) Destroy(ch.gameObject);
            enabled = false;
            if (tr) { tr.emitting = false; Destroy(gameObject, tr.time); }
            else Destroy(gameObject);
        }
    }
}
