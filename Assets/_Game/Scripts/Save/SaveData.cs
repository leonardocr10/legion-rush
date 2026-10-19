using System;
using System.Collections.Generic;

namespace LegionRush
{
    // Formato persistido (JSON). Ao mudar a estrutura: incremente CurrentVersion e adicione uma migração em SaveMigrations.
    [Serializable]
    public class SaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public long createdUtcTicks;
        public long updatedUtcTicks;
        public string playerName = string.Empty;   // vazio = "Comandante"
        public bool playerNameAsked;               // perguntado no primeiro acesso
        public WalletData wallet = new WalletData();
        public ProgressData progress = new ProgressData();
        public TroopData troops = new TroopData();
        public HeroData heroes = new HeroData();
        public InventoryData inventory = new InventoryData();
        public SkillData skills = new SkillData();
        public MissionData missions = new MissionData();
        public AchievementData achievements = new AchievementData();
        public DailyRewardData daily = new DailyRewardData();
        public RecordData records = new RecordData();
        public SettingsData settings = new SettingsData();
        public List<ItemStack> upgrades = new List<ItemStack>();
        public CosmeticData cosmetics = new CosmeticData();

        public static SaveData CreateNew()
        {
            var data = new SaveData { createdUtcTicks = DateTime.UtcNow.Ticks };
            data.Normalize();
            return data;
        }

        // Garante listas e tamanhos válidos depois de carregar ou migrar.
        public void Normalize()
        {
            wallet ??= new WalletData();
            progress ??= new ProgressData();
            troops ??= new TroopData();
            heroes ??= new HeroData();
            inventory ??= new InventoryData();
            skills ??= new SkillData();
            missions ??= new MissionData();
            achievements ??= new AchievementData();
            daily ??= new DailyRewardData();
            records ??= new RecordData();
            settings ??= new SettingsData();
            upgrades ??= new List<ItemStack>();
            cosmetics ??= new CosmeticData();
            cosmetics.owned ??= new List<string>();
            if (!cosmetics.owned.Contains("default")) cosmetics.owned.Add("default");
            if (string.IsNullOrEmpty(cosmetics.selected)) cosmetics.selected = "default";
            inventory.activePotions ??= new List<string>();

            wallet.gold = Math.Max(0, wallet.gold);
            wallet.gems = Math.Max(0, wallet.gems);
            progress.levels ??= new List<LevelRecord>();
            progress.highestUnlocked = Math.Max(1, progress.highestUnlocked);
            troops.levels = Resize(troops.levels, ArmyConfig.ClassCount, 1);
            for (int i = 0; i < troops.levels.Length; i++) troops.levels[i] = Math.Max(1, troops.levels[i]);
            troops.unlocked = Resize(troops.unlocked, ArmyConfig.ClassCount, false);
            troops.unlocked[(int)UnitClass.Archer] = true;
            troops.unlocked[(int)UnitClass.Warrior] = true;
            heroes.owned ??= new List<HeroRecord>();
            if (heroes.owned.Count == 0) heroes.owned.Add(new HeroRecord { id = "arthor", level = 1 });
            if (string.IsNullOrEmpty(heroes.selected)) heroes.selected = "arthor";
            inventory.items ??= new List<ItemStack>();
            inventory.equipped ??= new List<string>();
            skills.loadout = Resize(skills.loadout, SkillSystem.SlotCount, -1);
            skills.unlocked = Resize(skills.unlocked, SkillDefinition.Count, false);
            foreach (SkillId id in SkillSystem.DefaultLoadout) skills.unlocked[(int)id] = true;
            missions.entries ??= new List<MissionProgress>();
            achievements.unlocked ??= new List<string>();
        }

        private static T[] Resize<T>(T[] source, int length, T fill)
        {
            if (source != null && source.Length == length) return source;
            var result = new T[length];
            for (int i = 0; i < length; i++) result[i] = source != null && i < source.Length ? source[i] : fill;
            return result;
        }
    }

    [Serializable] public class WalletData { public int gold; public int gems; }

    [Serializable]
    public class ProgressData
    {
        public int highestUnlocked = 1;
        public List<LevelRecord> levels = new List<LevelRecord>();

        public LevelRecord Get(int levelNumber, bool create)
        {
            foreach (LevelRecord record in levels)
            {
                if (record.levelNumber == levelNumber) return record;
            }
            if (!create) return null;
            var added = new LevelRecord { levelNumber = levelNumber };
            levels.Add(added);
            return added;
        }

        public int TotalStars()
        {
            int total = 0;
            foreach (LevelRecord record in levels) total += record.stars;
            return total;
        }
    }

    [Serializable]
    public class LevelRecord
    {
        public int levelNumber;
        public bool completed;
        public int stars;
        public int bestScore;
        public float bestTimeSeconds;
        public int attempts;
    }

    [Serializable]
    public class TroopData
    {
        public int[] levels = { 1, 1, 1, 1, 1, 1 };
        public bool[] unlocked = { true, true, false, false, false, false };
    }

    [Serializable] public class HeroRecord { public string id; public int level = 1; public int shards; }
    [Serializable]
    public class HeroData
    {
        public string selected = string.Empty;
        public List<HeroRecord> owned = new List<HeroRecord>();

        public HeroRecord Get(string id)
        {
            foreach (HeroRecord hero in owned)
            {
                if (hero.id == id) return hero;
            }
            return null;
        }
    }
    [Serializable] public class ItemStack { public string id; public int count; }
    [Serializable]
    public class InventoryData
    {
        public List<ItemStack> items = new List<ItemStack>();
        public List<string> equipped = new List<string>();
        public List<string> activePotions = new List<string>();

        public int Count(string id)
        {
            foreach (ItemStack stack in items)
            {
                if (stack.id == id) return stack.count;
            }
            return 0;
        }

        public void Add(string id, int amount)
        {
            foreach (ItemStack stack in items)
            {
                if (stack.id != id) continue;
                stack.count = Math.Max(0, stack.count + amount);
                if (stack.count == 0) items.Remove(stack);
                return;
            }
            if (amount > 0) items.Add(new ItemStack { id = id, count = amount });
        }
    }

    [Serializable] public class CosmeticData { public List<string> owned = new List<string>(); public string selected = "default"; }
    [Serializable] public class SkillData { public int[] loadout = { -1, -1, -1, -1 }; public bool[] unlocked = new bool[SkillDefinition.Count]; }
    [Serializable] public class MissionProgress { public string id; public int progress; public bool claimed; public long periodStartTicks; }
    [Serializable] public class MissionData { public List<MissionProgress> entries = new List<MissionProgress>(); }
    [Serializable] public class AchievementData { public List<string> unlocked = new List<string>(); }
    [Serializable] public class DailyRewardData { public long lastClaimUtcTicks; public int streak; }

    [Serializable]
    public class RecordData
    {
        public int totalKills;
        public int bossesDefeated;
        public int bestArmySize;
        public int levelsPlayed;
        public int victories;
        public long totalGoldEarned;
    }

    [Serializable]
    public class SettingsData
    {
        public int composition;
        public int lastWorld;
        public bool haptics = true;
    }

    public static class SaveMigrations
    {
        // Cada passo leva o JSON da versão N para N+1. Hoje só existe a versão 1.
        public static SaveData Migrate(SaveData data, int fromVersion)
        {
            if (fromVersion < 1) data.version = 1;
            data.version = SaveData.CurrentVersion;
            data.Normalize();
            return data;
        }
    }
}
