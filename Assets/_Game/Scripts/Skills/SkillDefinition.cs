using UnityEngine;

namespace LegionRush
{
    public enum SkillId { ArrowRain = 0, CollectiveShield = 1, AreaHeal = 2, ArcaneExplosion = 3, CavalryCharge = 4, Freeze = 5, CommanderStrike = 6 }

    [CreateAssetMenu(fileName = "Skill_", menuName = "Legion Rush/Skill Definition")]
    public class SkillDefinition : ScriptableObject
    {
        public const int Count = 7;

        public SkillId id;
        public string displayName = "Habilidade";
        [Tooltip("Rótulo curto para o botão.")] public string shortLabel = "HAB";
        [TextArea] public string description = "";
        public Color color = Color.white;
        [Min(1f)] public float cooldown = 15f;
        [Min(0f)] public float duration = 1f;
        [Min(0.5f)] public float radius = 4f;
        [Min(0f), Tooltip("Dano, cura (fração) ou redução de dano, conforme a habilidade.")] public float power = 30f;
        [Tooltip("Bônus de poder por unidade da classe relacionada (ex.: +3% por arqueiro).")] public float perUnitBonus = 0.03f;
        [Tooltip("Exige alvo inimigo para ser usada.")] public bool needsTarget = true;

        public static SkillDefinition CreatePreset(SkillId id)
        {
            var s = CreateInstance<SkillDefinition>();
            s.id = id;
            s.name = "Skill_" + id;
            switch (id)
            {
                case SkillId.ArrowRain:
                    Set(s, "Chuva de Flechas", "FLECHAS", "Dezenas de flechas caem sobre o maior grupo inimigo. Mais forte com mais arqueiros.",
                        new Color(1f, 0.8f, 0.3f), 14f, 1.6f, 4.5f, 16f, 0.03f, true);
                    break;
                case SkillId.CollectiveShield:
                    Set(s, "Escudo Coletivo", "ESCUDO", "Todo o exército recebe 65% de redução de dano por 6 segundos.",
                        new Color(0.4f, 0.8f, 1f), 22f, 6f, 1f, 0.65f, 0f, false);
                    break;
                case SkillId.AreaHeal:
                    Set(s, "Cura em Área", "CURA", "Restaura 45% da vida de todas as tropas. Curandeiras aumentam o efeito.",
                        new Color(0.45f, 1f, 0.55f), 20f, 0f, 1f, 0.45f, 0.02f, false);
                    break;
                case SkillId.ArcaneExplosion:
                    Set(s, "Explosão Arcana", "ARCANA", "Explosão mágica em área no maior grupo inimigo. Magos aumentam o dano.",
                        new Color(0.75f, 0.4f, 1f), 16f, 0.35f, 5f, 90f, 0.08f, true);
                    break;
                case SkillId.CavalryCharge:
                    Set(s, "Investida de Cavalaria", "CAVALARIA", "Cavaleiros espectrais atravessam a pista, ferindo e atordoando inimigos.",
                        new Color(0.6f, 0.75f, 1f), 18f, 1.8f, 1.3f, 60f, 0.1f, false);
                    break;
                case SkillId.Freeze:
                    Set(s, "Congelamento", "GELO", "Congela inimigos na área por 3 segundos e os deixa lentos depois. Chefões só ficam lentos.",
                        new Color(0.55f, 0.9f, 1f), 18f, 3f, 6f, 15f, 0f, true);
                    break;
                default:
                    Set(s, "Ataque do Comandante", "COMANDANTE", "O comandante golpeia o chão: dano pesado e atordoamento ao redor dele.",
                        new Color(1f, 0.85f, 0.4f), 12f, 1f, 4.5f, 120f, 0.004f, false);
                    break;
            }
            return s;
        }

        private static void Set(SkillDefinition s, string name, string label, string description, Color color, float cooldown,
            float duration, float radius, float power, float perUnit, bool needsTarget)
        {
            s.displayName = name;
            s.shortLabel = label;
            s.description = description;
            s.color = color;
            s.cooldown = cooldown;
            s.duration = duration;
            s.radius = radius;
            s.power = power;
            s.perUnitBonus = perUnit;
            s.needsTarget = needsTarget;
        }
    }
}
