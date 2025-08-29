using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using ReLogic.Content;
using SpikysLib.IL;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Creative;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI;

namespace BetterInventory.Crafting;

public sealed class RecipeUIPlayer : ModPlayer {
    public static RecipeUIPlayer LocalPlayer => Main.LocalPlayer.GetModPlayer<RecipeUIPlayer>();

    public override void Load() {
        RecipeUI.AddSortStep(new RecipeSortStep.ByRecipeId());
        RecipeUI.AddSortStep(new RecipeSortStep.ByCreateItemName());
        RecipeUI.AddSortStep(new RecipeSortStep.ByCreateItemCreativeId());
        RecipeUI.AddSortStep(new RecipeSortStep.ByCreateItemValue());

        RecipeUI.SearchFilter = new ItemSearchFilterWrapper();

        List<(IItemEntryFilter, int)> filters = [
            (new ItemFilters.Weapon(), 0),
            (new ItemFilters.Armor(), 2),
            (new ItemFilters.Vanity(), 8),
            (new ItemFilters.BuildingBlock(), 4),
            (new ItemFilters.Furniture(), 7),
            (new ItemFilters.Accessories(), 1),
            (new ItemFilters.MiscAccessories(), 9),
            (new ItemFilters.Consumables(), 3),
            (new ItemFilters.Tools(), 6),
            (new ItemFilters.Materials(), 10)
        ];
        List<IRecipeFilter> allFilters = [];
        foreach (var (f, i) in filters) allFilters.Add(new ItemFilterWrapper(f, i));
        RecipeUI.Filterer.AddFilters(allFilters);

    }

    public override void SaveData(TagCompound tag) {
        int sort = RecipeUI.Sorter.GetPrioritizedStepIndex();
        if (sort != 0) tag[SortTag] = sort;
        int filters = 0;
        for (int i = 0; i < RecipeUI.Filterer.AvailableFilters.Count; i++) if (RecipeUI.Filterer.IsFilterActive(i)) filters |= 1 << i;
        if (filters != 0) tag[FiltersTag] = filters;
        var searchFilter = Reflection.EntryFilterer<RecipeListEntry, IRecipeFilter>._searchFilter.GetValue(RecipeUI.Filterer);
        string? search = searchFilter is ItemSearchFilterWrapper wrapper ? Reflection.ItemFilters.BySearch._search.GetValue(wrapper.Filter) : null;
        if (search is not null) tag[SearchTag] = search;
    }
    public override void LoadData(TagCompound tag) {
        if (tag.TryGet(SortTag, out int sort)) this.sort = sort;
        if (tag.TryGet(FiltersTag, out int filters)) this.filters = filters;
        if (tag.TryGet(SearchTag, out string search)) this.search = search;
    }

    public override void OnEnterWorld() {
        RecipeUI.Sorter.SetPrioritizedStepIndex(sort);
        RecipeUI.Filterer.ActiveFilters.Clear();
        for (int i = 0; i < RecipeUI.Filterer.AvailableFilters.Count; i++) if ((filters & (1 << i)) != 0) RecipeUI.Filterer.ToggleFilter(i);
        RecipeUI.SearchFilter.SetSearch(search);
        RecipeUI.RebuildUI();
    }

    public int sort;
    public int filters;
    public string? search;

    public const string SortTag = "sort";
    public const string FiltersTag = "filters";
    public const string SearchTag = "search";
}

public sealed class RecipeUI : ModSystem {

    public override void Load() {
        IL_Main.DrawInventory += static il => {
            if (!il.ApplyTo(ILDrawUI, Configs.Crafting.RecipeUI)) Configs.UnloadedCrafting.Value.RecipeUI = true;
        };

        recipeFilters = Mod.Assets.Request<Texture2D>($"Assets/Recipe_Filters");
        recipeFiltersGray = Mod.Assets.Request<Texture2D>($"Assets/Recipe_Filters_Gray");
        recipeSortToggle = Mod.Assets.Request<Texture2D>($"Assets/Sort_Toggle");
        recipeSortToggleBorder = Mod.Assets.Request<Texture2D>($"Assets/Sort_Toggle_Border");
        recipeSortingSteps = Mod.Assets.Request<Texture2D>($"Assets/RecipeSortingSteps");

        _recipeState = new();
        _recipeState.Activate();
        _recipeInterface = new();
        _recipeInterface.SetState(_recipeState);

        Sorter.SetPrioritizedStepIndex(0);
    }

    public static void RebuildUI() {
        if (!Main.gameMenu && _recipeState is not null) {
            RecipesPerFilter = new int[Filterer.AvailableFilters.Count];
            _recipeState.Rebuild();
        }
    }

    public override void PostSetupRecipes() {
        Filterer.AvailableFilters.Add(new RecipeMiscFallback(Filterer.AvailableFilters));
    }

