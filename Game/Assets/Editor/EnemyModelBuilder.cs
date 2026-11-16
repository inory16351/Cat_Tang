// Tripo 적 모델(Art/Characters/Ch1/*) → 게임용 프리팹(Resources/Enemies/*.prefab)
// - 원본 텍스처를 그대로 쓰는 툰 재질(CatTang/Toon)로 바꿈
// - 키를 맞추고(목표 높이) 발바닥을 y = 0에, 몸 가운데를 x · z = 0에 둠
// - 정면이 +Z가 되도록 yaw 보정 (Tripo 원본 방향이 다르면 Yaw 표를 고침)
// EnemyFactory가 Resources.Load("Enemies/이름")으로 불러 "Model" 자식으로 붙임.
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CatTang.EditorTools
{
    public static class EnemyModelBuilder
    {
        const string SrcDir = "Assets/Art/Characters/Ch1/";
        const string OutDir = "Assets/Resources/Enemies/";

        const string KitDir = "Assets/Resources/Kit/Ch1/";

        // 원본 이름 · 목표 크기(m) · 정면 보정 yaw(도) · 크기 기준(높이 h / 가로 w) · 출력 폴더 · 출력 이름
        static readonly (string name, float size, float yaw, char fit, string outDir, string outName)[] Models =
        {
            ("Ch1_Outlaw", 1.75f, 0f, 'h', OutDir, "Ch1_Outlaw"),
            ("Ch1_Brute", 2.3f, 0f, 'h', OutDir, "Ch1_Brute"),
            ("Ch1_Bomber", 1.35f, 0f, 'h', OutDir, "Ch1_Bomber"),
            ("Ch1_Outlaw_Plus", 1.95f, 0f, 'h', OutDir, "Ch1_Outlaw_Plus"),
            ("Ch1_Brute_Plus", 2.6f, 0f, 'h', OutDir, "Ch1_Brute_Plus"),
            ("Ch1_Bomber_Plus", 1.5f, 0f, 'h', OutDir, "Ch1_Bomber_Plus"),
            ("Ch1_MidBoss", 3.0f, 0f, 'h', OutDir, "Ch1_MidBoss"),
            ("Ch1_Boss", 5.5f, 0f, 'h', OutDir, "Ch1_Boss"),
            // 맵 타일 · 소품 키트 (Kit.cs가 Resources/Kit/Ch1/이름으로 불러 조립)
            ("Kit_Wall", 4f, 0f, 'w', KitDir, "Wall"),
            ("Kit_Cliff", 4f, 0f, 'w', KitDir, "Cliff"),
            ("Kit_Floor", 4f, 0f, 'w', KitDir, "Floor"),
            ("Kit_ClockTower", 7f, 0f, 'h', KitDir, "ClockTower"),
            ("Kit_Keg", 1.1f, 0f, 'h', KitDir, "Keg"),
            ("Kit_Wagon", 1.9f, 0f, 'h', KitDir, "Wagon"),
            ("Kit_Crates", 1.6f, 0f, 'h', KitDir, "Crates"),
            ("Kit_Fence", 1.2f, 0f, 'h', KitDir, "Fence"),
            ("Kit_MeltClock", 2.8f, 0f, 'h', KitDir, "MeltClock"),
            ("Kit_BarCounter", 1.3f, 0f, 'h', KitDir, "BarCounter"),
        };

        [MenuItem("CatTang/1장 적 프리팹 만들기 (Tripo)")]
        public static void BuildAll()
        {
            Directory.CreateDirectory(OutDir);
            Directory.CreateDirectory(KitDir);
            int made = 0;
            foreach (var m in Models)
                if (Build(m.name, m.size, m.yaw, m.fit, m.outDir, m.outName)) made++;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[EnemyModelBuilder] 프리팹 {made}/{Models.Length}개");
        }

        public static bool Build(string name, float height, float yaw) => Build(name, height, yaw, 'h', OutDir, name);

        static GameObject FindSource(string name)
        {
            foreach (var ext in new[] { ".fbx", ".glb", ".gltf", ".obj", ".prefab" })
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(SrcDir + name + ext);
                if (go) return go;
            }
            // 생성기가 하위 폴더에 넣었을 수 있음
            foreach (var guid in AssetDatabase.FindAssets(name + " t:GameObject", new[] { SrcDir.TrimEnd('/') }))
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (go) return go;
            }
            return null;
        }

        public static bool Build(string name, float height, float yaw, char fit, string outDir, string outName)
        {
            var src = FindSource(name);
            if (!src) { Debug.Log($"[EnemyModelBuilder] {name} 원본 없음 ({SrcDir}) — 건너뜀"); return false; }

            var root = new GameObject(outName);
            var inst = (GameObject)Object.Instantiate(src);
            inst.name = "Mesh";
            inst.transform.SetParent(root.transform, false);
            inst.transform.localRotation = Quaternion.Euler(0, yaw, 0) * inst.transform.localRotation;
            foreach (var c in inst.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            foreach (var a in inst.GetComponentsInChildren<Animator>()) Object.DestroyImmediate(a);

            // 재질: 원본 텍스처 → 툰
            var cache = new Dictionary<Material, Material>();
            foreach (var r in inst.GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    var m = mats[i];
                    if (m == null) continue;
                    if (!cache.TryGetValue(m, out var toon)) cache[m] = toon = ToonFor(name, m, cache.Count);
                    mats[i] = toon;
                }
                r.sharedMaterials = mats;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }

            // 크기 · 위치 맞춤
            var b = Bounds(inst);
            float basis = fit == 'w' ? b.size.x : b.size.y;
            if (basis > 0.0001f)
            {
                float s = height / basis;
                inst.transform.localScale *= s;
                b = Bounds(inst);
                // 바닥 타일은 윗면을 y = 0.02에, 나머지는 발바닥을 y = 0에
                float y = outName == "Floor" ? b.max.y - 0.02f : b.min.y;
                inst.transform.localPosition -= new Vector3(b.center.x, y, b.center.z);
            }

            string path = outDir + outName + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            Debug.Log($"[EnemyModelBuilder] {path} (키 {height}m)");
            return true;
        }

        static Bounds Bounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        static Material ToonFor(string name, Material src, int index)
        {
            string p = SrcDir + name + "_Toon" + (index > 0 ? "_" + index : "") + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (!m) { m = new Material(Shader.Find("CatTang/Toon")); AssetDatabase.CreateAsset(m, p); }
            // Tripo 내보내기: 원본 색 텍스처를 <이름>_basecolor.jpg/png로 옆에 둠 (FBX 안 재질은 연결이 안 될 때가 많음)
            Texture tex = AssetDatabase.LoadAssetAtPath<Texture2D>(SrcDir + name + "_basecolor.jpg") ?? AssetDatabase.LoadAssetAtPath<Texture2D>(SrcDir + name + "_basecolor.png");
            if (!tex)
            foreach (var prop in new[] { "_BaseMap", "_MainTex", "baseColorTexture", "_BaseColorMap" })
                if (src.HasProperty(prop) && src.GetTexture(prop)) { tex = src.GetTexture(prop); break; }
            if (!tex) tex = src.mainTexture;
            m.SetTexture("_BaseMap", tex);
            m.SetColor("_BaseColor", Color.white);
            // 1장 팔레트: 황혼 보라 그림자 + 금빛 테두리
            m.SetColor("_ShadeColor", new Color(0.72f, 0.58f, 0.66f));
            m.SetColor("_SecondShadeColor", new Color(0.45f, 0.34f, 0.46f));
            if (m.HasProperty("_OutlineWidth")) m.SetFloat("_OutlineWidth", 2.5f);
            if (m.HasProperty("_RimStrength")) m.SetFloat("_RimStrength", 0.5f);
            EditorUtility.SetDirty(m);
            return m;
        }
    }
}
