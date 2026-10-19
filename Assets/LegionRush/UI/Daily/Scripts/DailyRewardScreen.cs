using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LegionRush
{
    // Recompensas diárias (entrega/referencias/diarias_referencia.png e auditoria 1g/1h): calendário de 10 dias (5 + 5) com
    // os estados COLETADO, HOJE (borda dourada pulsando), AMANHÃ e trancado; destaque do dia com COLETAR antes de coletar e
    // "PRÓXIMA RECOMPENSA · DIA N" com a contagem depois; e a sequência de dias com os baús marcantes.
    public class DailyRewardScreen : UIScreen
    {
        private static readonly Vector4 Box = new Vector4(0, 0, LegionUI.RefWidth, LegionUI.RefHeight);
        private static readonly float[] Columns = { 22, 205, 388, 571, 753 };
        private const float RowA = 278f, RowB = 522f, DayH = 232f;
        private const float FeatureTop = 768f, FeatureBottom = 1166f;
        private const float StreakTop = 1180f;

        private MenuChrome chrome;
        private RectTransform body;
        private TMP_Text countdown;

        protected override void Build(RectTransform root)
        {
            chrome = MenuChrome.Build(root, Ui, "RECOMPENSAS DIÁRIAS", OnBack, 680f);
            body = UI.Rect(root, "Body");
            UI.Stretch(body);
            root.gameObject.AddComponent<UITicker>().Tick = Tick;
            MenuChrome.Nav(root, Ui, MainMenuDestination.More);
        }

        public override void Refresh()
        {
            if (!SaveManager.IsLoaded) return;
            chrome.Refresh(Ui);
            UI.Clear(body);
            countdown = null;
            RectTransform top = LegionUI.Place(UI.Rect(body, "Top"), LegionUI.Edge.Top, 0, 0, LegionUI.RefWidth, LegionUI.RefHeight);

            MenuChrome.Text(top, Box, 60, 234, 880, 270, "Entre todos os dias e ganhe recompensas!", 24f, LegionTextStyle.Menu,
                Color.white, TextAnchor.MiddleCenter);

            bool available = MetaService.CanClaimDaily(out int today);
            for (int i = 0; i < MetaCatalog.Daily.Length && i < 10; i++)
            {
                float x0 = Columns[i % 5];
                float y0 = i < 5 ? RowA : RowB;
                Day(top, i, x0, y0, x0 + 168, y0 + DayH, today, available);
            }

            Feature(top, today, available);
            Streak(top, today, available);
        }

        private void Tick()
        {
            if (countdown != null) countdown.text = "em " + MainMenuCountdown.Format(DateTime.UtcNow.Date.AddDays(1) - DateTime.UtcNow);
        }

        // ---------------------------------------------------------------- calendário

        private void Day(RectTransform parent, int index, float x0, float y0, float x1, float y1, int today, bool available)
        {
            bool collected = index < today || (!available && index == today);
            bool isToday = available && index == today;
            bool tomorrow = !collected && !isToday && index == today + (available ? 1 : 1);
            Image tile = MenuChrome.Panel(parent, Box, x0, y0, x1, y1, "Day" + (index + 1));
            if (collected) tile.color = new Color(0.55f, 1f, 0.65f);
            if (isToday)
            {
                // HOJE: borda dourada pulsando.
                Image glow = LegionArt.Image(parent, "Frames/frame_gold", false, "Today");
                glow.type = Image.Type.Sliced;
                glow.fillCenter = false;
                glow.pixelsPerUnitMultiplier = 2.2f;
                LegionUI.Inside(glow.rectTransform, Box, x0 - 6, y0 - 6, x1 + 6, y1 + 6);
                UIGlowPulse pulse = glow.gameObject.AddComponent<UIGlowPulse>();
                pulse.min = 0.45f;
                pulse.max = 1f;
            }

            DailyRewardDef reward = MetaCatalog.Daily[index];
            MenuChrome.Text(parent, Box, x0, y0 + 6, x1, y0 + 46, "DIA " + (index + 1), 24f, LegionTextStyle.PrimaryButton, Color.white, TextAnchor.MiddleCenter);
            Image art = MenuChrome.Art(parent, Box, x0 + 30, y0 + 48, x1 - 30, y0 + 128, ArtOf(reward));
            if (!collected && !isToday && !tomorrow) art.color = new Color(0.6f, 0.62f, 0.7f);
            MenuChrome.Text(parent, Box, x0 + 4, y0 + 128, x1 - 4, y0 + 166, Amount(reward), 26f, LegionTextStyle.Hud, Color.white, TextAnchor.MiddleCenter);

            float bandY = y1 - 54;
            if (collected)
            {
                LegionBadge.Pill(parent, Box, x0 + 8, bandY, x1 - 8, y1 - 10, "✓ COLETADO", MenuChrome.Green);
            }
            else if (isToday)
            {
                TMP_Text label = LegionBadge.Pill(parent, Box, x0 + 8, bandY, x1 - 8, y1 - 10, "HOJE", new Color(0.95f, 0.66f, 0.1f));
                Image pill = label.transform.parent.GetComponent<Image>();
                MainMenuButtons.Make(pill.rectTransform, pill, Claim);
            }
            else if (tomorrow)
            {
                LegionBadge.Pill(parent, Box, x0 + 8, bandY, x1 - 8, y1 - 10, "AMANHÃ", new Color(0.36f, 0.4f, 0.5f));
            }
            else
            {
                MenuChrome.Art(parent, Box, x0 + 62, bandY - 2, x1 - 62, y1 - 8, "Badges/lock");
            }
        }

        // ---------------------------------------------------------------- destaque

        private void Feature(RectTransform parent, int today, bool available)
        {
            int index = available ? today : (today + 1) % MetaCatalog.Daily.Length;
            DailyRewardDef reward = MetaCatalog.Daily[index];

            Image panel = UI.Panel(parent, new Color(0.95f, 0.58f, 0.14f), "Feature");
            LegionUI.Inside(panel.rectTransform, Box, 22, FeatureTop, 918, FeatureBottom);
            Outline frame = panel.gameObject.AddComponent<Outline>();
            frame.effectColor = new Color(1f, 0.85f, 0.4f);
            frame.effectDistance = new Vector2(3f, -3f);
            // Sem o retângulo laranja-claro de bordas duras; o brilho radial vem com a arte 1024 do Prompt 2.
            string ribbonText = available ? "DIA " + (index + 1) : "PRÓXIMA RECOMPENSA · DIA " + (index + 1);
            Image ribbon = UI.Panel(parent, new Color(0.12f, 0.36f, 0.92f), "Ribbon");
            LegionUI.Inside(ribbon.rectTransform, Box, 18, FeatureTop + 4, available ? 236 : 560, FeatureTop + 68);
            MenuChrome.Text(parent, Box, 30, FeatureTop + 4, available ? 230 : 552, FeatureTop + 68, ribbonText, available ? 40f : 28f,
                LegionTextStyle.PrimaryButton, Color.white, TextAnchor.MiddleCenter);

            MenuChrome.Art(parent, Box, 110, FeatureTop + 90, 480, FeatureBottom - 36, BigArtOf(reward));
            MenuChrome.Text(parent, Box, 556, FeatureTop + 80, 910, FeatureTop + 134, Title(reward).ToUpperInvariant(), 38f, LegionTextStyle.PrimaryButton,
                Color.white);
            MenuChrome.Text(parent, Box, 558, FeatureTop + 134, 910, FeatureTop + 166, reward.Chest != null ? "Pode conter:" : "Você recebe:", 24f,
                LegionTextStyle.Menu, Color.white);

            (string icon, string text)[] lines = Contents(reward);
            for (int i = 0; i < lines.Length; i++)
            {
                float y = FeatureTop + 172 + i * 46;
                MenuChrome.Art(parent, Box, 566, y, 608, y + 40, lines[i].icon);
                MenuChrome.Text(parent, Box, 620, y, 910, y + 40, lines[i].text, 24f, LegionTextStyle.Menu, Color.white);
            }

            if (available)
            {
                // COLETAR verde dentro do painel, ícone do presente fora do texto (espaço de 12).
                LegionBuyButton claim = LegionBuyButton.Create(parent, LegionBuyButton.Variant.Green, LegionCurrency.None, 0, "COLETAR", Claim);
                LegionUI.Inside((RectTransform)claim.transform, Box, 600, FeatureBottom - 96, 900, FeatureBottom - 18);
                MenuChrome.Art(parent, Box, 540, FeatureBottom - 92, 588, FeatureBottom - 22, "Rewards/gift_small");
            }
            else
            {
                // Depois de coletar: só a contagem, sem botão cinza por cima da borda.
                MenuChrome.Art(parent, Box, 566, FeatureBottom - 88, 616, FeatureBottom - 30, "Icons/energy");
                countdown = MenuChrome.Text(parent, Box, 628, FeatureBottom - 92, 906, FeatureBottom - 24, string.Empty, 34f, LegionTextStyle.Hud, Color.white);
                Tick();
            }
        }

        // ---------------------------------------------------------------- sequência

        private void Streak(RectTransform parent, int today, bool available)
        {
            int streak = available ? today : today + 1;
            float t = StreakTop;
            MenuChrome.Panel(parent, Box, 22, t, 918, t + 214, "Streak");
            Image calendar = MainMenuArt.Image(parent, "Icon_Events", "Calendar");
            LegionUI.Inside(calendar.rectTransform, Box, 40, t + 12, 116, t + 86);
            MenuChrome.Text(parent, Box, 132, t + 12, 640, t + 50, "Sequência de Dias", 28f, LegionTextStyle.PrimaryButton, Color.white);
            MenuChrome.Text(parent, Box, 132, t + 50, 680, t + 84, "Dias seguidos dão recompensas melhores!", 22f, LegionTextStyle.Body,
                new Color(0.45f, 0.75f, 1f));

            Image badge = UI.Panel(parent, new Color(0.04f, 0.08f, 0.2f, 0.95f), "Count");
            LegionUI.Inside(badge.rectTransform, Box, 684, t + 16, 906, t + 74);
            Outline ring = badge.gameObject.AddComponent<Outline>();
            ring.effectColor = MenuChrome.GoldColor;
            ring.effectDistance = new Vector2(2f, -2f);
            string days = streak == 1 ? "DIA SEGUIDO" : "DIAS SEGUIDOS";
            MenuChrome.Text(parent, Box, 694, t + 16, 900, t + 74, $"<size=140%><color=#FFC83A>{streak}</color></size> {days}", 22f,
                LegionTextStyle.PrimaryButton, Color.white, TextAnchor.MiddleCenter);

            // Trilho com marcos: dias 1, 2, 3, 5, 7 e 10.
            int[] marks = { 1, 2, 3, 5, 7, 10 };
            float[] xs = { 133, 252, 378, 513, 658, 822 };
            float ty = t + 130;
            Image track = UI.Panel(parent, new Color(0.04f, 0.08f, 0.2f, 0.95f), "Track");
            LegionUI.Inside(track.rectTransform, Box, 60, ty, 884, ty + 16);
            float reached = 60f;
            for (int i = 0; i < marks.Length; i++) if (streak >= marks[i]) reached = xs[i];
            Image fill = UI.Panel(parent, MenuChrome.Green, "Fill");
            LegionUI.Inside(fill.rectTransform, Box, 60, ty, Mathf.Max(62f, reached), ty + 16);
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
                    LegionUI.Inside(dot.rectTransform, Box, xs[i] - 20, ty - 12, xs[i] + 20, ty + 28);
                    MenuChrome.Art(parent, Box, xs[i] - 14, ty - 6, xs[i] + 14, ty + 22, "Badges/check");
                }
                else
                {
                    MenuChrome.Art(parent, Box, xs[i] - 36, current ? ty - 36 : ty - 28, xs[i] + 36, current ? ty + 32 : ty + 26, ChestFor(day));
                }
                MenuChrome.Text(parent, Box, xs[i] - 54, ty + 32, xs[i] + 54, ty + 66, "Dia " + day, 22f, LegionTextStyle.Menu,
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
            : r.Gems > 0 ? "Icons/gem" : "Icons/coin";

        // Arte grande: Daily_Featured_* (Prompt 2, 1024) quando existir; senão a arte do pacote.
        private static string BigArtOf(DailyRewardDef r)
        {
            string item = r.Chest != null ? (r.Chest == "epico" ? "chest_epic" : "chest_rare")
                : r.Potion != null ? "potion" : r.Gems > 0 ? "gems" : "coins";
            string fallback = r.Chest != null ? (r.Chest == "epico" ? "Chests/chest_epic" : "Chests/chest_rare")
                : r.Potion != null ? "Icons/potion_red"
                : r.Gems > 0 ? "Rewards/gem_bag" : "Rewards/coin_pile";
            return LegionArt.Pick("Daily/Daily_Featured_" + item, fallback);
        }

        private static string Amount(DailyRewardDef r) =>
            r.Chest != null ? "Baú"
            : r.Potion != null ? "×1"
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
                    ("Icons/coin", $"{LegionFormat.Short(chest.MinGold)}–{LegionFormat.Short(chest.MaxGold)} Moedas"),
                    ("Icons/potion_green", $"{chest.Potions} Poção(ões)"),
                    ("Icons/sword", chest.EquipmentChance >= 1f ? "Equipamento" : $"{Mathf.RoundToInt(chest.EquipmentChance * 100f)}% equipamento")
                };
            }
            if (r.Potion != null) return new[] { ("Icons/potion_red", MetaCatalog.Potion(r.Potion).Effect.Describe()) };
            return r.Gems > 0
                ? new[] { ("Icons/gem", MenuChrome.Format(r.Gems) + " Gemas") }
                : new[] { ("Icons/coin", MenuChrome.Format(r.Gold) + " Moedas") };
        }

        private static string ChestFor(int day) => day >= 10 ? "Chests/chest_red" : day >= 7 ? "Chests/chest_epic" : day >= 5 ? "Chests/chest_blue" : "Chests/chest_rare";
    }
}
