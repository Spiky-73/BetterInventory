using System.ComponentModel;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader.Config;

namespace BetterInventory.BetterItemPickup;


public sealed class BetterItemPickupConfig : ModConfig {
    public override bool Autoload(ref string name) => BetterInventoryConfig.EnsureLoaded(this) && base.Autoload(ref name) && BetterInventoryConfig.BetterItemPickup;

    // public Toggle<PickupToPreviousSlotConfig> pickupToPreviousSlot = new(true);
    [DefaultValue(true)] public bool smartPickup = true;
    [DefaultValue(true)] public bool pickupToBanks = true;
    [DefaultValue(true)] public bool autoQuickStack = true;
    // public Toggle<AutoEquipConfig> autoEquip = new(true);
    // public Toggle<AutoUpgradeConfig> autoUpgrade = new(true);
    [DefaultValue(false)] public bool prioritizeVoidBag = false;
    [Fallible, DefaultValue(true)] public bool pickupHotbarLast;

    public static BetterItemPickupConfig Instance = null!;

    // public static bool PickupToPreviousSlot => BetterInventoryConfig.BetterItemPickup && Instance.pickupToPreviousSlot && !FailedBetterItemPickupConfig.Instance.;
    public static bool SmartPickup => BetterInventoryConfig.BetterItemPickup && Instance.smartPickup;
    public static bool PickupToBanks => BetterInventoryConfig.BetterItemPickup && Instance.pickupToBanks;
    public static bool AutoQuickStack => BetterInventoryConfig.BetterItemPickup && Instance.autoQuickStack && (Main.netMode != NetmodeID.MultiplayerClient || !FailedBetterItemPickupConfig.Instance.autoQuickStack_Multiplayer);
    // public static bool AutoEquip => BetterInventoryConfig.BetterItemPickup && Instance.autoEquip && !FailedBetterItemPickupConfig.Instance.;
    // public static bool AutoUpgrade => BetterInventoryConfig.BetterItemPickup && Instance.autoUpgrade && !FailedBetterItemPickupConfig.Instance.;
    public static bool PrioritizeVoidBag => BetterInventoryConfig.BetterItemPickup && Instance.prioritizeVoidBag;
    public static bool PickupHotbarLast => BetterInventoryConfig.BetterItemPickup && Instance.pickupHotbarLast && !FailedBetterItemPickupConfig.Instance.pickupHotbarLast;

    public override ConfigScope Mode => ConfigScope.ClientSide;
}

public sealed class FailedBetterItemPickupConfig {
    public bool autoQuickStack_Multiplayer;
    public bool pickupHotbarLast;

    public static FailedBetterItemPickupConfig Instance => FailedBetterInventoryConfig.Instance.betterItemPickup;
}
