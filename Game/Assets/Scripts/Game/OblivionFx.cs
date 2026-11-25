// 망각 이펙트 (2026-11-18 사용자 확정: "6단계부턴 아예 꽃으로, 얼음 감옥에서 풀릴 때도 깨지게 ㄱ" → 게임에 연결)
// 연결: PlayerController TimeLock 효과 → OblivionFx.Lock / TimeLockEnded 폭발 겉모습 = 꽃 깨짐 / GunkataFx 망각 타격 불똥 끔
// 사용자 방향: 불똥은 빼고 얼음에 갇히는 느낌만 / 얼음 감옥 크기는 몬스터 크기에 비례 / 깨질 땐 더 화려하게 /
//              6단계(풀릴 때 폭발)는 얼음 꽃을 피웠다가 깨뜨리는 느낌 (스컬 정수 '나이아스' 참고)
//  - Hit: 지금 망각 타격(얼음 가시 박혔다 깨짐)에서 파란 불똥(BakedSpark)만 뺌
//  - 1~5단계 IceLock: 가둠 순간 발밑에서 반투명 각진 얼음 덩어리(fx_ice_block)가 0.14초에 솟아 몸(모델 크기 그대로)을 덮음 → 갇힌 동안 유지 →
//    풀리거나 죽으면 하얗게 번쩍하며 조각으로 깨짐
//  - 6단계 IceBloom: 감옥 대신 가둠 순간 블렌더로 구운 별 모양 얼음 수정 무더기(FX/IceBloom/fx_icebloom_0~2)가 땅에서 솟음 (2026-11-25, 전엔 꽃잎 16장 + 바닥 그림) → 갇힌 동안 유지 →
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

    /// <summary>6단계 가둠: 감옥 대신 얼음 꽃 (2026-11-18 사용자: 6단계부턴 아예 꽃으로) — 가둠 순간 피어 갇힌 동안 유지 → 풀리면 하얗게 차올라 크게 깨짐
    /// 2026-11-25 사용자: "보스 땅 내려찍을 때 나오는 오브젝트처럼 블렌더로 저 모양으로 3D 구워서" → 바닥에 눕힌 별 그림 · 꽃잎 16장 대신
    ///   블렌더로 구운 별 모양 얼음 수정 무더기(Resources/FX/IceBloom/fx_icebloom_0~2, Art/tools/blender_fx_icebloom.py)가 땅에서 솟음 (땅 솟음 Upheaval과 같은 방식). 깨짐 그림(4~7장)은 그대로</summary>
    public class IceBloom : MonoBehaviour
    {
        static GameObject[] src;
        static Material frostMat, bodyMat, tipMat;
        Transform cluster;
        Renderer[] clusterR;
        // 2026-11-25 사용자: "갑자기 생성 말고 얼음꽃 피어나듯이 얼어버리는 느낌으로 아래부터" → 수정 조각마다 밑동에서부터 차례로 자람
        //   조각 이름 c_<지연ms>_<번호> (블렌더에서 안쪽 → 중간 → 바깥, 빙글 도는 차례), frost = 바닥 서리판 (가운데부터 번짐)
        struct Grow { public Transform t; public Vector3 full; public float delay, len; }
        readonly List<Grow> grows = new();
        MaterialPropertyBlock mpb;
        Transform target;
        Health hp;
        MeshRenderer card;
        Material[] cardFrames;
        Vector3 ground, size, offset;
        float born, radius, minDur, releasedAt = -1f;
        bool broken;
        const float Open = 0.58f, Charge = 0.1f;   // Open = 다 피는 데 걸리는 시간 (바깥 칼날 끝 0.38 + 자람 0.2)
        static readonly int ShallowId = Shader.PropertyToID("_Shallow"), DeepId = Shader.PropertyToID("_Deep");

        static GameObject Src()
        {
            if (src == null)
            {
                src = new GameObject[3];
                for (int i = 0; i < 3; i++) src[i] = Resources.Load<GameObject>("FX/IceBloom/fx_icebloom_" + i);
                // 2026-11-25 사용자: "얼음 좀 반투명하게 해서 얼음 느낌" → 몸은 속이 비치게(알파 0.42) + 가장자리 흰 테 · 끝 결정면은 조금 더 차게(0.62) · 바닥 서리판은 옅게 비침(0.3)
                var baseIce = OblivionFx.IceMat(false);
                if (baseIce)
                {
                    bodyMat = new Material(baseIce) { name = "IceBloomBody" };
                    bodyMat.SetFloat("_Alpha", 0.42f); bodyMat.SetColor("_Rim", Color.white); bodyMat.SetFloat("_OutlinePx", 1.8f);
                    bodyMat.SetColor("_Tint", new Color(0.8f, 0.92f, 1f));
                    tipMat = new Material(baseIce) { name = "IceBloomTip" };
                    tipMat.SetFloat("_Alpha", 0.62f); tipMat.SetColor("_Rim", Color.white); tipMat.SetFloat("_OutlinePx", 1.8f);
                    tipMat.SetColor("_Tint", new Color(0.95f, 0.99f, 1f));
                    frostMat = new Material(baseIce) { name = "IceBloomFrost" };
                    frostMat.SetFloat("_Alpha", 0.3f); frostMat.SetFloat("_OutlinePx", 0f);
                    frostMat.SetColor("_Tint", new Color(0.85f, 0.97f, 1f));
                }
            }
            return src[Random.Range(0, 3)];
        }

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
            mpb = new MaterialPropertyBlock();
            var pf = Src();
            if (pf)
            {
                var go = Instantiate(pf, transform);
                go.name = "Cluster";
                cluster = go.transform;
                cluster.localRotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
                cluster.localScale = Vector3.one * radius;
                foreach (Transform c in cluster)
                {
                    float d = 0f, len = 0.2f;
                    if (c.name.StartsWith("frost")) { d = 0f; len = 0.42f; }
                    else if (c.name.StartsWith("c_") && c.name.Length >= 6 && int.TryParse(c.name.Substring(2, 4), out int ms)) d = ms / 1000f;
                    grows.Add(new Grow { t = c, full = c.localScale, delay = d + 0.03f, len = len });
                    c.localScale = Vector3.zero;
                }
                clusterR = go.GetComponentsInChildren<Renderer>();
                foreach (var r in clusterR)
                {
                    var ms = r.sharedMaterials;
                    for (int i = 0; i < ms.Length; i++)
                    {
                        string n = ms[i] ? ms[i].name : "";
                        ms[i] = n.Contains("Frost") ? frostMat : n.Contains("Tip") ? tipMat : bodyMat;
                    }
                    r.sharedMaterials = ms;
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;   // 반투명 얼음이라 짙은 그림자 없음
                    r.receiveShadows = false;
                }
            }
            OblivionFx.Ring(ground, 9f, radius * 0.9f, 0.18f, new Color(0.85f, 0.96f, 1f), 0.3f);
            // 2026-11-25 사용자: "터질 때 이미지 나오는 것도 제거" → 깨짐 그림판(cx_ice_bloom) 안 씀, 3D 조각 · 섬광만
        }

        /// <summary>(안 씀 — 2026-11-25 그림판 제거. card가 없으면 바로 돌아감)</summary>
        void PoseCard(float t)
        {
            if (!card) return;
            int fi = broken ? 4 + Mathf.FloorToInt((t - releasedAt - Charge) / 0.09f) : 99;
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
            if (cluster)
            {
                // 얼어붙으며 피어남: 서리판이 가운데부터 번지고 → 수정이 안쪽부터 바깥으로 빙글 돌며 밑동에서 자라 오름(끝에 살짝 넘침)
                //   → 갇힌 동안 살짝 반짝이며 숨 쉼 → 차오를 때 부풂
                foreach (var g in grows)
                {
                    if (!g.t) continue;
                    float x = Mathf.Clamp01((t - g.delay) / g.len);
                    float k = x <= 0 ? 0f : 1f - Mathf.Pow(1f - x, 3f) + Mathf.Sin(x * Mathf.PI) * 0.08f;
                    g.t.localScale = g.full * Mathf.Max(0.0001f, k);
                }
                float breathe = 1f + Mathf.Sin(t * 9f) * 0.015f + charge * 0.08f;
                cluster.localScale = Vector3.one * radius * breathe;
                mpb.SetColor(ShallowId, Color.Lerp(new Color(0.86f, 0.97f, 1f), Color.white, charge * charge));
                mpb.SetColor(DeepId, Color.Lerp(new Color(0.46f, 0.76f, 0.98f), new Color(0.75f, 0.92f, 1f), charge));
                foreach (var r in clusterR) if (r) r.SetPropertyBlock(mpb);
            }
            if (releasedAt >= 0 && t >= releasedAt + Charge) Break();
        }

        void Break()
        {
            broken = true;
            var s = IceShatter.Spawn(ground);
            string[] chunks = { "fx_ice_chunk_0", "fx_ice_chunk_1", "fx_ice_chunk_2" };
            string[] thin = { "fx_glass_0", "fx_glass_1", "fx_glass_2" };
            // 수정 무더기 자리에서 조각: 별 꼭짓점마다 바깥으로 각진 덩어리 2 + 얇은 조각 1, 안쪽 곧은 수정은 위로
            int n = 14;
            float a0 = Random.Range(0f, 360f);
            for (int i = 0; i < n; i++)
            {
                var outDir = Quaternion.Euler(0, a0 + i * 360f / n, 0) * Vector3.forward;
                for (int j = 0; j < 3; j++)
                {
                    var at = ground + outDir * radius * (0.3f + 0.25f * j) + Vector3.up * radius * (0.35f - 0.1f * j);
                    var vel = outDir * Random.Range(5f, 10f) + Vector3.up * Random.Range(3f, 7f) + Random.insideUnitSphere * 1.5f;
                    float sc = radius * (j == 2 ? 0.2f : 0.27f) * Random.Range(0.8f, 1.2f);
                    OblivionFx.Piece(s.transform, s.pieces, j == 2 ? thin[Random.Range(0, 3)] : chunks[Random.Range(0, 3)], at, vel, sc, Random.Range(0.65f, 0.95f));
                }
            }
            if (cluster) Destroy(cluster.gameObject);
            OblivionFx.Burst(this, ground, radius, Mathf.Max(size.y, 1.2f));
            OblivionFx.Release(target);
        }
    }
}
