// 스토리 맵 4 '시계를 멈춘 공방' (2026-11-25 사용자 확정 · 대사 확정, 명세 Art/tools/specs/ch4_story_spec.md) 
//   판 안에서 처음 4장 보스 문에 들어서면 한 번, 노년의 시계공이 그녀를 떠나보낸 비 오는 밤의 공방으로 → 끝나면 크로노 & 템포 방으로 바로 이어짐.
//   노인(그림자)이 옛 풍습대로 공방의 시계를 하나씩 멈춤. 보스 기믹을 압박 없이 익힘:
//   ① 순서대로 멈추기: 벽시계가 차례로 빛남 → 빛난 순서대로 그 시계 숫자의 시각 탄 → 진자가 멈춤 (3개 → 4개, 2번째는 내림차순이 섞여 중간 장전)
//   ② 그녀의 오르골: 톱니 3개, 톱니 숫자 시각 탄으로 쏘면 돌아감 (15초, 실패해도 다시) → 오르골이 한 번 울림
//   ③ 시계 그림자 전투 2번 + 뒤섞인 문자판(느슨하게: 섞인 뒤 내 발사 시각 숫자 칸이면 안전, 실패는 작은 피해)
//   끝: 시계가 하나씩 멈추고 노인 그림자가 괘종시계 바늘을 손으로 붙잡음 → 11:59 정지 → 하얗게 → 보스 방
// 맵 = Resources/Story/StoryWorkshop.prefab (블렌더 키트, Editor/StoryWorkshopBuilder.cs). 곡 = bgm_story_4 (Gemini)
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    public class StoryWorkshop : MonoBehaviour
    {
        public const string Key = "storymap_workshop";
        const string Old = "???";                                             // 노인 — 이름은 가림
        static readonly Vector3 Origin = new(-1400, 0, -1400);                 // 판 맵 · 다른 스토리 맵과 멀리 떨어진 자리
        const float SeqTime = 22f, GearTime = 15f, ShuffleWait = 6f, ShuffleFailFrac = 0.06f;
        static readonly int[][] SeqHours = { new[] { 2, 5, 9 }, new[] { 3, 8, 6, 11 } };   // 2번째: 8 → 6이 거꾸로 → 한 바퀴 돌려 장전
        static readonly int[] GearHours = { 2, 9, 5 };                          // 4장 기획서 예시 그대로
        static readonly Color Gold = new(1f, 0.85f, 0.4f), Cream = new(0.95f, 0.92f, 0.85f), Red = new(0.85f, 0.12f, 0.14f);
        static readonly Color NightBg = new(0.05f, 0.05f, 0.07f);

        /// <summary>이번 문이 처음 만나는 4장 보스 문이면 스토리 맵 4</summary>
        public static bool Pending(int chapter) => chapter == 4 && !Meta.StorySeen(Key);

        /// <summary>practice = 보스 패턴 연습 (StoryPractice): 대사 · 연출 없이 기믹만, 기록 안 함</summary>
        public static void Play(RunManager rm, System.Action done, bool practice = false)
        {
            var sm = new GameObject("StoryWorkshop").AddComponent<StoryWorkshop>();
            sm.rm = rm; sm.done = done; sm.practice = practice;
        }

        RunManager rm;
        System.Action done;
        PlayerController p;
        GameUI ui;
        Transform map;
        Camera cam;
        CameraClearFlags oldFlags;
        Color oldBg, oldAmbient;
        Light sun; Color sunColor; float sunIntensity; Quaternion sunRot;
        AudioSource rain, tick;
        readonly List<Enemy> shadows = new();
        readonly List<GameObject> temp = new();
        bool finished;
        bool practice;                 // 보스 패턴 연습
        StoryPractice pr;

        // 진자: 흔들리는 것 · 멈춘 것
        readonly List<Transform> pendulums = new();
        readonly HashSet<Transform> stopped = new();

        // 따뜻한 나무 공방 → 시계를 멈출수록 램프가 꺼지고 색이 빠짐 → 11:59엔 흑백 + 빨간 숄만 (2026-11-26 사용자 확정)
        static readonly int DesatId = Shader.PropertyToID("_CatTangDesat"), KeepId = Shader.PropertyToID("_KeepColor");
        const float DesatPerStop = 0.065f, DesatFight = 0.55f;
        float desatNow, desatTarget;
        readonly List<Light> warm = new();
        readonly Dictionary<Light, float> warmI = new();
        int warmOff;

        /// <summary>시계 하나가 멈춤: 색이 조금 빠지고 따뜻한 램프 하나가 꺼짐</summary>
        void Drain(float add, bool lamp = true)
        {
            desatTarget = Mathf.Min(1f, desatTarget + add);
            if (lamp && warmOff < warm.Count) { var l = warm[warmOff++]; if (l) StartCoroutine(LampOff(l)); }
        }

        IEnumerator LampOff(Light l)
        {
            float i0 = l.intensity;
            for (float t = 0; t < 1f && l; t += Time.deltaTime / 0.6f) { l.intensity = Mathf.Lerp(i0, 0, t * t) * (1f + 0.25f * Mathf.Sin(t * 40f) * (1 - t)); yield return null; }   // 깜빡이며 꺼짐
            if (l) l.intensity = 0;
        }
        readonly Dictionary<Transform, Quaternion> pendRest = new();

        // 쏴서 맞히는 과녁
        Health needTarget; int needHour; bool wrongOrder;
        readonly List<Health> seqTargets = new();
        readonly Dictionary<Health, int> gearHour = new();
        readonly HashSet<Health> gearDone = new();
        readonly Dictionary<Health, Transform> gearSpin = new();

        // 문자판 바닥
        readonly List<Transform> tiles = new();          // 칸 k(12시부터 시계 방향)에 놓인 칸 오브젝트
        readonly List<int> tileNum = new();               // 칸 k에 적힌 숫자
        Vector3 dialC; float dialR = 5.5f;

        /// <summary>맵 안에서 이름으로 찾기 (깊이 상관없이)</summary>
        Transform F(string name)
        {
            if (!map) return null;
            foreach (var t in map.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }
        static Transform Child(Transform root, string name)
        {
            if (!root) return null;
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }

        IEnumerator Start()
        {
            p = PlayerController.Instance;
            ui = FindFirstObjectByType<GameUI>();
            var ov = StoryOverlay.I;
            p.InputLocked = true;
            p.health.invulnerable = true;
            if (rm) rm.EnterStoryMap();
            Music.Duck(0.25f, 0.6f);
            if (ov) { if (!practice) yield return ov.Headache(0.6f, 1.0f); yield return ov.FadeWhite(0, 1, practice ? 0.35f : 0.9f); }

            var prefab = Resources.Load<GameObject>("Story/StoryWorkshop");
            if (!prefab) { Debug.LogWarning("[StoryWorkshop] Story/StoryWorkshop 프리팹 없음 — 건너뜀"); yield return Finish(false); yield break; }
            map = Instantiate(prefab, Origin, Quaternion.identity).transform;
            Night_On();
            CollectMap();
            var spawn = F("Spawn");
            p.Teleport((spawn ? spawn.position : Origin) + Vector3.up * 0.3f);
            CameraFollow.SnapZoom(1f);
            if (P) P.OnFired += OnPlayerFired;
            if (practice) pr = StoryPractice.Begin(this, Quit);
            if (ov) yield return ov.FadeWhite(1, 0, practice ? 0.5f : 1.2f);
            Music.Duck(0f, 0.8f);
            Music.Play(StoryBgm.For(4, "stage"));                                // 전용 곡 (Gemini) — 맵 내내, 전투에서도
            if (Sfx.Has("st_rain")) rain = Sfx.Loop("st_rain", 0.35f);
            if (Sfx.Has("e4_tick_1")) tick = Sfx.Loop("e4_tick_1", 0.45f);

            if (!practice) yield return Say("고양이#의문::...시끄럽군.", "고양이#의문::시계가 이렇게나 많았나.");
            p.InputLocked = false;
            p.health.invulnerable = false;

            // ① 순서대로 멈추기
            var grand = F("GrandClock");
            ui?.Toast("괘종시계 앞으로");
            yield return WaitNear(grand ? grand.position : Origin, 6f);
            if (!practice)
            {
                p.InputLocked = true;
                var wallFocus = Center("WallClock_1", "WallClock_2", "WallClock_3", "WallClock_4");
                CameraFollow.FocusOn(wallFocus, 0.85f);
                yield return Say(Old + "::멈춰야 해.", Old + "::그 사람이 간 시각에.");
                CameraFollow.FocusOn(null, 0f);
                p.InputLocked = false;
            }
            for (int r = 0; r < SeqHours.Length;)
            {
                bool ok = false;
                yield return Sequence(SeqHours[r], v => ok = v);
                if (!p || !p.health.Alive) yield break;
                if (!ok) { yield return new WaitForSeconds(0.8f); continue; }
                if (!practice)
                {
                    p.InputLocked = true;
                    if (r == 0) yield return Say(Old + "::하나씩... 순서대로.");
                    else yield return Say("고양이#의문::(시계를... 멈추는 건가?)");
                    p.InputLocked = false;
                }
                r++;
                yield return new WaitForSeconds(0.6f);
            }

            // ② 그녀의 오르골
            if (!practice)
            {
                p.InputLocked = true;
                CameraFollow.FocusOn(Center("Gear_1", "Gear_2", "Gear_3"), 0.85f);
                yield return Say(Old + "::이건... 그 사람 거였지.", Old + "::한 번만 더 듣고 싶어.");
                CameraFollow.FocusOn(null, 0f);
                p.InputLocked = false;
            }
            while (true)
            {
                bool ok = false;
                yield return Gears(v => ok = v);
                if (!p || !p.health.Alive) yield break;
                if (ok) break;
                yield return new WaitForSeconds(0.8f);
            }
            if (!practice)                                                     // 연습: 오르골 연출 · 대사 없음
            {
                yield return MusicBox();
                p.InputLocked = true;
                yield return Say("고양이#의문::...이 노래, 어디서 들었더라.");
                p.InputLocked = false;
            }

            // ③ 시계 그림자들이 떨어져 나옴
            if (!practice)
            {
                p.InputLocked = true;
                CameraFollow.FocusOn(dialC + Vector3.forward * 3f, 0.85f);
                yield return Say("고양이#결의::...시계 그림자가 떨어져 나온다.");
                CameraFollow.FocusOn(null, 0f);
                p.InputLocked = false;
            }
            desatTarget = Mathf.Max(desatTarget, DesatFight);                 // 그림자가 떨어져 나오며 반쯤 바랜 공방
            yield return Wave(4, shuffle: false);
            yield return Wave(5, shuffle: true);
            if (practice) { yield return Finish(true); yield break; }   // 연습: 끝 연출 · 기록 없음

            // 끝: 시계가 하나씩 멈추고 노인이 바늘을 붙잡음
            p.InputLocked = true;
            if (P) P.OnFired -= OnPlayerFired;
            Music.Duck(0.35f, 1.2f);
            yield return Ending();
            Meta.MarkStory(Key);
            yield return Finish(true);
        }

        // ---------- 밤 · 맵 ----------
        void Night_On()
        {
            cam = Camera.main;
            if (cam) { oldFlags = cam.clearFlags; oldBg = cam.backgroundColor; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = NightBg; }
            oldAmbient = RenderSettings.ambientLight;
            RenderSettings.ambientLight = new Color(0.42f, 0.43f, 0.5f);
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional && l.enabled) { sun = l; break; }
            // 창으로 드는 차가운 달빛 (느와르)
            if (sun) { sunColor = sun.color; sunIntensity = sun.intensity; sunRot = sun.transform.rotation; sun.color = new Color(0.78f, 0.84f, 1f); sun.intensity = sunIntensity * 0.75f; sun.transform.rotation = Quaternion.Euler(55f, -35f, 0f); }
        }

        void Night_Off()
        {
            if (cam) { cam.clearFlags = oldFlags; cam.backgroundColor = oldBg; }
            RenderSettings.ambientLight = oldAmbient;
            if (sun) { sun.color = sunColor; sun.intensity = sunIntensity; sun.transform.rotation = sunRot; }
        }

        void CollectMap()
        {
            // 따뜻한 빛: WarmLight_* 먼저(번호 순) → 작업대 램프 · 빨간 램프는 마지막까지 남김
            var ws = new List<Light>(); Light lamp = null, red = null;
            foreach (var l in map.GetComponentsInChildren<Light>(true))
            {
                if (l.name.StartsWith("WarmLight")) ws.Add(l);
                else if (l.name == "LampLight") lamp = l;
                else if (l.name == "RedLampLight") red = l;
            }
            static int Num(Light l) { int.TryParse(l.name.Substring(l.name.LastIndexOf('_') + 1), out var k); return k; }
            ws.Sort((a, b) => Num(a).CompareTo(Num(b)));                         // 번호 순 (WarmLight_10 = 작업대 램프가 마지막)
            warm.AddRange(ws); if (red) warm.Add(red); if (lamp) warm.Add(lamp);
            foreach (var l in warm) warmI[l] = l.intensity;
            // 빨간 숄은 끝까지 색을 남김 (빈 의자에서 붉은 재질만)
            var chair = F("EmptyChair");
            if (chair)
                foreach (var r in chair.GetComponentsInChildren<Renderer>(true))
                    foreach (var m in r.materials)
                        if (m.HasProperty("_BaseColor")) { var c = m.GetColor("_BaseColor"); if (c.r > 0.45f && c.r > c.g * 2.2f && c.r > c.b * 2.2f) m.SetFloat(KeepId, 1f); }
            Shader.SetGlobalFloat(DesatId, 0f);
            foreach (var t in map.GetComponentsInChildren<Transform>(true))
                if (t.name == "Pendulum") { pendulums.Add(t); pendRest[t] = t.localRotation; }
            tiles.Clear(); tileNum.Clear();
            var sum = Vector3.zero; int n = 0;
            for (int i = 1; i <= 12; i++)
            {
                var t = F("Tile_" + i);
                if (t) { sum += t.position; n++; }
            }
            dialC = n > 0 ? sum / n : Origin;
            dialC.y = Origin.y;
            // 칸 k = 12시 방향(+z)부터 시계 방향: 각 Tile_i를 실제 놓인 방향으로 정렬
            var tmp = new Transform[12];
            float rSum = 0; int rn = 0;
            for (int i = 1; i <= 12; i++)
            {
                var t = F("Tile_" + i);
                if (!t) continue;
                var d = Effects.Flat(t.position - dialC);
                rSum += d.magnitude; rn++;
                float a = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg; if (a < 0) a += 360f;
                int k = Mathf.RoundToInt(a / 30f) % 12;
                tmp[k] = t;
            }
            if (rn > 0) dialR = rSum / rn;
            for (int k = 0; k < 12; k++)
            {
                tiles.Add(tmp[k]);
                int num = 0;
                if (tmp[k]) int.TryParse(tmp[k].name.Replace("Tile_", ""), out num);
                tileNum.Add(num > 0 ? num : (k == 0 ? 12 : k));
            }
        }

        Vector3 Center(params string[] names)
        {
            var s = Vector3.zero; int n = 0;
            foreach (var nm in names) { var t = F(nm); if (t) { s += t.position; n++; } }
            return n > 0 ? s / n : (map ? map.position : Origin);
        }

        // ---------- ① 순서대로 멈추기 ----------
        IEnumerator Sequence(int[] hours, System.Action<bool> result)
        {
            seqTargets.Clear();
            var labels = new List<GameObject>();
            var clocks = new List<Transform>();
            for (int i = 0; i < hours.Length; i++)
            {
                var c = F("WallClock_" + (i + 1));
                if (!c) continue;
                var at = Child(c, "Target") ?? c;
                var go = new GameObject("SeqTarget_" + hours[i]);
                go.transform.position = at.position;
                go.transform.SetParent(map, true);
                temp.Add(go);
                seqTargets.Add(Ch3Fx.Target(go, 0.9f, 2.2f));
                clocks.Add(c);
            }
            // 차례로 빛남: 숫자 + 종 소리 (벽시계는 서쪽 벽이라 빛나는 동안 카메라가 비춤)
            if (clocks.Count > 0) CameraFollow.FocusOn(Center("WallClock_1", "WallClock_2", "WallClock_3", "WallClock_4"), 0.85f);
            for (int i = 0; i < seqTargets.Count; i++)
            {
                var at = seqTargets[i].transform.position;
                var lb = BigLabel.Show(seqTargets[i].transform, new Vector3(0, 2.6f, 0), hours[i].ToString(), Gold, 1f, BigLabel.Frame.Hour);
                if (lb) labels.Add(lb.gameObject);
                Telegraph.Ring(Effects.Flat(at) + Vector3.up * (Origin.y + 0.05f), 1.1f, 0.6f, Telegraph.Aim);
                FxBurst.Spawn(at + Vector3.up * 1.2f, 1.2f, FxBurst.Kind.Gold, Gold);
                Sfx.Play("e4_bell", 0.6f, 0f, 2);
                yield return new WaitForSeconds(0.7f);
            }
            yield return new WaitForSeconds(0.4f);
            CameraFollow.FocusOn(null, 0f);
            ui?.Toast("빛난 순서대로, 그 시계 숫자와 같은 시각 탄으로 맞히세요");
            float end = Time.time + SeqTime;
            int done = 0; wrongOrder = false;
            while (done < seqTargets.Count && Time.time < end && p.health.Alive && !wrongOrder)
            {
                needHour = hours[done]; needTarget = seqTargets[done];
                while (needTarget && Time.time < end && !wrongOrder) yield return null;
                if (!needTarget && !wrongOrder)
                {
                    var t = seqTargets[done];
                    FxBurst.Spawn(t.transform.position + Vector3.up, 1.6f, FxBurst.Kind.Gold, Color.white);
                    Snd("e4_tick_2", 0.8f);
                    var pend = Child(clocks[done], "Pendulum");
                    if (pend) StartCoroutine(StopPendulum(pend));
                    Drain(DesatPerStop);
                    t.gameObject.SetActive(false);
                    done++;
                }
            }
            needTarget = null; needHour = 0;
            bool ok = done >= seqTargets.Count;
            if (!ok) ui?.Toast("순서가 틀렸어요 — 처음부터");
            foreach (var l in labels) if (l) Destroy(l);
            foreach (var t in seqTargets) if (t) Destroy(t.gameObject);
            seqTargets.Clear();
            // 실패면 멈췄던 진자를 다시 흔듦
            if (!ok) foreach (var c in clocks) { var pend = Child(c, "Pendulum"); if (pend) stopped.Remove(pend); }
            result(ok);
        }

        IEnumerator StopPendulum(Transform pend)
        {
            if (!pend || stopped.Contains(pend)) yield break;
            stopped.Add(pend);
            var from = pend.localRotation;
            var to = pendRest.TryGetValue(pend, out var r) ? r : from;
            for (float t = 0; t < 1f; t += Time.deltaTime / 0.45f)
            {
                if (!pend) yield break;
                pend.localRotation = Quaternion.Slerp(from, to, Mathf.SmoothStep(0, 1, t));
                yield return null;
            }
        }

        // ---------- ② 그녀의 오르골: 톱니 ----------
        IEnumerator Gears(System.Action<bool> result)
        {
            gearHour.Clear(); gearDone.Clear(); gearSpin.Clear();
            var labels = new List<GameObject>();
            for (int i = 0; i < 3; i++)
            {
                var g = F("Gear_" + (i + 1));
                if (!g) continue;
                var go = new GameObject("GearTarget_" + GearHours[i]);
                go.transform.position = Effects.Flat(g.position) + Vector3.up * Origin.y;
                go.transform.SetParent(map, true);
                temp.Add(go);
                var h = Ch3Fx.Target(go, 1.1f, 2.4f);
                gearHour[h] = GearHours[i];
                gearSpin[h] = g;
                var lbAt = Child(g, "Label");
                var lb = BigLabel.Show(go.transform, new Vector3(0, lbAt ? lbAt.position.y - Origin.y + 0.6f : 2.8f, 0), GearHours[i].ToString(), Gold, 0.9f, BigLabel.Frame.Gear);
                if (lb) labels.Add(lb.gameObject);
                Telegraph.Ring(go.transform.position + Vector3.up * 0.05f, 1.3f, 0.6f, Telegraph.Aim);
            }
            ui?.Toast("톱니 숫자와 같은 시각 탄으로 쏘면 돌아가요");
            float end = Time.time + GearTime;
            while (Time.time < end && gearDone.Count < gearHour.Count && p.health.Alive) yield return null;
            bool ok = gearHour.Count > 0 && gearDone.Count >= gearHour.Count;
            foreach (var l in labels) if (l) Destroy(l);
            foreach (var kv in gearHour) if (kv.Key) Destroy(kv.Key.gameObject);
            if (!ok) { gearDone.Clear(); ui?.Toast("톱니가 다시 멈췄어요 — 다시"); }
            gearHour.Clear();
            result(ok || gearSpin.Count == 0);
        }

        IEnumerator MusicBox()
        {
            var cyl = F("MusicCylinder");
            var at = cyl ? cyl.position : Center("Gear_1", "Gear_2", "Gear_3");
            Music.Duck(0.15f, 0.6f);
            float keep = desatTarget;
            desatTarget = Mathf.Max(0f, keep - 0.3f);                            // 오르골이 울리는 동안만 색이 잠깐 돌아옴 (기억의 온기)
            if (Sfx.Has("q4_musicbox")) Sfx.Play("q4_musicbox", 0.9f, 0f);
            else Snd("e4_bell_1", 0.7f);
            FxBurst.Spawn(at + Vector3.up * 0.6f, 1.6f, FxBurst.Kind.Gold, Gold);
            CameraFollow.FocusOn(at, 0.75f);
            for (float t = 0; t < 6f; t += Time.deltaTime)
            {
                if (cyl) cyl.Rotate(Vector3.right, 50f * Time.deltaTime, Space.Self);
                foreach (var g in gearSpin.Values) if (g) g.Rotate(Vector3.forward, 40f * Time.deltaTime, Space.Self);
                yield return null;
            }
            desatTarget = keep;
            CameraFollow.FocusOn(null, 0f);
            Music.Duck(0f, 1.2f);
        }

        void OnPlayerFired(int slot, bool awake)
        {
            // ① 순서 과녁
            foreach (var t in seqTargets)
            {
                if (!t || Time.time - t.lastHitAt > 0.001f) continue;
                if (t == needTarget)
                {
                    if (slot == needHour) needTarget = null;
                    else { FxBurst.Spawn(t.transform.position + Vector3.up * 1.2f, 0.8f, FxBurst.Kind.Spark, Color.white); Snd("e4_tick_1", 0.4f); }
                }
                else if (t.gameObject.activeSelf && seqTargets.IndexOf(t) > seqTargets.IndexOf(needTarget)) wrongOrder = true;   // 아직 차례가 아닌 시계
            }
            // ② 톱니: 자기 숫자 시각 탄에만 돌아감
            foreach (var kv in gearHour)
            {
                var h = kv.Key;
                if (!h || gearDone.Contains(h) || Time.time - h.lastHitAt > 0.001f) continue;
                if (slot == kv.Value) { gearDone.Add(h); Sfx.Play("e4_clank", 0.8f, 0.05f, 2); FxBurst.Spawn(h.transform.position + Vector3.up, 1.4f, FxBurst.Kind.Spark, Gold); }
                else FxBurst.Spawn(h.transform.position + Vector3.up, 0.8f, FxBurst.Kind.Spark, Color.white);
            }
        }

        // ---------- ③ 시계 그림자 · 뒤섞인 문자판 ----------
        IEnumerator Wave(int n, bool shuffle)
        {
            var spots = new List<Vector3>();
            for (int i = 0; i < n; i++)
            {
                var sp = F("ShadowSpawn_" + (i % 6 + 1));
                var pos = sp ? sp.position : dialC + Quaternion.Euler(0, i * 360f / n, 0) * Vector3.forward * (dialR + 1.5f);
                pos += new Vector3(Random.Range(-0.8f, 0.8f), 0, Random.Range(-0.8f, 0.8f));
                spots.Add(pos);
                Telegraph.Ring(pos, 0.7f, RunManager.WavePreview, Telegraph.Aim);
            }
            // 벽시계 그림자가 떨어져 나옴: 벽 쪽에서 검은 연기
            foreach (var pos in spots) FxBurst.Spawn(pos + Vector3.up * 0.6f, 1.2f, FxBurst.Kind.Spark, new Color(0.15f, 0.15f, 0.18f));
            yield return new WaitForSeconds(RunManager.WavePreview);
            var ch = Tables.Chapter(4);
            foreach (var pos in spots)
            {
                var e = EnemyFactory.RandomMinion(pos, ch);
                if (e) shadows.Add(e);
            }
            float nextShuffle = Time.time + 4f;
            while (true)
            {
                shadows.RemoveAll(e => !e || !e.health || !e.health.Alive);
                if (shadows.Count == 0) break;
                if (shuffle && Time.time >= nextShuffle && tiles.Count == 12)
                {
                    yield return Shuffled();
                    nextShuffle = Time.time + 12f;
                }
                yield return null;
            }
            yield return new WaitForSeconds(0.6f);
        }

        /// <summary>문자판 12칸이 2초 동안 자리를 바꿈 → 6초 뒤 내 발사 시각 숫자 칸 위면 안전 (느슨: 실패는 작은 피해)</summary>
        IEnumerator Shuffled()
        {
            ui?.Toast("문자판이 뒤섞여요 — 내 발사 시각 숫자 칸에 서세요");
            var home = new Vector3[12];
            for (int k = 0; k < 12; k++) home[k] = tiles[k] ? tiles[k].position : SectorPos(k);
            float t0 = Time.time;
            while (Time.time - t0 < 2f)
            {
                int a = Random.Range(0, 12), b = Random.Range(0, 12);
                if (a != b)
                {
                    (tiles[a], tiles[b]) = (tiles[b], tiles[a]);
                    (tileNum[a], tileNum[b]) = (tileNum[b], tileNum[a]);
                    StartCoroutine(Slide(tiles[a], home[a], 0.18f));
                    StartCoroutine(Slide(tiles[b], home[b], 0.18f));
                    Sfx.Play("e4_tick", 0.35f, 0.2f, 2);
                }
                yield return new WaitForSeconds(0.2f);
            }
            // 기다리는 동안 내 발사 시각 칸을 흐리게 표시하진 않음 (스스로 읽기) — 남은 시간만 링으로
            var ring = Telegraph.Ring(dialC + Vector3.up * 0.04f, dialR + 1f, ShuffleWait, Telegraph.Aim);
            yield return new WaitForSeconds(ShuffleWait);
            if (ring) ring.Kill();
            if (!p || !p.health.Alive) yield break;
            var d = Effects.Flat(p.transform.position - dialC);
            float ang = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg; if (ang < 0) ang += 360f;
            int k2 = Mathf.RoundToInt(ang / 30f) % 12;
            int stand = tileNum[k2];
            bool onDial = d.magnitude > 1.2f && d.magnitude < dialR + 1.6f;
            if (onDial && stand == p.rev.next)
            {
                ui?.Toast("안전!");
                FxBurst.Spawn(p.transform.position + Vector3.up, 1.2f, FxBurst.Kind.Gold, Color.white);
            }
            else
            {
                CameraFollow.Shake(0.2f);
                p.health.TakeDamage(p.health.maxHp * ShuffleFailFrac);
                ui?.Toast($"{stand}시 칸 — 발사 시각은 {p.rev.next}시");
            }
        }

        Vector3 SectorPos(int k) => dialC + Quaternion.Euler(0, k * 30f, 0) * Vector3.forward * dialR;

        static IEnumerator Slide(Transform t, Vector3 to, float dur)
        {
            if (!t) yield break;
            var from = t.position;
            for (float u = 0; u < 1f; u += Time.deltaTime / dur)
            {
                if (!t) yield break;
                t.position = Vector3.Lerp(from, to, Mathf.SmoothStep(0, 1, u)) + Vector3.up * Mathf.Sin(u * Mathf.PI) * 0.25f;
                yield return null;
            }
            if (t) t.position = to;
        }

        // ---------- 끝 ----------
        IEnumerator Ending()
        {
            var grand = F("GrandClock");
            var gAt = grand ? grand.position : Origin;
            CameraFollow.FocusOn(gAt + Vector3.back * 2f, 0.85f);
            var ov = StoryOverlay.I;
            // 고양이는 괘종시계를 바라보는 자리로
            var watch = gAt + Vector3.back * 6f;
            if (ov) yield return ov.FadeBlack(0, 1, 0.4f);
            p.Teleport(new Vector3(watch.x, Origin.y + 0.3f, watch.z));
            if (p.model) p.model.rotation = Quaternion.LookRotation(Vector3.forward);
            if (ov) yield return ov.FadeBlack(1, 0, 0.5f);
            // 시계가 하나씩 멈춤 → 째깍이 줄어듦
            var left = new List<Transform>();
            foreach (var pd in pendulums) if (!stopped.Contains(pd) && !IsGrand(pd)) left.Add(pd);
            for (int i = 0; i < left.Count; i++)
            {
                StartCoroutine(StopPendulum(left[i]));
                Drain((0.92f - desatTarget) / Mathf.Max(1, left.Count - i), i >= left.Count - 2);   // 남은 시계가 다 멈추면 거의 흑백 · 마지막 두 램프도 꺼짐
                Snd("e4_tick_2", 0.5f * (1f - (float)i / Mathf.Max(1, left.Count)) + 0.1f);
                if (tick) tick.volume = Mathf.Lerp(0.45f, 0.12f, (float)(i + 1) / left.Count);
                yield return new WaitForSeconds(Mathf.Lerp(0.5f, 0.22f, (float)i / Mathf.Max(1, left.Count)));
            }
            yield return new WaitForSeconds(0.6f);
            // 노인 그림자가 괘종시계 바늘로 손을 뻗음
            var old = F("OldManShadow");
            var arm = old ? Child(old, "ArmPivot") : null;
            StartCoroutine(ArmUp(arm, 1.4f));
            yield return GrandHandsTo(11, 59, 2.4f);
            desatTarget = 1f;                                                    // 11:59 — 흑백 + 빨간 숄만
            while (warmOff < warm.Count) Drain(0f);
            yield return Say(Old + "::여기서 멈추자.", Old + "::더 가면... 그 사람이 정말 가 버리니까.");
            p.InputLocked = true;
            // 정지 · 정적
            if (grand) { var gp = Child(grand, "Pendulum"); if (gp) yield return StopPendulum(gp); }
            if (tick) { Sfx.StopLoop(tick, 0.3f); tick = null; }
            if (rain) rain.volume = 0.12f;
            Music.Duck(0.9f, 0.6f);
            Snd("e4_tick_2", 0.9f);
            yield return new WaitForSeconds(1.6f);
            yield return Say("고양이#의문::...11시 59분.");
        }

        bool IsGrand(Transform pd)
        {
            var g = F("GrandClock");
            return g && pd.IsChildOf(g);
        }

        static IEnumerator ArmUp(Transform arm, float dur)
        {
            if (!arm) yield break;
            var from = arm.localRotation;
            var to = from * Quaternion.Euler(-82f, 0, 0);                   // 손이 괘종시계 바늘에 닿게 (−70°면 0.1m 모자람)
            for (float t = 0; t < 1f; t += Time.deltaTime / dur)
            {
                if (!arm) yield break;
                arm.localRotation = Quaternion.Slerp(from, to, Mathf.SmoothStep(0, 1, t));
                yield return null;
            }
        }

        /// <summary>괘종시계 바늘을 h:m으로 (문자판 앞 = 로컬 -z 쪽, 바늘은 로컬 z축으로 돎 — 키트에 맞춰 조정)</summary>
        IEnumerator GrandHandsTo(int h, int m, float dur)
        {
            var g = F("GrandClock");
            var hh = g ? Child(g, "HourHand") : null;
            var mh = g ? Child(g, "MinuteHand") : null;
            if (!hh && !mh) { yield return new WaitForSeconds(dur); yield break; }
            var h0 = hh ? hh.localRotation : Quaternion.identity;
            var m0 = mh ? mh.localRotation : Quaternion.identity;
            var h1 = Quaternion.Euler(0, 0, -((h % 12) + m / 60f) * 30f);
            var m1 = Quaternion.Euler(0, 0, -m * 6f);
            for (float t = 0; t < 1f; t += Time.deltaTime / dur)
            {
                float k = 1f - Mathf.Pow(1f - t, 3f);                            // 점점 느려지다 붙잡힘
                if (hh) hh.localRotation = Quaternion.Slerp(h0, h1, k);
                if (mh) mh.localRotation = Quaternion.Slerp(m0, m1, k);
                yield return null;
            }
            if (hh) hh.localRotation = h1;
            if (mh) mh.localRotation = m1;
            Snd("e4_clank", 0.7f);
            CameraFollow.Shake(0.08f);
        }

        /// <summary>연습 중 'Enter 길게': 하던 걸 멈추고 바로 보스 방으로</summary>
        void Quit()
        {
            if (finished) return;
            StopAllCoroutines();
            StartCoroutine(Finish(true));
        }

        IEnumerator Finish(bool ok)
        {
            if (pr) pr.Stop();                                    // 연습: 체력 되돌림 · 표시 치움 (Enter 끝내기도 막음)
            var ov = StoryOverlay.I;
            if (ok && ov) yield return ov.FadeWhite(0, 1, practice ? 0.4f : 1.1f);
            CameraFollow.FocusOn(null, 0f);
            Cleanup();
            p.InputLocked = false;
            p.health.invulnerable = false;
            finished = true;
            done?.Invoke();
            CameraFollow.SnapZoom(1f);
            Music.Duck(0f, 0.8f);
            if (ok && ov) yield return ov.FadeWhite(1, 0, practice ? 0.5f : 0.9f);
            Destroy(gameObject);
        }

        void Cleanup()
        {
            if (P) P.OnFired -= OnPlayerFired;
            seqTargets.Clear(); gearHour.Clear(); needTarget = null; needHour = 0;
            foreach (var e in shadows) if (e) Destroy(e.gameObject);
            shadows.Clear();
            foreach (var g in temp) if (g) Destroy(g);
            temp.Clear();
            if (rain) { Sfx.StopLoop(rain, 0.6f); rain = null; }
            if (tick) { Sfx.StopLoop(tick, 0.4f); tick = null; }
            if (map) Destroy(map.gameObject);
            Night_Off();
            Shader.SetGlobalFloat(DesatId, 0f);
            RunManager.StoryHold = false;
        }

        void OnDestroy() { if (!finished) Cleanup(); }

        void Update()
        {
            if (!finished)
            {
                desatNow = Mathf.MoveTowards(desatNow, desatTarget, Time.deltaTime * 0.35f);
                Shader.SetGlobalFloat(DesatId, desatNow);
            }
            if (!finished && rm && rm.runOver) { finished = true; Cleanup(); Destroy(gameObject); return; }
            if (finished || !map) return;
            // 공방의 진자들: 멈추기 전까지 저마다 박자로 흔들림
            for (int i = 0; i < pendulums.Count; i++)
            {
                var pd = pendulums[i];
                if (!pd || stopped.Contains(pd)) continue;
                float sp = 2.2f + (i % 5) * 0.35f;
                pd.localRotation = pendRest[pd] * Quaternion.Euler(0, 0, Mathf.Sin(Time.time * sp + i) * 14f);
            }
            // 돌아가는 톱니
            foreach (var kv in gearSpin) if (kv.Value && gearDone.Contains(kv.Key)) kv.Value.Rotate(Vector3.forward, 120f * Time.deltaTime, Space.Self);
        }

        // ---------- 도우미 ----------
        static PlayerController P => PlayerController.Instance;
        static void Snd(string key, float vol) { if (Sfx.Has(key)) Sfx.Play(key, vol, 0f); }

        IEnumerator Say(params string[] lines)
        {
            if (practice) yield break;                            // 연습: 대사 없음
            bool d = false;
            ui.Talk(Old, lines, () => d = true);
            while (!d) yield return null;
        }

        IEnumerator WaitNear(Vector3 at, float r)
        {
            while (Effects.Flat(p.transform.position - at).magnitude > r) yield return null;
        }

        /// <summary>개발용: 판에서 바로 미리보기 (rm 없이)</summary>
        public static void DevPreview() { Play(null, null); }
    }
}
