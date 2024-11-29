using System.Collections.Generic;
using MonoMod.Cil;
using SpikysLib;
using SpikysLib.DataStructures;
using Terraria;
using Terraria.ModLoader;

namespace BetterInventory.ItemSearch;

public sealed partial class Guide : ModSystem {

    private static void HookUpdatedOwnedItems(On_Recipe.orig_CollectItemsToCraftWithFrom orig, Player player) {
        orig(player);
        if(player.whoAmI != Main.myPlayer) return;
        foreach(var key in PlayerHelper.OwnedItems.Keys) {
            if (key < 1000000 && !GuideCraftInMenu.LocalFilters.HasOwnedItem(key)) GuideCraftInMenu.LocalFilters.AddOwnedItem(new(key));
        }
    }

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
            // Skip unknown recipes
            if (Configs.BetterGuide.UnknownDisplay && Configs.BetterGuide.Value.unknownDisplay != Configs.UnknownDisplay.Known && !GuideCraftInMenu.LocalFilters.IsKnownRecipe(Main.recipe[r])) {
                s_unknownRecipes.Add(r);
                return true;
            }
            // Skip Favorited recipes
            if (Configs.BetterGuide.FavoritedRecipes) {
                if (GuideCraftInMenu.LocalFilters.FavoritedRecipes.Contains(r)) return true;
                if (GuideCraftInMenu.LocalFilters.BlacklistedRecipes.Contains(r)) return true;
            }
            return false;
        }

        // Add favorited recipes
        if (Configs.BetterGuide.FavoritedRecipes) foreach (int r in GuideCraftInMenu.LocalFilters.FavoritedRecipes) yield return r;
        
        // Add "normal" recipes
        for (int r = 0; r < Recipe.numRecipes; r++) if (!Skip(r)) yield return r;

        // Add blacklisted recipes
        if (Configs.BetterGuide.FavoritedRecipes) foreach (int r in GuideCraftInMenu.LocalFilters.BlacklistedRecipes) yield return r;
        
        // Add "???" recipes
        if (Configs.BetterGuide.UnknownDisplay && Configs.BetterGuide.Value.unknownDisplay == Configs.UnknownDisplay.Unknown) foreach (int r in s_unknownRecipes) yield return r;
    }

    public static bool IsUnknown(int recipe) => s_unknownRecipes.Contains(recipe);
    public static void ClearUnknownRecipes() => s_unknownRecipes.Clear();

    private static IEnumerator<int>? s_ilOrderedRecipes;
    private static readonly RangeSet s_unknownRecipes = [];
}
