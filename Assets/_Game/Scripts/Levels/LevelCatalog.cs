using System.Collections.Generic;
using UnityEngine;

namespace LegionRush
{
    // Campanha: 30 fases distribuídas em 4 mundos. Fases ausentes aparecem como "em breve" na seleção.
    [CreateAssetMenu(fileName = "LevelCatalog", menuName = "Legion Rush/Level Catalog")]
    public class LevelCatalog : ScriptableObject
    {
        public const int TotalLevels = 30;

        public List<LevelConfig> levels = new List<LevelConfig>();

        public static (int first, int last) WorldRange(WorldId world) => world switch
        {
            WorldId.LostDesert => (9, 15),
            WorldId.EnchantedForest => (16, 22),
            WorldId.VolcanicRealm => (23, 30),
            _ => (1, 8)
        };

        public static WorldId WorldOf(int levelNumber)
        {
            if (levelNumber >= 23) return WorldId.VolcanicRealm;
            if (levelNumber >= 16) return WorldId.EnchantedForest;
            if (levelNumber >= 9) return WorldId.LostDesert;
            return WorldId.FrozenKingdom;
        }

        public LevelConfig Get(int levelNumber)
        {
            foreach (LevelConfig level in levels)
            {
                if (level != null && level.levelNumber == levelNumber) return level;
            }
            return null;
        }

        public bool Has(int levelNumber) => Get(levelNumber) != null;

        public int HighestAvailable()
        {
            int highest = 0;
            foreach (LevelConfig level in levels)
            {
                if (level != null) highest = Mathf.Max(highest, level.levelNumber);
            }
            return highest;
        }

        public static LevelCatalog CreateDefault()
        {
            var catalog = CreateInstance<LevelCatalog>();
            catalog.name = "LevelCatalog_Default";
            catalog.levels.Add(LevelConfig.CreateDefault());
            return catalog;
        }
    }
}
