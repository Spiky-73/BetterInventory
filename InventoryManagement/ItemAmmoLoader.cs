using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace BetterInventory.InventoryManagement;

public static class ItemAmmoLoader {
    internal static void Add(ModItemAmmo itemAmmo) => _itemAmmos.Add(itemAmmo);
    internal static void Unload() => _itemAmmos.Clear();

    public static ReadOnlyCollection<ModItemAmmo> ItemAmmos => _itemAmmos.AsReadOnly();
    private readonly static List<ModItemAmmo> _itemAmmos = new();
}