    private static void ILDrawUI(ILContext il) {
        ILCursor cursor = new(il);

        // BetterGameUI Compatibility
        int screenY = 13;
        if (cursor.TryGotoNext(i => i.SaferMatchCallvirt(Reflection.AccessorySlotLoader.DrawAccSlots))) {
            cursor.GotoNext(i => i.MatchLdsfld(Reflection.Main.screenHeight));
            cursor.GotoNextLoc(out screenY, i => true, 13);
        }

        // ...
        // if(<showRecipes>){
        cursor.GotoRecipeDraw();

        //     ++<drawFilters>
        cursor.EmitLdloc(screenY); // int num54
        cursor.EmitDelegate((int y) => {
            if (Configs.Crafting.RecipeUI && UnfilteredCount != 0) DrawRecipeUI(94, 450 + y);
        });

        //     ...
        //     if(Main.numAvailableRecipes == 0) ...
        //     else {
        //         int num73 = 94;
        //         int num74 = 450 + num51;
        //         if (++false && Main.InGuideCraftMenu) num74 -= 150;
        cursor.GotoNext(i => i.MatchLdsfld(Reflection.TextureAssets.CraftToggle));
        cursor.GotoPrev(MoveType.After, i => i.MatchLdsfld(Reflection.Main.InGuideCraftMenu));
        cursor.EmitDelegate((bool inGuide) => !Configs.Crafting.RecipeUI && inGuide);
        //         ...
        //     }
    }

    public static void DrawRecipeUI(int hammerX, int hammerY) {
        _recipeState.container.Top.Pixels = hammerY - TextureAssets.InfoIcon[0].Width() - 1;
        _recipeState.container.Left.Pixels = hammerX - TextureAssets.InfoIcon[0].Width() - 1;
        _recipeInterface.Draw(Main.spriteBatch, _lastUpdateUiGameTime);
    }

    public override void UpdateUI(GameTime gameTime) {
        if (!Configs.Crafting.RecipeUI) return;
        _lastUpdateUiGameTime = gameTime;
        _recipeInterface.Update(gameTime);
    }

    public static void FilterAndSortRecipes() {
        UnfilteredCount = Main.numAvailableRecipes;
        List<RecipeListEntry> allRecipes = [];
        for (int i = 0; i < Main.numAvailableRecipes; i++) allRecipes.Add(new(Main.recipe[Main.availableRecipe[i]]));

        IEnumerable<RecipeListEntry> recipes = allRecipes;
        if (Configs.RecipeSearchBar.Enabled) recipes = ApplyRecipeSearch(recipes);
        if (Configs.RecipeFilters.Enabled) recipes = ApplyRecipeFilters(recipes);
        if (Configs.Crafting.RecipeSort) recipes = ApplyRecipeSort(recipes);

        int count = 0;
        foreach (var recipe in recipes) Main.availableRecipe[count++] = recipe.RecipeIndex;
        for (int i = count; i < UnfilteredCount; i++) Main.availableRecipe[i] = 0;
        Main.numAvailableRecipes = count;
    }

    private static IEnumerable<RecipeListEntry> ApplyRecipeSearch(IEnumerable<RecipeListEntry> recipes) {
        SearchFilter.SimpleSearch = Configs.RecipeSearchBar.Value.simpleSearch;
        return recipes.Where(SearchFilter.FitsFilter);
    }
    private static IEnumerable<RecipeListEntry> ApplyRecipeFilters(IEnumerable<RecipeListEntry> recipes) {
        RecipesPerFilter = new int[Filterer.AvailableFilters.Count];
        foreach(var entry in recipes) {
            for (int f = 0; f < Filterer.AvailableFilters.Count; f++) {
                if (Filterer.AvailableFilters[f].FitsFilter(entry)) RecipesPerFilter[f]++;
            }
        }
        _recipeState.RebuildFilterGrid();
        recipes = recipes.Where(Filterer.FitsFilter);
        return recipes;
    }
    private static IEnumerable<RecipeListEntry> ApplyRecipeSort(IEnumerable<RecipeListEntry> recipes) {
        return recipes.Order(Sorter);
    }

    public static void AddFilter(IRecipeFilter filter) => Filterer.AvailableFilters.Add(filter);
    public static void AddSortStep(IRecipeSortStep step) => Sorter.Steps.Add(step);

    internal static Asset<Texture2D> recipeFilters = null!;
    internal static Asset<Texture2D> recipeFiltersGray = null!;
    internal static Asset<Texture2D> recipeSortToggle = null!;
    internal static Asset<Texture2D> recipeSortToggleBorder = null!;
    internal static Asset<Texture2D> recipeSortingSteps = null!;

    private static GameTime _lastUpdateUiGameTime = null!;
    private static UI.States.RecipeInterface _recipeState = null!;
    private static UserInterface _recipeInterface = null!;

    public static int UnfilteredCount;
    public static int[] RecipesPerFilter = [];
    public readonly static EntryFilterer<RecipeListEntry, IRecipeFilter> Filterer = new();
    public readonly static EntrySorter<RecipeListEntry, IRecipeSortStep> Sorter = new();
    public static ItemSearchFilterWrapper SearchFilter = null!;
}
