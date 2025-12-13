using System.ComponentModel;
using SpikysLib.Configs;
using Terraria.ModLoader.Config;

namespace BetterInventory.Configs;

public sealed class Features : ModConfig {

    public Toggle<RecipeFiltering> recipeFiltering = new(true);
    public Toggle<QuickMove> quickMove = new(true);

    public static Features Instance = null!;
    public static bool RecipeFiltering => Instance.recipeFiltering;
    public static bool QuickMove => Instance.quickMove;

    public override ConfigScope Mode => ConfigScope.ClientSide;

    public override void OnChanged() {
        global::BetterInventory.Features.RecipeFiltering.RecipeFiltering.RebuildUI();
    }
}

public sealed class RecipeFiltering {
    public Toggle<RecipeSearchBar> search = new(true);
    public Toggle<RecipeFilters> filters = new(true);
    [DefaultValue(true)] public bool sort = true;

    public static RecipeFiltering Instance => Features.Instance.recipeFiltering.Value;
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

public sealed class QuickMove {
    [DefaultValue(HotkeyMode.Hotbar)] public HotkeyMode hotkeyMode = HotkeyMode.Hotbar;
    [Range(0, 3600), DefaultValue(60 * 3)] public int graceTime = 60 * 3;
    [DefaultValue(true)] public bool followItem = true;
    [DefaultValue(true)] public bool bringItem = true;
    [DefaultValue(true)] public bool returnToSlot = true;
    [DefaultValue(false)] public bool inactiveInventories = false;
    [DefaultValue(HotkeyDisplayMode.All)] public HotkeyDisplayMode displayedHotkeys = HotkeyDisplayMode.All;
    [DefaultValue(false)] public bool tooltip = false;

    public static bool DisplayHotkeys =>  Features.QuickMove && Instance.displayedHotkeys != HotkeyDisplayMode.None && !UnloadedFeatures.Instance.quickMove_displayHotkeys;
    public static bool Tooltip =>  Features.QuickMove && Instance.tooltip;
    public static QuickMove Instance => InventoryManagement.Instance.quickMove.Value;
}
