using BetterInventory.ItemSearch.BetterGuide;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace BetterInventory;

public sealed class DisplayedRecipes : ModSystem {

    public override void Load() {
        On_Recipe.FindRecipes += HookFilterRecipes;
    }

    private void HookFilterRecipes(On_Recipe.orig_FindRecipes orig, bool canDelayCheck) {
        orig(canDelayCheck);
        needsRefresh = true;
        if (!canDelayCheck && needsRefresh && !_refreshedThisTick) UpdateDisplayedRecipes();
    }

    public override void PostDrawInterface(SpriteBatch spriteBatch) {
        if (needsRefresh) UpdateDisplayedRecipes();
        _refreshedThisTick = false;
    }

    private static void UpdateDisplayedRecipes() {
        needsRefresh = false;
        _refreshedThisTick = true;

        int oldRecipe = Main.availableRecipe[Main.focusRecipe];
        float focusY = Main.availableRecipeY[Main.focusRecipe];

        if(Configs.BetterGuide.AvailableRecipes) AvailableRecipes.FindDisplayedRecipes();
        if(Configs.Crafting.RecipeUI) Crafting.RecipeUI.FilterAndSortRecipes();

        Reflection.Recipe.TryRefocusingRecipe.Invoke(oldRecipe);
        Reflection.Recipe.VisuallyRepositionRecipes.Invoke(focusY);
    }

    public static bool needsRefresh;
    private static bool _refreshedThisTick;
}