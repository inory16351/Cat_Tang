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
        public static bool TileWall(ChapterDef ch, GameObject wallCube, Vector3 inward)
        {
            // 스테이지 1(황무지 절벽길)은 절벽 바위 모듈, 그 뒤(마을)는 건물 정면 모듈
            var rm = RunManager.Instance;
            bool cliff = rm && rm.stageIndex == 0 && !rm.IsMidBossRoom && Has(ch, "Cliff");
            var prefab = Prefab(ch, cliff ? "Cliff" : "Wall");
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
            }
            var r = wallCube.GetComponent<Renderer>();
            if (r) r.enabled = false;
            return true;
        }

        /// <summary>바닥 타일을 격자로 깔기 (그림만, 충돌은 원래 바닥 상자)</summary>
        public static bool TileFloor(ChapterDef ch, Transform root, Vector2 size, GameObject floorCube, int seed)
        {
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
