using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using SpikysLib.Collections;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Creative;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace BetterInventory.BetterRecipeList.RecipeFilters;

public sealed class ItemFilterWrapper : IRecipeFilter {
    public IItemEntryFilter Filter { get; }
    public int Index { get; }

    public ItemFilterWrapper(IItemEntryFilter filter, int index) {
        Filter = filter;
        Index = index;
    }

    public bool FitsFilter(Recipe entry) => Filter.FitsFilter(entry.createItem);
    public string GetDisplayNameKey() => Filter.GetDisplayNameKey();
    public UIElement GetImage() => new UIImageFramed(RecipeFilteringUISystem.RecipeFilters, GetSourceFrame());
    public UIElement GetImageGray() => new UIImageFramed(RecipeFilteringUISystem.RecipeFiltersGray, GetSourceFrame());
    public Rectangle GetSourceFrame() => RecipeFilteringUISystem.RecipeFilters.Frame(horizontalFrames: 11, frameX: Index, sizeOffsetX: -2);
}

public sealed class MiscFallback : IRecipeFilter {
    public MiscFallback(IEnumerable<IRecipeFilter> otherFilters) {
        _fitsFilterByRecipeIndex = new bool[Recipe.numRecipes];
        for (int i = 1; i < _fitsFilterByRecipeIndex.Length; i++) {
            Recipe entry = Main.recipe[i];
            _fitsFilterByRecipeIndex[i] = !otherFilters.Exist(f => f.FitsFilter(entry));
        }
    }

    public bool FitsFilter(Recipe entry) => _fitsFilterByRecipeIndex.IndexInRange(entry.RecipeIndex) && _fitsFilterByRecipeIndex[entry.RecipeIndex];

    public string GetDisplayNameKey() => "CreativePowers.TabMisc";

    private readonly bool[] _fitsFilterByRecipeIndex;

    public UIElement GetImage() => new UIImageFramed(RecipeFilteringUISystem.RecipeFilters, GetSourceFrame());
    public UIElement GetImageGray() => new UIImageFramed(RecipeFilteringUISystem.RecipeFiltersGray, GetSourceFrame());
    public static Rectangle GetSourceFrame() => RecipeFilteringUISystem.RecipeFilters.Frame(horizontalFrames: 11, frameX: 5, sizeOffsetX: -2);
}

public sealed class BySearch : ISearchFilter<Recipe> {
    public void SetSearch(string? search) => _filter.SetSearch(_search = search);
    public bool FitsFilter(Recipe recipe) {
        if (!RecipeFiltersConfig.Instance.simpleSearch) return _filter.FitsFilter(recipe.createItem);
        return string.IsNullOrEmpty(_search) || recipe.createItem.HoverName.ToLower().Contains(_search, StringComparison.OrdinalIgnoreCase);
    }

    public string GetDisplayNameKey() => _filter.GetDisplayNameKey();
    public UIElement GetImage() => _filter.GetImage();

    private string? _search;
    private readonly ItemFilters.BySearch _filter = new();
}

