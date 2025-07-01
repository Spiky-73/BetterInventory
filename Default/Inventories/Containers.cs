using System;
using System.Collections.Generic;
using SpikysLib.DataStructures;
using Terraria;
using Terraria.ID;
using Terraria.UI;
using ContextID = Terraria.UI.ItemSlot.Context;
using SpikysLib.Constants;

namespace BetterInventory.Default.Inventories;

public sealed class Hotbar : ModSubInventory {
    public override int Context => ContextID.InventoryItem;
    public override bool IsPreferredInventory(Item item) => item.favorited;
    public override void Focus(int slot) => Entity.selectedItem = slot;
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

// TODO use new instances to save the chest
public sealed class Chest : Container {
    public override int Context => ContextID.ChestItem;
    public override void OnSlotChange(int slot) => NetMessage.SendData(MessageID.SyncChestItem, number: Entity.chest, number2: slot);
    public override Item[] Items => Entity.chest >= 0 ? Main.chest[Entity.chest].item : [];
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