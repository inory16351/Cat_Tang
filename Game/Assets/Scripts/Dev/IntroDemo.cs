// 개발용: 보스 등장 연출(BossIntro) 확인 — 고양이 앞에 보스를 세우고 연출을 돌리며 화면(UI 포함)을 일정 간격으로 저장
// 플레이 중에 IntroDemo.Run(folder, "1m,1b,...") 호출 (MCP execute_code). 결과: folder/<id>/f00.png ...
using System.Collections;
using UnityEngine;

namespace CatTang.Dev
{
    public class IntroDemo : MonoBehaviour
    {
        public static void Run(string folder, string list, float interval = 0.2f) =>
            new GameObject("IntroDemoRunner").AddComponent<IntroDemo>().StartCoroutine(Sequence(folder, list, interval));

        static IEnumerator Sequence(string folder, string list, float interval)
        {
            Time.timeScale = 1;
            var rm = FindFirstObjectByType<RunManager>(); if (rm) rm.enabled = false;
            var player = PlayerController.Instance;
            if (!player) yield break;
            player.health.invulnerable = true;
            foreach (var item in list.Split(','))
            {
                int ch = item[0] - '0'; bool mid = item.EndsWith("m");
                foreach (var old in FindObjectsByType<Enemy>(FindObjectsSortMode.None)) Destroy(old.gameObject);
                foreach (var td in FindObjectsByType<TwinDirector>(FindObjectsSortMode.None)) Destroy(td.gameObject);
                yield return null;
                var at = player.transform.position + new Vector3(0, 0, 9f);
                if (rm) at = rm.ClampToRoom(at, 4f);
                var e = mid ? EnemyFactory.MidBoss(at, Tables.Chapter(ch)) : EnemyFactory.Boss(at, Tables.Chapter(ch));
                var def = BossIntro.Def(ch, mid);
                string dir = System.IO.Path.Combine(folder, def.id);
                System.IO.Directory.CreateDirectory(dir);
                var cap = new GameObject("IntroCap").AddComponent<IntroDemo>();
                cap.StartCoroutine(Capture(dir, interval));
                yield return BossIntro.Play(e, ch, mid);
                Destroy(cap.gameObject);
                yield return new WaitForSeconds(0.5f);
            }
            foreach (var old in FindObjectsByType<Enemy>(FindObjectsSortMode.None)) Destroy(old.gameObject);
            player.health.invulnerable = false;
            Debug.Log("IntroDemo DONE");
        }

        /// <summary>보스 동작 확인: 보스를 세우고 두뇌를 끈 채 상태를 하나씩 재생하며 찍음 (folder/moves_&lt;id&gt;)</summary>
        public static void RunMoves(string folder, int ch, bool mid, string states, float each = 1.8f) =>
            new GameObject("MovesDemoRunner").AddComponent<IntroDemo>().StartCoroutine(MovesSeq(folder, ch, mid, states, each));

