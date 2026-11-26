// 스토리 맵 4 '시계를 멈춘 공방' 키트 → 프리팹 → 맵 조립 (2026-11-25 사용자: 4장 보스문 앞 기억 맵, 블렌더로)
//   원본 Art/tools/blender_kit_story_workshop.py → Assets/Art/Characters/StoryWorkshop/Kit_W_*.fbx → Resources/Kit/Story/W_*.prefab
//   맵 = Resources/Story/StoryWorkshop.prefab (프리팹 원점 = 방 가운데 바닥, StoryWorkshop.cs가 (−1400, 0, −1400)에 놓고 이름으로 깊이 찾음)
//   비 오는 밤 시계공의 공방: 남쪽 입구 → 가운데 문자판 바닥(메달 12칸) → 북쪽 괘종시계 · 서쪽 진열판 벽시계 4 · 동쪽 작업대 + 그녀의 오르골 + 톱니 3
//   색 (2026-11-26 사용자 확정, 4장 전투 맵과 '아예 다른 공간' = 기억 속 집): 따뜻한 나무 — 호두(짙은 갈색) · 참나무(꿀색) 바닥 · 벽 판재 · 몰딩, 크림 회벽,
//     놋쇠 · 금빛 시계 테 · 진자, 초록 펠트 작업대, 붉은 갈색 가죽 의자, 상아 문자판, 포도주 커튼, 문자판 바닥 = 메이플 + 호두 상감 + 놋쇠 숫자. 숄만 진한 빨강.
//     빛 = 따뜻한 점광원 WarmLight_1..10(시계를 멈출 때마다 하나씩 꺼짐 — StoryWorkshop.cs) + 창 달빛(차가운 푸른빛 MoonLight_L/R). 명세 Art/tools/specs/kit_story_workshop_spec.md
//   축 규칙 (StoryWorkshop.cs와 맞춤):
//     바늘 HourHand · MinuteHand = 원점이 축, 로컬 z축으로 돎 (0° = 12시, h시 = −30h°) · 진자 Pendulum = 매단 점, 로컬 z축으로 흔듦
//     Gear_n = 톱니 자체가 축 가운데(받침은 GearStand_n 따로), 로컬 forward로 돎 · MusicCylinder = 로컬 right 축
//     Tile_n = 메달 가운데가 원점, 회전 없이 자리(위치)만 바뀜 → 둥근 메달이라 어느 자리에 가도 맞음, 숫자 Num은 자식
//     OldManShadow/ArmPivot = 오른 어깨, 로컬 x −70°면 팔이 앞(괘종시계 쪽)으로 들림 (피벗을 y 180° 돌려 둠)
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CatTang.EditorTools
{
    public static partial class EnemyModelBuilder
    {
        public const string StoryWorkshopSrc = "Assets/Art/Characters/StoryWorkshop/";
        public const string StoryWorkshopMapPath = "Assets/Resources/Story/StoryWorkshop.prefab";

        static readonly string[] KitWorkshop =
        {
            "Ground", "Room", "Window", "DialRim", "DialTile",
            "Num_1", "Num_2", "Num_3", "Num_4", "Num_5", "Num_6", "Num_7", "Num_8", "Num_9", "Num_10", "Num_11", "Num_12",
            "GrandClock", "GrandHour", "GrandMinute", "GrandPendulum", "WallClock1", "WallClock2", "WallClock3", "WallClock4",
            "HandHour", "HandMinute", "HandHourRed", "HandMinuteRed", "PendulumS", "DecoRound", "DecoSun", "ClockShelf",
            "Workbench", "MusicBox", "MusicCylinder", "Lamp", "RedLamp", "SideTable", "Gear", "GearStand",
            "Chair", "EmptyChair", "Stool", "ToolCabinet", "PartsCrates", "CoveredMirror", "UmbrellaStand", "Rug", "Sconce",
        };
        // 블렌더 원점 그대로(회전축 · 맵 원점 · 벽 뒷면) — blender_kit_story_workshop.py KEEP과 같음
        static readonly HashSet<string> KeepWorkshop = new()
        {
            "Ground", "Room", "DialRim", "DialTile", "GrandHour", "GrandMinute", "GrandPendulum", "HandHour", "HandMinute", "HandHourRed", "HandMinuteRed",
            "PendulumS", "Gear", "MusicCylinder", "GrandClock", "WallClock1", "WallClock2", "WallClock3", "WallClock4", "MusicBox", "GearStand",
            "Window", "DecoRound", "DecoSun", "ClockShelf", "Sconce",
        };
        static readonly string[] FlatWorkshop = { "Ground", "DialRim", "DialTile", "Rug" };

        // 따뜻한 나무 공방 (2026-11-26 사용자: 기억 속 집 — 4장 전투 맵과 다른 공간). 블렌더 PALETTE와 같은 값
        public static readonly Dictionary<string, Color> StoryWorkshopPalette = new()
        {
            // 바닥 판자: 참나무 꿀색 · 호두 · 밝은 참나무 + 짙은 이음
            { "plank", new Color(0.7f, 0.48f, 0.26f) }, { "plank_dark", new Color(0.48f, 0.3f, 0.17f) }, { "plank_light", new Color(0.82f, 0.62f, 0.38f) },
            { "plank_seam", new Color(0.27f, 0.16f, 0.09f) },
            // 벽: 크림 회벽 · 금빛 스텐실 · 참나무 징두리 · 꿀색 몰딩
            { "wallpaper", new Color(0.9f, 0.84f, 0.7f) }, { "wallpaper_pat", new Color(0.8f, 0.68f, 0.48f) }, { "wainscot", new Color(0.52f, 0.32f, 0.17f) },
            { "molding", new Color(0.74f, 0.52f, 0.29f) }, { "plaster_dark", new Color(0.22f, 0.13f, 0.07f) }, { "void", new Color(0.05f, 0.03f, 0.02f) },
            // 나무: 참나무 · 호두 · 메이플
            { "wood", new Color(0.66f, 0.44f, 0.24f) }, { "wood_dark", new Color(0.36f, 0.21f, 0.11f) }, { "wood_light", new Color(0.88f, 0.72f, 0.48f) },
            { "walnut", new Color(0.3f, 0.17f, 0.09f) }, { "maple", new Color(0.93f, 0.8f, 0.58f) },
            // 금속: 놋쇠 · 금 · 따뜻한 강철 · 짙은 청동
            { "iron", new Color(0.32f, 0.23f, 0.16f) }, { "iron_dark", new Color(0.17f, 0.11f, 0.07f) }, { "steel", new Color(0.8f, 0.76f, 0.68f) },
            { "steel_dark", new Color(0.44f, 0.37f, 0.3f) }, { "silver", new Color(0.96f, 0.92f, 0.82f) }, { "brass", new Color(0.92f, 0.72f, 0.36f) },
            { "brass_dark", new Color(0.62f, 0.43f, 0.2f) }, { "gold", new Color(1f, 0.82f, 0.36f) },
            // 문자판: 따뜻한 상아 · 세피아 눈금 · 밝은 참나무 상감
            { "dial_face", new Color(1f, 0.94f, 0.8f) }, { "dial_mark", new Color(0.2f, 0.11f, 0.06f) }, { "dial_inlay", new Color(0.86f, 0.74f, 0.54f) },
            // 빨강: 숄만 진한 빨강, 나머지 붉은 것은 벽돌빛으로 낮춤
            { "red_lacquer", new Color(0.7f, 0.12f, 0.1f) }, { "red_dark", new Color(0.42f, 0.08f, 0.07f) }, { "gem_red", new Color(0.9f, 0.16f, 0.14f) },
            { "shawl_red", new Color(0.82f, 0.06f, 0.08f) }, { "shawl_red_dark", new Color(0.52f, 0.03f, 0.06f) }, { "velvet_red", new Color(0.46f, 0.08f, 0.12f) },
            // 천 · 가죽: 포도주 커튼 · 크림 덮개 · 녹슨 붉은 깔개 · 붉은 갈색 가죽 · 초록 펠트
            { "curtain", new Color(0.42f, 0.08f, 0.14f) }, { "curtain_dark", new Color(0.26f, 0.04f, 0.09f) }, { "sheet", new Color(0.95f, 0.9f, 0.8f) },
            { "sheet_shade", new Color(0.78f, 0.7f, 0.58f) }, { "rug", new Color(0.56f, 0.24f, 0.16f) }, { "rug_dark", new Color(0.32f, 0.13f, 0.09f) },
            { "leather", new Color(0.55f, 0.24f, 0.14f) }, { "paper", new Color(0.96f, 0.9f, 0.76f) }, { "felt_green", new Color(0.2f, 0.42f, 0.3f) },
            // 유리 · 기타: 밤 유리(푸른빛) · 비 · 자기 · 돌
            { "glass", new Color(0.2f, 0.26f, 0.4f) }, { "rain", new Color(0.66f, 0.76f, 0.9f) }, { "porcelain", new Color(0.98f, 0.95f, 0.88f) },
            { "stone_dark", new Color(0.42f, 0.33f, 0.26f) },
            // 빛 (빌더가 빛나는 단색으로 바꿈)
            { "lantern_glow", new Color(1f, 0.82f, 0.5f) }, { "red_glow", new Color(1f, 0.35f, 0.22f) }, { "moon_glow", new Color(0.82f, 0.9f, 1f) },
        };

        // ---------- 기준점 (blender_kit_story_workshop.py ANCHORS, 유니티 로컬 = 블렌더 (x, z, y)) ----------
        static readonly Vector3 GrandHandsAt = new(0f, 2.1f, -0.65f), GrandPendAt = new(0f, 1.2f, -0.05f);
        // (바늘 축, 바늘 배율, 진자 축, 진자 배율, 빨간 바늘, 처음 시각 h, m)
        static readonly (Vector3 hands, float handScale, Vector3 pend, float pendScale, bool red, int h, int m)[] WallClockAnchors =
        {
            (new(0f, 2.75f, -0.37f), 1.44f, new(0f, 1.92f, -0.26f), 0.6f, false, 3, 0),
            (new(0f, 3.15f, -0.5f), 1.0f, new(0f, 2.55f, -0.27f), 1.25f, true, 7, 20),
            (new(0f, 2.62f, -0.55f), 1.0f, new(0f, 2.0f, -0.33f), 0.85f, true, 10, 10),
            (new(0f, 2.85f, -0.35f), 1.2f, new(0f, 2.1f, -0.25f), 0.5f, false, 5, 45),
        };
        static readonly Vector3 MusicCylAt = new(-0.17f, 0.24f, 0.02f);
        const float GearAxleY = 1.25f;

        // ---------- 배치 (방 안쪽 x −13 ~ 13, z −11 ~ 11) ----------
        static readonly Vector3 DialCenter = new(0f, 0f, 0.5f);
        const float MedallionRing = 4.1f;                       // 메달 12칸 중심 반지름 (메달 반지름 1.0, 문자판 바닥 반지름 5.6)
        static readonly Vector3 GrandClockAt = new(0f, 0f, 10.5f);
        static readonly float[] WallClockZ = { 7.2f, 2.6f, -2.0f, -6.6f };
        const float WallClockX = -12.1f, WallClockYaw = -60f; // 서쪽 벽에 비스듬히 세운 진열판: 정면이 동남동(카메라 쪽)
        static readonly (Vector3 pos, float yaw)[] GearsAt = { (new(9.6f, 0, 6.6f), -12f), (new(7.9f, 0, -1.9f), 8f), (new(10.2f, 0, -5.6f), -18f) };
        static readonly Vector3 BenchAt = new(12.3f, 0, 1.6f);

        static GameObject Wk(string name, Transform parent, Vector3 pos, float yaw, float scale = 1f, string rename = null)
        {
            string path = name.StartsWith("Q/") ? Ch3QuestKitDir + name.Substring(2) + ".prefab" : StoryKitDir + "W_" + name + ".prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!prefab) { Debug.LogWarning("[StoryWorkshop] 키트 없음 " + path); return null; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            go.transform.localScale *= scale;
            if (rename != null) go.name = rename;
            return go;
        }

        static float WorkshopNativeSize(string name)
        {
            var src = AssetDatabase.LoadAssetAtPath<GameObject>(StoryWorkshopSrc + name + ".fbx");
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

        [MenuItem("CatTang/스토리 맵 4 공방 키트 만들기")]
        public static void BuildStoryWorkshopKit()
        {
            System.IO.Directory.CreateDirectory(StoryKitDir);
            SrcDir = StoryWorkshopSrc;
            ModelPalette.Active = StoryWorkshopPalette;
            foreach (var k in KeepWorkshop) KeepOrigin.Add("Kit_W_" + k);
            foreach (var k in FlatWorkshop) FlatFloor.Add("Kit_W_" + k);
            try
            {
                int made = 0;
                foreach (var n in KitWorkshop)
                {
                    if (!Build("Kit_W_" + n, WorkshopNativeSize("Kit_W_" + n), 180f, 'h', StoryKitDir, "W_" + n)) continue;
                    FixWorkshopPrefab(StoryKitDir + "W_" + n + ".prefab", KeepWorkshop.Contains(n));
                    made++;
                }
                WarmShade();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"[StoryWorkshop] 키트 {made}/{KitWorkshop.Length}개");
            }
            finally
            {
                foreach (var k in KeepWorkshop) KeepOrigin.Remove("Kit_W_" + k);
                foreach (var k in FlatWorkshop) FlatFloor.Remove("Kit_W_" + k);
                SrcDir = Ch1Src;
                ModelPalette.Active = ModelPalette.Ch1;
            }
        }

        /// <summary>원점 고정 조각은 Mesh를 원점으로 되돌림(피벗 = 블렌더 원점) + 빛나는 재질(램프 · 붉은 유리 · 달)</summary>
        static void FixWorkshopPrefab(string path, bool keep)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var mesh = root.transform.Find("Mesh");
                if (keep && mesh) mesh.localPosition = Vector3.zero;
                WorkshopGlowMats(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static void WorkshopGlowMats(GameObject inst)
        {
            Material Get(string file, Color c, float rim)
            {
                string p = StoryWorkshopSrc + file + ".mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(p);
                if (!m) { m = new Material(Shader.Find("CatTang/Silhouette")); AssetDatabase.CreateAsset(m, p); }
                m.SetColor("_Color", c);
                if (m.HasProperty("_RimBoost")) m.SetFloat("_RimBoost", rim);
                EditorUtility.SetDirty(m);
                return m;
            }
            var lamp = Get("Glow_Lamp", new Color(1f, 0.8f, 0.46f, 1f), 0.5f);
            var red = Get("Glow_RedLamp", new Color(1f, 0.22f, 0.18f, 1f), 0.5f);
            var moon = Get("Glow_Moon", new Color(0.84f, 0.92f, 1f, 1f), 0.4f);
            foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    var pick = SrcMatName(r, i) switch { "lantern_glow" => lamp, "red_glow" => red, "moon_glow" => moon, _ => null };
                    if (pick) { mats[i] = pick; changed = true; }
                }
                if (changed) r.sharedMaterials = mats;
            }
        }

        /// <summary>등잔불 실내 그늘: 따뜻한 갈색 (ToonFor 기본은 황혼 보라)</summary>
        static void WarmShade()
        {
            foreach (var guid in AssetDatabase.FindAssets("Kit_W_ t:Material", new[] { StoryWorkshopSrc.TrimEnd('/') }))
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (!m || !m.name.Contains("_Toon") || !m.HasProperty("_ShadeColor")) continue;
                m.SetColor("_ShadeColor", new Color(0.8f, 0.66f, 0.56f));
                m.SetColor("_SecondShadeColor", new Color(0.52f, 0.36f, 0.3f));
                EditorUtility.SetDirty(m);
            }
        }

        /// <summary>회전한 물체도 맞게: 렌더러 메시 경계를 물체 로컬로 모아 바닥에서 2.5m 높이 상자 판정</summary>
        static void WsBlock(GameObject go, float shrink = 0.9f)
        {
            if (!go) return;
            bool any = false;
            var b = new Bounds();
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
            {
                if (!mf.sharedMesh) continue;
                var mb = mf.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var c = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    var p = go.transform.InverseTransformPoint(mf.transform.TransformPoint(c));
                    if (!any) { b = new Bounds(p, Vector3.zero); any = true; } else b.Encapsulate(p);
                }
            }
            if (!any) return;
            var bc = go.AddComponent<BoxCollider>();                                   // 원점 = 바닥 (판정 물체는 모두 바닥에 섬)
            float h = 2.5f / Mathf.Max(0.01f, go.transform.lossyScale.y);
            bc.size = new Vector3(b.size.x * shrink, h, b.size.z * shrink);
            bc.center = new Vector3(b.center.x, h / 2, b.center.z);
        }

        static void SetHands(Transform hh, Transform mh, int h, int m)
        {
            if (hh) hh.localRotation = Quaternion.Euler(0, 0, -((h % 12) + m / 60f) * 30f);
            if (mh) mh.localRotation = Quaternion.Euler(0, 0, -m * 6f);
        }

        static readonly Color WarmLamp = new(1f, 0.72f, 0.42f);

        static Light AddLight(Transform parent, string name, LightType type, Vector3 pos, Color c, float range, float intensity)
        {
            var l = new GameObject(name).AddComponent<Light>();
            l.transform.SetParent(parent, false);
            l.transform.localPosition = pos;
            l.type = type; l.color = c; l.range = range; l.intensity = intensity;
            l.shadows = LightShadows.None;
            return l;
        }

        /// <summary>북쪽 벽 바깥에만 내리는 비 (창 너머 · 벽 위로 보임, 방 안엔 안 내림)</summary>
        static void WorkshopRain(Transform root)
        {
            var go = new GameObject("RainOutside");
            go.transform.SetParent(root, false);
            go.transform.localPosition = new Vector3(0f, 9f, 13.6f);
            go.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true; main.playOnAwake = true; main.duration = 5f;
            main.startLifetime = 0.6f; main.startSpeed = 18f;
            main.startSize3D = true; main.startSizeX = 0.02f; main.startSizeY = 0.6f; main.startSizeZ = 0.02f;
            main.startColor = new Color(0.78f, 0.84f, 1f, 0.22f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 1500;
            var em = ps.emission; em.rateOverTime = 900f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(32f, 3.4f, 0.5f);
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch; r.velocityScale = 0.03f; r.lengthScale = 1.5f;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            const string matPath = StoryWorkshopSrc + "FX_Rain.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (!m)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default"));
                AssetDatabase.CreateAsset(m, matPath);
            }
            if (m.HasProperty("_Surface")) { m.SetFloat("_Surface", 1f); m.SetOverrideTag("RenderType", "Transparent"); m.renderQueue = 3000; m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); }
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
            EditorUtility.SetDirty(m);
            r.sharedMaterial = m;
        }

        [MenuItem("CatTang/스토리 맵 4 공방 맵 조립")]
        public static void BuildStoryWorkshopMap()
        {
            System.IO.Directory.CreateDirectory("Assets/Resources/Story");
            var root = new GameObject("StoryWorkshop").transform;
            try
            {
                var geo = new GameObject("Geo").transform; geo.SetParent(root, false);
                var marks = new GameObject("Marks").transform; marks.SetParent(root, false);
                var edges = new GameObject("Edges").transform; edges.SetParent(root, false);
                var lights = new GameObject("Lights").transform; lights.SetParent(root, false);

                Wk("Ground", geo, Vector3.zero, 0, 1f, "Ground");
                Wk("Room", geo, Vector3.zero, 0, 1f, "Room");
                var walk = new GameObject("WalkFloor").AddComponent<BoxCollider>();
                walk.transform.SetParent(root, false);
                walk.center = new Vector3(0, -0.5f, 0); walk.size = new Vector3(28, 1, 24);
                Wk("Window", geo, new Vector3(-6f, 0, 11f), 0, 1f, "Window_L");
                Wk("Window", geo, new Vector3(6f, 0, 11f), 0, 1f, "Window_R");

                // 문자판 바닥: 고정 테 + 둥근 메달 12칸 (Tile_n = n시 자리, 숫자 Num은 자식)
                var dial = new GameObject("DialFloor").transform; dial.SetParent(geo, false); dial.localPosition = DialCenter;
                Wk("DialRim", dial, Vector3.zero, 0, 1f, "DialRim");
                for (int n = 1; n <= 12; n++)
                {
                    float a = n * 30f * Mathf.Deg2Rad;
                    var tile = Wk("DialTile", dial, new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * MedallionRing, 0, 1f, "Tile_" + n);
                    if (tile) Wk("Num_" + n, tile.transform, new Vector3(0, 0.058f, 0), 0, 1f, "Num");
                }

                // 괘종시계 (북쪽 가운데) + 바늘 · 진자 · 디딤대 · 노인 그림자
                var grand = Wk("GrandClock", geo, GrandClockAt, 0, 1f, "GrandClock");
                if (grand)
                {
                    var hh = Wk("GrandHour", grand.transform, GrandHandsAt, 0, 1f, "HourHand");
                    var mh = Wk("GrandMinute", grand.transform, GrandHandsAt, 0, 1f, "MinuteHand");
                    SetHands(hh ? hh.transform : null, mh ? mh.transform : null, 11, 52);
                    Wk("GrandPendulum", grand.transform, GrandPendAt, 0, 1f, "Pendulum");
                    WsBlock(grand, 0.95f);
                }
                // 노인: 디딤대(윗면 0.63) 위에서 시계를 마주봄 → 팔을 70° 들면 손(높이 약 1.76)이 바늘 평형추 쪽에 닿음
                var stool = Wk("Stool", geo, new Vector3(0f, 0, 9.1f), 180f, 1f, "Stool");
                var om = Wk("Q/OldManShadow", geo, new Vector3(-0.2f, 0.625f, 9.25f), 180f, 1f, "OldManShadow");
                if (om)
                {
                    var meshOff = om.transform.Find("Mesh") ? om.transform.Find("Mesh").localPosition : Vector3.zero;
                    var pivot = new GameObject("ArmPivot").transform;
                    pivot.SetParent(om.transform, false);
                    // 오른 어깨 = 키트 로컬 (−0.205, 1.365, 0.015): 왼 소매가 +x에 있음을 메시로 확인 (3장 배치는 y 180° 돌린 +x 쪽에 둠 — 왼어깨 자리)
                    pivot.localPosition = ShoulderR + meshOff;
                    pivot.localRotation = Quaternion.Euler(0, 180f, 0);                 // 로컬 x −70° = 앞으로 듦 (3장은 +x가 앞)
                    var arm = Wk("Q/OldManArm", pivot, Vector3.zero, 0f);
                    if (arm) { arm.transform.localPosition = Vector3.zero; arm.transform.localRotation = Quaternion.Euler(0, 180f, 0); }
                }

                // 서쪽: 진열판 벽시계 4 (바늘 · 진자 · 표적 자리)
                for (int i = 0; i < 4; i++)
                {
                    var a = WallClockAnchors[i];
                    var wc = Wk("WallClock" + (i + 1), geo, new Vector3(WallClockX, 0, WallClockZ[i]), WallClockYaw, 1f, "WallClock_" + (i + 1));
                    if (!wc) continue;
                    var hh = Wk(a.red ? "HandHourRed" : "HandHour", wc.transform, a.hands, 0, a.handScale, "HourHand");
                    var mh = Wk(a.red ? "HandMinuteRed" : "HandMinute", wc.transform, a.hands, 0, a.handScale, "MinuteHand");
                    SetHands(hh ? hh.transform : null, mh ? mh.transform : null, a.h, a.m);
                    Wk("PendulumS", wc.transform, a.pend, 0, a.pendScale, "Pendulum");
                    WsBlock(wc, 0.9f);
                    var tg = new GameObject("Target").transform;                       // 진열판 앞 바닥 (쏘는 표적 자리)
                    tg.SetParent(wc.transform, false);
                    tg.localPosition = new Vector3(0, 0, -2.0f);
                }
                // 벽 장식 시계 (북 · 동 벽, 뒷면 = 벽면) — 진자 달린 것도 이름 Pendulum
                Wk("ClockShelf", geo, new Vector3(-9.0f, 2.0f, 10.98f), 0, 1f, "ClockShelf");
                Wk("DecoSun", geo, new Vector3(-11.75f, 3.3f, 10.98f), 0, 1f, "DecoSun_N");
                var dr1 = Wk("DecoRound", geo, new Vector3(8.9f, 3.5f, 10.98f), 0, 1f, "DecoClock_N");
                var dr2 = Wk("DecoRound", geo, new Vector3(11.8f, 3.1f, 10.98f), 0, 1f, "DecoClock_NE");
                Wk("DecoSun", geo, new Vector3(12.98f, 3.6f, 7.4f), 90f, 1f, "DecoSun_E");
                var dr3 = Wk("DecoRound", geo, new Vector3(12.98f, 3.3f, -6.4f), 90f, 1f, "DecoClock_E");
                Wk("DecoSun", geo, new Vector3(-12.98f, 3.4f, -10.0f), -90f, 1f, "DecoSun_W");
                foreach (var d in new[] { dr1, dr2, dr3 })
                    if (d) Wk("PendulumS", d.transform, new Vector3(0, -0.36f, -0.12f), 0, 0.4f, "Pendulum");

                // 동쪽: 작업대 + 그녀의 오르골 + 램프 · 의자 둘 · 깔개 · 톱니 3
                var bench = Wk("Workbench", geo, BenchAt, 90f, 1f, "Workbench");
                if (bench)
                {
                    WsBlock(bench, 0.95f);
                    var box = Wk("MusicBox", bench.transform, new Vector3(-0.2f, 1.0f, 0f), 0, 1f, "MusicBox");
                    if (box) Wk("MusicCylinder", box.transform, MusicCylAt, 0, 1f, "MusicCylinder");
                    var lamp = Wk("Lamp", bench.transform, new Vector3(1.15f, 1.0f, 0.2f), 0, 1f, "Lamp");
                    if (lamp) AddLight(lamp.transform, "WarmLight_10", LightType.Point, new Vector3(0, 0.42f, 0), WarmLamp, 8f, 2.6f);   // 마지막까지 남는 불
                }
                Wk("Rug", geo, new Vector3(10.4f, 0, 1.5f), 90f, 1f, "Rug");
                WsBlock(Wk("Chair", geo, new Vector3(10.6f, 0, 2.5f), -90f, 1f, "Chair"), 0.8f);
                WsBlock(Wk("EmptyChair", geo, new Vector3(10.4f, 0, 0.5f), -112f, 1f, "EmptyChair"), 0.8f);
                var st = Wk("SideTable", geo, new Vector3(10.0f, 0, -0.95f), 0, 1f, "SideTable");
                WsBlock(st, 0.8f);
                var rl = Wk("RedLamp", geo, new Vector3(10.0f, 0.71f, -0.95f), 30f, 1f, "RedLamp");
                if (rl) AddLight(rl.transform, "WarmLight_9", LightType.Point, new Vector3(0, 0.3f, 0), new Color(1f, 0.42f, 0.26f), 4.5f, 1.6f);
                for (int i = 0; i < 3; i++)
                {
                    var (pos, yaw) = GearsAt[i];
                    WsBlock(Wk("GearStand", geo, pos, yaw, 1f, "GearStand_" + (i + 1)), 0.85f);
                    var g = Wk("Gear", geo, pos + Vector3.up * GearAxleY, yaw, 1f, "Gear_" + (i + 1));
                    if (!g) continue;
                    g.transform.localRotation = Quaternion.Euler(0, yaw, i * 7f);      // 축 = 로컬 forward
                    var lb = new GameObject("Label").transform;                        // 가운데 숫자판 앞
                    lb.SetParent(g.transform, false);
                    lb.localPosition = new Vector3(0, 0, -0.2f);
                }
                WsBlock(Wk("CoveredMirror", geo, new Vector3(12.0f, 0, 7.6f), 90f, 1f, "CoveredMirror"), 0.85f);
                WsBlock(Wk("ToolCabinet", geo, new Vector3(12.4f, 0, -8.6f), 90f, 1f, "ToolCabinet"), 0.95f);
                WsBlock(Wk("PartsCrates", geo, new Vector3(11.9f, 0, -2.7f), 90f, 1f, "PartsCrates"), 0.9f);
                WsBlock(Wk("PartsCrates", geo, new Vector3(-11.4f, 0, -9.7f), 25f, 1f, "PartsCrates_SW"), 0.9f);
                WsBlock(Wk("UmbrellaStand", geo, new Vector3(2.7f, 0, -10.3f), -20f, 1f, "UmbrellaStand"), 0.7f);

                // 빛: 창 달빛(차가움) 둘 + 작업대 램프(따뜻함) + 붉은 램프
                foreach (var x in new[] { -6f, 6f })
                {
                    var moon = AddLight(lights, x < 0 ? "MoonLight_L" : "MoonLight_R", LightType.Spot, new Vector3(x, 4.2f, 10.7f), new Color(0.68f, 0.78f, 1f), 18f, 6f);
                    moon.spotAngle = 55f; moon.innerSpotAngle = 25f;
                    moon.transform.LookAt(root.TransformPoint(new Vector3(x * 0.55f, 0, 3.8f)));
                    moon.transform.localRotation = Quaternion.Inverse(root.rotation) * moon.transform.rotation;
                }
                // 따뜻한 점광원 더: 벽 촛대 6 · 선반 램프 2 (WarmLight_1~8, 작업대 쪽 붉은 램프 9 · 작업대 램프 10)
                var sconces = new (string name, Vector3 pos, float yaw)[]
                {
                    ("Sconce_W1", new Vector3(-12.86f, 2.7f, 4.8f), -90f), ("Sconce_W2", new Vector3(-12.86f, 2.7f, -4.4f), -90f),
                    ("Sconce_E1", new Vector3(12.86f, 2.7f, -4.6f), 90f), ("Sconce_E2", new Vector3(12.86f, 2.7f, 4.6f), 90f),
                    ("Sconce_N1", new Vector3(-2.55f, 2.6f, 10.97f), 0f), ("Sconce_N2", new Vector3(2.55f, 2.6f, 10.97f), 0f),
                };
                int[] sconceLight = { 1, 2, 5, 6, 7, 8 };
                for (int i = 0; i < sconces.Length; i++)
                {
                    var sc = Wk("Sconce", geo, sconces[i].pos, sconces[i].yaw, 1f, sconces[i].name);
                    if (sc) AddLight(sc.transform, "WarmLight_" + sconceLight[i], LightType.Point, new Vector3(0, 0.15f, -0.3f), WarmLamp, 5.5f, 1.5f);
                }
                var shelfLamp = Wk("Lamp", geo, new Vector3(-7.95f, 2.41f, 10.78f), 0, 0.6f, "ShelfLamp");
                if (shelfLamp) AddLight(shelfLamp.transform, "WarmLight_3", LightType.Point, new Vector3(0, 0.42f, 0), WarmLamp, 5f, 1.4f);
                var cab = geo.Find("ToolCabinet");
                var cabLamp = cab ? Wk("Lamp", cab, new Vector3(-0.4f, 2.15f, 0f), 0, 0.7f, "CabinetLamp") : null;
                if (cabLamp) AddLight(cabLamp.transform, "WarmLight_4", LightType.Point, new Vector3(0, 0.42f, 0), WarmLamp, 5f, 1.4f);
                WorkshopRain(root);

                // 표지 (StoryWorkshop가 이름으로 찾음)
                Mark(marks, "Spawn", new Vector3(0f, 0, -9.2f));
                Mark(marks, "CamFocus", new Vector3(0f, 0, 1.0f));
                Mark(marks, "DialCenter", DialCenter);
                Mark(marks, "GrandClockFront", GrandClockAt + new Vector3(0, 0, -2.6f), 0);
                Mark(marks, "MusicBoxFront", BenchAt + new Vector3(-2.2f, 0, -0.2f), 90);
                for (int i = 0; i < 6; i++)
                {
                    float a = (30f + i * 60f) * Mathf.Deg2Rad;
                    Mark(marks, "ShadowSpawn_" + (i + 1), DialCenter + new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * 6.3f, (30f + i * 60f) + 180f);
                }

                // 가장자리: 방 네 벽 (입구는 장식 — 도착 지점이 안쪽)
                Wall(edges, new Vector3(-13f, 0, -11f), new Vector3(-13f, 0, 11f));
                Wall(edges, new Vector3(-13f, 0, 11f), new Vector3(13f, 0, 11f));
                Wall(edges, new Vector3(13f, 0, 11f), new Vector3(13f, 0, -11f));
                Wall(edges, new Vector3(13f, 0, -11f), new Vector3(-13f, 0, -11f));

                PrefabUtility.SaveAsPrefabAsset(root.gameObject, StoryWorkshopMapPath);
                Debug.Log("[StoryWorkshop] 맵 " + StoryWorkshopMapPath);
            }
            finally { Object.DestroyImmediate(root.gameObject); }
        }

        /// <summary>맵 프리팹을 미리보기 장면에 띄워 위(쿼터뷰 · 정수리)에서 찍음 → Art/tools/specs/story_workshop_top.png · _quarter.png (씬 파일 안 건드림)</summary>
        [MenuItem("CatTang/스토리 맵 4 공방 미리보기 찍기")]
        public static void ShotStoryWorkshop()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(StoryWorkshopMapPath);
            if (!prefab) { Debug.LogWarning("[StoryWorkshop] 맵 없음"); return; }
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
            try
            {
                var map = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                var sunGo = new GameObject("Sun");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(sunGo, scene);
                var sun = sunGo.AddComponent<Light>(); sun.type = LightType.Directional; sun.intensity = 0.55f; sun.color = new Color(0.78f, 0.84f, 1f);
                sunGo.transform.rotation = Quaternion.Euler(55f, -35f, 0f);
                var camGo = new GameObject("ShotCam");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(camGo, scene);
                var cam = camGo.AddComponent<Camera>();
                cam.scene = scene;
                cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.02f, 0.02f, 0.03f);
                cam.farClipPlane = 200f;
                var rt = new RenderTexture(1600, 1200, 24);
                cam.targetTexture = rt;
                void Shoot(string file)
                {
                    cam.Render();
                    RenderTexture.active = rt;
                    var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                    tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); tex.Apply();
                    RenderTexture.active = null;
                    System.IO.File.WriteAllBytes(System.IO.Path.Combine(Application.dataPath, "../../Art/tools/specs/" + file), tex.EncodeToPNG());
                    Object.DestroyImmediate(tex);
                }
                cam.orthographic = true; cam.orthographicSize = 14.5f;
                camGo.transform.SetPositionAndRotation(new Vector3(0, 40f, 0.2f), Quaternion.Euler(90f, 0, 0));
                Shoot("story_workshop_top.png");
                cam.orthographic = false; cam.fieldOfView = 50f;
                camGo.transform.position = new Vector3(0, 25f, -18.5f);
                camGo.transform.LookAt(new Vector3(0, 0, 1.2f));
                Shoot("story_workshop_quarter.png");
                cam.targetTexture = null;
                Object.DestroyImmediate(rt);
                Debug.Log("[StoryWorkshop] 미리보기 Art/tools/specs/story_workshop_top.png · _quarter.png");
            }
            finally { UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
