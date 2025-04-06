using System.ComponentModel;
using Terraria.ModLoader.Config;
using SpikysLib.Configs;
using Newtonsoft.Json;

namespace BetterInventory.Configs;

public sealed class Crafting : ModConfig {
    public Toggle<FixedUI> fixedUI = new(true);
    public Toggle<RecipeSearchBar> recipeSearchBar = new(true);
    public Toggle<RecipeFilters> recipeFilters = new(true);
    public Toggle<CraftOnList> craftOnList = new(true);
    public Toggle<MoreMaterials> moreMaterials = new(true);
    public Toggle<AvailableMaterials> availableMaterials = new(true);
    public Toggle<RecipeTooltip> recipeTooltip = new(true);

    [JsonProperty, DefaultValue(true)] private bool mouseMaterial { set => ConfigHelper.MoveMember(!value, _ => moreMaterials.Key = value); }

    public static bool RecipeUI => Instance.recipeSearchBar || Instance.recipeFilters;
    public static Crafting Instance = null!;

    public override void OnChanged() => global::BetterInventory.Crafting.RecipeUI.RebuildUI();

    public override ConfigScope Mode => ConfigScope.ClientSide;
}

public sealed class FixedUI {
    public Toggle<FastScroll> fastScroll = new(true);
    [DefaultValue(true)] public bool scrollButtons = true;
    [DefaultValue(true)] public bool wrapping = true;
    [DefaultValue(true)] public bool craftWhenHolding = true;
    [DefaultValue(true)] public bool recipeCount = true;
    [DefaultValue(true)] public bool noRecStartOffset = true;
    [DefaultValue(true)] public bool noRecListClose = true;
    [DefaultValue(true)] public bool rememberListPosition = true;
    [DefaultValue(true)] public bool focusButton = true;

    public static bool Enabled => Crafting.Instance.fixedUI;
    public static bool FastScroll => Enabled && Value.fastScroll && !UnloadedCrafting.Value.fastScroll;
    public static bool ScrollButtons => Enabled && Value.scrollButtons && !UnloadedCrafting.Value.scrollButtons;
    public static bool Wrapping => Enabled && Value.wrapping && !UnloadedCrafting.Value.wrapping;
    public static bool CraftWhenHolding => Enabled && Value.craftWhenHolding;
    public static bool RecipeCount => Enabled && Value.recipeCount && !UnloadedCrafting.Value.recipeCount;
    public static bool NoRecStartOffset => Enabled && Value.noRecStartOffset && !UnloadedCrafting.Value.noRecStartOffset;
    public static bool NoRecListClose => Enabled && Value.noRecListClose && !UnloadedCrafting.Value.noRecListClose;
    public static bool RememberListPosition => Enabled && Value.rememberListPosition;
    public static bool FocusButton => Enabled && Value.focusButton && !UnloadedCrafting.Value.focusButton;
    public static FixedUI Value => Crafting.Instance.fixedUI.Value;

    public static bool RecipeListUI => RecipeCount || FocusButton;

    // Compatibility version < v0.6
    [JsonProperty, DefaultValue(true)] private bool listScroll { set => ConfigHelper.MoveMember(!value, _ => scrollButtons = value); }
}

public sealed class FastScroll {
    [DefaultValue(true)] public bool listScroll = true;
    
    public static FastScroll Value => Crafting.Instance.fixedUI.Value.fastScroll.Value;
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

    public static bool Enabled => Crafting.Instance.recipeSearchBar && !UnloadedCrafting.Value.recipeSearchBar;
    public static RecipeSearchBar Value => Crafting.Instance.recipeSearchBar.Value;
}

public sealed class CraftOnList {
    [DefaultValue(false)] public bool focusHovered = false;

    public static bool Enabled => Crafting.Instance.craftOnList && !UnloadedCrafting.Value.craftOnList;
    public static CraftOnList Value => Crafting.Instance.craftOnList.Value;

    // Compatibility version < v0.6
    [JsonProperty, DefaultValue(false)] private bool focusRecipe { set => ConfigHelper.MoveMember(value, _ => focusHovered = value); }
}

public sealed class MoreMaterials {
    [DefaultValue(true)] public bool mouse = true;
    public Toggle<EquipementMaterials> equipment = new(true);

    public static bool Enabled => Crafting.Instance.moreMaterials;
    public static MoreMaterials Value => Crafting.Instance.moreMaterials.Value;
    public static bool Mouse => Enabled && Value.mouse;
    public static bool Equipment => Enabled && Value.equipment;
}
public sealed class EquipementMaterials {
    [DefaultValue(false)] public bool allLoadouts = false;
}

public sealed class AvailableMaterials {
    [DefaultValue(true)] public bool tooltip = true;
    [DefaultValue(true)] public bool itemSlot = true;

    public static bool Enabled => Crafting.Instance.availableMaterials;
    public static AvailableMaterials Value => Crafting.Instance.availableMaterials.Value;
    public static bool Tooltip => Enabled && Value.tooltip;
    public static bool ItemSlot => Enabled && Value.itemSlot && !UnloadedCrafting.Value.availableMaterialsItemSlot;
}

public sealed class RecipeTooltip {
    [DefaultValue(false)] public bool objectsLine = false;

    public static bool Enabled => Crafting.Instance.recipeTooltip;
    public static RecipeTooltip Value => Crafting.Instance.recipeTooltip.Value;

}
