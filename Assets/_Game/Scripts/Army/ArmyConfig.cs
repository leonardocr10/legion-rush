using System;
using System.Collections.Generic;
using UnityEngine;

namespace LegionRush
{
    // Ordem fixa: valores serializados em assets. Novas classes sempre no final.
    public enum UnitClass { Archer = 0, Warrior = 1, Lancer = 2, Mage = 3, Healer = 4, Knight = 5 }
    public enum TroopRole { Front, Back }
    public enum AttackKind { Melee, Projectile, AreaMagic, Heal }
    public enum TroopRarity { Common, Rare, Epic, Legendary }
    public enum FormationStance { Balanced, Aggressive, Defensive }

    [Serializable]
    public class UnitStats
    {
        public UnitClass unitClass;
        public string displayName = "Tropa";
        public TroopRole role = TroopRole.Front;
        public AttackKind attackKind = AttackKind.Melee;
        public DamageType damageType = DamageType.Physical;
        public TroopRarity rarity = TroopRarity.Common;
        [Min(1f)] public float maxHealth = 20f;
        [Tooltip("Dano por ataque. Para curandeiros, vida restaurada por cura.")]
        [Min(0f)] public float damage = 8f;
        [Min(0.5f)] public float attackRange = 9f;
        [Min(0.1f)] public float attackInterval = 1f;
        [Min(0f), Tooltip("Armadura física: dano × 100/(100+armadura).")] public float armor = 0f;
        [Range(0f, 1f)] public float critChance = 0.1f;
        [Min(1f)] public float critMultiplier = 2f;
        [Min(0.5f)] public float moveSpeed = 9f;
        [Min(0f), Tooltip("Raio do dano em área (mago).")] public float areaRadius;
        [Min(1), Tooltip("Inimigos atingidos em linha por golpe (lanceiro).")] public int pierceTargets = 1;
        [Min(1f), Tooltip("Multiplicador do primeiro golpe após avançar (cavaleiro).")] public float chargeMultiplier = 1f;
        [Min(0.2f)] public float bodyRadius = 0.3f;
        public Color color = Color.white;
    }

    [Serializable]
    public class CompositionPreset
    {
        public string displayName = "Equilibrada";
        [Tooltip("Peso por classe, na ordem de UnitClass. Normalizado automaticamente.")]
        public float[] weights = new float[ArmyConfig.ClassCount];
    }

    [CreateAssetMenu(fileName = "ArmyConfig", menuName = "Legion Rush/Army Config")]
    public class ArmyConfig : ScriptableObject
    {
        public const int ClassCount = 6;

        public List<UnitStats> classes = new List<UnitStats>();
        public List<CompositionPreset> compositions = new List<CompositionPreset>();
        [Min(1)] public int startingSoldiers = 12;
        [Min(1)] public int maxSoldiers = 150;
        [Min(0.3f)] public float formationSpacing = 0.6f;
        [Min(1f), Tooltip("Distância máxima que a linha de frente avança para engajar (postura equilibrada).")]
        public float frontLeashRange = 6f;
        [Min(1f)] public float projectileSpeed = 24f;
        [Min(1f)] public float orbSpeed = 14f;
        [Min(8)] public int projectilePoolSize = 64;
        [Range(0f, 0.5f), Tooltip("Ganho de vida e dano por nível de tropa.")] public float levelScaling = 0.12f;
        [Min(1)] public int maxTroopLevel = 10;

        private UnitStats[] lookup;

        public UnitStats Get(UnitClass unitClass)
        {
            if (lookup == null || lookup.Length != ClassCount) BuildLookup();
            return lookup[(int)unitClass];
        }

        public void Sanitize()
        {
            classes ??= new List<UnitStats>();
            var byClass = new UnitStats[ClassCount];
            foreach (UnitStats stats in classes)
            {
                if (stats == null) continue;
                int index = (int)stats.unitClass;
                if (index >= 0 && index < ClassCount && byClass[index] == null) byClass[index] = stats;
            }
            for (int i = 0; i < ClassCount; i++)
            {
                UnitStats stats = byClass[i] ?? DefaultStats((UnitClass)i);
                stats.unitClass = (UnitClass)i;
                stats.pierceTargets = Mathf.Max(1, stats.pierceTargets);
                stats.chargeMultiplier = Mathf.Max(1f, stats.chargeMultiplier);
                stats.attackInterval = Mathf.Max(0.1f, stats.attackInterval);
                byClass[i] = stats;
            }
            classes = new List<UnitStats>(byClass);

            compositions ??= new List<CompositionPreset>();
            compositions.RemoveAll(c => c == null);
            if (compositions.Count == 0) compositions.AddRange(DefaultCompositions());
            foreach (CompositionPreset preset in compositions)
            {
                if (preset.weights == null || preset.weights.Length != ClassCount)
                {
                    var resized = new float[ClassCount];
                    if (preset.weights != null) Array.Copy(preset.weights, resized, Mathf.Min(ClassCount, preset.weights.Length));
                    preset.weights = resized;
                }
                float total = 0f;
                for (int i = 0; i < ClassCount; i++)
                {
                    preset.weights[i] = Mathf.Max(0f, preset.weights[i]);
                    total += preset.weights[i];
                }
                if (total <= 0f) preset.weights[(int)UnitClass.Warrior] = 1f;
            }

            maxSoldiers = Mathf.Max(1, maxSoldiers);
            startingSoldiers = Mathf.Clamp(startingSoldiers, 1, maxSoldiers);
            projectilePoolSize = Mathf.Max(8, projectilePoolSize);
            maxTroopLevel = Mathf.Max(1, maxTroopLevel);
            lookup = null;
        }

