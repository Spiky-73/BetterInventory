using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
    public UIElement GetImage() => Filter.GetImage();
    public Texture2D GetSource(bool available) => available ? RecipeFiltering.recipeFilters.Value : RecipeFiltering.recipeFiltersGray.Value;
    public Rectangle GetSourceFrame() => RecipeFiltering.recipeFilters.Frame(horizontalFrames: 11, frameX: Index, sizeOffsetX: -2);
}