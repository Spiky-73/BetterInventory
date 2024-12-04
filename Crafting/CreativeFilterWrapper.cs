using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace BetterInventory.Crafting;

public sealed class ItemFilterWrapper : IRecipeFilter {
    public IItemEntryFilter Filter { get; }
    public int Index { get; }

    public ItemFilterWrapper(IItemEntryFilter filter, int index){
        Filter = filter;
        Index = index;
    }
    public bool FitsFilter(Item entry) => Filter.FitsFilter(entry);
    public string GetDisplayNameKey() => Filter.GetDisplayNameKey();
    public UIElement GetImage() => throw new NotSupportedException();
    public Asset<Texture2D> GetSource() => RecipeFiltering.recipeFilters;
    public Asset<Texture2D> GetSourceGray() => RecipeFiltering.recipeFiltersGray;
    public Rectangle GetSourceFrame() => RecipeFiltering.recipeFilters.Frame(horizontalFrames: 11, frameX: Index, sizeOffsetX: -2);
}

public sealed class ItemSearchFilterWrapper : IRecipeFilter, ISearchFilter<Item> {
    public ItemFilters.BySearch Filter { get; } = new ItemFilters.BySearch();

    public void SetSearch(string searchText) => Filter.SetSearch(searchText);
    public bool FitsFilter(Item entry) => Filter.FitsFilter(entry);
    public string GetDisplayNameKey() => Filter.GetDisplayNameKey();

    public UIElement GetImage() => throw new NotSupportedException();
    public Asset<Texture2D> GetSource() => RecipeFiltering.recipeFilters;
    public Asset<Texture2D> GetSourceGray() => RecipeFiltering.recipeFiltersGray;
    public Rectangle GetSourceFrame() => RecipeFiltering.recipeFilters.Frame(horizontalFrames: 11, frameX: 0, sizeOffsetX: -2);

}

public sealed class ItemMiscFallbackWrapper : IRecipeFilter {
    public ItemMiscFallbackWrapper(List<IRecipeFilter> otherFilters) {
        int count = ItemLoader.ItemCount;
        _fitsFilterByItemType = new bool[ItemLoader.ItemCount];
        for (int i = 1; i < _fitsFilterByItemType.Length; i++) {
            Item entry = ContentSamples.ItemsByType[i];
            _fitsFilterByItemType[i] = !otherFilters.Exists(f => f.FitsFilter(entry));
        }
    }

    public bool FitsFilter(Item entry) => _fitsFilterByItemType.IndexInRange(entry.type) && _fitsFilterByItemType[entry.type];

    public string GetDisplayNameKey() => "CreativePowers.TabMisc";

    private readonly bool[] _fitsFilterByItemType;

    public UIElement GetImage() => throw new NotSupportedException();
    public Asset<Texture2D> GetSource() => RecipeFiltering.recipeFilters;
    public Asset<Texture2D> GetSourceGray() => RecipeFiltering.recipeFiltersGray;
    public Rectangle GetSourceFrame() => RecipeFiltering.recipeFilters.Frame(horizontalFrames: 11, frameX: 5, sizeOffsetX: -2);
}