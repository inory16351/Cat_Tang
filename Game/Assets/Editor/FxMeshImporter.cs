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
            // 2026-11-18: 플레이 중에 돌리면 쓰이고 있는 메시를 제자리에서 덮어써 렌더러가 빈 메시를 그리다 Unity가 꺼짐(D3D12 DrawNullGeometry) → 플레이 중엔 막음
            if (EditorApplication.isPlaying) { Debug.LogWarning("[FxMeshImporter] 플레이 중에는 메시를 가져오지 않음 — 플레이를 멈춘 뒤 다시 실행"); return; }
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
