using System;
using System.Collections.Generic;
using BetterInventory.Crafting;
using BetterInventory.ItemSearch.BetterGuide;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace BetterInventory;

public sealed class DisplayedRecipes : ModSystem {

    public static bool Enabled => Configs.Crafting.RecipeUI || Configs.BetterGuide.AvailableRecipes || Configs.BetterGuide.RecipeOrdering;
    
    public override void Load() {
        On_Recipe.FindRecipes += HookFindAvailableRecipes;
        On_Main.HoverOverCraftingItemButton += HookDisableCraftWhenNonAvailable;
        On_Recipe.TryRefocusingRecipe += HookNoRefocus;
        On_Recipe.VisuallyRepositionRecipes += HookNoReposition;
    }

    public override void PostSetupRecipes() {
        numAvailableRecipes = 0;
        availableRecipes = new int[Main.availableRecipe.Length];
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers) {
        if(!_hasDelayedFindRecipes) return;
        _hasDelayedFindRecipes = false;
        FindDisplayedRecipes();
    }


    private static void HookDisableCraftWhenNonAvailable(On_Main.orig_HoverOverCraftingItemButton orig, int recipeIndex) {
        if (Enabled && !IsAvailable(Main.availableRecipe[recipeIndex])) Main.LockCraftingForThisCraftClickDuration();
        orig(recipeIndex);
    }

    public static bool IsAvailable(int recipe) => availableRecipes.AsSpan(0, numAvailableRecipes).BinarySearch(recipe) >= 0;
    public static bool IsAvailable(Item createItem) {
        var span = availableRecipes.AsSpan(0, numAvailableRecipes);
        foreach (var recipe in span){
            if(Main.recipe[recipe].createItem == createItem) return true;
        }
        return false;
    }

    private static void HookFindAvailableRecipes(On_Recipe.orig_FindRecipes orig, bool canDelayCheck) {
        if(canDelayCheck || !Enabled) {
            orig(canDelayCheck);
            return;
        }
        (var displayed, Main.availableRecipe) = (Main.availableRecipe, availableRecipes);
        (var numDisplayed, Main.numAvailableRecipes) = (Main.numAvailableRecipes, numAvailableRecipes);

        orig(canDelayCheck);
        numAvailableRecipes = Main.numAvailableRecipes;

        Main.availableRecipe = displayed;
        Main.numAvailableRecipes = numDisplayed;

        FindDisplayedRecipes(true);
    }

    private void HookNoRefocus(On_Recipe.orig_TryRefocusingRecipe orig, int oldRecipe) {
        if (!Enabled || Main.availableRecipe != availableRecipes) {
            FixedUI.HookTryRefocusingList(orig, oldRecipe);
        }
    }

    private void HookNoReposition(On_Recipe.orig_VisuallyRepositionRecipes orig, float focusY) {
        if (!Enabled || Main.availableRecipe != availableRecipes) orig(focusY);
    }

    public static void FindDisplayedRecipes(bool canDelayCheck = false) {
        if(canDelayCheck) {
            _hasDelayedFindRecipes = true;
            return;
        }
        int oldRecipe = Main.availableRecipe[Main.focusRecipe];
        float focusY = Main.availableRecipeY[Main.focusRecipe];

        if (!Configs.BetterGuide.AvailableRecipes) {
            for (int i = 0; i < numAvailableRecipes; i++) Main.availableRecipe[i] = availableRecipes[i];
            for (int i = numAvailableRecipes; i < Main.numAvailableRecipes; i++) Main.availableRecipe[i] = 0;
            Main.numAvailableRecipes = numAvailableRecipes;
        } else {
            Recipe.ClearAvailableRecipes();
            Reflection.Recipe.CollectGuideRecipes.Invoke();
        }

        if (Configs.Crafting.RecipeUI) Crafting.RecipeUI.FilterAndSortRecipes();
        if (Configs.BetterGuide.RecipeOrdering) FavoritedRecipesPlayer.SortRecipes();

        Reflection.Recipe.TryRefocusingRecipe.Invoke(oldRecipe);
        Reflection.Recipe.VisuallyRepositionRecipes.Invoke(focusY);
    }

    public static int numAvailableRecipes;
    public static int[] availableRecipes = [];
    
    private static bool _hasDelayedFindRecipes;
}