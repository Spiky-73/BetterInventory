using BetterInventory.Features.RecipeFiltering.UI.States;
using Microsoft.Xna.Framework;
using MonoMod.Cil;
using SpikysLib.IL;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI;

namespace BetterInventory.Features.RecipeFiltering;

public sealed class RecipeFilteringUIPlayer : ModPlayer {

    public override bool IsLoadingEnabled(Mod mod) => Compatibility.LoadDisabledFeatures || FeaturesConfig.RecipeFiltering;
    public override void OnEnterWorld() {
        if (FeaturesConfig.RecipeFiltering) RecipeFilteringUI.Rebuild();
    }
}

public sealed class RecipeFilteringUISystem : ModSystem {

    public override bool IsLoadingEnabled(Mod mod) => Compatibility.LoadDisabledFeatures || FeaturesConfig.RecipeFiltering;
    public override void Load() {
        IL_Main.DrawInventory += il => il.TryEdit(ILDrawUI, ref UnloadedFeaturesConfig.Instance.recipeFiltering);
    }

    public override void PostSetupRecipes() => RecipeFilteringUI.Setup();

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
            if (!FeaturesConfig.RecipeFiltering || _unfilteredCount == 0) return;
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
            if (!FeaturesConfig.RecipeFiltering) return inGuide;
            return false;
        });
        //         ...
        //     }
    }

    public override void UpdateUI(GameTime gameTime) {
        if (!FeaturesConfig.RecipeFiltering || !_recipeUIVisible) return;
        _recipeUIVisible = false;
        RecipeFilteringUI.Update(gameTime);
    }

    public static void PreRecipeFiltering() {
        if (FeaturesConfig.RecipeFiltering) _unfilteredCount = Main.numAvailableRecipes;
    }

    private static int _unfilteredCount;
    private static bool _recipeUIVisible;
}

public static class RecipeFilteringUI {

    public static void Setup() {
        _recipeState = new();
        _recipeState.Activate();
        _recipeInterface = new();
        _recipeInterface.SetState(_recipeState);
    }

    public static void Rebuild() => _recipeState?.Rebuild();

    public static void Draw(int x, int y) {
        _recipeState.Reposition(x, y);
        _recipeState.Draw(Main.spriteBatch);
    }

    public static void Update(GameTime gameTime) => _recipeInterface.Update(gameTime);

    private static UIRecipeFiltering _recipeState = null!;
    private static UserInterface _recipeInterface = null!;
}