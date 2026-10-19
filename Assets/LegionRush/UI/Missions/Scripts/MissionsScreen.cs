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
        private const float ListTop = 546f;
        private const float ListBottom = 1342f;
        private const float RowStep = 134f;

        private MenuChrome chrome;
        private RectTransform body;
        private readonly List<Image> tabs = new List<Image>();
        private readonly List<GameObject> alerts = new List<GameObject>();
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
            RectTransform top = LegionUI.Place(UI.Rect(root, "Tabs"), LegionUI.Edge.Top, 0, 0, LegionUI.RefWidth, LegionUI.RefHeight);
            float[] x0 = { 28, 328, 626 };
            float[] x1 = { 316, 614, 914 };
            for (int i = 0; i < TabNames.Length; i++)
            {
                int index = i;
                Image button = UI.Panel(top, new Color(0.05f, 0.1f, 0.26f, 0.97f), "Tab" + i);
                LegionUI.Inside(button.rectTransform, Box, x0[i], 290, x1[i], 356);
                Outline ring = button.gameObject.AddComponent<Outline>();
                ring.effectDistance = new Vector2(2f, -2f);
                TMP_Text label = MainMenuButtons.Label(button.transform, TabNames[i], 28f, LegionTextStyle.PrimaryButton, Color.white);
                UI.Stretch(label.rectTransform);
                MainMenuButtons.Make(button.rectTransform, button, () =>
                {
                    tab = index;
                    Refresh();
                });
                tabs.Add(button);
                Image alert = LegionArt.Image(top, "Badges/orb_red", false, "Alert");
                alert.preserveAspect = true;
                LegionUI.Inside(alert.rectTransform, Box, x1[i] - 36, 276, x1[i] - 2, 312);
                TMP_Text mark = MainMenuButtons.Label(alert.transform, "!", 22f, LegionTextStyle.PrimaryButton, Color.white);
                UI.Stretch(mark.rectTransform);
                alerts.Add(alert.gameObject);
            }
            body = UI.Rect(root, "Body");
            UI.Stretch(body);
            MenuChrome.Nav(root, Ui, MainMenuDestination.More);
        }

        protected virtual string TitleText => "MISSÕES";

        public override void Refresh()
        {
            if (!SaveManager.IsLoaded) return;
            chrome.Refresh(Ui);
            for (int i = 0; i < tabs.Count; i++)
            {
                bool on = i == tab;
                tabs[i].color = on ? new Color(0.13f, 0.55f, 1f) : new Color(0.05f, 0.1f, 0.26f, 0.97f);
                tabs[i].GetComponent<Outline>().effectColor = on ? new Color(0.55f, 0.9f, 1f) : new Color(0.25f, 0.4f, 0.75f, 0.7f);
                alerts[i].SetActive(Rows(i).Exists(r => !r.Claimed && r.Value >= r.Target));
            }

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
            MenuChrome.Panel(parent, Box, 26, 368, 914, 530, "Progress");
            string title = tab == 0 ? "Progresso Diário" : tab == 1 ? "Progresso Semanal" : "Conquistas";
            string subtitle = tab == 2 ? "Conclua desafios e ganhe gemas!" : $"Complete missões {(tab == 0 ? "diárias" : "semanais")} e ganhe recompensas!";
            MenuChrome.Text(parent, Box, 48, 380, 540, 428, title, 36f, LegionTextStyle.PrimaryButton, Color.white);
            MenuChrome.Text(parent, Box, 48, 424, 560, 458, subtitle, 21f, LegionTextStyle.Body, Color.white);
            Bar(parent, 50, 468, 520, 514, done, rows.Count, new Color(0.13f, 0.55f, 1f), 30f);
            MenuChrome.Art(parent, Box, 536, 384, 696, 524, "Chests/chest_epic");

            Image final = UI.Panel(parent, new Color(0.03f, 0.07f, 0.18f, 0.9f), "Final");
            LegionUI.Inside(final.rectTransform, Box, 704, 380, 904, 520);
            if (tab == 2)
            {
                int gems = 0;
                foreach (AchievementDef def in MetaCatalog.Achievements) if (MetaService.IsAchievementClaimed(def)) gems += def.RewardGems;
                MenuChrome.Text(parent, Box, 710, 386, 898, 422, "Gemas ganhas", 20f, LegionTextStyle.PrimaryButton, Color.white, TextAnchor.MiddleCenter);
                MenuChrome.Art(parent, Box, 740, 428, 790, 478, "Icons/gem");
                MenuChrome.Text(parent, Box, 794, 428, 900, 478, MenuChrome.Format(gems), 28f, LegionTextStyle.Hud, Color.white);
                return;
            }
            (int gold, int gems) box = BoxReward();
            MenuChrome.Text(parent, Box, 710, 386, 898, 422, "Recompensa Final", 20f, LegionTextStyle.PrimaryButton, Color.white, TextAnchor.MiddleCenter);
            MenuChrome.Art(parent, Box, 722, 426, 770, 474, "Icons/coin");
            MenuChrome.Text(parent, Box, 712, 474, 780, 508, MenuChrome.Format(box.gold), 20f, LegionTextStyle.Hud, Color.white, TextAnchor.MiddleCenter);
            MenuChrome.Art(parent, Box, 802, 426, 850, 474, "Icons/gem");
            MenuChrome.Text(parent, Box, 792, 474, 860, 508, MenuChrome.Format(box.gems), 20f, LegionTextStyle.Hud, Color.white, TextAnchor.MiddleCenter);
            MenuChrome.Art(parent, Box, 856, 426, 900, 474, "Icons/potion_red");
            MenuChrome.Text(parent, Box, 852, 474, 904, 508, "1", 20f, LegionTextStyle.Hud, Color.white, TextAnchor.MiddleCenter);
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
            MenuChrome.Text(parent, box, 172, y0 + 8, 560, y0 + 46, row.Title, 27f, LegionTextStyle.PrimaryButton, Color.white);
            MenuChrome.Text(parent, box, 172, y0 + 44, 570, y0 + 74, row.Description, 19f, LegionTextStyle.Body, new Color(0.75f, 0.88f, 1f));

            bool complete = row.Value >= row.Target;
            Image track = UI.Panel(parent, new Color(0.03f, 0.07f, 0.18f, 0.95f), "Track");
            LegionUI.Inside(track.rectTransform, box, 172, y0 + 80, 540, y0 + 108);
            float ratio = row.Target > 0 ? Mathf.Clamp01(row.Value / (float)row.Target) : 0f;
            if (ratio > 0f)
            {
                Image fill = UI.Panel(parent, complete ? MenuChrome.Green : new Color(0.13f, 0.55f, 1f), "Fill");
                LegionUI.Inside(fill.rectTransform, box, 174, y0 + 82, Mathf.Lerp(186, 538, ratio), y0 + 106);
            }
            MenuChrome.Text(parent, box, 172, y0 + 80, 540, y0 + 108, $"{MenuChrome.Format(Mathf.Min(row.Value, row.Target))}/{MenuChrome.Format(row.Target)}",
                20f, LegionTextStyle.Hud, Color.white, TextAnchor.MiddleCenter);

            Image rewards = UI.Panel(parent, new Color(0.03f, 0.07f, 0.18f, 0.6f), "Rewards");
            LegionUI.Inside(rewards.rectTransform, box, 570, y0 + 8, 740, y1 - 8);
            float ry = row.Gold > 0 && row.Gems > 0 ? y0 + 14 : y0 + 38;
            if (row.Gold > 0)
            {
                MenuChrome.Art(parent, box, 598, ry, 642, ry + 44, "Icons/coin");
                MenuChrome.Text(parent, box, 650, ry, 738, ry + 44, MenuChrome.Format(row.Gold), 22f, LegionTextStyle.Hud, Color.white);
                ry += 50;
            }
            if (row.Gems > 0)
            {
                MenuChrome.Art(parent, box, 598, ry, 642, ry + 44, "Icons/gem");
                MenuChrome.Text(parent, box, 650, ry, 738, ry + 44, MenuChrome.Format(row.Gems), 22f, LegionTextStyle.Hud, Color.white);
            }

            if (row.Claimed)
            {
                MenuChrome.Button(parent, box, 750, y0 + 30, 894, y1 - 30, "FEITO", MenuChrome.Gray, null, 26f);
                MenuChrome.Art(parent, box, 868, y0 + 14, 906, y0 + 52, "Badges/check");
            }
            else if (complete)
            {
                MenuChrome.Button(parent, box, 750, y0 + 30, 894, y1 - 30, "COLETAR", MenuChrome.Green, () =>
                {
                    if (row.Claim())
                    {
                        GameAudio.Play(SoundId.CoinPickup);
                        Ui.Toast(RewardText(row.Gold, row.Gems));
                    }
                    Ui.RefreshTop();
                }, 24f);
            }
            else
            {
                MenuChrome.Button(parent, box, 750, y0 + 30, 894, y1 - 30, "IR", new Color(0.13f, 0.55f, 1f), () =>
                {
                    Ui.Replace<MainMenuScreen>();
                    new UIRootMainMenuNavigator(Ui).Open(MainMenuDestination.Play);
                }, 36f);
            }
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
            MenuChrome.Text(parent, Box, 48, 1398, 600, 1428, $"Complete todas as missões {(tab == 0 ? "diárias" : "semanais")}", 21f,
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
            MenuChrome.Text(parent, Box, 300, 1468, 460, 1498, $"{done}/{rows.Count}", 20f, LegionTextStyle.Hud, Color.white, TextAnchor.MiddleCenter);

            MenuChrome.Art(parent, Box, 600, 1356, 760, 1500, "Chests/chest_epic");
            Image rewards = UI.Panel(parent, new Color(0.03f, 0.07f, 0.18f, 0.7f), "Rewards");
            LegionUI.Inside(rewards.rectTransform, Box, 772, 1362, 904, 1494);
            (int gold, int gems) reward = BoxReward();
            MenuChrome.Art(parent, Box, 784, 1370, 818, 1404, "Icons/coin");
            MenuChrome.Text(parent, Box, 824, 1370, 902, 1404, MenuChrome.Format(reward.gold), 20f, LegionTextStyle.Hud, Color.white);
            MenuChrome.Art(parent, Box, 784, 1412, 818, 1446, "Icons/gem");
            MenuChrome.Text(parent, Box, 824, 1412, 902, 1446, MenuChrome.Format(reward.gems), 20f, LegionTextStyle.Hud, Color.white);
            MenuChrome.Art(parent, Box, 784, 1454, 818, 1488, "Icons/potion_red");
            MenuChrome.Text(parent, Box, 824, 1454, 902, 1488, "1", 20f, LegionTextStyle.Hud, Color.white);

            bool claimed = PlayerPrefs.GetInt(BoxKey(), 0) == 1;
            bool ready = rows.Count > 0 && done >= rows.Count && !claimed;
            if (claimed) MenuChrome.Button(parent, Box, 610, 1462, 750, 1500, "COLETADA", MenuChrome.Gray, null, 18f);
            else if (ready) MenuChrome.Button(parent, Box, 610, 1462, 750, 1500, "ABRIR", MenuChrome.Green, OpenBox, 22f);
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

        private static string MissionIcon(MissionMetric metric) => metric switch
        {
            MissionMetric.KillEnemies => "Hud/hud_skull",
            MissionMetric.DefeatBosses => "Hud/hud_skull",
            MissionMetric.EarnGold => "Rewards/coin_pile",
            MissionMetric.UseSkills => "Hud/skill_storm",
            MissionMetric.EarnStars => "Icons/star_badge",
            _ => "Settings:Icon_Swords"
        };

        private static string AchievementIcon(AchievementMetric metric) => metric switch
        {
            AchievementMetric.BossesDefeated => "Hud/hud_skull",
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
