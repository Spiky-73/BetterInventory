
using System.ComponentModel;
using BetterInventory.Configs;

namespace BetterInventory.Improvements.SmartConsumption;

using SCUnloadable = UnloadableAttribute<UnloadedSmartConsumptionConfig>;

public sealed class SmartConsumptionConfig {
    [DefaultValue(true)] public bool consumables = true;
    [DefaultValue(true)] public bool ammo = true;
    [SCUnloadable(nameof(baits)), DefaultValue(true)] public bool baits = true;
    [DefaultValue(true)] public bool paints = true;
    [SCUnloadable(nameof(materials)), DefaultValue(true)] public bool materials = true;
    [DefaultValue(false)] public bool mouse = false;
    [DefaultValue(false)] public bool self = false;

    public static SmartConsumptionConfig Instance => ImprovementsConfig.Instance.smartConsumption.Value;
    public static bool Consumables => ImprovementsConfig.SmartConsumption && Instance.consumables;
    public static bool Ammo => ImprovementsConfig.SmartConsumption && Instance.ammo;
    public static bool Baits => ImprovementsConfig.SmartConsumption && Instance.baits && !UnloadedSmartConsumptionConfig.Instance.baits;
    public static bool Paints => ImprovementsConfig.SmartConsumption && Instance.paints;
    public static bool Materials => ImprovementsConfig.SmartConsumption && Instance.materials && !UnloadedSmartConsumptionConfig.Instance.materials;
}

public sealed class UnloadedSmartConsumptionConfig {
    public bool baits;
    public bool materials;

    public static UnloadedSmartConsumptionConfig Instance => UnloadedImprovementsConfig.Instance.smartConsumption;
}