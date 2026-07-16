using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace BetterInventory.BetterItemPickup;

using BIPUnloadableAttribute = UnloadableAttribute<UnloadedBetterItemPickupConfig>;

public sealed class BetterItemPickupConfig : ModConfig {
    [BIPUnloadable(nameof(ammoPickupOrder)), DefaultValue(true)] public bool ammoPickupOrder;

    public static BetterItemPickupConfig Instance = null!;
    public static bool AmmoPickupOrder => BetterInventoryConfig.BetterItemPickup && Instance.ammoPickupOrder && !UnloadedBetterItemPickupConfig.Instance.ammoPickupOrder;

    public override ConfigScope Mode => ConfigScope.ClientSide;
}

public sealed class UnloadedBetterItemPickupConfig {
    public bool ammoPickupOrder;

    public static UnloadedBetterItemPickupConfig Instance => BetterInventoryConfig.Instance.unloadedBetterItemPickup;
}
