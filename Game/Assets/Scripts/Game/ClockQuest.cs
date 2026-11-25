// 마을 시계바늘 수리 퀘스트 (2026-11-25 사용자 확정)
//   1장 클리어 → 꿈2 → 마을에서 깨어나면 광장 위 떠서 돌던 시곗바늘(ClockHand_Orbit)이 멈추고 금이 가 조각이 떠 있음
//   → 재봉사가 '바늘을 붙들 실' = 꿈 바깥(현실)에서 흘러든 물건 3개를 찾아 오라 함 → 반딧불 길이 물건까지 이어짐
//   → 주워서 재봉사에게 건네면 그 물건의 현실 기억 컷이 스치고 조각 하나가 바늘로 돌아감 → 셋 다 고치면 바늘이 다시 돎
// 씬 배치(물건 · 반딧불 길 · 재봉사 건네기)는 Editor/ClockQuestBuilder.cs가 Village 씬에 저장. 상태는 Meta.StorySeen 키
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CatTang
{
    public class ClockQuest : MonoBehaviour
    {
        public const string KeyStart = "cq_start", KeyDone = "cq_done";
        public const string Tailor = "실타래 재봉사";

        public struct Item
        {
            public string id, name, pickupLine;
            public string[] tailorLines, memories;   // 기억 컷 = Resources/Story/Memories/<이름>.png
            public string memoryDesc;                // 그림이 없을 때 자리 표시 설명
        }

        // 대사 초안 (사용자 컨펌 전) — 물건: 돋보기 안경 · 이름 지워진 팔찌 · 삐뚤빼뚤한 쪽지
        public static readonly Item[] Items =
        {
            new Item
            {
                id = "glasses", name = "돋보기 안경",
                pickupLine = "고양이#의문::......안경? 렌즈 한쪽에 확대경이 달려 있군.",
                tailorLines = new[] { Tailor + "::오래 쓴 안경이네.", Tailor + "::아주 작은 걸 오래 들여다본 사람의 것이야.", Tailor + "::톱니 하나, 나사 하나...... 평생을 그렇게 보냈겠지." },
                memories = new[] { "mem_glasses_a", "mem_glasses_b" },
                memoryDesc = "작업대 위 흐린 시야 — 핀셋 끝의 작은 톱니",
            },
            new Item
            {
                id = "band", name = "이름 지워진 팔찌",
                pickupLine = "고양이#의문::팔찌...... 이름이 적힌 자리가 번져서 읽을 수가 없어.",
                tailorLines = new[] { Tailor + "::이건...... 차가운 냄새가 나. 하얀 방의 냄새.", Tailor + "::이름이 지워졌다고 사람이 지워지는 건 아니란다." },
                memories = new[] { "mem_band_a", "mem_band_b" },
                memoryDesc = "링거 줄과 침대 난간 — 손목에 감긴 팔찌",
            },
            new Item
            {
                id = "note", name = "삐뚤빼뚤한 쪽지",
                pickupLine = "고양이#의문::크레용 글씨...... '시계 고쳐 주세요'?",
                tailorLines = new[] { Tailor + "::아이 글씨구나.", Tailor + "::꾹꾹 눌러 썼어.", Tailor + "::누군가는 이 부탁을 아주 오래 기억하고 있었던 거야." },
                memories = new[] { "mem_note_a" },   // 둘째 컷(창밖의 멈춘 시계탑)은 ChatGPT 생성 한도로 아직 없음 (2026-11-25)
                memoryDesc = "어린 손이 크레용으로 쓰는 쪽지 — 창밖의 멈춘 시계탑",
            },
        };

        public static bool Active => Meta.ChaptersCleared >= 1 && !Meta.StorySeen(KeyDone);
        public static bool Started => Meta.StorySeen(KeyStart);
        public static bool Got(string id) => Meta.StorySeen("cq_got_" + id);
        public static bool Fixed(string id) => Meta.StorySeen("cq_fix_" + id);
        public static int FixedCount { get { int n = 0; foreach (var it in Items) if (Fixed(it.id)) n++; return n; } }
        public static bool Carrying { get { foreach (var it in Items) if (Got(it.id) && !Fixed(it.id)) return true; return false; } }
        public static Item? Find(string id) { foreach (var it in Items) if (it.id == id) return it; return null; }

        /// <summary>받침 있으면 "을", 없으면 "를"</summary>
        static string Eul(string w) { char c = w[w.Length - 1]; return c >= 0xAC00 && c <= 0xD7A3 && (c - 0xAC00) % 28 != 0 ? "을" : "를"; }

        public static ClockQuest I { get; private set; }
        /// <summary>줍기 · 건네기 연출 중 (상호작용 안내를 숨김)</summary>
        public static bool Busy;
        Transform orbit, hand, droop;
        DreamFloat orbitFloat;
        TrailRenderer trail;
        ParticleSystem sand;
        float baseSpin;
        readonly List<GameObject> shards = new();
        bool broken;

        void Awake() => I = this;

        void Start()
        {
            orbit = GameObject.Find("ClockHand_Orbit")?.transform;
            if (orbit)
            {
                orbitFloat = orbit.GetComponent<DreamFloat>();
                if (orbitFloat) baseSpin = orbitFloat.spin;
                hand = orbit.Find("ClockHand_Float");
                // 바늘을 축째로 처지게 할 사이 마디 (돌기 · 오르내림은 DreamFloat가 orbit에 계속 줌)
                if (hand)
                {
                    droop = new GameObject("HandDroop").transform;
                    droop.SetParent(orbit, false);
                    hand.SetParent(droop, true);
                }
                trail = orbit.GetComponentInChildren<TrailRenderer>(true);
                sand = orbit.GetComponentInChildren<ParticleSystem>(true);
            }
            Refresh();
        }

        /// <summary>상태에 맞게 바늘 · 물건 · 반딧불을 맞춤</summary>
        public void Refresh()
        {
            SetBroken(Active || Ch2Quest.ClockStopped);   // 2장 클리어 뒤 다시 멈춤 (Ch2Quest)
            foreach (var m in GetComponentsInChildren<MementoPickup>(true)) m.gameObject.SetActive(Active && Started && !Got(m.id));
            foreach (var f in GetComponentsInChildren<FireflyTrail>(true)) f.gameObject.SetActive(Active && Started && !Got(f.id));
        }

        void SetBroken(bool on)
        {
            broken = on;
            if (!orbit) return;
            if (orbitFloat) { orbitFloat.spin = on ? 0f : baseSpin; orbitFloat.tilt = on ? 5f : 1.2f; }
            if (trail) trail.emitting = !on;
            if (sand) { if (on) sand.Stop(true, ParticleSystemStopBehavior.StopEmitting); else if (!sand.isPlaying) sand.Play(); }
            int want = on ? (Active ? Items.Length - FixedCount : 0) + Ch2Quest.ShardsLeft : 0;
            while (shards.Count > want) { var s = shards[shards.Count - 1]; shards.RemoveAt(shards.Count - 1); if (s) Destroy(s); }
            var prefab = Resources.Load<GameObject>("Kit/Village/ClockShards");
            while (on && shards.Count < want && prefab)
            {
                int i = shards.Count;
                float a = i * 120f + 20f;
                var s = Instantiate(prefab, orbit.position + Quaternion.Euler(0, a, 0) * Vector3.forward * 2.4f + Vector3.up * (0.5f * i - 1.2f), Quaternion.Euler(0, a, 25), transform);
                s.transform.localScale *= 0.75f;
                var f = s.AddComponent<DreamFloat>(); f.bob = 0.25f; f.period = 5f + i; f.spin = 12f; f.tilt = 8f;
                shards.Add(s);
            }
        }

        float droopNow;
        void Update()
        {
            if (!droop) return;
            // 고장: 바늘 끝이 아래로 처져 덜덜 떨림 (멈춘 채 가려다 못 감) · 고치면 수평으로 돌아옴
            float t = Time.time;
            float target = broken ? -24f + 4f * (shards.Count - 3) * -1f : 0f;
            droopNow = Mathf.Lerp(droopNow, target, Time.deltaTime * 3f);
            float shake = broken ? Mathf.Sin(t * 37f) * 1.1f + Mathf.Sin(t * 61f) * 0.6f : 0f;
            droop.localRotation = Quaternion.Euler(shake * 0.5f, 0, droopNow + shake);
        }

        // ---------- 퀘스트 흐름 ----------
        /// <summary>재봉사 첫 대화 뒤: 퀘스트 시작 → 반딧불 길이 켜지고 카메라가 하나 비춤</summary>
        public static IEnumerator StartQuest(GameUI ui)
        {
            if (Started || !Active) yield break;
            var p = PlayerController.Instance;
            if (p) p.InputLocked = true;
            bool d = false;
            ui.Talk(Tailor, new[]
            {
                Tailor + "::그리고...... 광장의 바늘 말이야.",
                Tailor + "::멈춰 버렸지?",
                Tailor + "::실로 꿰매 줄 수는 있어.",
                Tailor + "::다만 바늘을 붙들어 둘 '매듭'이 필요해.",
                Tailor + "::이 꿈 바깥에서 흘러든 물건들.",
                Tailor + "::반딧불이 모여드는 곳을 찾아봐.",
            }, () => d = true);
            while (!d) yield return null;
            Meta.MarkStory(KeyStart);
            I?.Refresh();
            var first = I ? I.GetComponentInChildren<FireflyTrail>() : null;
            if (first)
            {
                CameraFollow.FocusOn(first.End + Vector3.back * 1.5f, 0.8f);
                yield return new WaitForSecondsRealtime(1.6f);
                CameraFollow.FocusOn(null, 0f);
            }
            ui.Toast("<color=#FFD54A>반딧불을 따라 숨은 물건 3개를 찾아라</color>");
            if (p) p.InputLocked = false;
        }

        public static IEnumerator Pickup(MementoPickup m)
        {
            var ui = FindFirstObjectByType<GameUI>();
            var p = PlayerController.Instance;
            var it = Find(m.id);
            if (!ui || it == null) yield break;
            if (p) p.InputLocked = true;
            Busy = true;
            Sfx.Play("ui_buy", 0.7f, 0f);
            FxBurst.Spawn(m.transform.position + Vector3.up, 1.6f, FxBurst.Kind.Gold, new Color(1f, 0.9f, 0.5f));
            Meta.MarkStory("cq_got_" + m.id);
            I?.Refresh();
            bool d = false;
            ui.Talk("고양이", new[] { it.Value.pickupLine }, () => d = true);
            while (!d) yield return null;
            ui.Toast($"<color=#FFD54A>{it.Value.name}</color>{Eul(it.Value.name)} 주웠다 — 재봉사에게 가져가자");
            if (p) p.InputLocked = false;
            Busy = false;
            var spot = GameObject.Find("NpcSpot_Wardrobe");
            if (spot) GuideArrow.Show(spot.transform, "재봉사", keep: true);
        }

        /// <summary>재봉사에게 들고 있는 물건을 하나씩 건넴: 대사 → 기억 컷 → 조각이 바늘로 → (셋째) 바늘이 다시 돎</summary>
        public static IEnumerator Deliver(GameUI ui)
        {
            var p = PlayerController.Instance;
            if (p) p.InputLocked = true;
            Busy = true;
            GuideArrow.Clear();
            var spot = GameObject.Find("NpcSpot_Wardrobe");
            foreach (var it in Items)
            {
                if (!Got(it.id) || Fixed(it.id)) continue;
                if (spot) CameraFollow.FocusOn(spot.transform.position + Vector3.back * 1.5f, 0.8f);
                bool d = false;
                ui.Talk(Tailor, it.tailorLines, () => d = true);
                while (!d) yield return null;
                yield return MemoryCut(it);
                Meta.MarkStory("cq_fix_" + it.id);
                if (I) yield return I.RepairStep();
            }
            CameraFollow.FocusOn(null, 0f);
            if (FixedCount >= Items.Length && !Meta.StorySeen(KeyDone))
            {
                if (I) yield return I.RepairFinal(ui);
                Meta.MarkStory(KeyDone);
                I?.Refresh();
            }
            else ui.Toast($"시곗바늘 {FixedCount}/{Items.Length}");
            if (p) p.InputLocked = false;
            Busy = false;
        }

        /// <summary>떠 있던 조각 하나가 바늘로 돌아가 붙고 바늘이 한 칸 나아갔다 멈춤 (Ch2Quest도 씀)</summary>
        public IEnumerator RepairStep()
        {
            if (!orbit) yield break;
            CameraFollow.FocusOn(orbit.position + Vector3.down * 2f, 0.9f);
            yield return new WaitForSecondsRealtime(0.6f);
            var s = shards.Count > 0 ? shards[shards.Count - 1] : null;
            if (s)
            {
                shards.Remove(s);
                var f = s.GetComponent<DreamFloat>(); if (f) Destroy(f);
                Vector3 a = s.transform.position, b = hand ? hand.position : orbit.position;
                for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / 0.7f)
                {
                    if (!s) break;
                    s.transform.position = Vector3.Lerp(a, b, t * t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 1.2f;
                    s.transform.localScale *= 0.985f;
                    yield return null;
                }
                if (s) Destroy(s);
            }
            Sfx.Play("e4_tick_1", 1f, 0f);
            FxBurst.Spawn((hand ? hand.position : orbit.position), 2.2f, FxBurst.Kind.Gold, new Color(1f, 0.85f, 0.4f));
            // 바늘이 한 칸 나아갔다가 다시 멈춤
            float from = orbit.localEulerAngles.y;
            for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / 0.35f)
            {
                orbit.localRotation = Quaternion.Euler(0, from + 30f * Mathf.SmoothStep(0, 1, t), 0);
                yield return null;
            }
            yield return new WaitForSecondsRealtime(0.6f);
            CameraFollow.FocusOn(null, 0f);
        }

        IEnumerator RepairFinal(GameUI ui)
        {
            if (orbit) CameraFollow.FocusOn(orbit.position + Vector3.down * 2f, 0.85f);
            Sfx.Play("e4_tick_2", 1f, 0f);
            if (Sfx.Has("e4_bell")) Sfx.Play("e4_bell", 0.9f, 0f);
            SetBroken(Ch2Quest.ClockStopped);
            yield return new WaitForSecondsRealtime(1.5f);
            bool d = false;
            ui.Talk(Tailor, new[]
            {
                Tailor + "::자, 매듭은 다 지었어.",
                Tailor + "::다시 돌기 시작했지?",
                "고양이#의문::......저 바늘이 다 돌면, 어떻게 되는 거지.",
                Tailor + "::글쎄. 그건 네가 제일 잘 알지 않을까.",
            }, () => d = true);
            while (!d) yield return null;
            CameraFollow.FocusOn(null, 0f);
        }

        /// <summary>남은 조각 없이 바늘이 다시 돎 (종 · 모래 · 꼬리 다시 켜짐) — Ch2Quest 마지막</summary>
        public IEnumerator SpinAgain()
        {
            if (orbit) CameraFollow.FocusOn(orbit.position + Vector3.down * 2f, 0.85f);
            yield return new WaitForSecondsRealtime(0.6f);
            Sfx.Play("e4_tick_2", 1f, 0f);
            if (Sfx.Has("e4_bell")) Sfx.Play("e4_bell", 0.9f, 0f);
            if (orbit) FxBurst.Spawn(hand ? hand.position : orbit.position, 3f, FxBurst.Kind.Gold, new Color(1f, 0.85f, 0.4f));
            SetBroken(Active || Ch2Quest.ClockStopped);
            yield return new WaitForSecondsRealtime(1.6f);
            CameraFollow.FocusOn(null, 0f);
        }

        // ---------- 기억 컷: 화면 가득 현실 장면이 천천히 스치고 사라짐 ----------
        static IEnumerator MemoryCut(Item it)
        {
            var canvasGo = new GameObject("MemoryCut", typeof(Canvas), typeof(CanvasScaler));
            var cv = canvasGo.GetComponent<Canvas>(); cv.renderMode = RenderMode.ScreenSpaceOverlay; cv.sortingOrder = 900;
            var sc = canvasGo.GetComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080); sc.matchWidthOrHeight = 0.5f;
            var bg = new GameObject("Bg", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            bg.transform.SetParent(canvasGo.transform, false);
            var bgr = bg.rectTransform; bgr.anchorMin = Vector2.zero; bgr.anchorMax = Vector2.one; bgr.offsetMin = bgr.offsetMax = Vector2.zero;
            bg.color = new Color(0.05f, 0.03f, 0.06f, 0);
            var img = new GameObject("Cut", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            img.transform.SetParent(canvasGo.transform, false);
            var ir = img.rectTransform; ir.anchorMin = ir.anchorMax = new Vector2(0.5f, 0.5f); ir.sizeDelta = new Vector2(1920, 1080);
            UIKit.Init();
            var cap = UIKit.Label("Caption", canvasGo.transform, new Vector2(0.5f, 0.12f), Vector2.zero, new Vector2(1400, 80), 30, TextAnchor.MiddleCenter, false, UIKit.Cream);
            Sfx.Play("story_dream", 0.5f, 0f);
            for (int k = 0; k < it.memories.Length; k++)
            {
                var tex = Resources.Load<Texture2D>("Story/Memories/" + it.memories[k]);
                img.texture = tex;
                img.color = tex ? Color.white : new Color(0.3f, 0.22f, 0.2f);
                cap.text = tex ? "" : (k == 0 ? it.memoryDesc : "");
                if (tex) ir.sizeDelta = new Vector2(1920, 1920f * tex.height / tex.width);
                float dur = 2.4f;
                for (float t = 0; t < dur; t += Time.unscaledDeltaTime)
                {
                    float a = Mathf.Clamp01(t / 0.5f) * Mathf.Clamp01((dur - t) / 0.5f);
                    bg.color = new Color(0.05f, 0.03f, 0.06f, Mathf.Max(bg.color.a, k == 0 ? Mathf.Clamp01(t / 0.4f) * 0.9f : 0.9f));
                    img.color = new Color(img.color.r, img.color.g, img.color.b, a);
                    cap.color = new Color(UIKit.Cream.r, UIKit.Cream.g, UIKit.Cream.b, a);
                    ir.localScale = Vector3.one * (1.02f + 0.06f * t / dur);
                    ir.anchoredPosition = new Vector2((k % 2 == 0 ? -1 : 1) * 30f * t / dur, 0);
                    yield return null;
                }
            }
            for (float t = 0; t < 0.4f; t += Time.unscaledDeltaTime) { bg.color = new Color(0.05f, 0.03f, 0.06f, 0.9f * (1 - t / 0.4f)); yield return null; }
            Destroy(canvasGo);
        }
    }

}
