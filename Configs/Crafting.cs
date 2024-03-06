using System.ComponentModel;
using Terraria.ModLoader.Config;
using BetterInventory.Configs.UI;

namespace BetterInventory.Configs;

public sealed class Crafting : ModConfig {
    public Toggle<FixedUI> fixedUI = new(true);
    public Toggle<RecipeFiltering> recipeFiltering = new(true);
    public Toggle<CraftOnList> craftOnList = new(true);
    [DefaultValue(true)] public bool mouseMaterial;

    public static bool MouseMaterial => Instance.mouseMaterial;
    public static Crafting Instance = null!;

    public override ConfigScope Mode => ConfigScope.ClientSide;
}

public sealed class FixedUI {
    public Toggle<FastScroll> fastScroll = new(true);
    [DefaultValue(true)] public bool listScroll = true;
    [DefaultValue(true)] public bool wrapping = true;
    [DefaultValue(true)] public bool moveMouse = true;

    public static bool FastScroll => Crafting.Instance.fixedUI && Crafting.Instance.fixedUI.Value.fastScroll && !Compatibility.Crafting.fastScroll;
    public static bool ListScroll => Crafting.Instance.fixedUI && Crafting.Instance.fixedUI.Value.listScroll && !Compatibility.Crafting.listScroll;
    public static bool Wrapping => Crafting.Instance.fixedUI && Crafting.Instance.fixedUI.Value.wrapping && !Compatibility.Crafting.wrapping;
    public static bool MoveMouse => Crafting.Instance.fixedUI && Crafting.Instance.fixedUI.Value.moveMouse;
}

public sealed class FastScroll {
    [DefaultValue(true)] public bool listScroll = true;
    
    public static FastScroll Value => Crafting.Instance.fixedUI.Value.fastScroll.Value;
}

public sealed class RecipeFiltering {
    [DefaultValue(true)] public bool hideUnavailable = true;
    [Range(0, 6), DefaultValue(4)] public int width = 4;

    public static bool Enabled => Crafting.Instance.recipeFiltering && !Compatibility.Crafting.recipeFiltering;
    public static RecipeFiltering Value => Crafting.Instance.recipeFiltering.Value;
}

public sealed class CraftOnList {
    [DefaultValue(false)] public bool focusRecipe = false;

    public static bool Enabled => Crafting.Instance.craftOnList && !Compatibility.Crafting.craftOnList;
    public static CraftOnList Value => Crafting.Instance.craftOnList.Value;
}
