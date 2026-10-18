using System;
using System.Collections.Generic;
using UnityEngine;

namespace LegionRush.Bosses.DarkGolem
{
    [Serializable]
    public class DarkGolemVariant
    {
        public string name = "Padrão (Vermelho)";
        [Tooltip("Placas de armadura: face, chanfro claro e cavidades.")]
        public Color metal = new Color(0.25f, 0.25f, 0.29f);
        public Color metalEdge = new Color(0.47f, 0.47f, 0.53f);
        public Color metalDark = new Color(0.11f, 0.11f, 0.14f);
        public Color crystal = new Color(1f, 0.1f, 0.08f);
        public Color cloth = new Color(0.78f, 0.06f, 0.08f);
        public Color clothDark = new Color(0.45f, 0.02f, 0.05f);
        public Color eyes = new Color(1f, 0.16f, 0.1f);
        public Color weaponSpikes = new Color(0.55f, 0.55f, 0.6f);
    }

    // Parâmetros visuais do Golem Sombrio (cores, brilho, tempos de animação). A geometria é gerada por código
    // (DarkGolemModel); este asset só ajusta aparência e ritmo, sem tocar na lógica de combate (BossDefinition).
    [CreateAssetMenu(fileName = "DarkGolemProfile", menuName = "Legion Rush/Bosses/Dark Golem Profile")]
    public class DarkGolemProfile : ScriptableObject
    {
        public const string ResourcePath = "Bosses/DarkGolemProfile";

        [Header("Escala")]
        [Tooltip("Altura do modelo (em unidades do modelo) que corresponde a BossDefinition.scale metros.")]
        [Min(0.5f)] public float modelHeight = 2.05f;

        [Header("Brilho")]
        [Min(0f)] public float crystalEmission = 2.2f;
        [Min(0f)] public float eyeEmission = 4.5f;
        [Range(0f, 1f), Tooltip("Brilho dos olhos e cristais enquanto dormente.")] public float dormantGlow = 0.25f;
        [Range(0f, 1f), Tooltip("Brilho durante o atordoamento.")] public float stunnedGlow = 0.45f;
        [Min(0f), Tooltip("Pulsação do brilho na fase enfurecida (Hz).")] public float enragedPulse = 2.4f;
        [Range(0f, 0.05f)] public float outlineWidth = 0.012f;

        [Header("Animação")]
        [Min(0.01f)] public float crossFade = 0.18f;
        [Min(0.1f)] public float idleCycle = 2.6f;
        [Min(0.1f), Tooltip("Duração do ciclo de passada em velocidade máxima.")] public float walkCycle = 1.25f;
        [Min(0.05f)] public float hitReaction = 0.4f;
        [Min(0f)] public float hitCooldown = 0.45f;
        [Range(0f, 1f)] public float footstepShake = 0.12f;
        [Min(0.1f)] public float spawnSeconds = 1.3f;

        [Header("Efeitos")]
        [Min(1f)] public float boulderDropHeight = 14f;
        [Min(0.1f)] public float crystalLifetime = 0.9f;
        [Min(0.1f)] public float crystalHeight = 1.6f;

        public List<DarkGolemVariant> variants = new List<DarkGolemVariant>();

        public DarkGolemVariant Variant(int index)
        {
            if (variants == null || variants.Count == 0) variants = DefaultVariants();
            return variants[Mathf.Clamp(index, 0, variants.Count - 1)];
        }

        public static DarkGolemProfile LoadOrDefault()
        {
            DarkGolemProfile profile = Resources.Load<DarkGolemProfile>(ResourcePath);
            if (profile != null) return profile;
            profile = CreateInstance<DarkGolemProfile>();
            profile.name = "DarkGolemProfile_Default";
            profile.variants = DefaultVariants();
            return profile;
        }

        // As quatro variações da folha de referência: vermelho (padrão), roxo (sombrio), azul (gelo), laranja (lava).
        public static List<DarkGolemVariant> DefaultVariants() => new List<DarkGolemVariant>
        {
            new DarkGolemVariant(),
            new DarkGolemVariant
            {
                name = "Sombrio (Roxo)",
                metal = new Color(0.24f, 0.21f, 0.3f), metalEdge = new Color(0.45f, 0.41f, 0.55f), metalDark = new Color(0.1f, 0.08f, 0.14f),
                crystal = new Color(0.78f, 0.2f, 1f), cloth = new Color(0.55f, 0.12f, 0.75f), clothDark = new Color(0.3f, 0.05f, 0.42f),
                eyes = new Color(0.85f, 0.35f, 1f), weaponSpikes = new Color(0.55f, 0.5f, 0.62f)
            },
            new DarkGolemVariant
            {
                name = "Gelo (Azul)",
                metal = new Color(0.2f, 0.3f, 0.48f), metalEdge = new Color(0.45f, 0.6f, 0.82f), metalDark = new Color(0.08f, 0.12f, 0.22f),
                crystal = new Color(0.25f, 0.7f, 1f), cloth = new Color(0.15f, 0.4f, 0.85f), clothDark = new Color(0.06f, 0.18f, 0.45f),
                eyes = new Color(0.45f, 0.85f, 1f), weaponSpikes = new Color(0.6f, 0.72f, 0.85f)
            },
            new DarkGolemVariant
            {
                name = "Lava (Laranja)",
                metal = new Color(0.22f, 0.2f, 0.2f), metalEdge = new Color(0.45f, 0.4f, 0.38f), metalDark = new Color(0.1f, 0.08f, 0.07f),
                crystal = new Color(1f, 0.5f, 0.06f), cloth = new Color(0.9f, 0.38f, 0.05f), clothDark = new Color(0.5f, 0.16f, 0.02f),
                eyes = new Color(1f, 0.6f, 0.1f), weaponSpikes = new Color(0.6f, 0.55f, 0.5f)
            }
        };
    }
}
