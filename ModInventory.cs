using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using BetterInventory.DataStructures;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using System.Linq;

namespace BetterInventory;

public class InventorySlots : IComparable<InventorySlots> {

    public ModInventory Inventory { get; }
    public string? LocalizationKey { get; }
    public Predicate<Item>? Accepts { get; }
    public ReadOnlyCollection<(int context, Func<Player, ListIndices<Item>> items)> Slots => new(_slots);

    internal InventorySlots(ModInventory inventory, string? locKey, Predicate<Item>? accepts, params (int context, Func<Player, ListIndices<Item>> items)[] slots) {
        Inventory = inventory;
        LocalizationKey = locKey;
        Accepts = accepts;
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

    private IList<(int context, Func<Player, ListIndices<Item>> items)> _slots;
}

public abstract class ModInventory : ModType, ILocalizedModType {

    public ReadOnlyCollection<InventorySlots> Slots => _slots.AsReadOnly();
    public ReadOnlyCollection<InventorySlots> SlotsByPriority { get {
        List<InventorySlots> slots = new(Slots);
        slots.Sort();
        return slots.AsReadOnly();
    } }

    public virtual int? MaxStack => null;

    public virtual void Focus() { }

    public virtual bool FitsSlot(Player player, Item item, InventorySlots slots, int index, out IList<int> itemsToMove) {
        itemsToMove = Array.Empty<int>();
        return true;
    }

    public virtual Item GetItem(Player player, Item item, GetItemSettings settings) {
        foreach (InventorySlots slots in SlotsByPriority) {
            IList<Item> items = slots.Items(player);
            for (int i = 0; i < items.Count; i++) {
                if (!FitsSlot(player, item, slots, i, out _) || slots.Accepts is not null && !slots.Accepts(item)) continue;
                if (items[i].Stack(item, MaxStack)) {
                    OnSlotChange(player, slots, i);
                    if (item.IsAir) return item;
                }

            }
        }
        return item;
    }

    public virtual void OnSlotChange(Player player, InventorySlots slots, int index) {}

    public void AddSlots(string? locKey, Predicate<Item>? accepts, int context, Func<Player, ListIndices<Item>> items) => AddSlots(locKey, accepts, (context, items));
    public void AddSlots(string? locKey, Predicate<Item>? accepts, params (int context, Func<Player, ListIndices<Item>> items)[] slots) => _slots.Add(new(this, locKey, accepts, slots));

    protected sealed override void Register() {
        ModTypeLookup<ModInventory>.Register(this);
        InventoryLoader.Register(this);
    }
    public sealed override void SetupContent() => SetStaticDefaults();
    public override void Unload() => _slots.Clear();

    public string LocalizationCategory => "Inventories";
    public virtual LocalizedText DisplayName => this.GetLocalization("DisplayName", PrettyPrintName);

    // private readonly List<InventorySlotsOld> _slots = new();
    private readonly List<InventorySlots> _slots = new();
}