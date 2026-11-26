// 스토리 맵 '어린 날 시계탑 광장' (2026-11-25 사용자 확정)
//   판 안에서 처음 1장 보스 문에 들어서면 한 번, 꿈 맵으로 빨려 들어감 → 끝나면 그 보스 방으로 바로 이어짐.
//   보스 기믹(안전 지대의 시각에 발사 시각 맞추기)을 압박 없이 익히고, 꿈2 '멈춘 시계는 고치면 다시 간다'를 먼저 겪게 함.
//   ① 골목: 회중시계가 3시에 멈춤 → 작은 바닥 문자판 III에 서서 발사 시각 3시 → 첫 문이 열림
//   ② 광장: 바닥 큰 바늘이 째깍째깍 돌다 멈춤(거리 시계는 엉뚱한 시각 = 미끼) → 그 숫자에 서서 시각 맞춤 → 시계탑 바늘이 움직임
//   ③ 그림자들이 솟아오름: 짧은 전투 2번 + 넉넉한 시각 패턴 한 번(실패 피해 작게) → 종이 울리고 하얗게 → 보스 방
// 맵 = Resources/Story/StoryPlaza.prefab (블렌더 키트, Editor/StoryPlazaBuilder.cs). 대사 2026-11-25 사용자 질문으로 확정
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    public class StoryMap : MonoBehaviour
    {
        public const string Key = "storymap_plaza";
        const string Child = "???", Adult = "???";   // 그림자 아이 · 어른 — 이름은 가림 (2026-11-25 사용자)
        static readonly Vector3 Origin = new(0, 0, -900);                  // 판 맵과 멀리 떨어진 자리
        const float Dial1R = 3.6f, Dial2R = 10f, NumeralRing = 0.805f, ZoneR = 1.6f;
        const float TrialTime = 12f;                                         // 보스 8초보다 넉넉하게
        static readonly Color Dusk = new(0.66f, 0.36f, 0.38f);

        /// <summary>이번 문이 처음 만나는 1장 보스 문이면 스토리 맵</summary>
        public static bool Pending(int chapter) => chapter == 1 && !Meta.StorySeen(Key);

        /// <summary>practice = 보스 패턴 연습 (StoryPractice): 대사 · 연출 없이 기믹만, 기록 안 함</summary>
        public static void Play(RunManager rm, System.Action done, bool practice = false)
        {
            var go = new GameObject("StoryMap");
            var sm = go.AddComponent<StoryMap>();
            sm.rm = rm; sm.done = done; sm.practice = practice;
        }

        RunManager rm;
        System.Action done;
        PlayerController p;
        GameUI ui;
        Transform map;
        GameObject child, adult;
        Camera cam;
        CameraClearFlags oldFlags;
        Color oldBg;
        readonly List<Enemy> shadows = new();
        bool finished;
        bool practice;                 // 보스 패턴 연습
        StoryPractice pr;
        Telegraph matchRing;

        Transform M(string name) => map.Find("Marks/" + name);
        Transform G(string name) => map.Find("Geo/" + name);

        IEnumerator Start()
        {
            p = PlayerController.Instance;
            ui = FindFirstObjectByType<GameUI>();
            var ov = StoryOverlay.I;
            p.InputLocked = true;
            p.health.invulnerable = true;
            rm.EnterStoryMap();
            // 빨려 들어감: 두통 → 하얗게
            Music.Duck(0.25f, 0.6f);
            if (ov) { if (!practice) yield return ov.Headache(0.6f, 1.0f); yield return ov.FadeWhite(0, 1, practice ? 0.35f : 0.9f); }

            var prefab = Resources.Load<GameObject>("Story/StoryPlaza");
            if (!prefab) { Debug.LogWarning("[StoryMap] Story/StoryPlaza 프리팹 없음 — 건너뜀"); yield return Finish(false); yield break; }
            map = Instantiate(prefab, Origin, Quaternion.identity).transform;
            cam = Camera.main;
            if (cam) { oldFlags = cam.clearFlags; oldBg = cam.backgroundColor; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Dusk; }
            p.Teleport(M("Start").position + Vector3.up * 0.3f);
            // 시계탑은 11시 59분에 멈춰 있음 (게임 제목)
            var tw = G("Tower");
            if (tw) { var hh = tw.Find("TowerHandH"); var hm = tw.Find("TowerHandM"); if (hh) hh.localRotation = Quaternion.Euler(0, 0, 30f); if (hm) hm.localRotation = Quaternion.Euler(0, 0, 6f); }
            CameraFollow.SnapZoom(1f);
            child = Spawn("ShadowChild", M("Child"));
            if (practice) pr = StoryPractice.Begin(this, Quit);
            if (ov) yield return ov.FadeWhite(1, 0, practice ? 0.5f : 1.2f);
            Music.Duck(0f, 0.8f);
            Music.Play(StoryBgm.For(1, "stage"));                      // 2026-11-25 사용자: 스토리 맵 전용 곡 (Gemini) — 맵 내내, 전투에서도

            if (!practice) yield return Say("고양이#의문::......여긴 어디지.",
                             "고양이#의문::분명... 난 문을 열고 들어왔는데...",
                             "고양이#의문::노을...... 이렇게 따뜻한 색은 처음 보는군.");
            p.InputLocked = false;
            p.health.invulnerable = false;

            // ① 회중시계 (3시)
            yield return WaitNear(M("Watch").position, 4f);
            if (!practice)
            {
                p.InputLocked = true;
                CameraFollow.FocusOn(M("Watch").position, 0.8f);
                yield return Say(Child + "::시계 놀이 하자!",
                                 Child + "::이 시계, 세 시에 멈췄어.",
                                 Child + "::바늘이 가리키는 숫자에 서서, 똑같은 시각에 탕! 하는 거야.");
                CameraFollow.FocusOn(null, 0f);
                p.InputLocked = false;
            }
            ui?.Toast("회중시계의 시각 — 바닥 문자판의 그 숫자에 서서, 발사 시각을 맞춰라");
            yield return Match(M("Dial1").position, Dial1R, 3, null);
            Sfx.Play("e4_tick_1", 0.9f, 0f);
            if (!practice) yield return Say(Child + "::봐! 다시 간다!");
            yield return OpenGate();
            if (child) StartCoroutine(Walk(child.transform, M("ChildPlaza"), 3.2f));

            // ② 광장 큰 문자판
            yield return WaitNear(M("Dial2").position, Dial2R + 1.5f);
            adult = Spawn("ShadowAdult", M("Adult"));
            if (!practice)
            {
                p.InputLocked = true;
                CameraFollow.FocusOn(M("Adult").position, 0.8f);
                yield return Say(Adult + "::얘야, 광장 시계는 혼자선 못 고쳐.",
                                 Adult + "::바늘을 잘 봐 두렴.",
                                 Adult + "::거리의 시계는 믿지 마라. 저건 늘 제멋대로니까.");
                CameraFollow.FocusOn(null, 0f);
                p.InputLocked = false;
            }
            int h2 = PickHour(3);
            yield return SpinHand(h2, 2.2f);
            ui?.Toast("바닥 바늘이 멈춘 숫자에 서서, 발사 시각을 맞춰라");
            yield return Match(M("Dial2").position, Dial2R, h2, null);
            yield return TowerTick(2);

            // ③ 그림자들
            if (!practice)
            {
                p.InputLocked = true;
                yield return Say(Child + "::저 그림자들은 뭐야?",
                                 "고양이#결의::물러서 있어.",
                                 "고양이#결의::이건 내가 상대하지.");
                p.InputLocked = false;
            }
            if (!StoryBgm.Has(1)) Music.PlayFor("stage", 1);
            yield return Wave(4);
            yield return Wave(5, trial: true);
            yield return TowerTick(3);
            if (practice) { yield return Finish(true); yield break; }   // 연습: 끝 연출 · 기록 없음

            // 끝: 종 · 아이가 탑을 올려다봄 → 하얗게
            p.InputLocked = true;
            if (Sfx.Has("e4_bell")) Sfx.Play("e4_bell", 1f, 0f);
            CameraFollow.FocusOn(G("Tower") ? G("Tower").position + Vector3.up * 6f : M("Dial2").position, 0.9f);
            if (child) child.transform.rotation = Quaternion.LookRotation(Flat((G("Tower") ? G("Tower").position : M("Dial2").position) - child.transform.position));
            yield return new WaitForSecondsRealtime(1.2f);
            yield return Say(Child + "::고쳤다!! 움직이기 시작했어!",
                             "고양이#놀람::......!! 멈췄던 시계바늘이......");
            Meta.MarkStory(Key);
            yield return Finish(true);
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
            if (ok && ov) yield return ov.FadeWhite(1, 0, practice ? 0.5f : 0.9f);
            Destroy(gameObject);
        }

        void Cleanup()
        {
            foreach (var e in shadows) if (e) Destroy(e.gameObject);
            shadows.Clear();
            if (matchRing) { matchRing.Kill(); matchRing = null; }
            if (map) Destroy(map.gameObject);
            if (cam) { cam.clearFlags = oldFlags; cam.backgroundColor = oldBg; }
            RunManager.StoryHold = false;
        }

        void OnDestroy() { if (!finished) Cleanup(); }    // 맵 안에서 쓰러져 판이 끝나도 정리

        void Update()
        {
            if (!finished && rm && rm.runOver) { finished = true; Cleanup(); Destroy(gameObject); }
        }

        IEnumerator Say(params string[] lines)
        {
            if (practice) yield break;                            // 연습: 대사 없음
            bool d = false;
            ui.Talk(Child, lines, () => d = true);
            while (!d) yield return null;
        }

        IEnumerator WaitNear(Vector3 at, float r)
        {
            while (Flat(p.transform.position - at).magnitude > r) yield return null;
        }

        static Vector3 Flat(Vector3 v) { v.y = 0; return v.sqrMagnitude < 1e-6f ? Vector3.forward : v; }

        static Vector3 HourPoint(Vector3 c, float r, int h)
        {
            float a = h * 30f * Mathf.Deg2Rad;
            return c + new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * r * NumeralRing;
        }

        int PickHour(int avoid)
        {
            int h;
            do h = Random.Range(1, 13); while (h == avoid || h == p.rev.next);
            return h;
        }

        /// <summary>그 숫자 자리 원 안에서 발사 시각 = hour 를 0.6초 유지하면 성공. dur 있으면 시간 제한(넘기면 실패 → 피해 작게, 다시)</summary>
        IEnumerator Match(Vector3 dial, float dialR, int hour, float? dur)
        {
            var zone = HourPoint(dial, dialR, hour) + Vector3.up * 0.05f;
            float hold = 0, warnAt = -9;
            float end = dur.HasValue ? Time.time + dur.Value : float.MaxValue;
            var ring = matchRing = Telegraph.Ring(zone, ZoneR, dur ?? 9999f, BossFx.Safe);
            while (true)
            {
                if (!p || !p.health.Alive) yield break;
                bool inZone = Flat(p.transform.position - zone).magnitude <= ZoneR;
                bool timeOk = p.rev.next == hour;
                if (inZone && timeOk) { hold += Time.deltaTime; if (hold >= 0.6f) break; }
                else
                {
                    hold = 0;
                    if (inZone && !timeOk && Time.time > warnAt) { ui?.Toast($"발사 시각 {p.rev.next}시 — 쏘면 다음 시각으로 넘어간다"); warnAt = Time.time + 3.5f; }
                }
                if (Time.time > end)
                {
                    p.health.TakeDamage(p.health.maxHp * 0.15f);
                    FxBurst.Spawn(p.transform.position + Vector3.up, 2f, FxBurst.Kind.Explosion);
                    ui?.Toast("시각을 놓쳤다 — 다시");
                    matchRing = null;
                    yield break;
                }
                yield return null;
            }
            if (ring) ring.Kill();
            matchRing = null;
            FxBurst.Spawn(zone + Vector3.up, 2.2f, FxBurst.Kind.Gold, new Color(1f, 0.85f, 0.35f));
            if (Sfx.Has("ui_buy")) Sfx.Play("ui_buy", 0.7f, 0f);
        }

        /// <summary>바닥 큰 바늘이 12시부터 째깍째깍 돌아 hour에 멈춤</summary>
        IEnumerator SpinHand(int hour, float spin)
        {
            var hand = G("DialHand");
            if (!hand) yield break;
            int steps = 12 + hour;
            for (int i = 1; i <= steps; i++)
            {
                hand.localRotation = Quaternion.Euler(0, (i % 12) * 30f, 0);
                if (i % 2 == 0) Sfx.Play("e4_tick_1", 0.45f, 0.05f);
                yield return new WaitForSeconds(spin / steps * (i > steps - 4 ? 2.2f : 1f));
            }
            hand.localRotation = Quaternion.Euler(0, hour * 30f, 0);
        }

        IEnumerator TowerTick(int n)
        {
            var t = G("Tower");
            var hh = t ? t.Find("TowerHandH") : null;
            var hm = t ? t.Find("TowerHandM") : null;
            for (int i = 0; i < n * 4; i++)
            {
                if (hm) hm.localRotation *= Quaternion.Euler(0, 0, -30f);
                if (hh && i % 4 == 3) hh.localRotation *= Quaternion.Euler(0, 0, -30f);
                Sfx.Play(i % 2 == 0 ? "e4_tick_1" : "e4_tick_2", 0.7f, 0f);
                yield return new WaitForSeconds(0.14f);
            }
        }

        IEnumerator OpenGate()
        {
            var l = G("Gate1_L"); var r = G("Gate1_R");
            foreach (var g in new[] { l, r }) if (g) { var c = g.GetComponent<Collider>(); if (c) c.enabled = false; }
            if (Sfx.Has("fac_gate")) Sfx.Play("fac_gate", 0.8f, 0f);
            Quaternion l0 = l ? l.localRotation : Quaternion.identity, r0 = r ? r.localRotation : Quaternion.identity;
            for (float t = 0; t < 1f; t += Time.deltaTime / 0.9f)
            {
                float k = Mathf.SmoothStep(0, 1, t);
                if (l) l.localRotation = l0 * Quaternion.Euler(0, -100f * k, 0);
                if (r) r.localRotation = r0 * Quaternion.Euler(0, 100f * k, 0);
                yield return null;
            }
        }

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

        /// <summary>그림자들: 문자판 둘레에서 솟아오름. trial이면 중간에 넉넉한 시각 패턴</summary>
        IEnumerator Wave(int n, bool trial = false)
        {
            var c = M("Dial2").position;
            var spots = new List<Vector3>();
            for (int i = 0; i < n; i++)
            {
                Vector3 pos; int tries = 0;
                do pos = c + Quaternion.Euler(0, Random.Range(0f, 360f), 0) * Vector3.forward * Random.Range(4f, 11f);
                while (Flat(pos - p.transform.position).magnitude < 5f && ++tries < 12);
                spots.Add(pos);
                Telegraph.Ring(pos, 0.7f, RunManager.WavePreview, Telegraph.Aim);
            }
            yield return new WaitForSeconds(RunManager.WavePreview);
            var ch = Tables.Chapter(1);
            foreach (var pos in spots)
            {
                var e = EnemyFactory.RandomMinion(pos, ch);
                if (e) shadows.Add(e);
            }
            if (trial)
            {
                yield return new WaitForSeconds(2.5f);
                int h = PickHour(-1);
                StartCoroutine(SpinHand(h, 1.4f));
                yield return new WaitForSeconds(1.5f);
                BossFx.Toast("시각 패턴 — 바늘이 멈춘 숫자에 서서 발사 시각을 맞춰라");
                yield return Match(c, Dial2R, h, TrialTime);
            }
            while (true)
            {
                shadows.RemoveAll(e => !e || !e.health || !e.health.Alive);
                if (shadows.Count == 0) break;
                yield return null;
            }
            yield return new WaitForSeconds(0.6f);
        }
    }
}
