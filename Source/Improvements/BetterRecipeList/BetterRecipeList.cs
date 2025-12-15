using System;
using MonoMod.Cil;
using SpikysLib.IL;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace BetterInventory.Improvements.BetterRecipeList;

public sealed class BetterRecipeListHooks : ILoadable {

    public bool IsLoadingEnabled(Mod mod) => Compatibility.LoadDisabledFeatures || BetterRecipeListConfig.Enabled;
    public void Load(Mod mod) {
        IL_Main.DrawInventory += static il => il.TryEdit(ILFastScroll, ref UnloadedBetterRecipeListConfig.Instance.fastScroll);

        On_Main.TryAllowingToCraftRecipe += HookTryAllowingToCraftRecipe;
    }
    public void Unload() { }

    private static void ILFastScroll(ILContext il) {
        ILCursor cursor = new(il);

        cursor.GotoRecipeDraw();

        // ...
        // if(<showRecipes>){
        //     for (<recipeIndex>) { 
        cursor.GotoNextLoc(out int recipeIndex, i => i.Next.MatchBr(out _), 124);

        for (int j = 0; j < 2; j++) { // Up and Down

            //     if(<scrool>) {
            //         if(...) SoundEngine.PlaySound(...);
            //         Main.availableRecipeY[num63] += 6.5f;
            cursor.GotoNext(i => i.SaferMatchCall(typeof(SoundEngine), nameof(SoundEngine.PlaySound)));
            cursor.GotoNext(MoveType.AfterLabel, i => i.MatchLdsfld(() => Main.recFastScroll));

            // ++ <fastScroll>
            cursor.EmitLdloc(recipeIndex); // int num63
            int s = j == 0 ? -1 : 1;
            cursor.EmitDelegate((int r) => {
                if (!BetterRecipeListConfig.Enabled) return;
                if (BetterRecipeListConfig.FastScroll) FastScroll_ModifyScroll(s, r);
            });
            //         ...
            //     }
        }
        //         ...
        //     }
        //     ...
        // }
    }

    public static void FastScroll_ModifyScroll(int direction, int recipe) {
        Main.availableRecipeY[recipe] += direction * 6.5f;
        float d = Main.availableRecipeY[recipe] - (recipe - Main.focusRecipe) * 65;
        bool recFast = Main.recFastScroll && FastScrollConfig.Instance.listScroll;
        if (recFast) d *= 3;
        float old = Main.availableRecipeY[recipe];
        Main.availableRecipeY[recipe] -= direction == 1 ? MathF.Max(direction * 6.5f, d / 10) : MathF.Min(direction * 6.5f, d / 10);
        if (old * Main.availableRecipeY[recipe] < 0) SoundEngine.PlaySound(SoundID.MenuTick);
        if (recFast) Main.availableRecipeY[recipe] += 130000f * direction;
    }

    private static bool HookTryAllowingToCraftRecipe(On_Main.orig_TryAllowingToCraftRecipe orig, Recipe currentRecipe, bool tryFittingItemInInventoryToAllowCrafting, out bool movedAnItemToAllowCrafting) {
        if (BetterRecipeListConfig.Enabled && BetterRecipeListConfig.CraftWhenHolding) tryFittingItemInInventoryToAllowCrafting = true;
        return orig(currentRecipe, tryFittingItemInInventoryToAllowCrafting, out movedAnItemToAllowCrafting);
    }
}