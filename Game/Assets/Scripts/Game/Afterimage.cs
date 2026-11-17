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
        // 움직이는 분신: from → to 로 moveTime 동안 미끄러지며 들어옴 (끝에서 딱 멈춤), 지나간 자리에 옅은 꼬리 복제
        Vector3 from, to;
        float moveTime, trailT, popIn;
        bool moving, ownsMeshes = true;
        float baseScale = 1f;                            // 실루엣 크기 배율 (회상 실루엣은 크게)
        Color trailColor;

        /// <param name="vivid">고양이 쪽 잔상(Vivid 레이어: 회색 화면에서도 제 색). 몬스터 대시 잔상은 false</param>
        public static void Spawn(Transform model, Color c, float life = 0.35f, bool vivid = true)
        {
            if (!model) return;
            nextVivid = vivid;
            SpawnAt(model, model.position, c, life);
            nextVivid = true;
        }
        static bool nextVivid = true;

        public static void SpawnAt(Transform model, Vector3 pos, Color c, float life)
        {
            if (model) SpawnAt(model, pos, model.rotation, c, life);
        }

        /// <summary>크기 배율을 준 실루엣 (발밑 기준으로 커짐). 회상 실루엣 등 눈에 띄어야 하는 분신</summary>
        public static Afterimage SpawnAt(Transform model, Vector3 pos, Color c, float life, float scale)
        {
            if (!model) return null;
            SpawnAt(model, pos, model.rotation, c, life);
            var a = last;
            if (a) { a.baseScale = scale; a.transform.localScale = Vector3.one * scale; }
            return a;
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
                // 구운 메시는 스킨 메시 자기 단위(부모 배율 뺀 크기)로 나옴 → 부모 배율(고양이 Body 1.8배)을 다시 곱함.
                // (2026-11-17 전엔 이게 빠져 몸이 0.55배로 발 쪽으로 쪼그라들고, 제 위치에 놓인 총만 팔에서 떨어져 보였음)
                g.transform.localScale = smr.transform.lossyScale;
                g.AddComponent<MeshFilter>().sharedMesh = baked;
                var r = g.AddComponent<MeshRenderer>();
                r.sharedMaterial = baseMat;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                a.rends.Add(r);
                a.baked.Add(baked);
            }
            // 모델이 플레이어 발밑 기준이면 위치 보정
            root.transform.position = pos + (model.position - model.root.position);
            if (nextVivid) VividLayer.SetAll(a.gameObject); a.Apply(1);
            last = a;
        }

        /// <summary>분신 대시: 지금 자세 그대로 from에 나타나 to로 moveTime 동안 미끄러져 들어옴. 이동 중 꼬리 잔상, 도착 후 life 동안 사라짐</summary>
        public static Afterimage DashIn(Transform model, Vector3 from, Vector3 to, Quaternion rot, Color c, float moveTime, float life)
        {
            SpawnAt(model, from, rot, c, life + moveTime);
            var a = last;
            if (!a) return null;
            a.from = a.transform.position; a.to = to + (a.transform.position - from);
            a.moveTime = Mathf.Max(0.01f, moveTime);
            a.moving = true;
            a.popIn = 1;
            a.trailColor = new Color(c.r, c.g, c.b, c.a * 0.45f);
            return a;
        }
        static Afterimage last;

        /// <summary>같은 메시를 공유하는 정지 복제 (꼬리용)</summary>
        void SpawnCopy(Color c, float life)
        {
            var root = new GameObject("AfterimageTrail");
            root.transform.SetPositionAndRotation(transform.position, transform.rotation);
            root.transform.localScale = transform.localScale;
            var a = root.AddComponent<Afterimage>();
            a.mpb = new MaterialPropertyBlock(); a.born = Time.time; a.life = life; a.color = c; a.ownsMeshes = false; a.baseScale = baseScale;
            foreach (var r in rends)
            {
                if (!r) continue;
                var g = new GameObject("Part");
                g.transform.SetParent(root.transform, false);
                g.transform.localPosition = r.transform.localPosition;
                g.transform.localRotation = r.transform.localRotation;
                g.transform.localScale = r.transform.localScale;
                g.AddComponent<MeshFilter>().sharedMesh = r.GetComponent<MeshFilter>().sharedMesh;
                var rr = g.AddComponent<MeshRenderer>();
                rr.sharedMaterial = baseMat;
                rr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                a.rends.Add(rr);
            }
            if (nextVivid) VividLayer.SetAll(a.gameObject); a.Apply(1);
        }

        void Apply(float k)
        {
            mpb.SetColor("_Color", new Color(color.r, color.g, color.b, color.a * k));
            foreach (var r in rends) if (r) r.SetPropertyBlock(mpb);
        }

        void Update()
        {
            float age = Time.time - born;
            float t = age / life;
            if (t >= 1) { Destroy(gameObject); return; }
            if (moving)
            {
                float m = Mathf.Clamp01(age / moveTime);
                float e = 1 - (1 - m) * (1 - m) * (1 - m);
                transform.position = Vector3.LerpUnclamped(from, to, e);
                trailT += Time.deltaTime;
                if (m < 1 && trailT > 0.016f) { trailT = 0; SpawnCopy(trailColor, 0.16f); }
                // 나타날 때 톡: 0.75 → 1.1 → 1
                float pop = Mathf.Clamp01(age / 0.12f);
                float sc = pop < 1 ? Mathf.Lerp(0.95f, 1.06f, Mathf.Sin(pop * Mathf.PI * 0.5f)) : Mathf.Lerp(1.06f, 1f, Mathf.Clamp01((age - 0.12f) / 0.1f));
                transform.localScale = Vector3.one * sc * baseScale;
                // 도착 전엔 진하게, 도착 뒤 사라짐
                float fade = age < moveTime ? 1 : 1 - (age - moveTime) / Mathf.Max(0.01f, life - moveTime);
                Apply(fade);
                return;
            }
            Apply(1 - t);
            transform.localScale = Vector3.one * (1 + 0.08f * t) * baseScale;
        }

        void OnDestroy() { if (ownsMeshes) foreach (var m in baked) if (m) Destroy(m); }
    }
}
