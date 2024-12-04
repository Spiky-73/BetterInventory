using System.Collections.Generic;
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

public sealed class RecipeFiltersPlayer : ModPlayer {
    public static RecipeFiltersPlayer LocalPlayer => Main.LocalPlayer.GetModPlayer<RecipeFiltersPlayer>();

    public override void Load() {
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
        RecipeFiltering.Filterer.AddFilters(allFilters);
    }

    public override void SaveData(TagCompound tag) {
        filters = 0;
        for (int i = 0; i < RecipeFiltering.Filterer.AvailableFilters.Count; i++) if (RecipeFiltering.Filterer.IsFilterActive(i)) filters |= 1 << i;
        if (filters != 0) tag[FiltersTag] = filters;
        var searchFilter = Reflection.EntryFilterer<Item, IRecipeFilter>._searchFilter.GetValue(RecipeFiltering.Filterer);
        string? search = searchFilter is ItemSearchFilterWrapper wrapper ? Reflection.ItemFilters.BySearch._search.GetValue(wrapper.Filter) : null;
        if (search is not null) tag[SearchTag] = search;
    }
    public override void LoadData(TagCompound tag) {
        if (tag.TryGet(FiltersTag, out int filters)) this.filters = filters;
        if (tag.TryGet(SearchTag, out string search)) this.search = search;
    }

    public override void OnEnterWorld() {
        RecipeFiltering.Filterer.ActiveFilters.Clear();
        for (int i = 0; i < RecipeFiltering.Filterer.AvailableFilters.Count; i++) if ((filters & (1 << i)) != 0) RecipeFiltering.Filterer.ToggleFilter(i);
        RecipeFiltering.Filterer.SetSearchFilter(search);
    }

    public int filters;
    public string? search;

    public const string FiltersTag = "filters";
    public const string SearchTag = "search";
    public const string SortTag = "sort";
}

// TODO sorting
public sealed class RecipeFiltering : ModSystem {

    public override void Load() {
        IL_Main.DrawInventory += static il => {
            if (!il.ApplyTo(ILDrawUI, Configs.Crafting.RecipeUI)) Configs.UnloadedCrafting.Value.RecipeUI = true;
        };

        recipeFilters = Mod.Assets.Request<Texture2D>($"Assets/Recipe_Filters");
        recipeFiltersGray = Mod.Assets.Request<Texture2D>($"Assets/Recipe_Filters_Gray");

        _recipeState = new();
        _recipeState.Activate();
        _recipeInterface = new();
        _recipeInterface.SetState(_recipeState);
        
        On_Recipe.FindRecipes += HookFilterRecipes;
        On_Recipe.ClearAvailableRecipes += HookClearAvailableRecipes;

        Filterer.SetSearchFilterObject(new ItemSearchFilterWrapper());
    }

    public static void OnConfigChanged() {
        if (_recipeState?.filters is not null) _recipeState.filters.ItemsPerLine = Configs.RecipeFilters.Value.filtersPerLine;
        if (!Main.gameMenu && _recipeState is not null) _recipeState.RebuildList();
    }

    public override void PostSetupRecipes() {
        Filterer.AvailableFilters.Add(new ItemMiscFallbackWrapper(Filterer.AvailableFilters));
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
        cursor.EmitDelegate((bool inGuide) => !Configs.RecipeFilters.Enabled && inGuide);
        //         ...
        //     }
    }

    public static void DrawRecipeUI(int hammerX, int hammerY) {
        _recipeState.container.Top.Pixels = hammerY + TextureAssets.CraftToggle[0].Height() - TextureAssets.InfoIcon[0].Width() / 2;
        _recipeState.container.Left.Pixels = hammerX - TextureAssets.InfoIcon[0].Width() - 1;
        _recipeInterface.Draw(Main.spriteBatch, _lastUpdateUiGameTime);
    }

    public override void UpdateUI(GameTime gameTime) {
        _lastUpdateUiGameTime = gameTime;
        _recipeInterface.Update(gameTime);
    }

    private void HookClearAvailableRecipes(On_Recipe.orig_ClearAvailableRecipes orig) {
        orig();
        UnfilteredCount = 0;
        RecipesPerFilter = new int[Filterer.AvailableFilters.Count];
        _clearedDisplayed = true;
    }
    private void HookFilterRecipes(On_Recipe.orig_FindRecipes orig, bool canDelayCheck) {
        orig(canDelayCheck);
        if (canDelayCheck || !_clearedDisplayed) return;
        _clearedDisplayed = false;
        UnfilteredCount = Main.numAvailableRecipes;
        var recipes = FilterRecipes();
        SortRecipes(recipes);
        for (int i = 0; i < Recipe.maxRecipes; i++) Main.availableRecipe[i] = 0;
        for (int i = 0; i < recipes.Count; i++) Main.availableRecipe[i] = recipes[i].RecipeIndex;
        Main.numAvailableRecipes = recipes.Count;

        _recipeState.RebuildRecipeGrid();
    }
    private static List<Recipe> FilterRecipes() {
        List<Recipe> recipes = [];
        for (int r = 0; r < Main.numAvailableRecipes; r++) {
            var recipe = Main.recipe[Main.availableRecipe[r]];

            for (int f = 0; f < Filterer.AvailableFilters.Count; f++) {
                if (Filterer.AvailableFilters[f].FitsFilter(recipe.createItem)) RecipesPerFilter[f]++;
            }
            if (Filterer.FitsFilter(recipe.createItem)) recipes.Add(recipe);
        }
        return recipes;
    }
    private static void SortRecipes(List<Recipe> recipes) {
        //recipes.Sort(_sorter);
    }

    internal static Asset<Texture2D> recipeFilters = null!;
    internal static Asset<Texture2D> recipeFiltersGray = null!;

    private static GameTime _lastUpdateUiGameTime = null!;
    private static UI.States.RecipeFilters _recipeState = null!;
    private static UserInterface _recipeInterface = null!;

    private static bool _clearedDisplayed;

    public static int UnfilteredCount;
    public readonly static EntryFilterer<Item, IRecipeFilter> Filterer = new();
    public static int[] RecipesPerFilter = [];
}