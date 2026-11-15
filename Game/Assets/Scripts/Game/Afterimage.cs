// 실루엣 잔상: 모델의 메시를 그 자세 그대로 반투명 단색으로 복제해 남기고 서서히 사라짐 (대시 · 회상 실루엣 · 불릿 파티 금빛 잔상)
using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    public class Afterimage : MonoBehaviour
    {
        static Material baseMat;
        readonly List<MeshRenderer> rends = new();
        MaterialPropertyBlock mpb;
        float born, life;
        Color color;

        public static void Spawn(Transform model, Color c, float life = 0.35f)
        {
            if (model) SpawnAt(model, model.position, c, life);
        }

        public static void SpawnAt(Transform model, Vector3 pos, Color c, float life)
        {
            if (!model) return;
            if (!baseMat) baseMat = new Material(Shader.Find("Sprites/Default")) { renderQueue = 3050 };
            var root = new GameObject("Afterimage");
            root.transform.SetPositionAndRotation(pos + (model.position - model.parent.position) * 0, model.rotation);
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
    }
}
