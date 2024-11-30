using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader.Config;

namespace BetterInventory.DataStructures;

public sealed class RawRecipe {
    public RawRecipe(string mod, List<ItemDefinition> items, List<TileDefinition> tiles) {
        this.mod = mod;
        this.items = items;
        this.tiles = tiles;
    }

    public RawRecipe(Recipe recipe) {
        mod = recipe.Mod?.Name ?? "Terraria";
        items = [];
        tiles = [];
        AddItem(recipe.createItem);
        foreach (Item item in recipe.requiredItem) AddItem(item);
        foreach (int tile in recipe.requiredTile) AddTile(tile);
    }

    public Recipe? GetRecipe() {
        int type = items[0].Type;
        HashSet<int> requiredItem = [];
        for (int i = 1; i < items.Count; i++) requiredItem.Add(items[i].Type);

        HashSet<int> requiredTile = [];
        foreach (TileDefinition tile in tiles) requiredTile.Add(tile.Type);

        for (int r = 0; r < Recipe.numRecipes; r++) {
            Recipe recipe = Main.recipe[r];
            if ((recipe.Mod?.Name ?? "Terraria") != mod) continue;
            if (recipe.createItem.type != type) continue;
            if (recipe.requiredItem.Exists(m => !requiredItem.Contains(m.type))) continue;
            if (recipe.requiredTile.Exists(t => !requiredTile.Contains(t))) continue;
            return recipe;
        }
        return null;
    }

    public string mod;
    public List<ItemDefinition> items;
    public List<TileDefinition> tiles;

    private void AddItem(Item item) => items.Add(new(item.type));
    private void AddTile(int tile) => tiles.Add(new(tile));

}