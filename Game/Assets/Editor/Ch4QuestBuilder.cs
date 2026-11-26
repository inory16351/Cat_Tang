// 4장 클리어 퀘스트 '멈춘 마을' 마을 씬 배치 (2026-11-25 사용자, 명세 Art/tools/specs/ch4_story_spec.md "3. 마을 퀘스트")
//   섬 넷에 멈춘 시계 + 숫자 톱니(1 · 2 · 2 · 3개, 숫자는 Ch4Quest.Defs) · 광장 시계탑 정면에 11:59 바늘 한 쌍
//   모델 = 기존 키트 재사용 (새 메시 없음):
//     톱니 = 스토리 맵 4 키트(Resources/Kit/Story/W_GearFloor · W_Gear)가 있으면 그것, 없으면 4장 키트 GearFloor (쌍둥이 기믹 '멈춘 톱니'와 같은 모델)
//     멈춘 시계 = Resources/Kit/Story/W_StoppedClock · W_GrandfatherClock이 있으면 그것, 없으면 스토리 맵 1 키트 StreetClock(양면 가로등 시계)
//     11:59 바늘 = Resources/Kit/Story/DialHand (원점 = 축, +Z = 바늘 끝)를 광장 Centerpiece 문자판(clock_face 재질 정점)에 맞춤
//   배치 = Village 씬 Village/Ch4Quest. 시계 자리 = 3장 거울 자리(퀘스트 뒤 꺼짐), 톱니 = 시계에서 섬 안쪽으로 땅 · 빈자리를 물리 검사해 고름
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace CatTang.EditorTools
{
    public static class Ch4QuestBuilder
    {
        const string VillagePath = "Assets/Scenes/Village.unity";
        static readonly string[] GearCandidates = { "Assets/Resources/Kit/Story/W_GearFloor.prefab", "Assets/Resources/Kit/Story/W_Gear.prefab", "Assets/Resources/Kit/Ch4/GearFloor.prefab" };
        static readonly string[] ClockCandidates = { "Assets/Resources/Kit/Story/W_StoppedClock.prefab", "Assets/Resources/Kit/Story/W_GrandfatherClock.prefab", "Assets/Resources/Kit/Story/StreetClock.prefab" };
        internal const string HandPrefab = "Assets/Resources/Kit/Story/DialHand.prefab";
        const float GearWidth = 2.4f, ClockHeight = 3.7f;

        // (id, 시계 자리, 톱니를 펼칠 섬 안쪽 기준점) — 시계 자리 = 3장 거울 자리 (Ch3QuestBuilder.Stations), 안쪽 = 3장 거울 카메라 중심
        static readonly (string id, Vector3 clock, Vector3 inward)[] Stations =
        {
            ("inn",  new(-6.8f, 0, -0.8f), new(0f, 0, 2f)),
            ("ammo", new(28.4f, 0, -5.2f), new(34f, 0, -1f)),
            ("gun",  new(-30.4f, 0, -6.2f), new(-34f, 0, 1f)),
            ("ward", new(26.2f, 0, 21.2f), new(25.5f, 0, 26.5f)),
        };
        static readonly Vector3 PlazaFallback = new(0f, 0, -4.5f);

        static GameObject FirstPrefab(string[] paths, out string used)
        {
            foreach (var p in paths)
            {
                var g = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                if (g) { used = p; return g; }
            }
            used = null;
            return null;
        }

        static Bounds WorldBounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one);
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        /// <summary>프리팹을 놓고 높이(h) 또는 너비(w)가 size가 되게 맞춤</summary>
        static GameObject Put(GameObject prefab, Transform parent, Vector3 pos, float yaw, float size, char fit)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0, yaw, 0);
            var b = WorldBounds(go);
            float cur = fit == 'h' ? b.size.y : Mathf.Max(b.size.x, b.size.z);
            if (cur > 0.01f) go.transform.localScale *= size / cur;
            b = WorldBounds(go);
            go.transform.position += Vector3.up * (pos.y - b.min.y);              // 발밑 = 땅
            foreach (var c in go.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            return go;
        }

        [MenuItem("CatTang/4장 멈춘 마을 배치 (Village 씬)")]
        public static void PlaceCh4Quest()
        {
            var scene = EditorSceneManager.GetSceneByPath(VillagePath);
            if (!scene.IsValid() || !scene.isLoaded) scene = EditorSceneManager.OpenScene(VillagePath, OpenSceneMode.Additive);
            GameObject village = null;
            foreach (var r in scene.GetRootGameObjects()) if (r.name == "Village") village = r;
            if (!village) { Debug.LogError("[Ch4Quest] Village 없음"); return; }
            var gearPrefab = FirstPrefab(GearCandidates, out var gearPath);
            var clockPrefab = FirstPrefab(ClockCandidates, out var clockPath);
            if (!gearPrefab || !clockPrefab) { Debug.LogError($"[Ch4Quest] 키트 없음 — 톱니 {gearPath ?? "없음"} · 시계 {clockPath ?? "없음"}"); return; }

            var old = village.transform.Find("Ch4Quest");
            if (old) Object.DestroyImmediate(old.gameObject);
            var root = new GameObject("Ch4Quest");
            root.transform.SetParent(village.transform, false);
            var q = root.AddComponent<CatTang.Ch4Quest>();
            var ch3 = village.transform.Find("Ch3Quest");
            Physics.SyncTransforms();

            var list = new List<CatTang.Ch4Quest.Station>();
            var log = new System.Text.StringBuilder("[Ch4Quest] 배치 — 톱니 " + gearPath + " · 시계 " + clockPath + "\n");
            foreach (var s in Stations)
            {
                CatTang.Ch4Quest.Def def = default;
                foreach (var d in CatTang.Ch4Quest.Defs) if (d.id == s.id) def = d;
                if (def.hours == null) continue;
                var holder = new GameObject("Station_" + s.id).transform;
                holder.SetParent(root.transform, false);
                var clock = Put(clockPrefab, holder, s.clock, 0f, ClockHeight, 'h');
                clock.name = "StoppedClock_" + s.id;
                var placed = new List<Vector3> { s.clock };
                var gears = new List<Transform>();
                var dir = Effects.Flat(s.inward - s.clock).normalized;
                if (dir.sqrMagnitude < 0.01f) dir = Vector3.back;
                for (int k = 0; k < def.hours.Length; k++)
                {
                    var at = FindGearSpot(s.clock, dir, placed, ch3, root.transform, out bool ok);
                    placed.Add(at);
                    var g = new GameObject($"Gear_{s.id}_{k}_{def.hours[k]}").transform;
                    g.SetParent(holder, false);
                    g.position = at;
                    var vis = Put(gearPrefab, g, at + Vector3.up * 0.06f, k * 17f, GearWidth, 'w');
                    vis.name = "GearVis";
                    gears.Add(g);
                    log.Append($"  {s.id} 톱니 {def.hours[k]} @ ({at.x:0.0}, {at.z:0.0}){(ok ? "" : " — 빈자리 못 찾음, 기본 자리")}\n");
                }
                list.Add(new CatTang.Ch4Quest.Station { id = s.id, clock = clock.transform, gears = gears.ToArray() });
            }
            q.stations = list.ToArray();

            // 광장 시계탑: 발밑 표지 + 카메라 쪽 문자판에 11:59 바늘
            Transform tower = null;
            foreach (var t in village.GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith("Centerpiece") && !t.IsChildOf(root.transform)) { tower = t; break; }
            var plaza = new GameObject("PlazaClock").transform;
            plaza.SetParent(root.transform, false);
            plaza.position = tower ? Effects.Flat(tower.position) : PlazaFallback;
            q.plazaClock = plaza;
            bool faceOk = FindFace(tower, out var facePos, out var faceNormal, out float faceR);
            if (!faceOk) { facePos = plaza.position + new Vector3(0, 4.4f, -1.0f); faceNormal = Vector3.back; faceR = 0.6f; }
            var hand = AssetDatabase.LoadAssetAtPath<GameObject>(HandPrefab);
            if (hand)
            {
                q.minuteHand = MakeHand(hand, plaza, "MinuteHand", facePos + faceNormal * 0.12f, faceNormal, faceR * 0.86f, 0.9f);
                q.hourHand = MakeHand(hand, plaza, "HourHand", facePos + faceNormal * 0.07f, faceNormal, faceR * 0.55f, 1.25f);
            }
            else Debug.LogWarning("[Ch4Quest] 바늘 키트 없음 " + HandPrefab + " — 광장 시계 바늘 없이 (튕김 · 대화는 동작)");
            log.Append($"  광장 시계 {(tower ? tower.name : "없음(기본 자리)")} · 문자판 {(faceOk ? "찾음" : "못 찾음 — 기본 자리")} ({facePos.x:0.00}, {facePos.y:0.00}, {facePos.z:0.00}) r {faceR:0.00}");

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log(log.ToString());
        }

        /// <summary>시계에서 섬 안쪽으로 땅이 있고 막힌 게 없는 톱니 자리</summary>
        static Vector3 FindGearSpot(Vector3 clock, Vector3 dir, List<Vector3> placed, Transform ch3, Transform mine, out bool ok)
        {
            foreach (float dist in new[] { 3.2f, 4.0f, 4.8f, 5.8f })
                foreach (float a in new[] { 0f, 28f, -28f, 56f, -56f, 84f, -84f, 112f, -112f })
                {
                    var at = clock + Quaternion.Euler(0, a, 0) * dir * dist;
                    bool clear = true;
                    foreach (var o in placed) if (Effects.Flat(o - at).magnitude < 2.7f) clear = false;
                    if (!clear) continue;
                    if (!Physics.Raycast(at + Vector3.up * 6f, Vector3.down, out var hit, 14f, ~0, QueryTriggerInteraction.Ignore) || Mathf.Abs(hit.point.y) > 0.7f) continue;
                    foreach (var c in Physics.OverlapSphere(at + Vector3.up * 1.3f, GearWidth * 0.5f + 0.1f, ~0, QueryTriggerInteraction.Ignore))
                    {
                        if (c == hit.collider) continue;
                        if ((ch3 && c.transform.IsChildOf(ch3)) || c.transform.IsChildOf(mine)) continue;
                        if (c.name.Contains("Ground") || c.name == "Top" || c.name == "Deck") continue;
                        clear = false; break;
                    }
                    if (!clear) continue;
                    ok = true;
                    return new Vector3(at.x, 0, at.z);
                }
            ok = false;
            var perp = Vector3.Cross(Vector3.up, dir);
            var fb = clock + dir * 3.2f + perp * (placed.Count - 1) * 2.8f;
            return new Vector3(fb.x, 0, fb.z);
        }

        /// <summary>광장 Centerpiece의 clock_face 재질 정점을 네 면으로 나눠 카메라(남쪽, -Z) 쪽 문자판의 가운데 · 바깥 방향 · 반지름</summary>
        internal static bool FindFace(Transform tower, out Vector3 pos, out Vector3 normal, out float radius)
        {
            pos = default; normal = Vector3.back; radius = 0.6f;
            if (!tower) return false;
            var center = tower.position;
            var bins = new List<Vector3>[4];
            for (int i = 0; i < 4; i++) bins[i] = new List<Vector3>();
            Vector3[] axes = { tower.forward, tower.right, -tower.forward, -tower.right };
            foreach (var mf in tower.GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh = mf.sharedMesh;
                var mr = mf.GetComponent<MeshRenderer>();
                if (!mesh || !mr) continue;
                int slot = FaceSlot(mesh, mr);
                if (slot < 0 || slot >= mesh.subMeshCount) continue;
                var verts = mesh.vertices;
                foreach (int idx in mesh.GetIndices(slot))
                {
                    var w = mf.transform.TransformPoint(verts[idx]);
                    var d = Effects.Flat(w - center);
                    if (d.sqrMagnitude < 0.01f) continue;
                    int best = 0; float bd = -9f;
                    for (int i = 0; i < 4; i++) { float dot = Vector3.Dot(d.normalized, axes[i]); if (dot > bd) { bd = dot; best = i; } }
                    bins[best].Add(w);
                }
            }
            int pick = -1; float minZ = float.MaxValue;
            for (int i = 0; i < 4; i++)
            {
                if (bins[i].Count < 8) continue;
                float z = 0; foreach (var v in bins[i]) z += v.z; z /= bins[i].Count;
                if (z < minZ) { minZ = z; pick = i; }
            }
            if (pick < 0) return false;
            var face = bins[pick];
            normal = Effects.Flat(axes[pick]).normalized;
            var tangent = Vector3.Cross(Vector3.up, normal);
            float tMin = float.MaxValue, tMax = float.MinValue, yMax = float.MinValue, nMax = float.MinValue;
            foreach (var v in face)
            {
                float t = Vector3.Dot(v, tangent); tMin = Mathf.Min(tMin, t); tMax = Mathf.Max(tMax, t);
                yMax = Mathf.Max(yMax, v.y); nMax = Mathf.Max(nMax, Vector3.Dot(v, normal));
            }
            radius = (tMax - tMin) * 0.5f;
            float tMid = (tMin + tMax) * 0.5f;
            // 녹아 처진 아래 가장자리 대신 위쪽 끝 − 반지름 = 문자판 가운데 높이
            pos = tangent * tMid + normal * nMax + Vector3.up * (yMax - radius);
            return radius > 0.1f;
        }

        /// <summary>원본 FBX의 재질 이름으로 clock_face 칸 번호 (프리팹 재질은 툰 재질로 바뀌어 있음)</summary>
        static int FaceSlot(Mesh mesh, MeshRenderer mr)
        {
            var ms = mr.sharedMaterials;
            for (int i = 0; i < ms.Length; i++) if (ms[i] && ms[i].name.Contains("clock_face")) return i;
            var path = AssetDatabase.GetAssetPath(mesh);
            if (string.IsNullOrEmpty(path)) return -1;
            var src = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!src) return -1;
            foreach (var r in src.GetComponentsInChildren<MeshRenderer>(true))
            {
                var f = r.GetComponent<MeshFilter>();
                if (!f || f.sharedMesh != mesh) continue;
                var sm = r.sharedMaterials;
                for (int i = 0; i < sm.Length; i++) if (sm[i] && sm[i].name.StartsWith("clock_face")) return i;
            }
            return -1;
        }

        /// <summary>바늘 축 (앞 = 문자판 바깥, 위 = 12시) + DialHand를 그 판 위에 눕혀 끝이 축의 위를 가리키게</summary>
        internal static Transform MakeHand(GameObject prefab, Transform parent, string name, Vector3 at, Vector3 normal, float length, float thick)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(parent, true);
            pivot.position = at;
            pivot.rotation = Quaternion.LookRotation(normal, Vector3.up);
            var h = (GameObject)PrefabUtility.InstantiatePrefab(prefab, pivot);
            h.transform.localPosition = Vector3.zero;
            h.transform.localRotation = Quaternion.identity;
            h.transform.localScale = Vector3.one;
            // 바늘 길이 = 축(원점)에서 +Z 끝까지
            float tip = 0f;
            foreach (var mf in h.GetComponentsInChildren<MeshFilter>())
                if (mf.sharedMesh) foreach (var v in mf.sharedMesh.vertices) tip = Mathf.Max(tip, h.transform.InverseTransformPoint(mf.transform.TransformPoint(v)).z);
            float s = tip > 0.01f ? length / tip : 0.06f;
            h.transform.localRotation = Quaternion.Euler(-90f, 0, 0);            // 키트 +Z(바늘 끝) → 축 +Y(12시), 두께 → 문자판 앞뒤
            h.transform.localScale = new Vector3(s * thick, s, s);
            foreach (var c in h.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            foreach (var r in h.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.Off;
            return pivot;
        }
    }
}
