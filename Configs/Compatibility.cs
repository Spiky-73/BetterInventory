using System.ComponentModel;
using BetterInventory.Configs.UI;
using Newtonsoft.Json;
using Terraria.ModLoader.Config;

namespace BetterInventory.Configs;

public sealed class Compatibility : ModConfig {

    [DefaultValue(0), JsonProperty] internal int failedILs = 0;


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
            FixedUI.Value.fastScroll.Parent = false;
            FixedUI.Value.listScroll = false;
            FixedUI.Value.wrapping = false;
            Crafting.Instance.recipeFilters.Parent = false;
            Crafting.Instance.craftOnList.Parent = false;
            Crafting.Instance.SaveConfig();

            SmartConsumption.Value.materials = false;
            SmartConsumption.Value.baits = false;
            SmartPickup.Value.markIntensity = 0;
            QuickMove.Value.displayHotkeys.Parent = HotkeyDisplayMode.Off;
            QuickMove.Value.displayHotkeys.Value.highlightIntensity = 0;
            InventoryManagement.Instance.autoEquip = AutoEquipLevel.Off;
            InventoryManagement.Instance.favoriteInBanks = false;
            InventoryManagement.Instance.shiftRight = false;
            InventoryManagement.Instance.stackTrash = false;
            InventoryManagement.Instance.craftStack.Parent = false;
            InventoryManagement.Instance.smartPickup.Parent = SmartPickupLevel.Off;
            InventoryManagement.Instance.SaveConfig();

            BetterGuide.Value.moreRecipes = false;
            BetterGuide.Value.craftInMenu = false;
            BetterGuide.Value.unknownDisplay = UnknownDisplay.Off;
            FavoriteRecipes.Value.unfavoriteOnCraft = UnfavoriteOnCraft.Off;
            BetterBestiary.Value.unknownDisplay = UnknownDisplay.Off;
            BetterBestiary.Value.displayedUnlock = UnlockLevel.Off;
            SearchItems.Value.recipes = false;
            ItemSearch.Instance.SaveConfig();
        }
    }

    [JsonIgnore, ShowDespiteJsonIgnore, CustomModConfigItem(typeof(ObjectAsTextElement))] public UnloadedCrafting UnloadedCrafting {get; set;} = new();
    [JsonIgnore, ShowDespiteJsonIgnore, CustomModConfigItem(typeof(ObjectAsTextElement))] public UnloadedInventoryManagement UnloadedInventoryManagement {get; set;} = new();
    [JsonIgnore, ShowDespiteJsonIgnore, CustomModConfigItem(typeof(ObjectAsTextElement))] public UnloadedItemSearch UnloadedItemSearch {get; set;} = new();

    public static bool CompatibilityMode => Instance.compatibilityMode;
    public static Compatibility Instance = null!;
    
    public override ConfigScope Mode => ConfigScope.ClientSide;
}

public sealed class UnloadedCrafting {
    public bool fastScroll = false;
    public bool listScroll = false;
    public bool wrapping = false;
    public bool recipeFilters = false;
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
    public bool quickMoveHighlight = false;
    public bool craftStack = false;
    
    public static UnloadedInventoryManagement Value => Compatibility.Instance.UnloadedInventoryManagement;
}

public sealed class UnloadedItemSearch {
    public bool searchRecipes = false;
    public bool guideMoreRecipes = false;
    public bool guideFavorite = false;
    public bool guideUnfavoriteOnCraft = false;
    public bool guideCraftInMenu = false;
    public bool guideUnknown = false;
    public bool bestiaryUnknown = false;
    public bool bestiaryDisplayedUnlock = false;

    [JsonIgnore] public bool BestiaryUnlock { set { bestiaryUnknown = value; bestiaryDisplayedUnlock = value; } }
    [JsonIgnore] public bool GuideAvailableRecipes { set { guideFavorite = value; guideCraftInMenu = value; guideUnknown = value; } }
    
    public static UnloadedItemSearch Value => Compatibility.Instance.UnloadedItemSearch;
}