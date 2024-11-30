using System.Collections.Generic;
using MonoMod.Cil;
using Terraria;
using Terraria.ModLoader;

namespace BetterInventory.ItemSearch;

// TODO redo
public sealed partial class Guide : ModSystem {

    private static void HookCollectGuideRecipes(On_Recipe.orig_CollectGuideRecipes orig) {
        if (Configs.BetterGuide.RecipeOrdering) s_ilOrderedRecipes = GetDisplayedRecipes().GetEnumerator();
        orig();
        s_ilOrderedRecipes = null;
    }

    private static void ILGuideRecipeOrder(ILContext il) {
        ILCursor cursor = new(il);

        Utility.GotoRecipeDisabled(cursor, out ILLabel endLoop, out int index, out _);

        cursor.GotoLabel(endLoop!);
        cursor.GotoNext(i => i.MatchStloc(index));
        cursor.GotoNext(MoveType.After, i => i.MatchLdloc(index));
        cursor.EmitDelegate((int index) => {
            if (!Configs.BetterGuide.RecipeOrdering) return index;
            return s_ilOrderedRecipes!.MoveNext() ? s_ilOrderedRecipes.Current : Recipe.numRecipes;
        });
        cursor.EmitDup();
        cursor.EmitStloc(index);

        //     }
        // }
    }
    
    private static IEnumerable<int> GetDisplayedRecipes() {
        static bool Skip(int r) {
            // // Skip unknown recipes
            // if (Configs.BetterGuide.UnknownDisplay && Configs.BetterGuide.Value.unknownDisplay != Configs.UnknownDisplay.Known && !GuideUnknownDisplayPlayer.LocalPlayer.IsKnownRecipe(Main.recipe[r])) {
            //     s_unknownRecipes.Add(r);
            //     return true;
            // }
            // Skip Favorited recipes
            // if (Configs.BetterGuide.FavoritedRecipes) {
            //     if (GuideFavoritedRecipesPlayer.LocalPlayer.IsFavorited(r)) return true;
            //     if (GuideFavoritedRecipesPlayer.LocalPlayer.IsBlacklisted(r)) return true;
            // }
            return false;
        }

        // Add favorited recipes
        // if (Configs.BetterGuide.FavoritedRecipes) foreach (int r in GuideFavoritedRecipesPlayer.LocalPlayer.FavoritedRecipes) yield return r;
        
        // Add "normal" recipes
        for (int r = 0; r < Recipe.numRecipes; r++) if (!Skip(r)) yield return r;

        // Add blacklisted recipes
        // if (Configs.BetterGuide.FavoritedRecipes) foreach (int r in GuideFavoritedRecipesPlayer.LocalPlayer.BlacklistedRecipes) yield return r;
        
        // // Add "???" recipes
        // if (Configs.BetterGuide.UnknownDisplay && Configs.BetterGuide.Value.unknownDisplay == Configs.UnknownDisplay.Unknown) foreach (int r in s_unknownRecipes) yield return r;
    }

    private static IEnumerator<int>? s_ilOrderedRecipes;
}
