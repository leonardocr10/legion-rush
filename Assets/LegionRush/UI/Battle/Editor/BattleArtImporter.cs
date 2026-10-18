using UnityEditor;
using UnityEngine;

namespace LegionRush.EditorTools
{
    // Arte do HUD de batalha e das placas dos portais (Resources/BattleArt): Sprite único, sem mipmaps.
    public class BattleArtImporter : AssetPostprocessor
    {
        private const string Folder = "Assets/LegionRush/UI/Battle/Resources/BattleArt/";

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
