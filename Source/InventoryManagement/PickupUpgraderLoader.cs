using System.Collections.Generic;
using System.Collections.ObjectModel;
using SpikysLib.Configs;

namespace BetterInventory.InventoryManagement;

public static class PickupUpgraderLoader {
    internal static void Add(ModPickupUpgrader upgrader) {
        ConfigHelper.SetInstance(upgrader);
        _upgraders.Add(upgrader);
    }
    internal static void Unload() => _upgraders.Clear();

    public static ModPickupUpgrader? GetUpgrader(string mod, string name) => _upgraders.Find(p => p.Mod.Name == mod && p.Name == name);

    public static ReadOnlyCollection<ModPickupUpgrader> Upgraders => _upgraders.AsReadOnly();
    private readonly static List<ModPickupUpgrader> _upgraders = new();
}