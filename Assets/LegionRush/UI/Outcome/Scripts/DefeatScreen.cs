using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LegionRush
{
    // Derrota no layout do mockup: rei derrotado com a faixa DERROTA no topo,
    // painel de recompensas (ouro e pontuação) e os botões Jogar Novamente / Sair na base.
    public class DefeatScreen : OutcomeScreen
    {
        private static readonly Vector4 PanelBox = new Vector4(44, 925, 897, 1388);
        private static readonly Palette Colors = new Palette
        {
            Shell = new Color32(60, 36, 66, 250), ShellEdge = new Color32(150, 70, 140, 140),
            Inner = new Color32(29, 16, 34, 255), InnerEdge = new Color32(120, 50, 110, 150),
            Card = new Color32(17, 7, 19, 255), CardEdge = new Color32(80, 45, 90, 120)
        };

        private TMP_Text goldValue;
        private TMP_Text scoreValue;
        private TMP_Text scoreLabel;

        protected override string Title => "DERROTA";
        protected override Color Backdrop => new Color32(14, 10, 22, 255);
        protected override string SkySprite => "Defeat_Sky";
        protected override string HeroSprite => "Defeat_Hero";
        protected override Vector4 HeroBox => new Vector4(0, -30, 941, 1013);

        protected override void BuildBody(RectTransform body)
        {
            RectTransform panel = RewardsPanel(body, PanelBox, Colors, 40f);
            goldValue = Card(panel, PanelBox, new Vector4(92, 1055, 466, 1344), HeroesArt.Get("Icon_Coin"), "Ouro", Colors, 76f, 34f, out _);
            scoreValue = Card(panel, PanelBox, new Vector4(480, 1055, 854, 1344), HeroesArt.Get("Icon_Crown"), "Pontuação", Colors, 76f, 34f,
                out scoreLabel);

            Action(body, 70, 502, UIStyle.Blue, "Jogar Novamente", "Icon_Retry", () => Game.RetryLevel(), 0.06f, 0.2f);
            Action(body, 558, 877, UIStyle.Red, "Sair", "Icon_Exit", () => Game.ReturnToMenu(), 0.1f, 0.3f);
        }

        // Botão do pacote com ícone à esquerda e rótulo à direita.
        private static void Action(RectTransform body, float x0, float x1, Color color, string text, string icon, System.Action onClick,
            float iconMin, float iconMax)
        {
            Button button = UI.Button(body, text, color, onClick, 50, "Button_" + icon);
            LegionUI.Place((RectTransform)button.transform, LegionUI.Edge.Bottom, x0, 1460, x1, 1592);
            Image art = OutcomeArt.Image(button.transform, icon, "Icon");
            UI.Place(art.rectTransform, iconMin, 0.18f, iconMax, 0.82f);
            TMP_Text label = button.GetComponentInChildren<TMP_Text>();
            UI.Place(label.rectTransform, iconMax, 0.1f, 0.95f, 0.9f);
            label.textWrappingMode = TextWrappingModes.NoWrap;
        }

        protected override void RefreshBody()
        {
            LevelManager level = Game.CurrentLevel;
            if (level == null) return;
            LevelOutcome outcome = level.LastOutcome;
            goldValue.text = Amount(outcome.GoldEarned);
            scoreValue.text = Amount(outcome.Score, false);
            scoreLabel.text = outcome.NewBestScore ? "Novo recorde!" : "Pontuação";
            scoreLabel.color = outcome.NewBestScore ? HighlightColor : CaptionColor;
            LegionTextFx.Play(goldValue, LegionTextAnimation.FadeScale);
            LegionTextFx.Play(scoreValue, LegionTextAnimation.FadeScale);
            Haptics.Impact();
        }
    }
}
