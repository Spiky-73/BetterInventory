using System.Linq;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace BetterInventory.Features.RecipeFiltering;

public sealed class RecipeFilteringPlayer : ModPlayer {
    public static RecipeFilteringPlayer LocalPlayer => Main.LocalPlayer.GetModPlayer<RecipeFilteringPlayer>();

    public override bool IsLoadingEnabled(Mod mod) => Compatibility.LoadDisabledFeatures || FeaturesConfig.RecipeFiltering;
    public override void Load() {
        RecipeFiltersPlayer.Load(Mod);
        RecipeSearchPlayer.Load();
        RecipeSortPlayer.Load(Mod);
    }

    public override void SaveData(TagCompound tag) {
        // if (!FeaturesConfig.RecipeFiltering) return; // Always save/load data to preserve it if the feature is re-enabled in the future
        FiltersPlayer.SaveData(tag);
        SearchPlayer.SaveData(tag);
        SortPlayer.SaveData(tag);
    }
    public override void LoadData(TagCompound tag) {
        // if (!FeaturesConfig.RecipeFiltering) return; // Always save/load data to preserve it if the feature is re-enabled in the future
        FiltersPlayer.LoadData(tag);
        SearchPlayer.LoadData(tag);
        SortPlayer.LoadData(tag);
    }

    public RecipeFiltersPlayer FiltersPlayer { get; private set; } = new();
    public RecipeSearchPlayer SearchPlayer { get; private set; } = new();
    public RecipeSortPlayer SortPlayer { get; private set; } = new();

    public static void FilterAndSortRecipes() {
        if (!FeaturesConfig.RecipeFiltering) return;

        var allRecipes = Main.availableRecipe[0..Main.numAvailableRecipes].Select(i => Main.recipe[i]);
        var recipes = allRecipes;
        var player = LocalPlayer;
        if (RecipeFilteringConfig.Instance.filters && player.FiltersPlayer.IsActive()) recipes = recipes.Where(player.FiltersPlayer.FitsFilters);
        if (RecipeFilteringConfig.Instance.search && player.SearchPlayer.IsActive()) recipes = recipes.Where(player.SearchPlayer.FitsFilters);
        if (RecipeFilteringConfig.Instance.sort && player.SortPlayer.IsActive()) recipes = recipes.Order(player.SortPlayer.Comparer);
        if (recipes == allRecipes) return;

        RecipeFilteringUISystem.PreRecipeFiltering();
        int count = 0;
        foreach (var recipe in recipes) Main.availableRecipe[count++] = recipe.RecipeIndex;
        for (int i = count; i < Main.numAvailableRecipes; i++) Main.availableRecipe[i] = 0;
        Main.numAvailableRecipes = count;
    }
}

public sealed class RecipeFilteringSystem : ModSystem {
    public override bool IsLoadingEnabled(Mod mod) => Compatibility.LoadDisabledFeatures || FeaturesConfig.RecipeFiltering;
    public override void PostSetupRecipes() => RecipeFiltersPlayer.PostSetupRecipes();
}
