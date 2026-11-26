// 4장 클리어 뒤 마을 퀘스트 '멈춘 마을' (2026-11-25 사용자 확정 · 대사 확정, 명세 Art/tools/specs/ch4_story_spec.md "3. 마을 퀘스트", 해금 없음)
//   4장 클리어 = 꿈3 '11:59의 기원' → 마을에서 깨어나면 마을 전체가 멈춰 있음 (NPC · 연기 · 반딧불 · 떠 있는 것 정지, 화면 약간 회색)
//   광장의 거울 속 고양이(MirrorCatNpc, 3장 퀘스트 뒤 남음)만 움직임 → "이번엔 섬마다 시계가 멈췄어."
//   → 섬 넷(여관 뻐꾸기 · 공방 대장장이 · 총포상 보안관 · 의상실 재봉사)에 멈춘 시계 + 숫자 톱니 1 · 2 · 2 · 3개
//     톱니는 자기 숫자 시각 탄으로 쏴야 돌아감 (다른 시각은 튕김) — 4장 쌍둥이 기믹 '멈춘 톱니 돌리기'(Ch4Twins.Gears)와 같은 판정:
//     발사 시각 = 실린더 칸 번호(1 → 12, 되감는 실린더면 12 → 1), 12발이면 장전. 마을에선 전투가 꺼져 있어 Ch3Quest처럼 따로 쏨
//   → 섬의 톱니가 다 돌면 그 섬 시간이 다시 흐르고(그 섬 안의 애니메이션 · 파티클 · 떠다님이 다시 움직임) 색이 한 단계 돌아옴 → NPC 두 마디
//   → 넷 다 되면 "광장으로": 광장 시계는 11:59에서 째깍거리기만 하고 넘어가지 않음 · 쏴도 튕김 → 거울 속 고양이와 마지막 대화
//   → 마을이 다시 움직임 · 퀘스트 끝 (q4_done)
// 씬 배치(시계 · 톱니 · 광장 시계 바늘)는 Editor/Ch4QuestBuilder.cs가 Village 씬 Village/Ch4Quest 아래에 저장
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CatTang
{
    public class Ch4Quest : MonoBehaviour
    {
        public const string KeyIntro = "q4_intro", KeyDone = "q4_done";
        const string MirrorCat = Ch3Quest.MirrorCat;
        const string Cuckoo = "태엽 풀린 뻐꾸기", Smith = "모래시계 대장장이", Tailor = "실타래 재봉사", Sheriff = "빈 외투 보안관";
        const float HitR = 1.45f, StageR = 16f, PlazaR = 9f, PlazaHitR = 3.6f, FrozenSat = -55f, BaseSat = 16f;   // BaseSat = 새벽 볼륨(DawnVolume) 채도
        static readonly Color Gold = new(1f, 0.85f, 0.4f);

        public struct Def
        {
            public string id, isle, spot, speaker;
            public float radius;            // 0 = 섬 전체가 그 시계 몫, > 0 = 시계에서 이 반경 안만 (가운데 섬 여관: 광장은 마지막까지 멈춰 있게)
            public int[] hours;             // 톱니 숫자 (섬 순서대로 1 · 2 · 2 · 3개, 1~12 중 섬마다 다르게)
            public string[] lines;
        }

        // 섬 넷 (2026-11-25 사용자 확정 대사). 톱니 숫자: 공방은 오름차순, 총포상은 내림차순(10 → 5, 한 바퀴 돌려 장전) — 스토리 맵 4 ① 순서 연습과 같은 요령
        public static readonly Def[] Defs =
        {
            new Def { id = "inn", isle = "Isle_Center", spot = "NpcSpot_MemoryRoom", speaker = Cuckoo, radius = 6.5f, hours = new[] { 4 },
                      lines = new[] { "분명 누군가가 이 시계를 붙잡고 있었는데...." } },
            new Def { id = "ammo", isle = "Isle_Ammo", spot = "NpcSpot_AmmoWorkshop", speaker = Smith, hours = new[] { 2, 7 },
                      lines = new[] { "모래가 다시 떨어지는군.", ".....이 모래엔 무슨 비밀이 있는거지..??" } },
            new Def { id = "gun", isle = "Isle_Gun", spot = "NpcSpot_GunShop", speaker = Sheriff, hours = new[] { 10, 5 },
                      lines = new[] { "시간이 멈추면 영웅도 늙지 않지.", "그게 좋은 건지는 모르겠지만." } },
            new Def { id = "ward", isle = "Isle_Wardrobe", spot = "NpcSpot_Wardrobe", speaker = Tailor, hours = new[] { 3, 8, 12 },
                      lines = new[] { "멈춘 바늘로는 아무것도 지을 수 없어.", "바느질을 할때는 시작만큼이나 끝맺음도 중요한 법이야." } },
        };

        public static bool IsleDone(string id) => Meta.StorySeen("q4_i_" + id);
        public static bool GearDone(string id, int k) => Meta.StorySeen($"q4_g_{id}_{k}");
        public static int DoneCount { get { int n = 0; foreach (var d in Defs) if (IsleDone(d.id)) n++; return n; } }
        /// <summary>4장을 넘었고 3장 퀘스트(거울 마을)까지 마침 · 아직 마을이 안 움직임</summary>
        public static bool Pending => Meta.ChaptersCleared >= 4 && Ch3Quest.Done && !Meta.StorySeen(KeyDone);
        public static bool Done => Meta.StorySeen(KeyDone);
        /// <summary>규칙을 들은 뒤 · 끝나기 전 (거울 속 고양이 E가 힌트로 바뀜)</summary>
        public static bool InProgress => Pending && Meta.StorySeen(KeyIntro);
        public static Ch4Quest I { get; private set; }
        public static bool Busy;
        static GameUI uiCache;
        static GameUI UIRef => uiCache ? uiCache : (uiCache = FindFirstObjectByType<GameUI>());

        [System.Serializable]
        public class Station
        {
            public string id;
            public Transform clock;          // 멈춘 시계 (화살표 · 카메라)
            public Transform[] gears;        // 톱니 (돌아가는 축 = 이 Transform의 Y), 순서 = Def.hours
        }

        public Station[] stations;
        [Tooltip("광장 시계탑 발밑 (쏘면 튕김 · 마지막 대화)")] public Transform plazaClock;
        [Tooltip("광장 시계 바늘 축 (앞 = 문자판 바깥, 위 = 12시)")] public Transform minuteHand, hourHand;

        enum Stage { Off, Intro, Islands, Plaza, Finale, Finished }
        Stage stage;
        float nextShot;
        int hourNext = -1, shotsThisCycle;
        bool reloading;
        float reloadEnd;
        BigLabel hourBadge;
        readonly List<BigLabel> gearLabels = new();
        readonly Dictionary<Transform, BigLabel> labelOf = new();
        readonly HashSet<Transform> spinning = new();
        float plazaLinger;

        // ---------- 멈춤 ----------
        class Frozen { public Animator a; public float speed; public ParticleSystem ps; public Behaviour b; public int group; }
        readonly List<Frozen> frozen = new();
        Transform[] isleT;
        Volume vol;
        VolumeProfile prof;
        ColorAdjustments tone;
        float satNow, satTarget;
        bool ducked;

        void Awake() => I = this;

        void OnDestroy()
        {
            if (I == this) I = null;
            Busy = false;
            if (ducked) Music.Duck(1f, 0.5f);
            if (prof) Destroy(prof);
            foreach (var l in gearLabels) if (l) Destroy(l.gameObject);
            if (hourBadge) Destroy(hourBadge.gameObject);
        }

        IEnumerator Start()
        {
            I = this;
            stations ??= new Station[0];
            bool on = Pending;
            // 섬 시계 · 톱니는 퀘스트 동안만 (끝난 뒤 다음 방문부터는 치움) · 광장 11:59 바늘은 4장 퀘스트가 열린 뒤 계속 째깍거림
            foreach (var s in stations)
            {
                if (s.clock) s.clock.gameObject.SetActive(on);
                if (s.gears != null) foreach (var g in s.gears) if (g) g.gameObject.SetActive(on);
            }
            bool hands = on || Done;
            if (minuteHand) minuteHand.gameObject.SetActive(hands);
            if (hourHand) hourHand.gameObject.SetActive(hands);
            if (!on) { stage = Done ? Stage.Finished : Stage.Off; yield break; }

            yield return null;                                   // ClockQuest · Ch3Quest가 자리 잡은 뒤에 멈춤 (모래 파티클을 다시 켜지 않게)
            FreezeAll();
            MakeTone();
            satNow = satTarget = SatFor(DoneCount);
            Music.Duck(0.45f, 1.5f); ducked = true;

            for (int i = 0; i < stations.Length; i++)
            {
                var s = stations[i];
                var def = DefOf(s.id);
                if (IsleDone(s.id)) Resume(i);
                if (s.gears == null) continue;
                for (int k = 0; k < s.gears.Length && k < def.hours.Length; k++)
                {
                    var g = s.gears[k];
                    if (!g) continue;
                    if (GearDone(s.id, k)) spinning.Add(g);
                    else
                    {
                        var lb = BigLabel.Show(g, new Vector3(0, 2.2f, 0), def.hours[k].ToString(), Gold, 0.9f, BigLabel.Frame.Gear);
                        gearLabels.Add(lb); labelOf[g] = lb;
                    }
                }
            }
            if (!Meta.StorySeen(KeyIntro) && Meta.StorySeen("ch4")) Meta.MarkStory(KeyIntro);   // 대화 도중 꺼졌으면 규칙은 들은 것으로
            stage = !Meta.StorySeen(KeyIntro) ? Stage.Intro : DoneCount >= Defs.Length ? Stage.Plaza : Stage.Islands;
            if (stage != Stage.Intro) StartCoroutine(ArrowsLater());
        }

        static Def DefOf(string id) { foreach (var d in Defs) if (d.id == id) return d; return Defs[0]; }
        static float SatFor(int done) => FrozenSat * (1f - done / (Defs.Length + 1f));   // 섬 넷 + 광장 = 다섯 단계로 색이 돌아옴

        void FreezeAll()
        {
            frozen.Clear();
            var root = transform.root;
            isleT = new Transform[stations.Length];
            for (int i = 0; i < stations.Length; i++) { var g = GameObject.Find(DefOf(stations[i].id).isle); isleT[i] = g ? g.transform : null; }
            var p = PlayerController.Instance;
            var cat = Ch3Quest.I ? Ch3Quest.I.puddle : null;
            bool Skip(Transform t) => t.IsChildOf(transform) || (cat && t.IsChildOf(cat)) || (p && t.IsChildOf(p.transform));

            foreach (var a in root.GetComponentsInChildren<Animator>(true))
            {
                if (Skip(a.transform) || a.speed <= 0f) continue;
                frozen.Add(new Frozen { a = a, speed = a.speed, group = GroupOf(a.transform) });
                a.speed = 0f;
            }
            foreach (var ps in root.GetComponentsInChildren<ParticleSystem>(true))
            {
                if (Skip(ps.transform) || !ps.gameObject.activeInHierarchy || !ps.isPlaying) continue;
                frozen.Add(new Frozen { ps = ps, group = GroupOf(ps.transform) });
                ps.Pause(false);
            }
            var movers = new List<Behaviour>();
            movers.AddRange(root.GetComponentsInChildren<DreamFloat>(true));
            movers.AddRange(root.GetComponentsInChildren<CloudScroll>(true));
            movers.AddRange(root.GetComponentsInChildren<FireflyTrail>(true));
            foreach (var b in movers)
            {
                if (Skip(b.transform) || !b.enabled) continue;
                frozen.Add(new Frozen { b = b, group = GroupOf(b.transform) });
                b.enabled = false;
            }
        }

        /// <summary>그 물체가 어느 섬 시계 몫인지 (-1 = 광장 · 하늘 · 허공 → 마지막에 다시 움직임)</summary>
        int GroupOf(Transform t)
        {
            for (int i = 0; i < stations.Length; i++)
            {
                if (!isleT[i] || !t.IsChildOf(isleT[i])) continue;
                var def = DefOf(stations[i].id);
                if (def.radius > 0f && stations[i].clock && Effects.Flat(t.position - stations[i].clock.position).magnitude > def.radius) continue;
                return i;
            }
            return -1;
        }

        void Resume(int group)
        {
            foreach (var f in frozen)
            {
                if (group != int.MinValue && f.group != group) continue;
                if (f.a) f.a.speed = f.speed;
                if (f.ps) f.ps.Play(false);
                if (f.b) f.b.enabled = true;
            }
            frozen.RemoveAll(f => group == int.MinValue || f.group == group);
        }

        void MakeTone()
        {
            var cam = Camera.main;
            if (cam) { var data = cam.GetUniversalAdditionalCameraData(); if (data && !data.renderPostProcessing) data.renderPostProcessing = true; }
            var go = new GameObject("Ch4FrozenTone");
            go.transform.SetParent(transform, false);
            vol = go.AddComponent<Volume>();
            vol.isGlobal = true; vol.priority = 60; vol.weight = 1f;
            prof = ScriptableObject.CreateInstance<VolumeProfile>();
            tone = prof.Add<ColorAdjustments>(false);                             // 채도만 덮어씀 (새벽 볼륨의 노출 · 대비 · 색 필터는 그대로)
            tone.saturation.Override(BaseSat);
            vol.profile = prof;
        }

        IEnumerator ArrowsLater()
        {
            while (FindFirstObjectByType<VillageWake>() || FindFirstObjectByType<VillageStory>()) yield return null;
            ShowArrows();
        }

        void ShowArrows()
        {
            GuideArrow.Clear();
            if (stage == Stage.Plaza) { if (plazaClock) GuideArrow.Show(plazaClock, "광장 시계", 6.5f, 4f, keep: true); return; }
            foreach (var s in stations) if (!IsleDone(s.id) && s.clock) GuideArrow.Show(s.clock, "멈춘 시계", 5.2f, 4f, keep: true);
        }

        // ---------- 대화 ----------
        static IEnumerator Say(GameUI ui, string speaker, params string[] lines)
        {
            bool d = false;
            ui.Talk(speaker, lines, () => d = true);
            while (!d) yield return null;
            var p = PlayerController.Instance;
            if (p) p.InputLocked = true;   // 대화 창이 닫히면 풀리므로 연출 동안 다시 잠금
        }

        /// <summary>VillageStory "ch4" 뒤 (고양이 "...아무것도 안 움직여."): 거울 속 고양이가 규칙을 알려 줌 → 섬 시계에 화살표</summary>
        public static IEnumerator Intro(GameUI ui, System.Action<System.Action> setArrow)
        {
            var q = I;
            if (!q || !Pending) yield break;
            var p = PlayerController.Instance;
            if (p) p.InputLocked = true;
            var cat = Ch3Quest.I ? Ch3Quest.I.puddle : null;
            if (cat)
            {
                CameraFollow.FocusOn(cat.position + Vector3.back * 1.0f, 0.62f);
                yield return new WaitForSecondsRealtime(0.9f);
            }
            yield return Say(ui, MirrorCat, MirrorCat + "::이번엔 섬마다 시계가 멈췄어.", MirrorCat + "::시계에 적힌 시각대로 다시 감아 줘.");
            Meta.MarkStory(KeyIntro);
            q.stage = Stage.Islands;
            // 가까운 섬 시계 하나를 비춰 줌
            Station first = null; float best = 1e9f;
            foreach (var s in q.stations)
                if (!IsleDone(s.id) && s.clock) { float d = p ? Effects.Flat(s.clock.position - p.transform.position).magnitude : 0; if (d < best) { best = d; first = s; } }
            if (first != null)
            {
                CameraFollow.FocusOn(first.clock.position + Vector3.back * 1.5f, 0.8f);
                yield return new WaitForSecondsRealtime(1.5f);
            }
            CameraFollow.FocusOn(null, 0f);
            ui.Toast("<color=#FFD54A>섬마다 멈춘 시계를 찾아 톱니 숫자 시각 탄으로 쏘세요</color>");
            setArrow(() => q.ShowArrows());
        }

        /// <summary>퀘스트 동안 거울 속 고양이 E (MirrorCatNpc)</summary>
        public static IEnumerator CatHint(GameUI ui)
        {
            var q = I;
            var p = PlayerController.Instance;
            if (!q || !ui) yield break;
            Busy = true;
            if (p) p.InputLocked = true;
            var cat = Ch3Quest.I ? Ch3Quest.I.puddle : null;
            if (cat) CameraFollow.FocusOn(cat.position + Vector3.back * 1.0f, 0.7f);
            yield return Say(ui, MirrorCat, MirrorCat + "::시계에 적힌 시각대로 다시 감아 줘.");
            CameraFollow.FocusOn(null, 0f);
            if (p) p.InputLocked = false;
            Busy = false;
        }

        // ---------- 쏘기 ----------
        int FirstHour { get { var p = PlayerController.Instance; return p && p.rev.reversed ? 12 : 1; } }

        /// <summary>이번 발사 시각을 쓰고 다음 칸으로 (보스와 같은 실린더 순서, 12발 · 끝 칸이면 장전)</summary>
        int TakeHour()
        {
            var p = PlayerController.Instance;
            bool rev = p && p.rev.reversed;
            if (hourNext < 1 || hourNext > 12) hourNext = FirstHour;
            int h = hourNext;
            shotsThisCycle++;
            hourNext += rev ? -1 : 1;
            if (shotsThisCycle >= 12 || hourNext < 1 || hourNext > 12)
            {
                reloading = true; reloadEnd = Time.time + 0.9f;
                Sfx.Play("reload", 0.6f, 0.03f);
            }
            return h;
        }

        void Update()
        {
            TickPlazaClock();
            foreach (var g in spinning) if (g) g.Rotate(0, 90f * Time.deltaTime, 0, Space.World);
            if (stage == Stage.Off || stage == Stage.Finished) return;
            satNow = Mathf.MoveTowards(satNow, satTarget, 30f * Time.unscaledDeltaTime);
            if (tone) tone.saturation.Override(BaseSat + satNow);
            if (reloading && Time.time >= reloadEnd) { reloading = false; shotsThisCycle = 0; hourNext = FirstHour; Sfx.Play("reload_end", 0.6f, 0.03f); }

            var p = PlayerController.Instance;
            var ui = UIRef;
            bool near = p && (stage == Stage.Islands && NearStation(p.transform.position) != null || stage == Stage.Plaza && NearPlaza(p.transform.position, StageR));
            UpdateBadge(p, near && !Busy);
            // 톱니 숫자 배지는 그 톱니 가까이(섬 안)에서만 — 멀리 있으면 화면 가장자리에 붙어 어수선함
            if (p)
                foreach (var kv in labelOf)
                    if (kv.Key && kv.Value)
                    {
                        bool on = stage == Stage.Islands && Effects.Flat(kv.Key.position - p.transform.position).magnitude < StageR;
                        if (kv.Value.gameObject.activeSelf != on) kv.Value.gameObject.SetActive(on);
                    }
            if (Busy || !p || p.InputLocked || (ui && ui.ScreenOpen)) return;
            if (stage != Stage.Islands && stage != Stage.Plaza) return;

            // 광장 시계 앞에서 한참 서 있으면 (쏘지 않아도) 마지막 대화로
            if (stage == Stage.Plaza && NearPlaza(p.transform.position, PlazaR)) { plazaLinger += Time.deltaTime; if (plazaLinger > 8f) { StartCoroutine(Finale()); return; } }
            else plazaLinger = 0f;

            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame || Time.time < nextShot || reloading || !near) return;
            if (!GroundPoint(mouse.position.ReadValue(), out var at)) return;
            nextShot = Time.time + 0.28f;
            int hour = TakeHour();
            Shot(p, at + Vector3.up * 1.0f);
            if (hourBadge) hourBadge.text = "";

            // 광장 시계: 늘 튕김 → (섬 넷을 다 돌렸으면) 마지막 대화
            if (plazaClock && Effects.Flat(at - plazaClock.position).magnitude < PlazaHitR)
            {
                Bounce(plazaClock.position + Vector3.up * 3.6f + Vector3.back * 1.2f, 1.1f);
                if (stage == Stage.Plaza) StartCoroutine(Finale());
                return;
            }
            var st = NearStation(p.transform.position);
            if (st == null) return;
            int si = System.Array.IndexOf(stations, st);
            var def = DefOf(st.id);
            if (st.clock && Effects.Flat(at - st.clock.position).magnitude < 1.2f) { Bounce(st.clock.position + Vector3.up * 3f, 0.8f); return; }
            for (int k = 0; st.gears != null && k < st.gears.Length && k < def.hours.Length; k++)
            {
                var g = st.gears[k];
                if (!g || GearDone(st.id, k) || Effects.Flat(at - g.position).magnitude > HitR) continue;
                if (hour == def.hours[k]) TurnGear(si, k);
                else Bounce(g.position + Vector3.up, 0.8f);                       // 다른 시각 탄: 튕김 (패널티 없음)
                return;
            }
        }

        void UpdateBadge(PlayerController p, bool show)
        {
            if (!p) return;
            if (!hourBadge && show) hourBadge = BigLabel.Show(p.transform, new Vector3(0, 2.7f, 0), "", Gold, 0.85f);
            if (!hourBadge) return;
            if (hourNext < 1 || hourNext > 12) hourNext = FirstHour;
            hourBadge.text = show && !reloading && Time.time >= nextShot - 0.05f ? hourNext.ToString() : "";
        }

        Station NearStation(Vector3 from)
        {
            Station best = null; float bd = StageR;
            foreach (var s in stations)
            {
                if (IsleDone(s.id) || !s.clock || !s.clock.gameObject.activeInHierarchy) continue;
                float d = Effects.Flat(s.clock.position - from).magnitude;
                if (s.gears != null) foreach (var g in s.gears) if (g) d = Mathf.Min(d, Effects.Flat(g.position - from).magnitude);
                if (d < bd) { bd = d; best = s; }
            }
            return best;
        }

        bool NearPlaza(Vector3 from, float r) => plazaClock && Effects.Flat(plazaClock.position - from).magnitude < r;

        static void Bounce(Vector3 at, float size)
        {
            FxBurst.Spawn(at, size, FxBurst.Kind.Spark, Color.white);
            if (Sfx.Has("e4_tick")) Sfx.Play("e4_tick", 0.5f, 0.15f);
        }

        static bool GroundPoint(Vector2 screen, out Vector3 at)
        {
            at = default;
            var cam = Camera.main;
            if (!cam) return false;
            var ray = cam.ScreenPointToRay(screen);
            if (!new Plane(Vector3.up, Vector3.zero).Raycast(ray, out float d)) return false;
            at = ray.GetPoint(d);
            return true;
        }

        static void Shot(PlayerController p, Vector3 to)
        {
            var d = Effects.Flat(to - p.transform.position);
            if (p.model && d.sqrMagnitude > 0.01f) p.model.rotation = Quaternion.LookRotation(d);
            var muz = p.rightMuzzle ? p.rightMuzzle.position : p.transform.position + Vector3.up * 1.2f;
            Tracer.Spawn(muz, to, false);
            Sfx.Play("shot", 0.8f, 0.05f);
            if (d.sqrMagnitude > 0.01f) GunkataFx.Burst(muz, d.normalized, 0.6f, false);
            p.poser?.Recoil(true, 1.2f);
        }

        // ---------- 톱니 · 섬 되돌리기 ----------
        void TurnGear(int si, int k)
        {
            var st = stations[si];
            var g = st.gears[k];
            Meta.MarkStory($"q4_g_{st.id}_{k}");
            spinning.Add(g);
            if (labelOf.TryGetValue(g, out var lb) && lb) { Destroy(lb.gameObject); labelOf.Remove(g); }
            Sfx.Play("e4_clank", 0.8f, 0.05f, 2);
            FxBurst.Spawn(g.position + Vector3.up, 1.4f, FxBurst.Kind.Spark, Gold);
            CameraFollow.Shake(0.08f);
            var def = DefOf(st.id);
            for (int j = 0; j < def.hours.Length; j++) if (!GearDone(st.id, j)) return;
            Busy = true;
            StartCoroutine(IsleResume(si));
        }

        IEnumerator IsleResume(int si)
        {
            var st = stations[si];
            var def = DefOf(st.id);
            var ui = UIRef;
            var p = PlayerController.Instance;
            if (p) p.InputLocked = true;
            if (st.clock) GuideArrow.Reached(st.clock);
            yield return new WaitForSecondsRealtime(0.4f);
            // 시계가 다시 감김 → 그 섬 시간이 다시 흐름 · 색이 한 단계 돌아옴
            if (st.clock) CameraFollow.FocusOn(st.clock.position + Vector3.back * 1.5f, 0.75f);
            yield return new WaitForSecondsRealtime(0.6f);
            if (Sfx.Has("e4_wind")) Sfx.Play("e4_wind", 0.6f, 0f);
            yield return new WaitForSecondsRealtime(0.5f);
            if (Sfx.Has("e4_bell")) Sfx.Play("e4_bell", 0.65f, 0f);
            var at = st.clock ? st.clock.position + Vector3.up * 3.4f : p.transform.position;
            FxBurst.Spawn(at, 1.8f, FxBurst.Kind.Gold, Gold);
            for (int i = 0; i < 4; i++) Enemy.Fx3("Stardust", at + Random.insideUnitSphere * 1.5f, 0.7f);
            Meta.MarkStory("q4_i_" + st.id);
            Resume(si);
            satTarget = SatFor(DoneCount);
            yield return new WaitForSecondsRealtime(1.0f);
            // NPC 두 마디
            var spot = GameObject.Find(def.spot);
            if (spot) CameraFollow.FocusOn(spot.transform.position + Vector3.back * 1.2f, 0.75f);
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Say(ui, def.speaker, def.lines);
            CameraFollow.FocusOn(null, 0f);
            int n = DoneCount;
            ui.Toast($"<color=#FFD54A>시간이 다시 흐른다 ({n}/{Defs.Length})</color>");
            if (n >= Defs.Length)
            {
                yield return new WaitForSecondsRealtime(1.0f);
                stage = Stage.Plaza;
                if (plazaClock)
                {
                    CameraFollow.FocusOn(plazaClock.position + Vector3.up * 2f + Vector3.back * 1.5f, 0.8f);
                    yield return new WaitForSecondsRealtime(1.4f);
                    CameraFollow.FocusOn(null, 0f);
                }
                ui.Toast("<color=#FFD54A>광장으로</color>");
            }
            ShowArrows();
            if (p) p.InputLocked = false;
            Busy = false;
        }

        // ---------- 광장: 11:59 ----------
        float tickClock;
        void TickPlazaClock()
        {
            if (!minuteHand || !minuteHand.gameObject.activeInHierarchy) return;
            if (Ch5Ending.ClockOverride) return;                                 // 5장 엔딩: 바늘을 12:00으로 옮기는 동안
            // 11:59 — 분침이 12로 넘어가려다 툭 되돌아옴 (1초마다)
            tickClock += Time.deltaTime;
            if (tickClock >= 1f)
            {
                tickClock -= 1f;
                var p = PlayerController.Instance;
                if (p && plazaClock && Effects.Flat(plazaClock.position - p.transform.position).magnitude < 15f && Sfx.Has("e4_tick")) Sfx.Play("e4_tick", 0.22f, 0.02f, 1);
            }
            float t = tickClock;
            float kick = t < 0.07f ? t / 0.07f : t < 0.32f ? 1f - (t - 0.07f) / 0.25f : 0f;
            minuteHand.localRotation = Quaternion.Euler(0, 0, 6f - 4.5f * kick);   // 앞에서 보면 +Z = 반시계 → 6° = 59분
            if (hourHand) hourHand.localRotation = Quaternion.Euler(0, 0, 0.5f);    // 11:59의 시침 = 12 바로 앞
        }

        IEnumerator Finale()
        {
            if (stage != Stage.Plaza) yield break;
            stage = Stage.Finale;
            Busy = true;
            var ui = UIRef;
            var p = PlayerController.Instance;
            if (p) p.InputLocked = true;
            GuideArrow.Clear();
            if (hourBadge) hourBadge.text = "";
            yield return new WaitForSecondsRealtime(0.5f);
            if (plazaClock) CameraFollow.FocusOn(plazaClock.position + Vector3.up * 2.5f + Vector3.back * 1.2f, 0.7f);
            yield return new WaitForSecondsRealtime(1.6f);
            var cat = Ch3Quest.I ? Ch3Quest.I.puddle : null;
            if (cat) CameraFollow.FocusOn(cat.position + Vector3.back * 1.0f, 0.7f);
            yield return Say(ui, MirrorCat,
                MirrorCat + "::그건 네가 감을 수 있는 시계가 아니야.",
                MirrorCat + "::그 바늘은... 누가 붙잡고 있으니까.",
                "고양이#의문::...누가?",
                MirrorCat + "::알잖아.");
            // 마을이 다시 움직임
            yield return new WaitForSecondsRealtime(0.4f);
            CameraFollow.FocusOn(null, 0f);
            if (Sfx.Has("e5b_bell")) Sfx.Play("e5b_bell", 0.6f, 0f); else if (Sfx.Has("e4_bell")) Sfx.Play("e4_bell", 0.7f, 0f);
            if (Sfx.Has("story_rewind")) Sfx.Play("story_rewind", 0.45f, 0f);
            Meta.MarkStory(KeyDone);
            Resume(int.MinValue);
            satTarget = 0f;
            Music.Duck(1f, 2f); ducked = false;
            CameraFollow.Shake(0.12f);
            for (float t = 0; t < 1.6f; t += Time.unscaledDeltaTime)
            {
                satNow = Mathf.MoveTowards(satNow, 0f, 45f * Time.unscaledDeltaTime);
                if (tone) tone.saturation.Override(BaseSat + satNow);
                yield return null;
            }
            if (vol) vol.weight = 0f;
            ui.Toast("<color=#FFD54A>마을이 다시 움직인다</color>");
            stage = Stage.Finished;
            if (hourBadge) { Destroy(hourBadge.gameObject); hourBadge = null; }
            if (p) p.InputLocked = false;
            Busy = false;
        }

        // ---------- 개발용 ----------
        /// <summary>개발용: 4장 퀘스트 기록 지우기 (4장 클리어 직후 상태로)</summary>
        public static void DevReset()
        {
            var keys = new List<string> { KeyIntro, KeyDone, "ch4" };
            foreach (var d in Defs) { keys.Add("q4_i_" + d.id); for (int k = 0; k < d.hours.Length; k++) keys.Add($"q4_g_{d.id}_{k}"); }
            foreach (var k in keys) PlayerPrefs.DeleteKey(Meta.StoryPrefKey(k));
            PlayerPrefs.Save();
        }

        /// <summary>개발용 (플레이 중 마을): 아직 안 된 섬 하나를 완료 처리 (톱니를 다 돌린 것처럼)</summary>
        public void DevCompleteNext()
        {
            if (Busy || stage != Stage.Islands) { Debug.Log($"[Ch4Quest] 지금은 섬 완료 불가 (stage {stage}, busy {Busy})"); return; }
            for (int i = 0; i < stations.Length; i++)
            {
                var st = stations[i];
                if (IsleDone(st.id)) continue;
                var def = DefOf(st.id);
                for (int k = 0; k < def.hours.Length; k++)
                    if (!GearDone(st.id, k) && st.gears != null && k < st.gears.Length && st.gears[k]) { TurnGear(i, k); if (Busy) return; }
                return;
            }
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("CatTang/4장 멈춘 마을/퀘스트 기록 지우기 (지금 슬롯)")]
        static void MenuReset() { DevReset(); Debug.Log($"[Ch4Quest] 슬롯 {Meta.ActiveSlot}: 4장 퀘스트 기록 지움 — 마을에 다시 들어오면 멈춘 마을부터"); }

        [UnityEditor.MenuItem("CatTang/4장 멈춘 마을/4장 클리어 직후로 (지금 슬롯 · 시험용)")]
        static void MenuSetup()
        {
            Meta.MarkChapterCleared(4);
            foreach (var k in new[] { ClockQuest.KeyStart, ClockQuest.KeyDone, VillageStory.KeyTailorMeet, Ch2Quest.KeyAsk, Ch2Quest.KeySand, Ch2Quest.KeyAmmo, Ch2Quest.KeyGun, Ch3Quest.KeyIntro, Ch3Quest.KeyDone, "intro", "ch1", "ch2", "ch3" }) Meta.MarkStory(k);
            DevReset();
            Debug.Log($"[Ch4Quest] 슬롯 {Meta.ActiveSlot}: 4장 클리어 · 3장 퀘스트 끝 · 4장 퀘스트 처음 상태 (장 클리어 {Meta.ChaptersCleared})");
        }

        [UnityEditor.MenuItem("CatTang/4장 멈춘 마을/섬 하나 완료 (플레이 중)")]
        static void MenuCompleteOne()
        {
            if (!Application.isPlaying || !I) { Debug.Log("[Ch4Quest] 플레이 중 마을에서만 (멈춘 마을 상태)"); return; }
            I.DevCompleteNext();
        }

        [UnityEditor.MenuItem("CatTang/4장 멈춘 마을/광장 마지막 대화 바로 (플레이 중)")]
        static void MenuFinale()
        {
            if (!Application.isPlaying || !I) { Debug.Log("[Ch4Quest] 플레이 중 마을에서만"); return; }
            if (I.stage != Stage.Plaza) { Debug.Log("[Ch4Quest] 섬 넷을 먼저 (지금 " + I.stage + ")"); return; }
            I.StartCoroutine(I.Finale());
        }
#endif
    }
}
