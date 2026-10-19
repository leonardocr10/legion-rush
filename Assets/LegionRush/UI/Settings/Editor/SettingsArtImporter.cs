using UnityEditor;
using UnityEngine;

namespace LegionRush.EditorTools
{
    // Ícones da tela de configurações (Resources/SettingsArt): Sprite único, sem mipmaps.
    public class SettingsArtImporter : AssetPostprocessor
    {
        private const string Folder = "Assets/LegionRush/UI/Settings/Resources/SettingsArt/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Folder)) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
        }
    }
}
