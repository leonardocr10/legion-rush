using UnityEngine;
using UnityEngine.UI;

namespace LegionRush
{
    // 5. Seleção e evolução de heróis
    public class HeroScreen : MenuScreen
    {
        private RectTransform list;
        protected override string Title => "HERÓIS";
        protected override void BuildBody(RectTransform body) => list = UI.ScrollList(body);

        protected override void RefreshBody()
        {
            UI.Clear(list);
            SaveData save = SaveManager.Current;
            float power = 1f + 0.1f * MetaService.UpgradeLevel("hero_power");
            foreach (HeroDef hero in MetaCatalog.Heroes)
            {
                HeroRecord record = save.heroes.Get(hero.Id);
                bool owned = record != null;
                bool selected = save.heroes.selected == hero.Id;
                int level = owned ? record.level : 1;
                string passive = hero.Passive.Describe((1f + hero.PerLevel * (level - 1)) * power);
                string subtitle = $"{hero.Title} · {MetaCatalog.RarityName(hero.Rarity)} · {ArmyConfig.ClassName(hero.Class)}\n" +
                                  (owned ? $"Nível {level}/10 · Passiva: {passive}" : $"Passiva: {passive}");
                string id = hero.Id;
                HeroDef captured = hero;
                RectTransform row = UI.Row(list, 250f, UIStyle.Panel);
                Image stripe = UI.Panel(row, MetaCatalog.RarityColor(hero.Rarity), "Rarity");
                UI.Place(stripe.rectTransform, 0.01f, 0.1f, 0.025f, 0.9f);
                Text name = UI.Label(row, hero.Name + (selected ? "  (EM CAMPO)" : string.Empty), 48, selected ? UIStyle.Gold : Color.white, TextAnchor.MiddleLeft);
                UI.Place(name.rectTransform, 0.045f, 0.62f, 0.98f, 0.95f);
                Text detail = UI.Label(row, subtitle, 30, UIStyle.TextDim, TextAnchor.UpperLeft, false);
                UI.Place(detail.rectTransform, 0.045f, 0.3f, 0.98f, 0.62f);

                if (!owned)
                {
                    Button unlock = UI.Button(row, "DESBLOQUEAR\n" + Price(hero.Gold, hero.Gems), UIStyle.Gold,
                        () => ConfirmPurchase("Herói " + captured.Name, captured.Gold, captured.Gems, () => MetaService.UnlockHero(id), captured.Name + " entrou na legião!"), 30);
                    UI.Place((RectTransform)unlock.transform, 0.5f, 0.04f, 0.985f, 0.3f);
                    continue;
                }
                Button select = UI.Button(row, selected ? "SELECIONADO" : "SELECIONAR", selected ? UIStyle.Gray : UIStyle.Blue, () =>
                {
                    MetaService.SelectHero(id);
                    Ui.Toast(captured.Name + " lidera a legião");
                    Game.Restart();
                    Ui.RefreshTop();
                }, 32);
                UI.Place((RectTransform)select.transform, 0.045f, 0.04f, 0.48f, 0.3f);
                select.interactable = !selected;
                bool max = level >= 10;
                int cost = hero.LevelCost(level);
                Button upgrade = UI.Button(row, max ? "NÍVEL MÁX." : $"MELHORAR\n{cost} OURO", max ? UIStyle.Gray : UIStyle.Green,
                    () => ConfirmPurchase($"{captured.Name} nível {level + 1}", cost, 0, () => MetaService.LevelUpHero(id)), 30);
                UI.Place((RectTransform)upgrade.transform, 0.52f, 0.04f, 0.985f, 0.3f);
                upgrade.interactable = !max;
            }
        }
    }

    // 6. Gerenciamento de tropas (desbloqueio, nível e composição)
    public class TroopScreen : MenuScreen
    {
        private RectTransform list;
        protected override string Title => "TROPAS";
        protected override void BuildBody(RectTransform body) => list = UI.ScrollList(body);

