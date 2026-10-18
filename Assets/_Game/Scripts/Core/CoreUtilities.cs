using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace LegionRush
{
    public static class GameLog
    {
        public static bool Verbose = true;
        private const string Prefix = "[LegionRush] ";

        public static void Info(string message)
        {
            if (Verbose) Debug.Log(Prefix + message);
        }

        public static void Warn(string message) => Debug.LogWarning(Prefix + message);
        public static void Error(string message) => Debug.LogError(Prefix + message);
    }

    public static class MaterialLibrary
    {
        public const float OutlineWidth = 0.015f;
        private const string ToonShaderName = "LegionRush/ToonLit";
        private const string GlowShaderName = "LegionRush/AdditiveGlow";

        private static readonly Dictionary<(Color, float, float, bool), Material> Cache =
            new Dictionary<(Color, float, float, bool), Material>();
        private static Shader litShader;
        private static Shader glowShader;
        private static bool glowResolved;

        // Referências serializadas no GameBootstrap garantem que os shaders entrem no build.
        public static Shader PreferredToonShader { get; set; }
        public static Shader PreferredGlowShader { get; set; }
        public static bool UsingToon { get; private set; }

        public static Shader LitShader
        {
            get
            {
                if (litShader != null) return litShader;
                Shader toon = PreferredToonShader != null ? PreferredToonShader : Shader.Find(ToonShaderName);
                if (toon != null && toon.isSupported)
                {
                    litShader = toon;
                    UsingToon = true;
                    return litShader;
                }

                litShader = Shader.Find("Universal Render Pipeline/Lit");
                if (litShader == null)
                {
                    GameObject probe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    litShader = probe.GetComponent<Renderer>().sharedMaterial.shader;
                    Object.Destroy(probe);
                }
                UsingToon = false;
                GameLog.Warn($"Shader {ToonShaderName} indisponível; usando {litShader.name}.");
                return litShader;
            }
        }

        public static Shader GlowShader
        {
            get
            {
                if (glowResolved) return glowShader;
                glowResolved = true;
                glowShader = PreferredGlowShader != null ? PreferredGlowShader : Shader.Find(GlowShaderName);
                if (glowShader == null || !glowShader.isSupported)
                {
                    glowShader = null;
                    GameLog.Warn($"Shader {GlowShaderName} indisponível; brilhos, partículas e rastros desativados.");
                }
                return glowShader;
            }
        }

        public static Material Get(Color color, float emission = 0f, float metallic = 0f, bool outline = true)
        {
            var key = (color, emission, metallic, outline);
            if (Cache.TryGetValue(key, out Material cached) && cached != null) return cached;

            Material material = CreateLit(color, emission, metallic, outline);
            material.name = $"LR_{ColorUtility.ToHtmlStringRGB(color)}_{emission:0.##}_{metallic:0.##}{(outline ? "_o" : string.Empty)}";
            Cache[key] = material;
            return material;
        }

        public static Material CreateLit(Color color, float emission, float metallic, bool outline)
        {
            var material = new Material(LitShader) { color = color, enableInstancing = true };
            float smoothness = 0.25f + metallic * 0.45f;
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (material.HasProperty("_HighlightColor")) material.SetColor("_HighlightColor", new Color(1f, 1f, 1f, 0.15f + metallic * 0.6f));
            if (material.HasProperty("_OutlineWidth"))
            {
                material.SetFloat("_OutlineWidth", outline ? OutlineWidth : 0f);
                material.SetShaderPassEnabled("SRPDefaultUnlit", outline);
            }
            if (emission > 0f) SetEmission(material, color * emission);
            return material;
        }

        public static void SetEmission(Material material, Color hdrColor)
        {
            if (material == null || !material.HasProperty("_EmissionColor")) return;
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", hdrColor);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        }

        // Material transparente sem iluminação: aditivo (brilho) ou alpha (neve, poeira).
        public static Material CreateGlow(Texture texture, Color color, bool additive, float intensity = 1f)
        {
            Shader shader = GlowShader;
            if (shader == null) return null;
            var material = new Material(shader)
            {
                name = additive ? "LR_GlowAdditive" : "LR_GlowAlpha",
                mainTexture = texture,
                color = color,
                renderQueue = (int)RenderQueue.Transparent,
                enableInstancing = true
            };
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_Intensity", intensity);
            return material;
        }
    }

    public static class PrimitiveFactory
    {
        private static Font builtinFont;

        public static Font BuiltinFont
        {
            get
            {
                if (builtinFont == null) builtinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return builtinFont;
            }
        }

        public static Transform Part(PrimitiveType type, Transform parent, Vector3 localPosition, Vector3 localScale,
            Material material, Vector3 localEuler = default, bool castShadows = true)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            Collider collider = go.GetComponent<Collider>();
            if (collider != null) Object.Destroy(collider);

            Transform t = go.transform;
            t.SetParent(parent, false);
            t.localPosition = localPosition;
            t.localRotation = Quaternion.Euler(localEuler);
            t.localScale = localScale;

            Renderer renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            renderer.receiveShadows = true;
            return t;
        }

        public static TextMesh Label(Transform parent, Vector3 localPosition, string text, int fontSize,
            float characterSize, Color color)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;

            TextMesh mesh = go.AddComponent<TextMesh>();
            mesh.font = BuiltinFont;
            mesh.fontSize = fontSize;
            mesh.fontStyle = FontStyle.Bold;
            mesh.characterSize = characterSize;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = color;
            mesh.text = text;

            MeshRenderer renderer = go.GetComponent<MeshRenderer>();
            if (renderer == null) renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = mesh.font.material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return mesh;
        }
    }

    public static class PointerInput
    {
        public static bool IsPressed(out Vector2 screenPosition)
        {
#if ENABLE_INPUT_SYSTEM
            Pointer pointer = Pointer.current;
            if (pointer != null && pointer.press.isPressed)
            {
                screenPosition = pointer.position.ReadValue();
                return true;
            }
            screenPosition = Vector2.zero;
            return false;
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.touchCount > 0)
            {
                screenPosition = Input.GetTouch(0).position;
                return true;
            }
            if (Input.GetMouseButton(0))
            {
                screenPosition = Input.mousePosition;
                return true;
            }
            screenPosition = Vector2.zero;
            return false;
#else
            screenPosition = Vector2.zero;
            return false;
#endif
        }
    }
}
