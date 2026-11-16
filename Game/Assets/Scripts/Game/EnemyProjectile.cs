// 적 탄: 종류마다 모양 · 색이 다름. 적 탄은 늘 '보라 ~ 붉은' 계열로 플레이어의 금빛 궤적과 구분됨.
// 공통 구성: 잉크 외곽선 몸통(툰) + 빛 번짐(가산 빌보드) + 꼬리(TrailRenderer) + 맞으면 작은 불꽃
// - Orb      기본 구슬 (예전 보스 탄막)
// - Revolver 무법자 리볼버 탄: 짧고 빠른 붉은 탄두 + 가는 꼬리
// - Pellet   산탄 알갱이: 작고 많음, 짧은 꼬리
// - Sniper   수배 저격 관통탄: 길게 늘어난 탄두 + 긴 붉은 꼬리
// - Needle   초침 탄: 금빛 시곗바늘 모양 (날아가는 방향을 가리킴)
// - Numeral  12방향 숫자 탄: 그 시각의 로마 숫자가 날아감
using UnityEngine;

namespace CatTang
{
    public class EnemyProjectile : MonoBehaviour
    {
        public enum Style { Orb, Revolver, Pellet, Sniper, Needle, Numeral }

        Vector3 dir;
        float speed, damage, dieAt, rangeLeft, born, haloSize;
        Style style;
        Transform halo;
        static Material glowMat;
        static Texture2D dotTex;
        static readonly System.Collections.Generic.Dictionary<Color, Material> bodyMats = new();
        static readonly string[] Roman = { "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X", "XI", "XII" };

        static Color ColorOf(Style s) => s switch
        {
            Style.Revolver => new Color(1f, 0.32f, 0.22f),
            Style.Pellet => new Color(1f, 0.5f, 0.25f),
            Style.Sniper => new Color(1f, 0.15f, 0.2f),
            Style.Needle => new Color(1f, 0.78f, 0.3f),
            Style.Numeral => new Color(0.95f, 0.55f, 1f),
            _ => new Color(0.78f, 0.32f, 0.98f),
        };

        static Material Glow()
        {
            if (glowMat) return glowMat;
            dotTex = new Texture2D(64, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                {
                    float dx = (x + 0.5f) / 32f - 1, dy = (y + 0.5f) / 32f - 1;
                    float a = Mathf.Clamp01(1 - Mathf.Sqrt(dx * dx + dy * dy));
                    dotTex.SetPixel(x, y, new Color(1, 1, 1, a * a));
                }
            dotTex.Apply();
            var sh = Shader.Find("CatTang/AdditiveGlow") ?? Shader.Find("Sprites/Default");
            glowMat = new Material(sh) { mainTexture = dotTex };
            if (glowMat.HasProperty("_Intensity")) glowMat.SetFloat("_Intensity", 1.6f);
            return glowMat;
        }

        static Material Body(Color c)
        {
            if (bodyMats.TryGetValue(c, out var m) && m) return m;
            var sh = Shader.Find("CatTang/Toon") ?? Shader.Find("Universal Render Pipeline/Unlit");
            m = new Material(sh);
            m.SetColor("_BaseColor", c);
            if (m.HasProperty("_OutlineWidth")) m.SetFloat("_OutlineWidth", 3f);
            if (m.HasProperty("_OutlineColor")) m.SetColor("_OutlineColor", new Color(0.18f, 0.04f, 0.1f));
            bodyMats[c] = m;
            return m;
        }

        /// <summary>range > 0이면 그 거리만큼 날아가면 사라짐 (산탄 사거리). hour는 숫자 탄의 시각(1~12)</summary>
        public static EnemyProjectile Spawn(Vector3 pos, Vector3 dir, float speed, float damage, float range = 0, Style style = Style.Orb, int hour = 0)
        {
            dir.y = 0;
            dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.forward;
            var root = new GameObject("EnemyBullet_" + style);
            root.transform.position = pos;
            root.transform.rotation = Quaternion.LookRotation(dir);
            var c = ColorOf(style);

            // 몸통
            GameObject body = null;
            switch (style)
            {
                case Style.Revolver: body = Shape(PrimitiveType.Capsule, root.transform, new Vector3(0.2f, 0.2f, 0.42f), c); break;
                case Style.Pellet: body = Shape(PrimitiveType.Sphere, root.transform, Vector3.one * 0.22f, c); break;
                case Style.Sniper: body = Shape(PrimitiveType.Capsule, root.transform, new Vector3(0.2f, 0.2f, 0.9f), c); break;
                case Style.Needle:
                    body = Shape(PrimitiveType.Cube, root.transform, new Vector3(0.09f, 0.06f, 0.85f), c);
                    Shape(PrimitiveType.Sphere, root.transform, Vector3.one * 0.18f, new Color(0.35f, 0.2f, 0.12f)).transform.localPosition = new Vector3(0, 0, -0.38f);
                    break;
                case Style.Numeral:
                {
                    var t = new GameObject("Numeral");
                    t.transform.SetParent(root.transform, false);
                    t.transform.localPosition = Vector3.up * 0.05f;
                    var tm = t.AddComponent<TextMesh>();
                    var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    tm.font = font;
                    t.GetComponent<MeshRenderer>().sharedMaterial = font.material;
                    tm.text = Roman[Mathf.Clamp(hour, 1, 12) - 1];
                    tm.fontSize = 64; tm.characterSize = 0.07f; tm.fontStyle = FontStyle.Bold;
                    tm.anchor = TextAnchor.MiddleCenter; tm.color = new Color(1f, 0.92f, 1f);
                    t.AddComponent<Billboard>();
                    body = Shape(PrimitiveType.Sphere, root.transform, Vector3.one * 0.3f, c);
                    break;
                }
                default: body = Shape(PrimitiveType.Sphere, root.transform, Vector3.one * 0.42f, c); break;
            }

            // 빛 번짐 (카메라를 보는 가산 점)
            var halo = GameObject.CreatePrimitive(PrimitiveType.Quad);
            halo.name = "Halo";
            Destroy(halo.GetComponent<Collider>());
            halo.transform.SetParent(root.transform, false);
            float hs = style == Style.Pellet ? 0.55f : style == Style.Sniper ? 1.1f : style == Style.Numeral ? 1.1f : 0.85f;
            halo.transform.localScale = Vector3.one * hs;
            var hr = halo.GetComponent<MeshRenderer>();
            hr.sharedMaterial = Glow();
            hr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            SetVertexColor(halo, c);
            halo.AddComponent<Billboard>();

            // 꼬리
            var tr = root.AddComponent<TrailRenderer>();
            tr.sharedMaterial = Glow();
            tr.time = style switch { Style.Sniper => 0.35f, Style.Pellet => 0.08f, Style.Revolver => 0.14f, Style.Needle => 0.18f, _ => 0.22f };
            tr.widthMultiplier = style switch { Style.Sniper => 0.32f, Style.Pellet => 0.12f, Style.Needle => 0.14f, _ => 0.22f };
            tr.widthCurve = AnimationCurve.Linear(0, 1, 1, 0);
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.Lerp(c, Color.white, 0.35f), 0), new GradientColorKey(c, 1) },
                      new[] { new GradientAlphaKey(0.9f, 0), new GradientAlphaKey(0f, 1) });
            tr.colorGradient = g;
            tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            tr.minVertexDistance = 0.05f;

