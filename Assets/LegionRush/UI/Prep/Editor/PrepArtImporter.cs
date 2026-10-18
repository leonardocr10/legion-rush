using UnityEditor;
using UnityEngine;

namespace LegionRush.EditorTools
{
    // Arte da tela Preparar Tropas (Resources/PrepArt): Sprite único sem mipmaps; o painel ganha bordas 9-slice.
    public class PrepArtImporter : AssetPostprocessor
    {
        private const string Folder = "Assets/LegionRush/UI/Prep/Resources/PrepArt/";

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
            if (assetPath.EndsWith("Panel_Frame.png")) importer.spriteBorder = new Vector4(34f, 34f, 34f, 34f);
        }
    }
}
