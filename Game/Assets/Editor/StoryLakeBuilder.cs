// 스토리 맵 3 '거울 같은 호숫가' 키트 → 프리팹 → 맵 조립 (2026-11-25 사용자: 3장 보스문 앞 기억 맵, 블렌더로)
//   원본 Art/tools/blender_kit_story_lake.py → Assets/Art/Characters/StoryLake/Kit_L_*.fbx → Resources/Kit/Story/L_*.prefab
//   맵 = Resources/Story/StoryLake.prefab: 블렌더 바닥(풀밭 · 물가 · 호수 바닥) + 거울 물(CatTang/MirrorFloor) + 잔교 · 기둥 시계 · 보트 · 부표 · 피크닉 · 버드나무 · 갈대
//   남쪽 풀밭 → 가운데 잔교(북쪽으로 20m) → 북쪽 호수. 전투 원 = 풀밭 빈터. 카메라는 남쪽 위에서 봄
using UnityEditor;
using UnityEngine;

namespace CatTang.EditorTools
{
    public static partial class EnemyModelBuilder
    {
        public const string StoryLakeSrc = "Assets/Art/Characters/StoryLake/";
        public const string StoryLakeMapPath = "Assets/Resources/Story/StoryLake.prefab";
        const float LakeWaterY = -0.15f;
        static readonly Vector3 JettyAt = new(2f, 0f, 4.2f);

        static readonly string[] KitLake = { "Ground", "Jetty", "PierClock", "Boat", "Buoy", "Picnic", "Willow", "ReedClump", "LilyPad", "LilyFlower", "SkipStone", "ShadowYoungL", "ShadowWomanL", "ShadowYoungSit", "ShadowWomanSit" };

        // 맑은 한낮 호숫가 (밝게 — 그늘이 칙칙하지 않게)
        public static readonly System.Collections.Generic.Dictionary<string, Color> StoryLakePalette = new()
        {
            { "grass", new Color(0.5f, 0.78f, 0.42f) }, { "grass_light", new Color(0.66f, 0.88f, 0.48f) }, { "grass_dark", new Color(0.36f, 0.6f, 0.36f) },
            { "flower_white", new Color(0.98f, 0.98f, 0.94f) }, { "flower_yellow", new Color(1f, 0.86f, 0.32f) }, { "flower_pink", new Color(1f, 0.62f, 0.78f) },
            { "dirt", new Color(0.74f, 0.58f, 0.42f) }, { "dirt_dark", new Color(0.54f, 0.42f, 0.32f) }, { "sand", new Color(0.95f, 0.88f, 0.7f) },
            { "pebble", new Color(0.78f, 0.76f, 0.76f) }, { "pebble_dark", new Color(0.58f, 0.56f, 0.6f) }, { "stone", new Color(0.76f, 0.76f, 0.8f) },
            { "stone_dark", new Color(0.54f, 0.54f, 0.62f) }, { "moss", new Color(0.46f, 0.68f, 0.42f) }, { "lakebed", new Color(0.36f, 0.52f, 0.58f) },
            { "wood", new Color(0.7f, 0.5f, 0.36f) }, { "wood_dark", new Color(0.46f, 0.32f, 0.24f) }, { "wood_light", new Color(0.86f, 0.72f, 0.54f) },
            { "rope", new Color(0.9f, 0.8f, 0.6f) }, { "iron", new Color(0.38f, 0.4f, 0.48f) }, { "iron_dark", new Color(0.22f, 0.23f, 0.3f) },
            { "brass", new Color(0.92f, 0.72f, 0.4f) }, { "gold", new Color(1f, 0.84f, 0.42f) }, { "paint_white", new Color(0.97f, 0.97f, 0.94f) },
            { "paint_blue", new Color(0.36f, 0.56f, 0.82f) }, { "paint_red", new Color(0.9f, 0.36f, 0.32f) }, { "clock_face", new Color(1f, 0.97f, 0.88f) },
            { "lantern_glow", new Color(1f, 0.82f, 0.48f) }, { "cloth_red", new Color(0.92f, 0.36f, 0.36f) }, { "cloth_white", new Color(0.98f, 0.96f, 0.92f) },
            { "wicker", new Color(0.84f, 0.66f, 0.4f) }, { "ivory", new Color(0.97f, 0.94f, 0.85f) }, { "leaf", new Color(0.44f, 0.7f, 0.4f) },
            { "leaf_dark", new Color(0.3f, 0.52f, 0.34f) }, { "leaf_light", new Color(0.66f, 0.85f, 0.46f) }, { "bark", new Color(0.52f, 0.4f, 0.32f) },
            { "reed", new Color(0.66f, 0.72f, 0.44f) }, { "reed_tip", new Color(0.58f, 0.42f, 0.3f) }, { "lily_pad", new Color(0.38f, 0.68f, 0.42f) },
            { "lily_pink", new Color(1f, 0.72f, 0.84f) }, { "lily_core", new Color(1f, 0.86f, 0.38f) }, { "shadow", new Color(0.08f, 0.06f, 0.12f) },
            { "shadow_rim", new Color(1f, 0.76f, 0.46f) }, { "hat_straw", new Color(0.94f, 0.82f, 0.56f) }, { "ribbon", new Color(0.96f, 0.48f, 0.58f) },
        };

