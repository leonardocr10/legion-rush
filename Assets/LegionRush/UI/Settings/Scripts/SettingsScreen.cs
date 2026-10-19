using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LegionRush
{
    // Configurações no layout do mockup (941×1672): topo com perfil e carteira, título, abas à esquerda e, à direita,
    // os painéis Geral (música, efeitos, vibração, economia de bateria), Gráficos (qualidade, FPS, efeitos, sombras,
    // modo desempenho), Conta e Notificações, mais os botões do rodapé e a barra de navegação.
    // Tudo aplica na hora: volumes no AudioManager, qualidade no VisualQualityManager, vibração no save.
    public class SettingsScreen : UIScreen
    {
        private const string NotificationKey = "LR_Notify_";
        private static readonly Color Green = new Color(0.22f, 0.8f, 0.36f);
        private static readonly Color Off = new Color(0.32f, 0.36f, 0.46f);
        private static readonly Color Gold = new Color(1f, 0.82f, 0.3f);
        private static readonly Color Line = new Color(1f, 1f, 1f, 0.07f);

        private static readonly (string icon, string label)[] TabDefs =
        {
            ("Icon_General", "GERAL"), ("Icon_Graphics", "GRÁFICOS"), ("Icon_Audio", "ÁUDIO"), ("Icon_Account", "CONTA"),
            ("Icon_Language", "IDIOMA"), ("Icon_Chat", "NOTIFICAÇÕES"), ("Icon_Support", "SUPORTE")
        };

        private static readonly (string icon, string label, string key, bool def)[] Notifications =
        {
            ("Icon_Swords", "Ataques e Defesas", "attacks", true), ("Icon_Chest", "Recompensas e Eventos", "rewards", true),
            ("Icon_Crown", "Amigos e Clã", "friends", true), ("Icon_News", "Novidades do Jogo", "news", false),
            ("Icon_Gift", "Promoções e Ofertas", "offers", true)
        };

        private MainMenuProfilePanel profile;
        private MainMenuCurrencyBar gold, gems;
        private readonly List<Image> tabFrames = new List<Image>();
        private readonly List<Action> refreshers = new List<Action>();
        private RectTransform content;
        private readonly List<GameObject> nav = new List<GameObject>();
        private int selectedTab;

        protected override void Build(RectTransform root)
        {
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

            profile = MainMenuProfilePanel.Create(root, null);
            LegionUI.Place((RectTransform)profile.transform, LegionUI.Edge.TopLeft, 14, 12, 394, 120);
            gold = MainMenuCurrencyBar.Create(root, "Icon_Coin", () => Ui.Push<ShopScreen>().Focus(ShopScreen.SectionChests), "Gold");
            LegionUI.Place((RectTransform)gold.transform, LegionUI.Edge.Top, 403, 30, 630, 92);
            gems = MainMenuCurrencyBar.Create(root, "Icon_Gem", () => Ui.Push<ShopScreen>().Focus(ShopScreen.SectionSkins), "Gems");
            LegionUI.Place((RectTransform)gems.transform, LegionUI.Edge.Top, 638, 30, 852, 92);
            Image gear = MainMenuArt.Image(root, "Button_Settings", "Settings");
            LegionUI.Place(gear.rectTransform, LegionUI.Edge.TopRight, 860, 22, 928, 92);

            Image back = HeroesArt.Image(root, "Button_Back", "Back");
            LegionUI.Place(back.rectTransform, LegionUI.Edge.TopLeft, 20, 150, 106, 236);
            MainMenuButtons.Make(back.rectTransform, back, OnBack);

            Image header = LegionArt.Image(root, "Popups/popup_header", false, "Header");
            LegionUI.Place(header.rectTransform, LegionUI.Edge.Top, 150, 96, 790, 282);
            TMP_Text title = UI.Label(header.transform, "CONFIGURAÇÕES", 60, Color.white, TextAnchor.MiddleCenter, true, "Title", LegionTextStyle.Title);
            UI.Place(title.rectTransform, 0.2f, 0.14f, 0.8f, 0.6f);
            title.textWrappingMode = TextWrappingModes.NoWrap;

            for (int i = 0; i < TabDefs.Length; i++) Tab(root, i);

            content = UI.Rect(root, "Content");
            UI.Stretch(content);
            BuildGeneral();
            BuildGraphics();
            BuildAccount();
            BuildNotifications();
            BuildFooter(root);
            BuildNav(root);
        }

        // ---------------------------------------------------------------- abas

        private void Tab(RectTransform root, int index)
        {
            float y0 = 296f + index * 142f;
            Image frame = MainMenuArt.Image(root, "Button_SideFrame", "Tab_" + index, true);
            LegionUI.Place(frame.rectTransform, LegionUI.Edge.TopLeft, 18, y0, 214, y0 + 132);
            Image icon = SettingsArt.Image(frame.transform, TabDefs[index].icon);
            UI.Place(icon.rectTransform, 0.3f, 0.36f, 0.7f, 0.9f);
            TMP_Text label = MainMenuButtons.Label(frame.transform, TabDefs[index].label, 21f, LegionTextStyle.PrimaryButton, Color.white);
            UI.Place(label.rectTransform, 0.04f, 0.06f, 0.96f, 0.34f);
            MainMenuButtons.Make(frame.rectTransform, frame, () => OpenTab(index));
            tabFrames.Add(frame);
        }

        private void OpenTab(int index)
        {
            selectedTab = index;
            switch (index)
            {
                case 4: Ui.Info("IDIOMA", "Português (Brasil)\n\nOutros idiomas chegam em uma próxima atualização."); break;
                case 6: Support(); break;
            }
            RefreshTabs();
        }

        private void RefreshTabs()
        {
            for (int i = 0; i < tabFrames.Count; i++)
                tabFrames[i].sprite = MainMenuArt.Get(i == selectedTab ? "Button_NavFrameSelected" : "Button_SideFrame");
        }

        // ---------------------------------------------------------------- painéis

        private RectTransform Section(string name, string icon, string title, float y0, float y1)
        {
            Image panel = MainMenuArt.Image(content, "Button_SideFrame", name, true);
            RectTransform rect = LegionUI.Place(panel.rectTransform, LegionUI.Edge.TopRight, 224, y0, 922, y1);
            Image art = SettingsArt.Image(content, icon);
            LegionUI.Place(art.rectTransform, LegionUI.Edge.TopRight, 244, y0 + 12, 286, y0 + 54);
            Text(title, 298, y0 + 8, 700, y0 + 58, 30f, LegionTextStyle.PrimaryButton, Color.white);
            return rect;
        }

        // Linha: separador, ícone, rótulo (com subtítulo opcional). Devolve a área livre à direita (em coordenadas do mockup).
        private void Row(string icon, string label, float y0, float y1, string subtitle = null)
        {
            Image line = UI.Panel(content, Line, "Line");
            LegionUI.Place(line.rectTransform, LegionUI.Edge.TopRight, 236, y0, 910, y0 + 2);
            if (icon != null)
            {
                Image art = SettingsArt.Image(content, icon);
                LegionUI.Place(art.rectTransform, LegionUI.Edge.TopRight, 246, y0 + (y1 - y0) * 0.5f - 20, 286, y0 + (y1 - y0) * 0.5f + 20);
            }
            float x0 = icon != null ? 304 : 248;
            TMP_Text text = Text(label, x0, y0, 640, subtitle != null ? y0 + (y1 - y0) * 0.62f : y1, 22f, LegionTextStyle.Menu, Color.white);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            if (subtitle != null)
                Text(subtitle, x0, y0 + (y1 - y0) * 0.52f, 840, y1, 16f, LegionTextStyle.Body, new Color(0.7f, 0.82f, 1f));
        }

        private TMP_Text Text(string text, float x0, float y0, float x1, float y1, float size, LegionTextStyle style, Color color,
            TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            RectTransform rect = LegionUI.Place(UI.Rect(content, "Text"), LegionUI.Edge.TopRight, x0, y0, x1, y1);
            TMP_Text label = UI.Label(rect, text, Mathf.RoundToInt(size * LegionUI.Px), color, anchor, true, "Value", style);
            UI.Stretch(label.rectTransform);
            return label;
        }

        private void BuildGeneral()
        {
            Section("General", "Icon_General", "Geral", 296, 566);
            Volume("Icon_Music", "Música", AudioBus.Music, 352);
            Volume("Icon_Sfx", "Efeitos Sonoros", AudioBus.Sfx, 405);
            Row("Icon_Vibration", "Vibração", 458, 510);
            Toggle(458, 510, () => SaveManager.IsLoaded && SaveManager.Current.settings.haptics, on =>
            {
                if (!SaveManager.IsLoaded) return;
                SaveManager.Current.settings.haptics = on;
                SaveManager.MarkDirty();
                if (on) Haptics.Tap();
            });
            Row("Icon_Battery", "Economia de Bateria", 510, 562);
            Toggle(510, 562, () => Quality != null && Quality.BatterySaver, on => Quality?.SetBatterySaver(on));
        }

        // Música e efeitos: barra de volume + chave liga/desliga (o mudo guarda o volume escolhido).
        private void Volume(string icon, string label, AudioBus bus, float y0)
        {
            float y1 = y0 + 52;
            Row(icon, label, y0, y1);
            RectTransform area = LegionUI.Place(UI.Rect(content, "Volume"), LegionUI.Edge.TopRight, 560, y0 + 10, 806, y1 - 10);
            Slider slider = UI.Slider(area, Audio != null ? Audio.GetVolume(bus) : 1f, v =>
            {
                if (Audio == null) return;
                Audio.SetVolume(bus, v);
                if (v > 0.01f && Audio.IsMuted(bus)) Audio.SetMuted(bus, false);
            });
            UI.Stretch((RectTransform)slider.transform);
            StyleSlider(slider);
            refreshers.Add(() =>
            {
                if (Audio == null) return;
                // O Slider só redesenha quando o valor muda: passa por outro valor para posicionar barra e botão.
                float volume = Audio.GetVolume(bus);
                slider.SetValueWithoutNotify(volume > 0.5f ? 0f : 1f);
                slider.SetValueWithoutNotify(volume);
            });
            Toggle(y0, y1, () => Audio != null && !Audio.IsMuted(bus), on => Audio?.SetMuted(bus, !on));
        }

        private static void StyleSlider(Slider slider)
        {
            Transform t = slider.transform;
            Image background = t.Find("Background").GetComponent<Image>();
            background.color = new Color(0.02f, 0.05f, 0.14f, 0.95f);
            UI.Place(background.rectTransform, 0f, 0.28f, 1f, 0.72f);
            RectTransform fillArea = (RectTransform)t.Find("Fill Area");
            UI.Place(fillArea, 0f, 0.28f, 1f, 0.72f);
            fillArea.offsetMin = new Vector2(4f, 0f);
            fillArea.offsetMax = new Vector2(-4f, 0f);
            fillArea.Find("Fill").GetComponent<Image>().color = Green;
            Image handle = (Image)slider.targetGraphic;
            handle.rectTransform.sizeDelta = new Vector2(34f, 0f);
            handle.color = new Color(0.86f, 0.93f, 1f);
            Outline ring = handle.gameObject.AddComponent<Outline>();
            ring.effectColor = new Color(0.2f, 0.45f, 0.9f);
            ring.effectDistance = new Vector2(2f, -2f);
        }

        private void BuildGraphics()
        {
            Section("Graphics", "Icon_Graphics", "Gráficos", 576, 906);
            Row(null, "Qualidade Gráfica", 630, 682);
            string[] tiers = { "Baixa", "Média", "Alta", "Ultra" };
            Options(tiers, 496, 906, 634, 678, () => Quality != null ? (int)Quality.Current : -1, i => Quality?.SetTier((QualityTier)i, true));

            Row(null, "Taxa de Quadros (FPS)", 684, 736);
            FrameRateMode[] fps = { FrameRateMode.Fps30, FrameRateMode.Fps60, FrameRateMode.Fps120 };
            Options(new[] { "30", "60", "120" }, 590, 906, 688, 732,
                () => Quality == null ? -1 : Quality.FrameRate == FrameRateMode.Auto ? 1 : Array.IndexOf(fps, Quality.FrameRate),
                i => Quality?.SetFrameRate(fps[i]));

            Row("Icon_Effects", "Mostrar Efeitos Especiais", 738, 790);
            Toggle(738, 790, () => Quality == null || Quality.Effects, on => Quality?.SetEffects(on));
            Row("Icon_Shadows", "Sombras", 790, 842);
            Toggle(790, 842, () => Quality == null || Quality.Shadows, on => Quality?.SetShadows(on));
            Row("Icon_Performance", "Modo de Desempenho", 842, 900, "Reduz efeitos visuais para melhor desempenho.");
            Toggle(842, 900, () => Quality != null && Quality.PerformanceMode, on => Quality?.SetPerformanceMode(on));
        }

        // Grupo de botões exclusivos (o escolhido fica azul aceso).
        private void Options(string[] labels, float x0, float x1, float y0, float y1, Func<int> selected, Action<int> choose)
        {
            float gap = 8f;
            float width = (x1 - x0 - gap * (labels.Length - 1)) / labels.Length;
            var buttons = new Image[labels.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                int index = i;
                float bx = x0 + i * (width + gap);
                Image button = UI.Panel(content, Off, "Option_" + labels[i]);
                LegionUI.Place(button.rectTransform, LegionUI.Edge.TopRight, bx, y0, bx + width, y1);
                TMP_Text label = MainMenuButtons.Label(button.transform, labels[i], 21f, LegionTextStyle.PrimaryButton, Color.white);
                UI.Stretch(label.rectTransform);
                MainMenuButtons.Make(button.rectTransform, button, () =>
                {
                    choose(index);
                    RefreshAll();
                });
                buttons[i] = button;
            }
            refreshers.Add(() =>
            {
                int current = selected();
                for (int i = 0; i < buttons.Length; i++)
                {
                    bool on = i == current;
                    buttons[i].color = on ? new Color(0.13f, 0.55f, 1f) : new Color(0.04f, 0.08f, 0.2f, 0.95f);
                    Outline glow = buttons[i].GetComponent<Outline>() ?? buttons[i].gameObject.AddComponent<Outline>();
                    glow.effectColor = on ? new Color(0.5f, 0.85f, 1f) : new Color(0.2f, 0.3f, 0.5f, 0.6f);
                    glow.effectDistance = new Vector2(2f, -2f);
                }
            });
        }

        // Chave liga/desliga no estilo do mockup: trilho arredondado verde/cinza e botão branco que desliza.
        private void Toggle(float y0, float y1, Func<bool> get, Action<bool> set)
        {
            float cy = (y0 + y1) * 0.5f;
            Image track = UI.Panel(content, Off, "Toggle");
            LegionUI.Place(track.rectTransform, LegionUI.Edge.TopRight, 836, cy - 19, 904, cy + 19);
            Image knob = UI.Panel(track.transform, Color.white, "Knob");
            knob.sprite = UIStyle.Circle;
            knob.type = Image.Type.Simple;
            knob.preserveAspect = true;
            RectTransform k = knob.rectTransform;
            MainMenuButtons.Make(track.rectTransform, track, () =>
            {
                set(!get());
                RefreshAll();
            });
            refreshers.Add(() =>
            {
                bool on = get();
                track.color = on ? Green : Off;
                UI.Place(k, on ? 0.48f : 0.04f, 0.08f, on ? 0.96f : 0.52f, 0.92f);
            });
        }

        private void BuildAccount()
        {
            Section("Account", "Icon_Account", "Conta", 918, 1122);
            Row("Icon_Google", "Vincular com Google", 972, 1022);
            ActionButton("Conectar", 972, 1022, () => ComingSoon("Google"));
            Row("Icon_Facebook", "Vincular com Facebook", 1022, 1072);
            ActionButton("Conectar", 1022, 1072, () => ComingSoon("Facebook"));
            Row("Icon_Mail", "Progresso neste aparelho", 1072, 1122);
            Text("Salvo", 600, 1072, 742, 1122, 22f, LegionTextStyle.PrimaryButton, new Color(0.35f, 0.95f, 0.45f), TextAnchor.MiddleCenter);
            ActionButton("Gerenciar", 1072, 1122, ManageProgress);
        }

        private void ActionButton(string text, float y0, float y1, Action onClick)
        {
            Image button = UI.Panel(content, new Color(0.12f, 0.5f, 1f), "Action_" + text);
            LegionUI.Place(button.rectTransform, LegionUI.Edge.TopRight, 750, y0 + 6, 906, y1 - 6);
            Outline ring = button.gameObject.AddComponent<Outline>();
            ring.effectColor = new Color(0.55f, 0.85f, 1f);
            ring.effectDistance = new Vector2(2f, -2f);
            TMP_Text label = MainMenuButtons.Label(button.transform, text, 21f, LegionTextStyle.PrimaryButton, Color.white);
            UI.Stretch(label.rectTransform);
            MainMenuButtons.Make(button.rectTransform, button, onClick);
        }

        private void BuildNotifications()
        {
            Section("Notifications", "Icon_Bell", "Notificações", 1134, 1398);
            for (int i = 0; i < Notifications.Length; i++)
            {
                var n = Notifications[i];
                float y0 = 1186 + i * 42f;
                Row(n.icon, n.label, y0, y0 + 42);
                string key = NotificationKey + n.key;
                bool fallback = n.def;
                Toggle(y0, y0 + 42, () => PlayerPrefs.GetInt(key, fallback ? 1 : 0) == 1, on =>
                {
                    PlayerPrefs.SetInt(key, on ? 1 : 0);
                    PlayerPrefs.Save();
                });
            }
        }

        private void BuildFooter(RectTransform root)
        {
            FooterButton(root, "Política de Privacidade", 28, 310, new Color(0.12f, 0.5f, 1f), () => Ui.Info("POLÍTICA DE PRIVACIDADE",
                "O Legion Rush guarda seu progresso só neste aparelho. Nenhum dado pessoal é coletado ou enviado."));
            FooterButton(root, "Termos de Uso", 336, 576, new Color(0.12f, 0.5f, 1f), () => Ui.Info("TERMOS DE USO",
                "Jogo em desenvolvimento, oferecido como está para testes. Moedas e itens não têm valor fora do jogo."));
            Image exit = FooterButton(root, "Sair da Conta", 666, 912, new Color(0.88f, 0.15f, 0.18f), () => Ui.Info("CONTA",
                "Você está jogando como convidado: o progresso fica salvo neste aparelho e não há conta para sair."));
            Image icon = SettingsArt.Image(exit.transform, "Icon_Logout");
            UI.Place(icon.rectTransform, 0.08f, 0.2f, 0.22f, 0.8f);

            RectTransform version = LegionUI.Place(UI.Rect(root, "Version"), LegionUI.Edge.TopRight, 700, 1474, 912, 1506);
            TMP_Text text = UI.Label(version, "Versão " + Application.version, Mathf.RoundToInt(18 * LegionUI.Px), new Color(0.72f, 0.8f, 0.95f),
                TextAnchor.MiddleRight, true, "Value", LegionTextStyle.Body);
            UI.Stretch(text.rectTransform);
        }

        private Image FooterButton(RectTransform root, string text, float x0, float x1, Color color, Action onClick)
        {
            Image button = UI.Panel(root, color, "Footer_" + text);
            LegionUI.Place(button.rectTransform, LegionUI.Edge.TopRight, x0, 1410, x1, 1466);
            Outline ring = button.gameObject.AddComponent<Outline>();
            ring.effectColor = Color.Lerp(color, Color.white, 0.45f);
            ring.effectDistance = new Vector2(2f, -2f);
            TMP_Text label = MainMenuButtons.Label(button.transform, text, 22f, LegionTextStyle.PrimaryButton, Color.white);
            UI.Place(label.rectTransform, 0.06f, 0f, 0.94f, 1f);
            MainMenuButtons.Make(button.rectTransform, button, onClick);
            return button;
        }

        private void BuildNav(RectTransform root)
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
                    Ui.Replace<MainMenuScreen>();
                    if (destination != MainMenuDestination.Home && destination != MainMenuDestination.More)
                        new UIRootMainMenuNavigator(Ui).Open(destination);
                });
                LegionUI.Place((RectTransform)button.transform, LegionUI.Edge.Bottom, x0[i], 1506, x0[i] + 178, 1662);
                nav.Add(button.gameObject);
            }
        }

        // ---------------------------------------------------------------- ações

        // Suporte: copia o código do progresso (backup ou troca de aparelho).
        private void Support()
        {
            Ui.Confirm("PROGRESSO E SUPORTE",
                "Copie um código com todo o seu progresso para guardar ou levar a outro aparelho.", "COPIAR CÓDIGO", () =>
                {
                    GUIUtility.systemCopyBuffer = SaveManager.Export();
                    Ui.Toast("Código copiado");
                });
        }

        // Gerenciar: importar um código copiado antes (substitui o progresso; um backup é criado).
        private void ManageProgress()
        {
            Ui.Confirm("IMPORTAR PROGRESSO", "Copie o código do progresso e toque em IMPORTAR.\nO progresso atual é substituído (um backup é criado).",
                "IMPORTAR", () =>
                {
                    bool ok = SaveManager.Import(GUIUtility.systemCopyBuffer, out string message);
                    Ui.Toast(message);
                    if (ok) Ui.Game.ReturnToMenu();
                });
        }

        private void ComingSoon(string service) =>
            Ui.Info("CONTA", $"Vincular com {service} chega em uma próxima atualização.\n\nPor enquanto o progresso fica salvo neste aparelho.");

        private static AudioManager Audio => AudioManager.Instance;
        private VisualQualityManager Quality => Ui.Game != null ? Ui.Game.Quality : null;

        public override void Refresh()
        {
            if (!SaveManager.IsLoaded) return;
            MainMenuModel model = new GameMainMenuData(Ui.Game).Snapshot();
            profile.Set(model.PlayerName, model.PlayerLevel, model.Xp, model.XpMax);
            gold.Set(Economy.Gold);
            gems.Set(Economy.Gems);
            RefreshTabs();
            RefreshAll();
            // Aberta pela pausa no meio da partida: sem barra de navegação (voltar retorna à pausa).
            LevelManager level = Ui.Game != null ? Ui.Game.CurrentLevel : null;
            bool inBattle = level != null && level.Current == LevelManager.State.Running;
            foreach (GameObject button in nav) button.SetActive(!inBattle);
        }

        private void RefreshAll()
        {
            foreach (Action refresh in refreshers) refresh();
        }
    }

    // Ícones da tela de configurações (Assets/LegionRush/UI/Settings/Resources/SettingsArt/<nome>).
    public static class SettingsArt
    {
        private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        public static Image Image(Transform parent, string sprite)
        {
            if (!cache.TryGetValue(sprite, out Sprite s))
            {
                s = Resources.Load<Sprite>("SettingsArt/" + sprite);
                cache[sprite] = s;
            }
            RectTransform rect = UI.Rect(parent, sprite);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = s;
            image.preserveAspect = true;
            image.raycastTarget = false;
            if (s == null) image.color = Color.clear;
            return image;
        }
    }
}