        protected override void RefreshBody()
        {
            UI.Clear(list);
            Section(list, "COMPOSIÇÃO INICIAL");
            RectTransform compositions = UI.Row(list, 130f);
            int count = Game.CompositionCount;
            for (int i = 0; i < count; i++)
            {
                int index = i;
                bool selected = i == Game.CompositionIndex;
                bool available = Game.IsCompositionAvailable(i);
                Button button = UI.Button(compositions, Game.CompositionName(i).ToUpperInvariant(), selected ? UIStyle.Gold : UIStyle.Blue, () =>
                {
                    Game.SetComposition(index);
                    Ui.RefreshTop();
                }, 30);
                UI.Place((RectTransform)button.transform, i / (float)count + 0.005f, 0.05f, (i + 1) / (float)count - 0.005f, 0.95f);
                button.interactable = available;
            }

            Section(list, "CLASSES");
            SaveData save = SaveManager.Current;
            ArmyConfig config = ArmyConfig.CreateDefault();
            for (int c = 0; c < ArmyConfig.ClassCount; c++)
            {
                var unitClass = (UnitClass)c;
                UnitStats stats = config.Get(unitClass);
                bool unlocked = save.troops.unlocked[c];
                int level = save.troops.levels[c];
                string role = stats.role == TroopRole.Front ? "Linha de frente" : "Retaguarda";
                string subtitle = $"{role} · {MetaCatalog.RarityName(stats.rarity)} · Nível {level}/{Game.MaxTroopLevel}\n" +
                                  $"Vida {stats.maxHealth:0} · {(stats.attackKind == AttackKind.Heal ? "Cura" : "Dano")} {stats.damage:0} · Alcance {stats.attackRange:0.#} · Armadura {stats.armor:0}";
                if (!unlocked)
                {
                    int gold = MetaCatalog.ClassUnlockGold[c];
                    int gems = MetaCatalog.ClassUnlockGems[c];
                    ItemRow(list, ArmyConfig.ClassName(unitClass), subtitle, "LIBERAR\n" + Price(gold, gems), UIStyle.Gold,
                        () => ConfirmPurchase("Classe " + ArmyConfig.ClassName(unitClass), gold, gems, () => MetaService.UnlockClass(unitClass)),
                        stats.color, true, 210f);
                    continue;
                }
                bool max = level >= Game.MaxTroopLevel;
                int cost = MetaCatalog.TroopLevelCost(level);
                ItemRow(list, ArmyConfig.ClassName(unitClass), subtitle, max ? "NÍVEL MÁX." : $"NÍVEL {level + 1}\n{cost} OURO",
                    max ? UIStyle.Gray : UIStyle.Green,
                    () => ConfirmPurchase($"{ArmyConfig.ClassName(unitClass)} nível {level + 1}", cost, 0, () => MetaService.UpgradeTroop(unitClass, Game.MaxTroopLevel)),
                    stats.color, !max, 210f);
            }
            Object.Destroy(config);
        }
    }

    // 9. Melhorias permanentes
    public class UpgradeScreen : MenuScreen
    {
        private RectTransform list;
        protected override string Title => "MELHORIAS";
        protected override void BuildBody(RectTransform body) => list = UI.ScrollList(body);

        protected override void RefreshBody()
        {
            UI.Clear(list);
            foreach (UpgradeDef upgrade in MetaCatalog.Upgrades)
            {
                int level = MetaService.UpgradeLevel(upgrade.Id);
                bool max = level >= upgrade.MaxLevel;
                int cost = upgrade.CostFor(level);
                string effect = upgrade.Id == "hero_power" ? $"+{level * 10}% na passiva do herói" : upgrade.PerLevel.Describe(level);
                string next = max ? string.Empty : upgrade.Id == "hero_power" ? " → +10% por nível" : $" → {upgrade.PerLevel.Describe(level + 1)}";
                string id = upgrade.Id;
                UpgradeDef captured = upgrade;
                ItemRow(list, $"{upgrade.Name}  {level}/{upgrade.MaxLevel}", (level == 0 ? "Sem bônus" : effect) + next,
                    max ? "MÁXIMO" : $"{cost} OURO", max ? UIStyle.Gray : UIStyle.Green,
                    () => ConfirmPurchase(captured.Name, cost, 0, () => MetaService.BuyUpgrade(id)), UIStyle.Gold, !max);
            }
        }
    }

