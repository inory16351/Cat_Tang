// 스토리 맵 5 '11:59의 병실' 키트 → 프리팹 → 맵 조립 (2026-11-26 사용자: 5장 보스 문 앞 기억 맵 = 지금 이 순간의 병실, 블렌더로)
//   원본 Art/tools/blender_kit_story_ward.py → Assets/Art/Characters/StoryWard/Kit_H_*.fbx → Resources/Kit/Story/H_*.prefab
//   맵 = Resources/Story/StoryWard.prefab (원점 = 방 가운데 바닥, StoryWard.cs가 (−1600, 0, −1400)에 놓고 이름으로 깊이 찾음)
//   밤 병실: 남쪽 입구 → 가운데 아래 전투 원(0, −3.5, r 6) → 북쪽 침대(노인 그림자) · 협탁(그림책) · 링거 · 모니터 · 북벽 왼쪽 벽시계 · 동북 구석 사신 그림자
//   축 규칙 (StoryWard.cs와 맞춤): 벽시계 바늘 HourHand · MinuteHand · SecondHand = 로컬 z축으로 돎 (0° = 12시, h시 = −30h°)
//     사신 머리 = ReaperBody/HeadPivot(목) 아래 ReaperHead, 피벗 y를 돌리면 고개를 돌림 · 링거 물방울 자리 = Marks/DripAt
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CatTang.EditorTools
{
    public static partial class EnemyModelBuilder
    {
        public const string StoryWardSrc = "Assets/Art/Characters/StoryWard/";
        public const string StoryWardMapPath = "Assets/Resources/Story/StoryWard.prefab";

        static readonly string[] KitWard =
        {
            "Ground", "Room", "Window", "Bed", "OldManLying", "Nightstand", "PictureBook", "WallClock", "HandHour", "HandMinute", "HandSecond",
            "IVStand", "IVDrop", "Monitor", "VisitorChair", "Cabinet", "CurtainRail", "ReaperBody", "ReaperHead", "Door", "Hourglass",
        };
        // 블렌더 원점 그대로 (blender_kit_story_ward.py KEEP과 같음)
        static readonly HashSet<string> KeepWard = new() { "Ground", "Room", "WallClock", "HandHour", "HandMinute", "HandSecond", "ReaperHead", "IVDrop", "OldManLying", "CurtainRail" };
        static readonly string[] FlatWard = { "Ground" };

        public static readonly Dictionary<string, Color> StoryWardPalette = new()
        {
            // 2026-11-26 사용자: 가시성 — 흰색 계열 병실 (바닥 · 벽을 밝게, 어두운 그림자 · 적이 또렷하게)
            { "tile", new Color(0.97f, 0.96f, 0.93f) }, { "tile_light", new Color(0.92f, 0.93f, 0.92f) }, { "tile_dark", new Color(0.85f, 0.87f, 0.87f) }, { "tile_seam", new Color(0.7f, 0.72f, 0.74f) },
            { "wall", new Color(0.96f, 0.96f, 0.94f) }, { "wall_pat", new Color(0.89f, 0.91f, 0.91f) }, { "wainscot", new Color(0.8f, 0.87f, 0.85f) }, { "molding", new Color(1f, 0.99f, 0.96f) },
            { "plaster_dark", new Color(0.6f, 0.65f, 0.7f) }, { "void", new Color(0.02f, 0.02f, 0.04f) },
            { "wood", new Color(0.66f, 0.44f, 0.24f) }, { "wood_dark", new Color(0.36f, 0.21f, 0.11f) }, { "wood_light", new Color(0.88f, 0.72f, 0.48f) }, { "walnut", new Color(0.3f, 0.17f, 0.09f) },
            { "steel", new Color(0.78f, 0.8f, 0.84f) }, { "steel_dark", new Color(0.36f, 0.38f, 0.44f) }, { "chrome", new Color(0.92f, 0.94f, 0.97f) }, { "brass", new Color(0.92f, 0.72f, 0.36f) }, { "gold", new Color(1f, 0.82f, 0.36f) },
            { "sheet", new Color(0.97f, 0.97f, 0.95f) }, { "sheet_shade", new Color(0.78f, 0.82f, 0.86f) }, { "pillow", new Color(0.95f, 0.95f, 0.92f) }, { "blanket", new Color(0.62f, 0.75f, 0.85f) }, { "blanket_pat", new Color(0.45f, 0.58f, 0.72f) },
            { "curtain", new Color(0.86f, 0.82f, 0.72f) }, { "curtain_dark", new Color(0.56f, 0.6f, 0.66f) },
            { "dial_face", new Color(1f, 0.94f, 0.8f) }, { "dial_mark", new Color(0.2f, 0.11f, 0.06f) },
            { "iv_bag", new Color(0.92f, 0.94f, 0.96f) }, { "iv_fluid", new Color(1f, 0.95f, 0.7f) }, { "tube", new Color(0.88f, 0.92f, 0.96f) },
            { "screen", new Color(0.04f, 0.06f, 0.06f) }, { "screen_glow", new Color(0.3f, 1f, 0.55f) }, { "plastic", new Color(0.86f, 0.87f, 0.88f) }, { "plastic_dark", new Color(0.45f, 0.47f, 0.5f) },
            { "book_cover", new Color(0.55f, 0.22f, 0.15f) }, { "book_page", new Color(0.96f, 0.9f, 0.76f) }, { "book_gold", new Color(1f, 0.75f, 0.35f) },
            { "shawl_red", new Color(0.82f, 0.06f, 0.08f) }, { "shawl_red_dark", new Color(0.5f, 0.03f, 0.06f) },
            { "glass", new Color(0.4f, 0.55f, 0.75f) }, { "night", new Color(0.08f, 0.1f, 0.25f) }, { "moon_glow", new Color(1f, 0.96f, 0.8f) }, { "lamp_glow", new Color(1f, 0.75f, 0.4f) },
            { "flower", new Color(0.98f, 0.97f, 0.94f) }, { "leaf", new Color(0.35f, 0.6f, 0.35f) }, { "vase", new Color(0.6f, 0.78f, 0.9f) },
            { "shadow", new Color(0.08f, 0.05f, 0.12f) }, { "shadow_rim", new Color(1f, 0.75f, 0.45f) },
        };

        // ---------- 기준점 (blender_kit_story_ward.py ANCHORS, 유니티 로컬 = 블렌더 (x, z, y)) ----------
        static readonly Vector3 WardHandsAt = new(0f, 0f, -0.3f), ReaperNeck = new(-0.0773f, 2.0f, -0.0027f), DripChamber = new(0.0122f, 1.52f, 0f);
        static readonly Vector3 WardBed = new(0f, 0f, 5.4f), WardClock = new(-6f, 3.0f, 9.75f), WardReaper = new(9.0f, 0f, 7.4f), WardIV = new(-2.9f, 0f, 6.9f);
        const float Prop = 1.4f, ClockScale = 1.3f, ReaperScale = 1.15f;   // 가구 가독성 배율 (고양이 2.17m · 방 24 × 20m에서 침대가 고양이보다 작아 보여 1.4배)
        public static readonly Vector3 WardBattle = new(0f, 0f, -3.5f);

        static GameObject Wd(string name, Transform parent, Vector3 pos, float yaw, float scale = 1f, string rename = null)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(StoryKitDir + "H_" + name + ".prefab");
            if (!prefab) { Debug.LogWarning("[StoryWard] 키트 없음 H_" + name); return null; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            go.transform.localScale *= scale;
            if (rename != null) go.name = rename;
            return go;
        }

        /// <summary>정면(−Z)이 d 방향을 보게 하는 yaw</summary>
        static float Face(Vector3 from, Vector3 to) { var d = to - from; return Mathf.Atan2(-d.x, -d.z) * Mathf.Rad2Deg; }

        static float WardNativeSize(string name)
        {
            var src = AssetDatabase.LoadAssetAtPath<GameObject>(StoryWardSrc + name + ".fbx");
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

        [MenuItem("CatTang/스토리 맵 5 병실 키트 만들기")]
        public static void BuildStoryWardKit()
        {
            System.IO.Directory.CreateDirectory(StoryKitDir);
            SrcDir = StoryWardSrc;
            ModelPalette.Active = StoryWardPalette;
            foreach (var k in KeepWard) KeepOrigin.Add("Kit_H_" + k);
            foreach (var k in FlatWard) FlatFloor.Add("Kit_H_" + k);
            try
            {
                int made = 0;
                foreach (var n in KitWard)
                {
                    if (!Build("Kit_H_" + n, WardNativeSize("Kit_H_" + n), 180f, 'h', StoryKitDir, "H_" + n)) continue;
                    FixWardPrefab(StoryKitDir + "H_" + n + ".prefab", KeepWard.Contains(n));
                    made++;
                }
                NightShade();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"[StoryWard] 키트 {made}/{KitWard.Length}개");
            }
            finally
            {
                foreach (var k in KeepWard) KeepOrigin.Remove("Kit_H_" + k);
                foreach (var k in FlatWard) FlatFloor.Remove("Kit_H_" + k);
                SrcDir = Ch1Src;
                ModelPalette.Active = ModelPalette.Ch1;
            }
        }

        static void FixWardPrefab(string path, bool keep)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var mesh = root.transform.Find("Mesh");
                if (keep && mesh) mesh.localPosition = Vector3.zero;
                WardGlowMats(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static void WardGlowMats(GameObject inst)
        {
            Material Get(string file, Color c, float rim)
            {
                string p = StoryWardSrc + file + ".mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(p);
                if (!m) { m = new Material(Shader.Find("CatTang/Silhouette")); AssetDatabase.CreateAsset(m, p); }
                m.SetColor("_Color", c);
                if (m.HasProperty("_RimBoost")) m.SetFloat("_RimBoost", rim);
                EditorUtility.SetDirty(m);
                return m;
            }
            var lamp = Get("Glow_Lamp", new Color(1f, 0.78f, 0.44f, 1f), 0.5f);
            var moon = Get("Glow_Moon", new Color(1f, 0.96f, 0.82f, 1f), 0.4f);
            var screen = Get("Glow_Screen", new Color(0.35f, 1f, 0.58f, 1f), 0.4f);
            foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    var pick = SrcMatName(r, i) switch { "lamp_glow" => lamp, "moon_glow" => moon, "screen_glow" => screen, _ => null };
                    if (pick) { mats[i] = pick; changed = true; }
                }
                if (changed) r.sharedMaterials = mats;
            }
        }

        /// <summary>밤 병실 그늘: 차가운 청회 (달빛) — 따뜻한 빛은 협탁 스탠드만</summary>
        static void NightShade()
        {
            foreach (var guid in AssetDatabase.FindAssets("Kit_H_ t:Material", new[] { StoryWardSrc.TrimEnd('/') }))
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (!m || !m.name.Contains("_Toon") || !m.HasProperty("_ShadeColor")) continue;
                m.SetColor("_ShadeColor", new Color(0.84f, 0.87f, 0.93f));
                m.SetColor("_SecondShadeColor", new Color(0.64f, 0.68f, 0.78f));
                EditorUtility.SetDirty(m);
            }
        }

        [MenuItem("CatTang/스토리 맵 5 병실 맵 조립")]
        public static void BuildStoryWardMap()
        {
            System.IO.Directory.CreateDirectory("Assets/Resources/Story");
            var root = new GameObject("StoryWard").transform;
            try
            {
                var geo = new GameObject("Geo").transform; geo.SetParent(root, false);
                var marks = new GameObject("Marks").transform; marks.SetParent(root, false);
                var edges = new GameObject("Edges").transform; edges.SetParent(root, false);
                var lights = new GameObject("Lights").transform; lights.SetParent(root, false);

                Wd("Ground", geo, Vector3.zero, 0, 1f, "Ground");
                Wd("Room", geo, Vector3.zero, 0, 1f, "Room");
                var walk = new GameObject("WalkFloor").AddComponent<BoxCollider>();
                walk.transform.SetParent(root, false);
                walk.center = new Vector3(0, -0.5f, 0); walk.size = new Vector3(26, 1, 22);
                Wd("Window", geo, new Vector3(6.5f, 0, 9.7f), 0, 1f, "Window");
                // 남쪽 문틀은 카메라 앞을 가려서 뺌 (입구는 Room의 남벽 틈)

                // 침대 + 누운 노인 그림자 (매트리스 윗면 0.75)
                WsBlock(Wd("Bed", geo, WardBed, 0, Prop, "Bed"), 0.95f);
                Wd("OldManLying", geo, WardBed + Vector3.up * 0.75f * Prop, 0, Prop, "OldManLying");
                // 협탁 + 그림책
                var ns = Wd("Nightstand", geo, new Vector3(3.5f, 0, 7.4f), 0, Prop, "Nightstand");
                WsBlock(ns, 0.9f);
                if (ns) AddLight(ns.transform, "LampLight", LightType.Point, new Vector3(-0.15f, 1.1f, 0f), new Color(1f, 0.72f, 0.42f), 6f, 1.0f);   // 달빛이 주인공 — 스탠드는 침대 머리맡만 은은하게
                Wd("PictureBook", geo, new Vector3(3.35f, 0.72f * Prop, 7.05f), -12f, Prop, "PictureBook");
                // 링거 · 모니터
                WsBlock(Wd("IVStand", geo, WardIV, 0, Prop, "IVStand"), 0.6f);
                var mon = Wd("Monitor", geo, new Vector3(-4.4f, 0, 8.3f), Face(new Vector3(-4.4f, 0, 8.3f), new Vector3(0, 0, 4f)), Prop, "Monitor");
                WsBlock(mon, 0.8f);
                if (mon) AddLight(mon.transform, "MonitorLight", LightType.Point, new Vector3(0, 1.3f, -0.4f), new Color(0.4f, 1f, 0.6f), 3.5f, 0.55f);

                // 북벽 벽시계 (뒷면 = 벽, 바늘 축 = 앞 0.3)
                var wc = Wd("WallClock", geo, WardClock, 0, ClockScale, "WallClock");
                if (wc)
                {
                    var hh = Wd("HandHour", wc.transform, WardHandsAt, 0, 1f, "HourHand");
                    var mh = Wd("HandMinute", wc.transform, WardHandsAt + new Vector3(0, 0, -0.02f), 0, 1f, "MinuteHand");
                    var sh = Wd("HandSecond", wc.transform, WardHandsAt + new Vector3(0, 0, -0.04f), 0, 1f, "SecondHand");
                    SetHands(hh ? hh.transform : null, mh ? mh.transform : null, 11, 59);
                    if (sh) sh.transform.localRotation = Quaternion.Euler(0, 0, -40 * 6f);
                }

                // 방문객 의자(빨간 숄) · 수납장 · 가림 커튼
                var chairAt = new Vector3(4.6f, 0, 3.3f);
                WsBlock(Wd("VisitorChair", geo, chairAt, Face(chairAt, WardBed), Prop, "VisitorChair"), 0.8f);
                WsBlock(Wd("Cabinet", geo, new Vector3(-10.9f, 0, 1f), -90f, Prop, "Cabinet"), 0.95f);
                Wd("CurtainRail", geo, new Vector3(8.5f, 0, 2.2f), 0, 1.2f, "CurtainRail");

                // 사신 그림자 (구석에서 침대를 봄) — 머리는 목 피벗 아래
                var reaper = Wd("ReaperBody", geo, WardReaper, Face(WardReaper, WardBed), ReaperScale, "ReaperShadow");
                if (reaper)
                {
                    var meshOff = reaper.transform.Find("Mesh") ? reaper.transform.Find("Mesh").localPosition : Vector3.zero;
                    var pivot = new GameObject("HeadPivot").transform;
                    pivot.SetParent(reaper.transform, false);
                    pivot.localPosition = ReaperNeck + new Vector3(meshOff.x, 0, meshOff.z);
                    var head = Wd("ReaperHead", pivot, Vector3.zero, 0, 1f, "ReaperHead");
                    if (head) { head.transform.localPosition = Vector3.zero; head.transform.localRotation = Quaternion.identity; }
                    WsBlock(reaper, 0.7f);
                }

                // 빛: 창 달빛 · 방 전체 옅은 청색
                var moonL = AddLight(lights, "MoonLight", LightType.Spot, new Vector3(6.5f, 4.4f, 9.5f), new Color(0.62f, 0.74f, 1f), 20f, 6f);   // 다른 빛은 반쯤 줄이고 달빛은 조금만 → 상대적으로 달빛이 주도 (2026-11-26)
                moonL.spotAngle = 70f; moonL.innerSpotAngle = 30f; moonL.shadows = LightShadows.Soft; moonL.shadowStrength = 0.8f;
                moonL.transform.LookAt(root.TransformPoint(new Vector3(2f, 0, 2f)));
                moonL.transform.localRotation = Quaternion.Inverse(root.rotation) * moonL.transform.rotation;
                // 방 가운데 채움 빛은 뺌 (2026-11-26 사용자: 전방향 빛이 어색 — 실내는 창 달빛 한 방향 + 협탁 스탠드 · 모니터만)

                // 표지 (StoryWard가 이름으로 찾음)
                Mark(marks, "Spawn", new Vector3(0f, 0, -6.6f));
                Mark(marks, "BattleCenter", WardBattle);
                Mark(marks, "BedFront", WardBed + new Vector3(0, 0, -3.2f));
                Mark(marks, "ClockTarget", new Vector3(WardClock.x, 0, 7.2f));
                Mark(marks, "DripAt", WardIV + DripChamber * Prop);
                for (int i = 0; i < 6; i++)
                {
                    float a = (30f + i * 60f) * Mathf.Deg2Rad;
                    Mark(marks, "ShadowSpawn_" + (i + 1), WardBattle + new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * 6.5f, (30f + i * 60f) + 180f);
                }
                var glass = new[] { new Vector3(-4.5f, 0, -1f), new Vector3(4.5f, 0, -1.5f), new Vector3(-3f, 0, -7f), new Vector3(3.5f, 0, -7f), new Vector3(0f, 0, -4f), new Vector3(-6.5f, 0, -4.5f), new Vector3(6.5f, 0, -5f) };
                for (int i = 0; i < glass.Length; i++) Mark(marks, "HourglassSpot_" + (i + 1), glass[i]);

                // 가장자리: 방 네 벽
                Wall(edges, new Vector3(-12f, 0, -10f), new Vector3(-12f, 0, 10f));
                Wall(edges, new Vector3(-12f, 0, 10f), new Vector3(12f, 0, 10f));
                Wall(edges, new Vector3(12f, 0, 10f), new Vector3(12f, 0, -10f));
                Wall(edges, new Vector3(12f, 0, -10f), new Vector3(-12f, 0, -10f));

                PrefabUtility.SaveAsPrefabAsset(root.gameObject, StoryWardMapPath);
                Debug.Log("[StoryWard] 맵 " + StoryWardMapPath);
            }
            finally { Object.DestroyImmediate(root.gameObject); }
        }

        /// <summary>맵 미리보기 → Art/tools/specs/story_ward_top.png · _quarter.png</summary>
        [MenuItem("CatTang/스토리 맵 5 병실 미리보기 찍기")]
        public static void ShotStoryWard()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(StoryWardMapPath);
            if (!prefab) { Debug.LogWarning("[StoryWard] 맵 없음"); return; }
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
            try
            {
                PrefabUtility.InstantiatePrefab(prefab, scene);
                var sunGo = new GameObject("Sun");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(sunGo, scene);
                var sun = sunGo.AddComponent<Light>(); sun.type = LightType.Directional; sun.intensity = 0.7f; sun.color = new Color(0.78f, 0.84f, 1f);
                sunGo.transform.rotation = Quaternion.Euler(55f, -35f, 0f);
                var camGo = new GameObject("ShotCam");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(camGo, scene);
                var cam = camGo.AddComponent<Camera>();
                cam.scene = scene;
                cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.02f, 0.02f, 0.04f);
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
                cam.orthographic = true; cam.orthographicSize = 13f;
                camGo.transform.SetPositionAndRotation(new Vector3(0, 40f, 0.2f), Quaternion.Euler(90f, 0, 0));
                Shoot("story_ward_top.png");
                cam.orthographic = false; cam.fieldOfView = 50f;
                camGo.transform.position = new Vector3(0, 23f, -17f);
                camGo.transform.LookAt(new Vector3(0, 0, 1.5f));
                Shoot("story_ward_quarter.png");
                cam.targetTexture = null;
                Object.DestroyImmediate(rt);
                Debug.Log("[StoryWard] 미리보기 Art/tools/specs/story_ward_top.png · _quarter.png");
            }
            finally { UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
