using UnityEngine;

namespace LegionRush
{
    public enum WorldId { FrozenKingdom = 0, LostDesert = 1, EnchantedForest = 2, VolcanicRealm = 3 }
    public enum AmbientParticleType { None, Snow, Dust, Fireflies, Embers }
    public enum PropStyle { SnowPine, Palm, LeafyTree, Basalt }

    [CreateAssetMenu(fileName = "WorldLighting_", menuName = "Legion Rush/World Lighting Profile")]
    public class WorldLightingProfile : ScriptableObject
    {
        public const int WorldCount = 4;

        public WorldId world;
        public string displayName = "Reino Congelado";

        [Header("Sol (Directional Light)")]
        public Color sunColor = Color.white;
        [Range(0f, 3f)] public float sunIntensity = 1.2f;
        [Range(5f, 89f)] public float sunPitch = 50f;
        [Range(-180f, 180f)] public float sunYaw = -30f;
        [Range(0f, 1f)] public float shadowStrength = 0.7f;

        [Header("Sombras estilizadas (ToonLit)")]
        public Color shadowTint = Color.white;
        [Range(0f, 1f)] public float shadowTintAmount = 0.5f;

        [Header("Luz ambiente (Trilight; alimenta Light Probes e SH)")]
        public Color ambientSky = Color.gray;
        public Color ambientEquator = Color.gray;
        public Color ambientGround = Color.gray;

        [Header("Neblina e fundo")]
        public Color fogColor = Color.gray;
        [Min(1f)] public float fogStart = 45f;
        [Min(2f)] public float fogEnd = 110f;

        [Header("Color grading")]
        [Range(-2f, 2f)] public float postExposure;
        [Range(-100f, 100f)] public float contrast;
        [Range(-100f, 100f)] public float saturation;
        [Range(-100f, 100f)] public float temperature;
        [Range(-100f, 100f)] public float tint;
        public Color colorFilter = Color.white;
        public Color splitShadows = new Color(0.5f, 0.5f, 0.5f);
        public Color splitHighlights = new Color(0.5f, 0.5f, 0.5f);
        [Range(-100f, 100f)] public float splitBalance;

        [Header("Bloom e vinheta")]
        [Range(0f, 3f)] public float bloomIntensity = 0.6f;
        [Range(0f, 2f)] public float bloomThreshold = 0.95f;
        [Range(0f, 1f)] public float bloomScatter = 0.65f;
        public Color bloomTint = Color.white;
        [Range(0f, 1f)] public float vignetteIntensity = 0.2f;
        public Color vignetteColor = Color.black;

        [Header("Leitura dos personagens")]
        [Range(0f, 2f)] public float characterRimBoost = 0.4f;
        [Range(0f, 2f)] public float emissionBoost = 0.2f;

        [Header("Ambiência (simulada, sem luzes dinâmicas extras)")]
        public Color torchColor = new Color(1f, 0.55f, 0.2f);
        [Min(0f), Tooltip("0 = sem tochas")] public float torchSpacing = 36f;
        public bool lavaChannels;
        public Color lavaColor = new Color(1f, 0.42f, 0.08f);
        public bool godRays;
        public Color godRayColor = new Color(1f, 0.95f, 0.7f, 0.22f);
        public AmbientParticleType particles = AmbientParticleType.None;
        public Color particleColor = Color.white;
        [Min(0)] public int particleRate = 40;

        [Header("Paleta do cenário")]
        public PropStyle propStyle = PropStyle.SnowPine;
        public Color groundColor = Color.white;
        public Color roadColor = Color.gray;
        public Color borderColor = Color.gray;
        public Color propPrimary = Color.green;
        public Color propSecondary = new Color(0.35f, 0.24f, 0.16f);
        public Color accentColor = Color.cyan;
        [Min(0f)] public float accentEmission = 1.5f;

        private void OnValidate()
        {
            fogEnd = Mathf.Max(fogStart + 1f, fogEnd);
        }

        public static string DefaultName(WorldId id) => id switch
        {
            WorldId.LostDesert => "Deserto Perdido",
            WorldId.EnchantedForest => "Floresta Encantada",
            WorldId.VolcanicRealm => "Reino Vulcânico",
            _ => "Reino Congelado"
        };

        public static WorldLightingProfile CreatePreset(WorldId id)
        {
            var p = CreateInstance<WorldLightingProfile>();
            p.world = id;
            p.displayName = DefaultName(id);
            p.name = "WorldLighting_" + id;
            switch (id)
            {
                case WorldId.LostDesert: p.SetupDesert(); break;
                case WorldId.EnchantedForest: p.SetupForest(); break;
                case WorldId.VolcanicRealm: p.SetupVolcanic(); break;
                default: p.SetupFrozen(); break;
            }
            return p;
        }

