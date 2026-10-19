using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LegionRush
{
    // Passe de batalha no layout do mockup: temporada com contagem, barra de progresso, nível atual, LIBERAR PREMIUM,
    // trilha GRÁTIS × PREMIUM com 10 níveis numerados no meio.
    // A trilha grátis funciona: cada nível libera a cada 5 estrelas ganhas nas fases e o prêmio é coletado aqui.
    // A trilha premium depende de pagamento com dinheiro real, que o jogo ainda não tem: fica bloqueada.
    public class BattlePassScreen : UIScreen
    {
        private const int StarsPerTier = 5;
        private const string ClaimKey = "LR_Pass_S1_";
        private static readonly Vector4 Box = new Vector4(0, 0, LegionUI.RefWidth, LegionUI.RefHeight);

        private enum Kind { Gold, Gems, Potion, Chest }

        private static readonly (Kind kind, int amount, string id)[] Free =
        {
            (Kind.Gold, 500, null), (Kind.Gems, 15, null), (Kind.Gold, 300, null), (Kind.Potion, 1, "pocao_ataque"), (Kind.Gems, 15, null),
            (Kind.Chest, 1, "comum"), (Kind.Gold, 800, null), (Kind.Chest, 1, "raro"), (Kind.Gems, 30, null), (Kind.Chest, 1, "epico")
        };

        private static readonly (string art, string text)[] Premium =
        {
            ("Chests/chest_epic", "Baú Épico"), ("Icons/gem", "100"), ("Chests/chest_rare", "Baú Raro"), ("Icons/key", "1 Chave de Baú"),
            ("Hud/portrait_king", "Fragmentos de Herói x10"), ("Chests/chest_epic", "Baú Épico"), ("Rewards/gem_bag", "200"),
            ("Rewards/coin_pile", "5.000"), ("Icons/scroll", "Skin Épica\n<size=80%>Exclusiva da Temporada</size>"),
            ("Chests/chest_royal", "Skin Lendária\n<size=80%>Exclusiva da Temporada</size>")
        };

        private MenuChrome chrome;
        private RectTransform body;

        protected override void Build(RectTransform root)
        {
            chrome = MenuChrome.Build(root, Ui, "PASSE DE BATALHA", OnBack, 640f);
            Image info = UI.Panel(root, new Color(0.08f, 0.2f, 0.5f), "Info");
            LegionUI.Place(info.rectTransform, LegionUI.Edge.TopRight, 852, 150, 920, 218);
            Outline ring = info.gameObject.AddComponent<Outline>();
            ring.effectColor = new Color(0.5f, 0.8f, 1f);
            ring.effectDistance = new Vector2(2f, -2f);
            TMP_Text i = MainMenuButtons.Label(info.transform, "i", 40f, LegionTextStyle.PrimaryButton, Color.white);
            UI.Stretch(i.rectTransform);
            MainMenuButtons.Make(info.rectTransform, info, () => Ui.Info("PASSE DE BATALHA",
                $"Cada {StarsPerTier} estrelas ganhas nas fases liberam um nível. Toque em COLETAR para receber o prêmio grátis.\n\n" +
                "A trilha Premium chega quando a loja de pagamento for integrada."));
            body = UI.Rect(root, "Body");
            UI.Stretch(body);
            MenuChrome.Nav(root, Ui, MainMenuDestination.BattlePass);
        }

        public override void Refresh()
        {
            if (!SaveManager.IsLoaded) return;
            chrome.Refresh(Ui);
            UI.Clear(body);
            RectTransform area = LegionUI.Place(UI.Rect(body, "Area"), LegionUI.Edge.Top, 0, 0, LegionUI.RefWidth, LegionUI.RefHeight);

            int stars = SaveManager.Current.progress.TotalStars();
            int reached = Mathf.Min(Free.Length, stars / StarsPerTier);
            Header(area, stars, reached);

            Image freeHead = UI.Panel(area, new Color(0.12f, 0.42f, 0.95f), "Free");
            LegionUI.Inside(freeHead.rectTransform, Box, 46, 497, 462, 563);
            MenuChrome.Text(area, Box, 46, 497, 462, 563, "GRÁTIS", 32f, LegionTextStyle.PrimaryButton, Color.white, TextAnchor.MiddleCenter);
            Image premiumHead = UI.Panel(area, new Color(0.98f, 0.6f, 0.12f), "Premium");
            LegionUI.Inside(premiumHead.rectTransform, Box, 478, 497, 898, 563);
            MenuChrome.Text(area, Box, 478, 497, 860, 563, "PREMIUM", 32f, LegionTextStyle.PrimaryButton, Color.white, TextAnchor.MiddleCenter);
            MenuChrome.Art(area, Box, 838, 506, 884, 554, "Badges/lock");

            // Linha vertical que liga os números dos níveis.
            Image spine = UI.Panel(area, new Color(0.25f, 0.4f, 0.7f, 0.9f), "Spine");
            LegionUI.Inside(spine.rectTransform, Box, 462, 570, 478, 1440);
            for (int t = 0; t < Free.Length; t++) Row(area, t, reached);
        }

        // ---------------------------------------------------------------- topo

        private void Header(RectTransform parent, int stars, int reached)
        {
            Image hero = HeroesArt.Image(parent, "Showcase_King", "Hero");
            LegionUI.Inside(hero.rectTransform, Box, 560, 214, 940, 486);
            Image castle = MainMenuArt.Image(parent, "Thumb_FrozenKingdom", "Castle");
            castle.preserveAspect = true;
            LegionUI.Inside(castle.rectTransform, Box, 440, 240, 640, 390);

            Image season = UI.Panel(parent, new Color(0.04f, 0.09f, 0.24f, 0.92f), "Season");
            LegionUI.Inside(season.rectTransform, Box, 136, 250, 460, 392);
            MenuChrome.Text(parent, Box, 152, 258, 450, 296, "TEMPORADA 1", 26f, LegionTextStyle.PrimaryButton, Color.white);
            MenuChrome.Text(parent, Box, 152, 292, 450, 338, "REINO CONGELADO", 34f, LegionTextStyle.PrimaryButton, Color.white);
            MenuChrome.Art(parent, Box, 154, 346, 190, 382, "Icons/energy");
            MenuChrome.Text(parent, Box, 196, 344, 450, 384, "Termina em: " + SeasonLeft(), 24f, LegionTextStyle.Hud, Color.white);

            Image track = UI.Panel(parent, new Color(0.03f, 0.07f, 0.18f, 0.95f), "Track");
            LegionUI.Inside(track.rectTransform, Box, 46, 410, 484, 466);
            int total = Free.Length * StarsPerTier;
            float ratio = Mathf.Clamp01(stars / (float)total);
            if (ratio > 0f)
            {
                Image fill = UI.Panel(parent, MenuChrome.GoldColor, "Fill");
                LegionUI.Inside(fill.rectTransform, Box, 110, 418, Mathf.Lerp(120, 478, ratio), 458);
            }
            MenuChrome.Art(parent, Box, 54, 396, 124, 466, "Icons/crown");
            MenuChrome.Text(parent, Box, 130, 410, 470, 466, $"{Mathf.Min(stars, total)}/{total}", 30f, LegionTextStyle.Hud, Color.white, TextAnchor.MiddleCenter);
            Hex(parent, 468, 388, 550, 478, Mathf.Max(1, reached), reached > 0, 40f);

            MenuChrome.Button(parent, Box, 574, 398, 894, 476, "LIBERAR PREMIUM\n<size=75%>R$ 27,90</size>", MenuChrome.GoldColor,
                () => Ui.Info("PREMIUM", "A trilha Premium chega quando a loja de pagamento for integrada.\n\nA trilha grátis já funciona: ganhe estrelas nas fases e colete os prêmios."),
                26f);
        }

        // ---------------------------------------------------------------- trilha

        private void Row(RectTransform parent, int tier, int reached)
        {
            float y0 = 566 + tier * 88;
            float y1 = y0 + 80;
            bool unlocked = tier < reached;
            bool claimed = PlayerPrefs.GetInt(ClaimKey + tier, 0) == 1;
            bool last = tier == Free.Length - 1;

            Image left = MenuChrome.Panel(parent, Box, 46, y0, 436, y1, "Free" + tier);
            if (last) left.color = new Color(1f, 0.85f, 0.55f);
            var reward = Free[tier];
            MenuChrome.Art(parent, Box, 90, y0 + 6, 190, y1 - 6, FreeArt(reward.kind, reward.id));
            MenuChrome.Text(parent, Box, 206, y0, 330, y1, FreeText(reward.kind, reward.amount, reward.id), 24f, LegionTextStyle.Hud, Color.white);
            if (claimed)
            {
                MenuChrome.Art(parent, Box, 334, y0 + 20, 378, y1 - 20, "Badges/check");
            }
            else if (unlocked)
            {
                int t = tier;
                MenuChrome.Button(parent, Box, 300, y0 + 16, 428, y1 - 16, "COLETAR", MenuChrome.Green, () => Claim(t), 20f);
            }
            else
            {
                MenuChrome.Art(parent, Box, 380, y0 + 22, 414, y1 - 22, "Badges/lock");
            }

            Hex(parent, 438, y0 + 2, 502, y1 - 2, tier + 1, unlocked, 30f);

            Image right = MenuChrome.Panel(parent, Box, 504, y0, 898, y1, "Premium" + tier);
            right.color = new Color(1f, 0.72f, 0.4f);
            MenuChrome.Art(parent, Box, 540, y0 + 6, 650, y1 - 6, Premium[tier].art);
            MenuChrome.Text(parent, Box, 666, y0, 846, y1, Premium[tier].text, 22f, LegionTextStyle.PrimaryButton, Color.white);
            MenuChrome.Art(parent, Box, 852, y0 + 22, 886, y1 - 22, "Badges/lock");
        }

        private static void Hex(RectTransform parent, float x0, float y0, float x1, float y1, int number, bool gold, float size)
        {
            Image badge = UI.Panel(parent, gold ? MenuChrome.GoldColor : new Color(0.15f, 0.45f, 0.95f), "Tier");
            badge.sprite = UIStyle.Circle;
            badge.type = Image.Type.Simple;
            badge.preserveAspect = true;
            LegionUI.Inside(badge.rectTransform, Box, x0, y0, x1, y1);
            Outline ring = badge.gameObject.AddComponent<Outline>();
            ring.effectColor = gold ? new Color(1f, 0.95f, 0.6f) : new Color(0.6f, 0.85f, 1f);
            ring.effectDistance = new Vector2(3f, -3f);
            MenuChrome.Text(parent, Box, x0, y0, x1, y1, number.ToString(), size, LegionTextStyle.PrimaryButton,
                gold ? new Color(0.35f, 0.18f, 0.02f) : Color.white, TextAnchor.MiddleCenter);
        }

        // ---------------------------------------------------------------- prêmios

        private void Claim(int tier)
        {
            if (PlayerPrefs.GetInt(ClaimKey + tier, 0) == 1) return;
            var r = Free[tier];
            PlayerPrefs.SetInt(ClaimKey + tier, 1);
            PlayerPrefs.Save();
            RewardBundle bundle;
            switch (r.kind)
            {
                case Kind.Chest:
                    bundle = MetaService.OpenChest(MetaCatalog.Chest(r.id));
                    SaveManager.MarkDirty();
                    break;
                case Kind.Potion:
                    SaveManager.Current.inventory.Add(r.id, r.amount);
                    SaveManager.MarkDirty();
                    bundle = new RewardBundle();
                    bundle.Lines.Add(MetaCatalog.Potion(r.id).Name);
                    break;
                default:
                    int gold = r.kind == Kind.Gold ? r.amount : 0;
                    int gems = r.kind == Kind.Gems ? r.amount : 0;
                    Economy.Add(gold, gems, "passe de batalha");
                    bundle = new RewardBundle { Gold = gold, Gems = gems };
                    if (gold > 0) bundle.Lines.Add($"+{gold} ouro");
                    if (gems > 0) bundle.Lines.Add($"+{gems} gemas");
                    break;
            }
            SaveManager.Save();
            Ui.Push<RewardScreen>().Show("PASSE · NÍVEL " + (tier + 1), bundle);
        }

        private static string FreeArt(Kind kind, string id) => kind switch
        {
            Kind.Gold => "Rewards/coin_pile",
            Kind.Gems => "Icons/gem",
            Kind.Potion => "Icons/potion_red",
            _ => id == "epico" ? "Chests/chest_epic" : id == "raro" ? "Chests/chest_rare" : "Chests/chest_wood"
        };

        private static string FreeText(Kind kind, int amount, string id) => kind switch
        {
            Kind.Gold or Kind.Gems => MenuChrome.Format(amount),
            Kind.Potion => MetaCatalog.Potion(id).Name,
            _ => "Baú " + MetaCatalog.Chest(id).Name
        };

        // A temporada vira no fim de cada mês.
        private static string SeasonLeft()
        {
            DateTime now = DateTime.UtcNow;
            DateTime end = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1);
            TimeSpan left = end - now;
            return $"{left.Days}d {left.Hours}h";
        }
    }
}
