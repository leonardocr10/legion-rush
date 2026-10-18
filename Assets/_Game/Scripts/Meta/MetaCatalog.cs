using System.Collections.Generic;
using UnityEngine;

namespace LegionRush
{
    public enum BonusType { AllDamage, ClassDamage, AllHealth, ClassHealth, StartTroops, AttackSpeed, Heal, Gold, Armor }
    public enum EquipSlot { Weapon, Armor, Relic }
    public enum MissionMetric { PlayLevels, WinLevels, KillEnemies, EarnGold, UseSkills, DefeatBosses, EarnStars }
    public enum AchievementMetric { Victories, BossesDefeated, TotalKills, BestArmy, TotalStars, HeroesOwned }

    public class Bonus
    {
        public BonusType Type;
        public UnitClass Class;
        public float Value;
        public Bonus(BonusType type, float value, UnitClass unitClass = UnitClass.Archer) { Type = type; Value = value; Class = unitClass; }

        public string Describe(float scale = 1f)
        {
            float v = Value * scale;
            string pct = $"+{Mathf.RoundToInt(v * 100f)}%";
            return Type switch
            {
                BonusType.AllDamage => $"{pct} dano de todas as tropas",
                BonusType.ClassDamage => $"{pct} dano de {ArmyConfig.ClassName(Class)}",
                BonusType.AllHealth => $"{pct} vida de todas as tropas",
                BonusType.ClassHealth => $"{pct} vida de {ArmyConfig.ClassName(Class)}",
                BonusType.StartTroops => $"+{Mathf.RoundToInt(v)} tropas iniciais",
                BonusType.AttackSpeed => $"{pct} velocidade de ataque",
                BonusType.Heal => $"{pct} cura",
                BonusType.Gold => $"{pct} ouro",
                _ => $"{pct} armadura"
            };
        }
    }

    public class UpgradeDef
    {
        public string Id, Name;
        public Bonus PerLevel;
        public int MaxLevel, BaseCost;
        public float Growth = 1.45f;
        public int CostFor(int currentLevel) => Mathf.RoundToInt(BaseCost * Mathf.Pow(Growth, currentLevel));
    }

    public class EquipmentDef
    {
        public string Id, Name;
        public EquipSlot Slot;
        public TroopRarity Rarity;
        public Bonus Bonus;
        public int Gold, Gems;
    }

    public enum HeroRole { Tank, Damage, Support }

    public class HeroDef
    {
        public string Id, Name, Title;
        public UnitClass Class;
        public HeroRole Role;
        public string Portrait, LockedPortrait, Showcase;
        public TroopRarity Rarity;
        public Bonus Passive;
        public float PerLevel;
        public int Gold, Gems;
        public float PassiveAt(int level) => Passive.Value * (1f + PerLevel * (level - 1));
        public int LevelCost(int level) => Mathf.RoundToInt(300f * Mathf.Pow(1.5f, level - 1));
    }

    public class SkinDef
    {
        public string Id, Name;
        public Color Tint;
        public int Gems;
    }

    public class PotionDef
    {
        public string Id, Name;
        public Bonus Effect;
        public int Gold;
    }

    public class ChestDef
    {
        public string Id, Name;
        public int Gold, Gems, MinGold, MaxGold, Equipment, Potions;
        public float EquipmentChance;
    }

    public class MissionDef
    {
        public string Id, Title;
        public MissionMetric Metric;
        public int Target, RewardGold, RewardGems;
        public bool Weekly;
    }

    public class AchievementDef
    {
        public string Id, Title, Description;
        public AchievementMetric Metric;
        public int Target, RewardGems;
    }

    public class DailyRewardDef
    {
        public int Gold, Gems;
        public string Potion, Chest;
        public string Describe() => Chest != null ? "Baú " + MetaCatalog.Chest(Chest).Name
            : Potion != null ? MetaCatalog.Potion(Potion).Name
            : Gems > 0 ? $"{Gems} gemas" : $"{Gold} ouro";
    }

    // Conteúdo da meta-progressão. Valores de balanceamento centralizados aqui.
    public static class MetaCatalog
    {
        public static readonly int[] ClassUnlockGold = { 0, 0, 500, 800, 1000, 0 };
        public static readonly int[] ClassUnlockGems = { 0, 0, 0, 0, 0, 30 };
        public static readonly int[] SkillUnlockGold = { 0, 0, 600, 0, 0, 900, 0 };
        public static readonly int[] SkillUnlockGems = { 0, 0, 0, 0, 20, 0, 0 };

        public static int TroopLevelCost(int level) => Mathf.RoundToInt(100f * Mathf.Pow(1.5f, level - 1));

