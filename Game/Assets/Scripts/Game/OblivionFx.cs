// 망각 이펙트 (2026-11-18 사용자 확정: "6단계부턴 아예 꽃으로, 얼음 감옥에서 풀릴 때도 깨지게 ㄱ" → 게임에 연결)
// 연결: PlayerController TimeLock 효과 → OblivionFx.Lock / TimeLockEnded 폭발 겉모습 = 꽃 깨짐 / GunkataFx 망각 타격 불똥 끔
// 사용자 방향: 불똥은 빼고 얼음에 갇히는 느낌만 / 얼음 감옥 크기는 몬스터 크기에 비례 / 깨질 땐 더 화려하게 /
//              6단계(풀릴 때 폭발)는 얼음 꽃을 피웠다가 깨뜨리는 느낌 (스컬 정수 '나이아스' 참고)
//  - Hit: 지금 망각 타격(얼음 가시 박혔다 깨짐)에서 파란 불똥(BakedSpark)만 뺌
//  - 1~5단계 IceLock: 가둠 순간 발밑에서 반투명 각진 얼음 덩어리(fx_ice_block)가 0.14초에 솟아 몸(모델 크기 그대로)을 덮음 → 갇힌 동안 유지 →
//    풀리거나 죽으면 하얗게 번쩍하며 조각으로 깨짐
//  - 6단계 IceBloom: 감옥 대신 가둠 순간 얼음 꽃잎(fx_ice_petal) 바깥 10장 · 안쪽 6장이 빙글 돌아가며 피어남 (꽃 그림은 바닥에 눕혀 깖) → 갇힌 동안 유지 →
//    풀리면 하얗게 차올라 번쩍(짧은 멈춤) 깨짐: 꽃잎이 각진 얼음 조각 · 유리 조각으로 터져 튀고, 흰 별 섬광 · 반짝임 · 서리 고리 세 겹 · 흔들림
// 그림 + 3D (2026-11-18 사용자: 얼음도 Codex 그림이랑 합치는 방향으로): Codex 시트 cx_ice_lock(얼어붙는 순간 → 얼음 우리, 마지막 장은 갇힌 동안 유지) ·
//   cx_ice_bloom(얼음 꽃 피어남 → 번쩍 깨짐 → 조각 · 반짝임)을 카메라를 보는 판으로 3D 위에 겹침 (docs/images/fx_src/prompts/ice_lock.txt · ice_bloom.txt → fx_sheet_cut.py)
// 메시: Art/tools/blender_fx.py → 메뉴 CatTang/FX 메시 가져오기(플레이 중엔 막힘). 재질: CatTang/Water 셰이더에 얼음 색 (흐름 없음, 각진 면 그대로),
//       섬광 · 반짝임은 툰 불꽃(FX/ToonFire)에 얼음 색
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CatTang
{
    public static class OblivionFx
    {
        static Material iceMat, chunkMat, flashMat;
        public static readonly Color Ice = new(0.62f, 0.86f, 1f);

        public static Material IceMat(bool chunk)
        {
            if (chunk ? chunkMat : iceMat) return chunk ? chunkMat : iceMat;
            var sh = Shader.Find("CatTang/Water");
            if (!sh) return null;
            var m = new Material(sh) { name = chunk ? "IceChunk" : "IceBlock" };
            m.SetColor("_Shallow", new Color(0.86f, 0.97f, 1f));
            m.SetColor("_Deep", new Color(0.46f, 0.76f, 0.98f));
            m.SetColor("_Rim", Color.white);
            m.SetColor("_Tint", new Color(0.85f, 0.92f, 1f));
            m.SetColor("_OutlineColor", new Color(0.16f, 0.4f, 0.62f));
            m.SetFloat("_OutlinePx", chunk ? 1.6f : 2.2f);
            m.SetFloat("_Alpha", chunk ? 0.8f : 0.45f);
            m.SetFloat("_Flow", 0f);
            m.SetFloat("_FoamY", 99f);
            if (chunk) chunkMat = m; else iceMat = m;
            return m;
        }

        public static Material FlashMat()
        {
            if (flashMat) return flashMat;
            var fm = Resources.Load<Material>("FX/ToonFire");
            if (!fm) return null;
            flashMat = new Material(fm) { name = "IceFlash" };
            flashMat.SetFloat("_OutlineWidth", 1.4f);
            flashMat.SetFloat("_Intensity", 1.6f);
            flashMat.SetColor("_ColCore", Color.white);
            flashMat.SetColor("_ColHot", new Color(0.88f, 0.98f, 1f));
            flashMat.SetColor("_ColMid", new Color(0.55f, 0.85f, 1f));
            flashMat.SetColor("_ColEdge", new Color(0.25f, 0.55f, 0.9f));
            flashMat.SetColor("_ColAsh", new Color(0.18f, 0.32f, 0.55f));
            flashMat.SetColor("_OutlineColor", new Color(0.06f, 0.2f, 0.38f));
            return flashMat;
        }

        /// <summary>그림 판 (카메라를 봄, Vivid 층): 프레임 재질 배열을 받아 SetFrame으로 넘김. 시트가 없으면 null</summary>
        public static MeshRenderer ImageCard(Transform parent, string set, out Material[] frames)
        {
            frames = FxFlipbook.Frames(set);
            if (frames == null || frames.Length == 0) return null;
            var q = new GameObject("Image_" + set);
            q.transform.SetParent(parent, false);
            q.AddComponent<MeshFilter>().sharedMesh = GunkataFx.Quad;
            var r = q.AddComponent<MeshRenderer>();
            r.sharedMaterial = frames[0];
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            VividLayer.Set(q);
            return r;
        }

        static readonly int ColorId = Shader.PropertyToID("_Color");

        /// <summary>그림 판을 카메라 쪽으로 세우고 위치 · 크기 · 장 · 알파를 맞춤</summary>
        public static void PoseCard(MeshRenderer r, Material[] frames, MaterialPropertyBlock mpb, Vector3 at, float size, int frame, float alpha)
        {
            if (!r) return;
            var cam = Camera.main;
            r.enabled = frame >= 0 && frame < frames.Length && alpha > 0.01f;
            if (!r.enabled) return;
            r.sharedMaterial = frames[frame];
            if (cam)
            {
                r.transform.rotation = cam.transform.rotation;
                at += (cam.transform.position - at).normalized * 0.6f;              // 3D 얼음보다 살짝 앞
            }
            r.transform.position = at;
            r.transform.localScale = Vector3.one * size;
            mpb.SetColor(ColorId, new Color(1, 1, 1, alpha));
            r.SetPropertyBlock(mpb);
        }

        /// <summary>망각 타격 (불똥 뺌)</summary>
        public static void Hit(Vector3 p, Vector3 dir, float scale = 0.8f)
        {
            var look = Quaternion.LookRotation(dir.sqrMagnitude > 1e-4f ? dir.normalized : Vector3.forward, Vector3.up);
            var cam = Camera.main;
            if (cam) p += (cam.transform.position - p).normalized * 0.7f;
            var fx = FxEffect.Spawn("FX_Trait_Oblivion", p, look, scale, GunkataFx.TraitColor(3003, Color.white));
            if (!fx) return;
            var spark = fx.transform.Find("BakedSpark");
            if (spark) spark.gameObject.SetActive(false);
        }

        /// <summary>몸 범위: 모델(자식 "Model")의 렌더러만 (체력 바 · 표식 · 그림자 빼고). 없으면 전체</summary>
        public static Bounds BodyBounds(Transform t)
        {
            var root = t.Find("Model") ?? t;
            var b = new Bounds(t.position + Vector3.up, new Vector3(1f, 2f, 1f));
            bool any = false;
            foreach (var r in root.GetComponentsInChildren<Renderer>())
            {
                if (!r.enabled || r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer || r is SpriteRenderer) continue;
                if (!(r is SkinnedMeshRenderer) && !(r is MeshRenderer)) continue;
                if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
            }
            return b;
        }

        static readonly Dictionary<Transform, MonoBehaviour> active = new();

        /// <summary>메시 · 셰이더가 있어 얼음 연출을 쓸 수 있음 (없으면 예전 폭발 그대로)</summary>
        public static bool Ready => FxMeshes.Lib("fx_ice_block") && FxMeshes.Lib("fx_ice_petal") && IceMat(false);

        /// <summary>망각 6단계(갇힌 적이 풀릴 때 시간 파편 폭발)를 가졌는지</summary>
        public static bool Tier6
        {
            get
            {
                var p = PlayerController.Instance;
                return p && Effects.Global(p, "OnTimeLockEnd").Any(e => e.Type == "Explosion");
            }
        }

        /// <summary>게임: 적이 시간에 갇힐 때 (PlayerController). 이미 얼어 있으면 그대로 (갇힘이 연장되면 Health.TimeLocked를 보고 같이 늘어남)</summary>
        public static void Lock(Transform target) => Encase(target, 0f, Tier6);

        /// <summary>가둠 연출. minDur = 적어도 유지할 시간(미리보기용, 게임은 0 → Health.TimeLocked가 풀릴 때까지). bloom = 6단계 얼음 꽃</summary>
        public static MonoBehaviour Encase(Transform target, float minDur, bool bloom)
        {
            if (!target || !Ready) return null;
            if (active.TryGetValue(target, out var cur) && cur) return cur;
            var go = new GameObject(bloom ? "IceBloom" : "IceLock");
            MonoBehaviour fx;
            if (bloom) { var b = go.AddComponent<IceBloom>(); b.Begin(target, minDur); fx = b; }
            else { var l = go.AddComponent<IceLock>(); l.Begin(target, minDur); fx = l; }
            active[target] = fx;
            return fx;
        }

        public static void Release(Transform target) { if (target) active.Remove(target); else foreach (var k in active.Keys.Where(k => !k).ToList()) active.Remove(k); }

        public static void Ring(Vector3 ground, float speed, float maxR, float width, Color c, float startR)
            => RippleRing.Spawn(ground + Vector3.up * 0.03f, speed, 0f, maxR, width, c, startR);

        /// <summary>얼음 조각 하나 (각진 덩어리 · 얇은 유리 조각)</summary>
        public static void Piece(Transform parent, List<IceShatter.Piece> list, string mesh, Vector3 at, Vector3 vel, float size, float life)
        {
            var m = FxMeshes.Lib(mesh);
            if (!m) return;
            var go = new GameObject("IcePiece");
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(at, Random.rotation);
            go.transform.localScale = Vector3.one * size;
            go.AddComponent<MeshFilter>().sharedMesh = m;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = IceMat(true);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            list.Add(new IceShatter.Piece { t = go.transform, r = r, b = new MaterialPropertyBlock(), v = vel, spin = Random.onUnitSphere * Random.Range(360f, 900f), life = life });
        }

        /// <summary>깨지는 순간 공통: 흰 별 섬광 · 반짝임 · 서리 고리 세 겹 · 흔들림 · 짧은 멈춤</summary>
        public static void Burst(MonoBehaviour host, Vector3 ground, float radius, float height)
        {
            var mat = FlashMat();
            var cam = Camera.main;
            var c = ground + Vector3.up * height * 0.55f;
            var face = cam ? Quaternion.LookRotation(cam.transform.position - c) : Quaternion.identity;
            if (mat)
            {
                FxMesh.Spawn(FxMeshes.Lib("fx_star"), mat, c, face * Quaternion.Euler(0, 0, Random.Range(0f, 90f)), Vector3.one * radius * 1.5f, 0.2f, 0f, Vector3.zero, FxMesh.Kind.Core);
                FxMesh.Spawn(FxMeshes.Ball(0), mat, c, Random.rotation, Vector3.one * radius * 0.7f, 0.12f, 0f, Vector3.zero, FxMesh.Kind.Core);
                for (int i = 0; i < 9; i++)
                {
                    var d = Random.onUnitSphere; d.y = Mathf.Abs(d.y) * 0.8f;
                    FxMesh.Spawn(FxMeshes.Lib("fx_sparkle"), mat, c + d * radius * Random.Range(0.3f, 0.9f), face, Vector3.one * Random.Range(0.35f, 0.6f), Random.Range(0.25f, 0.4f), Random.Range(0f, 0.12f), d * 1.5f, FxMesh.Kind.Core);
                }
            }
            Ring(ground, 16f, radius * 1.15f, 0.4f, Color.white, radius * 0.3f);
            Ring(ground, 11f, radius * 0.9f, 0.25f, Ice, radius * 0.2f);
            Ring(ground, 7f, radius * 0.6f, 0.15f, new Color(0.8f, 0.95f, 1f), 0.2f);
            CameraFollow.Shake(0.16f);
            host.StartCoroutine(Stop(0.06f));
        }

        static IEnumerator Stop(float real)
        {
            if (Time.timeScale < 0.99f) yield break;
            Time.timeScale = 0.05f;
            yield return new WaitForSecondsRealtime(real);
            if (Time.timeScale > 0f && Time.timeScale < 0.06f) Time.timeScale = 1f;
        }
    }

    /// <summary>날아가는 얼음 조각 묶음 (깨질 때 공통): 중력 · 바닥에 한 번 튐 · 돌며 옅어짐</summary>
    public class IceShatter : MonoBehaviour
    {
        public class Piece { public Transform t; public MeshRenderer r; public MaterialPropertyBlock b; public Vector3 v, spin; public float life; }
        public readonly List<Piece> pieces = new();
        float born, floorY;
        static readonly int FadeId = Shader.PropertyToID("_Fade");

        public static IceShatter Spawn(Vector3 ground)
        {
            var go = new GameObject("IceShatter");
            go.transform.position = ground;
            var s = go.AddComponent<IceShatter>();
            s.born = Time.time; s.floorY = ground.y;
            return s;
        }

        /// <summary>둘레로 각진 덩어리 · 얇은 조각을 n개 흩뿌림 (w = 폭, h = 높이)</summary>
        public void Scatter(Vector3 ground, float w, float h, int n, float speed)
        {
            string[] chunks = { "fx_ice_chunk_0", "fx_ice_chunk_1", "fx_ice_chunk_2" };
            string[] thin = { "fx_glass_0", "fx_glass_1", "fx_glass_2" };
            for (int j = 0; j < n; j++)
            {
                var d = Random.onUnitSphere; d.y = Mathf.Abs(d.y);
                var flat = new Vector3(d.x, 0, d.z).normalized;
                var at = ground + flat * w * Random.Range(0.15f, 0.45f) + Vector3.up * h * Random.Range(0.15f, 0.95f);
                bool isThin = j % 3 == 2;
                OblivionFx.Piece(transform, pieces, isThin ? thin[j % 3] : chunks[j % 3], at,
                    flat * Random.Range(0.5f, 1f) * speed + Vector3.up * Random.Range(3f, 7f), w * (isThin ? Random.Range(0.3f, 0.45f) : Random.Range(0.3f, 0.5f)), Random.Range(0.6f, 0.9f));
            }
        }

        void Update()
        {
            float t = Time.time - born, dt = Time.deltaTime;
            bool alive = false;
            foreach (var p in pieces)
            {
                if (!p.t) continue;
                if (t > p.life) { Destroy(p.t.gameObject); continue; }
                alive = true;
                p.v.y -= 20f * dt;
                p.t.position += p.v * dt;
                if (p.t.position.y < floorY + 0.08f && p.v.y < 0) { p.v.y *= -0.3f; p.v.x *= 0.6f; p.v.z *= 0.6f; p.t.position = new Vector3(p.t.position.x, floorY + 0.08f, p.t.position.z); }
                p.t.rotation = Quaternion.Euler(p.spin * dt) * p.t.rotation;
                p.b.SetFloat(FadeId, 1f - Mathf.Clamp01((t - p.life * 0.6f) / (p.life * 0.4f)));
                p.r.SetPropertyBlock(p.b);
            }
            if (!alive && t > 0.2f) Destroy(gameObject);
        }
    }

    /// <summary>1~5단계 가둠: 얼음 감옥 — 갇힌 동안 유지(Health.TimeLocked), 풀리거나 죽으면 깨짐 (2026-11-18 사용자: 풀릴 때도 깨지게)</summary>
    public class IceLock : MonoBehaviour
    {
        Transform target, block;
        Health hp;
        MeshRenderer r, card;
        Material[] cardFrames;
        MaterialPropertyBlock cardB;   // 필드 초기화에서 만들면 Unity 오류 → Begin에서
        MaterialPropertyBlock b;
        Vector3 full, offset;
        float born, minDur;
        static readonly int FadeId = Shader.PropertyToID("_Fade"), ShallowId = Shader.PropertyToID("_Shallow");

        public void Begin(Transform t, float minimum)
        {
            target = t; hp = t.GetComponent<Health>(); minDur = minimum; born = Time.time;
            var bb = OblivionFx.BodyBounds(t);
            // 몬스터 크기에 비례 (2026-11-18 사용자): 몸 폭 · 높이에 살짝 여유만 (보스는 너무 커지지 않게)
            float w = Mathf.Clamp(Mathf.Max(bb.size.x, bb.size.z) * 1.05f, 0.6f, 6f);
            full = new Vector3(w, Mathf.Clamp((bb.max.y - t.position.y) * 1.08f, 0.8f, 8f), w);
            offset = new Vector3(bb.center.x - t.position.x, 0, bb.center.z - t.position.z);
            transform.position = new Vector3(bb.center.x, t.position.y, bb.center.z);
            block = new GameObject("IceBlock").transform;
            block.SetParent(transform, false);
            block.localRotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
            block.gameObject.AddComponent<MeshFilter>().sharedMesh = FxMeshes.Lib("fx_ice_block");
            r = block.gameObject.AddComponent<MeshRenderer>();
            r.sharedMaterial = OblivionFx.IceMat(false);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            b = new MaterialPropertyBlock();
            cardB = new MaterialPropertyBlock();
            card = OblivionFx.ImageCard(transform, "cx_ice_lock", out cardFrames);
            Pose(0f);
            OblivionFx.Ring(new Vector3(transform.position.x, t.position.y, transform.position.z), 10f, w * 1.1f, 0.15f, new Color(0.85f, 0.96f, 1f), w * 0.3f);
        }

        void Pose(float t)
        {
            float k = Mathf.Clamp01(t / 0.14f);
            float grow = 1f - (1f - k) * (1f - k) * (1f - k) + Mathf.Sin(Mathf.Clamp01((t - 0.14f) / 0.12f) * Mathf.PI) * 0.05f;
            block.localScale = new Vector3(full.x * (0.75f + 0.25f * k), full.y * Mathf.Max(0.02f, grow), full.z * (0.75f + 0.25f * k));
            float white = 1f - Mathf.Clamp01((t - 0.05f) / 0.25f);
            b.SetColor(ShallowId, Color.Lerp(new Color(0.86f, 0.97f, 1f), Color.white, white));
            b.SetFloat(FadeId, 1f);
            r.SetPropertyBlock(b);
            if (card)
            {
                int n = cardFrames.Length;
                int fi = Mathf.Min(n - 1, Mathf.FloorToInt(t / 0.36f * (n - 1)));
                float sz = Mathf.Max(full.x, full.y) * 1.45f;
                OblivionFx.PoseCard(card, cardFrames, cardB, transform.position + Vector3.up * sz * 0.42f, sz, fi, 1f);
            }
        }

        bool Holding(float t) => target && (hp == null || (hp.Alive && (hp.TimeLocked || t < minDur)));

        void Update()
        {
            float t = Time.time - born;
            if (target) transform.position = new Vector3(target.position.x + offset.x, target.position.y, target.position.z + offset.z);
            if (!Holding(t)) { Shatter(); return; }
            Pose(t);
        }

        /// <summary>풀림 · 죽음: 감옥이 하얗게 번쩍하며 조각으로 깨짐 (6단계 꽃보다 작게)</summary>
        void Shatter()
        {
            var ground = transform.position;
            var s = IceShatter.Spawn(ground);
            s.Scatter(ground, full.x, full.y, 12, 5.5f);
            var mat = OblivionFx.FlashMat();
            var cam = Camera.main;
            var c = ground + Vector3.up * full.y * 0.55f;
            if (mat) FxMesh.Spawn(FxMeshes.Lib("fx_star"), mat, c, (cam ? Quaternion.LookRotation(cam.transform.position - c) : Quaternion.identity) * Quaternion.Euler(0, 0, Random.Range(0f, 90f)), Vector3.one * full.x * 1.1f, 0.15f, 0f, Vector3.zero, FxMesh.Kind.Core);
            OblivionFx.Ring(ground, 12f, full.x * 1.3f, 0.25f, Color.white, full.x * 0.3f);
            CameraFollow.Shake(0.06f);
            OblivionFx.Release(target);
            Destroy(gameObject);
        }
    }

    /// <summary>6단계 가둠: 감옥 대신 얼음 꽃 (2026-11-18 사용자: 6단계부턴 아예 꽃으로) — 가둠 순간 피어 갇힌 동안 유지 → 풀리면 하얗게 차올라 크게 깨짐</summary>
    public class IceBloom : MonoBehaviour
    {
        class Petal { public Transform t; public MeshRenderer r; public MaterialPropertyBlock b; public float delay, len, tilt, yaw; }
        readonly List<Petal> petals = new();
        Transform target;
        Health hp;
        MeshRenderer card;
        Material[] cardFrames;
        Vector3 ground, size, offset;
        float born, radius, minDur, releasedAt = -1f;
        bool broken;
        const float Open = 0.24f, Charge = 0.1f;
        static readonly int ShallowId = Shader.PropertyToID("_Shallow"), DeepId = Shader.PropertyToID("_Deep");

        public void Begin(Transform t, float minimum)
        {
            target = t; hp = t.GetComponent<Health>(); minDur = minimum; born = Time.time;
            var bb = OblivionFx.BodyBounds(t);
            float w = Mathf.Max(bb.size.x, bb.size.z);
            size = new Vector3(w, bb.max.y - t.position.y, w);
            radius = Mathf.Clamp(w * 1.4f, 1.4f, 3.2f);                                       // 몸에 맞추되 폭발 반경(3m) 안
            offset = new Vector3(bb.center.x - t.position.x, 0, bb.center.z - t.position.z);
            ground = new Vector3(bb.center.x, t.position.y, bb.center.z);
            transform.position = ground;
            var mesh = FxMeshes.Lib("fx_ice_petal");
            float a0 = Random.Range(0f, 360f);
            // 바깥 10장 (눕듯 벌어짐) · 안쪽 6장 (곧게 서서 몸을 감쌈), 빙글 돌아가는 차례로 피어남
            for (int i = 0; i < 16; i++)
            {
                bool outer = i < 10;
                int k = outer ? i : i - 10;
                int n = outer ? 10 : 6;
                var p = new Petal
                {
                    yaw = a0 + k * 360f / n + (outer ? 0f : 30f) + Random.Range(-6f, 6f),
                    tilt = outer ? Random.Range(58f, 68f) : Random.Range(18f, 28f),
                    len = outer ? radius * Random.Range(0.85f, 1f) : Mathf.Max(radius * 0.6f, size.y * Random.Range(0.75f, 0.9f)),
                    delay = outer ? k * 0.014f : 0.05f + k * 0.012f,
                };
                var go = new GameObject("Petal");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = Quaternion.Euler(0, p.yaw, 0) * Vector3.forward * w * (outer ? 0.45f : 0.42f);
                go.transform.localRotation = Quaternion.Euler(0, p.yaw, 0) * Quaternion.Euler(p.tilt, 0, 0);   // +Y 꽃잎을 바깥(+Z) 쪽으로 기울임
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                p.r = go.AddComponent<MeshRenderer>();
                p.r.sharedMaterial = OblivionFx.IceMat(false);
                p.r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                p.r.receiveShadows = false;
                p.b = new MaterialPropertyBlock();
                p.t = go.transform;
                p.t.localScale = new Vector3(0.001f, 0.001f, 0.001f);
                petals.Add(p);
            }
            OblivionFx.Ring(ground, 9f, radius * 0.9f, 0.18f, new Color(0.85f, 0.96f, 1f), 0.3f);
            card = OblivionFx.ImageCard(transform, "cx_ice_bloom", out cardFrames);
        }

        /// <summary>꽃 그림은 바닥에 눕혀 깖 (몬스터가 꽃 한가운데 서 있게): 피어남 0~2장 → 갇힌 동안 2장 → 차오름 3장 → 깨짐 4~7장(0.36초)</summary>
        void PoseCard(float t)
        {
            if (!card) return;
            int fi;
            if (releasedAt < 0) fi = Mathf.Min(2, Mathf.FloorToInt(t / Open * 3f));
            else if (!broken) fi = 3;
            else fi = 4 + Mathf.FloorToInt((t - releasedAt - Charge) / 0.09f);
            card.enabled = fi < cardFrames.Length;
            if (!card.enabled) return;
            card.sharedMaterial = cardFrames[fi];
            card.transform.SetPositionAndRotation(ground + Vector3.up * 0.04f, Quaternion.Euler(90f, 0f, 0f));
            card.transform.localScale = Vector3.one * radius * 2.4f;
        }

        bool Holding(float t) => target && (hp == null || (hp.Alive && (hp.TimeLocked || t < minDur)));

        void Update()
        {
            float t = Time.time - born;
            if (target && !broken) { ground = new Vector3(target.position.x + offset.x, target.position.y, target.position.z + offset.z); transform.position = ground; }
            if (releasedAt < 0 && !Holding(t)) releasedAt = Mathf.Max(t, Open);                  // 다 피기 전에 풀려도 피고 나서 깨짐
            PoseCard(t);
            if (broken) { if (t > releasedAt + Charge + 0.45f) Destroy(gameObject); return; }
            float charge = releasedAt < 0 ? 0f : Mathf.Clamp01((t - releasedAt) / Charge);
            var shallow = Color.Lerp(new Color(0.86f, 0.97f, 1f), Color.white, charge * charge);
            var deep = Color.Lerp(new Color(0.46f, 0.76f, 0.98f), new Color(0.75f, 0.92f, 1f), charge);
            float shimmer = 1f + Mathf.Sin(t * 9f) * 0.015f;                                    // 갇힌 동안 살짝 반짝이며 숨 쉼
            foreach (var p in petals)
            {
                float k = Mathf.Clamp01((t - p.delay) / 0.12f);
                float g = 1f - (1f - k) * (1f - k) * (1f - k) + Mathf.Sin(Mathf.Clamp01((t - p.delay - 0.12f) / 0.1f) * Mathf.PI) * 0.06f;
                float w = p.len * 0.55f;
                p.t.localScale = new Vector3(w * Mathf.Max(0.001f, k), p.len * Mathf.Max(0.001f, g) * shimmer, w * Mathf.Max(0.001f, k));
                p.b.SetColor(ShallowId, shallow); p.b.SetColor(DeepId, deep);
                p.r.SetPropertyBlock(p.b);
            }
            if (releasedAt >= 0 && t >= releasedAt + Charge) Break();
        }

        void Break()
        {
            broken = true;
            var s = IceShatter.Spawn(ground);
            string[] chunks = { "fx_ice_chunk_0", "fx_ice_chunk_1", "fx_ice_chunk_2" };
            string[] thin = { "fx_glass_0", "fx_glass_1", "fx_glass_2" };
            // 꽃잎마다 길이를 따라 조각 3개 (각진 덩어리 2 + 얇은 조각 1)
            foreach (var p in petals)
            {
                var up = p.t.up;
                var outDir = new Vector3(up.x, 0, up.z).normalized;
                for (int j = 0; j < 3; j++)
                {
                    var at = p.t.position + up * p.len * (0.25f + 0.3f * j);
                    var vel = outDir * Random.Range(5f, 10f) + Vector3.up * Random.Range(3f, 7f) + Random.insideUnitSphere * 1.5f;
                    float sc = p.len * (j == 2 ? 0.22f : 0.3f) * Random.Range(0.8f, 1.2f);
                    OblivionFx.Piece(s.transform, s.pieces, j == 2 ? thin[Random.Range(0, 3)] : chunks[Random.Range(0, 3)], at, vel, sc, Random.Range(0.65f, 0.95f));
                }
                Destroy(p.t.gameObject);
            }
            OblivionFx.Burst(this, ground, radius, Mathf.Max(size.y, 1.2f));
            OblivionFx.Release(target);
        }
    }
}
