using System.ComponentModel;
using SpikysLib.Configs;
using Terraria.ModLoader.Config;

namespace BetterInventory.Configs;

public sealed class FeatureList : ModConfig {
    public Toggle<TooltipScroll> tooltipScroll = new(true);

    public static FeatureList Instance = null!;
    public static bool TooltipScroll => Instance.tooltipScroll;

    public override ConfigScope Mode => ConfigScope.ClientSide;
}

public sealed class TooltipScroll {
    [DefaultValue(1)] public float maximumHeight = 1;

    public static TooltipScroll Instance = FeatureList.Instance.tooltipScroll.Value;
}
