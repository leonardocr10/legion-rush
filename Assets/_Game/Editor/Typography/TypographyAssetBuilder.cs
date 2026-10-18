using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace LegionRush.EditorTools
{
    // Gera os Font Assets SDF (TextMeshPro), os materiais por estilo, os presets e o tema tipográfico a partir das
    // fontes em Assets/Fonts/Source. Idempotente: não recria o que já existe (mantém GUIDs e ajustes do Inspector).
    // Batch/CI: -executeMethod LegionRush.EditorTools.TypographyAssetBuilder.GenerateBatch
    public static class TypographyAssetBuilder
    {
        private const string SourceDir = "Assets/Fonts/Source";
        private const string TmpDir = "Assets/Fonts/TMP";
        private const string MaterialDir = "Assets/Fonts/Materials";
        private const string PresetDir = "Assets/Fonts/Presets";
        private const string ThemeDir = "Assets/_Game/Resources/Typography";
        private const string ThemePath = ThemeDir + "/LegionTypographyTheme.asset";
        private const string MobileShader = "TextMeshPro/Mobile/Distance Field";

        // Caracteres pré-gerados no atlas (português + inglês + símbolos do jogo). Outros entram dinamicamente.
        private const string Charset =
            " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~" +
            "ÀÁÂÃÄÇÈÉÊËÌÍÎÏÑÒÓÔÕÖÙÚÛÜÝàáâãäçèéêëìíîïñòóôõöùúûüýÿ" +
            "×–—‘’“”•…€·°ºª¡¿«»";

        private struct FontSpec
        {
            public string Source;
            public string Name;
            public FontSpec(string source, string name)
            {
                Source = source;
                Name = name;
            }
        }

        private static readonly FontSpec Title = new FontSpec("LilitaOne-Regular.ttf", "LilitaOne SDF");
        private static readonly FontSpec Menu = new FontSpec("Fredoka/Fredoka-Bold.ttf", "Fredoka Bold SDF");
        private static readonly FontSpec Body = new FontSpec("Fredoka/Fredoka-Medium.ttf", "Fredoka Medium SDF");
        private static readonly FontSpec Numbers = new FontSpec("Baloo2/Baloo2-ExtraBold.ttf", "Baloo2 ExtraBold SDF");

        [MenuItem("Legion Rush/Typography/Generate Font Assets & Theme")]
        public static void Generate()
        {
            EnsureTmpEssentials();
            EnsureFolder(TmpDir);
            EnsureFolder(MaterialDir);
            EnsureFolder(PresetDir);
            EnsureFolder(ThemeDir);

            TMP_FontAsset title = FontAsset(Title);
            TMP_FontAsset menu = FontAsset(Menu);
            TMP_FontAsset body = FontAsset(Body);
            TMP_FontAsset numbers = FontAsset(Numbers);
            Fallbacks(title, menu, numbers);
            Fallbacks(menu, numbers, title);
            Fallbacks(body, menu, numbers);
            Fallbacks(numbers, menu, title);
            EnsureTmpSettings(menu);

            var theme = AssetDatabase.LoadAssetAtPath<LegionTypographyTheme>(ThemePath);
            if (theme == null)
            {
                theme = ScriptableObject.CreateInstance<LegionTypographyTheme>();
                theme.styles = LegionTypographyTheme.DefaultStyles();
                AssetDatabase.CreateAsset(theme, ThemePath);
            }
            theme.titleFont = title;
            theme.menuFont = menu;
            theme.bodyFont = body;
            theme.numbersFont = numbers;
            theme.titleSource = LoadFont(Title);
            theme.menuSource = LoadFont(Menu);
            theme.numbersSource = LoadFont(Numbers);

            foreach (LegionStyleSpec spec in theme.styles)
            {
                TMP_FontAsset font = theme.Font(spec.font);
                if (font == null) continue;
                spec.materialPreset = StyleMaterial(font, spec);
                StylePreset(font, spec);
            }
            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();
            LegionTypographyTheme.NotifyChanged();
            Debug.Log("[LegionRush] Tipografia gerada: " + ThemePath);
        }

        public static void GenerateBatch()
        {
            try
            {
                Generate();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        // Chamado antes dos builds: gera somente se o tema ou as fontes estiverem faltando.
        public static void EnsureAssets()
        {
            var theme = AssetDatabase.LoadAssetAtPath<LegionTypographyTheme>(ThemePath);
            if (theme != null && theme.titleFont != null && theme.menuFont != null && theme.numbersFont != null) return;
            Generate();
        }

        private const string SettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";

        // TMP Settings em Resources (o TMP lê "TMP Settings" de qualquer pasta Resources). Fonte padrão = Fredoka.
        private static void EnsureTmpSettings(TMP_FontAsset defaultFont)
        {
            var settings = Resources.Load<TMP_Settings>("TMP Settings");
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<TMP_Settings>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }
            var serialized = new SerializedObject(settings);
            SerializedProperty font = serialized.FindProperty("m_defaultFontAsset");
            if (font != null && font.objectReferenceValue == null && defaultFont != null)
            {
                font.objectReferenceValue = defaultFont;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorUtility.SetDirty(settings);
        }

        private static void EnsureTmpEssentials()
        {
            EnsureFolder("Assets/_Game/Resources");
            if (Resources.Load<TMP_Settings>("TMP Settings") == null && AssetDatabase.LoadAssetAtPath<TMP_Settings>(SettingsPath) == null)
            {
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<TMP_Settings>(), SettingsPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }
            if (Shader.Find(MobileShader) != null) return;
            string package = Path.GetFullPath("Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage");
            if (!File.Exists(package)) throw new FileNotFoundException("TMP Essential Resources não encontrado", package);
            AssetDatabase.ImportPackage(package, false);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static Font LoadFont(FontSpec spec)
        {
            string path = SourceDir + "/" + spec.Source;
            var importer = AssetImporter.GetAtPath(path) as TrueTypeFontImporter;
            if (importer != null && (!importer.includeFontData || importer.fontTextureCase != FontTextureCase.Dynamic))
            {
                importer.includeFontData = true;
                importer.fontTextureCase = FontTextureCase.Dynamic;
                importer.SaveAndReimport();
            }
            Font font = AssetDatabase.LoadAssetAtPath<Font>(path);
            if (font == null) throw new FileNotFoundException("Fonte não encontrada (rode tools/fonts/build_fonts.py)", path);
            return font;
        }

        private static TMP_FontAsset FontAsset(FontSpec spec)
        {
            string path = TmpDir + "/" + spec.Name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (existing != null) return existing;

            Font font = LoadFont(spec);
            // SDF 90pt / padding 9 em atlas 1024: nítido em telas de celular; modo dinâmico cobre caracteres extras.
            TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024,
                AtlasPopulationMode.Dynamic, true);
            asset.name = spec.Name;
            asset.TryAddCharacters(Charset, out string missing);
            if (!string.IsNullOrEmpty(missing)) Debug.Log($"[LegionRush] {spec.Name}: sem glifo para '{missing}' (usa fallback)");

            Shader shader = Shader.Find(MobileShader);
            if (shader != null) asset.material.shader = shader;
            AssetDatabase.CreateAsset(asset, path);
            for (int i = 0; i < asset.atlasTextures.Length; i++)
            {
                Texture2D atlas = asset.atlasTextures[i];
                if (atlas == null || AssetDatabase.Contains(atlas)) continue;
                atlas.name = spec.Name + " Atlas" + (i > 0 ? " " + i : string.Empty);
                AssetDatabase.AddObjectToAsset(atlas, asset);
            }
            asset.material.name = spec.Name + " Material";
            if (!AssetDatabase.Contains(asset.material)) AssetDatabase.AddObjectToAsset(asset.material, asset);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return asset;
        }

        private static void Fallbacks(TMP_FontAsset font, params TMP_FontAsset[] fallbacks)
        {
            font.fallbackFontAssetTable ??= new System.Collections.Generic.List<TMP_FontAsset>();
            foreach (TMP_FontAsset fallback in fallbacks)
            {
                if (fallback != null && fallback != font && !font.fallbackFontAssetTable.Contains(fallback))
                    font.fallbackFontAssetTable.Add(fallback);
            }
            EditorUtility.SetDirty(font);
        }

        // Material de estilo nomeado "<Fonte> - <Estilo>" (convenção dos presets de material do TMP).
        private static Material StyleMaterial(TMP_FontAsset font, LegionStyleSpec spec)
        {
            string path = $"{MaterialDir}/{font.name} - Legion {spec.style}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(font.material);
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = font.material.shader;
                material.CopyPropertiesFromMaterial(font.material);
            }
            LegionTypography.Configure(material, spec);
            EditorUtility.SetDirty(material);
            return material;
        }

        // Preset de componente TextMeshProUGUI por estilo (Assets/Fonts/Presets), para textos montados no Inspector.
        private static void StylePreset(TMP_FontAsset font, LegionStyleSpec spec)
        {
            var go = new GameObject("PresetSource") { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
                text.font = font;
                text.fontSharedMaterial = spec.materialPreset;
                text.fontStyle = spec.fontStyle;
                text.characterSpacing = spec.characterSpacing;
                text.enableVertexGradient = spec.useGradient;
                text.colorGradient = new VertexGradient(spec.gradientTop, spec.gradientTop, spec.gradientBottom, spec.gradientBottom);
                text.color = spec.useGradient ? Color.white : spec.color;
                text.textWrappingMode = TextWrappingModes.Normal;
                text.enableAutoSizing = true;
                text.fontSizeMin = 18;
                text.fontSizeMax = 72;
                string path = $"{PresetDir}/Legion {spec.style}.preset";
                var existing = AssetDatabase.LoadAssetAtPath<Preset>(path);
                if (existing != null) existing.UpdateProperties(text);
                else AssetDatabase.CreateAsset(new Preset(text), path);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
