using System.ComponentModel;

namespace BetterInventory.VisualChanges.GrabBagTooltip;

public sealed class GrabBagTooltipConfig {
    [DefaultValue(true)] public bool compact = true;

    public static GrabBagTooltipConfig Instance => VisualChangesConfig.Instance.grabBagTooltip.Value;
}
