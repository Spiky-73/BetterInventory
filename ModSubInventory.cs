using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.Audio;
using Terraria.ID;
using SpikysLib;

namespace BetterInventory;

public readonly record struct InventorySlot(ModSubInventory Inventory, int Index) {
    public Item Item {
        get => Inventory.Items[Index];
        set => Inventory.Items[Index] = value;
    }
    public Item GetItem(Item item, GetItemSettings settings) => Inventory.GetItem(item, Index, settings);
    public void Focus() => Inventory.Focus(Index);
    public void OnChange() => Inventory.OnSlotChange(Index);
    public bool Fits(Item item, out IList<InventorySlot> itemsToMove) => Inventory.FitsSlot(item, Index, out itemsToMove);
}

public abstract class ModSubInventory : ModType<Player, ModSubInventory>, ILocalizedModType {
    public bool CanBePreferredInventory { get; internal set; }

    public abstract int Context { get; }
    public virtual int? MaxStack => null;

    public virtual bool Accepts(Item item) => true;
    public virtual bool IsPreferredInventory(Item item) => false;

    public abstract IList<Item> Items { get; }
    public virtual bool FitsSlot(Item item, int slot, out IList<InventorySlot> itemsToMove) {
        itemsToMove = [];
        return true;
    }

    public virtual void Focus(int slot) { }
    public virtual void OnSlotChange(int slot) { }

    public virtual Item GetItem(Item item, GetItemSettings settings) {
        if (!Accepts(item)) return item;
        IList<Item> items = Items;
        for (int i = 0; i < items.Count && !item.IsAir; i++) TryStackItem(item, i, settings, items);
        Recipe.FindRecipes();
        return item;
    }

    public Item GetItem(Item item, int slot, GetItemSettings settings) {
        if (!Accepts(item)) return item;
        TryStackItem(item, slot, settings, Items);
        Recipe.FindRecipes();
        return item;
    }

    private void TryStackItem(Item item, int slot, GetItemSettings settings, IList<Item> items) {
        if (!FitsSlot(item, slot, out var itemsToMove) || itemsToMove.Count != 0) return;
        items[slot] = ItemHelper.MoveInto(items[slot], item, out int transferred, MaxStack);
        if (transferred == 0) return;
        SoundEngine.PlaySound(SoundID.Grab);
        items[slot].position = Entity.position;
        if (!settings.NoText) PopupText.NewText(PopupTextContext.ItemPickupToVoidContainer, items[slot], transferred, false, settings.LongText);
        OnSlotChange(slot);
        return;
    }

    public virtual IList<ModSubInventory> GetInventories(Player player) => [NewInstance(player)];

    public virtual int ComparePositionTo(ModSubInventory other) => 0;

    protected sealed override void Register() {
        ModTypeLookup<ModSubInventory>.Register(this);
        InventoryLoader.Register(this);
        Language.GetOrRegister(this.GetLocalizationKey("DisplayName"), PrettyPrintName);
    }
    public sealed override void SetupContent() => SetStaticDefaults();

    public string LocalizationCategory => "SubInventories";
    public virtual LocalizedText DisplayName => this.GetLocalization("DisplayName");

    protected sealed override Player CreateTemplateEntity() => new();

    public override bool Equals(object? obj) => obj is ModSubInventory subInventory && FullName == subInventory.FullName;
    public override int GetHashCode() => FullName.GetHashCode();
}