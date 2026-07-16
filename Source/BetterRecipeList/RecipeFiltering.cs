using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Creative;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI;

namespace BetterInventory.BetterRecipeList;

public sealed class RecipeFilteringPlayer : ModPlayer {

    public static RecipeFilteringPlayer LocalPlayer => Main.LocalPlayer.GetModPlayer<RecipeFilteringPlayer>();

    public override bool IsLoadingEnabled(Mod mod) => Compatibility.LoadDisabledFeatures || BetterRecipeListConfig.RecipeFilters;
    public override void Load() {
        _allFilters = [
            new RecipeFilters.ItemFilterWrapper(new ItemFilters.Weapon(), 0),
            new RecipeFilters.ItemFilterWrapper(new ItemFilters.Armor(), 2),
            new RecipeFilters.ItemFilterWrapper(new ItemFilters.Vanity(), 8),
            new RecipeFilters.ItemFilterWrapper(new ItemFilters.BuildingBlock(), 4),
            new RecipeFilters.ItemFilterWrapper(new ItemFilters.Furniture(), 7),
            new RecipeFilters.ItemFilterWrapper(new ItemFilters.Accessories(), 1),
            new RecipeFilters.ItemFilterWrapper(new ItemFilters.MiscAccessories(), 9),
            new RecipeFilters.ItemFilterWrapper(new ItemFilters.Consumables(), 3),
            new RecipeFilters.ItemFilterWrapper(new ItemFilters.Tools(), 6),
            new RecipeFilters.ItemFilterWrapper(new ItemFilters.Materials(), 10)
        ];

        _allSortSteps = [
            new RecipeSortSteps.ByRecipeId(),
            new RecipeSortSteps.ByCreateItemName(),
            new RecipeSortSteps.ByCreateItemCreativeId(),
            new RecipeSortSteps.ByCreateItemValue()
        ];
    }
    public static void SetupMiscFallbackFilter() {
        _allFilters.Add(new RecipeFilters.MiscFallback(_allFilters));
    }

    public override ModPlayer NewInstance(Player entity) {
        var player = (RecipeFilteringPlayer)base.NewInstance(entity);
        player.Filterer.AddAvailableFilters(_allFilters);
        player.Filterer.SetSearchFilter(new RecipeFilters.BySearch());
        player.Sorter.AddSortSteps(_allSortSteps);
        return player;
    }

    public override void SaveData(TagCompound tag) {
        // if (!FeaturesConfig.RecipeFiltering) return; // Always save/load data to preserve it if the feature is re-enabled in the future
        int filters = 0;
        for (int i = 0; i < Filterer.AvailableFilters().Count; i++) if (Filterer.IsFilterActive(i)) filters |= 1 << i;
        if (Filterer.IsActive()) tag[FiltersTag] = filters;

        string? search = Filterer.Search();
        if (Filterer.IsActive()) tag[SearchTag] = search;

        int sort = Sorter.GetActiveSortStepIndex();
        if (Sorter.IsActive()) tag[SortTag] = sort;
    }
    public override void LoadData(TagCompound tag) {
        // if (!FeaturesConfig.RecipeFiltering) return; // Always save/load data to preserve it if the feature is re-enabled in the future
        Filterer.ClearActiveFilters();
        if (tag.TryGet(FiltersTag, out int filters)) {
            for (int i = 0; i < Filterer.AvailableFilters().Count; i++) if ((filters & (1 << i)) != 0) Filterer.ToggleFilter(i);
        }

        Filterer.ClearSearch();
        if (tag.TryGet(SearchTag, out string search)) Filterer.SetSearch(search);

        Sorter.ResetSortStep();
        if (tag.TryGet(SortTag, out int sort)) Sorter.SetActiveSortStep(sort);

    }
    public const string FiltersTag = "filters";
    public const string SearchTag = "search";
    public const string SortTag = "sort";

    public EntryFilterer<Recipe, IRecipeFilter, ISearchFilter<Recipe>> Filterer { get; private set; } = new();
    public EntrySorter<Recipe, IRecipeSortStep> Sorter { get; private set; } = new();

    // TODO called in DisplayedRecipes
    public static void FilterAndSortRecipes() {
        if (!BetterRecipeListConfig.RecipeFilters) return;
        RecipeFilteringUISystem.PreRecipeFiltering();

        var allRecipes = Main.availableRecipe[0..Main.numAvailableRecipes].Select(i => Main.recipe[i]);
        var recipes = allRecipes;
        var player = LocalPlayer;
        if (player.Filterer.IsActive()) recipes = recipes.Where(player.Filterer.FitsFilters);
        if (player.Sorter.IsActive()) recipes = recipes.Order(player.Sorter.Comparer);
        if (recipes == allRecipes) return;

        int count = 0;
        foreach (var recipe in recipes) Main.availableRecipe[count++] = recipe.RecipeIndex;
        for (int i = count; i < Main.numAvailableRecipes; i++) Main.availableRecipe[i] = 0;
        Main.numAvailableRecipes = count;
    }

    public override void OnEnterWorld() {
        RecipeFilteringUI.Activate();
    }

    private static List<IRecipeFilter> _allFilters = [];
    private static List<IRecipeSortStep> _allSortSteps = [];
}

public interface IRecipeFilter : IEntryFilter<Recipe> {
    UIElement GetImageGray();
}

public interface IRecipeSortStep : IEntrySortStep<Recipe> {
    UIElement GetImage();
}