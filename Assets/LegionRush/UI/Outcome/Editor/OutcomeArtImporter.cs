using UnityEditor;

namespace LegionRush.EditorTools
{
    // Arte da tela de derrota (Resources/DefeatArt): importa como Sprite único, sem mipmaps e sem esticar para potência de 2.
    public class DefeatArtImporter : AssetPostprocessor
    {
        private const string Folder = "Assets/LegionRush/UI/Defeat/Resources/DefeatArt/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Folder)) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
        }
    }
}
