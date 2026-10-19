using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LegionRush
{
    // Loja no layout do mockup (941×1672): topo com perfil e carteira, título, abas OFERTAS/GEMAS/MOEDAS/BAÚS/PASSES,
    // vitrine de ofertas, ofertas diárias e a barra de navegação.
    // Vende só o que existe no jogo: ouro por gemas, baús, poções, visuais, habilidades e equipamentos.
    // Pacotes em reais ainda não têm loja de pagamento integrada: o toque avisa que chegam em breve.
    public class ShopScreen : UIScreen
    {
        public const string SectionChests = "MOEDAS";
        public const string SectionSkins = "GEMAS";

        private const float ViewTop = 326f;
        private const float ViewBottom = 1496f;
        private static readonly string[] Tabs = { "OFERTAS", "GEMAS", "MOEDAS", "BAÚS", "PASSES" };
        private static readonly Color GoldText = new Color(1f, 0.82f, 0.3f);

        private MainMenuProfilePanel profile;
        private MainMenuCurrencyBar gold, gems;
        private LegionTabs tabs;
        private RectTransform view, content;
        private ScrollRect scroll;
        private Vector4 box;
        private int tab;

        // ---------------------------------------------------------------- estrutura

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
            gold = MainMenuCurrencyBar.Create(root, "Icon_Coin", () => Focus(SectionChests), "Gold");
            LegionUI.Place((RectTransform)gold.transform, LegionUI.Edge.Top, 403, 30, 630, 92);
            gems = MainMenuCurrencyBar.Create(root, "Icon_Gem", () => Focus(SectionSkins), "Gems");
            LegionUI.Place((RectTransform)gems.transform, LegionUI.Edge.Top, 638, 30, 852, 92);
            Image settings = MainMenuArt.Image(root, "Button_Settings", "Settings");
            LegionUI.Place(settings.rectTransform, LegionUI.Edge.TopRight, 860, 22, 928, 92);
            MainMenuButtons.Make(settings.rectTransform, settings, () => Ui.Push<SettingsScreen>());

            LegionHeader.Build(root, "LOJA", OnBack);

            // Abas no padrão LegionTabs (Tab_On / Tab_Off), logo abaixo do cabeçalho.
            RectTransform tabArea = LegionUI.Place(UI.Rect(root, "Tabs"), LegionUI.Edge.Top, 18, 246, 923, 312);
            tabs = LegionTabs.Create(tabArea, Tabs, ShowTab, 24f, 8f);

            // Área rolável entre as abas e a navegação; o conteúdo usa o grid do mockup.
            view = LegionUI.Place(UI.Rect(root, "View"), LegionUI.Edge.Top, 0, ViewTop, LegionUI.RefWidth, ViewBottom);
            view.gameObject.AddComponent<RectMask2D>();
            Image hit = view.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            scroll = view.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;

            BuildNav(root);
        }

        public override void Refresh()
        {
            if (!SaveManager.IsLoaded) return;
            if (Ui != null)
            {
                MainMenuModel model = new GameMainMenuData(Ui.Game).Snapshot();
                profile.Set(model.PlayerName, model.PlayerLevel, model.Xp, model.XpMax);
            }
            gold.Set(Economy.Gold);
            gems.Set(Economy.Gems);
            ShowTab(tab);
        }

        // "+" da carteira de outras telas: abre a aba de moedas ou de gemas.
        public void Focus(string section)
        {
            tab = Array.IndexOf(Tabs, section);
            if (tab < 0) tab = 0;
            if (content != null) ShowTab(tab);
        }

        private void ShowTab(int index)
        {
            tab = index;
            tabs.Select(tab);
            if (content != null) UnityEngine.Object.Destroy(content.gameObject);
            switch (tab)
            {
                case 1: BuildGems(); break;
                case 2: BuildCoins(); break;
                case 3: BuildChests(); break;
                case 4: BuildPasses(); break;
                default: BuildOffers(); break;
            }
        }

        // Conteúdo da aba com altura em pixels do mockup (a partir do topo da área rolável).
        private void NewContent(float height)
        {
            content = UI.Rect(view, "Content");
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = new Vector2(0f, height * LegionUI.Px);
            content.anchoredPosition = Vector2.zero;
            scroll.content = content;
            box = new Vector4(0, ViewTop, LegionUI.RefWidth, ViewTop + height);
        }

        private RectTransform Area(float x0, float y0, float x1, float y1, string name = "Area")
            => LegionUI.Inside(UI.Rect(content, name), box, x0, y0, x1, y1);

        // ---------------------------------------------------------------- abas

        private void BuildOffers()
        {
            NewContent(ViewBottom - ViewTop);
            SupremeBanner();

            Card(new Vector4(22, 655, 310, 892), "Pacote Inicial", new[] { "Rewards/coin_pile" },
                Lines(("Icons/coin", "2.000"), ("Icons/gem", "250")), Price.Real(4.90f));
            Card(new Vector4(328, 655, 614, 892), "Pacote Avançado", new[] { "Rewards/gem_bag" },
                Lines(("Icons/coin", "5.000"), ("Icons/gem", "750")), Price.Real(18.90f));
            Card(new Vector4(630, 655, 918, 892), "Pacote Premium", new[] { "Chests/chest_gold" },
                Lines(("Icons/coin", "12.000"), ("Icons/gem", "1.500")), Price.Real(49.90f), "MELHOR OFERTA");

            ChestDef rare = MetaCatalog.Chest("raro");
            ChestDef epic = MetaCatalog.Chest("epico");
            ChestDef common = MetaCatalog.Chest("comum");
            ChestCard(new Vector4(22, 912, 318, 1182), rare, "Chests/chest_rare");
            ChestCard(new Vector4(334, 912, 608, 1182), epic, "Chests/chest_epic");
            LimitedOffer(new Vector4(624, 912, 918, 1182), common);

            DailyHeader(1205, 1262);
            float[] dx = { 28, 207, 387, 567, 748 };
            DailyCard(dx[0], "Moedas", "Rewards/coin_pile", "1.000", Price.Gems(50), "coins1000", () => GrantGold(1000, 50));
            DailyCard(dx[1], "Gemas", "Rewards/gem_bag", "100", Price.Real(4.90f), null, null);
            PotionDef[] potions = MetaCatalog.Potions.ToArray();
            string[] potionArt = { "Icons/potion_red", "Icons/potion_green", "Icons/potion_blue" };
            for (int i = 0; i < 3 && i < potions.Length; i++)
            {
                PotionDef potion = potions[i];
                DailyCard(dx[2 + i], ShortPotion(potion.Name), potionArt[i], "1", Price.Gold(potion.Gold), "potion_" + potion.Id,
                    () => MetaService.BuyPotion(potion.Id) == PurchaseResult.Success);
            }
        }

        private void BuildGems()
        {
            var cards = new List<Action<Vector4>>
            {
                b => Card(b, "Punhado de Gemas", new[] { "Icons/gem" }, Lines(("Icons/gem", "100")), Price.Real(4.90f)),
                b => Card(b, "Saco de Gemas", new[] { "Rewards/gem_bag" }, Lines(("Icons/gem", "550")), Price.Real(19.90f)),
                b => Card(b, "Baú de Gemas", new[] { "Chests/chest_epic" }, Lines(("Icons/gem", "1.200")), Price.Real(39.90f), "MELHOR OFERTA")
            };
            SaveData save = SaveManager.Current;
            foreach (SkinDef skin in MetaCatalog.Skins)
            {
                SkinDef s = skin;
                bool owned = save.cosmetics.owned.Contains(s.Id);
                bool selected = save.cosmetics.selected == s.Id;
                cards.Add(b => SkinCard(b, s, owned, selected));
            }
            Grid("PACOTES DE GEMAS E VISUAIS", cards);
        }

        private void BuildCoins()
        {
            (int amount, int price, string art)[] packs = { (1000, 50, "Icons/coin"), (5500, 250, "Rewards/coin_pile"), (12000, 500, "Chests/chest_gold") };
            var cards = new List<Action<Vector4>>();
            foreach (var p in packs)
            {
                var pack = p;
                cards.Add(b => Card(b, $"{Format(pack.amount)} Moedas", new[] { pack.art }, Lines(("Icons/coin", Format(pack.amount))), Price.Gems(pack.price),
                    pack.amount >= 12000 ? "MELHOR OFERTA" : null, () => GrantGold(pack.amount, pack.price)));
            }
            PotionDef[] potions = MetaCatalog.Potions.ToArray();
            string[] potionArt = { "Icons/potion_red", "Icons/potion_green", "Icons/potion_blue" };
            for (int i = 0; i < potions.Length; i++)
            {
                PotionDef potion = potions[i];
                string art = potionArt[i % potionArt.Length];
                cards.Add(b => Card(b, potion.Name, new[] { art }, Lines((null, potion.Effect.Describe())), Price.Gold(potion.Gold), null,
                    () => Purchase(potion.Name, potion.Gold, 0, () => MetaService.BuyPotion(potion.Id))));
            }
            Grid("MOEDAS E POÇÕES", cards);
        }

        private void BuildChests()
        {
            string[] art = { "Chests/chest_wood", "Chests/chest_rare", "Chests/chest_epic" };
            var cards = new List<Action<Vector4>>();
            for (int i = 0; i < MetaCatalog.Chests.Count; i++)
            {
                ChestDef chest = MetaCatalog.Chests[i];
                string a = art[Mathf.Min(i, art.Length - 1)];
                cards.Add(b => ChestCard(b, chest, a));
            }
            foreach (EquipmentDef item in MetaCatalog.Equipment)
            {
                if (MetaService.OwnsItem(item.Id)) continue;
                EquipmentDef e = item;
                cards.Add(b => Card(b, e.Name, new[] { EquipmentArt(e) }, Lines((null, e.Bonus.Describe())), PriceOf(e.Gold, e.Gems), null,
                    () => Purchase(e.Name, e.Gold, e.Gems, () => MetaService.BuyEquipment(e.Id))));
            }
            Grid("BAÚS E EQUIPAMENTOS", cards);
        }

        private void BuildPasses()
        {
            var cards = new List<Action<Vector4>>
            {
                b => Card(b, "Passe de Batalha", new[] { "Chests/chest_royal" }, Lines((null, "Recompensas extras por temporada")), Price.Real(24.90f), "EM BREVE")
            };
            SkillSystem skills = Ui != null && Ui.Game != null ? Ui.Game.CurrentSkills : null;
            for (int s = 0; s < SkillDefinition.Count && skills != null; s++)
            {
                var id = (SkillId)s;
                if (MetaService.IsSkillUnlocked(id)) continue;
                SkillDefinition def = skills.Definition(id);
                int g = MetaCatalog.SkillUnlockGold[s];
                int m = MetaCatalog.SkillUnlockGems[s];
                cards.Add(b => Card(b, def.displayName, new[] { "Icons/scroll" }, Lines((null, "Nova habilidade")), PriceOf(g, m), null,
                    () => Purchase(def.displayName, g, m, () => MetaService.UnlockSkill(id))));
            }
            Grid("PASSES E HABILIDADES", cards);
        }

        // Grade de 3 cartas por linha com título de seção.
        private void Grid(string heading, List<Action<Vector4>> cards)
        {
            int rows = Mathf.CeilToInt(cards.Count / 3f);
            float height = Mathf.Max(ViewBottom - ViewTop, 80 + rows * 258 + 20);
            NewContent(height);
            SectionTitle(heading, ViewTop + 10, ViewTop + 64, null);
            float[] x0 = { 22, 328, 630 };
            for (int i = 0; i < cards.Count; i++)
            {
                float y0 = ViewTop + 80 + (i / 3) * 258;
                float x = x0[i % 3];
                cards[i](new Vector4(x, y0, x + 288, y0 + 240));
            }
        }

        // ---------------------------------------------------------------- peças

        private void SupremeBanner()
        {
            RectTransform banner = Area(22, 362, 918, 636, "Supreme");
            Image back = banner.gameObject.AddComponent<Image>();
            back.sprite = UIStyle.Rounded;
            back.type = Image.Type.Sliced;
            back.color = new Color(0.98f, 0.62f, 0.12f);
            Outline frame = banner.gameObject.AddComponent<Outline>();
            frame.effectColor = new Color(1f, 0.85f, 0.35f);
            frame.effectDistance = new Vector2(3f, -3f);

            Image ribbon = UI.Panel(content, new Color(0.85f, 0.12f, 0.15f), "Ribbon");
            LegionUI.Inside(ribbon.rectTransform, box, 12, 352, 285, 404);
            TMP_Text tag = MainMenuButtons.Label(ribbon.transform, "OFERTA ESPECIAL", 24f, LegionTextStyle.PrimaryButton, Color.white);
            UI.Stretch(tag.rectTransform);

            // Título branco com contorno marrom, 44.
            TMP_Text supreme = Label("PACOTE SUPREMO", 60, 398, 480, 460, 44f, LegionTextStyle.PrimaryButton, Color.white, TextAnchor.MiddleLeft);
            supreme.outlineWidth = 0.25f;
            supreme.outlineColor = new Color32(120, 40, 10, 255);
            (string icon, string text)[] lines = { ("Icons/coin", "10.000 Moedas"), ("Icons/gem", "500 Gemas"), ("Chests/chest_epic", "1 Baú Épico"), ("Icons/energy", "3 Poções") };
            for (int i = 0; i < lines.Length; i++)
            {
                float y = 466 + i * 41;
                Image icon = LegionArt.Image(content, lines[i].icon, false, "Icon");
                icon.preserveAspect = true;
                LegionUI.Inside(icon.rectTransform, box, 80, y, 120, y + 38);
                Label(lines[i].text, 130, y, 440, y + 38, 25f, LegionTextStyle.PrimaryButton, Color.white, TextAnchor.MiddleLeft);
            }

            // Brilho radial atrás do baú (no lugar do retângulo "Glow" de bordas duras).
            Image shine = LegionArt.Image(content, "Effects/fx_sparkle", false, "Shine");
            shine.preserveAspect = true;
            shine.color = new Color(1f, 0.95f, 0.7f, 0.85f);
            LegionUI.Inside(shine.rectTransform, box, 400, 352, 740, 632);
            Image coins = LegionArt.Image(content, "Rewards/coin_pile", false, "Coins");
            coins.preserveAspect = true;
            LegionUI.Inside(coins.rectTransform, box, 350, 500, 560, 620);
            Image chest = LegionArt.Image(content, "Chests/chest_royal", false, "Chest");
            chest.preserveAspect = true;
            LegionUI.Inside(chest.rectTransform, box, 430, 380, 710, 600);
            Image gemBag = LegionArt.Image(content, "Rewards/gem_bag", false, "Gems");
            gemBag.preserveAspect = true;
            LegionUI.Inside(gemBag.rectTransform, box, 560, 500, 720, 625);

            Image seal = UI.Panel(content, new Color(0.9f, 0.1f, 0.12f), "Discount");
            seal.sprite = UIStyle.Circle;
            seal.type = Image.Type.Simple;
            LegionUI.Inside(seal.rectTransform, box, 786, 366, 906, 482);
            Outline ring = seal.gameObject.AddComponent<Outline>();
            ring.effectColor = new Color(1f, 0.85f, 0.3f);
            ring.effectDistance = new Vector2(3f, -3f);
            TMP_Text off = MainMenuButtons.Label(seal.transform, "-70%", 34f, LegionTextStyle.Title, Color.white);
            UI.Stretch(off.rectTransform);

            BuyButton(new Vector4(706, 522, 902, 592), Price.Real(27.90f), null, null);
            Label("<s>R$ 89,90</s>", 720, 594, 890, 628, 22f, LegionTextStyle.Menu, new Color(1f, 1f, 1f, 0.8f), TextAnchor.MiddleCenter);
        }

        private RectTransform CardFrame(Vector4 b, string title)
        {
            RectTransform card = Area(b.x, b.y, b.z, b.w, "Card");
            Image frame = MainMenuArt.Image(card, "Button_SideFrame", "Frame", true);
            UI.Stretch(frame.rectTransform);
            Label(title, b.x + 10, b.y + 8, b.z - 10, b.y + 54, 24f, LegionTextStyle.PrimaryButton, Color.white, TextAnchor.MiddleCenter);
            return card;
        }

        private void Card(Vector4 b, string title, string[] art, (string icon, string text)[] lines, Price price, string badge = null, Action buy = null)
        {
            CardFrame(b, title);
            float w = b.z - b.x;
            bool hasLines = lines.Length > 0 && lines[0].icon != null;
            Image picture = LegionArt.Image(content, art[0], false, "Art");
            picture.preserveAspect = true;
            if (hasLines) LegionUI.Inside(picture.rectTransform, box, b.x + 10, b.y + 58, b.x + w * 0.52f, b.w - 70);
            else LegionUI.Inside(picture.rectTransform, box, b.x + w * 0.25f, b.y + 54, b.z - w * 0.25f, b.w - 104);
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].icon != null)
                {
                    float y = b.y + 72 + i * 42;
                    Image icon = LegionArt.Image(content, lines[i].icon, false, "Icon");
                    icon.preserveAspect = true;
                    LegionUI.Inside(icon.rectTransform, box, b.x + w * 0.52f, y, b.x + w * 0.52f + 34, y + 34);
                    Label(lines[i].text, b.x + w * 0.52f + 42, y - 2, b.z - 8, y + 36, 25f, LegionTextStyle.Hud, Color.white, TextAnchor.MiddleLeft);
                }
                else
                {
                    Label(lines[i].text, b.x + 12, b.w - 104, b.z - 12, b.w - 70, 22f, LegionTextStyle.Body, new Color(0.85f, 0.9f, 1f), TextAnchor.MiddleCenter);
                }
            }
            if (badge != null)
            {
                Image tag = UI.Panel(content, new Color(0.85f, 0.12f, 0.15f), "Badge");
                LegionUI.Inside(tag.rectTransform, box, b.z - 176, b.y - 16, b.z + 8, b.y + 20);
                TMP_Text t = MainMenuButtons.Label(tag.transform, badge, 22f, LegionTextStyle.PrimaryButton, Color.white);
                UI.Stretch(t.rectTransform);
            }
            BuyButton(new Vector4(b.x + 30, b.w - 66, b.z - 30, b.w - 14), price, buy, null);
        }

        private void ChestCard(Vector4 b, ChestDef chest, string art)
        {
            if (chest == null) return;
            // Ícone + valor (moeda "400–800", poção "1", espada "50 %") no lugar da descrição em 17.
            string equipment = chest.EquipmentChance >= 1f ? "100 %" : $"{Mathf.RoundToInt(chest.EquipmentChance * 100f)} %";
            Card(b, "Baú " + chest.Name, new[] { art },
                Lines(("Icons/coin", $"{Short(chest.MinGold)}–{Short(chest.MaxGold)}"), ("Icons/potion_red", chest.Potions.ToString()),
                    ("Icons/sword", equipment)),
                PriceOf(chest.Gold, chest.Gems), null, () => BuyChest(chest));
        }

        // Oferta limitada: baú comum pela metade do preço, uma vez por dia, com contagem até a virada.
        private void LimitedOffer(Vector4 b, ChestDef chest)
        {
            if (chest == null) return;
            int price = Mathf.Max(1, chest.Gold / 2);
            const string key = "limited_chest";
            bool done = DailyUsed(key);
            CardFrame(b, string.Empty);
            Label("Oferta Limitada", b.x + 10, b.y + 8, b.z - 10, b.y + 54, 24f, LegionTextStyle.PrimaryButton, Color.white, TextAnchor.MiddleCenter);
            // Relógio como selo no canto, por fora da carta (igual ao "MELHOR OFERTA").
            Image clock = UI.Panel(content, new Color(0.02f, 0.05f, 0.14f, 0.96f), "Timer");
            LegionUI.Inside(clock.rectTransform, box, b.z - 150, b.y - 16, b.z + 8, b.y + 20);
            Outline clockRing = clock.gameObject.AddComponent<Outline>();
            clockRing.effectColor = new Color(0.95f, 0.75f, 0.25f, 0.9f);
            clockRing.effectDistance = new Vector2(2f, -2f);
            TMP_Text timer = MainMenuButtons.Label(clock.transform, Countdown(), 22f, LegionTextStyle.Hud, Color.white);
            UI.Stretch(timer.rectTransform);
            Image picture = LegionArt.Image(content, "Chests/chest_steel", false, "Art");
            picture.preserveAspect = true;
            LegionUI.Inside(picture.rectTransform, box, b.x + 8, b.y + 58, b.x + 160, b.w - 72);
            (string, string)[] lines = { ("Icons/coin", $"{Format(chest.MinGold)}+"), ("Icons/potion_red", chest.Potions.ToString()), ("Icons/key", "-50%") };
            for (int i = 0; i < lines.Length; i++)
            {
                float y = b.y + 70 + i * 40;
                Image icon = LegionArt.Image(content, lines[i].Item1, false, "Icon");
                icon.preserveAspect = true;
                LegionUI.Inside(icon.rectTransform, box, b.x + 168, y, b.x + 200, y + 32);
                Label(lines[i].Item2, b.x + 206, y - 2, b.z - 8, y + 34, 24f, LegionTextStyle.Hud, Color.white, TextAnchor.MiddleLeft);
            }
            BuyButton(new Vector4(b.x + 30, b.w - 66, b.z - 30, b.w - 14), done ? Price.Done() : Price.Gold(price), done ? null : (Action)(() =>
            {
                if (!Economy.CanAfford(price, 0)) { NotEnough(); return; }
                Ui.Confirm("OFERTA LIMITADA", $"Baú {chest.Name} por {Format(price)} ouro?", "COMPRAR", () =>
                {
                    // Desconto: devolve a diferença depois de pagar o preço cheio do catálogo.
                    if (MetaService.BuyChest(chest.Id, out RewardBundle reward) == PurchaseResult.Success)
                    {
                        Economy.Add(chest.Gold - price, 0, "desconto da oferta limitada");
                        MarkDaily(key);
                        Ui.Push<RewardScreen>().Show("BAÚ " + chest.Name.ToUpperInvariant(), reward);
                    }
                    else NotEnough();
                });
            }), null);
        }

        private void DailyHeader(float y0, float y1)
        {
            SectionTitle("OFERTAS DIÁRIAS", y0, y1, null);
            Image clock = UI.Panel(content, new Color(0.02f, 0.05f, 0.14f, 0.92f), "Refresh");
            LegionUI.Inside(clock.rectTransform, box, 676, y0 + 10, 914, y1 - 8);
            TMP_Text text = MainMenuButtons.Label(clock.transform, "Atualiza em: " + Countdown(), 22f, LegionTextStyle.Hud, Color.white);
            UI.Stretch(text.rectTransform);
        }

        private void SectionTitle(string text, float y0, float y1, string icon)
        {
            Image ribbon = UI.Panel(content, new Color(0.1f, 0.32f, 0.85f), "Heading");
            LegionUI.Inside(ribbon.rectTransform, box, 14, y0, 350, y1);
            Outline o = ribbon.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0.45f, 0.75f, 1f);
            o.effectDistance = new Vector2(2f, -2f);
            Label(text, 30, y0, 345, y1, 28f, LegionTextStyle.PrimaryButton, Color.white, TextAnchor.MiddleLeft);
        }

        // Carta diária: uma compra por dia (o botão vira "COMPRADO" até a virada do dia).
        private void DailyCard(float x, string title, string art, string amount, Price price, string key, Func<bool> buy)
        {
            var b = new Vector4(x, 1268, x + 166, 1482);
            CardFrame(b, title);
            Image picture = LegionArt.Image(content, art, false, "Art");
            picture.preserveAspect = true;
            LegionUI.Inside(picture.rectTransform, box, x + 30, b.y + 50, b.z - 30, b.y + 130);
            Label("×" + amount, x + 8, b.y + 128, b.z - 8, b.y + 160, 24f, LegionTextStyle.Hud, Color.white, TextAnchor.MiddleCenter);
            bool done = key != null && DailyUsed(key);
            Action onBuy = null;
            if (buy != null && !done)
            {
                onBuy = () =>
                {
                    if (!Economy.CanAfford(price.GoldCost, price.GemCost)) { NotEnough(); return; }
                    Ui.Confirm("OFERTA DIÁRIA", $"{title} ({amount}) por {price.Describe()}?", "COMPRAR", () =>
                    {
                        if (buy())
                        {
                            MarkDaily(key);
                            GameAudio.Play(SoundId.UiConfirm);
                            Ui.Toast(title + " comprado!");
                        }
                        else NotEnough();
                        Ui.RefreshTop();
                    });
                };
            }
            BuyButton(new Vector4(x + 6, b.w - 54, b.z - 6, b.w - 8), done ? Price.Done() : price, onBuy, null);
        }

        private void SkinCard(Vector4 b, SkinDef skin, bool owned, bool selected)
        {
            CardFrame(b, skin.Name);
            Image swatch = UI.Panel(content, skin.Tint, "Tint");
            swatch.sprite = UIStyle.Circle;
            swatch.type = Image.Type.Simple;
            swatch.preserveAspect = true;
            LegionUI.Inside(swatch.rectTransform, box, b.x + 90, b.y + 60, b.z - 90, b.w - 76);
            Label("Cores das tropas", b.x + 10, b.w - 100, b.z - 10, b.w - 70, 22f, LegionTextStyle.Body, new Color(0.85f, 0.9f, 1f), TextAnchor.MiddleCenter);
            Vector4 button = new Vector4(b.x + 30, b.w - 66, b.z - 30, b.w - 14);
            if (owned)
            {
                BuyButton(button, selected ? Price.Text("EM USO") : Price.Text("USAR"), selected ? null : (Action)(() =>
                {
                    MetaService.SelectSkin(skin.Id);
                    Ui.Game.Restart();
                    Ui.RefreshTop();
                }), selected ? new Color(0.35f, 0.4f, 0.5f) : new Color(0.13f, 0.5f, 1f));
            }
            else
            {
                BuyButton(button, Price.Gems(skin.Gems), () => Purchase("Visual " + skin.Name, 0, skin.Gems, () => MetaService.BuySkin(skin.Id)), null);
            }
        }

        // LegionBuyButton (Tarefa 0.5): moeda + valor em cima, ação embaixo. COMPRADO vira selo escuro com ✓.
        // Preço em reais: selo EM BREVE e botão desligado até existir loja de pagamento.
        private void BuyButton(Vector4 b, Price price, Action onBuy, Color? color)
        {
            LegionBuyButton.Variant variant = color.HasValue && color.Value.b > color.Value.g ? LegionBuyButton.Variant.Blue : LegionBuyButton.Variant.Green;
            LegionBuyButton button;
            switch (price.Kind)
            {
                case PriceKind.Gold:
                case PriceKind.Gems:
                    button = LegionBuyButton.Create(content, variant, price.Kind == PriceKind.Gems ? LegionCurrency.Gems : LegionCurrency.Gold, price.Amount,
                        "COMPRAR", onBuy);
                    break;
                case PriceKind.Real:
                    button = LegionBuyButton.Create(content, variant, LegionCurrency.None, 0, "COMPRAR", null);
                    button.SetPriceText(price.Describe());
                    button.SetComingSoon(true);
                    break;
                case PriceKind.Done:
                    button = LegionBuyButton.Create(content, variant, LegionCurrency.None, 0, string.Empty, null);
                    button.SetBought(true);
                    break;
                default:
                    button = LegionBuyButton.Create(content, variant, LegionCurrency.None, 0, price.Describe(), onBuy);
                    if (onBuy == null) button.SetBought(true, price.Describe());
                    break;
            }
            LegionUI.Inside((RectTransform)button.transform, box, b.x, b.y, b.z, b.w);
        }

        private TMP_Text Label(string text, float x0, float y0, float x1, float y1, float size, LegionTextStyle style, Color color, TextAnchor anchor)
        {
            RectTransform rect = Area(x0, y0, x1, y1, "Text");
            TMP_Text label = UI.Label(rect, text, Mathf.RoundToInt(size * LegionUI.Px), color, anchor, true, "Value", style);
            label.richText = true;
            UI.Stretch(label.rectTransform);
            return label;
        }

        private static (string, string)[] Lines(params (string, string)[] lines) => lines;

        // ---------------------------------------------------------------- compras

        private bool GrantGold(int amount, int gemPrice)
        {
            if (!Economy.TrySpend(0, gemPrice, $"{amount} moedas")) return false;
            Economy.Add(amount, 0, "loja: moedas por gemas");
            return true;
        }

        private void Purchase(string item, int goldCost, int gemCost, Func<PurchaseResult> purchase)
        {
            if (!Economy.CanAfford(goldCost, gemCost)) { NotEnough(); return; }
            Ui.Confirm("CONFIRMAR COMPRA", $"{item}\npor {PriceOf(goldCost, gemCost).Describe()}?", "COMPRAR", () =>
            {
                PurchaseResult result = purchase();
                GameAudio.Play(result == PurchaseResult.Success ? SoundId.UiConfirm : SoundId.UiBack);
                Ui.Toast(result == PurchaseResult.Success ? item + " comprado!" : MetaService.Explain(result));
                Ui.RefreshTop();
            });
        }

        private void BuyChest(ChestDef chest)
        {
            if (!Economy.CanAfford(chest.Gold, chest.Gems)) { NotEnough(); return; }
            Ui.Confirm("ABRIR BAÚ", $"Baú {chest.Name}\npor {PriceOf(chest.Gold, chest.Gems).Describe()}?", "ABRIR", () =>
            {
                if (MetaService.BuyChest(chest.Id, out RewardBundle reward) == PurchaseResult.Success)
                    Ui.Push<RewardScreen>().Show("BAÚ " + chest.Name.ToUpperInvariant(), reward);
                else NotEnough();
            });
        }

        private void NotEnough()
        {
            Ui.Toast("Saldo insuficiente");
            GameAudio.Play(SoundId.UiBack);
        }

        // Uma compra por dia por oferta (data UTC salva em PlayerPrefs).
        private static string Today => DateTime.UtcNow.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        private static bool DailyUsed(string key) => PlayerPrefs.GetString("LR_Daily_" + key, string.Empty) == Today;

        private static void MarkDaily(string key)
        {
            PlayerPrefs.SetString("LR_Daily_" + key, Today);
            PlayerPrefs.Save();
        }

        private static string Countdown()
        {
            TimeSpan left = DateTime.UtcNow.Date.AddDays(1) - DateTime.UtcNow;
            return $"{(int)left.TotalHours}h {left.Minutes:00}m";
        }

        private static string Format(int value) => value.ToString("N0", CultureInfo.GetCultureInfo("pt-BR"));

        // Nomes curtos nas diárias: "Poção de Velocidade" -> "Velocidade".
        private static string ShortPotion(string name) => name.Replace("Poção de ", string.Empty).Replace("Poção ", string.Empty);

        private static string Short(int value) => LegionFormat.Short(value);

        private static string EquipmentArt(EquipmentDef item) => item.Slot switch
        {
            EquipSlot.Weapon => "Icons/sword",
            EquipSlot.Armor => "Icons/armor",
            _ => "Icons/crystal"
        };

        private static Price PriceOf(int goldCost, int gemCost) => gemCost > 0 ? Price.Gems(gemCost) : Price.Gold(goldCost);

        private enum PriceKind { Real, Gold, Gems, Text, Done }

        private struct Price
        {
            public PriceKind Kind;
            public int Amount;
            public float Reais;
            public string Label;

            public int GoldCost => Kind == PriceKind.Gold ? Amount : 0;
            public int GemCost => Kind == PriceKind.Gems ? Amount : 0;

            public static Price Real(float reais) => new Price { Kind = PriceKind.Real, Reais = reais };
            public static Price Gold(int amount) => new Price { Kind = PriceKind.Gold, Amount = amount };
            public static Price Gems(int amount) => new Price { Kind = PriceKind.Gems, Amount = amount };
            public static Price Text(string label) => new Price { Kind = PriceKind.Text, Label = label };
            public static Price Done() => new Price { Kind = PriceKind.Done, Label = "COMPRADO" };

            public string Describe() => Kind switch
            {
                PriceKind.Real => "R$ " + Reais.ToString("0.00", CultureInfo.GetCultureInfo("pt-BR")),
                PriceKind.Gold => Format(Amount) + " ouro",
                PriceKind.Gems => Format(Amount) + " gemas",
                _ => Label
            };
        }

        // ---------------------------------------------------------------- navegação

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
            }
        }
    }
}
