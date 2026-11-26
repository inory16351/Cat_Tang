// 개발용: 스토리 맵 4 '시계를 멈춘 공방'을 실제 탄으로 자동 플레이하며 녹화 (2026-11-26 검증용)
//   플레이 중 CatTang.Dev.StoryWorkshopDemo.Run(dir) → dir/rec/*.png(프레임) + dir/s*.png(장면) → docs/tools/frames_to_mp4.py로 영상
//   과녁(벽시계 · 톱니)은 그 시각이 될 때까지 빈 곳에 쏴서 맞춘 뒤 실제로 쏨. 쏘는 자리 = 과녁에서 문자판 가운데 쪽으로 5m (가구 안에 서지 않게)
//   문자판이 섞이면 내 발사 시각 숫자 칸에 서서 기다림 (그동안 안 쏨)
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace CatTang.Dev
{
    public class StoryWorkshopDemo : MonoBehaviour
    {
        static readonly BindingFlags BF = BindingFlags.NonPublic | BindingFlags.Instance;
        static readonly Vector3 O = new(-1400, 0, -1400);
        string dir;

        public static void Run(string dir) { var go = new GameObject("StoryWorkshopDemo"); var d = go.AddComponent<StoryWorkshopDemo>(); d.dir = dir.TrimEnd('/') + "/"; }

        void Log(string s) => Debug.Log("[WsDemo] " + s);
        void Shot(string n) => ScreenCapture.CaptureScreenshot(dir + n + ".png");

        static Transform Find(string n)
        {
            foreach (var t in FindObjectsByType<Transform>(FindObjectsSortMode.None)) if (t.name == n && (t.position - O).magnitude < 60f) return t;
            return null;
        }

        Vector3 dialC;

        IEnumerator FireAt(PlayerController p, Health t, int hour)
        {
            if (!t) yield break;
            var from = Effects.Flat(dialC - t.transform.position);
            var spot = Effects.Flat(t.transform.position) + (from.sqrMagnitude > 0.01f ? from.normalized : Vector3.back) * 5f;
            if (Effects.Flat(p.transform.position - spot).magnitude > 0.5f) { p.Teleport(spot + Vector3.up * 0.3f); yield return new WaitForSeconds(0.15f); }
            var to = Effects.Flat(t.transform.position - p.transform.position);
            int guard = 0;
            while (p.rev.next != hour && guard++ < 30) { p.DevFire(-to); yield return new WaitForSeconds(0.1f); }
            p.DevFire(to);
            Log($"fire {hour} -> {t.name}");
            yield return new WaitForSeconds(0.35f);
        }

        IEnumerator Start()
        {
            System.IO.Directory.CreateDirectory(dir);
            var rm = RunManager.Instance; var p = PlayerController.Instance;
            GameUI.DevAutoAdvance = true;
            bool fin = false;
            StoryWorkshop.Play(rm, () => fin = true);
            StoryWorkshop ws = null;
            while (!ws) { ws = FindFirstObjectByType<StoryWorkshop>(); yield return null; }
            while (!Find("GrandClock")) yield return null;
            foreach (var e in FindObjectsByType<Enemy>(FindObjectsSortMode.None)) if (e.health && (e.transform.position - O).magnitude > 60f) e.health.TakeDamage(1e7f);   // 판의 원래 방에 남은 적
            var sum = Vector3.zero; int n = 0;
            for (int i = 1; i <= 12; i++) { var t = Find("Tile_" + i); if (t) { sum += t.position; n++; } }
            dialC = n > 0 ? sum / n : O;
            var stop = IntroDemo.Record(dir + "rec", 1f / 15f);
            yield return new WaitForSeconds(3f); Shot("s0_arrive");
            var T = ws.GetType();
            float t0 = Time.time; bool walked = false, seenSeq = false, seenGear = false, endShot = false; int shots = 0, seqShots = 0, shuffleN = 0;
            string lastOrder = ""; float standUntil = 0;
            while (!fin && Time.time - t0 < 300f)
            {
                p.health.invulnerable = true;
                if (!ws) break;
                var nt = T.GetField("needTarget", BF).GetValue(ws) as Health;
                int nh = (int)T.GetField("needHour", BF).GetValue(ws);
                if (nt && nh > 0) { if (!seenSeq) { seenSeq = true; Shot("s1_seq"); } yield return FireAt(p, nt, nh); if (++seqShots == 4) Shot("s1_seq_round2"); continue; }
                var gh = T.GetField("gearHour", BF).GetValue(ws) as Dictionary<Health, int>;
                var gd = T.GetField("gearDone", BF).GetValue(ws) as HashSet<Health>;
                if (gh != null && gh.Count > 0)
                {
                    if (!seenGear) { seenGear = true; yield return new WaitForSeconds(0.5f); Shot("s2_gear"); }
                    Health pick = null; int ph = 0;
                    foreach (var kv in gh) if (kv.Key && !gd.Contains(kv.Key)) { pick = kv.Key; ph = kv.Value; break; }
                    if (pick) { yield return FireAt(p, pick, ph); if (gd.Count == gh.Count) { yield return new WaitForSeconds(1.5f); Shot("s2_musicbox"); } continue; }
                }
                if (!walked && !p.InputLocked) { var g = Find("GrandClock"); if (g) { walked = true; p.Teleport(g.position + Vector3.back * 4f + Vector3.up * 0.3f); Log("walk to grand clock"); } }
                // 문자판 섞임 → 내 발사 시각 칸에 서서 기다림
                var nums = T.GetField("tileNum", BF).GetValue(ws) as List<int>;
                var tiles = T.GetField("tiles", BF).GetValue(ws) as List<Transform>;
                string order = string.Join(",", nums);
                if (lastOrder != "" && order != lastOrder) standUntil = Time.time + 2.6f;
                lastOrder = order;
                if (standUntil > 0 && Time.time < standUntil + 6.5f)
                {
                    int k = nums.IndexOf(p.rev.next);
                    if (k >= 0 && tiles[k]) { var tp = tiles[k].position; p.Teleport(new Vector3(tp.x, O.y + 0.3f, tp.z)); }
                    if (Time.time > standUntil && shuffleN++ == 0) Shot("s3_shuffle_stand_" + p.rev.next);
                    yield return null; continue;
                }
                if (standUntil > 0) { standUntil = 0; Shot("s3_after_shuffle"); }
                Enemy best = null; float bd = 1e9f;
                foreach (var e in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
                    if (e.health && e.health.Alive && (e.transform.position - O).magnitude < 40f) { float d = (e.transform.position - p.transform.position).magnitude; if (d < bd) { bd = d; best = e; } }
                if (best)
                {
                    p.DevFire(Effects.Flat(best.transform.position - p.transform.position));
                    if (++shots == 20) Shot("s3_fight");
                    yield return new WaitForSeconds(0.14f);
                    continue;
                }
                if (p.InputLocked && seenGear && shots > 0 && !endShot) { endShot = true; StartCoroutine(EndShots()); }
                yield return null;
            }
            Log($"done fin={fin} t={Time.time - t0:0.0}");
            yield return new WaitForSeconds(1f);
            stop();
            GameUI.DevAutoAdvance = false;
        }

        IEnumerator EndShots() { for (int i = 0; i < 14; i++) { Shot("s4_end_" + i.ToString("00")); yield return new WaitForSeconds(1f); } }
    }
}
