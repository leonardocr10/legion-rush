using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LegionRush
{
    // Painel do mundo atual: miniatura, mundo, região, fase, coroa com progresso de estrelas e baú.
    public class MainMenuWorldPanel : MonoBehaviour
    {
        private static readonly Vector4 Box = new Vector4(0, 0, 672, 168);

        private TMP_Text world;
        private TMP_Text region;
        private TMP_Text stage;
        private TMP_Text count;
        private MainMenuProgressBar bar;

        public static MainMenuWorldPanel Create(Transform parent, Action onOpen, Action onChest)
        {
            Image panel = MainMenuArt.Image(parent, "Panel_WorldProgress", "WorldPanel");
            panel.preserveAspect = false;
            RectTransform rect = panel.rectTransform;
            MainMenuWorldPanel view = panel.gameObject.AddComponent<MainMenuWorldPanel>();
            view.world = LegionUI.Text(rect, Box, 188, 16, 470, 46, "MUNDO 1", 21, LegionTextStyle.PrimaryButton, new Color(0.86f, 0.92f, 1f), TextAnchor.MiddleLeft);
            view.region = LegionUI.Text(rect, Box, 188, 42, 480, 86, "REINO CONGELADO", 31, LegionTextStyle.PrimaryButton, Color.white, TextAnchor.MiddleLeft);
            view.stage = LegionUI.Text(rect, Box, 188, 84, 470, 118, "FASE 1-1", 27, LegionTextStyle.PrimaryButton, Color.white, TextAnchor.MiddleLeft);
            view.bar = MainMenuProgressBar.Create(rect, "Bar_TrackWorld", "Bar_FillTeal", "Stars");
            LegionUI.Inside((RectTransform)view.bar.transform, Box, 208, 121, 466, 146);
            Image crown = MainMenuArt.Image(rect, "Icon_Crown", "Crown");
            LegionUI.Inside(crown.rectTransform, Box, 186, 113, 228, 148);
            view.count = LegionUI.Text(rect, Box, 290, 118, 466, 149, "0/0", 21, LegionTextStyle.Hud, Color.white);

            Image chest = MainMenuArt.Image(rect, "Icon_WorldChest", "Chest");
            LegionUI.Inside(chest.rectTransform, Box, 490, 18, 650, 156);
            chest.gameObject.AddComponent<UIIdleBob>().amplitude = 4f;
            MainMenuButtons.Make(chest.rectTransform, chest, onChest);
            MainMenuButtons.Make(rect, panel, onOpen);
            return view;
        }

        public void Set(MainMenuModel model)
        {
            world.text = "MUNDO " + model.WorldNumber;
            region.text = model.WorldName.ToUpperInvariant();
            stage.text = $"FASE {model.WorldNumber}-{model.StageInWorld}";
            count.text = $"{model.WorldStars}/{model.WorldStarsMax}";
            bar.Set(model.WorldStarsMax > 0 ? model.WorldStars / (float)model.WorldStarsMax : 0f);
        }
    }
}
