using BetterInventory.Features.QuickMove;
using BetterInventory.Features.RecipeFiltering;
using SpikysLib.Configs;
using Terraria.ModLoader.Config;

namespace BetterInventory.Configs;

public sealed class FeaturesConfig : ModConfig {

    public Toggle<RecipeFilteringConfig> recipeFiltering = new(true);
    public Toggle<QuickMoveConfig> quickMove = new(true);

    public static FeaturesConfig Instance = null!;

    public override ConfigScope Mode => ConfigScope.ClientSide;

    public override void OnChanged() {
        if(RecipeFilteringConfig.Enabled) RecipeFilteringUI.RebuildUI();
    }
}

public sealed class UnloadedFeaturesConfig {
    public UnloadedQuickMoveConfig quickMove = new();
    public bool recipeFiltering = false;

    public static UnloadedFeaturesConfig Instance => CompatibilityConfig.Instance.unloadedFeatures;
}
