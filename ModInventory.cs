using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace BetterInventory;

public record struct InventorySlots(string? LocalizationKey, int Context, Func<Player, IList<Item>> Items, Predicate<Item>? Accepts) : IComparable<InventorySlots>{
    public ModInventory Inventory { get; internal set; } = null!;

    public readonly int CompareTo(InventorySlots other) {
            bool noCond = Accepts is null;
            if (noCond == other.Accepts is null) return 0;
            return !noCond ? -1 : 1;
    }
}


public abstract class ModInventory<TInventory> : ModInventory where TInventory: ModInventory<TInventory> {
    public static TInventory Instance { get; private set; } = null!;
    public override void Load() => Instance = (TInventory)this;
    public override void Unload() => Instance = null!;
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

    public int GlobalIndex(Player player, InventorySlots slots, int index) {
        int offset = 0;
        foreach (InventorySlots s in Slots) {
            if (s == slots) return offset + index;
            offset += s.Items(player).Count;
        }
        return -1;
    }

    public virtual bool FitsSlot(Player player, Item item, InventorySlots slots, int index, out IList<int> itemsToMove) {
        itemsToMove = Array.Empty<int>();
        return true;
    }

    public virtual Item GetItem(Player player, Item item, GetItemSettings settings) {
        foreach (InventorySlots slots in SlotsByPriority) {
            IList<Item> items = slots.Items(player);
            for (int i = 0; i < items.Count; i++) {
                if (!FitsSlot(player, item, slots, i, out _) || slots.Accepts is not null && !slots.Accepts(item)) continue;
                items[i].Stack(item, MaxStack);
                if (item.IsAir) return item;

            }
        }
        return item;
    }

    public void AddSlots(InventorySlots slots) {
        slots.Inventory = this;
        _slots.Add(slots);
    }

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