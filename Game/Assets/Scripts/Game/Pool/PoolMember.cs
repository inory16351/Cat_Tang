using UnityEngine;

namespace CatTang
{
    // 풀링 (2026-11-20): 계층과 컴포넌트는 처음 한 번만 찾아 반환과 복원을 처리한다.
    public sealed class PoolMember : MonoBehaviour
    {
        internal GameObjectPool.Bucket Owner;
        internal bool Idle;
        internal int Version;
        internal float ReturnAt = -1f;
        Transform[] nodes, parents;
        Vector3[] positions, scales;
        Quaternion[] rotations;
        bool[] active;
        int[] layers;
        MonoBehaviour[] scripts;
        Behaviour[] behaviours;
        Renderer[] renderers;
        Collider[] colliders;
        bool[] behaviourEnabled, rendererEnabled, colliderEnabled;
        IPooled[] callbacks;

        internal void Init(GameObjectPool.Bucket owner)
        {
            Owner = owner;
            enabled = false;
            nodes = GetComponentsInChildren<Transform>(true);
            parents = new Transform[nodes.Length];
            positions = new Vector3[nodes.Length]; scales = new Vector3[nodes.Length];
            rotations = new Quaternion[nodes.Length]; active = new bool[nodes.Length]; layers = new int[nodes.Length];
            for (int i = 0; i < nodes.Length; i++)
            {
                var t = nodes[i];
                parents[i] = t.parent;
                positions[i] = t.localPosition; scales[i] = t.localScale; rotations[i] = t.localRotation;
                active[i] = t.gameObject.activeSelf; layers[i] = t.gameObject.layer;
            }
            scripts = GetComponentsInChildren<MonoBehaviour>(true);
            behaviours = GetComponentsInChildren<Behaviour>(true);
            renderers = GetComponentsInChildren<Renderer>(true);
            colliders = GetComponentsInChildren<Collider>(true);
            behaviourEnabled = new bool[behaviours.Length]; rendererEnabled = new bool[renderers.Length]; colliderEnabled = new bool[colliders.Length];
            int count = 0;
            for (int i = 0; i < scripts.Length; i++) if (scripts[i] is IPooled) count++;
            callbacks = new IPooled[count];
            for (int i = 0, j = 0; i < scripts.Length; i++) if (scripts[i] is IPooled pooled) callbacks[j++] = pooled;
            for (int i = 0; i < behaviours.Length; i++) behaviourEnabled[i] = behaviours[i].enabled;
            for (int i = 0; i < renderers.Length; i++) rendererEnabled[i] = renderers[i].enabled;
            for (int i = 0; i < colliders.Length; i++) colliderEnabled[i] = colliders[i].enabled;
        }

        internal void Spawn()
        {
            Idle = false; ReturnAt = -1f;
            unchecked { Version++; }
            transform.SetParent(null, false);
            transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            transform.localScale = Vector3.one;
            for (int i = 0; i < nodes.Length; i++)
            {
                var t = nodes[i];
                if (!t) continue;
                if (i > 0)
                {
                    t.SetParent(parents[i] ? parents[i] : transform, false);
                    t.localPosition = positions[i]; t.localRotation = rotations[i]; t.localScale = scales[i];
                    t.gameObject.SetActive(active[i]);
                }
                t.gameObject.layer = layers[i];
            }
            for (int i = 0; i < behaviours.Length; i++) if (behaviours[i]) behaviours[i].enabled = behaviourEnabled[i];
            for (int i = 0; i < renderers.Length; i++) if (renderers[i]) { renderers[i].enabled = rendererEnabled[i]; renderers[i].SetPropertyBlock(null); }
            for (int i = 0; i < colliders.Length; i++) if (colliders[i]) colliders[i].enabled = colliderEnabled[i];
            for (int i = 0; i < callbacks.Length; i++) callbacks[i].OnSpawned();
            gameObject.SetActive(true);
        }

        internal void Despawn()
        {
            Idle = true; ReturnAt = -1f; enabled = false;
            for (int i = 0; i < scripts.Length; i++)
                if (scripts[i]) { scripts[i].StopAllCoroutines(); scripts[i].CancelInvoke(); }
            for (int i = 0; i < callbacks.Length; i++) callbacks[i].OnDespawned();
        }

        void Update()
        {
            if (ReturnAt >= 0f && Time.time >= ReturnAt) GameObjectPool.Release(gameObject);
        }
    }
}
