using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LegionRush
{
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
