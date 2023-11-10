using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using BetterInventory.DataStructures;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using System.Linq;
using Terraria.Audio;
using Terraria.ID;

namespace BetterInventory;

public sealed class InventorySlots : IComparable<InventorySlots> {

    public ModInventory Inventory { get; }
    public string? LocalizationKey { get; }
    public Predicate<Item>? Accepts { get; }
    public Predicate<Item> IsMainSlot { get; }

    public ReadOnlyCollection<(int context, Func<Player, ListIndices<Item>> items)> Slots => new(_slots);

    internal InventorySlots(ModInventory inventory, string? locKey = null, Predicate<Item>? accepts = null, Predicate<Item>? mainSlot = null, params (int context, Func<Player, ListIndices<Item>> items)[] slots) {
        Inventory = inventory;
        LocalizationKey = locKey;
        Accepts = accepts;
        IsMainSlot = mainSlot ?? (_ => true);
        _slots = slots;
    }
    
    public JoinedList<Item> Items(Player player) => new((from slots in Slots select (IList<Item>)slots.items(player)).ToArray());

    public int CompareTo(InventorySlots? other) {
        if (other is null) return 1;
        bool noCond = Accepts is null;
        if (noCond == other.Accepts is null) return 0;
        return !noCond ? -1 : 1;
    }

    public int GetContext(Player player, int targetSlot) {
        foreach((int context, Func<Player, ListIndices<Item>> items) in Slots) if ((targetSlot -= items(player).Count) < 0) return context;
        return 0;
    }

    public Item GetItem(Player player, Item item, GetItemSettings settings) {
        IList<Item> items = Items(player);
        if (Accepts?.Invoke(item) == false) return item;
        for (int i = 0; i < items.Count && !item.IsAir; i++) {
            if (!Inventory.FitsSlot(player, item, this, i, out var itemsToMove) || itemsToMove.Count != 0 || !items[i].Stack(item, out int tranfered, Inventory.MaxStack)) continue;
            SoundEngine.PlaySound(SoundID.Grab);
            items[i].position = player.position;
            if (!settings.NoText) PopupText.NewText(PopupTextContext.ItemPickupToVoidContainer, items[i], tranfered, false, settings.LongText);
            Inventory.OnSlotChange(player, this, i);
        }
        return item;
    }

    private IList<(int context, Func<Player, ListIndices<Item>> items)> _slots;
}

public abstract class ModInventory : ModType, ILocalizedModType {

    public ReadOnlyCollection<InventorySlots> Slots => _slots.AsReadOnly();
    public ReadOnlyCollection<InventorySlots> SlotsByPriority { get {
        List<InventorySlots> slots = new(Slots);
        slots.SortSlots();
        return slots.AsReadOnly();
    } }

    public virtual int? MaxStack => null;

    public virtual void Focus(Player player, InventorySlots slots, int slot) { }

    public virtual bool FitsSlot(Player player, Item item, InventorySlots slots, int index, out IList<int> itemsToMove) {
        itemsToMove = Array.Empty<int>();
        return true;
    }

    public virtual Item GetItem(Player player, Item item, GetItemSettings settings) {
        Configs.InventoryManagement.AutoEquipLevel autoEquip = Configs.InventoryManagement.Instance.autoEquip;
        bool checkSlot = !settings.NoText && autoEquip != Configs.InventoryManagement.AutoEquipLevel.Off;

        foreach (InventorySlots slots in Slots) {
            if (checkSlot && (slots.Accepts is null || !slots.IsMainSlot(item) && autoEquip == Configs.InventoryManagement.AutoEquipLevel.MainSlots)) continue;
            item = slots.GetItem(player, item, settings);
            if (item.IsAir) return new();
        }
        return item;
    }

    public virtual void OnSlotChange(Player player, InventorySlots slots, int index) {}

    protected void AddSlots(int context, Func<Player, ListIndices<Item>> items, string? locKey = null, Predicate<Item>? accepts = null, Predicate<Item>? mainSlot = null) => AddSlots(locKey, accepts, mainSlot, (context, items));
    protected void AddSlots(string? locKey = null, Predicate<Item>? accepts = null, Predicate<Item>? mainSlot = null, params (int context, Func<Player, ListIndices<Item>> items)[] slots) => _slots.Add(new(this, locKey, accepts, mainSlot, slots));

    protected sealed override void Register() {
        ModTypeLookup<ModInventory>.Register(this);
        InventoryLoader.Register(this);
    }
    public sealed override void SetupContent() => SetStaticDefaults();
    public override void Unload() => _slots.Clear();

    public string LocalizationCategory => "Inventories";
    public virtual LocalizedText DisplayName => this.GetLocalization("DisplayName", PrettyPrintName);

    private readonly List<InventorySlots> _slots = new();
}