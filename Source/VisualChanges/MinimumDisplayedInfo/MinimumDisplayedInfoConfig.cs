using System.ComponentModel;

namespace BetterInventory.VisualChanges.MinimalDisplayedInfo;

public sealed class MinimalDisplayedInfoConfig {
    [DefaultValue(DisplayedUnlockLevel.Drops)] public DisplayedUnlockLevel unlockLevel = DisplayedUnlockLevel.Drops;

    public static MinimalDisplayedInfoConfig Instance => VisualChangesConfig.Instance.minimalDisplayedInfo.Value;
}

public enum DisplayedUnlockLevel { Locked, Name, Stats, Drops, DropRates }
