// 이펙트 프리팹 (Resources/FX/Prefabs/*.prefab, 에디터에서 MCP로 만든 오브젝트)
// - 루트 FxEffect: 꺼내서 자리 · 방향 · 크기를 잡고, 탄 색을 입히고, 가장 늦게 끝나는 조각이 끝나면 지움
// - 조각 FxPart: 블렌더 메시(Resources/FX/Meshes, Art/tools/blender_fx.py) + 툰 불꽃 재질. 조각마다 수명 · 지연 · 날아가는 방향 · 회전 · 카메라 보기
//   크기 · 열 · 녹음 곡선은 FxMesh.Evaluate와 같음 (Kind)
// 런타임은 프리팹을 꺼내 놓기만 함 (도형을 코드로 조립하지 않음)
using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    public class FxEffect : MonoBehaviour, IPooled
    {
        [Tooltip("탄 색을 입힐지 (false면 조각 재질 색 그대로)")] public bool tintable = true;

        static readonly Dictionary<string, GameObject> cache = new();

        /// <summary>Resources/FX/Prefabs/name 을 꺼내 pos에 놓음. rot = 앞(+Z)이 탄도 방향. tint = 탄 색(없으면 재질 색)</summary>
        public static FxEffect Spawn(string name, Vector3 pos, Quaternion rot, float scale = 1f, Color? tint = null)
        {
            if (!cache.TryGetValue(name, out var pf) || !pf)
            {
                pf = Resources.Load<GameObject>("FX/Prefabs/" + name);
                cache[name] = pf;
            }
            if (!pf) return null;
            // 풀링 (2026-11-20): 이름별 완성 프리팹을 재사용하고 기존 재생 길이를 유지한다.
            var go = GameObjectPool.Get(name, factory);
            go.transform.SetPositionAndRotation(pos, rot);
            go.transform.localScale = Vector3.one * scale;
            var fx = go.GetComponent<FxEffect>();
            if (fx) { fx.SetLayer(); fx.Play(tint); }
            return fx;
        }

        static readonly System.Func<string, GameObject> factory = Create;
        static GameObject Create(string name)
        {
            var go = Instantiate(cache[name]);
            VividLayer.SetAll(go);
            return go;
        }

        public static void Prewarm(string name, int count, int maxCreate = int.MaxValue)
        {
            if (Has(name)) GameObjectPool.Prewarm(name, factory, count, maxCreate);
        }

        FxPart[] parts;
        Transform[] nodes;
        float releaseAt = -1f;
        void Awake() => CacheParts();
        void CacheParts()
        {
            if (parts != null) return;
            parts = GetComponentsInChildren<FxPart>(true);
            nodes = GetComponentsInChildren<Transform>(true);
            foreach (var p in parts) p.CachePose();
        }

        void SetLayer()
        {
            foreach (var t in nodes) if (t) VividLayer.Set(t.gameObject);
        }

        public void OnSpawned() { CacheParts(); follow = null; followOffset = Vector3.zero; releaseAt = -1f; }
        public void OnDespawned() { follow = null; followOffset = Vector3.zero; releaseAt = -1f; }

        void Update()
        {
            if (releaseAt >= 0f && Time.time >= releaseAt) GameObjectPool.Release(gameObject);
        }

        Transform follow;
        Vector3 followOffset;

        /// <summary>대상에 붙어 따라다님 (지연 충전 폭탄 · 미련 불씨). 대상이 사라지면 그 자리에 남음</summary>
        public void Follow(Transform target)
        {
            follow = target;
            if (target) followOffset = transform.position - target.position;
        }

        void LateUpdate()
        {
            if (follow) transform.position = follow.position + followOffset;
        }

        public static bool Has(string name)
        {
            if (!cache.TryGetValue(name, out var pf) || !pf) { pf = Resources.Load<GameObject>("FX/Prefabs/" + name); cache[name] = pf; }
            return pf;
        }

        internal void Play(Color? tint)
        {
            float end = 0;
            foreach (var p in parts)
            {
                p.Begin(tintable ? tint : null);
                end = Mathf.Max(end, p.EndTime);
            }
            releaseAt = Time.time + end + 0.05f;
        }
    }
}
