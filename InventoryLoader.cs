using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using BetterInventory.DataStructures;
using Terraria;

namespace BetterInventory;

public static class InventoryLoader {

    public static ReadOnlyCollection<ModInventory> Inventories => _inventories.AsReadOnly();

    internal static void Register(ModInventory inventory){
        _inventories.Add(inventory);
    }

    internal static void Unload(){
        _inventories.Clear();
    }

    public static (InventorySlots? slots, int index) GetInventorySlot(Player player, Item[] inventory, int context, int slot) {
        foreach (ModInventory modInventory in Inventories) {
            foreach (InventorySlots slots in modInventory.Slots) {
                int slotOffset = 0;
                foreach ((int c, Func<Player, ListIndices<Item>> s) in slots.Slots) {
                    ListIndices<Item> items = s(player);
                    int index = items.FromInnerIndex(slot);
                    if (items.List == inventory && index != -1) return (slots, index);
                    slotOffset += items.Count;
                }
            }
        }
        return (null, -1);
    }

    public static void SortSlots(this IList<InventorySlots> slots) {
        List<InventorySlots> low = new();
        List<InventorySlots> high = new();
        foreach(InventorySlots slot in slots) {
            if (slot.Accepts is null) low.Add(slot);
            else high.Add(slot);
        }
        for (int i = 0; i < high.Count; i++) slots[i] = high[i];
        for (int i = 0; i < low.Count; i++) slots[i+high.Count] = low[i];
    }


    private readonly static List<ModInventory> _inventories = new();
}