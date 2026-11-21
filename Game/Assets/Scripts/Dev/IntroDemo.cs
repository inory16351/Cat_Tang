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
            foreach (var s in states.Split(','))
            {
                e.Anim(s);
                yield return new WaitForSeconds(each);
            }
            Destroy(cap.gameObject);
            CameraFollow.Hold = false; cam.fieldOfView = savedFov;
            foreach (var c in hud) if (c) c.enabled = true;
            Destroy(camT.gameObject); Destroy(e.gameObject);
            player.health.invulnerable = false;
            Debug.Log("MovesDemo DONE");
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
