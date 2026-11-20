using System;
using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    // 풀링 (2026-11-20): 씬 안에서만 보관하며 생성과 파괴가 몰리는 비용을 줄인다.
    public static class GameObjectPool
    {
        internal sealed class Bucket
        {
            internal readonly Stack<GameObject> Idle = new();
            internal readonly Stack<GameObject> Scratch = new();
            internal int MaxKeep = 512, Created, Reused;
        }

        static readonly Dictionary<object, Bucket> pools = new();
        static Transform root;

        static void EnsureRoot()
        {
            if (root) return;
            pools.Clear();
            root = new GameObject("[Pools]").transform;
        }

        static Bucket For(object key)
        {
            EnsureRoot();
            if (!pools.TryGetValue(key, out var bucket)) pools[key] = bucket = new Bucket();
            return bucket;
        }

        static GameObject Take(Bucket bucket)
        {
            while (bucket.Idle.Count > 0)
            {
                var go = bucket.Idle.Pop();
                if (!go) continue;
                bucket.Reused++;
                go.GetComponent<PoolMember>().Spawn();
                return go;
            }
            return null;
        }

        static GameObject Register(GameObject go, Bucket bucket, bool spawn)
        {
            if (!go) return null;
            go.SetActive(false);
            var member = go.AddComponent<PoolMember>();
            member.Init(bucket);
            bucket.Created++;
            if (spawn) member.Spawn();
            else Release(go);
            return go;
        }

        public static GameObject Get(string key, Func<GameObject> factory)
        {
            var bucket = For(key);
            var go = Take(bucket);
            return go ? go : Register(factory(), bucket, true);
        }

        // 키를 받는 정적 팩토리는 호출마다 캡처 람다를 만들지 않는다.
        public static GameObject Get(string key, Func<string, GameObject> factory)
        {
            var bucket = For(key);
            var go = Take(bucket);
            return go ? go : Register(factory(key), bucket, true);
        }

        public static GameObject Get(GameObject prefab)
        {
            if (!prefab) return null;
            var bucket = For(prefab);
            var go = Take(bucket);
            return go ? go : Register(UnityEngine.Object.Instantiate(prefab), bucket, true);
        }

        public static GameObject Get(GameObject prefab, Func<GameObject> factory)
        {
            var bucket = For(prefab);
            var go = Take(bucket);
            return go ? go : Register(factory(), bucket, true);
        }

        public static void Release(GameObject go)
        {
            if (!go) return;
            var member = go.GetComponent<PoolMember>();
            if (!member) { UnityEngine.Object.Destroy(go); return; }
            if (member.Idle) return;
            member.Despawn();
            go.SetActive(false);
            if (!root || member.Owner.Idle.Count >= member.Owner.MaxKeep)
            {
                UnityEngine.Object.Destroy(go);
                return;
            }
            go.transform.SetParent(root, false);
            member.Owner.Idle.Push(go);
        }

        // 탄의 꼬리는 기존 시간만큼 남긴 뒤 통째로 반환한다.
        public static void ReleaseAfter(GameObject go, float delay)
        {
            var member = go ? go.GetComponent<PoolMember>() : null;
            if (member && !member.Idle) { member.ReturnAt = Time.time + delay; member.enabled = true; }
            else if (!member && go) UnityEngine.Object.Destroy(go, delay);
        }

        public static void Prewarm(string key, Func<GameObject> factory, int count, int maxCreate = int.MaxValue)
        {
            var bucket = For(key);
            Prune(bucket);
            int target = Mathf.Min(count, bucket.MaxKeep);
            int remaining = Mathf.Max(0, maxCreate);
            while (bucket.Idle.Count < target && remaining-- > 0)
                if (!Register(factory(), bucket, false)) break;
        }

        public static void Prewarm(string key, Func<string, GameObject> factory, int count, int maxCreate = int.MaxValue)
        {
            var bucket = For(key);
            Prune(bucket);
            int target = Mathf.Min(count, bucket.MaxKeep);
            int remaining = Mathf.Max(0, maxCreate);
            while (bucket.Idle.Count < target && remaining-- > 0)
                if (!Register(factory(key), bucket, false)) break;
        }

        public static void Prewarm(GameObject prefab, int count, int maxCreate = int.MaxValue)
        {
            if (!prefab) return;
            var bucket = For(prefab);
            Prune(bucket);
            int target = Mathf.Min(count, bucket.MaxKeep);
            int remaining = Mathf.Max(0, maxCreate);
            while (bucket.Idle.Count < target && remaining-- > 0)
                Register(UnityEngine.Object.Instantiate(prefab), bucket, false);
        }

        public static void Prewarm(GameObject prefab, Func<GameObject> factory, int count, int maxCreate = int.MaxValue)
        {
            var bucket = For(prefab);
            Prune(bucket);
            int target = Mathf.Min(count, bucket.MaxKeep);
            int remaining = Mathf.Max(0, maxCreate);
            while (bucket.Idle.Count < target && remaining-- > 0)
                if (!Register(factory(), bucket, false)) break;
        }

        static void Prune(Bucket bucket)
        {
            while (bucket.Idle.Count > 0)
            {
                var go = bucket.Idle.Pop();
                if (go) bucket.Scratch.Push(go);
            }
            while (bucket.Scratch.Count > 0) bucket.Idle.Push(bucket.Scratch.Pop());
        }

        public static void SetMaxKeep(object key, int maxKeep)
        {
            var bucket = For(key);
            bucket.MaxKeep = Mathf.Max(0, maxKeep);
            while (bucket.Idle.Count > bucket.MaxKeep)
            {
                var go = bucket.Idle.Pop();
                if (go) UnityEngine.Object.Destroy(go);
            }
        }

        // 장시간 참조를 보관하는 연출은 세대 번호로 다른 사용의 오브젝트인지 확인한다.
        public static int Version(GameObject go)
        {
            var member = go ? go.GetComponent<PoolMember>() : null;
            return member ? member.Version : 0;
        }

        public static bool IsCurrent(GameObject go, int version)
        {
            var member = go ? go.GetComponent<PoolMember>() : null;
            return member && !member.Idle && member.Version == version;
        }

        public static PoolStats Stats(object key)
        {
            if (!root || !pools.TryGetValue(key, out var bucket)) return default;
            int idle = 0;
            foreach (var go in bucket.Idle) if (go) idle++;
            return new PoolStats(bucket.Created, bucket.Reused, idle);
        }
    }
}
