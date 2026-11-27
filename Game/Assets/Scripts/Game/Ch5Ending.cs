// 엔딩 '무너지는 마을' (2026-11-26 사용자 확정 · 대사 확정, HANDOFF.md "2026-11-26 — 5장 스토리 · 엔딩 기획 확정")
//   5장 클리어 = 꿈4 '마지막 장' → 내면 세계 맵 '멈춘 1초'(StoryInner, 하얀 방에서 죽음을 받아들임) → 마을에서 깨어나면 마을이 녹아 무너지는 중 (첫 줄 "...마을이... 녹고 있어.")
//   → 다리가 열린 반대 순서로 섬을 하나씩 찾아감: 보안관 → 대장장이 → 재봉사 → 뻐꾸기 → 문지기
//     섬마다 NPC 작별 두 마디 → 원래 물건(3장 거울에 비쳤던 것)으로 돌아감 → 그 섬을 떠나면 섬이 무너져 허공으로
//   → 광장(시계탑 11:59)의 거울 속 고양이 "다 만났어?" → [E 총 내려놓기] → 거울 속 고양이가 웃으며 먼저 바스러짐
//   → 시간의 주인이 광장 가장자리에 나와 낫을 내림 → "...기다려 준 건가." → 시계 12:00 → 종 → 하얗게 → 병실 · 그림책 '끝' → 크레딧
//   → 복원된 마을에서 다시 깨어남 → 문지기 "...또 왔군." / "한 번 더 들여다볼 텐가?" → 승천 모드 입구만 (내용은 따로 기획)
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CatTang
{
    public static class Ch5Ending
    {
        public const string KeyDone = "ending_done", KeyAscend = "ascend_open";
        const string Cat = "고양이", Cuckoo = "태엽 풀린 뻐꾸기", Smith = "모래시계 대장장이", Tailor = "실타래 재봉사", Sheriff = "빈 외투 보안관", Keeper = "괘종시계 문지기";
        const float TalkR = 4.5f, LeaveR = 17f;
        static readonly Color Gold = new(1f, 0.85f, 0.4f);

        /// <summary>5장을 넘고 꿈4를 봄 · 엔딩 전</summary>
        public static bool Pending => Meta.ChaptersCleared >= 5 && Meta.StorySeen("dream" + StoryRun.DreamOf(5)) && !Meta.StorySeen(KeyDone);
        /// <summary>엔딩 뒤 처음 깨어남 → 문지기가 승천 모드를 엶</summary>
        public static bool AscendPending => Meta.StorySeen(KeyDone) && !Meta.StorySeen(KeyAscend);
        /// <summary>광장 시계 바늘을 엔딩이 움직이는 동안 (Ch4Quest 11:59 째깍임을 멈춤)</summary>
        public static bool ClockOverride;

        struct Isle { public string isle, spot, npc, bridge, speaker, obj, sfx; public string[] lines; public bool collapse; public int hour; }

        // 다리가 열린 반대 순서 (2026-11-26 사용자 확정 대사). 2026-11-26 사용자: 작별은 실제 마을 섬에서 ('자정의 시계판' 맵은 안 씀)
        static readonly Isle[] Isles =
        {
            new Isle { isle = "Isle_Gun", spot = "NpcSpot_GunShop", bridge = "Bridge_Gun", hour = 2, speaker = Sheriff, obj = "Kit/Ch3Quest/CoatPoster", sfx = "q3_projector", collapse = true,
                       lines = new[] { Sheriff + "::이제야 생각났어. 이 외투, 네 거였지.", Sheriff + "::...잘 어울렸어." } },
            new Isle { isle = "Isle_Ammo", spot = "NpcSpot_AmmoWorkshop", bridge = "Bridge_Ammo", hour = 4, speaker = Smith, obj = "Kit/Ch3Quest/HourglassBench", sfx = "e5b_hourglass", collapse = true,
                       lines = new[] { Smith + "::모래는 다 떨어졌군.", Smith + "::이번엔 뒤집지 마라." } },
            new Isle { isle = "Isle_Wardrobe", spot = "NpcSpot_Wardrobe", bridge = "Bridge_Wardrobe", hour = 8, speaker = Tailor, obj = "Kit/Ch3Quest/SewingWindow", sfx = "q3_sewing", collapse = true,
                       lines = new[] { "실타래 재봉사#다정::마지막 단추까지 달았어.", "실타래 재봉사#쓸쓸::...이제 입고 가." } },
            new Isle { isle = "Isle_Center", spot = "NpcSpot_MemoryRoom", bridge = null, hour = 10, speaker = Cuckoo, obj = "Kit/Ch3Quest/CuckooBench", sfx = "q3_cuckoo", collapse = false,
                       lines = new[] { Cuckoo + "::노래, 기억났어.", Cuckoo + "::...한 번만 더 울어 줄게." } },
            new Isle { isle = "Isle_Gate", spot = "NpcSpot_Gatekeeper", bridge = "Bridge_Gate", hour = 12, speaker = Keeper, obj = "Kit/Story/W_GrandClock", sfx = "e4_bell", collapse = true,
                       lines = new[] { "괘종시계 문지기#엄숙::문은 이제 하나뿐이야.", "괘종시계 문지기#쓸쓸::그건 내가 지키는 문이 아니야." } },
        };

        /// <summary>마을 씬에서 이름으로 (2026-11-26 사용자: 작별은 실제 마을 섬에서 — '자정의 시계판' 맵은 안 씀)</summary>
        static Transform Find(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (t.name == name && t.gameObject.scene.IsValid()) return t;
            return null;
        }

        static Transform FindUnder(Transform root, string name)
        {
            if (!root) return null;
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }

        static IEnumerator Talk(GameUI ui, string speaker, params string[] lines)
        {
            bool d = false;
            ui.Talk(speaker, lines, () => d = true);
            while (!d) yield return null;
        }

        static void Snd(string key, float vol) { if (Sfx.Has(key)) Sfx.Play(key, vol, 0f); }

        // ================= 엔딩 본편 (VillageStory "ch5" 비트에서) =================
        public static IEnumerator Run(GameUI ui)
        {
            var p = PlayerController.Instance;
            if (!p || !ui) yield break;
            // 내면 세계 맵 '멈춘 1초' 먼저 (2026-11-26 사용자: 죽음을 받아들이는 과정을 보여 준 뒤 마을 작별)
            if (StoryInner.Pending) yield return StoryInner.Run(ui);
            var runner = new GameObject("Ch5Ending").AddComponent<EndingRunner>();
            Music.Play("bgm_story_5");
            var ov = StoryOverlay.I;
            p.InputLocked = true;
            yield return Talk(ui, Cat, Cat + "#의문::...마을이... 녹고 있어.");

            // 마을이 금빛으로 녹아 흩어지기 시작
            Snd("e5_rift_shatter", 0.6f);
            for (float t = 0; t < 1.8f; t += Time.deltaTime)
            {
                CameraFollow.Shake(0.12f);
                if (Random.value < 0.5f) FxBurst.Spawn(p.transform.position + new Vector3(Random.Range(-9f, 9f), Random.Range(0.3f, 4f), Random.Range(-6f, 9f)), Random.Range(1f, 2f), FxBurst.Kind.Gold, new Color(1f, 0.82f, 0.55f));
                yield return null;
            }
            p.InputLocked = false;

            for (int i = 0; i < Isles.Length; i++)
            {
                var def = Isles[i];
                var spot = Find(def.spot);
                var isle = Find(def.isle);
                if (!spot) continue;
                GuideArrow.Clear();
                GuideArrow.Show(spot, def.speaker, keep: true);
                ui.Toast(i == 0 ? "모두에게 작별인사를 전하세요." : "다음 섬으로");   // 2026-11-26 사용자 문구
                while (Effects.Flat(p.transform.position - spot.position).magnitude > TalkR) yield return null;
                GuideArrow.Clear();
                p.InputLocked = true;
                CameraFollow.FocusOn(spot.position + Vector3.back * 1.5f, 0.8f);
                yield return Talk(ui, def.speaker, def.lines);
                yield return ToObject(spot, def);
                runner.Dawn((i + 1f) / (Isles.Length + 1f));
                CameraFollow.FocusOn(null, 0f);
                p.InputLocked = false;
                if (def.collapse && isle) runner.StartCoroutine(CollapseWhenLeft(isle, Find(def.bridge)));
            }

            // 광장: 거울 속 고양이
            var mirror = MirrorSpot();
            GuideArrow.Show(mirror, "광장으로", keep: true);
            ui.Toast("광장으로");
            while (Effects.Flat(p.transform.position - mirror.position).magnitude > TalkR) yield return null;
            GuideArrow.Clear();
            p.InputLocked = true;
            CameraFollow.FocusOn(mirror.position + Vector3.back * 1.2f, 0.7f);
            const string MC = Ch3Quest.MirrorCat;
            // 2026-11-26 사용자 확정: 내면 맵에서 이미 받아들였으니 광장은 짧게
            yield return Talk(ui, MC, MC + "::이제 후회는 없는거지?", Cat + "#의문::...응.", MC + "::그럼, 이제 내려놔.");

            // [E 총 내려놓기]
            var label = Interactable.WorldLabel(p.transform, "E  총 내려놓기", new Vector3(0, 3.2f, 0), 0.06f, UIKit.Gold);
            ui.Toast("E — 총 내려놓기");
            yield return null;
            float asked = Time.unscaledTime;
            while (!(Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) && !(Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame)
                   && !(GameUI.DevAutoAdvance && Time.unscaledTime - asked > 2f)) yield return null;   // 개발용 자동 넘김 (연출 녹화 · 시험)
            if (label) Object.Destroy(label.gameObject);
            yield return LayGuns(p);
            yield return new WaitForSeconds(1.0f);

            // 거울 속 고양이가 웃으며 먼저 바스러짐
            yield return Talk(ui, MC, MC + "::...이제 내 몫은 끝났어.", MC + "::잘 가.");
            yield return Crumble(mirror);
            yield return new WaitForSeconds(0.8f);

            // 시간의 주인이 광장 가장자리에서 낫을 내림
            var rAt = Find("ReaperAt");
            var reaper = SpawnReaper(rAt ? rAt.position : p.transform.position + new Vector3(6.5f, 0, -5.5f), p.transform.position);
            CameraFollow.FocusOn(Vector3.Lerp(p.transform.position, reaper ? reaper.transform.position : p.transform.position, 0.5f), 0.75f);
            if (reaper) yield return runner.Rise(reaper.transform);
            yield return new WaitForSeconds(0.6f);
            if (reaper) yield return runner.Bow(reaper.transform);
            yield return Talk(ui, Cat, Cat + "#의문::...기다려 준 건가.");

            // 시계 12:00 → 종 → 하얗게
            var clock = Find("PlazaClock");
            if (clock) CameraFollow.FocusOn(clock.position + Vector3.up * 2f, 0.8f);
            ClockOverride = true;
            yield return runner.ClockToTwelve(clock);
            Music.Duck(1f, 0.4f);
            Snd("e5b_bell", 1f); Snd("e4_bell", 0.8f);
            CameraFollow.Shake(0.25f);
            runner.Dawn(1f);
            yield return new WaitForSeconds(1.2f);
            if (ov) yield return ov.FadeWhite(0, 1, 2.2f);
            Snd("e5b_bell", 0.6f);
            yield return new WaitForSeconds(1.0f);

            // 병실 → 그림책 '끝'
            if (ov)
            {
                Music.Duck(1f, 1f);
                ov.SetWhite(0);
                yield return ov.PlayDream(7, "", new string[0], authored: DreamCuts.For(7));
                ov.SetBlack(1);
                ov.SetCaption("끝");
                yield return new WaitForSecondsRealtime(2.8f);
                ov.SetCaption("");
            }
            Meta.MarkStory("ch5");
            Meta.MarkStory(KeyDone);
            ClockOverride = false;

            // 크레딧 → 복원된 마을에서 깨어남
            yield return Credits();
            if (runner) Object.Destroy(runner.gameObject);
            if (ov) ov.RevealAfterLoad(0.8f, 1.6f);
            MiniMapView.Suppressed = false;
            StoryRun.PendingWake = null;
            RunManager.ReturnToVillage();
            while (true) yield return null;                                     // 씬이 바뀔 때까지 (VillageStory가 다음 비트로 넘어가지 않게)
        }

        static Transform MirrorSpot()
        {
            var t = Find("MirrorCatSpot");
            if (!t) { t = new GameObject("MirrorCatSpot").transform; t.position = new Vector3(-6f, 0, -8.4f); }
            t.gameObject.SetActive(true);
            if (!t.Find("MirrorCatFigure"))
            {
                var p = PlayerController.Instance;
                if (p && p.model)
                {
                    var fig = Object.Instantiate(p.model.gameObject, t);
                    fig.name = "MirrorCatFigure";
                    foreach (var mb in fig.GetComponentsInChildren<MonoBehaviour>(true)) Object.Destroy(mb);
                    foreach (var c in fig.GetComponentsInChildren<Collider>(true)) Object.Destroy(c);
                    fig.transform.localPosition = Vector3.zero;
                    fig.transform.rotation = Quaternion.Euler(0, 180f, 0);
                    var s = fig.transform.localScale; fig.transform.localScale = new Vector3(-Mathf.Abs(s.x), s.y, s.z);
                    MirrorGhost.Apply(fig, rings: false);
                }
            }
            return t;
        }

        /// <summary>NPC가 빛 속에서 원래 물건으로 돌아감</summary>
        static IEnumerator ToObject(Transform spot, Isle def)
        {
            var at = spot.position;
            Snd(def.sfx, 0.8f);
            FxBurst.Spawn(at + Vector3.up * 1.2f, 2.2f, FxBurst.Kind.Gold, Gold);
            var npcs = new List<GameObject>();
            foreach (Transform c in spot) if (c.name.StartsWith("Npc_")) npcs.Add(c.gameObject);
            for (float t = 0; t < 1f; t += Time.deltaTime / 0.9f)
            {
                foreach (var n in npcs) if (n) n.transform.localScale = Vector3.one * Mathf.Lerp(1f, 0.05f, t * t);
                yield return null;
            }
            foreach (var n in npcs) if (n) n.SetActive(false);
            var pf = Resources.Load<GameObject>(def.obj);
            if (pf)
            {
                var o = Object.Instantiate(pf, at, Quaternion.Euler(0, 180f, 0));
                o.transform.SetParent(spot, true);                                   // 섬이 무너질 때 같이 떨어지게
                foreach (var c in o.GetComponentsInChildren<Collider>()) Object.Destroy(c);
                var full = o.transform.localScale * (def.obj.Contains("GrandClock") ? 0.8f : 1f);
                for (float t = 0; t < 1f; t += Time.deltaTime / 0.7f) { o.transform.localScale = full * Mathf.SmoothStep(0.1f, 1f, t); yield return null; }
                o.transform.localScale = full;
            }
            FxBurst.Spawn(at + Vector3.up * 0.8f, 1.6f, FxBurst.Kind.Gold, Color.white);
            yield return new WaitForSeconds(0.9f);
        }

        /// <summary>그 섬을 떠나면(가운데에서 멀어지면) 섬과 다리가 흔들리다 허공으로 무너짐</summary>
        static IEnumerator CollapseWhenLeft(Transform isle, Transform bridge, Transform numeral = null)
        {
            var p = PlayerController.Instance;
            // 다리 가운데 쪽 끝(다리는 가운데 섬 → 이 섬 방향, 길이 = Deck 판정)까지 돌아와야 무너짐 — 다리 위에 있을 때 무너지면 허공으로 떨어짐
            Vector3 nearEnd = isle.position; float leave = LeaveR;
            if (bridge)
            {
                float half = 0f; var deck = bridge.Find("Deck");
                if (deck && deck.TryGetComponent<BoxCollider>(out var bc)) half = bc.size.z * 0.5f;
                nearEnd = bridge.position - bridge.forward * half;
                leave = Mathf.Max(LeaveR, Effects.Flat(nearEnd - isle.position).magnitude + 1.5f);
            }
            while (p && Effects.Flat(p.transform.position - isle.position).magnitude < leave) yield return null;
            var nHome = Vector3.zero;
            if (numeral)                                                         // 시계판 그 숫자 돌도 같이 떨어짐 (떠다니기 멈춤)
            {
                var df = numeral.GetComponent<DreamFloat>(); if (df) df.enabled = false;
                nHome = numeral.position;
            }
            // 다리 입구를 막음 (무너지는 쪽으로 다시 못 가게)
            if (bridge)
            {
                var wall = new GameObject("EndingBlock").AddComponent<BoxCollider>();   // 다리 입구(가운데 쪽 끝)를 막음
                wall.transform.SetPositionAndRotation(nearEnd + bridge.forward * 0.6f + Vector3.up * 1.5f, bridge.rotation);
                wall.size = new Vector3(4.5f, 3f, 1.2f);
            }
            Snd("e5_rift_shatter", 0.7f);
            var home = isle.position;
            for (float t = 0; t < 1.6f; t += Time.deltaTime)
            {
                isle.position = home + new Vector3(Random.Range(-0.12f, 0.12f), 0, Random.Range(-0.12f, 0.12f));
                if (Random.value < 0.25f) FxBurst.Spawn(home + new Vector3(Random.Range(-9f, 9f), 0.5f, Random.Range(-9f, 9f)), 1.6f, FxBurst.Kind.Gold, new Color(1f, 0.8f, 0.55f));
                yield return null;
            }
            var bHome = bridge ? bridge.position : Vector3.zero;
            for (float t = 0; t < 1f; t += Time.deltaTime / 3.2f)
            {
                float k = t * t;
                isle.position = home + Vector3.down * 40f * k;
                if (bridge) bridge.position = bHome + Vector3.down * 40f * Mathf.Clamp01(k * 1.3f);
                if (numeral) numeral.position = nHome + Vector3.down * 40f * Mathf.Clamp01(k * 0.9f);
                if (Random.value < 0.2f) FxBurst.Spawn(home + new Vector3(Random.Range(-10f, 10f), 1f, Random.Range(-10f, 10f)), 2f, FxBurst.Kind.Gold, new Color(1f, 0.85f, 0.6f));
                yield return null;
            }
            isle.gameObject.SetActive(false);
            if (bridge) bridge.gameObject.SetActive(false);
            if (numeral) numeral.gameObject.SetActive(false);
        }

        static IEnumerator LayGuns(PlayerController p)
        {
            Snd("reload", 0.7f);
            var poser = p.poser;
            var guns = new List<Transform>();
            if (poser) { if (poser.gunR) guns.Add(poser.gunR); if (poser.gunL) guns.Add(poser.gunL); }
            var fwd = p.model ? p.model.forward : Vector3.forward;
            var right = Vector3.Cross(Vector3.up, fwd);
            for (int i = 0; i < guns.Count; i++)
            {
                var g = guns[i];
                var copy = Object.Instantiate(g.gameObject);
                foreach (var mb in copy.GetComponentsInChildren<MonoBehaviour>(true)) Object.Destroy(mb);
                copy.transform.localScale = g.lossyScale;
                var to = p.transform.position + fwd * 0.9f + right * (i == 0 ? 0.3f : -0.3f) + Vector3.up * 0.06f;
                var from = g.position;
                var rFrom = g.rotation;
                var rTo = Quaternion.LookRotation(fwd + right * (i == 0 ? 0.25f : -0.25f), Vector3.up) * Quaternion.Euler(0, 0, 90f);
                foreach (var r in g.GetComponentsInChildren<Renderer>()) r.enabled = false;
                p.StartCoroutine(Place(copy.transform, from, to, rFrom, rTo, 0.9f + i * 0.25f));
            }
            yield return new WaitForSeconds(1.4f);
            Snd("reload_end", 0.6f);
            FxBurst.Spawn(p.transform.position + fwd * 0.9f + Vector3.up * 0.2f, 1.2f, FxBurst.Kind.Gold, Gold);
        }

        static IEnumerator Place(Transform t, Vector3 a, Vector3 b, Quaternion ra, Quaternion rb, float dur)
        {
            for (float u = 0; u < 1f && t; u += Time.deltaTime / dur)
            {
                float k = Mathf.SmoothStep(0, 1, u);
                t.position = Vector3.Lerp(a, b, k); t.rotation = Quaternion.Slerp(ra, rb, k);
                yield return null;
            }
            if (t) { t.position = b; t.rotation = rb; }
        }

        static IEnumerator Crumble(Transform mirror)
        {
            var fig = mirror ? mirror.Find("MirrorCatFigure") : null;
            Snd("e_glass", 0.6f);
            if (fig)
            {
                var s0 = fig.localScale;
                for (float t = 0; t < 1f; t += Time.deltaTime / 1.4f)
                {
                    fig.localScale = new Vector3(s0.x * (1f - t * 0.3f), s0.y * (1f - t), s0.z * (1f - t * 0.3f));
                    if (Random.value < 0.4f) FxBurst.Spawn(fig.position + Vector3.up * Random.Range(0.3f, 2f), 0.8f, FxBurst.Kind.Gold, new Color(0.8f, 0.9f, 1f));
                    yield return null;
                }
                fig.gameObject.SetActive(false);
            }
            FxBurst.Spawn((mirror ? mirror.position : Vector3.zero) + Vector3.up, 2.4f, FxBurst.Kind.Gold, Color.white);
        }

        static GameObject SpawnReaper(Vector3 at, Vector3 look)
        {
            at.y = 0;
            var pf = Resources.Load<GameObject>("Kit/Story/H_ReaperBody");
            if (!pf) return null;
            var d = Effects.Flat(look - at);
            var go = Object.Instantiate(pf, at, d.sqrMagnitude > 0.01f ? Quaternion.LookRotation(-d.normalized) : Quaternion.identity);   // 정면 = −Z
            foreach (var c in go.GetComponentsInChildren<Collider>()) Object.Destroy(c);
            var head = Resources.Load<GameObject>("Kit/Story/H_ReaperHead");
            if (head)
            {
                var pivot = new GameObject("HeadPivot").transform;
                pivot.SetParent(go.transform, false);
                var mesh = go.transform.Find("Mesh");
                var off = mesh ? mesh.localPosition : Vector3.zero;
                pivot.localPosition = new Vector3(-0.0773f + off.x, 2.0f, -0.0027f + off.z);
                var h = Object.Instantiate(head, pivot);
                h.transform.localPosition = Vector3.zero; h.transform.localRotation = Quaternion.identity;
                foreach (var c in h.GetComponentsInChildren<Collider>()) Object.Destroy(c);
            }
            go.transform.localScale *= 1.25f;
            return go;
        }

        // ================= 크레딧 =================
        static IEnumerator Credits()
        {
            var canvasGo = new GameObject("EndingCredits");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 5000;
            var sc = canvasGo.AddComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080);
            var bg = new GameObject("Bg").AddComponent<Image>(); bg.transform.SetParent(canvasGo.transform, false); bg.color = Color.black;
            var bgr = bg.rectTransform; bgr.anchorMin = Vector2.zero; bgr.anchorMax = Vector2.one; bgr.offsetMin = bgr.offsetMax = Vector2.zero;
            var roll = new GameObject("Roll").AddComponent<RectTransform>(); roll.SetParent(canvasGo.transform, false);
            roll.anchorMin = roll.anchorMax = new Vector2(0.5f, 0f); roll.sizeDelta = new Vector2(1200, 1400); roll.pivot = new Vector2(0.5f, 1f);
            var lines = new (string text, int size, bool title, Color c)[]
            {
                ("11:59", 120, true, Gold), ("", 60, false, Color.white),
                ("기획 · 개발", 30, false, new Color(0.7f, 0.7f, 0.75f)), ("정승운", 46, true, Color.white), ("", 60, false, Color.white),
                ("도구", 30, false, new Color(0.7f, 0.7f, 0.75f)), ("Unity · Blender", 40, true, Color.white),
            };
            float y = 0;
            foreach (var (text, size, title, c) in lines)
            {
                var t = UIKit.Label("L", roll, new Vector2(0.5f, 1f), new Vector2(0, -y), new Vector2(1200, size * 1.6f), size, TextAnchor.MiddleCenter, title, c);
                t.text = text;
                y += size * 1.7f;
            }
            Music.Story("bgm_ending", 1.5f);
            float total = 1080f + y + 200f, dur = 26f;
            for (float t = 0; t < dur; t += Time.unscaledDeltaTime)
            {
                roll.anchoredPosition = new Vector2(0, Mathf.Lerp(0, total, t / dur));
                if (t > 3f && (Keyboard.current?.escapeKey.wasPressedThisFrame ?? false)) break;
                yield return null;
            }
            Music.StoryEnd(2f);
            yield return new WaitForSecondsRealtime(1.5f);
            Object.Destroy(canvasGo);
        }

        // ================= 엔딩 뒤 처음 깨어남: 승천 모드 입구 =================
        public static IEnumerator Ascend(GameUI ui)
        {
            var spot = Find("NpcSpot_Gatekeeper");
            if (spot) CameraFollow.FocusOn(spot.position + Vector3.back * 1.5f, 0.8f);
            yield return Talk(ui, Keeper, Keeper + "::...또 왔군.", "괘종시계 문지기#의미심장::한 번 더 들여다볼 텐가?");
            CameraFollow.FocusOn(null, 0f);
            Meta.MarkStory(KeyAscend);
            ui.Toast("<color=#FFD54A>승천 모드가 열렸다</color> — 시간의 문 (준비 중)");
            yield return new WaitForSecondsRealtime(0.6f);
        }

        /// <summary>엔딩 동안 계속 돌아가는 것: 녹아 오르는 금빛 먼지 · 새벽빛 · 바늘 · 사신</summary>
        class EndingRunner : MonoBehaviour
        {
            Light sun; Color c0; float i0; float dawn, dawnNow; float nextDust;
            void Start()
            {
                foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None)) if (l.type == LightType.Directional && l.enabled) { sun = l; break; }
                if (sun) { c0 = sun.color; i0 = sun.intensity; }
            }
            public void Dawn(float k) => dawn = Mathf.Max(dawn, k);
            void Update()
            {
                dawnNow = Mathf.MoveTowards(dawnNow, dawn, Time.deltaTime * 0.25f);
                if (sun) { sun.color = Color.Lerp(c0, new Color(1f, 0.88f, 0.74f), dawnNow * 0.7f); sun.intensity = Mathf.Lerp(i0, i0 * 1.15f, dawnNow); }
                var p = PlayerController.Instance;
                if (p && Time.time >= nextDust)
                {
                    nextDust = Time.time + 0.35f;
                    var at = p.transform.position + new Vector3(Random.Range(-18f, 18f), Random.Range(0.5f, 5f), Random.Range(-12f, 14f));
                    FxBurst.Spawn(at, Random.Range(0.6f, 1.3f), FxBurst.Kind.Gold, new Color(1f, 0.85f, 0.6f));
                }
            }
            public IEnumerator Rise(Transform t)
            {
                var s = t.localScale;
                FxBurst.Spawn(t.position + Vector3.up * 1.5f, 3f, FxBurst.Kind.Spark, new Color(0.15f, 0.1f, 0.2f));
                for (float u = 0; u < 1f; u += Time.deltaTime / 1.4f) { t.localScale = s * Mathf.SmoothStep(0.05f, 1f, u); yield return null; }
                t.localScale = s;
            }
            public IEnumerator Bow(Transform t)
            {
                var r0 = t.rotation; var r1 = r0 * Quaternion.Euler(14f, 0, 0);       // 낫을 내리며 고개 숙임
                Snd("e5b_reap_wind", 0.3f);
                for (float u = 0; u < 1f; u += Time.deltaTime / 1.6f) { t.rotation = Quaternion.Slerp(r0, r1, Mathf.SmoothStep(0, 1, u)); yield return null; }
            }
            public IEnumerator ClockToTwelve(Transform clock)
            {
                var mh = FindUnder(clock, "MinuteHand");                       // 마을 광장 시계탑
                var hh = FindUnder(clock, "HourHand");
                if (mh) mh.gameObject.SetActive(true);
                if (hh) hh.gameObject.SetActive(true);
                yield return new WaitForSeconds(0.8f);
                var m0 = mh ? mh.localRotation : Quaternion.identity; var h0 = hh ? hh.localRotation : Quaternion.identity;
                for (float u = 0; u < 1f; u += Time.deltaTime / 1.2f)
                {
                    float k = Mathf.SmoothStep(0, 1, u);
                    if (mh) mh.localRotation = Quaternion.Slerp(m0, Quaternion.identity, k);
                    if (hh) hh.localRotation = Quaternion.Slerp(h0, Quaternion.identity, k);
                    yield return null;
                }
                Snd("e4_clank", 0.8f);
            }
        }
    }
}
