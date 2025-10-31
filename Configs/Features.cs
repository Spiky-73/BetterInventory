using Terraria.ModLoader.Config;

namespace BetterInventory.Configs;

public sealed class Features : ModConfig {

    public static Features Instance = null!;

    public override ConfigScope Mode => ConfigScope.ClientSide;
}
