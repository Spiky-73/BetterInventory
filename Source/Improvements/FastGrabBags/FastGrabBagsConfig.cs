using System.ComponentModel;

namespace BetterInventory.Improvements.FastGrabBags;

public sealed class FastGrabBagsConfig {
    [DefaultValue(true)] public bool fastContainerOpening;
    [DefaultValue(true)] public bool fastExtractinator;

    public static FastGrabBagsConfig Instance => ImprovementsConfig.Instance.fastGrabBags.Value;

    public static bool FastContainerOpening => ImprovementsConfig.FastGrabBags && Instance.fastContainerOpening;
    public static bool FastExtractinator => ImprovementsConfig.FastGrabBags && Instance.fastExtractinator;
}