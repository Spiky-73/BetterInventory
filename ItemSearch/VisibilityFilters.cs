using System.Collections;
using System.Collections.Generic;
using SpikysLib.DataStructures;
using Terraria;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.IO;

namespace BetterInventory.ItemSearch;

public sealed class VisibilityFilters {
    
    public static Flags CurrentVisibility => GuideAvailableRecipes.s_guideRecipes ? Flags.ShowAllGuide : Flags.ShowAllAir;
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
        foreach(RangeSet set in OwnedItems.Values) if(set.Contains(type)) return true;
        return false;
    }
    public bool AddOwnedItem(Item item) => AddOwnedItem(item.ModItem?.Mod.Name ?? "Terraria", item.type);
    public bool AddOwnedItem(string mod, int type) {
        if(!OwnedItems.ContainsKey(mod)) OwnedItems.Add(mod, new());
        return OwnedItems[mod].Add(type);
    }

    public bool IsFavorited(int recipe) => FavoritedRecipes.Contains(recipe);
    public bool IsBlacklisted(int recipe) => BlacklistedRecipes.Contains(recipe);
    public void ToggleFavorited(int recipe, bool force = false) {
        if (!ResetRecipeState(recipe) || force) FavoritedRecipes.Add(recipe);
    }
    public void ToggleBlacklisted(int recipe, bool force = false) {
        if (!ResetRecipeState(recipe) || force) BlacklistedRecipes.Add(recipe);
    }
    public bool ResetRecipeState(int recipe) => FavoritedRecipes.Remove(recipe) | BlacklistedRecipes.Remove(recipe);


    public Flags Visibility { get; set; } = Flags.Default;

    public readonly RangeSet FavoritedRecipes = new();
    public readonly RangeSet BlacklistedRecipes = new();

    public readonly Dictionary<string, RangeSet> OwnedItems = new();

    public readonly List<(RawRecipe, bool)> UnloadedRecipes = new();
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

public sealed class VisibilityFiltersSerializer : TagSerializer<VisibilityFilters, TagCompound> {

    public override TagCompound Serialize(VisibilityFilters value) {
        TagCompound tag = new();

        if (value.Visibility != VisibilityFilters.Flags.Default) tag[FiltersTag] = (byte)value.Visibility;

        List<RawRecipe> favorited = new();
        List<RawRecipe> blacklisted = new();
        foreach (int i in value.FavoritedRecipes) favorited.Add(new(Main.recipe[i]));
        foreach (int i in value.BlacklistedRecipes) blacklisted.Add(new(Main.recipe[i]));
        foreach ((RawRecipe r, bool f) in value.UnloadedRecipes) (f ? favorited : blacklisted).Add(r);
        if (favorited.Count != 0) tag[FavoritedTag] = favorited;
        if (blacklisted.Count != 0) tag[BlacklistedTag] = blacklisted;

        List<ItemDefinition> owned = new();
        foreach((string mod, RangeSet set) in value.OwnedItems) {
            foreach (Range range in set.Ranges) {
                owned.Add(new(range.Start));
                owned.Add(new(range.End-1));
            }
        }
        owned.AddRange(value.UnloadedItems);
        if(owned.Count != 0) tag[OwnedTag] = owned;
        
        return tag;
    }

