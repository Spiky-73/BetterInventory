using System.Collections.Generic;
using TRecipe = Terraria.Recipe;

namespace BetterInventory.Reflection;

public static class Recipe {
    public static readonly Field<TRecipe, Dictionary<int, int>> _ownedItems = new(nameof(_ownedItems));
    public static readonly Property<TRecipe, bool> Disabled = new(nameof(TRecipe.Disabled));

    public static readonly StaticMethod<object?> CollectGuideRecipes = new(typeof(TRecipe), nameof(CollectGuideRecipes));
    public static readonly StaticMethod<int, object?> TryRefocusingRecipe = new(typeof(TRecipe), nameof(TryRefocusingRecipe));
    public static readonly StaticMethod<float, object?> VisuallyRepositionRecipes = new(typeof(TRecipe), nameof(VisuallyRepositionRecipes));
    public static readonly StaticMethod<int, object?> AddToAvailableRecipes = new(typeof(TRecipe), nameof(AddToAvailableRecipes));
}