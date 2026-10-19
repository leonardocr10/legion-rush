using System;
using UnityEngine;

namespace LegionRush
{
    // Tudo o que a tela inicial mostra, num retrato só. A tela não lê o save nem a economia diretamente:
    // recebe este modelo de um IMainMenuData e chama um IMainMenuNavigator ao tocar nos botões.
    public struct MainMenuModel
    {
        public string PlayerName;
        public int PlayerLevel;
        public int Xp;
        public int XpMax;

        public int Gold;
        public int Gems;

        public int WorldNumber;
        public string WorldName;
        public int LevelNumber;
        public int StageInWorld;
        public int WorldStars;
        public int WorldStarsMax;

        public bool BossAvailable;
        public int BossLevel;

        public int ClaimableMissions;
        public int ClaimableAchievements;
        public int AchievementsDone;
        public int AchievementsTotal;
        public bool DailyReady;
        public TimeSpan DailyIn;
        public bool UpgradeAffordable;
    }

    public interface IMainMenuData
    {
        MainMenuModel Snapshot();
    }

    public enum MainMenuDestination
    {
        Profile, Shop, ShopGold, ShopGems, Settings,
        Missions, Events, Upgrades, DailyChest,
        Ranking, Achievements, Season, Friends, Messages,
        Home, Heroes, Troops, BattlePass, More,
        Play, Boss, World, WorldChest,
        Worlds, Inventory, Equipment
    }

    public interface IMainMenuNavigator
    {
        void Open(MainMenuDestination destination);
    }

    // Dados reais do jogo: save, carteira, missões, conquistas, recompensa diária e catálogo de fases.
    public class GameMainMenuData : IMainMenuData
    {
        private readonly GameBootstrap game;

        public GameMainMenuData(GameBootstrap game) => this.game = game;

        public MainMenuModel Snapshot()
        {
            SaveData save = SaveManager.Current;
            int level = game.DefaultLevelNumber();
            WorldId world = LevelCatalog.WorldOf(level);
            (int first, int last) = LevelCatalog.WorldRange(world);
            int worldStars = 0;
            for (int n = first; n <= last; n++)
            {
                LevelRecord record = save.progress.Get(n, false);
                if (record != null) worldStars += record.stars;
            }

            int achievementsDone = 0;
            foreach (AchievementDef def in MetaCatalog.Achievements)
            {
                if (MetaService.IsAchievementClaimed(def)) achievementsDone++;
            }

            int boss = HighestBossLevel();
            var model = new MainMenuModel
            {
                PlayerName = "Comandante",
                PlayerLevel = save.progress.highestUnlocked,
                Xp = save.progress.TotalStars(),
                XpMax = LevelCatalog.TotalLevels * 3,
                Gold = Economy.Gold,
                Gems = Economy.Gems,
                WorldNumber = (int)world + 1,
                WorldName = WorldLightingProfile.DefaultName(world),
                LevelNumber = level,
                StageInWorld = level - first + 1,
                WorldStars = worldStars,
                WorldStarsMax = (last - first + 1) * 3,
                BossAvailable = boss > 0,
                BossLevel = boss > 0 ? boss : NextBossLevel(),
                ClaimableMissions = MetaService.ClaimableMissions(),
                ClaimableAchievements = MetaService.ClaimableAchievements(),
                AchievementsDone = achievementsDone,
                AchievementsTotal = MetaCatalog.Achievements.Count,
                DailyReady = MetaService.CanClaimDaily(out _),
                DailyIn = DateTime.UtcNow.Date.AddDays(1) - DateTime.UtcNow,
                UpgradeAffordable = AnyUpgradeAffordable()
            };
            return model;
        }

        // Fase de chefão mais avançada já liberada e presente no catálogo (0 = nenhuma ainda).
        private int HighestBossLevel()
        {
            for (int i = LevelCatalog.BossStages.Length - 1; i >= 0; i--)
            {
                int n = LevelCatalog.BossStages[i];
                if (game.IsLevelAvailable(n) && game.IsLevelUnlocked(n)) return n;
            }
            return 0;
        }

        private static int NextBossLevel()
        {
            foreach (int n in LevelCatalog.BossStages)
            {
                if (n > SaveManager.Current.progress.highestUnlocked) return n;
            }
            return LevelCatalog.BossStages[0];
        }

        private static bool AnyUpgradeAffordable()
        {
            foreach (UpgradeDef upgrade in MetaCatalog.Upgrades)
            {
                int level = MetaService.UpgradeLevel(upgrade.Id);
                if (level < upgrade.MaxLevel && Economy.CanAfford(upgrade.CostFor(level), 0)) return true;
            }
            return false;
        }
    }

