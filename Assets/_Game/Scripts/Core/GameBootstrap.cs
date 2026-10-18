using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace LegionRush
{
    [DefaultExecutionOrder(-100)]
    public class GameBootstrap : MonoBehaviour
    {
        private const string WorldPrefKey = "LR_DebugWorld";

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

        [Header("Áudio (opcional — carrega Resources/LegionAudio se vazio)")]
        [SerializeField] private AudioLibrary audioLibrary;

        [Header("Runtime")]
        [SerializeField] private bool verboseLogging = true;
        [SerializeField] private Vector3 commanderStart = new Vector3(0f, 0f, 2f);

        public string CurrentWorldName => ResolveProfile(currentWorld).displayName;
        public VisualQualityManager Quality => quality;

        private readonly WorldLightingProfile[] runtimePresets = new WorldLightingProfile[WorldLightingProfile.WorldCount];
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

        private void Build()
        {
            CombatRegistry.Reset();
            WorldLightingProfile profile = ResolveProfile(currentWorld);
            lighting.Apply(profile);

            runtimeRoot = new GameObject("LegionRush_Runtime");
            Transform root = runtimeRoot.transform;

            GameObject commanderGo = CharacterFactory.CreateCommander(root);
            commanderGo.transform.position = commanderStart;
            var commander = commanderGo.AddComponent<CommanderController>();
            commander.Initialize(runtimeLevel.forwardSpeed, runtimeLevel.trackHalfWidth);

            var armyGo = new GameObject("Army");
            armyGo.transform.SetParent(root, false);
            var army = armyGo.AddComponent<ArmyManager>();
            army.Initialize(runtimeArmy, commanderGo.transform, runtimeLevel.trackHalfWidth);

            cameraRig.Initialize(commanderGo.transform, army);

            var levelGo = new GameObject("Level");
            levelGo.transform.SetParent(root, false);
            var level = levelGo.AddComponent<LevelManager>();
            level.Initialize(runtimeLevel, profile, army, commander, cameraRig, quality);

            runtimeRoot.AddComponent<LevelAudioDirector>().Initialize(level);
            runtimeRoot.AddComponent<GameHud>().Initialize(level, army, this, quality);
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
        }
    }
}
