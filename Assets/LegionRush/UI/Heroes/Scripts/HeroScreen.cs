using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LegionRush
{
    // Tela de heróis (proposta 1b de entrega/telas/Tela Herois - Ajustes.html), no grid do mockup (941×1672):
    // cabeçalho padrão, abas LegionTabs, vitrine com setas e selo LÍDER, painel LegionPanel com atributos,
    // habilidades e LegionBuyButton, e grade uniforme de 5 colunas (ScrollRect com mais de 10 heróis).
    public class HeroScreen : UIScreen
    {
        private static readonly Vector4 ShowBox = new Vector4(0, 335, 493, 967);
        private static readonly Vector4 InfoBox = new Vector4(493, 335, 925, 967);
        private static readonly Vector4 GridBox = new Vector4(0, 980, 941, 1492);

        // Carta 172×242, espaço de 14: x0 = 14 + c·186, y0 = 980 + r·256.
        private const float CardW = 172f, CardH = 242f, StepX = 186f, StepY = 256f, GridX = 14f;

        private static readonly (string label, HeroRole? role)[] TabDefs =
        {
            ("TODOS", null), ("TANQUE", HeroRole.Tank), ("DANO", HeroRole.Damage), ("SUPORTE", HeroRole.Support)
        };

        private MainMenuProfilePanel profile;
        private MainMenuCurrencyBar gold, gems;
        private LegionTabs tabs;
        private HeroRole? filter;
        private string viewing;

        private RectTransform show, info, gridView, grid;
        private ScrollRect gridScroll;

        public override bool AllowBack => true;

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
            LegionUI.Place(back.rectTransform, LegionUI.Edge.TopLeft, LegionHeader.BackX0, LegionHeader.BackY0, LegionHeader.BackX1, LegionHeader.BackY1);
            MainMenuButtons.Make(back.rectTransform, back, OnBack);
            // Faixa com o título desenhado na arte, na caixa padrão (abaixo da carteira).
            Image title = HeroesArt.Image(root, "Title_Heroes", "Title");
            LegionUI.Place(title.rectTransform, LegionUI.Edge.Top, LegionHeader.TitleX0, LegionHeader.TitleY0, LegionHeader.TitleX1, LegionHeader.TitleY1);

            // Abas: x0 {23,251,479,707}, largura 211, y 242–312.
            RectTransform tabArea = LegionUI.Place(UI.Rect(root, "Tabs"), LegionUI.Edge.Top, 23, 242, 918, 312);
            string[] labels = Array.ConvertAll(TabDefs, t => t.label);
            tabs = LegionTabs.Create(tabArea, labels, i =>
            {
                filter = TabDefs[i].role;
                Refresh();
            }, 26f, 17f);
            tabs.Select(0);

            show = LegionUI.Place(UI.Rect(root, "Showcase"), LegionUI.Edge.Top, ShowBox.x, ShowBox.y, ShowBox.z, ShowBox.w);
            info = LegionUI.Place(UI.Rect(root, "Info"), LegionUI.Edge.Top, InfoBox.x, InfoBox.y, InfoBox.z, InfoBox.w);

            gridView = LegionUI.Place(UI.Rect(root, "Grid"), LegionUI.Edge.Bottom, GridBox.x, GridBox.y, GridBox.z, GridBox.w);
            gridView.gameObject.AddComponent<RectMask2D>();
            Image hit = gridView.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            grid = UI.Rect(gridView, "Content");
            gridScroll = gridView.gameObject.AddComponent<ScrollRect>();
            gridScroll.content = grid;
            gridScroll.viewport = gridView;
            gridScroll.horizontal = false;
            gridScroll.movementType = ScrollRect.MovementType.Clamped;
            gridScroll.scrollSensitivity = 40f;

            BuildNav(root);
            Economy.WalletChanged += (g, m) => { if (Root != null && Root.gameObject.activeInHierarchy) RefreshWallet(); };
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
            Image shade = UI.Panel(root, new Color(0.02f, 0.05f, 0.18f, 0.55f), "Shade");
            UI.Stretch(shade.rectTransform);
            shade.raycastTarget = false;
            UISnow.Attach(root);
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
                button.SetSelected(destination == MainMenuDestination.Heroes);
            }
        }

        private void Navigate(MainMenuDestination destination)
        {
            if (destination == MainMenuDestination.Heroes) return;
            Ui.Replace<MainMenuScreen>();
            if (destination != MainMenuDestination.Home && destination != MainMenuDestination.More)
                new UIRootMainMenuNavigator(Ui).Open(destination);
        }

        // ---------------------------------------------------------------- dados

        public override void Refresh()
        {
            if (!SaveManager.IsLoaded) return;
            SaveData save = SaveManager.Current;
            if (string.IsNullOrEmpty(viewing) || MetaCatalog.Hero(viewing) == null) viewing = save.heroes.selected;

            if (Ui != null)
            {
                MainMenuModel model = new GameMainMenuData(Ui.Game).Snapshot();
                profile.Set(model.PlayerName, model.PlayerLevel, model.Xp, model.XpMax);
            }
            RefreshWallet();

            List<HeroDef> list = MetaCatalog.Heroes.FindAll(h => filter == null || h.Role == filter);
            if (list.Count > 0 && !list.Exists(h => h.Id == viewing)) viewing = list[0].Id;

            BuildCards(list, save);
            HeroDef hero = MetaCatalog.Hero(viewing);
            if (hero != null)
            {
                BuildShowcase(hero, save, list);
                BuildInfo(hero, save);
            }
        }

        private void RefreshWallet()
        {
            gold.Set(Economy.Gold);
            gems.Set(Economy.Gems);
        }

        private static string RoleName(HeroRole role) => role switch
        {
            HeroRole.Tank => "TANQUE",
            HeroRole.Damage => "DANO",
            _ => "SUPORTE"
        };

        // Retrato colorido: Paladino, Assassino e Elfa usam Portrait_Paladin/Assassin/Elf (Prompt 2) quando existirem;
        // o bloqueio é só do código (brilho a 45 % + cadeado), igual para todos.
        private static string PortraitOf(HeroDef hero)
        {
            if (hero.LockedPortrait == null) return hero.Portrait;
            string colored = hero.LockedPortrait.Replace("Locked", string.Empty);
            return HeroesArt.Has(colored) ? colored : hero.LockedPortrait;
        }

        // Nomes longos viram a primeira palavra para caber em 22 sem quebrar ("Atiradora Élfica" -> "Atiradora").
        private static string CardName(string name)
        {
            if (name.Length <= 11) return name;
            int space = name.IndexOf(' ');
            return space > 0 ? name.Substring(0, space) : name;
        }

        // ---------------------------------------------------------------- cartas

        private void BuildCards(List<HeroDef> list, SaveData save)
        {
            UI.Clear(grid);
            int rows = Mathf.Max(1, (list.Count + 4) / 5);
            float height = rows * StepY - (StepY - CardH);
            grid.anchorMin = new Vector2(0f, 1f);
            grid.anchorMax = new Vector2(1f, 1f);
            grid.pivot = new Vector2(0.5f, 1f);
            grid.sizeDelta = new Vector2(0f, height * LegionUI.Px);
            grid.anchoredPosition = Vector2.zero;
            gridScroll.vertical = list.Count > 10;
            Vector4 box = new Vector4(0, GridBox.y, GridBox.z, GridBox.y + height);

            for (int i = 0; i < list.Count; i++)
            {
                HeroDef hero = list[i];
                HeroRecord record = save.heroes.Get(hero.Id);
                bool owned = record != null;
                float x0 = GridX + (i % 5) * StepX;
                float y0 = GridBox.y + (i / 5) * StepY;
                RectTransform card = LegionUI.Inside(UI.Rect(grid, "Card_" + hero.Id), box, x0, y0, x0 + CardW, y0 + CardH);

                Image frame = LegionPanel.Create(card, "Frame", 0.55f);
                UI.Stretch(frame.rectTransform);
                frame.raycastTarget = true;

                // Retrato no topo (75 %), recortado.
                RectTransform window = UI.Rect(card, "PortraitWindow");
                UI.Place(window, 0.05f, 0.25f, 0.95f, 0.97f);
                window.gameObject.AddComponent<RectMask2D>();
                Image portrait = HeroesArt.Image(window, PortraitOf(hero), "Portrait");
                portrait.preserveAspect = false;
                AspectRatioFitter fit = portrait.gameObject.AddComponent<AspectRatioFitter>();
                fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fit.aspectRatio = portrait.sprite != null ? portrait.sprite.rect.width / portrait.sprite.rect.height : 0.7f;
                if (!owned) portrait.color = new Color(0.45f, 0.45f, 0.45f);

                Image fade = MainMenuArt.Image(card, "Shade_Vertical", "Fade");
                fade.preserveAspect = false;
                UI.Place(fade.rectTransform, 0.05f, 0.25f, 0.95f, 0.55f);

                TMP_Text name = MainMenuButtons.Label(card, CardName(hero.Name), 22f, LegionTextStyle.PrimaryButton, Color.white);
                name.fontSizeMin = name.fontSizeMax;
                UI.Place(name.rectTransform, 0.04f, 0.26f, 0.96f, 0.38f);

                if (owned)
                {
                    Stars(card, StarsOf(record.level), 5, new Vector4(0.1f, 0.13f, 0.9f, 0.24f));
                    TMP_Text level = MainMenuButtons.Label(card, "Nv " + record.level, 22f, LegionTextStyle.Hud, new Color(1f, 0.84f, 0.25f));
                    UI.Place(level.rectTransform, 0.04f, 0.02f, 0.96f, 0.13f);
                    if (save.heroes.selected == hero.Id)
                    {
                        Image crown = HeroesArt.Image(card, "Icon_Crown", "Leader");
                        UI.Place(crown.rectTransform, 0.7f, 0.86f, 0.98f, 1.02f);
                    }
                }
                else
                {
                    Image lockIcon = HeroesArt.Image(card, "Icon_Lock", "Lock");
                    UI.Place(lockIcon.rectTransform, 0.72f, 0.82f, 0.96f, 0.98f);
                    PricePill(card, hero, new Vector4(0.08f, 0.04f, 0.92f, 0.21f));
                }

                if (hero.Id == viewing)
                {
                    Image glow = LegionArt.Image(card, "Frames/frame_gold", false, "Selected");
                    glow.type = Image.Type.Sliced;
                    glow.fillCenter = false;
                    glow.pixelsPerUnitMultiplier = 2.2f;
                    UI.Stretch(glow.rectTransform, -5f, -5f, -5f, -5f);
                    glow.raycastTarget = false;
                }

                string id = hero.Id;
                MainMenuButtons.Make(card, frame, () =>
                {
                    viewing = id;
                    Refresh();
                });
            }
        }

        // Pílula com ícone de moeda ou gema + preço.
        private static void PricePill(RectTransform card, HeroDef hero, Vector4 area)
        {
            Image pill = UI.Panel(card, new Color(0.03f, 0.07f, 0.2f, 0.95f), "Price");
            UI.Place(pill.rectTransform, area.x, area.y, area.z, area.w);
            pill.raycastTarget = false;
            Outline ring = pill.gameObject.AddComponent<Outline>();
            ring.effectColor = new Color(0.95f, 0.75f, 0.25f, 0.8f);
            ring.effectDistance = new Vector2(1.5f, -1.5f);
            bool free = hero.Gems <= 0 && hero.Gold <= 0;
            if (!free)
            {
                Image icon = LegionArt.Image(pill.transform, hero.Gems > 0 ? "Icons/gem" : "Icons/coin", false, "Icon");
                UI.Place(icon.rectTransform, 0.04f, 0.08f, 0.32f, 0.92f);
            }
            string text = free ? "GRÁTIS" : MainMenuCurrencyBar.Short(hero.Gems > 0 ? hero.Gems : hero.Gold);
            TMP_Text price = MainMenuButtons.Label(pill.transform, text, 22f, LegionTextStyle.Hud, Color.white);
            UI.Place(price.rectTransform, free ? 0.04f : 0.32f, 0f, 0.96f, 1f);
        }

        private static int StarsOf(int level) => Mathf.Clamp(1 + (level - 1) / 2, 1, 5);

        private static void Stars(RectTransform parent, int filled, int total, Vector4 area)
        {
            RectTransform row = UI.Rect(parent, "Stars");
            UI.Place(row, area.x, area.y, area.z, area.w);
            for (int i = 0; i < total; i++)
            {
                Image star = HeroesArt.Image(row, i < filled ? "Star_Gold" : "Star_Blue", "Star" + i);
                UI.Place(star.rectTransform, i / (float)total, 0f, (i + 1) / (float)total, 1f);
            }
        }

        // ---------------------------------------------------------------- vitrine

        private void BuildShowcase(HeroDef hero, SaveData save, List<HeroDef> list)
        {
            UI.Clear(show);
            bool owned = save.heroes.Get(hero.Id) != null;

            // Pedestal (arte Heroes/Pedestal do Prompt 2, quando existir) em 60,826,430,900.
            if (LegionArt.Pick("Heroes/Pedestal", "Frames/frame_gold") == "Heroes/Pedestal")
                LegionUI.Inside(LegionArt.Image(show, "Heroes/Pedestal", false, "Pedestal").rectTransform, ShowBox, 60, 826, 430, 900);

            // Arte em 28,372,470,880, proporção mantida e pivô embaixo (o herói pisa no pedestal).
            RectTransform area = LegionUI.Inside(UI.Rect(show, "HeroArea"), ShowBox, 28, 372, 470, 880);
            Image art = HeroesArt.Image(area, hero.Showcase ?? PortraitOf(hero), "Hero");
            art.preserveAspect = true;
            RectTransform a = art.rectTransform;
            a.anchorMin = new Vector2(0f, 0f);
            a.anchorMax = new Vector2(1f, 0f);
            a.pivot = new Vector2(0.5f, 0f);
            float boxW = (470 - 28) * LegionUI.Px, boxH = (880 - 372) * LegionUI.Px;
            float aspect = art.sprite != null ? art.sprite.rect.width / art.sprite.rect.height : 0.7f;
            a.sizeDelta = new Vector2(0f, Mathf.Min(boxH, boxW / aspect));
            a.anchoredPosition = Vector2.zero;
            if (!owned) art.color = new Color(0.45f, 0.45f, 0.45f);

            // Setas de 70 px em 18,826 e 400,826 (o botão voltar redondo da própria arte, espelhado para a direita).
            int index = list.FindIndex(h => h.Id == hero.Id);
            Arrow(-1, list, index, 18);
            Arrow(1, list, index, 400);

            // Selo LÍDER ATUAL ou botão LIDERAR em y 900–940.
            if (owned && save.heroes.selected == hero.Id)
            {
                LegionBadge.Pill(show, ShowBox, 150, 900, 350, 940, "LÍDER ATUAL", new Color(0.85f, 0.6f, 0.1f));
            }
            else if (owned)
            {
                LegionBuyButton lead = LegionBuyButton.Create(show, LegionBuyButton.Variant.Blue, LegionCurrency.None, 0, "LIDERAR", () =>
                {
                    MetaService.SelectHero(hero.Id);
                    Ui.Toast(hero.Name + " lidera a legião");
                    Ui.Game.Restart();
                    Refresh();
                }, "Lead");
                LegionUI.Inside((RectTransform)lead.transform, ShowBox, 150, 896, 350, 944);
            }
        }

        private void Arrow(int step, List<HeroDef> list, int index, float x0)
        {
            Image arrow = HeroesArt.Image(show, "Button_Back", step < 0 ? "Prev" : "Next");
            LegionUI.Inside(arrow.rectTransform, ShowBox, x0, 826, x0 + 70, 896);
            if (step > 0) arrow.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
            MainMenuButtons.Make(arrow.rectTransform, arrow, () =>
            {
                if (list.Count == 0) return;
                viewing = list[(index + step + list.Count) % list.Count].Id;
                Refresh();
            });
        }

        // ---------------------------------------------------------------- painel

        private void BuildInfo(HeroDef hero, SaveData save)
        {
            UI.Clear(info);
            HeroRecord record = save.heroes.Get(hero.Id);
            bool owned = record != null;
            int level = owned ? record.level : 1;

            Image panel = LegionPanel.Create(info, "Panel");
            UI.Stretch(panel.rectTransform);

            Image crown = HeroesArt.Image(info, "Icon_Crown", "Crown");
            LegionUI.Inside(crown.rectTransform, InfoBox, 516, 356, 562, 396);
            Text(hero.Name, 570, 350, 910, 402, 34f, LegionTextStyle.PrimaryButton, Color.white, TextAnchor.MiddleLeft);

            Chip(MetaCatalog.RarityName(hero.Rarity).ToUpperInvariant(), MetaCatalog.RarityColor(hero.Rarity), 516, 404, 640, 440);
            Chip(RoleName(hero.Role), new Color(0.12f, 0.4f, 0.85f), 648, 404, 768, 440);
            Text(owned ? "Nível " + level : "BLOQUEADO", 772, 404, 908, 440, 22f, LegionTextStyle.Hud, new Color(1f, 0.84f, 0.25f), TextAnchor.MiddleRight);

            RectTransform stars = LegionUI.Inside(UI.Rect(info, "Stars"), InfoBox, 518, 448, 702, 486);
            Stars(stars, owned ? StarsOf(level) : 0, 5, new Vector4(0f, 0f, 1f, 1f));

            UnitStats basis = ArmyConfig.DefaultStats(hero.Class);
            float scale = 1f + 0.1f * (level - 1);
            float next = 1f + 0.1f * level;
            Stat("Icon_Health", "Vida", basis.maxHealth * 50f, scale, next, 0);
            Stat("Icon_Damage", "Dano", basis.damage * 12f, scale, next, 1);
            Stat("Icon_Defense", "Defesa", basis.armor * 4f, scale, next, 2);

            // Habilidades: ícone de 88, nome em 22 e nível em 20.
            (string icon, string name)[] skills = Skills(hero);
            int[] divisors = { 3, 4, 5, 6 };
            for (int i = 0; i < skills.Length; i++)
            {
                float x0 = 512 + i * 102f;
                Image icon = HeroesArt.Image(info, skills[i].icon, "Skill" + i);
                LegionUI.Inside(icon.rectTransform, InfoBox, x0 + 7, 666, x0 + 95, 754);
                if (!owned) icon.color = new Color(0.5f, 0.52f, 0.6f);
                Text(skills[i].name, x0, 756, x0 + 102, 786, 22f, LegionTextStyle.Menu, Color.white, TextAnchor.MiddleCenter);
                Text("Nível " + (1 + (level - 1) / divisors[i]), x0, 786, x0 + 102, 814, 20f, LegionTextStyle.Hud,
                    new Color(0.55f, 0.85f, 1f), TextAnchor.MiddleCenter);
            }

            // Compra: moeda/gema + valor na 1ª linha, ação na 2ª.
            bool max = owned && level >= 10;
            int cost = hero.LevelCost(level);
            LegionBuyButton buy;
            if (!owned)
            {
                LegionCurrency currency = hero.Gems > 0 ? LegionCurrency.Gems : hero.Gold > 0 ? LegionCurrency.Gold : LegionCurrency.None;
                buy = LegionBuyButton.Create(info, LegionBuyButton.Variant.Gold, currency, hero.Gems > 0 ? hero.Gems : hero.Gold, "DESBLOQUEAR",
                    () => Buy(hero.Name, hero.Gold, hero.Gems, () => MetaService.UnlockHero(hero.Id), hero.Name + " entrou na legião!"));
            }
            else
            {
                buy = LegionBuyButton.Create(info, LegionBuyButton.Variant.Gold, max ? LegionCurrency.None : LegionCurrency.Gold, max ? 0 : cost,
                    max ? "NÍVEL MÁX." : "MELHORAR",
                    () => { if (!max) Buy($"{hero.Name} nível {level + 1}", cost, 0, () => MetaService.LevelUpHero(hero.Id), null); });
                if (max) buy.SetBought(true, "NÍVEL MÁX.");
            }
            LegionUI.Inside((RectTransform)buy.transform, InfoBox, 530, 836, 888, 944);
        }

        private void Stat(string icon, string name, float value, float scale, float next, int row)
        {
            float y0 = 498 + row * 54;
            Image line = UI.Panel(info, new Color(1f, 1f, 1f, 0.06f), "Row" + row);
            LegionUI.Inside(line.rectTransform, InfoBox, 516, y0, 902, y0 + 48);
            Image art = HeroesArt.Image(info, icon, "Stat" + row);
            LegionUI.Inside(art.rectTransform, InfoBox, 526, y0 + 4, 568, y0 + 44);
            Text(name, 580, y0, 700, y0 + 48, 24f, LegionTextStyle.Menu, new Color(0.82f, 0.88f, 1f), TextAnchor.MiddleLeft);
            int now = Mathf.RoundToInt(value * scale);
            int gain = Mathf.RoundToInt(value * next) - now;
            Text(MenuChrome.Format(now), 700, y0, 800, y0 + 48, 26f, LegionTextStyle.Hud, Color.white, TextAnchor.MiddleRight);
            Text("+" + gain, 810, y0, 900, y0 + 48, 24f, LegionTextStyle.Positive, new Color(0.45f, 1f, 0.4f), TextAnchor.MiddleLeft);
        }

        private TMP_Text Text(string text, float x0, float y0, float x1, float y1, float size, LegionTextStyle style, Color color, TextAnchor anchor)
            => LegionUI.Text(info, InfoBox, x0, y0, x1, y1, text, size, style, color, anchor);

        private void Chip(string text, Color color, float x0, float y0, float x1, float y1)
        {
            LegionBadge.Pill(info, InfoBox, x0, y0, x1, y1, text, color, 22f);
        }

        private static (string, string)[] Skills(HeroDef hero) => hero.Role switch
        {
            HeroRole.Tank => new[] { ("Skill_RoyalStrike", "Golpe"), ("Skill_Leadership", "Liderança"), ("Skill_RoyalDefense", "Defesa"), ("Skill_Rally", "Rally") },
            HeroRole.Support => new[] { ("Skill_Green", "Cura"), ("Skill_Gold", "Bênção"), ("Skill_RoyalDefense", "Proteção"), ("Skill_Rally", "Rally") },
            _ => hero.Class == UnitClass.Mage
                ? new[] { ("Skill_Purple", "Arcano"), ("Skill_Blue", "Gelo"), ("Skill_Gold", "Chama"), ("Skill_Rally", "Rally") }
                : new[] { ("Skill_RoyalStrike", "Golpe"), ("Skill_Blue", "Rajada"), ("Skill_Gold", "Precisão"), ("Skill_Rally", "Rally") }
        };

        private void Buy(string item, int goldCost, int gemCost, Func<PurchaseResult> purchase, string success)
        {
            if (!Economy.CanAfford(goldCost, gemCost))
            {
                Ui.Toast("Saldo insuficiente");
                GameAudio.Play(SoundId.UiBack);
                return;
            }
            string price = gemCost > 0 ? $"{gemCost} gemas" : $"{goldCost} de ouro";
            Ui.Confirm("CONFIRMAR COMPRA", $"{item}\npor {price}?", "COMPRAR", () =>
            {
                PurchaseResult result = purchase();
                GameAudio.Play(result == PurchaseResult.Success ? SoundId.UiConfirm : SoundId.UiBack);
                Ui.Toast(result == PurchaseResult.Success && success != null ? success : MetaService.Explain(result));
                Refresh();
            });
        }
    }
}
