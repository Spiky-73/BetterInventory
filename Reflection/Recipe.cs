using System.Collections.Generic;
using TRecipe = Terraria.Recipe;

namespace BetterInventory.Reflection;

public static class Recipe {
    public static readonly Field<TRecipe, Dictionary<int, int>> _ownedItems = new(nameof(_ownedItems));
    public static readonly Property<TRecipe, bool> Disabled = new(nameof(TRecipe.Disabled));
    public static readonly Method<TRecipe, object?> CollectGuideRecipes = new(nameof(CollectGuideRecipes));
    public static readonly Method<TRecipe, int, object?> TryRefocusingRecipe = new(nameof(TryRefocusingRecipe));
    public static readonly Method<TRecipe, float, object?> VisuallyRepositionRecipes = new(nameof(VisuallyRepositionRecipes));
    public static readonly Method<TRecipe, int, object?> AddToAvailableRecipes = new(nameof(AddToAvailableRecipes));
}
