using System.ComponentModel;
using BetterInventory.Configs;
using Terraria.ModLoader.Config;


namespace BetterInventory.Features.QuickMove;

using QMUnloadableAttribute = UnloadableAttribute<UnloadedQuickMoveConfig>;

public sealed class QuickMoveConfig {
    [DefaultValue(HotkeyMode.Hotbar)] public HotkeyMode hotkeyMode = HotkeyMode.Hotbar;
    [Range(0, 3600), DefaultValue(60 * 3)] public int graceTime = 60 * 3;

    [DefaultValue(true)] public bool followItem = true;
    [DefaultValue(true)] public bool bringItem = true;
    [DefaultValue(true)] public bool returnToSlot = true;
    [DefaultValue(false)] public bool inactiveInventories = false;

    [QMUnloadable(nameof(displayedHotkeys)), DefaultValue(HotkeyDisplayMode.All)] public HotkeyDisplayMode displayedHotkeys = HotkeyDisplayMode.All;
    [DefaultValue(false)] public bool itemTooltip = false;

    public static bool Enabled => FeaturesConfig.Instance.quickMove;
    public static QuickMoveConfig Instance => FeaturesConfig.Instance.quickMove.Value;
    public static bool DisplayHotkeys => Instance.displayedHotkeys != HotkeyDisplayMode.None && !UnloadedQuickMoveConfig.Instance.displayedHotkeys;
    public static bool ItemTooltip => Instance.itemTooltip;
}

public enum HotkeyDisplayMode { None, Next, All }
public enum HotkeyMode { Hotbar, FromEnd, Reversed }

public sealed class UnloadedQuickMoveConfig {
    public bool displayedHotkeys;

    public static UnloadedQuickMoveConfig Instance => UnloadedFeaturesConfig.Instance.quickMove;
}