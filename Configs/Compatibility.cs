using System.ComponentModel;
using Newtonsoft.Json;
using Terraria.ModLoader.Config;

namespace BetterInventory.Configs;

public sealed class Compatibility : ModConfig {

    // [DefaultValue(false)] public bool compatibilityMode;

    [JsonIgnore, ShowDespiteJsonIgnore] public CraftingCompatibility crafting = new();
    [JsonIgnore, ShowDespiteJsonIgnore] public InventoryManagementCompatibility inventoryManagement = new();
    [JsonIgnore, ShowDespiteJsonIgnore] public ItemSearchCompatibility itemSearch = new();

    public static CraftingCompatibility Crafting => Instance.crafting;
    public static InventoryManagementCompatibility InventoryManagement => Instance.inventoryManagement;
    public static ItemSearchCompatibility ItemSearch => Instance.itemSearch;
    public static Compatibility Instance = null!;
    
    public override ConfigScope Mode => ConfigScope.ClientSide;
}

public sealed class CraftingCompatibility {
    [DefaultValue(false)] public bool fastScroll = false;
    [DefaultValue(false)] public bool listScroll = false;
    [DefaultValue(false)] public bool wrapping = false;
    [DefaultValue(false)] public bool recipeFiltering = false;
    [DefaultValue(false)] public bool craftOnList = false;
}

public sealed class InventoryManagementCompatibility {
    [DefaultValue(false)] public bool autoEquip = false;
    [DefaultValue(false)] public bool favoriteInBanks = false;
    [DefaultValue(false)] public bool shiftRight = false;
    [DefaultValue(false)] public bool stackTrash = false;
    [DefaultValue(false)] public bool baits = false;
    [DefaultValue(false)] public bool materials = false;
    [DefaultValue(false)] public bool smartPickup = false;
    [DefaultValue(false)] public bool marks = false;
    [DefaultValue(false)] public bool QuickMoveDisplay = false;
    [DefaultValue(false)] public bool QuickMoveHightlight = false;
    [DefaultValue(false)] public bool craftStack = false;
}

public sealed class ItemSearchCompatibility {
    [DefaultValue(false)] public bool guideFavorite = false;
    [DefaultValue(false)] public bool guideUnfavoriteOnCraft = false;
    [DefaultValue(false)] public bool guideCraftInMenu = false;
    [DefaultValue(false)] public bool guideProgression = false;
    [DefaultValue(false)] public bool bestiaryProgression = false;
    [DefaultValue(false)] public bool bestiaryDisplayedUnlock = false;
}