    // 8. Equipamentos
    public class EquipmentScreen : MenuScreen
    {
        private RectTransform list;
        protected override string Title => "EQUIPAMENTOS";
        protected override void BuildBody(RectTransform body) => list = UI.ScrollList(body);

        protected override void RefreshBody()
        {
            UI.Clear(list);
            string[] slotNames = { "ARMAS", "ARMADURAS", "RELÍQUIAS" };
            for (int slot = 0; slot < 3; slot++)
            {
                Section(list, slotNames[slot]);
                foreach (EquipmentDef item in MetaCatalog.Equipment)
                {
                    if ((int)item.Slot != slot) continue;
                    bool owned = MetaService.OwnsItem(item.Id);
                    bool equipped = MetaService.IsEquipped(item.Id);
                    string id = item.Id;
                    string subtitle = $"{MetaCatalog.RarityName(item.Rarity)} · {item.Bonus.Describe()}";
                    if (!owned)
                    {
                        ItemRow(list, item.Name, subtitle + "\nDisponível na loja", "LOJA", UIStyle.Gold, () => Ui.Push<ShopScreen>(), MetaCatalog.RarityColor(item.Rarity));
                        continue;
                    }
                    ItemRow(list, item.Name + (equipped ? "  (EQUIPADO)" : string.Empty), subtitle, equipped ? "REMOVER" : "EQUIPAR",
                        equipped ? UIStyle.Gray : UIStyle.Green, () =>
                        {
                            MetaService.ToggleEquip(id);
                            Game.Restart();
                            Ui.RefreshTop();
                        }, MetaCatalog.RarityColor(item.Rarity));
                }
            }
        }
    }

    // 7. Inventário (poções e itens)
    public class InventoryScreen : MenuScreen
    {
        private RectTransform list;
        protected override string Title => "INVENTÁRIO";
        protected override void BuildBody(RectTransform body) => list = UI.ScrollList(body);

        protected override void RefreshBody()
        {
            UI.Clear(list);
            SaveData save = SaveManager.Current;
            Section(list, "POÇÕES (valem para a próxima fase)");
            foreach (PotionDef potion in MetaCatalog.Potions)
            {
                int count = save.inventory.Count(potion.Id);
                bool active = save.inventory.activePotions.Contains(potion.Id);
                string id = potion.Id;
                ItemRow(list, $"{potion.Name}  x{count}", potion.Effect.Describe() + (active ? "\nATIVA na próxima fase" : string.Empty),
                    active ? "ATIVA" : "USAR", active ? UIStyle.Gray : UIStyle.Green, () =>
                    {
                        if (MetaService.ActivatePotion(id))
                        {
                            Ui.Toast("Poção ativada para a próxima fase");
                            Game.Restart();
                        }
                        Ui.RefreshTop();
                    }, UIStyle.Purple, !active && count > 0);
            }

            Section(list, "EQUIPAMENTOS");
            bool any = false;
            foreach (EquipmentDef item in MetaCatalog.Equipment)
            {
                if (!MetaService.OwnsItem(item.Id)) continue;
                any = true;
                ItemRow(list, item.Name, $"{MetaCatalog.RarityName(item.Rarity)} · {item.Bonus.Describe()}", "VER",
                    UIStyle.Blue, () => Ui.Push<EquipmentScreen>(), MetaCatalog.RarityColor(item.Rarity));
            }
            if (!any) ItemRow(list, "Nenhum equipamento", "Compre na loja ou abra baús.", "LOJA", UIStyle.Gold, () => Ui.Push<ShopScreen>(), UIStyle.Gray);
        }
    }

    // 10. Loja offline (moedas e gemas obtidas no jogo; sem dinheiro real)
    public class ShopScreen : MenuScreen
    {
        private RectTransform list;
        protected override string Title => "LOJA";
        protected override void BuildBody(RectTransform body) => list = UI.ScrollList(body);

