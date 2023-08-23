using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace BetterInventory.Configs;

public enum SmartPickupLevel {
    Off,
    FavoriteOnly,
    AllItems
}

public class ClientConfig : ModConfig {

    [DefaultValue(true)] public bool smartConsumption;
    [DefaultValue(true)] public bool smartAmmo;
    [DefaultValue(SmartPickupLevel.FavoriteOnly)] public SmartPickupLevel smartPickup;
    [DefaultValue(true)] public bool itemSwap;
    [DefaultValue(true)] public bool fastRightClick;
    [DefaultValue(true)] public bool itemRightClick;
    
    [DefaultValue(true)] public bool filterRecipes;


    public override ConfigScope Mode => ConfigScope.ClientSide;
    public static ClientConfig Instance = null!;
}