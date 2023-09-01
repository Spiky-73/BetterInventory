using Terraria.ModLoader;

namespace BetterInventory.Globals;

public sealed class BetterWorld : ModSystem {
    public override void PostAddRecipes() {
        Crafting.RecipeFiltering.PostAddRecipes();
    }
} 