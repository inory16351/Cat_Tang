// 챕터 모듈 키트 (Tripo 모델): Resources/Kit/Ch{장}/{이름}.prefab
// - 맵은 통째로 만들지 않고 타일 조각을 이어 붙여 조립함: 벽 모듈을 벽 길이만큼 반복, 바닥 타일을 격자로 깔기
// - 소품: 화약통(쏘면 터짐) · 부서지는 엄폐물(마차 · 상자 · 울타리) · 녹은 시계 장식 · 술집 카운터(상인)
// 키트가 없으면 RoomBuilder가 예전처럼 도형으로 만듦.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    public static class Kit
    {
        static readonly Dictionary<string, GameObject> cache = new();

        public static GameObject Prefab(ChapterDef ch, string name)
        {
            if (ch == null) return null;
            string key = $"Kit/Ch{ch.Id}/{name}";
            if (!cache.TryGetValue(key, out var go)) cache[key] = go = Resources.Load<GameObject>(key);
            return go;
        }

        public static bool Has(ChapterDef ch, string name) => Prefab(ch, name);

        public static Bounds LocalBounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(Vector3.zero, Vector3.one);
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            b.center -= go.transform.position;
            return b;
        }

        /// <summary>키트 조각 하나 놓기 (발밑 = pos, 정면 = forward)</summary>
        public static GameObject Place(ChapterDef ch, string name, Transform parent, Vector3 localPos, float yaw, float scale = 1f, bool collider = true)
        {
            var prefab = Prefab(ch, name);
            if (!prefab) return null;
            var go = Object.Instantiate(prefab, parent, false);
            go.name = name;
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            go.transform.localScale *= scale;
            if (collider && !go.GetComponentInChildren<Collider>())
            {
                var b = LocalBounds(go);
                var bc = go.AddComponent<BoxCollider>();
                // 월드 기준 크기를 로컬로 되돌림 (회전 · 크기 반영)
                bc.center = go.transform.InverseTransformVector(b.center);
                var s = go.transform.InverseTransformVector(b.size);
                bc.size = new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
            }
            return go;
        }

        /// <summary>벽 한 줄을 모듈로 채움: 충돌용 상자는 그대로 두고 그림만 모듈로 바꿈. inward = 방 안쪽 방향</summary>
        public static bool TileWall(ChapterDef ch, GameObject wallCube, Vector3 inward, string module = null, bool hideCube = true)
        {
            // 스테이지 1(황무지 절벽길)은 절벽 바위 모듈, 그 뒤(마을)는 건물 정면 모듈. module을 주면 그것 (술집 실내 = PlankWall)
            var rm = RunManager.Instance;
            bool cliff = rm && rm.stageIndex == 0 && !rm.IsMidBossRoom && Has(ch, "Cliff");
            if (module == null && !cliff && wallCube && Has(ch, Facades[0])) return BuildingRow(ch, wallCube, inward, hideCube);
            var prefab = Prefab(ch, module ?? (cliff ? "Cliff" : "Wall"));
            if (!prefab || !wallCube) return false;
            var t = wallCube.transform;
            bool alongX = t.localScale.x >= t.localScale.z;
            float len = alongX ? t.localScale.x : t.localScale.z;
            float modW = Mathf.Max(0.5f, LocalBounds(prefab).size.x);
            int n = Mathf.Max(1, Mathf.RoundToInt(len / modW));
            float w = len / n;
            var axis = alongX ? Vector3.right : Vector3.forward;
            float yaw = Quaternion.LookRotation(inward).eulerAngles.y;
            var holder = new GameObject("WallTiles").transform;
            holder.SetParent(t.parent, false);
            holder.localPosition = new Vector3(t.localPosition.x, 0, t.localPosition.z) - inward * 0.2f;
            for (int i = 0; i < n; i++)
            {
                var go = Object.Instantiate(prefab, holder, false);
                go.transform.localPosition = axis * (-len / 2 + w * (i + 0.5f));
                go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
                go.transform.localScale = new Vector3(w / modW, 1, 1);
                if (cliff && module == null)
                {
                    // 절벽도 똑같이 늘어서지 않게: 높이 · 들여쓰기 · 폭을 조금씩
                    var jr = new System.Random(Mathf.RoundToInt(t.position.x * 13 + t.position.z * 7) + i * 31);
                    float J(float lo, float hi) => lo + (float)jr.NextDouble() * (hi - lo);
                    go.transform.localScale = new Vector3(w / modW * J(1f, 1.18f), J(0.8f, 1.35f), J(0.9f, 1.15f));
                    go.transform.localPosition += -inward * J(0f, 0.6f);
                }
            }
            var r = wallCube.GetComponent<Renderer>();
            if (r && hideCube) r.enabled = false;
            return true;
        }

        // ---------- 절차적 건물 줄 (2026-11-17 사용자: 벽이 너무 일정함, 바둑판 느낌 없애기) ----------
        // 블렌더 + Codex 건물 정면 8종(Art/tools/blender_kit_facades.py, 폭 3~6m, 높이 · 지붕 모양 · 칠 색이 다 다름)을
        // 벽 길이에 맞춰 무작위로 이어 붙임: 같은 건물 연속 금지, 건물마다 높이 ±, 뒤로 물러남 0~0.7m,
        // 가끔 1~1.6m 골목(안쪽 울타리 + 상자 · 술통). 마지막 칸은 폭을 늘이거나 줄여 딱 맞춤.
        // 충돌은 원래 벽 상자 그대로. 정면 본체 앞면이 벽 상자 안쪽 면(벽 선에서 0.3)에 오게 놓음 (차양 · 현관은 방 쪽으로 0.4 더 나옴)
        public static readonly string[] Facades = { "Facade_Store", "Facade_Hotel", "Facade_Narrow", "Facade_Adobe", "Facade_Shack", "Facade_Bank", "Facade_Barn", "Facade_Blue" };
        static readonly float[] FacadeW = { 5f, 6f, 3f, 4f, 3f, 4f, 5f, 4f };
        const float FacadeFront = 1.1f;     // 정면 본체 앞면 (조각 원점 기준 +Z)

        static bool BuildingRow(ChapterDef ch, GameObject wallCube, Vector3 inward, bool hideCube)
        {
            var t = wallCube.transform;
            bool alongX = t.localScale.x >= t.localScale.z;
            float len = alongX ? t.localScale.x : t.localScale.z;
            float face = (alongX ? t.localScale.z : t.localScale.x) / 2;     // 벽 상자 반 두께
            var axis = alongX ? Vector3.right : Vector3.forward;
            float yaw = Quaternion.LookRotation(inward).eulerAngles.y;
            var holder = new GameObject("Buildings").transform;
            holder.SetParent(t.parent, false);
            holder.localPosition = new Vector3(t.localPosition.x, 0, t.localPosition.z);
            var rnd = new System.Random(Mathf.RoundToInt(t.position.x * 17.3f + t.position.z * 5.1f) ^ 0x5bd1);
            float R(float lo, float hi) => lo + (float)rnd.NextDouble() * (hi - lo);
            // 남쪽 벽(카메라 쪽, 정면이 북쪽을 봄)은 플레이어를 가리지 않게 낮춤
            float hMul = inward.z > 0.5f ? 0.72f : 1f;
            float x = -len / 2;
            int last = -1;
            bool first = true;
            while (len / 2 - x > 0.4f)
            {
                float left = len / 2 - x;
                // 골목
                if (!first && left > 4.5f && rnd.NextDouble() < 0.18)
                {
                    float gw = R(1f, 1.6f);
                    Alley(ch, holder, axis * (x + gw / 2), inward, yaw, gw, face, rnd);
                    x += gw; last = -1;
                    continue;
                }
                first = false;
                // 남은 길이에 맞는 건물 (같은 건물 연속 금지)
                var fits = new List<int>();
                for (int i = 0; i < Facades.Length; i++)
                    if (i != last && FacadeW[i] <= left + 0.3f && Has(ch, Facades[i])) fits.Add(i);
                int pick; float w;
                // 고른 뒤 남는 자투리가 애매(0.4~2.6m)하면 너무 늘어나니, 딱 맞거나 넉넉히 남는 건물을 우선
                var good = fits.FindAll(i => { float a = left - FacadeW[i]; return a >= 2.6f || a <= FacadeW[i] * 0.3f; });
                if (good.Count > 0) fits = good;
                if (fits.Count > 0)
                {
                    pick = fits[rnd.Next(fits.Count)];
                    w = FacadeW[pick];
                    float after = left - w;
                    if (after < 2.6f) w = left;                 // 남는 자투리는 이 건물을 늘여 메움
                }
                else
                {
                    // 3m보다 짧게 남음: 가장 좁은 건물을 줄여 넣거나, 아주 짧으면 울타리
                    if (left < 2.2f) { Alley(ch, holder, axis * (x + left / 2), inward, yaw, left, face, rnd); break; }
                    pick = last == 2 ? 4 : 2; w = left;
                }
                var prefab = Prefab(ch, Facades[pick]);
                var go = Object.Instantiate(prefab, holder, false);
                go.name = Facades[pick];
                float back = R(0f, 0.7f);
                go.transform.localPosition = axis * (x + w / 2) + inward * (face - FacadeFront - back);
                go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
                var s0 = go.transform.localScale;
                go.transform.localScale = new Vector3(s0.x * w / FacadeW[pick], s0.y * R(0.88f, 1.15f) * hMul, s0.z);
                foreach (var c in go.GetComponentsInChildren<Collider>()) Object.Destroy(c);
                x += w; last = pick;
            }
            var r = wallCube.GetComponent<Renderer>();
            if (r && hideCube) r.enabled = false;
            return true;
        }

        /// <summary>건물 사이 골목: 안쪽으로 물러난 울타리 + 가끔 상자 · 술통 (충돌 없음, 벽 상자가 막음)</summary>
        static void Alley(ChapterDef ch, Transform holder, Vector3 along, Vector3 inward, float yaw, float gw, float face, System.Random rnd)
        {
            var f = Place(ch, "AlleyFence", holder, along - inward * 1.2f, yaw, 1f, false);
            if (f) f.transform.localScale = new Vector3(f.transform.localScale.x * gw / 2f, f.transform.localScale.y, f.transform.localScale.z);
            if (gw > 1.2f && rnd.NextDouble() < 0.6)
            {
                var name = rnd.NextDouble() < 0.5 ? "Crates" : "BarrelStack";
                var p = Place(ch, name, holder, along - inward * 0.3f, yaw + (float)rnd.NextDouble() * 40f - 20f, 0.75f, false);
                if (p) foreach (var c in p.GetComponentsInChildren<Collider>()) Object.Destroy(c);
            }
        }

        /// <summary>바닥 타일을 격자로 깔기 (그림만, 충돌은 원래 바닥 상자)</summary>
        /// <summary>바닥 (2026-11-17 사용자: 반복 이음매 없애기): 이어지는 흙 텍스처(Resources/Kit/Ch{장}/Ground.mat, Art/tools/ground_texture.py)를
        /// 방 크기의 판 하나에 8m마다 반복해 깔고, 블렌더 바닥 조각(판자 · 자갈 · 마른 풀 · 모래 둔덕 · 해골 · 바위)을 무작위로 흩뿌려 반복을 깸.
        /// 재질이 없으면 예전처럼 4m 타일을 격자로</summary>
        public static bool TileFloor(ChapterDef ch, Transform root, Vector2 size, GameObject floorCube, int seed, string material = "Ground", bool scatter = true)
        {
            var mat = ch != null ? Resources.Load<Material>($"Kit/Ch{ch.Id}/{material}") : null;
            if (mat)
            {
                var go = new GameObject("Ground");
                go.transform.SetParent(root, false);
                go.transform.localPosition = new Vector3(0, 0.01f, 0);
                go.AddComponent<MeshFilter>().sharedMesh = GroundMesh(size, 8f, seed);
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = mat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                if (scatter) ScatterGround(ch, root, size, seed);
                var fr = floorCube ? floorCube.GetComponent<Renderer>() : null;
                if (fr) fr.enabled = false;
                return true;
            }
            var prefab = Prefab(ch, "Floor");
            if (!prefab) return false;
            var b = LocalBounds(prefab);
            float tw = Mathf.Max(1f, b.size.x), td = Mathf.Max(1f, b.size.z);
            int nx = Mathf.CeilToInt(size.x / tw), nz = Mathf.CeilToInt(size.y / td);
            var rnd = new System.Random(seed);
            var holder = new GameObject("FloorTiles").transform;
            holder.SetParent(root, false);
            for (int x = 0; x < nx; x++)
                for (int z = 0; z < nz; z++)
                {
                    var go = Object.Instantiate(prefab, holder, false);
                    go.transform.localPosition = new Vector3(-size.x / 2 + tw * (x + 0.5f), 0, -size.y / 2 + td * (z + 0.5f));
                    go.transform.localRotation = Quaternion.Euler(0, rnd.Next(4) * 90, 0);   // 반복 무늬가 덜 보이게
                    go.transform.localScale = new Vector3(size.x / (nx * tw), 1, size.y / (nz * td));
                    foreach (var c in go.GetComponentsInChildren<Collider>()) Object.Destroy(c);
                }
            var r = floorCube ? floorCube.GetComponent<Renderer>() : null;
            if (r) r.enabled = false;
            return true;
        }
        static Mesh GroundMesh(Vector2 size, float tile, int seed)
        {
            var rnd = new System.Random(seed);
            var off = new Vector2((float)rnd.NextDouble(), (float)rnd.NextDouble());
            float hx = size.x / 2 + 1f, hz = size.y / 2 + 1f;            // 벽 밑까지 살짝 더 깔기
            var m = new Mesh { name = "Ground" };
            m.vertices = new[] { new Vector3(-hx, 0, -hz), new Vector3(hx, 0, -hz), new Vector3(-hx, 0, hz), new Vector3(hx, 0, hz) };
            m.uv = new[] { new Vector2(-hx, -hz) / tile + off, new Vector2(hx, -hz) / tile + off, new Vector2(-hx, hz) / tile + off, new Vector2(hx, hz) / tile + off };
            m.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            m.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            m.RecalculateBounds();
            return m;
        }

        static readonly string[] GroundBits = { "GroundPlanks", "Pebbles", "DryGrass", "SandDrift", "Skull", "Rock_1" };
        static readonly float[] GroundWeight = { 1.2f, 2f, 2f, 1.5f, 0.35f, 0.8f };

        /// <summary>바닥 조각 흩뿌리기: 넓이 약 14㎡당 하나, 무게대로 고르고 방향 · 크기 제각각 (충돌 없음)</summary>
        static void ScatterGround(ChapterDef ch, Transform root, Vector2 size, int seed)
        {
            var rnd = new System.Random(seed * 7 + 3);
            int n = Mathf.RoundToInt(size.x * size.y / 14f);
            float total = 0; foreach (var w in GroundWeight) total += w;
            var holder = new GameObject("GroundBits").transform;
            holder.SetParent(root, false);
            for (int i = 0; i < n; i++)
            {
                float r = (float)rnd.NextDouble() * total; int k = 0;
                while (k < GroundWeight.Length - 1 && r > GroundWeight[k]) { r -= GroundWeight[k]; k++; }
                if (!Has(ch, GroundBits[k])) continue;
                var p = new Vector3(((float)rnd.NextDouble() - 0.5f) * (size.x - 1.5f), 0.02f, ((float)rnd.NextDouble() - 0.5f) * (size.y - 1.5f));
                Place(ch, GroundBits[k], holder, p, rnd.Next(360), 0.75f + (float)rnd.NextDouble() * 0.5f, false);
            }
        }
    }

    /// <summary>부서지는 엄폐물: 적 탄을 막고, 맞으면 깎이다 부서짐</summary>
    public class Breakable : MonoBehaviour
    {
        public static Health Make(GameObject go, float hp)
        {
            var h = go.AddComponent<Health>();
            h.maxHp = hp;
            h.ResetHp(hp);
            go.AddComponent<Breakable>();
            h.RefreshRenderers();
            return h;
        }

        void Awake() => GetComponent<Health>().OnDeath += Break;

        void Break(Health h)
        {
            FxBurst.Spawn(transform.position + Vector3.up * 0.6f, 1.6f, FxBurst.Kind.Spark, new Color(0.75f, 0.55f, 0.35f));
            Destroy(gameObject);
        }
    }

    /// <summary>화약통 (1장 기획서): 쏘면 반경 3m 폭발 · 적 40 · 고양이 15</summary>
    public class PowderKeg : MonoBehaviour
    {
        public const float Radius = 3f, EnemyDmg = 40f, PlayerDmg = 15f;
        bool done;

        public static void Make(GameObject go)
        {
            var h = go.AddComponent<Health>();
            h.maxHp = 1;
            h.ResetHp(1);
            go.AddComponent<PowderKeg>();
        }

        void Awake() => GetComponent<Health>().OnDeath += Boom;

        void Boom(Health h)
        {
            if (done) return;
            done = true;
            var at = transform.position;
            Effects.Explosion(at, Radius, EnemyDmg, new Color(1f, 0.55f, 0.2f));
            var p = PlayerController.Instance;
            if (p && p.health.Alive && Effects.Flat(p.transform.position - at).magnitude <= Radius) p.health.TakeDamage(PlayerDmg);
            CameraFollow.Shake(0.12f);
            // 옆 화약통 연쇄
            foreach (var k in FindObjectsByType<PowderKeg>(FindObjectsSortMode.None))
                if (k != this && !k.done && Effects.Flat(k.transform.position - at).magnitude <= Radius)
                    k.StartCoroutine(k.ChainSoon());
            Destroy(gameObject);
        }

        IEnumerator ChainSoon()
        {
            yield return new WaitForSeconds(0.15f);
            var h = GetComponent<Health>();
            if (h && h.Alive) h.TakeDamage(999);
        }
    }

    /// <summary>바닥에서 솟아오르기 (술집 카운터 · 상인)</summary>
    public class RiseUp : MonoBehaviour
    {
        public float depth = 2.2f, time = 0.7f;
        Vector3 target;
        float t;

        void Start()
        {
            target = transform.localPosition;
            transform.localPosition = target - Vector3.up * depth;
            FxBurst.Spawn(transform.position + Vector3.up * depth * 0.5f, 2.4f, FxBurst.Kind.Gold, new Color(0.85f, 0.65f, 0.4f));
        }

        void Update()
        {
            t += Time.deltaTime / time;
            float k = 1 - Mathf.Pow(1 - Mathf.Clamp01(t), 3);
            transform.localPosition = target - Vector3.up * depth * (1 - k);
            if (t >= 1) Destroy(this);
        }
    }
}