        static IEnumerator MovesSeq(string folder, int ch, bool mid, string states, float each)
        {
            Time.timeScale = 1;
            var rm = FindFirstObjectByType<RunManager>(); if (rm) rm.enabled = false;
            var player = PlayerController.Instance;
            if (!player) yield break;
            player.health.invulnerable = true;
            foreach (var old in FindObjectsByType<Enemy>(FindObjectsSortMode.None)) Destroy(old.gameObject);
            yield return null;
            var at = player.transform.position + new Vector3(0, 0, 7f);
            if (rm) at = rm.ClampToRoom(at, 4f);
            var e = mid ? EnemyFactory.MidBoss(at, Tables.Chapter(ch)) : EnemyFactory.Boss(at, Tables.Chapter(ch));
            yield return null;
            foreach (var b in e.GetComponents<BossBrain>()) b.enabled = false;
            e.enabled = false;
            foreach (var td in FindObjectsByType<TwinDirector>(FindObjectsSortMode.None)) Destroy(td.gameObject);   // 쌍둥이 지휘가 자리를 옮기지 않게
            foreach (var o in FindObjectsByType<Enemy>(FindObjectsSortMode.None)) if (o != e) Destroy(o.gameObject);
            e.transform.rotation = Quaternion.LookRotation(Effects.Flat(player.transform.position - e.transform.position));
            var camT = new GameObject("MovesCam").transform;
            var cam = Camera.main; CameraFollow.Hold = true;
            var hud = new System.Collections.Generic.List<Canvas>();
            foreach (var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None)) if (c.isRootCanvas && c.enabled) { c.enabled = false; hud.Add(c); }
            var fwd = e.transform.forward;
            float savedFov = cam.fieldOfView; cam.fieldOfView = 40;
            cam.transform.position = e.transform.position + Quaternion.Euler(0, 25, 0) * fwd * 8.5f + Vector3.up * 2.6f;
            cam.transform.LookAt(e.transform.position + Vector3.up * 1.9f);
            string dir = System.IO.Path.Combine(folder, "moves_" + (mid ? "mid" : "boss") + ch);
            System.IO.Directory.CreateDirectory(dir);
            var cap = new GameObject("IntroCap").AddComponent<IntroDemo>();
            cap.StartCoroutine(Capture(dir, 0.06f));
            var an = e.GetComponentInChildren<Animator>();
            foreach (var s in states.Split(','))
            {
                // "Run" = 걷기 값을 1로 붙잡음 (두뇌를 꺼서 아무도 안 넣어 줌), "Spin" = 눈빛 꼬리를 켠 채 제자리 한 바퀴 반 (꼬리가 눈에 붙었는지)
                if (an) an.SetFloat("Speed", s == "Run" ? 1f : 0f);
                if (s == "Spin")
                {
                    var tr = e.GetComponent<EyeTrail>();
                    var r0 = e.transform.rotation;
                    for (float t = 0; t < each; t += Time.deltaTime)
                    {
                        if (tr) tr.Hot(0.1f);
                        e.transform.rotation = r0 * Quaternion.Euler(0, 540f * Mathf.SmoothStep(0, 1, t / each), 0);
                        yield return null;
                    }
                    e.transform.rotation = r0;
                    continue;
                }
                if (s != "Run") e.Anim(s);
                yield return new WaitForSeconds(each);
            }
            Destroy(cap.gameObject);
            CameraFollow.Hold = false; cam.fieldOfView = savedFov;
            foreach (var c in hud) if (c) c.enabled = true;
            Destroy(camT.gameObject); Destroy(e.gameObject);
            player.health.invulnerable = false;
            Debug.Log("MovesDemo DONE");
        }

