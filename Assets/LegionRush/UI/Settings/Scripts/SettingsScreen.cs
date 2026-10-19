using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LegionRush
{
    // Configurações (auditoria 1d): a coluna lateral troca o painel, uma seção por vez, com mais espaço e fonte maior.
    // Seletores com LegionTabs e chaves com LegionToggle. Só o que existe no jogo: áudio, vibração, bateria, gráficos,
    // notificações locais, "Progresso neste aparelho: Salvo" com exportar/importar código. Google e Facebook ficam com o
    // selo EM BREVE; "Sair da Conta", Clã/Amigos e "Ataques e Defesas" saíram. Privacidade, Termos e Versão estão em SUPORTE.
    // Tudo aplica na hora: volumes no AudioManager, qualidade no VisualQualityManager, vibração no save.
    public class SettingsScreen : UIScreen
    {
        private const string NotificationKey = "LR_Notify_";
        private static readonly Color Line = new Color(1f, 1f, 1f, 0.07f);
        private static readonly Color Green = new Color(0.22f, 0.8f, 0.36f);

        // Painel à direita e coluna à esquerda, no grid do mockup.
        private const float PanelX0 = 232f, PanelX1 = 922f, PanelY0 = 246f, PanelY1 = 1490f;
        private const float RowH = 74f;

        private static readonly (string icon, string label)[] TabDefs =
        {
            ("Icon_General", "GERAL"), ("Icon_Graphics", "GRÁFICOS"), ("Icon_Audio", "ÁUDIO"), ("Icon_Account", "CONTA"),
            ("Icon_Bell", "AVISOS"), ("Icon_Support", "SUPORTE")
        };

        private static readonly (string icon, string label, string key, bool def)[] Notifications =
        {
            ("Icon_Chest", "Recompensas e Eventos", "rewards", true), ("Icon_News", "Novidades do Jogo", "news", false),
            ("Icon_Gift", "Promoções e Ofertas", "offers", true)
        };

        private MainMenuProfilePanel profile;
        private MainMenuCurrencyBar gold, gems;
        private LegionTabs tabs;
        private RectTransform content;
        private readonly List<Action> refreshers = new List<Action>();
        private readonly List<GameObject> nav = new List<GameObject>();
        private int selectedTab;
        private float y;

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

            LegionHeader.Build(root, "CONFIGURAÇÕES", OnBack);

            // Coluna lateral: troca a seção do painel.
            RectTransform column = LegionUI.Place(UI.Rect(root, "Tabs"), LegionUI.Edge.TopLeft, 18, PanelY0, 220, PanelY0 + 6 * 132);
            tabs = LegionTabs.CreateVertical(column, Array.ConvertAll(TabDefs, t => t.label), OpenTab, 22f, 10f);
            for (int i = 0; i < TabDefs.Length; i++)
            {
                Transform inner = column.GetChild(i).Find("Inner");
                HorizontalLayoutGroup row = inner.GetComponent<HorizontalLayoutGroup>();
                if (row != null) UnityEngine.Object.DestroyImmediate(row);
                TMP_Text label = inner.GetComponentInChildren<TMP_Text>();
                UI.Place(label.rectTransform, 0f, 0.02f, 1f, 0.36f);
                Image icon = SettingsArt.Image(inner, TabDefs[i].icon);
                UI.Place(icon.rectTransform, 0.25f, 0.38f, 0.75f, 0.96f);
            }

            Image panel = LegionPanel.Create(root, "Panel");
            LegionUI.Place(panel.rectTransform, LegionUI.Edge.TopRight, PanelX0, PanelY0, PanelX1, PanelY1);
            content = UI.Rect(root, "Content");
            UI.Stretch(content);

            BuildNav(root);
            OpenTab(0);
        }

        // ---------------------------------------------------------------- seções

        private void OpenTab(int index)
        {
            selectedTab = index;
            tabs.Select(index);
            UI.Clear(content);
            refreshers.Clear();
            y = PanelY0 + 20f;
            switch (index)
            {
                case 1: BuildGraphics(); break;
                case 2: BuildAudio(); break;
                case 3: BuildAccount(); break;
                case 4: BuildNotifications(); break;
                case 5: BuildSupport(); break;
                default: BuildGeneral(); break;
            }
            RefreshAll();
        }

        private void Heading(string icon, string title)
        {
            Image art = SettingsArt.Image(content, icon);
            LegionUI.Place(art.rectTransform, LegionUI.Edge.TopRight, 256, y + 6, 310, y + 60);
            Text(title, 324, y, 900, y + 66, 34f, LegionTextStyle.PrimaryButton, Color.white);
            y += 80f;
        }

        // Linha: separador, ícone e rótulo; devolve o topo da linha.
        private float Row(string icon, string label, string subtitle = null)
        {
            float y0 = y;
            float y1 = y + RowH;
            Image line = UI.Panel(content, Line, "Line");
            LegionUI.Place(line.rectTransform, LegionUI.Edge.TopRight, 248, y0, 906, y0 + 2);
            if (icon != null)
            {
                Image art = SettingsArt.Image(content, icon);
                LegionUI.Place(art.rectTransform, LegionUI.Edge.TopRight, 256, y0 + RowH * 0.5f - 22, 300, y0 + RowH * 0.5f + 22);
            }
            float x0 = icon != null ? 316 : 260;
            TMP_Text text = Text(label, x0, y0, 800, subtitle != null ? y0 + RowH * 0.6f : y1, 26f, LegionTextStyle.Menu, Color.white);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            if (subtitle != null) Text(subtitle, x0, y0 + RowH * 0.5f, 900, y1, 22f, LegionTextStyle.Body, new Color(0.7f, 0.82f, 1f));
            y = y1;
            return y0;
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
            Heading("Icon_General", "Geral");
            float row = Row("Icon_Vibration", "Vibração");
            Toggle(row, () => SaveManager.IsLoaded && SaveManager.Current.settings.haptics, on =>
            {
                if (!SaveManager.IsLoaded) return;
                SaveManager.Current.settings.haptics = on;
                SaveManager.MarkDirty();
                if (on) Haptics.Tap();
            });
            row = Row("Icon_Battery", "Economia de Bateria");
            Toggle(row, () => Quality != null && Quality.BatterySaver, on => Quality?.SetBatterySaver(on));
            row = Row("Icon_Language", "Idioma");
            Text("Português (BR)", 600, row, 900, row + RowH, 24f, LegionTextStyle.Hud, MenuChrome.GoldText, TextAnchor.MiddleRight);
        }

        private void BuildAudio()
        {
            Heading("Icon_Audio", "Áudio");
            Volume("Icon_Music", "Música", AudioBus.Music);
            Volume("Icon_Sfx", "Efeitos Sonoros", AudioBus.Sfx);
        }

        // Volume: rótulo + chave na primeira linha, barra larga na segunda (o mudo guarda o volume escolhido).
        private void Volume(string icon, string label, AudioBus bus)
        {
            float row = Row(icon, label);
            Toggle(row, () => Audio != null && !Audio.IsMuted(bus), on => Audio?.SetMuted(bus, !on));
            RectTransform area = LegionUI.Place(UI.Rect(content, "Volume"), LegionUI.Edge.TopRight, 316, y + 8, 900, y + 58);
            y += 70f;
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
            handle.rectTransform.sizeDelta = new Vector2(44f, 0f);
            handle.color = new Color(0.86f, 0.93f, 1f);
            Outline ring = handle.gameObject.AddComponent<Outline>();
            ring.effectColor = new Color(0.2f, 0.45f, 0.9f);
            ring.effectDistance = new Vector2(2f, -2f);
        }

        private void BuildGraphics()
        {
            Heading("Icon_Graphics", "Gráficos");
            Row(null, "Qualidade Gráfica");
            string[] tiers = { "Baixa", "Média", "Alta", "Ultra" };
            Options(tiers, () => Quality != null ? (int)Quality.Current : -1, i => Quality?.SetTier((QualityTier)i, true));

            Row(null, "Taxa de Quadros (FPS)");
            FrameRateMode[] fps = { FrameRateMode.Fps30, FrameRateMode.Fps60, FrameRateMode.Fps120 };
            Options(new[] { "30", "60", "120" },
                () => Quality == null ? -1 : Quality.FrameRate == FrameRateMode.Auto ? 1 : Array.IndexOf(fps, Quality.FrameRate),
                i => Quality?.SetFrameRate(fps[i]));

            float row = Row("Icon_Effects", "Efeitos Especiais");
            Toggle(row, () => Quality == null || Quality.Effects, on => Quality?.SetEffects(on));
            row = Row("Icon_Shadows", "Sombras");
            Toggle(row, () => Quality == null || Quality.Shadows, on => Quality?.SetShadows(on));
            row = Row("Icon_Performance", "Modo Desempenho", "Menos efeitos, mais fluidez.");
            Toggle(row, () => Quality != null && Quality.PerformanceMode, on => Quality?.SetPerformanceMode(on));
        }

        // Seletor (LegionTabs): a opção escolhida fica acesa, nunca branca.
        private void Options(string[] labels, Func<int> selected, Action<int> choose)
        {
            RectTransform area = LegionUI.Place(UI.Rect(content, "Options"), LegionUI.Edge.TopRight, 260, y + 4, 900, y + 70);
            y += 82f;
            LegionTabs options = LegionTabs.Create(area, labels, i =>
            {
                choose(i);
                RefreshAll();
            }, 24f, 8f);
            refreshers.Add(() => options.Select(selected()));
        }

        private void Toggle(float rowTop, Func<bool> get, Action<bool> set)
        {
            LegionToggle toggle = LegionToggle.Create(content, get, on =>
            {
                set(on);
                RefreshAll();
            });
            LegionUI.Place((RectTransform)toggle.transform, LegionUI.Edge.TopRight, 806, rowTop + 14, 900, rowTop + RowH - 14);
            refreshers.Add(toggle.Refresh);
        }

        private void BuildAccount()
        {
            Heading("Icon_Account", "Conta");
            float row = Row("Icon_Mail", "Progresso neste aparelho");
            Text("Salvo", 700, row, 900, row + RowH, 26f, LegionTextStyle.PrimaryButton, new Color(0.35f, 0.95f, 0.45f), TextAnchor.MiddleRight);
            row = Row(null, "Código de progresso", "Guarde ou leve para outro aparelho.");
            y += 10f;
            ActionButton("EXPORTAR", 260, 572, ExportProgress, LegionBuyButton.Variant.Blue);
            ActionButton("IMPORTAR", 588, 900, ManageProgress, LegionBuyButton.Variant.Green);
            y += 90f;
            row = Row("Icon_Google", "Vincular com Google");
            SoonSeal(row);
            row = Row("Icon_Facebook", "Vincular com Facebook");
            SoonSeal(row);
        }

        private void SoonSeal(float rowTop)
        {
            RectTransform holder = LegionUI.Place(UI.Rect(content, "Soon"), LegionUI.Edge.TopRight, 760, rowTop + 32, 900, rowTop + 32);
            RectTransform seal = LegionBadge.Soon(holder);
            seal.pivot = new Vector2(1f, 0.5f);
            seal.anchoredPosition = Vector2.zero;
        }

        private void ActionButton(string text, float x0, float x1, Action onClick, LegionBuyButton.Variant variant)
        {
            LegionBuyButton button = LegionBuyButton.Create(content, variant, LegionCurrency.None, 0, text, onClick, "Action_" + text);
            LegionUI.Place((RectTransform)button.transform, LegionUI.Edge.TopRight, x0, y, x1, y + 76);
        }

        private void BuildNotifications()
        {
            Heading("Icon_Bell", "Notificações");
            foreach (var n in Notifications)
            {
                float row = Row(n.icon, n.label);
                string key = NotificationKey + n.key;
                bool fallback = n.def;
                Toggle(row, () => PlayerPrefs.GetInt(key, fallback ? 1 : 0) == 1, on =>
                {
                    PlayerPrefs.SetInt(key, on ? 1 : 0);
                    PlayerPrefs.Save();
                });
            }
        }

        private void BuildSupport()
        {
            Heading("Icon_Support", "Suporte");
            y += 10f;
            ActionButton("POLÍTICA DE PRIVACIDADE", 260, 900, () => Ui.Info("POLÍTICA DE PRIVACIDADE",
                "O Legion Rush guarda seu progresso só neste aparelho. Nenhum dado pessoal é coletado ou enviado."), LegionBuyButton.Variant.Blue);
            y += 96f;
            ActionButton("TERMOS DE USO", 260, 900, () => Ui.Info("TERMOS DE USO",
                "Jogo em desenvolvimento, oferecido como está para testes. Moedas e itens não têm valor fora do jogo."), LegionBuyButton.Variant.Blue);
            y += 110f;
            Row(null, "Versão");
            Text(Application.version, 600, y - RowH, 900, y, 26f, LegionTextStyle.Hud, MenuChrome.GoldText, TextAnchor.MiddleRight);
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

        // Exportar: copia um código com todo o progresso (backup ou troca de aparelho).
        private void ExportProgress()
        {
            GUIUtility.systemCopyBuffer = SaveManager.Export();
            Ui.Toast("Código copiado");
        }

        // Importar um código copiado antes (substitui o progresso; um backup é criado).
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

        private static AudioManager Audio => AudioManager.Instance;
        private VisualQualityManager Quality => Ui != null && Ui.Game != null ? Ui.Game.Quality : null;

        public override void Refresh()
        {
            if (!SaveManager.IsLoaded) return;
            if (Ui != null && Ui.Game != null)
            {
                MainMenuModel model = new GameMainMenuData(Ui.Game).Snapshot();
                profile.Set(model.PlayerName, model.PlayerLevel, model.Xp, model.XpMax);
            }
            gold.Set(Economy.Gold);
            gems.Set(Economy.Gems);
            OpenTab(selectedTab);
            // Aberta pela pausa no meio da partida: sem barra de navegação (voltar retorna à pausa).
            LevelManager level = Ui != null && Ui.Game != null ? Ui.Game.CurrentLevel : null;
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
