using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LegionRush
{
    // Missões no layout do mockup: abas DIÁRIAS/SEMANAIS/CONQUISTAS, painel de progresso com a recompensa final,
    // lista de missões (ícone, descrição, barra, prêmio, IR/COLETAR) e a Caixa de Missões com os marcos.
    // Missões e conquistas são as do jogo (MetaCatalog + MetaService); a caixa paga um bônus quando todas as
    // missões do período foram coletadas (uma vez por período).
    public class MissionsScreen : UIScreen
    {
        private static readonly Vector4 Box = new Vector4(0, 0, LegionUI.RefWidth, LegionUI.RefHeight);
        private static readonly string[] TabNames = { "DIÁRIAS", "SEMANAIS", "CONQUISTAS" };
        private const float ListTop = 498f;
        private const float ListBottom = 1342f;
        private const float RowStep = 134f;

        private MenuChrome chrome;
        private RectTransform body;
        private LegionTabs tabs;
        private int tab;

        protected virtual int StartTab => 0;

        private class Row
        {
            public string Icon, Title, Description;
            public int Value, Target, Gold, Gems;
            public bool Claimed;
            public Func<bool> Claim;
        }

        protected override void Build(RectTransform root)
        {
            tab = StartTab;
            chrome = MenuChrome.Build(root, Ui, TitleText, OnBack, 560f);
            // Abas LegionTabs logo abaixo do cabeçalho, com selo "!" na aba que tem algo para resgatar.
            RectTransform tabArea = LegionUI.Place(UI.Rect(root, "Tabs"), LegionUI.Edge.Top, 28, 246, 914, 312);
            tabs = LegionTabs.Create(tabArea, TabNames, i =>
            {
                tab = i;
                Refresh();
            }, 28f, 12f);
            body = UI.Rect(root, "Body");
            UI.Stretch(body);
            MenuChrome.Nav(root, Ui, MainMenuDestination.More);
        }

        protected virtual string TitleText => "MISSÕES";

        public override void Refresh()
        {
            if (!SaveManager.IsLoaded) return;
            chrome.Refresh(Ui);
            tabs.Select(tab);
            for (int i = 0; i < TabNames.Length; i++) tabs.SetAlert(i, Rows(i).Exists(r => !r.Claimed && r.Value >= r.Target));

            UI.Clear(body);
            RectTransform area = LegionUI.Place(UI.Rect(body, "Area"), LegionUI.Edge.Top, 0, 0, LegionUI.RefWidth, LegionUI.RefHeight);
            List<Row> rows = Rows(tab);
            int done = rows.FindAll(r => r.Claimed).Count;
            Header(area, rows, done);
            List(area, rows);
            if (tab < 2) MissionBox(area, rows, done);
        }

        // ---------------------------------------------------------------- topo

        private void Header(RectTransform parent, List<Row> rows, int done)
        {
            MenuChrome.Panel(parent, Box, 26, 324, 914, 486, "Progress");
            string title = tab == 0 ? "Progresso Diário" : tab == 1 ? "Progresso Semanal" : "Conquistas";
            string subtitle = tab == 2 ? "Conclua desafios e ganhe gemas!" : "Complete as missões e ganhe a recompensa final!";
            MenuChrome.Text(parent, Box, 48, 334, 540, 380, title, 34f, LegionTextStyle.PrimaryButton, Color.white);
            MenuChrome.Text(parent, Box, 48, 380, 560, 414, subtitle, 22f, LegionTextStyle.Body, Color.white);
            Bar(parent, 50, 424, 520, 470, done, rows.Count, new Color(0.13f, 0.55f, 1f), 28f);
            MenuChrome.Art(parent, Box, 536, 340, 696, 480, "Chests/chest_epic");

            // Recompensa final: o prêmio (moeda, gema e item), não o total já ganho.
            Image final = UI.Panel(parent, new Color(0.03f, 0.07f, 0.18f, 0.9f), "Final");
            LegionUI.Inside(final.rectTransform, Box, 704, 336, 904, 476);
            MenuChrome.Text(parent, Box, 710, 340, 898, 376, "Recompensa Final", 22f, LegionTextStyle.PrimaryButton, Color.white, TextAnchor.MiddleCenter);
            (int gold, int gems) box = tab == 2 ? (0, NextAchievementGems()) : BoxReward();
            float x = 716;
            if (box.gold > 0)
            {
                MenuChrome.Art(parent, Box, x, 380, x + 52, 428, "Icons/coin");
                MenuChrome.Text(parent, Box, x - 10, 428, x + 62, 466, MenuChrome.Format(box.gold), 22f, LegionTextStyle.Hud, Color.white, TextAnchor.MiddleCenter);
                x += 64;
            }
            MenuChrome.Art(parent, Box, x, 380, x + 52, 428, "Icons/gem");
            MenuChrome.Text(parent, Box, x - 10, 428, x + 62, 466, MenuChrome.Format(box.gems), 22f, LegionTextStyle.Hud, Color.white, TextAnchor.MiddleCenter);
            x += 64;
            if (tab < 2)
            {
                MenuChrome.Art(parent, Box, x, 380, x + 52, 428, "Icons/potion_red");
                MenuChrome.Text(parent, Box, x - 10, 428, x + 62, 466, "1", 22f, LegionTextStyle.Hud, Color.white, TextAnchor.MiddleCenter);
            }
        }

        // Conquistas: gemas da próxima conquista ainda não resgatada.
        private static int NextAchievementGems()
        {
            foreach (AchievementDef def in MetaCatalog.Achievements)
                if (!MetaService.IsAchievementClaimed(def)) return def.RewardGems;
            return 0;
        }

        private void Bar(RectTransform parent, float x0, float y0, float x1, float y1, int value, int max, Color color, float size)
        {
            Image track = UI.Panel(parent, new Color(0.03f, 0.07f, 0.18f, 0.95f), "Track");
            LegionUI.Inside(track.rectTransform, Box, x0, y0, x1, y1);
            float ratio = max > 0 ? Mathf.Clamp01(value / (float)max) : 0f;
            if (ratio > 0f)
            {
                Image fill = UI.Panel(parent, color, "Fill");
                LegionUI.Inside(fill.rectTransform, Box, x0 + 3, y0 + 3, Mathf.Lerp(x0 + 14, x1 - 3, ratio), y1 - 3);
            }
            MenuChrome.Text(parent, Box, x0, y0, x1, y1, $"{MenuChrome.Format(Mathf.Min(value, max))}/{MenuChrome.Format(max)}", size,
                LegionTextStyle.Hud, Color.white, TextAnchor.MiddleCenter);
        }

        // ---------------------------------------------------------------- lista

        private void List(RectTransform parent, List<Row> rows)
        {
            RectTransform view = LegionUI.Inside(UI.Rect(parent, "View"), Box, 0, ListTop, LegionUI.RefWidth, ListBottom);
            view.gameObject.AddComponent<RectMask2D>();
            Image hit = view.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            ScrollRect scroll = view.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;

            float height = Mathf.Max(ListBottom - ListTop, rows.Count * RowStep);
            RectTransform content = UI.Rect(view, "Content");
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = new Vector2(0f, height * LegionUI.Px);
            scroll.content = content;
            var box = new Vector4(0, ListTop, LegionUI.RefWidth, ListTop + height);

            for (int i = 0; i < rows.Count; i++) RowView(content, box, rows[i], ListTop + 2 + i * RowStep);
        }

        private void RowView(RectTransform parent, Vector4 box, Row row, float y0)
        {
            float y1 = y0 + 120;
            MenuChrome.Panel(parent, box, 26, y0, 914, y1, "Row");
            Icon(parent, box, row.Icon, 34, y0 + 10, 156, y1 - 10);
            MenuChrome.Text(parent, box, 172, y0 + 6, 560, y0 + 44, row.Title, 28f, LegionTextStyle.PrimaryButton, Color.white);
            MenuChrome.Text(parent, box, 172, y0 + 44, 570, y0 + 76, row.Description, 22f, LegionTextStyle.Body, new Color(0.75f, 0.88f, 1f));

            bool complete = row.Value >= row.Target;
            Image track = UI.Panel(parent, new Color(0.03f, 0.07f, 0.18f, 0.95f), "Track");
            LegionUI.Inside(track.rectTransform, box, 172, y0 + 78, 540, y0 + 114);
            float ratio = row.Target > 0 ? Mathf.Clamp01(row.Value / (float)row.Target) : 0f;
            if (ratio > 0f)
            {
                Image fill = UI.Panel(parent, complete ? MenuChrome.Green : new Color(0.13f, 0.55f, 1f), "Fill");
                LegionUI.Inside(fill.rectTransform, box, 174, y0 + 80, Mathf.Lerp(186, 538, ratio), y0 + 112);
            }
            MenuChrome.Text(parent, box, 172, y0 + 76, 540, y0 + 116, $"{MenuChrome.Format(Mathf.Min(row.Value, row.Target))}/{MenuChrome.Format(row.Target)}",
                24f, LegionTextStyle.Hud, Color.white, TextAnchor.MiddleCenter);

            Image rewards = UI.Panel(parent, new Color(0.03f, 0.07f, 0.18f, 0.6f), "Rewards");
            LegionUI.Inside(rewards.rectTransform, box, 570, y0 + 8, 740, y1 - 8);
            float ry = row.Gold > 0 && row.Gems > 0 ? y0 + 14 : y0 + 38;
            if (row.Gold > 0)
            {
                MenuChrome.Art(parent, box, 598, ry, 642, ry + 44, "Icons/coin");
                MenuChrome.Text(parent, box, 650, ry, 738, ry + 44, MenuChrome.Format(row.Gold), 24f, LegionTextStyle.Hud, Color.white);
                ry += 50;
            }
            if (row.Gems > 0)
            {
                MenuChrome.Art(parent, box, 598, ry, 642, ry + 44, "Icons/gem");
                MenuChrome.Text(parent, box, 650, ry, 738, ry + 44, MenuChrome.Format(row.Gems), 24f, LegionTextStyle.Hud, Color.white);
            }

            // RESGATAR (verde, completa) · IR (azul, abre a fase) · ✓ (selo escuro, já resgatada).
            LegionBuyButton button;
            if (row.Claimed)
            {
                button = LegionBuyButton.Create(parent, LegionBuyButton.Variant.Green, LegionCurrency.None, 0, string.Empty, null);
                button.SetBought(true, "FEITO");
            }
            else if (complete)
            {
                button = LegionBuyButton.Create(parent, LegionBuyButton.Variant.Green, LegionCurrency.None, 0, "RESGATAR", () =>
                {
                    if (row.Claim())
                    {
                        GameAudio.Play(SoundId.CoinPickup);
                        Ui.Toast(RewardText(row.Gold, row.Gems));
                    }
                    Ui.RefreshTop();
                });
            }
            else
            {
                button = LegionBuyButton.Create(parent, LegionBuyButton.Variant.Blue, LegionCurrency.None, 0, "IR", () =>
                {
                    Ui.Replace<MainMenuScreen>();
                    new UIRootMainMenuNavigator(Ui).Open(MainMenuDestination.Play);
                });
            }
            LegionUI.Inside((RectTransform)button.transform, box, 750, y0 + 26, 900, y1 - 26);
        }

        private static void Icon(RectTransform parent, Vector4 box, string path, float x0, float y0, float x1, float y1)
        {
            if (path.StartsWith("Settings:"))
            {
                Image image = SettingsArt.Image(parent, path.Substring(9));
                LegionUI.Inside(image.rectTransform, box, x0, y0, x1, y1);
                return;
            }
            MenuChrome.Art(parent, box, x0, y0, x1, y1, path);
        }

        // ---------------------------------------------------------------- caixa

        private void MissionBox(RectTransform parent, List<Row> rows, int done)
        {
            Image panel = UI.Panel(parent, new Color(0.95f, 0.6f, 0.12f), "MissionBox");
            LegionUI.Inside(panel.rectTransform, Box, 26, 1352, 914, 1502);
            Outline frame = panel.gameObject.AddComponent<Outline>();
            frame.effectColor = new Color(1f, 0.85f, 0.4f);
            frame.effectDistance = new Vector2(3f, -3f);
            MenuChrome.Text(parent, Box, 48, 1360, 560, 1400, "Caixa de Missões", 32f, LegionTextStyle.PrimaryButton, Color.white);
            MenuChrome.Text(parent, Box, 48, 1398, 600, 1428, $"Complete todas as missões {(tab == 0 ? "diárias" : "semanais")}", 22f,
                LegionTextStyle.Menu, new Color(1f, 0.95f, 0.6f));

            // Marcos: um por missão, preenchidos conforme as coletadas.
            float x0 = 56, x1 = 580;
            Image track = UI.Panel(parent, new Color(0.04f, 0.08f, 0.2f, 0.95f), "Track");
            LegionUI.Inside(track.rectTransform, Box, x0, 1446, x1, 1462);
            float step = (x1 - x0) / Mathf.Max(1, rows.Count);
            if (done > 0)
            {
                Image fill = UI.Panel(parent, MenuChrome.Green, "Fill");
                LegionUI.Inside(fill.rectTransform, Box, x0, 1446, x0 + step * done, 1462);
            }
            for (int i = 1; i <= rows.Count; i++)
            {
                float cx = x0 + step * i - 12;
                Image dot = UI.Panel(parent, i <= done ? MenuChrome.Green : new Color(0.08f, 0.14f, 0.32f), "Mark");
                dot.sprite = UIStyle.Circle;
                dot.type = Image.Type.Simple;
                LegionUI.Inside(dot.rectTransform, Box, cx - 16, 1438, cx + 16, 1470);
                if (i <= done) MenuChrome.Art(parent, Box, cx - 12, 1442, cx + 12, 1466, "Badges/check");
            }
            MenuChrome.Text(parent, Box, 300, 1466, 460, 1500, $"{done}/{rows.Count}", 22f, LegionTextStyle.Hud, Color.white, TextAnchor.MiddleCenter);

            MenuChrome.Art(parent, Box, 600, 1356, 760, 1500, "Chests/chest_epic");
            Image rewards = UI.Panel(parent, new Color(0.03f, 0.07f, 0.18f, 0.7f), "Rewards");
            LegionUI.Inside(rewards.rectTransform, Box, 772, 1362, 904, 1494);
            (int gold, int gems) reward = BoxReward();
            MenuChrome.Art(parent, Box, 784, 1370, 818, 1404, "Icons/coin");
            MenuChrome.Text(parent, Box, 824, 1370, 902, 1404, MenuChrome.Format(reward.gold), 22f, LegionTextStyle.Hud, Color.white);
            MenuChrome.Art(parent, Box, 784, 1412, 818, 1446, "Icons/gem");
            MenuChrome.Text(parent, Box, 824, 1412, 902, 1446, MenuChrome.Format(reward.gems), 22f, LegionTextStyle.Hud, Color.white);
            MenuChrome.Art(parent, Box, 784, 1454, 818, 1488, "Icons/potion_red");
            MenuChrome.Text(parent, Box, 824, 1454, 902, 1488, "1", 22f, LegionTextStyle.Hud, Color.white);

            bool claimed = PlayerPrefs.GetInt(BoxKey(), 0) == 1;
            bool ready = rows.Count > 0 && done >= rows.Count && !claimed;
            if (claimed || ready)
            {
                LegionBuyButton open = LegionBuyButton.Create(parent, LegionBuyButton.Variant.Green, LegionCurrency.None, 0, "ABRIR", OpenBox);
                LegionUI.Inside((RectTransform)open.transform, Box, 606, 1456, 756, 1502);
                if (claimed) open.SetBought(true, "ABERTA");
            }
        }

        private void OpenBox()
        {
            string key = BoxKey();
            if (PlayerPrefs.GetInt(key, 0) == 1) return;
            PlayerPrefs.SetInt(key, 1);
            PlayerPrefs.Save();
            (int gold, int gems) reward = BoxReward();
            Economy.Add(reward.gold, reward.gems, "caixa de missões");
            string potion = MetaCatalog.Potions[UnityEngine.Random.Range(0, MetaCatalog.Potions.Count)].Id;
            SaveManager.Current.inventory.Add(potion, 1);
            SaveManager.Save();
            var bundle = new RewardBundle { Gold = reward.gold, Gems = reward.gems };
            bundle.Lines.Add($"+{reward.gold} ouro");
            bundle.Lines.Add($"+{reward.gems} gemas");
            bundle.Lines.Add(MetaCatalog.Potion(potion).Name);
            Ui.Push<RewardScreen>().Show("CAIXA DE MISSÕES", bundle);
        }

        // Período atual das missões da aba (diário ou semanal): a caixa vale uma vez por período.
        private string BoxKey()
        {
            bool weekly = tab == 1;
            MissionDef first = MetaCatalog.Missions.Find(m => m.Weekly == weekly);
            long period = first != null ? MetaService.Mission(first).periodStartTicks : 0;
            return $"LR_MissionBox_{(weekly ? "w" : "d")}_{period}";
        }

        private (int gold, int gems) BoxReward() => tab == 1 ? (1000, 30) : (300, 10);

        // ---------------------------------------------------------------- dados

        private static List<Row> Rows(int index)
        {
            var list = new List<Row>();
            if (index == 2)
            {
                foreach (AchievementDef def in MetaCatalog.Achievements)
                {
                    AchievementDef d = def;
                    list.Add(new Row
                    {
                        Icon = AchievementIcon(d.Metric), Title = d.Title, Description = d.Description,
                        Value = MetaService.AchievementValue(d), Target = d.Target, Gems = d.RewardGems,
                        Claimed = MetaService.IsAchievementClaimed(d), Claim = () => MetaService.ClaimAchievement(d)
                    });
                }
                return list;
            }
            foreach (MissionDef def in MetaCatalog.Missions)
            {
                if (def.Weekly != (index == 1)) continue;
                MissionDef d = def;
                MissionProgress progress = MetaService.Mission(d);
                list.Add(new Row
                {
                    Icon = MissionIcon(d.Metric), Title = d.Title, Description = MissionDescription(d),
                    Value = progress.progress, Target = d.Target, Gold = d.RewardGold, Gems = d.RewardGems,
                    Claimed = progress.claimed, Claim = () => MetaService.ClaimMission(d)
                });
            }
            return list;
        }

        // Um ícone por tipo de missão (Atlas de Ícones): espadas (vencer), caveira (inimigos), coroa (chefões),
        // moedas (ouro), habilidade, estrela e mapa (jogar).
        private static string MissionIcon(MissionMetric metric) => metric switch
        {
            MissionMetric.WinLevels => "Icons/sword",
            MissionMetric.KillEnemies => "Hud/hud_skull",
            MissionMetric.DefeatBosses => "Icons/crown",
            MissionMetric.EarnGold => "Rewards/coin_pile",
            MissionMetric.UseSkills => "Hud/skill_storm",
            MissionMetric.EarnStars => "Icons/star_badge",
            _ => "Icons/map"
        };

        private static string AchievementIcon(AchievementMetric metric) => metric switch
        {
            AchievementMetric.BossesDefeated => "Icons/crown",
            AchievementMetric.TotalKills => "Hud/hud_skull",
            AchievementMetric.BestArmy => "Hud/hud_people",
            AchievementMetric.TotalStars => "Icons/star_badge",
            AchievementMetric.HeroesOwned => "Hud/portrait_king",
            _ => "Icons/trophy"
        };

        private static string MissionDescription(MissionDef def) => def.Metric switch
        {
            MissionMetric.PlayLevels => $"Jogue {def.Target} fases no modo campanha",
            MissionMetric.WinLevels => $"Vença {def.Target} fases no modo campanha",
            MissionMetric.KillEnemies => $"Elimine {def.Target} inimigos em qualquer fase",
            MissionMetric.EarnGold => $"Junte {def.Target} de ouro nas fases",
            MissionMetric.UseSkills => $"Use {def.Target} habilidades na batalha",
            MissionMetric.DefeatBosses => $"Derrote {def.Target} chefões",
            MissionMetric.EarnStars => $"Conquiste {def.Target} estrelas",
            _ => string.Empty
        };

        private static string RewardText(int gold, int gems) =>
            gold > 0 && gems > 0 ? $"+{gold} ouro, +{gems} gemas" : gems > 0 ? $"+{gems} gemas" : $"+{gold} ouro";
    }

    // Conquistas: a mesma tela de missões, aberta na aba CONQUISTAS.
    public class AchievementsScreen : MissionsScreen
    {
        protected override int StartTab => 2;
    }
}