        public static ArmyConfig CreateDefault()
        {
            var config = CreateInstance<ArmyConfig>();
            config.name = "ArmyConfig_Default";
            config.Sanitize();
            return config;
        }

        private void BuildLookup()
        {
            lookup = new UnitStats[ClassCount];
            foreach (UnitStats stats in classes)
            {
                if (stats != null) lookup[(int)stats.unitClass] = stats;
            }
            for (int i = 0; i < ClassCount; i++) lookup[i] ??= DefaultStats((UnitClass)i);
        }

        private void OnValidate() => lookup = null;

        public static string ClassName(UnitClass unitClass) => unitClass switch
        {
            UnitClass.Archer => "Arqueiro",
            UnitClass.Warrior => "Guerreiro",
            UnitClass.Lancer => "Lanceiro",
            UnitClass.Mage => "Mago",
            UnitClass.Healer => "Curandeira",
            _ => "Cavaleiro"
        };

        public static UnitStats DefaultStats(UnitClass unitClass)
        {
            switch (unitClass)
            {
                case UnitClass.Archer:
                    return new UnitStats
                    {
                        unitClass = unitClass, displayName = "Arqueiro", role = TroopRole.Back, attackKind = AttackKind.Projectile,
                        maxHealth = 20f, damage = 9f, attackRange = 9f, attackInterval = 1f, armor = 5f, critChance = 0.12f,
                        critMultiplier = 2f, moveSpeed = 9f, color = new Color(0.2f, 0.62f, 0.32f)
                    };
                case UnitClass.Lancer:
                    return new UnitStats
                    {
                        unitClass = unitClass, displayName = "Lanceiro", role = TroopRole.Front, attackKind = AttackKind.Melee,
                        maxHealth = 38f, damage = 13f, attackRange = 2.3f, attackInterval = 1f, armor = 22f, critChance = 0.1f,
                        critMultiplier = 1.8f, moveSpeed = 8.5f, pierceTargets = 3, color = new Color(0.16f, 0.58f, 0.62f)
                    };
                case UnitClass.Mage:
                    return new UnitStats
                    {
                        unitClass = unitClass, displayName = "Mago", role = TroopRole.Back, attackKind = AttackKind.AreaMagic,
                        damageType = DamageType.Magic, rarity = TroopRarity.Rare, maxHealth = 22f, damage = 13f, attackRange = 8f,
                        attackInterval = 1.7f, armor = 4f, critChance = 0.08f, critMultiplier = 1.6f, moveSpeed = 8.5f,
                        areaRadius = 1.8f, color = new Color(0.45f, 0.25f, 0.85f)
                    };
                case UnitClass.Healer:
                    return new UnitStats
                    {
                        unitClass = unitClass, displayName = "Curandeira", role = TroopRole.Back, attackKind = AttackKind.Heal,
                        damageType = DamageType.Magic, rarity = TroopRarity.Rare, maxHealth = 24f, damage = 7f, attackRange = 6f,
                        attackInterval = 1.3f, armor = 6f, critChance = 0f, critMultiplier = 1f, moveSpeed = 8.5f,
                        color = new Color(0.95f, 0.92f, 0.8f)
                    };
                case UnitClass.Knight:
                    return new UnitStats
                    {
                        unitClass = unitClass, displayName = "Cavaleiro", role = TroopRole.Front, attackKind = AttackKind.Melee,
                        rarity = TroopRarity.Epic, maxHealth = 90f, damage = 20f, attackRange = 1.5f, attackInterval = 1.1f,
                        armor = 40f, critChance = 0.15f, critMultiplier = 1.8f, moveSpeed = 10.5f, chargeMultiplier = 2.5f,
                        bodyRadius = 0.5f, color = new Color(0.3f, 0.36f, 0.62f)
                    };
                default:
                    return new UnitStats
                    {
                        unitClass = UnitClass.Warrior, displayName = "Guerreiro", role = TroopRole.Front, attackKind = AttackKind.Melee,
                        maxHealth = 45f, damage = 12f, attackRange = 1.2f, attackInterval = 0.8f, armor = 30f, critChance = 0.15f,
                        critMultiplier = 1.8f, moveSpeed = 8.5f, color = new Color(0.22f, 0.4f, 0.85f)
                    };
            }
        }

        // Ordem dos pesos: Archer, Warrior, Lancer, Mage, Healer, Knight.
        public static List<CompositionPreset> DefaultCompositions()
        {
            return new List<CompositionPreset>
            {
                new CompositionPreset { displayName = "Equilibrada", weights = new[] { 0.35f, 0.3f, 0.12f, 0.12f, 0.08f, 0.03f } },
                new CompositionPreset { displayName = "Arqueiros", weights = new[] { 0.6f, 0.2f, 0.05f, 0.1f, 0.05f, 0f } },
                new CompositionPreset { displayName = "Muralha", weights = new[] { 0.2f, 0.4f, 0.25f, 0f, 0.1f, 0.05f } },
                new CompositionPreset { displayName = "Arcana", weights = new[] { 0.15f, 0.25f, 0.05f, 0.35f, 0.2f, 0f } }
            };
        }
    }
}
