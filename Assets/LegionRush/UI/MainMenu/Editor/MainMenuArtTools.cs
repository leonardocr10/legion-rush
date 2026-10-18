using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace LegionRush.EditorTools
{
    // Arte da tela inicial (Assets/LegionRush/UI/MainMenu, gerada por tools/art/extract_main_menu.py):
    //  - importa os PNGs como Sprite para UI (alpha, sem mipmaps, 9-slice de MainMenuBorders.json, ASTC no Android);
    //  - mantém o catálogo Resources/MainMenuArt.asset e o Sprite Atlas dos ícones, botões e painéis;
    //  - gera os prefabs dos componentes e a cena de prévia;
    //  - captura a tela em PNG em várias proporções (também em lote, para comparar com a referência).
    // Lote: Unity.exe -batchmode -projectPath . -executeMethod LegionRush.EditorTools.MainMenuArtTools.BuildAllBatch -captureDir <pasta>
    public class MainMenuArtTools : AssetPostprocessor
    {
        private const string Root = "Assets/LegionRush/UI/MainMenu/";
        private const string BordersFile = Root + "Resources/MainMenuBorders.json";
        private const string CatalogPath = Root + "Resources/MainMenuArt.asset";
        private const string AtlasPath = Root + "Sprites/MainMenu.spriteatlasv2";
        private const string PrefabDir = Root + "Prefabs";
        private const string ScenePath = Root + "Scenes/MainMenuPreview.unity";
        private static readonly string[] ArtFolders = { "Icons", "Buttons", "Panels", "Sprites", "Backgrounds", "Characters" };
        private static readonly string[] AtlasFolders = { "Icons", "Buttons", "Panels" };
        private static bool rebuildQueued;

        // ---------------------------------------------------------------- importação

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Root) || !assetPath.EndsWith(".png")) return;
            var importer = (TextureImporter)assetImporter;
            bool large = assetPath.Contains("/Backgrounds/") || assetPath.Contains("/Characters/");
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.spritePixelsPerUnit = 100f;
            importer.maxTextureSize = large ? 2048 : 1024;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.npotScale = TextureImporterNPOTScale.None;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteBorder = BorderFor(Path.GetFileNameWithoutExtension(assetPath));
            importer.SetTextureSettings(settings);

            TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings("Android");
            android.overridden = true;
            android.format = large ? TextureImporterFormat.ASTC_8x8 : TextureImporterFormat.ASTC_6x6;
            android.maxTextureSize = importer.maxTextureSize;
            importer.SetPlatformTextureSettings(android);
        }

        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            bool touched = imported.Concat(deleted).Concat(moved).Any(p => p.StartsWith(Root) && p.EndsWith(".png"));
            if (!touched || rebuildQueued || Application.isBatchMode) return;
            rebuildQueued = true;
            EditorApplication.delayCall += () =>
            {
                rebuildQueued = false;
                RebuildCatalog();
            };
        }

        private static Vector4 BorderFor(string name)
        {
            if (!File.Exists(BordersFile)) return Vector4.zero;
            Dictionary<string, int[]> table = LegionArt.ParseBorders(File.ReadAllText(BordersFile));
            return table.TryGetValue(name, out int[] b) && b.Length == 4 ? new Vector4(b[0], b[1], b[2], b[3]) : Vector4.zero;
        }

        // ---------------------------------------------------------------- catálogo e atlas

        [MenuItem("Legion Rush/UI/Tela Inicial/Atualizar arte (catálogo e atlas)")]
        public static void RebuildCatalog()
        {
            var sprites = new List<Sprite>();
            foreach (string folder in ArtFolders)
            {
                string path = Root + folder;
                if (!AssetDatabase.IsValidFolder(path)) continue;
                foreach (string guid in AssetDatabase.FindAssets("t:Sprite", new[] { path }))
                {
                    Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guid));
                    if (sprite != null) sprites.Add(sprite);
                }
            }
            sprites.Sort((a, b) => string.CompareOrdinal(a.name, b.name));

            var catalog = AssetDatabase.LoadAssetAtPath<MainMenuArtCatalog>(CatalogPath);
            if (catalog == null)
            {
                EnsureFolder(Root + "Resources");
                catalog = ScriptableObject.CreateInstance<MainMenuArtCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }
            catalog.sprites = sprites.ToArray();
            EditorUtility.SetDirty(catalog);
            BuildAtlas();
            AssetDatabase.SaveAssets();
            Debug.Log($"[MainMenu] Catálogo com {sprites.Count} sprites; atlas em {AtlasPath}.");
        }

        private static void BuildAtlas()
        {
            if (EditorSettings.spritePackerMode != SpritePackerMode.SpriteAtlasV2)
                EditorSettings.spritePackerMode = SpritePackerMode.SpriteAtlasV2;
            var folders = AtlasFolders.Select(f => AssetDatabase.LoadAssetAtPath<DefaultAsset>(Root + f)).Where(f => f != null).ToArray();
            if (folders.Length == 0) return;
            bool existed = File.Exists(AtlasPath);
            var atlas = existed ? SpriteAtlasAsset.Load(AtlasPath) : new SpriteAtlasAsset();
            if (!existed) atlas.Add(folders); // pastas inteiras: sprites novos entram sozinhos
            SpriteAtlasAsset.Save(atlas, AtlasPath);
            AssetDatabase.ImportAsset(AtlasPath);
            if (AssetImporter.GetAtPath(AtlasPath) is SpriteAtlasImporter importer)
            {
                importer.includeInBuild = true;
                importer.packingSettings = new SpriteAtlasPackingSettings
                {
                    enableRotation = false, enableTightPacking = false, enableAlphaDilation = true, padding = 4
                };
                importer.textureSettings = new SpriteAtlasTextureSettings
                {
                    generateMipMaps = false, filterMode = FilterMode.Bilinear, sRGB = true, readable = false
                };
                importer.SetPlatformSettings(new TextureImporterPlatformSettings
                {
                    name = "Android", overridden = true, format = TextureImporterFormat.ASTC_6x6, maxTextureSize = 2048,
                    textureCompression = TextureImporterCompression.Compressed
                });
                importer.SaveAndReimport();
            }
        }

        // ---------------------------------------------------------------- prefabs e cena de prévia

        [MenuItem("Legion Rush/UI/Tela Inicial/Gerar prefabs e cena de prévia")]
        public static void BuildPrefabsAndScene()
        {
            EnsureFolder(PrefabDir);
            var holder = new GameObject("PrefabHolder", typeof(RectTransform), typeof(Canvas));
            Transform parent = holder.transform;
            try
            {
                Save(MainMenuSideButton.Create(parent, "Icon_Store", "LOJA", null).gameObject, "MainMenu_SideButton", 150, 136);
                Save(MainMenuSideButton.Create(parent, "Icon_Rewards", "RECOMPENSAS", null, MainMenuSideButton.Kind.LabeledWithTimer).gameObject,
                    "MainMenu_SideButtonTimer", 152, 197);
                Save(MainMenuNavButton.Create(parent, "Icon_Home", "INÍCIO", null).gameObject, "MainMenu_NavButton", 204, 190);
                Save(MainMenuBadge.Create(parent, Vector2.zero).gameObject, "MainMenu_NotificationBadge", 41, 41);
                Save(MainMenuCurrencyBar.Create(parent, "Icon_Coin", null, "CurrencyBar").gameObject, "MainMenu_CurrencyBarGold", 260, 71);
                Save(MainMenuCurrencyBar.Create(parent, "Icon_Gem", null, "CurrencyBar").gameObject, "MainMenu_CurrencyBarGems", 245, 71);
                Save(MainMenuProfilePanel.Create(parent, null).gameObject, "MainMenu_ProfilePanel", 436, 124);
                Save(MainMenuProgressBar.Create(parent, "Bar_Track", "Bar_FillBlue").gameObject, "MainMenu_ProgressBar", 338, 37);
                Save(MainMenuWorldPanel.Create(parent, null, null).gameObject, "MainMenu_WorldPanel", 771, 197);
                Save(MainMenuCountdown.Create(parent).gameObject, "MainMenu_Countdown", 130, 34);
                Save(MainMenuModal.Create(parent, "TÍTULO").gameObject, "MainMenu_Modal", 1080, 1920);
                Image play = MainMenuArt.Image(parent, "Button_Play", "PlayButton");
                MainMenuButtons.Make(play.rectTransform, play, null);
                UIShineSweep.Attach(play.rectTransform);
                Save(play.gameObject, "MainMenu_PlayButton", 647, 197);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(holder);
            }
            BuildPreviewScene();
            AssetDatabase.SaveAssets();
            Debug.Log("[MainMenu] Prefabs em " + PrefabDir + " e cena " + ScenePath);
        }

        private static void Save(GameObject go, string name, float width, float height)
        {
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = Vector2.zero;
            go.SetActive(true);
            PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabDir}/{name}.prefab");
        }

        private static void BuildPreviewScene()
        {
            EnsureFolder(Root + "Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            Camera cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.05f, 0.14f);
            Canvas canvas = CreateCanvas(null);
            var events = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem));
