using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using MonoMod.Cil;
using System.Reflection;
using Mono.Cecil.Cil;
using Terraria.Utilities;

namespace BetterInventory.Globals;

public class BetterInventorySystem : ModSystem {


    public override void Load() {
        // IL_Recipe.FindRecipes += ILFindRecipes;
        // On_Main.TryAllowingToCraftRecipe += HookTryAllowingToCraftRecipe;
    }

    private static void ILFindRecipes(ILContext il) {
        ILCursor cursor = new(il);

        if (!cursor.TryGotoNext(MoveType.After, i => i.MatchStloc(6))){
            BetterInventory.Instance.Logger.Error("Recipe filtering hook could not be applied. This feature has been automaticaly disabled");
            return;
        }
        cursor.Emit(OpCodes.Ldloc_S, (byte)6);
        cursor.EmitDelegate((Dictionary<int, int> materials) => {
            if (Configs.ClientConfig.Instance.filterRecipes) InventoryManagement.Chests.AddCratingMaterials(materials);
        });

        MethodInfo recipeAvailableMethod = typeof(RecipeLoader).GetMethod(nameof(RecipeLoader.RecipeAvailable), BindingFlags.Public | BindingFlags.Static, new System.Type[]{typeof(Recipe)})!;
        cursor.GotoNext(MoveType.After, i => i.MatchCall(recipeAvailableMethod));
        cursor.Emit(OpCodes.Ldloc_S, (byte)13);
        cursor.EmitDelegate((bool available, int n) => available && (!Configs.ClientConfig.Instance.filterRecipes || !InventoryManagement.Chests.HideRecipe(Main.recipe[n])));
    }

    private static bool HookTryAllowingToCraftRecipe(On_Main.orig_TryAllowingToCraftRecipe orig, Recipe currentRecipe, bool tryFittingItemInInventoryToAllowCrafting, out bool movedAnItemToAllowCrafting)
        => orig(currentRecipe, Configs.ClientConfig.Instance.filterRecipes || tryFittingItemInInventoryToAllowCrafting, out movedAnItemToAllowCrafting);
}