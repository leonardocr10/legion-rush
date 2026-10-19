using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LegionRush
{
    // Painel do jogador: avatar, nome, coroa com nível, barra de experiência e progresso numérico.
    public class MainMenuProfilePanel : MonoBehaviour
    {
        private static readonly Vector4 Box = new Vector4(0, 0, 470, 133); // pixels da arte do painel

        private Image avatar;
        private TMP_Text playerName;
        private TMP_Text level;
        private TMP_Text xp;
        private MainMenuProgressBar bar;

        public static MainMenuProfilePanel Create(Transform parent, Action onClick)
        {
            Image panel = MainMenuArt.Image(parent, "Panel_PlayerProfile", "Profile");
            panel.preserveAspect = false;
            RectTransform rect = panel.rectTransform;
            MainMenuProfilePanel profile = panel.gameObject.AddComponent<MainMenuProfilePanel>();
            profile.avatar = MainMenuArt.Image(rect, "Avatar_King", "Avatar");
            LegionUI.Inside(profile.avatar.rectTransform, Box, 0, 0, 150, 133);
            profile.playerName = LegionUI.Text(rect, Box, 164, 6, 455, 52, "Comandante", 32, LegionTextStyle.PrimaryButton, Color.white, TextAnchor.MiddleLeft);
            profile.playerName.textWrappingMode = TextWrappingModes.NoWrap;
            Image crown = MainMenuArt.Image(rect, "Icon_Crown", "Crown");
            LegionUI.Inside(crown.rectTransform, Box, 160, 52, 198, 82);
            profile.level = LegionUI.Text(rect, Box, 202, 46, 455, 88, "Nível 1", 27, LegionTextStyle.Hud, new Color(1f, 0.84f, 0.25f), TextAnchor.MiddleLeft);
            profile.bar = MainMenuProgressBar.Create(rect, "Bar_Track", "Bar_FillBlue", "Xp");
            LegionUI.Inside((RectTransform)profile.bar.transform, Box, 160, 88, 455, 120);
            profile.xp = LegionUI.Text(rect, Box, 160, 86, 455, 122, "0 / 0", 21, LegionTextStyle.Hud, Color.white);
            MainMenuButtons.Make(rect, panel, onClick);
            return profile;
        }

        public void Set(string name, int playerLevel, int xpNow, int xpMax)
        {
            playerName.text = name;
            level.text = "Nível " + playerLevel;
            xp.text = $"{MainMenuCurrencyBar.Short(xpNow)} / {MainMenuCurrencyBar.Short(xpMax)}";
            // Preenchimento mínimo de 6 %: a barra vazia parecia quebrada.
            bar.Set(Mathf.Max(0.06f, xpMax > 0 ? xpNow / (float)xpMax : 0f));
        }
    }
}