        // 걷는 곳 윤곽 (blender_kit_story_lake.py WALK OUTLINE과 같음) — 북쪽 가장자리 = 물가
        static readonly Vector2[] LakeOutline =
        {
            new(-17.0f, -26.0f), new(-18.5f, -15.0f), new(-17.0f, -5.0f), new(-13.0f, 1.5f), new(-7.0f, 4.0f), new(0.0f, 4.6f), new(6.0f, 4.2f),
            new(12.0f, 2.0f), new(16.5f, -4.0f), new(18.0f, -15.0f), new(16.0f, -26.0f),
        };

        [MenuItem("CatTang/스토리 호숫가 키트 만들기 (블렌더)")]
        public static void BuildStoryLakeKit()
        {
            System.IO.Directory.CreateDirectory(StoryKitDir);
            SrcDir = StoryLakeSrc;
            ModelPalette.Active = StoryLakePalette;
            foreach (var k in new[] { "Kit_L_Ground", "Kit_L_Jetty", "Kit_L_ShadowYoungSit", "Kit_L_ShadowWomanSit" }) KeepOrigin.Add(k);
            FlatFloor.Add("Kit_L_Ground");                                     // 풀밭 조각 경계에 외곽선이 그물처럼 비치지 않게 얇게
            try
            {
                int made = 0;
                foreach (var n in KitLake)
                    if (Build("Kit_L_" + n, LakeNativeSize("Kit_L_" + n), 180f, 'h', StoryKitDir, "L_" + n)) made++;
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"[StoryLake] 키트 {made}개");
            }
            finally
            {
                SrcDir = Ch1Src;
                ModelPalette.Active = ModelPalette.Ch1;
            }
        }

