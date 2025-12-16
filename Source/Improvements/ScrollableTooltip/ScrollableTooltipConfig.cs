using System.ComponentModel;

namespace BetterInventory.Improvements.ScrollableTooltip;

public sealed class ScrollableTooltipConfig {
    [DefaultValue(1)] public float maximumHeight = 1;

    public static ScrollableTooltipConfig Instance = ImprovementsConfig.Instance.scrollableTooltip.Value;

}