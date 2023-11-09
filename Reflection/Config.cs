using Terraria.ModLoader.Config;
using TManager = Terraria.ModLoader.Config.ConfigManager;

namespace BetterInventory.Reflection;

public static class ConfigManager {
    public static readonly StaticMethod<ModConfig, object?> Save = new(typeof(TManager), nameof(Save));
}