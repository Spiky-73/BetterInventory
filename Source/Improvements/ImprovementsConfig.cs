using BetterInventory.Improvements.BetterRecipeGrid;
using BetterInventory.Improvements.BetterRecipeList;
using BetterInventory.Improvements.SmartConsumption;
using SpikysLib.Configs;
using Terraria.ModLoader.Config;
using BetterInventory.Configs;
using System.ComponentModel;
using BetterInventory.Improvements.FavoriteInBanks;
using BetterInventory.Improvements.BetterQuickStack;
using BetterInventory.Improvements.BetterTrash;
using BetterInventory.Improvements.BetterTooltip;
using BetterInventory.Improvements.FastGrabBags;
using BetterInventory.Improvements.MoreCraftingMaterials;
using BetterInventory.Improvements.UnknownEntities;

namespace BetterInventory.Improvements;

using IUnloadableAttribute = UnloadableAttribute<UnloadedImprovementsConfig>;

public sealed class ImprovementsConfig : ModConfig {

    [IUnloadable(nameof(betterRecipeList))] public Toggle<BetterRecipeListConfig> betterRecipeList = new(true);
    [IUnloadable(nameof(betterRecipeGrid))] public Toggle<BetterRecipeGridConfig> betterRecipeGrid = new(true);
    public Toggle<MoreCraftingMaterialsConfig> moreCraftingMaterials = new(true);
    [IUnloadable(nameof(betterTooltip))] public Toggle<BetterTooltipConfig> betterTooltip = new(true);
    [IUnloadable(nameof(smartConsumption))] public Toggle<SmartConsumptionConfig> smartConsumption = new(true);
    [IUnloadable(nameof(favoriteInBanks)), DefaultValue(true)] public bool favoriteInBanks;
    [IUnloadable(nameof(betterQuickStack))] public Toggle<BetterQuickStackConfig> betterQuickStack = new(true);
    [IUnloadable(nameof(betterTrash))] public Toggle<BetterTrashConfig> betterTrash = new(true);
    public Toggle<FastGrabBagsConfig> fastGrabBags = new(true);
    [DefaultValue(true)] public bool keepSwappedFavorited = true;
    [DefaultValue(true)] public bool unlockFilter = true;
    [IUnloadable(nameof(unknownEntities))] public Toggle<UnknownEntitiesConfig> unknownEntities = new(true);

    public static ImprovementsConfig Instance = null!;
    public static bool SmartConsumption => Instance.smartConsumption;
    public static bool BetterTooltip => Instance.betterTooltip;
    public static bool MoreCraftingMaterials => Instance.moreCraftingMaterials;
    public static bool BetterRecipeList => Instance.betterRecipeList;
    public static bool BetterRecipeGrid => Instance.betterRecipeGrid;
    public static bool FavoriteInBanks => Instance.favoriteInBanks && !UnloadedImprovementsConfig.Instance.favoriteInBanks;
    public static bool BetterQuickStack => Instance.betterQuickStack;
    public static bool BetterTrash => Instance.betterTrash;
    public static bool FastGrabBags => Instance.fastGrabBags;
    public static bool KeepSwappedFavorited => Instance.keepSwappedFavorited;
    public static bool UnlockFilter => Instance.unlockFilter;
    public static bool UnknownEntities => Instance.unknownEntities;

    public override ConfigScope Mode => ConfigScope.ClientSide;

    public override void OnChanged() {
        FavoriteInBanksPlayer.OnConfigChanged();
    }
}

public sealed class UnloadedImprovementsConfig {
    public UnloadedBetterRecipeListConfig betterRecipeList = new();
    public UnloadedBetterRecipeGridConfig betterRecipeGrid = new();
    public UnloadedSmartConsumptionConfig smartConsumption = new();
    public bool favoriteInBanks;
    public UnloadedBetterQuickStackConfig betterQuickStack = new();
    public UnloadedBetterTrashConfig betterTrash = new();
    public UnloadedBetterTooltipConfig betterTooltip = new();
    public UnloadedUnknownEntitiesConfig unknownEntities = new();

    public static UnloadedImprovementsConfig Instance => CompatibilityConfig.Instance.unloadedImprovements;
}
