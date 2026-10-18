using UnityEngine;
using UnityEngine.Rendering.Universal;

#pragma warning disable 0618 // FindObjectsSortMode: obsoleto apenas em versões mais novas do Unity 6
namespace LegionRush
{
    // Ponto de entrada. Monta a fase atual (que também serve de cenário 3D atrás dos menus) e conduz o fluxo:
    // Splash → Menu → Preparação → Partida → Resultado → Menu/Próxima fase.
    [DefaultExecutionOrder(-100)]
    public class GameBootstrap : MonoBehaviour
    {
        private const string WorldPrefKey = "LR_DebugWorldOverride";
        private const string LoadoutPrefKey = "LR_SkillLoadout";
        private const string BossPrefKey = "LR_BossOverride";

        [Header("Campanha (opcional — usa a fase padrão se vazio)")]
        [SerializeField] private LevelCatalog levelCatalog;
        [SerializeField, Tooltip("Compatibilidade: fase única adicionada ao catálogo se não existir.")]
        private LevelConfig levelConfig;
        [SerializeField] private ArmyConfig armyConfig;

        [Header("Visual")]
        [SerializeField] private CharacterVisualLibrary visualLibrary;
        [SerializeField, Tooltip("LegionRush/ToonLit — referência garante inclusão no build.")]
        private Shader toonShader;
        [SerializeField, Tooltip("LegionRush/AdditiveGlow — referência garante inclusão no build.")]
        private Shader glowShader;
        [SerializeField] private ScriptableRendererFeature ambientOcclusionFeature;
        [SerializeField] private QualityTier editorQuality = QualityTier.High;

        [Header("Mundos (opcional — usa presets se vazio)")]
        [SerializeField] private WorldLightingProfile[] worldProfiles = new WorldLightingProfile[0];

        [Header("Combate (opcional — usa presets se vazio)")]
        [SerializeField] private BossDefinition[] bossDefinitions = new BossDefinition[0];
        [SerializeField] private SkillDefinition[] skillDefinitions = new SkillDefinition[0];

        [Header("Áudio (opcional — carrega Resources/LegionAudio se vazio)")]
        [SerializeField] private AudioLibrary audioLibrary;

        [Header("Runtime")]
        [SerializeField] private bool verboseLogging = true;
        [SerializeField] private bool showSplash = true;
        [SerializeField] private Vector3 commanderStart = new Vector3(0f, 0f, 2f);

        public event System.Action<LevelManager> LevelBuilt;

        public LevelCatalog Catalog => catalog;
        public LevelManager CurrentLevel { get; private set; }
        public SkillSystem CurrentSkills => currentSkills;
        public int CurrentLevelNumber => runtimeLevel != null ? runtimeLevel.levelNumber : 1;
        public WorldId CurrentWorld => currentWorld;
        public string CurrentWorldName => ResolveProfile(currentWorld).displayName;
        public VisualQualityManager Quality => quality;
        public int CompositionIndex => compositionIndex;
        public int CompositionCount => runtimeArmy != null ? runtimeArmy.compositions.Count : 0;
        public string CompositionName(int index) => runtimeArmy.compositions[Mathf.Clamp(index, 0, CompositionCount - 1)].displayName;
        public SkillId LoadoutSlot(int slot) => loadout[slot];
        public string BossOverrideName => bossOverride < 0 ? "AUTO" : BossDefinition.NameOf((BossId)bossOverride).ToUpperInvariant();
        public string WorldOverrideName => worldOverride < 0 ? "DA FASE" : WorldLightingProfile.DefaultName((WorldId)worldOverride).ToUpperInvariant();
        public int MaxTroopLevel => runtimeArmy != null ? runtimeArmy.maxTroopLevel : 10;

        private readonly WorldLightingProfile[] runtimePresets = new WorldLightingProfile[WorldLightingProfile.WorldCount];
        private readonly BossDefinition[] bossPresets = new BossDefinition[5];
        private readonly SkillId[] loadout = (SkillId[])SkillSystem.DefaultLoadout.Clone();
        private LevelCatalog catalog;
        private bool ownsCatalog;
        private ArmyManager currentArmy;
        private SkillSystem currentSkills;
        private UIRoot ui;
        private int compositionIndex;
        private int bossOverride = -1;
        private int worldOverride = -1;
        private LevelConfig runtimeLevel;
        private ArmyConfig runtimeArmy;
        private GameObject runtimeRoot;
        private Camera sceneCamera;
        private ArmyCameraController cameraRig;
        private Light sun;
        private VisualQualityManager quality;
        private WorldLightingController lighting;
        private WorldId currentWorld;

