using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LegionRush
{
    // Vitória no layout do mockup: rei comemorando com a faixa VITÓRIA no topo, painel de recompensas
    // (ouro, gemas e pontuação), estrelas conquistadas no lugar do baú do mockup e o botão Continuar.
    public class VictoryScreen : OutcomeScreen
    {
        private static readonly Vector4 PanelBox = new Vector4(26, 935, 915, 1280);
        private static readonly Vector4 StarsBox = new Vector4(172, 1292, 769, 1515);
        private static readonly float[] StarCenters = { 340, 470, 600 };
        private static readonly Palette Colors = new Palette
        {
            Shell = new Color32(16, 42, 82, 245), ShellEdge = new Color32(70, 120, 200, 150),
            Inner = new Color32(9, 27, 56, 255), InnerEdge = new Color32(40, 80, 150, 140),
            Card = new Color32(4, 16, 36, 255), CardEdge = new Color32(40, 70, 130, 130)
        };

        private TMP_Text goldValue;
        private TMP_Text gemsValue;
        private TMP_Text scoreValue;
        private TMP_Text scoreLabel;
        private TMP_Text unlocked;
        private readonly Image[] stars = new Image[3];
        private Button primary;

        protected override string Title => "VITÓRIA";
        protected override Color Backdrop => new Color32(4, 20, 42, 255);
        protected override string SkySprite => "Victory_Sky";
        protected override string HeroSprite => "Victory_Hero";
        protected override Vector4 HeroBox => new Vector4(0, 32, 941, 995);

        protected override void BuildBody(RectTransform body)
        {
            RectTransform panel = RewardsPanel(body, PanelBox, Colors, 38f);
            goldValue = Card(panel, PanelBox, new Vector4(44, 1020, 322, 1261), HeroesArt.Get("Icon_Coin"), "Ouro", Colors, 66f, 30f, out _);
            gemsValue = Card(panel, PanelBox, new Vector4(332, 1020, 610, 1261), LegionArt.Get("Icons/gem"), "Gemas", Colors, 66f, 30f, out _);
            scoreValue = Card(panel, PanelBox, new Vector4(620, 1020, 898, 1261), HeroesArt.Get("Icon_Crown"), "Pontuação", Colors, 66f, 30f,
                out scoreLabel);

            BuildStars(body);

            primary = UI.Button(body, "Continuar", UIStyle.Gold, OnContinue, 60, "Continue");
            LegionUI.Place((RectTransform)primary.transform, LegionUI.Edge.Bottom, 170, 1530, 771, 1650);
        }

        // Estrelas da fase sobre um brilho dourado; a do meio fica mais alta e maior.
        private void BuildStars(RectTransform body)
        {
            RectTransform area = LegionUI.Place(UI.Rect(body, "Stars"), LegionUI.Edge.Bottom, StarsBox.x, StarsBox.y, StarsBox.z, StarsBox.w);
            Box(area, new Color32(6, 22, 46, 170), new Color32(30, 60, 110, 90), 2f);

            Image glow = OutcomeArt.Image(area, "Glow_Soft", "Glow");
            glow.preserveAspect = false;
            glow.color = new Color(1f, 0.82f, 0.35f, 0.55f);
            LegionUI.Inside(glow.rectTransform, StarsBox, 250, 1320, 690, 1515);

            LegionUI.Text(area, StarsBox, StarsBox.x + 20, StarsBox.y + 8, StarsBox.z - 20, StarsBox.y + 58, "Estrelas conquistadas", 36f,
                LegionTextStyle.Menu, Color.white);

            for (int i = 0; i < stars.Length; i++)
            {
                float half = i == 1 ? 72f : 60f;
                float cy = i == 1 ? 1405f : 1420f;
                Image star = UI.Rect(area, "Star" + i).gameObject.AddComponent<Image>();
                star.sprite = HeroesArt.Get("Star_Gold");
                star.preserveAspect = true;
                star.raycastTarget = false;
                LegionUI.Inside(star.rectTransform, StarsBox, StarCenters[i] - half, cy - half, StarCenters[i] + half, cy + half);
                UISlideIn slide = star.gameObject.AddComponent<UISlideIn>();
                slide.offset = new Vector2(0f, 40f);
                slide.delay = 0.25f + i * 0.15f;
                stars[i] = star;
            }

            unlocked = LegionUI.Text(area, StarsBox, StarsBox.x + 20, StarsBox.w - 38, StarsBox.z - 20, StarsBox.w - 6, "Próxima fase liberada!", 26f,
                LegionTextStyle.Menu, HighlightColor);
        }

        protected override void RefreshBody()
        {
            LevelManager level = Game.CurrentLevel;
            if (level == null) return;
            LevelOutcome outcome = level.LastOutcome;
            goldValue.text = Amount(outcome.GoldEarned);
            gemsValue.text = Amount(outcome.GemsEarned);
            scoreValue.text = Amount(outcome.Score, false);
            scoreLabel.text = outcome.NewBestScore ? "Novo recorde!" : "Pontuação";
            scoreLabel.color = outcome.NewBestScore ? HighlightColor : CaptionColor;
            for (int i = 0; i < stars.Length; i++)
                stars[i].color = i < outcome.Stars ? Color.white : new Color(0.2f, 0.25f, 0.38f, 0.9f);
            unlocked.gameObject.SetActive(outcome.UnlockedNext);
            UI.SetLabel(primary, Game.HasNextLevel() ? "Continuar" : "Menu");
            LegionTextFx.Play(goldValue, LegionTextAnimation.FadeScale);
            LegionTextFx.Play(gemsValue, LegionTextAnimation.FadeScale);
            LegionTextFx.Play(scoreValue, LegionTextAnimation.FadeScale);
            Haptics.Impact();
        }

        private void OnContinue()
        {
            if (Game.HasNextLevel()) Game.PlayNextLevel();
            else Game.ReturnToMenu();
        }
    }
}
