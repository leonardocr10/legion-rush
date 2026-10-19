using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LegionRush
{
    // 11. Missões diárias e semanais
    public class MissionsScreen : MenuScreen
    {
        private RectTransform list;
        protected override string Title => "MISSÕES";
        protected override void BuildBody(RectTransform body) => list = UI.ScrollList(body);

        protected override void RefreshBody()
        {
            UI.Clear(list);
            Section(list, "DIÁRIAS (renovam todo dia)");
            AddMissions(false);
            Section(list, "SEMANAIS (renovam toda segunda)");
            AddMissions(true);
        }

        private void AddMissions(bool weekly)
        {
            foreach (MissionDef def in MetaCatalog.Missions)
            {
                if (def.Weekly != weekly) continue;
                MissionProgress progress = MetaService.Mission(def);
                bool done = progress.progress >= def.Target;
                string reward = def.RewardGems > 0 ? $"{def.RewardGems} gemas" : $"{def.RewardGold} ouro";
                MissionDef captured = def;
                Button claim = ItemRow(list, def.Title, $"{progress.progress}/{def.Target}  ·  Recompensa: {reward}",
                    progress.claimed ? "COLETADA" : done ? "COLETAR" : "EM ANDAMENTO",
                    progress.claimed ? UIStyle.Gray : done ? UIStyle.Gold : UIStyle.Blue, () =>
                    {
                        if (MetaService.ClaimMission(captured))
                        {
                            GameAudio.Play(SoundId.CoinPickup);
                            Ui.Toast("+" + reward);
                        }
                        Ui.RefreshTop();
                    }, done ? UIStyle.Green : UIStyle.Blue, done && !progress.claimed);
                RectTransform row = (RectTransform)claim.transform.parent;
                Image fill = UI.Bar(row, progress.progress / (float)def.Target, UIStyle.Green);
                UI.Place(((RectTransform)fill.transform.parent), 0.045f, 0.08f, 0.62f, 0.2f);
            }
        }
    }

    // 12. Recompensas diárias (ciclo de 7 dias)
    public class DailyRewardScreen : MenuScreen
    {
        private RectTransform grid;
        private Button claimButton;
        protected override string Title => "RECOMPENSA DIÁRIA";

        protected override void BuildBody(RectTransform body)
        {
            grid = UI.Rect(body, "Days");
            UI.Place(grid, 0f, 0.2f, 1f, 1f);
            claimButton = UI.Button(body, "COLETAR", UIStyle.Gold, Claim, 60);
            UI.Place((RectTransform)claimButton.transform, 0.15f, 0.04f, 0.85f, 0.15f);
        }

        protected override void RefreshBody()
        {
            UI.Clear(grid);
            bool available = MetaService.CanClaimDaily(out int dayIndex);
            for (int i = 0; i < MetaCatalog.Daily.Length; i++)
            {
                int column = i % 3;
                int row = i / 3;
                bool current = i == dayIndex;
                bool past = i < dayIndex || (!available && i == dayIndex);
                Color color = current && available ? UIStyle.Gold : past ? UIStyle.Gray : UIStyle.Panel;
                bool last = i == MetaCatalog.Daily.Length - 1;
                Image tile = UI.Panel(grid, color, "Day" + (i + 1), true);
                if (last) UI.Place(tile.rectTransform, 0.02f, 0.02f, 0.98f, 0.3f);
                else UI.Place(tile.rectTransform, 0.02f + column * 0.327f, 0.68f - row * 0.33f, 0.02f + column * 0.327f + 0.31f, 0.98f - row * 0.33f);
                TMP_Text label = UI.Label(tile.transform, $"DIA {i + 1}\n{MetaCatalog.Daily[i].Describe()}{(past ? "\nOK" : string.Empty)}", 38, Color.white);
                UI.Stretch(label.rectTransform, 10f, 10f, 10f, 10f);
            }
            claimButton.interactable = available;
            UI.SetLabel(claimButton, available ? "COLETAR" : "VOLTE AMANHÃ");
        }

        private void Claim()
        {
            RewardBundle reward = MetaService.ClaimDaily();
            if (reward == null) return;
            Ui.Push<RewardScreen>().Show("RECOMPENSA DIÁRIA", reward);
        }
    }

    // 13. Conquistas
    public class AchievementsScreen : MenuScreen
    {
        private RectTransform list;
        protected override string Title => "CONQUISTAS";
        protected override void BuildBody(RectTransform body) => list = UI.ScrollList(body);

        protected override void RefreshBody()
        {
            UI.Clear(list);
            foreach (AchievementDef def in MetaCatalog.Achievements)
            {
                int value = Mathf.Min(def.Target, MetaService.AchievementValue(def));
                bool done = value >= def.Target;
                bool claimed = MetaService.IsAchievementClaimed(def);
                AchievementDef captured = def;
                ItemRow(list, def.Title, $"{def.Description}  ·  {value}/{def.Target}  ·  {def.RewardGems} gemas",
                    claimed ? "CONCLUÍDA" : done ? "COLETAR" : "BLOQUEADA", claimed ? UIStyle.Gray : done ? UIStyle.Gold : UIStyle.Blue, () =>
                    {
                        if (MetaService.ClaimAchievement(captured))
                        {
                            GameAudio.Play(SoundId.GemPickup);
                            Ui.Toast($"+{captured.RewardGems} gemas");
                        }
                        Ui.RefreshTop();
                    }, claimed ? UIStyle.Gold : UIStyle.Purple, done && !claimed);
            }
        }
    }

    // 14. Recordes pessoais
    public class RecordsScreen : MenuScreen
    {
        private RectTransform list;
        protected override string Title => "RECORDES";
        protected override void BuildBody(RectTransform body) => list = UI.ScrollList(body);

        protected override void RefreshBody()
        {
            UI.Clear(list);
            SaveData save = SaveManager.Current;
            RecordData r = save.records;
            Section(list, "GERAL");
            Stat("Fases jogadas", r.levelsPlayed.ToString());
            Stat("Vitórias", r.victories.ToString());
            Stat("Inimigos derrotados", r.totalKills.ToString());
            Stat("Chefões derrotados", r.bossesDefeated.ToString());
            Stat("Maior exército", r.bestArmySize.ToString());
            Stat("Ouro ganho no total", r.totalGoldEarned.ToString());
            Stat("Estrelas", $"{save.progress.TotalStars()}/{LevelCatalog.TotalLevels * 3}");

            Section(list, "POR FASE");
            save.progress.levels.Sort((a, b) => a.levelNumber.CompareTo(b.levelNumber));
            foreach (LevelRecord level in save.progress.levels)
            {
                string time = level.bestTimeSeconds > 0f ? TimeSpan.FromSeconds(level.bestTimeSeconds).ToString(@"m\:ss") : "--";
                Stat($"Fase {level.levelNumber}  {new string('*', level.stars)}", $"{level.bestScore} pts · {time} · {level.attempts}x");
            }
        }

        private void Stat(string name, string value)
        {
            RectTransform row = UI.Row(list, 100f, UIStyle.Panel);
            TMP_Text left = UI.Label(row, name, 38, Color.white, TextAnchor.MiddleLeft);
            UI.Place(left.rectTransform, 0.04f, 0f, 0.6f, 1f);
            TMP_Text right = UI.Label(row, value, 38, UIStyle.Gold, TextAnchor.MiddleRight);
            UI.Place(right.rectTransform, 0.5f, 0f, 0.96f, 1f);
        }
    }

}
