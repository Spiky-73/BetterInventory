using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using BetterInventory.DataStructures;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace BetterInventory;

public record struct SubInventory(string LocalizationKey, Predicate<Item> Accepts, Func<Player, IList<int>> Slots){
    public ModInventory Inventory { get; internal set; } = null!;
}


public abstract class ModInventory<TInventory> : ModInventory where TInventory: ModInventory<TInventory> {
    public static TInventory Instance { get; private set; } = null!;
    public override void Load() => Instance = (TInventory)this;
    public override void Unload() => Instance = null!;
}
public abstract class ModInventory : ModType, ILocalizedModType {

    public abstract HashSet<int> Contexts { get; }
    public abstract IList<Item> Items(Player player);
    public ReadOnlyCollection<SubInventory> SubInventories => _subInventories.AsReadOnly();

    public virtual int? MaxStack => null;

    public virtual void Focus() { }

    public virtual int ToIndex(Player player, int context, int slot) => slot;
    public int IndexOf(Player player, int type, int prefix) {
        IList<Item> items = Items(player);
        for (int i = 0; i < items.Count; i++) {
            if (items[i].type == type && items[i].prefix == prefix) return i;
        }
        return -1;
    }

    public virtual bool SlotEnabled(Player player, int slot) => true;
    public virtual bool CanSlotAccepts(Player player, Item item, int slot, out IList<int> itemsToMove) {;
        itemsToMove = Array.Empty<int>();
        return true;
    }

    public virtual Item GetItem(Player player, Item item, GetItemSettings settings) {
        IList<Item> items = Items(player);
        RangeSet slots = new(){ new DataStructures.Range(0, items.Count-1) };
        void StackOnSlot(int slot){
            if (!SlotEnabled(player, slot) || !CanSlotAccepts(player, item, slot, out _)) return;
            items[slot].Stack(item, MaxStack);
        }

        foreach (var sub in _subInventories) {
            bool ok = sub.Accepts(item);
            foreach(int slot in sub.Slots(player)) {
                slots.Remove(slot);
                StackOnSlot(slot);
                if (item.IsAir) return item;
            }
        }

        foreach(int slot in slots.Values()) {
            StackOnSlot(slot);
            if (item.IsAir) return item;
        }

        return item;
    }

    protected void SubInventory(string locKey, Predicate<Item> accepts, IList<int> slots) => SubInventory(locKey, accepts, _ => slots);
    protected void SubInventory(string locKey, Predicate<Item> accepts, Func<Player, IList<int>> slots) => SubInventory(new(locKey, accepts, slots));
    protected void SubInventory(SubInventory sub) {
        sub.Inventory = this;
        _subInventories.Add(sub);
    }

    protected sealed override void Register() {
        ModTypeLookup<ModInventory>.Register(this);
        InventoryLoader.Register(this);
    }
    public sealed override void SetupContent() => SetStaticDefaults();
    public override void Unload() => _subInventories.Clear();

    public string LocalizationCategory => "Inventories";
    public virtual LocalizedText DisplayName => this.GetLocalization("DisplayName", PrettyPrintName);

    private readonly List<SubInventory> _subInventories = new();
}