using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LegionRush
{
    // Ranking no layout do mockup: título com o rei e o troféu, abas GLOBAL/AMIGOS/CLÃS/TEMPORADA, pódio dos 3 primeiros,
    // tabela do 4º ao 10º e a linha do jogador.
    // O jogo ainda não tem servidor: GLOBAL é uma liga de rivais simulados (avisado na tela) onde o jogador entra pelos
    // troféus ganhos nas fases; TEMPORADA mostra os recordes reais; AMIGOS e CLÃS chegam com o modo online.
    public class RankingScreen : UIScreen
    {
        private static readonly Vector4 Box = new Vector4(0, 0, LegionUI.RefWidth, LegionUI.RefHeight);
        private static readonly string[] TabNames = { "GLOBAL", "AMIGOS", "CLÃS", "TEMPORADA" };

        private static readonly (string name, string clan, string avatar, int trophies)[] Rivals =
        {
            ("ShadowKing", "Legion Brasil", "Portrait_King", 5482), ("DarkQueen", "Titãs", "Portrait_Healer", 4987),
            ("FireLord", "Nova Era", "Portrait_Knight", 4653), ("WarriorBR", "Dragões de Aço", "Portrait_Lancer", 4312),
            ("LunaStar", "Ordem Real", "Portrait_Archer", 4125), ("IceWolf", "Reino do Norte", "Portrait_Mage", 3984),
            ("StormBlade", "Sombras", "Portrait_AssassinLocked", 3761), ("Ragnar", "Valquírias", "Portrait_PaladinLocked", 3542),
            ("PhoenixBR", "Fênix", "Portrait_ElfLocked", 3421), ("CavaleiroMG", "Minas Gerais", "Portrait_Knight", 3218),
            ("NeveAzul", "Gelo Eterno", "Portrait_Healer", 2410), ("Lanceiro77", "Muralha", "Portrait_Lancer", 1620),
            ("ArqueiraSP", "Flechas", "Portrait_Archer", 980), ("Novato", "Recrutas", "Portrait_Mage", 320)
        };

        private MenuChrome chrome;
        private RectTransform body;
        private readonly List<Image> tabs = new List<Image>();
        private int tab;

        private class Entry
        {
            public string Name, Clan, Avatar, Sub;
            public int Trophies;
            public bool Player;
        }

        protected override void Build(RectTransform root)
        {
            chrome = MenuChrome.Build(root, Ui, string.Empty, OnBack, 10f);
            RectTransform top = LegionUI.Place(UI.Rect(root, "Top"), LegionUI.Edge.Top, 0, 0, LegionUI.RefWidth, LegionUI.RefHeight);

            Image king = HeroesArt.Image(top, "Showcase_King", "King");
            LegionUI.Inside(king.rectTransform, Box, 640, 150, 940, 394);
            MenuChrome.Art(top, Box, 650, 270, 760, 380, "Icons/trophy");
            Image header = LegionArt.Image(top, "Popups/popup_header", false, "Header");
            LegionUI.Inside(header.rectTransform, Box, 200, 120, 740, 350);
            TMP_Text title = UI.Label(header.transform, "RANKING", 84, Color.white, TextAnchor.MiddleCenter, true, "Title", LegionTextStyle.Title);
            UI.Place(title.rectTransform, 0.15f, 0.12f, 0.85f, 0.64f);

            float[] x0 = { 26, 250, 474, 696 };
            float[] x1 = { 244, 468, 690, 914 };
            for (int i = 0; i < TabNames.Length; i++)
            {
                int index = i;
                Image button = UI.Panel(top, new Color(0.05f, 0.1f, 0.26f, 0.97f), "Tab" + i);
                LegionUI.Inside(button.rectTransform, Box, x0[i], 396, x1[i], 462);
                Outline ring = button.gameObject.AddComponent<Outline>();
                ring.effectDistance = new Vector2(2f, -2f);
                TMP_Text label = MainMenuButtons.Label(button.transform, TabNames[i], 26f, LegionTextStyle.PrimaryButton, Color.white);
                UI.Stretch(label.rectTransform);
                MainMenuButtons.Make(button.rectTransform, button, () =>
                {
                    tab = index;
                    Refresh();
                });
                tabs.Add(button);
            }
            body = UI.Rect(root, "Body");
            UI.Stretch(body);
            MenuChrome.Nav(root, Ui, MainMenuDestination.More);
        }

        public override void Refresh()
        {
            if (!SaveManager.IsLoaded) return;
            chrome.Refresh(Ui);
            for (int i = 0; i < tabs.Count; i++)
            {
                bool on = i == tab;
                tabs[i].color = on ? new Color(0.13f, 0.55f, 1f) : new Color(0.05f, 0.1f, 0.26f, 0.97f);
                tabs[i].GetComponent<Outline>().effectColor = on ? new Color(0.55f, 0.9f, 1f) : new Color(0.25f, 0.4f, 0.75f, 0.7f);
            }
            UI.Clear(body);
            RectTransform area = LegionUI.Place(UI.Rect(body, "Area"), LegionUI.Edge.Top, 0, 0, LegionUI.RefWidth, LegionUI.RefHeight);
            switch (tab)
            {
                case 0: Global(area); break;
                case 3: Season(area); break;
                default: Soon(area, tab == 1 ? "AMIGOS" : "CLÃS"); break;
            }
        }

        // ---------------------------------------------------------------- global

        private void Global(RectTransform parent)
        {
            List<Entry> list = League(out int playerRank);

            Podium(parent, list[1], 2, 52, 560, 310, 800, new Color(0.25f, 0.5f, 0.95f));
            Podium(parent, list[2], 3, 630, 560, 888, 800, new Color(0.85f, 0.42f, 0.16f));
            Podium(parent, list[0], 1, 326, 520, 614, 800, new Color(1f, 0.68f, 0.12f));

            MenuChrome.Panel(parent, Box, 24, 802, 916, 1324, "Table");
            Image head = UI.Panel(parent, new Color(0.03f, 0.07f, 0.18f, 0.9f), "Head");
            LegionUI.Inside(head.rectTransform, Box, 34, 816, 906, 856);
            MenuChrome.Text(parent, Box, 40, 816, 110, 856, "#", 22f, LegionTextStyle.PrimaryButton, Color.white, TextAnchor.MiddleCenter);
            MenuChrome.Text(parent, Box, 228, 816, 420, 856, "Jogador", 22f, LegionTextStyle.PrimaryButton, Color.white);
            MenuChrome.Text(parent, Box, 526, 816, 700, 856, "Clã", 22f, LegionTextStyle.PrimaryButton, Color.white);
            MenuChrome.Art(parent, Box, 780, 820, 812, 852, "Icons/trophy");
            MenuChrome.Text(parent, Box, 818, 816, 902, 856, "Troféus", 22f, LegionTextStyle.PrimaryButton, Color.white);

            for (int i = 3; i < 10 && i < list.Count; i++)
            {
                float y0 = 862 + (i - 3) * 66;
                TableRow(parent, list[i], i + 1, y0, y0 + 60);
            }

            // Linha do jogador (sempre visível, destacada).
            Image mine = UI.Panel(parent, new Color(0.1f, 0.4f, 0.95f), "Player");
            LegionUI.Inside(mine.rectTransform, Box, 24, 1338, 916, 1456);
            Outline glow = mine.gameObject.AddComponent<Outline>();
            glow.effectColor = new Color(0.55f, 0.9f, 1f);
            glow.effectDistance = new Vector2(3f, -3f);
            Entry me = list[playerRank - 1];
            Image rankBox = UI.Panel(parent, new Color(0.05f, 0.18f, 0.5f), "Rank");
            LegionUI.Inside(rankBox.rectTransform, Box, 34, 1352, 112, 1442);
            MenuChrome.Text(parent, Box, 34, 1352, 112, 1442, playerRank.ToString(), 36f, LegionTextStyle.PrimaryButton, Color.white, TextAnchor.MiddleCenter);
            Avatar(parent, "Portrait_King", 126, 1346, 230, 1450);
            MenuChrome.Text(parent, Box, 248, 1360, 460, 1400, me.Name, 30f, LegionTextStyle.PrimaryButton, Color.white);
            MenuChrome.Text(parent, Box, 248, 1398, 460, 1432, me.Sub, 22f, LegionTextStyle.Hud, MenuChrome.GoldText);
            MenuChrome.Art(parent, Box, 464, 1368, 516, 1426, "Icons/shield");
            MenuChrome.Text(parent, Box, 528, 1376, 740, 1420, me.Clan, 24f, LegionTextStyle.Menu, Color.white);
            MenuChrome.Art(parent, Box, 752, 1366, 804, 1420, "Icons/trophy");
            MenuChrome.Text(parent, Box, 806, 1366, 900, 1420, MenuChrome.Format(me.Trophies), 32f, LegionTextStyle.Hud, Color.white);

            MenuChrome.Text(parent, Box, 30, 1458, 910, 1500, "Liga de rivais (offline) · ranking online chega em uma próxima atualização", 16f,
                LegionTextStyle.Body, new Color(0.7f, 0.82f, 1f), TextAnchor.MiddleCenter);
        }

        private void Podium(RectTransform parent, Entry e, int place, float x0, float y0, float x1, float y1, Color color)
        {
            Image card = UI.Panel(parent, color, "Podium" + place);
            LegionUI.Inside(card.rectTransform, Box, x0, y0, x1, y1);
            Outline ring = card.gameObject.AddComponent<Outline>();
            ring.effectColor = Color.Lerp(color, Color.white, 0.5f);
            ring.effectDistance = new Vector2(3f, -3f);
            if (e.Player)
            {
                Outline me = card.gameObject.AddComponent<Outline>();
                me.effectColor = Color.white;
                me.effectDistance = new Vector2(6f, -6f);
            }
            float cx = (x0 + x1) * 0.5f;
            MenuChrome.Art(parent, Box, cx - 54, y0 - 66, cx + 54, y0 + 20, "Icons/crown");
            MenuChrome.Text(parent, Box, cx - 30, y0 - 46, cx + 30, y0 + 6, place.ToString(), 34f, LegionTextStyle.PrimaryButton, Color.white, TextAnchor.MiddleCenter);
            // Do pé do card para cima: troféus, clã e nome; o avatar ocupa o que sobra no alto.
            float avatar = Mathf.Min(106f, y1 - 140 - (y0 + 20));
            Avatar(parent, e.Avatar, cx - avatar * 0.5f, y0 + 20, cx + avatar * 0.5f, y0 + 20 + avatar);
            MenuChrome.Text(parent, Box, x0 + 6, y1 - 134, x1 - 6, y1 - 94, e.Name, 30f, LegionTextStyle.PrimaryButton, Color.white, TextAnchor.MiddleCenter);
            MenuChrome.Art(parent, Box, x0 + 30, y1 - 90, x0 + 64, y1 - 56, "Icons/shield");
            MenuChrome.Text(parent, Box, x0 + 68, y1 - 94, x1 - 10, y1 - 54, e.Clan, 22f, LegionTextStyle.Menu, Color.white);
            MenuChrome.Art(parent, Box, cx - 86, y1 - 52, cx - 40, y1 - 6, "Icons/trophy");
            MenuChrome.Text(parent, Box, cx - 36, y1 - 52, x1 - 6, y1 - 6, MenuChrome.Format(e.Trophies), 32f, LegionTextStyle.Hud, Color.white);
        }

        private void TableRow(RectTransform parent, Entry e, int rank, float y0, float y1)
        {
            Image row = UI.Panel(parent, e.Player ? new Color(0.1f, 0.4f, 0.95f) : new Color(0.06f, 0.13f, 0.33f, 0.95f), "Row");
            LegionUI.Inside(row.rectTransform, Box, 30, y0, 910, y1);
            MenuChrome.Text(parent, Box, 34, y0, 112, y1, rank.ToString(), 30f, LegionTextStyle.PrimaryButton, Color.white, TextAnchor.MiddleCenter);
            Avatar(parent, e.Avatar, 138, y0 + 2, 204, y1 - 2);
            MenuChrome.Text(parent, Box, 228, y0, 460, y1, e.Name, 26f, LegionTextStyle.PrimaryButton, Color.white);
            MenuChrome.Art(parent, Box, 468, y0 + 8, 510, y1 - 8, "Icons/shield");
            MenuChrome.Text(parent, Box, 524, y0, 760, y1, e.Clan, 23f, LegionTextStyle.Menu, Color.white);
            MenuChrome.Art(parent, Box, 772, y0 + 10, 812, y1 - 10, "Icons/trophy");
            MenuChrome.Text(parent, Box, 816, y0, 906, y1, MenuChrome.Format(e.Trophies), 26f, LegionTextStyle.Hud, Color.white);
        }

        private static void Avatar(RectTransform parent, string portrait, float x0, float y0, float x1, float y1)
        {
            Image frame = UI.Panel(parent, new Color(0.9f, 0.7f, 0.25f), "AvatarFrame");
            LegionUI.Inside(frame.rectTransform, Box, x0, y0, x1, y1);
            Image art = HeroesArt.Image(parent, portrait, "Avatar");
            art.preserveAspect = false;
            LegionUI.Inside(art.rectTransform, Box, x0 + 4, y0 + 4, x1 - 4, y1 - 4);
        }

        // ---------------------------------------------------------------- temporada

        private void Season(RectTransform parent)
        {
            SaveData save = SaveManager.Current;
            RecordData r = save.records;
            MenuChrome.Panel(parent, Box, 24, 490, 916, 1456, "Records");
            MenuChrome.Text(parent, Box, 50, 506, 890, 556, "Seus recordes", 34f, LegionTextStyle.PrimaryButton, Color.white);
            (string icon, string label, string value)[] stats =
            {
                ("Icons/trophy", "Troféus", MenuChrome.Format(Trophies())),
                ("Icons/star_badge", "Estrelas", $"{save.progress.TotalStars()}/{LevelCatalog.TotalLevels * 3}"),
                ("Icons/map", "Fases jogadas", MenuChrome.Format(r.levelsPlayed)),
                ("Icons/crown", "Vitórias", MenuChrome.Format(r.victories)),
                ("Hud/hud_skull", "Inimigos derrotados", MenuChrome.Format(r.totalKills)),
                ("Hud/hud_skull", "Chefões derrotados", MenuChrome.Format(r.bossesDefeated)),
                ("Hud/hud_people", "Maior exército", MenuChrome.Format(r.bestArmySize)),
                ("Icons/coin", "Ouro ganho no total", MenuChrome.Format((int)Math.Min(int.MaxValue, r.totalGoldEarned)))
            };
            for (int i = 0; i < stats.Length; i++)
            {
                float y0 = 570 + i * 106;
                Image row = UI.Panel(parent, new Color(0.06f, 0.13f, 0.33f, 0.95f), "Row");
                LegionUI.Inside(row.rectTransform, Box, 40, y0, 900, y0 + 94);
                MenuChrome.Art(parent, Box, 60, y0 + 14, 126, y0 + 80, stats[i].icon);
                MenuChrome.Text(parent, Box, 146, y0, 640, y0 + 94, stats[i].label, 28f, LegionTextStyle.PrimaryButton, Color.white);
                MenuChrome.Text(parent, Box, 600, y0, 880, y0 + 94, stats[i].value, 32f, LegionTextStyle.Hud, MenuChrome.GoldText, TextAnchor.MiddleRight);
            }
        }

        private void Soon(RectTransform parent, string name)
        {
            MenuChrome.Panel(parent, Box, 24, 490, 916, 900, "Soon");
            MenuChrome.Art(parent, Box, 380, 520, 560, 700, "Icons/trophy");
            MenuChrome.Text(parent, Box, 60, 710, 880, 770, "RANKING DE " + name, 36f, LegionTextStyle.PrimaryButton, Color.white, TextAnchor.MiddleCenter);
            MenuChrome.Text(parent, Box, 60, 770, 880, 870, "Chega com o modo online: dispute troféus com seus amigos e clãs.", 24f,
                LegionTextStyle.Menu, new Color(0.8f, 0.88f, 1f), TextAnchor.MiddleCenter);
        }

        // ---------------------------------------------------------------- dados

        // Troféus do jogador: 50 por estrela, 200 por chefão e 10 por vitória.
        private static int Trophies()
        {
            SaveData save = SaveManager.Current;
            return save.progress.TotalStars() * 50 + save.records.bossesDefeated * 200 + save.records.victories * 10;
        }

        private static List<Entry> League(out int playerRank)
        {
            var list = new List<Entry>();
            foreach (var rival in Rivals)
                list.Add(new Entry { Name = rival.name, Clan = rival.clan, Avatar = rival.avatar, Trophies = rival.trophies });
            var me = new Entry
            {
                Name = "Comandante",
                Clan = "Legion Brasil",
                Avatar = "Portrait_King",
                Sub = "Nível " + SaveManager.Current.progress.highestUnlocked,
                Trophies = Trophies(),
                Player = true
            };
            list.Add(me);
            list.Sort((a, b) => b.Trophies != a.Trophies ? b.Trophies.CompareTo(a.Trophies) : a.Player.CompareTo(b.Player) * -1);
            playerRank = list.IndexOf(me) + 1;
            return list;
        }
    }
}
