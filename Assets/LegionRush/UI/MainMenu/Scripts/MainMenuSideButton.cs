using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LegionRush
{
    // Botão das colunas laterais: moldura azul, ícone volumétrico, rótulo e (opcional) contador e selo.
    public class MainMenuSideButton : MonoBehaviour
    {
        public Button Button { get; private set; }
        public TMP_Text Label { get; private set; }
        public MainMenuBadge Badge { get; private set; }
        public MainMenuCountdown Countdown { get; private set; }

        private Image glow;

        public enum Kind { Labeled, LabeledWithTimer, TimerOnly }

        public static MainMenuSideButton Create(Transform parent, string icon, string label, Action onClick, Kind kind = Kind.Labeled)
        {
            Image frame = MainMenuArt.Image(parent, "Button_SideFrame", "Side_" + icon, true);
            RectTransform rect = frame.rectTransform;
            MainMenuSideButton side = frame.gameObject.AddComponent<MainMenuSideButton>();

            side.glow = UI.Panel(rect, new Color(0.45f, 0.8f, 1f, 0f), "SelectedGlow");
            side.glow.sprite = MainMenuArt.Get("Button_SideFrame");
            side.glow.type = Image.Type.Sliced;
            side.glow.pixelsPerUnitMultiplier = 1f / LegionUI.Px;
            UI.Stretch(side.glow.rectTransform, -6f, -6f, -6f, -6f);
            side.glow.transform.SetAsFirstSibling();

            Image art = MainMenuArt.Image(rect, icon, "Icon");
            switch (kind)
            {
                case Kind.LabeledWithTimer:
                    UI.Place(art.rectTransform, 0.12f, 0.42f, 0.88f, 0.94f);
                    break;
                case Kind.TimerOnly:
                    UI.Place(art.rectTransform, 0.1f, 0.3f, 0.9f, 0.95f);
                    break;
                default:
                    UI.Place(art.rectTransform, 0.12f, 0.3f, 0.88f, 0.93f);
                    break;
            }
            art.gameObject.AddComponent<UIIdleBob>().phase = UnityEngine.Random.value * 6f;

            if (kind != Kind.TimerOnly)
            {
                side.Label = MainMenuButtons.Label(rect, label, 19f, LegionTextStyle.PrimaryButton, Color.white);
                if (kind == Kind.LabeledWithTimer) UI.Place(side.Label.rectTransform, 0.04f, 0.24f, 0.96f, 0.42f);
                else UI.Place(side.Label.rectTransform, 0.05f, 0.06f, 0.95f, 0.3f);
            }
            if (kind != Kind.Labeled)
            {
                side.Countdown = MainMenuCountdown.Create(rect);
                UI.Place((RectTransform)side.Countdown.transform, 0.06f, 0.05f, 0.94f, kind == Kind.TimerOnly ? 0.28f : 0.22f);
            }

            side.Button = MainMenuButtons.Make(rect, frame, onClick);
            side.Badge = MainMenuBadge.Create(rect, new Vector2(-12f, -12f));
            side.Badge.Set(false);
            return side;
        }

        public void SetSelected(bool selected) => glow.color = new Color(0.45f, 0.8f, 1f, selected ? 0.7f : 0f);
    }
}
