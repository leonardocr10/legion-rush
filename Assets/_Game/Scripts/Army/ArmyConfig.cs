using System;
using UnityEngine;

namespace LegionRush
{
    public enum UnitClass { Archer, Warrior }

    [Serializable]
    public class UnitStats
    {
        public UnitClass unitClass;
        [Min(1f)] public float maxHealth = 20f;
        [Min(0f)] public float damage = 8f;
        [Min(0.5f)] public float attackRange = 9f;
        [Min(0.1f)] public float attackInterval = 1f;
        [Range(0f, 1f)] public float critChance = 0.1f;
        [Min(1f)] public float critMultiplier = 2f;
        [Min(0.5f)] public float moveSpeed = 9f;
        public Color color = Color.white;
    }

    [CreateAssetMenu(fileName = "ArmyConfig", menuName = "Legion Rush/Army Config")]
    public class ArmyConfig : ScriptableObject
    {
        public UnitStats archer = new UnitStats
        {
            unitClass = UnitClass.Archer, maxHealth = 20f, damage = 9f, attackRange = 9f, attackInterval = 1f,
            critChance = 0.1f, critMultiplier = 2f, moveSpeed = 9f, color = new Color(0.2f, 0.62f, 0.32f)
        };

        public UnitStats warrior = new UnitStats
        {
            unitClass = UnitClass.Warrior, maxHealth = 45f, damage = 12f, attackRange = 1.2f, attackInterval = 0.8f,
            critChance = 0.15f, critMultiplier = 1.8f, moveSpeed = 8.5f, color = new Color(0.22f, 0.4f, 0.85f)
        };

        [Range(0f, 1f)] public float archerRatio = 0.6f;
        [Min(1)] public int startingSoldiers = 10;
        [Min(1)] public int maxSoldiers = 150;
        [Min(0.3f)] public float formationSpacing = 0.55f;
        [Min(1f)] public float warriorLeashRange = 6f;
        [Min(1f)] public float projectileSpeed = 24f;
        [Min(8)] public int projectilePoolSize = 64;

        public UnitStats Get(UnitClass unitClass) => unitClass == UnitClass.Archer ? archer : warrior;

        public void Sanitize()
        {
            archer.unitClass = UnitClass.Archer;
            warrior.unitClass = UnitClass.Warrior;
            maxSoldiers = Mathf.Max(1, maxSoldiers);
            startingSoldiers = Mathf.Clamp(startingSoldiers, 1, maxSoldiers);
            projectilePoolSize = Mathf.Max(8, projectilePoolSize);
        }

        public static ArmyConfig CreateDefault()
        {
            var config = CreateInstance<ArmyConfig>();
            config.name = "ArmyConfig_Default";
            return config;
        }
    }
}
