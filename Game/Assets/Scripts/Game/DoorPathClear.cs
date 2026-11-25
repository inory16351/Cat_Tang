// 보상 문이 생길 때 길 막는 오브젝트 부수기 (2026-11-25 사용자: 문이 생기면 문 둘레 · 문과 방 가운데를 잇는 통로 안의 소품 · 엄폐물 · 떠다니는 잔해를 부숨)
// RunManager.SpawnDoors가 문을 다 세운 뒤 부름.
//
// 고르는 기준 (방 꾸미기 코드 RoomBuilder · Kit · Ch2~5Kit을 읽고 정함 — 태그 · 레이어는 안 씀):
//  - 대상은 방 루트(RoomInfo.root)의 바로 아래 자식만. 소품 · 엄폐물 · 지형지물 · 화약통 · 기믹은 전부 Kit.Place(…, info.root, …)로 여기 붙음.
//    벽 모듈(WallTiles · Buildings) · 복도(Corridor) · 바닥(Floor · Ground · GroundBits · FloorTiles) · 선로 묶음(Rail)은 묶음 오브젝트라 이름으로 뺌.
//  - 문(RoomDoor) · 나무 문(WoodGate) · 상인 등 상호작용 물체(Interactable) · 적 · 고양이 · 픽업(하트 · 경험치 · 모래 · 모래시계)
//    · 바닥 기믹(톱니 바닥 GearFloor · GearPinch) · 이펙트 조각은 자식 어디에 있어도 건드리지 않음.
//  - 실제로 길을 막는 것만: 켜진 일반(트리거 아님) 콜라이더가 있거나, 떠다니는 잔해(FloatingDebris) · 떠다니는 거울 조각(MirrorShard).
//    바닥에 깔린 납작한 것(높이 0.35m 미만)은 뺌.
//  - 방 가장자리(벽 선에서 0.9m 안쪽 밖)에 중심이 있거나 방 폭의 70%를 넘게 걸친 것(벽 · 바닥 판)은 뺌.
//  - 위치: 문에서 DoorRadius 안이거나, 문 → 방 가운데 선분에서 LaneHalf 안 (콜라이더 경계 기준, 없으면 그림 경계).
// 부서짐: 콜라이더를 바로 끄고(길이 곧장 열림) 기믹 스크립트를 멈춤(화약통이 터지거나 짐이 날아가지 않게) → 문에서 가까운 것부터 차례로 덜컹 →
//   자기 메시를 작게 복제한 조각이 통로 바깥쪽으로 튀어(문 조각 GateDebris 재사용) + 흙먼지 · 불똥 · 흙덩이(FX_DustClump) · 유리면 유리 파열 → 오그라들며 사라짐.
//   굳은 승객 석상(2장)은 자기 깨짐 연출(FrozenPassenger.Awaken)을 그대로 씀.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    public static class DoorPathClear
    {
        public const float DoorRadius = 3f;          // 문 둘레 (문틀 폭 2.6m + 여유)
        public const float LaneHalf = 1.5f;          // 문 → 방 가운데 통로 반폭
        const float EdgeInset = 0.9f, FlatHeight = 0.35f, SpanLimit = 0.7f;

        static readonly HashSet<string> SkipNames = new()
        {
            "Floor", "Ground", "GroundBits", "FloorTiles", "FloorClock", "FloorClockIn", "Wall", "WallTiles", "Buildings",
            "Corridor", "BackCorridor", "Rail", "ArenaFence", "Void", "RiftLight", "SparkFloorPlane",
        };
        static readonly string[] SkipPrefixes = { "Door_", "Gate", "SaloonGate", "BackGate", "Merchant", "Corridor_", "Portal_", "FloorPatch", "FloorVoidPit" };

        /// <summary>문들 둘레 · 통로의 막는 오브젝트를 부숨. 부순 개수를 돌려줌</summary>
        public static int Clear(RoomBuilder.RoomInfo room, IList<Transform> doors)
        {
            if (room == null || !room.root || doors == null || doors.Count == 0) return 0;
            var player = PlayerController.Instance;
            var center = Flat(room.center);
            int broken = 0;
            var victims = new List<Transform>();
            foreach (Transform t in room.root) victims.Add(t);      // 순회 중에 지우지 않게 먼저 모음
            foreach (var t in victims)
            {
                if (!Candidate(t, room, player, out var zone, out var vis)) continue;
                float best = float.MaxValue; Vector3 bestDoor = Vector3.zero;
                foreach (var d in doors)
                {
                    if (!d) continue;
                    var dp = Flat(d.position);
                    float near = DistToRect(zone, dp);
                    bool hit = near < DoorRadius || DistSegToRect(zone, dp, center) < LaneHalf;
                    if (hit && near < best) { best = near; bestDoor = dp; }
                }
                if (best == float.MaxValue) continue;
                // 조각이 튈 방향: 통로 바깥쪽 (통로 어느 쪽에 있느냐), 통로 위면 문 반대쪽
                var lane = center - bestDoor; lane.y = 0;
                var c = Flat(vis.center);
                Vector3 away;
                if (lane.sqrMagnitude > 0.01f)
                {
                    var side = Vector3.Cross(Vector3.up, lane.normalized);
                    float s = Vector3.Dot(c - bestDoor, side);
                    away = Mathf.Abs(s) > 0.4f ? side * Mathf.Sign(s) : (c - bestDoor).sqrMagnitude > 0.01f ? (c - bestDoor).normalized : lane.normalized;
                }
                else away = (c - bestDoor).sqrMagnitude > 0.01f ? (c - bestDoor).normalized : Vector3.forward;
                // 막힌 곳 표시(적 생성 · 지도용)에서 이 지형지물 발자국을 뺌
                room.blocks.RemoveAll(r => r.Contains(new Vector2(c.x, c.z)));
                PropCrumble.Begin(t.gameObject, Mathf.Min(0.35f, best * 0.04f), away, room.root.position.y);
                broken++;
            }
            return broken;
        }

        static bool Candidate(Transform t, RoomBuilder.RoomInfo room, PlayerController player, out Bounds zone, out Bounds vis)
        {
            zone = vis = default;
            if (!t || !t.gameObject.activeInHierarchy) return false;
            string n = t.name;
            if (SkipNames.Contains(n)) return false;
            foreach (var pre in SkipPrefixes) if (n.StartsWith(pre)) return false;
            if (player && player.transform.IsChildOf(t)) return false;
            if (t.GetComponentInChildren<Interactable>(true) || t.GetComponentInChildren<WoodGate>(true) || t.GetComponentInChildren<Enemy>(true)
                || t.GetComponentInChildren<PlayerController>(true) || t.GetComponentInChildren<HeartPickup>(true) || t.GetComponentInChildren<XpOrb>(true)
                || t.GetComponentInChildren<SandPickup>(true) || t.GetComponentInChildren<HourglassPickup>(true) || t.GetComponentInChildren<GearFloor>(true)
                || t.GetComponentInChildren<GearPinch>(true) || t.GetComponentInChildren<GateDebris>(true) || t.GetComponentInChildren<FxEffect>(true)
                || t.GetComponent<PropCrumble>()) return false;

            // 그림 경계 (메시만 — 꼭대기에 붙은 파티클(5장 떠오르는 부스러기)은 경계를 부풀리므로 뺌)
            bool any = false;
            foreach (var r in t.GetComponentsInChildren<Renderer>())
            {
                if (!r.enabled || !(r is MeshRenderer || r is SkinnedMeshRenderer)) continue;
                if (!any) { vis = r.bounds; any = true; } else vis.Encapsulate(r.bounds);
            }
            if (!any) return false;

            // 실제로 막는 것: 켜진 일반 콜라이더 · 떠다니는 잔해 · 떠다니는 거울 조각
            bool solid = false;
            foreach (var col in t.GetComponentsInChildren<Collider>())
            {
                if (!col.enabled || col.isTrigger) continue;
                if (!solid) { zone = col.bounds; solid = true; } else zone.Encapsulate(col.bounds);
            }
            bool floater = t.GetComponent<FloatingDebris>() || t.GetComponent<MirrorShard>();
            if (!solid && !floater) return false;
            if (!solid) zone = vis;
            float floorY = room.root.position.y;
            if (!floater && vis.max.y - floorY < FlatHeight) return false;               // 바닥에 깔린 납작한 것

            // 방 가장자리(벽 · 건물 줄) · 방을 가로지르는 큰 판은 뺌
            var lc = vis.center - room.center;
            float hw = room.size.x / 2 - EdgeInset, hd = room.size.y / 2 - EdgeInset;
            if (Mathf.Abs(lc.x) > hw || Mathf.Abs(lc.z) > hd) return false;
            if (vis.size.x > room.size.x * SpanLimit || vis.size.z > room.size.y * SpanLimit) return false;
            return true;
        }

        static Vector3 Flat(Vector3 v) { v.y = 0; return v; }

        /// <summary>점에서 경계 상자(XZ)까지 거리 (안이면 0)</summary>
        static float DistToRect(Bounds b, Vector3 p)
        {
            float dx = Mathf.Max(b.min.x - p.x, 0f, p.x - b.max.x);
            float dz = Mathf.Max(b.min.z - p.z, 0f, p.z - b.max.z);
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        /// <summary>선분에서 경계 상자(XZ)까지 거리 (0.25m 간격으로 짚음)</summary>
        static float DistSegToRect(Bounds b, Vector3 a, Vector3 c)
        {
            float len = Vector3.Distance(a, c);
            int steps = Mathf.Max(1, Mathf.CeilToInt(len / 0.25f));
            float best = float.MaxValue;
            for (int i = 0; i <= steps; i++)
            {
                best = Mathf.Min(best, DistToRect(b, Vector3.Lerp(a, c, i / (float)steps)));
                if (best <= 0f) break;
            }
            return best;
        }
    }

    /// <summary>문 길 트기로 부서지는 오브젝트 하나: 덜컹 → 자기 메시 조각이 튐 + 흙먼지 → 오그라들며 사라짐</summary>
    public class PropCrumble : MonoBehaviour
    {
        const float Rattle = 0.16f, Shrink = 0.14f;
        static readonly Color DustCol = FxBurst.MistDust;
        static float lastSfx = -1f;

        float delay, groundY;
        Vector3 away;

        public static void Begin(GameObject go, float delay, Vector3 away, float groundY)
        {
            if (!go || go.GetComponent<PropCrumble>()) return;
            // 길은 바로 열림
            foreach (var c in go.GetComponentsInChildren<Collider>()) c.enabled = false;
            // 굳은 승객 석상: 자기 깨짐 연출(금 → 떨림 → 조각) 그대로
            var fp = go.GetComponent<FrozenPassenger>();
            if (fp) { fp.Awaken(0.3f + delay); return; }
            // 기믹은 멈춤 (화약통 폭발 · 짐 날아감 · 밸브 증기 · 둥실 · 거울 반사가 이 틈에 일어나지 않게)
            foreach (var b in go.GetComponentsInChildren<MonoBehaviour>()) if (b) b.enabled = false;
            var pc = go.AddComponent<PropCrumble>();
            pc.delay = delay; pc.away = away.sqrMagnitude > 1e-4f ? away.normalized : Vector3.forward; pc.groundY = groundY;
        }

        IEnumerator Start()
        {
            if (delay > 0) yield return new WaitForSeconds(delay);
            var rends = new List<MeshRenderer>();
            foreach (var r in GetComponentsInChildren<MeshRenderer>())
                if (r.enabled && r.TryGetComponent<MeshFilter>(out var mf) && mf.sharedMesh) rends.Add(r);
            Bounds b = rends.Count > 0 ? rends[0].bounds : new Bounds(transform.position + Vector3.up * 0.5f, Vector3.one);
            foreach (var r in rends) b.Encapsulate(r.bounds);

            // 1) 덜컹 (점점 세게)
            var home = transform.position;
            for (float t = 0; t < Rattle; t += Time.deltaTime)
            {
                float amp = Mathf.Lerp(0.02f, 0.08f, t / Rattle);
                transform.position = home + new Vector3(Mathf.Sin(t * 71f), 0, Mathf.Sin(t * 57f + 1f)) * amp;
                yield return null;
            }
            transform.position = home;

            // 2) 펑: 조각 · 흙먼지 · 불똥
            Burst(rends, b);

            // 3) 본체는 눌리며 오그라들어 사라짐
            var s0 = transform.localScale;
            for (float t = 0; t < Shrink; t += Time.deltaTime)
            {
                float k = t / Shrink;
                transform.localScale = new Vector3(s0.x * (1 - k * 0.7f), s0.y * (1 - k), s0.z * (1 - k * 0.7f));
                yield return null;
            }
            Destroy(gameObject);
        }

        void Burst(List<MeshRenderer> rends, Bounds b)
        {
            string n = name;
            bool glass = n.Contains("Mirror") || n.Contains("Crystal") || n.Contains("Glass") || n.Contains("Shard") || GetComponent<MirrorShard>();
            var parent = transform.parent;
            var c = b.center;
            var tint = new Color(0.75f, 0.55f, 0.35f);
            if (rends.Count > 0 && rends[0].sharedMaterial && rends[0].sharedMaterial.HasProperty("_BaseColor")) tint = rends[0].sharedMaterial.GetColor("_BaseColor");

            // 자기 메시를 작게 복제한 조각 (재질 그대로): 덩치에 비례해 5~12개, 통로 바깥쪽 + 사방으로
            if (rends.Count > 0)
            {
                float vol = b.size.x * b.size.y * b.size.z;
                int count = Mathf.Clamp(Mathf.RoundToInt(4 + vol * 0.8f), 5, 12);
                for (int i = 0; i < count; i++)
                {
                    var r = rends[Random.Range(0, rends.Count)];
                    var mf = r.GetComponent<MeshFilter>();
                    var at = c + Vector3.Scale(b.extents * 0.7f, new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)));
                    var outDir = Effects.Flat(at - c);
                    outDir = outDir.sqrMagnitude > 1e-3f ? outDir.normalized : Effects.Flat(Random.onUnitSphere).normalized;
                    var v = (outDir * 0.5f + away).normalized * Random.Range(2.5f, 5.5f) + Vector3.up * Random.Range(2.5f, 5.5f);
                    // 조각 크기: 원래 크기의 22~38%, 최대 1.2m
                    float k = Random.Range(0.22f, 0.38f);
                    var sc = r.transform.lossyScale * k;
                    float big = r.bounds.size.magnitude * k;
                    if (big > 1.2f) sc *= 1.2f / big;
                    Debris(parent, mf.sharedMesh, r.sharedMaterials, at, Random.rotation, sc, v, groundY, Random.Range(0.8f, 1.3f));
                }
            }
            var foot = new Vector3(c.x, groundY + 0.15f, c.z);
            float size = Mathf.Clamp(Mathf.Max(b.size.x, b.size.z), 1f, 4f);
            FxBurst.Spawn(c, size * 1.1f, FxBurst.Kind.Dust, DustCol, 0.6f);
            FxBurst.Spawn(foot, size * 1.3f, FxBurst.Kind.Dust, DustCol, 0.65f);
            FxBurst.Spawn(c + away * 0.3f, size * 0.9f, FxBurst.Kind.Spark, tint);
            if (FxEffect.Has("FX_DustClump"))
                for (int i = 0; i < 4; i++)
                {
                    float a = i * Mathf.PI * 0.5f + Random.Range(-0.4f, 0.4f);
                    var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                    FxEffect.Spawn("FX_DustClump", foot + dir * size * 0.35f, Quaternion.LookRotation(dir), Mathf.Clamp(size * 0.45f, 0.5f, 1.2f));
                }
            if (glass)
            {
                Enemy.Fx3("GlassBurst", c, Mathf.Clamp(size * 0.6f, 0.8f, 1.6f));
                Enemy.Fx3("MirrorFlash", c + Vector3.up * 0.2f, Mathf.Clamp(size * 0.5f, 0.7f, 1.4f));
            }
            // 소리 · 흔들림은 한꺼번에 여러 개가 부서져도 한 번만
            if (Time.time - lastSfx > 0.12f)
            {
                lastSfx = Time.time;
                string sfx = glass && Sfx.Has("e_glass") ? "e_glass" : Sfx.Has("e_slam") ? "e_slam" : null;
                if (sfx != null) Sfx.Play(sfx, 0.55f, 0.08f, 2);
                CameraFollow.Shake(0.07f);
            }
        }

        static void Debris(Transform parent, Mesh mesh, Material[] mats, Vector3 pos, Quaternion rot, Vector3 scale, Vector3 vel, float groundY, float life)
        {
            var go = new GameObject("PropDebris");
            go.transform.SetParent(parent, true);
            go.transform.SetPositionAndRotation(pos, rot);
            var ls = go.transform.lossyScale;
            go.transform.localScale = new Vector3(scale.x / ls.x, scale.y / ls.y, scale.z / ls.z);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterials = mats;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var d = go.AddComponent<GateDebris>();
            d.vel = vel;
            d.spin = Random.onUnitSphere * Random.Range(250f, 700f);
            d.groundY = groundY;
            d.life = life;
            d.rend = mr;
        }
    }
}
