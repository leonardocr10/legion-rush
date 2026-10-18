// Stubs mínimos do URP/Core RP usados SOMENTE pela verificação de compilação no CI (sem licença Unity).
// Não fazem parte do projeto Unity. Espelham as assinaturas públicas usadas pelo jogo.
using System;
using UnityEngine;

namespace UnityEngine.Rendering
{
    public class VolumeComponent : ScriptableObject { public bool active = true; }
    public abstract class VolumeParameter { public bool overrideState; }
    public class VolumeParameter<T> : VolumeParameter
    {
        public T value;
        public VolumeParameter(T value = default, bool overrideState = false) { this.value = value; this.overrideState = overrideState; }
        public void Override(T x) { overrideState = true; value = x; }
    }
    public class FloatParameter : VolumeParameter<float> { public FloatParameter(float v, bool o = false) : base(v, o) { } }
    public class MinFloatParameter : FloatParameter { public MinFloatParameter(float v, float min, bool o = false) : base(v, o) { } }
    public class ClampedFloatParameter : FloatParameter { public ClampedFloatParameter(float v, float min, float max, bool o = false) : base(v, o) { } }
    public class ColorParameter : VolumeParameter<Color> { public ColorParameter(Color v, bool o = false) : base(v, o) { } }
    public class BoolParameter : VolumeParameter<bool> { public BoolParameter(bool v, bool o = false) : base(v, o) { } }
    public class VolumeProfile : ScriptableObject
    {
        public T Add<T>(bool overrides = false) where T : VolumeComponent => CreateInstance<T>();
    }
    public class Volume : MonoBehaviour
    {
        public bool isGlobal;
        public float priority;
        public VolumeProfile sharedProfile;
    }
}

namespace UnityEngine.Rendering.Universal
{
    public enum AntialiasingMode { None, FastApproximateAntialiasing, SubpixelMorphologicalAntiAliasing, TemporalAntiAliasing }
    public enum TonemappingMode { None, Neutral, ACES }
    public class TonemappingModeParameter : VolumeParameter<TonemappingMode> { public TonemappingModeParameter(TonemappingMode v, bool o = false) : base(v, o) { } }

    public class PostProcessData : ScriptableObject { }
    public abstract class ScriptableRendererData : ScriptableObject { }
    public class UniversalRendererData : ScriptableRendererData { public PostProcessData postProcessData; }
    public abstract class ScriptableRendererFeature : ScriptableObject
    {
        public bool isActive => true;
        public void SetActive(bool active) { }
    }

    public class UniversalRenderPipelineAsset : RenderPipelineAsset
    {
        public float renderScale { get; set; }
        public int msaaSampleCount { get; set; }
        public float shadowDistance { get; set; }
        public int shadowCascadeCount { get; set; }
        public bool supportsHDR { get; set; }
        public static UniversalRenderPipelineAsset Create(ScriptableRendererData rendererData = null) => CreateInstance<UniversalRenderPipelineAsset>();
        protected override RenderPipeline CreatePipeline() => null;
    }

    public class UniversalAdditionalCameraData : MonoBehaviour
    {
        public bool renderPostProcessing { get; set; }
        public AntialiasingMode antialiasing { get; set; }
        public bool renderShadows { get; set; }
    }

    public static class CameraExtensions
    {
        public static UniversalAdditionalCameraData GetUniversalAdditionalCameraData(this Camera camera) => camera.GetComponent<UniversalAdditionalCameraData>();
    }

    public sealed class Tonemapping : VolumeComponent { public TonemappingModeParameter mode = new TonemappingModeParameter(TonemappingMode.None); }
    public sealed class ColorAdjustments : VolumeComponent
    {
        public FloatParameter postExposure = new FloatParameter(0f);
        public ClampedFloatParameter contrast = new ClampedFloatParameter(0f, -100f, 100f);
        public ColorParameter colorFilter = new ColorParameter(Color.white);
        public ClampedFloatParameter hueShift = new ClampedFloatParameter(0f, -180f, 180f);
        public ClampedFloatParameter saturation = new ClampedFloatParameter(0f, -100f, 100f);
    }
    public sealed class WhiteBalance : VolumeComponent
    {
        public ClampedFloatParameter temperature = new ClampedFloatParameter(0f, -100f, 100f);
        public ClampedFloatParameter tint = new ClampedFloatParameter(0f, -100f, 100f);
    }
    public sealed class SplitToning : VolumeComponent
    {
        public ColorParameter shadows = new ColorParameter(Color.grey);
        public ColorParameter highlights = new ColorParameter(Color.grey);
        public ClampedFloatParameter balance = new ClampedFloatParameter(0f, -100f, 100f);
    }
    public sealed class Bloom : VolumeComponent
    {
        public MinFloatParameter threshold = new MinFloatParameter(0.9f, 0f);
        public MinFloatParameter intensity = new MinFloatParameter(0f, 0f);
        public ClampedFloatParameter scatter = new ClampedFloatParameter(0.7f, 0f, 1f);
        public ColorParameter tint = new ColorParameter(Color.white);
        public BoolParameter highQualityFiltering = new BoolParameter(false);
    }
    public sealed class Vignette : VolumeComponent
    {
        public ColorParameter color = new ColorParameter(Color.black);
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);
        public ClampedFloatParameter smoothness = new ClampedFloatParameter(0.2f, 0.01f, 1f);
    }
}
