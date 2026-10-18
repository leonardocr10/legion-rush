using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LegionRush
{
    // Botão da barra inferior: moldura escura (ou azul acesa quando selecionado), ícone e rótulo.
    public class MainMenuNavButton : MonoBehaviour
    {
        public Button Button { get; private set; }
        public MainMenuBadge Badge { get; private set; }

        private Image frame;
        private RectTransform icon;
        private TMP_Text label;

        public static MainMenuNavButton Create(Transform parent, string icon, string label, Action onClick)
        {
            Image frame = MainMenuArt.Image(parent, "Button_NavFrame", "Nav_" + icon, true);
            MainMenuNavButton nav = frame.gameObject.AddComponent<MainMenuNavButton>();
            nav.frame = frame;
            Image art = MainMenuArt.Image(frame.transform, icon, "Icon");
            UI.Place(art.rectTransform, 0.2f, 0.38f, 0.8f, 0.92f);
            nav.icon = art.rectTransform;
            nav.label = MainMenuButtons.Label(frame.transform, label, 21f, LegionTextStyle.PrimaryButton, Color.white);
            UI.Place(nav.label.rectTransform, 0.04f, 0.08f, 0.96f, 0.36f);
            nav.Button = MainMenuButtons.Make(frame.rectTransform, frame, onClick);
            nav.Badge = MainMenuBadge.Create(frame.rectTransform, new Vector2(-30f, -14f));
            nav.Badge.Set(false);
            return nav;
        }

        public void SetSelected(bool selected)
        {
            frame.sprite = MainMenuArt.Get(selected ? "Button_NavFrameSelected" : "Button_NavFrame");
            icon.localScale = Vector3.one * (selected ? 1.08f : 1f);
            label.color = selected ? new Color(1f, 0.93f, 0.62f) : Color.white;
        }
    }
}
