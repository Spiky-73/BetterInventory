using System;
using System.Collections.Generic;
using SpikysLib.DataStructures;
using Terraria;
using Terraria.ID;
using Terraria.UI;
using ContextID = Terraria.UI.ItemSlot.Context;
using SpikysLib.Constants;
using Terraria.ModLoader.IO;

namespace BetterInventory.Default.Inventories;

public sealed class Hotbar : ModSubInventory {
    private int _previousSlot;
    public override int Context => ContextID.InventoryItem;
    public override bool IsPreferredInventory(Item item) => item.favorited;
    public override void Focus(int slot) => (_previousSlot, Entity.selectedItem) = (Entity.selectedItem, slot);
    public override void Unfocus(int slot) => Entity.selectedItem = _previousSlot;
    public override ListIndices<Item> Items => new(Entity.inventory, InventorySlots.Hotbar);
    public override int ComparePositionTo(ModSubInventory other) => 1;
}
public sealed class Inventory : ModSubInventory {
    public override int Context => ContextID.InventoryItem;
    public override ListIndices<Item> Items => new(Entity.inventory, new SpikysLib.DataStructures.Range(InventorySlots.Hotbar.End, InventorySlots.Coins.Start));
    public override Item GetItem(Item item, GetItemSettings settings) => Utility.GetItem_Inner(Entity, Entity.whoAmI, item, settings);
}
public sealed class Coins : ModSubInventory {
    public override int Context => ContextID.InventoryCoin;
    public sealed override bool Accepts(Item item) => item.IsACoin;
    public override ListIndices<Item> Items => new(Entity.inventory, InventorySlots.Coins);
}
public sealed class Ammo : ModSubInventory {
    public override int Context => ContextID.InventoryAmmo;
    public override bool IsPreferredInventory(Item item) => true;
    public sealed override bool Accepts(Item item) => !item.IsAir && item.FitsAmmoSlot();
    public override ListIndices<Item> Items => new(Entity.inventory, InventorySlots.Ammo);
}

public abstract class Container : ModSubInventory {
    public override abstract Item[] Items { get; }
    public sealed override bool FitsSlot(Item item, int slot, out IList<InventorySlot> itemsToMove) {
        itemsToMove = Array.Empty<InventorySlot>();
        return !ChestUI.IsBlockedFromTransferIntoChest(item, Items);
    }
    public sealed override Item GetItem(Item item, GetItemSettings settings) {
        ChestUI.TryPlacingInChest(item, false, Context);
        return item;
    }
}

public sealed class Chest : Container {
    public int Index { get; private set; } = -1;
    public override int Context => ContextID.ChestItem;
    public override void OnSlotChange(int slot) => NetMessage.SendData(MessageID.SyncChestItem, number: Index, number2: slot);
    public override Item[] Items => Index >= 0 ? Main.chest[Index].item : [];

    public override bool Accepts(Item item) => Entity.chest == Index;

    public override IList<ModSubInventory> GetInventories(Player player) {
        if (player.chest < 0) return [];
        var inventory = (Chest)NewInstance(player);
        inventory.Index = player.chest;
        return [inventory];
    }

    public override bool Equals(object? obj) => base.Equals(obj) && Index == ((Chest)obj).Index;
    public override int GetHashCode() => (Index, base.GetHashCode()).GetHashCode();
}
public sealed class PiggyBank : Container {
    public override int Context => ContextID.BankItem;
    public override Item[] Items => Entity.bank.item;
}
public sealed class Safe : Container {
    public override int Context => ContextID.BankItem;
    public override Item[] Items => Entity.bank2.item;
}
public sealed class DefenderForge : Container {
    public override int Context => ContextID.BankItem;
    public override Item[] Items => Entity.bank3.item;
}
public sealed class VoidBag : Container {
    public override int Context => ContextID.VoidItem;
    public override Item[] Items => Entity.bank4.item;
}