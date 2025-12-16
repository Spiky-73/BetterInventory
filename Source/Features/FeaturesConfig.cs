using BetterInventory.Features.QuickMove;
using BetterInventory.Features.RecipeFiltering;
using SpikysLib.Configs;
using Terraria.ModLoader.Config;
using BetterInventory.Configs;

namespace BetterInventory.Features;

using FUnloadableAttribute = UnloadableAttribute<UnloadedFeaturesConfig>;

public sealed class FeaturesConfig : ModConfig {

    [FUnloadable(nameof(recipeFiltering))] public Toggle<RecipeFilteringConfig> recipeFiltering = new(true);
    [FUnloadable(nameof(quickMove))] public Toggle<QuickMoveConfig> quickMove = new(true);

    public static FeaturesConfig Instance = null!;
    public static bool RecipeFiltering => Instance.recipeFiltering;
    public static bool QuickMove => Instance.quickMove;

    public override ConfigScope Mode => ConfigScope.ClientSide;

    public override void OnChanged() {
        if (RecipeFiltering) RecipeFilteringConfig.OnChanged();
    }
}

public sealed class UnloadedFeaturesConfig {
    public UnloadedQuickMoveConfig quickMove = new();
    public bool recipeFiltering = false;

    public static UnloadedFeaturesConfig Instance => CompatibilityConfig.Instance.unloadedFeatures;
}
