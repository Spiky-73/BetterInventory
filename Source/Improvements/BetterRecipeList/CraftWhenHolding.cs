using Terraria;
using Terraria.ModLoader;

namespace BetterInventory.Improvements.BetterRecipeList;

public sealed class CraftWhenHolding : ILoadable {

    public bool IsLoadingEnabled(Mod mod) => Compatibility.LoadDisabledFeatures || BetterRecipeListConfig.CraftWhenHolding;
    public void Load(Mod mod) {
        On_Main.TryAllowingToCraftRecipe += HookTryAllowingToCraftRecipe;
    }
    public void Unload() { }

    private static bool HookTryAllowingToCraftRecipe(On_Main.orig_TryAllowingToCraftRecipe orig, Recipe currentRecipe, bool tryFittingItemInInventoryToAllowCrafting, out bool movedAnItemToAllowCrafting) {
        if (BetterRecipeListConfig.CraftWhenHolding) tryFittingItemInInventoryToAllowCrafting = true;
        return orig(currentRecipe, tryFittingItemInInventoryToAllowCrafting, out movedAnItemToAllowCrafting);
    }
}