using System.ComponentModel;
using Terraria;
using Terraria.ModLoader.Config;

namespace BetterInventory.Configs;

public enum SmartPickupLevel {
    Off,
    FavoriteOnly,
    AllItems
}

public sealed class ClientConfig : ModConfig {

    [DefaultValue(true)] public bool smartConsumption;
    [DefaultValue(true)] public bool smartAmmo;
    [DefaultValue(SmartPickupLevel.FavoriteOnly)] public SmartPickupLevel smartPickup;
    [DefaultValue(true)] public bool itemSwap;
    [DefaultValue(true)] public bool fastRightClick;
    [DefaultValue(true)] public bool itemRightClick;

    [DefaultValue(true)] public bool betterCrafting; // ? Split in 2 

    public static bool SmartPickupEnabled(Item item) => Instance.smartPickup switch {
        SmartPickupLevel.AllItems => true,
        SmartPickupLevel.FavoriteOnly => item.favorited,
        SmartPickupLevel.Off or _ => false
    };
    public override ConfigScope Mode => ConfigScope.ClientSide;
    public static ClientConfig Instance = null!;

    public override void OnChanged() {
        BetterCrafting.OnStateChanged();
    }
}