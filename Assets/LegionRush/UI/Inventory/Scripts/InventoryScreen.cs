using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LegionRush
{
    // Inventário no layout do mockup: abas EQUIPAMENTOS/ITENS/FRAGMENTOS/SKINS/OUTROS, vitrine do rei com os espaços
    // equipados, detalhe do item escolhido (raridade, estrelas, atributos, ações) e grade 4×5 com filtro de raridade.
    // Equipamentos, poções e visuais são os do jogo; o que ainda não existe (fragmentos, venda) avisa que chega em breve.
    public class InventoryScreen : UIScreen
    {
        private static readonly Vector4 Box = new Vector4(0, 0, LegionUI.RefWidth, LegionUI.RefHeight);
        private static readonly (string label, string icon)[] TabDefs =
        {
            ("EQUIPAMENTOS", "Icons/sword"), ("ITENS", "Icons/potion_red"), ("FRAGMENTOS", "Icons/crystal"), ("SKINS", "Icons/helmet"), ("OUTROS", "Chests/chest_wood")
        };
        private static readonly string[] FilterNames = { "Raridade", "Comum", "Raro", "Épico", "Lendário" };

        private MenuChrome chrome;
        private RectTransform body;
        private readonly List<Image> tabs = new List<Image>();
        private int tab;
        private int filter;
        private string selected;

        private class Item
        {
            public string Id, Name, Icon, Detail;
            public TroopRarity Rarity;
            public int Count = 1;
            public bool Active;
            public (string icon, string label, string value)[] Stats;
            public string Action;
            public Action Use;
        }

        protected override void Build(RectTransform root)
        {
            chrome = MenuChrome.Build(root, Ui, "INVENTÁRIO", OnBack, 600f);
            RectTransform top = LegionUI.Place(UI.Rect(root, "Tabs"), LegionUI.Edge.Top, 0, 0, LegionUI.RefWidth, LegionUI.RefHeight);
            float[] x0 = { 18, 208, 384, 562, 746 };
            float[] x1 = { 204, 380, 558, 742, 922 };
            for (int i = 0; i < TabDefs.Length; i++)
            {
                int index = i;
                Image button = MenuChrome.Panel(top, Box, x0[i], 298, x1[i], 418, "Tab" + i);
                Image icon = LegionArt.Image(button.transform, TabDefs[i].icon, false, "Icon");
                icon.preserveAspect = true;
                UI.Place(icon.rectTransform, 0.3f, 0.4f, 0.7f, 0.92f);
                TMP_Text label = MainMenuButtons.Label(button.transform, TabDefs[i].label, 20f, LegionTextStyle.PrimaryButton, Color.white);
                UI.Place(label.rectTransform, 0.03f, 0.05f, 0.97f, 0.38f);
                MainMenuButtons.Make(button.rectTransform, button, () =>
                {
                    tab = index;
                    selected = null;
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
                tabs[i].sprite = MainMenuArt.Get(i == tab ? "Button_NavFrameSelected" : "Button_SideFrame");

            UI.Clear(body);
            RectTransform area = LegionUI.Place(UI.Rect(body, "Area"), LegionUI.Edge.Top, 0, 0, LegionUI.RefWidth, LegionUI.RefHeight);
            List<Item> items = Items(tab);
            List<Item> shown = items.FindAll(i => filter == 0 || (int)i.Rarity == filter - 1);
            if (selected == null || !shown.Exists(i => i.Id == selected)) selected = shown.Count > 0 ? shown[0].Id : null;
            Item current = shown.Find(i => i.Id == selected);

            Showcase(area);
            Detail(area, current);
            Grid(area, shown, items.Count);
        }

        // ---------------------------------------------------------------- vitrine

        private void Showcase(RectTransform parent)
        {
            Image panel = MenuChrome.Panel(parent, Box, 18, 428, 500, 1000, "Showcase");
            Image hero = HeroesArt.Image(parent, "Showcase_King", "Hero");
            LegionUI.Inside(hero.rectTransform, Box, 110, 470, 410, 990);

            // Espaços do jogo: arma, armadura e relíquia (cada um mostra o item equipado ou fica vazio).
            (EquipSlot slot, float x0, float y0)[] slots = { (EquipSlot.Weapon, 30, 492), (EquipSlot.Armor, 388, 492), (EquipSlot.Relic, 388, 656) };
            foreach (var s in slots)
            {
                EquipmentDef equipped = MetaCatalog.Equipment.Find(e => e.Slot == s.slot && MetaService.IsEquipped(e.Id));
                Image frame = RarityTile(parent, s.x0, s.y0, s.x0 + 110, s.y0 + 116, equipped != null ? equipped.Rarity : (TroopRarity?)null);
                MenuChrome.Art(parent, Box, s.x0 + 12, s.y0 + 10, s.x0 + 98, s.y0 + 100, equipped != null ? SlotIcon(equipped.Slot) : SlotIcon(s.slot));
                if (equipped == null) frame.color = new Color(0.4f, 0.45f, 0.6f, 0.9f);
                string id = equipped?.Id;
                if (id != null)
                    MainMenuButtons.Make(frame.rectTransform, frame, () =>
                    {
                        tab = 0;
                        selected = id;
                        Refresh();
                    });
            }
        }

        private Image RarityTile(RectTransform parent, float x0, float y0, float x1, float y1, TroopRarity? rarity)
        {
            Image tile = UI.Panel(parent, rarity.HasValue ? RarityBack(rarity.Value) : new Color(0.05f, 0.09f, 0.22f, 0.95f), "Tile");
            LegionUI.Inside(tile.rectTransform, Box, x0, y0, x1, y1);
            Outline ring = tile.gameObject.AddComponent<Outline>();
            ring.effectColor = rarity.HasValue ? Color.Lerp(RarityBack(rarity.Value), Color.white, 0.5f) : new Color(0.25f, 0.4f, 0.75f, 0.7f);
            ring.effectDistance = new Vector2(3f, -3f);
            return tile;
        }

        // ---------------------------------------------------------------- detalhe

        private void Detail(RectTransform parent, Item item)
        {
            MenuChrome.Panel(parent, Box, 18, 1004, 494, 1452, "Detail");
            if (item == null)
            {
                MenuChrome.Text(parent, Box, 40, 1150, 470, 1300, EmptyMessage(), 22f, LegionTextStyle.Menu, Color.white, TextAnchor.MiddleCenter);
                return;
            }
            RarityTile(parent, 30, 1016, 160, 1144, item.Rarity);
            MenuChrome.Art(parent, Box, 42, 1026, 148, 1134, item.Icon);
            if (item.Count > 1)
                MenuChrome.Text(parent, Box, 100, 1108, 158, 1142, "x" + item.Count, 20f, LegionTextStyle.Hud, Color.white, TextAnchor.MiddleRight);

            MenuChrome.Text(parent, Box, 172, 1018, 486, 1060, item.Name, 28f, LegionTextStyle.PrimaryButton, MenuChrome.GoldText);
            MenuChrome.Text(parent, Box, 172, 1058, 486, 1090, MetaCatalog.RarityName(item.Rarity), 22f, LegionTextStyle.Menu,
                Color.Lerp(MetaCatalog.RarityColor(item.Rarity), Color.white, 0.25f));
            Stars(parent, 172, 1094, (int)item.Rarity + 2);

            MenuChrome.Text(parent, Box, 34, 1146, 486, 1184, item.Detail, 19f, LegionTextStyle.Body, new Color(0.8f, 0.88f, 1f));
            Image header = UI.Panel(parent, new Color(0.03f, 0.07f, 0.18f, 0.85f), "Header");
            LegionUI.Inside(header.rectTransform, Box, 30, 1192, 482, 1228);
            MenuChrome.Text(parent, Box, 40, 1192, 470, 1228, "Atributos", 22f, LegionTextStyle.PrimaryButton, new Color(0.45f, 0.75f, 1f));
            for (int i = 0; i < item.Stats.Length && i < 3; i++)
            {
                float y = 1236 + i * 38;
                MenuChrome.Art(parent, Box, 40, y + 2, 72, y + 34, item.Stats[i].icon);
                MenuChrome.Text(parent, Box, 82, y, 330, y + 36, item.Stats[i].label, 21f, LegionTextStyle.Menu, Color.white);
                MenuChrome.Text(parent, Box, 300, y, 470, y + 36, item.Stats[i].value, 23f, LegionTextStyle.Positive, new Color(0.45f, 1f, 0.4f),
                    TextAnchor.MiddleRight);
            }

            MenuChrome.Button(parent, Box, 34, 1362, 246, 1436, "MELHORAR", MenuChrome.Blue,
                () => Ui.Info("MELHORAR", "Itens ainda não sobem de nível. Para ficar mais forte, use as Melhorias e abra baús na loja."), 30f);
            bool actionable = item.Use != null;
            MenuChrome.Button(parent, Box, 262, 1362, 480, 1436, item.Action, actionable ? MenuChrome.GoldColor : MenuChrome.Gray,
                actionable ? item.Use : null, 30f);
        }

        private void Stars(RectTransform parent, float x0, float y0, int filled)
        {
            for (int i = 0; i < 5; i++)
                MenuChrome.Art(parent, Box, x0 + i * 30, y0, x0 + i * 30 + 28, y0 + 28, i < filled ? "Badges/star" : "Badges/star_empty");
        }

        // ---------------------------------------------------------------- grade

        private void Grid(RectTransform parent, List<Item> items, int total)
        {
            MenuChrome.Panel(parent, Box, 506, 428, 922, 1452, "Grid");
            MenuChrome.Button(parent, Box, 520, 446, 722, 504, FilterNames[filter] + "  <size=70%>v</size>", new Color(0.1f, 0.3f, 0.75f), CycleFilter, 24f);
            Image capacity = UI.Panel(parent, new Color(0.03f, 0.07f, 0.18f, 0.95f), "Capacity");
            LegionUI.Inside(capacity.rectTransform, Box, 742, 446, 912, 504);
            MenuChrome.Text(parent, Box, 748, 446, 860, 504, $"{total}/200", 24f, LegionTextStyle.Hud, Color.white, TextAnchor.MiddleCenter);
            Image plus = MainMenuArt.Image(parent, "Button_Add", "Add");
            LegionUI.Inside(plus.rectTransform, Box, 864, 452, 908, 498);
            MainMenuButtons.Make(plus.rectTransform, plus, () => Ui.Push<ShopScreen>().Focus(ShopScreen.SectionChests));

            float[] xs = { 516, 619, 722, 825 };
            for (int i = 0; i < 20; i++)
            {
                float x0 = xs[i % 4];
                float y0 = 522 + (i / 4) * 150;
                if (i >= items.Count)
                {
                    RarityTile(parent, x0, y0, x0 + 94, y0 + 128, null);
                    continue;
                }
                Item item = items[i];
                Image tile = RarityTile(parent, x0, y0, x0 + 94, y0 + 128, item.Rarity);
                if (item.Id == selected)
                {
                    Outline ring = tile.GetComponent<Outline>();
                    ring.effectColor = new Color(1f, 0.95f, 0.5f);
                    ring.effectDistance = new Vector2(5f, -5f);
                }
                MenuChrome.Art(parent, Box, x0 + 10, y0 + 6, x0 + 84, y0 + 92, item.Icon);
                int stars = Mathf.Clamp((int)item.Rarity + 1, 1, 4);
                for (int s = 0; s < stars; s++)
                {
                    float sx = x0 + 47 - stars * 10 + s * 20;
                    MenuChrome.Art(parent, Box, sx, y0 + 96, sx + 20, y0 + 118, "Badges/star");
                }
                if (item.Count > 1)
                    MenuChrome.Text(parent, Box, x0 + 40, y0 + 4, x0 + 90, y0 + 30, "x" + item.Count, 18f, LegionTextStyle.Hud, Color.white, TextAnchor.MiddleRight);
                if (item.Active) MenuChrome.Art(parent, Box, x0 + 60, y0 + 4, x0 + 90, y0 + 34, "Badges/check");
                string id = item.Id;
                MainMenuButtons.Make(tile.rectTransform, tile, () =>
                {
                    selected = id;
                    Refresh();
                });
            }

            MenuChrome.Button(parent, Box, 512, 1338, 628, 1446, "VENDER", new Color(0.1f, 0.3f, 0.75f),
                () => Ui.Info("VENDER", "A venda de itens chega em uma próxima atualização."), 22f);
            MenuChrome.Button(parent, Box, 642, 1338, 778, 1446, "DESMONTAR", new Color(0.1f, 0.3f, 0.75f),
                () => Ui.Info("DESMONTAR", "Desmontar itens em fragmentos chega em uma próxima atualização."), 22f);
            MenuChrome.Button(parent, Box, 792, 1338, 918, 1446, "FILTRO", new Color(0.1f, 0.3f, 0.75f), CycleFilter, 22f);
        }

        private void CycleFilter()
        {
            filter = (filter + 1) % FilterNames.Length;
            Refresh();
        }

        // ---------------------------------------------------------------- dados

        private List<Item> Items(int index)
        {
            var list = new List<Item>();
            SaveData save = SaveManager.Current;
            switch (index)
            {
                case 0:
                    foreach (EquipmentDef e in MetaCatalog.Equipment)
                    {
                        if (!MetaService.OwnsItem(e.Id)) continue;
                        EquipmentDef def = e;
                        bool equipped = MetaService.IsEquipped(def.Id);
                        list.Add(new Item
                        {
                            Id = def.Id, Name = def.Name, Icon = SlotIcon(def.Slot), Rarity = def.Rarity, Active = equipped,
                            Detail = SlotName(def.Slot) + (equipped ? " · equipado" : string.Empty),
                            Stats = new[] { ("Icons/sword", "Bônus", def.Bonus.Describe()) },
                            Action = equipped ? "REMOVER" : "EQUIPAR",
                            Use = () =>
                            {
                                MetaService.ToggleEquip(def.Id);
                                Ui.Game?.Restart();
                                Ui.Toast(MetaService.IsEquipped(def.Id) ? def.Name + " equipado" : def.Name + " removido");
                                Ui.RefreshTop();
                            }
                        });
                    }
                    list.Sort((a, b) => b.Rarity.CompareTo(a.Rarity));
                    break;
                case 1:
                    string[] art = { "Icons/potion_red", "Icons/potion_green", "Icons/potion_blue" };
                    for (int i = 0; i < MetaCatalog.Potions.Count; i++)
                    {
                        PotionDef p = MetaCatalog.Potions[i];
                        int count = save.inventory.Count(p.Id);
                        if (count <= 0) continue;
                        bool active = save.inventory.activePotions.Contains(p.Id);
                        list.Add(new Item
                        {
                            Id = p.Id, Name = p.Name, Icon = art[i % art.Length], Rarity = TroopRarity.Rare, Count = count, Active = active,
                            Detail = active ? "Ativa na próxima fase" : "Vale para a próxima fase",
                            Stats = new[] { ("Icons/potion_green", "Efeito", p.Effect.Describe()) },
                            Action = active ? "ATIVA" : "USAR",
                            Use = active ? null : (Action)(() =>
                            {
                                if (MetaService.ActivatePotion(p.Id))
                                {
                                    Ui.Toast("Poção ativada para a próxima fase");
                                    Ui.Game?.Restart();
                                }
                                Ui.RefreshTop();
                            })
                        });
                    }
                    break;
                case 3:
                    foreach (SkinDef skin in MetaCatalog.Skins)
                    {
                        if (!save.cosmetics.owned.Contains(skin.Id)) continue;
                        SkinDef s = skin;
                        bool inUse = save.cosmetics.selected == s.Id;
                        list.Add(new Item
                        {
                            Id = s.Id, Name = s.Name, Icon = "Icons/helmet", Rarity = TroopRarity.Epic, Active = inUse,
                            Detail = "Cores das tropas" + (inUse ? " · em uso" : string.Empty),
                            Stats = new[] { ("Icons/star_badge", "Visual", "Exército") },
                            Action = inUse ? "EM USO" : "USAR",
                            Use = inUse ? null : (Action)(() =>
                            {
                                MetaService.SelectSkin(s.Id);
                                Ui.Game?.Restart();
                                Ui.RefreshTop();
                            })
                        });
                    }
                    break;
            }
            return list;
        }

        private string EmptyMessage() => tab switch
        {
            0 => "Nenhum equipamento ainda.\nCompre na loja ou abra baús.",
            1 => "Nenhuma poção.\nCompre na loja ou ganhe nos baús.",
            2 => "Fragmentos chegam em uma próxima atualização.",
            3 => "Nenhum visual comprado.\nVeja a aba GEMAS da loja.",
            _ => "Nada por aqui ainda."
        };

        private static string SlotIcon(EquipSlot slot) => slot switch
        {
            EquipSlot.Weapon => "Icons/sword",
            EquipSlot.Armor => "Icons/armor",
            _ => "Icons/crystal"
        };

        private static string SlotName(EquipSlot slot) => slot switch
        {
            EquipSlot.Weapon => "Arma",
            EquipSlot.Armor => "Armadura",
            _ => "Relíquia"
        };

        private static Color RarityBack(TroopRarity rarity) => rarity switch
        {
            TroopRarity.Legendary => new Color(0.85f, 0.42f, 0.08f),
            TroopRarity.Epic => new Color(0.45f, 0.2f, 0.8f),
            TroopRarity.Rare => new Color(0.12f, 0.38f, 0.85f),
            _ => new Color(0.4f, 0.44f, 0.52f)
        };
    }
}
