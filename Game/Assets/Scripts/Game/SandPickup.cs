// 2장 퀘스트 (Ch2Quest) — 흐르는 모래: 2장 클리어 뒤 판의 첫 중간 보스가 확정으로 떨어뜨림
// 튀어나와 떨어진 뒤 떠서 돌고, 가까이 오면(4m) 빨려 와 주워짐 → 바로 기록(Meta.MarkStory) = 판에서 죽어도 유지
// 모델 = Resources/Kit/Story/FlowingSand.prefab (작은 모래시계 + 호박색 빛, MCP로 만든 프리팹). 없으면 5장 공전 모래시계
using UnityEngine;

namespace CatTang
{
    public class SandPickup : MonoBehaviour
    {
        const float Magnet = 4f, Grab = 0.6f, Height = 0.9f;
        Transform model;
        Vector3 vel;
        float t, groundY;
        bool landed, pulling, taken;

        public static SandPickup Drop(Vector3 at)
        {
            var go = new GameObject("FlowingSand");
            var root = RunManager.Instance ? RunManager.Instance.StageRoot : null;
            if (root) go.transform.SetParent(root, true);
            go.transform.position = new Vector3(at.x, at.y + 1.4f, at.z);
            var s = go.AddComponent<SandPickup>();
            s.groundY = at.y;
            var dir = Random.insideUnitCircle.normalized;
            s.vel = new Vector3(dir.x * 1.6f, 7f, dir.y * 1.6f);
            var prefab = Resources.Load<GameObject>("Kit/Story/FlowingSand") ?? Resources.Load<GameObject>("Kit/Ch5/BP_Orbit_Hourglass");
            GameObject m = prefab ? Instantiate(prefab, go.transform, false) : GameObject.CreatePrimitive(PrimitiveType.Sphere);
            if (!prefab) { m.transform.SetParent(go.transform, false); m.transform.localScale = Vector3.one * 0.5f; }
            foreach (var c in m.GetComponentsInChildren<Collider>()) Destroy(c);
            m.name = "Model";
            s.model = m.transform;
            if (Sfx.Has("ui_buy")) Sfx.Play("ui_buy", 0.6f, 0f);
            FxBurst.Spawn(go.transform.position, 1.6f, FxBurst.Kind.Gold, new Color(1f, 0.8f, 0.4f));
            Object.FindFirstObjectByType<GameUI>()?.Toast($"<color=#FFD54A>{Ch2Quest.SandName}</color>가 떨어졌다");
            return s;
        }

        void Update()
        {
            t += Time.deltaTime;
            var p = PlayerController.Instance;
            if (!landed)
            {
                vel.y -= 22f * Time.deltaTime;
                transform.position += vel * Time.deltaTime;
                if (transform.position.y <= groundY + Height && vel.y < 0) { landed = true; transform.position = new Vector3(transform.position.x, groundY + Height, transform.position.z); }
            }
            else if (!pulling) transform.position = new Vector3(transform.position.x, groundY + Height + Mathf.Sin(t * 2f) * 0.15f, transform.position.z);
            if (model) model.localRotation = Quaternion.Euler(0, t * 90f, Mathf.Sin(t * 1.3f) * 8f);
            if (!p || taken) return;
            var to = p.transform.position + Vector3.up * 1f - transform.position;
            if (landed && to.magnitude < Magnet) pulling = true;
            if (pulling) transform.position += to.normalized * Mathf.Min(to.magnitude, (6f + 14f * t) * Time.deltaTime);
            if (pulling && to.magnitude < Grab) Take();
        }

        void Take()
        {
            taken = true;
            Meta.MarkStory(Ch2Quest.KeySand);
            if (Sfx.Has("ui_buy")) Sfx.Play("ui_buy", 0.9f, 0f);
            FxBurst.Spawn(transform.position, 2f, FxBurst.Kind.Gold, new Color(1f, 0.85f, 0.45f));
            Object.FindFirstObjectByType<GameUI>()?.Toast($"<color=#FFD54A>{Ch2Quest.SandName}</color>를 얻었다 — 마을의 대장장이에게 가져가자");
            Destroy(gameObject);
        }
    }
}
