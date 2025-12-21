using System.ComponentModel;

namespace BetterInventory.Features.ExtraItemRightClick;

public sealed class ExtraItemRightClickConfig {
    [DefaultValue(false)] public bool stackableItems = false;

    public static ExtraItemRightClickConfig Instance => FeaturesConfig.Instance.extraItemRightClick.Value;

}