        public static readonly List<UpgradeDef> Upgrades = new List<UpgradeDef>
        {
            new UpgradeDef { Id = "archer_dmg", Name = "Dano dos Arqueiros", PerLevel = new Bonus(BonusType.ClassDamage, 0.08f, UnitClass.Archer), MaxLevel = 10, BaseCost = 150 },
            new UpgradeDef { Id = "warrior_hp", Name = "Vida dos Guerreiros", PerLevel = new Bonus(BonusType.ClassHealth, 0.1f, UnitClass.Warrior), MaxLevel = 10, BaseCost = 150 },
            new UpgradeDef { Id = "start_troops", Name = "Tropas Iniciais", PerLevel = new Bonus(BonusType.StartTroops, 2f), MaxLevel = 10, BaseCost = 250 },
            new UpgradeDef { Id = "magic_dmg", Name = "Dano Mágico", PerLevel = new Bonus(BonusType.ClassDamage, 0.1f, UnitClass.Mage), MaxLevel = 10, BaseCost = 200 },
            new UpgradeDef { Id = "atk_speed", Name = "Velocidade de Ataque", PerLevel = new Bonus(BonusType.AttackSpeed, 0.04f), MaxLevel = 8, BaseCost = 300 },
            new UpgradeDef { Id = "hero_power", Name = "Poder do Herói", PerLevel = new Bonus(BonusType.AllDamage, 0f), MaxLevel = 10, BaseCost = 300 },
            new UpgradeDef { Id = "gold_gain", Name = "Bônus de Ouro", PerLevel = new Bonus(BonusType.Gold, 0.05f), MaxLevel = 10, BaseCost = 400 }
        };

        public static readonly List<EquipmentDef> Equipment = new List<EquipmentDef>
        {
            new EquipmentDef { Id = "espada_aco", Name = "Espada de Aço", Slot = EquipSlot.Weapon, Rarity = TroopRarity.Common, Bonus = new Bonus(BonusType.AllDamage, 0.05f), Gold = 400 },
            new EquipmentDef { Id = "arco_elfico", Name = "Arco Élfico", Slot = EquipSlot.Weapon, Rarity = TroopRarity.Rare, Bonus = new Bonus(BonusType.ClassDamage, 0.12f, UnitClass.Archer), Gold = 900 },
            new EquipmentDef { Id = "cajado_rubi", Name = "Cajado de Rubi", Slot = EquipSlot.Weapon, Rarity = TroopRarity.Epic, Bonus = new Bonus(BonusType.ClassDamage, 0.15f, UnitClass.Mage), Gems = 30 },
            new EquipmentDef { Id = "cota_malha", Name = "Cota de Malha", Slot = EquipSlot.Armor, Rarity = TroopRarity.Common, Bonus = new Bonus(BonusType.AllHealth, 0.08f), Gold = 500 },
            new EquipmentDef { Id = "armadura_real", Name = "Armadura Real", Slot = EquipSlot.Armor, Rarity = TroopRarity.Rare, Bonus = new Bonus(BonusType.AllHealth, 0.15f), Gold = 1200 },
            new EquipmentDef { Id = "manto_sagrado", Name = "Manto Sagrado", Slot = EquipSlot.Armor, Rarity = TroopRarity.Epic, Bonus = new Bonus(BonusType.Heal, 0.2f), Gems = 35 },
            new EquipmentDef { Id = "estandarte", Name = "Estandarte da Legião", Slot = EquipSlot.Relic, Rarity = TroopRarity.Rare, Bonus = new Bonus(BonusType.StartTroops, 3f), Gold = 800 },
            new EquipmentDef { Id = "amuleto_ouro", Name = "Amuleto de Ouro", Slot = EquipSlot.Relic, Rarity = TroopRarity.Epic, Bonus = new Bonus(BonusType.Gold, 0.15f), Gems = 25 },
            new EquipmentDef { Id = "ampulheta", Name = "Ampulheta Arcana", Slot = EquipSlot.Relic, Rarity = TroopRarity.Legendary, Bonus = new Bonus(BonusType.AttackSpeed, 0.08f), Gems = 45 }
        };

