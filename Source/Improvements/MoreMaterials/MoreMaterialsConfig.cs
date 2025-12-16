using System.ComponentModel;
using BetterInventory.Configs;
using SpikysLib.Configs;

namespace BetterInventory.Improvements.MoreMaterials;

public sealed class MoreMaterialsConfig {
    [DefaultValue(true)] public bool mouse = true;
    public Toggle<EquipmentMaterialsConfig> equipment = new(true);

    public static MoreMaterialsConfig Instance => ImprovementsConfig.Instance.moreMaterials.Value;
}

public sealed class EquipmentMaterialsConfig {
    [DefaultValue(false)] public bool allLoadouts = false;

    public static EquipmentMaterialsConfig Instance => MoreMaterialsConfig.Instance.equipment.Value;
}
