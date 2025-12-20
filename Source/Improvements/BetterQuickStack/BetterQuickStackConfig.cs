using System.ComponentModel;
using BetterInventory.Configs;

namespace BetterInventory.Improvements.BetterQuickStack;

using BQSUnloadableAttribute = UnloadableAttribute<UnloadedBetterQuickStackConfig>;

public sealed class BetterQuickStackConfig {
    [BQSUnloadable(nameof(completeQuickStack)), DefaultValue(true)] public bool completeQuickStack = true;
    [BQSUnloadable(nameof(limitedBanksQuickStack)), DefaultValue(true)] public bool limitedBanksQuickStack = true;

    public static BetterQuickStackConfig Instance => ImprovementsConfig.Instance.betterQuickStack.Value;
    public static bool CompleteQuickStack => ImprovementsConfig.BetterQuickStack && Instance.completeQuickStack && !UnloadedBetterQuickStackConfig.Instance.completeQuickStack;
    public static bool LimitedBanksQuickStack => ImprovementsConfig.BetterQuickStack && Instance.limitedBanksQuickStack && !UnloadedBetterQuickStackConfig.Instance.limitedBanksQuickStack;
}

public sealed class UnloadedBetterQuickStackConfig {
    public bool completeQuickStack;
    public bool limitedBanksQuickStack;

    public static UnloadedBetterQuickStackConfig Instance => UnloadedImprovementsConfig.Instance.betterQuickStack;
}