        public static readonly List<HeroDef> Heroes = new List<HeroDef>
        {
            new HeroDef { Id = "arthor", Name = "Rei Supremo", Title = "Guardião Real", Class = UnitClass.Warrior, Role = HeroRole.Tank, Rarity = TroopRarity.Epic, Passive = new Bonus(BonusType.Armor, 0.1f), PerLevel = 0.2f, Portrait = "Portrait_King", Showcase = "Showcase_King" },
            new HeroDef { Id = "lyra", Name = "Arqueira Real", Title = "Arqueira Celestial", Class = UnitClass.Archer, Role = HeroRole.Damage, Rarity = TroopRarity.Epic, Passive = new Bonus(BonusType.ClassDamage, 0.15f, UnitClass.Archer), PerLevel = 0.2f, Gold = 1500, Portrait = "Portrait_Archer" },
            new HeroDef { Id = "kael", Name = "Cavaleiro", Title = "Guerreiro das Sombras", Class = UnitClass.Knight, Role = HeroRole.Tank, Rarity = TroopRarity.Legendary, Passive = new Bonus(BonusType.AllDamage, 0.1f), PerLevel = 0.2f, Gold = 2500, Portrait = "Portrait_Knight" },
            new HeroDef { Id = "morvan", Name = "Mago Arcano", Title = "Mago Arcano", Class = UnitClass.Mage, Role = HeroRole.Damage, Rarity = TroopRarity.Legendary, Passive = new Bonus(BonusType.ClassDamage, 0.2f, UnitClass.Mage), PerLevel = 0.2f, Gems = 40, Portrait = "Portrait_Mage", Showcase = "Showcase_Mage" },
            new HeroDef { Id = "doran", Name = "Lanceiro", Title = "Muralha de Lanças", Class = UnitClass.Lancer, Role = HeroRole.Tank, Rarity = TroopRarity.Rare, Passive = new Bonus(BonusType.ClassHealth, 0.15f, UnitClass.Lancer), PerLevel = 0.2f, Gold = 1200, Portrait = "Portrait_Lancer" },
            new HeroDef { Id = "elara", Name = "Curandeira", Title = "Sacerdotisa da Luz", Class = UnitClass.Healer, Role = HeroRole.Support, Rarity = TroopRarity.Epic, Passive = new Bonus(BonusType.Heal, 0.25f), PerLevel = 0.2f, Gems = 50, Portrait = "Portrait_Healer" },
            new HeroDef { Id = "galen", Name = "Paladino", Title = "Escudo Sagrado", Class = UnitClass.Knight, Role = HeroRole.Tank, Rarity = TroopRarity.Legendary, Passive = new Bonus(BonusType.AllHealth, 0.12f), PerLevel = 0.2f, Gems = 80, Portrait = "Portrait_PaladinLocked", LockedPortrait = "Portrait_PaladinLocked" },
            new HeroDef { Id = "vex", Name = "Assassino", Title = "Lâmina Oculta", Class = UnitClass.Warrior, Role = HeroRole.Damage, Rarity = TroopRarity.Legendary, Passive = new Bonus(BonusType.AttackSpeed, 0.12f), PerLevel = 0.2f, Gold = 4000, Portrait = "Portrait_AssassinLocked", LockedPortrait = "Portrait_AssassinLocked" },
            new HeroDef { Id = "sylva", Name = "Atiradora Élfica", Title = "Olho da Floresta", Class = UnitClass.Archer, Role = HeroRole.Damage, Rarity = TroopRarity.Legendary, Passive = new Bonus(BonusType.ClassDamage, 0.25f, UnitClass.Archer), PerLevel = 0.2f, Gems = 100, Portrait = "Portrait_ElfLocked", LockedPortrait = "Portrait_ElfLocked" }
        };

        public static readonly List<SkinDef> Skins = new List<SkinDef>
        {
            new SkinDef { Id = "default", Name = "Original", Tint = Color.white },
            new SkinDef { Id = "esmeralda", Name = "Esmeralda", Tint = new Color(0.3f, 0.85f, 0.45f), Gems = 40 },
            new SkinDef { Id = "carmesim", Name = "Carmesim", Tint = new Color(0.9f, 0.3f, 0.35f), Gems = 40 },
            new SkinDef { Id = "sombra", Name = "Sombra", Tint = new Color(0.35f, 0.3f, 0.45f), Gems = 60 },
            new SkinDef { Id = "solar", Name = "Solar", Tint = new Color(1f, 0.8f, 0.35f), Gems = 60 }
        };

        public static readonly List<PotionDef> Potions = new List<PotionDef>
        {
            new PotionDef { Id = "pocao_ataque", Name = "Poção de Ataque", Effect = new Bonus(BonusType.AllDamage, 0.2f), Gold = 150 },
            new PotionDef { Id = "pocao_vida", Name = "Poção de Vida", Effect = new Bonus(BonusType.AllHealth, 0.2f), Gold = 150 },
            new PotionDef { Id = "pocao_veloc", Name = "Poção de Velocidade", Effect = new Bonus(BonusType.AttackSpeed, 0.15f), Gold = 150 }
        };

        public static readonly List<ChestDef> Chests = new List<ChestDef>
        {
            new ChestDef { Id = "comum", Name = "Comum", Gold = 300, MinGold = 150, MaxGold = 320, Potions = 1, EquipmentChance = 0.1f },
            new ChestDef { Id = "raro", Name = "Raro", Gems = 25, MinGold = 400, MaxGold = 800, Potions = 1, Equipment = 1, EquipmentChance = 0.5f },
            new ChestDef { Id = "epico", Name = "Épico", Gems = 60, MinGold = 1000, MaxGold = 2000, Potions = 2, Equipment = 1, EquipmentChance = 1f }
        };

