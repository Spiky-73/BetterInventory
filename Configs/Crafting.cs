using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace BetterInventory.Configs;

public sealed class Crafting : ModConfig {
    public Toggle<FixedUI> fixedUI = new(true);
    public Toggle<RecipeFiltering> recipeFiltering = new(true);
    public Toggle<CraftOnList> craftOnList = new(true);
    [DefaultValue(true)] public bool mouseMaterial;

    public override ConfigScope Mode => ConfigScope.ClientSide;
    public static Crafting Instance = null!;
}

public sealed class FixedUI {
    public Toggle<RecipeScroll> fastScroll = new(true);
    [DefaultValue(true)] public bool listScroll = true;
    [DefaultValue(true)] public bool wrapping = true;
    [DefaultValue(true)] public bool moveMouse = true;
}

public sealed class RecipeScroll {
    [DefaultValue(true)] public bool listScroll = true;
}

public sealed class RecipeFiltering {
    [DefaultValue(true)] public bool hideUnavailable = true;
    [Range(0, 6), DefaultValue(4)] public int width = 4;
}

public sealed class CraftOnList {
    [DefaultValue(false)] public bool focusRecipe = false;
}
