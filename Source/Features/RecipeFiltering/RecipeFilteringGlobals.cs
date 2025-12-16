using System.Linq;
using Microsoft.Xna.Framework;
using MonoMod.Cil;
using SpikysLib.IL;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace BetterInventory.Features.RecipeFiltering;

public sealed class RecipeFilteringPlayer : ModPlayer {
    public static RecipeFilteringPlayer LocalPlayer => Main.LocalPlayer.GetModPlayer<RecipeFilteringPlayer>();

    public override bool IsLoadingEnabled(Mod mod) => Compatibility.LoadDisabledFeatures || FeaturesConfig.RecipeFiltering;
    public override void Load() {
        RecipeFiltersPlayer.Load();
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

    public override void OnEnterWorld() {
        if (!FeaturesConfig.RecipeFiltering) return;
        RecipeFilteringUI.RebuildUI();
    }

    public RecipeFiltersPlayer FiltersPlayer { get; private set; } = new();
    public RecipeSearchPlayer SearchPlayer { get; private set; } = new();
    public RecipeSortPlayer SortPlayer { get; private set; } = new();
}

public sealed class RecipeFilteringSystem : ModSystem {

    public override bool IsLoadingEnabled(Mod mod) => Compatibility.LoadDisabledFeatures || FeaturesConfig.RecipeFiltering;
    public override void Load() {
        IL_Main.DrawInventory += il => il.TryEdit(ILDrawUI, ref UnloadedFeaturesConfig.Instance.recipeFiltering);
    }

    public override void PostSetupRecipes() {
        RecipeFilteringUI.PostSetupRecipes();
        RecipeFiltersPlayer.PostSetupRecipes();
    }

    private static void ILDrawUI(ILContext il) {
        ILCursor cursor = new(il);

        // BetterGameUI Compatibility
        // TODO test
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
            if (!FeaturesConfig.RecipeFiltering) return;
            RecipeFilteringUI.DrawRecipeUI(94, 450 + y);
        });

        //     ...
        //     if(Main.numAvailableRecipes == 0) ...
        //     else {
        //         int num73 = 94;
        //         int num74 = 450 + num51;
        //         if (++false && Main.InGuideCraftMenu) num74 -= 150;
        cursor.GotoNext(i => i.MatchLdsfld(() => TextureAssets.CraftToggle));
        cursor.GotoPrev(MoveType.After, i => i.MatchLdsfld(() => Main.InGuideCraftMenu));
        cursor.EmitDelegate((bool inGuide) => {
            if (!FeaturesConfig.RecipeFiltering) return inGuide;
            return false;
        });
        //         ...
        //     }
    }

    public override void UpdateUI(GameTime gameTime) {
        if (!FeaturesConfig.RecipeFiltering) return;
        RecipeFilteringUI.UpdateUI(gameTime);
    }

    public static void FilterAndSortRecipes() {
        if (!FeaturesConfig.RecipeFiltering) return;
        if (!(RecipeFilteringConfig.Search || RecipeFilteringConfig.Filters || RecipeFilteringConfig.Sort)) return;

        RecipeFilteringUI.PreRecipeFiltering();

        var allRecipes = Main.availableRecipe[0..Main.numAvailableRecipes].Select(i => Main.recipe[i]);
        var recipes = allRecipes;
        var player = RecipeFilteringPlayer.LocalPlayer;
        if (RecipeFilteringConfig.Filters && player.FiltersPlayer.IsActive()) recipes = recipes.Where(player.FiltersPlayer.FitsFilters);
        if (RecipeFilteringConfig.Search && player.SearchPlayer.IsActive()) recipes = recipes.Where(player.SearchPlayer.FitsFilters);
        if (RecipeFilteringConfig.Sort && player.SortPlayer.IsActive()) recipes = recipes.Order(player.SortPlayer.Comparer);
        if (recipes == allRecipes) return;

        int count = 0;
        foreach (var recipe in recipes) Main.availableRecipe[count++] = recipe.RecipeIndex;
        for (int i = count; i < Main.numAvailableRecipes; i++) Main.availableRecipe[i] = 0;
        Main.numAvailableRecipes = count;
    }
}
