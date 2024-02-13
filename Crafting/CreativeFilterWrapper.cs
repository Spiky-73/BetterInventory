using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.UI;

namespace BetterInventory.Crafting;

public sealed class CreativeFilterWrapper : IItemEntryFilter {
    public IItemEntryFilter Filter { get; }
    public int Index { get; }

    public CreativeFilterWrapper(IItemEntryFilter filter, int index){
        Filter = filter;
        Index = index;
    }
    public bool FitsFilter(Item entry) => Filter.FitsFilter(entry);
    public string GetDisplayNameKey() => Filter.GetDisplayNameKey();
    public UIElement GetImage() => Filter.GetImage();

    public Rectangle GetSourceFrame() => RecipeFiltering.RecipeFilters.Frame(11, 1, frameX: Index);
}