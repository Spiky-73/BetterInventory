using System;
using System.Collections.Generic;
using SpikysLib.DataStructures;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.Audio;
using Terraria.ID;
using SpikysLib.Extensions;

namespace BetterInventory;

public readonly record struct Slot(ModSubInventory Inventory, int Index) {
    public Item Item(Player player) => Inventory.Items(player)[Index];
    public Item GetItem(Player player, Item item, GetItemSettings settings) => Inventory.GetItem(player, item, Index, settings);
}

public abstract class ModSubInventory<TInventory> : ModSubInventory where TInventory : ModSubInventory<TInventory> {
    public static TInventory Instance = null!;
}

public abstract class ModSubInventory : ModType, ILocalizedModType {
    public abstract int Context { get; }
    public virtual int? MaxStack => null;

    public virtual bool Accepts(Item item) => true;
    public virtual bool IsDefault(Item item) => false;

    public abstract Joined<ListIndices<Item>, Item> Items(Player player);
    public virtual bool FitsSlot(Player player, Item item, int slot, out IList<Slot> itemsToMove) {
        itemsToMove = Array.Empty<Slot>();
        return true;
    }

    public virtual void Focus(Player player, int slot) { }
    public virtual void OnSlotChange(Player player, int slot) { }

    public virtual Item GetItem(Player player, Item item, GetItemSettings settings) {
        if (!Accepts(item)) return item;
        IList<Item> items = Items(player);
        for (int i = 0; i < items.Count && !item.IsAir; i++) TryStackItem(player, item, i, settings, items);
        return item;
    }
    
    public Item GetItem(Player player, Item item, int slot, GetItemSettings settings) {
        if (!Accepts(item)) return item;
        TryStackItem(player, item, slot, settings, Items(player));
        return item;
    }

    private void TryStackItem(Player player, Item item, int slot, GetItemSettings settings, IList<Item> items) {
        if (!FitsSlot(player, item, slot, out var itemsToMove) || itemsToMove.Count != 0) return;
        items[slot] = ItemExtensions.MoveInto(items[slot], item, out int transferred, MaxStack);
        if (transferred == 0) return;
        SoundEngine.PlaySound(SoundID.Grab);
        items[slot].position = player.position;
        if (!settings.NoText) PopupText.NewText(PopupTextContext.ItemPickupToVoidContainer, items[slot], transferred, false, settings.LongText);
        OnSlotChange(player, slot);
        return;
    }

    public virtual int ComparePositionTo(ModSubInventory other) => 0;

    protected sealed override void Register() {
        ModTypeLookup<ModSubInventory>.Register(this);
        InventoryLoader.Register(this);
    }
    public sealed override void SetupContent() => SetStaticDefaults();

    public string LocalizationCategory => "Inventories";
    public virtual LocalizedText DisplayName => this.GetLocalization("DisplayName", PrettyPrintName);
}