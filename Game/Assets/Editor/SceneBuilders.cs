// 씬 만들기 (MCP execute_menu_item으로도 실행):
//  Cat_Tang/Build Village Scene  — 여관 중심 마을 (3D 탑뷰, 시설 4곳 + 시간의 문)
//  Cat_Tang/Build Run Scene      — 챕터 판 (방은 실행 중에 만들어짐)
//  Cat_Tang/Build All Scenes     — 둘 다 + 빌드 목록 (Village 0, Run 1)
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace CatTang.EditorTools
{
    public static class SceneBuilders
    {
        const string VillagePath = "Assets/Scenes/Village.unity";
        const string RunPath = "Assets/Scenes/Run.unity";
        const string MatDir = "Assets/Art/Materials/Proto";

        [MenuItem("Cat_Tang/Build All Scenes")]
        public static void BuildAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            BuildRunInternal();
            BuildVillageInternal();
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(VillagePath, true), new EditorBuildSettingsScene(RunPath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("[CatTang] 마을 · 판 씬 생성 완료 (빌드 목록: Village, Run)");
        }

        [MenuItem("Cat_Tang/Build Village Scene")]
        public static void BuildVillage() { if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) BuildVillageInternal(); }

        [MenuItem("Cat_Tang/Build Run Scene")]
        public static void BuildRun() { if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) BuildRunInternal(); }

        // ---------- 공통 ----------
        static Material Mat(string name, Color c, float outline)
        {
            Directory.CreateDirectory(MatDir);
            var path = $"{MatDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            var sh = Shader.Find("CatTang/Toon") ?? Shader.Find("Universal Render Pipeline/Lit");
            if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, path); }
            m.shader = sh;
            m.SetColor("_BaseColor", c);
            if (m.HasProperty("_OutlineWidth")) m.SetFloat("_OutlineWidth", outline);
            if (m.HasProperty("_RimStrength")) m.SetFloat("_RimStrength", 0.5f);
            EditorUtility.SetDirty(m);
            return m;
        }

        static GameObject Prim(PrimitiveType t, string name, Transform parent, Vector3 pos, Vector3 scale, Material m, bool collider = true, Vector3? euler = null)
        {
            var go = GameObject.CreatePrimitive(t);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            if (euler.HasValue) go.transform.localRotation = Quaternion.Euler(euler.Value);
            go.GetComponent<Renderer>().sharedMaterial = m;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        static void MakeLight()
        {
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.25f;
            sun.color = new Color(1f, 0.93f, 0.84f);
            sun.shadows = LightShadows.Soft;
            sun.shadowBias = 0.08f;
            sun.shadowNormalBias = 0.8f;
            sun.transform.rotation = Quaternion.Euler(52f, -35f, 0f);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.36f, 0.3f, 0.44f);
        }

        static GameObject MakeCamera(Vector3 at)
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.106f, 0.09f, 0.149f);
            cam.fieldOfView = 40f;
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<CameraFollow>();
            camGo.transform.position = at + new Vector3(0, 17, -11);
            camGo.transform.LookAt(at);
            return camGo;
        }

        /// <summary>도형 고양이 총잡이 (3등신 · 큰 모자 · 쌍권총 · 줄무늬 꼬리)</summary>
        static PlayerController MakePlayer(Vector3 pos)
        {
            var coatM = Mat("CatCoat", new Color(0.35f, 0.24f, 0.2f), 5);
            var furM = Mat("CatFur", new Color(0.62f, 0.6f, 0.62f), 5);
            var hatM = Mat("CatHat", new Color(0.28f, 0.16f, 0.14f), 5);
            var scarfM = Mat("CatScarf", new Color(0.8f, 0.18f, 0.16f), 5);
            var gunM = Mat("CatGun", new Color(0.88f, 0.68f, 0.3f), 4);
            var eyeM = Mat("CatEye", new Color(1f, 0.85f, 0.3f), 2);
            var player = new GameObject("Player");
            player.tag = "Player";
            player.transform.position = pos;
            var cc = player.AddComponent<CharacterController>();
            cc.radius = 0.45f; cc.height = 1.6f; cc.center = new Vector3(0, 0.8f, 0);
            player.AddComponent<Health>();
            var pc = player.AddComponent<PlayerController>();
            var model = new GameObject("Model").transform;
            model.SetParent(player.transform, false);
            Prim(PrimitiveType.Capsule, "Body", model, new Vector3(0, 0.55f, 0), new Vector3(0.8f, 0.5f, 0.7f), coatM, false);
            Prim(PrimitiveType.Sphere, "Head", model, new Vector3(0, 1.25f, 0), Vector3.one * 0.8f, furM, false);
            Prim(PrimitiveType.Cube, "Ear_L", model, new Vector3(-0.25f, 1.62f, 0), new Vector3(0.2f, 0.28f, 0.12f), furM, false, new Vector3(0, 0, 18));
            Prim(PrimitiveType.Cube, "Ear_R", model, new Vector3(0.25f, 1.62f, 0), new Vector3(0.2f, 0.28f, 0.12f), furM, false, new Vector3(0, 0, -18));
            Prim(PrimitiveType.Cylinder, "Hat_Brim", model, new Vector3(0, 1.62f, 0), new Vector3(1.45f, 0.03f, 1.45f), hatM, false);
            Prim(PrimitiveType.Cylinder, "Hat_Crown", model, new Vector3(0, 1.8f, 0), new Vector3(0.62f, 0.17f, 0.62f), hatM, false);
            Prim(PrimitiveType.Cylinder, "Hat_Band", model, new Vector3(0, 1.69f, 0), new Vector3(0.64f, 0.04f, 0.64f), scarfM, false);
            Prim(PrimitiveType.Cube, "Scarf", model, new Vector3(0, 0.9f, 0.05f), new Vector3(0.7f, 0.14f, 0.6f), scarfM, false);
            Prim(PrimitiveType.Sphere, "Eye_L", model, new Vector3(-0.17f, 1.3f, 0.34f), Vector3.one * 0.13f, eyeM, false);
            Prim(PrimitiveType.Sphere, "Eye_R", model, new Vector3(0.17f, 1.3f, 0.34f), Vector3.one * 0.13f, eyeM, false);
            for (int i = 0; i < 4; i++)
                Prim(PrimitiveType.Cylinder, "Tail_" + i, model, new Vector3(0, 0.4f + i * 0.12f, -0.5f - i * 0.16f), new Vector3(0.16f, 0.09f, 0.16f), i % 2 == 0 ? furM : coatM, false, new Vector3(-50, 0, 0));
            Prim(PrimitiveType.Cube, "Gun_R", model, new Vector3(0.48f, 0.75f, 0.35f), new Vector3(0.14f, 0.18f, 0.55f), gunM, false);
            Prim(PrimitiveType.Cube, "Gun_L", model, new Vector3(-0.48f, 0.75f, 0.35f), new Vector3(0.14f, 0.18f, 0.55f), gunM, false);
            var mR = new GameObject("Muzzle_R").transform; mR.SetParent(model, false); mR.localPosition = new Vector3(0.48f, 0.78f, 0.7f);
            var mL = new GameObject("Muzzle_L").transform; mL.SetParent(model, false); mL.localPosition = new Vector3(-0.48f, 0.78f, 0.7f);
            pc.model = model;
            pc.rightMuzzle = mR;
            pc.leftMuzzle = mL;
            return pc;
        }

        // ---------- 판 ----------
        static void BuildRunInternal()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            MakeLight();
            var p = MakePlayer(new Vector3(0, 0.1f, -8));
            MakeCamera(p.transform.position);
            new GameObject("RunManager").AddComponent<RunManager>();
            var ui = new GameObject("GameUI").AddComponent<GameUI>();
            ui.village = false;
            EditorSceneManager.SaveScene(scene, RunPath);
        }

        // ---------- 마을 ----------
        static void BuildVillageInternal()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            MakeLight();
            var sand = Mat("VillageGround", new Color(0.72f, 0.56f, 0.42f), 0);
            var path = Mat("VillagePath", new Color(0.62f, 0.47f, 0.36f), 0);
            var wood = Mat("VillageWood", new Color(0.55f, 0.36f, 0.24f), 4);
            var woodDark = Mat("VillageWoodDark", new Color(0.36f, 0.22f, 0.16f), 4);
            var roofRed = Mat("RoofRed", new Color(0.62f, 0.22f, 0.2f), 4);
            var roofBlue = Mat("RoofBlue", new Color(0.3f, 0.36f, 0.55f), 4);
            var roofGreen = Mat("RoofGreen", new Color(0.32f, 0.5f, 0.36f), 4);
            var roofPurple = Mat("RoofPurple", new Color(0.46f, 0.32f, 0.6f), 4);
            var brass = Mat("Brass", new Color(0.92f, 0.7f, 0.28f), 4);
            var ivory = Mat("Ivory", new Color(0.95f, 0.9f, 0.78f), 4);
            var cactus = Mat("Cactus", new Color(0.45f, 0.62f, 0.38f), 4);
            var locked = Mat("Boards", new Color(0.3f, 0.24f, 0.2f), 4);

            var town = new GameObject("Village").transform;
            Prim(PrimitiveType.Cube, "Ground", town, new Vector3(0, -0.1f, 0), new Vector3(100, 0.2f, 72), sand);
            Prim(PrimitiveType.Cube, "MainStreet", town, new Vector3(0, 0.005f, -4), new Vector3(9, 0.02f, 64), path, false);
            Prim(PrimitiveType.Cube, "CrossStreet", town, new Vector3(0, 0.006f, 4), new Vector3(78, 0.02f, 8), path, false);
            // 바깥 울타리 (못 나가게)
            Prim(PrimitiveType.Cube, "Fence_N", town, new Vector3(0, 0.7f, 35), new Vector3(100, 1.4f, 0.4f), woodDark);
            Prim(PrimitiveType.Cube, "Fence_S", town, new Vector3(0, 0.7f, -35), new Vector3(100, 1.4f, 0.4f), woodDark);
            Prim(PrimitiveType.Cube, "Fence_W", town, new Vector3(-49, 0.7f, 0), new Vector3(0.4f, 1.4f, 72), woodDark);
            Prim(PrimitiveType.Cube, "Fence_E", town, new Vector3(49, 0.7f, 0), new Vector3(0.4f, 1.4f, 72), woodDark);

            // 여관 (가운데 북쪽) — 2층 = 기억의 방
            var inn = Building(town, "Inn_MemoryRoom", new Vector3(0, 0, 21), new Vector3(14, 8, 9), wood, roofRed, CatTang.Meta.Facility.MemoryRoom, "여관 · 2층 기억의 방", locked);
            Prim(PrimitiveType.Cube, "Balcony", inn, new Vector3(0, 3.6f, -4.4f), new Vector3(9, 0.25f, 1.2f), woodDark);
            Prim(PrimitiveType.Cube, "SignBoard", inn, new Vector3(0, 5.4f, -4.15f), new Vector3(5, 1.2f, 0.2f), brass, false);
            // 시계탑 (여관 옆)
            var tower = new GameObject("ClockTower").transform;
            tower.SetParent(town, false);
            tower.localPosition = new Vector3(14, 0, 22);
            Prim(PrimitiveType.Cube, "Tower", tower, new Vector3(0, 4.5f, 0), new Vector3(3, 9, 3), wood);
            Prim(PrimitiveType.Cylinder, "Clock", tower, new Vector3(0, 7.5f, -1.55f), new Vector3(2.2f, 0.08f, 2.2f), ivory, false, new Vector3(90, 0, 0));
            Prim(PrimitiveType.Cube, "Hand", tower, new Vector3(0.3f, 7.8f, -1.65f), new Vector3(0.12f, 0.9f, 0.05f), woodDark, false, new Vector3(0, 0, -30));
            Prim(PrimitiveType.Cube, "Roof", tower, new Vector3(0, 9.6f, 0), new Vector3(3.6f, 0.6f, 3.6f), roofRed, false);

            Building(town, "GunShop", new Vector3(-28, 0, 12), new Vector3(9, 5, 7), wood, roofBlue, CatTang.Meta.Facility.GunShop, "총포상", locked);
            Building(town, "AmmoWorkshop", new Vector3(28, 0, 12), new Vector3(9, 5, 7), wood, roofGreen, CatTang.Meta.Facility.AmmoWorkshop, "탄약 공방", locked);
            Building(town, "Wardrobe", new Vector3(28, 0, -14), new Vector3(8, 4.5f, 7), wood, roofPurple, CatTang.Meta.Facility.Wardrobe, "의상실", locked);

            // 시간의 문 (남쪽 끝, 챕터 선택)
            var gate = new GameObject("TimeGate").transform;
            gate.SetParent(town, false);
            gate.localPosition = new Vector3(0, 0, -29);
            Prim(PrimitiveType.Cube, "Post_L", gate, new Vector3(-3, 2.5f, 0), new Vector3(0.8f, 5, 0.8f), brass);
            Prim(PrimitiveType.Cube, "Post_R", gate, new Vector3(3, 2.5f, 0), new Vector3(0.8f, 5, 0.8f), brass);
            Prim(PrimitiveType.Cube, "Lintel", gate, new Vector3(0, 5.2f, 0), new Vector3(7.4f, 0.7f, 0.9f), brass, false);
            Prim(PrimitiveType.Cylinder, "GateClock", gate, new Vector3(0, 6.6f, 0), new Vector3(2.6f, 0.1f, 2.6f), ivory, false, new Vector3(90, 0, 0));
            Prim(PrimitiveType.Cube, "Portal", gate, new Vector3(0, 2.4f, 0.1f), new Vector3(5.2f, 4.6f, 0.1f), Mat("Portal", new Color(0.55f, 0.42f, 0.85f), 0), false);
            var cg = gate.gameObject.AddComponent<ChapterGate>();
            cg.prompt = "시간의 문 · 챕터로";
            cg.radius = 3.4f;

            // 소품
            var rnd = new System.Random(7);
            for (int i = 0; i < 40; i++)
            {
                float x = (float)(rnd.NextDouble() * 90 - 45), z = (float)(rnd.NextDouble() * 64 - 32);
                if (Mathf.Abs(x) < 7 || (Mathf.Abs(z - 4) < 6) || (Mathf.Abs(x) < 36 && z > 5 && z < 28) || (x > 21 && z < -7 && z > -21) || (x < -28 && z < -14)) continue;
                int k = rnd.Next(0, 3);
                if (k == 0) Prim(PrimitiveType.Cylinder, "Barrel", town, new Vector3(x, 0.6f, z), new Vector3(0.8f, 0.6f, 0.8f), woodDark);
                else if (k == 1) Prim(PrimitiveType.Cube, "Crate", town, new Vector3(x, 0.5f, z), Vector3.one, wood, true, new Vector3(0, rnd.Next(0, 90), 0));
                else
                {
                    var c = Prim(PrimitiveType.Capsule, "Cactus", town, new Vector3(x, 1, z), new Vector3(0.6f, 1, 0.6f), cactus);
                    Prim(PrimitiveType.Capsule, "Arm", c.transform, new Vector3(0.8f, 0.2f, 0), new Vector3(0.6f, 0.4f, 0.6f), cactus, false);
                }
            }
            // 물탱크
            var wt = new GameObject("WaterTower").transform;
            wt.SetParent(town, false);
            wt.localPosition = new Vector3(-34, 0, -20);
            for (int i = 0; i < 4; i++) Prim(PrimitiveType.Cube, "Leg" + i, wt, new Vector3((i % 2) * 2 - 1, 2, (i / 2) * 2 - 1), new Vector3(0.25f, 4, 0.25f), woodDark);
            Prim(PrimitiveType.Cylinder, "Tank", wt, new Vector3(0, 5, 0), new Vector3(3, 1.5f, 3), wood);

            var p = MakePlayer(new Vector3(0, 0.1f, 12));
            p.combatEnabled = false;
            MakeCamera(p.transform.position);
            new GameObject("VillageManager").AddComponent<VillageManager>();
            var ui = new GameObject("GameUI").AddComponent<GameUI>();
            ui.village = true;
            EditorSceneManager.SaveScene(scene, VillagePath);
        }

        static Transform Building(Transform town, string name, Vector3 pos, Vector3 size, Material wall, Material roof, CatTang.Meta.Facility f, string label, Material boards)
        {
            var b = new GameObject(name).transform;
            b.SetParent(town, false);
            b.localPosition = pos;
            Prim(PrimitiveType.Cube, "Walls", b, new Vector3(0, size.y / 2, 0), size, wall);
            Prim(PrimitiveType.Cube, "Roof", b, new Vector3(0, size.y + 0.3f, 0), new Vector3(size.x + 1, 0.6f, size.z + 1), roof, false);
            Prim(PrimitiveType.Cube, "Porch", b, new Vector3(0, 0.15f, -size.z / 2 - 1), new Vector3(size.x * 0.7f, 0.3f, 2), Mat("VillageWoodDark", new Color(0.36f, 0.22f, 0.16f), 4));
            var door = Prim(PrimitiveType.Cube, "Door", b, new Vector3(0, 1.3f, -size.z / 2 - 0.05f), new Vector3(1.8f, 2.6f, 0.15f), Mat("DoorOpen", new Color(0.85f, 0.62f, 0.25f), 4), false);
            var boardsGo = new GameObject("Locked").transform;
            boardsGo.SetParent(b, false);
            Prim(PrimitiveType.Cube, "Board1", boardsGo, new Vector3(0, 1.6f, -size.z / 2 - 0.15f), new Vector3(2.4f, 0.3f, 0.1f), boards, false, new Vector3(0, 0, 20));
            Prim(PrimitiveType.Cube, "Board2", boardsGo, new Vector3(0, 1.0f, -size.z / 2 - 0.15f), new Vector3(2.4f, 0.3f, 0.1f), boards, false, new Vector3(0, 0, -18));
            var entry = new GameObject("Entrance");
            entry.transform.SetParent(b, false);
            entry.transform.localPosition = new Vector3(0, 0, -size.z / 2 - 1.6f);
            var fd = entry.AddComponent<FacilityDoor>();
            fd.facility = f;
            fd.prompt = label;
            fd.radius = 2.8f;
            fd.lockedVisual = boardsGo.gameObject;
            fd.openVisual = door;
            return b;
        }
    }
}
