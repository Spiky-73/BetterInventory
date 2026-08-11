using System;
using SpikysLib.Constants;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace BetterInventory.BetterItemPickup;

public sealed class PickupToBanks : ILoadable {

    // TODO autoload
    public void Load(Mod mod) {
        _fakeInventory = new Item[InventorySlots.Count];
        Item air = new();
        Array.Fill(_fakeInventory, air);
    }
    public void Unload() { }

    public static bool GetItem_Banks(Player player, int plr, Item item, GetItemSettings settings) {
        if (settings.NoText) return false;

        if ((player.HasItemInInventoryOrOpenVoidBag(ItemID.PiggyBank) || player.HasItemInInventoryOrOpenVoidBag(ItemID.MoneyTrough)) && GetItem_QuickStack_Chest(player, plr, item, settings, InventorySlots.PiggyBank)) return true;
        if (player.HasItemInInventoryOrOpenVoidBag(ItemID.Safe) && GetItem_QuickStack_Chest(player, plr, item, settings, InventorySlots.Safe)) return true;
        if (player.HasItemInInventoryOrOpenVoidBag(ItemID.DefendersForge) && GetItem_QuickStack_Chest(player, plr, item, settings, InventorySlots.DefendersForge)) return true;

        return false;
    }

    private static Item[] _fakeInventory = null!;
    private static bool GetItem_QuickStack_Chest(Player player, int plr, Item item, GetItemSettings settings, int chest) {
        var newItem = item.Clone();
        _fakeInventory[0] = item;
        (var inventory, player.inventory) = (player.inventory, _fakeInventory);
        (var oldChest, player.chest) = (player.chest, chest);
        ChestUI.QuickStack(ContainerTransferContext.FromUnknown(player));
        player.inventory = inventory;
        player.chest = oldChest;
        var numTransferred = newItem.stack - _fakeInventory[0].stack;
        if (numTransferred > 0) {
            BetterItemPickup.HandlePickup(player, plr, newItem, numTransferred, settings, PopupTextContext.ItemPickupToVoidContainer);
        }
        return _fakeInventory[0].IsAir;
    }
}
