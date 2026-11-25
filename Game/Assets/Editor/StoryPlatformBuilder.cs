// 스토리 맵 2 '배웅하던 승강장' 키트 → 프리팹 → 맵 조립 (2026-11-25 사용자: 2장 보스문 앞 기억 맵, 블렌더로)
//   원본 Art/tools/blender_kit_story_platform.py → Assets/Art/Characters/StoryPlatform/Kit_P_*.fbx → Resources/Kit/Story/P_*.prefab
//   맵 = Resources/Story/StoryPlatform.prefab: 블렌더 바닥 · 개찰구 · 지붕 기둥 · 역사 벽 · 가스등 · 매단 시계 + 2장 키트 기차(Kit/Ch2 TrainEngine · TrainCar) · 벤치 · 짐 · 신호대
//   남쪽 개찰구 → 동쪽 선로 옆 긴 승강장(기차는 북쪽을 향해 섬) → 북쪽 앞마당(전투 원). 카메라는 남쪽 위에서 봄
using UnityEditor;
using UnityEngine;

namespace CatTang.EditorTools
{
    public static partial class EnemyModelBuilder
    {
        public const string StoryPlatformSrc = "Assets/Art/Characters/StoryPlatform/";
        public const string StoryPlatformMapPath = "Assets/Resources/Story/StoryPlatform.prefab";

        // (원본, 맞출 크기, 맞출 축, 프리팹 이름) — 크기 = 블렌더에서 뽑은 치수 그대로 (배율 1)
        static readonly (string name, float size, char fit, string outName)[] KitPlatform =
        {
            ("Kit_P_Ground", 0f, 'w', "P_Ground"),
            ("Kit_P_CanopyBay", 0f, 'h', "P_CanopyBay"),
            ("Kit_P_GasLamp", 0f, 'h', "P_GasLamp"),
            ("Kit_P_HangClock", 0f, 'h', "P_HangClock"),
            ("Kit_P_TicketGate", 0f, 'w', "P_TicketGate"),
            ("Kit_P_StationWall", 0f, 'h', "P_StationWall"),
            ("Kit_P_Fence", 0f, 'h', "P_Fence"),
            ("Kit_P_ShadowYoung", 0f, 'h', "P_ShadowYoung"),
            ("Kit_P_ShadowWoman", 0f, 'h', "P_ShadowWoman"),
        };

        /// <summary>FBX 치수(맞출 축)를 그대로 읽음 → 배율 1로 빌드</summary>
        static float NativeSize(string name, char fit)
        {
            var src = AssetDatabase.LoadAssetAtPath<GameObject>(StoryPlatformSrc + name + ".fbx");
            if (!src) return 1f;
            var inst = (GameObject)Object.Instantiate(src);
            try
            {
                var rs = inst.GetComponentsInChildren<Renderer>();
                if (rs.Length == 0) return 1f;
                var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                return fit switch { 'h' => b.size.y, 'd' => b.size.z, _ => b.size.x };   // Build와 같은 축 (180° 돌려도 x · z 길이는 같음)
            }
            finally { Object.DestroyImmediate(inst); }
        }

        [MenuItem("CatTang/스토리 승강장 키트 만들기 (블렌더)")]
        public static void BuildStoryPlatformKit()
        {
            System.IO.Directory.CreateDirectory(StoryKitDir);
            SrcDir = StoryPlatformSrc;
            ModelPalette.Active = ModelPalette.StoryPlatform;
            try
            {
                int made = 0;
                foreach (var k in KitPlatform)
                    if (Build(k.name, k.size > 0 ? k.size : NativeSize(k.name, k.fit), 180f, k.fit, StoryKitDir, k.outName)) made++;
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"[StoryPlatform] 키트 {made}개");
            }
            finally
            {
                SrcDir = Ch1Src;
                ModelPalette.Active = ModelPalette.Ch1;
            }
        }

        // 걷는 곳 윤곽 (blender_kit_story_platform.py WALK OUTLINE과 같음) — 동쪽 x 3.2 = 승강장 끝(선로로 떨어짐)
        static readonly Vector2[] PlatformOutline =
        {
            new(-7.0f, -41.0f), new(-7.2f, -20.0f), new(-7.0f, 0.0f), new(-7.4f, 12.0f), new(-11.0f, 14.0f), new(-15.5f, 17.5f), new(-17.0f, 23.0f),
            new(-16.0f, 29.0f), new(-12.5f, 34.0f), new(-6.0f, 36.5f), new(0.0f, 36.0f), new(3.2f, 34.0f), new(3.2f, -41.0f),
        };
        public const float TrackX = 5.5f, TrainY = -0.45f, EngineZ = 8f;

