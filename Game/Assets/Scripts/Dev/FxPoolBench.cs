// 개발용: 이펙트 생성 방식 비교 (2026-11-20 사용자: 최적화 — 오브젝트 풀링을 지금 방식과 비교해 보고)
// 같은 이펙트 N개를 세 방식으로 띄워 잼:
//   A) 지금 방식  = 프리팹 Instantiate → 수명 끝에 Destroy (FxEffect.Spawn · FxMesh.Spawn · StylizedFx.Spawn · FxBurst 모두 이 방식)
//   B) 오브젝트 풀 = 미리 만들어 둔 복제본을 SetActive로 꺼내고 넣기
//   C) 오브젝트 없는 입자 풀 = 배열 + Graphics.DrawMesh (DashFx)
// 재는 것: 띄우는 시간 · 그때 쓰레기(GC) 할당 · 살아 있는 동안 LateUpdate 프레임 비용 · 치우는 시간. 결과는 콘솔 + JSON 파일
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Profiling;

namespace CatTang.Dev
{
    public class FxPoolBench : MonoBehaviour
    {
        public static void Run(string outFile, int n = 300) => new GameObject("FxPoolBench").AddComponent<FxPoolBench>().StartCoroutine(Go(outFile, n));

        // 관리 힙(C# 쓰레기) + 네이티브(엔진 오브젝트) 메모리 증가량. GC.GetAllocatedBytesForCurrentThread는 Unity Mono에서 0만 돌려줘서 안 씀
        static long Alloc() => System.GC.GetTotalMemory(false) + Profiler.GetTotalAllocatedMemoryLong();

        static IEnumerator LateCost(List<double> into, int frames)
        {
            var rec = Recorder.Get("LateBehaviourUpdate");
            var loop = Recorder.Get("PlayerLoop");
            rec.enabled = true; loop.enabled = true;
            yield return null;
            loopMs.Clear();
            for (int i = 0; i < frames; i++) { yield return null; into.Add(rec.elapsedNanoseconds / 1e6); loopMs.Add(loop.elapsedNanoseconds / 1e6); }
        }

        // 풀링 (2026-11-20): A는 실제 풀 도입 뒤에도 Instantiate 기준 측정으로 유지한다.
        static GameObject baselinePrefab;
        static FxEffect InstantiateBaseline(Vector3 pos)
        {
            if (!baselinePrefab) baselinePrefab = Resources.Load<GameObject>("FX/Prefabs/FX_FootDust");
            var prefab = baselinePrefab;
            if (!prefab) return null;
            var go = Object.Instantiate(prefab, pos, Quaternion.identity);
            VividLayer.SetAll(go);
            var fx = go.GetComponent<FxEffect>();
            if (fx) fx.Play(null);
            return fx;
        }

        static readonly List<double> loopMs = new();
        static double Avg(List<double> l) { double s = 0; foreach (var x in l) s += x; return l.Count > 0 ? s / l.Count : 0; }

        static IEnumerator Go(string outFile, int n)
        {
            var rm = Object.FindFirstObjectByType<RunManager>(); if (rm) rm.enabled = false;
            // 풀링 (2026-11-20): 스테이지 예열이 측정 도중 메모리와 생성 시간을 섞지 않게 멈춘다.
            if (rm) rm.StopAllCoroutines();
            foreach (var e in Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None)) Object.Destroy(e.gameObject);
            var origin = PlayerController.Instance ? PlayerController.Instance.transform.position + new Vector3(0, 0, 4) : Vector3.zero;
            var res = new System.Text.StringBuilder("{\n");
            var sw = new Stopwatch();
            yield return new WaitForSeconds(0.5f);

            // 바탕 (아무것도 없을 때 LateUpdate)
            var baseL = new List<double>(); yield return LateCost(baseL, 30);
            res.AppendLine($"  \"baseline_late_ms\": {Avg(baseL):0.000}, \"baseline_frame_ms\": {Avg(loopMs):0.000},");

            // A) Instantiate / Destroy — FX_FootDust (FxEffect + FxPart 플립북 1조각, 수명을 길게 늘려 재는 동안 살아 있게)
            InstantiateBaseline(origin);          // 첫 Resources.Load 캐시 채움 (공정하게)
            yield return null;
            var listA = new List<FxEffect>(n);
            long a0 = Alloc(); sw.Restart();
            for (int i = 0; i < n; i++) listA.Add(InstantiateBaseline(origin + Random.insideUnitSphere * 3f));
            sw.Stop(); long aAlloc = Alloc() - a0; double aSpawn = sw.Elapsed.TotalMilliseconds;
            foreach (var f in listA) if (f) foreach (var p in f.GetComponentsInChildren<FxPart>()) p.life = 30f;
            var lateA = new List<double>(); yield return LateCost(lateA, 30);
            sw.Restart();
            foreach (var f in listA) if (f) Object.DestroyImmediate(f.gameObject);
            sw.Stop(); double aKill = sw.Elapsed.TotalMilliseconds;
            res.AppendLine($"  \"A_instantiate\": {{ \"spawn_ms\": {aSpawn:0.000}, \"spawn_gc_kb\": {aAlloc / 1024.0:0.0}, \"late_ms\": {Avg(lateA):0.000}, \"frame_ms\": {Avg(loopMs):0.000}, \"destroy_ms\": {aKill:0.000} }},");
            yield return new WaitForSeconds(0.3f);