#if ENABLE_INPUT_SYSTEM
            events.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            events.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
            RectTransform screen = UI.Rect(canvas.transform, "MainMenu");
            screen.gameObject.AddComponent<MainMenuPreview>().target = screen;
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static Canvas CreateCanvas(Camera camera)
        {
            var go = new GameObject("Canvas", typeof(RectTransform));
            go.layer = 5;
            Canvas canvas = go.AddComponent<Canvas>();
            if (camera != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 10f;
            }
            else
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }
            CanvasScaler scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        // ---------------------------------------------------------------- captura

        [MenuItem("Legion Rush/UI/Tela Inicial/Capturar prévias (PNG)")]
        public static void CaptureMenu()
        {
            string dir = Path.GetFullPath("Builds/MainMenuPreview");
            Capture(dir);
            EditorUtility.RevealInFinder(dir);
        }

        public static void BuildAllBatch()
        {
            int code = 0;
            try
            {
                AssetDatabase.Refresh();
                RebuildCatalog();
                BuildPrefabsAndScene();
                Capture(GetArgument("-captureDir") ?? Path.GetFullPath("Builds/MainMenuPreview"));
            }
            catch (Exception exception)
            {
                Debug.LogError("[MainMenu] Falhou: " + exception);
                code = 1;
            }
            EditorApplication.Exit(code);
        }

        public static void CaptureBatch()
        {
            int code = 0;
            try
            {
                Capture(GetArgument("-captureDir") ?? Path.GetFullPath("Builds/MainMenuPreview"));
            }
            catch (Exception exception)
            {
                Debug.LogError("[MainMenu] Captura falhou: " + exception);
                code = 1;
            }
            EditorApplication.Exit(code);
        }

        // Proporções: referência (941×1672 ≈ 9:16), celular alto 20:9, tablet 3:4 e dobrável aberto.
        private static readonly (string name, int w, int h)[] Sizes =
        {
            ("ref_1080x1920", 1080, 1920), ("phone_1080x2400", 1080, 2400), ("tablet_1536x2048", 1536, 2048), ("fold_1840x2208", 1840, 2208)
        };

        private static void Capture(string dir)
        {
            Directory.CreateDirectory(dir);
            // Com Sprite Atlas V2 ativo, o editor desenha os sprites a partir do atlas: empacota antes de renderizar.
            SpriteAtlasUtility.PackAllAtlases(EditorUserBuildSettings.activeBuildTarget, false);
            foreach (string probe in new[] { "Icon_Coin", "Background_Sky", "Button_Play" })
            {
                Sprite sprite = MainMenuArt.Get(probe);
                Texture2D tex = sprite != null ? sprite.texture : null;
                Debug.Log($"[MainMenu] {probe}: sprite={(sprite != null)} textura={(tex != null ? tex.name + " " + tex.width + "x" + tex.height : "nula")} packed={(sprite != null && sprite.packed)}");
            }
            foreach (var size in Sizes)
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var camGo = new GameObject("Camera");
                Camera cam = camGo.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.02f, 0.05f, 0.14f);
                cam.orthographic = true;
                var target = new RenderTexture(size.w, size.h, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
                cam.targetTexture = target;
                Canvas canvas = CreateCanvas(cam);
                CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
                scaler.matchWidthOrHeight = size.w / (float)size.h > 0.7f ? 1f : 0f; // igual ao UIRoot
                RectTransform screen = UI.Rect(canvas.transform, "MainMenu");
                var view = new MainMenuView(screen, new NullMainMenuNavigator());
                view.Bind(new SampleMainMenuData().Snapshot());
                for (int i = 0; i < 3; i++)
                {
                    Canvas.ForceUpdateCanvases();
                    view.Bind(new SampleMainMenuData().Snapshot()); // barras recalculam com os tamanhos já resolvidos
                }
                cam.Render();
                RenderTexture.active = target;
                var texture = new Texture2D(size.w, size.h, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, size.w, size.h), 0, 0);
                texture.Apply();
                RenderTexture.active = null;
                File.WriteAllBytes(Path.Combine(dir, $"main_menu_{size.name}.png"), texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
                cam.targetTexture = null;
                target.Release();
                Debug.Log($"[MainMenu] Captura {size.name} salva em {dir}");
            }
        }

        // ---------------------------------------------------------------- util

        private static string GetArgument(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name) return args[i + 1];
            }
            return null;
        }

        private static void EnsureFolder(string path)
        {
            path = path.TrimEnd('/');
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
