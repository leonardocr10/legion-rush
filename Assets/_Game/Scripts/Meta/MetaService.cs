using System;
using System.Collections.Generic;
using UnityEngine;

namespace LegionRush
{
    // Bônus permanentes e temporários consolidados (melhorias, equipamentos, herói, poções) aplicados no início da fase.
    public class MetaBonuses
    {
        public readonly float[] ClassDamage = { 1f, 1f, 1f, 1f, 1f, 1f };
        public readonly float[] ClassHealth = { 1f, 1f, 1f, 1f, 1f, 1f };
        public float AllDamage = 1f;
        public float AllHealth = 1f;
        public float AttackSpeed;
        public float Heal = 1f;
        public float Gold = 1f;
        public float Armor = 1f;
        public int StartTroops;

        public void Add(Bonus bonus, float scale = 1f)
        {
            if (bonus == null) return;
            float v = bonus.Value * scale;
            switch (bonus.Type)
            {
                case BonusType.AllDamage: AllDamage += v; break;
                case BonusType.ClassDamage: ClassDamage[(int)bonus.Class] += v; break;
                case BonusType.AllHealth: AllHealth += v; break;
                case BonusType.ClassHealth: ClassHealth[(int)bonus.Class] += v; break;
                case BonusType.StartTroops: StartTroops += Mathf.RoundToInt(v); break;
                case BonusType.AttackSpeed: AttackSpeed += v; break;
                case BonusType.Heal: Heal += v; break;
                case BonusType.Gold: Gold += v; break;
                case BonusType.Armor: Armor += v; break;
            }
        }
    }

    public class RewardBundle
    {
        public int Gold;
        public int Gems;
        public readonly List<string> Items = new List<string>();
        public readonly List<string> Lines = new List<string>();
    }

    public enum PurchaseResult { Success, NotEnoughCurrency, AlreadyOwned, MaxLevel, Locked, Invalid }

    // Todas as operações de meta-progressão. Cada compra valida saldo e grava de forma atômica (Economy.TrySpend).
    public static class MetaService
    {
        public static event Action Changed;

        private static SaveData Save => SaveManager.Current;

        // ---------- Bônus ----------

        public static MetaBonuses ComputeBonuses(bool includePotions)
        {
            var bonuses = new MetaBonuses();
            foreach (UpgradeDef upgrade in MetaCatalog.Upgrades)
            {
                int level = UpgradeLevel(upgrade.Id);
                if (level > 0 && upgrade.Id != "hero_power") bonuses.Add(upgrade.PerLevel, level);
            }
            foreach (string id in Save.inventory.equipped)
            {
                EquipmentDef equipment = MetaCatalog.Equip(id);
                if (equipment != null) bonuses.Add(equipment.Bonus);
            }
            HeroDef hero = MetaCatalog.Hero(Save.heroes.selected);
            HeroRecord record = hero != null ? Save.heroes.Get(hero.Id) : null;
            if (hero != null && record != null)
            {
                float heroPower = 1f + 0.1f * UpgradeLevel("hero_power");
                bonuses.Add(hero.Passive, (1f + hero.PerLevel * (record.level - 1)) * heroPower);
            }
            if (includePotions)
            {
                foreach (string id in Save.inventory.activePotions)
                {
                    PotionDef potion = MetaCatalog.Potion(id);
                    if (potion != null) bonuses.Add(potion.Effect);
                }
            }
            return bonuses;
        }

        public static void ConsumeActivePotions()
        {
            if (Save.inventory.activePotions.Count == 0) return;
            Save.inventory.activePotions.Clear();
            SaveManager.MarkDirty();
            Changed?.Invoke();
        }

        // ---------- Melhorias e tropas ----------

        public static int UpgradeLevel(string id)
        {
            foreach (ItemStack stack in Save.upgrades)
            {
                if (stack.id == id) return stack.count;
            }
            return 0;
        }

        public static PurchaseResult BuyUpgrade(string id)
        {
            UpgradeDef upgrade = MetaCatalog.Upgrade(id);
            if (upgrade == null) return PurchaseResult.Invalid;
            int level = UpgradeLevel(id);
            if (level >= upgrade.MaxLevel) return PurchaseResult.MaxLevel;
            if (!Economy.TrySpend(upgrade.CostFor(level), 0, "melhoria " + upgrade.Name)) return PurchaseResult.NotEnoughCurrency;
            ItemStack stack = Save.upgrades.Find(s => s.id == id);
            if (stack == null) Save.upgrades.Add(new ItemStack { id = id, count = 1 });
            else stack.count++;
            return Commit();
        }

        public static bool IsClassUnlocked(UnitClass unitClass) => Save.troops.unlocked[(int)unitClass];