            // B) 오브젝트 풀 — 같은 프리팹을 미리 n개(꺼 둠) → SetActive + 자리 + 조각 다시 시작
            var pf = Resources.Load<GameObject>("FX/Prefabs/FX_FootDust");
            sw.Restart(); long w0 = Alloc();
            var pool = new List<GameObject>(n);
            for (int i = 0; i < n; i++) { var g = Object.Instantiate(pf); g.SetActive(false); pool.Add(g); }
            sw.Stop(); double warm = sw.Elapsed.TotalMilliseconds; long warmAlloc = Alloc() - w0;
            var parts = new List<FxPart[]>(n);
            foreach (var g in pool) parts.Add(g.GetComponentsInChildren<FxPart>(true));
            yield return null;
            long b0 = Alloc(); sw.Restart();
            for (int i = 0; i < n; i++)
            {
                var g = pool[i];
                g.transform.position = origin + Random.insideUnitSphere * 3f;
                g.SetActive(true);
                foreach (var p in parts[i]) { p.life = 30f; p.Begin(null); }
            }
            sw.Stop(); long bAlloc = Alloc() - b0; double bSpawn = sw.Elapsed.TotalMilliseconds;
            var lateB = new List<double>(); yield return LateCost(lateB, 30);
            sw.Restart();
            foreach (var g in pool) g.SetActive(false);
            sw.Stop(); double bKill = sw.Elapsed.TotalMilliseconds;
            res.AppendLine($"  \"B_pool\": {{ \"warmup_ms\": {warm:0.000}, \"warmup_gc_kb\": {warmAlloc / 1024.0:0.0}, \"spawn_ms\": {bSpawn:0.000}, \"spawn_gc_kb\": {bAlloc / 1024.0:0.0}, \"late_ms\": {Avg(lateB):0.000}, \"frame_ms\": {Avg(loopMs):0.000}, \"return_ms\": {bKill:0.000} }},");
            foreach (var g in pool) Object.Destroy(g);
            yield return new WaitForSeconds(0.3f);

            // C) 오브젝트 없는 입자 풀 — DashFx (구름 덩어리 메시 + 툰 재질)
            var mesh = DashFx.M("fx_ball_0");
            DashFx.Emit(mesh, FxMesh.Kind.Ball, origin, Vector3.zero, Vector3.one * 0.5f, 0.1f, Color.white);   // 풀 · 재질 만들기
            yield return new WaitForSeconds(0.2f);
            long c0 = Alloc(); sw.Restart();
            for (int i = 0; i < n; i++) DashFx.Emit(mesh, FxMesh.Kind.Ball, origin + Random.insideUnitSphere * 3f, Vector3.zero, Vector3.one * 0.5f, 30f, new Color(0.9f, 0.7f, 0.4f));
            sw.Stop(); long cAlloc = Alloc() - c0; double cSpawn = sw.Elapsed.TotalMilliseconds;
            var lateC = new List<double>(); yield return LateCost(lateC, 30);
            res.AppendLine($"  \"C_drawmesh_pool\": {{ \"spawn_ms\": {cSpawn:0.000}, \"spawn_gc_kb\": {cAlloc / 1024.0:0.0}, \"late_ms\": {Avg(lateC):0.000}, \"frame_ms\": {Avg(loopMs):0.000}, \"alive\": {DashFx.Alive}, \"return_ms\": 0 }},");
            // 풀링 (2026-11-20): D는 실제 Spawn으로 끝까지 재생한 뒤 같은 수를 다시 꺼낸다.
            var actual = new List<FxEffect>(n);
            for (int round = 0; round < 2; round++)
            {
                actual.Clear();
                var before = GameObjectPool.Stats("FX_FootDust");
                long d0 = Alloc(); sw.Restart();
                for (int i = 0; i < n; i++) actual.Add(FxEffect.Spawn("FX_FootDust", origin + Random.insideUnitSphere * 3f, Quaternion.identity));
                sw.Stop(); double dSpawn = sw.Elapsed.TotalMilliseconds; long dAlloc = Alloc() - d0;
                float waitingFrom = Time.realtimeSinceStartup;
                bool alive;
                do
                {
                    yield return null;
                    alive = false;
                    foreach (var fx in actual) if (fx && fx.gameObject.activeSelf) { alive = true; break; }
                } while (alive);
                var after = GameObjectPool.Stats("FX_FootDust");
                long endAlloc = Alloc() - d0;
                double waitMs = (Time.realtimeSinceStartup - waitingFrom) * 1000.0;
                res.AppendLine($"  \"D_actual_pool_{round + 1}\": {{ \"spawn_ms\": {dSpawn:0.000}, \"spawn_gc_kb\": {dAlloc / 1024.0:0.0}, \"until_return_ms\": {waitMs:0.000}, \"end_gc_kb\": {endAlloc / 1024.0:0.0}, \"created\": {after.Created - before.Created}, \"reused\": {after.Reused - before.Reused}, \"idle\": {after.Idle} }},");
            }
            res.AppendLine($"  \"n\": {n}\n}}");
            System.IO.File.WriteAllText(outFile, res.ToString());
            UnityEngine.Debug.Log("FxPoolBench DONE\n" + res);
        }
    }
}
