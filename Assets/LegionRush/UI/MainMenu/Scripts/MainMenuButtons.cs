using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LegionRush
{
// Componentes visuais da tela inicial. Cada um monta a própria hierarquia a partir da arte recortada
    // (MainMenuArt) e expõe só o que muda com os dados: texto, progresso, selo, contador, estado selecionado.
    // Não conhecem save nem telas: quem cria passa a ação do toque.
    // Base de botão: área de toque na arte, som, vibração e afundar ao pressionar.
    public static class MainMenuButtons
    {
        public static Button Make(RectTransform rect, Graphic target, Action onClick)
        {
            target.raycastTarget = true;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = target;
            button.transition = Selectable.Transition.None;
            var navigation = new Navigation { mode = Navigation.Mode.None };
            button.navigation = navigation;
            rect.gameObject.AddComponent<UIPressScale>();
            if (onClick != null)
            {
                button.onClick.AddListener(() =>
                {
                    GameAudio.Play(SoundId.UiClick);
                    Haptics.Tap();
                    onClick();
                });
            }
            return button;
        }

        public static TMP_Text Label(Transform parent, string text, float size, LegionTextStyle style, Color color,
            TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            TMP_Text label = UI.Label(parent, text, Mathf.RoundToInt(size * LegionUI.Px), color, anchor, true, "Label", style);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.fontSizeMin = Mathf.Max(10f, label.fontSizeMax * 0.6f);
            return label;
        }
    }
}
