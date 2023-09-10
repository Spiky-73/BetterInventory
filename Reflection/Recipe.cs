using System.Collections.Generic;
using TRecipe = Terraria.Recipe;

namespace BetterInventory.Reflection;

public static class Recipe {
    public static readonly Property<TRecipe, bool> Disabled = new(nameof(TRecipe.Disabled));
    public static readonly Field<TRecipe, bool> needWater = new(nameof(needWater));
    public static readonly Field<TRecipe, bool> needHoney = new(nameof(needHoney));
    public static readonly Field<TRecipe, bool> needLava = new(nameof(needLava));
    public static readonly Field<TRecipe, bool> needSnowBiome = new(nameof(needSnowBiome));
    public static readonly Field<TRecipe, bool> needGraveyardBiome = new(nameof(needGraveyardBiome));

    public static readonly StaticField<Dictionary<int, int>> _ownedItems = new(typeof(TRecipe), nameof(_ownedItems));
    public static readonly StaticMethod<object?> CollectGuideRecipes = new(typeof(TRecipe), nameof(CollectGuideRecipes));
    public static readonly StaticMethod<int, object?> TryRefocusingRecipe = new(typeof(TRecipe), nameof(TryRefocusingRecipe));
    public static readonly StaticMethod<float, object?> VisuallyRepositionRecipes = new(typeof(TRecipe), nameof(VisuallyRepositionRecipes));
    public static readonly StaticMethod<int, object?> AddToAvailableRecipes = new(typeof(TRecipe), nameof(AddToAvailableRecipes));
    public static readonly StaticMethod<object?> ClearAvailableRecipes = new(typeof(TRecipe), nameof(TRecipe.ClearAvailableRecipes));
    public static readonly StaticMethod<Terraria.Player, object?> CollectItemsToCraftWithFrom = new(typeof(TRecipe), nameof(CollectItemsToCraftWithFrom));
}