        protected override void RefreshBody()
        {
            UI.Clear(list);
            Section(list, "BAÚS");
            foreach (ChestDef chest in MetaCatalog.Chests)
            {
                string id = chest.Id;
                ChestDef captured = chest;
                string contents = $"{chest.MinGold}–{chest.MaxGold} ouro · {chest.Potions} poção(ões)" +
                                  (chest.EquipmentChance >= 1f ? " · equipamento garantido" : $" · {Mathf.RoundToInt(chest.EquipmentChance * 100f)}% equipamento");
                ItemRow(list, "Baú " + chest.Name, contents, Price(chest.Gold, chest.Gems), UIStyle.Gold, () =>
                    ConfirmPurchaseChest(captured, id), UIStyle.Gold);
            }

            Section(list, "EQUIPAMENTOS");
            foreach (EquipmentDef item in MetaCatalog.Equipment)
            {
                if (MetaService.OwnsItem(item.Id)) continue;
                string id = item.Id;
                EquipmentDef captured = item;
                ItemRow(list, item.Name, $"{MetaCatalog.RarityName(item.Rarity)} · {item.Bonus.Describe()}", Price(item.Gold, item.Gems),
                    UIStyle.Green, () => ConfirmPurchase(captured.Name, captured.Gold, captured.Gems, () => MetaService.BuyEquipment(id), captured.Name + " comprado!"),
                    MetaCatalog.RarityColor(item.Rarity));
            }

            Section(list, "HABILIDADES");
            SkillSystem skills = Game.CurrentSkills;
            for (int s = 0; s < SkillDefinition.Count; s++)
            {
                var skillId = (SkillId)s;
                if (MetaService.IsSkillUnlocked(skillId) || skills == null) continue;
                SkillDefinition definition = skills.Definition(skillId);
                int gold = MetaCatalog.SkillUnlockGold[s];
                int gems = MetaCatalog.SkillUnlockGems[s];
                ItemRow(list, definition.displayName, definition.description, Price(gold, gems), UIStyle.Green,
                    () => ConfirmPurchase(definition.displayName, gold, gems, () => MetaService.UnlockSkill(skillId), definition.displayName + " liberada!"),
                    definition.color, true, 210f);
            }

            Section(list, "POÇÕES");
            foreach (PotionDef potion in MetaCatalog.Potions)
            {
                string id = potion.Id;
                PotionDef captured = potion;
                ItemRow(list, potion.Name, potion.Effect.Describe() + " na próxima fase", Price(potion.Gold, 0), UIStyle.Green,
                    () => ConfirmPurchase(captured.Name, captured.Gold, 0, () => MetaService.BuyPotion(id)), UIStyle.Purple);
            }

            Section(list, "VISUAIS DO EXÉRCITO");
            SaveData save = SaveManager.Current;
            foreach (SkinDef skin in MetaCatalog.Skins)
            {
                string id = skin.Id;
                SkinDef captured = skin;
                bool owned = save.cosmetics.owned.Contains(id);
                bool selected = save.cosmetics.selected == id;
                if (owned)
                {
                    ItemRow(list, skin.Name + (selected ? "  (EM USO)" : string.Empty), "Cores das tropas", selected ? "EM USO" : "USAR",
                        selected ? UIStyle.Gray : UIStyle.Blue, () =>
                        {
                            MetaService.SelectSkin(id);
                            Game.Restart();
                            Ui.RefreshTop();
                        }, skin.Tint, !selected);
                }
                else
                {
                    ItemRow(list, skin.Name, "Cores das tropas", Price(0, skin.Gems), UIStyle.Gold,
                        () => ConfirmPurchase("Visual " + captured.Name, 0, captured.Gems, () => MetaService.BuySkin(id)), skin.Tint);
                }
            }
        }

        private void ConfirmPurchaseChest(ChestDef chest, string id)
        {
            if (!Economy.CanAfford(chest.Gold, chest.Gems))
            {
                Ui.Toast("Saldo insuficiente");
                GameAudio.Play(SoundId.UiBack);
                return;
            }
            Ui.Confirm("ABRIR BAÚ", $"Baú {chest.Name}\npor {Price(chest.Gold, chest.Gems)}?", "ABRIR", () =>
            {
                if (MetaService.BuyChest(id, out RewardBundle reward) == PurchaseResult.Success)
                    Ui.Push<RewardScreen>().Show("BAÚ " + chest.Name.ToUpperInvariant(), reward);
                else Ui.Toast("Saldo insuficiente");
            });
        }
    }
}
