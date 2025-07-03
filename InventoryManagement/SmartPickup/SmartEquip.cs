using BetterInventory.Default.Inventories;
using Terraria;

namespace BetterInventory.InventoryManagement.SmartPickup;

public static class SmartEquip {

    public static Item AutoEquip(Player player, Item item, GetItemSettings settings) {
        var inventories = Configs.SmartPickup.Value.autoEquip.Value.inactiveInventories ? InventoryLoader.GetPreferredInventories(player) : InventoryLoader.GetPreferredActiveInventories(player);
        foreach (var inv in inventories) {
            if (inv is Hotbar || !inv.Accepts(item)) continue;
            if (Configs.SmartPickup.Value.autoEquip < Configs.AutoEquipLevel.AnySlot && !inv.IsPreferredInventory(item)) continue;
            item = inv.GetItem(item, settings);
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

    public static Item VoidBag(Player player, Item item, GetItemSettings settings) {
        if (!settings.CanGoIntoVoidVault || !player.IsVoidVaultEnabled) return item;
        if (Configs.SmartPickup.Value.voidBag == Configs.VoidBagLevel.IfInside && !player.HasItem(item.type, player.bank4.item)) return item;
        if (Reflection.Player.GetItem_VoidVault.Invoke(player, player.whoAmI, player.bank4.item, item, settings, item)) return new();
        return item;
    }
}