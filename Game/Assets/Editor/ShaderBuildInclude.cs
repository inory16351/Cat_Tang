// 빌드 전처리 (2026-11-19): 코드에서 Shader.Find("CatTang/...")로만 찾는 셰이더는 그걸 쓰는 재질이 빌드에 없으면 빠짐
// → 빌드에서 Shader.Find가 null → HudView.Build에서 예외 → GameUI.Awake가 중간에 멈춰 커서 · 레벨 업 창 등이 안 만들어졌음.
// 그래서 빌드할 때마다 CatTang 셰이더를 Graphics Settings의 '항상 포함'에 넣음.
// CatTang/Toon · MirrorFloor는 키워드 변형이 많아(전부 컴파일하면 빌드가 매우 느려짐) 빼고, 재질이 쓰고 있으니 원래 들어감.
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace CatTang.EditorTools
{
    public class ShaderBuildInclude : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        static readonly HashSet<string> Skip = new() { "CatTang/Toon", "CatTang/MirrorFloor" };
        static readonly string[] Extra = { "Sprites/Default" };

        public void OnPreprocessBuild(BuildReport report) => Apply();

        [MenuItem("Cat_Tang/Include Code Shaders In Build")]
        public static void Apply()
        {
            var want = new List<Shader>();
            foreach (var guid in AssetDatabase.FindAssets("t:Shader"))
            {
                var sh = AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath(guid));
                if (sh && sh.name.StartsWith("CatTang/") && !Skip.Contains(sh.name)) want.Add(sh);
            }
            foreach (var n in Extra) { var sh = Shader.Find(n); if (sh) want.Add(sh); }

            var gs = AssetDatabase.LoadAssetAtPath<GraphicsSettings>("ProjectSettings/GraphicsSettings.asset");
            var so = new SerializedObject(gs);
            var arr = so.FindProperty("m_AlwaysIncludedShaders");
            var have = new HashSet<Object>();
            for (int i = 0; i < arr.arraySize; i++) have.Add(arr.GetArrayElementAtIndex(i).objectReferenceValue);
            int added = 0;
            foreach (var sh in want)
            {
                if (have.Contains(sh)) continue;
                arr.InsertArrayElementAtIndex(arr.arraySize);
                arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = sh;
                added++;
            }
            if (added > 0)
            {
                so.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
            }
            Debug.Log($"[ShaderBuildInclude] 항상 포함 셰이더 +{added} (CatTang {want.Count})");
        }
    }
}
