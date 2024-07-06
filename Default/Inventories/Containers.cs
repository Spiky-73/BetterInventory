using System;
using System.Collections.Generic;
using SpikysLib.DataStructures;
using BetterInventory.InventoryManagement;
using Terraria;
using Terraria.ID;
using Terraria.UI;
using ContextID = Terraria.UI.ItemSlot.Context;
using SpikysLib.Constants;

namespace BetterInventory.Default.Inventories;

public sealed class Hotbar : ModSubInventory {
    public override int Context => ContextID.InventoryItem;
    public override bool IsPrimaryFor(Item item) => item.favorited;
    public override void Focus(Player player, int slot) => player.selectedItem = slot;
    public override ListIndices<Item> Items(Player player) => new(player.inventory, InventorySlots.Hotbar);
    public override int ComparePositionTo(ModSubInventory other) => 1;
}
public sealed class Inventory : ModSubInventory {
    public override int Context => ContextID.InventoryItem;
    public override ListIndices<Item> Items(Player player) => new(player.inventory, new SpikysLib.DataStructures.Range(InventorySlots.Hotbar.End, InventorySlots.Coins.Start));
    public override Item GetItem(Player player, Item item, GetItemSettings settings) => SmartPickup.GetItem_Inner(player, player.whoAmI, item, settings);
}
public sealed class Coins : ModSubInventory {
    public override int Context => ContextID.InventoryCoin;
    public sealed override bool Accepts(Item item) => item.IsACoin;
    public override ListIndices<Item> Items(Player player) => new(player.inventory, InventorySlots.Coins);
}
public sealed class Ammo : ModSubInventory {
    public override int Context => ContextID.InventoryAmmo;
    public override bool IsPrimaryFor(Item item) => true;
    public sealed override bool Accepts(Item item) => !item.IsAir && item.FitsAmmoSlot();
    public override ListIndices<Item> Items(Player player) => new(player.inventory, InventorySlots.Ammo);
}

public abstract class Container : ModSubInventory {
    public abstract override Item[] Items(Player player);
    public sealed override bool FitsSlot(Player player, Item item, int slot, out IList<Slot> itemsToMove) {
        itemsToMove = Array.Empty<Slot>();
        return !ChestUI.IsBlockedFromTransferIntoChest(item, Items(player));
    }
    public sealed override Item GetItem(Player player, Item item, GetItemSettings settings) {
        ChestUI.TryPlacingInChest(item, false, Context);
        return item;
    }
}
public sealed class Chest : Container {
    public override int Context => ContextID.ChestItem;
    public override void OnSlotChange(Player player, int slot) => NetMessage.SendData(MessageID.SyncChestItem, number: player.chest, number2: slot);
    public override Item[] Items(Player player) => player.chest >= 0 ? Main.chest[player.chest].item : Array.Empty<Item>();
}
public sealed class PiggyBank : Container {
    public override int Context => ContextID.BankItem;
    public override Item[] Items(Player player) => player.bank.item;
}
public sealed class Safe : Container {
    public override int Context => ContextID.BankItem;
    public override Item[] Items(Player player) => player.bank2.item;
}
public sealed class DefenderForge : Container {
    public override int Context => ContextID.BankItem;
    public override Item[] Items(Player player) => player.bank3.item;
}
public sealed class VoidBag : Container {
    public override int Context => ContextID.VoidItem;
    public override Item[] Items(Player player) => player.bank4.item;
}