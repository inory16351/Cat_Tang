// 내면 세계 맵 '멈춘 1초' 키트 → 프리팹 → 맵 조립 (2026-11-26 사용자: 죽음을 받아들이는 내면 세계를 맵으로 · 아무것도 없는 하얀 방 · 블렌더로)
//   원본 Art/tools/blender_kit_story_inner.py → Assets/Art/Characters/StoryInner/Kit_I_*.fbx → Resources/Kit/Story/I_*.prefab
//   맵 = Resources/Story/StoryInner.prefab (원점 = 가운데 바닥, StoryInner.cs가 (2600, 0, 2600)에 놓고 이름으로 깊이 찾음)
//   남 → 북으로 걸어감: 도착 (0, −32) → 부정 구간 끝 DenialEnd (0, −8) → 그림자 원 ArenaCenter (0, 3) → 기억 넷 Mem_* (z 15 ~ 18) → 거울 (0, 31)
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CatTang.EditorTools
{
    public static partial class EnemyModelBuilder
    {
        public const string StoryInnerSrc = "Assets/Art/Characters/StoryInner/";
        public const string StoryInnerMapPath = "Assets/Resources/Story/StoryInner.prefab";

        static readonly string[] KitInner = { "Ground", "Pillar", "Mirror", "MemHand", "MemLantern", "MemWatch", "MemShawl", "MemRing" };
        static readonly HashSet<string> KeepInner = new() { "Ground" };
        static readonly string[] FlatInner = { "Ground" };

        public static readonly Dictionary<string, Color> StoryInnerPalette = new()
        {
            // 하얀 방: 흰색 · 상아 · 아주 옅은 회색이 거의 전부, 금 조금, 빨강 = 숄 하나 (2026-11-26 사용자)
            { "floor", new Color(1f, 1f, 1f) }, { "floor_ivory", new Color(1f, 0.99f, 0.97f) }, { "floor_pale", new Color(0.97f, 0.97f, 0.98f) }, { "floor_seam", new Color(0.9f, 0.9f, 0.92f) },   // 청크 차이는 아주 옅게 (하얀 방)
            { "marble", new Color(0.98f, 0.98f, 0.97f) }, { "marble_vein", new Color(0.86f, 0.87f, 0.9f) }, { "white_shade", new Color(0.8f, 0.82f, 0.88f) },
            { "gold", new Color(1f, 0.82f, 0.38f) }, { "brass", new Color(0.9f, 0.7f, 0.36f) }, { "gold_dark", new Color(0.62f, 0.42f, 0.16f) },
            { "mirror", new Color(0.84f, 0.9f, 0.97f) }, { "mirror_edge", new Color(0.96f, 0.98f, 1f) },
            { "iron", new Color(0.32f, 0.32f, 0.36f) }, { "glass", new Color(0.86f, 0.93f, 0.98f) }, { "lamp_glow", new Color(1f, 0.85f, 0.5f) },
            { "watch_face", new Color(1f, 0.96f, 0.86f) }, { "dial_mark", new Color(0.2f, 0.12f, 0.06f) },
            { "cloth", new Color(0.9f, 0.86f, 0.78f) }, { "cloth_shade", new Color(0.74f, 0.7f, 0.64f) }, { "steel", new Color(0.75f, 0.78f, 0.82f) },
            { "shawl_red", new Color(0.82f, 0.06f, 0.08f) }, { "shawl_red_dark", new Color(0.48f, 0.03f, 0.06f) },
            { "glow", new Color(1f, 0.93f, 0.72f) },
        };

        const float InnerProp = 1.6f, InnerMirror = 1.15f;
        static readonly Vector3 InnerMirrorAt = new(0f, 0f, 31f);

        static GameObject In(string name, Transform parent, Vector3 pos, float yaw, float scale = 1f, string rename = null)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(StoryKitDir + "I_" + name + ".prefab");
            if (!prefab) { Debug.LogWarning("[StoryInner] 키트 없음 I_" + name); return null; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            go.transform.localScale *= scale;
            if (rename != null) go.name = rename;
            return go;
        }

        static float InnerNativeSize(string name)
        {
            var src = AssetDatabase.LoadAssetAtPath<GameObject>(StoryInnerSrc + name + ".fbx");
            if (!src) return 1f;
            var inst = (GameObject)Object.Instantiate(src);
            try
            {
                var rs = inst.GetComponentsInChildren<Renderer>();
                if (rs.Length == 0) return 1f;
                var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                return b.size.y;
            }
            finally { Object.DestroyImmediate(inst); }
        }

        [MenuItem("CatTang/내면 맵 키트 만들기")]
        public static void BuildStoryInnerKit()
        {
            System.IO.Directory.CreateDirectory(StoryKitDir);
            SrcDir = StoryInnerSrc;
            ModelPalette.Active = StoryInnerPalette;
            foreach (var k in KeepInner) KeepOrigin.Add("Kit_I_" + k);
            foreach (var k in FlatInner) FlatFloor.Add("Kit_I_" + k);
            try
            {
                int made = 0;
                foreach (var n in KitInner)
                {
                    if (!Build("Kit_I_" + n, InnerNativeSize("Kit_I_" + n), 180f, 'h', StoryKitDir, "I_" + n)) continue;
                    FixInnerPrefab(StoryKitDir + "I_" + n + ".prefab", KeepInner.Contains(n));
                    made++;
                }
                WhiteShade();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"[StoryInner] 키트 {made}/{KitInner.Length}개");
            }
            finally
            {
                foreach (var k in KeepInner) KeepOrigin.Remove("Kit_I_" + k);
                foreach (var k in FlatInner) FlatFloor.Remove("Kit_I_" + k);
                SrcDir = Ch1Src;
                ModelPalette.Active = ModelPalette.Ch1;
            }
        }

        static void FixInnerPrefab(string path, bool keep)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var mesh = root.transform.Find("Mesh");
                if (keep && mesh) mesh.localPosition = Vector3.zero;
                InnerGlowMats(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static void InnerGlowMats(GameObject inst)
        {
            Material Get(string file, Color c, float rim)
            {
                string p = StoryInnerSrc + file + ".mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(p);
                if (!m) { m = new Material(Shader.Find("CatTang/Silhouette")); AssetDatabase.CreateAsset(m, p); }
                m.SetColor("_Color", c);
                if (m.HasProperty("_RimBoost")) m.SetFloat("_RimBoost", rim);
                EditorUtility.SetDirty(m);
                return m;
            }
            var lamp = Get("Glow_Lamp", new Color(1f, 0.82f, 0.48f, 1f), 0.5f);
            var glow = Get("Glow_Memory", new Color(1f, 0.9f, 0.62f, 1f), 0.4f);
            var pane = Get("Glow_Mirror", new Color(0.86f, 0.92f, 1f, 1f), 0.3f);   // 거울 면은 그늘 없이 옅게 빛남 (하얀 방에서 거울이 읽히게)
            foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    var pick = SrcMatName(r, i) switch { "lamp_glow" => lamp, "glow" => glow, "mirror" => pane, _ => null };
                    if (pick) { mats[i] = pick; changed = true; }
                }
                if (changed) r.sharedMaterials = mats;
            }
        }

        /// <summary>하얀 방 그늘: 아주 옅은 회청 (하얀 배경에서도 형태가 읽히게, 어둡지 않게)</summary>
        static void WhiteShade()
        {
            foreach (var guid in AssetDatabase.FindAssets("Kit_I_ t:Material", new[] { StoryInnerSrc.TrimEnd('/') }))
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (!m || !m.name.Contains("_Toon") || !m.HasProperty("_ShadeColor")) continue;
                m.SetColor("_ShadeColor", new Color(0.95f, 0.95f, 0.97f));
                m.SetColor("_SecondShadeColor", new Color(0.84f, 0.86f, 0.91f));
                EditorUtility.SetDirty(m);
            }
        }

        [MenuItem("CatTang/내면 맵 조립")]
        public static void BuildStoryInnerMap()
        {
            System.IO.Directory.CreateDirectory("Assets/Resources/Story");
            var root = new GameObject("StoryInner").transform;
            try
            {
                var geo = new GameObject("Geo").transform; geo.SetParent(root, false);
                var marks = new GameObject("Marks").transform; marks.SetParent(root, false);
                var edges = new GameObject("Edges").transform; edges.SetParent(root, false);

                In("Ground", geo, Vector3.zero, 0, 1f, "Ground");
                var walk = new GameObject("WalkFloor").AddComponent<BoxCollider>();
                walk.transform.SetParent(root, false);
                walk.center = new Vector3(0, -0.5f, 0); walk.size = new Vector3(44, 1, 84);

                // 드문드문 선 하얀 기둥 (깊이감만)
                var pillars = new[]
                {
                    new Vector3(-16f, 0, -34f), new Vector3(17.5f, 0, -27f), new Vector3(-18.5f, 0, -17f), new Vector3(15.5f, 0, -9f), new Vector3(-15.5f, 0, 0f),
                    new Vector3(18.5f, 0, 8f), new Vector3(-17.5f, 0, 17f), new Vector3(16f, 0, 23f), new Vector3(-15f, 0, 31f), new Vector3(17f, 0, 36f),
                };
                for (int i = 0; i < pillars.Length; i++) In("Pillar", geo, pillars[i], i * 37f, 1f, "Pillar_" + (i + 1));

                // 북쪽 끝 거울 (정면 남쪽) + 거울 면 자리
                var mirror = In("Mirror", geo, InnerMirrorAt, 0, InnerMirror, "Mirror");
                if (mirror) WsBlock(mirror, 0.8f);
                Mark(marks, "MirrorPane", InnerMirrorAt + new Vector3(0, 1.67f * InnerMirror, -0.3f));
                Mark(marks, "MirrorStand", InnerMirrorAt + new Vector3(0, 0, -3.6f));

                // 기억 넷 — 처음엔 숨김 (③에서 나타남). 표지 아래 Item(물건) + Ring(빛 고리)
                var mems = new (string mark, string kit, Vector3 at, float yaw)[]
                {
                    ("Mem_Hand", "MemHand", new Vector3(-6f, 0, 15f), 25f),
                    ("Mem_Lantern", "MemLantern", new Vector3(-2.2f, 0, 18f), -15f),
                    ("Mem_Watch", "MemWatch", new Vector3(2.2f, 0, 18f), 10f),
                    ("Mem_Shawl", "MemShawl", new Vector3(6f, 0, 15f), -30f),
                };
                foreach (var (mk, kit, at, yaw) in mems)
                {
                    var m = new GameObject(mk).transform; m.SetParent(marks, false); m.localPosition = at;
                    In(kit, m, Vector3.zero, yaw, InnerProp, "Item");
                    In("MemRing", m, new Vector3(0, 0.02f, 0), 0, 1f, "Ring");
                    m.gameObject.SetActive(false);
                }

                Mark(marks, "Spawn", new Vector3(0f, 0, -32f));
                Mark(marks, "DenialEnd", new Vector3(0f, 0, -8f));
                Mark(marks, "ArenaCenter", new Vector3(0f, 0, 3f));

                // 가장자리: 보이지 않는 벽 (바닥 끝 2m 안쪽)
                Wall(edges, new Vector3(-20f, 0, -40f), new Vector3(-20f, 0, 40f));
                Wall(edges, new Vector3(-20f, 0, 40f), new Vector3(20f, 0, 40f));
                Wall(edges, new Vector3(20f, 0, 40f), new Vector3(20f, 0, -40f));
                Wall(edges, new Vector3(20f, 0, -40f), new Vector3(-20f, 0, -40f));

                PrefabUtility.SaveAsPrefabAsset(root.gameObject, StoryInnerMapPath);
                Debug.Log("[StoryInner] 맵 " + StoryInnerMapPath);
            }
            finally { Object.DestroyImmediate(root.gameObject); }
        }
    }
}
