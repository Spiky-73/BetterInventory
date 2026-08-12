using System.ComponentModel;
using SpikysLib.Configs;
using Terraria.ModLoader.Config;

namespace BetterInventory.BetterRecipeList;

public sealed class BetterRecipeListConfig : ModConfig {
    public override bool Autoload(ref string name) => BetterInventoryConfig.EnsureLoaded(this) && base.Autoload(ref name) && BetterInventoryConfig.BetterRecipeList;

    [DefaultValue(true)] public bool craftWhenHolding = true;
    [Fallible] public Toggle<FastScrollConfig> fastScroll = new(true);
    [Fallible] public Toggle<CraftOnRecipeGridConfig> craftOnRecipeGrid = new(true);
    [Fallible, DefaultValue(true)] public bool refocusButton = true;
    [Fallible, DefaultValue(true)] public bool noRecGridOffset = true;
    [Fallible, DefaultValue(true)] public bool noRecGridClose = true;
    [DefaultValue(true)] public bool rememberGridPosition = true;
    [Fallible, DefaultValue(true)] public bool pageScroll = true;
    [Fallible, DefaultValue(true)] public bool recipeCount = true;
    public Toggle<RecipeTooltipConfig> recipeTooltip = new(true);
    [Fallible] public Toggle<AvailableMaterialsCountConfig> availableMaterialsCount = new(true);
    [Fallible, DefaultValue(true)] public bool materialsWrapping;
    [Fallible] public Toggle<RecipeFiltersConfig> recipeFilters = new();

    public static BetterRecipeListConfig Instance = null!;
    public static bool CraftWhenHolding => BetterInventoryConfig.BetterRecipeList && Instance.craftWhenHolding;
    public static bool FastScroll => BetterInventoryConfig.BetterRecipeList && Instance.fastScroll && !FailedBetterRecipeListConfig.Instance.fastScroll;
    public static bool CraftOnRecGrid => BetterInventoryConfig.BetterRecipeList && Instance.craftOnRecipeGrid && !FailedBetterRecipeListConfig.Instance.craftOnRecipeGrid;
    public static bool RefocusButton => BetterInventoryConfig.BetterRecipeList && Instance.refocusButton && !FailedBetterRecipeListConfig.Instance.refocusButton;
    public static bool NoRecGridOffset => BetterInventoryConfig.BetterRecipeList && Instance.noRecGridOffset && !FailedBetterRecipeListConfig.Instance.noRecGridOffset;
    public static bool NoRecGridClose => BetterInventoryConfig.BetterRecipeList && Instance.noRecGridClose && !FailedBetterRecipeListConfig.Instance.noRecGridClose;
    public static bool RememberGridPosition => BetterInventoryConfig.BetterRecipeList && Instance.rememberGridPosition;
    public static bool PageScroll => BetterInventoryConfig.BetterRecipeList && Instance.pageScroll && !FailedBetterRecipeListConfig.Instance.pageScroll;
    public static bool RecipeCount => BetterInventoryConfig.BetterRecipeList && Instance.recipeCount && !FailedBetterRecipeListConfig.Instance.recipeCount;
    public static bool RecipeTooltip => BetterInventoryConfig.BetterRecipeList && Instance.recipeTooltip;
    public static bool AvailableMaterialsCount => BetterInventoryConfig.BetterRecipeList && Instance.availableMaterialsCount;
    public static bool MaterialsWrapping => BetterInventoryConfig.BetterRecipeList && Instance.materialsWrapping && !FailedBetterRecipeListConfig.Instance.materialsWrapping;
    public static bool RecipeFilters => BetterInventoryConfig.BetterRecipeList && Instance.recipeFilters && !FailedBetterRecipeListConfig.Instance.recipeFilters;

    public override ConfigScope Mode => ConfigScope.ClientSide;
}

public sealed class FastScrollConfig {
    [DefaultValue(true)] public bool listScroll = true;

    public static FastScrollConfig Instance => BetterRecipeListConfig.Instance.fastScroll.Value;
}

public sealed class CraftOnRecipeGridConfig {
    [DefaultValue(false)] public bool focusHovered = false;

    public static CraftOnRecipeGridConfig Instance => BetterRecipeListConfig.Instance.craftOnRecipeGrid.Value;
}

public sealed class RecipeTooltipConfig {
    [DefaultValue(false)] public bool objectsLine = false;

    public static RecipeTooltipConfig Instance => BetterRecipeListConfig.Instance.recipeTooltip.Value;
}

public sealed class AvailableMaterialsCountConfig {
    [DefaultValue(true)] public bool tooltip = true;
    [Fallible, DefaultValue(true)] public bool itemSlot = true;

    public static AvailableMaterialsCountConfig Instance => BetterRecipeListConfig.Instance.availableMaterialsCount.Value;
    public static bool Tooltip => BetterRecipeListConfig.AvailableMaterialsCount && Instance.tooltip;
    public static bool ItemSlot => BetterRecipeListConfig.AvailableMaterialsCount && Instance.itemSlot && !FailedAvailableMaterialsCountConfig.Instance.itemSlot;
}

public sealed class RecipeFiltersConfig {
    [DefaultValue(true)] public bool simpleSearch = true;
    // [DefaultValue(true)] public bool hideUnavailableFilters = true; // TODO reimplement ?
    [DefaultValue(14 * 4 + 3 * 6), Range(0, 220)] public int minWidth = 14 * 4 + 3 * 6;
    [DefaultValue(true)] public bool expand = true;

    public static RecipeFiltersConfig Instance => BetterRecipeListConfig.Instance.recipeFilters.Value;
}

public sealed class FailedBetterRecipeListConfig {
    public bool fastScroll;
    public bool craftOnRecipeGrid;
    public bool refocusButton;
    public bool noRecGridOffset;
    public bool noRecGridClose;
    public bool pageScroll;
    public bool recipeCount;
    public FailedAvailableMaterialsCountConfig availableMaterialsCount = new();
    public bool materialsWrapping;
    public bool recipeFilters;

    public static FailedBetterRecipeListConfig Instance => FailedBetterInventoryConfig.Instance.betterRecipeList;
}

public sealed class FailedAvailableMaterialsCountConfig {
    public bool itemSlot;

    public static FailedAvailableMaterialsCountConfig Instance => FailedBetterRecipeListConfig.Instance.availableMaterialsCount;
}