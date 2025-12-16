
using System;
using System.ComponentModel;
using SpikysLib.Configs;
using Terraria.ModLoader.Config;

namespace BetterInventory.Features.RecipeFiltering;

public sealed class RecipeFilteringConfig {
    public Toggle<RecipeFiltersConfig> filters = new(true);
    public Toggle<RecipeSearchBarConfig> search = new(true);
    [DefaultValue(true)] public bool sort = true;

    public static RecipeFilteringConfig Instance => FeaturesConfig.Instance.recipeFiltering.Value;
    public static bool Filters => Instance.filters;
    public static bool Search => Instance.search;
    public static bool Sort => Instance.sort;

    public static void OnChanged() => RecipeFilteringUI.RebuildUI();
}
public sealed class RecipeSearchBarConfig {
    [DefaultValue(true)] public bool expand = true;
    [DefaultValue(14 * 4 + 3 * 6), Range(0, 220)] public int minWidth = 14 * 4 + 3 * 6;
    [DefaultValue(true)] public bool simpleSearch = true;

    public static RecipeSearchBarConfig Instance => RecipeFilteringConfig.Instance.search.Value;
}
public sealed class RecipeFiltersConfig {
    [DefaultValue(true)] public bool hideUnavailable = true; // TODO reimplement ?
    [Range(1, 6), DefaultValue(4)] public int filtersPerLine = 4;

    public static RecipeFiltersConfig Instance => RecipeFilteringConfig.Instance.filters.Value;
}
