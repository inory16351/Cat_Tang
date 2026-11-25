// 3장 클리어 퀘스트 '거울 마을' 키트 → 프리팹 → 마을 씬 배치 (2026-11-25 사용자)
//   원본 Art/tools/blender_kit_ch3_quest.py → Assets/Art/Characters/Ch3Quest/Kit_Q_*.fbx → Resources/Kit/Ch3Quest/*.prefab
//   거울 유리(재질 mirror_glass)는 MirrorGlass 재질로 바꿔 둠 → 실행 중 MirrorView가 거울 카메라 그림을 띄움
//   배치 = Village 씬 Village/Ch3Quest (원래 = 안 뒤집힌 좌표). 마을 뿌리가 뒤집히면 같이 뒤집힘
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CatTang.EditorTools
{
    public static partial class EnemyModelBuilder
    {
        public const string Ch3QuestSrc = "Assets/Art/Characters/Ch3Quest/";
        public const string Ch3QuestKitDir = "Assets/Resources/Kit/Ch3Quest/";
        const string MirrorGlassMat = Ch3QuestSrc + "MirrorGlass.mat";

        static readonly string[] KitQuest = { "StandMirror", "PlazaMirror", "CuckooBench", "HourglassBench", "CoatPoster", "SewingWindow", "OldManShadow", "OldManArm" };

        // 새벽 별밤 마을과 어울리는 밝은 배색 (그늘이 칙칙해지지 않게)
        public static readonly Dictionary<string, Color> Ch3QuestPalette = new()
        {
            { "wood", new Color(0.66f, 0.45f, 0.32f) }, { "wood_dark", new Color(0.42f, 0.28f, 0.22f) }, { "wood_light", new Color(0.84f, 0.68f, 0.5f) },
            { "gold", new Color(1f, 0.82f, 0.38f) }, { "brass", new Color(0.92f, 0.7f, 0.38f) }, { "bronze", new Color(0.72f, 0.5f, 0.32f) },
            { "iron", new Color(0.36f, 0.37f, 0.48f) }, { "iron_dark", new Color(0.2f, 0.2f, 0.3f) },
            { "stone", new Color(0.74f, 0.72f, 0.84f) }, { "stone_dark", new Color(0.5f, 0.48f, 0.64f) }, { "moss", new Color(0.45f, 0.68f, 0.52f) },
            { "mirror_glass", new Color(0.8f, 0.9f, 1f) }, { "mirror_back", new Color(0.24f, 0.26f, 0.42f) },
            { "gem_rose", new Color(1f, 0.5f, 0.75f) }, { "gem_amber", new Color(1f, 0.7f, 0.25f) }, { "ivory", new Color(0.97f, 0.94f, 0.85f) },
            { "velvet", new Color(0.62f, 0.18f, 0.4f) }, { "fabric", new Color(0.58f, 0.5f, 0.42f) }, { "fabric_dark", new Color(0.38f, 0.32f, 0.3f) },
            { "leather", new Color(0.58f, 0.38f, 0.26f) }, { "poster", new Color(0.95f, 0.78f, 0.55f) }, { "paper", new Color(0.97f, 0.93f, 0.82f) },
            { "clock_face", new Color(1f, 0.96f, 0.86f) }, { "sand", new Color(0.98f, 0.84f, 0.55f) }, { "glass", new Color(0.66f, 0.84f, 0.96f) },
            { "window_lit", new Color(1f, 0.84f, 0.52f) }, { "curtain", new Color(0.8f, 0.36f, 0.42f) }, { "thread_red", new Color(0.95f, 0.25f, 0.3f) },
            { "shadow", new Color(0.09f, 0.07f, 0.14f) }, { "shadow_blur", new Color(0.32f, 0.27f, 0.42f) }, { "water_edge", new Color(0.5f, 0.66f, 0.9f) },
        };

        [MenuItem("CatTang/3장 거울 마을 키트 만들기 (블렌더)")]
        public static void BuildCh3QuestKit()
        {
            System.IO.Directory.CreateDirectory(Ch3QuestKitDir);
            SrcDir = Ch3QuestSrc;
            ModelPalette.Active = Ch3QuestPalette;
            foreach (var k in new[] { "Kit_Q_OldManShadow", "Kit_Q_OldManArm" }) KeepOrigin.Add(k);
            try
            {
                int made = 0;
                foreach (var n in KitQuest)
                    if (Build("Kit_Q_" + n, NativeQuestSize("Kit_Q_" + n), 180f, 'h', Ch3QuestKitDir, n)) { made++; SwapGlass("Kit_Q_" + n, Ch3QuestKitDir + n + ".prefab"); }
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"[Ch3Quest] 키트 {made}개");
            }
            finally
            {
                SrcDir = Ch1Src;
                ModelPalette.Active = ModelPalette.Ch1;
            }
        }

        static float NativeQuestSize(string name)
        {
            var src = AssetDatabase.LoadAssetAtPath<GameObject>(Ch3QuestSrc + name + ".fbx");
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

        /// <summary>원본 FBX에서 mirror_glass 재질 칸을 찾아 프리팹의 같은 칸을 MirrorGlass 재질로</summary>
        static void SwapGlass(string srcName, string prefabPath)
        {
            var src = AssetDatabase.LoadAssetAtPath<GameObject>(Ch3QuestSrc + srcName + ".fbx");
            var sr = src ? src.GetComponentInChildren<Renderer>() : null;
            if (!sr) return;
            int slot = -1;
            var sm = sr.sharedMaterials;
            for (int i = 0; i < sm.Length; i++) if (sm[i] && sm[i].name.StartsWith("mirror_glass")) slot = i;
            if (slot < 0) return;
            var glass = AssetDatabase.LoadAssetAtPath<Material>(MirrorGlassMat);
            if (!glass)
            {
                glass = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "MirrorGlass" };
                glass.SetColor("_BaseColor", new Color(0.8f, 0.9f, 1f));
                AssetDatabase.CreateAsset(glass, MirrorGlassMat);
            }
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var r = root.GetComponentInChildren<Renderer>();
                var ms = r.sharedMaterials;
                if (slot < ms.Length) { ms[slot] = glass; r.sharedMaterials = ms; }
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        // ---------- 마을 씬 배치 ----------
        // (id, 거울 자리, 거울 방향(도), NPC 설 자리, 거울 카메라 중심, 물건 프리팹)
        static readonly (string id, string spot, Vector3 mirror, float yaw, Vector3 npc, Vector3 view, string obj)[] Stations =
        {
            ("inn",  "NpcSpot_MemoryRoom",   new(-6.8f, 0, -0.8f), 10f,  new(-4.9f, 0, -1.6f), new(0f, 0, 2f),    "CuckooBench"),
            ("ammo", "NpcSpot_AmmoWorkshop", new(28.4f, 0, -5.2f), -15f, new(30.3f, 0, -5.8f), new(34f, 0, -1f),  "HourglassBench"),
            ("gun",  "NpcSpot_GunShop",      new(-30.4f, 0, -6.2f), 15f, new(-28.5f, 0, -6.8f), new(-34f, 0, 1f), "CoatPoster"),
            ("ward", "NpcSpot_Wardrobe",     new(26.2f, 0, 21.2f), -10f, new(28.0f, 0, 20.6f), new(25.5f, 0, 26.5f), "SewingWindow"),
        };
        static readonly Vector3 PlazaMirrorAt = new(0f, 0, -11.6f), MirrorCatAt = new(-6.0f, 0f, -8.4f);
        static readonly Vector3 ShoulderR = new(-0.205f, 1.365f, 0.015f);   // blender_kit_ch3_quest.py SHOULDER_R (X, Z, Y)

        static GameObject Qk(string name, Transform parent, Vector3 pos, float yaw)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Ch3QuestKitDir + name + ".prefab");
            if (!prefab) { Debug.LogWarning("[Ch3Quest] 키트 없음 " + name); return null; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0, yaw, 0);
            return go;
        }

        [MenuItem("CatTang/3장 거울 마을 배치 (Village 씬)")]
        public static void PlaceCh3Quest()
        {
            var scene = EditorSceneManager.GetSceneByPath("Assets/Scenes/Village.unity");
            if (!scene.IsValid() || !scene.isLoaded) scene = EditorSceneManager.OpenScene("Assets/Scenes/Village.unity", OpenSceneMode.Additive);
            GameObject village = null;
            foreach (var r in scene.GetRootGameObjects()) if (r.name == "Village") village = r;
            if (!village) { Debug.LogError("[Ch3Quest] Village 없음"); return; }
            // 섬 윗면이 정적 배치(static batching)면 뒤집기가 안 먹음 → 끔
            foreach (var t in village.GetComponentsInChildren<Transform>(true))
                if (t.name == "Top") GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);

            var old = village.transform.Find("Ch3Quest");
            if (old) Object.DestroyImmediate(old.gameObject);
            var root = new GameObject("Ch3Quest");
            root.transform.SetParent(village.transform, false);
            var q = root.AddComponent<CatTang.Ch3Quest>();
            var list = new List<CatTang.Ch3Quest.Station>();
            foreach (var s in Stations)
            {
                var holder = new GameObject("Station_" + s.id).transform;
                holder.SetParent(root.transform, false);
                var m = Qk("StandMirror", holder, s.mirror, s.yaw);
                if (!m) continue;
                m.name = "Mirror_" + s.id;
                m.transform.localScale = Vector3.one * 1.3f;                     // 위에서 보는 화면에서도 거울 속이 읽히게
                var mv = m.AddComponent<CatTang.MirrorView>();
                mv.viewScale = 1.75f; mv.range = 24f;
                m.AddComponent<CatTang.MirrorPeek>();
                var col = m.AddComponent<BoxCollider>(); col.center = new Vector3(0, 1.6f, 0); col.size = new Vector3(2.4f, 3.2f, 1.0f);
                var spot = GameObject.Find(s.spot);
                Vector3 objAt = spot ? spot.transform.position : s.mirror + Vector3.right * 4f;
                var obj = Qk(s.obj, holder, new Vector3(objAt.x, 0, objAt.z), spot ? spot.transform.eulerAngles.y : 180f);
                if (obj) { obj.name = "Object_" + s.id; obj.transform.localScale = Vector3.one * 1.35f; }
                var npc = new GameObject("NpcStand_" + s.id).transform; npc.SetParent(holder, false); npc.position = s.npc;
                var view = new GameObject("View_" + s.id).transform; view.SetParent(holder, false); view.position = s.view;
                list.Add(new CatTang.Ch3Quest.Station { id = s.id, mirror = mv, obj = obj ? obj.transform : null, npcStand = npc, view = view });
            }
            q.stations = list.ToArray();

            var pm = Qk("PlazaMirror", root.transform, PlazaMirrorAt, 0f);
            if (pm)
            {
                q.plazaMirror = pm.AddComponent<CatTang.MirrorView>();
                q.plazaMirror.followPlayer = true; q.plazaMirror.faceFromGlass = true; q.plazaMirror.range = 30f;
                var col = pm.AddComponent<BoxCollider>(); col.center = new Vector3(0, 2.4f, 0); col.size = new Vector3(4.2f, 4.8f, 2.6f);
            }
            var om = Qk("OldManShadow", root.transform, PlazaMirrorAt + Vector3.back * 4f, 180f);
            if (om)
            {
                om.name = "OldMan";
                q.oldMan = om.transform;
                var meshOff = om.transform.Find("Mesh") ? om.transform.Find("Mesh").localPosition : Vector3.zero;
                var pivot = new GameObject("ArmPivot").transform;
                pivot.SetParent(om.transform, false);
                pivot.localPosition = Quaternion.Euler(0, 180f, 0) * ShoulderR + meshOff;
                var arm = Qk("OldManArm", pivot, Vector3.zero, 0f);
                if (arm) { arm.transform.localPosition = Vector3.zero; arm.transform.localRotation = Quaternion.identity; }
                q.oldManArm = pivot;
            }
            // 거울 속 고양이: 광장에 그냥 나와 서 있는 NPC (2026-11-25 사용자: 물웅덩이 · 손거울 대신). 모습은 실행 중 고양이 모델로 만듦 (Ch3Quest.MakePuddleCat)
            var mc = new GameObject("MirrorCatSpot");
            mc.transform.SetParent(root.transform, false);
            mc.transform.position = MirrorCatAt;
            mc.AddComponent<CatTang.MirrorCatNpc>();
            var cap = mc.AddComponent<CapsuleCollider>(); cap.center = new Vector3(0, 0.9f, 0); cap.radius = 0.5f; cap.height = 1.8f;
            q.puddle = mc.transform;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Ch3Quest] 마을 배치 완료");
        }
    }
}
