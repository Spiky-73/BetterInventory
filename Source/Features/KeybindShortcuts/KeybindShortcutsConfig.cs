using System.ComponentModel;

namespace BetterInventory.Features.KeybindShortcuts;

public sealed class KeybindShortcutsConfig {
    [DefaultValue(true)] public bool favoritedBuff;
    [DefaultValue(true)] public bool builderAccs;
    [DefaultValue(true)] public bool quickStack;

    public static KeybindShortcutsConfig Instance => FeaturesConfig.Instance.keybindShortcuts.Value;
    public static bool FavoritedBuff => FeaturesConfig.KeybindShortcuts && Instance.favoritedBuff;
    public static bool BuilderAccs => FeaturesConfig.KeybindShortcuts && Instance.builderAccs;
    public static bool QuickStack => FeaturesConfig.KeybindShortcuts && Instance.quickStack;
}