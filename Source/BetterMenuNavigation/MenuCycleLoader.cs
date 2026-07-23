using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace BetterInventory.BetterMenuNavigation;

public static class MenuCycleLoader {

    internal static int Register(ModMenuCycle cycle) {
        _cycles.Add(cycle);
        return cycle.Type = _cycles.Count;
    }

    public static ReadOnlyCollection<ModMenuCycle> Cycles => _cycles.AsReadOnly();
    private readonly static List<ModMenuCycle> _cycles = [];
}