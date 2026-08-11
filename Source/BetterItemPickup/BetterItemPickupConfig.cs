using System.ComponentModel;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader.Config;

namespace BetterInventory.BetterItemPickup;

using BIPUnloadableAttribute = UnloadableAttribute<UnloadedBetterItemPickupConfig>;

public sealed class BetterItemPickupConfig : ModConfig {

    // public Toggle<PickupToPreviousSlotConfig> pickupToPreviousSlot = new(true);
    [DefaultValue(true)] public bool smartPickup = true;
    [DefaultValue(true)] public bool pickupToBanks = true;
    [DefaultValue(true)] public bool autoQuickStack = true;
    // public Toggle<AutoEquipConfig> autoEquip = new(true);
    // public Toggle<AutoUpgradeConfig> autoUpgrade = new(true);
    [DefaultValue(false)] public bool prioritizeVoidBag = false;
    [BIPUnloadable(nameof(pickupHotbarLast)), DefaultValue(true)] public bool pickupHotbarLast;

    public static BetterItemPickupConfig Instance = null!;

    // public static bool PickupToPreviousSlot => BetterInventoryConfig.BetterItemPickup && Instance.pickupToPreviousSlot && !UnloadedBetterItemPickupConfig.Instance.;
    public static bool SmartPickup => BetterInventoryConfig.BetterItemPickup && Instance.smartPickup;
    public static bool PickupToBanks => BetterInventoryConfig.BetterItemPickup && Instance.pickupToBanks;
    public static bool AutoQuickStack => BetterInventoryConfig.BetterItemPickup && Instance.autoQuickStack && (Main.netMode != NetmodeID.MultiplayerClient || !UnloadedBetterItemPickupConfig.Instance.autoQuickStack_Multiplayer);
    // public static bool AutoEquip => BetterInventoryConfig.BetterItemPickup && Instance.autoEquip && !UnloadedBetterItemPickupConfig.Instance.;
    // public static bool AutoUpgrade => BetterInventoryConfig.BetterItemPickup && Instance.autoUpgrade && !UnloadedBetterItemPickupConfig.Instance.;
    public static bool PrioritizeVoidBag => BetterInventoryConfig.BetterItemPickup && Instance.prioritizeVoidBag;
    public static bool PickupHotbarLast => BetterInventoryConfig.BetterItemPickup && Instance.pickupHotbarLast && !UnloadedBetterItemPickupConfig.Instance.pickupHotbarLast;

    public override ConfigScope Mode => ConfigScope.ClientSide;
}

public sealed class UnloadedBetterItemPickupConfig {
    public bool pickupHotbarLast;
    public bool autoQuickStack_Multiplayer;

    public static UnloadedBetterItemPickupConfig Instance => BetterInventoryConfig.Instance.unloadedBetterItemPickup;
}
