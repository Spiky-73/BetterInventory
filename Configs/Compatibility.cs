using System.ComponentModel;
using Newtonsoft.Json;
using Terraria.ModLoader.Config;

namespace BetterInventory.Configs;

public sealed class Compatibility : ModConfig {

    [ReloadRequired, DefaultValue(false)] public bool compatibilityMode;

    // public override bool NeedsReload(ModConfig pendingConfig) {
    //     if(!Compatibility.CompatibilityMode) return base.NeedsReload(pendingConfig);
    //     foreach (PropertyFieldWrapper fieldsAndProperty in ConfigManager.GetFieldsAndProperties(this)) {
    //         if (!ConfigManager.ObjectEquals(fieldsAndProperty.GetValue(this), fieldsAndProperty.GetValue(pendingConfig))) {
    //             return true;
    //         }
    //     }
    //     return false;
    // }

    [JsonIgnore, ShowDespiteJsonIgnore, NullAllowed] public object? DisableAll {
        get => null;
        set {
            // TODO disable all
        }
    }

    [JsonIgnore, ShowDespiteJsonIgnore] public UnloadedCrafting UnloadedCrafting {get; set;} = new();
    [JsonIgnore, ShowDespiteJsonIgnore] public UnloadedInventoryManagement UnloadedInventoryManagement {get; set;} = new();
    [JsonIgnore, ShowDespiteJsonIgnore] public UnloadedItemSearch UnloadedItemSearch {get; set;} = new();

    public static bool CompatibilityMode => Instance.compatibilityMode;
    public static Compatibility Instance = null!;
    
    public override ConfigScope Mode => ConfigScope.ClientSide;
}

public sealed class UnloadedCrafting {
    public bool fastScroll = false;
    public bool listScroll = false;
    public bool wrapping = false;
    public bool recipeFiltering = false;
    public bool craftOnList = false;

    public static UnloadedCrafting Value => Compatibility.Instance.UnloadedCrafting;
}

public sealed class UnloadedInventoryManagement {
    public bool autoEquip = false;
    public bool favoriteInBanks = false;
    public bool shiftRight = false;
    public bool stackTrash = false;
    public bool materials = false;
    public bool baits = false;
    public bool smartPickup = false;
    public bool marks = false;
    public bool quickMoveHotkeys = false;
    public bool quickMoveHightlight = false;
    public bool craftStack = false;

    [JsonIgnore] public bool ClickOverrides { set { craftStack = value; shiftRight = value;} }
    
    public static UnloadedInventoryManagement Value => Compatibility.Instance.UnloadedInventoryManagement;
}

public sealed class UnloadedItemSearch {
    public bool searchRecipes = false;
    public bool searchDrops = false;
    public bool guideAnyItem = false;
    public bool guideFavorite = false;
    public bool guideUnfavoriteOnCraft = false;
    public bool guideCraftInMenu = false;
    public bool guideProgression = false;
    public bool bestiaryProgression = false;
    public bool bestiaryDisplayedUnlock = false;

    [JsonIgnore] public bool BestiaryUnlock { set { bestiaryProgression = value; bestiaryDisplayedUnlock = value; } }
    [JsonIgnore] public bool GuideAvailablesRecipes { set { guideFavorite = value; guideCraftInMenu = value; guideProgression = value; } }
    
    public static UnloadedItemSearch Value => Compatibility.Instance.UnloadedItemSearch;
}