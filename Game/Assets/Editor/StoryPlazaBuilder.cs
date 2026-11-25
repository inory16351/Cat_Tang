// 스토리 맵 '어린 날 시계탑 광장' 키트 → 프리팹 (2026-11-25 사용자: 첫 1장 보스문 앞 꿈 맵, 블렌더로)
//   원본 Art/tools/blender_kit_story_plaza.py → Assets/Art/Characters/StoryPlaza/Kit_S_*.fbx → Resources/Kit/Story/*.prefab
//   바늘(TowerHand · DialHand)은 hub 재질 정점 가운데를 회전축으로 다시 맞춤 (프리팹 원점 = 축, 바늘은 +Z를 가리킴)
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CatTang.EditorTools
{
    public static partial class EnemyModelBuilder
    {
        public const string StorySrc = "Assets/Art/Characters/StoryPlaza/";
        public const string StoryKitDir = "Assets/Resources/Kit/Story/";

        static readonly (string name, float size, char fit, string outName)[] KitStory =
        {
            ("Kit_S_Tower", 4.8f, 'w', "Tower"),
            ("Kit_S_TowerHand_Hour", 1.314f, 'h', "TowerHand_Hour"),
            ("Kit_S_TowerHand_Min", 1.517f, 'h', "TowerHand_Min"),
            ("Kit_S_PlazaDial", 20f, 'w', "PlazaDial"),
            ("Kit_S_DialHand", 9.22f, 'd', "DialHand"),
            ("Kit_S_ShopA", 8.8f, 'w', "ShopA"),
            ("Kit_S_ShopB", 8.4f, 'w', "ShopB"),
            ("Kit_S_ShopC", 7.423f, 'w', "ShopC"),
            ("Kit_S_StreetClock", 4.6f, 'h', "StreetClock"),
            ("Kit_S_WatchPedestal", 2.469f, 'h', "WatchPedestal"),
            ("Kit_S_GateArch", 6.4f, 'w', "GateArch"),
            ("Kit_S_GateLeaf", 2.575f, 'w', "GateLeaf"),
            ("Kit_S_ShadowChild", 1.148f, 'h', "ShadowChild"),
            ("Kit_S_ShadowAdult", 2.014f, 'h', "ShadowAdult"),
            ("Kit_S_Ladder", 5.2f, 'h', "Ladder"),
            ("Kit_S_Bunting", 8.12f, 'w', "Bunting"),
            ("Kit_S_Ground", 58.06f, 'w', "Ground"),
            ("Kit_S_Memento_Glasses", 1.2f, 'w', "Memento_Glasses"),
            ("Kit_S_Memento_Band", 0.9f, 'w', "Memento_Band"),
            ("Kit_S_Memento_Note", 0.9f, 'w', "Memento_Note"),
        };

        [MenuItem("CatTang/스토리 광장 키트 만들기 (블렌더)")]
        public static void BuildStoryKit()
        {
            System.IO.Directory.CreateDirectory(StoryKitDir);
            SrcDir = StorySrc;
            ModelPalette.Active = ModelPalette.StoryPlaza;
            try
            {
                int made = 0;
                foreach (var k in KitStory)
                    if (Build(k.name, k.size, 180f, k.fit, StoryKitDir, k.outName)) made++;
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                RepivotHand(StoryKitDir + "TowerHand_Hour.prefab", false); RepivotHand(StoryKitDir + "TowerHand_Min.prefab", false); RepivotHand(StoryKitDir + "DialHand.prefab", true);
                Debug.Log($"[StoryPlaza] 키트 {made}개");
            }
            finally
            {
                SrcDir = Ch1Src;
                ModelPalette.Active = ModelPalette.Ch1;
            }
        }

        // ---------- 맵 조립: Resources/Story/StoryPlaza.prefab ----------
        // 남쪽 골목(회중시계 · 작은 문자판 · 첫 문) → 북쪽 원형 광장(바닥 큰 문자판 · 시계탑 · 거리 시계). 카메라는 남쪽 위에서 봄
        // 표지(빈 오브젝트, 이름으로 StoryMap이 찾음): Start · Dial1 · Watch · Gate1 · Dial2 · DialHand · StreetClock · TowerHandH/M · Child · Adult · ChildEnd
        public const string StoryMapPath = "Assets/Resources/Story/StoryPlaza.prefab";
        public const float Dial1R = 3.6f, Dial2R = 10f, Gate1Z = -14.2f;
        public static readonly Vector3 Dial2Pos = new(0.5f, 0.005f, 0.5f);
        // 걷는 곳 윤곽 (blender_kit_story_plaza.py OUTLINE과 같음) — 굽은 골목 + 울퉁불퉁한 광장
        static readonly Vector2[] Outline =
        {
            new(-4.5f, -37.0f), new(-6.2f, -33.0f), new(-7.0f, -29.0f), new(-5.6f, -25.0f), new(-6.4f, -21.0f), new(-5.2f, -17.0f), new(-6.1f, -14.2f),
            new(-10.5f, -13.2f), new(-14.8f, -10.5f), new(-16.6f, -5.5f), new(-17.2f, 0.5f), new(-15.4f, 6.0f), new(-13.8f, 10.2f), new(-9.6f, 14.2f),
            new(-4.8f, 17.6f), new(0.5f, 18.4f), new(5.6f, 17.2f), new(10.4f, 14.6f), new(14.2f, 10.8f), new(16.9f, 5.2f), new(16.2f, -0.8f), new(17.0f, -6.0f),
            new(14.0f, -10.8f), new(9.8f, -13.8f), new(6.3f, -14.2f), new(5.0f, -16.8f), new(6.0f, -20.5f), new(4.8f, -24.5f), new(5.5f, -28.5f),
            new(4.2f, -32.5f), new(5.0f, -37.0f), new(0.4f, -38.2f),
        };
        static Vector3 V3(Vector2 p) => new(p.x, 0, p.y);
        /// <summary>(x, z)에 놓인 건물이 (tx, tz)를 바라보는 yaw (+ 비틀기). 키트 정면 = 로컬 −Z</summary>
        static float Face(float x, float z, float tx, float tz, float jitter) => Mathf.Atan2(-(tx - x), -(tz - z)) * Mathf.Rad2Deg + jitter;

        static GameObject S(string name, Transform parent, Vector3 pos, float yaw, float scale = 1f)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(name.StartsWith("V/") ? VillageKitDir + name.Substring(2) + ".prefab" : StoryKitDir + name + ".prefab");
            if (!prefab) { Debug.LogWarning("[StoryPlaza] 키트 없음 " + name); return null; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            go.transform.localScale *= scale;
            return go;
        }

        static Transform Mark(Transform parent, string name, Vector3 pos, float yaw = 0)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = pos;
            t.localRotation = Quaternion.Euler(0, yaw, 0);
            return t;
        }

        /// <summary>속이 보이는 몸통(건물 · 탑)에만 바닥 2.5m 높이 상자 판정</summary>
        static void Block(GameObject go, float shrink = 0.9f)
        {
            if (!go) return;
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return;
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            var bc = go.AddComponent<BoxCollider>();
            var size = go.transform.InverseTransformVector(new Vector3(b.size.x * shrink, 2.5f, b.size.z * shrink));
            bc.size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
            bc.center = go.transform.InverseTransformPoint(new Vector3(b.center.x, go.transform.position.y + 1.25f, b.center.z));
        }

        static void Wall(Transform parent, Vector3 a, Vector3 b)
        {
            var w = new GameObject("Edge").transform;
            w.SetParent(parent, false);
            w.localPosition = (a + b) / 2 + Vector3.up * 1.5f;
            w.localRotation = Quaternion.LookRotation(b - a);
            w.gameObject.AddComponent<BoxCollider>().size = new Vector3(0.6f, 3f, (b - a).magnitude + 0.6f);
        }

        [MenuItem("CatTang/스토리 광장 맵 조립")]
        public static void BuildStoryPlazaMap()
        {
            System.IO.Directory.CreateDirectory("Assets/Resources/Story");
            var root = new GameObject("StoryPlaza").transform;
            try
            {
                var geo = new GameObject("Geo").transform; geo.SetParent(root, false);
                var marks = new GameObject("Marks").transform; marks.SetParent(root, false);
                var edges = new GameObject("Edges").transform; edges.SetParent(root, false);

                // 바닥: 굽은 골목 + 울퉁불퉁한 광장 한 덩어리 (블렌더 원점 = 맵 원점, 윤곽 = Outline). 걷기 판정은 넓은 판 하나 + 윤곽 벽
                var ground = S("Ground", geo, Vector3.zero, 0);
                if (ground) { var m = ground.transform.Find("Mesh"); if (m) { m.localPosition = Vector3.zero; m.localScale = Vector3.one; } }
                var walk = new GameObject("WalkFloor").AddComponent<BoxCollider>();
                walk.transform.SetParent(root, false);
                walk.center = new Vector3(0, -0.5f, -10); walk.size = new Vector3(60, 1, 70);

                // 골목 (남쪽, 굽음): 상점이 골목 쪽을 엇갈리게 바라봄
                Block(S("ShopC", geo, new Vector3(-10.4f, 0, -33.2f), Face(-10.4f, -33.2f, 0, -31, 7)));
                Block(S("ShopA", geo, new Vector3(-10.9f, 0, -24.4f), Face(-10.9f, -24.4f, 0, -24, -5)));
                Block(S("ShopB", geo, new Vector3(9.8f, 0, -29.6f), Face(9.8f, -29.6f, 0, -28, -8)));
                Block(S("ShopC", geo, new Vector3(9.3f, 0, -19.9f), Face(9.3f, -19.9f, 0, -21, 11)));
                S("Bunting", geo, new Vector3(-0.6f, 0.4f, -27.2f), 9, 1.45f);
                S("Bunting", geo, new Vector3(0.4f, 0.9f, -16.6f), -6, 1.5f);
                S("V/Lamp", geo, new Vector3(-4.4f, 0, -35.2f), 0);
                S("V/Lamp", geo, new Vector3(3.9f, 0, -31.4f), 0);
                S("V/Lamp", geo, new Vector3(-4.6f, 0, -19.4f), 0);
                S("V/Barrels", geo, new Vector3(4.3f, 0, -25.6f), 34);
                S("V/Crates", geo, new Vector3(-5.2f, 0, -28.4f), -23);
                S("V/Crates", geo, new Vector3(4.1f, 0, -35.4f), 61, 0.8f);
                S("V/Fence", geo, new Vector3(-5.6f, 0, -23.2f), 84, 0.9f);
                // 작은 문자판 + 회중시계 (첫 수수께끼)
                S("PlazaDial", geo, new Vector3(-0.4f, 0.005f, -22.6f), 0, Dial1R / 10f);
                Block(S("WatchPedestal", geo, new Vector3(-3.6f, 0, -18.3f), 22), 0.7f);
                // 첫 문 (골목 → 광장)
                S("GateArch", geo, new Vector3(0, 0, Gate1Z), 0);
                var gl = S("GateLeaf", geo, new Vector3(-1.25f, 0, Gate1Z), 0); if (gl) { gl.name = "Gate1_L"; var c = gl.AddComponent<BoxCollider>(); c.size = new Vector3(2.6f, 3f, 0.4f); c.center = new Vector3(0, 1.5f, 0); }
                var gr = S("GateLeaf", geo, new Vector3(1.25f, 0, Gate1Z), 180); if (gr) { gr.name = "Gate1_R"; var c = gr.AddComponent<BoxCollider>(); c.size = new Vector3(2.6f, 3f, 0.4f); c.center = new Vector3(0, 1.5f, 0); }

                // 광장: 문자판은 가운데에서 살짝 비껴, 시계탑은 북쪽에 조금 틀어져 섬
                S("PlazaDial", geo, Dial2Pos, 0);
                var hand = S("DialHand", geo, Dial2Pos + Vector3.up * 0.025f, 0); if (hand) hand.name = "DialHand";
                var tower = S("Tower", geo, new Vector3(1.2f, 0, 14.6f), -4); Block(tower, 0.95f);
                if (tower)
                {
                    var th = S("TowerHand_Hour", tower.transform, new Vector3(0, 9.9f, -2.42f), 0); if (th) th.name = "TowerHandH";
                    var tm = S("TowerHand_Min", tower.transform, new Vector3(0, 9.9f, -2.5f), 0); if (tm) tm.name = "TowerHandM";
                    var ld = S("Ladder", tower.transform, new Vector3(1.6f, 0, -3.45f), 0); if (ld) ld.transform.localRotation = Quaternion.Euler(12, 0, 0);
                    Mark(marks, "Adult", root.InverseTransformPoint(tower.transform.TransformPoint(new Vector3(2.7f, 0, -3.9f))), 176);
                }
                Block(S("ShopB", geo, new Vector3(-14.6f, 0, 11.8f), Face(-14.6f, 11.8f, 0, 0, 6)));
                Block(S("ShopA", geo, new Vector3(13.6f, 0, 13.4f), Face(13.6f, 13.4f, 0, 0, -9)));
                Block(S("ShopC", geo, new Vector3(-20.6f, 0, -1.6f), Face(-20.6f, -1.6f, 0, 0, 5)));
                Block(S("ShopC", geo, new Vector3(20.4f, 0, 1.2f), Face(20.4f, 1.2f, 0, 0, -12)));
                Block(S("ShopB", geo, new Vector3(18.6f, 0, -11.4f), Face(18.6f, -11.4f, 0, 0, 9)));
                Block(S("StreetClock", geo, new Vector3(10.2f, 0, -9.1f), -38), 0.5f);
                S("Bunting", geo, new Vector3(-7.6f, 1.3f, 13.8f), -27, 1.35f);
                S("Bunting", geo, new Vector3(7.8f, 1.0f, 14.6f), 17, 1.4f);
                S("Bunting", geo, new Vector3(-14.2f, 0.7f, 3.6f), 78, 1.2f);
                foreach (var lp in new[] { new Vector3(-13.6f, 0, 6.2f), new Vector3(13.8f, 0, 6.8f), new Vector3(-13.4f, 0, -9.6f), new Vector3(12.6f, 0, -11.6f), new Vector3(4.6f, 0, 16.4f) })
                    S("V/Lamp", geo, lp, 0);
                S("V/Bench", geo, new Vector3(-12.2f, 0, -7.6f), 47);
                S("V/Bench", geo, new Vector3(-6.8f, 0, -12.6f), 7);
                S("V/Wagon", geo, new Vector3(14.6f, 0, 2.2f), 71);
                S("V/Barrels", geo, new Vector3(-10.6f, 0, 12.4f), 12);
                S("V/Crates", geo, new Vector3(9.6f, 0, 15.2f), -18);

                // 표지
                Mark(marks, "Start", new Vector3(0.4f, 0, -35.6f));
                Mark(marks, "Dial1", new Vector3(-0.4f, 0, -22.6f));
                Mark(marks, "Watch", new Vector3(-3.6f, 0, -18.3f));
                Mark(marks, "Gate1", new Vector3(0, 0, Gate1Z));
                Mark(marks, "Dial2", Dial2Pos);
                Mark(marks, "Child", new Vector3(-2.3f, 0, -17.2f), 160);
                Mark(marks, "ChildPlaza", new Vector3(-3.4f, 0, 11.2f), 10);
                Mark(marks, "StreetClock", new Vector3(10.2f, 0, -9.1f));

                // 가장자리 판정: 윤곽 그대로 + 첫 문 양옆
                for (int i = 0; i < Outline.Length; i++)
                    Wall(edges, V3(Outline[i]), V3(Outline[(i + 1) % Outline.Length]));
                Wall(edges, new Vector3(-6.4f, 0, Gate1Z), new Vector3(-3.0f, 0, Gate1Z));
                Wall(edges, new Vector3(3.0f, 0, Gate1Z), new Vector3(6.6f, 0, Gate1Z));
                PrefabUtility.SaveAsPrefabAsset(root.gameObject, StoryMapPath);
                Debug.Log("[StoryPlaza] 맵 " + StoryMapPath);
            }
            finally { Object.DestroyImmediate(root.gameObject); }
        }

        /// <summary>그림자 인물(1장 아이 · 어른, 2장 청년 · 여인): 짙은 보라 단색 + 테두리 빛, 꼬리 끝은 투명하게 녹음 (CatTang/GhostFade)</summary>
        static void StoryShadowMats(GameObject inst)
        {
            string p = StorySrc + "Glow_Shadow.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (!m) { m = new Material(Shader.Find("CatTang/GhostFade")); AssetDatabase.CreateAsset(m, p); }
            m.shader = Shader.Find("CatTang/GhostFade");                           // 2026-11-25: 꼬리 끝이 정점 알파로 녹아 사라지는 유령 (예전 Silhouette)
            m.SetColor("_Color", new Color(0.16f, 0.08f, 0.2f, 1f));
            if (m.HasProperty("_RimBoost")) m.SetFloat("_RimBoost", 1.2f);
            EditorUtility.SetDirty(m);
            foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                    if (SrcMatName(r, i) == "shadow") { mats[i] = m; changed = true; }
                if (changed) r.sharedMaterials = mats;
            }
        }

        /// <summary>바늘 프리팹: hub 재질 정점 가운데(월드)를 프리팹 원점으로 옮김 → 원점 둘레로 돌리면 축을 중심으로 돔</summary>
        static void RepivotHand(string path, bool keepHeight)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var mesh = root.transform.Find("Mesh");
                Vector3 sum = Vector3.zero; int n = 0;
                foreach (var mf in root.GetComponentsInChildren<MeshFilter>())
                {
                    var r = mf.GetComponent<Renderer>();
                    var m = mf.sharedMesh;
                    for (int s = 0; s < m.subMeshCount; s++)
                    {
                        if (SrcMatName(r, s) != "hub") continue;
                        var seen = new HashSet<int>(m.GetTriangles(s));
                        var vs = m.vertices;
                        foreach (int i in seen) { sum += mf.transform.TransformPoint(vs[i]); n++; }
                    }
                }
                if (n == 0) { Debug.LogWarning("[StoryPlaza] hub 없음 " + path); return; }
                var hub = root.transform.InverseTransformPoint(sum / n);
                if (keepHeight) hub.y = 0;                            // 바닥 바늘은 높이 그대로 (바닥에 붙음), 시계탑 바늘은 세로로 서 있어 높이도 축으로
                mesh.localPosition -= hub;
                PrefabUtility.SaveAsPrefabAsset(root, path);
                Debug.Log($"[StoryPlaza] {path} 축 옮김 {hub}");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}
