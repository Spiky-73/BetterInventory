using Terraria.ModLoader.Config;
using Terraria.ModLoader.Config.UI;
using TManager = Terraria.ModLoader.Config.ConfigManager;

namespace BetterInventory.Reflection;

public static class ConfigManager {
    public static readonly StaticMethod<ModConfig, object?> Save = new(typeof(TManager), nameof(Save));
    public static readonly StaticMethod<ModConfig, object?> Load = new(typeof(TManager), nameof(Load));
    public static readonly StaticMethod<PropertyFieldWrapper, string> GetLocalizedLabel = new(typeof(TManager), nameof(GetLocalizedLabel));
    public static readonly StaticMethod<PropertyFieldWrapper, string> GetLocalizedTooltip = new(typeof(TManager), nameof(GetLocalizedTooltip));
}