            var p = root.AddComponent<EnemyProjectile>();
            p.dir = dir;
            p.speed = speed;
            p.damage = damage;
            p.style = style;
            p.halo = halo.transform;
            p.haloSize = hs;
            p.born = Time.time;
            p.dieAt = Time.time + 6f;
            p.rangeLeft = range > 0 ? range : float.MaxValue;
            return p;
        }

        static GameObject Shape(PrimitiveType t, Transform parent, Vector3 scale, Color c)
        {
            var go = GameObject.CreatePrimitive(t);
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localScale = scale;
            if (t == PrimitiveType.Capsule) go.transform.localRotation = Quaternion.Euler(90, 0, 0);   // 캡슐 길이 방향 = 진행 방향
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = Body(c);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        // 쿼드 정점 색 = 탄 색 (가산 셰이더가 정점 색을 곱함)
        static void SetVertexColor(GameObject quad, Color c)
        {
            var mf = quad.GetComponent<MeshFilter>();
            var mesh = Instantiate(mf.sharedMesh);
            var cols = new Color[mesh.vertexCount];
            for (int i = 0; i < cols.Length; i++) cols[i] = c;
            mesh.colors = cols;
            mf.sharedMesh = mesh;
        }

        void Update()
        {
            float step = speed * Time.deltaTime;
            if (Physics.Raycast(transform.position, dir, out var hit, step + 0.2f, ~0, QueryTriggerInteraction.Ignore))
            {
                var hp = hit.collider.GetComponentInParent<Health>();
                if (hp == null) { Pop(hit.point); return; }                                                     // 벽
                // 엄폐물 · 화약통은 적 탄을 막음 (맞으면 깎임)
                if (!hp.isPlayer && !hp.GetComponent<Enemy>()) { hp.TakeDamage(damage); Pop(hit.point); return; }
            }
            transform.position += dir * step;
            rangeLeft -= step;
            if (rangeLeft <= 0) { Pop(transform.position, true); return; }
            // 빛 번짐이 살짝 일렁임
            if (halo) halo.localScale = Vector3.one * haloSize * (1f + 0.12f * Mathf.Sin((Time.time - born) * 30f));
            var pl = PlayerController.Instance;
            if (pl && pl.health.Alive)
            {
                var d = pl.transform.position + Vector3.up * 0.9f - transform.position;
                float hitR = style == Style.Pellet ? 0.45f : 0.55f;
                if (d.sqrMagnitude < hitR * hitR)
                {
                    pl.health.TakeDamage(damage);
                    Pop(transform.position);
                    return;
                }
            }
            if (Time.time > dieAt) Pop(transform.position, true);
        }

        /// <summary>사라짐: 맞으면 작은 불꽃, 사거리 끝이면 조용히</summary>
        void Pop(Vector3 at, bool quiet = false)
        {
            if (!quiet) FxBurst.Spawn(at, style == Style.Pellet ? 0.6f : 0.9f, FxBurst.Kind.Spark, ColorOf(style));
            // 꼬리는 남겨 자연스럽게 사라지게
            var tr = GetComponent<TrailRenderer>();
            if (tr)
            {
                foreach (Transform ch in transform) Destroy(ch.gameObject);
                tr.emitting = false;
                enabled = false;
                Destroy(gameObject, tr.time);
                return;
            }
            Destroy(gameObject);
        }
    }
}
