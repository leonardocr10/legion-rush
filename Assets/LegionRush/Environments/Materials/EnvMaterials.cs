using UnityEngine;
using UnityEngine.Rendering;

namespace LegionRush.Environments
{
    public enum EnvMaterial { Opaque, Masonry, Road, Glow, Banner, Ice, Sky, Mountains, Distant, Count }

    // Materiais do kit (compatíveis com Android/WebGL, instancing ligado, compatíveis com SRP Batcher).
    // Os shaders ficam em Shaders/Resources para entrarem no build sem referência na cena.
    public static class EnvMaterials
    {
        private static readonly Material[] Cache = new Material[(int)EnvMaterial.Count];
        private static Shader toon, ice, sky;
        private static bool resolved;

        public static bool Available
        {
            get
            {
                Resolve();
                return toon != null;
            }
        }

        public static Material Get(EnvMaterial id)
        {
            Material cached = Cache[(int)id];
            if (cached != null) return cached;
            Resolve();
            Material material = Create(id);
            Cache[(int)id] = material;
            return material;
        }

        private static void Resolve()
        {
            if (resolved) return;
            resolved = true;
            toon = Load("LegionEnvToon", "LegionRush/EnvToon");
            ice = Load("LegionEnvIce", "LegionRush/EnvIce");
            sky = Load("LegionEnvSky", "LegionRush/EnvSky");
            if (toon == null) GameLog.Warn("Shader LegionRush/EnvToon indisponível; cenário usa o ToonLit dos personagens.");
        }

        private static Shader Load(string resource, string shaderName)
        {
            Shader shader = Resources.Load<Shader>(resource);
            if (shader == null) shader = Shader.Find(shaderName);
            return shader != null && shader.isSupported ? shader : null;
        }

        private static Material Create(EnvMaterial id)
        {
            Shader baseShader = toon != null ? toon : MaterialLibrary.LitShader;
            Material m;
            switch (id)
            {
                case EnvMaterial.Masonry:
                    m = NewToon(baseShader, "LR_Env_Masonry");
                    m.mainTexture = EnvTextures.Masonry;
                    m.mainTextureScale = Vector2.one;
                    break;
                case EnvMaterial.Road:
                    m = NewToon(baseShader, "LR_Env_Road");
                    m.mainTexture = EnvTextures.Cobble;
                    SetFloat(m, "_RimIntensity", 0.1f);
                    break;
                case EnvMaterial.Glow:
                    m = NewToon(baseShader, "LR_Env_Glow");
                    SetFloat(m, "_EmissionStrength", 2.2f);
                    SetFloat(m, "_RimIntensity", 0.6f);
                    SetFloat(m, "_FogScale", 0.6f);
                    break;
                case EnvMaterial.Banner:
                    m = NewToon(baseShader, "LR_Env_Banner");
                    m.mainTexture = EnvTextures.Banner;
                    SetFloat(m, "_WaveAmount", 0.16f);
                    SetFloat(m, "_WaveSpeed", 3.2f);
                    SetFloat(m, "_WaveFrequency", 3f);
                    SetFloat(m, "_RampSmoothness", 0.3f);
                    break;
                case EnvMaterial.Distant:
                    // Cordilheira 3D: pouca neblina para continuar nítida atrás do castelo (a neblina cheia a apagaria).
                    m = NewToon(baseShader, "LR_Env_Distant");
                    SetFloat(m, "_FogScale", 0.45f);
                    SetFloat(m, "_RimIntensity", 0.15f);
                    break;
                case EnvMaterial.Ice:
                    if (ice == null) return Get(EnvMaterial.Glow);
                    m = new Material(ice) { name = "LR_Env_Ice", enableInstancing = true, mainTexture = EnvTextures.IceStreaks };
                    m.SetColor("_BaseColor", new Color(0.3f, 0.66f, 1f, 1f));
                    break;
                case EnvMaterial.Sky:
                case EnvMaterial.Mountains:
                    if (sky == null) return null;
                    m = new Material(sky) { name = id == EnvMaterial.Sky ? "LR_Env_Sky" : "LR_Env_Mountains", enableInstancing = true };
                    bool mountains = id == EnvMaterial.Mountains;
                    m.SetFloat("_ZWrite", mountains ? 1f : 0f);
                    m.renderQueue = (int)RenderQueue.GeometryLast + (mountains ? -10 : 0);
                    break;
                default:
                    m = NewToon(baseShader, "LR_Env_Opaque");
                    break;
            }
            return m;
        }

        private static Material NewToon(Shader shader, string name)
        {
            var m = new Material(shader) { name = name, color = Color.white, enableInstancing = true };
            if (m.HasProperty("_OutlineWidth"))
            {
                // Fallback ToonLit: sem contorno no cenário.
                m.SetFloat("_OutlineWidth", 0f);
                m.SetShaderPassEnabled("SRPDefaultUnlit", false);
            }
            return m;
        }

        private static void SetFloat(Material m, string property, float value)
        {
            if (m.HasProperty(property)) m.SetFloat(property, value);
        }
    }
}
