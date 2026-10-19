using UnityEditor;
using UnityEngine;

namespace TopDownRacing.EditorTools
{
    /// <summary>
    /// Автоматически настраивает импорт всей графики из Assets/Art:
    /// спрайты, 64 пикселя на юнит, без мипмапов.
    /// </summary>
    public class ArtImportPostprocessor : AssetPostprocessor
    {
        private const string ArtRoot = "Assets/Art/";
        private const float PixelsPerUnit = 64f;

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArtRoot, System.StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            TextureImporter importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 1024;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.spritePivot = Vector2.one * 0.5f;
        }
    }
}