    // Navegação pelas telas que já existem no jogo. Recursos que o jogo ainda não tem avisam "em breve".
    public class UIRootMainMenuNavigator : IMainMenuNavigator
    {
        private readonly UIRoot ui;

        public UIRootMainMenuNavigator(UIRoot ui) => this.ui = ui;

        public void Open(MainMenuDestination destination)
        {
            GameBootstrap game = ui.Game;
            switch (destination)
            {
                case MainMenuDestination.Profile:
                case MainMenuDestination.Heroes: ui.Push<HeroScreen>(); break;
                case MainMenuDestination.Shop: ui.Push<ShopScreen>(); break;
                case MainMenuDestination.ShopGold: ui.Push<ShopScreen>().Focus(ShopScreen.SectionChests); break;
                case MainMenuDestination.ShopGems: ui.Push<ShopScreen>().Focus(ShopScreen.SectionSkins); break;
                case MainMenuDestination.Settings: ui.Push<SettingsScreen>(); break;
                case MainMenuDestination.Missions: ui.Push<MissionsScreen>(); break;
                case MainMenuDestination.Upgrades: ui.Push<UpgradeScreen>(); break;
                case MainMenuDestination.DailyChest: ui.Push<DailyRewardScreen>(); break;
                case MainMenuDestination.Ranking: ui.Push<RankingScreen>(); break;
                case MainMenuDestination.Achievements: ui.Push<AchievementsScreen>(); break;
                case MainMenuDestination.Troops: ui.Push<TroopScreen>(); break;
                case MainMenuDestination.Worlds: ui.Push<WorldSelectScreen>(); break;
                case MainMenuDestination.Inventory: ui.Push<InventoryScreen>(); break;
                case MainMenuDestination.Equipment: ui.Push<EquipmentScreen>(); break;
                case MainMenuDestination.World:
                case MainMenuDestination.WorldChest:
                    ui.Push<LevelSelectScreen>().Show(LevelCatalog.WorldOf(game.DefaultLevelNumber()));
                    break;
                case MainMenuDestination.Play: game.PlayLevel(game.DefaultLevelNumber()); break;
                case MainMenuDestination.Boss: OpenBoss(game); break;
                case MainMenuDestination.Events: ComingSoon("EVENTOS", "Eventos por tempo limitado chegam em uma próxima atualização."); break;
                case MainMenuDestination.Season: ComingSoon("TEMPORADA", "As temporadas ranqueadas chegam em uma próxima atualização."); break;
                case MainMenuDestination.Friends: ComingSoon("AMIGOS", "Jogar com amigos chega em uma próxima atualização."); break;
                case MainMenuDestination.Messages: ComingSoon("MENSAGENS", "Sua caixa de mensagens está vazia."); break;
                case MainMenuDestination.BattlePass: ui.Push<BattlePassScreen>(); break;
                case MainMenuDestination.Home:
                case MainMenuDestination.More:
                    break;
            }
        }

        // Modo Boss: entra direto na fase de chefão mais avançada liberada; se ainda não há, diz qual liberar.
        private void OpenBoss(GameBootstrap game)
        {
            MainMenuModel model = new GameMainMenuData(game).Snapshot();
            if (model.BossAvailable)
            {
                game.PlayLevel(model.BossLevel);
                return;
            }
            GameAudio.Play(SoundId.UiBack);
            ui.Toast($"Modo Boss libera na fase {model.BossLevel}");
        }

        private void ComingSoon(string title, string message) => ui.Info(title, message + "\n\nFique de olho!");
    }

    // Valores do mockup, para prévias no editor e capturas de referência sem save carregado.
    public class SampleMainMenuData : IMainMenuData
    {
        public MainMenuModel Snapshot() => new MainMenuModel
        {
            PlayerName = "Leonardo",
            PlayerLevel = 12,
            Xp = 4250,
            XpMax = 8000,
            Gold = 125400,
            Gems = 1250,
            WorldNumber = 1,
            WorldName = "Reino Congelado",
            LevelNumber = 5,
            StageInWorld = 8,
            WorldStars = 12,
            WorldStarsMax = 50,
            BossAvailable = true,
            BossLevel = 8,
            ClaimableMissions = 0,
            ClaimableAchievements = 0,
            AchievementsDone = 3,
            AchievementsTotal = 12,
            DailyReady = false,
            DailyIn = new TimeSpan(23, 45, 0),
            UpgradeAffordable = true
        };
    }

    public class NullMainMenuNavigator : IMainMenuNavigator
    {
        public void Open(MainMenuDestination destination) => Debug.Log("[MainMenu] " + destination);
    }
}
