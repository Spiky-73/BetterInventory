using System;
using MonoMod.Cil;
using SpikysLib.IL;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace BetterInventory.BetterRecipeList;

public sealed class FastScroll : ILoadable {

    public bool IsLoadingEnabled(Mod mod) => Compatibility.LoadDisabledFeatures || BetterRecipeListConfig.FastScroll;
    public void Load(Mod mod) {
        IL_Main.DrawInventory += static il => il.TryEdit(ILFastScroll, ref UnloadedBetterRecipeListConfig.Instance.fastScroll);
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
            int direction = j == 0 ? -1 : 1;
            cursor.EmitDelegate((int recipe) => {
                if (!BetterRecipeListConfig.FastScroll) return;
                Main.availableRecipeY[recipe] += direction * 6.5f;
                float d = Main.availableRecipeY[recipe] - (recipe - Main.focusRecipe) * 65;
                bool recFast = Main.recFastScroll && FastScrollConfig.Instance.listScroll;
                if (recFast) d *= 3;
                float old = Main.availableRecipeY[recipe];
                Main.availableRecipeY[recipe] -= direction == 1 ? MathF.Max(direction * 6.5f, d / 10) : MathF.Min(direction * 6.5f, d / 10);
                if (old * Main.availableRecipeY[recipe] < 0) SoundEngine.PlaySound(SoundID.MenuTick);
                if (recFast) Main.availableRecipeY[recipe] += 130000f * direction;
            });
            //         ...
            //     }
        }
        //         ...
        //     }
        //     ...
        // }
    }
}