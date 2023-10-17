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


    private readonly static List<ModInventory> _inventories = new();
}