using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.UI;

namespace BetterInventory.Crafting;

public sealed class CreativeFilterWrapper : IRecipeFilter {
    public IItemEntryFilter Filter { get; }
    public int Index { get; }

    public CreativeFilterWrapper(IItemEntryFilter filter, int index){
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