        // Garante que o jogo rode mesmo numa cena vazia (sem precisar montar nada no editor).
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (FindAnyObjectByType<GameBootstrap>() != null) return;
            new GameObject("Bootstrap (auto)").AddComponent<GameBootstrap>();
        }

        private void Awake()
        {
            GameLog.Verbose = verboseLogging;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Screen.orientation = ScreenOrientation.Portrait;
            MaterialLibrary.PreferredToonShader = toonShader;
            MaterialLibrary.PreferredGlowShader = glowShader;
            CharacterFactory.Library = visualLibrary;
            if (visualLibrary != null) visualLibrary.Validate();

            SaveManager.Load();
            catalog = levelCatalog != null ? levelCatalog : LevelCatalog.CreateDefault();
            ownsCatalog = levelCatalog == null;
            if (levelConfig != null && !catalog.Has(levelConfig.levelNumber)) catalog.levels.Add(levelConfig);
            runtimeArmy = armyConfig != null ? Instantiate(armyConfig) : ArmyConfig.CreateDefault();
            runtimeArmy.Sanitize();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!GateMathSelfTest.Run()) GameLog.Error("Gate math self-test failed. Check gate rules before playtesting.");
#endif

            compositionIndex = Mathf.Clamp(SaveManager.Current.settings.composition, 0, runtimeArmy.compositions.Count - 1);
            bossOverride = Mathf.Clamp(PlayerPrefs.GetInt(BossPrefKey, -1), -1, 4);
            worldOverride = Mathf.Clamp(PlayerPrefs.GetInt(WorldPrefKey, -1), -1, WorldLightingProfile.WorldCount - 1);
            LoadLoadout();
            SelectLevel(DefaultLevelNumber());

            sun = ResolveSun();
            SetupCamera();
            quality = gameObject.AddComponent<VisualQualityManager>();
            quality.Initialize(sceneCamera, ambientOcclusionFeature, editorQuality);
            lighting = gameObject.AddComponent<WorldLightingController>();
            lighting.Initialize(sun, sceneCamera, quality);

            AudioManager.EnsureInstance(audioLibrary);
            AudioManager.EnsureSingleListener();

