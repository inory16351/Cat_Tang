// 적 머리 위 작은 체력 바 (월드 쿼드 + 빌보드). 맞으면 깎인 부분이 잠깐 하얗게 남았다가 줄어듦.
// 보스는 머리 위 대신 화면 위쪽 큰 바 (GameUI)
using UnityEngine;

namespace CatTang
{
    public class EnemyHpBar : MonoBehaviour
    {
        static Material mat;
        Health h;
        Transform root, fill, trail;
        MaterialPropertyBlock mpb;
        float shown = 1, trailV = 1, lastHp;
        const float W = 1.0f, H = 0.11f;

        public static void Attach(Health h, float height)
        {
            if (!h || h.IsBoss) return;
            var b = h.gameObject.AddComponent<EnemyHpBar>();
            b.h = h;
            b.Build(height);
        }

        Transform Quad(string name, Color c, float w, float hgt, float z, int order)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(q.GetComponent<Collider>());
            q.name = name;
            q.transform.SetParent(root, false);
            q.transform.localScale = new Vector3(w, hgt, 1);
            q.transform.localPosition = new Vector3(0, 0, z);
            var r = q.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.sortingOrder = 100 + order;          // 반투명 정렬이 뒤집히지 않게 순서 고정
            mpb.SetColor("_Color", c);
            r.SetPropertyBlock(mpb);
            return q.transform;
        }

        void Build(float height)
        {
            if (!mat) mat = new Material(Shader.Find("Sprites/Default")) { renderQueue = 3100 };
            mpb = new MaterialPropertyBlock();
            root = new GameObject("HpBar").transform;
            root.SetParent(transform, false);
            root.localPosition = new Vector3(0, height, 0);
            Quad("Frame", new Color(0.08f, 0.05f, 0.07f, 0.9f), W + 0.06f, H + 0.06f, 0.002f, 0);
            Quad("Back", new Color(0.25f, 0.16f, 0.18f, 0.95f), W, H, 0.001f, 1);
            trail = Quad("Trail", new Color(1f, 0.95f, 0.85f, 1f), W, H, 0.0005f, 2);
            fill = Quad("Fill", new Color(0.86f, 0.24f, 0.22f, 1f), W, H, 0f, 3);
            lastHp = h.hp;
            MemoryBadge.Attach(h, root, W);   // 추억 표식: 시계 + 개수 (2026-11-18)
        }

        static void SetBar(Transform t, float v)
        {
            v = Mathf.Clamp01(v);
            t.localScale = new Vector3(W * v, t.localScale.y, 1);
            t.localPosition = new Vector3(-W * (1 - v) / 2, 0, t.localPosition.z);
        }

        void LateUpdate()
        {
            if (!h || !root) return;
            var cam = Camera.main;
            if (cam) root.rotation = Quaternion.LookRotation(root.position - cam.transform.position, cam.transform.up);
            float v = h.maxHp > 0 ? h.hp / h.maxHp : 0;
            shown = Mathf.MoveTowards(shown, v, Time.deltaTime * 6f);
            if (h.hp < lastHp - 0.01f) trailHold = 0.35f;
            lastHp = h.hp;
            if (trailHold > 0) trailHold -= Time.deltaTime;
            else trailV = Mathf.MoveTowards(trailV, v, Time.deltaTime * 1.2f);
            if (trailV < v) trailV = v;
            SetBar(fill, shown);
            SetBar(trail, trailV);
            root.gameObject.SetActive(h.Alive);
        }
        float trailHold;
    }
}