        public static PurchaseResult UnlockClass(UnitClass unitClass)
        {
            int c = (int)unitClass;
            if (Save.troops.unlocked[c]) return PurchaseResult.AlreadyOwned;
            if (!Economy.TrySpend(MetaCatalog.ClassUnlockGold[c], MetaCatalog.ClassUnlockGems[c], "desbloquear " + ArmyConfig.ClassName(unitClass)))
                return PurchaseResult.NotEnoughCurrency;
            Save.troops.unlocked[c] = true;
            return Commit();
        }

        public static PurchaseResult UpgradeTroop(UnitClass unitClass, int maxLevel)
        {
            int c = (int)unitClass;
            if (!Save.troops.unlocked[c]) return PurchaseResult.Locked;
            int level = Save.troops.levels[c];
            if (level >= maxLevel) return PurchaseResult.MaxLevel;
            if (!Economy.TrySpend(MetaCatalog.TroopLevelCost(level), 0, "nível " + ArmyConfig.ClassName(unitClass))) return PurchaseResult.NotEnoughCurrency;
            Save.troops.levels[c] = level + 1;
            return Commit();
        }

        // ---------- Habilidades ----------

        public static bool IsSkillUnlocked(SkillId id) => Save.skills.unlocked[(int)id];

        public static PurchaseResult UnlockSkill(SkillId id)
        {
            int s = (int)id;
            if (Save.skills.unlocked[s]) return PurchaseResult.AlreadyOwned;
            if (!Economy.TrySpend(MetaCatalog.SkillUnlockGold[s], MetaCatalog.SkillUnlockGems[s], "habilidade " + id)) return PurchaseResult.NotEnoughCurrency;
            Save.skills.unlocked[s] = true;
            return Commit();
        }

        // ---------- Equipamentos, poções, skins ----------

        public static bool OwnsItem(string id) => Save.inventory.Count(id) > 0;
        public static bool IsEquipped(string id) => Save.inventory.equipped.Contains(id);

        public static PurchaseResult BuyEquipment(string id)
        {
            EquipmentDef equipment = MetaCatalog.Equip(id);
            if (equipment == null) return PurchaseResult.Invalid;
            if (OwnsItem(id)) return PurchaseResult.AlreadyOwned;
            if (!Economy.TrySpend(equipment.Gold, equipment.Gems, "equipamento " + equipment.Name)) return PurchaseResult.NotEnoughCurrency;
            Save.inventory.Add(id, 1);
            return Commit();
        }

        public static void ToggleEquip(string id)
        {
            EquipmentDef equipment = MetaCatalog.Equip(id);
            if (equipment == null || !OwnsItem(id)) return;
            if (Save.inventory.equipped.Remove(id))
            {
                Commit();
                return;
            }
            Save.inventory.equipped.RemoveAll(other => MetaCatalog.Equip(other) is EquipmentDef e && e.Slot == equipment.Slot);
            Save.inventory.equipped.Add(id);
            Commit();
        }

        public static PurchaseResult BuyPotion(string id)
        {
            PotionDef potion = MetaCatalog.Potion(id);
            if (potion == null) return PurchaseResult.Invalid;
            if (!Economy.TrySpend(potion.Gold, 0, potion.Name)) return PurchaseResult.NotEnoughCurrency;
            Save.inventory.Add(id, 1);
            return Commit();
        }

        public static bool ActivatePotion(string id)
        {
            if (Save.inventory.Count(id) <= 0 || Save.inventory.activePotions.Contains(id)) return false;
            Save.inventory.Add(id, -1);
            Save.inventory.activePotions.Add(id);
            Commit();
            return true;
        }

        public static PurchaseResult BuySkin(string id)
        {
            SkinDef skin = MetaCatalog.Skin(id);
            if (skin == null) return PurchaseResult.Invalid;
            if (Save.cosmetics.owned.Contains(id)) return PurchaseResult.AlreadyOwned;
            if (!Economy.TrySpend(0, skin.Gems, "visual " + skin.Name)) return PurchaseResult.NotEnoughCurrency;
            Save.cosmetics.owned.Add(id);
            return Commit();
        }

        public static void SelectSkin(string id)
        {
            if (!Save.cosmetics.owned.Contains(id)) return;
            Save.cosmetics.selected = id;
            Commit();
        }

        public static Color SkinTint()
        {
            SkinDef skin = MetaCatalog.Skin(Save.cosmetics.selected);
            return skin != null ? skin.Tint : Color.white;
        }

        // ---------- Heróis ----------

