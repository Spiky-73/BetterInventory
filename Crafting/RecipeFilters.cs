using Terraria.ModLoader.IO;

namespace BetterInventory.Crafting;

public sealed class RecipeFilters {
    public int filters;
}


public sealed class RecipeFiltersSerializer : TagSerializer<RecipeFilters, int> {

    public override int Serialize(RecipeFilters value) {
        return value.filters;
    }

    public override RecipeFilters Deserialize(int tag) {
        RecipeFilters value = new() { filters = tag };
        return value;
    }
}
