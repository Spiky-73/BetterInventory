using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace BetterInventory.Configs;

public sealed class Crafting : ModConfig {
    [DefaultValue(true)] public bool tweeks;
    public Toggle<RecipeFiltering> recipeFiltering = new(true);
    [DefaultValue(true)] public bool craftingOnRecList;
    public Toggle<RecipeScroll> recipeScroll = new(true);
    [DefaultValue(false)] public bool focusRecipe = false;

    public override ConfigScope Mode => ConfigScope.ClientSide;
    public static Crafting Instance = null!;
}

public sealed class RecipeScroll {
    [DefaultValue(true)] public bool listScroll = true;
}

public sealed class RecipeFiltering {
    [DefaultValue(true)] public bool hideUnavailable = true;
    [Range(0, 6), DefaultValue(4)] public int width = 4;
}