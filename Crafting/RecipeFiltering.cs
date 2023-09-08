using Terraria.ModLoader;

namespace BetterInventory.Crafting;

public sealed class RecipeFiltering : ILoadable {

    public static bool Enabled => Configs.ClientConfig.Instance.recipeFiltering;

    public void Load(Mod mod) {}
    public void Unload() {}

}