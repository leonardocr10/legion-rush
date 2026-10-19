using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LegionRush
{
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

}
