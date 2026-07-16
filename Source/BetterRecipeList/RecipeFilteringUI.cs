using BetterInventory.BetterRecipeList.UI.States;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using ReLogic.Content;
using SpikysLib.IL;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI;

namespace BetterInventory.BetterRecipeList;

public sealed class RecipeFilteringUISystem : ModSystem {

    public override bool IsLoadingEnabled(Mod mod) => Compatibility.LoadDisabledFeatures || BetterRecipeListConfig.RecipeFilters;
    public override void Load() {
        IL_Main.DrawInventory += il => il.TryEdit(ILDrawUI, ref UnloadedBetterRecipeListConfig.Instance.recipeFilters);

        RecipeFilters = Mod.Assets.Request<Texture2D>($"Assets/Recipe_Filters");
        RecipeFiltersGray = Mod.Assets.Request<Texture2D>($"Assets/Recipe_Filters_Gray");
        ImageSearchCancel = Main.Assets.Request<Texture2D>("Images/UI/SearchCancel");
        RecipeSortToggle = Mod.Assets.Request<Texture2D>($"Assets/Sort_Toggle");
        RecipeSortToggleBorder = Mod.Assets.Request<Texture2D>($"Assets/Sort_Toggle_Border");
        RecipeSortingSteps = Mod.Assets.Request<Texture2D>($"Assets/RecipeSortingSteps");
    }

    public override void PostSetupRecipes() {
        RecipeFilteringPlayer.SetupMiscFallbackFilter();
        RecipeFilteringUI.Setup();

        // TODO update UI pre-enter world (e.g. different player)
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
            if (!BetterRecipeListConfig.RecipeFilters || _unfilteredCount == 0) return;
            _recipeUIVisible = true;
            RecipeFilteringUI.Draw(94, 450 + y);
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
            if (!BetterRecipeListConfig.RecipeFilters) return inGuide;
            return false;
        });
        //         ...
        //     }
    }

    public override void UpdateUI(GameTime gameTime) {
        if (!BetterRecipeListConfig.RecipeFilters || !_recipeUIVisible) return;
        _recipeUIVisible = false;
        RecipeFilteringUI.Update(gameTime);
    }

    public static void PreRecipeFiltering() {
        if (BetterRecipeListConfig.RecipeFilters) _unfilteredCount = Main.numAvailableRecipes;
    }

    private static int _unfilteredCount;
    private static bool _recipeUIVisible;

    public static Asset<Texture2D> RecipeFilters = null!;
    public static Asset<Texture2D> RecipeFiltersGray = null!;
    public static Asset<Texture2D> RecipeSortToggle = null!;
    public static Asset<Texture2D> RecipeSortToggleBorder = null!;
    public static Asset<Texture2D> RecipeSortingSteps = null!;
    public static Asset<Texture2D> ImageSearchCancel = null!;
}

public static class RecipeFilteringUI {

    public static void Setup() {
        _recipeState = new();
        _recipeInterface = new();
    }

    public static void Draw(int x, int y) {
        _recipeState.Reposition(x, y);
        _recipeState.Draw(Main.spriteBatch);
    }

    public static void Activate() {
        _recipeInterface.SetState(null);
        _recipeInterface.SetState(_recipeState);
    }

    public static void Update(GameTime gameTime) => _recipeInterface.Update(gameTime);

    private static UIRecipeFiltering _recipeState = null!;
    private static UserInterface _recipeInterface = null!;
}