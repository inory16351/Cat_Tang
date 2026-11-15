// 총알 셰이더 비교 씬을 만들고 스크린샷을 찍는다.
// 배치 실행: Unity -batchmode -projectPath Game -executeMethod CatTang.EditorTools.ShaderTestBuilder.BuildAndRender -outDir <폴더> -quit
// 에디터에서: 메뉴 Cat_Tang > Shader Test > Build Bullet Scene
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace CatTang.EditorTools
{
    public static class ShaderTestBuilder
    {
        const string ScenePath = "Assets/Scenes/ShaderTest_Bullets.unity";
        const string MatDir = "Assets/Art/Materials/ShaderTest";
        const string ModelPath = "Assets/Art/Models/TestBullet.fbx";

        // 열: 비교 단계
        static readonly string[] Variants = { "URP Lit", "Toon", "Toon+Outline", "Toon+Outline+Ink", "Full" };

        // 줄: 아이콘 시안의 배색 (탄피 · 탄두 · 링)
        static readonly Color[][] Colorways =
        {
            new[] { new Color(0.36f, 0.62f, 0.58f), new Color(0.93f, 0.86f, 0.72f), new Color(0.86f, 0.66f, 0.32f) }, // 티 타임
            new[] { new Color(0.70f, 0.20f, 0.24f), new Color(0.80f, 0.29f, 0.31f), new Color(0.86f, 0.66f, 0.32f) }, // 쌍둥이 초침
            new[] { new Color(0.52f, 0.44f, 0.70f), new Color(0.66f, 0.56f, 0.82f), new Color(0.86f, 0.66f, 0.32f) }, // 모래시계
        };

        [MenuItem("Cat_Tang/Shader Test/Build Bullet Scene")]
        public static void BuildMenu() => Build();

        public static void BuildAndRender()
        {
            string outDir = "ShaderTestOut";
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == "-outDir") outDir = args[i + 1];
            BuildAndRenderTo(outDir);
        }

        public static void BuildAndRenderTo(string outDir)
        {
            Directory.CreateDirectory(outDir);
            var cams = Build();
            Render(cams.overview, Path.Combine(outDir, "shader_overview.png"), 1920, 1080);
            Render(cams.close, Path.Combine(outDir, "shader_closeup.png"), 1920, 1080);
            Debug.Log("[ShaderTest] rendered to " + outDir);
        }

        static (Camera overview, Camera close) Build()
        {
            Directory.CreateDirectory("Assets/Scenes");
            Directory.CreateDirectory(MatDir);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var toon = Shader.Find("CatTang/Toon");
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            if (toon == null || lit == null) throw new System.Exception("shader not found: toon=" + toon + " lit=" + lit);

            // 빛
            var lightGo = new GameObject("Sun");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.3f;
            light.color = new Color(1f, 0.95f, 0.88f);
            light.shadows = LightShadows.Soft; // 셰이더가 경계를 다시 딱 끊음
            light.shadowBias = 0.08f;
            light.shadowNormalBias = 0.8f;
            // 인벤토리 보기: 왼쪽 위 앞에서 비추는 빛
            lightGo.transform.rotation = Quaternion.Euler(35f, 30f, 0f);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.32f, 0.28f, 0.42f);

            // 총알 격자 (인벤토리처럼 세워서, 아이콘 시안과 같은 기울기)
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (model == null) throw new System.Exception("model not found " + ModelPath);
            var root = new GameObject("Bullets");
            for (int row = 0; row < Colorways.Length; row++)
            {
                for (int col = 0; col < Variants.Length; col++)
                {
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(model);
                    go.name = $"Bullet_{row}_{Variants[col]}";
                    go.transform.SetParent(root.transform);
                    go.transform.position = new Vector3((col - 2) * 1.6f - 0.25f, (1 - row) * 2.2f - 0.8f, 0f);
                    // FBX 가져오기 때 붙는 축 보정 회전(-90, 0, 0)을 지키고 그 위에 기울기를 더한다
                    go.transform.rotation = Quaternion.Euler(0f, 25f, -24f) * model.transform.rotation;
                    go.transform.localScale = Vector3.one * 1.6f;
                    var mats = new Material[3];
                    string[] part = { "Case", "Head", "Ring" };
                    for (int p = 0; p < 3; p++)
                    {
                        string n = $"R{row}_{part[p]}_{col}";
                        mats[p] = col == 0
                            ? MakeLit(lit, n, Colorways[row][p])
                            : MakeToon(toon, n, Colorways[row][p], col >= 2 ? 5.0f : 0f, col >= 3, col >= 4, col >= 4);
                    }
                    foreach (var r in go.GetComponentsInChildren<Renderer>())
                    {
                        var arr = new Material[r.sharedMaterials.Length];
                        for (int k = 0; k < arr.Length; k++) arr[k] = mats[Mathf.Min(k, 2)];
                        r.sharedMaterials = arr;
                    }
                }
            }

            // 카메라 2대: 전체 · 확대
            // 인벤토리처럼 정면 직교 카메라
            var overview = MakeCamera("Cam_Inventory", new Vector3(0f, 0.2f, -10f), new Vector3(0f, 0.2f, 0f), 30f);
            overview.orthographic = true;
            overview.orthographicSize = 3.6f;
            var close = MakeCamera("Cam_Closeup", new Vector3(0.55f, 2.15f, -10f), new Vector3(0.55f, 2.15f, 0f), 30f);
            close.orthographic = true;
            close.orthographicSize = 1.6f;
            close.enabled = false;

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            return (overview, close);
        }

        static Camera MakeCamera(string name, Vector3 pos, Vector3 target, float fov)
        {
            var go = new GameObject(name);
            go.transform.position = pos;
            go.transform.LookAt(target);
            var cam = go.AddComponent<Camera>();
            cam.fieldOfView = fov;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.106f, 0.09f, 0.149f);
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 60f;
            return cam;
        }

        static Material MakeLit(Shader lit, string name, Color c)
        {
            var m = new Material(lit) { name = name };
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", 0.35f);
            return Save(m);
        }

        static Material MakeToon(Shader toon, string name, Color c, float outline, bool ink, bool hatch, bool rim)
        {
            var m = new Material(toon) { name = name };
            m.SetColor("_BaseColor", c);
            m.SetColor("_ShadeColor", new Color(0.62f, 0.55f, 0.74f));
            m.SetColor("_SecondShadeColor", new Color(0.38f, 0.32f, 0.52f));
            m.SetFloat("_OutlineWidth", outline);
            m.SetFloat("_RimStrength", rim ? 0.8f : 0f);
            m.SetFloat("_InkWidth", ink ? 0.05f : 0f);
            m.SetFloat("_InkStrength", ink ? 0.85f : 0f);
            m.SetFloat("_HatchStrength", hatch ? 0.5f : 0f);
            m.SetFloat("_HatchDensity", 12f);
            m.SetFloat("_HatchWidth", 0.32f);
            return Save(m);
        }

        static Material Save(Material m)
        {
            string path = $"{MatDir}/{m.name}.mat";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        static void Render(Camera cam, string file, int w, int h)
        {
            // 에디터의 비동기 셰이더 컴파일 중엔 물체가 안 보이게 그려지므로, 렌더하는 동안만 끈다
            bool prevAsync = ShaderUtil.allowAsyncCompilation;
            ShaderUtil.allowAsyncCompilation = false;
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            var prevActive = RenderTexture.active;
            bool prevEnabled = cam.enabled;
            cam.enabled = true;
            cam.targetTexture = rt;
            cam.Render(); // 첫 렌더는 준비용 (에디터에서 첫 장이 검게 나오는 문제)
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            File.WriteAllBytes(file, tex.EncodeToPNG());
            cam.targetTexture = null;
            cam.enabled = prevEnabled;
            RenderTexture.active = prevActive;
            Object.DestroyImmediate(tex);
            rt.Release();
            ShaderUtil.allowAsyncCompilation = prevAsync;
        }
    }
}