        public static readonly List<MissionDef> Missions = new List<MissionDef>
        {
            new MissionDef { Id = "d_play3", Title = "Jogue 3 fases", Metric = MissionMetric.PlayLevels, Target = 3, RewardGold = 200 },
            new MissionDef { Id = "d_kill150", Title = "Derrote 150 inimigos", Metric = MissionMetric.KillEnemies, Target = 150, RewardGold = 250 },
            new MissionDef { Id = "d_gold500", Title = "Ganhe 500 ouro", Metric = MissionMetric.EarnGold, Target = 500, RewardGems = 5 },
            new MissionDef { Id = "d_skills5", Title = "Use 5 habilidades", Metric = MissionMetric.UseSkills, Target = 5, RewardGold = 200 },
            new MissionDef { Id = "w_win10", Title = "Vença 10 fases", Metric = MissionMetric.WinLevels, Target = 10, RewardGems = 30, Weekly = true },
            new MissionDef { Id = "w_boss3", Title = "Derrote 3 chefões", Metric = MissionMetric.DefeatBosses, Target = 3, RewardGems = 40, Weekly = true },
            new MissionDef { Id = "w_stars15", Title = "Conquiste 15 estrelas", Metric = MissionMetric.EarnStars, Target = 15, RewardGold = 1000, Weekly = true }
        };

        public static readonly List<AchievementDef> Achievements = new List<AchievementDef>
        {
            new AchievementDef { Id = "primeira_vitoria", Title = "Primeira Vitória", Description = "Vença uma fase", Metric = AchievementMetric.Victories, Target = 1, RewardGems = 10 },
            new AchievementDef { Id = "veterano", Title = "Veterano", Description = "Vença 10 fases", Metric = AchievementMetric.Victories, Target = 10, RewardGems = 25 },
            new AchievementDef { Id = "cacador", Title = "Caçador de Chefões", Description = "Derrote um chefão", Metric = AchievementMetric.BossesDefeated, Target = 1, RewardGems = 20 },
            new AchievementDef { Id = "matador", Title = "Matador de Lendas", Description = "Derrote 5 chefões", Metric = AchievementMetric.BossesDefeated, Target = 5, RewardGems = 50 },
            new AchievementDef { Id = "exterminador", Title = "Exterminador", Description = "Derrote 1000 inimigos", Metric = AchievementMetric.TotalKills, Target = 1000, RewardGems = 30 },
            new AchievementDef { Id = "legiao", Title = "Legião Completa", Description = "Tenha 150 soldados ao mesmo tempo", Metric = AchievementMetric.BestArmy, Target = 150, RewardGems = 30 },
            new AchievementDef { Id = "estrategista", Title = "Estrategista", Description = "Conquiste 30 estrelas", Metric = AchievementMetric.TotalStars, Target = 30, RewardGems = 40 },
            new AchievementDef { Id = "colecionador", Title = "Colecionador", Description = "Tenha 3 heróis", Metric = AchievementMetric.HeroesOwned, Target = 3, RewardGems = 30 }
        };

        public static readonly DailyRewardDef[] Daily =
        {
            new DailyRewardDef { Gold = 200 },
            new DailyRewardDef { Gems = 10 },
            new DailyRewardDef { Gold = 400 },
            new DailyRewardDef { Potion = "pocao_ataque" },
            new DailyRewardDef { Gold = 600 },
            new DailyRewardDef { Gems = 25 },
            new DailyRewardDef { Chest = "epico" }
        };

        public static UpgradeDef Upgrade(string id) => Upgrades.Find(u => u.Id == id);
        public static EquipmentDef Equip(string id) => Equipment.Find(e => e.Id == id);
        public static HeroDef Hero(string id) => Heroes.Find(h => h.Id == id);
        public static SkinDef Skin(string id) => Skins.Find(s => s.Id == id);
        public static PotionDef Potion(string id) => Potions.Find(p => p.Id == id);
        public static ChestDef Chest(string id) => Chests.Find(c => c.Id == id);

        public static string RarityName(TroopRarity rarity) => rarity switch
        {
            TroopRarity.Rare => "Raro",
            TroopRarity.Epic => "Épico",
            TroopRarity.Legendary => "Lendário",
            _ => "Comum"
        };

        public static Color RarityColor(TroopRarity rarity) => rarity switch
        {
            TroopRarity.Rare => new Color(0.3f, 0.6f, 1f),
            TroopRarity.Epic => new Color(0.7f, 0.35f, 1f),
            TroopRarity.Legendary => new Color(1f, 0.7f, 0.15f),
            _ => new Color(0.65f, 0.7f, 0.78f)
        };
    }
}
