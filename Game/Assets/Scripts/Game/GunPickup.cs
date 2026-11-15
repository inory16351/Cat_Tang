// 바닥에 떨어진 총. E로 주우면 교체 창(GameUI.ShowGunPick)이 열림.
// 손에서 빠진 총 · 버린 총이 여기로 떨어진다. 스테이지가 바뀌면 스테이지와 함께 사라짐.
using UnityEngine;

namespace CatTang
{
    public class GunPickup : Interactable
    {
        public GunDef gun;

        public static GunPickup Drop(GunDef g, Vector3 at)
        {
            if (g == null) return null;
            var go = new GameObject("GunPickup_" + g.Id);
            var lay = RunManager.Instance ? RunManager.Instance.StageRoot : null;
            if (lay) go.transform.SetParent(lay, true);
            var rnd = Random.insideUnitCircle.normalized * 1.2f;
            go.transform.position = new Vector3(at.x + rnd.x, 0, at.z + rnd.y);
            var gp = go.AddComponent<GunPickup>();
            gp.gun = g;
            gp.prompt = $"{g.Name} 줍기";
            gp.radius = 1.8f;
            WorldIcon(go.transform, "Icons/guns/gun_" + g.Id, new Vector3(0, 0.9f, 0), 0.9f);
            WorldLabel(go.transform, g.Name, new Vector3(0, 1.7f, 0), 0.05f, new Color(1f, 0.85f, 0.4f));
            return gp;
        }

        void Update()
        {
            // 살짝 떠다님
            if (transform.childCount > 0) transform.GetChild(0).localPosition = new Vector3(0, 0.9f + Mathf.Sin(Time.time * 2.5f) * 0.12f, 0);
        }

        public override void Interact(PlayerController p)
        {
            var ui = FindFirstObjectByType<GameUI>();
            if (!ui) return;
            var g = gun;
            Destroy(gameObject);                      // 창에서 '버리기'를 고르면 다시 떨어짐
            ui.ShowGunPick(g, null);
        }
    }
}
