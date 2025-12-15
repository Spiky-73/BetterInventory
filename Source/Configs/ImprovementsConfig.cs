using BetterInventory.Improvements.BetterRecipeGrid;
using BetterInventory.Improvements.BetterRecipeList;
using BetterInventory.Improvements.MoreMaterials;
using BetterInventory.Improvements.ScrollableTooltip;
using BetterInventory.Improvements.SmartConsumption;
using SpikysLib.Configs;
using Terraria.ModLoader.Config;

namespace BetterInventory.Configs;

using IUnloadableAttribute = UnloadableAttribute<UnloadedImprovementsConfig>;

public sealed class ImprovementsConfig : ModConfig {

    [IUnloadable(nameof(betterRecipeList))] public Toggle<BetterRecipeListConfig> betterRecipeList = new(true);
    [IUnloadable(nameof(betterRecipeGrid))] public Toggle<BetterRecipeGridConfig> betterRecipeGrid = new(true);
    public Toggle<MoreMaterialsConfig> moreMaterials = new(true);
    public Toggle<ScrollableTooltipConfig> scrollableTooltip = new(true);
    [IUnloadable(nameof(smartConsumption))] public Toggle<SmartConsumptionConfig> smartConsumption = new(true);

    public static ImprovementsConfig Instance = null!;
    public static bool SmartConsumption => Instance.smartConsumption;

    public override ConfigScope Mode => ConfigScope.ClientSide;
}

public sealed class UnloadedImprovementsConfig {
    public UnloadedBetterRecipeListConfig betterRecipeList = new();
    public UnloadedBetterRecipeGridConfig betterRecipeGrid = new();
    public UnloadedSmartConsumptionConfig smartConsumption = new();

    public static UnloadedImprovementsConfig Instance => CompatibilityConfig.Instance.unloadedImprovements;
}
