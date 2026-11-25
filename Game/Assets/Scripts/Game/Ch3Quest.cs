// 3장 클리어 뒤 마을 퀘스트 '거울 마을' (2026-11-25 사용자 확정 · 대사 컨펌, 해금 없음)
//   3장 클리어 = 꿈 없이 블랙아웃 → 좌우가 뒤집힌 마을에서 깨어남("...뭔가 이상해." / "여관이... 왼쪽에 있었던가?")
//   → 광장에 나와 선 거울 속 고양이(3장 상인)가 규칙을 알려 줌
//   → NPC 섬 넷(여관 뻐꾸기 · 공방 대장장이 · 총포상 보안관 · 의상실 재봉사)에 선 거울: 거울 속엔 NPC 대신 노인 집의 원래 물건이 비침.
//     NPC는 거울 옆으로 나와 서 있고, 거울 속 물건은 원래 그 NPC가 서던 자리에 놓여 있음 → 거울은 좌우가 바뀌어 보이므로
//     '비친 자리'(화면 그대로)가 아니라 그게 진짜 있어야 할 자리(지금 빈 NPC 자리)를 쏘면 허공에 금 → 거울이 깨짐. 틀린 자리는 튕길 뿐 (패널티 없음)
//     깨질 때 그 물건의 현실 소리가 먹먹하게 + NPC가 원래 모습을 어렴풋이 느끼는 두 마디 (그림 컷 없음)
//   → 넷 다 깨면 광장 큰 거울: 고양이 대신 낡은 외투의 노인 실루엣이 비침, 손을 들면 느리게 따라 듦 → 고양이가 그 모습을 쏴서 깸
//     → 총성 · 깨짐 · 병실 심전도 '삐' → 마을이 돌아옴 → 거울 고양이는 광장에 남음 (E: 판마다 다른 한 마디)
//   거울 속 고양이 = 광장에 그냥 나와 서 있는 NPC (2026-11-25 사용자: 물웅덩이 · 손거울 대신). 3장 상인 모습(좌우 반전 고양이 + 거울빛 + 가면)
// 뒤집기 = 마을 뿌리(Village) x 배율 -1 (섬 · 다리 · 건물 · NPC가 통째로 거울상). 마을에 들어올 때 한 번 정함 (Setup)
// 씬 배치(거울 · 물건 · NPC 설 자리 · 물웅덩이)는 Editor/Ch3QuestBuilder.cs가 Village 씬의 Village/Ch3Quest 아래에 원래(안 뒤집힌) 좌표로 저장
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CatTang
{
    public class Ch3Quest : MonoBehaviour
    {
        public const string KeyIntro = "q3_intro", KeyDone = "q3_done", KeyVisit = "q3_visit";
        public const string MirrorCat = "거울 속 고양이";
        const string Cuckoo = "태엽 풀린 뻐꾸기", Smith = "모래시계 대장장이", Tailor = "실타래 재봉사", Sheriff = "빈 외투 보안관";
        const float HitR = 1.7f, StageR = 17f, PlazaR = 7.5f;

        public struct Def
        {
            public string id, spot, isle, speaker, sound;
            public string[] lines;
        }

        // 섬 거울 넷 (2026-11-25 사용자 초안 확정): 물건 · 깨질 때 소리 · NPC 두 마디
        public static readonly Def[] Defs =
        {
            new Def { id = "inn", spot = "NpcSpot_MemoryRoom", isle = "Isle_Center", speaker = Cuckoo, sound = "q3_cuckoo",
                      lines = new[] { "...방금, 내 목소리였나?", "노래를 잊은 줄 알았는데." } },
            new Def { id = "ammo", spot = "NpcSpot_AmmoWorkshop", isle = "Isle_Ammo", speaker = Smith, sound = "e5b_sand",
                      lines = new[] { "누군가 날 뒤집어 주던 손이 있었지.", "그 손이 없으니 모래가 새기만 하는군." } },
            new Def { id = "gun", spot = "NpcSpot_GunShop", isle = "Isle_Gun", speaker = Sheriff, sound = "q3_projector",
                      lines = new[] { "이 외투, 누가 입었더라.", "영웅은 늘 화면 속에만 있었지." } },
            new Def { id = "ward", spot = "NpcSpot_Wardrobe", isle = "Isle_Wardrobe", speaker = Tailor, sound = "q3_sewing",
                      lines = new[] { "이 소리... 저녁마다 들리던 소리야.", "누군가의 옷을 짓고 있었는데." } },
        };

        // 물웅덩이 E (2026-11-25 사용자 컨펌, 판마다 하나)
        static readonly string[] PuddleLines =
        {
            "오늘은 몇 번 쓰러졌어?",
            "총이 무거워 보이네.",
            "서두를 거 없어. 시계는 어차피 갈 테니까.",
            "넌 싸우고, 난 기다리고. 그게 우리 몫이야.",
        };

        public static bool Broke(string id) => Meta.StorySeen("q3_m_" + id);
        public static int BrokenCount { get { int n = 0; foreach (var d in Defs) if (Broke(d.id)) n++; return n; } }
        /// <summary>3장을 넘었고 2장 퀘스트(총포상)까지 마쳐 섬 넷이 다 열림 · 아직 마을이 안 돌아옴</summary>
        public static bool Pending => Meta.ChaptersCleared >= 3 && Meta.StorySeen(Ch2Quest.KeyGun) && !Meta.StorySeen(KeyDone);
        /// <summary>이번에 마을이 뒤집힌 채로 들어왔는지 (들어올 때 한 번 정함)</summary>
        public static bool Flipped { get; private set; }
        public static bool Done => Meta.StorySeen(KeyDone);
        public static Ch3Quest I { get; private set; }
        static GameUI uiCache;
        public static GameUI UIRef => uiCache ? uiCache : (uiCache = FindFirstObjectByType<GameUI>());
        public static bool Busy;

        [System.Serializable]
        public class Station
        {
            public string id;
            public MirrorView mirror;
            public Transform obj;        // 거울에만 비치는 물건 (NpcSpot 자리)
            public Transform npcStand;   // 퀘스트 동안 NPC가 서는 자리 (거울 옆)
            public Transform view;       // 거울 카메라가 비추는 한가운데 (NPC 자리에서 옆으로 떨어져 있어야 '비친 자리'가 갈림)
        }

        public Station[] stations;
        public MirrorView plazaMirror;
        public Transform oldMan, oldManArm;
        [Tooltip("거울 속 고양이 NPC가 서는 자리 (MirrorCatNpc)")] public Transform puddle;

        Transform village;
        GameObject puddleCat;
        readonly Dictionary<string, (Transform npc, Vector3 pos, Quaternion rot)> npcHome = new();
        readonly List<(Renderer r, int layer)> playerLayers = new();
        enum Stage { Idle, Islands, PlazaWait, PlazaShoot, Finished }
        Stage stage;
        float nextShot;
        float armRaise;
        bool plazaRunning;

        void Awake() => I = this;
        void OnDestroy() { if (I == this) I = null; Busy = false; }

        /// <summary>VillageManager.Start 맨 앞에서: 뒤집을지 정하고 퀘스트 물건을 켜고 끔</summary>
        public static void Setup()
        {
            Flipped = Pending;
            var q = FindFirstObjectByType<Ch3Quest>(FindObjectsInactive.Include);
            if (!q) { Flipped = false; return; }
            q.gameObject.SetActive(true);
            q.Init();
        }

        void Init()
        {
            I = this;
            village = GameObject.Find("Village")?.transform;
            MirrorLayers.HideFromMain();
            if (Flipped && village) village.localScale = new Vector3(-Mathf.Abs(village.localScale.x), village.localScale.y, village.localScale.z);
            if (Done) PlayerPrefs.SetInt(Meta.StoryPrefKey(KeyVisit), PlayerPrefs.GetInt(Meta.StoryPrefKey(KeyVisit), 0) + 1);   // 판마다 물웅덩이 한 마디가 바뀜

            foreach (var s in stations)
            {
                bool show = Flipped;
                s.mirror.gameObject.SetActive(show);
                if (s.obj) { MirrorLayers.SetLayer(s.obj.gameObject, MirrorLayers.ReflectOnly); s.obj.gameObject.SetActive(show); }   // 거울 속 물건은 거울에만 비침
                if (!show) continue;
                var def = DefOf(s.id);
                var isle = GameObject.Find(def.isle);
                s.mirror.viewCenter = s.view ? s.view.position : isle ? isle.transform.position : s.mirror.transform.position;
                if (s.obj) MirrorLayers.SetLayer(s.obj.gameObject, MirrorLayers.ReflectOnly);
                if (Broke(s.id)) { s.mirror.Break(); if (s.obj) s.obj.gameObject.SetActive(false); }
                else MoveNpcToMirror(def, s);
            }
            if (plazaMirror) plazaMirror.gameObject.SetActive(Flipped && BrokenCount >= Defs.Length);
            if (oldMan) { MirrorLayers.SetLayer(oldMan.gameObject, MirrorLayers.ReflectOnly); oldMan.gameObject.SetActive(false); }
            bool puddleOn = Flipped || Done;
            if (puddle) puddle.gameObject.SetActive(puddleOn);
            if (puddleOn) MakePuddleCat();
            stage = !Flipped ? (Done ? Stage.Finished : Stage.Idle)
                  : !Meta.StorySeen(KeyIntro) ? Stage.Idle
                  : BrokenCount >= Defs.Length ? Stage.PlazaWait : Stage.Islands;
            if (Flipped && Meta.StorySeen(KeyIntro)) StartCoroutine(ArrowsLater());
        }

        static Def DefOf(string id) { foreach (var d in Defs) if (d.id == id) return d; return Defs[0]; }

        static Transform Spot(string name) { var g = GameObject.Find(name); return g ? g.transform : null; }

        void MoveNpcToMirror(Def def, Station s)
        {
            var spot = Spot(def.spot);
            if (!spot || !s.npcStand) return;
            var npc = spot.childCount > 0 ? spot.GetChild(0) : null;
            if (!npc) return;
            npcHome[def.id] = (npc, npc.localPosition, npc.localRotation);
            npc.position = s.npcStand.position;
            var look = Effects.Flat(s.mirror.transform.position - s.npcStand.position);
            npc.rotation = Quaternion.LookRotation(look);
            // 거울엔 NPC 대신 물건이 비침
            foreach (var r in npc.GetComponentsInChildren<Renderer>(true)) r.gameObject.layer = MirrorLayers.MainOnly;
        }

        void RestoreNpc(string id)
        {
            if (!npcHome.TryGetValue(id, out var h) || !h.npc) return;
            h.npc.localPosition = h.pos; h.npc.localRotation = h.rot;
            foreach (var r in h.npc.GetComponentsInChildren<Renderer>(true)) r.gameObject.layer = 0;
            npcHome.Remove(id);
        }

        IEnumerator ArrowsLater()
        {
            while (FindFirstObjectByType<VillageWake>() || FindFirstObjectByType<VillageStory>()) yield return null;
            ShowArrows();
        }

        void ShowArrows()
        {
            GuideArrow.Clear();
            if (stage == Stage.PlazaWait && plazaMirror) { GuideArrow.Show(plazaMirror.transform, "광장의 큰 거울", 6f, 4f, keep: true); return; }
            foreach (var s in stations) if (!Broke(s.id) && s.mirror) GuideArrow.Show(s.mirror.transform, "거울", 4.2f, 4f, keep: true);
        }

        // ---------- 물웅덩이 속 거울 고양이 ----------
        void MakePuddleCat()
        {
            var p = PlayerController.Instance;
            if (puddleCat || !p || !p.model || !puddle) return;
            puddleCat = Instantiate(p.model.gameObject, puddle);
            puddleCat.name = "MirrorCatFigure";
            foreach (var mb in puddleCat.GetComponentsInChildren<MonoBehaviour>(true)) Destroy(mb);
            foreach (var c in puddleCat.GetComponentsInChildren<Collider>(true)) Destroy(c);
            puddleCat.transform.localPosition = Vector3.zero;
            puddleCat.transform.rotation = Quaternion.Euler(0, 180f, 0);   // 카메라(남쪽)를 봄
            var ls = p.model.lossyScale;
            float sx = Mathf.Abs(ls.x) / Mathf.Max(0.001f, Mathf.Abs(puddle.lossyScale.x)) * (puddle.lossyScale.x < 0 ? 1f : -1f);   // 좌우가 뒤집힌 고양이 총잡이 (마을이 뒤집혀 있으면 이미 거울상)
            puddleCat.transform.localScale = new Vector3(sx, ls.y / Mathf.Max(0.001f, puddle.lossyScale.y), ls.z / Mathf.Max(0.001f, puddle.lossyScale.z));
            MirrorGhost.Apply(puddleCat, rings: false);
        }

        public static IEnumerator Intro(GameUI ui, System.Action<System.Action> setArrow)
        {
            var q = I;
            if (!q || !Flipped) yield break;
            var p = PlayerController.Instance;
            if (p) p.InputLocked = true;
            if (q.puddle)
            {
                CameraFollow.FocusOn(q.puddle.position + Vector3.back * 1.0f, 0.62f);
                yield return new WaitForSecondsRealtime(1.1f);
                if (Sfx.Has("q3_crack")) Sfx.Play("q3_crack", 0.35f, 0f);
                Enemy.Fx3("MirrorFlash", q.puddle.position + Vector3.up * 1.2f, 0.9f);
            }
            yield return Say(ui,
                MirrorCat + "::늦었군.",
                "고양이#의문::...내가 말을 하는 건가?",
                MirrorCat + "::난 네가 아니야. 정확히는... 반만.",
                MirrorCat + "::여긴 뒤집힌 쪽이야.",
                MirrorCat + "::거울 속엔 원래 있던 것들이 비쳐.",
                MirrorCat + "::비친 자리 말고, 그게 진짜 있어야 할 자리를 쏴.");
            Meta.MarkStory(KeyIntro);
            q.stage = Stage.Islands;
            // 가까운 섬 거울 하나를 비춰 줌
            Station first = null; float best = 1e9f;
            foreach (var s in q.stations)
                if (!Broke(s.id) && s.mirror) { float d = p ? Effects.Flat(s.mirror.transform.position - p.transform.position).magnitude : 0; if (d < best) { best = d; first = s; } }
            if (first != null)
            {
                CameraFollow.FocusOn(first.mirror.transform.position + Vector3.back * 1.5f, 0.8f);
                yield return new WaitForSecondsRealtime(1.5f);
            }
            CameraFollow.FocusOn(null, 0f);
            ui.Toast("<color=#FFD54A>거울 속 물건이 원래 있어야 할 자리를 찾아 쏘세요</color>");
            setArrow(() => q.ShowArrows());
        }

        static IEnumerator Say(GameUI ui, params string[] lines)
        {
            bool d = false;
            ui.Talk(MirrorCat, lines, () => d = true);
            while (!d) yield return null;
            var p = PlayerController.Instance;
            if (p) p.InputLocked = true;   // 대화 창이 닫히면 풀리므로 연출 동안 다시 잠금
        }

        public static IEnumerator PuddleTalk(GameUI ui)
        {
            var q = I;
            var p = PlayerController.Instance;
            if (!q || !ui) yield break;
            Busy = true;
            if (p) p.InputLocked = true;
            if (q.puddle) CameraFollow.FocusOn(q.puddle.position + Vector3.back * 1.0f, 0.7f);
            string line = !Done
                ? (q.stage == Stage.PlazaWait || q.stage == Stage.PlazaShoot ? "이제 광장이야. 마지막은 네 몫이야." : "비친 자리 말고, 그게 진짜 있어야 할 자리를 쏴.")
                : PuddleLines[PlayerPrefs.GetInt(Meta.StoryPrefKey(KeyVisit), 0) % PuddleLines.Length];
            yield return Say(ui, MirrorCat + "::" + line);
            CameraFollow.FocusOn(null, 0f);
            if (p) p.InputLocked = false;
            Busy = false;
        }

        // ---------- 쏘기 (퀘스트 동안만 · 거울 가까이에서) ----------
        void Update()
        {
            if (!Flipped || Busy) return;
            var p = PlayerController.Instance;
            var ui = UIRef;
            if (!p || p.InputLocked || (ui && ui.ScreenOpen)) return;
            if (stage == Stage.PlazaWait && plazaMirror && !plazaRunning && Effects.Flat(plazaMirror.transform.position - p.transform.position).magnitude < PlazaR)
            { StartCoroutine(PlazaScene()); return; }
            if (stage == Stage.PlazaShoot) FollowOldMan(p);
            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame || Time.time < nextShot) return;
            if (stage != Stage.Islands && stage != Stage.PlazaShoot) return;
            if (!GroundPoint(mouse.position.ReadValue(), out var at)) return;
            if (stage == Stage.PlazaShoot)
            {
                if (Effects.Flat(at - plazaMirror.transform.position).magnitude < 4.2f) { nextShot = Time.time + 9f; StartCoroutine(BreakPlaza(p)); }
                else { Shot(p, at + Vector3.up * 1.0f); nextShot = Time.time + 0.3f; }
                return;
            }
            var st = NearStation(p.transform.position);
            if (st == null) return;                                  // 거울 가까이가 아니면 마을에선 총을 안 씀
            nextShot = Time.time + 0.3f;
            var target = st.obj ? st.obj.position : Vector3.zero;
            var decoy = Decoy(st);
            Shot(p, at + Vector3.up * 1.1f);
            if (Effects.Flat(at - target).magnitude < HitR) { Busy = true; StartCoroutine(BreakIsland(st, target)); }
            else if (Effects.Flat(at - decoy).magnitude < HitR * 1.2f)
            {
                // 비친 자리 그대로 쏨: 거울빛에 튕김 (패널티 없음)
                var hit = new Vector3(at.x, 1.1f, at.z);
                Enemy.Fx3("MirrorFlash", hit, 0.7f);
                FxBurst.Spawn(hit, 0.9f, FxBurst.Kind.Spark, new Color(0.8f, 0.95f, 1f));
                if (Sfx.Has("e_glass")) Sfx.Play("e_glass", 0.25f, 0.1f);
            }
        }

        Station NearStation(Vector3 from)
        {
            Station best = null; float bd = StageR;
            foreach (var s in stations)
            {
                if (Broke(s.id) || !s.mirror || !s.mirror.gameObject.activeInHierarchy) continue;
                float d = Mathf.Min(Effects.Flat(s.mirror.transform.position - from).magnitude, s.obj ? Effects.Flat(s.obj.position - from).magnitude : 999f);
                if (d < bd) { bd = d; best = s; }
            }
            return best;
        }

        /// <summary>거울에 비친 그대로(화면상 같은 쪽)의 자리 = 거울 카메라 중심선 기준 좌우 반대</summary>
        static Vector3 Decoy(Station s)
        {
            var t = s.obj ? s.obj.position : s.mirror.transform.position;
            var c = s.mirror.viewCenter;
            var right = Camera.main ? Effects.Flat(Camera.main.transform.right).normalized : Vector3.right;
            float off = Vector3.Dot(t - c, right);
            return t - right * off * 2f;
        }

        static bool GroundPoint(Vector2 screen, out Vector3 at)
        {
            at = default;
            var cam = Camera.main;
            if (!cam) return false;
            var ray = cam.ScreenPointToRay(screen);
            var plane = new Plane(Vector3.up, Vector3.zero);
            if (!plane.Raycast(ray, out float d)) return false;
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

        // ---------- 섬 거울 깨기 ----------
        IEnumerator BreakIsland(Station s, Vector3 target)
        {
            var ui = UIRef;
            var p = PlayerController.Instance;
            var def = DefOf(s.id);
            if (p) p.InputLocked = true;
            GuideArrow.Reached(s.mirror.transform);
            // 허공에 금
            var air = target + Vector3.up * 1.3f;
            if (Sfx.Has("q3_crack")) Sfx.Play("q3_crack", 0.9f, 0f);
            Enemy.Fx3("MirrorFlash", air, 1.4f);
            FxBurst.Spawn(air, 1.6f, FxBurst.Kind.Spark, new Color(0.85f, 0.95f, 1f));
            CameraFollow.Shake(0.12f);
            yield return new WaitForSecondsRealtime(0.5f);
            // 거울이 깨짐 → 조각마다 그 물건의 현실 소리가 먹먹하게
            CameraFollow.FocusOn(s.mirror.transform.position + Vector3.back * 1.2f, 0.75f);
            yield return new WaitForSecondsRealtime(0.55f);
            var gc = s.mirror.GlassCenter;
            s.mirror.Break();
            if (s.obj) s.obj.gameObject.SetActive(false);
            if (Sfx.Has("e5_rift_shatter")) Sfx.Play("e5_rift_shatter", 0.75f, 0.05f); else Sfx.Play("e_glass", 0.8f, 0.05f);
            for (int i = 0; i < 6; i++) Enemy.Fx3("GlassBurst", gc + Random.insideUnitSphere * 0.8f, 0.7f + Random.value * 0.4f);
            CameraFollow.Shake(0.2f);
            Music.Duck(0.25f, 0.4f);
            if (Sfx.Has(def.sound)) Sfx.Play(def.sound, 0.55f, 0f);
            yield return new WaitForSecondsRealtime(1.0f);
            for (int i = 0; i < 3; i++)
            {
                Enemy.Fx3("Stardust", gc + Vector3.down * (0.6f + i * 0.5f) + Random.insideUnitSphere * 0.6f, 0.6f);
                yield return new WaitForSecondsRealtime(0.45f);
            }
            Music.Duck(0f, 0.8f);
            Meta.MarkStory("q3_m_" + s.id);
            // NPC가 원래 모습을 어렴풋이 느낌
            var npc = npcHome.TryGetValue(s.id, out var h) ? h.npc : null;
            if (npc) CameraFollow.FocusOn(npc.position + Vector3.back * 1.2f, 0.75f);
            bool d = false;
            ui.Talk(def.speaker, def.lines, () => d = true);
            while (!d) yield return null;
            if (p) p.InputLocked = true;
            CameraFollow.FocusOn(null, 0f);
            int n = BrokenCount;
            ui.Toast($"<color=#FFD54A>거울이 깨졌다 ({n}/{Defs.Length})</color>");
            if (n >= Defs.Length)
            {
                yield return new WaitForSecondsRealtime(0.9f);
                if (puddle) { CameraFollow.FocusOn(puddle.position + Vector3.back * 1.0f, 0.7f); yield return new WaitForSecondsRealtime(0.8f); }
                yield return Say(ui, MirrorCat + "::이제 광장이야. 마지막은 네 몫이야.");
                stage = Stage.PlazaWait;
                if (plazaMirror)
                {
                    plazaMirror.gameObject.SetActive(true);
                    CameraFollow.FocusOn(plazaMirror.transform.position + Vector3.back * 1.5f, 0.85f);
                    yield return RiseUp(plazaMirror.transform, 1.4f);
                    yield return new WaitForSecondsRealtime(0.6f);
                }
                CameraFollow.FocusOn(null, 0f);
                ui.Toast("<color=#FFD54A>광장의 큰 거울로</color>");
            }
            ShowArrows();
            if (p) p.InputLocked = false;
            Busy = false;
        }

        static IEnumerator RiseUp(Transform t, float dur)
        {
            var end = t.position;
            var start = end + Vector3.down * 6f;
            if (Sfx.Has("e5b_bell")) Sfx.Play("e5b_bell", 0.5f, 0f);
            for (float k = 0; k < 1f; k += Time.unscaledDeltaTime / dur)
            {
                t.position = Vector3.Lerp(start, end, 1f - (1f - k) * (1f - k));
                if (Random.value < 0.25f) Enemy.Fx3("Stardust", end + Random.insideUnitSphere * 2f, 0.6f);
                yield return null;
            }
            t.position = end;
            CameraFollow.Shake(0.15f);
        }

        // ---------- 광장 큰 거울 ----------
        void HidePlayerFromMirror(PlayerController p, bool hide)
        {
            // 고양이 모델은 PlayerController가 계속 Vivid 레이어로 되돌림 → 큰 거울 카메라에서 그 레이어를 뺌
            if (plazaMirror) plazaMirror.excludeMask = hide && VividLayer.Id >= 0 ? 1 << VividLayer.Id : 0;
            if (hide)
            {
                playerLayers.Clear();
                foreach (var r in p.GetComponentsInChildren<Renderer>(true)) { playerLayers.Add((r, r.gameObject.layer)); r.gameObject.layer = MirrorLayers.MainOnly; }
            }
            else
            {
                foreach (var (r, l) in playerLayers) if (r) r.gameObject.layer = l;
                playerLayers.Clear();
            }
        }

        void FollowOldMan(PlayerController p)
        {
            if (!oldMan || !p) return;
            oldMan.position = p.transform.position;
            if (p.model) oldMan.rotation = Quaternion.Euler(0, p.model.eulerAngles.y + 180f, 0);   // 키트 앞 = 로컬 −Z → 고양이와 같은 쪽을 봄
            if (oldManArm) oldManArm.localRotation = Quaternion.Euler(100f * armRaise, 0, 0);   // 키트 앞 = 로컬 −Z → +X 회전이 앞으로 듦
        }

        IEnumerator PlazaScene()
        {
            plazaRunning = true;
            Busy = true;
            var ui = UIRef;
            var p = PlayerController.Instance;
            p.InputLocked = true;
            GuideArrow.Clear();
            HidePlayerFromMirror(p, true);
            if (oldMan) oldMan.gameObject.SetActive(true);
            armRaise = 0;
            // 고양이를 거울 앞에 세우고 거울을 보게
            var m = plazaMirror.transform;
            var face = Effects.Flat(m.position - p.transform.position);
            if (p.model) p.model.rotation = Quaternion.LookRotation(face);
            FollowOldMan(p);
            CameraFollow.FocusOn((p.transform.position + m.position) * 0.5f + Vector3.up * 0.5f, 0.72f);
            Music.Duck(0.3f, 0.8f);
            yield return new WaitForSecondsRealtime(1.4f);
            bool d = false;
            ui.Talk("고양이", new[] { "고양이#의문::...이건 내가 아니야." }, () => d = true);
            while (!d) { FollowOldMan(p); yield return null; }
            p.InputLocked = true;
            // 고양이가 손을 들면 노인도 느리게 따라 듦
            p.poser?.SetAimWeight(1f);
            yield return new WaitForSecondsRealtime(0.6f);
            for (float k = 0; k < 1f; k += Time.unscaledDeltaTime / 2.2f)
            {
                armRaise = Mathf.SmoothStep(0, 1, k);
                FollowOldMan(p);
                yield return null;
            }
            yield return new WaitForSecondsRealtime(0.5f);
            d = false;
            ui.Talk(MirrorCat, new[] { MirrorCat + "::정말 그래?", "고양이#결의::...내가 아니라고." }, () => d = true);
            while (!d) { FollowOldMan(p); yield return null; }
            CameraFollow.FocusOn(null, 0f);
            ui.Toast("<color=#FFD54A>거울 속 모습을 쏘세요</color>");
            stage = Stage.PlazaShoot;
            p.InputLocked = false;
            Busy = false;
            // 손은 천천히 내려옴
            for (float k = 0; k < 1f && stage == Stage.PlazaShoot; k += Time.unscaledDeltaTime / 3f) { armRaise = 1f - k * 0.6f; yield return null; }
        }

        IEnumerator BreakPlaza(PlayerController p)
        {
            Busy = true;
            stage = Stage.Finished;
            var ui = UIRef;
            var ov = StoryOverlay.I;
            p.InputLocked = true;
            var gc = plazaMirror.GlassCenter;
            // 총성 → 깨짐
            p.poser?.SetAimWeight(1f);
            Shot(p, gc);
            Time.timeScale = 0.3f;
            CameraFollow.FocusOn(gc + Vector3.down * 1.5f, 0.7f);
            yield return new WaitForSecondsRealtime(0.25f);
            plazaMirror.Break();
            if (oldMan) oldMan.gameObject.SetActive(false);
            if (Sfx.Has("e5_rift_shatter")) Sfx.Play("e5_rift_shatter", 1f, 0f);
            Sfx.Play("e_glass", 0.9f, 0f);
            for (int i = 0; i < 12; i++) Enemy.Fx3("GlassBurst", gc + Random.insideUnitSphere * 1.6f, 0.9f + Random.value * 0.6f);
            CameraFollow.Shake(0.4f);
            yield return new WaitForSecondsRealtime(0.9f);
            Time.timeScale = 1f;
            // 삐 —
            Music.Duck(0.05f, 0.4f);
            if (Sfx.Has("story_flatline")) Sfx.Play("story_flatline", 0.8f, 0f);
            if (ov) yield return ov.FadeWhite(0, 1, 1.0f);
            yield return new WaitForSecondsRealtime(0.8f);
            // 마을이 돌아옴
            Restore(p);
            CameraFollow.FocusOn(null, 0f);
            if (ov) yield return ov.FadeWhite(1, 0, 1.2f);
            Music.Duck(0f, 1.2f);
            if (puddle)
            {
                CameraFollow.FocusOn(puddle.position + Vector3.back * 1.0f, 0.7f);
                yield return new WaitForSecondsRealtime(0.9f);
            }
            yield return Say(ui, MirrorCat + "::깨도 비치는 건 그대로야.", MirrorCat + "::난 여기 있을게. 언제든 들여다봐.");
            CameraFollow.FocusOn(null, 0f);
            ui.Toast("<color=#FFD54A>마을이 원래대로 돌아왔다</color>");
            p.InputLocked = false;
            Busy = false;
        }

        void Restore(PlayerController p)
        {
            Meta.MarkStory(KeyDone);
            HidePlayerFromMirror(p, false);
            p.poser?.SetAimWeight(0f);
            if (village) village.localScale = new Vector3(Mathf.Abs(village.localScale.x), village.localScale.y, village.localScale.z);
            if (puddleCat) { var ls = puddleCat.transform.localScale; puddleCat.transform.localScale = new Vector3(-ls.x, ls.y, ls.z); }   // 마을이 돌아와도 거울 고양이는 거울상 그대로
            Flipped = false;
            foreach (var s in stations) { RestoreNpc(s.id); if (s.mirror) s.mirror.gameObject.SetActive(false); if (s.obj) s.obj.gameObject.SetActive(false); }
            foreach (var id in new List<string>(npcHome.Keys)) RestoreNpc(id);
            if (plazaMirror) plazaMirror.gameObject.SetActive(false);
            if (puddle) puddle.gameObject.SetActive(true);
            // 고양이는 같은 자리(x만 뒤집힌 섬 위)에 서 있게
            var pos = p.transform.position;
            p.Teleport(new Vector3(-pos.x, pos.y, pos.z));
            VillageManager.RefreshFacilityLabels();
        }

        /// <summary>개발용: 퀘스트 기록 지우기 (3장 클리어 직후 상태로)</summary>
        public static void DevReset()
        {
            foreach (var k in new[] { KeyIntro, KeyDone, "ch3", StoryRun.KeyBlackout3, "q3_m_inn", "q3_m_ammo", "q3_m_gun", "q3_m_ward" }) PlayerPrefs.DeleteKey(Meta.StoryPrefKey(k));
            PlayerPrefs.Save();
        }
    }
}
