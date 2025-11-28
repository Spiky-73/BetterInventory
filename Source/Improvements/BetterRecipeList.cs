using System;
using MonoMod.Cil;
using SpikysLib.IL;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace BetterInventory.Improvements;

public sealed class BetterRecipeList : ILoadable {

    public bool IsLoadingEnabled(Mod mod) => !Configs.Compatibility.CompatibilityMode || Configs.Improvements.BetterRecipeList;
    public void Load(Mod mod) {
        IL_Main.DrawInventory += static il => {
            if (!il.ApplyTo(ILFastScroll, Configs.BetterRecipeList.FastScroll)) Configs.UnloadedImprovements.Instance.betterRecipeList_fastScroll = true;
        };

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
            cursor.GotoNext(MoveType.AfterLabel, i => i.MatchLdsfld(Reflection.Main.recFastScroll));

            // ++ <fastScroll>
            cursor.EmitLdloc(recipeIndex); // int num63
            int s = j == 0 ? -1 : 1;
            cursor.EmitDelegate((int r) => {
                if (!Configs.BetterRecipeList.FastScroll) return;
                Main.availableRecipeY[r] += s * 6.5f;
                float d = Main.availableRecipeY[r] - (r - Main.focusRecipe) * 65;
                bool recFast = Main.recFastScroll && Configs.FastScroll.Instance.listScroll;
                if (recFast) d *= 3;
                float old = Main.availableRecipeY[r];
                Main.availableRecipeY[r] -= s == 1 ? MathF.Max(s * 6.5f, d / 10) : MathF.Min(s * 6.5f, d / 10);
                if (old * Main.availableRecipeY[r] < 0) SoundEngine.PlaySound(SoundID.MenuTick);
                if (recFast) Main.availableRecipeY[r] += 130000f * s;
            });
            //         ...
            //     }
        }
        //         ...
        //     }
        //     ...
        // }
    }

    private static bool HookTryAllowingToCraftRecipe(On_Main.orig_TryAllowingToCraftRecipe orig, Recipe currentRecipe, bool tryFittingItemInInventoryToAllowCrafting, out bool movedAnItemToAllowCrafting)
        => orig(currentRecipe, tryFittingItemInInventoryToAllowCrafting || Configs.BetterRecipeList.CraftWhenHolding, out movedAnItemToAllowCrafting);
}