        static GameObject Pk(string name, Transform parent, Vector3 pos, float yaw, float scale = 1f)
        {
            string path = name.StartsWith("C2/") ? "Assets/Resources/Kit/Ch2/" + name.Substring(3) + ".prefab" : StoryKitDir + name + ".prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!prefab) { Debug.LogWarning("[StoryPlatform] 키트 없음 " + path); return null; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            go.transform.localScale *= scale;
            return go;
        }

        /// <summary>가스등 · 랜턴 불빛 (호박색 점광)</summary>
        static void LampLight(Transform parent, Vector3 local, float range = 7f, float intensity = 2.2f)
        {
            var l = new GameObject("LampLight").AddComponent<Light>();
            l.transform.SetParent(parent, false);
            l.transform.localPosition = local;
            l.type = LightType.Point;
            l.color = new Color(1f, 0.72f, 0.38f);
            l.range = range; l.intensity = intensity;
            l.shadows = LightShadows.None;
        }

        /// <summary>맵 전체에 내리는 비 (가는 빗줄기, 월드 공간)</summary>
        static void Rain(Transform root)
        {
            var go = new GameObject("Rain");
            go.transform.SetParent(root, false);
            go.transform.localPosition = new Vector3(-2f, 16f, 0f);
            go.transform.localRotation = Quaternion.Euler(90f, 0, 0);          // 모양(상자)의 앞 = 아래
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true; main.playOnAwake = true;
            main.duration = 5f;
            main.startLifetime = 0.85f;
            main.startSpeed = 22f;
            main.startSize3D = true;
            main.startSizeX = 0.018f; main.startSizeY = 0.7f; main.startSizeZ = 0.018f;
            main.startColor = new Color(0.8f, 0.86f, 1f, 0.2f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 4000;
            main.gravityModifier = 0.4f;
            var em = ps.emission; em.rateOverTime = 2600f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(36f, 96f, 0.5f);
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.velocityScale = 0.025f; r.lengthScale = 1.6f;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            const string matPath = StoryPlatformSrc + "FX_Rain.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (!m)
            {
                var sh2 = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default");
                m = new Material(sh2);
                AssetDatabase.CreateAsset(m, matPath);
            }
            if (m.HasProperty("_Surface")) { m.SetFloat("_Surface", 1f); m.SetOverrideTag("RenderType", "Transparent"); m.renderQueue = 3000; m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); }
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
            EditorUtility.SetDirty(m);
            r.sharedMaterial = m;
        }

