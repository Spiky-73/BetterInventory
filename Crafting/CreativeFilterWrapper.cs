using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Creative;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace BetterInventory.Crafting;

public sealed class ItemFilterWrapper : IRecipeFilter {
    public IItemEntryFilter Filter { get; }
    public int Index { get; }

    public ItemFilterWrapper(IItemEntryFilter filter, int index){
        Filter = filter;
        Index = index;
    }
    public bool FitsFilter(RecipeListEntry entry) => Filter.FitsFilter(entry.CreateItem);
    public string GetDisplayNameKey() => Filter.GetDisplayNameKey();
    public UIElement GetImage() => new UIImageFramed(RecipeFiltering.recipeFilters, GetSourceFrame());
    public UIElement GetImageGray() => new UIImageFramed(RecipeFiltering.recipeFiltersGray, GetSourceFrame());
    public Rectangle GetSourceFrame() => RecipeFiltering.recipeFilters.Frame(horizontalFrames: 11, frameX: Index, sizeOffsetX: -2);
}

public sealed class ItemSearchFilterWrapper : IRecipeFilter, ISearchFilter<RecipeListEntry> {
    public ItemFilters.BySearch Filter { get; } = new ItemFilters.BySearch();

    public void SetSearch(string searchText) => Filter.SetSearch(searchText);
    public bool FitsFilter(RecipeListEntry entry) => Filter.FitsFilter(entry.CreateItem);
    public string GetDisplayNameKey() => Filter.GetDisplayNameKey();

    public UIElement GetImage() => new UIImageFramed(RecipeFiltering.recipeFilters, GetSourceFrame());
    public UIElement GetImageGray() => new UIImageFramed(RecipeFiltering.recipeFiltersGray, GetSourceFrame());
    public static Rectangle GetSourceFrame() => RecipeFiltering.recipeFilters.Frame(horizontalFrames: 11, frameX: 0, sizeOffsetX: -2);

}

public sealed class RecipeMiscFallback : IRecipeFilter {
    public RecipeMiscFallback(List<IRecipeFilter> otherFilters) {
        _fitsFilterByRecipeIndex = new bool[Recipe.numRecipes];
        for (int i = 1; i < _fitsFilterByRecipeIndex.Length; i++) {
            RecipeListEntry entry = new(Main.recipe[i]);
            _fitsFilterByRecipeIndex[i] = !otherFilters.Exists(f => f.FitsFilter(entry));
        }
    }

    public bool FitsFilter(RecipeListEntry entry) => _fitsFilterByRecipeIndex.IndexInRange(entry.Index) && _fitsFilterByRecipeIndex[entry.Index];

    public string GetDisplayNameKey() => "CreativePowers.TabMisc";

    private readonly bool[] _fitsFilterByRecipeIndex;

    public UIElement GetImage() => new UIImageFramed(RecipeFiltering.recipeFilters, GetSourceFrame());
    public UIElement GetImageGray() => new UIImageFramed(RecipeFiltering.recipeFiltersGray, GetSourceFrame());
    public static Rectangle GetSourceFrame() => RecipeFiltering.recipeFilters.Frame(horizontalFrames: 11, frameX: 5, sizeOffsetX: -2);
}