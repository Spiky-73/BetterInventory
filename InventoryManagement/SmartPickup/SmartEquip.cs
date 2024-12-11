using System.Linq;
using BetterInventory.Default.Inventories;
using Terraria;

namespace BetterInventory.InventoryManagement.SmartPickup;

public static class SmartEquip {

    public static Item AutoEquip(Player player, Item item, GetItemSettings settings) {
        foreach (ModSubInventory inv in InventoryLoader.Special.Where(i => i is not Hotbar && i.Accepts(item))) {
            if (Configs.SmartPickup.Value.autoEquip < Configs.AutoEquipLevel.AnySlot && !inv.IsPrimaryFor(item)) continue;
            item = inv.GetItem(player, item, settings);
            if (item.IsAir) return item;
        }
        return item;
    }
    public static Item UpgradeItems(Player player, Item item, GetItemSettings settings) {
        foreach (var upgrader in PickupUpgraderLoader.Upgraders) {
            if (upgrader.Enabled && upgrader.AppliesTo(item)) item = upgrader.AttemptUpgrade(player, item);
        }
        return item;
    }
}