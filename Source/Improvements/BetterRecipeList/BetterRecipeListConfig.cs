using System.ComponentModel;
using BetterInventory.Configs;
using SpikysLib.Configs;

namespace BetterInventory.Improvements.BetterRecipeList;

using BRLUnloadableAttribute = UnloadableAttribute<UnloadedBetterRecipeListConfig>;

public sealed class BetterRecipeListConfig {
    [DefaultValue(true)] public bool craftWhenHolding = true;
    [BRLUnloadable(nameof(fastScroll))] public Toggle<FastScrollConfig> fastScroll = new(true);

    public static bool Enabled => ImprovementsConfig.Instance.betterRecipeList;
    public static BetterRecipeListConfig Instance => ImprovementsConfig.Instance.betterRecipeList.Value;
    public static bool CraftWhenHolding => Instance.craftWhenHolding;
    public static bool FastScroll => Instance.fastScroll && !UnloadedBetterRecipeListConfig.Instance.fastScroll;
}

public sealed class FastScrollConfig {
    [DefaultValue(true)] public bool listScroll = true;

    public static FastScrollConfig Instance => BetterRecipeListConfig.Instance.fastScroll.Value;
}

public sealed class UnloadedBetterRecipeListConfig {
    public bool fastScroll;

    public static UnloadedBetterRecipeListConfig Instance => UnloadedImprovementsConfig.Instance.betterRecipeList;
}
