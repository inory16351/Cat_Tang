// 인벤토리 (Tab) — 세 칸 배치
//  왼쪽: 탄환 시계(12칸, 깨어나면 금빛 · 연산 수는 작은 점) + 양손 총
//  가운데: 보관함 4×3 + 팔기 칸 + 골드
//  오른쪽: 설명 / 능력치 (탭)
// 탄을 클릭하면 '탄 강화' 창: 위 · 아래 연산 칸, 보관 중인 연산 목록, 깨어나는 시각 미리보기. 전투 중에는 보기만.
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace CatTang
{
    public class InventoryScreen : MonoBehaviour
    {
        PlayerController p;
        readonly List<SlotWidget> widgets = new();
        readonly Dictionary<int, Image> slotFrames = new(), slotGlow = new();
        readonly Dictionary<int, Transform> pipRoots = new();
        Text infoTitle, infoBody, lockText, goldText, tabDesc, tabStats;
        Image infoIcon;
        bool statsTab;
        RectTransform root;
        // 탄 강화 창
        GameObject enhance;
        SlotPlace enhPlace;
        int enhIndex;

        static readonly Color Brown = new Color(0.3f, 0.18f, 0.1f);
        static readonly Color BrownText = new Color(0.27f, 0.17f, 0.1f);

        public bool CanEdit => p && (RunManager.Instance == null || !RunManager.Instance.inCombat);

        public static InventoryScreen Open(Transform canvas, PlayerController p)
        {
            var rt = UIKit.Stretch("Inventory", canvas);
            var s = rt.gameObject.AddComponent<InventoryScreen>();
            s.p = p;
            s.root = rt;
            s.Build();
            return s;
        }

        void Build()
        {
            UIKit.Dim(root, 0.8f);
            var panel = UIKit.Panel("Panel", root, new Vector2(0.5f, 0.5f), new Vector2(0, -10), new Vector2(1760, 940), true);
            var P = panel.transform;
            UIKit.Banner("인벤토리", P, new Vector2(0.5f, 1), new Vector2(0, 10), 520);
            UIKit.CloseButton(P, () => FindFirstObjectByType<GameUI>()?.CloseAll());

            // ---- 왼쪽: 탄환 시계 + 총 ----
            Section(P, "탄환 시계", new Vector2(-560, 340));
            var clock = UIKit.MakeRect("Clock", P, new Vector2(0.5f, 0.5f), new Vector2(-560, 70), new Vector2(10, 10));
            var face = UIKit.Img("Face", clock, "slot_round", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150, 150));
            face.color = new Color(1, 1, 1, 0.55f);
            float R = 205;
            for (int s = 1; s <= 12; s++)
            {
                float ang = s * 30 * Mathf.Deg2Rad;
                var pos = new Vector2(Mathf.Sin(ang), Mathf.Cos(ang)) * R;
                var glow = UIKit.Img("Glow" + s, clock, "slot_round", new Vector2(0.5f, 0.5f), pos, new Vector2(118, 118));
                glow.color = new Color(1f, 0.82f, 0.3f, 0f);
                slotGlow[s] = glow;
                var frame = UIKit.Img("Slot" + s, clock, "slot_round", new Vector2(0.5f, 0.5f), pos, new Vector2(100, 100));
                frame.raycastTarget = true;
                slotFrames[s] = frame;
                AddWidget(frame.gameObject, SlotPlace.Cylinder, s, 74);
                var hour = UIKit.Label("Hour", clock, new Vector2(0.5f, 0.5f), pos * 0.62f, new Vector2(40, 26), 20, TextAnchor.MiddleCenter, true, s % 2 == 1 ? UIKit.Gold : UIKit.Blue);
                hour.text = s.ToString();
                pipRoots[s] = UIKit.MakeRect("Pips", frame.transform, new Vector2(0.5f, 0), new Vector2(0, 2), new Vector2(60, 14));
            }
            GunSlot(P, SlotPlace.GunLeft, new Vector2(-700, -260), "왼손 · 짝수");
            GunSlot(P, SlotPlace.GunRight, new Vector2(-420, -260), "오른손 · 홀수");

            // ---- 가운데: 보관함 ----
            Section(P, "보관함", new Vector2(0, 340));
            for (int i = 0; i < Inventory.StorageSize; i++)
            {
                var pos = new Vector2(-195 + (i % 4) * 130, 220 - (i / 4) * 130);
                var fr = UIKit.Img("Store" + i, P, "slot", new Vector2(0.5f, 0.5f), pos, new Vector2(116, 116));
                fr.raycastTarget = true;
                AddWidget(fr.gameObject, SlotPlace.Storage, i, 84);
            }
            var sell = UIKit.Img("Sell", P, "slot", new Vector2(0.5f, 0.5f), new Vector2(-130, -235), new Vector2(116, 116));
            sell.raycastTarget = true;
            sell.color = new Color(1f, 0.78f, 0.62f);
            var sw = AddWidget(sell.gameObject, SlotPlace.Sell, 0, 64);
            sw.icon.sprite = UIKit.Spr("coin");
            UIKit.Label("SellLabel", P, new Vector2(0.5f, 0.5f), new Vector2(-130, -315), new Vector2(200, 30), 20, TextAnchor.MiddleCenter, true, UIKit.Gold).text = "끌어서 팔기";
            var coin = UIKit.Img("Coin", P, "coin", new Vector2(0.5f, 0.5f), new Vector2(60, -235), new Vector2(58, 58));
            goldText = UIKit.Label("Gold", P, new Vector2(0.5f, 0.5f), new Vector2(170, -235), new Vector2(180, 50), 34, TextAnchor.MiddleLeft, true, UIKit.Gold);
            lockText = UIKit.Label("Lock", P, new Vector2(0.5f, 0), new Vector2(0, 60), new Vector2(1500, 30), 20, TextAnchor.MiddleCenter, false, UIKit.Muted);

            // ---- 오른쪽: 설명 / 능력치 ----
            var info = UIKit.Panel("Info", P, new Vector2(0.5f, 0.5f), new Vector2(560, 10), new Vector2(520, 760), false, "tooltip", 1.6f);
            var I = info.transform;
            var tabA = UIKit.MakeButton("TabDesc", I, new Vector2(0.5f, 1), new Vector2(-118, -50), new Vector2(220, 64), "설명", () => { statsTab = false; Refresh(); }, 22);
            var tabB = UIKit.MakeButton("TabStats", I, new Vector2(0.5f, 1), new Vector2(118, -50), new Vector2(220, 64), "능력치", () => { statsTab = true; Refresh(); }, 22);
            tabDesc = tabA.GetComponentInChildren<Text>();
            tabStats = tabB.GetComponentInChildren<Text>();
            infoIcon = UIKit.Img("Icon", I, "slot", new Vector2(0.5f, 1), new Vector2(0, -170), new Vector2(130, 130));
            infoIcon.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            infoTitle = UIKit.Label("Title", I, new Vector2(0.5f, 1), new Vector2(0, -250), new Vector2(460, 44), 30, TextAnchor.MiddleCenter, true, Brown);
            infoBody = UIKit.Label("Body", I, new Vector2(0.5f, 1), new Vector2(0, -280), new Vector2(440, 440), 20, TextAnchor.UpperLeft, false, BrownText);
            infoBody.rectTransform.pivot = new Vector2(0.5f, 1);
            foreach (var t in new[] { infoTitle, infoBody }) foreach (var sh in t.GetComponents<Shadow>()) Destroy(sh);
            Refresh();
            ShowTip(null);
        }

        void Section(Transform P, string title, Vector2 pos)
        {
            UIKit.Label("Section_" + title, P, new Vector2(0.5f, 0.5f), pos, new Vector2(400, 40), 30, TextAnchor.MiddleCenter, true, UIKit.Gold).text = title;
        }

        void GunSlot(Transform P, SlotPlace place, Vector2 pos, string label)
        {
            var fr = UIKit.Img(place.ToString(), P, "slot", new Vector2(0.5f, 0.5f), pos, new Vector2(150, 150));
            fr.raycastTarget = true;
            AddWidget(fr.gameObject, place, 0, 120);
            UIKit.Label("Label", fr.transform, new Vector2(0.5f, 0), new Vector2(0, -20), new Vector2(220, 30), 20, TextAnchor.MiddleCenter, true, place == SlotPlace.GunRight ? UIKit.Gold : UIKit.Blue).text = label;
        }

        SlotWidget AddWidget(GameObject go, SlotPlace place, int index, float iconSize)
        {
            var w = go.AddComponent<SlotWidget>();
            w.place = place;
            w.index = index;
            w.owner = this;
            var ic = UIKit.MakeRect("Item", go.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(iconSize, iconSize)).gameObject.AddComponent<Image>();
            ic.preserveAspect = true;
            ic.raycastTarget = false;
            w.icon = ic;
            widgets.Add(w);
            return w;
        }

        // ---------- 자료 ----------
        Slot EnhSlot => enhPlace == SlotPlace.Cylinder ? p.rev.slots[enhIndex] : null;
        Item EnhItem => enhPlace == SlotPlace.Storage ? p.inv.storage[enhIndex] : null;
        List<OpDef> EnhOps => EnhSlot != null ? EnhSlot.ops : EnhItem?.ops;
        BulletDef EnhBullet => EnhSlot != null ? EnhSlot.bullet : EnhItem?.bullet;

        public Item ItemAt(SlotWidget w)
        {
            var rev = p.rev;
            switch (w.place)
            {
                case SlotPlace.Cylinder:
                    var sl = rev.slots[w.index];
                    return sl.bullet == null ? null : new Item { kind = ItemKind.Bullet, bullet = sl.bullet, ops = sl.ops };
                case SlotPlace.Storage:
                case SlotPlace.EnhanceStore: return p.inv.storage[w.index];
                case SlotPlace.GunRight: return rev.rightGun == null ? null : Item.Of(rev.rightGun);
                case SlotPlace.GunLeft: return rev.leftGun == null ? null : Item.Of(rev.leftGun);
                case SlotPlace.EnhanceOp:
                    var ops = EnhOps;
                    return ops != null && w.index < ops.Count ? Item.Of(ops[w.index]) : null;
                default: return null;
            }
        }

        void Put(SlotWidget w, Item it)
        {
            var rev = p.rev;
            switch (w.place)
            {
                case SlotPlace.Cylinder:
                    rev.slots[w.index].bullet = it?.bullet;
                    rev.slots[w.index].ops = it != null ? new List<OpDef>(it.ops) : new List<OpDef>();
                    break;
                case SlotPlace.Storage:
                case SlotPlace.EnhanceStore: p.inv.storage[w.index] = it; break;
                case SlotPlace.GunRight: rev.rightGun = it?.gun; break;
                case SlotPlace.GunLeft: rev.leftGun = it?.gun; break;
            }
        }

        static bool Accepts(SlotPlace place, Item it)
        {
            if (it == null) return place == SlotPlace.Storage || place == SlotPlace.Cylinder || place == SlotPlace.EnhanceStore;
            return place switch
            {
                SlotPlace.Cylinder => it.kind == ItemKind.Bullet,
                SlotPlace.GunRight or SlotPlace.GunLeft => it.kind == ItemKind.Gun,
                SlotPlace.Storage or SlotPlace.EnhanceStore => it.kind != ItemKind.Gun,      // 총은 양손에만 (2자루)
                _ => false
            };
        }

        GameUI Gui => FindFirstObjectByType<GameUI>();

        public void Move(SlotWidget src, SlotWidget dst)
        {
            if (!CanEdit) return;
            var a = ItemAt(src);
            if (a == null) return;

            // ---- 탄 강화 창 안 ----
            if (dst.place == SlotPlace.EnhanceOp || src.place == SlotPlace.EnhanceOp)
            {
                var ops = EnhOps;
                var b = EnhBullet;
                if (ops == null || b == null) return;
                if (src.place == SlotPlace.EnhanceOp && dst.place == SlotPlace.EnhanceOp)
                {
                    // 위 · 아래 맞바꾸기 (계산 순서)
                    if (src.index < ops.Count && dst.index < ops.Count) (ops[src.index], ops[dst.index]) = (ops[dst.index], ops[src.index]);
                    else if (src.index < ops.Count) { var o = ops[src.index]; ops.RemoveAt(src.index); ops.Add(o); }
                }
                else if (dst.place == SlotPlace.EnhanceOp)
                {
                    if (a.kind != ItemKind.Op) { Gui?.Toast("연산만 끼울 수 있음"); return; }
                    if (dst.index < ops.Count)
                    {
                        var old = ops[dst.index];
                        ops[dst.index] = a.op;
                        Put(src, Item.Of(old));
                    }
                    else
                    {
                        if (ops.Count >= b.OpSlots) { Gui?.Toast($"'{b.Name}'의 연산 칸은 {b.OpSlots}개"); return; }
                        ops.Add(a.op);
                        Put(src, null);
                    }
                }
                else
                {
                    // 연산 떼어 보관함으로
                    if (!Accepts(dst.place, a) || ItemAt(dst) != null) { Gui?.Toast("빈 보관함 칸으로 끌어 놓기"); return; }
                    ops.RemoveAt(src.index);
                    Put(dst, a);
                }
                Refresh();
                return;
            }

            if (dst.place == SlotPlace.Sell)
            {
                if (src.place == SlotPlace.GunLeft || src.place == SlotPlace.GunRight) { Gui?.Toast("손에 든 총은 팔 수 없음"); return; }
                p.inv.gold += a.SellPrice;
                Put(src, null);
                Gui?.Toast($"{a.Name} 판매 +{a.SellPrice} 골드");
                Refresh();
                return;
            }
            if (a.kind == ItemKind.Op && dst.place == SlotPlace.Cylinder)
            {
                var sl = p.rev.slots[dst.index];
                if (sl.bullet == null || sl.ops.Count >= sl.bullet.OpSlots) { Gui?.Toast(sl.bullet == null ? "빈 칸" : $"연산 칸이 가득 참 — 탄을 클릭해 강화 창에서 바꾸기"); return; }
                Put(src, null);
                sl.ops.Add(a.op);
                Refresh();
                return;
            }
            var bItem = ItemAt(dst);
            if (a.kind == ItemKind.Gun && dst.place != SlotPlace.GunLeft && dst.place != SlotPlace.GunRight) { Gui?.Toast("총은 양손에만 든다 — 왼손 · 오른손 칸끼리 끌어 바꾸기"); return; }
            if (!Accepts(dst.place, a) || !Accepts(src.place, bItem)) { Gui?.Toast("거기에는 놓을 수 없음"); return; }
            if (a.kind == ItemKind.Gun && bItem == null && (src.place == SlotPlace.GunLeft || src.place == SlotPlace.GunRight)) { Gui?.Toast("손을 비울 수는 없음"); return; }
            Put(dst, a);
            Put(src, bItem);
            Refresh();
        }

        public void Click(SlotWidget w)
        {
            var it = ItemAt(w);
            if (it == null || it.kind != ItemKind.Bullet) return;
            if (w.place != SlotPlace.Cylinder && w.place != SlotPlace.Storage) return;
            OpenEnhance(w.place, w.index);
        }

        // ---------- 탄 강화 창 ----------
        readonly List<SlotWidget> enhWidgets = new();
        Text enhJudge;
        Transform enhHours;

        void OpenEnhance(SlotPlace place, int index)
        {
            CloseEnhance();
            enhPlace = place;
            enhIndex = index;
            enhance = UIKit.Stretch("Enhance", root).gameObject;
            UIKit.Dim(enhance.transform, 0.6f);
            var panel = UIKit.Panel("Panel", enhance.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1240, 760), true);
            var P = panel.transform;
            UIKit.Banner("탄 강화", P, new Vector2(0.5f, 1), new Vector2(0, 10), 460);
            UIKit.CloseButton(P, CloseEnhance);
            var b = EnhBullet;
            // 왼쪽: 탄 + 연산 칸 2개 (위 · 아래)
            var big = UIKit.Img("Bullet", P, "slot", new Vector2(0.5f, 0.5f), new Vector2(-400, 90), new Vector2(230, 230));
            big.enabled = false;                                         // 네모 틀 없이 아이콘만
            var bi = UIKit.MakeRect("I", big.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(230, 230)).gameObject.AddComponent<Image>();
            bi.sprite = UIKit.BulletIcon(b); bi.preserveAspect = true; bi.raycastTarget = false;
            var nm = UIKit.Label("Name", P, new Vector2(0.5f, 0.5f), new Vector2(-400, -60), new Vector2(380, 44), 30, TextAnchor.MiddleCenter, true);
            nm.text = b.Name;
            UIKit.Label("Cond", P, new Vector2(0.5f, 0.5f), new Vector2(-400, -100), new Vector2(380, 30), 20, TextAnchor.MiddleCenter, false, UIKit.Muted).text = $"자리 조건: {UIKit.CondKo(b)} · 연산 칸 {b.OpSlots}";
            for (int k = 0; k < 2; k++)
            {
                bool usable = k < b.OpSlots;
                var fr = UIKit.Img("OpSlot" + k, P, "slot_round", new Vector2(0.5f, 0.5f), new Vector2(-140, 170 - k * 150), new Vector2(124, 124));
                fr.raycastTarget = usable;
                fr.color = usable ? Color.white : new Color(1, 1, 1, 0.25f);
                UIKit.Label("L", P, new Vector2(0.5f, 0.5f), new Vector2(-140, 245 - k * 150), new Vector2(200, 26), 18, TextAnchor.MiddleCenter, true, UIKit.Gold).text = k == 0 ? "위 (먼저 계산)" : "아래";
                if (usable) enhWidgets.Add(AddWidget(fr.gameObject, SlotPlace.EnhanceOp, k, 80));
            }
            // 판정 미리보기
            enhJudge = UIKit.Label("Judge", P, new Vector2(0.5f, 0.5f), new Vector2(-270, -170), new Vector2(560, 60), 24, TextAnchor.MiddleCenter, true, UIKit.Cream);
            UIKit.Label("HoursTitle", P, new Vector2(0.5f, 0.5f), new Vector2(-270, -225), new Vector2(560, 28), 18, TextAnchor.MiddleCenter, false, UIKit.Muted).text = "이 탄이 깨어나는 칸 (지금 연산 기준)";
            enhHours = UIKit.MakeRect("Hours", P, new Vector2(0.5f, 0.5f), new Vector2(-270, -270), new Vector2(560, 50));
            // 오른쪽: 보관 중인 연산
            UIKit.Label("OpsTitle", P, new Vector2(0.5f, 0.5f), new Vector2(330, 250), new Vector2(460, 36), 26, TextAnchor.MiddleCenter, true, UIKit.Gold).text = "보관함의 연산";
            for (int i = 0; i < Inventory.StorageSize; i++)
            {
                var fr = UIKit.Img("St" + i, P, "slot", new Vector2(0.5f, 0.5f), new Vector2(160 + (i % 4) * 112, 160 - (i / 4) * 112), new Vector2(102, 102));
                fr.raycastTarget = true;
                enhWidgets.Add(AddWidget(fr.gameObject, SlotPlace.EnhanceStore, i, 72));
            }
            UIKit.Label("Help", P, new Vector2(0.5f, 0), new Vector2(0, 70), new Vector2(1100, 30), 19, TextAnchor.MiddleCenter, false, UIKit.Muted).text =
                "연산을 칸으로 끌어 끼우기 · 칸끼리 끌면 순서 바꾸기 · 칸의 연산을 빈 보관함으로 끌면 떼기";
            Refresh();
        }

        void CloseEnhance()
        {
            foreach (var w in enhWidgets) widgets.Remove(w);
            enhWidgets.Clear();
            if (enhance) Destroy(enhance);
            enhance = null;
            Refresh();
        }

        // ---------- 그리기 ----------
        public void Refresh()
        {
            if (!p) return;
            var rev = p.rev;
            foreach (var w in widgets)
            {
                if (!w || w.place == SlotPlace.Sell) continue;
                var it = ItemAt(w);
                bool show = it != null && !(w.place == SlotPlace.EnhanceStore && it.kind != ItemKind.Op);
                w.icon.enabled = show;
                if (w.place == SlotPlace.EnhanceStore) w.GetComponent<Image>().color = it == null || it.kind == ItemKind.Op ? Color.white : new Color(1, 1, 1, 0.3f);
                if (!show) continue;
                w.icon.sprite = it.kind switch { ItemKind.Bullet => UIKit.BulletIcon(it.bullet), ItemKind.Gun => UIKit.GunIcon(it.gun), _ => UIKit.Spr("op_token") };
                w.icon.color = Color.white;
                var label = w.icon.transform.Find("OpName");
                if (it.kind == ItemKind.Op)
                {
                    var t = label ? label.GetComponent<Text>() : UIKit.Label("OpName", w.icon.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(80, 40), 24, TextAnchor.MiddleCenter, true);
                    t.text = it.op.Name;
                }
                else if (label) Destroy(label.gameObject);
            }
            for (int s = 1; s <= 12; s++)
            {
                var b = rev.slots[s].bullet;
                bool aw = rev.Awakened(s);
                slotGlow[s].color = new Color(1f, 0.82f, 0.3f, aw ? 0.9f : 0f);
                slotFrames[s].color = b == null ? new Color(0.55f, 0.55f, 0.6f) : Color.white;
                var pr = pipRoots[s];
                for (int c = pr.childCount - 1; c >= 0; c--) Destroy(pr.GetChild(c).gameObject);
                int slots = b != null ? b.OpSlots : 0;
                for (int k = 0; k < slots; k++)
                {
                    var dot = UIKit.Img("Pip", pr, "op_token", new Vector2(0.5f, 0.5f), new Vector2((k - (slots - 1) / 2f) * 18, 0), new Vector2(16, 16));
                    dot.color = k < rev.slots[s].ops.Count ? Color.white : new Color(1, 1, 1, 0.3f);
                }
            }
            lockText.text = CanEdit ? "끌어놓기로 교체 · 탄을 클릭하면 탄 강화 창 · 깨어난 칸은 금빛" : "전투 중에는 바꿀 수 없음 (보기만)";
            lockText.color = CanEdit ? UIKit.Muted : UIKit.Red;
            goldText.text = p.inv.gold.ToString();
            if (tabDesc) { tabDesc.color = statsTab ? UIKit.Muted : UIKit.Gold; tabStats.color = statsTab ? UIKit.Gold : UIKit.Muted; }
            if (statsTab) ShowStats();
            if (enhance) RefreshEnhance();
        }

        void RefreshEnhance()
        {
            var b = EnhBullet;
            if (b == null) { CloseEnhance(); return; }
            var ops = EnhOps;
            if (enhPlace == SlotPlace.Cylinder)
            {
                int j = p.rev.JudgeTime(enhIndex);
                bool aw = p.rev.Awakened(enhIndex);
                string chain = ops.Count == 0 ? $"{enhIndex}" : $"{enhIndex} " + string.Join(" ", ops.Select(o => o.Name));
                enhJudge.text = $"{enhIndex}시 칸 · {chain} = 판정 <b>{j}시</b>  {(aw ? "<color=#FFD54A>깨어남</color>" : "<color=#9AA0A6>잠듦</color>")}";
            }
            else enhJudge.text = "보관함의 탄 — 실린더에 놓으면 판정";
            for (int c = enhHours.childCount - 1; c >= 0; c--) Destroy(enhHours.GetChild(c).gameObject);
            for (int h = 1; h <= 12; h++)
            {
                bool wake = Revolver.Meets(b.CondType, b.CondValue, Revolver.ApplyOps(h, ops));
                var dot = UIKit.Img("H" + h, enhHours, "slot_round", new Vector2(0.5f, 0.5f), new Vector2((h - 6.5f) * 44, 0), new Vector2(40, 40));
                dot.color = wake ? new Color(1f, 0.85f, 0.35f) : new Color(1, 1, 1, 0.3f);
                UIKit.Label("T", dot.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40, 30), 16, TextAnchor.MiddleCenter, true, wake ? Brown : UIKit.Muted).text = h.ToString();
            }
        }

        void ShowStats()
        {
            var rev = p.rev;
            infoIcon.sprite = UIKit.Spr("hp_frame");
            infoTitle.text = "능력치";
            string Gun(GunDef g, bool right) => g == null ? "-" : $"{g.Name}  ×{g.DamageMul:0.00} · {1f / (g.FireInterval / Mathf.Max(0.2f, 1 + p.AttackSpeedBonus)):0.0}발/초\n   <size=17>{UIKit.HandEffectLine(g, right)}</size>";
            var lines = new List<string>
            {
                $"<b>체력</b>  {Mathf.CeilToInt(p.health.hp)} / {Mathf.CeilToInt(p.health.maxHp)}",
                $"<b>공격력</b>  ×{p.DamageMul:0.00}",
                $"<b>장전</b>  {p.ReloadTime:0.00}초   <b>대시</b>  {p.dashMax}칸 · {p.DashRechargeTime:0.0}초",
                $"<b>회피 확률</b>  {p.DodgeChance * 100:0}%   <b>열기</b>  1발 {p.HeatPerHit:0.00}칸",
                $"<b>오른손</b>  {Gun(rev.rightGun, true)}",
                $"<b>왼손</b>  {Gun(rev.leftGun, false)}",
                $"<b>깨어난 탄</b>  {12 - rev.UnawakenedCount()} / 12",
                "",
                "<b>특성</b>"
            };
            var tc = rev.TraitCounts();
            if (tc.Count == 0) lines.Add("  깨어난 탄이 없음 (기본 '탄환'은 늘 잠듦)");
            foreach (var kv in tc.OrderByDescending(k => k.Value))
            {
                var name = Tables.Traits.TryGetValue(kv.Key, out var t) ? t.Name : kv.Key.ToString();
                int tier = rev.TierOf(kv.Value);
                string eff = string.Join(" / ", Tables.TraitTiers.Where(x => x.trait == kv.Key && x.tier <= tier).Select(x => Tables.Effects.TryGetValue(x.effect, out var e) ? e.Desc : ""));
                lines.Add(tier > 0 ? $"  <b>{name} {kv.Value} · {tier / 2}단계</b>\n   <size=17>{eff}</size>" : $"  {name} {kv.Value}  <size=16>(2개부터)</size>");
            }
            infoBody.text = string.Join("\n", lines);
        }

        public void ShowTip(SlotWidget w)
        {
            if (statsTab && w == null) { ShowStats(); return; }
            if (w) statsTab = false;
            var it = w ? ItemAt(w) : null;
            if (w && w.place == SlotPlace.Sell) { SetTip(UIKit.Spr("coin"), "팔기", "탄 · 연산을 여기로 끌어 오면 절반 값에 판다. 총은 팔 수 없다 (주울 때 바꾸거나 버린다)."); return; }
            if (it == null) { SetTip(UIKit.Spr("slot_round"), "탄환 시계", "오른손 총이 홀수 칸, 왼손 총이 짝수 칸을 쏜다. 발사 순서는 1 → 12.\n\n탄은 판정 시각이 자리 조건에 맞으면 깨어나 특성 · 고유 효과가 켜진다 (금빛 칸).\n탄을 클릭하면 강화 창에서 연산을 끼워 판정 시각을 바꿀 수 있다.\n\n처음 든 '탄환'은 효과가 없는 기본 탄."); return; }
            switch (it.kind)
            {
                case ItemKind.Bullet:
                    var b = it.bullet;
                    string traits = b.Trait1 == 0 ? "없음" : string.Join(" · ", new[] { b.Trait1, b.Trait2 }.Where(x => x != 0).Select(x => Tables.Traits.TryGetValue(x, out var t) ? t.Name : x.ToString()));
                    string eff = string.Join("\n", new[] { b.Effect1, b.Effect2 }.Where(x => x != 0).Select(x => Tables.Effects.TryGetValue(x, out var e) ? "· " + e.Desc : ""));
                    string judge = w.place == SlotPlace.Cylinder ? $"\n판정 <b>{p.rev.JudgeTime(w.index)}시</b> · {(p.rev.Awakened(w.index) ? "<b>깨어남</b>" : "잠듦")}" : "";
                    string ops = it.ops.Count > 0 ? $"\n연산: {string.Join(" → ", it.ops.Select(o => o.Name))}" : "";
                    SetTip(UIKit.BulletIcon(b), b.Name,
                        $"{UIKit.GradeKo(b.Grade)} · 공격 {b.Damage} · 연산 칸 {b.OpSlots}\n조건: <b>{UIKit.CondKo(b)}</b>{judge}{ops}\n특성: {traits}\n\n{(eff == "" ? "효과 없음" : eff)}\n\n<i>{b.Desc}</i>\n\n<size=16>클릭: 탄 강화</size>");
                    break;
                case ItemKind.Gun:
                    var g = it.gun;
                    int hand = w.place == SlotPlace.GunRight ? 1 : w.place == SlotPlace.GunLeft ? 2 : 0;
                    SetTip(UIKit.GunIcon(g), g.Name, $"{UIKit.GradeKo(g.Grade)}\n공격 ×{g.DamageMul:0.00} · 연사 {g.FireInterval:0.00}초 · 장전 {g.ReloadTime:0.0}초 · 사거리 {g.Range:0}m\n\n{UIKit.GunHandText(g, hand)}\n\n<i>{g.Desc}</i>\n\n<size=16>왼손 · 오른손 칸끼리 끌면 좌우를 바꾼다</size>");
                    break;
                default:
                    SetTip(UIKit.Spr("op_token"), "연산 " + it.op.Name, $"탄에 끼우면 판정 시각에 {it.op.Name}.\n위 칸부터 계산한다. 12를 넘거나 0 이하가 되어도 값을 그대로 둔다.");
                    break;
            }
        }

        void SetTip(Sprite icon, string title, string body)
        {
            infoIcon.sprite = icon;
            infoTitle.text = title;
            infoBody.text = body;
            if (tabDesc) { tabDesc.color = UIKit.Gold; tabStats.color = UIKit.Muted; }
        }
    }
}
