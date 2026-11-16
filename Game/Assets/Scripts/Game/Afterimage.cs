// 실루엣 잔상: 모델의 메시를 그 자세 그대로 반투명 단색으로 복제해 남기고 서서히 사라짐 (대시 · 회상 실루엣 · 불릿 파티 금빛 잔상)
using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    public class Afterimage : MonoBehaviour
    {
        static Material baseMat;
        readonly List<MeshRenderer> rends = new();
        readonly List<Mesh> baked = new();
        MaterialPropertyBlock mpb;
        float born, life;
        Color color;

        public static void Spawn(Transform model, Color c, float life = 0.35f)
        {
            if (model) SpawnAt(model, model.position, c, life);
        }

        public static void SpawnAt(Transform model, Vector3 pos, Color c, float life)
        {
            if (model) SpawnAt(model, pos, model.rotation, c, life);
        }

        /// <summary>지금 자세 그대로, 바라보는 방향만 rot으로 바꾼 실루엣 (불릿 파티 분신: 사방을 향해 동시에 쏨)</summary>
        public static void SpawnAt(Transform model, Vector3 pos, Quaternion rot, Color c, float life)
        {
            if (!model) return;
            if (!baseMat) { var sh = Shader.Find("CatTang/Silhouette") ?? Shader.Find("Sprites/Default"); baseMat = new Material(sh) { renderQueue = 3050 }; }
            var root = new GameObject("Afterimage");
            root.transform.SetPositionAndRotation(pos, rot);
            var a = root.AddComponent<Afterimage>();
            a.mpb = new MaterialPropertyBlock();
            a.born = Time.time;
            a.life = life;
            a.color = c;
            foreach (var mf in model.GetComponentsInChildren<MeshFilter>())
            {
                var mr = mf.GetComponent<MeshRenderer>();
                if (!mr || !mr.enabled) continue;
                var g = new GameObject("Part");
                g.transform.SetParent(root.transform, false);
                var local = model.InverseTransformPoint(mf.transform.position);
                g.transform.localPosition = local;
                g.transform.localRotation = Quaternion.Inverse(model.rotation) * mf.transform.rotation;
                g.transform.localScale = mf.transform.lossyScale;
                g.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
                var r = g.AddComponent<MeshRenderer>();
                r.sharedMaterial = baseMat;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                a.rends.Add(r);
            }
            // 스킨 메시(고양이 모델): 지금 자세를 그대로 구워서 복제
            foreach (var smr in model.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (!smr.enabled || !smr.sharedMesh) continue;
                var baked = new Mesh();
                smr.BakeMesh(baked, true);
                var g = new GameObject("Skin");
                g.transform.SetParent(root.transform, false);
                g.transform.localPosition = model.InverseTransformPoint(smr.transform.position);
                g.transform.localRotation = Quaternion.Inverse(model.rotation) * smr.transform.rotation;
                g.AddComponent<MeshFilter>().sharedMesh = baked;
                var r = g.AddComponent<MeshRenderer>();
                r.sharedMaterial = baseMat;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                a.rends.Add(r);
                a.baked.Add(baked);
            }
            // 모델이 플레이어 발밑 기준이면 위치 보정
            root.transform.position = pos + (model.position - model.root.position);
            a.Apply(1);
        }

        void Apply(float k)
        {
            mpb.SetColor("_Color", new Color(color.r, color.g, color.b, color.a * k));
            foreach (var r in rends) if (r) r.SetPropertyBlock(mpb);
        }

        void Update()
        {
            float t = (Time.time - born) / life;
            if (t >= 1) { Destroy(gameObject); return; }
            Apply(1 - t);
            transform.localScale = Vector3.one * (1 + 0.08f * t);
        }

        void OnDestroy() { foreach (var m in baked) if (m) Destroy(m); }
    }
}
