using System.ComponentModel;
using SpikysLib.Configs;
using Terraria.ModLoader.Config;

namespace BetterInventory.Configs;

public sealed class FeatureList : ModConfig {
    public Toggle<ScrollableTooltip> scrollableTooltip = new(true);

    public static FeatureList Instance = null!;
    public static bool ScrollableTooltip => Instance.scrollableTooltip;

    public override ConfigScope Mode => ConfigScope.ClientSide;
}

public sealed class ScrollableTooltip {
    [DefaultValue(1)] public float maximumHeight = 1;

    public static ScrollableTooltip Instance = FeatureList.Instance.scrollableTooltip.Value;
}
