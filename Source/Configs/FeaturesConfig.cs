using System.ComponentModel;
using BetterInventory.Features.QuickMove;
using SpikysLib.Configs;
using Terraria.ModLoader.Config;

namespace BetterInventory.Configs;

public sealed class FeaturesConfig : ModConfig {

    public Toggle<RecipeFiltering> recipeFiltering = new(true);
    public Toggle<QuickMoveConfig> quickMove = new(true);

    public static FeaturesConfig Instance = null!;
    public static bool RecipeFiltering => Instance.recipeFiltering;

    public override ConfigScope Mode => ConfigScope.ClientSide;

    public override void OnChanged() {
        Features.RecipeFiltering.RecipeFiltering.RebuildUI();
    }
}

public sealed class UnloadedFeaturesConfig {
    public UnloadedQuickMoveConfig quickMove = new();

    public static UnloadedFeaturesConfig Instance => CompatibilityConfig.Instance.unloadedFeatures;
}


public sealed class RecipeFiltering {
    public Toggle<RecipeSearchBar> search = new(true);
    public Toggle<RecipeFilters> filters = new(true);
    [DefaultValue(true)] public bool sort = true;

    public static RecipeFiltering Instance => FeaturesConfig.Instance.recipeFiltering.Value;
    public static bool Search => Instance.search;
    public static bool Filters => Instance.filters;
    public static bool Sort => Instance.sort;
}
public sealed class RecipeSearchBar {
    [DefaultValue(true)] public bool expand = true;
    [DefaultValue(14 * 4 + 3 * 6), Range(0, 220)] public int minWidth = 14 * 4 + 3 * 6;
    [DefaultValue(true)] public bool simpleSearch = true;

    public static RecipeSearchBar Instance => RecipeFiltering.Instance.search.Value;
}
public sealed class RecipeFilters {
    [DefaultValue(true)] public bool hideUnavailable = true; // TODO reimplement ?
    [Range(1, 6), DefaultValue(4)] public int filtersPerLine = 4;

    public static RecipeFilters Instance => RecipeFiltering.Instance.filters.Value;
}

public enum HotkeyDisplayMode { None, Next, All }
public enum HotkeyMode { Hotbar, FromEnd, Reversed }
