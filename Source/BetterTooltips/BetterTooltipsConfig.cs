using System.ComponentModel;
using SpikysLib.Configs;
using Terraria.ModLoader.Config;

namespace BetterInventory.BetterTooltips;

using BTUnloadableAttribute = UnloadableAttribute<UnloadedBetterTooltipsConfig>;

public sealed class BetterTooltipsConfig : ModConfig {

    public Toggle<ScrollableTooltipConfig> scrollableTooltip = new(true);
    [BTUnloadable(nameof(tooltipHover))] public Toggle<TooltipHoverConfig> tooltipHover = new(true);
    public bool fixedTooltipPosition;

    public static BetterTooltipsConfig Instance = null!;
    public static bool ScrollableTooltip => BetterInventoryConfig.BetterTooltips && Instance.scrollableTooltip;
    public static bool TooltipHover => BetterInventoryConfig.BetterTooltips && Instance.tooltipHover && !UnloadedBetterTooltipsConfig.Instance.tooltipHover;
    public static bool FixedTooltipPosition => BetterInventoryConfig.BetterTooltips && Instance.fixedTooltipPosition;

    public override ConfigScope Mode => ConfigScope.ClientSide;
}

public sealed class ScrollableTooltipConfig {
    [DefaultValue(1)] public float maximumHeight = 1;

    public static ScrollableTooltipConfig Instance = BetterTooltipsConfig.Instance.scrollableTooltip.Value;
}

public sealed class TooltipHoverConfig {
    [DefaultValue(10)] public int graceTime = 10;

    public static TooltipHoverConfig Value => BetterTooltipsConfig.Instance.tooltipHover.Value;
}

public sealed class UnloadedBetterTooltipsConfig {
    public bool tooltipHover;

    public static UnloadedBetterTooltipsConfig Instance => BetterInventoryConfig.Instance.unloadedBetterTooltips;
}