        // Dia frio e ensolarado: sol branco-azulado, sombras azuis, tochas laranja de contraste.
        private void SetupFrozen()
        {
            sunColor = new Color(1f, 0.97f, 0.94f); sunIntensity = 1.25f; sunPitch = 50f; sunYaw = -30f; shadowStrength = 0.65f;
            shadowTint = new Color(0.55f, 0.68f, 1f); shadowTintAmount = 0.5f;
            ambientSky = new Color(0.54f, 0.63f, 0.82f); ambientEquator = new Color(0.48f, 0.54f, 0.66f); ambientGround = new Color(0.38f, 0.42f, 0.52f);
            // Neblina azul-clara mais longa: deixa ver o cânion e o castelo do fim (guia de iluminação: "luz fria e brilhante").
            fogColor = new Color(0.64f, 0.79f, 0.98f); fogStart = 55f; fogEnd = 150f;
            // Neve branca na luz e azul só na sombra (referência): balanço quase neutro, split toning suave.
            postExposure = -0.12f; contrast = 28f; saturation = 24f; temperature = -2f; tint = 0f; colorFilter = Color.white;
            splitShadows = new Color(0.47f, 0.5f, 0.57f); splitHighlights = new Color(0.55f, 0.53f, 0.5f); splitBalance = -10f;
            bloomIntensity = 0.55f; bloomThreshold = 1f; bloomScatter = 0.6f; bloomTint = new Color(0.85f, 0.93f, 1f);
            vignetteIntensity = 0.18f; vignetteColor = new Color(0.1f, 0.15f, 0.3f);
            characterRimBoost = 0.4f; emissionBoost = 0.2f;
            torchColor = new Color(1f, 0.55f, 0.2f); torchSpacing = 36f; lavaChannels = false; godRays = false;
            particles = AmbientParticleType.Snow; particleColor = new Color(1f, 1f, 1f, 0.9f); particleRate = 70;
            propStyle = PropStyle.SnowPine; groundColor = new Color(0.92f, 0.95f, 1f); roadColor = new Color(0.66f, 0.74f, 0.86f);
            borderColor = new Color(0.48f, 0.55f, 0.66f); propPrimary = new Color(0.12f, 0.38f, 0.33f); propSecondary = new Color(0.35f, 0.24f, 0.16f);
            accentColor = new Color(0.35f, 0.75f, 1f); accentEmission = 1.8f;
        }

        // Sol dourado e quente, sombras marrom-alaranjadas, poeira discreta.
        private void SetupDesert()
        {
            sunColor = new Color(1f, 0.86f, 0.62f); sunIntensity = 1.4f; sunPitch = 42f; sunYaw = -50f; shadowStrength = 0.7f;
            shadowTint = new Color(0.75f, 0.48f, 0.32f); shadowTintAmount = 0.6f;
            ambientSky = new Color(0.95f, 0.82f, 0.62f); ambientEquator = new Color(0.85f, 0.68f, 0.48f); ambientGround = new Color(0.62f, 0.45f, 0.3f);
            fogColor = new Color(0.96f, 0.84f, 0.64f); fogStart = 40f; fogEnd = 105f;
            postExposure = 0.12f; contrast = 16f; saturation = 22f; temperature = 12f; tint = 2f; colorFilter = Color.white;
            splitShadows = new Color(0.56f, 0.47f, 0.42f); splitHighlights = new Color(0.58f, 0.53f, 0.45f); splitBalance = 15f;
            bloomIntensity = 0.45f; bloomThreshold = 1f; bloomScatter = 0.6f; bloomTint = new Color(1f, 0.88f, 0.7f);
            vignetteIntensity = 0.22f; vignetteColor = new Color(0.3f, 0.15f, 0.05f);
            characterRimBoost = 0.3f; emissionBoost = 0.1f;
            torchColor = new Color(1f, 0.6f, 0.25f); torchSpacing = 42f; lavaChannels = false; godRays = false;
            particles = AmbientParticleType.Dust; particleColor = new Color(1f, 0.88f, 0.65f, 0.35f); particleRate = 30;
            propStyle = PropStyle.Palm; groundColor = new Color(0.93f, 0.78f, 0.52f); roadColor = new Color(0.84f, 0.7f, 0.5f);
            borderColor = new Color(0.72f, 0.55f, 0.36f); propPrimary = new Color(0.25f, 0.6f, 0.25f); propSecondary = new Color(0.62f, 0.45f, 0.28f);
            accentColor = new Color(1f, 0.75f, 0.3f); accentEmission = 0f;
        }

