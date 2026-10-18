using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LegionRush
{
    // Aplica um WorldLightingProfile: sol, luz ambiente (SH/probes), neblina, color grading,
    // bloom/vinheta escalados pelo tier e parâmetros globais do shader ToonLit.
    public class WorldLightingController : MonoBehaviour
    {
        private static readonly int ShadowTintId = Shader.PropertyToID("_LR_ShadowTint");
        private static readonly int ShadowTintAmountId = Shader.PropertyToID("_LR_ShadowTintAmount");
        private static readonly int RimBoostId = Shader.PropertyToID("_LR_RimBoost");
        private static readonly int EmissionBoostId = Shader.PropertyToID("_LR_EmissionBoost");

        public WorldLightingProfile Profile { get; private set; }

        private Light sun;
        private Camera cam;
        private VisualQualityManager quality;
        private Volume volume;
        private VolumeProfile volumeProfile;
        private ColorAdjustments colorAdjustments;
        private WhiteBalance whiteBalance;
        private SplitToning splitToning;
        private Bloom bloom;
        private Vignette vignette;

        public void Initialize(Light sunLight, Camera targetCamera, VisualQualityManager qualityManager)
        {
            sun = sunLight;
            cam = targetCamera;
            quality = qualityManager;
            BuildVolume();
            if (quality != null) quality.TierChanged += HandleTierChanged;
        }

        public void Apply(WorldLightingProfile profile)
        {
            if (profile == null)
            {
                GameLog.Warn("WorldLightingController: perfil nulo ignorado.");
                return;
            }
            Profile = profile;

            if (sun != null)
            {
                sun.type = LightType.Directional;
                sun.color = profile.sunColor;
                sun.intensity = profile.sunIntensity;
                sun.transform.rotation = Quaternion.Euler(profile.sunPitch, profile.sunYaw, 0f);
                sun.shadowStrength = profile.shadowStrength;
                RenderSettings.sun = sun;
            }

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = profile.ambientSky;
            RenderSettings.ambientEquatorColor = profile.ambientEquator;
            RenderSettings.ambientGroundColor = profile.ambientGround;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = profile.fogColor;
            RenderSettings.fogStartDistance = profile.fogStart;
            RenderSettings.fogEndDistance = Mathf.Max(profile.fogStart + 1f, profile.fogEnd);

            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = profile.fogColor;
                cam.farClipPlane = Mathf.Max(cam.nearClipPlane + 1f, profile.fogEnd + 25f);
            }

            Shader.SetGlobalColor(ShadowTintId, profile.shadowTint);
            Shader.SetGlobalFloat(ShadowTintAmountId, profile.shadowTintAmount);
            Shader.SetGlobalFloat(RimBoostId, profile.characterRimBoost);
            Shader.SetGlobalFloat(EmissionBoostId, profile.emissionBoost);

            colorAdjustments.postExposure.Override(profile.postExposure);
            colorAdjustments.contrast.Override(profile.contrast);
            colorAdjustments.saturation.Override(profile.saturation);
            colorAdjustments.colorFilter.Override(profile.colorFilter);
            whiteBalance.temperature.Override(profile.temperature);
            whiteBalance.tint.Override(profile.tint);
            splitToning.shadows.Override(profile.splitShadows);
            splitToning.highlights.Override(profile.splitHighlights);
            splitToning.balance.Override(profile.splitBalance);
            bloom.threshold.Override(profile.bloomThreshold);
            bloom.scatter.Override(profile.bloomScatter);
            bloom.tint.Override(profile.bloomTint);
            vignette.color.Override(profile.vignetteColor);
            vignette.smoothness.Override(0.45f);

            ApplyQualityDependent();
            DynamicGI.UpdateEnvironment();
            GameLog.Info($"Iluminação aplicada: {profile.displayName}.");
        }

        private void HandleTierChanged(QualityTier tier) => ApplyQualityDependent();

        private void ApplyQualityDependent()
        {
            if (Profile == null) return;
            float bloomScale = 1f;
            bool vignetteOn = true;
            bool softShadows = true;
            if (quality != null)
            {
                QualityTierSettings s = quality.Settings;
                bloomScale = s.BloomScale;
                vignetteOn = s.Vignette;
                softShadows = s.SoftShadows;
            }

            bloom.active = bloomScale > 0.001f && Profile.bloomIntensity > 0f;
            bloom.intensity.Override(Profile.bloomIntensity * bloomScale);
            vignette.active = vignetteOn && Profile.vignetteIntensity > 0f;
            vignette.intensity.Override(Profile.vignetteIntensity);
            if (sun != null) sun.shadows = softShadows ? LightShadows.Soft : LightShadows.Hard;
        }

        private void BuildVolume()
        {
            volumeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            volumeProfile.name = "LR_WorldGrading";

            Tonemapping tonemapping = volumeProfile.Add<Tonemapping>();
            tonemapping.mode.Override(TonemappingMode.Neutral);
            colorAdjustments = volumeProfile.Add<ColorAdjustments>();
            whiteBalance = volumeProfile.Add<WhiteBalance>();
            splitToning = volumeProfile.Add<SplitToning>();
            bloom = volumeProfile.Add<Bloom>();
            bloom.highQualityFiltering.Override(false);
            vignette = volumeProfile.Add<Vignette>();

            var go = new GameObject("LR_WorldVolume");
            go.transform.SetParent(transform, false);
            volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10f;
            volume.sharedProfile = volumeProfile;
        }

        private void OnDestroy()
        {
            if (quality != null) quality.TierChanged -= HandleTierChanged;
            if (volume != null) Destroy(volume.gameObject);
            if (volumeProfile != null) Destroy(volumeProfile);
        }
    }
}
