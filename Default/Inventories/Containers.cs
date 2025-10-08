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
    public override ListIndices<Item> Items => new(Entity.inventory, new Range(InventorySlots.Hotbar.End, InventorySlots.Coins.Start));
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
    public sealed override bool Accepts(Item item) => !ChestUI.IsBlockedFromTransferIntoChest(item, Items) && IsActive();
    public sealed override Item GetItem(Item item, GetItemSettings settings) {
        ChestUI.TryPlacingInChest(item, false, Context);
        return item;
    }
}

public sealed class Chest : Container {
    public int WorldId { get; private set; } = -1;
    public int Index { get; private set; } = -1;
    public sealed override Item[] Items => Index > -1 ? Main.chest[Index].item : [];
    public sealed override int Context => ContextID.ChestItem;
    
    public sealed override void OnSlotChange(int slot) => NetMessage.SendData(MessageID.SyncChestItem, number: Index, number2: slot);

    // TODO return nearby chests
    public sealed override IEnumerable<ModSubInventory> GetInventories(Player player) => GetActiveInventories(player);
    public sealed override IEnumerable<ModSubInventory> GetActiveInventories(Player player) => player.chest < 0 ? [] : [NewInstance(player, player.chest)];
    public sealed override bool IsActive() => Entity.chest == Index;

    public override void SaveData(TagCompound tag) {
        if (WorldId != -1) tag[WorldTag] = WorldId;
        if (Index != -1) tag[IndexTag] = Index;
    }
    public override void LoadData(TagCompound tag) {
        if (tag.TryGet(WorldTag, out int id)) WorldId = id;
        if (tag.TryGet(IndexTag, out int index)) Index = index;
    }
    public sealed override bool Equals(object? obj) => base.Equals(obj) && Index == ((Chest)obj).Index && WorldId == ((Chest)obj).WorldId;
    public sealed override int GetHashCode() => (base.GetHashCode(), Index, WorldId).GetHashCode();
    public override bool ForceUnloaded => WorldId != Main.worldID;
    public const string WorldTag = "world";
    public const string IndexTag = "index";

    public Chest NewInstance(Player player, int index) {
        var inventory = (Chest)NewInstance(player);
        inventory.WorldId = Main.worldID;
        inventory.Index = index;
        return inventory;
    }
}

public abstract class Bank : Container {
    public abstract int Index { get; }
    public sealed override bool IsActive() => Entity.chest == Index || (Index == InventorySlots.VoidBag && Entity.IsVoidVaultEnabled);
    public sealed override IEnumerable<ModSubInventory> GetActiveInventories(Player player) => IsActive() ? GetInventories(player) : [];
}
public sealed class PiggyBank : Bank {
    public sealed override int Context => ContextID.BankItem;
    public sealed override int Index => InventorySlots.PiggyBank;
    public sealed override Item[] Items => Entity.bank.item;
}
public sealed class Safe : Bank {
    public sealed override int Context => ContextID.BankItem;
    public sealed override int Index => InventorySlots.Safe;
    public sealed override Item[] Items => Entity.bank2.item;
}
public sealed class DefenderForge : Bank {
    public sealed override int Context => ContextID.BankItem;
    public sealed override int Index => InventorySlots.DefendersForge;
    public sealed override Item[] Items => Entity.bank3.item;
}
public sealed class VoidBag : Bank {
    public sealed override int Context => ContextID.VoidItem;
    public sealed override int Index => InventorySlots.VoidBag;
    public sealed override Item[] Items => Entity.bank4.item;
}