        // Luz filtrada pelas árvores, verde e azul, feixes de sol simulados e vaga-lumes.
        private void SetupForest()
        {
            sunColor = new Color(1f, 0.95f, 0.78f); sunIntensity = 1.15f; sunPitch = 55f; sunYaw = -20f; shadowStrength = 0.6f;
            shadowTint = new Color(0.3f, 0.5f, 0.55f); shadowTintAmount = 0.6f;
            ambientSky = new Color(0.55f, 0.75f, 0.82f); ambientEquator = new Color(0.42f, 0.6f, 0.5f); ambientGround = new Color(0.3f, 0.42f, 0.3f);
            fogColor = new Color(0.62f, 0.8f, 0.78f); fogStart = 40f; fogEnd = 100f;
            postExposure = 0.1f; contrast = 12f; saturation = 25f; temperature = -3f; tint = -4f; colorFilter = Color.white;
            splitShadows = new Color(0.44f, 0.52f, 0.53f); splitHighlights = new Color(0.55f, 0.54f, 0.46f); splitBalance = 0f;
            bloomIntensity = 0.6f; bloomThreshold = 0.95f; bloomScatter = 0.7f; bloomTint = new Color(0.85f, 1f, 0.9f);
            vignetteIntensity = 0.24f; vignetteColor = new Color(0.05f, 0.15f, 0.1f);
            characterRimBoost = 0.5f; emissionBoost = 0.3f;
            torchColor = new Color(1f, 0.62f, 0.25f); torchSpacing = 40f; lavaChannels = false;
            godRays = true; godRayColor = new Color(1f, 0.95f, 0.7f, 0.22f);
            particles = AmbientParticleType.Fireflies; particleColor = new Color(0.75f, 1f, 0.5f, 1f); particleRate = 22;
            propStyle = PropStyle.LeafyTree; groundColor = new Color(0.33f, 0.55f, 0.25f); roadColor = new Color(0.62f, 0.58f, 0.48f);
            borderColor = new Color(0.45f, 0.42f, 0.36f); propPrimary = new Color(0.2f, 0.55f, 0.22f); propSecondary = new Color(0.38f, 0.26f, 0.16f);
            accentColor = new Color(0.3f, 0.85f, 1f); accentEmission = 2.2f;
        }

        // Vermelho, laranja e roxo; ambiente escuro mas com rim forte para leitura das tropas.
        private void SetupVolcanic()
        {
            sunColor = new Color(1f, 0.6f, 0.4f); sunIntensity = 0.95f; sunPitch = 38f; sunYaw = -40f; shadowStrength = 0.75f;
            shadowTint = new Color(0.45f, 0.15f, 0.35f); shadowTintAmount = 0.7f;
            ambientSky = new Color(0.45f, 0.3f, 0.48f); ambientEquator = new Color(0.52f, 0.28f, 0.24f); ambientGround = new Color(0.62f, 0.24f, 0.12f);
            fogColor = new Color(0.28f, 0.1f, 0.12f); fogStart = 35f; fogEnd = 95f;
            postExposure = 0.08f; contrast = 20f; saturation = 15f; temperature = 8f; tint = 6f; colorFilter = new Color(1f, 0.95f, 0.92f);
            splitShadows = new Color(0.52f, 0.42f, 0.56f); splitHighlights = new Color(0.6f, 0.5f, 0.42f); splitBalance = -10f;
            bloomIntensity = 0.85f; bloomThreshold = 0.9f; bloomScatter = 0.7f; bloomTint = new Color(1f, 0.6f, 0.4f);
            vignetteIntensity = 0.32f; vignetteColor = new Color(0.15f, 0f, 0.02f);
            characterRimBoost = 1f; emissionBoost = 0.4f;
            torchColor = new Color(1f, 0.45f, 0.15f); torchSpacing = 32f;
            lavaChannels = true; lavaColor = new Color(1f, 0.42f, 0.08f); godRays = false;
            particles = AmbientParticleType.Embers; particleColor = new Color(1f, 0.55f, 0.15f, 1f); particleRate = 55;
            propStyle = PropStyle.Basalt; groundColor = new Color(0.18f, 0.14f, 0.15f); roadColor = new Color(0.36f, 0.32f, 0.34f);
            borderColor = new Color(0.24f, 0.2f, 0.22f); propPrimary = new Color(0.2f, 0.16f, 0.18f); propSecondary = new Color(0.3f, 0.1f, 0.08f);
            accentColor = new Color(1f, 0.4f, 0.1f); accentEmission = 2.5f;
        }
    }
}
