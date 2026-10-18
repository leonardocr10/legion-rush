using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace LegionRush
{
    [DefaultExecutionOrder(-100)]
    public class GameBootstrap : MonoBehaviour
    {
        private const string WorldPrefKey = "LR_DebugWorld";
        private const string CompositionPrefKey = "LR_Composition";
        private const string LoadoutPrefKey = "LR_SkillLoadout";
        private const string BossPrefKey = "LR_BossOverride";

        [Header("Config (opcional — usa padrões se vazio)")]
        [SerializeField] private LevelConfig levelConfig;
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
        [SerializeField] private WorldId startWorld = WorldId.FrozenKingdom;
        [SerializeField, Tooltip("Lembra o último mundo escolhido no menu de pausa (teste no aparelho).")]
        private bool rememberWorld = true;

        [Header("Combate (opcional — usa presets se vazio)")]
        [SerializeField] private BossDefinition[] bossDefinitions = new BossDefinition[0];
        [SerializeField] private SkillDefinition[] skillDefinitions = new SkillDefinition[0];

        [Header("Áudio (opcional — carrega Resources/LegionAudio se vazio)")]
        [SerializeField] private AudioLibrary audioLibrary;

        [Header("Runtime")]
        [SerializeField] private bool verboseLogging = true;
        [SerializeField] private Vector3 commanderStart = new Vector3(0f, 0f, 2f);

        public string CurrentWorldName => ResolveProfile(currentWorld).displayName;
        public VisualQualityManager Quality => quality;
        public int CompositionIndex => compositionIndex;
        public int CompositionCount => runtimeArmy != null ? runtimeArmy.compositions.Count : 0;
        public string CompositionName(int index) => runtimeArmy.compositions[Mathf.Clamp(index, 0, CompositionCount - 1)].displayName;
        public SkillId LoadoutSlot(int slot) => loadout[slot];
        public string BossOverrideName => bossOverride < 0 ? "AUTO" : BossDefinition.NameOf((BossId)bossOverride).ToUpperInvariant();

        private readonly WorldLightingProfile[] runtimePresets = new WorldLightingProfile[WorldLightingProfile.WorldCount];
        private readonly BossDefinition[] bossPresets = new BossDefinition[5];
        private readonly SkillId[] loadout = (SkillId[])SkillSystem.DefaultLoadout.Clone();
        private ArmyManager currentArmy;
        private SkillSystem currentSkills;
        private int compositionIndex;
        private int bossOverride = -1;
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
            runtimeLevel = levelConfig != null ? Instantiate(levelConfig) : LevelConfig.CreateDefault();
            runtimeArmy = armyConfig != null ? Instantiate(armyConfig) : ArmyConfig.CreateDefault();
            runtimeLevel.Sanitize();
            runtimeArmy.Sanitize();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!GateMathSelfTest.Run()) GameLog.Error("Gate math self-test failed. Check gate rules before playtesting.");
#endif

            currentWorld = startWorld;
            if (rememberWorld && PlayerPrefs.HasKey(WorldPrefKey))
                currentWorld = (WorldId)Mathf.Clamp(PlayerPrefs.GetInt(WorldPrefKey), 0, WorldLightingProfile.WorldCount - 1);

            compositionIndex = Mathf.Clamp(PlayerPrefs.GetInt(CompositionPrefKey, 0), 0, runtimeArmy.compositions.Count - 1);
            bossOverride = Mathf.Clamp(PlayerPrefs.GetInt(BossPrefKey, -1), -1, 4);
            LoadLoadout();

            sun = ResolveSun();
            SetupCamera();
            quality = gameObject.AddComponent<VisualQualityManager>();
            quality.Initialize(sceneCamera, ambientOcclusionFeature, editorQuality);
            lighting = gameObject.AddComponent<WorldLightingController>();
            lighting.Initialize(sun, sceneCamera, quality);

            AudioManager.EnsureInstance(audioLibrary);
            AudioManager.EnsureSingleListener();
            Build();
        }

        public void Restart()
        {
            Time.timeScale = 1f;
            GameAudio.SetGameplayPaused(false);
            GameAudio.StopAllSfx();
            if (runtimeRoot != null) Destroy(runtimeRoot);
            Build();
            GameLog.Info("Level restarted.");
        }

        public void CycleWorld()
        {
            currentWorld = (WorldId)(((int)currentWorld + 1) % WorldLightingProfile.WorldCount);
            if (rememberWorld)
            {
                PlayerPrefs.SetInt(WorldPrefKey, (int)currentWorld);
                PlayerPrefs.Save();
            }
            Restart();
        }

        // Composição só pode mudar antes de começar a fase.
        public void SetComposition(int index)
        {
            compositionIndex = Mathf.Clamp(index, 0, CompositionCount - 1);
            PlayerPrefs.SetInt(CompositionPrefKey, compositionIndex);
            PlayerPrefs.Save();
            if (currentArmy != null) currentArmy.SetComposition(compositionIndex);
        }

        // Troca a habilidade de um espaço pela próxima que ainda não está equipada.
        public void CycleSkillSlot(int slot)
        {
            if (slot < 0 || slot >= SkillSystem.SlotCount) return;
            int next = (int)loadout[slot];
            for (int attempt = 0; attempt < SkillDefinition.Count; attempt++)
            {
                next = (next + 1) % SkillDefinition.Count;
                if (System.Array.IndexOf(loadout, (SkillId)next) < 0) break;
            }
            loadout[slot] = (SkillId)next;
            PlayerPrefs.SetString(LoadoutPrefKey, string.Join(",", System.Array.ConvertAll(loadout, id => ((int)id).ToString())));
            PlayerPrefs.Save();
            if (currentSkills != null) currentSkills.SetSlot(slot, loadout[slot]);
        }

        public void CycleBossOverride()
        {
            bossOverride = bossOverride >= 4 ? -1 : bossOverride + 1;
            PlayerPrefs.SetInt(BossPrefKey, bossOverride);
            PlayerPrefs.Save();
            Restart();
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
                parsed[i] = (SkillId)value;
            }
            System.Array.Copy(parsed, loadout, loadout.Length);
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
            int[] savedLevels = SaveManager.Current.troops.levels;
            for (int i = 0; i < ArmyConfig.ClassCount; i++) army.SetClassLevelBeforeSpawn((UnitClass)i, savedLevels[i]);
            army.Initialize(runtimeArmy, commanderGo.transform, runtimeLevel.trackHalfWidth, compositionIndex);
            currentArmy = army;

            cameraRig.Initialize(commanderGo.transform, army);

            var levelGo = new GameObject("Level");
            levelGo.transform.SetParent(root, false);
            var level = levelGo.AddComponent<LevelManager>();
            level.Initialize(runtimeLevel, profile, army, commander, cameraRig, quality, ResolveBoss());

            var skills = runtimeRoot.AddComponent<SkillSystem>();
            skills.Initialize(army, level, commander, cameraRig, skillDefinitions, loadout);
            currentSkills = skills;

            runtimeRoot.AddComponent<LevelAudioDirector>().Initialize(level);
            runtimeRoot.AddComponent<GameHud>().Initialize(level, army, this, quality, skills);
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
        }
    }
}
