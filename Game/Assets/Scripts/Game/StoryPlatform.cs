// 스토리 맵 2 '배웅하던 승강장' (2026-11-25 사용자 확정)
//   판 안에서 처음 2장 보스 문에 들어서면 한 번, 비 오는 밤 승강장 기억으로 빨려 들어감 → 끝나면 로코모스 방으로 바로 이어짐.
//   로코모스 기믹(연기 링 · 출발 시각)을 압박 없이 익히고, '약속한 사람을 기다리느라 떠나는 시간을 붙잡는다'를 먼저 겪게 함.
//   ① 개찰구 → 청년 그림자 → 기관차 굴뚝 연기 고리의 빈틈으로 빠져 객차 창가까지 (느린 링 → 두 겹)
//   ② 창가: 여인 그림자 '곧 올게' → 기적 → 기관차 앞마당에서 헤드라이트 시계 시각에 쏜 탄을 헤드라이트에 맞혀 출발을 두 번 막음
//   ③ 그림자 승객 전투 + 연기 링 · 출발 시각을 합친 느슨한 패턴 → 막아도 결국 기차가 떠남 → 하얗게 → 보스 방
// 맵 = Resources/Story/StoryPlatform.prefab (블렌더 키트 + 2장 기차, Editor/StoryPlatformBuilder.cs). 대사 2026-11-25 사용자 컨펌
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    public class StoryPlatform : MonoBehaviour
    {
        public const string Key = "storymap_platform";
        const string Young = "???", Woman = "???";      // 그림자 청년 · 여인 — 이름은 가림 (2026-11-25 사용자, 여인 = 훗날의 아내 암시)
        static readonly Vector3 Origin = new(0, 0, -1400);                 // 판 맵 · 스토리 맵 1과 멀리 떨어진 자리
        const float RingSpeedSlow = 3.2f, RingSpeed = 3.8f, RingDmg = 6f, RingMaxR = 34f, RingEvery = 2.8f;
        const float DepartTime = 16f, TrialDepartTime = 14f, DepartFailFrac = 0.08f;   // 보스보다 넉넉하게 · 실패 피해 작게
        static readonly Color Night = new(0.05f, 0.07f, 0.16f);

        /// <summary>이번 문이 처음 만나는 2장 보스 문이면 스토리 맵 2</summary>
        public static bool Pending(int chapter) => chapter == 2 && !Meta.StorySeen(Key);

        /// <summary>practice = 보스 패턴 연습 (StoryPractice): 대사 · 연출 없이 기믹만, 기록 안 함</summary>
        public static void Play(RunManager rm, System.Action done, bool practice = false)
        {
            var sm = new GameObject("StoryPlatform").AddComponent<StoryPlatform>();
            sm.rm = rm; sm.done = done; sm.practice = practice;
        }

        RunManager rm;
        System.Action done;
        PlayerController p;
        GameUI ui;
        Transform map, train, engine, car1;
        GameObject young, woman;
        Camera cam;
        CameraClearFlags oldFlags;
        Color oldBg, oldAmbient;
        Light sun; Color sunColor; float sunIntensity;
        Health headlight;
        TextMesh headLabel;
        AudioSource rain;
        readonly List<Enemy> shadows = new();
        bool finished;
        bool practice;                 // 보스 패턴 연습
        StoryPractice pr;
        Telegraph spotRing;
        // 출발 시각
        int departHour; bool departOn, departHit;

        Transform M(string name) => map.Find("Marks/" + name);
        Transform G(string name) => map.Find("Geo/" + name);

        IEnumerator Start()
        {
            p = PlayerController.Instance;
            ui = FindFirstObjectByType<GameUI>();
            var ov = StoryOverlay.I;
            p.InputLocked = true;
            p.health.invulnerable = true;
            if (rm) rm.EnterStoryMap();                          // rm 없음 = 개발용 미리보기 (마을에서 바로)
            Music.Duck(0.25f, 0.6f);
            if (ov) { if (!practice) yield return ov.Headache(0.6f, 1.0f); yield return ov.FadeWhite(0, 1, practice ? 0.35f : 0.9f); }

            var prefab = Resources.Load<GameObject>("Story/StoryPlatform");
            if (!prefab) { Debug.LogWarning("[StoryPlatform] Story/StoryPlatform 프리팹 없음 — 건너뜀"); yield return Finish(false); yield break; }
            map = Instantiate(prefab, Origin, Quaternion.identity).transform;
            train = G("Train"); engine = train ? train.Find("Engine") : null; car1 = train ? train.Find("Car1") : null;
            Night_On();
            p.Teleport(M("Start").position + Vector3.up * 0.3f);
            CameraFollow.SnapZoom(1f);
            young = Spawn("P_ShadowYoung", M("Young"));
            woman = Spawn("P_ShadowWoman", M("Woman"));
            MakeHeadlight();
            if (P) P.OnFired += OnPlayerFired;
            if (practice) pr = StoryPractice.Begin(this, Quit);
            if (ov) yield return ov.FadeWhite(1, 0, practice ? 0.5f : 1.2f);
            Music.Duck(0f, 0.8f);
            Music.Play(StoryBgm.For(2, "stage"));                      // 2026-11-25 사용자: 스토리 맵 전용 곡 (Gemini) — 맵 내내, 전투에서도
            if (Sfx.Has("st_rain")) rain = Sfx.Loop("st_rain", 0.55f);

            if (!practice) yield return Say("고양이#의문::....비?");                      // 2026-11-25 사용자: 1장 도착 대사를 되풀이하지 않고 이 한 줄만
            p.InputLocked = false;
            p.health.invulnerable = false;

            // ① 청년 그림자 → 연기 고리
            yield return WaitNear(M("Young").position, 4.5f);
            if (!practice)
            {
                p.InputLocked = true;
                CameraFollow.FocusOn(M("Young").position, 0.8f);
                yield return Say(Young + "::...아직 시간 있어.",
                                 Young + "::연기 사이로 지나가면 돼. 틈은 늘 있으니까.");
                CameraFollow.FocusOn(null, 0f);
                p.InputLocked = false;
            }
            ui?.Toast("연기 고리의 빈틈으로 빠져나가세요");
            yield return SmokeWalk(M("Window").position, 3.2f);

            // ② 창가: 여인 → 기적 → 출발 막기 두 번
            if (!practice)
            {
                p.InputLocked = true;
                CameraFollow.FocusOn(M("Woman").position, 0.8f);
                yield return Say(Woman + "::왔구나. 기차가 곧 떠나.",
                                 "고양이#의문::...나한테 하는 말인가?",
                                 Woman + "::곧 올게. 약속해.");
                Snd("st_train_whistle", 1f);
                yield return Jolt(0.5f);
            }
            if (young) StartCoroutine(Walk(young.transform, M("YoungFront"), 3.4f));
            if (!practice)
            {
                CameraFollow.FocusOn(M("YoungFront").position, 0.8f);
                yield return new WaitForSeconds(0.9f);
                yield return Say(Young + "::안 돼... 아직이야. 조금만 더.");
                CameraFollow.FocusOn(null, 0f);
                p.InputLocked = false;
            }
            var spot = spotRing = Telegraph.Ring(M("ShootSpot").position + Vector3.up * 0.05f, 2.2f, 9999f, BossFx.Safe);
            ui?.Toast("기관차 앞으로 — 헤드라이트 시계가 가리키는 시각에 쏜 탄을 헤드라이트에 맞히세요");
            yield return WaitNear(M("ShootSpot").position, 6f);
            if (spot) spot.Kill();
            spotRing = null;
            for (int ok = 0; ok < 2;)
            {
                bool hit = false;
                yield return Departure(DepartTime, r => hit = r);
                if (!p || !p.health.Alive) yield break;
                if (!hit) { yield return new WaitForSeconds(1.2f); continue; }
                ok++;
                if (!practice)
                {
                    p.InputLocked = true;
                    if (ok == 1) yield return Say(Young + "::한 번만 더...");
                    else { CameraFollow.FocusOn(M("Woman").position, 0.7f); yield return Say(Woman + "::...억지로 붙잡지 마."); CameraFollow.FocusOn(null, 0f); }
                    p.InputLocked = false;
                }
                yield return new WaitForSeconds(0.8f);
            }

            // ③ 그림자 승객
            if (!practice)
            {
                p.InputLocked = true;
                yield return Say("고양이#결의::...뭐든 상관없어. 이 기차는 아직 못 보낸다.");
                p.InputLocked = false;
            }
            if (!StoryBgm.Has(2)) Music.PlayFor("stage", 2);
            yield return Wave(4);
            yield return Wave(5, trial: true);
            if (practice) { yield return Finish(true); yield break; }   // 연습: 끝 연출 · 기록 없음

            // 끝: 기차가 떠남 → 청년만 남음 → 하얗게
            p.InputLocked = true;
            if (P) P.OnFired -= OnPlayerFired;
            Music.Duck(0.3f, 0.8f);
            yield return BoardWoman();
            Snd("st_train_whistle", 1f);
            CameraFollow.FocusOn(car1 ? car1.position : M("Woman").position, 0.9f);
            StartCoroutine(Depart());
            yield return new WaitForSeconds(1.0f);
            yield return Say(Woman + "::곧 올게.");
            if (young) { var yt = M("YoungEnd"); StartCoroutine(Walk(young.transform, yt, 2.6f)); CameraFollow.FocusOn(yt.position, 0.9f); }
            yield return new WaitForSeconds(1.4f);
            yield return Say(Young + "::......기다릴게. 몇 시가 되든.",
                             "고양이#놀람::...잠깐...!");
            Meta.MarkStory(Key);
            yield return Finish(true);
        }

        // ---------- 밤 · 비 ----------
        void Night_On()
        {
            cam = Camera.main;
            if (cam) { oldFlags = cam.clearFlags; oldBg = cam.backgroundColor; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Night; }
            oldAmbient = RenderSettings.ambientLight;
            RenderSettings.ambientLight = Color.Lerp(oldAmbient, new Color(0.42f, 0.44f, 0.58f), 0.45f);
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional && l.enabled) { sun = l; break; }
            if (sun) { sunColor = sun.color; sunIntensity = sun.intensity; sun.color = new Color(0.86f, 0.88f, 1f); sun.intensity = sunIntensity * 0.75f; }
        }

        void Night_Off()
        {
            if (cam) { cam.clearFlags = oldFlags; cam.backgroundColor = oldBg; }
            RenderSettings.ambientLight = oldAmbient;
            if (sun) { sun.color = sunColor; sun.intensity = sunIntensity; }
        }

        // ---------- ① 연기 고리 ----------
        /// <summary>굴뚝에서 연기 고리가 퍼짐 — 빈틈은 플레이어 줄의 왼쪽 · 오른쪽을 번갈아 가리킴. 목표 자리에 닿으면 끝</summary>
        IEnumerator SmokeWalk(Vector3 goal, float goalR)
        {
            var chimney = M("Chimney").position;
            int n = 0;
            float next = 0;
            while (Flat(p.transform.position - goal).magnitude > goalR)
            {
                if (!p.health.Alive) yield break;
                if (Time.time >= next)
                {
                    bool twin = n >= 2;
                    var gapAt = new Vector3(map.position.x + (n % 2 == 0 ? -4.6f : 0.4f), 0, p.transform.position.z);
                    var gap = Flat(gapAt - chimney);
                    SmokeRing.Spawn(chimney, twin ? RingSpeed : RingSpeedSlow, RingDmg, RingMaxR, gap);
                    if (twin) SmokeRing.Spawn(chimney, RingSpeed, RingDmg, RingMaxR, gap, 45f, 0.6f);
                    Snd("e_steam", 0.55f);
                    n++;
                    next = Time.time + RingEvery + (twin ? 0.6f : 0f);
                }
                yield return null;
            }
        }

        // ---------- ② 출발 시각 ----------
        /// <summary>헤드라이트 앞 판정 구: 맞으면 HitFrom만 남고 죽지 않음 (같은 프레임 발사 시각으로 성공 판정)</summary>
        void MakeHeadlight()
        {
            var at = M("Headlight");
            if (!at) return;
            var go = new GameObject("HeadlightTarget");
            go.transform.SetParent(at, false);
            var sc = go.AddComponent<SphereCollider>(); sc.radius = 1.3f;
            headlight = go.AddComponent<Health>();
            headlight.maxHp = 99999; headlight.minHp = 1; headlight.ResetHp(99999);
            headLabel = BossFx.Label(at, new Vector3(0, 3.4f, 0), "", new Color(1f, 0.92f, 0.6f), 0.09f);   // 보스 '출발 시각' 표시와 같은 모양
        }

        void OnPlayerFired(int slot, bool awake)
        {
            if (!departOn || departHit || slot != departHour || !headlight) return;
            if (Time.time - headlight.lastHitAt > 0.001f) return;              // 이 탄이 헤드라이트를 맞혔음 (같은 프레임)
            departHit = true;
        }

        IEnumerator Departure(float time, System.Action<bool> result)
        {
            int hour = Random.Range(4, 10);
            if (hour == p.rev.next) hour = hour == 9 ? 4 : hour + 1;
            departHour = hour; departHit = false; departOn = true;
            ui?.Toast($"헤드라이트 시계 {hour}시 — 발사 시각이 {hour}시일 때 쏜 탄을 헤드라이트에 맞히세요");
            float end = Time.time + time;
            while (Time.time < end && !departHit && p.health.Alive)
            {
                int left = Mathf.CeilToInt(end - Time.time);
                bool ready = p.rev.next == hour;
                if (headLabel) { headLabel.text = $"{hour}시\n<size=46>{left}초</size>"; headLabel.color = ready ? Ch2Fx.Green : new Color(1f, 0.92f, 0.6f); }
                yield return null;
            }
            departOn = false;
            if (headLabel) headLabel.text = "";
            var head = M("Headlight").position;
            if (departHit)
            {
                FxBurst.Spawn(head, 3f, FxBurst.Kind.Gold, new Color(1f, 0.85f, 0.35f));
                GunkataFx.Smoke(head + Vector3.up * 0.5f, 2.4f, 1.2f);
                Snd("e_steam", 0.9f);
                CameraFollow.Shake(0.15f);
                ui?.Toast("출발을 막았다");
                result(true);
                yield break;
            }
            // 놓침: 기적과 함께 기차가 덜컹 — 앞마당에 증기 (피해 작게)
            Snd("st_train_whistle", 0.9f);
            ui?.Toast("기차가 덜컹 — 다시");
            yield return Jolt(1.1f);
            if (Flat(p.transform.position - head).magnitude < 14f) p.health.TakeDamage(p.health.maxHp * DepartFailFrac);
            GunkataFx.Smoke(head + Vector3.up * 0.5f, 3f, 1.4f);
            result(false);
        }

        /// <summary>기차 전체가 앞으로 덜컹했다가 제자리</summary>
        IEnumerator Jolt(float dist)
        {
            if (!train) yield break;
            var z0 = train.localPosition;
            CameraFollow.Shake(0.1f);
            for (float t = 0; t < 1f; t += Time.deltaTime / 0.55f)
            {
                train.localPosition = z0 + Vector3.forward * dist * Mathf.Sin(t * Mathf.PI);
                yield return null;
            }
            train.localPosition = z0;
        }

        // ---------- ③ 그림자 승객 ----------
        IEnumerator Wave(int n, bool trial = false)
        {
            var c = M("Arena").position;
            var spots = new List<Vector3>();
            for (int i = 0; i < n; i++)
            {
                Vector3 pos; int tries = 0;
                do pos = c + Quaternion.Euler(0, Random.Range(0f, 360f), 0) * Vector3.forward * Random.Range(3f, 7.5f);
                while (Flat(pos - p.transform.position).magnitude < 4.5f && ++tries < 12);
                spots.Add(pos);
                Telegraph.Ring(pos, 0.7f, RunManager.WavePreview, Telegraph.Aim);
            }
            yield return new WaitForSeconds(RunManager.WavePreview);
            var ch = Tables.Chapter(2);
            foreach (var pos in spots)
            {
                var e = EnemyFactory.RandomMinion(pos, ch);
                if (e) shadows.Add(e);
            }
            if (trial)
            {
                // 합친 느슨한 패턴: 연기 고리 두 겹(빈틈 = 마당 가운데 쪽) + 출발 시각 한 번 (놓쳐도 이어 감)
                yield return new WaitForSeconds(2.5f);
                var chimney = M("Chimney").position;
                var gap = Flat(c - chimney);
                SmokeRing.Spawn(chimney, RingSpeed, RingDmg, RingMaxR, gap);
                SmokeRing.Spawn(chimney, RingSpeed, RingDmg, RingMaxR, gap, 45f, 0.6f);
                Snd("e_steam", 0.6f);
                yield return new WaitForSeconds(1.5f);
                yield return Departure(TrialDepartTime, _ => { });
            }
            while (true)
            {
                shadows.RemoveAll(e => !e || !e.health || !e.health.Alive);
                if (shadows.Count == 0) break;
                yield return null;
            }
            yield return new WaitForSeconds(0.6f);
        }

        // ---------- 끝 ----------
        /// <summary>여인 그림자가 객차 문 앞까지 걸어가 뒤 난간에 올라탐 (기차와 같이 떠남)</summary>
        IEnumerator BoardWoman()
        {
            var at = M("WomanBoard");
            if (!woman || !at || !car1) yield break;
            CameraFollow.FocusOn(M("WomanDoor").position, 0.8f);
            yield return Walk(woman.transform, M("WomanDoor"), 1.6f);
            var from = woman.transform.position;
            for (float t = 0; t < 1f; t += Time.deltaTime / 0.45f)                 // 난간으로 한 걸음 올라섬
            {
                woman.transform.position = Vector3.Lerp(from, at.position, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.35f;
                yield return null;
            }
            woman.transform.SetParent(car1, true);
            woman.transform.SetPositionAndRotation(at.position, at.rotation);
        }

        IEnumerator Depart()
        {
            if (!train) yield break;
            Snd("st_train_depart", 1f);
            float v = 0;
            for (float t = 0; t < 9f; t += Time.deltaTime)
            {
                v = Mathf.Min(v + 1.6f * Time.deltaTime, 9f);
                train.localPosition += Vector3.forward * v * Time.deltaTime;
                if (Random.value < 0.06f && engine) GunkataFx.Smoke(M("Chimney").position + Vector3.up * 1.5f, 1.6f, 1f);
                yield return null;
            }
        }

        // ---------- 진행 ----------
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
            done?.Invoke();                                       // 보스 방을 지음 (플레이어 순간이동)
            CameraFollow.SnapZoom(1f);
            Music.Duck(0f, 0.8f);
            if (ok && ov) yield return ov.FadeWhite(1, 0, practice ? 0.5f : 0.9f);
            Destroy(gameObject);
        }

        void Cleanup()
        {
            if (P) P.OnFired -= OnPlayerFired;
            if (spotRing) { spotRing.Kill(); spotRing = null; }
            departOn = false;
            foreach (var e in shadows) if (e) Destroy(e.gameObject);
            shadows.Clear();
            foreach (var r in FindObjectsByType<SmokeRing>(FindObjectsSortMode.None)) Destroy(r.gameObject);
            if (rain) { Sfx.StopLoop(rain, 0.6f); rain = null; }
            if (map) Destroy(map.gameObject);
            Night_Off();
            RunManager.StoryHold = false;
        }

        void OnDestroy() { if (!finished) Cleanup(); }    // 맵 안에서 쓰러져 판이 끝나도 정리

        void Update()
        {
            if (!finished && rm && rm.runOver) { finished = true; Cleanup(); Destroy(gameObject); }
        }

        // ---------- 도우미 ----------
        static PlayerController P => PlayerController.Instance;

        static void Snd(string key, float vol) { if (Sfx.Has(key)) Sfx.Play(key, vol, 0f); }

        IEnumerator Say(params string[] lines)
        {
            if (practice) yield break;                            // 연습: 대사 없음
            bool d = false;
            ui.Talk(Young, lines, () => d = true);
            while (!d) yield return null;
        }

        IEnumerator WaitNear(Vector3 at, float r)
        {
            while (Flat(p.transform.position - at).magnitude > r) yield return null;
        }

        static Vector3 Flat(Vector3 v) { v.y = 0; return v.sqrMagnitude < 1e-6f ? Vector3.forward : v; }

        GameObject Spawn(string kit, Transform at)
        {
            var prefab = Resources.Load<GameObject>("Kit/Story/" + kit);
            if (!prefab || !at) return null;
            return Instantiate(prefab, at.position, at.rotation, map);
        }

        IEnumerator Walk(Transform who, Transform to, float speed)
        {
            if (!who || !to) yield break;
            while (who && Flat(to.position - who.position).magnitude > 0.2f)
            {
                var d = Flat(to.position - who.position);
                who.rotation = Quaternion.LookRotation(d);
                who.position += d.normalized * Mathf.Min(d.magnitude, speed * Time.deltaTime);
                yield return null;
            }
            if (who) who.rotation = to.rotation;
        }
    }
}
