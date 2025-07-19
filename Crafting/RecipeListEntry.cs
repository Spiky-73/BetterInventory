using Terraria;
using Terraria.DataStructures;
using Terraria.UI;

namespace BetterInventory.Crafting;

public class RecipeListEntry {
    public RecipeListEntry() {}
    public RecipeListEntry(Recipe recipe) => Recipe = recipe;

    public Recipe Recipe { get; } = null!;
    public Item createItem => Recipe.createItem;
    public int RecipeIndex => Recipe.RecipeIndex;

    public static implicit operator Recipe(RecipeListEntry entry) => entry.Recipe;
}

public interface IRecipeFilter : IEntryFilter<RecipeListEntry> {
    UIElement GetImageGray();
}

public interface IRecipeSortStep : IEntrySortStep<RecipeListEntry> {
    public bool HiddenFromSortOptions { get; }
    UIElement GetImage();
}
