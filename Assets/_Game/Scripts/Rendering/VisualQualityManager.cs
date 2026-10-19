using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LegionRush
{
    public enum QualityTier { Low = 0, Medium = 1, High = 2, Ultra = 3 }
    public enum FrameRateMode { Auto = 0, Fps30 = 30, Fps60 = 60, Fps120 = 120 }

    public struct QualityTierSettings
    {
        public float RenderScale;
        public int Msaa;
        public float ShadowDistance;
        public int ShadowCascades;
        public bool SoftShadows;
        public bool Hdr;
        public bool PostProcessing;
        public float BloomScale;
        public bool Vignette;
        public bool AmbientOcclusion;
        public float AmbienceScale;
        public float GodRayDensity;
        public bool TorchPools;
        public int DefaultFps;
        // Cenário 3D (kit de ambiente): densidade de decoração (0..1), sombras do cenário
        // (0 nenhuma, 1 só peças grandes, 2 todas) e partículas de fogo (0..1).
        public float EnvironmentDensity;
        public int EnvironmentShadows;
        public float FireParticles;
    }

    public class VisualQualityManager : MonoBehaviour
    {
        private const string TierKey = "LR_QualityTier_v2";
        private const string FpsKey = "LR_FrameRate_v1";
        private const string ShadowsKey = "LR_Shadows_v1";
        private const string EffectsKey = "LR_Effects_v1";
        private const string PerformanceKey = "LR_Performance_v1";
        private const string BatteryKey = "LR_BatterySaver_v1";

        private static readonly QualityTierSettings[] Tiers =
        {
            // BAIXO: desempenho primeiro (meta 60 FPS em aparelho modesto). Sombras duras e curtas só dos personagens,
            // só color grading, cenário com ~55% da decoração, fogo sem partículas.
            new QualityTierSettings { RenderScale = 0.8f, Msaa = 1, ShadowDistance = 22f, ShadowCascades = 1, SoftShadows = false, Hdr = false, PostProcessing = true, BloomScale = 0f, Vignette = false, AmbientOcclusion = false, AmbienceScale = 0.35f, GodRayDensity = 0f, TorchPools = false, DefaultFps = 60, EnvironmentDensity = 0.55f, EnvironmentShadows = 0, FireParticles = 0f },
            // MÉDIO: sombras suaves (cenário incluído), MSAA 2x, bloom moderado, fogo com poucas partículas.
            new QualityTierSettings { RenderScale = 0.9f, Msaa = 2, ShadowDistance = 32f, ShadowCascades = 1, SoftShadows = true, Hdr = false, PostProcessing = true, BloomScale = 0.6f, Vignette = true, AmbientOcclusion = false, AmbienceScale = 0.7f, GodRayDensity = 0.5f, TorchPools = true, DefaultFps = 60, EnvironmentDensity = 0.8f, EnvironmentShadows = 1, FireParticles = 0.6f },
            // ALTO: HDR, MSAA 4x, 2 cascatas, SSAO em aparelhos compatíveis, ambiência e cenário completos.
            new QualityTierSettings { RenderScale = 1f, Msaa = 4, ShadowDistance = 45f, ShadowCascades = 2, SoftShadows = true, Hdr = true, PostProcessing = true, BloomScale = 1f, Vignette = true, AmbientOcclusion = true, AmbienceScale = 1f, GodRayDensity = 1f, TorchPools = true, DefaultFps = 60, EnvironmentDensity = 1f, EnvironmentShadows = 2, FireParticles = 1f },
            // ULTRA: como o ALTO, com sombras mais longas em 4 cascatas e resolução acima da tela (só por escolha manual).
            new QualityTierSettings { RenderScale = 1.15f, Msaa = 4, ShadowDistance = 60f, ShadowCascades = 4, SoftShadows = true, Hdr = true, PostProcessing = true, BloomScale = 1.1f, Vignette = true, AmbientOcclusion = true, AmbienceScale = 1f, GodRayDensity = 1f, TorchPools = true, DefaultFps = 60, EnvironmentDensity = 1f, EnvironmentShadows = 2, FireParticles = 1f }
        };

        [SerializeField] private bool adaptiveDowngrade = true;
        [SerializeField, Range(0.5f, 1f)] private float adaptiveThreshold = 0.85f;
        [SerializeField] private float adaptiveWindow = 4f;
        [SerializeField] private float adaptiveCooldown = 8f;

        public QualityTier Current { get; private set; }
        public QualityTierSettings Settings => Effective(Current);
        public FrameRateMode FrameRate { get; private set; }
        public bool IsManual { get; private set; }
        public bool AmbientOcclusionSupported { get; private set; }

        // Preferências do jogador por cima do nível escolhido (tela de configurações).
        public bool Shadows { get; private set; } = true;
        public bool Effects { get; private set; } = true;
        public bool PerformanceMode { get; private set; }
        public bool BatterySaver { get; private set; }

        public int TargetFps => BatterySaver ? 30 : FrameRate == FrameRateMode.Auto ? Settings.DefaultFps : (int)FrameRate;

        public void SetShadows(bool on) => SetFlag(ShadowsKey, on, v => Shadows = v);
        public void SetEffects(bool on) => SetFlag(EffectsKey, on, v => Effects = v);
        public void SetPerformanceMode(bool on) => SetFlag(PerformanceKey, on, v => PerformanceMode = v);
        public void SetBatterySaver(bool on) => SetFlag(BatteryKey, on, v => BatterySaver = v);

        private void SetFlag(string key, bool on, Action<bool> assign)
        {
            assign(on);
            PlayerPrefs.SetInt(key, on ? 1 : 0);
            PlayerPrefs.Save();
            ApplyTier(Current);
        }

        private QualityTierSettings Effective(QualityTier tier)
        {
            QualityTierSettings s = Tiers[Mathf.Clamp((int)tier, 0, Tiers.Length - 1)];
            if (!Shadows)
            {
                s.ShadowDistance = 0f;
                s.EnvironmentShadows = 0;
            }
            if (!Effects)
            {
                s.FireParticles = 0f;
                s.GodRayDensity = 0f;
                s.AmbienceScale *= 0.35f;
                s.TorchPools = false;
            }
            if (PerformanceMode)
            {
                s.RenderScale = Mathf.Min(s.RenderScale, 0.75f);
                s.Msaa = 1;
                s.Hdr = false;
                s.SoftShadows = false;
                s.AmbientOcclusion = false;
                s.BloomScale = Mathf.Min(s.BloomScale, 0.5f);
                s.EnvironmentDensity = Mathf.Min(s.EnvironmentDensity, 0.6f);
            }
            return s;
        }
        public event Action<QualityTier> TierChanged;

        private UniversalRenderPipelineAsset urpAsset;
        private Camera targetCamera;
        private ScriptableRendererFeature ambientOcclusionFeature;
        private float originalRenderScale;
        private float originalShadowDistance;
        private int originalCascades;
        private int originalMsaa;
        private bool originalHdr;
        private bool originalAo;
        private bool capturedOriginals;
        private float windowTime;
        private int windowFrames;
        private float cooldownTimer;

        public void Initialize(Camera cam, ScriptableRendererFeature aoFeature, QualityTier editorDefault)
        {
            targetCamera = cam;
            ambientOcclusionFeature = aoFeature;
            QualitySettings.vSyncCount = 0;

            urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urpAsset == null)
            {
                GameLog.Error("URP asset não encontrado. Configure o projeto com Universal Render Pipeline.");
            }
            else
            {
                originalRenderScale = urpAsset.renderScale;
                originalShadowDistance = urpAsset.shadowDistance;
                originalCascades = urpAsset.shadowCascadeCount;
                originalMsaa = urpAsset.msaaSampleCount;
                originalHdr = urpAsset.supportsHDR;
                capturedOriginals = true;
            }
            if (ambientOcclusionFeature != null) originalAo = ambientOcclusionFeature.isActive;
            AmbientOcclusionSupported = DetectAmbientOcclusionSupport();

            int savedFps = PlayerPrefs.GetInt(FpsKey, 0);
            FrameRate = savedFps == 30 ? FrameRateMode.Fps30 : savedFps == 60 ? FrameRateMode.Fps60 : savedFps == 120 ? FrameRateMode.Fps120 : FrameRateMode.Auto;
            Shadows = PlayerPrefs.GetInt(ShadowsKey, 1) == 1;
            Effects = PlayerPrefs.GetInt(EffectsKey, 1) == 1;
            PerformanceMode = PlayerPrefs.GetInt(PerformanceKey, 0) == 1;
            BatterySaver = PlayerPrefs.GetInt(BatteryKey, 0) == 1;

            QualityTier tier;
            if (PlayerPrefs.HasKey(TierKey))
            {
                tier = (QualityTier)Mathf.Clamp(PlayerPrefs.GetInt(TierKey), 0, Tiers.Length - 1);
                IsManual = true;
            }
            else
            {
                tier = DetectTier(editorDefault);
                IsManual = false;
            }
            ApplyTier(tier);
        }

        public void SetTier(QualityTier tier, bool manual)
        {
            if (manual)
            {
                IsManual = true;
                PlayerPrefs.SetInt(TierKey, (int)tier);
                PlayerPrefs.Save();
            }
            ApplyTier(tier);
        }

        public void SetFrameRate(FrameRateMode mode)
        {
            FrameRate = mode;
            PlayerPrefs.SetInt(FpsKey, (int)mode);
            PlayerPrefs.Save();
            ApplyFrameRate();
            GameLog.Info($"Frame rate -> {mode} ({TargetFps} FPS).");
        }

        public void ResetToAutomatic(QualityTier editorDefault)
        {
            PlayerPrefs.DeleteKey(TierKey);
            PlayerPrefs.DeleteKey(FpsKey);
            PlayerPrefs.Save();
            IsManual = false;
            FrameRate = FrameRateMode.Auto;
            ApplyTier(DetectTier(editorDefault));
        }

        private void ApplyTier(QualityTier tier)
        {
            Current = tier;
            QualityTierSettings s = Effective(tier);

            if (urpAsset != null)
            {
                urpAsset.renderScale = s.RenderScale;
                urpAsset.msaaSampleCount = s.Msaa;
                urpAsset.shadowDistance = s.ShadowDistance;
                urpAsset.shadowCascadeCount = s.ShadowCascades;
                urpAsset.supportsHDR = s.Hdr;
            }
            if (targetCamera != null)
            {
                UniversalAdditionalCameraData data = targetCamera.GetUniversalAdditionalCameraData();
                data.renderPostProcessing = s.PostProcessing;
                data.antialiasing = AntialiasingMode.None;
                data.renderShadows = Shadows;
                targetCamera.allowMSAA = s.Msaa > 1;
                targetCamera.allowHDR = s.Hdr;
            }

            bool ambientOcclusion = s.AmbientOcclusion && AmbientOcclusionSupported && ambientOcclusionFeature != null;
            if (ambientOcclusionFeature != null) ambientOcclusionFeature.SetActive(ambientOcclusion);
            ApplyFrameRate();

            windowTime = 0f;
            windowFrames = 0;
            cooldownTimer = adaptiveCooldown;
            GameLog.Info($"Qualidade -> {tier} (escala {s.RenderScale}, MSAA {s.Msaa}x, sombras {s.ShadowDistance}m/{(s.SoftShadows ? "suaves" : "duras")}, HDR {s.Hdr}, AO {ambientOcclusion}, {TargetFps} FPS{(IsManual ? ", manual" : string.Empty)}).");
            TierChanged?.Invoke(tier);
        }

        private void ApplyFrameRate()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = TargetFps;
        }

        private void Update()
        {
            if (Application.isEditor || !adaptiveDowngrade || IsManual || Current == QualityTier.Low) return;
            float dt = Time.unscaledDeltaTime;
            if (cooldownTimer > 0f)
            {
                cooldownTimer -= dt;
                return;
            }

            windowTime += dt;
            windowFrames++;
            if (windowTime < adaptiveWindow) return;

            float fps = windowFrames / windowTime;
            windowTime = 0f;
            windowFrames = 0;
            if (fps >= TargetFps * adaptiveThreshold) return;

            GameLog.Warn($"FPS médio {fps:0.0} abaixo da meta {TargetFps}; reduzindo qualidade.");
            ApplyTier((QualityTier)((int)Current - 1));
        }

        private static QualityTier DetectTier(QualityTier editorDefault)
        {
#if UNITY_EDITOR
            return editorDefault;
#else
            int ramMb = SystemInfo.systemMemorySize;
            int cores = SystemInfo.processorCount;
            int shaderLevel = SystemInfo.graphicsShaderLevel;
            QualityTier tier;
            if (ramMb >= 6000 && cores >= 8 && shaderLevel >= 45) tier = QualityTier.High;
            else if (ramMb >= 3500 && shaderLevel >= 35) tier = QualityTier.Medium;
            else tier = QualityTier.Low;
            GameLog.Info($"Aparelho {SystemInfo.deviceModel} | RAM {ramMb}MB | {cores} núcleos | GPU {SystemInfo.graphicsDeviceName} | SM {shaderLevel} -> {tier}");
            return tier;
#endif
        }

        private static bool DetectAmbientOcclusionSupport()
        {
#if UNITY_EDITOR
            return true;
#else
            return SystemInfo.graphicsShaderLevel >= 45 && SystemInfo.systemMemorySize >= 6000;
#endif
        }

        private void OnDestroy()
        {
            if (capturedOriginals && urpAsset != null)
            {
                urpAsset.renderScale = originalRenderScale;
                urpAsset.shadowDistance = originalShadowDistance;
                urpAsset.shadowCascadeCount = originalCascades;
                urpAsset.msaaSampleCount = originalMsaa;
                urpAsset.supportsHDR = originalHdr;
            }
            if (ambientOcclusionFeature != null) ambientOcclusionFeature.SetActive(originalAo);
        }
    }
}
