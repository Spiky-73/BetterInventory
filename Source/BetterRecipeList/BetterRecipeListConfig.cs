using System.ComponentModel;
using SpikysLib.Configs;
using Terraria.ModLoader.Config;

namespace BetterInventory.BetterRecipeList;

using BRLUnloadableAttribute = UnloadableAttribute<UnloadedBetterRecipeListConfig>;
using AMCUnloadableAttribute = UnloadableAttribute<UnloadedAvailableMaterialsCountConfig>;

public sealed class BetterRecipeListConfig : ModConfig {
    [DefaultValue(true)] public bool craftWhenHolding = true;
    [BRLUnloadable(nameof(fastScroll))] public Toggle<FastScrollConfig> fastScroll = new(true);
    [BRLUnloadable(nameof(craftOnRecipeGrid))] public Toggle<CraftOnRecipeGridConfig> craftOnRecipeGrid = new(true);
    [BRLUnloadable(nameof(refocusButton)), DefaultValue(true)] public bool refocusButton = true;
    [BRLUnloadable(nameof(noRecGridOffset)), DefaultValue(true)] public bool noRecGridOffset = true;
    [BRLUnloadable(nameof(noRecGridClose)), DefaultValue(true)] public bool noRecGridClose = true;
    [DefaultValue(true)] public bool rememberGridPosition = true;
    [BRLUnloadable(nameof(pageScroll)), DefaultValue(true)] public bool pageScroll = true;
    [BRLUnloadable(nameof(recipeCount)), DefaultValue(true)] public bool recipeCount = true;
    public Toggle<RecipeTooltipConfig> recipeTooltip = new(true);
    [BRLUnloadable(nameof(availableMaterialsCount))] public Toggle<AvailableMaterialsCountConfig> availableMaterialsCount = new(true);
    [BRLUnloadable(nameof(materialsWrapping)), DefaultValue(true)] public bool materialsWrapping;
    [BRLUnloadable(nameof(recipeFilters))] public Toggle<RecipeFiltersConfig> recipeFilters = new();

    public static BetterRecipeListConfig Instance = null!;
    public static bool CraftWhenHolding => BetterInventoryConfig.BetterRecipeList && Instance.craftWhenHolding;
    public static bool FastScroll => BetterInventoryConfig.BetterRecipeList && Instance.fastScroll && !UnloadedBetterRecipeListConfig.Instance.fastScroll;
    public static bool CraftOnRecGrid => BetterInventoryConfig.BetterRecipeList && Instance.craftOnRecipeGrid && !UnloadedBetterRecipeListConfig.Instance.craftOnRecipeGrid;
    public static bool RefocusButton => BetterInventoryConfig.BetterRecipeList && Instance.refocusButton && !UnloadedBetterRecipeListConfig.Instance.refocusButton;
    public static bool NoRecGridOffset => BetterInventoryConfig.BetterRecipeList && Instance.noRecGridOffset && !UnloadedBetterRecipeListConfig.Instance.noRecGridOffset;
    public static bool NoRecGridClose => BetterInventoryConfig.BetterRecipeList && Instance.noRecGridClose && !UnloadedBetterRecipeListConfig.Instance.noRecGridClose;
    public static bool RememberGridPosition => BetterInventoryConfig.BetterRecipeList && Instance.rememberGridPosition;
    public static bool PageScroll => BetterInventoryConfig.BetterRecipeList && Instance.pageScroll && !UnloadedBetterRecipeListConfig.Instance.pageScroll;
    public static bool RecipeCount => BetterInventoryConfig.BetterRecipeList && Instance.recipeCount && !UnloadedBetterRecipeListConfig.Instance.recipeCount;
    public static bool RecipeTooltip => BetterInventoryConfig.BetterRecipeList && Instance.recipeTooltip;
    public static bool AvailableMaterialsCount => BetterInventoryConfig.BetterRecipeList && Instance.availableMaterialsCount;
    public static bool MaterialsWrapping => BetterInventoryConfig.BetterRecipeList && Instance.materialsWrapping && !UnloadedBetterRecipeListConfig.Instance.materialsWrapping;
    public static bool RecipeFilters => BetterInventoryConfig.BetterRecipeList && Instance.recipeFilters && !UnloadedBetterRecipeListConfig.Instance.recipeFilters;

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
    [AMCUnloadable(nameof(itemSlot)), DefaultValue(true)] public bool itemSlot = true;

    public static AvailableMaterialsCountConfig Instance => BetterRecipeListConfig.Instance.availableMaterialsCount.Value;
    public static bool Tooltip => BetterRecipeListConfig.AvailableMaterialsCount && Instance.tooltip;
    public static bool ItemSlot => BetterRecipeListConfig.AvailableMaterialsCount && Instance.itemSlot && !UnloadedAvailableMaterialsCountConfig.Instance.itemSlot;
}

public sealed class UnloadedAvailableMaterialsCountConfig {
    public bool itemSlot;

    public static UnloadedAvailableMaterialsCountConfig Instance => UnloadedBetterRecipeListConfig.Instance.availableMaterialsCount;
}

public sealed class RecipeFiltersConfig {
    [DefaultValue(true)] public bool simpleSearch = true;
    // [DefaultValue(true)] public bool hideUnavailableFilters = true; // TODO reimplement ?
    [DefaultValue(14 * 4 + 3 * 6), Range(0, 220)] public int minWidth = 14 * 4 + 3 * 6;
    [DefaultValue(true)] public bool expand = true;

    public static RecipeFiltersConfig Instance => BetterRecipeListConfig.Instance.recipeFilters.Value;
}

public sealed class UnloadedBetterRecipeListConfig {
    public bool fastScroll;
    public bool craftOnRecipeGrid;
    public bool refocusButton;
    public bool noRecGridOffset;
    public bool noRecGridClose;
    public bool pageScroll;
    public bool recipeCount;
    public UnloadedAvailableMaterialsCountConfig availableMaterialsCount = new();
    public bool materialsWrapping;
    public bool recipeFilters;

    public static UnloadedBetterRecipeListConfig Instance => BetterInventoryConfig.Instance.unloadedBetterRecipeList;
}
