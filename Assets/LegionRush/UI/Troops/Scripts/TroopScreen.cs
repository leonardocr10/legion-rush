using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LegionRush
{
    // Tela de tropas no layout do mockup (941×1672): topo com perfil e carteira, título e abas de raridade,
    // grade de cartas das classes, painel da tropa escolhida (atributos, nível, MELHORAR, traços) e a barra de navegação.
    public class TroopScreen : UIScreen
    {
        private static readonly Vector4 GridBox = new Vector4(14, 350, 928, 995);
        private static readonly Vector4 PanelBox = new Vector4(14, 1008, 928, 1478);
        private static readonly Vector4 ShowBox = new Vector4(18, 1013, 378, 1474);

        private static readonly Vector4[] Slots =
        {
            new Vector4(16, 357, 234, 662), new Vector4(249, 357, 461, 662), new Vector4(482, 357, 694, 662), new Vector4(715, 357, 927, 662),
            new Vector4(18, 684, 229, 987), new Vector4(249, 684, 461, 987), new Vector4(482, 684, 694, 987), new Vector4(715, 684, 927, 987)
        };

        // Ordem das cartas e visual de cada classe (cor do fundo da arte no mockup).
        private static readonly (UnitClass unitClass, string art, Color frame, Color edge)[] Classes =
        {
            (UnitClass.Warrior, "Warrior", new Color32(14, 46, 120, 255), new Color32(70, 140, 255, 255)),
            (UnitClass.Archer, "Archer", new Color32(14, 80, 48, 255), new Color32(60, 200, 120, 255)),
            (UnitClass.Lancer, "Lancer", new Color32(14, 46, 120, 255), new Color32(70, 140, 255, 255)),
            (UnitClass.Knight, "Knight", new Color32(56, 24, 110, 255), new Color32(170, 90, 255, 255)),
            (UnitClass.Mage, "Mage", new Color32(56, 24, 110, 255), new Color32(170, 90, 255, 255)),
            (UnitClass.Healer, "Healer", new Color32(110, 52, 14, 255), new Color32(255, 170, 60, 255))
        };

        private static readonly (string label, TroopRarity? rarity)[] TabDefs =
        {
            ("TODAS", null), ("COMUNS", TroopRarity.Common), ("RARAS", TroopRarity.Rare), ("ÉPICAS", TroopRarity.Epic), ("LENDÁRIAS", TroopRarity.Legendary)
        };

        private static ArmyConfig defaults;

        private MainMenuProfilePanel profile;
        private MainMenuCurrencyBar gold, gems;
        private readonly List<(Image image, TMP_Text label)> tabs = new List<(Image, TMP_Text)>();
        private TroopRarity? filter;
        private UnitClass viewing = UnitClass.Warrior;
        private RectTransform grid, panel;

        public override bool AllowBack => true;

        private static ArmyConfig Defaults => defaults != null ? defaults : defaults = ArmyConfig.CreateDefault();

        protected override void Build(RectTransform root)
        {
            BuildBackground(root);

            profile = MainMenuProfilePanel.Create(root, null);
            LegionUI.Place((RectTransform)profile.transform, LegionUI.Edge.TopLeft, 14, 12, 394, 120);
            gold = MainMenuCurrencyBar.Create(root, "Icon_Coin", () => Ui.Push<ShopScreen>().Focus(ShopScreen.SectionChests), "Gold");
            LegionUI.Place((RectTransform)gold.transform, LegionUI.Edge.Top, 403, 30, 630, 92);
            gems = MainMenuCurrencyBar.Create(root, "Icon_Gem", () => Ui.Push<ShopScreen>().Focus(ShopScreen.SectionSkins), "Gems");
            LegionUI.Place((RectTransform)gems.transform, LegionUI.Edge.Top, 638, 30, 852, 92);
            Image settings = MainMenuArt.Image(root, "Button_Settings", "Settings");
            LegionUI.Place(settings.rectTransform, LegionUI.Edge.TopRight, 860, 22, 928, 92);
            MainMenuButtons.Make(settings.rectTransform, settings, () => Ui.Push<SettingsScreen>());

            Image back = HeroesArt.Image(root, "Button_Back", "Back");
            LegionUI.Place(back.rectTransform, LegionUI.Edge.TopLeft, 20, 150, 106, 236);
            MainMenuButtons.Make(back.rectTransform, back, OnBack);

            Image title = TroopsArt.Image(root, "Title_Troops", "Title");
            LegionUI.Place(title.rectTransform, LegionUI.Edge.Top, 160, 104, 780, 258);

            float[] x0 = { 22, 207, 387, 568, 750 };
            float[] x1 = { 193, 372, 555, 737, 918 };
            for (int i = 0; i < TabDefs.Length; i++)
            {
                TroopRarity? rarity = TabDefs[i].rarity;
                RectTransform rect = LegionUI.Place(UI.Rect(root, "Tab_" + i), LegionUI.Edge.Top, x0[i], 268, x1[i], 334);
                Image image = rect.gameObject.AddComponent<Image>();
                image.type = Image.Type.Sliced;
                TMP_Text label = MainMenuButtons.Label(rect, TabDefs[i].label, 24f, LegionTextStyle.PrimaryButton, Color.white);
                UI.Stretch(label.rectTransform, 8f, 8f, 4f, 8f);
                MainMenuButtons.Make(rect, image, () =>
                {
                    filter = rarity;
                    Refresh();
                });
                tabs.Add((image, label));
            }

            // A grade fica centrada no espaço entre as abas e o painel (em telas altas sobra espaço dos dois lados).
            RectTransform area = UI.Rect(root, "GridArea");
            area.offsetMin = new Vector2(0f, (LegionUI.RefHeight - PanelBox.y + 6f) * LegionUI.Px);
            area.offsetMax = new Vector2(0f, -(GridBox.y - 6f) * LegionUI.Px);
            grid = UI.Rect(area, "Grid");
            grid.anchorMin = grid.anchorMax = new Vector2(0.5f, 0.5f);
            grid.sizeDelta = new Vector2((GridBox.z - GridBox.x) * LegionUI.Px, (GridBox.w - GridBox.y) * LegionUI.Px);
            grid.anchoredPosition = new Vector2(((GridBox.x + GridBox.z) * 0.5f - LegionUI.RefWidth * 0.5f) * LegionUI.Px, 0f);
            panel = LegionUI.Place(UI.Rect(root, "Panel"), LegionUI.Edge.Bottom, PanelBox.x, PanelBox.y, PanelBox.z, PanelBox.w);

            BuildNav(root);
            Economy.WalletChanged += (g, m) => { if (Root != null && Root.gameObject.activeInHierarchy) Refresh(); };
        }

        private static void BuildBackground(RectTransform root)
        {
            Image fill = root.gameObject.AddComponent<Image>();
            fill.color = new Color(0.03f, 0.06f, 0.17f);
            Image scenery = LegionArt.Image(root, "Backgrounds/bg_frozen_castle", false, "Scenery");
            scenery.preserveAspect = false;
            AspectRatioFitter fitter = scenery.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = scenery.sprite != null ? scenery.sprite.rect.width / scenery.sprite.rect.height : 0.56f;
            scenery.color = new Color(0.42f, 0.5f, 0.85f);
            Image shade = UI.Panel(root, new Color(0.02f, 0.05f, 0.18f, 0.62f), "Shade");
            UI.Stretch(shade.rectTransform);
            shade.raycastTarget = false;
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
                MainMenuNavButton button = MainMenuNavButton.Create(root, items[i].icon, items[i].label, () => Navigate(destination));
                LegionUI.Place((RectTransform)button.transform, LegionUI.Edge.Bottom, x0[i], 1494, x0[i] + 178, 1660);
                button.SetSelected(destination == MainMenuDestination.Troops);
            }
        }

        private void Navigate(MainMenuDestination destination)
        {
            if (destination == MainMenuDestination.Troops) return;
            Ui.Replace<MainMenuScreen>();
            if (destination != MainMenuDestination.Home && destination != MainMenuDestination.More)
                new UIRootMainMenuNavigator(Ui).Open(destination);
        }

        // ---------------------------------------------------------------- dados

        public override void Refresh()
        {
            if (!SaveManager.IsLoaded) return;
            SaveData save = SaveManager.Current;

            if (Ui != null && Ui.Game != null)
            {
                MainMenuModel model = new GameMainMenuData(Ui.Game).Snapshot();
                profile.Set(model.PlayerName, model.PlayerLevel, model.Xp, model.XpMax);
            }
            gold.Set(Economy.Gold);
            gems.Set(Economy.Gems);

            for (int i = 0; i < tabs.Count; i++) StyleTab(tabs[i].image, TabDefs[i].rarity == filter);

            var list = new List<int>();
            for (int i = 0; i < Classes.Length; i++)
            {
                if (filter == null || Stats(Classes[i].unitClass).rarity == filter) list.Add(i);
            }
            if (list.Count > 0 && !list.Exists(i => Classes[i].unitClass == viewing)) viewing = Classes[list[0]].unitClass;

            BuildCards(list, save);
            BuildPanel(save);
        }

        private static void StyleTab(Image image, bool on)
        {
            image.sprite = on ? LegionArt.Get("Buttons/btn_blue") : UIStyle.Rounded;
            image.color = on ? Color.white : new Color32(14, 34, 78, 240);
            Outline outline = image.GetComponent<Outline>() ?? image.gameObject.AddComponent<Outline>();
            outline.effectColor = on ? new Color(0.5f, 0.85f, 1f, 0.8f) : new Color32(50, 90, 160, 200);
            outline.effectDistance = new Vector2(2f, -2f);
        }

        private static UnitStats Stats(UnitClass unitClass) => Defaults.Get(unitClass);
        private static float LevelScale(int level) => 1f + Defaults.levelScaling * (level - 1);
        private int MaxLevel => Ui != null && Ui.Game != null ? Ui.Game.MaxTroopLevel : 10;

        // ---------------------------------------------------------------- cartas

        private void BuildCards(List<int> list, SaveData save)
        {
            UI.Clear(grid);
            if (list.Count == 0)
            {
                string name = TabDefs[Array.FindIndex(TabDefs, t => t.rarity == filter)].label.ToLowerInvariant();
                TMP_Text empty = LegionUI.Text(grid, GridBox, 40, 560, 900, 640, $"Nenhuma tropa {name.TrimEnd('s')} ainda", 30f,
                    LegionTextStyle.Menu, new Color(0.78f, 0.86f, 1f));
                empty.textWrappingMode = TextWrappingModes.Normal;
                return;
            }

            for (int n = 0; n < list.Count && n < Slots.Length; n++)
            {
                var entry = Classes[list[n]];
                UnitClass unitClass = entry.unitClass;
                int c = (int)unitClass;
                bool unlocked = save.troops.unlocked[c];
                int level = save.troops.levels[c];
                Vector4 box = Slots[n];
                RectTransform card = LegionUI.Inside(UI.Rect(grid, "Card_" + entry.art), GridBox, box.x, box.y, box.z, box.w);

                Image frame = Box(card, unlocked ? entry.frame : new Color32(46, 48, 58, 255), unlocked ? entry.edge : new Color32(130, 134, 150, 255), 3f);
                frame.raycastTarget = true;

                Image portrait = TroopsArt.Image(card, "Portrait_" + entry.art, "Portrait");
                portrait.preserveAspect = false;
                LegionUI.Inside(portrait.rectTransform, box, box.x + 7, box.y + 7, box.z - 7, box.y + 190);
                if (!unlocked) portrait.color = new Color(0.4f, 0.42f, 0.48f);

                Image fade = MainMenuArt.Image(card, "Shade_Vertical", "Fade");
                fade.preserveAspect = false;
                fade.color = unlocked ? entry.frame : new Color32(46, 48, 58, 255);
                LegionUI.Inside(fade.rectTransform, box, box.x + 7, box.y + 130, box.z - 7, box.y + 192);

                Image star = TroopsArt.Image(card, StarSprite(Stats(unitClass).rarity), "Rarity");
                LegionUI.Inside(star.rectTransform, box, box.z - 56, box.y + 10, box.z - 12, box.y + 54);

                TMP_Text name = MainMenuButtons.Label(card, ArmyConfig.ClassName(unitClass), 27f, LegionTextStyle.PrimaryButton, Color.white);
                LegionUI.Inside(name.rectTransform, box, box.x + 6, box.y + 188, box.z - 6, box.y + 226);

                if (unlocked)
                {
                    TMP_Text lvl = MainMenuButtons.Label(card, "Nível " + level, 22f, LegionTextStyle.Hud, new Color(0.78f, 0.88f, 1f));
                    LegionUI.Inside(lvl.rectTransform, box, box.x + 6, box.y + 224, box.z - 6, box.y + 254);
                    bool max = level >= MaxLevel;
                    int cost = MetaCatalog.TroopLevelCost(level);
                    Image fill = LegionUI.ProgressBar(card, box, box.x + 16, box.y + 260, box.z - 50, box.y + 292, new Color(0.15f, 0.62f, 1f));
                    LegionUI.SetProgress(fill, max ? 1f : Mathf.Clamp01(Economy.Gold / (float)cost));
                    string count = max ? "MÁX." : $"{MainMenuCurrencyBar.Short(Mathf.Min(Economy.Gold, cost))}/{MainMenuCurrencyBar.Short(cost)}";
                    TMP_Text text = MainMenuButtons.Label(card, count, 20f, LegionTextStyle.Hud, Color.white);
                    LegionUI.Inside(text.rectTransform, box, box.x + 16, box.y + 260, box.z - 50, box.y + 292);
                    if (!max && Economy.Gold >= cost)
                    {
                        Image arrow = HeroesArt.Image(card, "Icon_UpgradeArrow", "Upgrade");
                        LegionUI.Inside(arrow.rectTransform, box, box.z - 50, box.y + 252, box.z - 10, box.y + 298);
                        arrow.gameObject.AddComponent<UIBreath>().amount = 0.05f;
                    }
                }
                else
                {
                    Image lockIcon = HeroesArt.Image(card, "Icon_Lock", "Lock");
                    float cx = (box.x + box.z) * 0.5f;
                    LegionUI.Inside(lockIcon.rectTransform, box, cx - 34, box.y + 70, cx + 34, box.y + 150);
                    TMP_Text lvl = MainMenuButtons.Label(card, "Bloqueado", 22f, LegionTextStyle.Hud, new Color(0.75f, 0.77f, 0.84f));
                    LegionUI.Inside(lvl.rectTransform, box, box.x + 6, box.y + 224, box.z - 6, box.y + 254);
                    Image pill = UI.Panel(card, new Color(0.04f, 0.05f, 0.1f, 0.9f), "Price");
                    LegionUI.Inside(pill.rectTransform, box, box.x + 18, box.y + 260, box.z - 18, box.y + 294);
                    Outline outline = pill.gameObject.AddComponent<Outline>();
                    outline.effectColor = new Color(0.75f, 0.77f, 0.84f, 0.8f);
                    TMP_Text price = MainMenuButtons.Label(pill.transform, UnlockPrice(c), 20f, LegionTextStyle.Hud, Color.white);
                    UI.Stretch(price.rectTransform);
                }

                if (unitClass == viewing)
                {
                    Image glow = LegionArt.Image(card, "Frames/frame_gold", false, "Selected");
                    glow.type = Image.Type.Sliced;
                    glow.fillCenter = false;
                    glow.pixelsPerUnitMultiplier = 2.2f;
                    UI.Stretch(glow.rectTransform, -6f, -6f, -6f, -6f);
                    glow.raycastTarget = false;
                }

                MainMenuButtons.Make(card, frame, () =>
                {
                    viewing = unitClass;
                    Refresh();
                });
            }
        }

        private static string StarSprite(TroopRarity rarity) => rarity switch
        {
            TroopRarity.Rare => "Star_Green",
            TroopRarity.Epic => "Star_Purple",
            TroopRarity.Legendary => "Star_Gold",
            _ => "Star_Blue"
        };

        private static Color RarityChip(TroopRarity rarity) => rarity switch
        {
            TroopRarity.Rare => new Color(0.16f, 0.62f, 0.32f),
            TroopRarity.Epic => new Color(0.52f, 0.25f, 0.9f),
            TroopRarity.Legendary => new Color(0.95f, 0.6f, 0.1f),
            _ => new Color(0.12f, 0.45f, 0.95f)
        };

        private static string UnlockPrice(int c)
        {
            int goldCost = MetaCatalog.ClassUnlockGold[c];
            int gemCost = MetaCatalog.ClassUnlockGems[c];
            if (gemCost > 0) return MainMenuCurrencyBar.Short(gemCost) + " GEMAS";
            return goldCost > 0 ? MainMenuCurrencyBar.Short(goldCost) + " OURO" : "GRÁTIS";
        }

        private static Image Box(RectTransform rect, Color color, Color edge, float thickness)
        {
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = UIStyle.Rounded;
            image.type = Image.Type.Sliced;
            image.color = color;
            image.raycastTarget = false;
            Outline outline = rect.gameObject.AddComponent<Outline>();
            outline.effectColor = edge;
            outline.effectDistance = new Vector2(thickness, -thickness);
            return image;
        }

        // ---------------------------------------------------------------- painel

        private void BuildPanel(SaveData save)
        {
            UI.Clear(panel);
            int c = (int)viewing;
            var entry = Array.Find(Classes, e => e.unitClass == viewing);
            UnitStats stats = Stats(viewing);
            bool unlocked = save.troops.unlocked[c];
            int level = save.troops.levels[c];
            bool max = level >= MaxLevel;

            Box(panel, new Color32(10, 28, 66, 238), new Color32(60, 110, 190, 200), 3f);

            // Vitrine: arte grande do guerreiro; as outras classes usam o retrato da carta sobre o fundo da cor da classe.
            RectTransform show = LegionUI.Inside(UI.Rect(panel, "Showcase"), PanelBox, ShowBox.x, ShowBox.y, ShowBox.z, ShowBox.w);
            Image showBack = Box(show, entry.frame, entry.edge, 2f);
            if (viewing == UnitClass.Warrior)
            {
                Image art = TroopsArt.Image(show, "Showcase_Warrior", "Art");
                art.preserveAspect = false;
                UI.Stretch(art.rectTransform, 2f, 2f, 2f, 2f);
            }
            else
            {
                Image art = TroopsArt.Image(show, "Portrait_" + entry.art, "Art");
                LegionUI.Inside(art.rectTransform, ShowBox, ShowBox.x + 4, ShowBox.y + 60, ShowBox.z - 4, ShowBox.w - 60);
            }
            if (!unlocked) showBack.color = new Color32(46, 48, 58, 255);
            Arrow(show, -1, new Vector4(24, 1214, 76, 1276));
            Arrow(show, 1, new Vector4(320, 1214, 372, 1276));

            Text(ArmyConfig.ClassName(viewing), 403, 1020, 740, 1068, 38f, LegionTextStyle.PrimaryButton, Color.white, TextAnchor.MiddleLeft);
            Image star = TroopsArt.Image(panel, StarSprite(stats.rarity), "Rarity");
            LegionUI.Inside(star.rectTransform, PanelBox, 748, 1024, 788, 1064);
            Image chip = UI.Panel(panel, RarityChip(stats.rarity), "Chip");
            LegionUI.Inside(chip.rectTransform, PanelBox, 798, 1022, 912, 1060);
            TMP_Text chipLabel = MainMenuButtons.Label(chip.transform, MetaCatalog.RarityName(stats.rarity).ToUpperInvariant(), 22f,
                LegionTextStyle.PrimaryButton, Color.white);
            UI.Stretch(chipLabel.rectTransform, 4f, 4f, 2f, 4f);

            TMP_Text about = Text(Description(viewing), 403, 1068, 912, 1120, 20f, LegionTextStyle.Body, new Color(0.88f, 0.92f, 1f), TextAnchor.UpperLeft);
            about.textWrappingMode = TextWrappingModes.Normal;

            float scale = LevelScale(level);
            float next = LevelScale(level + 1);
            bool heal = stats.attackKind == AttackKind.Heal;
            Stat("Icon_Health", "Vida", stats.maxHealth * scale, max ? 0f : stats.maxHealth * (next - scale), 394, 563);
            Stat("Icon_Damage", heal ? "Cura" : "Dano", stats.damage * scale, max ? 0f : stats.damage * (next - scale), 568, 738);
            Stat("Icon_Defense", "Defesa", stats.armor, 0f, 744, 915);

            Image levelBox = Box(UI.Rect(panel, "LevelBox"), new Color32(8, 22, 54, 230), new Color32(50, 90, 160, 160), 2f);
            LegionUI.Inside(levelBox.rectTransform, PanelBox, 394, 1205, 650, 1292);
            Text(unlocked ? "Nível " + level : "Bloqueado", 408, 1210, 640, 1246, 28f, LegionTextStyle.PrimaryButton, Color.white, TextAnchor.MiddleLeft);
            Image fill = LegionUI.ProgressBar(panel, PanelBox, 408, 1250, 636, 1282, new Color(0.15f, 0.62f, 1f));
            LegionUI.SetProgress(fill, unlocked ? level / (float)MaxLevel : 0f);
            Text($"{(unlocked ? level : 0)}/{MaxLevel}", 408, 1250, 636, 1282, 22f, LegionTextStyle.Hud, Color.white, TextAnchor.MiddleCenter);

            UpgradeButton(unlocked, level, max, c);
            Traits(stats);
        }

        private void Arrow(RectTransform show, int step, Vector4 box)
        {
            Image frame = MainMenuArt.Image(show, "Button_SideFrame", step < 0 ? "Prev" : "Next", true);
            LegionUI.Inside(frame.rectTransform, ShowBox, box.x, box.y, box.z, box.w);
            TMP_Text glyph = MainMenuButtons.Label(frame.transform, step < 0 ? "<" : ">", 40f, LegionTextStyle.PrimaryButton, Color.white);
            UI.Stretch(glyph.rectTransform);
            MainMenuButtons.Make(frame.rectTransform, frame, () =>
            {
                var list = new List<UnitClass>();
                foreach (var e in Classes)
                {
                    if (filter == null || Stats(e.unitClass).rarity == filter) list.Add(e.unitClass);
                }
                if (list.Count == 0) return;
                int index = Mathf.Max(0, list.IndexOf(viewing));
                viewing = list[(index + step + list.Count) % list.Count];
                Refresh();
            });
        }

        private void Stat(string icon, string name, float value, float gain, float x0, float x1)
        {
            Image back = Box(UI.Rect(panel, "Stat_" + name), new Color32(8, 22, 54, 230), new Color32(50, 90, 160, 120), 2f);
            LegionUI.Inside(back.rectTransform, PanelBox, x0, 1128, x1, 1196);
            Image art = HeroesArt.Image(panel, icon, "Icon_" + name);
            LegionUI.Inside(art.rectTransform, PanelBox, x0 + 10, 1136, x0 + 62, 1188);
            Text(name, x0 + 70, 1132, x1 - 4, 1162, 20f, LegionTextStyle.Menu, new Color(0.82f, 0.88f, 1f), TextAnchor.MiddleLeft);
            string text = Mathf.RoundToInt(value).ToString(CultureInfo.InvariantCulture);
            int plus = Mathf.RoundToInt(gain);
            if (plus > 0) text += $" <color=#5CFF5C>+{plus}</color>";
            TMP_Text label = Text(text, x0 + 70, 1160, x1 - 4, 1192, 22f, LegionTextStyle.Hud, Color.white, TextAnchor.MiddleLeft);
            label.richText = true;
        }

        private void UpgradeButton(bool unlocked, int level, bool max, int c)
        {
            UnitClass unitClass = viewing;
            int cost = MetaCatalog.TroopLevelCost(level);
            Color color = max ? UIStyle.Gray : unlocked ? UIStyle.Green : UIStyle.Gold;
            Button button = UI.Button(panel, max ? "NÍVEL MÁX." : unlocked ? "MELHORAR" : "DESBLOQUEAR", color, () =>
            {
                string name = ArmyConfig.ClassName(unitClass);
                if (!unlocked)
                    Buy("Classe " + name, MetaCatalog.ClassUnlockGold[c], MetaCatalog.ClassUnlockGems[c], () => MetaService.UnlockClass(unitClass), name + " liberado!");
                else if (!max)
                    Buy($"{name} nível {level + 1}", cost, 0, () => MetaService.UpgradeTroop(unitClass, MaxLevel), null);
            }, 34, "Upgrade");
            LegionUI.Inside((RectTransform)button.transform, PanelBox, 667, 1208, 912, 1292);
            button.interactable = !max;

            TMP_Text head = button.GetComponentInChildren<TMP_Text>();
            UI.Place(head.rectTransform, 0.06f, 0.48f, 0.74f, 0.94f);
            if (max) return;
            string price = unlocked ? MainMenuCurrencyBar.Short(cost) : UnlockPrice(c);
            bool gemsPrice = !unlocked && MetaCatalog.ClassUnlockGems[c] > 0;
            Image coin = HeroesArt.Image(button.transform, "Icon_Coin", "Coin");
            if (gemsPrice) coin.sprite = LegionArt.Get("Icons/gem");
            UI.Place(coin.rectTransform, 0.12f, 0.1f, 0.26f, 0.48f);
            TMP_Text sub = MainMenuButtons.Label(button.transform, unlocked ? price : price.Split(' ')[0], 26f, LegionTextStyle.Hud, Color.white);
            UI.Place(sub.rectTransform, 0.27f, 0.08f, 0.72f, 0.5f);
            Image arrow = HeroesArt.Image(button.transform, "Icon_UpgradeArrow", "Arrow");
            UI.Place(arrow.rectTransform, 0.74f, 0.14f, 0.96f, 0.86f);
        }

        // Traços da tropa nos ícones do mockup: ritmo de ataque, função, crítico e alcance.
        private void Traits(UnitStats stats)
        {
            (string name, string value)[] traits =
            {
                ("Ataque", (1f / stats.attackInterval).ToString("0.0", CultureInfo.InvariantCulture).Replace('.', ',') + "/s"),
                ("Função", stats.role == TroopRole.Front ? "Frente" : "Retaguarda"),
                ("Crítico", Mathf.RoundToInt(stats.critChance * 100f) + "%"),
                ("Alcance", stats.attackRange.ToString("0.#", CultureInfo.InvariantCulture).Replace('.', ','))
            };
            float[] x0 = { 408, 533, 660, 798 };
            for (int i = 0; i < traits.Length; i++)
            {
                Image icon = TroopsArt.Image(panel, "Trait_" + i, "Trait" + i);
                LegionUI.Inside(icon.rectTransform, PanelBox, x0[i], 1308, x0[i] + 106, 1412);
                Text(traits[i].name, x0[i] - 12, 1416, x0[i] + 118, 1446, 20f, LegionTextStyle.Menu, Color.white, TextAnchor.MiddleCenter);
                Text(traits[i].value, x0[i] - 12, 1444, x0[i] + 118, 1472, 19f, LegionTextStyle.Hud, new Color(0.55f, 0.85f, 1f), TextAnchor.MiddleCenter);
            }
        }

        private static string Description(UnitClass unitClass) => unitClass switch
        {
            UnitClass.Archer => "Ataca de longe com flechas precisas, protegido atrás da linha de frente.",
            UnitClass.Lancer => "Sua lança atravessa vários inimigos em linha com um único golpe.",
            UnitClass.Mage => "Lança magia em área que atinge grupos inteiros de inimigos.",
            UnitClass.Healer => "Cura os aliados feridos e mantém o exército de pé por mais tempo.",
            UnitClass.Knight => "Avança a galope e causa dano reforçado no primeiro golpe da carga.",
            _ => "Soldado confiável e versátil, ideal para formar a linha de frente do seu exército."
        };

        private TMP_Text Text(string text, float x0, float y0, float x1, float y1, float size, LegionTextStyle style, Color color, TextAnchor anchor)
            => LegionUI.Text(panel, PanelBox, x0, y0, x1, y1, text, size, style, color, anchor);

        private void Buy(string item, int goldCost, int gemCost, Func<PurchaseResult> purchase, string success)
        {
            if (!Economy.CanAfford(goldCost, gemCost))
            {
                Ui.Toast("Saldo insuficiente");
                GameAudio.Play(SoundId.UiBack);
                return;
            }
            string price = gemCost > 0 ? $"{gemCost} GEMAS" : $"{goldCost} OURO";
            Ui.Confirm("CONFIRMAR COMPRA", $"{item}\npor {price}?", "COMPRAR", () =>
            {
                PurchaseResult result = purchase();
                GameAudio.Play(result == PurchaseResult.Success ? SoundId.UiConfirm : SoundId.UiBack);
                Ui.Toast(result == PurchaseResult.Success && success != null ? success : MetaService.Explain(result));
                Refresh();
            });
        }
    }

    // Sprites da tela de tropas (Assets/LegionRush/UI/Troops/Resources/TroopsArt/<nome>).
    public static class TroopsArt
    {
        private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        public static Sprite Get(string name)
        {
            if (cache.TryGetValue(name, out Sprite sprite)) return sprite;
            sprite = Resources.Load<Sprite>("TroopsArt/" + name);
            if (sprite == null) sprite = HeroesArt.Get(name);
            cache[name] = sprite;
            return sprite;
        }

        public static Image Image(Transform parent, string sprite, string name = null)
        {
            RectTransform rect = UI.Rect(parent, name ?? sprite);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = Get(sprite);
            image.preserveAspect = true;
            image.raycastTarget = false;
            if (image.sprite == null)
            {
                image.color = Color.clear;
                GameLog.Warn("Arte das tropas ausente: " + sprite);
            }
            return image;
        }
    }
}
