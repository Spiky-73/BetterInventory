using System.Linq;
using BetterInventory.Features.RecipeFiltering.UI.States;
using Microsoft.Xna.Framework;
using MonoMod.Cil;
using SpikysLib.IL;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI;

namespace BetterInventory.Features.RecipeFiltering;

public sealed class RecipeFilteringPlayer : ModPlayer {
    public static RecipeFilteringPlayer LocalPlayer => Main.LocalPlayer.GetModPlayer<RecipeFilteringPlayer>();

    public override void Load() {
        RecipeFiltersPlayer.Load();
        RecipeSearchPlayer.Load();
        RecipeSortPlayer.Load();
    }

    public override void SaveData(TagCompound tag) {
        _filters.SaveData(tag);
        _search.SaveData(tag);
        _sort.SaveData(tag);

    }
    public override void LoadData(TagCompound tag) {
        _filters.LoadData(tag);
        _search.LoadData(tag);
        _sort.LoadData(tag);
    }

    public override void OnEnterWorld() {
        RecipeFiltering.RebuildUI();
    }

    public RecipeFiltersPlayer GetFiltersPlayer() => _filters;
    public RecipeSearchPlayer GetSearchPlayer() => _search;
    public RecipeSortPlayer GetSortPlayer() => _sort;
    
    private readonly RecipeFiltersPlayer _filters = new();
    private readonly RecipeSearchPlayer _search = new();
    private readonly RecipeSortPlayer _sort = new();


    public static void FilterAndSortRecipes() {
        UnfilteredCount = Main.numAvailableRecipes;
 
        var player = LocalPlayer;
        var filters = player.GetFiltersPlayer();
        var search = player.GetSearchPlayer();
        var sort = player.GetSortPlayer();
        if (!filters.IsActive() && !search.IsActive() && !sort.IsActive()) return;

        var recipes = Main.availableRecipe[0..Main.numAvailableRecipes].Select(i => Main.recipe[i]);
        if(filters.IsActive()) recipes = recipes.Where(filters.FitsFilters);
        if(search.IsActive()) recipes = recipes.Where(search.FitsFilters);
        if(sort.IsActive()) recipes = recipes.Order(sort.Comparer);

        int count = 0;
        foreach (var recipe in recipes) Main.availableRecipe[count++] = recipe.RecipeIndex;
        for (int i = count; i < UnfilteredCount; i++) Main.availableRecipe[i] = 0;
        Main.numAvailableRecipes = count;
    }


    public static int UnfilteredCount { get; private set; }
}

public sealed class RecipeFiltering : ModSystem {

    public override void Load() {
        IL_Main.DrawInventory += static il => {
            if (!il.ApplyTo(ILDrawUI, Configs.FeaturesConfig.RecipeFiltering)) Configs.UnloadedFeatures.Instance.recipeFiltering = true;
        };

    }

    public static void RebuildUI() {
        if (!Main.gameMenu && _recipeState is not null) {
            _recipeState.Rebuild();
        }
    }

    public override void PostSetupRecipes() {
        RecipeFiltersPlayer.PostSetupRecipes();
        _recipeState = new();
        _recipeState.Activate();
        _recipeInterface = new();
        _recipeInterface.SetState(_recipeState);
    }

    private static void ILDrawUI(ILContext il) {
        ILCursor cursor = new(il);

        // BetterGameUI Compatibility
        int screenY = 13;
        if (cursor.TryGotoNext(i => i.MatchCallvirt((AccessorySlotLoader i) => i.DrawAccSlots))) {
            cursor.GotoNext(i => i.MatchLdsfld(() => Main.screenHeight));
            cursor.GotoNextLoc(out screenY, i => true, 13);
        }

        // ...
        // if(<showRecipes>){
        cursor.GotoRecipeDraw();

        //     ++<drawFilters>
        cursor.EmitLdloc(screenY); // int num54
        cursor.EmitDelegate((int y) => {
            if (Configs.FeaturesConfig.RecipeFiltering && RecipeFilteringPlayer.UnfilteredCount != 0) DrawRecipeUI(94, 450 + y);
        });

        //     ...
        //     if(Main.numAvailableRecipes == 0) ...
        //     else {
        //         int num73 = 94;
        //         int num74 = 450 + num51;
        //         if (++false && Main.InGuideCraftMenu) num74 -= 150;
        cursor.GotoNext(i => i.MatchLdsfld(() => TextureAssets.CraftToggle));
        cursor.GotoPrev(MoveType.After, i => i.MatchLdsfld(() => Main.InGuideCraftMenu));
        cursor.EmitDelegate((bool inGuide) => !Configs.FeaturesConfig.RecipeFiltering && inGuide);
        //         ...
        //     }
    }

    public static void DrawRecipeUI(int hammerX, int hammerY) {
        _recipeUIVisible = true;
        _recipeState.Reposition(hammerX, hammerY);
        _recipeState.Draw(Main.spriteBatch);
    }

    public override void UpdateUI(GameTime gameTime) {
        if (!_recipeUIVisible) return;
        _recipeUIVisible = false;
        _recipeInterface.Update(gameTime);
    }
    private static bool _recipeUIVisible;

    private static UIRecipeFiltering _recipeState = null!;
    private static UserInterface _recipeInterface = null!;

}
