using System.ComponentModel;
using Terraria.ModLoader.Config;
using SpikysLib.Configs;
using Newtonsoft.Json;

namespace BetterInventory.Configs;

public sealed class Crafting : ModConfig {
    public Toggle<RecipeSearchBar> recipeSearchBar = new(true);
    public Toggle<RecipeFilters> recipeFilters = new(true);
    [DefaultValue(true)] public bool recipeSort = true;

    public static bool RecipeSort => Instance.recipeSort && !UnloadedCrafting.Value.recipeSort;
    public static bool RecipeUI => Instance.recipeSearchBar || Instance.recipeFilters || Instance.recipeSort;
    public static Crafting Instance = null!;

    public override void OnChanged() => global::BetterInventory.Crafting.RecipeUI.RebuildUI();

    public override ConfigScope Mode => ConfigScope.ClientSide;
}

public sealed class RecipeFilters {
    [DefaultValue(true)] public bool hideUnavailable = true;
    [Range(1, 6), DefaultValue(4)] public int filtersPerLine = 4;

    public static bool Enabled => Crafting.Instance.recipeFilters && !UnloadedCrafting.Value.recipeFilters;
    public static RecipeFilters Value => Crafting.Instance.recipeFilters.Value;

    // Compatibility version < v0.6
    [JsonProperty, DefaultValue(4)] private int width { set => ConfigHelper.MoveMember(value != 4, _ => filtersPerLine = value); }
}

public sealed class RecipeSearchBar {
    [DefaultValue(true)] public bool expand = true;
    [DefaultValue(14 * 4 + 3 * 6), Range(0, 220)] public int minWidth = 14 * 4 + 3 * 6;
    [DefaultValue(true)] public bool simpleSearch = true;

    public static bool Enabled => Crafting.Instance.recipeSearchBar && !UnloadedCrafting.Value.recipeSearchBar;
    public static RecipeSearchBar Value => Crafting.Instance.recipeSearchBar.Value;
}
