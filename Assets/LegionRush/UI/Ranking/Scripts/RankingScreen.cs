using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LegionRush
{
    // Ranking (auditoria 1i): cabeçalho padrão, abas GLOBAL e TEMPORADA (EM BREVE), selo "LIGA DE RIVAIS · OFFLINE",
    // pódio com molduras ouro/prata/bronze (o 1º mais alto), tabela sem a coluna Clã e a linha do jogador.
    // O jogo ainda não tem servidor: GLOBAL é uma liga de rivais simulados onde o jogador entra pelos troféus das fases.
    public class RankingScreen : UIScreen
    {
        private static readonly Vector4 Box = new Vector4(0, 0, LegionUI.RefWidth, LegionUI.RefHeight);
        private static readonly string[] TabNames = { "GLOBAL", "TEMPORADA" };

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
        private LegionTabs tabs;

        private class Entry
        {
            public string Name, Clan, Avatar, Sub;
            public int Trophies;
            public bool Player;
        }

        protected override void Build(RectTransform root)
        {
            // Cabeçalho padrão: a arte do rei que invadia a faixa saiu.
            chrome = MenuChrome.Build(root, Ui, "RANKING", OnBack);
            RectTransform tabArea = LegionUI.Place(UI.Rect(root, "Tabs"), LegionUI.Edge.Top, 120, 246, 820, 312);
            tabs = LegionTabs.Create(tabArea, TabNames, null, 28f, 16f);
            tabs.Select(0);
            tabs.SetLocked(1, true);
            body = UI.Rect(root, "Body");
            UI.Stretch(body);
            MenuChrome.Nav(root, Ui, MainMenuDestination.More);
        }

        public override void Refresh()
        {
            if (!SaveManager.IsLoaded) return;
            chrome.Refresh(Ui);
            UI.Clear(body);
            RectTransform area = LegionUI.Place(UI.Rect(body, "Area"), LegionUI.Edge.Top, 0, 0, LegionUI.RefWidth, LegionUI.RefHeight);
            Global(area);
        }

        // ---------------------------------------------------------------- global

        private void Global(RectTransform parent)
        {
            List<Entry> list = League(out int playerRank);
            LegionBadge.Pill(parent, Box, 250, 318, 690, 352, "LIGA DE RIVAIS · OFFLINE", new Color(0.06f, 0.14f, 0.38f, 0.95f), 22f);

            // Pódio: 1º no meio e mais alto; molduras ouro, prata e bronze.
            Podium(parent, list[1], 2, 40, 480, 300, 700, "Rankings/rank_silver");
            Podium(parent, list[2], 3, 640, 496, 900, 700, "Rankings/rank_bronze");
            Podium(parent, list[0], 1, 320, 456, 620, 700, "Rankings/rank_gold");

            // Tabela 24 abaixo do pódio, sem a coluna Clã; textos em 22+.
            const float tableTop = 724f;
            MenuChrome.Panel(parent, Box, 24, tableTop, 916, tableTop + 556, "Table");
            Image head = UI.Panel(parent, new Color(0.03f, 0.07f, 0.18f, 0.9f), "Head");
            LegionUI.Inside(head.rectTransform, Box, 34, tableTop + 12, 906, tableTop + 54);
            MenuChrome.Text(parent, Box, 40, tableTop + 12, 110, tableTop + 54, "#", 22f, LegionTextStyle.PrimaryButton, Color.white, TextAnchor.MiddleCenter);
            MenuChrome.Text(parent, Box, 228, tableTop + 12, 600, tableTop + 54, "Jogador", 22f, LegionTextStyle.PrimaryButton, Color.white);
            MenuChrome.Art(parent, Box, 744, tableTop + 16, 778, tableTop + 50, "Icons/trophy");
            MenuChrome.Text(parent, Box, 784, tableTop + 12, 902, tableTop + 54, "Troféus", 22f, LegionTextStyle.PrimaryButton, Color.white);
            for (int i = 3; i < 10 && i < list.Count; i++)
            {
                float y0 = tableTop + 62 + (i - 3) * 68;
                TableRow(parent, list[i], i + 1, y0, y0 + 62);
            }

            // Linha do jogador: posição real na liga, ou "Sem classificação" enquanto não tem troféus.
            Entry me = list[playerRank - 1];
            float py = tableTop + 572;
            Image mine = UI.Panel(parent, new Color(0.1f, 0.4f, 0.95f), "Player");
            LegionUI.Inside(mine.rectTransform, Box, 24, py, 916, py + 110);
            Outline glow = mine.gameObject.AddComponent<Outline>();
            glow.effectColor = new Color(0.55f, 0.9f, 1f);
            glow.effectDistance = new Vector2(3f, -3f);
            bool ranked = me.Trophies > 0;
            Image rankBox = UI.Panel(parent, new Color(0.05f, 0.18f, 0.5f), "Rank");
            LegionUI.Inside(rankBox.rectTransform, Box, 34, py + 12, 112, py + 98);
            MenuChrome.Text(parent, Box, 34, py + 12, 112, py + 98, ranked ? playerRank.ToString() : "–", 36f, LegionTextStyle.PrimaryButton, Color.white,
                TextAnchor.MiddleCenter);
            Avatar(parent, "Portrait_King", 126, py + 6, 226, py + 104);
            MenuChrome.Text(parent, Box, 244, py + 10, 700, py + 56, me.Name, 30f, LegionTextStyle.PrimaryButton, Color.white);
            MenuChrome.Text(parent, Box, 244, py + 54, 740, py + 98, ranked ? me.Sub : "Sem classificação · vença uma fase", 22f, LegionTextStyle.Hud,
                MenuChrome.GoldText);
            MenuChrome.Art(parent, Box, 744, py + 28, 794, py + 82, "Icons/trophy");
            MenuChrome.Text(parent, Box, 800, py + 26, 904, py + 84, MenuChrome.Format(me.Trophies), 32f, LegionTextStyle.Hud, Color.white);
        }

        private void Podium(RectTransform parent, Entry e, int place, float x0, float y0, float x1, float y1, string emblem)
        {
            Image card = MenuChrome.Panel(parent, Box, x0, y0, x1, y1, "Podium" + place);
            if (e.Player)
            {
                Outline me = card.gameObject.AddComponent<Outline>();
                me.effectColor = Color.white;
                me.effectDistance = new Vector2(4f, -4f);
            }
            float cx = (x0 + x1) * 0.5f;
            // Moldura de rank (Atlas › Ranks) no alto, com a posição; o 1º leva a coroa grande.
            float size = place == 1 ? 96f : 80f;
            if (place == 1) MenuChrome.Art(parent, Box, cx - 56, y0 - 98, cx + 56, y0 - 44, "Icons/crown");
            MenuChrome.Art(parent, Box, cx - size * 0.5f, y0 - size * 0.62f, cx + size * 0.5f, y0 + size * 0.62f, emblem);
            MenuChrome.Text(parent, Box, cx - 30, y0 - 30, cx + 30, y0 + 24, place.ToString(), 34f, LegionTextStyle.PrimaryButton, Color.white,
                TextAnchor.MiddleCenter);
            float top = y0 + size * 0.45f + 4;
            float avatar = Mathf.Min(104f, y1 - 96 - top);
            Avatar(parent, e.Avatar, cx - avatar * 0.5f, top, cx + avatar * 0.5f, top + avatar);
            MenuChrome.Text(parent, Box, x0 + 8, y1 - 94, x1 - 8, y1 - 52, e.Name, 28f, LegionTextStyle.PrimaryButton, Color.white, TextAnchor.MiddleCenter);
            MenuChrome.Art(parent, Box, cx - 80, y1 - 50, cx - 38, y1 - 8, "Icons/trophy");
            MenuChrome.Text(parent, Box, cx - 32, y1 - 50, x1 - 8, y1 - 8, MenuChrome.Format(e.Trophies), 30f, LegionTextStyle.Hud, Color.white);
        }

        private void TableRow(RectTransform parent, Entry e, int rank, float y0, float y1)
        {
            Image row = UI.Panel(parent, e.Player ? new Color(0.1f, 0.4f, 0.95f) : new Color(0.06f, 0.13f, 0.33f, 0.95f), "Row");
            LegionUI.Inside(row.rectTransform, Box, 30, y0, 910, y1);
            MenuChrome.Text(parent, Box, 34, y0, 112, y1, rank.ToString(), 30f, LegionTextStyle.PrimaryButton, Color.white, TextAnchor.MiddleCenter);
            Avatar(parent, e.Avatar, 138, y0 + 2, 204, y1 - 2);
            MenuChrome.Text(parent, Box, 228, y0, 700, y1, e.Name, 26f, LegionTextStyle.PrimaryButton, Color.white);
            MenuChrome.Art(parent, Box, 744, y0 + 12, 784, y1 - 12, "Icons/trophy");
            MenuChrome.Text(parent, Box, 790, y0, 906, y1, MenuChrome.Format(e.Trophies), 26f, LegionTextStyle.Hud, Color.white);
        }

        private static void Avatar(RectTransform parent, string portrait, float x0, float y0, float x1, float y1)
        {
            Image frame = UI.Panel(parent, new Color(0.9f, 0.7f, 0.25f), "AvatarFrame");
            LegionUI.Inside(frame.rectTransform, Box, x0, y0, x1, y1);
            Image art = HeroesArt.Image(parent, portrait, "Avatar");
            art.preserveAspect = false;
            LegionUI.Inside(art.rectTransform, Box, x0 + 4, y0 + 4, x1 - 4, y1 - 4);
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
                Name = PlayerNamePrompt.NameOf(SaveManager.Current),
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
