using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LegionRush.EditorTools
{
    // Importa a arte de interface (Assets/LegionRush/Art/UI) como Sprite 2D/UI com alpha, sem mipmaps,
    // compressão ASTC no Android e bordas 9-slice lidas de borders.json (gerado por tools/art/extract_ui_pack.py).
    public class LegionUIArtImporter : AssetPostprocessor
    {
        private const string Root = "Assets/LegionRush/Art/UI/";
        private const string BordersFile = "Assets/LegionRush/Art/UI/Resources/borders.json";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Root)) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.spritePixelsPerUnit = 100f;
            importer.maxTextureSize = 1024;
            importer.textureCompression = TextureImporterCompression.Compressed;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteBorder = BorderFor(assetPath);
            importer.SetTextureSettings(settings);

            var android = importer.GetPlatformTextureSettings("Android");
            android.overridden = true;
            android.format = TextureImporterFormat.ASTC_6x6;
            android.maxTextureSize = 1024;
            importer.SetPlatformTextureSettings(android);
        }

        private static Vector4 BorderFor(string path)
        {
            if (!File.Exists(BordersFile)) return Vector4.zero;
            string key = path.Substring("Assets/LegionRush/Art/UI/Resources/".Length);
            key = key.Substring(0, key.Length - Path.GetExtension(key).Length);
            Dictionary<string, int[]> table = LegionArt.ParseBorders(File.ReadAllText(BordersFile));
            return table.TryGetValue(key, out int[] b) && b.Length == 4 ? new Vector4(b[0], b[1], b[2], b[3]) : Vector4.zero;
        }

        [MenuItem("Legion Rush/UI/Reimport UI Art")]
        public static void Reimport()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/LegionRush/Art/UI" }))
                AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid), ImportAssetOptions.ForceUpdate);
        }
    }
}
