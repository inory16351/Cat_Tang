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
        }
    }
}
