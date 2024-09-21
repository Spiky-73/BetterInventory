using System.ComponentModel;
using Terraria.ModLoader.Config;
using SpikysLib.Configs;
using Newtonsoft.Json;
using SpikysLib;

namespace BetterInventory.Configs;

public sealed class Crafting : ModConfig {
    public Toggle<FixedUI> fixedUI = new(true);
    public Toggle<RecipeFilters> recipeFilters = new(true);
    public Toggle<CraftOnList> craftOnList = new(true);
    [DefaultValue(true)] public bool mouseMaterial;

    public static bool MouseMaterial => Instance.mouseMaterial;
    public static Crafting Instance = null!;

    public override ConfigScope Mode => ConfigScope.ClientSide;
}

public sealed class FixedUI {
    public Toggle<FastScroll> fastScroll = new(true);
    [DefaultValue(true)] public bool scrollButtons = true;
    [DefaultValue(true)] public bool wrapping = true;
    [DefaultValue(true)] public bool craftWhenHolding = true;

    public static bool Enabled => Crafting.Instance.fixedUI;
    public static bool FastScroll => Enabled && Value.fastScroll && !UnloadedCrafting.Value.fastScroll;
    public static bool ScrollButtons => Enabled && Value.scrollButtons && !UnloadedCrafting.Value.scrollButtons;
    public static bool Wrapping => Enabled && Value.wrapping && !UnloadedCrafting.Value.wrapping;
    public static bool CraftWhenHolding => Enabled && Value.craftWhenHolding;
    public static FixedUI Value => Crafting.Instance.fixedUI.Value;
    
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

public sealed class CraftOnList {
    [DefaultValue(false)] public bool focusHovered = false;

    public static bool Enabled => Crafting.Instance.craftOnList && !UnloadedCrafting.Value.craftOnList;
    public static CraftOnList Value => Crafting.Instance.craftOnList.Value;

    // Compatibility version < v0.6
    [JsonProperty, DefaultValue(false)] private bool focusRecipe { set => ConfigHelper.MoveMember(value, _ => focusHovered = value); }
}
