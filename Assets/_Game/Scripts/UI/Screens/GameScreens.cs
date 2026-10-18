using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LegionRush
{
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
