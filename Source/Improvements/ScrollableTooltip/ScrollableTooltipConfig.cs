using System.ComponentModel;
using BetterInventory.Configs;

namespace BetterInventory.Improvements.ScrollableTooltip;

public sealed class ScrollableTooltipConfig {
    [DefaultValue(1)] public float maximumHeight = 1;

    public static bool Enabled => ImprovementsConfig.Instance.scrollableTooltip;
    public static ScrollableTooltipConfig Instance = ImprovementsConfig.Instance.scrollableTooltip.Value;

}