using UnityEngine;
using UnityEngine.UI;

namespace LegionRush
{
    // Preparação da fase: herói, composição, habilidades e poções ativas. Fica sobre o cenário 3D da fase.
    public class PrepScreen : MenuScreen
    {
        private Text info;
        private RectTransform compositions;
        private RectTransform skillRow;
        private Text skillHint;

        protected override string Title => "PREPARAR TROPAS";
        protected override Color BackgroundColor => new Color(0.02f, 0.04f, 0.1f, 0.35f);

        public override void OnBack() => Game.ReturnToMenu();

        protected override void BuildBody(RectTransform body)
        {
            Image panel = UI.Panel(body, UIStyle.Background, "Panel");
            UI.Place(panel.rectTransform, 0f, 0f, 1f, 0.55f);
            RectTransform content = panel.rectTransform;

            info = UI.Label(content, string.Empty, 36, Color.white, TextAnchor.UpperLeft, false);
            UI.Place(info.rectTransform, 0.04f, 0.78f, 0.96f, 0.97f);

            Text compositionTitle = UI.Label(content, "COMPOSIÇÃO", 38, UIStyle.Gold, TextAnchor.MiddleLeft);
            UI.Place(compositionTitle.rectTransform, 0.04f, 0.7f, 0.96f, 0.78f);
            compositions = UI.Rect(content, "Compositions");
            UI.Place(compositions, 0.02f, 0.58f, 0.98f, 0.7f);

            Text skillTitle = UI.Label(content, "HABILIDADES (toque para trocar)", 38, UIStyle.Gold, TextAnchor.MiddleLeft);
            UI.Place(skillTitle.rectTransform, 0.04f, 0.49f, 0.96f, 0.57f);
            skillRow = UI.Rect(content, "Skills");
            UI.Place(skillRow, 0.02f, 0.33f, 0.98f, 0.49f);
            skillHint = UI.Label(content, string.Empty, 30, UIStyle.TextDim, TextAnchor.MiddleCenter, false);
            UI.Place(skillHint.rectTransform, 0.04f, 0.25f, 0.96f, 0.33f);

            Button play = UI.Button(content, "JOGAR", UIStyle.Gold, () =>
            {
                GameAudio.Play(SoundId.UiConfirm);
                Game.StartRun();
            }, 70, "Play");
            UI.Place((RectTransform)play.transform, 0.2f, 0.04f, 0.8f, 0.22f);
        }

        protected override void RefreshBody()
        {
            SaveData save = SaveManager.Current;
            HeroDef hero = MetaCatalog.Hero(save.heroes.selected);
            LevelManager level = Game.CurrentLevel;
            string boss = level != null && level.Boss != null ? $"  ·  Chefão: {level.Boss.Definition.displayName}" : string.Empty;
            string potions = save.inventory.activePotions.Count > 0 ? $"\nPoções ativas: {save.inventory.activePotions.Count}" : string.Empty;
            info.text = $"FASE {Game.CurrentLevelNumber} · {Game.CurrentWorldName}{boss}\nHerói: {(hero != null ? hero.Name + " — " + hero.Title : "—")}{potions}";

            UI.Clear(compositions);
            int count = Game.CompositionCount;
            for (int i = 0; i < count; i++)
            {
                int index = i;
                Button button = UI.Button(compositions, Game.CompositionName(i).ToUpperInvariant(), i == Game.CompositionIndex ? UIStyle.Gold : UIStyle.Blue, () =>
                {
                    Game.SetComposition(index);
                    Ui.RefreshTop();
                }, 30);
                UI.Place((RectTransform)button.transform, i / (float)count + 0.005f, 0.05f, (i + 1) / (float)count - 0.005f, 0.95f);
                button.interactable = Game.IsCompositionAvailable(i);
            }

            UI.Clear(skillRow);
            SkillSystem skills = Game.CurrentSkills;
            if (skills == null) return;
            for (int i = 0; i < SkillSystem.SlotCount; i++)
            {
                int slot = i;
                SkillDefinition skill = skills.Definition(Game.LoadoutSlot(i));
                Button button = UI.Button(skillRow, skill.shortLabel, skill.color * 0.85f, () =>
                {
                    Game.CycleSkillSlot(slot);
                    SkillDefinition next = Game.CurrentSkills.Definition(Game.LoadoutSlot(slot));
                    skillHint.text = $"{next.displayName}: {next.description}";
                    Ui.RefreshTop();
                }, 30);
                UI.Place((RectTransform)button.transform, i / (float)SkillSystem.SlotCount + 0.005f, 0.05f, (i + 1) / (float)SkillSystem.SlotCount - 0.005f, 0.95f);
            }
        }
    }

    // 16. Pausa
    public class PauseScreen : MenuScreen
    {
        protected override string Title => "PAUSA";
        protected override bool ShowWallet => false;
        protected override Color BackgroundColor => new Color(0.02f, 0.04f, 0.1f, 0.75f);

        public override void OnBack() => Resume();

        protected override void BuildBody(RectTransform body)
        {
            string[] labels = { "CONTINUAR", "REINICIAR", "AJUSTES", "MENU" };
            Color[] colors = { UIStyle.Green, UIStyle.Blue, UIStyle.Blue, UIStyle.Red };
            System.Action[] actions =
            {
                Resume,
                () => Ui.Confirm("REINICIAR", "Recomeçar a fase do início?", "REINICIAR", Game.RetryLevel),
                () => Ui.Push<SettingsScreen>(),
                () => Ui.Confirm("SAIR DA FASE", "Voltar ao menu? O progresso desta partida será perdido.", "SAIR", Game.ReturnToMenu)
            };
            for (int i = 0; i < labels.Length; i++)
            {
                Button button = UI.Button(body, labels[i], colors[i], actions[i], 56);
                UI.Place((RectTransform)button.transform, 0.15f, 0.62f - i * 0.13f, 0.85f, 0.73f - i * 0.13f);
            }
        }

