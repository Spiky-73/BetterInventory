using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace BetterInventory.Configs;

public sealed class Compatibility : ModConfig {

    [DefaultValue(false)] public bool compatibilityMode;

    public override ConfigScope Mode => ConfigScope.ClientSide;
    public static Compatibility Instance = null!;
}