using System.ComponentModel;

namespace BetterInventory.VisualChanges.GrabBagContent;

public sealed class GrabBagContentConfig {
    [DefaultValue(true)] public bool tooltip = true;
    [DefaultValue(true)] public bool bestiary = true;
    [DefaultValue(true)] public bool compact = true;

    public static GrabBagContentConfig Instance => VisualChangesConfig.Instance.grabBagContent.Value;
}
