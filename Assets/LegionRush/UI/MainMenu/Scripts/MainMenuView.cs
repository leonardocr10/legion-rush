using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LegionRush
{
    // Monta a tela inicial na composição da arte de referência (941×1672) com os componentes de MainMenuWidgets.
    // Cada grupo fica preso à borda certa: topo (perfil, carteira, ajustes, logo), colunas laterais (topo-esquerda e
    // topo-direita) e base (mundo, JOGAR, MODO BOSS, navegação). Em telas mais altas sobra cenário no meio; em
    // telas largas (tablets, dobráveis) os grupos ficam nas bordas e nada é esticado.
    public class MainMenuView
    {
        private readonly IMainMenuNavigator nav;

        private MainMenuProfilePanel profile;
        private MainMenuCurrencyBar gold;
        private MainMenuCurrencyBar gems;
        private MainMenuSideButton store, missions, events, upgrades, chest;
        private MainMenuSideButton ranking, rewards, season, friends, messages;
        private MainMenuWorldPanel world;
        private Image bossArt;
        private Image bossLock;
        private MainMenuNavButton home, heroes, troops, pass, more;
        private LegionModal moreModal;
        private MainMenuModel model;

        public RectTransform Root { get; }

        public MainMenuView(RectTransform root, IMainMenuNavigator navigator)
        {
            Root = root;
            nav = navigator;
            BuildScenery(root);
            BuildTop(root);
            BuildSides(root);
            BuildBottom(root);
            BuildMoreMenu(root);
        }

        private void Go(MainMenuDestination destination) => nav.Open(destination);

        // Versão 2048 do Prompt 2 (ex.: Background_FrozenKingdom_2048), quando já estiver no catálogo.
        private static string HiRes(string sprite) => MainMenuArt.Has(sprite + "_2048") ? sprite + "_2048" : sprite;

        // ---------------------------------------------------------------- cenário em camadas

        private static void BuildScenery(RectTransform root)
        {
            RectTransform scenery = UI.Rect(root, "Scenery");
            scenery.gameObject.AddComponent<UIFullBleed>();

            Image sky = MainMenuArt.Image(scenery, "Background_Sky", "Sky");
            sky.preserveAspect = false;
            UI.Stretch(sky.rectTransform);

            // Alturas em fração da tela (referência 1920): em celulares mais altos o cenário desce junto e não abre vão.
            Image mountains = Layer(scenery, "Background_Mountains", new Vector2(0.5f, 1f - 150f / 1920f), 1.02f, 0f, 0.4f);
            mountains.color = new Color(0.92f, 0.95f, 1f);

            // Castelo mais distante, como na referência: menor e com o topo logo abaixo do logo.
            Image castle = Layer(scenery, HiRes("Background_FrozenKingdom"), new Vector2(0.5f, 1f - 470f / 1920f), 1.2f, 0f, 0.7f);
            RectTransform castleRect = castle.rectTransform;

            // Chão: degradê azul (transparente -> #1C3E96 -> #081A4D) a partir de 44 % da altura, sem a faixa de névoa clara.
            Image ground = UI.Rect(scenery, "Ground").gameObject.AddComponent<Image>();
            ground.sprite = GroundGradient();
            ground.raycastTarget = false;
            RectTransform g = ground.rectTransform;
            g.anchorMin = new Vector2(0f, 0f);
            g.anchorMax = new Vector2(1f, 0.44f);
            g.offsetMin = g.offsetMax = Vector2.zero;

            // Só as tochas presas ao castelo; bandeiras, pinheiros e rochas saíram (ficavam atrás dos botões e da multidão).
            Deco(castleRect, "Deco_Torch_1", 0.27f, -0.2f, 0.36f, 0.16f, false, 0f, 1.1f);
            Deco(castleRect, "Deco_Torch_2", 0.64f, -0.2f, 0.73f, 0.16f, false, 0f, 1.1f);

            // Exército nas laterais, como na referência. Com Character_ArmyCrowd (Prompt 2) usa a arte nova; sem ela, monta a
            // tropa com os soldados do próprio sprite do rei (recortes sem a capa), espelhados e em profundidades diferentes.
            // Rei e tropa num grupo preso à base que sobe em telas mais altas que 16:9 (sem vão de neve sob o castelo).
            RectTransform army = UI.Rect(scenery, "ArmyGroup");
            army.gameObject.AddComponent<UITallScreenLift>().factor = 0.55f;
            bool crowdArt = MainMenuArt.Has("Character_ArmyCrowd");
            if (crowdArt)
            {
                Crowd(army, 610f, 0.78f, 1.2f, 300f, 330f);
                Crowd(army, 660f, 0.92f, 1.4f, 340f, 230f);
            }
            else
            {
                // Fundo: lanceiros menores e mais escuros, mais altos na tela (mais distantes).
                SoldierCrop(army, SpearSoldiers, 1.25f, -360f, 640f, true, 0.66f, 1.15f);
                SoldierCrop(army, SpearSoldiers, 1.25f, 360f, 640f, false, 0.66f, 1.15f);
                SoldierCrop(army, ShieldSoldiers, 1.35f, -230f, 590f, false, 0.72f, 1.2f);
                SoldierCrop(army, ShieldSoldiers, 1.35f, 230f, 590f, true, 0.72f, 1.2f);
                SoldierCrop(army, SpearSoldiers, 1.1f, -140f, 690f, false, 0.6f, 1.1f);
                SoldierCrop(army, SpearSoldiers, 1.1f, 140f, 690f, true, 0.6f, 1.1f);
                // Meio.
                SoldierCrop(army, ShieldSoldiers, 1.6f, -300f, 520f, true, 0.8f, 1.25f);
                SoldierCrop(army, ShieldSoldiers, 1.6f, 300f, 520f, false, 0.8f, 1.25f);
                SoldierCrop(army, ShieldSoldiers, 1.8f, -450f, 450f, false, 0.86f, 1.3f);
                SoldierCrop(army, ShieldSoldiers, 1.8f, 450f, 450f, true, 0.86f, 1.3f);
            }

            // Rei de costas erguendo a espada: a espada fica logo abaixo do logo e o pé atrás do painel do mundo.
            Image king = MainMenuArt.Image(army, MainMenuArt.Has("Character_King_2048") ? "Character_King_2048" : "Character_KingArmy", "KingArmy");
            RectTransform a = king.rectTransform;
            a.anchorMin = a.anchorMax = new Vector2(0.5f, 0f);
            a.pivot = new Vector2(0.5f, 0f);
            // Mesma escala da referência (rei ~64 % da largura do grid, espada logo abaixo do logo, capa atrás do painel).
            float width = 602f;
            float aspect = king.sprite != null ? king.sprite.rect.height / king.sprite.rect.width : 463f / 420f;
            a.sizeDelta = new Vector2(width, width * aspect);
            a.anchoredPosition = new Vector2(0f, 414f);
            king.gameObject.AddComponent<UIParallaxLayer>().depth = 1.6f;

            // Frente: soldados maiores nas bordas, por cima das laterais do rei.
            if (!crowdArt)
            {
                SoldierCrop(army, SpearSoldiers, 2.2f, -430f, 300f, true, 0.95f, 1.5f);
                SoldierCrop(army, SpearSoldiers, 2.2f, 430f, 300f, false, 0.95f, 1.5f);
                SoldierCrop(army, ShieldSoldiers, 2.4f, -470f, 150f, false, 0.9f, 1.6f);
                SoldierCrop(army, ShieldSoldiers, 2.4f, 470f, 150f, true, 0.9f, 1.6f);
            }

            UISnow.Attach(scenery);

            // Sombras para leitura: topo (perfil e carteira) e base (painel, JOGAR, navegação).
            Shade(scenery, true, 300f, 0.55f);
            Image bottom = Shade(scenery, false, 420f, 0.85f);
            bottom.color = new Color(0.02f, 0.07f, 0.23f, 0.85f);
        }

        // Duas metades espelhadas da multidão (esquerda e direita), com o centro vazio para o rei.
        private static void Crowd(RectTransform parent, float width, float tint, float depth, float offsetX, float y)
        {
            // Sem a arte própria não monta nada: o recorte do sprite do rei mostrava só a capa, não soldados.
            if (!MainMenuArt.Has("Character_ArmyCrowd")) return;
            for (int side = -1; side <= 1; side += 2)
            {
                RectTransform holder = UI.Rect(parent, side < 0 ? "CrowdL" : "CrowdR");
                holder.anchorMin = holder.anchorMax = new Vector2(0.5f, 0f);
                holder.pivot = new Vector2(0.5f, 0f);
                holder.anchoredPosition = new Vector2(side * offsetX, y);
                holder.gameObject.AddComponent<UIParallaxLayer>().depth = depth;
                holder.gameObject.AddComponent<RectMask2D>();
                // Faixa larga, mais densa nas laterais; cada lado mostra a sua metade.
                holder.sizeDelta = new Vector2(width, width * 0.75f);
                Image crowd = MainMenuArt.Image(holder, "Character_ArmyCrowd", "Crowd");
                RectTransform r = crowd.rectTransform;
                r.anchorMin = new Vector2(side < 0 ? 0f : -1f, 0f);
                r.anchorMax = new Vector2(side < 0 ? 2f : 1f, 1f);
                r.offsetMin = r.offsetMax = Vector2.zero;
                crowd.color = new Color(tint, tint, tint);
            }
        }

        // Recortes (em pixels do sprite Character_KingArmy, origem no canto inferior esquerdo) com os soldados sem a capa.
        private static readonly Rect ShieldSoldiers = new Rect(0f, 0f, 150f, 160f);   // capacetes e escudos, à esquerda
        private static readonly Rect SpearSoldiers = new Rect(300f, 0f, 115f, 225f);  // lanceiros, à direita

        // Mostra só o retângulo `crop` do sprite do rei, na escala `scale` (unidades por pixel), com o pé em (x, y).
        private static void SoldierCrop(RectTransform parent, Rect crop, float scale, float x, float y, bool mirror, float tint, float depth)
        {
            Sprite king = MainMenuArt.Get("Character_KingArmy");
            if (king == null) return;
            RectTransform holder = UI.Rect(parent, "Soldiers");
            holder.anchorMin = holder.anchorMax = new Vector2(0.5f, 0f);
            holder.pivot = new Vector2(0.5f, 0f);
            holder.sizeDelta = new Vector2(crop.width * scale, crop.height * scale);
            holder.anchoredPosition = new Vector2(x, y);
            holder.gameObject.AddComponent<RectMask2D>();
            holder.gameObject.AddComponent<UIParallaxLayer>().depth = depth;
            if (mirror) holder.localScale = new Vector3(-1f, 1f, 1f);

            Image image = MainMenuArt.Image(holder, "Character_KingArmy", "Crop");
            image.preserveAspect = false;
            RectTransform r = image.rectTransform;
            r.anchorMin = r.anchorMax = r.pivot = Vector2.zero;
            r.sizeDelta = new Vector2(king.rect.width * scale, king.rect.height * scale);
            r.anchoredPosition = new Vector2(-crop.x * scale, -crop.y * scale);
            image.color = new Color(tint, tint, tint * 1.04f);
        }

        private static Sprite groundGradient;

        private static Sprite GroundGradient()
        {
            if (groundGradient != null) return groundGradient;
            const int h = 64;
            var texture = new Texture2D(1, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "LR_MenuGround" };
            Color clear = new Color(0.11f, 0.24f, 0.59f, 0f);
            Color mid = new Color(0x1C / 255f, 0x3E / 255f, 0x96 / 255f, 1f);
            Color deep = new Color(0x08 / 255f, 0x1A / 255f, 0x4D / 255f, 1f);
            for (int y = 0; y < h; y++)
            {
                float t = y / (h - 1f); // 0 = base, 1 = topo
                texture.SetPixel(0, y, t > 0.5f ? Color.Lerp(mid, clear, (t - 0.5f) * 2f) : Color.Lerp(deep, mid, t * 2f));
            }
            texture.Apply(false, true);
            groundGradient = Sprite.Create(texture, new Rect(0, 0, 1, h), new Vector2(0.5f, 0.5f), 100f);
            return groundGradient;
        }

        // Camada larga presa ao topo: largura relativa ao canvas, altura pela proporção do sprite.
        private static Image Layer(RectTransform parent, string sprite, Vector2 anchor, float widthScale, float y, float depth)
        {
            Image image = MainMenuArt.Image(parent, sprite, sprite);
            RectTransform rect = image.rectTransform;
            rect.anchorMin = new Vector2(0.5f - widthScale * 0.5f, anchor.y);
            rect.anchorMax = new Vector2(0.5f + widthScale * 0.5f, anchor.y);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, y);
            image.preserveAspect = false;
            AspectRatioFitter fitter = image.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
            fitter.aspectRatio = image.sprite != null ? image.sprite.rect.width / image.sprite.rect.height : 2f;
            image.gameObject.AddComponent<UIParallaxLayer>().depth = depth;
            return image;
        }

        private static void Deco(RectTransform parent, string sprite, float x0, float y0, float x1, float y1, bool mirror, float sway, float depth)
        {
            Image image = MainMenuArt.Image(parent, sprite, sprite);
            UI.Place(image.rectTransform, x0, y0, x1, y1);
            if (mirror) image.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
            UIParallaxLayer layer = image.gameObject.AddComponent<UIParallaxLayer>();
            layer.depth = depth;
            layer.sway = sway;
            layer.phase = x0 * 7f;
            if (sway != 0f) image.rectTransform.pivot = new Vector2(0.5f, 0.95f);
        }

        private static Image Shade(RectTransform parent, bool top, float height, float alpha)
        {
            Image shade = MainMenuArt.Image(parent, "Shade_Vertical", top ? "ShadeTop" : "ShadeBottom");
            shade.preserveAspect = false;
            shade.color = new Color(1f, 1f, 1f, alpha);
            RectTransform rect = shade.rectTransform;
            rect.anchorMin = new Vector2(0f, top ? 1f : 0f);
            rect.anchorMax = new Vector2(1f, top ? 1f : 0f);
            rect.pivot = new Vector2(0.5f, top ? 1f : 0f);
            rect.sizeDelta = new Vector2(0f, height);
            if (top) rect.localScale = new Vector3(1f, -1f, 1f);
            return shade;
        }

        // ---------------------------------------------------------------- topo

        private void BuildTop(RectTransform root)
        {
            Image logo = MainMenuArt.Image(root, HiRes("Logo_LegionRush"), "Logo");
            LegionUI.Place(logo.rectTransform, LegionUI.Edge.Top, 170, 104, 770, 506);
            logo.gameObject.AddComponent<UILogoIntro>();
            UIShineSweep.Attach(logo.rectTransform);
            UIShineSweep shine = logo.GetComponentInChildren<UIShineSweep>();
            shine.interval = 6.5f;
            shine.sweep = 0.9f;
            Transform clip = shine.transform;
            ((RectTransform)clip).offsetMin = new Vector2(80f, 90f);
            ((RectTransform)clip).offsetMax = new Vector2(-80f, -150f);
            clip.GetChild(0).GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.18f);

            profile = MainMenuProfilePanel.Create(root, () => Go(MainMenuDestination.Profile));
            LegionUI.Place((RectTransform)profile.transform, LegionUI.Edge.TopLeft, 14, 12, 394, 120);
            Enter(profile.transform, new Vector2(0f, 90f), 0f);

            gold = MainMenuCurrencyBar.Create(root, "Icon_Coin", () => Go(MainMenuDestination.ShopGold), "Gold");
            LegionUI.Place((RectTransform)gold.transform, LegionUI.Edge.Top, 403, 30, 630, 92);
            Enter(gold.transform, new Vector2(0f, 90f), 0.05f);
            gems = MainMenuCurrencyBar.Create(root, "Icon_Gem", () => Go(MainMenuDestination.ShopGems), "Gems");
            LegionUI.Place((RectTransform)gems.transform, LegionUI.Edge.Top, 638, 30, 852, 92);
            Enter(gems.transform, new Vector2(0f, 90f), 0.1f);

            Image settings = MainMenuArt.Image(root, "Button_Settings", "Settings");
            LegionUI.Place(settings.rectTransform, LegionUI.Edge.TopRight, 860, 22, 928, 92);
            MainMenuButtons.Make(settings.rectTransform, settings, () => Go(MainMenuDestination.Settings));
            Enter(settings.transform, new Vector2(0f, 90f), 0.15f);
        }

        // ---------------------------------------------------------------- colunas laterais

        private void BuildSides(RectTransform root)
        {
            var left = new Vector2(-180f, 0f);
            var right = new Vector2(180f, 0f);
            store = Side(root, "Icon_Store", "LOJA", MainMenuDestination.Shop, LegionUI.Edge.TopLeft, 18, 150, 150, 286, left, 0.08f);
            missions = Side(root, "Icon_Missions", "MISSÕES", MainMenuDestination.Missions, LegionUI.Edge.TopLeft, 18, 298, 150, 434, left, 0.12f);
            events = Side(root, "Icon_Events", "EVENTOS", MainMenuDestination.Events, LegionUI.Edge.TopLeft, 18, 446, 150, 582, left, 0.16f);
            upgrades = Side(root, "Icon_Upgrades", "MELHORIAS", MainMenuDestination.Upgrades, LegionUI.Edge.TopLeft, 18, 594, 150, 730, left, 0.2f);
            chest = Side(root, "Icon_TimedChest", string.Empty, MainMenuDestination.DailyChest, LegionUI.Edge.TopLeft, 18, 744, 150, 876, left, 0.24f,
                MainMenuSideButton.Kind.TimerOnly);

            ranking = Side(root, "Icon_Ranking", "RANKING", MainMenuDestination.Ranking, LegionUI.Edge.TopRight, 791, 150, 923, 294, right, 0.08f);
            rewards = Side(root, "Icon_Rewards", "RECOMPENSAS", MainMenuDestination.DailyChest, LegionUI.Edge.TopRight, 791, 306, 923, 478, right, 0.12f,
                MainMenuSideButton.Kind.LabeledWithTimer);
            season = Side(root, "Icon_Season", "TEMPORADA", MainMenuDestination.Season, LegionUI.Edge.TopRight, 791, 490, 923, 626, right, 0.16f);
            friends = Side(root, "Icon_Friends", "AMIGOS", MainMenuDestination.Friends, LegionUI.Edge.TopRight, 791, 638, 923, 774, right, 0.2f);
            messages = Side(root, "Icon_Messages", "MENSAGENS", MainMenuDestination.Messages, LegionUI.Edge.TopRight, 791, 786, 923, 922, right, 0.24f);
        }

        private MainMenuSideButton Side(RectTransform root, string icon, string label, MainMenuDestination destination, LegionUI.Edge edge,
            float x0, float y0, float x1, float y1, Vector2 from, float delay, MainMenuSideButton.Kind kind = MainMenuSideButton.Kind.Labeled)
        {
            MainMenuSideButton button = MainMenuSideButton.Create(root, icon, label, () => Go(destination), kind);
            LegionUI.Place((RectTransform)button.transform, edge, x0, y0, x1, y1);
            Enter(button.transform, from, delay);
            return button;
        }

        // ---------------------------------------------------------------- base

        private void BuildBottom(RectTransform root)
        {
            var up = new Vector2(0f, -200f);
            world = MainMenuWorldPanel.Create(root, () => Go(MainMenuDestination.World), () => Go(MainMenuDestination.WorldChest));
            LegionUI.Place((RectTransform)world.transform, LegionUI.Edge.Bottom, 135, 1112, 807, 1284);
            Enter(world.transform, up, 0.05f);

            // JOGAR: contêiner respirando + botão com afundar ao toque + faixa de brilho periódica.
            RectTransform playHolder = LegionUI.Place(UI.Rect(root, "PlayHolder"), LegionUI.Edge.Bottom, 180, 1300, 744, 1472);
            playHolder.gameObject.AddComponent<UIBreath>().amount = 0.018f;
            Enter(playHolder, up, 0.1f);
            Image play = MainMenuArt.Image(playHolder, "Button_Play", "Play");
            UI.Stretch(play.rectTransform);
            MainMenuButtons.Make(play.rectTransform, play, () => Go(MainMenuDestination.Play));
            UIShineSweep.Attach(play.rectTransform);
            RectTransform playClip = (RectTransform)play.rectTransform.Find("ShineClip");
            if (playClip != null) UI.Stretch(playClip, 30f, 30f, 22f, 26f);

            // MODO BOSS: aura vermelha pulsando atrás do retrato; cadeado quando ainda não liberado.
            RectTransform bossHolder = LegionUI.Place(UI.Rect(root, "BossHolder"), LegionUI.Edge.Bottom, 752, 1292, 922, 1470);
            Enter(bossHolder, up, 0.14f);
            Image aura = UI.Panel(bossHolder, new Color(1f, 0.1f, 0.05f, 0.3f), "Aura");
            aura.sprite = MainMenuArt.Get("Button_SideFrame");
            aura.type = Image.Type.Sliced;
            aura.pixelsPerUnitMultiplier = 0.5f;
            UI.Stretch(aura.rectTransform, -10f, -10f, -10f, -10f);
            UIGlowPulse pulse = aura.gameObject.AddComponent<UIGlowPulse>();
            pulse.min = 0.15f;
            pulse.max = 0.55f;
            bossArt = MainMenuArt.Image(bossHolder, "Button_Boss", "Boss");
            UI.Stretch(bossArt.rectTransform);
            TMP_TextShim.Label(bossArt.transform, "MODO BOSS", 20f, 0.06f, 0.05f, 0.94f, 0.22f);
            MainMenuButtons.Make(bossArt.rectTransform, bossArt, () => Go(MainMenuDestination.Boss));
            bossLock = LegionArt.Image(bossArt.transform, "Badges/lock", false, "Lock");
            UI.Place(bossLock.rectTransform, 0.62f, 0.66f, 0.96f, 0.98f);
            bossLock.gameObject.SetActive(false);

            float[] x0 = { 10, 196, 382, 568, 754 };
            home = NavButton(root, "Icon_Home", "INÍCIO", MainMenuDestination.Home, x0[0], 0.12f);
            heroes = NavButton(root, "Icon_Heroes", "HERÓIS", MainMenuDestination.Heroes, x0[1], 0.15f);
            troops = NavButton(root, "Icon_Troops", "TROPAS", MainMenuDestination.Troops, x0[2], 0.18f);
            pass = NavButton(root, "Icon_BattlePass", "PASSE", MainMenuDestination.BattlePass, x0[3], 0.21f);
            more = MainMenuNavButton.Create(root, "Icon_More", "MAIS", () => moreModal.Open());
            PlaceNav(more, x0[4], 0.24f);
            home.SetSelected(true);
        }

        private MainMenuNavButton NavButton(RectTransform root, string icon, string label, MainMenuDestination destination, float x, float delay)
        {
            MainMenuNavButton button = MainMenuNavButton.Create(root, icon, label, () => Go(destination));
            PlaceNav(button, x, delay);
            return button;
        }

        private static void PlaceNav(MainMenuNavButton button, float x, float delay)
        {
            LegionUI.Place((RectTransform)button.transform, LegionUI.Edge.Bottom, x, 1494, x + 178, 1660);
            Enter(button.transform, new Vector2(0f, -220f), delay);
        }

        // MAIS (LegionModal): fundo #040C24 a 75 %, moldura 9-slice e X dentro. Ícone com 60 % da altura e rótulo em 26.
        // RECORDES e DIÁRIA repetiam o RANKING e o baú da tela inicial: no lugar, MELHORIAS e CONFIGURAÇÕES.
        private void BuildMoreMenu(RectTransform root)
        {
            moreModal = LegionModal.Create(root, "MAIS", 0.26f, 0.74f);
            (string icon, string label, MainMenuDestination destination)[] items =
            {
                ("Icon_Home", "MUNDOS", MainMenuDestination.Worlds),
                ("Icon_Store", "INVENTÁRIO", MainMenuDestination.Inventory),
                ("Icon_Upgrades", "EQUIPAR", MainMenuDestination.Equipment),
                ("Icon_Rewards", "CONQUISTAS", MainMenuDestination.Achievements),
                ("Icon_Upgrades", "MELHORIAS", MainMenuDestination.Upgrades),
                ("Settings", "AJUSTES", MainMenuDestination.Settings),
            };
            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];
                Image tile = MainMenuArt.Image(moreModal.Content, "Button_SideFrame", "Item_" + item.label, true);
                int column = i % 3;
                int row = i / 3;
                UI.Place(tile.rectTransform, 0.02f + column * 0.33f, 0.52f - row * 0.52f, 0.31f + column * 0.33f, 1f - row * 0.52f);
                Image icon = item.icon == "Settings"
                    ? MainMenuArt.Image(tile.transform, "Button_Settings", "Icon")
                    : MainMenuArt.Image(tile.transform, item.icon, "Icon");
                UI.Place(icon.rectTransform, 0.14f, 0.36f, 0.86f, 0.96f);
                TMP_Text label = MainMenuButtons.Label(tile.transform, item.label, 26f, LegionTextStyle.PrimaryButton, Color.white);
                UI.Place(label.rectTransform, 0.03f, 0.04f, 0.97f, 0.34f);
                MainMenuButtons.Make(tile.rectTransform, tile, () =>
                {
                    moreModal.gameObject.SetActive(false);
                    Go(item.destination);
                });
            }
        }

        private static void Enter(Transform target, Vector2 from, float delay)
        {
            UISlideIn slide = target.gameObject.AddComponent<UISlideIn>();
            slide.offset = from;
            slide.delay = delay;
            if (!Application.isPlaying) slide.enabled = false;
        }

        // ---------------------------------------------------------------- dados

        public void Bind(MainMenuModel data)
        {
            model = data;
            profile.Set(data.PlayerName, data.PlayerLevel, data.Xp, data.XpMax);
            gold.Set(data.Gold);
            gems.Set(data.Gems);
            world.Set(data);

            missions.Badge.Set(data.ClaimableMissions > 0);
            rewards.Badge.Set(data.DailyReady);
            upgrades.Badge.Set(data.UpgradeAffordable);
            chest.Badge.Set(data.DailyReady);
            store.Badge.Set(false);
            events.Badge.Set(false);
            messages.Badge.Set(false);
            pass.Badge.Set(false);
            heroes.Badge.Set(false);

            bossLock.gameObject.SetActive(!data.BossAvailable);
            bossArt.color = data.BossAvailable ? Color.white : new Color(0.62f, 0.62f, 0.66f);
            Tick();
        }

        // Uma vez por segundo: contagem regressiva do baú diário.
        public void Tick()
        {
            // Baú diário e RECOMPENSAS abrem a recompensa diária: "ABRIR!" quando pronta, senão a contagem até a virada.
            foreach (MainMenuSideButton button in new[] { chest, rewards })
            {
                if (model.DailyReady) button.Countdown.SetMessage("ABRIR!", new Color(0.5f, 1f, 0.45f));
                else button.Countdown.Set(System.DateTime.UtcNow.Date.AddDays(1) - System.DateTime.UtcNow);
            }
        }

        public bool ModalOpen => moreModal != null && moreModal.gameObject.activeSelf;

        public bool CloseModal()
        {
            if (moreModal == null || !moreModal.gameObject.activeSelf) return false;
            moreModal.Close();
            return true;
        }

        // Rótulo "MODO BOSS" sobre a faixa escura do retrato.
        private static class TMP_TextShim
        {
            public static void Label(Transform parent, string text, float size, float x0, float y0, float x1, float y1)
            {
                TMPro.TMP_Text label = MainMenuButtons.Label(parent, text, size, LegionTextStyle.PrimaryButton, Color.white);
                UI.Place(label.rectTransform, x0, y0, x1, y1);
            }
        }
    }
}
