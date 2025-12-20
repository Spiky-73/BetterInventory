using System.ComponentModel;
using SpikysLib.Configs;
using BetterInventory.Configs;

namespace BetterInventory.Improvements.BetterTooltip;

using BTUnloadableAttribute = UnloadableAttribute<UnloadedBetterTooltipConfig>;

public sealed class BetterTooltipConfig {
    public Toggle<ScrollableTooltipConfig> scrollableTooltip = new(true);
    [BTUnloadable(nameof(tooltipHover))] public Toggle<TooltipHoverConfig> tooltipHover = new(true);
    public bool fixedTooltipPosition;

    public static BetterTooltipConfig Instance => ImprovementsConfig.Instance.betterTooltip.Value;
    public static bool ScrollableTooltip => Instance.scrollableTooltip;
    public static bool TooltipHover => Instance.tooltipHover;
    public static bool FixedTooltipPosition => Instance.fixedTooltipPosition;
}

public sealed class ScrollableTooltipConfig {
    [DefaultValue(1)] public float maximumHeight = 1;

    public static ScrollableTooltipConfig Instance = BetterTooltipConfig.Instance.scrollableTooltip.Value;
}

public sealed class TooltipHoverConfig {
    [DefaultValue(10)] public int graceTime = 10;

    public static TooltipHoverConfig Value => BetterTooltipConfig.Instance.tooltipHover.Value;
}

public sealed class UnloadedBetterTooltipConfig {
    public bool tooltipHover;

    public static UnloadedBetterTooltipConfig Instance => UnloadedImprovementsConfig.Instance.betterTooltip;
}