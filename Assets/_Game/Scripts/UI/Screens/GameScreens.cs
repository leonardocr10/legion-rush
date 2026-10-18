using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LegionRush
{
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
        private TMP_Text heading;
        private TMP_Text stars;
        private TMP_Text details;
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
            UI.SetStyle(heading, victory ? LegionTextStyle.Epic : LegionTextStyle.Negative);
            LegionTextFx.Play(heading, victory ? LegionTextAnimation.Entrance : LegionTextAnimation.Shake);
            stars.text = victory ? new string('*', outcome.Stars) + new string('-', 3 - outcome.Stars) : "Tente outra estratégia";
            stars.fontSizeMax = victory ? 90 : 48;
            UI.SetStyle(stars, victory ? LegionTextStyle.Epic : LegionTextStyle.Menu);
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
        private TMP_Text heading;
        private TMP_Text lines;
        protected override string Title => "RECOMPENSAS";

        public RewardScreen Show(string title, RewardBundle reward)
        {
            heading.text = title;
            lines.text = reward != null && reward.Lines.Count > 0 ? string.Join("\n", reward.Lines) : "Nada desta vez";
            LegionTextFx.Play(heading, LegionTextAnimation.FadeScale);
            LegionTextFx.Play(lines, LegionTextAnimation.FadeScale);
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
            heading = UI.Label(body, string.Empty, 64, UIStyle.Gold, TextAnchor.MiddleCenter, true, "Label", LegionTextStyle.Epic);
            UI.Place(heading.rectTransform, 0.05f, 0.48f, 0.95f, 0.56f);
            lines = UI.Label(body, string.Empty, 46, Color.white, TextAnchor.MiddleCenter, true, "Label", LegionTextStyle.Positive);
            UI.Place(lines.rectTransform, 0.08f, 0.2f, 0.92f, 0.47f);
            Button ok = UI.Button(body, "CONTINUAR", UIStyle.Green, () => Ui.Pop(), 54);
            UI.Place((RectTransform)ok.transform, 0.2f, 0.05f, 0.8f, 0.16f);
        }
    }
}
