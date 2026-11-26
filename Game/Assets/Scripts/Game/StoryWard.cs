// 스토리 맵 5 '11:59의 병실' (2026-11-26 사용자 확정 · 대사 확정, HANDOFF.md "2026-11-26 — 5장 스토리 · 엔딩 기획 확정")
//   판 안에서 처음 5장 보스 문에 들어서면 한 번, 지금 이 순간 시계공이 누운 밤의 병실로 → 끝나면 시간의 주인 방으로 바로 이어짐.
//   침대엔 노인 그림자, 구석엔 지켜보기만 하는 사신 그림자. 보스 기믹을 압박 없이 익힘:
//   ① 죽음의 시각: 벽시계 바늘이 한 시각을 가리킴 → 그 시각 탄으로 시계를 맞힘 (2번, 틀린 탄은 튕길 뿐)
//   ② 시간 게이지: 링거 방울이 떨어질 때마다 남은 시간이 샘 → 바닥의 모래시계를 주워 버팀 (실패해도 다시)
//   ③ 그림자 전투 2번 + 숫자 인장(느슨하게: 그 시각 탄을 쏘고 인장 위에 서기, 실패는 작은 피해)
//   끝: 초침이 12로 다가감 → 고양이가 벽시계를 쏴서 멈춤(거부가 가장 강한 순간) → 사신 그림자가 처음 고개를 돌림 → 하얗게 → 보스 방
// 맵 = Resources/Story/StoryWard.prefab (블렌더 키트, Editor/StoryWardBuilder.cs). 곡 = bgm_story_5 (Gemini)
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    public class StoryWard : MonoBehaviour
    {
        public const string Key = "storymap_ward";
        const string Old = "???";
        static readonly Vector3 Origin = new(-1600, 0, -1400);
        const float HourTime = 12f, GaugeStart = 12f, GaugeMax = 20f, GaugeSurvive = 24f, GlassGain = 6f, GlassEvery = 5f, GlassLife = 8f, DripEvery = 1f, SealEvery = 16f, SealTime = 18f, SealFailFrac = 0.04f;
        static readonly int[] DeathHours = { 3, 8 };
        static readonly Color Gold = new(1f, 0.85f, 0.4f), Pale = new(0.75f, 0.78f, 0.9f);
        static readonly Color NightBg = new(0.05f, 0.06f, 0.11f);

        public static bool Pending(int chapter) => chapter == 5 && !Meta.StorySeen(Key);

        /// <summary>practice = 보스 패턴 연습 (StoryPractice): 대사 · 연출 없이 기믹만, 기록 안 함</summary>
        public static void Play(RunManager rm, System.Action done, bool practice = false)
        {
            var sm = new GameObject("StoryWard").AddComponent<StoryWard>();
            sm.rm = rm; sm.done = done; sm.practice = practice;
        }

        RunManager rm;
        System.Action done;
        PlayerController p;
        GameUI ui;
        Transform map, hourHand, minuteHand, secondHand, headPivot;
        Camera cam;
        CameraClearFlags oldFlags;
        Color oldBg, oldAmbient;
        Light sun; Color sunColor; float sunIntensity; Quaternion sunRot; LightShadows sunShadows; float sunStrength;
        AudioSource tick;
        readonly List<Enemy> shadows = new();
        readonly List<GameObject> temp = new();
        bool finished;
        bool practice;                 // 보스 패턴 연습
        StoryPractice pr;
        float nextBeep;

        // 쏴서 맞히는 과녁 · 마지막 발사
        Health clockTarget; int needHour; bool hourHit;
        int lastSlot; float lastShotAt = -9f;

        Transform F(string name)
        {
            if (!map) return null;
            foreach (var t in map.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }
        Vector3 At(string name, Vector3 fallback) { var t = F(name); return t ? t.position : fallback; }

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

            var prefab = Resources.Load<GameObject>("Story/StoryWard");
            if (!prefab) { Debug.LogWarning("[StoryWard] Story/StoryWard 프리팹 없음 — 건너뜀"); yield return Finish(false); yield break; }
            map = Instantiate(prefab, Origin, Quaternion.identity).transform;
            Night_On();
            var wc = F("WallClock");
            if (wc)
            {
                hourHand = FindUnder(wc, "HourHand"); minuteHand = FindUnder(wc, "MinuteHand"); secondHand = FindUnder(wc, "SecondHand");
            }
            headPivot = F("HeadPivot");
            SetClock(11, 59, 40);
            p.Teleport(At("Spawn", Origin) + Vector3.up * 0.3f);
            CameraFollow.SnapZoom(1f);
            if (P) P.OnFired += OnPlayerFired;
            if (practice) pr = StoryPractice.Begin(this, Quit);
            if (ov) yield return ov.FadeWhite(1, 0, practice ? 0.5f : 1.2f);
            Music.Duck(0f, 0.8f);
            Music.Play(StoryBgm.For(5, "stage"));
            if (Sfx.Has("e4_tick_1")) tick = Sfx.Loop("e4_tick_1", 0.35f);

            if (!practice) yield return Say("고양이#의문::...여긴...", "고양이#의문::약 냄새... 낯익어.");
            p.InputLocked = false;
            p.health.invulnerable = false;

            // 침대 · 사신 그림자를 봄 (연습: 도착 연출이라 건너뜀)
            if (!practice)
            {
                ui?.Toast("침대 쪽으로");
                yield return WaitNear(At("BedFront", Origin), 3.5f);
                p.InputLocked = true;
                CameraFollow.FocusOn(At("OldManLying", Origin), 0.8f);
                yield return Say("고양이#의문::...누구지.");
                var reaper = F("ReaperShadow");
                CameraFollow.FocusOn(reaper ? reaper.position : Origin, 0.8f);
                yield return Say("고양이#의문::...저건 처음부터 날 보고 있었어.");
                CameraFollow.FocusOn(null, 0f);
                p.InputLocked = false;
            }

            // ① 죽음의 시각
            for (int r = 0; r < DeathHours.Length;)
            {
                if (!practice)
                {
                    p.InputLocked = true;
                    CameraFollow.FocusOn(wc ? wc.position : Origin, 0.85f);
                    if (r == 0) yield return Say(Old + "::...몇 시... 지...");
                    CameraFollow.FocusOn(null, 0f);
                    p.InputLocked = false;
                }
                bool ok = false;
                yield return DeathHour(DeathHours[r], v => ok = v);
                if (!p || !p.health.Alive) yield break;
                if (!ok) { yield return new WaitForSeconds(0.8f); continue; }
                if (!practice)
                {
                    p.InputLocked = true;
                    yield return Say(Old + (r == 0 ? "::...아직... 아니야." : "::...조금만... 더."));
                    p.InputLocked = false;
                }
                r++;
                yield return new WaitForSeconds(0.5f);
            }

            // ② 시간 게이지 (링거)
            if (!practice)
            {
                p.InputLocked = true;
                CameraFollow.FocusOn(At("DripAt", Origin), 0.85f);
                yield return Say(Old + "::...시간이... 새고 있어.", "고양이#의문::(새는 걸 막으면 돼.)");
                CameraFollow.FocusOn(null, 0f);
                p.InputLocked = false;
            }
            while (true)
            {
                bool ok = false;
                yield return Gauge(v => ok = v);
                if (!p || !p.health.Alive) yield break;
                if (ok) break;
                yield return new WaitForSeconds(0.8f);
            }
            if (!practice)
            {
                p.InputLocked = true;
                yield return Say("고양이#의문::...버텼다.");
                p.InputLocked = false;
            }

            // ③ 그림자 전투 + 숫자 인장
            if (!practice)
            {
                p.InputLocked = true;
                CameraFollow.FocusOn(At("BattleCenter", Origin), 0.85f);
                yield return Say("고양이#결의::...그림자들이 침대로 몰려든다.");
                CameraFollow.FocusOn(null, 0f);
                p.InputLocked = false;
            }
            yield return Wave(4);
            yield return Wave(5);
            if (practice) { yield return Finish(true); yield break; }   // 연습: 끝 연출(벽시계 쏘기 · 사신 고개) · 기록 없음
            p.InputLocked = true;
            yield return Say("고양이#결의::아무도 못 데려가.");

            // 끝: 초침이 12로 → 벽시계를 쏨 → 사신이 고개를 돌림
            if (P) P.OnFired -= OnPlayerFired;
            yield return Ending();
            Meta.MarkStory(Key);
            yield return Finish(true);
        }

        static Transform FindUnder(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }

        // ---------- 밤 · 맵 ----------
        void Night_On()
        {
            cam = Camera.main;
            if (cam) { oldFlags = cam.clearFlags; oldBg = cam.backgroundColor; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = NightBg; }
            oldAmbient = RenderSettings.ambientLight;
            RenderSettings.ambientLight = new Color(0.27f, 0.3f, 0.42f);    // 주변광은 낮고 푸르게 — 창 달빛 한 방향이 읽히게 (2026-11-26 사용자: 실내니까 광원 방향 맞춤 · 전체 밝기 줄여 달빛 느낌)
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional && l.enabled) { sun = l; break; }
            if (sun) { sunColor = sun.color; sunIntensity = sun.intensity; sunRot = sun.transform.rotation; sunShadows = sun.shadows; sunStrength = sun.shadowStrength; sun.color = new Color(0.72f, 0.8f, 1f); sun.intensity = sunIntensity * 0.6f; sun.transform.rotation = Quaternion.Euler(40f, -146f, 0f); sun.shadows = LightShadows.Soft; sun.shadowStrength = 0.9f; }   // 북동쪽 창(맵 (6.5, 9.7))에서 방 안으로 — 그림자는 남서쪽으로 길게
        }

        void Night_Off()
        {
            if (cam) { cam.clearFlags = oldFlags; cam.backgroundColor = oldBg; }
            RenderSettings.ambientLight = oldAmbient;
            if (sun) { sun.color = sunColor; sun.intensity = sunIntensity; sun.transform.rotation = sunRot; sun.shadows = sunShadows; sun.shadowStrength = sunStrength; }
        }

        /// <summary>벽시계 바늘 (로컬 z축, 0° = 12시)</summary>
        void SetClock(int h, int m, float s)
        {
            if (hourHand) hourHand.localRotation = Quaternion.Euler(0, 0, -((h % 12) + m / 60f) * 30f);
            if (minuteHand) minuteHand.localRotation = Quaternion.Euler(0, 0, -m * 6f);
            if (secondHand) secondHand.localRotation = Quaternion.Euler(0, 0, -s * 6f);
        }

        IEnumerator HandsTo(int h, int m, float dur)
        {
            if (!hourHand && !minuteHand) { yield return new WaitForSeconds(dur); yield break; }
            var h0 = hourHand ? hourHand.localRotation : Quaternion.identity;
            var m0 = minuteHand ? minuteHand.localRotation : Quaternion.identity;
            var h1 = Quaternion.Euler(0, 0, -((h % 12) + m / 60f) * 30f);
            var m1 = Quaternion.Euler(0, 0, -m * 6f);
            for (float t = 0; t < 1f; t += Time.deltaTime / dur)
            {
                float k = Mathf.SmoothStep(0, 1, t);
                if (hourHand) hourHand.localRotation = Quaternion.Slerp(h0, h1, k);
                if (minuteHand) minuteHand.localRotation = Quaternion.Slerp(m0, m1, k);
                yield return null;
            }
            if (hourHand) hourHand.localRotation = h1;
            if (minuteHand) minuteHand.localRotation = m1;
            Snd("e4_clank", 0.5f);
        }

        // ---------- ① 죽음의 시각 ----------
        IEnumerator DeathHour(int h, System.Action<bool> result)
        {
            var wc = F("WallClock");
            yield return HandsTo(h, 0, 0.9f);
            var go = new GameObject("ClockTarget_" + h);
            go.transform.position = At("ClockTarget", Origin);
            go.transform.SetParent(map, true);
            temp.Add(go);
            clockTarget = Ch3Fx.Target(go, 1.1f, 2.6f);
            needHour = h; hourHit = false;
            var lb = BigLabel.Show(go.transform, new Vector3(0, 2.8f, 0), h.ToString(), Gold, 1.1f, BigLabel.Frame.Hour);
            Telegraph.Ring(go.transform.position + Vector3.up * 0.05f, 1.2f, 0.6f, Telegraph.Aim);
            Sfx.Play("e4_bell", 0.5f, 0f, 2);
            if (wc) FxBurst.Spawn(wc.position, 1.4f, FxBurst.Kind.Gold, Gold);
            ui?.Toast("벽시계가 가리킨 시각의 탄으로 시계 앞 표적을 맞히세요");
            float end = Time.time + HourTime;
            while (Time.time < end && !hourHit && p.health.Alive) { if (lb) lb.text = $"{h}  {Mathf.CeilToInt(end - Time.time)}"; yield return null; }
            needHour = 0;
            if (lb) Destroy(lb.gameObject);
            if (go) Destroy(go);
            clockTarget = null;
            if (hourHit)
            {
                if (wc) FxBurst.Spawn(wc.position, 2f, FxBurst.Kind.Gold, Color.white);
                Snd("e4_tick_2", 0.8f);
                CameraFollow.Shake(0.1f);
            }
            else ui?.Toast("시각이 지나갔어요 — 다시");
            yield return HandsTo(11, 59, 0.8f);
            result(hourHit);
        }

        // ---------- ② 시간 게이지 ----------
        IEnumerator Gauge(System.Action<bool> result)
        {
            float gauge = GaugeStart, survive = GaugeSurvive, nextDrip = Time.time, nextGlass = Time.time + 1f;
            var glasses = new List<(GameObject go, float born)>();
            var gaugeLb = BigLabel.Show(p.transform, new Vector3(0, 3.1f, 0), "", Gold, 0.9f, BigLabel.Frame.Timer);
            var drip = At("DripAt", Origin);
            var ivAnchor = new GameObject("IVLabel"); ivAnchor.transform.position = drip + Vector3.up * 1.2f; ivAnchor.transform.SetParent(map, true); temp.Add(ivAnchor);
            var ivLb = BigLabel.Show(ivAnchor.transform, Vector3.zero, "", Pale, 0.8f, BigLabel.Frame.Plain);
            ui?.Toast("링거가 떨어질 때마다 시간이 새요 — 모래시계를 주워 버티세요");
            var spots = new List<Vector3>();
            for (int i = 1; i <= 9; i++) { var t = F("HourglassSpot_" + i); if (t) spots.Add(t.position); }
            if (spots.Count == 0) spots.Add(At("BattleCenter", Origin));
            int lastSpot = -1;
            bool ok = false;
            while (p.health.Alive)
            {
                float dt = Time.deltaTime;
                gauge -= dt; survive -= dt;
                if (Time.time >= nextDrip)
                {
                    nextDrip = Time.time + DripEvery;
                    StartCoroutine(Drop(drip));
                }
                if (Time.time >= nextGlass)
                {
                    nextGlass = Time.time + GlassEvery;
                    for (int k = 0; k < 2; k++)
                    {
                        int s; do s = Random.Range(0, spots.Count); while (spots.Count > 1 && s == lastSpot);
                        lastSpot = s;
                        glasses.Add((SpawnGlass(spots[s]), Time.time));
                    }
                }
                for (int i = glasses.Count - 1; i >= 0; i--)
                {
                    var (go, born) = glasses[i];
                    if (!go) { glasses.RemoveAt(i); continue; }
                    go.transform.Rotate(0, 90f * dt, 0, Space.World);
                    if (Effects.Flat(p.transform.position - go.transform.position).magnitude <= 1.5f)
                    {
                        gauge = Mathf.Min(GaugeMax, gauge + GlassGain);
                        Snd("e5b_hourglass", 0.7f);
                        FxBurst.Spawn(go.transform.position + Vector3.up * 0.6f, 1.4f, FxBurst.Kind.Gold, Gold);
                        Destroy(go); glasses.RemoveAt(i);
                    }
                    else if (Time.time - born > GlassLife) { Destroy(go); glasses.RemoveAt(i); }
                }
                if (gaugeLb) gaugeLb.text = Mathf.CeilToInt(Mathf.Max(0, gauge)).ToString();
                if (ivLb) ivLb.text = $"버티기 {Mathf.CeilToInt(Mathf.Max(0, survive))}";
                if (gauge <= 0f) break;
                if (survive <= 0f) { ok = true; break; }
                yield return null;
            }
            foreach (var (go, _) in glasses) if (go) Destroy(go);
            if (gaugeLb) Destroy(gaugeLb.gameObject);
            if (ivLb) Destroy(ivLb.gameObject);
            if (!ok) { ui?.Toast("시간이 다 샜어요 — 다시"); CameraFollow.Shake(0.15f); }
            result(ok);
        }

        GameObject SpawnGlass(Vector3 at)
        {
            at.y = Origin.y;
            var pf = Resources.Load<GameObject>("Kit/Story/H_Hourglass");
            var go = pf ? Instantiate(pf) : GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            foreach (var c in go.GetComponentsInChildren<Collider>()) Destroy(c);
            go.transform.position = at;
            go.transform.localScale *= pf ? 1.5f : 0.5f;
            go.transform.SetParent(map, true);
            Telegraph.Ring(at + Vector3.up * 0.05f, 1.0f, 0.5f, Telegraph.Aim);
            return go;
        }

        IEnumerator Drop(Vector3 from)
        {
            var pf = Resources.Load<GameObject>("Kit/Story/H_IVDrop");
            if (!pf) yield break;
            var d = Instantiate(pf, from, Quaternion.identity, map);
            d.transform.localScale *= 1.6f;
            temp.Add(d);
            for (float t = 0; t < 0.45f && d; t += Time.deltaTime) { d.transform.position = from + Vector3.down * (t * t * 2.2f); yield return null; }
            if (d) Destroy(d);
            Snd("st_drip", 0.35f);
        }

        void OnPlayerFired(int slot, bool awake)
        {
            lastSlot = slot; lastShotAt = Time.time;
            if (clockTarget && Time.time - clockTarget.lastHitAt <= 0.001f)
            {
                if (slot == needHour) hourHit = true;
                else { FxBurst.Spawn(clockTarget.transform.position + Vector3.up * 1.2f, 0.8f, FxBurst.Kind.Spark, Color.white); Snd("e4_tick_1", 0.4f); }
            }
        }

        // ---------- ③ 그림자 전투 · 숫자 인장 ----------
        IEnumerator Wave(int n)
        {
            var center = At("BattleCenter", Origin);
            var spots = new List<Vector3>();
            for (int i = 0; i < n; i++)
            {
                var sp = F("ShadowSpawn_" + (i % 6 + 1));
                var pos = sp ? sp.position : center + Quaternion.Euler(0, i * 360f / n, 0) * Vector3.forward * 6f;
                pos += new Vector3(Random.Range(-0.8f, 0.8f), 0, Random.Range(-0.8f, 0.8f));
                spots.Add(pos);
                Telegraph.Ring(pos, 0.7f, RunManager.WavePreview, Telegraph.Aim);
            }
            foreach (var pos in spots) FxBurst.Spawn(pos + Vector3.up * 0.6f, 1.2f, FxBurst.Kind.Spark, new Color(0.15f, 0.12f, 0.2f));
            yield return new WaitForSeconds(RunManager.WavePreview);
            var ch = Tables.Chapter(5);
            foreach (var pos in spots) { var e = EnemyFactory.RandomMinion(pos, ch); if (e) shadows.Add(e); }
            float nextSeal = Time.time + 5f;
            Coroutine seal = null;
            bool sealing = false;
            while (true)
            {
                shadows.RemoveAll(e => !e || !e.health || !e.health.Alive);
                if (shadows.Count == 0 && !sealing) break;
                if (!sealing && Time.time >= nextSeal && shadows.Count > 0)
                {
                    sealing = true;
                    seal = StartCoroutine(Seals(() => { sealing = false; nextSeal = Time.time + SealEvery; }));
                }
                yield return null;
            }
            if (seal != null) StopCoroutine(seal);
            yield return new WaitForSeconds(0.6f);
        }

        /// <summary>숫자 인장 3개 (느슨하게): 순서대로 그 숫자 시각 탄을 쏜 뒤(1.5초 안) 인장 위에 서면 봉인. 실패는 남은 인장당 작은 피해</summary>
        IEnumerator Seals(System.Action done)
        {
            var center = At("BattleCenter", Origin);
            var nums = new List<int>();
            while (nums.Count < 3) { int k = Random.Range(1, 13); if (!nums.Contains(k)) nums.Add(k); }
            var spots = new List<Vector3>(); var marks = new List<GameObject>(); var labels = new List<BigLabel>();
            for (int i = 0; i < 3; i++)
            {
                var at = center + Quaternion.Euler(0, i * 120f + Random.Range(-25f, 25f), 0) * Vector3.forward * Random.Range(2.5f, 4.5f);
                at.y = Origin.y;
                spots.Add(at);
                var m = new GameObject("Seal_" + nums[i]); m.transform.position = at; m.transform.SetParent(map, true); temp.Add(m);
                var c = i == 0 ? Gold : Pale;
                var lb = BigLabel.Show(m.transform, new Vector3(0, 1.6f, 0), nums[i].ToString(), c, 1.1f, BigLabel.Frame.Hour);
                lb?.Corner((i + 1).ToString(), c);
                labels.Add(lb);
                marks.Add(m);
                Telegraph.Ring(at + Vector3.up * 0.05f, 1.6f, SealTime, i == 0 ? Telegraph.Aim : Telegraph.Aim);
            }
            ui?.Toast("숫자 인장 — 그 시각 탄을 쏘고 인장 위에 서세요 (순서대로)");
            int doneN = 0; float end = Time.time + SealTime;
            while (Time.time < end && doneN < 3 && p.health.Alive)
            {
                var at = spots[doneN];
                bool on = Effects.Flat(p.transform.position - at).magnitude <= 1.6f;
                if (on && lastSlot == nums[doneN] && Time.time - lastShotAt < 1.5f)
                {
                    FxBurst.Spawn(at + Vector3.up * 0.4f, 1.8f, FxBurst.Kind.Gold, Gold);
                    Snd("e5b_seal", 0.6f);
                    if (marks[doneN]) Destroy(marks[doneN]);
                    doneN++;
                    if (doneN < 3 && labels[doneN]) labels[doneN].color = Gold;
                }
                yield return null;
            }
            foreach (var m in marks) if (m) Destroy(m);
            if (doneN >= 3)
            {
                ui?.Toast("봉인 — 그림자가 움츠러든다");
                foreach (var e in shadows) if (e && e.health && e.health.Alive) e.health.TakeDamage(e.health.maxHp * 0.35f);
            }
            else
            {
                ui?.Toast("봉인 실패");
                if (p.health.Alive) p.health.TakeDamage(p.health.maxHp * SealFailFrac * (3 - doneN));
            }
            done?.Invoke();
        }

        // ---------- 끝 ----------
        IEnumerator Ending()
        {
            var wc = F("WallClock");
            var wAt = wc ? wc.position : Origin;
            var ov = StoryOverlay.I;
            var stand = new Vector3(wAt.x, Origin.y, At("ClockTarget", Origin).z - 1.2f);
            if (ov) yield return ov.FadeBlack(0, 1, 0.4f);
            p.Teleport(stand + Vector3.up * 0.3f);
            if (p.model) p.model.rotation = Quaternion.LookRotation(Vector3.forward);
            CameraFollow.FocusOn(Vector3.Lerp(stand, new Vector3(wAt.x, Origin.y, wAt.z), 0.6f), 0.75f);
            if (ov) yield return ov.FadeBlack(1, 0, 0.5f);
            // 초침이 한 칸씩 12로
            Music.Duck(0.55f, 1f);
            for (int s = 52; s <= 56; s++)
            {
                SetClock(11, 59, s);
                Snd("e4_tick_2", 0.35f + (s - 52) * 0.1f);
                CameraFollow.Shake(0.02f);
                yield return new WaitForSeconds(0.9f);
            }
            yield return Say("고양이#결의::...안 돼.", "고양이#결의::거기까지 가게 두지 않아.");
            p.InputLocked = true;
            SetClock(11, 59, 57);
            Snd("e4_tick_2", 0.85f);
            yield return new WaitForSeconds(0.7f);
            // 총성 → 벽시계 멈춤
            Sfx.Play("shot", 1f, 0f);
            if (p.model) FxBurst.Spawn(p.transform.position + Vector3.up * 1.2f + p.model.forward * 0.8f, 1.2f, FxBurst.Kind.Spark, Gold);
            FxBurst.Spawn(wAt, 2.4f, FxBurst.Kind.Spark, Color.white);
            CameraFollow.Shake(0.35f);
            if (tick) { Sfx.StopLoop(tick, 0.05f); tick = null; }
            Music.Duck(1f, 0.2f);
            if (secondHand) secondHand.localRotation *= Quaternion.Euler(0, 0, 3f);   // 맞아서 살짝 비뚤어진 채 멈춤
            yield return new WaitForSeconds(1.4f);
            // 사신 그림자가 처음으로 고개를 돌림
            var reaper = F("ReaperShadow");
            if (reaper) CameraFollow.FocusOn(reaper.position, 0.75f);
            yield return new WaitForSeconds(0.6f);
            if (headPivot)
            {
                var from = headPivot.rotation;
                var d = Effects.Flat(p.transform.position - headPivot.position);
                var to = d.sqrMagnitude > 0.01f ? Quaternion.LookRotation(-d.normalized) : from;          // 머리 정면 = 로컬 −Z
                var localTo = Quaternion.Inverse(headPivot.parent.rotation) * to;
                var localFrom = headPivot.localRotation;
                var e = localTo.eulerAngles; localTo = Quaternion.Euler(0, e.y, 0);                     // 고개만 돌림
                for (float t = 0; t < 1f; t += Time.deltaTime / 1.6f)
                {
                    headPivot.localRotation = Quaternion.Slerp(localFrom, localTo, Mathf.SmoothStep(0, 1, t));
                    yield return null;
                }
            }
            yield return new WaitForSeconds(1.6f);
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
            foreach (var e in shadows) if (e) Destroy(e.gameObject);
            shadows.Clear();
            foreach (var g in temp) if (g) Destroy(g);
            temp.Clear();
            if (tick) { Sfx.StopLoop(tick, 0.4f); tick = null; }
            if (map) Destroy(map.gameObject);
            Night_Off();
            RunManager.StoryHold = false;
        }

        void OnDestroy() { if (!finished) Cleanup(); }

        void Update()
        {
            if (!finished && rm && rm.runOver) { finished = true; Cleanup(); Destroy(gameObject); return; }
            if (finished || !map) return;
            // 심전도 '삐' — 낮게, 일정하게
            if (Time.time >= nextBeep && tick) { nextBeep = Time.time + 1.5f; Snd("story_heartbeat", 0.18f); }
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

        public static void DevPreview() { Play(null, null); }
    }
}
