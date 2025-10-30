using Terraria;
using Terraria.ModLoader;

namespace BetterInventory.VanillaFixes;

// TODO move

public sealed class CraftWhenHolding : ILoadable {

    public static bool Enabled => Configs.VanillaFixes.Instance.craftWhenHolding;

    public void Load(Mod mod) {
        On_Main.TryAllowingToCraftRecipe += HookTryAllowingToCraftRecipe;
    }
    public void Unload() { }


    private static bool HookTryAllowingToCraftRecipe(On_Main.orig_TryAllowingToCraftRecipe orig, Recipe currentRecipe, bool tryFittingItemInInventoryToAllowCrafting, out bool movedAnItemToAllowCrafting)
        => orig(currentRecipe, tryFittingItemInInventoryToAllowCrafting || Enabled, out movedAnItemToAllowCrafting);
}