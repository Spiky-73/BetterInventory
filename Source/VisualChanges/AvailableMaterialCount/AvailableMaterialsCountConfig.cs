using System.ComponentModel;
using BetterInventory.Configs;

namespace BetterInventory.VisualChanges.AvailableMaterialsCount;

using AMCUnloadableAttribute = UnloadableAttribute<UnloadedAvailableMaterialsCountConfig>;

public sealed class AvailableMaterialsCountConfig {
    [DefaultValue(true)] public bool tooltip = true;
    [AMCUnloadable(nameof(itemSlot)), DefaultValue(true)] public bool itemSlot = true;

    public static AvailableMaterialsCountConfig Instance => VisualChangesConfig.Instance.availableMaterialsCount.Value;
    public static bool Tooltip => Instance.tooltip;
    public static bool ItemSlot => Instance.itemSlot && !UnloadedAvailableMaterialsCountConfig.Instance.itemSlot;
}

public sealed class UnloadedAvailableMaterialsCountConfig {
    public bool itemSlot;

    public static UnloadedAvailableMaterialsCountConfig Instance => UnloadedVisualChangesConfig.Instance.availableMaterialsCount;
}
