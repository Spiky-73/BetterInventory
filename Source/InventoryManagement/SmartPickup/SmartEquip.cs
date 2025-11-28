using System;
using BetterInventory.Default.Inventories;
using SpikysLib;
using SpikysLib.Constants;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace BetterInventory.InventoryManagement.SmartPickup;

public static class SmartEquip {

    public static Item RefillMouse(Player player, Item item, GetItemSettings settings) {
        if (item == Main.mouseItem || item == player.inventory[InventorySlots.Mouse] || Main.mouseItem.IsAir || Main.mouseItem.type != item.type) return item;
        Main.mouseItem = ItemHelper.MoveInto(Main.mouseItem, item, out int transferred, item.maxStack);
        if (transferred == 0) return item;
        SoundEngine.PlaySound(SoundID.Grab);
        Main.mouseItem.position = player.position;
        if (!settings.NoText) PopupText.NewText(PopupTextContext.ItemPickupToVoidContainer, Main.mouseItem, transferred, false, settings.LongText);
        return item;
    }

    public static Item QuickStack(Player player, Item item, GetItemSettings settings) {
        if (Configs.QuickStackPickup.Value.chests) {
            Item[] fakeInventory = new Item[player.inventory.Length];
            for (int i = 0; i < fakeInventory.Length; i++) fakeInventory[i] = new();
            fakeInventory[0] = item;
            (var inventory, player.inventory) = (player.inventory, fakeInventory);
            player.QuickStackAllChests();
            player.inventory = inventory;
            item = fakeInventory[0];
            if (Main.netMode == NetmodeID.MultiplayerClient) {
                // Resync inventory[0] as it the modified slot was synched by QuickStackAllChests
                NetMessage.SendData(MessageID.SyncEquipment, -1, -1, null, player.whoAmI, PlayerItemSlotID.Inventory0, player.inventory[0].prefix, 0f, 0, 0, 0);
            }
        }
        if (Configs.QuickStackPickup.Value.voidBag && player.HasItem(item.type, player.bank4.item)) item = VoidBagFirst(player, item, settings);

        return item;
    }

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

    public static Item VoidBagFirst(Player player, Item item, GetItemSettings settings) {
        if (!settings.CanGoIntoVoidVault || !player.IsVoidVaultEnabled) return item;
        if (item.IsACoin && Array.FindIndex(player.inventory, i => i.IsACoin) != -1) return item;
        if (Reflection.Player.GetItem_VoidVault.Invoke(player, player.whoAmI, player.bank4.item, item, settings, item)) return new();
        return item;
    }
}