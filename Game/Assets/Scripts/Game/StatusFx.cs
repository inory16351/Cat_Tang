// 적에게 걸린 상태 이상을 계속 보여 주는 벡터 셰이더 이펙트 (CatTang/FX 모드 3~8)
// 시간에 가둠 = 푸른 시계 고리 · 정지 = 머리 위 별 · 둔화 = 발밑 모래 소용돌이 · 화상 = 불꽃
// 추억 표식 = 분홍 마름모(겹 수만큼 커짐) · 방어력 감소 = 금 간 방패 · 공격 속도 감소 = 회색 시계
using UnityEngine;

namespace CatTang
{
    public class StatusFx : MonoBehaviour
    {
        static Material mat;
        static readonly int ProgressId = Shader.PropertyToID("_Progress");
        static readonly int ModeId = Shader.PropertyToID("_Mode");
        static readonly int TintId = Shader.PropertyToID("_Tint");
        static readonly int SeedId = Shader.PropertyToID("_Seed");

        Health h;
        float height;
        Layer lockFx, stunFx, slowFx, burnFx, markFx, defFx, atkFx;

        class Layer
        {
            public Transform t; public MeshRenderer r; public MaterialPropertyBlock mpb; public float speed; public bool flat; public float phase;
        }

        public static void Attach(Health h, float height)
        {
            if (!h) return;
            var s = h.gameObject.AddComponent<StatusFx>();
            s.h = h;
            s.height = height;
        }

        Layer Make(string name, float mode, float size, Vector3 local, float speed, Color tint, bool flat = false)
        {
            if (!mat) { var sh = Shader.Find("CatTang/FX"); if (!sh) return null; mat = new Material(sh) { renderQueue = 3050 }; }
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(q.GetComponent<Collider>());
            q.name = "Status_" + name;
            q.transform.SetParent(transform, false);
            q.transform.localPosition = local;
            q.transform.localScale = Vector3.one * size;
            var l = new Layer { t = q.transform, r = q.GetComponent<MeshRenderer>(), mpb = new MaterialPropertyBlock(), speed = speed, flat = flat, phase = Random.value };
            l.r.sharedMaterial = mat;
            l.r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            l.mpb.SetFloat(ModeId, mode);
            l.mpb.SetColor(TintId, tint);
            l.mpb.SetFloat(SeedId, Random.Range(0f, 50f));
            q.SetActive(false);
            return l;
        }

        void Start()
        {
            float hh = height;
            lockFx = Make("TimeLock", 3, hh * 1.25f, new Vector3(0, hh * 0.5f, 0), 0.6f, Color.white);
            stunFx = Make("Stun", 4, hh * 1.35f, new Vector3(0, hh + 0.35f, 0), 1.4f, Color.white);
            slowFx = Make("Slow", 5, hh * 2.1f, new Vector3(0, 0.12f, 0), 0.7f, Color.white, true);
            burnFx = Make("Burn", 6, hh * 1.0f, new Vector3(0, hh * 0.95f, 0), 1.5f, Color.white);   // 몸 위로 솟는 불꽃 (몸 안에 묻히지 않게)
            markFx = Make("Mark", 7, 0.45f, new Vector3(0, hh + 0.95f, 0), 0.8f, Color.white);
            defFx = Make("DefDown", 8, 0.7f, new Vector3(0.7f, hh + 0.35f, 0), 0.5f, Color.white);
            atkFx = Make("AtkDown", 3, 0.6f, new Vector3(-0.7f, hh + 0.35f, 0), 1.2f, new Color(0.7f, 0.7f, 0.72f));
        }

        void Run(Layer l, bool on, float scaleMul = 1f)
        {
            if (l == null) return;
            if (l.t.gameObject.activeSelf != on) l.t.gameObject.SetActive(on);
            if (!on) return;
            l.phase = (l.phase + Time.deltaTime * l.speed) % 1f;
            l.mpb.SetFloat(ProgressId, l.phase);
            l.r.SetPropertyBlock(l.mpb);
            var cam = Camera.main;
            if (l.flat) l.t.rotation = Quaternion.Euler(90, 0, 0);
            else if (cam) l.t.rotation = cam.transform.rotation;
            if (scaleMul != 1f) l.t.localScale = Vector3.one * 0.45f * scaleMul;
        }

        void LateUpdate()
        {
            if (!h) return;
            bool alive = h.Alive;
            Run(lockFx, alive && h.TimeLocked);
            Run(stunFx, alive && h.Stunned && !h.TimeLocked);
            Run(slowFx, alive && h.Slowed);
            Run(burnFx, alive && h.Burning);
            int m = h.MarkStacks;
            Run(markFx, alive && m > 0, 1f + Mathf.Min(m, 10) * 0.09f);
            Run(defFx, alive && h.DefDowned);
            Run(atkFx, alive && h.AtkDowned);
        }
    }
}
