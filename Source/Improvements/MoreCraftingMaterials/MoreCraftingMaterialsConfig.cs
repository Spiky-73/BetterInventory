using System.ComponentModel;
using SpikysLib.Configs;

namespace BetterInventory.Improvements.MoreCraftingMaterials;

public sealed class MoreCraftingMaterialsConfig {
    [DefaultValue(true)] public bool mouse = true;
    public Toggle<EquipmentMaterialsConfig> equipment = new(true);

    public static MoreCraftingMaterialsConfig Instance => ImprovementsConfig.Instance.moreCraftingMaterials.Value;
}

public sealed class EquipmentMaterialsConfig {
    [DefaultValue(false)] public bool allLoadouts = false;

    public static EquipmentMaterialsConfig Instance => MoreCraftingMaterialsConfig.Instance.equipment.Value;
}