        static float LakeNativeSize(string name)
        {
            var src = AssetDatabase.LoadAssetAtPath<GameObject>(StoryLakeSrc + name + ".fbx");
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

        static GameObject Lk(string name, Transform parent, Vector3 pos, float yaw, float scale = 1f)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(StoryKitDir + "L_" + name + ".prefab");
            if (!prefab) { Debug.LogWarning("[StoryLake] 키트 없음 L_" + name); return null; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            go.transform.localScale *= scale;
            return go;
        }

        /// <summary>원점을 지킨 키트(바닥 · 잔교 · 앉은 그림자): 빌더가 x · z만 가운데로 옮긴 것을 되돌림</summary>
        static void ResetMesh(GameObject go)
        {
            var m = go ? go.transform.Find("Mesh") : null;
            if (m) m.localPosition = new Vector3(0, m.localPosition.y, 0);
        }

        static Material LakeWaterMat()
        {
            const string path = StoryLakeSrc + "LakeWater.mat";
            var sh = Shader.Find("CatTang/LakeWater");
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m) { m = new Material(sh); AssetDatabase.CreateAsset(m, path); }
            m.shader = sh;                                                     // 2026-11-25 사용자: 물 느낌 확실하게 → 전용 물 셰이더 (굴절 · 깊이 색 · 거품 · 반짝임)
            m.SetFloat("_WaveStrength", 0.42f);                                // 물결 조금 더 (2026-11-25 사용자)
            m.SetFloat("_GlintAmount", 1f);
            m.SetFloat("_CrestAmount", 0.8f);
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>기둥 시계를 가리는 햇빛 번짐: 가운데가 하얗게 타고 가장자리로 부드럽게 사라지는 둥근 빛 + 가는 빛살 8줄 (예전엔 그림이 없어 네모로 보였음)</summary>
        static Texture2D SunGlareTex()
        {
            const string path = StoryLakeSrc + "SunGlare.png";
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex) return tex;
            const int N = 256;
            var t = new Texture2D(N, N, TextureFormat.RGBA32, false);
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float dx = (x + 0.5f) / N * 2f - 1f, dy = (y + 0.5f) / N * 2f - 1f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float core = Mathf.Clamp01(1f - r / 0.42f); core = core * core * (3f - 2f * core);
                    float halo = Mathf.Clamp01(1f - r); halo = halo * halo * halo;
                    float ang = Mathf.Atan2(dy, dx);
                    float rays = Mathf.Pow(Mathf.Abs(Mathf.Cos(ang * 4f)), 24f) * Mathf.Clamp01(1f - r) * 0.55f;
                    float a = Mathf.Clamp01(core + halo * 0.6f + rays);
                    t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            t.Apply();
            System.IO.File.WriteAllBytes(path, t.EncodeToPNG());
            Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(path);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.alphaIsTransparency = true; imp.wrapMode = TextureWrapMode.Clamp; imp.mipmapEnabled = false;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        [MenuItem("CatTang/스토리 호숫가 맵 조립")]
        public static void BuildStoryLakeMap()
        {
            System.IO.Directory.CreateDirectory("Assets/Resources/Story");
            var root = new GameObject("StoryLake").transform;
            try
            {
                var geo = new GameObject("Geo").transform; geo.SetParent(root, false);
                var marks = new GameObject("Marks").transform; marks.SetParent(root, false);
                var edges = new GameObject("Edges").transform; edges.SetParent(root, false);

                var ground = Lk("Ground", geo, Vector3.zero, 0); ResetMesh(ground);
                var walk = new GameObject("WalkFloor").AddComponent<BoxCollider>();
                walk.transform.SetParent(root, false);
                walk.center = new Vector3(0, -0.5f, -10f); walk.size = new Vector3(40, 1, 34);

                // 거울 같은 물: 큰 판 하나 (반사는 MirrorReflection이 그림)
                var water = GameObject.CreatePrimitive(PrimitiveType.Quad);
                water.name = "Water";
                Object.DestroyImmediate(water.GetComponent<Collider>());
                water.transform.SetParent(geo, false);
                water.transform.localPosition = new Vector3(0, LakeWaterY, 34f);
                water.transform.localRotation = Quaternion.Euler(90, 0, 0);
                water.transform.localScale = new Vector3(130f, 64f, 1f);
                var wr = water.GetComponent<MeshRenderer>();
                wr.sharedMaterial = LakeWaterMat();
                wr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                // 잔교 (걷는 판 = 상자 충돌체 + 양옆 벽)
                var jetty = Lk("Jetty", geo, JettyAt, 0); ResetMesh(jetty);
                var deck = new GameObject("JettyFloor").AddComponent<BoxCollider>();
                deck.transform.SetParent(root, false);
                deck.center = JettyAt + new Vector3(0, -0.25f, 10f); deck.size = new Vector3(4.4f, 0.5f, 20.4f);
                Wall(edges, JettyAt + new Vector3(-1.5f, 0, 0.2f), JettyAt + new Vector3(-1.5f, 0, 17.8f));
                Wall(edges, JettyAt + new Vector3(1.5f, 0, 0.2f), JettyAt + new Vector3(1.5f, 0, 17.8f));
                Wall(edges, JettyAt + new Vector3(-2.2f, 0, 17.8f), JettyAt + new Vector3(-2.2f, 0, 20.1f));
                Wall(edges, JettyAt + new Vector3(2.2f, 0, 17.8f), JettyAt + new Vector3(2.2f, 0, 20.1f));
                Wall(edges, JettyAt + new Vector3(-2.2f, 0, 20.1f), JettyAt + new Vector3(2.2f, 0, 20.1f));
                Wall(edges, JettyAt + new Vector3(-2.2f, 0, 17.8f), JettyAt + new Vector3(-1.5f, 0, 17.8f));
                Wall(edges, JettyAt + new Vector3(1.5f, 0, 17.8f), JettyAt + new Vector3(2.2f, 0, 17.8f));
                // 기둥 시계 (잔교 끝 뒤) · 햇빛 번짐
                var clock = Lk("PierClock", geo, JettyAt + new Vector3(1.7f, 0, 19.6f), 0);
                if (clock)
                {
                    clock.name = "PierClock";
                    var glare = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    glare.name = "SunGlare";
                    Object.DestroyImmediate(glare.GetComponent<Collider>());
                    glare.transform.SetParent(clock.transform, false);
                    glare.transform.localPosition = new Vector3(0, 3.2f, -0.2f);
                    glare.transform.localScale = Vector3.one * 2.6f;
                    glare.AddComponent<CatTang.Billboard>();
                    const string gp = StoryLakeSrc + "SunGlare.mat";
                    var gm = AssetDatabase.LoadAssetAtPath<Material>(gp);
                    if (!gm)
                    {
                        gm = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default"));
                        AssetDatabase.CreateAsset(gm, gp);
                    }
                    var glow = SunGlareTex();
                    if (gm.HasProperty("_BaseMap")) gm.SetTexture("_BaseMap", glow);
                    if (gm.HasProperty("_Surface")) { gm.SetFloat("_Surface", 1f); gm.SetFloat("_Blend", 2f); gm.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); gm.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One); gm.SetFloat("_ZWrite", 0f); gm.SetOverrideTag("RenderType", "Transparent"); gm.renderQueue = 3100; gm.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); }
                    if (gm.HasProperty("_BaseColor")) gm.SetColor("_BaseColor", new Color(1f, 0.97f, 0.85f, 0.95f));
                    EditorUtility.SetDirty(gm);
                    var grr = glare.GetComponent<MeshRenderer>(); grr.sharedMaterial = gm; grr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
                // 보트 (동쪽에 묶임) · 부표 둘
                var boat = Lk("Boat", geo, new Vector3(5.6f, LakeWaterY - 0.35f, 13f), 0); if (boat) boat.name = "Boat";
                var ba = Lk("Buoy", geo, new Vector3(-1.5f, LakeWaterY - 0.3f, 28.5f), 0); if (ba) ba.name = "BuoyA";
                var bb = Lk("Buoy", geo, new Vector3(-4.6f, LakeWaterY - 0.3f, 31.6f), 0); if (bb) bb.name = "BuoyB";
                // 풀밭: 피크닉 · 버드나무 · 갈대 · 물 위 연잎(꾸밈)
                Lk("Picnic", geo, new Vector3(-7.5f, 0, -3.2f), 15);
                // 버드나무: 가장자리로 물려 화면을 덜 가리게, 조금 작게
                foreach (var (p, y) in new[] { (new Vector3(-16.5f, 0, -7f), 0f), (new Vector3(16.8f, 0, -8f), 70f), (new Vector3(-15.5f, 0, -22f), 140f), (new Vector3(15.8f, 0, -22f), 210f) })
                    Block(Lk("Willow", geo, p, y, 0.8f), 0.25f);
                foreach (var p in new[] { new Vector3(-10f, LakeWaterY - 0.2f, 6.5f), new Vector3(9.5f, LakeWaterY - 0.2f, 5.8f), new Vector3(-4.2f, LakeWaterY - 0.2f, 6.2f), new Vector3(14f, LakeWaterY - 0.2f, 4f) })
                    Lk("ReedClump", geo, p, Random.Range(0f, 360f));
                foreach (var p in new[] { new Vector3(-12f, LakeWaterY, 12f), new Vector3(-6f, LakeWaterY, 16f), new Vector3(9f, LakeWaterY, 19f), new Vector3(12f, LakeWaterY, 11f), new Vector3(-9f, LakeWaterY, 24f) })
                    Lk("LilyPad", geo, p, Random.Range(0f, 360f), 0.9f);

                // 표지 (StoryLake가 이름으로 찾음)
                Mark(marks, "Start", new Vector3(0f, 0, -22f));
                Mark(marks, "Young", JettyAt + new Vector3(0.9f, 0, 17.0f), 180);
                Mark(marks, "Woman", JettyAt + new Vector3(-0.9f, 0, 17.6f), 180);
                Mark(marks, "JettyEnd", JettyAt + new Vector3(0f, 0, 16.0f));
                Mark(marks, "ClockWater", JettyAt + new Vector3(0f, 0, 23.6f));
                Mark(marks, "Circle", new Vector3(7.6f, 0, 14.5f));
                Mark(marks, "Throw", JettyAt + new Vector3(-1.2f, 0, 18.6f));
                Mark(marks, "BuoyA", ba ? ba.transform.localPosition : new Vector3(-1.5f, 0, 28.5f));
                Mark(marks, "BuoyB", bb ? bb.transform.localPosition : new Vector3(-4.6f, 0, 31.6f));
                Mark(marks, "Arena", new Vector3(0f, 0, -6.5f));
                Mark(marks, "Lily0", new Vector3(-7.5f, 0, 7.2f));
                Mark(marks, "Lily1", new Vector3(-2.2f, 0, 8.0f));
                Mark(marks, "Lily2", new Vector3(7.0f, 0, 7.4f));
                // 끝: 잔교 서쪽 가장자리에 나란히 앉아 서쪽 노을을 봄 (끝 단은 난간이 있어 가장자리 = 본 잔교 서쪽, 카메라엔 옆모습)
                Mark(marks, "SitYoung", JettyAt + new Vector3(-1.5f, 0, 14.4f), -90);
                Mark(marks, "SitWoman", JettyAt + new Vector3(-1.5f, 0, 15.5f), -90);
                Mark(marks, "CatWatch", JettyAt + new Vector3(0.7f, 0, 11.4f), 0);
                Mark(marks, "WaterLook", JettyAt + new Vector3(-3.6f, LakeWaterY, 14.6f));

                for (int i = 0; i < LakeOutline.Length; i++)
                {
                    var a = V3(LakeOutline[i]); var b = V3(LakeOutline[(i + 1) % LakeOutline.Length]);
                    // 잔교 입구(물가 x 0.5 ~ 3.5)는 열어 둠
                    if (a.z > 3f && b.z > 3f && a.x <= JettyAt.x + 1.5f && b.x >= JettyAt.x - 1.5f)
                    {
                        Wall(edges, a, new Vector3(JettyAt.x - 1.5f, 0, Mathf.Lerp(a.z, b.z, Mathf.InverseLerp(a.x, b.x, JettyAt.x - 1.5f))));
                        Wall(edges, new Vector3(JettyAt.x + 1.5f, 0, Mathf.Lerp(a.z, b.z, Mathf.InverseLerp(a.x, b.x, JettyAt.x + 1.5f))), b);
                        continue;
                    }
                    Wall(edges, a, b);
                }
                PrefabUtility.SaveAsPrefabAsset(root.gameObject, StoryLakeMapPath);
                Debug.Log("[StoryLake] 맵 " + StoryLakeMapPath);
            }
            finally { Object.DestroyImmediate(root.gameObject); }
        }
    }
}
