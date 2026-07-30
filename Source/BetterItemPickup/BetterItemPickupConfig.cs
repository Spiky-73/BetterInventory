using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace BetterInventory.BetterItemPickup;

using BIPUnloadableAttribute = UnloadableAttribute<UnloadedBetterItemPickupConfig>;

public sealed class BetterItemPickupConfig : ModConfig {
    [BIPUnloadable(nameof(fixAmmoPickupOrder)), DefaultValue(true)] public bool fixAmmoPickupOrder;
    [BIPUnloadable(nameof(fixPickupSlot)), DefaultValue(true)] public bool fixPickupSlot;
    [BIPUnloadable(nameof(pickupHotbarLast)), DefaultValue(true)] public bool pickupHotbarLast;

    public static BetterItemPickupConfig Instance = null!;
    public static bool FixAmmoPickupOrder => BetterInventoryConfig.BetterItemPickup && Instance.fixAmmoPickupOrder && !UnloadedBetterItemPickupConfig.Instance.fixAmmoPickupOrder;
    public static bool FixPickupSlot => BetterInventoryConfig.BetterItemPickup && Instance.fixPickupSlot && !UnloadedBetterItemPickupConfig.Instance.fixPickupSlot;
    public static bool PickupHotbarLast => BetterInventoryConfig.BetterItemPickup && Instance.pickupHotbarLast && !UnloadedBetterItemPickupConfig.Instance.fixPickupSlot;

    public override ConfigScope Mode => ConfigScope.ClientSide;
}

public sealed class UnloadedBetterItemPickupConfig {
    public bool fixAmmoPickupOrder;
    public bool fixPickupSlot;
    public bool pickupHotbarLast;

    public static UnloadedBetterItemPickupConfig Instance => BetterInventoryConfig.Instance.unloadedBetterItemPickup;
}
