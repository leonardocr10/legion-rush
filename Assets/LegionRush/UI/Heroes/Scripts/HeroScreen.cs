using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LegionRush
{
    // Tela de heróis no layout do mockup (941×1672): topo com perfil e carteira, título e abas de função,
    // vitrine do herói com painel de atributos, habilidades e MELHORAR, grade de cartas e a barra de navegação.
    public class HeroScreen : UIScreen
    {
        private static readonly Vector4 InfoBox = new Vector4(500, 345, 925, 958);
        private static readonly Vector4 ShowBox = new Vector4(0, 335, 500, 965);
        private static readonly Vector4 GridBox = new Vector4(16, 978, 925, 1496);

        // Moldura das cartas: linha de cima (4) e de baixo (5), como no mockup.
        private static readonly Vector4[] Slots =
        {
            new Vector4(25, 985, 217, 1240), new Vector4(232, 985, 438, 1240), new Vector4(456, 985, 666, 1240), new Vector4(686, 985, 913, 1240),
            new Vector4(30, 1255, 198, 1492), new Vector4(213, 1255, 389, 1492), new Vector4(404, 1255, 563, 1492),
            new Vector4(579, 1255, 738, 1492), new Vector4(754, 1255, 914, 1492)
        };

        private static readonly (string sprite, HeroRole? role)[] TabDefs =
        {
            ("Tab_All", null), ("Tab_Tank", HeroRole.Tank), ("Tab_Damage", HeroRole.Damage), ("Tab_Support", HeroRole.Support)
        };

        private MainMenuProfilePanel profile;
        private MainMenuCurrencyBar gold, gems;
        private readonly List<Image> tabs = new List<Image>();
        private HeroRole? filter;
        private string viewing;

        private RectTransform show, info, grid;

        public override bool AllowBack => true;

        protected override void Build(RectTransform root)
        {
            BuildBackground(root);

            profile = MainMenuProfilePanel.Create(root, null);
            LegionUI.Place((RectTransform)profile.transform, LegionUI.Edge.TopLeft, 14, 12, 394, 120);
            gold = MainMenuCurrencyBar.Create(root, "Icon_Coin", () => Ui.Push<ShopScreen>().Focus(ShopScreen.SectionChests), "Gold");
            LegionUI.Place((RectTransform)gold.transform, LegionUI.Edge.Top, 403, 30, 630, 92);
            gems = MainMenuCurrencyBar.Create(root, "Icon_Gem", () => Ui.Push<ShopScreen>().Focus(ShopScreen.SectionSkins), "Gems");
            LegionUI.Place((RectTransform)gems.transform, LegionUI.Edge.Top, 638, 30, 852, 92);
            Image settings = MainMenuArt.Image(root, "Button_Settings", "Settings");
            LegionUI.Place(settings.rectTransform, LegionUI.Edge.TopRight, 860, 22, 928, 92);
            MainMenuButtons.Make(settings.rectTransform, settings, () => Ui.Push<SettingsScreen>());

            Image back = HeroesArt.Image(root, "Button_Back", "Back");
            LegionUI.Place(back.rectTransform, LegionUI.Edge.TopLeft, 20, 150, 106, 236);
            MainMenuButtons.Make(back.rectTransform, back, OnBack);

            Image title = HeroesArt.Image(root, "Title_Heroes", "Title");
            LegionUI.Place(title.rectTransform, LegionUI.Edge.Top, 160, 104, 780, 258);

            float[] x0 = { 30, 254, 478, 702 };
            for (int i = 0; i < TabDefs.Length; i++)
            {
                HeroRole? role = TabDefs[i].role;
                Image tab = HeroesArt.Image(root, TabDefs[i].sprite, "Tab_" + i);
                tab.preserveAspect = false;
                LegionUI.Place(tab.rectTransform, LegionUI.Edge.Top, x0[i], 266, x0[i] + 212, 332);
                MainMenuButtons.Make(tab.rectTransform, tab, () =>
                {
                    filter = role;
                    Refresh();
                });
                tabs.Add(tab);
            }

            show = LegionUI.Place(UI.Rect(root, "Showcase"), LegionUI.Edge.Top, ShowBox.x, ShowBox.y, ShowBox.z, ShowBox.w);
            info = LegionUI.Place(UI.Rect(root, "Info"), LegionUI.Edge.Top, InfoBox.x, InfoBox.y, InfoBox.z, InfoBox.w);
            grid = LegionUI.Place(UI.Rect(root, "Grid"), LegionUI.Edge.Bottom, GridBox.x, GridBox.y, GridBox.z, GridBox.w);

            BuildNav(root);
            Economy.WalletChanged += (g, m) => { if (Root != null && Root.gameObject.activeInHierarchy) RefreshWallet(); };
        }

        private static void BuildBackground(RectTransform root)
        {
            Image fill = root.gameObject.AddComponent<Image>();
            fill.color = new Color(0.03f, 0.06f, 0.17f);
            Image scenery = LegionArt.Image(root, "Backgrounds/bg_frozen_castle", false, "Scenery");
            scenery.preserveAspect = false;
            AspectRatioFitter fitter = scenery.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = scenery.sprite != null ? scenery.sprite.rect.width / scenery.sprite.rect.height : 0.56f;
            scenery.color = new Color(0.42f, 0.5f, 0.85f);
            Image shade = UI.Panel(root, new Color(0.02f, 0.05f, 0.18f, 0.55f), "Shade");
            UI.Stretch(shade.rectTransform);
            shade.raycastTarget = false;
            UISnow.Attach(root);
        }

        private void BuildNav(RectTransform root)
        {
            (string icon, string label, MainMenuDestination destination)[] items =
            {
                ("Icon_Home", "INÍCIO", MainMenuDestination.Home), ("Icon_Heroes", "HERÓIS", MainMenuDestination.Heroes),
                ("Icon_Troops", "TROPAS", MainMenuDestination.Troops), ("Icon_BattlePass", "PASSE", MainMenuDestination.BattlePass),
                ("Icon_More", "MAIS", MainMenuDestination.More)
            };
            float[] x0 = { 10, 196, 382, 568, 754 };
            for (int i = 0; i < items.Length; i++)
            {
                MainMenuDestination destination = items[i].destination;
                MainMenuNavButton button = MainMenuNavButton.Create(root, items[i].icon, items[i].label, () => Navigate(destination));
                LegionUI.Place((RectTransform)button.transform, LegionUI.Edge.Bottom, x0[i], 1494, x0[i] + 178, 1660);
                button.SetSelected(destination == MainMenuDestination.Heroes);
            }
        }

        private void Navigate(MainMenuDestination destination)
        {
            if (destination == MainMenuDestination.Heroes) return;
            Ui.Replace<MainMenuScreen>();
            if (destination != MainMenuDestination.Home && destination != MainMenuDestination.More)
                new UIRootMainMenuNavigator(Ui).Open(destination);
        }

        // ---------------------------------------------------------------- dados

        public override void Refresh()
        {
            if (!SaveManager.IsLoaded) return;
            SaveData save = SaveManager.Current;
            if (string.IsNullOrEmpty(viewing) || MetaCatalog.Hero(viewing) == null) viewing = save.heroes.selected;

            MainMenuModel model = new GameMainMenuData(Ui.Game).Snapshot();
            profile.Set(model.PlayerName, model.PlayerLevel, model.Xp, model.XpMax);
            RefreshWallet();

            for (int i = 0; i < tabs.Count; i++)
            {
                bool on = TabDefs[i].role == filter;
                tabs[i].sprite = HeroesArt.Get(on ? "Tab_All" : TabDefs[i].sprite);
                tabs[i].color = Color.white;
                SetTabLabel(tabs[i], on && i > 0 ? Label(TabDefs[i].role) : null);
            }

            List<HeroDef> list = MetaCatalog.Heroes.FindAll(h => filter == null || h.Role == filter);
            if (list.Count > 0 && !list.Exists(h => h.Id == viewing)) viewing = list[0].Id;

            BuildCards(list, save);
            HeroDef hero = MetaCatalog.Hero(viewing);
            if (hero != null)
            {
                BuildShowcase(hero, save);
                BuildInfo(hero, save);
            }
        }

        private void RefreshWallet()
        {
            gold.Set(Economy.Gold);
            gems.Set(Economy.Gems);
        }

        private static string Label(HeroRole? role) => role switch
        {
            HeroRole.Tank => "TANQUE",
            HeroRole.Damage => "DANO",
            HeroRole.Support => "SUPORTE",
            _ => "TODOS"
        };

        // A arte da aba acesa traz "TODOS" escrito; para as outras abas, cobre o texto com o rótulo certo.
        private static void SetTabLabel(Image tab, string text)
        {
            Transform old = tab.transform.Find("Cover");
            if (old != null) UnityEngine.Object.Destroy(old.gameObject);
            if (text == null) return;
            Image cover = UI.Panel(tab.transform, new Color(0.13f, 0.55f, 1f), "Cover");
            UI.Place(cover.rectTransform, 0.12f, 0.24f, 0.88f, 0.76f);
            cover.raycastTarget = false;
            TMP_Text label = MainMenuButtons.Label(cover.transform, text, 24f, LegionTextStyle.PrimaryButton, Color.white);
            UI.Stretch(label.rectTransform);
        }

        // ---------------------------------------------------------------- cartas

        private void BuildCards(List<HeroDef> list, SaveData save)
        {
            UI.Clear(grid);
            for (int i = 0; i < list.Count && i < Slots.Length; i++)
            {
                HeroDef hero = list[i];
                HeroRecord record = save.heroes.Get(hero.Id);
                bool owned = record != null;
                Vector4 box = Slots[i];
                RectTransform card = LegionUI.Inside(UI.Rect(grid, "Card_" + hero.Id), GridBox, box.x, box.y, box.z, box.w);

                Image frame = MainMenuArt.Image(card, "Button_SideFrame", "Frame", true);
                UI.Stretch(frame.rectTransform);
                frame.color = owned ? Color.white : new Color(0.6f, 0.62f, 0.7f);

                Image portrait = HeroesArt.Image(card, hero.Portrait, "Portrait");
                portrait.preserveAspect = false;
                UI.Place(portrait.rectTransform, 0.05f, 0.03f, 0.95f, 0.97f);
                if (!owned && hero.LockedPortrait == null) portrait.color = new Color(0.62f, 0.64f, 0.72f);

                Image fade = MainMenuArt.Image(card, "Shade_Vertical", "Fade");
                fade.preserveAspect = false;
                UI.Place(fade.rectTransform, 0.05f, 0.03f, 0.95f, 0.5f);

                TMP_Text name = MainMenuButtons.Label(card, hero.Name, 23f, LegionTextStyle.PrimaryButton, Color.white);
                UI.Place(name.rectTransform, 0.04f, 0.24f, 0.96f, 0.37f);

                if (owned)
                {
                    TMP_Text level = MainMenuButtons.Label(card, "Nível " + record.level, 19f, LegionTextStyle.Hud, new Color(1f, 0.84f, 0.25f));
                    UI.Place(level.rectTransform, 0.04f, 0.13f, 0.96f, 0.25f);
                    Stars(card, Stars(record.level), 5, new Vector4(0.12f, 0.03f, 0.88f, 0.13f));
                    if (save.heroes.selected == hero.Id)
                    {
                        Image crown = HeroesArt.Image(card, "Icon_Crown", "Leader");
                        UI.Place(crown.rectTransform, 0.74f, 0.84f, 0.97f, 0.99f);
                    }
                }
                else
                {
                    if (hero.LockedPortrait == null)
                    {
                        Image lockIcon = HeroesArt.Image(card, "Icon_Lock", "Lock");
                        UI.Place(lockIcon.rectTransform, 0.72f, 0.8f, 0.96f, 0.98f);
                    }
                    Stars(card, 1, 3, new Vector4(0.25f, 0.14f, 0.75f, 0.23f));
                    Image pill = UI.Panel(card, new Color(0.04f, 0.07f, 0.18f, 0.92f), "Price");
                    UI.Place(pill.rectTransform, 0.14f, 0.04f, 0.86f, 0.13f);
                    pill.raycastTarget = false;
                    TMP_Text price = MainMenuButtons.Label(pill.transform, PriceText(hero), 17f, LegionTextStyle.Hud, Color.white);
                    UI.Stretch(price.rectTransform);
                }

                if (hero.Id == viewing)
                {
                    Image glow = LegionArt.Image(card, "Frames/frame_gold", false, "Selected");
                    glow.type = Image.Type.Sliced;
                    glow.fillCenter = false;
                    glow.pixelsPerUnitMultiplier = 2.2f;
                    UI.Stretch(glow.rectTransform, -6f, -6f, -6f, -6f);
                    glow.raycastTarget = false;
                }

                string id = hero.Id;
                MainMenuButtons.Make(card, frame, () =>
                {
                    viewing = id;
                    Refresh();
                });
            }
        }

        private static int Stars(int level) => Mathf.Clamp(1 + (level - 1) / 2, 1, 5);

        private static void Stars(RectTransform parent, int filled, int total, Vector4 area)
        {
            RectTransform row = UI.Rect(parent, "Stars");
            UI.Place(row, area.x, area.y, area.z, area.w);
            for (int i = 0; i < total; i++)
            {
                Image star = HeroesArt.Image(row, i < filled ? "Star_Gold" : "Star_Blue", "Star" + i);
                UI.Place(star.rectTransform, i / (float)total, 0f, (i + 1) / (float)total, 1f);
            }
        }

        private static string PriceText(HeroDef hero)
        {
            if (hero.Gems > 0) return MainMenuCurrencyBar.Short(hero.Gems) + " GEMAS";
            return hero.Gold > 0 ? MainMenuCurrencyBar.Short(hero.Gold) + " OURO" : "GRÁTIS";
        }

        // ---------------------------------------------------------------- vitrine

        private void BuildShowcase(HeroDef hero, SaveData save)
        {
            UI.Clear(show);
            bool owned = save.heroes.Get(hero.Id) != null;
            Image art = HeroesArt.Image(show, hero.Showcase ?? hero.Portrait, "Hero");
            LegionUI.Inside(art.rectTransform, ShowBox, -40, 350, 540, 940);
            if (!owned && hero.LockedPortrait == null) art.color = new Color(0.45f, 0.47f, 0.55f);

            List<HeroDef> list = MetaCatalog.Heroes.FindAll(h => filter == null || h.Role == filter);
            int index = list.FindIndex(h => h.Id == hero.Id);
            Arrow(-1, list, index, new Vector4(32, 612, 82, 680));
            Arrow(1, list, index, new Vector4(452, 612, 502, 680));

            if (owned && save.heroes.selected != hero.Id)
            {
                Button lead = UI.Button(show, "LIDERAR", UIStyle.Blue, () =>
                {
                    MetaService.SelectHero(hero.Id);
                    Ui.Toast(hero.Name + " lidera a legião");
                    Ui.Game.Restart();
                    Refresh();
                }, Mathf.RoundToInt(26 * LegionUI.Px), "Lead");
                LegionUI.Inside((RectTransform)lead.transform, ShowBox, 150, 885, 350, 945);
            }
        }

        private void Arrow(int step, List<HeroDef> list, int index, Vector4 box)
        {
            Image frame = MainMenuArt.Image(show, "Button_SideFrame", step < 0 ? "Prev" : "Next", true);
            LegionUI.Inside(frame.rectTransform, ShowBox, box.x, box.y, box.z, box.w);
            TMP_Text glyph = MainMenuButtons.Label(frame.transform, step < 0 ? "<" : ">", 40f, LegionTextStyle.PrimaryButton, Color.white);
            UI.Stretch(glyph.rectTransform);
            MainMenuButtons.Make(frame.rectTransform, frame, () =>
            {
                if (list.Count == 0) return;
                viewing = list[(index + step + list.Count) % list.Count].Id;
                Refresh();
            });
        }

        // ---------------------------------------------------------------- painel

        private void BuildInfo(HeroDef hero, SaveData save)
        {
            UI.Clear(info);
            HeroRecord record = save.heroes.Get(hero.Id);
            bool owned = record != null;
            int level = owned ? record.level : 1;

            Image panel = MainMenuArt.Image(info, "Button_SideFrame", "Panel", true);
            UI.Stretch(panel.rectTransform);

            Image crown = HeroesArt.Image(info, "Icon_Crown", "Crown");
            LegionUI.Inside(crown.rectTransform, InfoBox, 520, 356, 566, 396);
            Text(hero.Name, 520 + 54, 352, 912, 402, 34f, LegionTextStyle.PrimaryButton, Color.white, TextAnchor.MiddleLeft);

            Chip(MetaCatalog.RarityName(hero.Rarity).ToUpperInvariant(), MetaCatalog.RarityColor(hero.Rarity), 522, 402, 614, 438);
            Chip(Label(hero.Role), new Color(0.12f, 0.4f, 0.85f), 622, 402, 738, 438);
            Text(owned ? "Nível " + level : "BLOQUEADO", 744, 402, 908, 438, 24f, LegionTextStyle.Hud, new Color(1f, 0.84f, 0.25f), TextAnchor.MiddleRight);

            RectTransform stars = LegionUI.Inside(UI.Rect(info, "Stars"), InfoBox, 524, 446, 708, 486);
            Stars(stars, owned ? Stars(level) : 0, 5, new Vector4(0f, 0f, 1f, 1f));

            Image statsBox = MainMenuArt.Image(info, "Button_SideFrame", "StatsBox", true);
            LegionUI.Inside(statsBox.rectTransform, InfoBox, 520, 496, 910, 662);
            statsBox.color = new Color(0.55f, 0.62f, 0.85f);
            UnitStats basis = ArmyConfig.DefaultStats(hero.Class);
            float scale = 1f + 0.1f * (level - 1);
            float next = 1f + 0.1f * level;
            Stat("Icon_Health", "Vida", basis.maxHealth * 50f, scale, next, 0);
            Stat("Icon_Damage", "Dano", basis.damage * 12f, scale, next, 1);
            Stat("Icon_Defense", "Defesa", basis.armor * 4f, scale, next, 2);

            (string icon, string name)[] skills = Skills(hero);
            int[] divisors = { 3, 4, 5, 6 };
            for (int i = 0; i < skills.Length; i++)
            {
                float x0 = 522 + i * 99.5f;
                Image icon = HeroesArt.Image(info, skills[i].icon, "Skill" + i);
                LegionUI.Inside(icon.rectTransform, InfoBox, x0, 676, x0 + 90, 770);
                if (!owned) icon.color = new Color(0.5f, 0.52f, 0.6f);
                Text(skills[i].name, x0 - 6, 772, x0 + 96, 800, 17f, LegionTextStyle.Menu, Color.white, TextAnchor.MiddleCenter);
                Text("Nível " + (1 + (level - 1) / divisors[i]), x0 - 6, 798, x0 + 96, 822, 15f, LegionTextStyle.Hud,
                    new Color(0.55f, 0.85f, 1f), TextAnchor.MiddleCenter);
            }

            Image button = HeroesArt.Image(info, "Button_UpgradeBlank", "Upgrade");
            button.preserveAspect = false;
            LegionUI.Inside(button.rectTransform, InfoBox, 518, 832, 912, 948);
            bool max = owned && level >= 10;
            int cost = hero.LevelCost(level);
            string title = !owned ? "DESBLOQUEAR" : max ? "NÍVEL MÁX." : "MELHORAR";
            string price = !owned ? PriceText(hero) : max ? string.Empty : MainMenuCurrencyBar.Short(cost);
            TMP_Text head = MainMenuButtons.Label(button.transform, title, owned ? 31f : 25f, LegionTextStyle.PrimaryButton, new Color(0.25f, 0.12f, 0.02f));
            UI.Place(head.rectTransform, 0.29f, 0.42f, 0.77f, 0.86f);
            if (price.Length > 0)
            {
                TMP_Text sub = MainMenuButtons.Label(button.transform, price, 23f, LegionTextStyle.Hud, new Color(0.25f, 0.12f, 0.02f));
                UI.Place(sub.rectTransform, 0.27f, 0.12f, 0.79f, 0.44f);
            }
            if (max) button.color = new Color(0.6f, 0.6f, 0.6f);
            MainMenuButtons.Make(button.rectTransform, button, () =>
            {
                if (!owned) Buy(hero.Name, hero.Gold, hero.Gems, () => MetaService.UnlockHero(hero.Id), hero.Name + " entrou na legião!");
                else if (!max) Buy($"{hero.Name} nível {level + 1}", cost, 0, () => MetaService.LevelUpHero(hero.Id), null);
            });
        }

        private void Stat(string icon, string name, float value, float scale, float next, int row)
        {
            float y0 = 503 + row * 52;
            Image art = HeroesArt.Image(info, icon, "Stat" + row);
            LegionUI.Inside(art.rectTransform, InfoBox, 536, y0 + 4, 584, y0 + 46);
            Text(name, 598, y0, 720, y0 + 50, 23f, LegionTextStyle.Menu, new Color(0.82f, 0.88f, 1f), TextAnchor.MiddleLeft);
            int now = Mathf.RoundToInt(value * scale);
            int gain = Mathf.RoundToInt(value * next) - now;
            Text(now.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("pt-BR")), 700, y0, 790, y0 + 50, 26f,
                LegionTextStyle.Hud, Color.white, TextAnchor.MiddleRight);
            Text("+" + gain, 800, y0, 900, y0 + 50, 26f, LegionTextStyle.Positive, new Color(0.45f, 1f, 0.4f), TextAnchor.MiddleLeft);
        }

        private TMP_Text Text(string text, float x0, float y0, float x1, float y1, float size, LegionTextStyle style, Color color, TextAnchor anchor)
            => LegionUI.Text(info, InfoBox, x0, y0, x1, y1, text, size, style, color, anchor);

        private void Chip(string text, Color color, float x0, float y0, float x1, float y1)
        {
            Image chip = UI.Panel(info, color, "Chip");
            LegionUI.Inside(chip.rectTransform, InfoBox, x0, y0, x1, y1);
            chip.raycastTarget = false;
            TMP_Text label = MainMenuButtons.Label(chip.transform, text, 20f, LegionTextStyle.PrimaryButton, Color.white);
            UI.Stretch(label.rectTransform);
        }

        private static (string, string)[] Skills(HeroDef hero) => hero.Role switch
        {
            HeroRole.Tank => new[] { ("Skill_RoyalStrike", "Golpe Real"), ("Skill_Leadership", "Liderança"), ("Skill_RoyalDefense", "Defesa Real"), ("Skill_Rally", "Rally") },
            HeroRole.Support => new[] { ("Skill_Green", "Cura"), ("Skill_Gold", "Bênção"), ("Skill_RoyalDefense", "Proteção"), ("Skill_Rally", "Rally") },
            _ => hero.Class == UnitClass.Mage
                ? new[] { ("Skill_Purple", "Arcano"), ("Skill_Blue", "Gelo"), ("Skill_Gold", "Chama"), ("Skill_Rally", "Rally") }
                : new[] { ("Skill_RoyalStrike", "Golpe"), ("Skill_Blue", "Rajada"), ("Skill_Gold", "Precisão"), ("Skill_Rally", "Rally") }
        };

        private void Buy(string item, int goldCost, int gemCost, Func<PurchaseResult> purchase, string success)
        {
            if (!Economy.CanAfford(goldCost, gemCost))
            {
                Ui.Toast("Saldo insuficiente");
                GameAudio.Play(SoundId.UiBack);
                return;
            }
            string price = gemCost > 0 ? $"{gemCost} GEMAS" : $"{goldCost} OURO";
            Ui.Confirm("CONFIRMAR COMPRA", $"{item}\npor {price}?", "COMPRAR", () =>
            {
                PurchaseResult result = purchase();
                GameAudio.Play(result == PurchaseResult.Success ? SoundId.UiConfirm : SoundId.UiBack);
                Ui.Toast(result == PurchaseResult.Success && success != null ? success : MetaService.Explain(result));
                Refresh();
            });
        }
    }
}