        private void Resume()
        {
            Ui.CloseAll();
            if (Game.CurrentLevel != null) Game.CurrentLevel.SetPaused(false);
        }
    }

    // 17/18. Vitória e derrota
    public class ResultScreen : MenuScreen
    {
        private Text heading;
        private Text stars;
        private Text details;
        private Button primary;
        private Button secondary;
        private bool victory;

        protected override string Title => "RESULTADO";
        protected override bool ShowBack => false;
        protected override Color BackgroundColor => new Color(0.02f, 0.04f, 0.1f, 0.6f);
        public override bool AllowBack => false;

        protected override void BuildBody(RectTransform body)
        {
            Image panel = UI.Panel(body, UIStyle.Panel, "Panel", true);
            UI.Place(panel.rectTransform, 0.03f, 0.12f, 0.97f, 0.9f);
            heading = UI.Label(panel.transform, string.Empty, 110, UIStyle.Gold);
            UI.Place(heading.rectTransform, 0.03f, 0.8f, 0.97f, 0.97f);
            stars = UI.Label(panel.transform, string.Empty, 90, UIStyle.Gold);
            UI.Place(stars.rectTransform, 0.03f, 0.68f, 0.97f, 0.8f);
            details = UI.Label(panel.transform, string.Empty, 40, Color.white);
            UI.Place(details.rectTransform, 0.06f, 0.3f, 0.94f, 0.68f);

            primary = UI.Button(panel.transform, "PRÓXIMA FASE", UIStyle.Gold, OnPrimary, 54);
            UI.Place((RectTransform)primary.transform, 0.1f, 0.15f, 0.9f, 0.27f);
            secondary = UI.Button(panel.transform, "REPETIR", UIStyle.Blue, Game.RetryLevel, 44);
            UI.Place((RectTransform)secondary.transform, 0.1f, 0.03f, 0.48f, 0.13f);
            Button menu = UI.Button(panel.transform, "MENU", UIStyle.Gray, Game.ReturnToMenu, 44);
            UI.Place((RectTransform)menu.transform, 0.52f, 0.03f, 0.9f, 0.13f);
        }

        protected override void RefreshBody()
        {
            LevelManager level = Game.CurrentLevel;
            if (level == null) return;
            victory = level.Current == LevelManager.State.Victory;
            LevelOutcome outcome = level.LastOutcome;
            heading.text = victory ? "VITÓRIA!" : "DERROTA";
            heading.color = victory ? UIStyle.Gold : UIStyle.Red;
            stars.text = victory ? new string('*', outcome.Stars) + new string('-', 3 - outcome.Stars) : "Tente outra estratégia";
            stars.fontSize = victory ? 90 : 48;
            string best = outcome.NewBestScore ? "  (RECORDE!)" : string.Empty;
            string unlocked = outcome.UnlockedNext ? "\nPróxima fase liberada!" : string.Empty;
            details.text = $"Pontuação {outcome.Score}{best}\nSoldados {level.Army.Count}  ·  Inimigos {level.Kills}\n" +
                           $"+{outcome.GoldEarned} ouro  ·  +{outcome.GemsEarned} gemas{unlocked}";
            bool next = victory && Game.HasNextLevel();
            UI.SetLabel(primary, next ? "PRÓXIMA FASE" : victory ? "MENU" : "TENTAR NOVAMENTE");
            Haptics.Impact();
        }

        private void OnPrimary()
        {
            if (victory && Game.HasNextLevel()) Game.PlayNextLevel();
            else if (victory) Game.ReturnToMenu();
            else Game.RetryLevel();
        }
    }

    // 19. Recompensas (baús e recompensa diária)
    public class RewardScreen : MenuScreen
    {
        private Text heading;
        private Text lines;
        protected override string Title => "RECOMPENSAS";

        public RewardScreen Show(string title, RewardBundle reward)
        {
            heading.text = title;
            lines.text = reward != null && reward.Lines.Count > 0 ? string.Join("\n", reward.Lines) : "Nada desta vez";
            GameAudio.Play(SoundId.ChestOpen);
            Haptics.Impact();
            Refresh();
            return this;
        }

        protected override void BuildBody(RectTransform body)
        {
            Image glow = UI.Panel(body, new Color(1f, 0.8f, 0.3f, 0.25f), "Glow");
            glow.sprite = UIStyle.Circle;
            UI.Place(glow.rectTransform, 0.2f, 0.55f, 0.8f, 0.9f);
            Image chest = UI.Panel(body, UIStyle.Gold, "Chest", true);
            UI.Place(chest.rectTransform, 0.33f, 0.63f, 0.67f, 0.8f);
            heading = UI.Label(body, string.Empty, 64, UIStyle.Gold);
            UI.Place(heading.rectTransform, 0.05f, 0.48f, 0.95f, 0.56f);
            lines = UI.Label(body, string.Empty, 46, Color.white);
            UI.Place(lines.rectTransform, 0.08f, 0.2f, 0.92f, 0.47f);
            Button ok = UI.Button(body, "CONTINUAR", UIStyle.Green, () => Ui.Pop(), 54);
            UI.Place((RectTransform)ok.transform, 0.2f, 0.05f, 0.8f, 0.16f);
        }
    }
}
