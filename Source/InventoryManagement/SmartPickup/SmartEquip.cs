using System.Collections.Generic;
using BetterInventory.Default.Inventories;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

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

    public static void UpdateLockedItems(Player player) {
        if (!Configs.UpgradeItems.Value.autoLockItems) return;
        foreach (var upgrader in PickupUpgraderLoader.Upgraders) {
            if (upgrader.Enabled) upgrader.CheckLockedItems(player);
        }
    }
}

public sealed class UpgradeItemsItem : GlobalItem {
    public override void ModifyTooltips(Item item, List<TooltipLine> tooltips) {
        if (!Configs.SmartPickup.UpgradeItems || !Configs.UpgradeItems.Value.lockedTooltip) return;
        if (!Configs.UpgradeItems.Value.IsLocked(new(item.type))) return;
        tooltips.Add(new(
            BetterInventory.Instance, "UpgradeLocked",
            Language.GetTextValue($"{Localization.Keys.UI}.UpgradeLocked")
        ));
    }
}