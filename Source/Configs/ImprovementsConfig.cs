using System.ComponentModel;
using BetterInventory.Improvements.BetterRecipeGrid;
using SpikysLib.Configs;
using Terraria.ModLoader.Config;

namespace BetterInventory.Configs;

using IUnloadableAttribute = UnloadableAttribute<UnloadedImprovementsConfig>;

public sealed class ImprovementsConfig : ModConfig {

    public Toggle<BetterRecipeList> betterRecipeList = new(true);
    [IUnloadable(nameof(betterRecipeGrid))]public Toggle<BetterRecipeGridConfig> betterRecipeGrid = new(true);
    public Toggle<MoreMaterials> moreMaterials = new(true);
    public Toggle<ScrollableTooltip> scrollableTooltip = new(true);
    public Toggle<SmartConsumption> smartConsumption = new(true);

    public static ImprovementsConfig Instance = null!;
    public static bool BetterRecipeList => Instance.betterRecipeList;
    public static bool BetterRecipeGrid => Instance.betterRecipeGrid;
    public static bool MoreMaterials => Instance.moreMaterials;
    public static bool ScrollableTooltip => Instance.scrollableTooltip;
    public static bool SmartConsumption => Instance.smartConsumption;

    public override ConfigScope Mode => ConfigScope.ClientSide;
}

public sealed class UnloadedImprovementsConfig {
    public UnloadedBetterRecipeGridConfig betterRecipeGrid = new();

    public static UnloadedImprovementsConfig Instance => CompatibilityConfig.Instance.unloadedImprovements;
}

public sealed class MoreMaterials {
    [DefaultValue(true)] public bool mouse = true;
    public Toggle<EquipmentMaterials> equipment = new(true);

    public static MoreMaterials Instance => ImprovementsConfig.Instance.moreMaterials.Value;
    public static bool Mouse => ImprovementsConfig.MoreMaterials && Instance.mouse;
    public static bool Equipment => ImprovementsConfig.MoreMaterials && Instance.equipment;
}

public sealed class EquipmentMaterials {
    [DefaultValue(false)] public bool allLoadouts = false;
    
    public static EquipmentMaterials Instance => MoreMaterials.Instance.equipment.Value;
}

public sealed class BetterRecipeList {
    [DefaultValue(true)] public bool craftWhenHolding = true;
    public Toggle<FastScroll> fastScroll = new(true);

    public static BetterRecipeList Instance => ImprovementsConfig.Instance.betterRecipeList.Value;
    public static bool CraftWhenHolding => ImprovementsConfig.BetterRecipeList && Instance.craftWhenHolding;
    public static bool FastScroll => ImprovementsConfig.BetterRecipeList && Instance.fastScroll && !UnloadedImprovements.Instance.betterRecipeList_fastScroll;
}

public sealed class FastScroll {
    [DefaultValue(true)] public bool listScroll = true;

    public static FastScroll Instance => BetterRecipeList.Instance.fastScroll.Value;
}

public sealed class ScrollableTooltip {
    [DefaultValue(1)] public float maximumHeight = 1;

    public static ScrollableTooltip Instance = ImprovementsConfig.Instance.scrollableTooltip.Value;
}

public sealed class SmartConsumption {
    [DefaultValue(true)] public bool consumables = true;
    [DefaultValue(true)] public bool ammo = true;
    [DefaultValue(true)] public bool baits = true;
    [DefaultValue(true)] public bool paints = true;
    [DefaultValue(true)] public bool materials = true;
    [DefaultValue(false)] public bool mouse = false;
    [DefaultValue(false)] public bool self = false;

    public static SmartConsumption Value => ImprovementsConfig.Instance.smartConsumption.Value;
    public static bool Consumables => ImprovementsConfig.SmartConsumption && Value.consumables;
    public static bool Ammo => ImprovementsConfig.SmartConsumption && Value.ammo;
    public static bool Baits => ImprovementsConfig.SmartConsumption && Value.baits && !UnloadedImprovements.Instance.smartConsumption_baits;
    public static bool Paints => ImprovementsConfig.SmartConsumption && Value.paints;
    public static bool Materials => ImprovementsConfig.SmartConsumption && Value.materials && !UnloadedImprovements.Instance.smartConsumption_materials;
}