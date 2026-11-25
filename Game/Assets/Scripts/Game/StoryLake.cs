// 스토리 맵 3 '거울 같은 호숫가' (2026-11-25 사용자 확정 · 대사 컨펌)
//   판 안에서 처음 3장 보스 문에 들어서면 한 번, 맑은 한낮 호숫가 기억으로 빨려 들어감 → 끝나면 모비 글래스 방으로 바로 이어짐.
//   약속이 지켜진 뒤 그녀와 함께한 날(과거). 보스 기믹을 압박 없이 익힘:
//   ① 거울에 비친 시각: 잔교 끝 기둥 시계는 햇빛에 번져 안 보이고, 수면에 비친 좌우 반전 시계만 보임 → 실제 시각 탄을 물 위 동그라미에 → 보트 밧줄이 풀림 (실제 3시 → 5시)
//   ② 고래의 노래 = 그녀의 물수제비: 돌이 n번 튐(튈 때마다 물결 한 줄 · 통 소리, 숫자 없음) → 그 수 시각 탄을 돌이 멈춘 부표에 (3번 → 7번)
//   ③ 흩어진 기억 조각(느슨하게): 해가 기울며 수면에 비친 그림자들이 일어섬(전투 2번) + 호수에 숫자 연잎 3개(자기 숫자 시각 탄에만 피어남, 15초)
//      다 피우면 그림자 소멸, 못 피워도 작은 피해 후 이어 감
//   끝: 저물녘 두 그림자가 잔교에 나란히 앉아 해를 봄 → 카메라가 수면으로 내려가면 둘이 비치고, 다시 올라오면 잔교엔 청년만 → 하얗게 → 보스 방
// 맵 = Resources/Story/StoryLake.prefab (블렌더 키트, Editor/StoryLakeBuilder.cs). 곡 = bgm_story_3 (Gemini)
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    public class StoryLake : MonoBehaviour
    {
        public const string Key = "storymap_lake";
        const string Young = "???", Woman = "???";      // 청년 · 그녀 — 이름은 가림
        static readonly Vector3 Origin = new(1400, 0, -1400);              // 판 맵 · 다른 스토리 맵과 멀리 떨어진 자리
        const float WaterY = -0.15f;
        const float LilyTime = 15f, LilyFailFrac = 0.06f, ClockTime = 18f, SkipTime = 16f;
        static readonly int[] ClockReal = { 3, 5 };                          // 2026-11-25 사용자: 1번 실제 3시(물엔 9시) → 2번 실제 5시(물엔 7시)
        static readonly int[] SkipCount = { 3, 7 };
        static readonly Color Noon = new(0.62f, 0.8f, 0.98f), Dusk = new(0.98f, 0.62f, 0.42f);
        static readonly int SkyId = Shader.PropertyToID("_CatTangSkyColor");
        float nextRipple;

        /// <summary>이번 문이 처음 만나는 3장 보스 문이면 스토리 맵 3</summary>
        public static bool Pending(int chapter) => chapter == 3 && !Meta.StorySeen(Key);

        public static void Play(RunManager rm, System.Action done)
        {
            var sm = new GameObject("StoryLake").AddComponent<StoryLake>();
            sm.rm = rm; sm.done = done;
        }

        RunManager rm;
        System.Action done;
        PlayerController p;
        GameUI ui;
        Transform map;
        GameObject young, woman, youngSit, womanSit;
        Camera cam;
        CameraClearFlags oldFlags;
        Color oldBg, oldAmbient;
        Light sun; Color sunColor; float sunIntensity; Quaternion sunRot;
        float oldPlaneY;
        AudioSource amb;
        readonly List<Enemy> shadows = new();
        bool finished;
        // 쏴서 맞히는 과녁 (시각이 맞아야 함)
        Health target; int needHour; bool targetHit;
        Telegraph targetRing;
        readonly Dictionary<Health, int> lilyNum = new();
        readonly HashSet<Health> lilyDone = new();

        Transform M(string name) => map.Find("Marks/" + name);
        Transform G(string name) => map.Find("Geo/" + name);

        IEnumerator Start()
        {
            p = PlayerController.Instance;
            ui = FindFirstObjectByType<GameUI>();
            var ov = StoryOverlay.I;
            p.InputLocked = true;
            p.health.invulnerable = true;
            if (rm) rm.EnterStoryMap();
            Music.Duck(0.25f, 0.6f);
            if (ov) { yield return ov.Headache(0.6f, 1.0f); yield return ov.FadeWhite(0, 1, 0.9f); }

            var prefab = Resources.Load<GameObject>("Story/StoryLake");
            if (!prefab) { Debug.LogWarning("[StoryLake] Story/StoryLake 프리팹 없음 — 건너뜀"); yield return Finish(false); yield break; }
            map = Instantiate(prefab, Origin, Quaternion.identity).transform;
            Day_On();
            p.Teleport(M("Start").position + Vector3.up * 0.3f);
            CameraFollow.SnapZoom(1f);
            young = Spawn("L_ShadowYoungL", M("Young"));
            woman = Spawn("L_ShadowWomanL", M("Woman"));
            if (P) P.OnFired += OnPlayerFired;
            if (ov) yield return ov.FadeWhite(1, 0, 1.2f);
            Music.Duck(0f, 0.8f);
            Music.Play(StoryBgm.For(3, "stage"));                                // 전용 곡 (Gemini) — 맵 내내, 전투에서도
            if (Sfx.Has("st_lake")) amb = Sfx.Loop("st_lake", 0.5f);

            yield return Say("고양이#의문::....눈부시군.", "고양이#의문::물이... 하늘을 그대로 비추고 있어.");
            p.InputLocked = false;
            p.health.invulnerable = false;

            // ① 잔교 끝: 물에 비친 시각
            ui?.Toast("잔교 끝으로");
            yield return WaitNear(M("JettyEnd").position, 4.5f);
            p.InputLocked = true;
            CameraFollow.FocusOn(M("JettyEnd").position, 0.8f);
            yield return Say(Young + "::시계가 안 보여. 해가 너무 밝아.",
                             Woman + "::물을 봐. 거기 비쳐 있잖아.",
                             Woman + "::대신 물에 비친 건 거꾸로야.");
            CameraFollow.FocusOn(null, 0f);
            p.InputLocked = false;
            for (int i = 0; i < ClockReal.Length;)
            {
                bool ok = false;
                yield return ReflectedTime(ClockReal[i], r => ok = r);
                if (!p || !p.health.Alive) yield break;
                if (!ok) { ui?.Toast("물에 비친 건 거꾸로 — 다시"); yield return new WaitForSeconds(1f); continue; }
                p.InputLocked = true;
                if (i == 0) yield return Say(Young + "::...풀렸다.", Woman + "::이번엔 조금 헷갈릴걸?");
                else yield return Say(Young + "::...풀렸다.");
                p.InputLocked = false;
                i++;
                yield return new WaitForSeconds(0.6f);
            }
            StartCoroutine(BoatDrift());

            // ② 그녀의 물수제비
            p.InputLocked = true;
            CameraFollow.FocusOn(M("Throw").position, 0.8f);
            yield return Say(Woman + "::내가 몇 번 튀기는지 세어 봐.");
            CameraFollow.FocusOn(null, 0f);
            p.InputLocked = false;
            for (int i = 0; i < SkipCount.Length;)
            {
                bool ok = false;
                yield return SkipStone(SkipCount[i], M(i == 0 ? "BuoyA" : "BuoyB"), r => ok = r);
                if (!p || !p.health.Alive) yield break;
                if (!ok) { ui?.Toast("다시 세어 보자"); yield return new WaitForSeconds(0.8f); continue; }
                p.InputLocked = true;
                if (i == 0) yield return Say(Young + "::세 번.", Woman + "::다음엔 더 멀리 갈 거야.");
                else yield return Say(Woman + "::봐. 기다리면 다 돌아오잖아.", "고양이#의문::(기다리면... 돌아온다?)");
                p.InputLocked = false;
                i++;
                yield return new WaitForSeconds(0.6f);
            }

            // ③ 해가 기울며 수면에 비친 그림자들이 일어섬
            StartCoroutine(SunGoesDown(70f));
            p.InputLocked = true;
            CameraFollow.FocusOn(M("Arena").position + Vector3.forward * 6f, 0.85f);
            yield return Say("고양이#결의::...물에 비친 게 일어서고 있다.");
            CameraFollow.FocusOn(null, 0f);
            p.InputLocked = false;
            yield return Wave(4);
            yield return Wave(5, lilies: true);

            // 끝: 저물녘 잔교 · 두 그림자 → 수면 → 청년만
            p.InputLocked = true;
            if (P) P.OnFired -= OnPlayerFired;
            Music.Duck(0.35f, 1.2f);
            yield return Ending();
            Meta.MarkStory(Key);
            yield return Finish(true);
        }

        // ---------- 한낮 → 저녁 ----------
        void Day_On()
        {
            cam = Camera.main;
            if (cam) { oldFlags = cam.clearFlags; oldBg = cam.backgroundColor; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Noon; }
            oldAmbient = RenderSettings.ambientLight;
            RenderSettings.ambientLight = Color.Lerp(oldAmbient, new Color(0.95f, 0.95f, 0.9f), 0.45f);
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional && l.enabled) { sun = l; break; }
            if (sun) { sunColor = sun.color; sunIntensity = sun.intensity; sunRot = sun.transform.rotation; sun.color = new Color(1f, 0.97f, 0.9f); sun.intensity = sunIntensity * 1.1f; }
            MirrorLayers.HideFromMain();
            oldPlaneY = MirrorReflection.PlaneY;
            MirrorReflection.PlaneY = Origin.y + WaterY;
            MirrorReflection.Ensure(true);                                   // 잔잔한 호수 = 거울 바닥
        }

        void Day_Off()
        {
            if (cam) { cam.clearFlags = oldFlags; cam.backgroundColor = oldBg; }
            RenderSettings.ambientLight = oldAmbient;
            if (sun) { sun.color = sunColor; sun.intensity = sunIntensity; sun.transform.rotation = sunRot; }
            MirrorReflection.Ensure(false);
            MirrorReflection.PlaneY = oldPlaneY;
            Shader.SetGlobalColor(SkyId, new Color(0, 0, 0, 0));
        }

        IEnumerator SunGoesDown(float dur)
        {
            var c0 = sun ? sun.color : Color.white;
            float i0 = sun ? sun.intensity : 1f;
            var r0 = sun ? sun.transform.rotation : Quaternion.identity;
            var r1 = Quaternion.Euler(18f, 20f, 0f);
            var a0 = RenderSettings.ambientLight;
            for (float t = 0; t < 1f && !finished; t += Time.deltaTime / dur)
            {
                float k = Mathf.SmoothStep(0, 1, t);
                if (cam) cam.backgroundColor = Color.Lerp(Noon, Dusk, k);
                if (sun) { sun.color = Color.Lerp(c0, new Color(1f, 0.62f, 0.38f), k); sun.intensity = Mathf.Lerp(i0, i0 * 0.8f, k); sun.transform.rotation = Quaternion.Slerp(r0, r1, k * 0.7f); }
                RenderSettings.ambientLight = Color.Lerp(a0, new Color(0.85f, 0.62f, 0.55f), k * 0.6f);
                yield return null;
            }
        }

        // ---------- ① 물에 비친 시각 ----------
        IEnumerator ReflectedTime(int real, System.Action<bool> result)
        {
            int shown = real == 12 || real == 6 ? real : 12 - real;
            var at = M("ClockWater").position;
            at.y = Origin.y + WaterY + 0.03f;
            var hand = Ch3Fx.FloorClock(at, 2.6f, out var clock);
            clock.transform.position = at;
            clock.transform.localScale = new Vector3(-1f, 1f, 1f);           // 좌우 반전 (12시 표시는 그대로 위)
            hand.localRotation = Quaternion.Euler(0, -shown * 30f, 0);
            clock.transform.SetParent(map, true);
            var numerals = ReflectedClock.Show(at, 2.6f, 999f);
            Enemy.Fx3("MirrorFlash", at + Vector3.up * 0.3f, 1f);
            var ring = MakeTarget(M("Circle"));
            ui?.Toast($"물에 비친 시계는 좌우가 뒤집혀 있어요 — 실제 시각에 쏜 탄을 물 위 동그라미에 맞히세요");
            needHour = real; targetHit = false;
            float end = Time.time + ClockTime;
            while (Time.time < end && !targetHit && p.health.Alive) yield return null;
            needHour = 0;
            if (numerals) numerals.Sink();
            Destroy(clock, 0.4f);
            if (ring) Destroy(ring.gameObject);
            if (targetRing) { targetRing.Kill(); targetRing = null; }
            if (targetHit)
            {
                var c = M("Circle").position;
                RippleRing.Spawn(c, 6f, 0f, 5f, 0.4f, Ch3Fx.Teal);
                FxBurst.Spawn(c + Vector3.up * 0.4f, 1.6f, FxBurst.Kind.Gold, Ch3Fx.Gold);
                Snd("st_skip", 0.8f);
            }
            result(targetHit);
        }

        /// <summary>물 위 과녁: 금빛 동그라미 + 고양이 탄만 맞는 기둥</summary>
        Health MakeTarget(Transform at)
        {
            if (!at) return null;
            var go = new GameObject("WaterTarget");
            go.transform.position = new Vector3(at.position.x, Origin.y + WaterY, at.position.z);
            go.transform.SetParent(map, true);
            target = Ch3Fx.Target(go, 0.9f, 2.4f);
            Ch3Fx.Prim(PrimitiveType.Cylinder, "Ring", go.transform, Vector3.up * 0.04f, new Vector3(1.8f, 0.02f, 1.8f), Ch3Fx.Gold, 1f);
            if (targetRing) targetRing.Kill();
            targetRing = Telegraph.Ring(go.transform.position + Vector3.up * 0.05f, 1.4f, 9999f, Telegraph.Aim);
            return target;
        }

        void OnPlayerFired(int slot, bool awake)
        {
            // 연잎: 자기 숫자 탄에만 피어남
            foreach (var kv in lilyNum)
            {
                var h = kv.Key;
                if (!h || lilyDone.Contains(h) || Time.time - h.lastHitAt > 0.001f) continue;
                if (slot == kv.Value) { lilyDone.Add(h); StartCoroutine(Bloom(h)); }
                else FxBurst.Spawn(h.transform.position + Vector3.up * 0.6f, 0.7f, FxBurst.Kind.Spark, Ch3Fx.Light);
            }
            if (needHour == 0 || targetHit || !target) return;
            if (Time.time - target.lastHitAt > 0.001f) return;
            if (slot == needHour) targetHit = true;
            else { FxBurst.Spawn(target.transform.position + Vector3.up * 1f, 0.8f, FxBurst.Kind.Spark, Ch3Fx.Light); Snd("st_skip", 0.4f); }
        }

        IEnumerator BoatDrift()
        {
            var boat = G("Boat");
            if (!boat) yield break;
            Snd("st_skip", 0.6f);
            var from = boat.position;
            var to = from + new Vector3(2.2f, 0, 1.2f);
            for (float t = 0; t < 1f; t += Time.deltaTime / 6f)
            {
                if (!boat) yield break;
                boat.position = Vector3.Lerp(from, to, Mathf.SmoothStep(0, 1, t)) + Vector3.up * Mathf.Sin(Time.time * 1.4f) * 0.04f;
                boat.rotation = Quaternion.Euler(0, Mathf.Lerp(0, 18f, t), Mathf.Sin(Time.time * 1.1f) * 2f);
                yield return null;
            }
        }

        // ---------- ② 물수제비 ----------
        IEnumerator SkipStone(int n, Transform buoy, System.Action<bool> result)
        {
            if (!buoy) { result(true); yield break; }
            var hand = M("Throw").position + Vector3.up * 1.3f;
            var end = buoy.position;
            end.y = Origin.y + WaterY;
            var stonePrefab = Resources.Load<GameObject>("Kit/Story/L_SkipStone");
            var stone = stonePrefab ? Instantiate(stonePrefab, hand, Quaternion.identity, map) : Ch3Fx.Prim(PrimitiveType.Sphere, "Stone", map, map.InverseTransformPoint(hand), new Vector3(0.18f, 0.06f, 0.14f), new Color(0.6f, 0.6f, 0.65f));
            Snd("e_swing", 0.4f);
            // 튀는 자리: 손 → 부표까지 n번, 점점 짧게
            var pts = new List<Vector3>();
            float total = 0, seg = 1f;
            for (int i = 0; i < n; i++) { total += seg; seg *= 0.82f; }
            float acc = 0; seg = 1f;
            var startW = new Vector3(hand.x, end.y, hand.z);
            for (int i = 0; i < n; i++) { acc += seg; seg *= 0.82f; pts.Add(Vector3.Lerp(startW, end, acc / total)); }
            Vector3 prev = hand;
            for (int i = 0; i < pts.Count; i++)
            {
                var a = prev; var b = pts[i];
                float h = i == 0 ? 1.2f : 0.8f * Mathf.Pow(0.8f, i);
                float dur = i == 0 ? 0.55f : 0.42f * Mathf.Pow(0.93f, i);
                for (float t = 0; t < 1f; t += Time.deltaTime / dur)
                {
                    if (stone) { stone.transform.position = Vector3.Lerp(a, b, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * h; stone.transform.Rotate(0, 900f * Time.deltaTime, 0, Space.World); }
                    yield return null;
                }
                // 튈 때마다 물결 한 줄 · 통 소리 (숫자 없음)
                RippleRing.Spawn(b + Vector3.up * 0.02f, 3.2f, 0f, 2.4f, 0.18f, Ch3Fx.Light);
                Snd("st_skip", 0.85f);
                prev = b;
            }
            if (stone) Destroy(stone, 0.2f);
            // 돌이 멈춘 부표가 빛남
            var bt = MakeTarget(buoy);
            var glow = Ch3Fx.Prim(PrimitiveType.Sphere, "BuoyGlow", buoy, Vector3.up * 1.1f, Vector3.one * 0.6f, Ch3Fx.Gold, 0f);
            ui?.Toast("돌이 튄 횟수만큼의 시각에 쏜 탄을 부표에 맞히세요");
            needHour = n; targetHit = false;
            float endT = Time.time + SkipTime;
            while (Time.time < endT && !targetHit && p.health.Alive)
            {
                if (glow) glow.transform.localScale = Vector3.one * (0.6f + Mathf.Sin(Time.time * 8f) * 0.08f);
                yield return null;
            }
            needHour = 0;
            if (glow) Destroy(glow);
            if (bt) Destroy(bt.gameObject);
            if (targetRing) { targetRing.Kill(); targetRing = null; }
            if (targetHit) { FxBurst.Spawn(buoy.position + Vector3.up * 1.2f, 1.6f, FxBurst.Kind.Gold, Ch3Fx.Gold); Snd("e4_bell_1", 0.6f); }
            result(targetHit);
        }

        // ---------- ③ 그림자 · 연잎 ----------
        IEnumerator Wave(int n, bool lilies = false)
        {
            var c = M("Arena").position;
            var spots = new List<Vector3>();
            for (int i = 0; i < n; i++)
            {
                Vector3 pos; int tries = 0;
                do pos = c + Quaternion.Euler(0, Random.Range(0f, 360f), 0) * Vector3.forward * Random.Range(3f, 7.5f);
                while (Effects.Flat(pos - p.transform.position).magnitude < 4.5f && ++tries < 12);
                spots.Add(pos);
                Telegraph.Ring(pos, 0.7f, RunManager.WavePreview, Telegraph.Aim);
                Enemy.Fx3("MirrorFlash", pos + Vector3.up * 0.2f, 0.7f);
            }
            yield return new WaitForSeconds(RunManager.WavePreview);
            var ch = Tables.Chapter(3);
            foreach (var pos in spots)
            {
                var e = EnemyFactory.RandomMinion(pos, ch);
                if (e) shadows.Add(e);
            }
            if (lilies)
            {
                yield return new WaitForSeconds(2f);
                yield return Lilies();
            }
            while (true)
            {
                shadows.RemoveAll(e => !e || !e.health || !e.health.Alive);
                if (shadows.Count == 0) break;
                yield return null;
            }
            yield return new WaitForSeconds(0.6f);
        }

        IEnumerator Lilies()
        {
            var nums = new List<int>();
            for (int i = 1; i <= 12; i++) nums.Add(i);
            for (int i = nums.Count - 1; i > 0; i--) { int j = Random.Range(0, i + 1); (nums[i], nums[j]) = (nums[j], nums[i]); }
            lilyNum.Clear(); lilyDone.Clear();
            var labels = new List<GameObject>();
            var pads = new List<GameObject>();
            var padPrefab = Resources.Load<GameObject>("Kit/Story/L_LilyPad");
            for (int k = 0; k < 3; k++)
            {
                var at = M("Lily" + k);
                if (!at) continue;
                var go = new GameObject("NumberLily_" + nums[k]);
                go.transform.position = new Vector3(at.position.x, Origin.y + WaterY, at.position.z);
                go.transform.SetParent(map, true);
                if (padPrefab) pads.Add(Instantiate(padPrefab, go.transform.position, Quaternion.Euler(0, Random.Range(0f, 360f), 0), go.transform));
                var h = Ch3Fx.Target(go, 0.8f, 2.2f);
                lilyNum[h] = nums[k];
                var lb = BossFx.Label(go.transform, new Vector3(0, 2.2f, 0), nums[k].ToString(), Ch3Fx.Gold, 0.11f);
                if (lb) labels.Add(lb.gameObject);
                RippleRing.Spawn(go.transform.position + Vector3.up * 0.02f, 3f, 0f, 2f, 0.2f, Ch3Fx.Pink);
            }
            ui?.Toast("숫자 연잎 — 연잎 숫자와 같은 시각 탄으로 맞히면 피어나요");
            float end = Time.time + LilyTime;
            while (Time.time < end && lilyDone.Count < lilyNum.Count && p.health.Alive) yield return null;
            if (lilyDone.Count >= lilyNum.Count && lilyNum.Count > 0)
            {
                ui?.Toast("연잎이 다 피었다 — 그림자가 흩어진다");
                foreach (var e in shadows) if (e && e.health && e.health.Alive) { Enemy.Fx3("Stardust", e.transform.position + Vector3.up, 1f); e.health.TakeDamage(e.health.maxHp * 10f); }
            }
            else
            {
                ui?.Toast("연잎이 다시 오므라든다");
                CameraFollow.Shake(0.2f);
                p.health.TakeDamage(p.health.maxHp * LilyFailFrac);
            }
            yield return new WaitForSeconds(1.2f);
            foreach (var l in labels) if (l) Destroy(l);
            lilyNum.Clear();
        }

        IEnumerator Bloom(Health h)
        {
            var go = h.gameObject;
            foreach (var lb in go.GetComponentsInChildren<TextMesh>()) lb.gameObject.SetActive(false);
            var flower = Resources.Load<GameObject>("Kit/Story/L_LilyFlower");
            Enemy.Fx3("Stardust", go.transform.position + Vector3.up * 0.4f, 0.8f);
            FxBurst.Spawn(go.transform.position + Vector3.up * 0.5f, 1.3f, FxBurst.Kind.Gold, Ch3Fx.Pink);
            Snd("e4_bell_1", 0.4f);
            if (!flower) yield break;
            var f = Instantiate(flower, go.transform.position, Quaternion.identity, go.transform);
            for (float t = 0; t < 1f; t += Time.deltaTime / 0.5f) { if (f) f.transform.localScale = Vector3.one * Mathf.SmoothStep(0.2f, 1f, t); yield return null; }
        }

        // ---------- 끝 ----------
        IEnumerator Ending()
        {
            var end = M("JettyEnd").position;
            CameraFollow.FocusOn(end, 0.85f);
            var ov = StoryOverlay.I;
            if (ov) yield return ov.FadeBlack(0, 1, 0.5f);
            if (young) young.SetActive(false);
            if (woman) woman.SetActive(false);
            youngSit = Spawn("L_ShadowYoungSit", M("SitYoung"));
            womanSit = Spawn("L_ShadowWomanSit", M("SitWoman"));
            p.Teleport(M("CatWatch").position + Vector3.up * 0.3f);
            if (p.model) p.model.rotation = Quaternion.LookRotation(Vector3.forward);
            if (ov) yield return ov.FadeBlack(1, 0, 0.6f);
            yield return new WaitForSeconds(0.8f);
            yield return Say(Woman + "::해가 지네.", Young + "::내일도 오자.", Woman + "::...응. 내일도.");
            p.InputLocked = true;
            // 카메라가 수면으로 내려감: 둘이 물에 비침
            var water = M("WaterLook").position;
            // 물이 거울처럼 또렷하게: 반사를 세게 · 물결을 잔잔하게
            var wr = G("Water") ? G("Water").GetComponent<Renderer>() : null;
            if (wr) { wr.material.SetFloat("_ReflStrength", 0.97f); wr.material.SetFloat("_WaveStrength", 0.12f); wr.material.SetFloat("_Distort", 0.008f); }
            CameraFollow.FocusOn(water, 0.42f);
            yield return new WaitForSeconds(2.6f);
            // 물속에서만 그녀가 남음 (메인 카메라엔 안 보임, 반사엔 보임)
            if (womanSit) MirrorLayers.SetLayer(womanSit, MirrorLayers.ReflectOnly);
            yield return new WaitForSeconds(1.4f);
            // 다시 올라오면 잔교엔 청년만
            CameraFollow.FocusOn(end + Vector3.back * 1.5f, 0.75f);
            yield return new WaitForSeconds(2.2f);
            yield return Say("고양이#놀람::...어디 간 거지...?");
        }

        IEnumerator Finish(bool ok)
        {
            var ov = StoryOverlay.I;
            if (ok && ov) yield return ov.FadeWhite(0, 1, 1.1f);
            CameraFollow.FocusOn(null, 0f);
            Cleanup();
            p.InputLocked = false;
            p.health.invulnerable = false;
            finished = true;
            done?.Invoke();
            CameraFollow.SnapZoom(1f);
            Music.Duck(0f, 0.8f);
            if (ok && ov) yield return ov.FadeWhite(1, 0, 0.9f);
            Destroy(gameObject);
        }

        void Cleanup()
        {
            if (P) P.OnFired -= OnPlayerFired;
            if (targetRing) { targetRing.Kill(); targetRing = null; }
            foreach (var e in shadows) if (e) Destroy(e.gameObject);
            shadows.Clear();
            if (amb) { Sfx.StopLoop(amb, 0.6f); amb = null; }
            if (map) Destroy(map.gameObject);
            Day_Off();
            RunManager.StoryHold = false;
        }

        void OnDestroy() { if (!finished) Cleanup(); }

        void Update()
        {
            if (!finished && rm && rm.runOver) { finished = true; Cleanup(); Destroy(gameObject); return; }
            if (finished || !map) return;
            // 물에 비칠 하늘색 = 지금 하늘 (한낮 → 저녁)
            if (cam) Shader.SetGlobalColor(SkyId, new Color(cam.backgroundColor.r, cam.backgroundColor.g, cam.backgroundColor.b, 1f));
            // 살아 있는 호수: 화면 안 물 위에 가끔 동그란 물결 (물고기 · 바람)
            if (Time.time >= nextRipple && p)
            {
                nextRipple = Time.time + Random.Range(0.35f, 0.9f);
                var at = p.transform.position + new Vector3(Random.Range(-16f, 16f), 0, Random.Range(-4f, 20f));
                if (at.z - Origin.z > 5.5f && Mathf.Abs(at.x - Origin.x - 2f) > 2.6f)
                {
                    at.y = Origin.y + WaterY + 0.02f;
                    RippleRing.Spawn(at, Random.Range(0.8f, 1.4f), 0f, Random.Range(0.9f, 1.8f), 0.07f, new Color(0.92f, 0.97f, 1f), 0.1f);
                }
            }
        }

        // ---------- 도우미 ----------
        static PlayerController P => PlayerController.Instance;
        static void Snd(string key, float vol) { if (Sfx.Has(key)) Sfx.Play(key, vol, 0f); }

        IEnumerator Say(params string[] lines)
        {
            bool d = false;
            ui.Talk(Young, lines, () => d = true);
            while (!d) yield return null;
        }

        IEnumerator WaitNear(Vector3 at, float r)
        {
            while (Effects.Flat(p.transform.position - at).magnitude > r) yield return null;
        }

        GameObject Spawn(string kit, Transform at)
        {
            var prefab = Resources.Load<GameObject>("Kit/Story/" + kit);
            if (!prefab || !at) return null;
            return Instantiate(prefab, at.position, at.rotation, map);
        }

        /// <summary>개발용: 마을이나 판에서 바로 미리보기 (rm 없이)</summary>
        public static void DevPreview() { Meta.StorySeen(Key); Play(null, null); }
    }
}
