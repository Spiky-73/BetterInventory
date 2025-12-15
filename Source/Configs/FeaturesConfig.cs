using BetterInventory.Features.QuickMove;
using BetterInventory.Features.RecipeFiltering;
using SpikysLib.Configs;
using Terraria.ModLoader.Config;

namespace BetterInventory.Configs;

using FUnloadableAttribute = UnloadableAttribute<UnloadedFeaturesConfig>;

public sealed class FeaturesConfig : ModConfig {

    [FUnloadable(nameof(recipeFiltering))] public Toggle<RecipeFilteringConfig> recipeFiltering = new(true);
    [FUnloadable(nameof(quickMove))] public Toggle<QuickMoveConfig> quickMove = new(true);

    public static FeaturesConfig Instance = null!;

    public override ConfigScope Mode => ConfigScope.ClientSide;

    public override void OnChanged() {
        RecipeFilteringConfig.OnChanged();
    }
}

public sealed class UnloadedFeaturesConfig {
    public UnloadedQuickMoveConfig quickMove = new();
    public bool recipeFiltering = false;

    public static UnloadedFeaturesConfig Instance => CompatibilityConfig.Instance.unloadedFeatures;
}
