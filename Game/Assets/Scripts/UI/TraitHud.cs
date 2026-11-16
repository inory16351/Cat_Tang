// 활성 특성 HUD (왼쪽, 대시 게이지 아래): 지금 셀 수 있는 특성마다 한 줄 = 아이콘 · 이름 · 개수 · 단계 칸(2 · 4 · 6).
// 단계가 켜진 특성은 밝게, 아직 2개 미만이면 흐리게. 줄에 마우스를 올리면 옆에 툴팁: 특성 설명 + 단계별 효과(켜진 단계는 금색) + 다음 단계까지 남은 개수.
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CatTang
{
    public class TraitHud : MonoBehaviour
    {
        const float RowW = 290, RowH = 46, Gap = 5;
        static readonly int[] Tiers = { 2, 4, 6 };
        RectTransform root, tip;
        Text header, tipText, tipTitle, tipDesc;
        Image tipIcon;
        readonly List<GameObject> rows = new();
        string lastKey = "";
        float nextRefresh;
        int hoverTrait;
        float hoverY;

        public static TraitHud Build(RectTransform parent, Vector2 pos)
        {
            var rt = UIKit.MakeRect("Traits", parent, new Vector2(0, 1), pos, new Vector2(RowW, 400), new Vector2(0, 1));
            var h = rt.gameObject.AddComponent<TraitHud>();
            h.root = rt;
            h.header = UIKit.Label("Header", rt, new Vector2(0, 1), Vector2.zero, new Vector2(RowW, 30), 20, TextAnchor.MiddleLeft, true, UIKit.Gold);
            h.header.rectTransform.pivot = new Vector2(0, 1);
            // 툴팁 (줄 오른쪽)
            var tp = UIKit.Panel("Tip", parent, new Vector2(0, 1), Vector2.zero, new Vector2(420, 200), false, "panel_plain", 1.4f);
            tp.color = new Color(0.12f, 0.08f, 0.15f, 0.96f);
            tp.raycastTarget = false;
            h.tip = tp.rectTransform;
            h.tip.pivot = new Vector2(0, 1);
            // 머리: 아이콘 + 이름 · 개수 + 설명, 아래: 단계별 효과
            h.tipIcon = UIKit.MakeRect("Icon", h.tip, new Vector2(0, 1), new Vector2(18, -16), new Vector2(64, 64), new Vector2(0, 1)).gameObject.AddComponent<Image>();
            h.tipIcon.preserveAspect = true; h.tipIcon.raycastTarget = false;
            h.tipTitle = UIKit.Label("Title", h.tip, new Vector2(0, 1), new Vector2(92, -18), new Vector2(310, 32), 24, TextAnchor.MiddleLeft, true, UIKit.Gold);
            h.tipTitle.rectTransform.pivot = new Vector2(0, 1);
            h.tipDesc = UIKit.Label("Desc", h.tip, new Vector2(0, 1), new Vector2(92, -52), new Vector2(310, 30), 17, TextAnchor.UpperLeft, false, new Color(0.79f, 0.76f, 0.83f));
            h.tipDesc.rectTransform.pivot = new Vector2(0, 1);
            h.tipText = UIKit.Label("Body", h.tip, new Vector2(0, 1), new Vector2(24, -96), new Vector2(372, 160), 19, TextAnchor.UpperLeft);
            h.tipText.rectTransform.pivot = new Vector2(0, 1);
            h.tipText.lineSpacing = 1.1f;
            tp.gameObject.SetActive(false);
            return h;
        }

        void Update()
        {
            var p = PlayerController.Instance;
            if (!p) return;
            if (Time.unscaledTime >= nextRefresh)
            {
                nextRefresh = Time.unscaledTime + 0.25f;
                Refresh(p.rev);
            }
            if (hoverTrait != 0) ShowTip(p.rev);
        }

        // ---------- 줄 ----------
        void Refresh(Revolver rev)
        {
            var counts = rev.TraitCounts();
            var list = counts.Where(kv => kv.Value > 0)
                             .Select(kv => (id: kv.Key, n: kv.Value, tier: rev.TierOf(kv.Value)))
                             .OrderByDescending(x => x.tier).ThenByDescending(x => x.n).ThenBy(x => x.id).ToList();
            string key = string.Join(",", list.Select(x => $"{x.id}:{x.n}:{x.tier}"));
            if (key == lastKey) return;
            lastKey = key;
            foreach (var r in rows) Destroy(r);
            rows.Clear();
            int active = list.Count(x => x.tier > 0);
            header.text = list.Count == 0 ? "" : $"특성  <size=16><color=#C9C2D4>활성 {active} / {list.Count}</color></size>";
            for (int i = 0; i < list.Count; i++) rows.Add(MakeRow(list[i].id, list[i].n, list[i].tier, -34 - i * (RowH + Gap), rev));
            if (hoverTrait != 0 && !counts.ContainsKey(hoverTrait)) Hover(0, 0);
        }

        GameObject MakeRow(int id, int n, int tier, float y, Revolver rev)
        {
            var rt = UIKit.MakeRect("Trait" + id, root, new Vector2(0, 1), new Vector2(0, y), new Vector2(RowW, RowH), new Vector2(0, 1));
            var bg = rt.gameObject.AddComponent<Image>();
            bg.color = tier > 0 ? new Color(0.15f, 0.1f, 0.2f, 0.95f) : new Color(0.1f, 0.08f, 0.12f, 0.78f);
            bg.raycastTarget = true;
            var chip = rt.gameObject.AddComponent<TraitChip>();
            chip.hud = this; chip.trait = id; chip.y = y;
            // 테두리: 켜진 특성만 금빛
            if (tier > 0)
            {
                var ol = rt.gameObject.AddComponent<Outline>();
                ol.effectColor = new Color(0.98f, 0.78f, 0.32f, 0.9f);
                ol.effectDistance = new Vector2(1.5f, -1.5f);
            }
            var c = TraitColor(id);
            // 아이콘 (Codex: Icons/traits/trait_<id>), 없으면 색 마름모
            var icon = Icon(id);
            var ic = UIKit.MakeRect("Icon", rt, new Vector2(0, 0.5f), new Vector2(3, 0), new Vector2(RowH - 4, RowH - 4), new Vector2(0, 0.5f)).gameObject.AddComponent<Image>();
            ic.raycastTarget = false;
            if (icon) { ic.sprite = icon; ic.preserveAspect = true; ic.color = tier > 0 ? Color.white : new Color(0.55f, 0.55f, 0.6f, 0.85f); }
            else { ic.rectTransform.sizeDelta = new Vector2(14, 14); ic.rectTransform.anchoredPosition = new Vector2(14, 0); ic.rectTransform.localRotation = Quaternion.Euler(0, 0, 45); ic.color = tier > 0 ? c : Color.Lerp(c, Color.gray, 0.6f); }
            // 이름 · 개수
            string name = Tables.Traits.TryGetValue(id, out var t) ? t.Name : id.ToString();
            var lbl = UIKit.Label("Name", rt, new Vector2(0, 0.5f), new Vector2(RowH + 4, 0), new Vector2(110, RowH), 20, TextAnchor.MiddleLeft, true, tier > 0 ? UIKit.Cream : UIKit.Muted);
            lbl.rectTransform.pivot = new Vector2(0, 0.5f);
            lbl.text = name;
            var cnt = UIKit.Label("Count", rt, new Vector2(0, 0.5f), new Vector2(RowH + 112, 0), new Vector2(36, RowH), 22, TextAnchor.MiddleCenter, true, tier > 0 ? c : UIKit.Muted);
            cnt.rectTransform.pivot = new Vector2(0, 0.5f);
            cnt.text = n.ToString();
            // 단계 칸 2 · 4 · 6 (찬 단계 = 금색 칸)
            int red = rev.TierThresholdReduce;
            for (int k = 0; k < Tiers.Length; k++)
            {
                bool on = tier >= Tiers[k];
                var box = UIKit.MakeRect("Tier" + k, rt, new Vector2(1, 0.5f), new Vector2(-8 - (2 - k) * 27, 0), new Vector2(24, 22), new Vector2(1, 0.5f)).gameObject.AddComponent<Image>();
                box.raycastTarget = false;
                box.color = on ? new Color(0.98f, 0.78f, 0.32f) : new Color(0.22f, 0.19f, 0.27f, 0.9f);
                var bt = UIKit.Label("N", box.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(24, 22), 15, TextAnchor.MiddleCenter, true, on ? new Color(0.15f, 0.08f, 0.05f) : new Color(0.55f, 0.5f, 0.6f));
                foreach (var sh in bt.GetComponents<Shadow>()) Destroy(sh);
                bt.text = (Tiers[k] - red).ToString();
            }
            return rt.gameObject;
        }

        static readonly Dictionary<int, Sprite> icons = new();
        public static Sprite Icon(int id)
        {
            if (icons.TryGetValue(id, out var s)) return s;
            return icons[id] = UIKit.Icon("Icons/traits/trait_" + id);
        }

        // ---------- 툴팁 ----------
        public void Hover(int trait, float y)
        {
            hoverTrait = trait;
            hoverY = y;
            if (trait == 0) { tip.gameObject.SetActive(false); return; }
            var p = PlayerController.Instance;
            if (p) ShowTip(p.rev);
        }

        void ShowTip(Revolver rev)
        {
            var counts = rev.TraitCounts();
            counts.TryGetValue(hoverTrait, out int n);
            int tier = rev.TierOf(n), red = rev.TierThresholdReduce;
            Tables.Traits.TryGetValue(hoverTrait, out var t);
            var ico = Icon(hoverTrait);
            tipIcon.sprite = ico; tipIcon.enabled = ico;
            tipTitle.text = $"<b>{t?.Name ?? hoverTrait.ToString()}</b>   <size=18><color=#F3EDE2>{n}개 · {(tier > 0 ? (tier / 2) + "단계 활성" : "비활성")}</color></size>";
            tipDesc.text = t != null ? t.Desc : "";
            var lines = new List<string>();
            foreach (var (trait, tr, effect) in Tables.TraitTiers.Where(x => x.trait == hoverTrait).OrderBy(x => x.tier))
            {
                string desc = Tables.Effects.TryGetValue(effect, out var e) ? e.Desc : "";
                bool on = tier >= tr;
                lines.Add(on ? $"<color=#FFD54A><b>{tr - red}개</b>  {desc}</color>" : $"<color=#8C8696><b>{tr - red}개</b>  {desc}</color>");
            }
            int next = Tiers.Select(k => k - red).Where(k => k > n).DefaultIfEmpty(0).First();
            if (next > 0) lines.Add($"\n<size=17>다음 단계까지 {next - n}개</size>");
            else lines.Add("\n<size=17><color=#FFD54A>최고 단계</color></size>");
            tipText.text = string.Join("\n", lines);
            float h = tipText.preferredHeight + 96 + 24;
            tip.sizeDelta = new Vector2(420, h);
            tipText.rectTransform.sizeDelta = new Vector2(372, h - 110);
            tip.anchoredPosition = root.anchoredPosition + new Vector2(RowW + 12, hoverY + 6);
            if (!tip.gameObject.activeSelf) tip.gameObject.SetActive(true);
        }

        static Color TraitColor(int id)
        {
            var b = new BulletDef { Trait1 = id };
            return BulletLook.Of(b);
        }
    }

    /// <summary>특성 줄 하나의 마우스 올림 감지</summary>
    public class TraitChip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public TraitHud hud;
        public int trait;
        public float y;
        public void OnPointerEnter(PointerEventData e) { if (hud) hud.Hover(trait, y); }
        public void OnPointerExit(PointerEventData e) { if (hud) hud.Hover(0, 0); }
        void OnDisable() { if (hud) hud.Hover(0, 0); }
    }
}
