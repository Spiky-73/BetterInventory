using BetterInventory.Features.RecipeFiltering.UI.States;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.UI;

namespace BetterInventory.Features.RecipeFiltering;

public static class RecipeFilteringUI {

    public static void PostSetupRecipes() {
        _recipeState = new();
        _recipeState.Activate();
        _recipeInterface = new();
        _recipeInterface.SetState(_recipeState);
    }

    public static void RebuildUI() {
        if (!Main.gameMenu && _recipeState is not null) {
            _recipeState.Rebuild();
        }
    }

    public static void DrawRecipeUI(int hammerX, int hammerY) {
        if (_unfilteredCount == 0) return;
        _recipeUIVisible = true;
        _recipeState.Reposition(hammerX, hammerY);
        _recipeState.Draw(Main.spriteBatch);
    }

    public static void UpdateUI(GameTime gameTime) {
        if (!_recipeUIVisible) return;
        _recipeUIVisible = false;
        _recipeInterface.Update(gameTime);
    }

    public static void PreRecipeFiltering() {
        _unfilteredCount = Main.numAvailableRecipes;
    }

    private static int _unfilteredCount;

    private static bool _recipeUIVisible;
    private static UIRecipeFiltering _recipeState = null!;
    private static UserInterface _recipeInterface = null!;
}