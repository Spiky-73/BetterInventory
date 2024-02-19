using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.IO;

namespace BetterInventory.ItemSearch;

public sealed class VisibilityFilters {
    
    public Flags Visibility { get; set; } = Flags.Default;

    public static Flags CurrentVisibility => (Main.guideItem.IsAir && (!Guide.Config.guideTile || Guide.guideTile.IsAir)) ? Flags.ShowAllAir : Flags.ShowAllGuide;
    public bool ShowAllRecipes {
        get => Visibility.HasFlag(CurrentVisibility);
        set => SetFlag(CurrentVisibility, value);
    }

    public bool IsKnownRecipe(Recipe recipe) {
        if (HasOwnedItem(recipe.createItem) || recipe.requiredItem.Exists(i => HasOwnedItem(i))) return true;
        foreach (int group in recipe.acceptedGroups) {
            foreach (int type in RecipeGroup.recipeGroups[group].ValidItems) {
                if (HasOwnedItem(type)) return true;
            }
        }
        return false;
    }

    public bool HasOwnedItem(Item item) => OwnedItems.TryGetValue(item.ModItem?.Mod.Name ?? "Terraria", out var items) && items.Contains(item.type);
    public bool HasOwnedItem(int type) {
        foreach(DataStructures.RangeSet set in OwnedItems.Values) if(set.Contains(type)) return true;
        return false;
    }
    public bool AddOwnedItem(Item item) => AddOwnedItem(item.ModItem?.Mod.Name ?? "Terraria", item.type);
    public bool AddOwnedItem(string mod, int type) {
        if(!OwnedItems.ContainsKey(mod)) OwnedItems.Add(mod, new());
        return OwnedItems[mod].Add(type);
    }

    public FavoriteState GetFavoriteState(int recipe) => FavoriteRecipes.GetValueOrDefault(recipe);
    public void SetFavoriteState(int recipe, FavoriteState state) {
        if (state == FavoriteState.Default) FavoriteRecipes.Remove(recipe);
        else FavoriteRecipes[recipe] = state;
    }

    public readonly Dictionary<int, FavoriteState> FavoriteRecipes = new();
    public readonly List<(RawRecipe, byte)> MissingRecipes = new();

    public readonly Dictionary<string, DataStructures.RangeSet> OwnedItems = new();
    public readonly List<ItemDefinition> UnloadedItems = new();
    
    private void SetFlag(Flags flag, bool set) {
        if (set) Visibility |= flag;
        else Visibility &= ~flag;
    }

    [System.Flags]
    public enum Flags {
        Default = ShowAllGuide,
        ShowAllAir =    1 << 0,
        ShowAllGuide =  1 << 1,
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

        List<ItemDefinition> items = new();
        foreach((string mod, DataStructures.RangeSet set) in value.OwnedItems) {
            foreach (DataStructures.Range range in set.GetRanges()) {
                items.Add(new(range.Start));
                items.Add(new(range.End));
            }
        }
        items.AddRange(value.UnloadedItems);
        if(items.Count != 0) tag[ItemsTag] = items;
        return tag;
    }

    public override VisibilityFilters Deserialize(TagCompound tag) {
        VisibilityFilters value = new();

        if (tag.TryGet(FiltersTag, out byte raw)) value.Visibility = (VisibilityFilters.Flags)raw;

        if (tag.TryGet(RecipesTag, out IList<RawRecipe> recipes)) {
            IList<byte> favorites = tag.Get<IList<byte>>(FavoritesTag);
            for (int r = 0; r < recipes.Count; r++) {
                Recipe? recipe = recipes[r].GetRecipe();
                if (recipe is null) value.MissingRecipes.Add((recipes[r], favorites[r]));
                else value.FavoriteRecipes[recipe.RecipeIndex] = (FavoriteState)favorites[r];
            }
        }
        if (tag.TryGet(ItemsTag, out IList<ItemDefinition> items)) {
            for (int i = 0; i < items.Count; i+=2) {
                if (items[i].IsUnloaded) {
                    value.UnloadedItems.Add(items[i]);
                    value.UnloadedItems.Add(items[i+1]);
                } else {
                    if (!value.OwnedItems.ContainsKey(items[i].Mod)) value.OwnedItems.Add(items[i].Mod, new());
                    value.OwnedItems[items[i].Mod].Add(new DataStructures.Range(items[i].Type, items[i+1].Type));
                }
            }
        }
        return value;
    }

    public const string FiltersTag = "filters";
    public const string RecipesTag = "recipes";
    public const string FavoritesTag = "favorites";
    public const string ItemsTag = "items";
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