    public override VisibilityFilters Deserialize(TagCompound tag) {
        VisibilityFilters value = new();

        if (tag.TryGet(FiltersTag, out byte raw)) value.Visibility = (VisibilityFilters.Flags)raw;

        if (tag.TryGet(FavoritedTag, out IList<RawRecipe> favorited)) {
            for (int r = 0; r < favorited.Count; r++) {
                Recipe? recipe = favorited[r].GetRecipe();
                if (recipe is null) value.UnloadedRecipes.Add((favorited[r], true));
                else value.FavoritedRecipes.Add(recipe.RecipeIndex);
            }
        }
        if (tag.TryGet(BlacklistedTag, out IList<RawRecipe> blacklisted)) {
            for (int r = 0; r < blacklisted.Count; r++) {
                Recipe? recipe = blacklisted[r].GetRecipe();
                if (recipe is null) value.UnloadedRecipes.Add((blacklisted[r], false));
                else value.BlacklistedRecipes.Add(recipe.RecipeIndex);
            }
        }

        if (tag.TryGet(RecipesTag, out IList<RawRecipe> recipes)) {
            IList<byte> favorites = tag.Get<IList<byte>>(FavoritesTag);
            for (int r = 0; r < recipes.Count; r++) {
                bool fav = favorites[r] == (byte)FavoriteState.Favorited;
                Recipe? recipe = recipes[r].GetRecipe();
                if (recipe is null) value.UnloadedRecipes.Add((recipes[r], fav));
                else if (fav) value.FavoritedRecipes.Add(recipe.RecipeIndex);
                else value.BlacklistedRecipes.Add(recipe.RecipeIndex);
            }
        }

        if (tag.TryGet(OwnedTag, out IList<ItemDefinition> owned) || tag.TryGet(ItemsTag, out owned)) {
            for (int i = 0; i < owned.Count; i+=2) {
                if (owned[i].IsUnloaded) {
                    value.UnloadedItems.Add(owned[i]);
                    value.UnloadedItems.Add(owned[i+1]);
                } else {
                    if (!value.OwnedItems.ContainsKey(owned[i].Mod)) value.OwnedItems.Add(owned[i].Mod, new());
                    value.OwnedItems[owned[i].Mod].Add(new Range(owned[i].Type, owned[i+1].Type+1));
                }
            }
        }
        return value;
    }

    public const string FiltersTag = "filters";
    public const string FavoritedTag = "favorited";
    public const string BlacklistedTag = "blacklisted";
    public const string OwnedTag = "owned";

    public const string RecipesTag = "recipes";
    public const string FavoritesTag = "favorites";
    public const string ItemsTag = "items";
}


public sealed class RawRecipe {
    public RawRecipe(string mod, List<ItemDefinition> items, List<TileDefinition> tiles) {
        this.mod = mod;
        this.items = items;
        this.tiles = tiles;
    }

    public RawRecipe(Recipe recipe) {
        mod = recipe.Mod?.Name ?? "Terraria";
        items = new();
        tiles = new();
        AddItem(recipe.createItem);
        foreach (Item item in recipe.requiredItem) AddItem(item);
        foreach (int tile in recipe.requiredTile) AddTile(tile);
    }

    public Recipe? GetRecipe() {
        int type = items[0].Type;
        HashSet<int> requiredItem = new();
        for (int i = 1; i < items.Count; i++) requiredItem.Add(items[i].Type);

        HashSet<int> requiredTile = new();
        foreach (TileDefinition tile in tiles) requiredTile.Add(tile.Type);

        for (int r = 0; r < Recipe.numRecipes; r++) {
            Recipe recipe = Main.recipe[r];
            if ((recipe.Mod?.Name ?? "Terraria") != mod) continue;
            if (recipe.createItem.type != type) continue;
            foreach (Item material in recipe.requiredItem) {
                if (!requiredItem.Contains(material.type)) goto next;
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
    public List<ItemDefinition> items;
    public List<TileDefinition> tiles;

    private void AddItem(Item item) => items.Add(new(item.type));
    private void AddTile(int tile) => tiles.Add(new(tile));

}
public sealed class RawRecipeSerializer : TagSerializer<RawRecipe, TagCompound> {
    public override TagCompound Serialize(RawRecipe value) => new() { [ModTag] = value.mod, [ItemsTag] = value.items, [TilesTag] = value.tiles };

    public override RawRecipe Deserialize(TagCompound tag){
        List<ItemDefinition> items;
        if (tag.Get<IList>(ItemsTag) is not List<string> i) items = tag.Get<List<ItemDefinition>>(ItemsTag);
        else {
            items = new();
            foreach (string s in i) items.Add(new(s));
        }
        List<TileDefinition> tiles;
        if (tag.Get<IList>(TilesTag) is not List<string> t) tiles = tag.Get<List<TileDefinition>>(TilesTag);
        else {
            tiles = new();
            foreach (string s in t) tiles.Add(new(s));
        }
        return new(tag.GetString(ModTag), items, tiles);
    }


    public const string ModTag = "mod";
    public const string ItemsTag = "items";
    public const string TilesTag = "tiles";
}