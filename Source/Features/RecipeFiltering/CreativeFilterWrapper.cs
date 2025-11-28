using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace BetterInventory.Features.RecipeFiltering;

public sealed class ItemFilterWrapper : IRecipeFilter {
    public IItemEntryFilter Filter { get; }
    public int Index { get; }

    public ItemFilterWrapper(IItemEntryFilter filter, int index){
        Filter = filter;
        Index = index;
    }

    public bool FitsFilter(Recipe entry) => Filter.FitsFilter(entry.createItem);
    public string GetDisplayNameKey() => Filter.GetDisplayNameKey();
    public UIElement GetImage() => new UIImageFramed(RecipeFiltersPlayer.RecipeFilters, GetSourceFrame());
    public UIElement GetImageGray() => new UIImageFramed(RecipeFiltersPlayer.RecipeFiltersGray, GetSourceFrame());
    public Rectangle GetSourceFrame() => RecipeFiltersPlayer.RecipeFilters.Frame(horizontalFrames: 11, frameX: Index, sizeOffsetX: -2);
}

public sealed class RecipeMiscFallback : IRecipeFilter {
    public RecipeMiscFallback(List<IRecipeFilter> otherFilters) {
        _fitsFilterByRecipeIndex = new bool[Recipe.numRecipes];
        for (int i = 1; i < _fitsFilterByRecipeIndex.Length; i++) {
            Recipe entry = Main.recipe[i];
            _fitsFilterByRecipeIndex[i] = !otherFilters.Exists(f => f.FitsFilter(entry));
        }
    }

    public bool FitsFilter(Recipe entry) => _fitsFilterByRecipeIndex.IndexInRange(entry.RecipeIndex) && _fitsFilterByRecipeIndex[entry.RecipeIndex];

    public string GetDisplayNameKey() => "CreativePowers.TabMisc";

    private readonly bool[] _fitsFilterByRecipeIndex;

    public UIElement GetImage() => new UIImageFramed(RecipeFiltersPlayer.RecipeFilters, GetSourceFrame());
    public UIElement GetImageGray() => new UIImageFramed(RecipeFiltersPlayer.RecipeFiltersGray, GetSourceFrame());
    public static Rectangle GetSourceFrame() => RecipeFiltersPlayer.RecipeFilters.Frame(horizontalFrames: 11, frameX: 5, sizeOffsetX: -2);
}