        public static PurchaseResult UnlockHero(string id)
        {
            HeroDef hero = MetaCatalog.Hero(id);
            if (hero == null) return PurchaseResult.Invalid;
            if (Save.heroes.Get(id) != null) return PurchaseResult.AlreadyOwned;
            if (!Economy.TrySpend(hero.Gold, hero.Gems, "herói " + hero.Name)) return PurchaseResult.NotEnoughCurrency;
            Save.heroes.owned.Add(new HeroRecord { id = id, level = 1 });
            return Commit();
        }

        public static PurchaseResult LevelUpHero(string id)
        {
            HeroDef hero = MetaCatalog.Hero(id);
            HeroRecord record = Save.heroes.Get(id);
            if (hero == null || record == null) return PurchaseResult.Locked;
            if (record.level >= 10) return PurchaseResult.MaxLevel;
            if (!Economy.TrySpend(hero.LevelCost(record.level), 0, "nível herói " + hero.Name)) return PurchaseResult.NotEnoughCurrency;
            record.level++;
            return Commit();
        }

        public static void SelectHero(string id)
        {
            if (Save.heroes.Get(id) == null) return;
            Save.heroes.selected = id;
            Commit();
        }

        // ---------- Baús ----------

        public static PurchaseResult BuyChest(string id, out RewardBundle reward)
        {
            reward = null;
            ChestDef chest = MetaCatalog.Chest(id);
            if (chest == null) return PurchaseResult.Invalid;
            if (!Economy.TrySpend(chest.Gold, chest.Gems, "baú " + chest.Name)) return PurchaseResult.NotEnoughCurrency;
            reward = OpenChest(chest);
            return Commit();
        }

        public static RewardBundle OpenChest(ChestDef chest)
        {
            var reward = new RewardBundle();
            var rng = new System.Random(Environment.TickCount);
            reward.Gold = rng.Next(chest.MinGold, chest.MaxGold + 1);
            reward.Lines.Add($"+{reward.Gold} ouro");
            for (int i = 0; i < chest.Potions; i++)
            {
                PotionDef potion = MetaCatalog.Potions[rng.Next(MetaCatalog.Potions.Count)];
                Save.inventory.Add(potion.Id, 1);
                reward.Items.Add(potion.Id);
                reward.Lines.Add(potion.Name);
            }
            if (rng.NextDouble() < chest.EquipmentChance)
            {
                EquipmentDef equipment = MetaCatalog.Equipment[rng.Next(MetaCatalog.Equipment.Count)];
                if (OwnsItem(equipment.Id))
                {
                    reward.Gold += 200;
                    reward.Lines.Add($"{equipment.Name} (repetido → +200 ouro)");
                }
                else
                {
                    Save.inventory.Add(equipment.Id, 1);
                    reward.Items.Add(equipment.Id);
                    reward.Lines.Add($"{equipment.Name} ({MetaCatalog.RarityName(equipment.Rarity)})");
                }
            }
            Economy.Add(reward.Gold, reward.Gems, "baú " + chest.Name);
            GameAudio.Play(SoundId.ChestOpen);
            return reward;
        }

        // ---------- Missões ----------

        public static MissionProgress Mission(MissionDef def)
        {
            long period = PeriodStart(def.Weekly);
            MissionProgress progress = Save.missions.entries.Find(m => m.id == def.Id);
            if (progress == null)
            {
                progress = new MissionProgress { id = def.Id, periodStartTicks = period };
                Save.missions.entries.Add(progress);
            }
            else if (progress.periodStartTicks != period)
            {
                progress.progress = 0;
                progress.claimed = false;
                progress.periodStartTicks = period;
            }
            return progress;
        }

        public static void ReportLevel(LevelResult result, int stars, int goldEarned)
        {
            foreach (MissionDef def in MetaCatalog.Missions)
            {
                MissionProgress progress = Mission(def);
                int add = def.Metric switch
                {
                    MissionMetric.PlayLevels => 1,
                    MissionMetric.WinLevels => result.Victory ? 1 : 0,
                    MissionMetric.KillEnemies => result.Kills,
                    MissionMetric.EarnGold => goldEarned,
                    MissionMetric.UseSkills => result.SkillsUsed,
                    MissionMetric.DefeatBosses => result.BossDefeated ? 1 : 0,
                    MissionMetric.EarnStars => stars,
                    _ => 0
                };
                progress.progress = Mathf.Min(def.Target, progress.progress + Mathf.Max(0, add));
            }
            SaveManager.MarkDirty();
            Changed?.Invoke();
        }

        public static bool ClaimMission(MissionDef def)
        {
            MissionProgress progress = Mission(def);
            if (progress.claimed || progress.progress < def.Target) return false;
            progress.claimed = true;
            Economy.Add(def.RewardGold, def.RewardGems, "missão " + def.Title);
            Commit();
            return true;
        }