            ui = new GameObject("LR_UI").AddComponent<UIRoot>();
            ui.transform.SetParent(transform, false);
            ui.Initialize(this);
            Build();
            if (showSplash) ui.Replace<SplashScreen>();
            else ui.Replace<MainMenuScreen>();
        }

        // ---------- Fluxo ----------

        public bool IsLevelAvailable(int levelNumber) => catalog.Has(levelNumber);
        public bool IsLevelUnlocked(int levelNumber) => levelNumber <= SaveManager.Current.progress.highestUnlocked;

        public int DefaultLevelNumber()
        {
            int target = Mathf.Min(SaveManager.Current.progress.highestUnlocked, Mathf.Max(1, catalog.HighestAvailable()));
            for (int n = target; n >= 1; n--)
            {
                if (catalog.Has(n)) return n;
            }
            return 1;
        }

        // Carrega a fase e abre a preparação de tropas.
        public void PlayLevel(int levelNumber)
        {
            if (!catalog.Has(levelNumber) || !IsLevelUnlocked(levelNumber)) return;
            SelectLevel(levelNumber);
            Restart();
            ui.Replace<PrepScreen>();
        }

        public bool HasNextLevel() => catalog.Has(CurrentLevelNumber + 1) && IsLevelUnlocked(CurrentLevelNumber + 1);
        public void PlayNextLevel() => PlayLevel(CurrentLevelNumber + 1);
        public void RetryLevel() => PlayLevel(CurrentLevelNumber);

        public void ReturnToMenu()
        {
            Restart();
            ui.Replace<MainMenuScreen>();
        }

        public void StartRun()
        {
            if (CurrentLevel == null) return;
            ui.CloseAll();
            CurrentLevel.StartRun();
        }

        public void Restart()
        {
            Time.timeScale = 1f;
            GameAudio.SetGameplayPaused(false);
            GameAudio.StopAllSfx();
            if (runtimeRoot != null) Destroy(runtimeRoot);
            Build();
            GameLog.Info($"Fase {CurrentLevelNumber} montada.");
        }

        // Ferramentas de teste no aparelho (Ajustes): sobrescrevem mundo e chefão da fase.
        public void CycleWorldOverride()
        {
            worldOverride = worldOverride >= WorldLightingProfile.WorldCount - 1 ? -1 : worldOverride + 1;
            PlayerPrefs.SetInt(WorldPrefKey, worldOverride);
            PlayerPrefs.Save();
            SelectLevel(CurrentLevelNumber);
            ReturnToSettings();
        }

        public void CycleBossOverride()
        {
            bossOverride = bossOverride >= 4 ? -1 : bossOverride + 1;
            PlayerPrefs.SetInt(BossPrefKey, bossOverride);
            PlayerPrefs.Save();
            ReturnToSettings();
        }

        private void ReturnToSettings()
        {
            Restart();
            ui.Replace<MainMenuScreen>();
            ui.Push<SettingsScreen>();
        }

        // ---------- Preparação ----------

        public bool IsCompositionAvailable(int index)
        {
            float[] weights = runtimeArmy.compositions[Mathf.Clamp(index, 0, CompositionCount - 1)].weights;
            for (int i = 0; i < weights.Length; i++)
            {
                if (weights[i] > 0f && MetaService.IsClassUnlocked((UnitClass)i)) return true;
            }
            return false;
        }

        public void SetComposition(int index)
        {
            compositionIndex = Mathf.Clamp(index, 0, CompositionCount - 1);
            SaveManager.Current.settings.composition = compositionIndex;
            SaveManager.MarkDirty();
            if (currentArmy != null) currentArmy.SetComposition(compositionIndex);
        }

        // Troca a habilidade de um espaço pela próxima desbloqueada que ainda não está equipada.
        public void CycleSkillSlot(int slot)
        {
            if (slot < 0 || slot >= SkillSystem.SlotCount) return;
            int next = (int)loadout[slot];
            for (int attempt = 0; attempt < SkillDefinition.Count; attempt++)
            {
                next = (next + 1) % SkillDefinition.Count;
                if (MetaService.IsSkillUnlocked((SkillId)next) && System.Array.IndexOf(loadout, (SkillId)next) < 0) break;
            }
            if (!MetaService.IsSkillUnlocked((SkillId)next) || System.Array.IndexOf(loadout, (SkillId)next) >= 0) return;
            loadout[slot] = (SkillId)next;
            PlayerPrefs.SetString(LoadoutPrefKey, string.Join(",", System.Array.ConvertAll(loadout, id => ((int)id).ToString())));
            PlayerPrefs.Save();
            if (currentSkills != null) currentSkills.SetSlot(slot, loadout[slot]);
        }

        private void LoadLoadout()
        {
            string saved = PlayerPrefs.GetString(LoadoutPrefKey, string.Empty);
            if (string.IsNullOrEmpty(saved)) return;
            string[] parts = saved.Split(',');
            if (parts.Length != SkillSystem.SlotCount) return;
            var parsed = new SkillId[SkillSystem.SlotCount];
            for (int i = 0; i < parts.Length; i++)
            {
                if (!int.TryParse(parts[i], out int value) || value < 0 || value >= SkillDefinition.Count) return;
                if (!MetaService.IsSkillUnlocked((SkillId)value) || System.Array.IndexOf(parsed, (SkillId)value, 0, i) >= 0) return;
                parsed[i] = (SkillId)value;
            }
            System.Array.Copy(parsed, loadout, loadout.Length);
        }

        // ---------- Montagem ----------

        private void SelectLevel(int levelNumber)
        {
            LevelConfig source = catalog.Get(levelNumber) ?? catalog.Get(1);
            if (runtimeLevel != null) Destroy(runtimeLevel);
            runtimeLevel = source != null ? Instantiate(source) : LevelConfig.CreateDefault();
            runtimeLevel.Sanitize();
            currentWorld = worldOverride >= 0 ? (WorldId)worldOverride : runtimeLevel.world;
        }

        private BossDefinition ResolveBoss()
        {
            BossEncounter encounter = runtimeLevel.boss;
            if (encounter == null || !encounter.enabled) return null;
            BossId id = bossOverride >= 0 ? (BossId)bossOverride
                : encounter.useWorldDefault ? BossDefinition.DefaultFor(currentWorld) : encounter.boss;
            if (bossDefinitions != null)
            {
                foreach (BossDefinition definition in bossDefinitions)
                {
                    if (definition != null && definition.id == id) return definition;
                }
            }
            int index = (int)id;
            if (bossPresets[index] == null) bossPresets[index] = BossDefinition.CreatePreset(id);
            return bossPresets[index];
        }

        private void Build()
        {
            CombatRegistry.Reset();
            WorldLightingProfile profile = ResolveProfile(currentWorld);
            lighting.Apply(profile);

            runtimeRoot = new GameObject("LegionRush_Runtime");
            Transform root = runtimeRoot.transform;
            var vfxGo = new GameObject("Vfx");
            vfxGo.transform.SetParent(root, false);
            vfxGo.AddComponent<VfxManager>().Initialize();

            GameObject commanderGo = CharacterFactory.CreateCommander(root);
            commanderGo.transform.position = commanderStart;
            var commander = commanderGo.AddComponent<CommanderController>();
            commander.Initialize(runtimeLevel.forwardSpeed, runtimeLevel.trackHalfWidth);

            var armyGo = new GameObject("Army");
            armyGo.transform.SetParent(root, false);
            var army = armyGo.AddComponent<ArmyManager>();
            SaveData save = SaveManager.Current;
            for (int i = 0; i < ArmyConfig.ClassCount; i++) army.SetClassLevelBeforeSpawn((UnitClass)i, save.troops.levels[i]);
            army.ApplyMeta(MetaService.ComputeBonuses(true), save.troops.unlocked, MetaService.SkinTint());
            army.Initialize(runtimeArmy, commanderGo.transform, runtimeLevel.trackHalfWidth, compositionIndex);
            currentArmy = army;

            cameraRig.Initialize(commanderGo.transform, army);

            var levelGo = new GameObject("Level");
            levelGo.transform.SetParent(root, false);
            var level = levelGo.AddComponent<LevelManager>();
            level.Initialize(runtimeLevel, profile, army, commander, cameraRig, quality, ResolveBoss());
            CurrentLevel = level;

            var skills = runtimeRoot.AddComponent<SkillSystem>();
            skills.Initialize(army, level, commander, cameraRig, skillDefinitions, loadout);
            currentSkills = skills;

            runtimeRoot.AddComponent<LevelAudioDirector>().Initialize(level);
            runtimeRoot.AddComponent<GameHud>().Initialize(level, army, this, skills);
            LevelBuilt?.Invoke(level);
        }

        private WorldLightingProfile ResolveProfile(WorldId id)
        {
            if (worldProfiles != null)
            {
                foreach (WorldLightingProfile profile in worldProfiles)
                {
                    if (profile != null && profile.world == id) return profile;
                }
            }
            int index = (int)id;
            if (runtimePresets[index] == null) runtimePresets[index] = WorldLightingProfile.CreatePreset(id);
            return runtimePresets[index];
        }

        private static Light ResolveSun()
        {
            foreach (Light light in FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light.type == LightType.Directional) return light;
            }
            Light created = new GameObject("Sun").AddComponent<Light>();
            created.type = LightType.Directional;
            return created;
        }

        private void SetupCamera()
        {
            sceneCamera = Camera.main;
            if (sceneCamera == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                sceneCamera = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            sceneCamera.clearFlags = CameraClearFlags.SolidColor;
            sceneCamera.nearClipPlane = 0.3f;
            sceneCamera.farClipPlane = 140f;

            cameraRig = sceneCamera.GetComponent<ArmyCameraController>();
            if (cameraRig == null) cameraRig = sceneCamera.gameObject.AddComponent<ArmyCameraController>();
        }

        private void OnDestroy()
        {
            for (int i = 0; i < runtimePresets.Length; i++)
            {
                if (runtimePresets[i] != null) Destroy(runtimePresets[i]);
            }
            for (int i = 0; i < bossPresets.Length; i++)
            {
                if (bossPresets[i] != null) Destroy(bossPresets[i]);
            }
            if (runtimeLevel != null) Destroy(runtimeLevel);
            if (ownsCatalog && catalog != null)
            {
                foreach (LevelConfig level in catalog.levels)
                {
                    if (level != null && level != levelConfig) Destroy(level);
                }
                Destroy(catalog);
            }
        }
    }
}
