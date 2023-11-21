using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace BetterInventory.Configs;

public sealed class Crafting : ModConfig {
    [DefaultValue(true)] public bool tweeks;
    [DefaultValue(true)] public bool recipeFiltering;
    public Toggle<CraftOverrides> craftOverrides = new(true);
    [DefaultValue(true)] public bool craftingOnRecList;

    public Toggle<RecipeScroll> recipeScroll = new(true);
    [DefaultValue(false)] public bool focusRecipe = false;

    public override ConfigScope Mode => ConfigScope.ClientSide;
    public static Crafting Instance = null!;
}

public sealed class CraftOverrides {
    [DefaultValue(false)] public bool invertClicks = false;
}
public sealed class RecipeScroll {
    [DefaultValue(true)] public bool listScroll = true;
}