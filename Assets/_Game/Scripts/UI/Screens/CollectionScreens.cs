using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LegionRush
{
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
}
