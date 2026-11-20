// 개발용: 육중한 캐릭터 발걸음 흙먼지(HeavyStep) 확인 — 대상을 하나씩 꺼내 행동을 끄고 옆으로 걷게 한 뒤 따라가는 카메라로 프레임을 저장
// 플레이 중에 WalkDemo.Run(folder) 호출 (MCP execute_code). 결과: folder/<이름>/f00.png ...
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace CatTang.Dev
{
    public class WalkDemo : MonoBehaviour
    {
        public float speed = 2.5f, radius = 4f;
        Animator anim;
        Vector3 center;
        float ang;

        void Start() { anim = GetComponentInChildren<Animator>(); center = transform.position - Vector3.right * radius; }

        // 방 안에서 원을 그리며 걸음 (반시계)
        void Update()
        {
            ang += speed / radius * Time.deltaTime;
            var p = center + new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang)) * radius;
            var dir = new Vector3(-Mathf.Sin(ang), 0, Mathf.Cos(ang));
            transform.position = p;
            transform.rotation = Quaternion.LookRotation(dir);
            if (anim) anim.SetFloat("Speed", 1f);
        }

        // ---------- 돌진 꼬리 확인 (DashTrail): 장마다 대표 돌진 몬스터가 웅크렸다 일직선으로 돌진 ----------
        class DashMover : MonoBehaviour
        {
            public float speed = 14f, length = 9f, windup = 0.45f;
            public string clip = "Dash";
            float t; Vector3 start;
            void Start() { start = transform.position; transform.rotation = Quaternion.LookRotation(Vector3.right); var e = GetComponent<Enemy>(); if (e) e.Anim(clip); }
            void Update()
            {
                t += Time.deltaTime;
                float run = t - windup;
                if (run <= 0) return;
                float d = Mathf.Min(length, run * speed);
                transform.position = start + Vector3.right * d;
            }
        }

        public static void RunDash(string folder) => new GameObject("DashDemoRunner").AddComponent<Runner>().StartCoroutine(DashSequence(folder));

        static IEnumerator DashSequence(string folder)
        {
            Time.timeScale = 1;
            var rm = Object.FindFirstObjectByType<RunManager>();
            if (rm) rm.enabled = false;
            var player = PlayerController.Instance;
            // 고양이 자리 앞을 비워 두고(고양이는 뒤로 옮김) 그 줄을 따라 왼쪽 → 오른쪽 9m 돌진, 카메라는 길 가운데에 고정
            var basePos = player ? player.transform.position + new Vector3(-4.5f, 0, -3f) : Vector3.zero;
            if (rm) basePos = rm.ClampToRoom(basePos, 4f);
            if (player) player.transform.position = basePos + new Vector3(4.5f, 0, 6f);
            // 길 위 소품(바닥 말고 서 있는 것)은 촬영 동안 숨김
            foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                var bc = r.bounds.center;
                if (r.GetComponentInParent<PlayerController>() || r.bounds.size.y < 0.3f || r.bounds.max.y < 0.4f) continue;
                if (Mathf.Abs(bc.z - basePos.z) < 3.5f && Mathf.Abs(bc.x - basePos.x - 4.5f) < 9f) r.enabled = false;
            }
            var mid = new GameObject("DashDemoCam").transform;
            mid.position = basePos + new Vector3(4.5f, 0, 0);
            var make5 = typeof(EnemyFactory).GetMethod("Make5", BindingFlags.NonPublic | BindingFlags.Static);
            var list = new (string name, int ch, float camK, string clip, float spd)[] {
                ("dash_ch1_brute", 1, 0.8f, "Charge", 13f), ("dash_ch2_loco", 2, 1.5f, "Charge", 16f), ("dash_ch3_knight", 3, 0.8f, "Push", 12f),
                ("dash_ch4_minute", 4, 1.0f, "Dash", 16f), ("dash_ch5_giant", 5, 1.0f, "Dash", 13f), ("dash_ch5_mirrorcat", 6, 0.7f, "Run", 15f) };
            foreach (var (name, chId, camK, clip, spd) in list)
            {
                foreach (var old in Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None)) Object.Destroy(old.gameObject);
                yield return null;
                var ch = Tables.Chapter(Mathf.Min(chId, 5));
                Enemy e = chId switch
                {
                    1 => EnemyFactory.Brute(basePos, ch),
                    2 => EnemyFactory.Boss(basePos, ch),
                    3 => EnemyFactory.GlassKnight(basePos, ch),
                    4 => EnemyFactory.Boss(basePos, ch),
                    5 => (Enemy)make5.Invoke(null, new object[] { "Enemy_RuinGiant", "Ch5_Giant", basePos, ch, false, 544f, Enemy.Kind.RuinGiant, 1.0f, 3.4f, 3.2f, 0f, 0f }),
                    _ => EnemyFactory.MidBoss(basePos, ch),
                };
                foreach (var td in Object.FindObjectsByType<TwinDirector>(FindObjectsSortMode.None)) Object.Destroy(td.gameObject);
                if (chId == 4)
                {
                    // 분침으로 시연: 시침은 지우고 분침을 시작 자리로
                    var minute = GameObject.Find("Boss_MinuteHand");
                    Object.Destroy(e.gameObject);
                    e = minute ? minute.GetComponent<Enemy>() : null;
                    if (!e) continue;
                    e.transform.position = basePos;
                }
                else { var minute = GameObject.Find("Boss_MinuteHand"); if (minute) Object.Destroy(minute); }
                foreach (var b in e.GetComponents<MonoBehaviour>()) if (!(b is HeavyStep) && !(b is Health) && !(b is DashTrail)) b.enabled = false;
                foreach (var rb in e.GetComponentsInChildren<Rigidbody>()) rb.isKinematic = true;
                yield return new WaitForSeconds(0.5f);
                var m = e.gameObject.AddComponent<DashMover>();
                m.clip = clip; m.speed = spd;
                FrameCapture.Begin(30, 0.05f, System.IO.Path.Combine(folder, name), 720, mid, new Vector3(0, 9f, -8.5f) * Mathf.Max(1f, camK));
                var cap = GameObject.Find("FrameCapture");
                while (cap) yield return null;
                Object.Destroy(e.gameObject);
            }
            Debug.Log("DashDemo DONE");
        }

        // ---------- 4장 중간 보스 전광석화 확인: 4장 방을 짓고 진자 집행관을 고양이 앞 8m에 꺼냄 → 행동 그대로 (점멸 사슬 · 기습). 고양이는 무적 ----------
        public static void RunBlink(string folder) => new GameObject("BlinkDemoRunner").AddComponent<Runner>().StartCoroutine(BlinkSequence(folder));

        static IEnumerator BlinkSequence(string folder)
        {
            Time.timeScale = 1;
            var rm = Object.FindFirstObjectByType<RunManager>();
            if (rm)
            {
                // 4장 스테이지 방으로 다시 지음 (핸드오프의 테스트 요령: chapter 필드 + Chapter 속성 + BuildStage)
                rm.chapter = 4;
                typeof(RunManager).GetProperty("Chapter").SetValue(rm, Tables.Chapter(4));
                var build = typeof(RunManager).GetMethod("BuildStage", BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(int), typeof(RewardKind) }, null);
                build.Invoke(rm, new object[] { 1, RewardKind.Gold });
            }
            yield return new WaitForSeconds(0.6f);
            if (rm) rm.enabled = false;
            foreach (var old in Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None)) Object.Destroy(old.gameObject);
            yield return null;
            var player = PlayerController.Instance;
            if (!player) yield break;
            player.health.ResetHp(player.health.maxHp); player.health.invulnerable = true;
            var at = player.transform.position + new Vector3(0, 0, 11f);   // 7m보다 멀게 → 전광석화로 다가와야 함
            if (rm) at = rm.ClampToRoom(at, 2f);
            var e = EnemyFactory.MidBoss(at, Tables.Chapter(4));
            var cam = new GameObject("BlinkDemoCam").transform;
            cam.position = Vector3.Lerp(player.transform.position, at, 0.5f);
            yield return new WaitForSeconds(0.9f);                     // 첫 점멸 사슬은 1.2초 뒤
            FrameCapture.Begin(140, 0.04f, System.IO.Path.Combine(folder, "ch4_midboss_blink"), 720, cam, new Vector3(0, 13f, -10f));
            var cap = GameObject.Find("FrameCapture");
            while (cap) yield return null;
            player.health.invulnerable = false;
            Debug.Log("BlinkDemo DONE");
        }

        // ---------- 거울 고양이 사망: 조각상처럼 유리로 산산조각 (GlassShatter) ----------
        public static void RunShatter(string folder) => new GameObject("ShatterDemoRunner").AddComponent<Runner>().StartCoroutine(ShatterSequence(folder));

        static IEnumerator ShatterSequence(string folder)
        {
            Time.timeScale = 1;
            var rm = Object.FindFirstObjectByType<RunManager>(); if (rm) rm.enabled = false;
            foreach (var old in Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None)) Object.Destroy(old.gameObject);
            yield return null;
            var player = PlayerController.Instance;
            var at = player ? player.transform.position + new Vector3(2.5f, 0, 3.5f) : Vector3.zero;
            if (rm) at = rm.ClampToRoom(at, 2f);
            var e = EnemyFactory.MidBoss(at, Tables.Chapter(5));
            if (e.brain) e.brain.enabled = false;
            var cam = new GameObject("ShatterDemoCam").transform; cam.position = at;
            yield return new WaitForSeconds(0.8f);
            FrameCapture.Begin(55, 0.04f, System.IO.Path.Combine(folder, "ch5_mirrorcat_shatter"), 720, cam, new Vector3(0, 5.5f, -5f));
            yield return new WaitForSeconds(0.35f);
            var h = e.GetComponent<Health>();
            h.minHp = 0; h.invulnerable = false;
            h.TakeDamage(h.hp + 10);
            var cap = GameObject.Find("FrameCapture");
            while (cap) yield return null;
            Debug.Log("ShatterDemo DONE");
        }

        // ---------- 휘두르기 손맛 확인: 휘두르는 적 모델 4개를 나란히 세우고 휘두르기 상태를 차례로 재생 ----------
        public static void RunSwings(string folder) => new GameObject("SwingDemoRunner").AddComponent<Runner>().StartCoroutine(SwingSequence(folder));

        static IEnumerator SwingSequence(string folder)
        {
            var rm = Object.FindFirstObjectByType<RunManager>(); if (rm) rm.enabled = false;
            foreach (var old in Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None)) Object.Destroy(old.gameObject);
            var player = PlayerController.Instance;
            var basePos = player ? player.transform.position + new Vector3(0, 0, 4f) : Vector3.zero;
            if (rm) basePos = rm.ClampToRoom(basePos, 3f);
            var names = new[] { "Ch4_Knight", "Ch4_Assassin", "Ch5_Assassin", "Ch3_Blade" };
            var rx = new System.Text.RegularExpressions.Regex("(Swing|Slash|Whirl|Stab|Cut)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            var actors = new List<(Animator a, List<string> states)>();
            for (int i = 0; i < names.Length; i++)
            {
                var pf = Resources.Load<GameObject>("Enemies/" + names[i]);
                if (!pf) continue;
                var root = new GameObject("SwingDemo_" + names[i]);
                root.transform.position = basePos + Vector3.right * (i - 1.5f) * 2.6f;
                root.transform.rotation = Quaternion.LookRotation(Vector3.back);
                var m = Object.Instantiate(pf, root.transform, false);
                GroundFeet.Add(m);
                var an = m.GetComponentInChildren<Animator>();
                var st = new List<string>();
#if UNITY_EDITOR
                if (an && an.runtimeAnimatorController is UnityEditor.Animations.AnimatorController ac)
                    foreach (var s in ac.layers[0].stateMachine.states) if (s.state.motion && rx.IsMatch(s.state.motion.name)) st.Add(s.state.name);
#endif
                if (an) actors.Add((an, st));
            }
            var cam = new GameObject("SwingDemoCam").transform; cam.position = basePos;
            yield return new WaitForSeconds(0.5f);
            FrameCapture.Begin(90, 0.04f, System.IO.Path.Combine(folder, "swings"), 960, cam, new Vector3(0, 3.2f, -7.5f));
            for (int round = 0; round < 3; round++)
            {
                foreach (var (an, st) in actors) if (st.Count > 0) an.CrossFadeInFixedTime(st[round % st.Count], 0.05f);
                yield return new WaitForSeconds(1.3f);
            }
            var cap = GameObject.Find("FrameCapture");
            while (cap) yield return null;
            foreach (var (an, _) in actors) if (an) Object.Destroy(an.transform.root.gameObject);
            Debug.Log("SwingDemo DONE");
        }

        // ---------- 최종 보스 대낫 확인: 시간의 주인을 꺼내 자동 행동은 끄고 대낫 베기 → 유령 돌진 베기를 직접 부름 ----------
        public static void RunReaper(string folder) => new GameObject("ReaperDemoRunner").AddComponent<Runner>().StartCoroutine(ReaperSequence(folder));

        static IEnumerator ReaperSequence(string folder)
        {
            var rm = Object.FindFirstObjectByType<RunManager>(); if (rm) rm.enabled = false;
            foreach (var old in Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None)) Object.Destroy(old.gameObject);
            var player = PlayerController.Instance;
            if (!player) yield break;
            player.health.ResetHp(player.health.maxHp); player.health.invulnerable = true;
            var at = player.transform.position + new Vector3(-3f, 0, 5f);
            if (rm) at = rm.ClampToRoom(at, 3f);
            var e = EnemyFactory.Boss(at, Tables.Chapter(5));
            var brain = e.GetComponent<Ch5Boss>();
            yield return new WaitForSeconds(0.6f);
            e.enabled = false;                                 // 패턴 추첨 멈춤 (코루틴은 brain에서 계속 돎)
            var cam = new GameObject("ReaperDemoCam").transform; cam.position = Vector3.Lerp(at, player.transform.position, 0.2f) + Vector3.up * 2.5f;
            FrameCapture.Begin(150, 0.04f, System.IO.Path.Combine(folder, "reaper"), 960, cam, new Vector3(0, 17f, -15f));
            var bf = BindingFlags.NonPublic | BindingFlags.Instance;
            // 떠서 미끄러지는 이동 1.4초 (HoverFloat · Ch5M_BossWalk)
            var ban = e.GetComponentInChildren<Animator>();
            for (float t = 0; t < 1.4f; t += Time.deltaTime)
            {
                if (ban) ban.SetFloat("Speed", 1f);
                var dirM = Vector3.right * (t < 0.7f ? 1f : -1f);
                e.transform.position += dirM * 3.5f * Time.deltaTime;
                e.transform.rotation = Quaternion.Slerp(e.transform.rotation, Quaternion.LookRotation(dirM), 6f * Time.deltaTime);
                yield return null;
            }
            if (ban) ban.SetFloat("Speed", 0f);
            e.transform.rotation = Quaternion.LookRotation(Effects.Flat(player.transform.position - e.transform.position));
            var reap = (IEnumerator)typeof(Ch5Boss).GetMethod("Reap180", bf).Invoke(brain, null);
            yield return brain.StartCoroutine(reap);
            yield return new WaitForSeconds(0.3f);
            var ghost = (IEnumerator)typeof(Ch5Boss).GetMethod("GhostReap", bf).Invoke(brain, null);
            yield return brain.StartCoroutine(ghost);
            var cap = GameObject.Find("FrameCapture");
            while (cap) yield return null;
            player.health.invulnerable = false;
            Debug.Log("ReaperDemo DONE");
        }

        // ---------- 큰 무기 휘두르기 확인 (WeaponSwingRig): 시침 내려찍기 · 대검 회전 → 진자 기사 좌우 낫 → 최종 보스 대낫 · 유령 돌진 ----------
        public static void RunHeavy(string folder) => new GameObject("HeavyDemoRunner").AddComponent<Runner>().StartCoroutine(HeavySequence(folder));

        static IEnumerator HeavySequence(string folder)
        {
            Time.timeScale = 1;
            var rm = Object.FindFirstObjectByType<RunManager>(); if (rm) rm.enabled = false;
            foreach (var old in Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None)) Object.Destroy(old.gameObject);
            yield return null;
            var player = PlayerController.Instance;
            if (!player) yield break;
            player.health.ResetHp(player.health.maxHp); player.health.invulnerable = true;
            var bf = BindingFlags.NonPublic | BindingFlags.Instance;
            // ① 시침
            var at = player.transform.position + new Vector3(0, 0, -6f);           // 시침을 고양이 아래(화면 앞)에 → 내려찍기 길 · 파동이 화면 안쪽으로
            if (rm) { at = rm.ClampToRoom(at, 7f); player.transform.position = rm.ClampToRoom(at + new Vector3(0, 0, 7f), 2f); }
            var e = EnemyFactory.Boss(at, Tables.Chapter(4));
            foreach (var td in Object.FindObjectsByType<TwinDirector>(FindObjectsSortMode.None)) Object.Destroy(td.gameObject);
            var minute = GameObject.Find("Boss_MinuteHand"); if (minute) Object.Destroy(minute);
            var brain = e.GetComponent<Ch4HourBrain>();
            var keep = e;
            new GameObject("DemoKeepClear").AddComponent<Runner>().StartCoroutine(KeepClear(() => keep));
            yield return new WaitForSeconds(0.6f);
            e.enabled = false;
            var cam = new GameObject("HeavyDemoCam").transform; cam.position = Vector3.Lerp(at, player.transform.position, 0.5f);
            var hrig = e.GetComponent<WeaponSwingRig>();
            Debug.Log($"HeavyDemo 시침 칼빛 반지름 {(hrig ? hrig.EffectRadius(1f) : 0):0.0}m");
            FrameCapture.Begin(105, 0.04f, System.IO.Path.Combine(folder, "hour_greatsword"), 960, cam, new Vector3(0, 13f, -12f));
            yield return new WaitForSeconds(0.3f);
            yield return brain.StartCoroutine((IEnumerator)typeof(Ch4TwinBrain).GetMethod("Slam", bf).Invoke(brain, null));
            yield return new WaitForSeconds(0.3f);
            yield return brain.StartCoroutine((IEnumerator)typeof(Ch4TwinBrain).GetMethod("Spin", bf).Invoke(brain, null));
            var cap = GameObject.Find("FrameCapture");
            while (cap) yield return null;
            Object.Destroy(e.gameObject); Object.Destroy(cam.gameObject);
            yield return null;
            // ② 진자 기사 (행동 그대로: 고양이 3m 앞에서 좌우 번갈아 휘두름)
            var make4 = typeof(EnemyFactory).GetMethod("Make4", BindingFlags.NonPublic | BindingFlags.Static);
            var kat = player.transform.position + new Vector3(1.5f, 0, 3.2f);
            if (rm) kat = rm.ClampToRoom(kat, 2f);
            var k = (Enemy)make4.Invoke(null, new object[] { "Enemy_PendulumKnight", "Ch4_Knight", kat, Tables.Chapter(4), false, 416f, Enemy.Kind.PendulumKnight, 0.95f, 3.0f, 2.8f, 0f, (float)Enemy.PKDmg });
            keep = k;
            var kcam = new GameObject("HeavyDemoCam2").transform; kcam.position = Vector3.Lerp(kat, player.transform.position, 0.4f);
            yield return new WaitForSeconds(0.4f);
            FrameCapture.Begin(90, 0.04f, System.IO.Path.Combine(folder, "knight_scythe"), 960, kcam, new Vector3(0, 6.5f, -6.5f));
            cap = GameObject.Find("FrameCapture");
            while (cap) yield return null;
            Object.Destroy(k.gameObject); Object.Destroy(kcam.gameObject);
            yield return null;
            // ③ 최종 보스
            yield return ReaperSequence(folder);
            player.health.invulnerable = false;
            Debug.Log("HeavyDemo DONE");
        }

        // 촬영 동안 대상 말고 새로 생긴 적은 계속 지움 (방 웨이브 · 소환 때문에 화면이 어지러워지지 않게)
        static IEnumerator KeepClear(System.Func<Enemy> keep)
        {
            for (float t = 0; t < 40f; t += Time.unscaledDeltaTime)
            {
                var k = keep();
                foreach (var o in Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None))
                    if (o != k && !o.GetComponent<Ch5Boss>() && o.name != "Boss_LordOfTime") Object.Destroy(o.gameObject);
                yield return null;
            }
        }

        public static void Run(string folder) => new GameObject("WalkDemoRunner").AddComponent<Runner>().StartCoroutine(Sequence(folder));

        class Runner : MonoBehaviour { }

        static IEnumerator Sequence(string folder)
        {
            Time.timeScale = 1;
            HeavyStep.ShowFootprint = true;
            var rm = Object.FindFirstObjectByType<RunManager>();
            if (rm) rm.enabled = false;
            var player = PlayerController.Instance;
            var basePos = player ? player.transform.position + new Vector3(4f, 0, 4f) : Vector3.zero;   // 원 중심 = 고양이 앞 4m
            var make5 = typeof(EnemyFactory).GetMethod("Make5", BindingFlags.NonPublic | BindingFlags.Static);
            var list = new (string name, int ch, float camK)[] { ("ch1_giant", 1, 1.6f), ("ch2_loco", 2, 1.7f), ("ch4_hour", 4, 1.4f), ("ch5_ruin", 5, 1.0f), ("ch5_mirrorcat", 6, 0.7f) };
            foreach (var (name, chId, camK) in list)
            {
                foreach (var old in Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None)) Object.Destroy(old.gameObject);
                yield return null;
                var ch = Tables.Chapter(Mathf.Min(chId, 5));
                Enemy e = chId switch
                {
                    5 => (Enemy)make5.Invoke(null, new object[] { "Enemy_RuinGiant", "Ch5_Giant", basePos, ch, false, 544f, Enemy.Kind.RuinGiant, 1.0f, 3.4f, 3.2f, 0f, 0f }),
                    6 => EnemyFactory.MidBoss(basePos, ch),
                    _ => EnemyFactory.Boss(basePos, ch),
                };
                // 행동 · 쌍둥이 지휘 · 분침은 끔 (걷기만)
                foreach (var b in e.GetComponents<MonoBehaviour>()) if (!(b is HeavyStep) && !(b is Health)) b.enabled = false;
                foreach (var td in Object.FindObjectsByType<TwinDirector>(FindObjectsSortMode.None)) Object.Destroy(td.gameObject);
                var minute = GameObject.Find("Boss_MinuteHand");
                if (minute) Object.Destroy(minute);
                foreach (var rb in e.GetComponentsInChildren<Rigidbody>()) rb.isKinematic = true;
                var w = e.gameObject.AddComponent<WalkDemo>();
                w.speed = e.speed;
                yield return new WaitForSeconds(0.6f);
                // 바닥과 모델 사이 틈 (뜸 확인): 렌더러 경계 가장 낮은 점 − 루트 높이
                float low = float.MaxValue;
                foreach (var r in e.GetComponentsInChildren<Renderer>()) if (r.enabled && !(r is LineRenderer) && !(r is TrailRenderer)) low = Mathf.Min(low, r.bounds.min.y);
                Debug.Log($"WalkDemo {name}: 바닥 틈 {low - e.transform.position.y:0.00}m");
                FrameCapture.Begin(48, 0.06f, System.IO.Path.Combine(folder, name), 720, e.transform, new Vector3(0, 6.5f, -5.5f) * camK);
                var cap = GameObject.Find("FrameCapture");
                while (cap) yield return null;          // 캡처가 끝난 뒤 지움 (먼저 지우면 따라가던 카메라가 사라진 대상을 찾음)
                Object.Destroy(e.gameObject);
            }
            HeavyStep.ShowFootprint = false;
            Debug.Log("WalkDemo DONE");
        }
    }
}
