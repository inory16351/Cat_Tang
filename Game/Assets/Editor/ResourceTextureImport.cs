// Resources 아래 UI · 아이콘 이미지: 원래 크기 유지(2의 거듭제곱으로 늘리지 않음), 밉맵 끔, 압축 끔.
// (HUD 이미지가 1024로 늘어나 탄피 위치가 어긋났던 문제 방지)
using UnityEditor;

namespace CatTang.EditorTools
{
    public class ResourceTextureImport : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').Contains("/Resources/")) return;
            var imp = (TextureImporter)assetImporter;
            imp.npotScale = TextureImporterNPOTScale.None;
            imp.mipmapEnabled = false;
            imp.wrapMode = UnityEngine.TextureWrapMode.Clamp;
            imp.alphaIsTransparency = true;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            // 로비 '그려지는' 순서 가면(reveal_*): 값 = 시각이라 색공간 변환 없이 (sRGB로 읽으면 순서가 휨)
            if (System.IO.Path.GetFileName(assetPath).StartsWith("reveal_")) imp.sRGBTexture = false;
        }
    }
}