        /// <summary>
        /// 눈 자리 맞추기 (편집 모드에서도 됨): 모델 프리팹마다 얼굴 정면 · 옆 그림에 번호 붙은 점을 높이별로 찍어 저장.
        /// 점 = 머리 뼈 위아래로 키의 3%씩, 그 높이 띠에서 가장 앞 얼굴 면 위. folder/eyecal_<모델>_{0,70}.png + eyecal.json (점 화면 위치 · 머리 뼈 로컬 좌표)
        /// </summary>
        static readonly System.Collections.Generic.List<(string name, GameObject go)> calib = new();
        /// <summary>1단계: 모델들을 멀리 줄지어 세움 (플레이 중, 다음 프레임에 EyeCalib으로 측정 — 만든 프레임엔 스킨이 아직 안 돌아 구운 메시가 빔)</summary>
        public static string EyeCalibSpawn(string models)
        {
            foreach (var (_, g) in calib) if (g) Destroy(g);
            calib.Clear();
            int k = 0;
            foreach (var name in models.Split(','))
            {
                var pf = Resources.Load<GameObject>("Enemies/" + name);
                if (!pf) continue;
                calib.Add((name, Instantiate(pf, new Vector3(500 + 30 * k++, 0, 500), Quaternion.identity)));
            }
            return calib.Count + " spawned";
        }
        public static string EyeCalib(string folder, string models)
        {
            System.IO.Directory.CreateDirectory(folder);
            var json = new System.Text.StringBuilder("[");
            var camGo = new GameObject("EyeCalCam"); var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 28; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.12f, 0.12f, 0.16f); cam.nearClipPlane = 0.02f;
            var rt = new RenderTexture(512, 512, 24); cam.targetTexture = rt;
            var light = new GameObject("EyeCalLight").AddComponent<Light>(); light.type = LightType.Directional; light.transform.rotation = Quaternion.Euler(30, 160, 0);
            string log = "";
            foreach (var (name, go) in calib)
            {
                if (!go) continue;
                var an = go.GetComponentInChildren<Animator>();
                var head = an && an.isHuman ? an.GetBoneTransform(HumanBodyBones.Head) : null;
                if (!head) { log += name + " nohead; "; Destroy(go); continue; }
                // 몸 전체 정점 (얼굴 면 깊이용)
                var pts = new System.Collections.Generic.List<Vector3>();
                foreach (var sm in go.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    var m = new Mesh(); sm.BakeMesh(m, true);
                    var M = sm.transform.localToWorldMatrix;
                    foreach (var v in m.vertices) pts.Add(M.MultiplyPoint3x4(v));
                    DestroyImmediate(m);
                }
                float minY = float.MaxValue, maxY = float.MinValue; foreach (var p in pts) { minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y); }
                float H = maxY - minY, step = H * 0.03f;
                var fw = go.transform.forward; var c = head.position;
                var marks = new System.Collections.Generic.List<(int i, Vector3 p)>();
                var balls = new System.Collections.Generic.List<GameObject>();
                for (int i = -12; i <= 3; i++)
                {
                    float y = c.y + i * step, z = float.MinValue;
                    foreach (var p in pts) if (Mathf.Abs(p.y - y) < step * 0.5f && Mathf.Abs(p.x - c.x) < H * 0.03f) z = Mathf.Max(z, Vector3.Dot(p - c, fw));
                    if (z == float.MinValue) continue;
                    var at = new Vector3(c.x, y, c.z) + fw * (z + H * 0.005f);
                    marks.Add((i, at));
                    var b = GameObject.CreatePrimitive(PrimitiveType.Sphere); DestroyImmediate(b.GetComponent<Collider>());
                    b.transform.position = at; b.transform.localScale = Vector3.one * H * 0.012f;
                    var mr = b.GetComponent<Renderer>(); mr.sharedMaterial = new Material(Shader.Find("Sprites/Default")) { color = Color.HSVToRGB(((i + 12) * 0.13f) % 1f, 1, 1) };
                    balls.Add(b);
                }
                foreach (var bb in balls) DestroyImmediate(bb);
                balls.Clear(); marks.Clear();
                if (EyeTrail.FindEyes(go, name.StartsWith("Ch5") ? Enemy.Ch5Magenta : (Color?)null, out var eL, out var eR))
                    foreach (var ep in new[] { eL, eR })
                    {
                        var b = GameObject.CreatePrimitive(PrimitiveType.Sphere); DestroyImmediate(b.GetComponent<Collider>());
                        b.transform.position = ep; b.transform.localScale = Vector3.one * H * 0.014f;
                        b.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Sprites/Default")) { color = Color.green };
                        balls.Add(b);
                    }
                else log += "(noeyes)";
                var mid = (eL + eR) * 0.5f; if (mid == Vector3.zero) mid = new Vector3(c.x, c.y - 2f * step, c.z);
                foreach (var view in new[] { 0f, 70f })
                {
                    var dir = Quaternion.Euler(0, view, 0) * fw;
                    cam.transform.position = mid + dir * H * 0.75f; cam.transform.LookAt(mid);
                    cam.Render(); RenderTexture.active = rt;
                    var tex = new Texture2D(512, 512, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 512, 512), 0, 0); tex.Apply();
                    System.IO.File.WriteAllBytes(System.IO.Path.Combine(folder, $"eyecal_{name}_{view}.png"), tex.EncodeToPNG());
                    DestroyImmediate(tex);
                    foreach (var (i, p) in marks)
                    {
                        var sp = cam.WorldToScreenPoint(p); var hl = head.InverseTransformPoint(p);
                        // 머리 뼈 로컬: 몸 기준 좌우(오른쪽) 한 단위도 같이 (눈 간격을 로컬로 옮길 때)
                        var rl = head.InverseTransformVector(go.transform.right);
                        json.Append($"{{\"m\":\"{name}\",\"v\":{view},\"i\":{i},\"x\":{sp.x:0},\"y\":{512 - sp.y:0},\"hl\":[{hl.x:0.0000},{hl.y:0.0000},{hl.z:0.0000}],\"rl\":[{rl.x:0.0000},{rl.y:0.0000},{rl.z:0.0000}],\"H\":{H:0.000},\"avatar\":\"{(an.avatar ? an.avatar.name : "")}\"}},");
                    }
                }
                foreach (var b in balls) DestroyImmediate(b);
                Destroy(go);
                log += name + " ok(" + pts.Count + "); ";
            }
            calib.Clear();
            RenderTexture.active = null; cam.targetTexture = null; DestroyImmediate(camGo); DestroyImmediate(light.gameObject);
            if (json[json.Length - 1] == ',') json.Length--;
            json.Append("]");
            System.IO.File.WriteAllText(System.IO.Path.Combine(folder, "eyecal.json"), json.ToString());
            return log;
        }

        /// <summary>실제 게임 흐름(보스방 입장 → 연출)을 그대로 찍음: 장 보스방을 지은 뒤 seconds초 동안 저장</summary>
        public static void RunReal(string folder, int ch, float seconds = 14f, float interval = 0.04f, bool mid = false) =>
            new GameObject("RealDemoRunner").AddComponent<IntroDemo>().StartCoroutine(RealSeq(folder, ch, seconds, interval, mid));

        /// <summary>보스 패턴 하나만 확인: 장 보스방을 짓고 보스를 세운 뒤(두뇌 끔) 패턴 코루틴(method 이름)을 불러 게임 화면 그대로 seconds초 녹화 → folder/pattern_&lt;method&gt;</summary>
        public static void RunPattern(string folder, int ch, string method, float seconds = 8f, float interval = 0.05f) =>
            new GameObject("RealDemoRunner").AddComponent<IntroDemo>().StartCoroutine(PatternSeq(folder, ch, method, seconds, interval));

        static IEnumerator PatternSeq(string folder, int ch, string method, float seconds, float interval)
        {
            Time.timeScale = 1;
            var rm = FindFirstObjectByType<RunManager>();
            var player = PlayerController.Instance;
            if (!rm || !player) yield break;
            player.health.invulnerable = true; player.health.hp = player.health.maxHp;
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            rm.chapter = ch;
            typeof(RunManager).GetProperty("Chapter")?.SetValue(rm, Tables.Chapter(ch));
            typeof(RunManager).GetMethod("BuildStage", flags, null, new[] { typeof(int), typeof(RewardKind) }, null).Invoke(rm, new object[] { RunManager.Stages, RewardKind.Boss });
            // 방에 들어간 걸로 쳐서 등장 연출 · 웨이브가 안 나오게 (같은 프레임, RunManager.Update 전에)
            foreach (var r in rm.Layout.rooms) r.entered = true;
            foreach (var old in FindObjectsByType<Enemy>(FindObjectsSortMode.None)) Destroy(old.gameObject);
            yield return new WaitForSeconds(0.5f);
            var at = player.transform.position + new Vector3(0, 0, 7f);
            var e = EnemyFactory.Boss(rm.ClampToRoom(at, 4f), Tables.Chapter(ch));
            yield return null;
            // "Ch4MinuteBrain.Dash:3" = 그 두뇌를 가진 보스(쌍둥이 중 하나)의 패턴을 인수와 함께
            foreach (var td in FindObjectsByType<TwinDirector>(FindObjectsSortMode.None)) Destroy(td.gameObject);
            foreach (var o in FindObjectsByType<Enemy>(FindObjectsSortMode.None)) { o.enabled = false; foreach (var b in o.GetComponents<BossBrain>()) b.enabled = false; }
            object[] args = null;
            if (method.Contains(":")) { var sp = method.Split(':'); method = sp[0]; args = new object[] { int.Parse(sp[1]) }; }
            BossBrain brain = e.GetComponent<BossBrain>();
            if (method.Contains("."))
            {
                var sp = method.Split('.'); method = sp[1];
                foreach (var o in FindObjectsByType<BossBrain>(FindObjectsSortMode.None)) if (o.GetType().Name == sp[0]) { brain = o; e = o.GetComponent<Enemy>(); }
            }
            e.transform.rotation = Quaternion.LookRotation(Effects.Flat(player.transform.position - e.transform.position));
            string dir = System.IO.Path.Combine(folder, "pattern_" + brain.GetType().Name + "_" + method);
            System.IO.Directory.CreateDirectory(dir);
            var cap = new GameObject("IntroCap").AddComponent<IntroDemo>();
            cap.StartCoroutine(Capture(dir, interval));
            System.Reflection.MethodInfo m = null;   // 부모 클래스(Ch4TwinBrain 등)의 비공개 패턴도 찾게 위로 올라가며
            for (var ty = brain.GetType(); m == null && ty != null; ty = ty.BaseType) m = ty.GetMethod(method, flags | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.DeclaredOnly);
            if (m == null) { Debug.LogWarning("PatternDemo: 패턴 없음 " + method); Destroy(cap.gameObject); yield break; }
            brain.StartCoroutine((IEnumerator)m.Invoke(brain, args));
            yield return new WaitForSecondsRealtime(seconds);
            Destroy(cap.gameObject);
            foreach (var o in FindObjectsByType<Enemy>(FindObjectsSortMode.None)) Destroy(o.gameObject);
            Debug.Log("PatternDemo DONE");
        }

        /// <summary>
        /// 긴 연출 녹화 (스토리 몽타주 · 꿈): 화면을 960x540으로 줄여 JPG로 바로 저장 (메모리에 쌓지 않음 → 몇 분도 가능).
        /// 파일 이름에 실제 시각(ms) — docs 쪽 enc.py가 그 간격대로 MP4로 묶음. stop()을 부르면 멈춤
        /// </summary>
        /// <summary>꿈 n을 재생하며 녹화 (2026-11-23 배색 변주 확인용): 플레이 중 IntroDemo.RecordDream(dir, 1)</summary>
        public static void RecordDream(string dir, int n, float interval = 0.1f) =>
            new GameObject("DreamDemoRunner").AddComponent<IntroDemo>().StartCoroutine(DreamSeq(dir, n, interval));

        static IEnumerator DreamSeq(string dir, int n, float interval)
        {
            var ov = StoryOverlay.I;
            if (!ov) { Debug.LogWarning("DreamDemo: StoryOverlay 없음"); yield break; }
            var stop = Record(dir, interval);
            var d = StoryRun.Dreams[n - 1];
            yield return ov.PlayDream(n, d.title, d.fragments, authored: DreamCuts.For(n));
            stop();
            Debug.Log("DreamDemo DONE");
        }

        /// <summary>오프닝 몽타주를 재생하며 녹화: 플레이 중 IntroDemo.RecordOpening(dir)</summary>
        public static void RecordOpening(string dir, float interval = 1f / 30f) =>
            new GameObject("OpeningDemoRunner").AddComponent<IntroDemo>().StartCoroutine(OpeningSeq(dir, interval));

        static IEnumerator OpeningSeq(string dir, float interval)
        {
            var ov = StoryOverlay.I;
            if (!ov) { Debug.LogWarning("OpeningDemo: StoryOverlay 없음"); yield break; }
            var stop = Record(dir, interval);
            yield return StoryRun1.PlayOpening(ov);
            stop();
            Debug.Log("OpeningDemo DONE");
        }

        public static System.Action Record(string dir, float interval = 1f / 15f)
        {
            System.IO.Directory.CreateDirectory(dir);
            var go = new GameObject("StoryRecorder"); DontDestroyOnLoad(go);
            var me = go.AddComponent<IntroDemo>();
            bool on = true;
            me.StartCoroutine(RecordRun(dir, interval, () => on));
            return () => { on = false; if (go) Destroy(go, 1f); };
        }

        static IEnumerator RecordRun(string dir, float interval, System.Func<bool> on)
        {
            var rt = new RenderTexture(960, 540, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);   // 캡처 값은 이미 sRGB → 변환 없이 그대로 (리니어 프로젝트에서 두 번 밝아지던 문제)
            var small = new Texture2D(960, 540, TextureFormat.RGB24, false);
            float t0 = Time.realtimeSinceStartup, next = 0; int i = 0;
            while (on())
            {
                yield return new WaitForEndOfFrame();
                float now = Time.realtimeSinceStartup - t0;
                if (now < next) continue;
                next = now + interval;
                var full = ScreenCapture.CaptureScreenshotAsTexture();
                Graphics.Blit(full, rt);
                Destroy(full);
                var prev = RenderTexture.active; RenderTexture.active = rt;
                small.ReadPixels(new Rect(0, 0, 960, 540), 0, 0); small.Apply();
                RenderTexture.active = prev;
                var bytes = small.EncodeToJPG(88);
                var path = System.IO.Path.Combine(dir, $"f{i++:0000}_{Mathf.RoundToInt(now * 1000):000000}.png");   // 이름은 .png지만 내용은 JPG (enc.py 규칙)
                System.Threading.Tasks.Task.Run(() => System.IO.File.WriteAllBytes(path, bytes));
            }
            rt.Release(); Destroy(small);
        }

        /// <summary>여러 명을 차례로: "1m,1b,2m,..." (m = 중간 보스 8초 · b = 메인 보스 11초)</summary>
        public static void RunRealAll(string folder, string list, float interval = 0.05f) =>
            new GameObject("RealDemoRunner").AddComponent<IntroDemo>().StartCoroutine(RealAllSeq(folder, list, interval));

        static IEnumerator RealAllSeq(string folder, string list, float interval)
        {
            foreach (var item in list.Split(','))
            {
                bool mid = item.EndsWith("m");
                yield return RealSeq(folder, item[0] - '0', mid ? 8f : 11f, interval, mid);
                yield return new WaitForSecondsRealtime(1f);   // 캡처 저장 (OnDestroy) 끝나고 다음
            }
            Debug.Log("RealDemo ALL DONE");
        }

        static IEnumerator RealSeq(string folder, int ch, float seconds, float interval, bool mid)
        {
            Time.timeScale = 1;
            var rm = FindFirstObjectByType<RunManager>();
            var player = PlayerController.Instance;
            if (!rm || !player) yield break;
            player.health.invulnerable = true;
            player.health.hp = player.health.maxHp;
            foreach (var old in FindObjectsByType<Enemy>(FindObjectsSortMode.None)) Destroy(old.gameObject);
            foreach (var td in FindObjectsByType<TwinDirector>(FindObjectsSortMode.None)) Destroy(td.gameObject);
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            rm.chapter = ch;
            typeof(RunManager).GetProperty("Chapter")?.SetValue(rm, Tables.Chapter(ch));
            typeof(RunManager).GetField("midBossDone", flags)?.SetValue(rm, false);
            var build = typeof(RunManager).GetMethod("BuildStage", flags, null, new[] { typeof(int), typeof(RewardKind) }, null);
            build.Invoke(rm, new object[] { mid ? 1 : RunManager.Stages, mid ? RewardKind.Gold : RewardKind.Boss });
            if (mid)
            {
                // 중간 보스 홀(스테이지 2 끝에 붙은 술집 · 대합실 · 극장 …): 입구 문 바로 안쪽에 세우면 방 입장 → 연출이 실제 흐름대로 돎
                var layout = (RoomBuilder.StageLayout)typeof(RunManager).GetField("layout", flags).GetValue(rm);
                var hall = layout.rooms.Find(r => r.midBoss);
                if (hall == null) { Debug.LogWarning("RealDemo: 중간 보스 홀 없음 ch" + ch); yield break; }
                var gp = hall.entranceGate ? hall.entranceGate.transform.position : hall.center - hall.farSide * (hall.size.y / 2);
                var inward = Effects.Flat(hall.center - gp).normalized;
                player.Teleport(gp + inward * 3.5f + Vector3.up * 0.3f);
            }
            string dir = System.IO.Path.Combine(folder, mid ? $"real_ch{ch}_mid" : $"real_ch{ch}");
            System.IO.Directory.CreateDirectory(dir);
            var cap = new GameObject("IntroCap").AddComponent<IntroDemo>();
            cap.StartCoroutine(Capture(dir, interval));
            yield return new WaitForSecondsRealtime(seconds);
            Destroy(cap.gameObject);
            // 다음 녹화 전에 남은 보스가 고양이를 쓰러뜨리지 않게: 적을 치우고 무적 유지, 체력 채움
            foreach (var old in FindObjectsByType<Enemy>(FindObjectsSortMode.None)) Destroy(old.gameObject);
            foreach (var td in FindObjectsByType<TwinDirector>(FindObjectsSortMode.None)) Destroy(td.gameObject);
            player.health.hp = player.health.maxHp;
            Debug.Log("RealDemo DONE");
        }

        // 프레임은 메모리에 모았다가 끝날 때 한꺼번에 저장 (찍는 도중 인코딩하면 초당 5장밖에 안 나옴). 파일 이름에 실제 시각(ms)
        readonly System.Collections.Generic.List<(Texture2D tex, int ms)> frames = new();
        string outDir;

        static IEnumerator Capture(string dir, float interval)
        {
            var self = FindObjectsByType<IntroDemo>(FindObjectsSortMode.None);
            IntroDemo me = null;
            foreach (var s in self) if (s.name == "IntroCap") me = s;
            if (!me) yield break;
            me.outDir = dir;
            float t0 = Time.realtimeSinceStartup, next = 0;
            while (me.frames.Count < 300)
            {
                yield return new WaitForEndOfFrame();
                float now = Time.realtimeSinceStartup - t0;
                if (now < next) continue;
                next = now + interval;
                me.frames.Add((ScreenCapture.CaptureScreenshotAsTexture(), Mathf.RoundToInt(now * 1000)));
            }
        }

        void OnDestroy()
        {
            if (string.IsNullOrEmpty(outDir)) return;
            for (int i = 0; i < frames.Count; i++)
            {
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(outDir, $"f{i:000}_{frames[i].ms:00000}.png"), frames[i].tex.EncodeToJPG(85));
                Destroy(frames[i].tex);
            }
            frames.Clear();
        }
    }
}
