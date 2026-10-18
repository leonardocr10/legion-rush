using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace LegionRush.EditorTools
{
    // Monta a tela de abertura a partir dos sprites recortados por tools/art/extract_splash.py:
    // prefab em camadas (Prefabs/SplashScreenCanvas), cena SplashScreen (índice 0 da build), Sprite Atlas e capturas.
    // Menu: Legion Rush › UI › Splash Screen. Batch:
    //   Unity.exe -batchmode -projectPath . -executeMethod LegionRush.EditorTools.SplashScreenBuilder.BuildBatch
    //   Unity.exe -batchmode -projectPath . -executeMethod LegionRush.EditorTools.SplashScreenBuilder.CaptureBatch -captureDir <pasta>
    public static class SplashScreenBuilder
    {
        public const string Root = "Assets/LegionRush/UI/SplashScreen";
        public const string ScenePath = Root + "/Scenes/SplashScreen.unity";
        public const string PrefabPath = Root + "/Prefabs/SplashScreenCanvas.prefab";
        private const string AtlasPath = Root + "/Sprites/SplashScreenAtlas.spriteatlasv2";
        private const float RefWidth = 1080f;
        private const float RefHeight = 1920f;

        // Interior da barra e posição do cristal, normalizados no sprite (Sprites/splash_sprites.json).
        private static readonly Vector4 BarInner = new Vector4(0.04f, 0.1714f, 0.96f, 0.5714f);
        private static readonly Vector4 BarGem = new Vector4(0.4526f, 0f, 0.5474f, 1f);

        private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
        private static readonly Vector2 Bottom = new Vector2(0.5f, 0f);
        private static readonly Vector2 Top = new Vector2(0.5f, 1f);
        private static readonly Vector2 Left = new Vector2(0f, 0.5f);
        private static readonly Vector2 Right = new Vector2(1f, 0.5f);

        private static readonly List<SplashEntrance> entrances = new List<SplashEntrance>();

        // ---------- Menus ----------

        [MenuItem("Legion Rush/UI/Splash Screen/Build Prefab + Scene")]
        public static void BuildMenu()
        {
            Build();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        [MenuItem("Legion Rush/UI/Splash Screen/Capture Preview")]
        public static void CaptureMenu()
        {
            string dir = Path.GetFullPath("Builds/SplashPreview");
            Capture(dir);
            EditorUtility.RevealInFinder(dir);
        }

        public static void BuildBatch() => RunBatch(Build);

        public static void CaptureBatch() => RunBatch(() =>
        {
            Build();
            Capture(GetArgument("-captureDir") ?? Path.GetFullPath("Builds/SplashPreview"));
        });

        private static void RunBatch(Action action)
        {
            int code = 0;
            try { action(); }
            catch (Exception exception)
            {
                Debug.LogError("[LegionRush] Splash: " + exception);
                code = 1;
            }
            EditorApplication.Exit(code);
        }

        // ---------- Construção ----------

        public static void Build()
        {
            foreach (string folder in new[] { "Sprites", "Characters", "Backgrounds", "Decorations", "Buttons", "Fonts", "Materials", "Prefabs", "Animations", "Scripts", "Scenes" })
                EnsureFolder(Root + "/" + folder);
            ReimportSprites();
            TypographyAssetBuilder.EnsureAssets();
            BuildAtlas();

            GameObject root = BuildHierarchy();
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            BuildScene();
            RegisterBuildScenes();
            AssetDatabase.SaveAssets();
            Debug.Log($"[LegionRush] Splash: prefab {PrefabPath} e cena {ScenePath} gerados; build = {string.Join(", ", ProjectSetup.BuildScenePaths())}.");
        }

        public static void ReimportSprites()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Root }))
                AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid), ImportAssetOptions.ForceUpdate);
        }

        private static void BuildAtlas()
        {
            if (EditorSettings.spritePackerMode != SpritePackerMode.SpriteAtlasV2)
                EditorSettings.spritePackerMode = SpritePackerMode.SpriteAtlasV2;
            if (!File.Exists(AtlasPath))
            {
                var atlas = new SpriteAtlasAsset();
                var folders = new List<UnityEngine.Object>();
                foreach (string folder in new[] { "Sprites", "Characters", "Backgrounds", "Decorations", "Buttons" })
                    folders.Add(AssetDatabase.LoadAssetAtPath<DefaultAsset>(Root + "/" + folder));
                atlas.Add(folders.ToArray());
                SpriteAtlasAsset.Save(atlas, AtlasPath);
                AssetDatabase.ImportAsset(AtlasPath, ImportAssetOptions.ForceUpdate);
            }
            if (AssetImporter.GetAtPath(AtlasPath) is SpriteAtlasImporter importer)
            {
                importer.includeInBuild = true;
                importer.packingSettings = new SpriteAtlasPackingSettings
                {
                    enableRotation = false,
                    enableTightPacking = false,
                    enableAlphaDilation = true,
                    padding = 4
                };
                importer.textureSettings = new SpriteAtlasTextureSettings
                {
                    generateMipMaps = false,
                    filterMode = FilterMode.Bilinear,
                    sRGB = true,
                    readable = false
                };
                var standalone = importer.GetPlatformSettings("DefaultTexturePlatform");
                standalone.maxTextureSize = 2048;
                standalone.textureCompression = TextureImporterCompression.Compressed;
                importer.SetPlatformSettings(standalone);
                var android = importer.GetPlatformSettings("Android");
                android.overridden = true;
                android.maxTextureSize = 2048;
                android.format = TextureImporterFormat.ASTC_6x6;
                importer.SetPlatformSettings(android);
                importer.SaveAndReimport();
            }
        }

        private static GameObject BuildHierarchy()
        {
            entrances.Clear();
            var canvasGo = new GameObject("SplashScreenCanvas", typeof(RectTransform)) { layer = 5 };
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // acima do LR_UICanvas (10) enquanto a fase monta por trás
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(RefWidth, RefHeight);
            // Expand: a área 1080x1920 cabe sempre inteira; telas 20:9 ganham altura, dobráveis ganham largura.
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            canvasGo.AddComponent<GraphicRaycaster>();
            CanvasGroup rootGroup = canvasGo.AddComponent<CanvasGroup>();
            Transform t = canvasGo.transform;

            // Fundo opaco: garante que nada da cena por trás apareça em proporções extremas.
            Image backdrop = canvasGo.AddComponent<Image>();
            backdrop.color = new Color(0.35f, 0.55f, 0.85f);
            backdrop.raycastTarget = true; // bloqueia toques na cena por trás

            BuildBackground(Layer(t, "BackgroundLayer"));
            BuildDecorations(Layer(t, "DecorationLayer"));
            BuildArmy(Layer(t, "ArmyLayer"));
            BuildCharacters(Layer(t, "CharacterLayer"));
            BuildLogo(Layer(t, "LogoLayer"));
            BuildEffects(NestedCanvas(Layer(t, "EffectsLayer"), false));

            RectTransform ui = NestedCanvas(Layer(t, "InterfaceLayer"), true);
            RectTransform safe = Rect(ui, "SafeArea");
            Stretch(safe);
            safe.gameObject.AddComponent<SplashSafeArea>();
            var refs = BuildInterface(safe);

            var controller = canvasGo.AddComponent<SplashScreenController>();
            var so = new SerializedObject(controller);
            so.FindProperty("rootGroup").objectReferenceValue = rootGroup;
            so.FindProperty("startButton").objectReferenceValue = refs.button;
            so.FindProperty("startGroup").objectReferenceValue = refs.buttonGroup;
            so.FindProperty("startArt").objectReferenceValue = refs.buttonArt;
            so.FindProperty("startLabel").objectReferenceValue = refs.buttonLabel;
            so.FindProperty("loadingGroup").objectReferenceValue = refs.loadingGroup;
            so.FindProperty("loadingFill").objectReferenceValue = refs.fill;
            so.FindProperty("statusLabel").objectReferenceValue = refs.status;
            so.FindProperty("versionLabel").objectReferenceValue = refs.version;
            SerializedProperty list = so.FindProperty("entrances");
            list.arraySize = entrances.Count;
            for (int i = 0; i < entrances.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = entrances[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            return canvasGo;
        }

        // Coordenadas no quadro de referência 1080x1920 (y de baixo para cima). Elementos do cenário presos ao centro,
        // personagens e interface à base, logotipo ao topo da área segura, decorações laterais às bordas.

        private static void BuildBackground(RectTransform layer)
        {
            Image sky = Img(layer, "Sky", "Backgrounds/Splash_Sky", Center, Vector2.zero, 1f, Center);
            var fitter = sky.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = sky.sprite.rect.width / sky.sprite.rect.height;
            sky.preserveAspect = false;

            // Cenário preso ao topo (quadro de 1920 de altura): em telas 20:9 o castelo acompanha o logotipo,
            // e o chão/personagens/interface acompanham a base. O vão entre os dois vira campo de neve.
            RectTransform scenery = TopBox(layer, "Scenery");
            RectTransform mountains = Group(scenery, "Mountains");
            Img(mountains, "Mountains_Left", "Backgrounds/Splash_Mountains", Center, new Vector2(-700f, 150f), 1.9f, Bottom, true);
            Img(mountains, "Mountains_Right", "Backgrounds/Splash_Mountains", Center, new Vector2(700f, 150f), 1.9f, Bottom);
            Img(mountains, "Mountains_Main", "Backgrounds/Splash_Mountains", Center, new Vector2(0f, 175f), 2.35f, Bottom);
            Drift(mountains, new Vector2(5f, 2f), 16f, 0.1f);
            Band(scenery, "MountainHaze", 205f, 300f, 0.5f);

            RectTransform castle = Group(scenery, "Castle");
            Image castleArt = Img(castle, "FrozenCastle", "Backgrounds/Splash_Castle", Center, new Vector2(0f, -110f), 1.48f, Bottom);
            Glow(castle, "CastleLight", new Vector2(0f, 120f), new Vector2(760f, 520f), new Color(0.6f, 0.82f, 1f), new Vector2(0.18f, 0.32f), 0.12f);
            Entrance(castle, 0.05f, 0.9f, new Vector2(0f, -30f));
            Drift(castle, new Vector2(3f, 1.5f), 13f, 0.4f);
            castleArt.transform.SetAsLastSibling();

            // camada atmosférica: separa castelo e montanhas dos personagens
            Band(scenery, "Atmosphere", -150f, 460f, 0.62f);

            RectTransform ground = Rect(layer, "SnowTerrain");
            ground.anchorMin = Vector2.zero;
            ground.anchorMax = Vector2.one;
            ground.offsetMin = new Vector2(-40f, 0f);
            ground.offsetMax = new Vector2(40f, -1010f); // topo do chão a 1010 do topo da tela (base do castelo)
            Image groundArt = ground.gameObject.AddComponent<Image>();
            groundArt.sprite = LoadSprite("Sprites/Splash_SnowGround");
            groundArt.raycastTarget = false;

            RectTransform pines = Group(TopBox(layer, "SceneryFront"), "PineTrees");
            Img(pines, "Pine_L3", "Decorations/Splash_PineTree_2", Left, new Vector2(250f, -60f), 1.4f, Bottom);
            Img(pines, "Pine_L2", "Decorations/Splash_PineTree_3", Left, new Vector2(160f, -110f), 1.9f, Bottom);
            Img(pines, "Pine_L1", "Decorations/Splash_PineTree", Left, new Vector2(40f, -170f), 2.4f, Bottom);
            Img(pines, "Pine_R3", "Decorations/Splash_PineTree_4", Right, new Vector2(-250f, -60f), 1.5f, Bottom);
            Img(pines, "Pine_R2", "Decorations/Splash_PineTree_3", Right, new Vector2(-160f, -110f), 1.9f, Bottom, true);
            Img(pines, "Pine_R1", "Decorations/Splash_PineTree", Right, new Vector2(-40f, -170f), 2.4f, Bottom, true);
            Drift(pines, new Vector2(7f, 2f), 11f, 0.7f);

            // sombra azul-escura na base: dá peso ao chão e contraste ao botão
            RectTransform shade = Rect(layer, "GroundShade");
            shade.anchorMin = Vector2.zero;
            shade.anchorMax = new Vector2(1f, 0.42f);
            shade.offsetMin = new Vector2(-40f, 0f);
            shade.offsetMax = new Vector2(40f, 0f);
            Image shadeArt = shade.gameObject.AddComponent<Image>();
            shadeArt.sprite = LoadSprite("Sprites/Splash_Vignette");
            shadeArt.color = new Color(0.05f, 0.12f, 0.32f, 0.62f);
            shadeArt.raycastTarget = false;
        }

        private static void BuildDecorations(RectTransform layer)
        {
            RectTransform scenery = TopBox(layer, "Scenery");
            RectTransform rocks = Group(scenery, "Rocks");
            Img(rocks, "Rocks_Left", "Decorations/Splash_Rocks_2", Center, new Vector2(-390f, -175f), 1.6f, Bottom);
            Img(rocks, "Rocks_Right", "Decorations/Splash_Rocks", Center, new Vector2(400f, -185f), 1.5f, Bottom);
            RectTransform front = Group(layer, "ForegroundRocks");
            Img(front, "Rocks_Front", "Decorations/Splash_Rocks_3", new Vector2(0f, 0f), new Vector2(120f, 30f), 1.9f, Bottom);
            Img(front, "Rocks_FrontRight", "Decorations/Splash_Rocks_2", new Vector2(1f, 0f), new Vector2(-110f, 40f), 1.7f, Bottom, true);

            RectTransform banners = Group(scenery, "Banners");
            Img(banners, "Banner_L2", "Decorations/Splash_Banner_2", Left, new Vector2(235f, 150f), 0.95f, Bottom);
            Img(banners, "Banner_L1", "Decorations/Splash_Banner", Left, new Vector2(95f, 40f), 1.3f, Bottom);
            Img(banners, "Banner_R2", "Decorations/Splash_Banner_2", Right, new Vector2(-235f, 150f), 0.95f, Bottom, true);
            Img(banners, "Banner_R1", "Decorations/Splash_Banner", Right, new Vector2(-95f, 40f), 1.3f, Bottom, true);
            Entrance(banners, 0.25f, 0.7f, new Vector2(0f, -40f));

            RectTransform torches = Group(scenery, "Torches");
            Torch(torches, "Torch_Left", "Decorations/Splash_Torch_2", Left, new Vector2(195f, -150f), 1.15f, 0f);
            Torch(torches, "Torch_Right", "Decorations/Splash_Torch_2", Right, new Vector2(-195f, -150f), 1.15f, 3.7f);
        }

        private static void Torch(RectTransform parent, string name, string sprite, Vector2 anchor, Vector2 pos, float scale, float seed)
        {
            Image art = Img(parent, name, sprite, anchor, pos, scale, Bottom);
            Vector2 size = art.rectTransform.sizeDelta;
            // luz quente atrás e por cima da chama (topo do sprite)
            Image light = GlowImage(art.rectTransform, "WarmLight", new Vector2(0f, size.y * 0.42f), new Vector2(size.x * 2.2f, size.x * 2.2f),
                new Color(1f, 0.62f, 0.25f));
            light.rectTransform.anchorMin = light.rectTransform.anchorMax = Center;
            light.gameObject.AddComponent<SplashGlowPulse>().Configure(new Vector2(0.25f, 0.55f), new Vector2(0.92f, 1.08f), 1.1f, true, seed);
            light.transform.SetAsFirstSibling();
        }

        private static void BuildArmy(RectTransform layer)
        {
            RectTransform army = Group(layer, "BlueArmy");
            var back = new Color(0.78f, 0.84f, 0.96f);
            Img(army, "Army_Left", "Characters/Splash_Army", Bottom, new Vector2(-370f, 690f), 1.2f, Bottom, false, back);
            Img(army, "Army_Right", "Characters/Splash_Army", Bottom, new Vector2(370f, 690f), 1.2f, Bottom, true, back);
            Img(army, "Army_Main", "Characters/Splash_Army", Bottom, new Vector2(0f, 600f), 1.7f, Bottom);
            Entrance(army, 0.35f, 0.8f, new Vector2(0f, -50f));
            Drift(army, new Vector2(4f, 3f), 7f, 0.2f);
        }

        private static void BuildCharacters(RectTransform layer)
        {
            RectTransform archer = Group(layer, "RoyalArcher");
            Image archerArt = Img(archer, "Archer", "Characters/Splash_Archer", Bottom, new Vector2(-330f, 470f), 1.42f, Bottom);
            Entrance(archer, 0.6f, 0.7f, new Vector2(-140f, 0f));
            Drift(archerArt.rectTransform, new Vector2(2f, 5f), 4.2f, 0.3f);

            RectTransform mage = Group(layer, "ArcaneMage");
            Image mageArt = Img(mage, "Mage", "Characters/Splash_Mage", Bottom, new Vector2(330f, 470f), 1.42f, Bottom);
            // orbe mágico na mão estendida (≈ 74% da largura, 40% da altura a partir do topo)
            Vector2 ms = mageArt.rectTransform.sizeDelta;
            Image orb = GlowImage(mageArt.rectTransform, "MagicOrbGlow", new Vector2(ms.x * 0.24f, ms.y * 0.1f), new Vector2(300f, 300f),
                new Color(0.45f, 0.8f, 1f));
            orb.rectTransform.anchorMin = orb.rectTransform.anchorMax = Center;
            orb.gameObject.AddComponent<SplashGlowPulse>().Configure(new Vector2(0.35f, 0.85f), new Vector2(0.85f, 1.12f), 0.8f, false, 0f);
            Entrance(mage, 0.6f, 0.7f, new Vector2(140f, 0f));
            Drift(mageArt.rectTransform, new Vector2(2f, 5f), 4.6f, 0.75f);

            RectTransform king = Group(layer, "SupremeKing");
            Image kingArt = Img(king, "King", "Characters/Splash_King", Bottom, new Vector2(0f, 150f), 1.78f, Bottom);
            Entrance(king, 0.85f, 0.75f, new Vector2(0f, -120f), 0.92f);
            Drift(kingArt.rectTransform, new Vector2(1.5f, 4f), 3.8f, 0.5f);
        }

        private static void BuildLogo(RectTransform layer)
        {
            RectTransform safe = Rect(layer, "SafeArea");
            Stretch(safe);
            safe.gameObject.AddComponent<SplashSafeArea>();

            RectTransform group = Rect(safe, "LegionRushLogo");
            group.anchorMin = group.anchorMax = Top;
            group.pivot = Top;
            group.anchoredPosition = new Vector2(0f, -24f);
            Sprite logoSprite = LoadSprite("Sprites/Splash_Logo");
            group.sizeDelta = logoSprite.rect.size * 1.6f;

            Image glow = GlowImage(group, "LogoGlow", Vector2.zero, group.sizeDelta * 1.35f, new Color(1f, 0.82f, 0.4f));
            glow.rectTransform.anchorMin = glow.rectTransform.anchorMax = Center;
            glow.gameObject.AddComponent<SplashGlowPulse>().Configure(new Vector2(0.25f, 0.5f), new Vector2(0.97f, 1.04f), 0.35f, false, 0f);

            Image logo = Img(group, "Logo", "Sprites/Splash_Logo", Center, Vector2.zero, 1.6f, Center);
            logo.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            Image shine = Img(logo.rectTransform, "LogoShine", "Sprites/Splash_Shine", Center, Vector2.zero, 1f, Center);
            shine.rectTransform.sizeDelta = new Vector2(150f, group.sizeDelta.y * 1.4f);
            shine.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -18f);
            shine.color = new Color(1f, 0.96f, 0.8f, 1f);
            shine.preserveAspect = false;
            shine.gameObject.AddComponent<SplashShine>();

            Entrance(group, 0.15f, 0.7f, new Vector2(0f, 40f), 0.82f, true);
        }

        private static void BuildEffects(RectTransform layer)
        {
            Sprite dot = LoadSprite("Sprites/Splash_Glow");
            SplashParticleField snow = Field(layer, "SnowParticles");
            Stretch(snow.rectTransform);
            snow.Configure(dot, 70, new Vector2(7f, 20f), new Vector2(35f, 80f), new Vector2(-0.15f, -1f), 22f, new Vector2(9f, 16f),
                false, new Color(1f, 1f, 1f, 0.85f));

            SplashParticleField castle = Field(layer, "MagicParticles");
            castle.rectTransform.anchorMin = castle.rectTransform.anchorMax = Top; // segue o castelo (preso ao topo)
            castle.rectTransform.anchoredPosition = new Vector2(0f, 150f - RefHeight * 0.5f);
            castle.rectTransform.sizeDelta = new Vector2(900f, 520f);
            castle.Configure(dot, 22, new Vector2(8f, 18f), new Vector2(10f, 28f), Vector2.up, 10f, new Vector2(2.5f, 5f),
                true, new Color(0.55f, 0.85f, 1f, 0.9f));

            SplashParticleField mage = Field(layer, "MagicParticles_Mage");
            mage.rectTransform.anchorMin = mage.rectTransform.anchorMax = Bottom;
            mage.rectTransform.anchoredPosition = new Vector2(400f, 860f);
            mage.rectTransform.sizeDelta = new Vector2(260f, 260f);
            mage.Configure(dot, 16, new Vector2(8f, 20f), new Vector2(15f, 40f), Vector2.up, 8f, new Vector2(1.2f, 2.6f),
                true, new Color(0.5f, 0.9f, 1f, 1f));
        }

        private struct InterfaceRefs
        {
            public Button button;
            public CanvasGroup buttonGroup;
            public RectTransform buttonArt;
            public TMP_Text buttonLabel;
            public CanvasGroup loadingGroup;
            public Image fill;
            public TMP_Text status;
            public TMP_Text version;
        }

        private static InterfaceRefs BuildInterface(RectTransform safe)
        {
            var refs = new InterfaceRefs();

            // Botão: retângulo de toque maior que a arte (área ampla), arte e brilho dentro.
            RectTransform hit = Rect(safe, "StartButton");
            hit.anchorMin = hit.anchorMax = Bottom;
            hit.anchoredPosition = new Vector2(0f, 330f);
            hit.sizeDelta = new Vector2(940f, 240f);
            Image hitImage = hit.gameObject.AddComponent<Image>();
            hitImage.color = new Color(1f, 1f, 1f, 0f);
            refs.button = hit.gameObject.AddComponent<Button>();
            refs.button.transition = Selectable.Transition.None;
            refs.button.targetGraphic = hitImage;
            refs.buttonGroup = hit.gameObject.AddComponent<CanvasGroup>();

            Image glow = GlowImage(hit, "ButtonGlow", new Vector2(0f, 0f), new Vector2(1040f, 300f), new Color(0.45f, 0.78f, 1f));
            glow.rectTransform.anchorMin = glow.rectTransform.anchorMax = Center;
            Image art = Img(hit, "ButtonArt", "Buttons/Splash_StartButton", Center, Vector2.zero, 1.72f, Center);
            refs.buttonArt = art.rectTransform;
            refs.buttonLabel = Label(art.rectTransform, "Label", "TOQUE PARA COMEÇAR", 56f, LegionTextStyle.PrimaryButton, Color.white);
            UIStretch(refs.buttonLabel.rectTransform, 120f, 34f);
            hit.gameObject.AddComponent<SplashButtonPulse>().Configure(art.rectTransform, glow);

            // Carregamento: texto de estado + barra com preenchimento real.
            RectTransform loading = Rect(safe, "LoadingPanel");
            loading.anchorMin = loading.anchorMax = Bottom;
            loading.anchoredPosition = new Vector2(0f, 190f);
            loading.sizeDelta = new Vector2(860f, 190f);
            refs.loadingGroup = loading.gameObject.AddComponent<CanvasGroup>();
            refs.loadingGroup.blocksRaycasts = false;
            refs.status = Label(loading, "StatusText", "Inicializando...", 36f, LegionTextStyle.Menu, Color.white);
            RectTransform sr = refs.status.rectTransform;
            sr.anchorMin = sr.anchorMax = Top;
            sr.pivot = Top;
            sr.anchoredPosition = Vector2.zero;
            sr.sizeDelta = new Vector2(860f, 56f);

            Image bar = Img(loading, "LoadingBar", "Sprites/Splash_LoadingBar", Bottom, new Vector2(0f, 0f), 1.72f, Bottom);
            Image fill = Img(bar.rectTransform, "Fill", "Sprites/Splash_LoadingBarFill", Center, Vector2.zero, 1f, Center);
            SetAnchors(fill.rectTransform, BarInner);
            fill.preserveAspect = false;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 0f;
            refs.fill = fill;
            Image gem = Img(bar.rectTransform, "Gem", "Sprites/Splash_LoadingBarGem", Center, Vector2.zero, 1f, Center);
            SetAnchors(gem.rectTransform, BarGem);
            gem.preserveAspect = false;

            refs.version = Label(safe, "VersionText", "v" + Application.version, 28f, LegionTextStyle.Body, new Color(1f, 1f, 1f, 0.8f));
            RectTransform vr = refs.version.rectTransform;
            vr.anchorMin = vr.anchorMax = Bottom;
            vr.pivot = Bottom;
            vr.anchoredPosition = new Vector2(0f, 36f);
            vr.sizeDelta = new Vector2(500f, 44f);
            return refs;
        }

        private static void BuildScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // Câmera só para limpar a tela até a cena Main assumir (não desenha nada além da cor).
            var camGo = new GameObject("SplashCamera");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.1f, 0.25f);
            cam.cullingMask = 0;
            cam.orthographic = true;
            camGo.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            PrefabUtility.InstantiatePrefab(prefab, scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static void RegisterBuildScenes()
        {
            var scenes = new List<EditorBuildSettingsScene>();
            foreach (string path in ProjectSetup.BuildScenePaths()) scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        // ---------- Captura ----------

        public static void Capture(string dir)
        {
            Directory.CreateDirectory(dir);
            var shots = new (string name, int w, int h, SplashScreenController.LoadState state, float progress)[]
            {
                ("splash_1080x1920_ready", 1080, 1920, SplashScreenController.LoadState.Ready, 1f),
                ("splash_1080x1920_loading", 1080, 1920, SplashScreenController.LoadState.LoadingResources, 0.55f),
                ("splash_1080x2400_ready", 1080, 2400, SplashScreenController.LoadState.Ready, 1f),
                ("splash_1768x2208_foldable", 1768, 2208, SplashScreenController.LoadState.Ready, 1f),
                ("splash_720x1280_error", 720, 1280, SplashScreenController.LoadState.Error, 0.8f),
            };
            foreach (var shot in shots) CaptureOne(dir, shot.name, shot.w, shot.h, shot.state, shot.progress);
            Debug.Log("[LegionRush] Splash: capturas em " + dir);
        }

        private static void CaptureOne(string dir, string name, int width, int height, SplashScreenController.LoadState state, float progress)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camGo = new GameObject("CaptureCamera");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.orthographic = true;
            camGo.AddComponent<UniversalAdditionalCameraData>();
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
            cam.targetTexture = rt;

            var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 5f;
            // mesma regra do CanvasScaler (Expand), aplicada direto porque fora do Play ele não atualiza sozinho
            root.GetComponent<CanvasScaler>().enabled = false;
            canvas.scaleFactor = Mathf.Min(width / RefWidth, height / RefHeight);
            foreach (SplashSafeArea area in root.GetComponentsInChildren<SplashSafeArea>(true))
            {
                area.enabled = false;
                Stretch((RectTransform)area.transform);
            }
            foreach (SplashShine shine in root.GetComponentsInChildren<SplashShine>(true))
                shine.gameObject.SetActive(false); // o reflexo só passa de tempos em tempos
            foreach (SplashScreenController c in root.GetComponentsInChildren<SplashScreenController>(true)) c.enabled = false;
            foreach (SplashGlowPulse pulse in root.GetComponentsInChildren<SplashGlowPulse>(true))
            {
                Graphic g = pulse.GetComponent<Graphic>();
                Color c = g.color;
                c.a = 0.55f;
                g.color = c;
            }
            foreach (SplashParticleField field in root.GetComponentsInChildren<SplashParticleField>(true)) field.Prewarm();
            root.GetComponent<SplashScreenController>().PreviewState(state, progress);

            for (int i = 0; i < 3; i++)
            {
                Canvas.ForceUpdateCanvases();
                foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true)) text.ForceMeshUpdate(true, true);
                foreach (AspectRatioFitter fitter in root.GetComponentsInChildren<AspectRatioFitter>(true)) fitter.SendMessage("UpdateRect", SendMessageOptions.DontRequireReceiver);
                cam.Render();
            }

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply();
            RenderTexture.active = previous;
            cam.targetTexture = null;
            File.WriteAllBytes(Path.Combine(dir, name + ".png"), texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            rt.Release();
            UnityEngine.Object.DestroyImmediate(rt);
            UnityEngine.Object.DestroyImmediate(root);
        }

        // ---------- Ajudantes ----------

        private static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = 5 };
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static void UIStretch(RectTransform rect, float horizontal, float vertical)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(horizontal, vertical);
            rect.offsetMax = new Vector2(-horizontal, -vertical);
        }

        private static void SetAnchors(RectTransform rect, Vector4 box)
        {
            rect.anchorMin = new Vector2(box.x, box.y);
            rect.anchorMax = new Vector2(box.z, box.w);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static RectTransform Layer(Transform parent, string name)
        {
            RectTransform layer = Rect(parent, name);
            Stretch(layer);
            return layer;
        }

        private static RectTransform Group(RectTransform parent, string name) => Layer(parent, name);

        // Quadro de referência (largura total, 1920 de altura) preso ao topo da tela.
        private static RectTransform TopBox(RectTransform parent, string name)
        {
            RectTransform box = Rect(parent, name);
            box.anchorMin = new Vector2(0f, 1f);
            box.anchorMax = Vector2.one;
            box.pivot = Top;
            box.offsetMin = new Vector2(0f, -RefHeight);
            box.offsetMax = Vector2.zero;
            return box;
        }

        // Canvas filho: o que muda a cada quadro (partículas, pulso do botão) não reconstrói o resto.
        private static RectTransform NestedCanvas(RectTransform layer, bool interactive)
        {
            layer.gameObject.AddComponent<Canvas>();
            if (interactive) layer.gameObject.AddComponent<GraphicRaycaster>();
            return layer;
        }

        private static Sprite LoadSprite(string relative)
        {
            string path = $"{Root}/{relative}.png";
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) throw new FileNotFoundException("Sprite ausente (rode tools/art/extract_splash.py): " + path);
            return sprite;
        }

        private static Image Img(Transform parent, string name, string sprite, Vector2 anchor, Vector2 position, float scale, Vector2 pivot,
            bool flip = false, Color? color = null)
        {
            RectTransform rect = Rect(parent, name);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = LoadSprite(sprite);
            image.preserveAspect = true;
            image.raycastTarget = false;
            if (color.HasValue) image.color = color.Value;
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = image.sprite.rect.size * scale;
            if (flip) rect.localScale = new Vector3(-1f, 1f, 1f);
            return image;
        }

        private static Image GlowImage(Transform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            Image glow = Img(parent, name, "Sprites/Splash_Glow", Center, position, 1f, Center, false, color);
            glow.preserveAspect = false;
            glow.rectTransform.sizeDelta = size;
            return glow;
        }

        private static void Glow(RectTransform parent, string name, Vector2 position, Vector2 size, Color color, Vector2 alpha, float speed)
        {
            Image glow = GlowImage(parent, name, position, size, color);
            glow.gameObject.AddComponent<SplashGlowPulse>().Configure(alpha, new Vector2(0.98f, 1.03f), speed, false, 0f);
        }

        // Faixa de névoa horizontal (largura da tela inteira, presa ao centro vertical).
        private static void Band(RectTransform parent, string name, float y, float height, float alpha)
        {
            RectTransform band = Rect(parent, name);
            band.anchorMin = new Vector2(0f, 0.5f);
            band.anchorMax = new Vector2(1f, 0.5f);
            band.offsetMin = new Vector2(-60f, y - height * 0.5f);
            band.offsetMax = new Vector2(60f, y + height * 0.5f);
            Image image = band.gameObject.AddComponent<Image>();
            image.sprite = LoadSprite("Sprites/Splash_Fog");
            image.color = new Color(1f, 1f, 1f, alpha);
            image.raycastTarget = false;
        }

        private static TMP_Text Label(Transform parent, string name, string text, float size, LegionTextStyle style, Color color)
        {
            RectTransform rect = Rect(parent, name);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            LegionStyledText.Attach(label, style, false);
            label.color = color;
            return label;
        }

        private static SplashParticleField Field(RectTransform parent, string name)
        {
            RectTransform rect = Rect(parent, name);
            return rect.gameObject.AddComponent<SplashParticleField>();
        }

        private static void Entrance(RectTransform target, float delay, float duration, Vector2 offset, float scale = 1f, bool bounce = false)
        {
            var entrance = target.gameObject.AddComponent<SplashEntrance>();
            entrance.Configure(delay, duration, offset, scale, bounce);
            entrances.Add(entrance);
        }

        private static void Drift(RectTransform target, Vector2 amplitude, float period, float phase) =>
            target.gameObject.AddComponent<SplashDrift>().Configure(amplitude, period, phase);

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private static string GetArgument(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase)) return args[i + 1];
            return null;
        }
    }

    // Importa os sprites da tela de abertura como Sprite (2D and UI) com alpha real, sem mipmaps.
    // No Android a compressão vem do Sprite Atlas (ASTC 6x6); a textura solta também fica em ASTC.
    public class SplashSpriteImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(SplashScreenBuilder.Root + "/")) return;
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
            importer.SetTextureSettings(settings);
            var android = importer.GetPlatformTextureSettings("Android");
            android.overridden = true;
            android.format = TextureImporterFormat.ASTC_6x6;
            android.maxTextureSize = 1024;
            importer.SetPlatformTextureSettings(android);
        }
    }
}
