using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LegionRush
{
    // Melhorias no layout do mockup: abas BASE/TROPAS/HERÓIS/FEITIÇOS, melhoria em destaque (arte, nível atual → próximo,
    // atributos e MELHORAR) e grade de cartas com barra de nível, atributo e custo.
    // Usa as melhorias permanentes do jogo (MetaCatalog.Upgrades), o nível do herói em campo e as habilidades.
    public class UpgradeScreen : UIScreen
    {
        private static readonly Vector4 Box = new Vector4(0, 0, LegionUI.RefWidth, LegionUI.RefHeight);
        private static readonly (string label, string icon)[] TabDefs =
        {
            ("BASE", "MainMenu:Icon_Home"), ("TROPAS", "Settings:Icon_Swords"), ("HERÓIS", "MainMenu:Icon_Heroes"), ("FEITIÇOS", "Icons/potion_blue")
        };
        private static readonly string[][] TabUpgrades =
        {
            new[] { "start_troops", "gold_gain", "atk_speed" },
            new[] { "archer_dmg", "warrior_hp", "magic_dmg" },
            new[] { "hero_power" },
            new string[0]
        };

        private MenuChrome chrome;
        private RectTransform body;
        private readonly List<Image> tabs = new List<Image>();
        private int tab;

        private class Entry
        {
            public string Title, Description, Art, StatIcon, StatLabel, Now, Gain;
            public int Level, Max, Gold, Gems;
            public bool Locked;
            public Func<PurchaseResult> Buy;
        }

        protected override void Build(RectTransform root)
        {
            chrome = MenuChrome.Build(root, Ui, "MELHORIAS", OnBack);
            RectTransform top = LegionUI.Place(UI.Rect(root, "Top"), LegionUI.Edge.Top, 0, 0, LegionUI.RefWidth, LegionUI.RefHeight);
            MenuChrome.Text(top, Box, 250, 258, 690, 298, "Deixe seu exército mais forte!", 24f, LegionTextStyle.PrimaryButton, Color.white,
                TextAnchor.MiddleCenter);
            float[] x0 = { 18, 252, 474, 696 };
            float[] x1 = { 246, 468, 690, 922 };
            for (int i = 0; i < TabDefs.Length; i++)
            {
                int index = i;
                Image button = MenuChrome.Panel(top, Box, x0[i], 304, x1[i], 410, "Tab" + i);
                Image icon = TabIcon(button.transform, TabDefs[i].icon);
                UI.Place(icon.rectTransform, 0.32f, 0.38f, 0.68f, 0.94f);
                TMP_Text label = MainMenuButtons.Label(button.transform, TabDefs[i].label, 24f, LegionTextStyle.PrimaryButton, Color.white);
                UI.Place(label.rectTransform, 0.04f, 0.04f, 0.96f, 0.38f);
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

        private static Image TabIcon(Transform parent, string path)
        {
            if (path.StartsWith("MainMenu:")) return MainMenuArt.Image(parent, path.Substring(9));
            if (path.StartsWith("Settings:")) return SettingsArt.Image(parent, path.Substring(9));
            Image image = LegionArt.Image(parent, path, false, "Icon");
            image.preserveAspect = true;
            return image;
        }

        public override void Refresh()
        {
            if (!SaveManager.IsLoaded) return;
            chrome.Refresh(Ui);
            for (int i = 0; i < tabs.Count; i++)
                tabs[i].sprite = MainMenuArt.Get(i == tab ? "Button_NavFrameSelected" : "Button_SideFrame");

            UI.Clear(body);
            RectTransform area = LegionUI.Place(UI.Rect(body, "Area"), LegionUI.Edge.Top, 0, 0, LegionUI.RefWidth, LegionUI.RefHeight);
            List<Entry> entries = Entries(tab);
            if (entries.Count == 0)
            {
                MenuChrome.Panel(area, Box, 20, 420, 922, 790);
                MenuChrome.Text(area, Box, 60, 560, 880, 650, "Todas as melhorias desta aba já foram feitas!", 30f, LegionTextStyle.PrimaryButton,
                    Color.white, TextAnchor.MiddleCenter);
                return;
            }
            Featured(area, entries[0]);
            float[] cx = { 20, 328, 630 };
            float[] cw = { 294, 288, 290 };
            for (int i = 1; i < entries.Count && i <= 6; i++)
            {
                int k = i - 1;
                float y0 = k < 3 ? 805f : 1122f;
                Card(area, entries[i], cx[k % 3], y0, cx[k % 3] + cw[k % 3], y0 + (k < 3 ? 302 : 321));
            }
        }

        // ---------------------------------------------------------------- destaque

        private void Featured(RectTransform parent, Entry e)
        {
            MenuChrome.Panel(parent, Box, 20, 420, 922, 790, "Featured");
            Image picture = tab == 0 && e.Art == "Icons/helmet"
                ? MainMenuArt.Image(parent, "Background_FrozenKingdom", "Art")
                : LegionArt.Image(parent, e.Art, false, "Art");
            picture.preserveAspect = true;
            LegionUI.Inside(picture.rectTransform, Box, 28, 428, 452, 782);

            MenuChrome.Text(parent, Box, 468, 428, 910, 474, e.Title, 36f, LegionTextStyle.PrimaryButton, Color.white);
            MenuChrome.Text(parent, Box, 468, 472, 910, 504, e.Description, 19f, LegionTextStyle.Body, new Color(0.85f, 0.9f, 1f));

            bool max = e.Level >= e.Max && !e.Locked;
            Chip(parent, 538, 514, 662, 562, e.Locked ? "Bloqueada" : "Nível " + e.Level, false);
            if (!max)
            {
                MenuChrome.Text(parent, Box, 670, 514, 708, 562, "<color=#4FB4FF>></color>", 30f, LegionTextStyle.PrimaryButton, Color.white,
                    TextAnchor.MiddleCenter);
                Chip(parent, 715, 514, 842, 562, e.Locked ? "Liberar" : "Nível " + (e.Level + 1), true);
            }

            Image stats = UI.Panel(parent, new Color(0.03f, 0.07f, 0.18f, 0.85f), "Stats");
            LegionUI.Inside(stats.rectTransform, Box, 470, 572, 906, 702);
            StatRow(parent, 576, e.StatIcon, e.StatLabel, e.Now, max ? string.Empty : e.Gain);
            StatRow(parent, 618, "Icons/star_badge", "Nível", $"{e.Level}/{e.Max}", max ? string.Empty : "+1");
            StatRow(parent, 660, "Icons/coin", "Custo", max ? "—" : CostText(e), string.Empty);

            Image box = UI.Panel(parent, new Color(0.03f, 0.07f, 0.18f, 0.9f), "Progress");
            LegionUI.Inside(box.rectTransform, Box, 470, 712, 634, 772);
            MenuChrome.Art(parent, Box, 478, 718, 526, 766, "Icons/trophy");
            MenuChrome.Text(parent, Box, 528, 712, 630, 772, max ? "MÁX." : $"{e.Level}/{e.Max}", 28f, LegionTextStyle.Hud, Color.white, TextAnchor.MiddleCenter);

            BuyButton(parent, e, 646, 706, 906, 782, 30f);
        }

        private void Chip(RectTransform parent, float x0, float y0, float x1, float y1, string text, bool next)
        {
            Image chip = UI.Panel(parent, new Color(0.03f, 0.07f, 0.18f, 0.95f), "Chip");
            LegionUI.Inside(chip.rectTransform, Box, x0, y0, x1, y1);
            if (next)
            {
                Outline ring = chip.gameObject.AddComponent<Outline>();
                ring.effectColor = MenuChrome.GoldColor;
                ring.effectDistance = new Vector2(3f, -3f);
            }
            MenuChrome.Text(parent, Box, x0, y0, x1, y1, text, 24f, LegionTextStyle.PrimaryButton, Color.white, TextAnchor.MiddleCenter);
        }

        private void StatRow(RectTransform parent, float y, string icon, string label, string now, string gain)
        {
            MenuChrome.Art(parent, Box, 484, y + 4, 520, y + 38, icon);
            MenuChrome.Text(parent, Box, 532, y, 720, y + 42, label, 20f, LegionTextStyle.Menu, Color.white);
            MenuChrome.Text(parent, Box, 700, y, 800, y + 42, now, 22f, LegionTextStyle.Hud, Color.white, TextAnchor.MiddleRight);
            MenuChrome.Text(parent, Box, 812, y, 904, y + 42, gain, 22f, LegionTextStyle.Positive, new Color(0.45f, 1f, 0.4f));
        }

        // ---------------------------------------------------------------- cartas

        private void Card(RectTransform parent, Entry e, float x0, float y0, float x1, float y1)
        {
            MenuChrome.Panel(parent, Box, x0, y0, x1, y1, "Card");
            MenuChrome.Art(parent, Box, x0 + 8, y0 + 8, x0 + 150, y0 + 118, e.Art);
            MenuChrome.Text(parent, Box, x0 + 140, y0 + 10, x1 - 8, y0 + 46, e.Title, 21f, LegionTextStyle.PrimaryButton, Color.white, TextAnchor.MiddleRight);
            MenuChrome.Text(parent, Box, x0 + 140, y0 + 46, x1 - 8, y0 + 80, e.Locked ? "Bloqueada" : "Nível " + e.Level, 21f, LegionTextStyle.Hud,
                MenuChrome.GoldText, TextAnchor.MiddleRight);

            // Barra de nível.
            Image track = UI.Panel(parent, new Color(0.03f, 0.07f, 0.18f, 0.95f), "Track");
            LegionUI.Inside(track.rectTransform, Box, x0 + 16, y0 + 124, x1 - 16, y0 + 152);
            float ratio = e.Max > 0 ? Mathf.Clamp01(e.Level / (float)e.Max) : 0f;
            if (ratio > 0f)
            {
                Image fill = UI.Panel(parent, MenuChrome.Green, "Fill");
                LegionUI.Inside(fill.rectTransform, Box, x0 + 18, y0 + 126, Mathf.Lerp(x0 + 30, x1 - 18, ratio), y0 + 150);
            }
            MenuChrome.Text(parent, Box, x0 + 16, y0 + 124, x1 - 16, y0 + 152, $"{e.Level}/{e.Max}", 18f, LegionTextStyle.Hud, Color.white,
                TextAnchor.MiddleCenter);

            Image stat = UI.Panel(parent, new Color(0.03f, 0.07f, 0.18f, 0.85f), "Stat");
            LegionUI.Inside(stat.rectTransform, Box, x0 + 12, y0 + 162, x1 - 12, y1 - 76);
            MenuChrome.Art(parent, Box, x0 + 18, y0 + 168, x0 + 62, y1 - 82, e.StatIcon);
            MenuChrome.Text(parent, Box, x0 + 70, y0 + 162, x1 - 14, y0 + 190, e.StatLabel, 17f, LegionTextStyle.Menu, Color.white);
            bool max = e.Level >= e.Max && !e.Locked;
            MenuChrome.Text(parent, Box, x0 + 70, y0 + 188, x1 - 14, y1 - 78,
                e.Now + (max ? string.Empty : $"   <color=#6CFF5A>{e.Gain}</color>"), 20f, LegionTextStyle.Hud, Color.white);

            BuyButton(parent, e, x0 + 16, y1 - 70, x1 - 16, y1 - 10, 22f);
        }

        private void BuyButton(RectTransform parent, Entry e, float x0, float y0, float x1, float y1, float size)
        {
            bool max = e.Level >= e.Max && !e.Locked;
            string price = max ? "MÁXIMO" : $"{CostText(e)}\nMELHORAR";
            if (e.Locked && !max) price = $"{CostText(e)}\nLIBERAR";
            MenuChrome.Button(parent, Box, x0, y0, x1, y1, price, max ? MenuChrome.Gray : MenuChrome.Green, max ? null : (Action)(() => Buy(e)), size);
        }

        private static string CostText(Entry e) => e.Gems > 0
            ? $"{MenuChrome.Format(e.Gems)} gemas"
            : $"{MenuChrome.Format(e.Gold)} ouro";

        private void Buy(Entry e)
        {
            if (!Economy.CanAfford(e.Gold, e.Gems))
            {
                Ui.Toast("Saldo insuficiente");
                GameAudio.Play(SoundId.UiBack);
                return;
            }
            Ui.Confirm(e.Locked ? "LIBERAR" : "MELHORAR", $"{e.Title}\npor {CostText(e)}?", e.Locked ? "LIBERAR" : "MELHORAR", () =>
            {
                PurchaseResult result = e.Buy();
                GameAudio.Play(result == PurchaseResult.Success ? SoundId.UiConfirm : SoundId.UiBack);
                Ui.Toast(result == PurchaseResult.Success ? e.Title + (e.Locked ? " liberada!" : " melhorada!") : MetaService.Explain(result));
                if (result == PurchaseResult.Success && Ui.Game != null) Ui.Game.Restart();
                Ui.RefreshTop();
            });
        }

        // ---------------------------------------------------------------- dados

        private List<Entry> Entries(int index)
        {
            var list = new List<Entry>();
            foreach (string id in TabUpgrades[index])
            {
                UpgradeDef def = MetaCatalog.Upgrade(id);
                if (def != null) list.Add(FromUpgrade(def));
            }
            if (index == 2) AddHero(list);
            if (index == 3) AddSkills(list);
            // Melhorias no máximo vão para o fim (o destaque mostra a próxima a fazer).
            list.Sort((a, b) => (a.Level >= a.Max && !a.Locked).CompareTo(b.Level >= b.Max && !b.Locked));
            return list;
        }

        private static Entry FromUpgrade(UpgradeDef def)
        {
            int level = MetaService.UpgradeLevel(def.Id);
            bool hero = def.Id == "hero_power";
            return new Entry
            {
                Title = def.Name,
                Description = Description(def.Id),
                Art = ArtOf(def.Id),
                StatIcon = StatIconOf(def.Id),
                StatLabel = hero ? "Passiva do herói" : "Bônus",
                Now = hero ? $"+{level * 10}%" : level == 0 ? "—" : def.PerLevel.Describe(level),
                Gain = hero ? "+10%" : "→ " + def.PerLevel.Describe(level + 1),
                Level = level,
                Max = def.MaxLevel,
                Gold = def.CostFor(level),
                Buy = () => MetaService.BuyUpgrade(def.Id)
            };
        }

        private static void AddHero(List<Entry> list)
        {
            HeroDef hero = MetaCatalog.Hero(SaveManager.Current.heroes.selected);
            HeroRecord record = hero != null ? SaveManager.Current.heroes.Get(hero.Id) : null;
            if (hero == null || record == null) return;
            list.Add(new Entry
            {
                Title = hero.Name,
                Description = hero.Title + " · herói em campo",
                Art = "Hud/portrait_king",
                StatIcon = "Icons/crown",
                StatLabel = "Passiva",
                Now = hero.Passive.Describe(1f + hero.PerLevel * (record.level - 1)),
                Gain = "+" + Mathf.RoundToInt(hero.PerLevel * 100f) + "%",
                Level = record.level,
                Max = 10,
                Gold = hero.LevelCost(record.level),
                Buy = () => MetaService.LevelUpHero(hero.Id)
            });
        }

        private void AddSkills(List<Entry> list)
        {
            SkillSystem skills = Ui.Game != null ? Ui.Game.CurrentSkills : null;
            if (skills == null) return;
            for (int s = 0; s < SkillDefinition.Count; s++)
            {
                var id = (SkillId)s;
                SkillDefinition def = skills.Definition(id);
                bool unlocked = MetaService.IsSkillUnlocked(id);
                list.Add(new Entry
                {
                    Title = def.displayName,
                    Description = def.description,
                    Art = SkillArt(id),
                    StatIcon = "Icons/energy",
                    StatLabel = "Recarga",
                    Now = Mathf.RoundToInt(def.cooldown) + "s",
                    Gain = unlocked ? string.Empty : "nova",
                    Level = unlocked ? 1 : 0,
                    Max = 1,
                    Locked = !unlocked,
                    Gold = MetaCatalog.SkillUnlockGold[s],
                    Gems = MetaCatalog.SkillUnlockGems[s],
                    Buy = () => MetaService.UnlockSkill(id)
                });
            }
        }

        private static string Description(string id) => id switch
        {
            "start_troops" => "Começa cada fase com mais soldados.",
            "gold_gain" => "Mais ouro ao fim de cada fase.",
            "atk_speed" => "Todas as tropas atacam mais rápido.",
            "archer_dmg" => "Flechas mais fortes para os arqueiros.",
            "warrior_hp" => "Guerreiros aguentam mais golpes.",
            "magic_dmg" => "Magias dos magos causam mais dano.",
            "hero_power" => "Fortalece a habilidade passiva do herói.",
            _ => string.Empty
        };

        private static string ArtOf(string id) => id switch
        {
            "start_troops" => "Icons/helmet",
            "gold_gain" => "Rewards/coin_pile",
            "atk_speed" => "Icons/energy",
            "archer_dmg" => "Hud/portrait_archer",
            "warrior_hp" => "Avatars/avatar_warrior",
            "magic_dmg" => "Hud/portrait_mage",
            "hero_power" => "Hud/portrait_king",
            _ => "Icons/star_badge"
        };

        private static string StatIconOf(string id) => id switch
        {
            "gold_gain" => "Icons/coin",
            "warrior_hp" => "Icons/shield",
            "start_troops" => "Icons/helmet",
            "atk_speed" => "Icons/energy",
            _ => "Icons/sword"
        };

        private static string SkillArt(SkillId id) => id switch
        {
            SkillId.ArrowRain => "Hud/skill_arrows",
            SkillId.CommanderStrike => "Hud/skill_charge",
            SkillId.CavalryCharge => "Hud/skill_charge",
            SkillId.ArcaneExplosion => "Hud/skill_storm",
            SkillId.Freeze => "Hud/skill_storm",
            SkillId.CollectiveShield => "Icons/shield",
            SkillId.AreaHeal => "Icons/potion_green",
            _ => "Icons/star_badge"
        };
    }
}
