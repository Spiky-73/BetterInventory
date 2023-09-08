using Terraria.ModLoader;

namespace BetterInventory.Globals;

public sealed class BetterWorld : ModSystem {
    public override void PostAddRecipes() {
        ItemSearch.BetterGuide.PostAddRecipes();
    }
} 