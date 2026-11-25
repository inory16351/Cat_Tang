// 3장 상인 '거울 속 고양이' 겉모습 (2026-11-18 사용자: 가면을 쓰고, 거울에서 반사된 존재라는 걸 알 수 있게 반투명 + 오라)
// - 몸: 모든 렌더러를 CatTang/MirrorGhost(원래 색 텍스처 + 옅은 거울빛 + 금빛 림 + 위로 흐르는 반짝임 띠, 반투명)로 바꿈
// - 가면: Resources/Kit/Ch3/MerchantMask(블렌더 + Codex 베네치아 반가면)를 머리 뼈에 붙임 (가면은 불투명 → 반투명 몸 위에서 또렷)
// - 오라: 발밑 금빛 · 분홍 빛 고리 두 겹(맥동) · 몸 둘레를 도는 반짝임 3개 · 가끔 피어오르는 빛가루(FX_Ch3_Stardust)
using UnityEngine;

namespace CatTang
{
    public class MirrorGhost : MonoBehaviour
    {
        static readonly int BaseMapId = Shader.PropertyToID("_BaseMap"), BaseColorId = Shader.PropertyToID("_BaseColor");
        Transform ringA, ringB;
        readonly Transform[] motes = new Transform[3];
        float nextDust;

        /// <param name="rings">발밑 원형 오라 (상인). 마을의 거울 속 고양이 NPC는 끔 → 대신 둘레를 도는 거울 조각 (2026-11-25 사용자: 바닥 원형 오라 안 어울림)</param>
        public static void Apply(GameObject fig, bool rings = true)
        {
            var sh = Shader.Find("CatTang/MirrorGhost");
            if (sh)
                foreach (var r in fig.GetComponentsInChildren<Renderer>(true))
                {
                    var src = r.sharedMaterials;
                    var mats = new Material[src.Length];
                    for (int i = 0; i < src.Length; i++)
                    {
                        var m = new Material(sh);
                        if (src[i])
                        {
                            if (src[i].HasProperty(BaseMapId)) m.SetTexture(BaseMapId, src[i].GetTexture(BaseMapId));
                            if (src[i].HasProperty(BaseColorId)) m.SetColor(BaseColorId, src[i].GetColor(BaseColorId));
                        }
                        mats[i] = m;
                    }
                    r.sharedMaterials = mats;
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            AttachMask(fig);
            fig.AddComponent<MirrorGhost>().BuildAura(rings);
        }

        static void AttachMask(GameObject fig)
        {
            var prefab = Resources.Load<GameObject>("Kit/Ch3/MerchantMask");
            if (!prefab) return;
            var an = fig.GetComponentInChildren<Animator>();
            var head = an && an.isHuman ? an.GetBoneTransform(HumanBodyBones.Head) : null;
            var mask = Instantiate(prefab);
            mask.name = "MerchantMask";
            foreach (var c in mask.GetComponentsInChildren<Collider>()) Destroy(c);
            var fwd = fig.transform.forward;
            // 얼굴 앞: 머리 뼈(목 위) 기준 앞으로 · 살짝 위. 뼈가 없으면 키 비율로
            var anchor = head ? head.position : fig.transform.position + Vector3.up * 1.75f * fig.transform.lossyScale.y;
            mask.transform.position = anchor + fwd * 0.34f + Vector3.up * 0.12f;
            mask.transform.rotation = Quaternion.LookRotation(fwd, Vector3.up) * Quaternion.Euler(-8f, 0, 0);
            mask.transform.SetParent(head ? head : fig.transform, true);
        }

        bool shardMotes;

        void BuildAura(bool rings)
        {
            shardMotes = !rings;
            if (rings)
            {
                var gold = Glow(new Color(1f, 0.82f, 0.45f, 0.55f), 1.2f);
                var pink = Glow(new Color(1f, 0.72f, 0.88f, 0.35f), 0.8f);
                ringA = Disc("AuraRingA", 1.9f, 0.03f, gold);
                ringB = Disc("AuraRingB", 2.5f, 0.02f, pink);
            }
            for (int i = 0; i < motes.Length; i++)
            {
                // 상인: 빛 구슬 / 거울 고양이 NPC: 얇은 거울 조각 (하늘빛 반짝임, 천천히 돌며 몸을 비춤)
                var m = GameObject.CreatePrimitive(shardMotes ? PrimitiveType.Cube : PrimitiveType.Sphere);
                Destroy(m.GetComponent<Collider>());
                m.name = (shardMotes ? "MirrorShard" : "AuraMote") + i;
                m.transform.SetParent(transform, false);
                float s = Mathf.Max(0.01f, transform.lossyScale.y);
                m.transform.localScale = shardMotes ? new Vector3(0.16f, 0.26f, 0.02f) / s : Vector3.one * 0.16f;
                m.GetComponent<Renderer>().sharedMaterial = shardMotes ? Glow(new Color(0.82f, 0.93f, 1f, 0.9f), 1.4f) : Glow(new Color(1f, 0.95f, 0.75f, 0.95f), 0.6f);
                m.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                motes[i] = m.transform;
            }
        }

        Transform Disc(string name, float diameter, float height, Material m)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(go.GetComponent<Collider>());
            go.name = name;
            go.transform.SetParent(transform, false);
            // 몸 배율과 상관없이 같은 크기
            float s = Mathf.Max(0.01f, transform.lossyScale.y);
            go.transform.localPosition = Vector3.up * 0.04f / s;
            go.transform.localScale = new Vector3(diameter, height, diameter) / s;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go.transform;
        }

        static Material Glow(Color c, float rim)
        {
            var m = new Material(Shader.Find("CatTang/Silhouette"));
            m.SetColor("_Color", c);
            m.SetFloat("_RimBoost", rim);
            return m;
        }

        void Update()
        {
            float t = Time.time;
            float s = Mathf.Max(0.01f, transform.lossyScale.y);
            if (ringA) { float k = 1f + Mathf.Sin(t * 2.4f) * 0.08f; ringA.localScale = new Vector3(1.9f * k, 0.03f, 1.9f * k) / s; ringA.Rotate(0, 40f * Time.deltaTime, 0); }
            if (ringB) { float k = 1f + Mathf.Sin(t * 2.4f + 1.6f) * 0.1f; ringB.localScale = new Vector3(2.5f * k, 0.02f, 2.5f * k) / s; }
            for (int i = 0; i < motes.Length; i++)
            {
                if (!motes[i]) continue;
                float a = t * 1.4f + i * Mathf.PI * 2 / motes.Length;
                if (shardMotes)
                {
                    float sa = t * 0.6f + i * Mathf.PI * 2 / motes.Length;
                    motes[i].position = transform.position + new Vector3(Mathf.Cos(sa) * 0.95f, 1.2f + i * 0.4f + Mathf.Sin(t * 1.3f + i) * 0.12f, Mathf.Sin(sa) * 0.95f);
                    motes[i].rotation = Quaternion.Euler(Mathf.Sin(t + i) * 25f, t * 70f + i * 120f, Mathf.Cos(t * 0.8f + i) * 20f);
                    continue;
                }
                motes[i].position = transform.position + new Vector3(Mathf.Cos(a) * 1.1f, 1.0f + i * 0.55f + Mathf.Sin(t * 2f + i) * 0.15f, Mathf.Sin(a) * 1.1f);
            }
            if (t >= nextDust)
            {
                Enemy.Fx3("Stardust", transform.position + Vector3.up * Random.Range(0.6f, 2.2f) + Random.insideUnitSphere * 0.5f, 0.35f);
                nextDust = t + Random.Range(0.6f, 1.1f);
            }
        }
    }
}
