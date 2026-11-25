// 개발용: 2장 해금 탄 · 총 시연 녹화 (2026-11-25) — 판(Run 씬) 플레이 중 MCP execute_code로 Ch2UnlockDemo.All(폴더) 호출
//   장면마다 탄을 12칸에 채우고(Revolver.DevAllAwake로 전부 활성화) 적을 세워 쏘거나 스킬을 써서 30fps 고정으로 프레임 저장
//   결과: 폴더/<장면>/f_000.png … → docs/tools/fx_frames_to_gif.py 로 GIF
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CatTang.Dev
{
    public class Ch2UnlockDemo : MonoBehaviour
    {
        public static bool Busy;
        public static string Only;   // 장면 하나만 (null = 전부)

        public static void All(string folder, string only = null)
        {
            Only = only;
            new GameObject("Ch2UnlockDemo").AddComponent<Ch2UnlockDemo>().StartCoroutine(Run(folder));
        }

        static PlayerController P => PlayerController.Instance;

        static void Load(int bulletId)
        {
            var rev = P.rev;
            for (int i = 1; i <= 12; i++) { rev.slots[i].bullet = Tables.Bullets[bulletId]; rev.slots[i].ops.Clear(); }
            P.ResetCylinder();
        }

        static void Guns(int id)
        {
            P.rev.rightGun = Tables.Guns[id];
            P.rev.leftGun = Tables.Guns[id];
        }

        static readonly List<Enemy> foes = new();
        static readonly List<Vector3> spots = new();   // 세운 자리 (기다리는 동안 걸어 나가도 녹화 전에 되돌림)

        static void Clear()
        {
            foreach (var e in new List<Enemy>(Enemy.All)) if (e) Destroy(e.gameObject);
            foes.Clear(); spots.Clear();
        }

        static Enemy Foe(Vector3 at, float hp = 99999f)
        {
            var ch = RunManager.Instance ? RunManager.Instance.Chapter : null;
            var e = EnemyFactory.Outlaw(at, ch);
            if (!e) return null;
            e.health.maxHp = e.health.hp = hp;
            foes.Add(e); spots.Add(at);
            return e;
        }

        static IEnumerator Capture(string dir, int frames, List<Texture2D> into)
        {
            for (int i = 0; i < frames; i++)
            {
                yield return new WaitForEndOfFrame();
                into.Add(ScreenCapture.CaptureScreenshotAsTexture());
            }
        }

        static void Save(string dir, List<Texture2D> shots)
        {
            System.IO.Directory.CreateDirectory(dir);
            foreach (var f in System.IO.Directory.GetFiles(dir, "f_*.png")) System.IO.File.Delete(f);
            for (int i = 0; i < shots.Count; i++) { System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, $"f_{i:000}.png"), shots[i].EncodeToPNG()); Destroy(shots[i]); }
            shots.Clear();
        }

        static IEnumerator Wait(float s) { for (float t = 0; t < s; t += Time.deltaTime) yield return null; }

        /// <summary>장면 하나: 준비(setup) → 녹화 시작과 함께 동작(act) → frames장 저장</summary>
        static IEnumerator Scene(MonoBehaviour host, string folder, string name, int frames, System.Action setup, System.Func<Vector3, IEnumerator> act)
        {
            if (Only != null && Only != name) yield break;
            Clear();
            Revolver.DevAllAwake = true;
            yield return null;
            var c = P.transform.position;
            setup?.Invoke();
            yield return new WaitForSeconds(0.7f);
            for (int i = 0; i < foes.Count; i++) if (foes[i]) { foes[i].health.ApplyStun(30f); var q = spots[i]; q.y = foes[i].transform.position.y; foes[i].transform.position = q; }
            Time.captureFramerate = 30;
            var shots = new List<Texture2D>();
            var cap = host.StartCoroutine(Capture(folder, frames, shots));
            yield return act(c);
            while (shots.Count < frames) yield return null;
            Time.captureFramerate = 0;
            Save(System.IO.Path.Combine(folder, name), shots);
            Debug.Log($"Ch2UnlockDemo {name} {frames}");
            Clear();
            yield return new WaitForSeconds(0.4f);
        }

        static IEnumerator Run(string folder)
        {
            Busy = true;
            var host = FindFirstObjectByType<Ch2UnlockDemo>();
            var p = P;
            if (!p) { Busy = false; yield break; }
            p.health.invulnerable = true;
            if (RunManager.Instance) { var cc = RunManager.Instance.ClampToRoom(p.transform.position + new Vector3(-2f, 0, -1f), 7f); cc.y = p.transform.position.y; p.Teleport(cc); }
            yield return new WaitForSeconds(0.6f);
            var keepR = p.rev.rightGun; var keepL = p.rev.leftGun;
            var fwd = Vector3.forward;
            Vector3 c0 = p.transform.position;
            Vector3 Cl(Vector3 c) => c + new Vector3(0.6f, 0, 5f);   // 무리 가운데

            // 1. 연착 + 미련 6단계: 둔화 · 공명 강화 · 푸른 원혼
            yield return Scene(host, folder, "linger", 75, () => { Load(1047); Guns(2001); var m = Cl(p.transform.position); Foe(m); Foe(m + new Vector3(1.6f, 0, 0.6f)); Foe(m + new Vector3(-1.4f, 0, 0.9f)); },
                c => Fire(c, Cl(c), 3, 0.45f));
            // 1-2. 망각 6단계 얼음 꽃 (2026-11-25 사용자: 피어나듯이 얼어버리는 느낌으로 아래부터) — 녹화 중에 가둠 → 피어남 · 유지 · 깨짐
            yield return Scene(host, folder, "bloom", 66, () => { var m = Cl(p.transform.position); Foe(m); },
                c => Bloom());
            // 2. 끝나지 않는 여정: 공명에 둔화 · 반경 +1
            yield return Scene(host, folder, "journey", 60, () => { Load(1049); Guns(2001); var m = Cl(p.transform.position); Foe(m); Foe(m + new Vector3(2.3f, 0, 0.4f)); Foe(m + new Vector3(-2.2f, 0, 0.8f)); },
                c => Fire(c, Cl(c), 2, 0.6f));
            // 3. 환승역: 갇힌 적을 맞히면 가둠이 옆 적에게
            yield return Scene(host, folder, "transfer", 60, () =>
            {
                Load(1048); Guns(2001); var m = Cl(p.transform.position);
                var a = Foe(m); Foe(m + new Vector3(2.6f, 0, 1.2f)); Foe(m + new Vector3(-2.8f, 0, 1.8f));
                if (a) { a.health.ApplyTimeLock(4f); a.WatchTimeLock(p); OblivionFx.Lock(a.health.transform); }
            }, c => Fire(c, Cl(c), 2, 0.7f));
            // 4. 마지막 기차: 한 줄 적 전부 관통 · 끝 공명 폭발
            yield return Scene(host, folder, "train", 54, () => { Load(1052); Guns(2001); var s = p.transform.position; for (int i = 0; i < 4; i++) Foe(s + new Vector3(0.3f * i, 0, 3f + 2.1f * i)); },
                c => Fire(c, c + fwd * 10f, 1, 0.2f));
            // 5. 레일 위의 불꽃: 치명타 폭발
            yield return Scene(host, folder, "rail", 54, () => { Load(1050); Guns(2001); var m = Cl(p.transform.position); Foe(m); Foe(m + new Vector3(1.8f, 0, 0.5f)); },
                c => Fire(c, Cl(c), 3, 0.4f, true));
            // 6. 연기 고리: 처치 폭발
            yield return Scene(host, folder, "smoke", 54, () => { Load(1051); Guns(2001); var m = Cl(p.transform.position); Foe(m, 4f); Foe(m + new Vector3(2f, 0, 0.6f)); Foe(m + new Vector3(-2f, 0, 0.8f)); },
                c => Fire(c, Cl(c), 2, 0.5f));
            // 7. 가스등 랜턴: 랜턴 던지기 → 빛 장판 안 공명
            yield return Scene(host, folder, "lantern", 90, () => { Load(1047); Guns(2009); var m = Cl(p.transform.position); Foe(m); Foe(m + new Vector3(2f, 0, 0.5f)); Foe(m + new Vector3(-1.8f, 0, 1.1f)); },
                c => Lantern(c, Cl(c)));
            // 8. 기관차: 증기 돌진
            yield return Scene(host, folder, "steam", 54, () => { Load(1050); Guns(2010); var s = p.transform.position; Foe(s + new Vector3(0.4f, 0, 2.5f)); Foe(s + new Vector3(-0.5f, 0, 4.2f)); Foe(s + new Vector3(0.6f, 0, 6.6f)); },
                c => Steam(c, c + fwd * 6f));

            p.rev.rightGun = keepR; p.rev.leftGun = keepL;
            Revolver.DevAllAwake = false;
            Debug.Log("Ch2UnlockDemo ALL DONE");
            Busy = false;
        }

        static IEnumerator Fire(Vector3 c, Vector3 at, int n, float gap, bool crit = false)
        {
            yield return Wait(0.25f);
            for (int i = 0; i < n; i++) { P.DevFire(at - P.transform.position, crit); yield return Wait(gap); }
        }

        static IEnumerator Bloom()
        {
            yield return Wait(0.15f);
            if (foes.Count > 0 && foes[0]) OblivionFx.Encase(foes[0].health.transform, 1.3f, true);
        }

        static IEnumerator Lantern(Vector3 c, Vector3 at)
        {
            yield return Wait(0.2f);
            P.DevSkill(at);
            yield return Wait(0.9f);
            for (int i = 0; i < 2; i++) { P.DevFire(at - P.transform.position); yield return Wait(0.5f); }
        }

        static IEnumerator Steam(Vector3 c, Vector3 at)
        {
            yield return Wait(0.3f);
            P.DevSkill(at);
        }
    }
}
