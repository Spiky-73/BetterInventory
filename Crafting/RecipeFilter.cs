using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;

namespace BetterInventory.Crafting;

public class RecipeListEntry {
    public RecipeListEntry() {}
    public RecipeListEntry(Recipe recipe) => Recipe = recipe;

    public Recipe Recipe { get; } = null!;
    public Item CreateItem => Recipe.createItem;
    public int Index => Recipe.RecipeIndex;

    public static implicit operator Recipe(RecipeListEntry entry) => entry.Recipe;
}

public interface IRecipeFilter : IEntryFilter<RecipeListEntry> {
    Asset<Texture2D> GetSource();
    Asset<Texture2D> GetSourceGray();
    Rectangle GetSourceFrame();
}

public interface IRecipeSortStep : IEntrySortStep<RecipeListEntry> {
    public bool HiddenFromSortOptions { get; }
}
