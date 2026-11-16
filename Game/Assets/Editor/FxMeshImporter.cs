// 블렌더 VFX 메시(Art/FX/Meshes/fx_*.fbx, Art/tools/blender_fx.py) → 독립 메시 자산(Resources/FX/Meshes/fx_*.asset)
// - FBX 안 하위 메시는 경로로 참조하기 어려워서(프리팹 · MCP) 메시만 따로 뽑아 둠
// - 이미 있으면 내용만 덮어써서 GUID 유지 (프리팹 참조가 안 끊김)
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CatTang.EditorTools
{
    public static class FxMeshImporter
    {
        const string Src = "Assets/Art/FX/Meshes";
        const string Dst = "Assets/Resources/FX/Meshes";

        [MenuItem("CatTang/FX 메시 가져오기 (블렌더)")]
        public static void ImportAll()
        {
            Directory.CreateDirectory(Dst);
            int n = 0;
            foreach (var path in Directory.GetFiles(Src, "*.fbx"))
            {
                var p = path.Replace("\\", "/");
                var src = AssetDatabase.LoadAssetAtPath<Mesh>(p);
                if (!src) continue;
                string outPath = Dst + "/" + Path.GetFileNameWithoutExtension(p) + ".asset";
                var dst = AssetDatabase.LoadAssetAtPath<Mesh>(outPath);
                var copy = Object.Instantiate(src);
                copy.name = Path.GetFileNameWithoutExtension(p);
                if (dst) { EditorUtility.CopySerialized(copy, dst); Object.DestroyImmediate(copy); }
                else AssetDatabase.CreateAsset(copy, outPath);
                n++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[FxMeshImporter] 메시 {n}개 → {Dst}");
        }
    }
}
