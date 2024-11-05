using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Creative;
using Terraria.ModLoader.IO;

namespace BetterInventory.Crafting;

// TODO search filter
public sealed class RecipeFilters {

    public EntryFilterer<Item, IRecipeFilter> Filterer { get; } = new();
    public List<IRecipeFilter> Filters => Filterer.AvailableFilters;
    public List<int> RecipeInFilter = [];
    public int AllRecipes;

    public RecipeFilters(){
        List<IItemEntryFilter> filters = [
            new ItemFilters.Weapon(),
            new ItemFilters.Armor(),
            new ItemFilters.Vanity(),
            new ItemFilters.BuildingBlock(),
            new ItemFilters.Furniture(),
            new ItemFilters.Accessories(),
            new ItemFilters.MiscAccessories(),
            new ItemFilters.Consumables(),
            new ItemFilters.Tools(),
            new ItemFilters.Materials()
        ];
        int[] indexes = [0, 2, 8, 4, 7, 1, 9, 3, 6, 10];

        List<IRecipeFilter> allFilters = [];
        for (int i = 0; i < filters.Count; i++) allFilters.Add(new CreativeFilterWrapper(filters[i], indexes[i]));
        allFilters.Add(new CreativeFilterWrapper(new ItemFilters.MiscFallback(filters), 5));
        Filterer.AddFilters(allFilters);
    }

}


public sealed class RecipeFiltersSerializer : TagSerializer<RecipeFilters, int> {

    public override int Serialize(RecipeFilters value) {
        int raw = 0;
        for (int i = 0; i < value.Filterer.AvailableFilters.Count; i++) if (value.Filterer.IsFilterActive(i)) raw |= 1 << i;
        return raw;
    }

    public override RecipeFilters Deserialize(int tag) {
        RecipeFilters value = new();
        for (int i = 0; i < value.Filterer.AvailableFilters.Count; i++) if ((tag & (1 << i)) != 0) value.Filterer.ToggleFilter(i);
        return value;
    }
}
