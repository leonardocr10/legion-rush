using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LegionRush
{
    // Moldura comum das telas de menu no grid do mockup (941×1672): fundo do castelo, perfil, carteira, ajustes,
    // botão voltar, faixa de título e barra de navegação. Cada tela monta só o próprio conteúdo.
    public class MenuChrome
    {
        public MainMenuProfilePanel Profile { get; private set; }
        public MainMenuCurrencyBar Gold { get; private set; }
        public MainMenuCurrencyBar Gems { get; private set; }
        public TMP_Text Title { get; private set; }

        // titleWidth ficou só por compatibilidade: a faixa de título tem tamanho fixo no padrão novo.
        public static MenuChrome Build(RectTransform root, UIRoot ui, string title, Action onBack, float titleWidth = 560f)
        {
            var chrome = new MenuChrome();
            Image fill = root.gameObject.AddComponent<Image>();
            fill.color = new Color(0.03f, 0.07f, 0.2f);
            Image scenery = LegionArt.Image(root, "Backgrounds/bg_frozen_castle", false, "Scenery");
            scenery.preserveAspect = false;
            AspectRatioFitter fitter = scenery.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = scenery.sprite != null ? scenery.sprite.rect.width / scenery.sprite.rect.height : 0.56f;
            scenery.color = new Color(0.36f, 0.45f, 0.85f);
            Image shade = UI.Panel(root, new Color(0.02f, 0.05f, 0.18f, 0.6f), "Shade");
            UI.Stretch(shade.rectTransform);

            chrome.Profile = MainMenuProfilePanel.Create(root, null);
            LegionUI.Place((RectTransform)chrome.Profile.transform, LegionUI.Edge.TopLeft, 14, 12, 394, 120);
            chrome.Gold = MainMenuCurrencyBar.Create(root, "Icon_Coin", () => ui.Push<ShopScreen>().Focus(ShopScreen.SectionChests), "Gold");
            LegionUI.Place((RectTransform)chrome.Gold.transform, LegionUI.Edge.Top, 403, 30, 630, 92);
            chrome.Gems = MainMenuCurrencyBar.Create(root, "Icon_Gem", () => ui.Push<ShopScreen>().Focus(ShopScreen.SectionSkins), "Gems");
            LegionUI.Place((RectTransform)chrome.Gems.transform, LegionUI.Edge.Top, 638, 30, 852, 92);
            Image settings = MainMenuArt.Image(root, "Button_Settings", "Settings");
            LegionUI.Place(settings.rectTransform, LegionUI.Edge.TopRight, 860, 22, 928, 92);
            MainMenuButtons.Make(settings.rectTransform, settings, () => ui.Push<SettingsScreen>());

            // Cabeçalho padrão (Tarefa 0.1): voltar 18,134,102,216 e título 223,130,718,226; o conteúdo começa em y 240.
            chrome.Title = LegionHeader.Build(root, title, onBack);
            return chrome;
        }

        public void Refresh(UIRoot ui)
        {
            if (!SaveManager.IsLoaded) return;
            if (ui != null && ui.Game != null)
            {
                MainMenuModel model = new GameMainMenuData(ui.Game).Snapshot();
                Profile.Set(model.PlayerName, model.PlayerLevel, model.Xp, model.XpMax);
            }
            Gold.Set(Economy.Gold);
            Gems.Set(Economy.Gems);
        }

        // Barra inferior; a aba da tela atual fica acesa e não navega.
        public static void Nav(RectTransform root, UIRoot ui, MainMenuDestination current)
        {
            (string icon, string label, MainMenuDestination destination)[] items =
            {
                ("Icon_Home", "INÍCIO", MainMenuDestination.Home), ("Icon_Heroes", "HERÓIS", MainMenuDestination.Heroes),
                ("Icon_Troops", "TROPAS", MainMenuDestination.Troops), ("Icon_BattlePass", "PASSE", MainMenuDestination.BattlePass),
                ("Icon_More", "MAIS", MainMenuDestination.More)
            };
            float[] x0 = { 10, 196, 382, 568, 754 };
            for (int i = 0; i < items.Length; i++)
            {
                MainMenuDestination destination = items[i].destination;
                MainMenuNavButton button = MainMenuNavButton.Create(root, items[i].icon, items[i].label, () =>
                {
                    if (destination == current) return;
                    ui.Replace<MainMenuScreen>();
                    if (destination != MainMenuDestination.Home && destination != MainMenuDestination.More)
                        new UIRootMainMenuNavigator(ui).Open(destination);
                });
                LegionUI.Place((RectTransform)button.transform, LegionUI.Edge.Bottom, x0[i], 1506, x0[i] + 178, 1662);
                button.SetSelected(destination == current);
            }
        }

        // ---------- peças comuns ----------

        // LegionPanel (Tarefa 0.6): 9-slice com borda azul e friso dourado; nunca o Button_SideFrame esticado.
        public static Image Panel(Transform parent, Vector4 box, float x0, float y0, float x1, float y1, string name = "Panel")
        {
            float edge = Mathf.Clamp((Mathf.Min(x1 - x0, y1 - y0)) / 220f, 0.55f, 1f);
            return LegionPanel.Create(parent, box, x0, y0, x1, y1, name, edge);
        }

        public static TMP_Text Text(Transform parent, Vector4 box, float x0, float y0, float x1, float y1, string text, float size,
            LegionTextStyle style, Color color, TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            RectTransform rect = LegionUI.Inside(UI.Rect(parent, "Text"), box, x0, y0, x1, y1);
            TMP_Text label = UI.Label(rect, text, Mathf.RoundToInt(size * LegionUI.Px), color, anchor, true, "Value", style);
            label.richText = true;
            UI.Stretch(label.rectTransform);
            return label;
        }

        public static Image Art(Transform parent, Vector4 box, float x0, float y0, float x1, float y1, string legionArtPath)
        {
            Image image = LegionArt.Image(parent, legionArtPath, false, "Art");
            image.preserveAspect = true;
            LegionUI.Inside(image.rectTransform, box, x0, y0, x1, y1);
            return image;
        }

        // Botão chapado com contorno claro (verde, azul, dourado...).
        public static Image Button(Transform parent, Vector4 box, float x0, float y0, float x1, float y1, string text, Color color, Action onClick,
            float size = 26f)
        {
            Image button = UI.Panel(parent, color, "Button");
            LegionUI.Inside(button.rectTransform, box, x0, y0, x1, y1);
            Outline ring = button.gameObject.AddComponent<Outline>();
            ring.effectColor = Color.Lerp(color, Color.white, 0.5f);
            ring.effectDistance = new Vector2(2f, -2f);
            TMP_Text label = MainMenuButtons.Label(button.transform, text, size, LegionTextStyle.PrimaryButton, Color.white);
            UI.Place(label.rectTransform, 0.04f, 0f, 0.96f, 1f);
            label.richText = true;
            if (onClick != null) MainMenuButtons.Make(button.rectTransform, button, onClick);
            return button;
        }

        public static readonly Color Green = new Color(0.18f, 0.72f, 0.2f);
        public static readonly Color Blue = new Color(0.13f, 0.5f, 1f);
        public static readonly Color GoldColor = new Color(1f, 0.72f, 0.12f);
        public static readonly Color Gray = new Color(0.35f, 0.4f, 0.5f);
        public static readonly Color GoldText = new Color(1f, 0.82f, 0.3f);

        public static string Format(int value) => value.ToString("N0", CultureInfo.GetCultureInfo("pt-BR"));
    }
}
