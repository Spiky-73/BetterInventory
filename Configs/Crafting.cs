using System.ComponentModel;
using Terraria.ModLoader.Config;
using SpikysLib.Configs;
using Newtonsoft.Json;
using BetterInventory.ItemSearch;
using Terraria;

namespace BetterInventory.Configs;

public sealed class Crafting : ModConfig {
    public Toggle<FixedUI> fixedUI = new(true);
    [DefaultValue(true)] public bool recipeSearchBar;
    public Toggle<RecipeFilters> recipeFilters = new(true);
    public Toggle<CraftOnList> craftOnList = new(true);
    [DefaultValue(true)] public bool mouseMaterial;
    public Toggle<AvailableMaterials> availableMaterials = new(true);

    public static bool MouseMaterial => Instance.mouseMaterial;
    public static bool RecipeSearchBar => Instance.recipeSearchBar;
    public static bool RecipeUI => RecipeSearchBar || Instance.recipeFilters;
    public static Crafting Instance = null!;

    public override void OnChanged() {
        if (Guide.recipeUI?.filters is not null) Guide.recipeUI.filters.ItemsPerLine = RecipeFilters.Value.filtersPerLine;
        if (!Main.gameMenu && Guide.recipeUI is not null) Guide.recipeUI.RebuildList();
    }

    public override ConfigScope Mode => ConfigScope.ClientSide;
}

public sealed class FixedUI {
    public Toggle<FastScroll> fastScroll = new(true);
    [DefaultValue(true)] public bool scrollButtons = true;
    [DefaultValue(true)] public bool wrapping = true;
    [DefaultValue(true)] public bool craftWhenHolding = true;
    [DefaultValue(true)] public bool recipeCount = true;
    [DefaultValue(true)] public bool noRecStartOffset = true;

    public static bool Enabled => Crafting.Instance.fixedUI;
    public static bool FastScroll => Enabled && Value.fastScroll && !UnloadedCrafting.Value.fastScroll;
    public static bool ScrollButtons => Enabled && Value.scrollButtons && !UnloadedCrafting.Value.scrollButtons;
    public static bool Wrapping => Enabled && Value.wrapping && !UnloadedCrafting.Value.wrapping;
    public static bool CraftWhenHolding => Enabled && Value.craftWhenHolding;
    public static bool RecipeCount => Enabled && Value.recipeCount && !UnloadedCrafting.Value.recipeCount;
    public static bool NoRecStartOffset => Enabled && Value.noRecStartOffset && !UnloadedCrafting.Value.noRecStartOffset;
    public static FixedUI Value => Crafting.Instance.fixedUI.Value;
    
    // Compatibility version < v0.6
    [JsonProperty, DefaultValue(true)] private bool listScroll { set => ConfigHelper.MoveMember(!value, _ => scrollButtons = value); }
}

public sealed class FastScroll {
    [DefaultValue(true)] public bool listScroll = true;
    
    public static FastScroll Value => Crafting.Instance.fixedUI.Value.fastScroll.Value;
}

public sealed class RecipeFilters {
    [DefaultValue(true)] public bool searchBar = true;
    [DefaultValue(true)] public bool hideUnavailable = true;
    [Range(1, 6), DefaultValue(4)] public int filtersPerLine = 4;

    public static bool Enabled => Crafting.Instance.recipeFilters && !UnloadedCrafting.Value.recipeFilters;
    public static RecipeFilters Value => Crafting.Instance.recipeFilters.Value;

    // Compatibility version < v0.6
    [JsonProperty, DefaultValue(4)] private int width { set => ConfigHelper.MoveMember(value != 4, _ => filtersPerLine = value); }
}

public sealed class CraftOnList {
    [DefaultValue(false)] public bool focusHovered = false;

    public static bool Enabled => Crafting.Instance.craftOnList && !UnloadedCrafting.Value.craftOnList;
    public static CraftOnList Value => Crafting.Instance.craftOnList.Value;

    // Compatibility version < v0.6
    [JsonProperty, DefaultValue(false)] private bool focusRecipe { set => ConfigHelper.MoveMember(value, _ => focusHovered = value); }
}

public class AvailableMaterials {
    [DefaultValue(true)] public bool tooltip = true;
    [DefaultValue(true)] public bool itemSlot = true;

    public static bool Enabled => Crafting.Instance.availableMaterials;
    public static AvailableMaterials Value => Crafting.Instance.availableMaterials.Value;
    public static bool Tooltip => Enabled && Value.tooltip;
    public static bool ItemSlot => Enabled && Value.itemSlot && !UnloadedCrafting.Value.availableMaterialsItemSlot;
}
