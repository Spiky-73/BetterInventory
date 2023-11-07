using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace BetterInventory;

public static class InventoryLoader {

    public static ReadOnlyCollection<ModInventory> Inventories => _inventories.AsReadOnly();

    internal static void Register(ModInventory inventory){
        _inventories.Add(inventory);
    }

    internal static void Unload(){
        _inventories.Clear();
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