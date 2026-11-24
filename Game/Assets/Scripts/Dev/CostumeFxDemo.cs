// 개발용: 거울 망토 그림자 자객(ShadowStrike) 녹화 — 고양이 둘레에 적 3을 세워 기절시키고, 게임 시간 30fps 고정으로 발동 장면을 프레임 저장
// 플레이 중에 CostumeFxDemo.Mirror(folder) 호출 (MCP execute_code). 결과: folder/f000.png ... → docs/tools/fx_frames_to_gif.py
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CatTang.Dev
{
    public class CostumeFxDemo : MonoBehaviour
    {
        public static void Mirror(string folder, int frames = 42, float slow = 1f) =>
            new GameObject("CostumeFxDemo").AddComponent<CostumeFxDemo>().StartCoroutine(Run(folder, frames, slow));

        /// <summary>나머지 코스튬 효과 확인: 장의사 처형 회복 · 모래 소용돌이 · 잠옷 오라를 차례로 띄워 folder/undertaker|sand|pajama/f_000.png로 저장</summary>
        public static void Others(string folder) =>
            new GameObject("CostumeFxDemo").AddComponent<CostumeFxDemo>().StartCoroutine(RunOthers(folder));

        static IEnumerator Shoot(string dir, int frames, int every)
        {
            System.IO.Directory.CreateDirectory(dir);
            foreach (var f in System.IO.Directory.GetFiles(dir, "f*.png")) System.IO.File.Delete(f);
            var shots = new List<Texture2D>();
            for (int i = 0; i < frames * every; i++)
            {
                yield return new WaitForEndOfFrame();
                if (i % every == 0) shots.Add(ScreenCapture.CaptureScreenshotAsTexture());
            }
            for (int i = 0; i < shots.Count; i++) { System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, $"f_{i:000}.png"), shots[i].EncodeToPNG()); Destroy(shots[i]); }
        }

        static IEnumerator RunOthers(string folder)
        {
            var p = PlayerController.Instance;
            if (!p) yield break;
            p.health.invulnerable = true;
            foreach (var e in new List<Enemy>(Enemy.All)) if (e) Destroy(e.gameObject);
            yield return new WaitForSeconds(0.5f);
            var c = p.transform.position;
            Time.captureFramerate = 30;
            // 장의사: 처형한 적 자리에서 잉크 · 혼 줄기 → 고양이
            CostumeFx.ExecuteHeal(c + new Vector3(2.6f, 0, 1.2f), p.transform);
            yield return Shoot(System.IO.Path.Combine(folder, "undertaker"), 24, 1);
            // 모래 순례자: 맞힌 자리 소용돌이 (2초)
            SandVortex.Spawn(c + new Vector3(2.4f, 0, 0.8f), 5f, 0.3f, 2f);
            yield return Shoot(System.IO.Path.Combine(folder, "sand"), 30, 2);
            // 노인의 잠옷: 체력 40% 미만 → 펄스 + 오라
            int keep = Meta.SelectedCostume;
            Meta.SelectedCostume = 4008;
            p.health.hp = p.health.maxHp * 0.3f;
            LowHpAura.Attach(p);
            yield return Shoot(System.IO.Path.Combine(folder, "pajama"), 30, 2);
            var aura = p.GetComponent<LowHpAura>(); if (aura) Destroy(aura);
            Meta.SelectedCostume = keep;
            p.health.hp = p.health.maxHp;
            Time.captureFramerate = 0;
            Debug.Log("CostumeFxDemo OTHERS DONE");
        }

        static IEnumerator Run(string folder, int frames, float slow)
        {
            System.IO.Directory.CreateDirectory(folder);
            foreach (var f in System.IO.Directory.GetFiles(folder, "f*.png")) System.IO.File.Delete(f);
            var p = PlayerController.Instance;
            if (!p) { Debug.LogWarning("CostumeFxDemo: 플레이어 없음"); yield break; }
            p.health.invulnerable = true;
            foreach (var e in new List<Enemy>(Enemy.All)) if (e) Destroy(e.gameObject);
            yield return null;
            var ch = RunManager.Instance ? RunManager.Instance.Chapter : null;
            if (RunManager.Instance) { var cc = RunManager.Instance.ClampToRoom(p.transform.position + new Vector3(-3f, 0, 4f), 6.5f); cc.y = p.transform.position.y; p.transform.position = cc; }   // 벽에서 떨어진 방 가운데 쪽
            yield return new WaitForSeconds(0.5f);                           // 카메라가 따라옴
            var c = p.transform.position;
            var spots = new[] { new Vector3(3.0f, 0, 1.8f), new Vector3(-3.2f, 0, 1.2f), new Vector3(1.0f, 0, -2.2f) };
            var foes = new List<Enemy>();
            foreach (var s in spots)
            {
                var e = EnemyFactory.Outlaw(c + s, ch);
                if (!e) continue;
                e.health.maxHp = e.health.hp = 99999f;
                foes.Add(e);
            }
            yield return new WaitForSeconds(0.6f);
            foreach (var e in foes) if (e) e.health.ApplyStun(30f);
            Time.captureFramerate = Mathf.RoundToInt(30 * slow);              // 게임 시간 고정 간격 (30fps로 틀면 slow배 느리게)
            var shots = new List<Texture2D>();
            var sp = Camera.main ? Camera.main.WorldToScreenPoint(c + Vector3.up) : Vector3.zero;
            Debug.Log($"CostumeFxDemo CENTER {sp.x:0} {sp.y:0} {Screen.width}x{Screen.height}");
            ShadowStrike.Launch(p, 1f);
            for (int i = 0; i < frames; i++)
            {
                yield return new WaitForEndOfFrame();
                shots.Add(ScreenCapture.CaptureScreenshotAsTexture());
            }
            Time.captureFramerate = 0;
            for (int i = 0; i < shots.Count; i++)
            {
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(folder, $"f_{i:000}.png"), shots[i].EncodeToPNG());
                Destroy(shots[i]);
            }
            foreach (var e in foes) if (e) Destroy(e.gameObject);
            Debug.Log("CostumeFxDemo DONE " + shots.Count);
        }
    }
}
