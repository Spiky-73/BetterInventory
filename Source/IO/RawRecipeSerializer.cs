using System.Collections;
using System.Collections.Generic;
using BetterInventory.DataStructures;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.IO;

namespace BetterInventory.IO;

public sealed class RawRecipeSerializer : TagSerializer<RawRecipe, TagCompound> {
    public override TagCompound Serialize(RawRecipe value) => new() { [ModTag] = value.mod, [ItemsTag] = value.items, [TilesTag] = value.tiles };

    public override RawRecipe Deserialize(TagCompound tag) {
        List<ItemDefinition> items;
        if (tag.Get<IList>(ItemsTag) is not List<string> i) items = tag.Get<List<ItemDefinition>>(ItemsTag);
        else {
            items = [];
            foreach (string s in i) items.Add(new(s));
        }
        List<TileDefinition> tiles;
        if (tag.Get<IList>(TilesTag) is not List<string> t) tiles = tag.Get<List<TileDefinition>>(TilesTag);
        else {
            tiles = [];
            foreach (string s in t) tiles.Add(new(s));
        }
        return new(tag.GetString(ModTag), items, tiles);
    }


    public const string ModTag = "mod";
    public const string ItemsTag = "items";
    public const string TilesTag = "tiles";
}