        [MenuItem("CatTang/스토리 승강장 맵 조립")]
        public static void BuildStoryPlatformMap()
        {
            System.IO.Directory.CreateDirectory("Assets/Resources/Story");
            var root = new GameObject("StoryPlatform").transform;
            try
            {
                var geo = new GameObject("Geo").transform; geo.SetParent(root, false);
                var marks = new GameObject("Marks").transform; marks.SetParent(root, false);
                var edges = new GameObject("Edges").transform; edges.SetParent(root, false);

                // 바닥 (블렌더 원점 = 맵 원점) + 걷기 판정 넓은 판 하나 + 윤곽 벽
                var ground = Pk("P_Ground", geo, Vector3.zero, 0);
                if (ground) { var m = ground.transform.Find("Mesh"); if (m) { m.localPosition = Vector3.zero; m.localScale = Vector3.one; } }
                var walk = new GameObject("WalkFloor").AddComponent<BoxCollider>();
                walk.transform.SetParent(root, false);
                walk.center = new Vector3(-6, -0.5f, -2); walk.size = new Vector3(30, 1, 84);

                // 남쪽 개찰구 (가운데 통로만 열림 — 양옆 표 파는 칸은 막힘)
                Pk("P_TicketGate", geo, new Vector3(-1.9f, 0, -31f), 0);
                Wall(edges, new Vector3(-7.0f, 0, -31f), new Vector3(-4.4f, 0, -31f));
                Wall(edges, new Vector3(0.6f, 0, -31f), new Vector3(3.2f, 0, -31f));

                // 서쪽: 역사 벽 + 지붕 기둥 (지붕은 승강장 서쪽 2.6m만 덮음)
                foreach (float z in new[] { -34f, -22f, -10f, 2f })
                    Block(Pk("P_StationWall", geo, new Vector3(-9.4f, 0, z), 0), 0.9f);
                foreach (float z in new[] { -24f, -16f, -8f, 0f, 8f })
                {
                    var bay = Pk("P_CanopyBay", geo, new Vector3(-6.5f, 0, z), 0);
                    if (bay) LampLight(bay.transform, new Vector3(1.3f, 3.6f, 0), 6f, 1.6f);
                }

                // 승강장: 가스등 · 매단 시계 · 벤치 · 짐
                foreach (var lp in new[] { new Vector3(1.9f, 0, -26f), new Vector3(1.9f, 0, -14f), new Vector3(1.9f, 0, -2f), new Vector3(1.9f, 0, 10f) })
                {
                    var lamp = Pk("P_GasLamp", geo, lp, 0);
                    if (lamp) LampLight(lamp.transform, new Vector3(0, 3.3f, 0));
                }
                Pk("P_HangClock", geo, new Vector3(-3.6f, 0, -12f), 0);
                Pk("C2/Bench", geo, new Vector3(-5.3f, 0, -19.5f), 90);
                Pk("C2/Bench", geo, new Vector3(-5.3f, 0, -4.5f), 90);
                Pk("C2/LuggagePile", geo, new Vector3(-5.0f, 0, 4.5f), 20);
                Pk("C2/LuggagePile", geo, new Vector3(-5.4f, 0, -27.5f), -35, 0.85f);

                // 앞마당: 가스등 둘레 + 신호대(선로 위)
                foreach (var lp in new[] { new Vector3(-13.2f, 0, 19.5f), new Vector3(-13.6f, 0, 29.5f), new Vector3(-4.0f, 0, 33.5f), new Vector3(1.8f, 0, 18.0f) })
                {
                    var lamp = Pk("P_GasLamp", geo, lp, 0);
                    if (lamp) LampLight(lamp.transform, new Vector3(0, 3.3f, 0));
                }
                Pk("C2/LM_SignalGantry", geo, new Vector3(TrackX, -0.6f, 24f), 0);

                // 맞은편 승강장 난간
                for (float z = -45f; z <= 60f; z += 6f) Pk("P_Fence", geo, new Vector3(12.2f, 0, z), 0);

                // 기차: 북쪽을 향해 선 기관차 + 객차 셋 (2장 키트 — 피벗 = 가운데)
                var train = new GameObject("Train").transform; train.SetParent(geo, false);
                train.localPosition = new Vector3(TrackX, TrainY, 0);
                var eng = Pk("C2/TrainEngine", train, new Vector3(0, 0, EngineZ), 180); if (eng) eng.name = "Engine";   // 키트 앞(헤드라이트) = 로컬 −Z → 돌려서 북쪽을 봄
                float z0 = EngineZ - 4.5f;
                for (int i = 1; i <= 3; i++)
                {
                    var car = Pk("C2/TrainCar", train, new Vector3(0, 0, z0 - 0.2f - 4f), 0);
                    if (car) { car.name = "Car" + i; LampLight(car.transform, new Vector3(-1.8f, 1.8f, 0), 5f, 1.2f); }
                    z0 -= 8.2f;
                }

                Rain(root);

                // 표지 (StoryPlatform이 이름으로 찾음)
                Mark(marks, "Start", new Vector3(-1.9f, 0, -38.5f));
                Mark(marks, "Young", new Vector3(-2.6f, 0, -25f), 180);
                Mark(marks, "Window", new Vector3(0.6f, 0, -4.0f));
                Mark(marks, "Woman", new Vector3(2.4f, 0, -3.6f), 270);
                Mark(marks, "WomanDoor", new Vector3(2.95f, 0, -4.55f), 90);                 // 첫 객차 뒤 난간 앞 (승강장 끝)
                Mark(marks, "WomanBoard", new Vector3(TrackX, 0.6f, -4.3f), 270);   // 승강장(서쪽)을 보고 섬
                Mark(marks, "Chimney", new Vector3(TrackX, 0, EngineZ + 2.1f));   // 굴뚝 (기관차 모델 측정)
                Mark(marks, "Headlight", new Vector3(TrackX, 1.6f, EngineZ + 5.2f));
                Mark(marks, "ShootSpot", new Vector3(-1.5f, 0, 16.0f));                     // 헤드라이트가 화면 안에 들어오는 앞마당 남쪽
                Mark(marks, "YoungFront", new Vector3(1.4f, 0, 15.2f), 0);
                Mark(marks, "YoungEnd", new Vector3(2.2f, 0, 13.6f), 20);
                Mark(marks, "Arena", new Vector3(-6.5f, 0, 25f));

                // 가장자리 판정: 윤곽 그대로
                for (int i = 0; i < PlatformOutline.Length; i++)
                    Wall(edges, V3(PlatformOutline[i]), V3(PlatformOutline[(i + 1) % PlatformOutline.Length]));
                PrefabUtility.SaveAsPrefabAsset(root.gameObject, StoryPlatformMapPath);
                Debug.Log("[StoryPlatform] 맵 " + StoryPlatformMapPath);
            }
            finally { Object.DestroyImmediate(root.gameObject); }
        }
    }
}