        public static int ClaimableMissions()
        {
            int count = 0;
            foreach (MissionDef def in MetaCatalog.Missions)
            {
                MissionProgress progress = Mission(def);
                if (!progress.claimed && progress.progress >= def.Target) count++;
            }
            return count;
        }

        private static long PeriodStart(bool weekly)
        {
            DateTime today = DateTime.UtcNow.Date;
            if (!weekly) return today.Ticks;
            int offset = ((int)today.DayOfWeek + 6) % 7;
            return today.AddDays(-offset).Ticks;
        }

        // ---------- Conquistas ----------

        public static int AchievementValue(AchievementDef def) => def.Metric switch
        {
            AchievementMetric.Victories => Save.records.victories,
            AchievementMetric.BossesDefeated => Save.records.bossesDefeated,
            AchievementMetric.TotalKills => Save.records.totalKills,
            AchievementMetric.BestArmy => Save.records.bestArmySize,
            AchievementMetric.TotalStars => Save.progress.TotalStars(),
            AchievementMetric.HeroesOwned => Save.heroes.owned.Count,
            _ => 0
        };

        public static bool IsAchievementClaimed(AchievementDef def) => Save.achievements.unlocked.Contains(def.Id);

        public static bool ClaimAchievement(AchievementDef def)
        {
            if (IsAchievementClaimed(def) || AchievementValue(def) < def.Target) return false;
            Save.achievements.unlocked.Add(def.Id);
            Economy.Add(0, def.RewardGems, "conquista " + def.Title);
            Commit();
            return true;
        }

        public static int ClaimableAchievements()
        {
            int count = 0;
            foreach (AchievementDef def in MetaCatalog.Achievements)
            {
                if (!IsAchievementClaimed(def) && AchievementValue(def) >= def.Target) count++;
            }
            return count;
        }

        // ---------- Recompensa diária ----------

        public static bool CanClaimDaily(out int dayIndex)
        {
            DateTime today = DateTime.UtcNow.Date;
            DateTime last = new DateTime(Math.Max(0L, Save.daily.lastClaimUtcTicks), DateTimeKind.Utc).Date;
            bool claimedToday = Save.daily.lastClaimUtcTicks > 0 && last == today;
            bool continues = Save.daily.lastClaimUtcTicks > 0 && last == today.AddDays(-1);
            int streak = claimedToday ? Save.daily.streak : continues ? Save.daily.streak + 1 : 1;
            dayIndex = (Mathf.Max(1, streak) - 1) % MetaCatalog.Daily.Length;
            return !claimedToday;
        }

        public static RewardBundle ClaimDaily()
        {
            if (!CanClaimDaily(out int dayIndex)) return null;
            DateTime today = DateTime.UtcNow.Date;
            DateTime last = new DateTime(Math.Max(0L, Save.daily.lastClaimUtcTicks), DateTimeKind.Utc).Date;
            bool continues = Save.daily.lastClaimUtcTicks > 0 && last == today.AddDays(-1);
            Save.daily.streak = continues ? Save.daily.streak + 1 : 1;
            Save.daily.lastClaimUtcTicks = DateTime.UtcNow.Ticks;

            DailyRewardDef def = MetaCatalog.Daily[dayIndex];
            RewardBundle reward;
            if (def.Chest != null)
            {
                reward = OpenChest(MetaCatalog.Chest(def.Chest));
            }
            else
            {
                reward = new RewardBundle { Gold = def.Gold, Gems = def.Gems };
                if (def.Potion != null)
                {
                    Save.inventory.Add(def.Potion, 1);
                    reward.Lines.Add(MetaCatalog.Potion(def.Potion).Name);
                }
                if (def.Gold > 0) reward.Lines.Add($"+{def.Gold} ouro");
                if (def.Gems > 0) reward.Lines.Add($"+{def.Gems} gemas");
                Economy.Add(def.Gold, def.Gems, "recompensa diária");
            }
            Commit();
            return reward;
        }

        private static PurchaseResult Commit()
        {
            SaveManager.Save();
            Changed?.Invoke();
            return PurchaseResult.Success;
        }

        public static string Explain(PurchaseResult result) => result switch
        {
            PurchaseResult.Success => "Concluído!",
            PurchaseResult.NotEnoughCurrency => "Saldo insuficiente",
            PurchaseResult.AlreadyOwned => "Você já possui",
            PurchaseResult.MaxLevel => "Nível máximo",
            PurchaseResult.Locked => "Bloqueado",
            _ => "Indisponível"
        };
    }
}
