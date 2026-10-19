using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LegionRush
{
    // Melhorias (entrega/referencias/melhorias_referencia.png e auditoria 1e): abas BASE/TROPAS/HERÓIS/FEITIÇOS com ícone,
    // melhoria em destaque (a selecionada) e grade de 3 colunas com todas as melhorias da aba. Cada carta: ícone à esquerda,
    // título + nível à direita, barra com número, bônus "atual → +próximo" e LegionBuyButton.
    // Usa as 7 melhorias permanentes (MetaCatalog.Upgrades), o herói em campo e as habilidades.
    public class UpgradeScreen : UIScreen
    {
        private static readonly Vector4 Box = new Vector4(0, 0, LegionUI.RefWidth, LegionUI.RefHeight);
        private const float GridTop = 712f, GridBottom = 1492f;
        private const float CardW = 288f, CardH = 310f, RowStep = 322f;
        private static readonly float[] ColumnX = { 22, 328, 634 };

        private static readonly (string label, string icon)[] TabDefs =
        {
            ("BASE", "MainMenu:Icon_Home"), ("TROPAS", "Icons/sword"), ("HERÓIS", "MainMenu:Icon_Heroes"), ("FEITIÇOS", "Icons/potion_blue")
        };

        // BASE: tropas iniciais, ouro, vida · TROPAS: velocidade de ataque, dano · HERÓIS: poder do herói · FEITIÇOS: habilidades.
        private static readonly string[][] TabUpgrades =
        {
            new[] { "start_troops", "gold_gain", "warrior_hp" },
            new[] { "atk_speed", "archer_dmg", "magic_dmg" },
            new[] { "hero_power" },
            new string[0]
        };

        private MenuChrome chrome;
        private RectTransform body;
        private LegionTabs tabs;
        private int tab;
        private string selected;

        private class Entry
        {
            public string Key, Title, Description, Art, StatIcon, StatLabel, Now, Next;
            public int Level, Max, Gold, Gems;
            public bool Locked;
            public Func<PurchaseResult> Buy;
            public bool IsMax => Level >= Max && !Locked;
        }

        protected override void Build(RectTransform root)
        {
            chrome = MenuChrome.Build(root, Ui, "MELHORIAS", OnBack);
            RectTransform top = LegionUI.Place(UI.Rect(root, "Top"), LegionUI.Edge.Top, 0, 0, LegionUI.RefWidth, LegionUI.RefHeight);
            MenuChrome.Text(top, Box, 180, 230, 760, 264, "Deixe seu exército mais forte!", 26f, LegionTextStyle.PrimaryButton, Color.white,
                TextAnchor.MiddleCenter);

            // Abas com ícone a 60 % da altura.
            RectTransform tabArea = LegionUI.Inside(UI.Rect(top, "Tabs"), Box, 18, 272, 923, 372);
            string[] labels = Array.ConvertAll(TabDefs, t => t.label);
            tabs = LegionTabs.Create(tabArea, labels, i =>
            {
                tab = i;
                selected = null;
                Refresh();
            }, 24f, 8f);
            tabs.Select(0);
            for (int i = 0; i < TabDefs.Length; i++)
            {
                Transform frame = tabArea.GetChild(i);
                Transform inner = frame.Find("Inner");
                // Ícone em cima e rótulo embaixo: troca o layout horizontal por vertical.
                HorizontalLayoutGroup row = inner.GetComponent<HorizontalLayoutGroup>();
                if (row != null) UnityEngine.Object.DestroyImmediate(row);
                TMP_Text label = inner.GetComponentInChildren<TMP_Text>();
                UI.Place(label.rectTransform, 0f, 0f, 1f, 0.36f);
                Image icon = TabIcon(inner, TabDefs[i].icon);
                UI.Place(icon.rectTransform, 0.2f, 0.36f, 0.8f, 1f);
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

            UI.Clear(body);
            RectTransform area = LegionUI.Place(UI.Rect(body, "Area"), LegionUI.Edge.Top, 0, 0, LegionUI.RefWidth, LegionUI.RefHeight);
            List<Entry> entries = Entries(tab);
            if (entries.Count == 0)
            {
                MenuChrome.Panel(area, Box, 20, 384, 922, 700);
                MenuChrome.Text(area, Box, 60, 500, 880, 590, "Nada para melhorar nesta aba agora.", 30f, LegionTextStyle.PrimaryButton,
                    Color.white, TextAnchor.MiddleCenter);
                return;
            }
            Entry featured = entries.Find(e => e.Key == selected) ?? entries.Find(e => !e.IsMax) ?? entries[0];
            selected = featured.Key;
            Featured(area, featured);
            Grid(body, entries);
        }

        // ---------------------------------------------------------------- destaque

        private void Featured(RectTransform parent, Entry e)
        {
            MenuChrome.Panel(parent, Box, 20, 384, 922, 700, "Featured");
            Image picture = LegionArt.Image(parent, e.Art, false, "Art");
            picture.preserveAspect = true;
            LegionUI.Inside(picture.rectTransform, Box, 40, 400, 420, 684);

            MenuChrome.Text(parent, Box, 440, 396, 906, 442, e.Title, 36f, LegionTextStyle.PrimaryButton, Color.white);
            MenuChrome.Text(parent, Box, 440, 442, 906, 474, e.Description, 22f, LegionTextStyle.Body, new Color(0.85f, 0.9f, 1f));

            Chip(parent, 470, 482, 620, 528, e.Locked ? "Bloqueada" : "Nível " + e.Level, false);
            if (!e.IsMax)
            {
                MenuChrome.Text(parent, Box, 626, 482, 670, 528, "<color=#4FB4FF>→</color>", 30f, LegionTextStyle.PrimaryButton, Color.white,
                    TextAnchor.MiddleCenter);
                Chip(parent, 676, 482, 830, 528, e.Locked ? "Liberar" : "Nível " + (e.Level + 1), true);
            }

            Image stats = UI.Panel(parent, new Color(0.03f, 0.07f, 0.18f, 0.85f), "Stats");
            LegionUI.Inside(stats.rectTransform, Box, 440, 538, 906, 612);
            MenuChrome.Art(parent, Box, 452, 548, 496, 602, e.StatIcon);
            MenuChrome.Text(parent, Box, 506, 538, 700, 575, e.StatLabel, 22f, LegionTextStyle.Menu, new Color(0.82f, 0.88f, 1f));
            MenuChrome.Text(parent, Box, 506, 573, 900, 612, Bonus(e), 26f, LegionTextStyle.Hud, Color.white);

            BuyButton(parent, e, 600, 622, 906, 690);
            if (!e.IsMax) MenuChrome.Text(parent, Box, 440, 622, 592, 690, $"{e.Level}/{e.Max}", 30f, LegionTextStyle.Hud, MenuChrome.GoldText,
                TextAnchor.MiddleCenter);
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

        // "0 → +2" (atual → próximo); no máximo só o valor atual. Nunca "—".
        private static string Bonus(Entry e) => e.IsMax ? e.Now : $"{e.Now}  <color=#6CFF5A>→ {e.Next}</color>";

        // ---------------------------------------------------------------- grade

        private void Grid(RectTransform parent, List<Entry> entries)
        {
            RectTransform view = LegionUI.Place(UI.Rect(parent, "Grid"), LegionUI.Edge.Bottom, 0, GridTop, LegionUI.RefWidth, GridBottom);
            view.gameObject.AddComponent<RectMask2D>();
            Image hit = view.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            int rows = (entries.Count + 2) / 3;
            float height = rows * RowStep - (RowStep - CardH);
            RectTransform content = UI.Rect(view, "Content");
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = new Vector2(0f, height * LegionUI.Px);
            content.anchoredPosition = Vector2.zero;
            ScrollRect scroll = view.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = view;
            scroll.horizontal = false;
            scroll.vertical = height > GridBottom - GridTop;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;

            Vector4 box = new Vector4(0, GridTop, LegionUI.RefWidth, GridTop + height);
            for (int i = 0; i < entries.Count; i++)
            {
                float x0 = ColumnX[i % 3];
                float y0 = GridTop + (i / 3) * RowStep;
                Card(content, box, entries[i], x0, y0, x0 + CardW, y0 + CardH);
            }
        }

        private void Card(RectTransform parent, Vector4 box, Entry e, float x0, float y0, float x1, float y1)
        {
            Image frame = MenuChrome.Panel(parent, box, x0, y0, x1, y1, "Card");
            frame.raycastTarget = true;
            string key = e.Key;
            MainMenuButtons.Make(frame.rectTransform, frame, () =>
            {
                selected = key;
                Refresh();
            });
            if (e.Key == selected)
            {
                Image glow = LegionArt.Image(parent, "Frames/frame_gold", false, "Selected");
                glow.type = Image.Type.Sliced;
                glow.fillCenter = false;
                glow.pixelsPerUnitMultiplier = 2.2f;
                LegionUI.Inside(glow.rectTransform, box, x0 - 4, y0 - 4, x1 + 4, y1 + 4);
            }

            // Ícone à esquerda; título e nível à direita.
            MenuChrome.Art(parent, box, x0 + 10, y0 + 12, x0 + 120, y0 + 116, e.Art);
            TMP_Text title = MenuChrome.Text(parent, box, x0 + 124, y0 + 12, x1 - 10, y0 + 70, e.Title, 22f, LegionTextStyle.PrimaryButton, Color.white,
                TextAnchor.MiddleRight);
            title.textWrappingMode = TextWrappingModes.Normal;
            MenuChrome.Text(parent, box, x0 + 124, y0 + 70, x1 - 10, y0 + 108, e.Locked ? "Bloqueada" : "Nível " + e.Level, 24f, LegionTextStyle.Hud,
                MenuChrome.GoldText, TextAnchor.MiddleRight);

            // Barra de nível com o número por cima.
            Image track = UI.Panel(parent, new Color(0.03f, 0.07f, 0.18f, 0.95f), "Track");
            LegionUI.Inside(track.rectTransform, box, x0 + 14, y0 + 120, x1 - 14, y0 + 152);
            float ratio = e.Max > 0 ? Mathf.Clamp01(e.Level / (float)e.Max) : 0f;
            if (ratio > 0f)
            {
                Image fill = UI.Panel(parent, MenuChrome.Green, "Fill");
                LegionUI.Inside(fill.rectTransform, box, x0 + 16, y0 + 122, Mathf.Lerp(x0 + 30, x1 - 16, ratio), y0 + 150);
            }
            MenuChrome.Text(parent, box, x0 + 14, y0 + 120, x1 - 14, y0 + 152, $"{e.Level}/{e.Max}", 22f, LegionTextStyle.Hud, Color.white,
                TextAnchor.MiddleCenter);

            // Bônus: nome do efeito em cima, "atual → +próximo" embaixo.
            Image stat = UI.Panel(parent, new Color(0.03f, 0.07f, 0.18f, 0.85f), "Stat");
            LegionUI.Inside(stat.rectTransform, box, x0 + 10, y0 + 160, x1 - 10, y0 + 234);
            MenuChrome.Art(parent, box, x0 + 16, y0 + 172, x0 + 58, y0 + 222, e.StatIcon);
            MenuChrome.Text(parent, box, x0 + 64, y0 + 160, x1 - 12, y0 + 194, e.StatLabel, 22f, LegionTextStyle.Menu, new Color(0.82f, 0.88f, 1f));
            MenuChrome.Text(parent, box, x0 + 64, y0 + 194, x1 - 12, y0 + 232, Bonus(e), 24f, LegionTextStyle.Hud, Color.white);

            BuyButton(parent, e, x0 + 14, y0 + 242, x1 - 14, y1 - 10, box);
        }

        private void BuyButton(RectTransform parent, Entry e, float x0, float y0, float x1, float y1) => BuyButton(parent, e, x0, y0, x1, y1, Box);

        // LegionBuyButton: moeda + valor / MELHORAR (ou LIBERAR); no máximo, selo ✓ MÁXIMO.
        private void BuyButton(RectTransform parent, Entry e, float x0, float y0, float x1, float y1, Vector4 box)
        {
            LegionCurrency currency = e.Gems > 0 ? LegionCurrency.Gems : LegionCurrency.Gold;
            LegionBuyButton button = LegionBuyButton.Create(parent, e.Locked ? LegionBuyButton.Variant.Gold : LegionBuyButton.Variant.Green,
                e.IsMax ? LegionCurrency.None : currency, e.IsMax ? 0 : e.Gems > 0 ? e.Gems : e.Gold, e.Locked ? "LIBERAR" : "MELHORAR",
                () => { if (!e.IsMax) Buy(e); });
            LegionUI.Inside((RectTransform)button.transform, box, x0, y0, x1, y1);
            if (e.IsMax) button.SetBought(true, "MÁXIMO");
        }

        private static string CostText(Entry e) => e.Gems > 0
            ? $"{MenuChrome.Format(e.Gems)} gemas"
            : $"{MenuChrome.Format(e.Gold)} de ouro";

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
            return list;
        }

        // Separa "+16% dano de Arqueiro" em valor ("+16%") e efeito ("Dano de Arqueiro").
        private static (string value, string effect) Split(string described)
        {
            int space = described.IndexOf(' ');
            if (space < 0) return (described, string.Empty);
            string effect = described.Substring(space + 1);
            return (described.Substring(0, space), char.ToUpperInvariant(effect[0]) + effect.Substring(1));
        }

        private static Entry FromUpgrade(UpgradeDef def)
        {
            int level = MetaService.UpgradeLevel(def.Id);
            bool hero = def.Id == "hero_power";
            var next = Split(def.PerLevel.Describe(level + 1));
            var now = Split(def.PerLevel.Describe(Mathf.Max(1, level)));
            return new Entry
            {
                Key = def.Id,
                Title = def.Name,
                Description = Description(def.Id),
                Art = ArtOf(def.Id),
                StatIcon = StatIconOf(def.Id),
                StatLabel = hero ? "Passiva do herói" : next.effect,
                Now = hero ? $"+{level * 10}%" : level == 0 ? "0" : now.value,
                Next = hero ? $"+{(level + 1) * 10}%" : next.value,
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
            var now = Split(hero.Passive.Describe(1f + hero.PerLevel * (record.level - 1)));
            var next = Split(hero.Passive.Describe(1f + hero.PerLevel * record.level));
            list.Add(new Entry
            {
                Key = "hero_" + hero.Id,
                Title = hero.Name,
                Description = hero.Title + " · herói em campo",
                Art = "Hud/portrait_king",
                StatIcon = "Icons/crown",
                StatLabel = now.effect,
                Now = now.value,
                Next = next.value,
                Level = record.level,
                Max = 10,
                Gold = hero.LevelCost(record.level),
                Buy = () => MetaService.LevelUpHero(hero.Id)
            });
        }

        private void AddSkills(List<Entry> list)
        {
            SkillSystem skills = Ui != null && Ui.Game != null ? Ui.Game.CurrentSkills : null;
            if (skills == null) return;
            for (int s = 0; s < SkillDefinition.Count; s++)
            {
                var id = (SkillId)s;
                SkillDefinition def = skills.Definition(id);
                bool unlocked = MetaService.IsSkillUnlocked(id);
                string cooldown = Mathf.RoundToInt(def.cooldown) + "s";
                list.Add(new Entry
                {
                    Key = "skill_" + s,
                    Title = def.displayName,
                    Description = def.description,
                    Art = SkillArt(id),
                    StatIcon = "Icons/energy",
                    StatLabel = "Recarga",
                    Now = unlocked ? cooldown : "0",
                    Next = cooldown,
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
