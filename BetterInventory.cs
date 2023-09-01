using Terraria.ModLoader;

namespace BetterInventory;
public sealed class BetterInventory : Mod {
    public static BetterInventory Instance { get; private set; } = null!;

    public override void Load() {
        Instance = this;
        Utility.Load();
        Crafting.RecipeFiltering.Load();
        Crafting.BetterGuide.Load();
        Crafting.CraftingActions.Load();
    }

    public override void Unload() {
        Utility.Unload();
        Instance = null!;
    }
}
