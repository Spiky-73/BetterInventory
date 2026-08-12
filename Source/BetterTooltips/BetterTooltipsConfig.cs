using System.ComponentModel;
using SpikysLib.Configs;
using Terraria.ModLoader.Config;

namespace BetterInventory.BetterTooltips;

public sealed class BetterTooltipsConfig : ModConfig {
    public override bool Autoload(ref string name) => BetterInventoryConfig.EnsureLoaded(this) && base.Autoload(ref name) && BetterInventoryConfig.BetterTooltips;

    public Toggle<ScrollableTooltipConfig> scrollableTooltip = new(true);
    [Fallible] public Toggle<TooltipHoverConfig> tooltipHover = new(true);
    public bool fixedTooltipPosition;

    public static BetterTooltipsConfig Instance = null!;
    public static bool ScrollableTooltip => BetterInventoryConfig.BetterTooltips && Instance.scrollableTooltip;
    public static bool TooltipHover => BetterInventoryConfig.BetterTooltips && Instance.tooltipHover && !FailedBetterTooltipsConfig.Instance.tooltipHover;
    public static bool FixedTooltipPosition => BetterInventoryConfig.BetterTooltips && Instance.fixedTooltipPosition;

    public override ConfigScope Mode => ConfigScope.ClientSide;
}

public sealed class ScrollableTooltipConfig {
    [DefaultValue(1)] public float maximumHeight = 1;

    public static ScrollableTooltipConfig Instance = BetterTooltipsConfig.Instance.scrollableTooltip.Value;
}

public sealed class TooltipHoverConfig {
    [Range(0, 3600), DefaultValue(10)] public int graceTime = 10;

    public static TooltipHoverConfig Value => BetterTooltipsConfig.Instance.tooltipHover.Value;
}

public sealed class FailedBetterTooltipsConfig {
    public bool tooltipHover;

    public static FailedBetterTooltipsConfig Instance => FailedBetterInventoryConfig.Instance.betterTooltips;
}
