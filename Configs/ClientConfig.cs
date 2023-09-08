using System.ComponentModel;
using Terraria;
using Terraria.ModLoader.Config;

namespace BetterInventory.Configs;

public enum SmartPickupLevel {
    Off,
    FavoriteOnly,
    AllItems
}

public enum UnknownSearchBehaviour { Hidden, Unknown, Known}

public sealed class ClientConfig : ModConfig {

    [Header("InventoryManagement")]
    [DefaultValue(true)] public bool smartConsumption;
    [DefaultValue(true)] public bool smartAmmo;
    [DefaultValue(SmartPickupLevel.FavoriteOnly)] public SmartPickupLevel smartPickup;
    [DefaultValue(true)] public bool itemSwap;
    [DefaultValue(true)] public bool fastRightClick;
    [DefaultValue(true)] public bool itemRightClick;

    [Header("Crafting")]
    [DefaultValue(true)] public bool recipeFiltering;
    [DefaultValue(true)] public bool craftOverride;
    [Header("ItemSearch")]
    [DefaultValue(true)] public bool betterGuide;
    [DefaultValue(true)] public bool searchDrops;
    [DefaultValue(UnknownSearchBehaviour.Known)] public UnknownSearchBehaviour unknownBehaviour;

    public static bool SmartPickupEnabled(Item item) => Instance.smartPickup switch {
        SmartPickupLevel.AllItems => true,
        SmartPickupLevel.FavoriteOnly => item.favorited,
        SmartPickupLevel.Off or _ => false
    };
    public override ConfigScope Mode => ConfigScope.ClientSide;
    public static ClientConfig Instance = null!;

    public override void OnChanged() => ItemSearch.SearchItem.UpdateMouseItem();
}