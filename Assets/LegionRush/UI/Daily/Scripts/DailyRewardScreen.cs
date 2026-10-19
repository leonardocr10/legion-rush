using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LegionRush
{
    // Recompensas diárias no layout do mockup: calendário de 10 dias (coletado, hoje, amanhã, bloqueado), destaque da
    // recompensa do dia com o que ela pode conter, botão COLETAR e a sequência de dias com os baús marcantes.
    public class DailyRewardScreen : UIScreen
    {
        private static readonly Vector4 Box = new Vector4(0, 0, LegionUI.RefWidth, LegionUI.RefHeight);
        private static readonly float[] Columns = { 22, 205, 388, 571, 753 };

        private MenuChrome chrome;
        private RectTransform body;

        protected override void Build(RectTransform root)
        {
            chrome = MenuChrome.Build(root, Ui, "RECOMPENSAS DIÁRIAS", OnBack, 680f);
            body = UI.Rect(root, "Body");
            UI.Stretch(body);
            MenuChrome.Nav(root, Ui, MainMenuDestination.More);
        }

        public override void Refresh()
        {
            if (!SaveManager.IsLoaded) return;
            chrome.Refresh(Ui);
            UI.Clear(body);
            RectTransform top = LegionUI.Place(UI.Rect(body, "Top"), LegionUI.Edge.Top, 0, 0, LegionUI.RefWidth, LegionUI.RefHeight);

            MenuChrome.Text(top, Box, 60, 276, 880, 314, "Entre todos os dias e ganhe recompensas incríveis!", 24f, LegionTextStyle.Menu,
                Color.white, TextAnchor.MiddleCenter);

            bool available = MetaService.CanClaimDaily(out int today);
            for (int i = 0; i < MetaCatalog.Daily.Length && i < 10; i++)
            {
                float x0 = Columns[i % 5];
                float y0 = i < 5 ? 325f : 593f;
                Day(top, i, x0, y0, x0 + 168, y0 + (i < 5 ? 253 : 237), today, available);
            }

            Feature(top, today, available);
            Streak(top, today, available);
        }

        // ---------------------------------------------------------------- calendário

        private void Day(RectTransform parent, int index, float x0, float y0, float x1, float y1, int today, bool available)
        {
            bool collected = index < today || (!available && index == today);
            bool isToday = available && index == today;
            bool tomorrow = !isToday && index == (available ? today + 1 : today + 1);
            Image tile = MenuChrome.Panel(parent, Box, x0, y0, x1, y1, "Day" + (index + 1));
            tile.color = collected ? new Color(0.55f, 1f, 0.7f) : isToday ? new Color(1f, 0.92f, 0.6f) : Color.white;
            if (isToday)
            {
                Image glow = LegionArt.Image(parent, "Frames/frame_gold", false, "Today");
                glow.type = Image.Type.Sliced;
                glow.fillCenter = false;
                glow.pixelsPerUnitMultiplier = 2.2f;
                LegionUI.Inside(glow.rectTransform, Box, x0 - 6, y0 - 6, x1 + 6, y1 + 6);
            }

            DailyRewardDef reward = MetaCatalog.Daily[index];
            MenuChrome.Text(parent, Box, x0, y0 + 8, x1, y0 + 50, "DIA " + (index + 1), 24f, LegionTextStyle.PrimaryButton, Color.white, TextAnchor.MiddleCenter);
            MenuChrome.Art(parent, Box, x0 + 30, y0 + 52, x1 - 30, y0 + 140, ArtOf(reward));
            MenuChrome.Text(parent, Box, x0 + 4, y0 + 138, x1 - 4, y0 + 172, Amount(reward), 22f, LegionTextStyle.Hud, Color.white, TextAnchor.MiddleCenter);

            float statusY = y1 - 50;
            if (collected)
            {
                MenuChrome.Art(parent, Box, x0 + 68, y0 + 170, x1 - 68, y0 + 200, "Badges/check");
                MenuChrome.Button(parent, Box, x0 + 8, statusY, x1 - 8, y1 - 10, "COLETADO", MenuChrome.Green, null, 20f);
            }
            else if (isToday)
            {
                MenuChrome.Button(parent, Box, x0 + 8, statusY, x1 - 8, y1 - 10, "HOJE", MenuChrome.GoldColor, Claim, 22f);
            }
            else
            {
                MenuChrome.Art(parent, Box, x0 + 66, y0 + 174, x1 - 66, y0 + 208, "Badges/lock");
                if (tomorrow) MenuChrome.Button(parent, Box, x0 + 8, statusY, x1 - 8, y1 - 10, "AMANHÃ", MenuChrome.Gray, null, 20f);
            }
        }

        // ---------------------------------------------------------------- destaque

        private void Feature(RectTransform parent, int today, bool available)
        {
            int index = available ? today : (today + 1) % MetaCatalog.Daily.Length;
            DailyRewardDef reward = MetaCatalog.Daily[index];

            Image panel = UI.Panel(parent, new Color(0.95f, 0.58f, 0.14f), "Feature");
            LegionUI.Inside(panel.rectTransform, Box, 22, 848, 918, 1248);
            Outline frame = panel.gameObject.AddComponent<Outline>();
            frame.effectColor = new Color(1f, 0.85f, 0.4f);
            frame.effectDistance = new Vector2(3f, -3f);
            Image shine = UI.Panel(parent, new Color(1f, 0.85f, 0.4f, 0.45f), "Shine");
            LegionUI.Inside(shine.rectTransform, Box, 60, 880, 520, 1210);

            Image ribbon = UI.Panel(parent, new Color(0.12f, 0.36f, 0.92f), "Ribbon");
            LegionUI.Inside(ribbon.rectTransform, Box, 18, 852, 236, 932);
            MenuChrome.Text(parent, Box, 30, 852, 230, 932, "DIA " + (index + 1), 44f, LegionTextStyle.PrimaryButton, Color.white, TextAnchor.MiddleCenter);

            MenuChrome.Art(parent, Box, 60, 900, 540, 1196, BigArtOf(reward));
            MenuChrome.Text(parent, Box, 566, 862, 910, 920, Title(reward).ToUpperInvariant(), 42f, LegionTextStyle.PrimaryButton, Color.white);
            MenuChrome.Text(parent, Box, 568, 918, 910, 952, reward.Chest != null ? "Pode conter:" : "Você recebe:", 24f, LegionTextStyle.Menu, Color.white);

            (string icon, string text)[] lines = Contents(reward);
            for (int i = 0; i < lines.Length; i++)
            {
                float y = 962 + i * 46;
                MenuChrome.Art(parent, Box, 584, y, 626, y + 40, lines[i].icon);
                MenuChrome.Text(parent, Box, 642, y, 910, y + 40, lines[i].text, 24f, LegionTextStyle.Menu, Color.white);
            }

            Image button = MenuChrome.Button(parent, Box, 232, 1188, 708, 1278, available ? "COLETAR" : "VOLTE AMANHÃ",
                available ? MenuChrome.Green : MenuChrome.Gray, available ? (System.Action)Claim : null, 46f);
            MenuChrome.Art(button.transform, new Vector4(232, 1188, 708, 1278), 262, 1200, 330, 1266, "Rewards/gift_small");
        }

        // ---------------------------------------------------------------- sequência

        private void Streak(RectTransform parent, int today, bool available)
        {
            int streak = available ? today : today + 1;
            MenuChrome.Panel(parent, Box, 22, 1278, 918, 1482, "Streak");
            MenuChrome.Art(parent, Box, 40, 1290, 116, 1366, "Icons/map");
            MenuChrome.Text(parent, Box, 136, 1292, 640, 1330, "Sequência de Dias", 26f, LegionTextStyle.PrimaryButton, Color.white);
            MenuChrome.Text(parent, Box, 136, 1328, 680, 1360, "Quanto mais dias seguidos, melhores as recompensas!", 19f, LegionTextStyle.Body,
                new Color(0.45f, 0.75f, 1f));

            Image badge = UI.Panel(parent, new Color(0.04f, 0.08f, 0.2f, 0.95f), "Count");
            LegionUI.Inside(badge.rectTransform, Box, 684, 1294, 906, 1352);
            Outline ring = badge.gameObject.AddComponent<Outline>();
            ring.effectColor = MenuChrome.GoldColor;
            ring.effectDistance = new Vector2(2f, -2f);
            MenuChrome.Text(parent, Box, 700, 1294, 900, 1352, $"<size=150%><color=#FFC83A>{streak}</color></size>  DIAS SEGUIDOS", 20f,
                LegionTextStyle.PrimaryButton, Color.white, TextAnchor.MiddleCenter);

            // Trilho com marcos: dias 1, 2, 3, 5, 7 e 10.
            int[] marks = { 1, 2, 3, 5, 7, 10 };
            float[] xs = { 133, 252, 378, 513, 658, 822 };
            Image track = UI.Panel(parent, new Color(0.04f, 0.08f, 0.2f, 0.95f), "Track");
            LegionUI.Inside(track.rectTransform, Box, 60, 1408, 884, 1424);
            float reached = 60f;
            for (int i = 0; i < marks.Length; i++) if (streak >= marks[i]) reached = xs[i];
            Image fill = UI.Panel(parent, MenuChrome.Green, "Fill");
            LegionUI.Inside(fill.rectTransform, Box, 60, 1408, Mathf.Max(62f, reached), 1424);
            for (int i = 0; i < marks.Length; i++)
            {
                int day = marks[i];
                bool done = streak > day || (streak == day && !available);
                bool current = streak == day && available;
                if (done)
                {
                    Image dot = UI.Panel(parent, MenuChrome.Green, "Done");
                    dot.sprite = UIStyle.Circle;
                    dot.type = Image.Type.Simple;
                    LegionUI.Inside(dot.rectTransform, Box, xs[i] - 20, 1396, xs[i] + 20, 1436);
                    MenuChrome.Art(parent, Box, xs[i] - 14, 1402, xs[i] + 14, 1430, "Badges/check");
                }
                else
                {
                    MenuChrome.Art(parent, Box, xs[i] - 36, current ? 1372 : 1380, xs[i] + 36, current ? 1440 : 1434, ChestFor(day));
                }
                MenuChrome.Text(parent, Box, xs[i] - 50, 1440, xs[i] + 50, 1470, "Dia " + day, 19f, LegionTextStyle.Menu,
                    current ? Color.white : new Color(0.75f, 0.85f, 1f), TextAnchor.MiddleCenter);
            }
        }

        // ---------------------------------------------------------------- dados

        private void Claim()
        {
            RewardBundle reward = MetaService.ClaimDaily();
            if (reward == null) return;
            Ui.Push<RewardScreen>().Show("RECOMPENSA DIÁRIA", reward);
        }

        private static string ArtOf(DailyRewardDef r) =>
            r.Chest != null ? (r.Chest == "epico" ? "Chests/chest_epic" : "Chests/chest_rare")
            : r.Potion != null ? "Icons/potion_red"
            : r.Gems > 0 ? "Icons/gem" : "Rewards/coin_pile";

        private static string BigArtOf(DailyRewardDef r) =>
            r.Chest != null ? (r.Chest == "epico" ? "Chests/chest_epic" : "Chests/chest_rare")
            : r.Potion != null ? "Icons/potion_red"
            : r.Gems > 0 ? "Rewards/gem_bag" : "Rewards/coin_pile";

        private static string Amount(DailyRewardDef r) =>
            r.Chest != null ? "Baú " + MetaCatalog.Chest(r.Chest).Name
            : r.Potion != null ? "Poção"
            : r.Gems > 0 ? MenuChrome.Format(r.Gems) : MenuChrome.Format(r.Gold);

        private static string Title(DailyRewardDef r) =>
            r.Chest != null ? "Baú " + MetaCatalog.Chest(r.Chest).Name
            : r.Potion != null ? MetaCatalog.Potion(r.Potion).Name
            : r.Gems > 0 ? MenuChrome.Format(r.Gems) + " Gemas" : MenuChrome.Format(r.Gold) + " Moedas";

        private static (string, string)[] Contents(DailyRewardDef r)
        {
            if (r.Chest != null)
            {
                ChestDef chest = MetaCatalog.Chest(r.Chest);
                return new[]
                {
                    ("Icons/coin", $"{MenuChrome.Format(chest.MinGold)}–{MenuChrome.Format(chest.MaxGold)} Moedas"),
                    ("Icons/potion_green", $"{chest.Potions} Poção(ões)"),
                    ("Icons/sword", chest.EquipmentChance >= 1f ? "Equipamento garantido" : $"{Mathf.RoundToInt(chest.EquipmentChance * 100f)}% de equipamento")
                };
            }
            if (r.Potion != null) return new[] { ("Icons/potion_red", MetaCatalog.Potion(r.Potion).Effect.Describe() + " na próxima fase") };
            return r.Gems > 0
                ? new[] { ("Icons/gem", MenuChrome.Format(r.Gems) + " Gemas") }
                : new[] { ("Icons/coin", MenuChrome.Format(r.Gold) + " Moedas") };
        }

        private static string ChestFor(int day) => day >= 10 ? "Chests/chest_red" : day >= 7 ? "Chests/chest_epic" : day >= 5 ? "Chests/chest_blue" : "Chests/chest_rare";
    }
}
