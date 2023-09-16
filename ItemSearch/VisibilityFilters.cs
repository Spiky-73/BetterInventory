using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader.IO;

namespace BetterInventory.ItemSearch;

public sealed class VisibilityFilters {
    
    public Flags Visibility { get; set; } = Flags.Default;

    public static Flags CurrentVisibility => BetterGuide.CraftingStations.ContainsKey(Main.guideItem.createTile) ? Flags.ShowAllTile : Main.guideItem.IsAir ? Flags.ShowAllAir : Flags.ShowAllGuide;
    public bool ShowAllRecipes {
        get => Visibility.HasFlag(CurrentVisibility);
        set => SetFlag(CurrentVisibility, value);
    }
    public bool TileMode {
        get => CurrentVisibility == Flags.ShowAllTile && Visibility.HasFlag(Flags.TileMode);
        set => SetFlag(Flags.TileMode, value);
    }

    public readonly Dictionary<int, FavoriteState> FavoriteRecipes = new();
    public readonly List<(RawRecipe, byte)> MissingRecipes = new();

    
    private void SetFlag(Flags flag, bool set) {
        if (set) Visibility |= flag;
        else Visibility &= ~flag;
    }

    [System.Flags]
    public enum Flags {
        Default = TileMode | ShowAllTile | ShowAllGuide,

        ShowAllAir =   1 << 0,
        ShowAllGuide = 1 << 1,
        ShowAllTile =  1 << 2,
        TileMode =     1 << 3,
    }
}


public enum FavoriteState : byte { Default, Blacklisted, Favorited }

public sealed class VisibilityFiltersSerialiser : TagSerializer<VisibilityFilters, TagCompound> {

    public override TagCompound Serialize(VisibilityFilters value) {
        TagCompound tag = new();

        if (value.Visibility != VisibilityFilters.Flags.Default) tag[FiltersTag] = (byte)value.Visibility;

        List<RawRecipe> recipes = new();
        List<byte> favorites = new();
        foreach ((int index, FavoriteState state) in value.FavoriteRecipes) {
            recipes.Add(new(Main.recipe[index]));
            favorites.Add((byte)state);
        }
        foreach ((RawRecipe recipe, byte state) in value.MissingRecipes) {
            recipes.Add(recipe);
            favorites.Add(state);
        }

        if (recipes.Count != 0) {
            tag[RecipesTag] = recipes;
            tag[FavoritesTag] = favorites;
        }
        return tag;
    }

    public override VisibilityFilters Deserialize(TagCompound tag) {
        VisibilityFilters value = new();

        if (tag.TryGet(FiltersTag, out byte raw)) value.Visibility = (VisibilityFilters.Flags)raw;

        if (tag.TryGet(RecipesTag, out List<RawRecipe> recipes)) {
            List<byte> favorites = tag.Get<List<byte>>(FavoritesTag);
            for (int r = 0; r < recipes.Count; r++) {
                Recipe? recipe = recipes[r].GetRecipe();
                if (recipe is null) value.MissingRecipes.Add((recipes[r], favorites[r]));
                else value.FavoriteRecipes[recipe.RecipeIndex] = (FavoriteState)favorites[r];
            }
        }
        return value;
    }

    public const string FiltersTag = "filters";
    public const string RecipesTag = "recipes";
    public const string FavoritesTag = "favorites";
}


public sealed class RawRecipe {
    public RawRecipe(string mod, List<string> items, List<int> stacks, List<string> tiles) {
        this.mod = mod;
        this.items = items;
        this.stacks = stacks;
        this.tiles = tiles;
    }

    public RawRecipe(Recipe recipe) {
        mod = recipe.Mod?.Name ?? "Terraria";
        items = new();
        stacks = new();
        AddItem(recipe.createItem);
        foreach (Item item in recipe.requiredItem) AddItem(item);
        tiles = new();
        foreach (int tile in recipe.requiredTile) AddTile(tile);
    }

    public Recipe? GetRecipe() {
        (int type, int stack) = (ItemID.Search.GetId(items[0]), stacks[0]);
        Dictionary<int, int> requiredItem = new();
        for (int i = 1; i < items.Count; i++) requiredItem[ItemID.Search.GetId(items[i])] = stacks[i];

        HashSet<int> requiredTile = new();
        foreach (string tile in tiles) requiredTile.Add(TileID.Search.GetId(tile));

        for (int r = 0; r < Recipe.numRecipes; r++) {
            Recipe recipe = Main.recipe[r];
            if ((recipe.Mod?.Name ?? "Terraria") != mod) continue;
            if (recipe.createItem.type != type || recipe.createItem.stack != stack) continue;
            foreach (Item material in recipe.requiredItem) {
                if (!requiredItem.TryGetValue(material.type, out int s) || material.stack != s) goto next;
            }
            foreach (int tile in recipe.requiredTile) {
                if (!requiredTile.Contains(tile)) goto next;
            }
            return recipe;
        next:;
        }
        return null;
    }

    public string mod;
    public List<string> items;
    public List<int> stacks;
    public List<string> tiles;

    private void AddItem(Item item) {
        items.Add(ItemID.Search.GetName(item.type));
        stacks.Add(item.stack);
    }
    private void AddTile(int tile) => tiles.Add(TileID.Search.GetName(tile));

}
public sealed class RawRecipeSerialiser : TagSerializer<RawRecipe, TagCompound> {
    public override TagCompound Serialize(RawRecipe value) => new() {
        [ModTag] = value.mod,
        [ItemsTag] = value.items, [StacksTag] = value.stacks,
        [TilesTag] = value.tiles,
    };

    public override RawRecipe Deserialize(TagCompound tag) => new(
        tag.GetString(ModTag),
        tag.Get<List<string>>(ItemsTag), tag.Get<List<int>>(StacksTag),
        tag.Get<List<string>>(TilesTag)
    );

    public const string ModTag = "mod";
    public const string ItemsTag = "items";
    public const string StacksTag = "stacks";
    public const string TilesTag = "tiles";
}