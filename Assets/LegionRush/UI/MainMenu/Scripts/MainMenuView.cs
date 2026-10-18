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
        private MainMenuModal moreModal;
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

            Image castle = Layer(scenery, "Background_FrozenKingdom", new Vector2(0.5f, 1f - 410f / 1920f), 1.08f, 0f, 0.7f);
            RectTransform castleRect = castle.rectTransform;

            // Chão de neve escurecendo até a base, fundindo com o pé do castelo.
            Image ground = MainMenuArt.Image(scenery, "Background_Ground", "Ground");
            ground.preserveAspect = false;
            RectTransform g = ground.rectTransform;
            g.anchorMin = new Vector2(0f, 0f);
            g.anchorMax = new Vector2(1f, 0f);
            g.pivot = new Vector2(0.5f, 0f);
            g.sizeDelta = new Vector2(0f, 1130f);

            // Pinheiros, rochas, bandeiras e tochas nas laterais, presos ao castelo (acompanham sua escala).
            Deco(castleRect, "Deco_Pine_2", -0.06f, -0.38f, 0.17f, 0.32f, false, 0f, 0.9f);
            Deco(castleRect, "Deco_Pine_2", 0.83f, -0.38f, 1.06f, 0.32f, true, 0f, 0.9f);
            Deco(castleRect, "Deco_SnowRock_3", -0.04f, -0.36f, 0.2f, -0.08f, false, 0f, 1f);
            Deco(castleRect, "Deco_SnowRock_2", 0.8f, -0.36f, 1.04f, -0.1f, true, 0f, 1f);
            Deco(castleRect, "Deco_Torch_1", 0.27f, -0.2f, 0.36f, 0.16f, false, 0f, 1.1f);
            Deco(castleRect, "Deco_Torch_2", 0.64f, -0.2f, 0.73f, 0.16f, false, 0f, 1.1f);
            Deco(castleRect, "Deco_Banner_1", 0.1f, -0.28f, 0.26f, 0.3f, false, 2.5f, 1.2f);
            Deco(castleRect, "Deco_Banner_1", 0.74f, -0.28f, 0.9f, 0.3f, false, 2.5f, 1.2f);

            // Rei de costas erguendo a espada e o exército avançando (primeiro plano).
            Image army = MainMenuArt.Image(scenery, "Character_KingArmy", "KingArmy");
            RectTransform a = army.rectTransform;
            a.anchorMin = a.anchorMax = new Vector2(0.5f, 0f);
            a.pivot = new Vector2(0.5f, 0f);
            float width = 860f;
            a.sizeDelta = new Vector2(width, width * 463f / 420f);
            a.anchoredPosition = new Vector2(10f, 150f);
            army.gameObject.AddComponent<UIParallaxLayer>().depth = 1.6f;

            UISnow.Attach(scenery);

            // Sombras para leitura: topo (perfil e carteira) e base (painel, JOGAR, navegação).
            Shade(scenery, true, 300f, 0.55f);
            Shade(scenery, false, 760f, 1f);
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

        private static void Shade(RectTransform parent, bool top, float height, float alpha)
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
        }

        // ---------------------------------------------------------------- topo

        private void BuildTop(RectTransform root)
        {
            Image logo = MainMenuArt.Image(root, "Logo_LegionRush", "Logo");
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
            rewards = Side(root, "Icon_Rewards", "RECOMPENSAS", MainMenuDestination.Achievements, LegionUI.Edge.TopRight, 791, 306, 923, 478, right, 0.12f,
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

        // MAIS: as demais telas do jogo, com os mesmos botões das colunas.
        private void BuildMoreMenu(RectTransform root)
        {
            moreModal = MainMenuModal.Create(root, "MAIS");
            (string icon, string label, MainMenuDestination destination)[] items =
            {
                ("Icon_Home", "MUNDOS", MainMenuDestination.Worlds),
                ("Icon_Store", "INVENTÁRIO", MainMenuDestination.Inventory),
                ("Icon_Upgrades", "EQUIPAR", MainMenuDestination.Equipment),
                ("Icon_Rewards", "CONQUISTAS", MainMenuDestination.Achievements),
                ("Icon_Season", "RECORDES", MainMenuDestination.Ranking),
                ("Icon_TimedChest", "DIÁRIA", MainMenuDestination.DailyChest),
            };
            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];
                MainMenuSideButton button = MainMenuSideButton.Create(moreModal.Content, item.icon, item.label, () =>
                {
                    moreModal.gameObject.SetActive(false);
                    Go(item.destination);
                });
                int column = i % 3;
                int row = i / 3;
                UI.Place((RectTransform)button.transform, 0.03f + column * 0.33f, 0.52f - row * 0.5f, 0.3f + column * 0.33f, 0.98f - row * 0.5f);
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
            rewards.Badge.Set(data.ClaimableAchievements > 0);
            upgrades.Badge.Set(data.UpgradeAffordable);
            chest.Badge.Set(data.DailyReady);
            store.Badge.Set(false);
            events.Badge.Set(false);
            messages.Badge.Set(false);
            pass.Badge.Set(false);
            heroes.Badge.Set(false);

            rewards.Countdown.SetMessage($"{data.AchievementsDone}/{data.AchievementsTotal}", Color.white);
            bossLock.gameObject.SetActive(!data.BossAvailable);
            bossArt.color = data.BossAvailable ? Color.white : new Color(0.62f, 0.62f, 0.66f);
            Tick();
        }

        // Uma vez por segundo: contagem regressiva do baú diário.
        public void Tick()
        {
            if (model.DailyReady) chest.Countdown.SetMessage("ABRIR!", new Color(0.5f, 1f, 0.45f));
            else chest.Countdown.Set(System.DateTime.UtcNow.Date.AddDays(1) - System.DateTime.UtcNow);
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
