using System.ComponentModel;
using BetterInventory.Default.Catalogues;
using Newtonsoft.Json;
using SpikysLib.Configs;
using SpikysLib.Configs.UI;
using Terraria.ModLoader.Config;

namespace BetterInventory.Configs;

public sealed class Compatibility : ModConfig {

    [Header("Bug")]
    public Text? bug;

    [Header("Compatibility")]
    [ReloadRequired, DefaultValue(false)] public bool compatibilityMode;

    [JsonIgnore, ShowDespiteJsonIgnore, NullAllowed] public Empty? DisableAll { get => null; set => DisableAllILs(); }


    [JsonIgnore, ShowDespiteJsonIgnore, CustomModConfigItem(typeof(HideDefaultElement))] public UnloadedInventoryManagement unloadedInventoryManagement {get; set;} = new();
    [JsonIgnore, ShowDespiteJsonIgnore, CustomModConfigItem(typeof(HideDefaultElement))] public UnloadedItemActions unloadedItemActions { get; set;} = new();
    [JsonIgnore, ShowDespiteJsonIgnore, CustomModConfigItem(typeof(HideDefaultElement))] public UnloadedItemSearch unloadedItemSearch {get; set;} = new();
    
    [JsonIgnore, ShowDespiteJsonIgnore, CustomModConfigItem(typeof(HideDefaultElement))] public UnloadedImprovements unloadedImprovements {get; set;} = new();
    [JsonIgnore, ShowDespiteJsonIgnore, CustomModConfigItem(typeof(HideDefaultElement))] public UnloadedVisualChanges unloadedVisualChanges {get; set;} = new();
    [JsonIgnore, ShowDespiteJsonIgnore, CustomModConfigItem(typeof(HideDefaultElement))] public UnloadedVanillaFixes unloadedVanillaFixes {get; set;} = new();
    [JsonIgnore, ShowDespiteJsonIgnore, CustomModConfigItem(typeof(HideDefaultElement))] public UnloadedFeatures unloadedFeatures {get; set;} = new();
    
    [DefaultValue(0), JsonProperty] internal int failedILs = 0;

    public static bool CompatibilityMode => Instance.compatibilityMode;
    public static Compatibility Instance = null!;

    private static void DisableAllILs() {
        SmartConsumption.Value.materials = false;
        SmartConsumption.Value.baits = false;
        SmartPickup.Value.refillMouse = false;
        SmartPickup.Value.previousSlot.Key = ItemPickupLevel.None;
        SmartPickup.Value.quickStack.Key = false;
        SmartPickup.Value.autoEquip.Key = AutoEquipLevel.None;
        SmartPickup.Value.upgradeItems.Key = false;
        SmartPickup.Value.voidBagFirst = false;
        SmartPickup.Value.hotbarLast = false;
        SmartPickup.Value.fixSlot = false;
        PreviousDisplay.Value.fakeItem.Key = false;
        PreviousDisplay.Value.icon.Key = false;
        InventoryManagement.Instance.betterShiftClick.Value.shiftRight = false;
        InventoryManagement.Instance.betterShiftClick.Value.universalShift = false;
        InventoryManagement.Instance.favoriteInBanks = false;
        BetterTrash.Value.stackTrash = false;
        InventoryManagement.Instance.craftStack.Key = false;
        InventoryManagement.Instance.Save();
        InventoryManagement.Instance.betterQuickStack.Value.completeQuickStack = false;
        InventoryManagement.Instance.betterQuickStack.Value.limitedBanksQuickStack = false;
        InventoryManagement.Instance.inventorySlotsTexture = false;

        ItemActions.Instance.fixedTooltipPosition = false;
        ItemActions.Instance.tooltipHover.Key = false;
        ItemActions.Instance.Save();

        BetterGuide.Value.favoritedRecipes.Key = false;
        BetterGuide.Value.craftInMenu = false;
        BetterGuide.Value.moreRecipes = false;
        BetterGuide.Value.craftingStation = false;
        BetterGuide.Value.conditionsDisplay = false;
        BetterGuide.Value.unknownDisplay = UnknownDisplay.Vanilla;
        BetterBestiary.Value.displayedInfo = UnlockLevel.Vanilla;
        BetterBestiary.Value.unknownDisplay = UnknownDisplay.Vanilla;
        QuickSearch.Value.catalogues[new(RecipeList.Instance)] = false;
        ItemSearch.Instance.Save();

        UnloadedImprovements.DisableAllILs();
        UnloadedVisualChanges.DisableAllILs();
        UnloadedVanillaFixes.DisableAllILs();
        UnloadedFeatures.DisableAllILs();
    }

    public override ConfigScope Mode => ConfigScope.ClientSide;
}

public sealed class UnloadedVanillaFixes {
    public bool ammoPickupOrder;
    public bool consistantScrollDirection_recipesUnpaused;
    public bool consistantScrollDirection_recipesPaused;
    public bool consistantScrollDirection_accessories;
    public bool materialsWrapping;

    public static void DisableAllILs() {
        var config = VanillaFixes.Instance;
        config.ammoPickupOrder = false;
        config.consistantScrollDirection.Value.accessories = false;
        config.consistantScrollDirection.Value.recipesPaused = false;
        config.consistantScrollDirection.Value.recipesUnpaused = false;
        config.materialsWrapping = false;
        config.SaveChanges();
    }

    public static UnloadedVanillaFixes Instance => Compatibility.Instance.unloadedVanillaFixes;
}

public sealed class UnloadedImprovements {
    public bool betterRecipeGrid_craftOnRecipeGrid;
    public bool betterRecipeGrid_refocusButton;
    public bool betterRecipeGrid_noRecStartOffset;
    public bool betterRecipeGrid_noRecListClose;
    public bool betterRecipeGrid_pageScroll;
    public bool betterRecipeList_fastScroll;
    public bool smartConsumption_baits;
    public bool smartConsumption_materials;

    public static void DisableAllILs() {
        var config = Improvements.Instance;
        config.betterRecipeGrid.Value.craftOnRecipeGrid.Key = false;
        config.betterRecipeGrid.Value.refocusButton = false;
        config.betterRecipeGrid.Value.noRecStartOffset = false;
        config.betterRecipeGrid.Value.noRecListClose = false;
        config.betterRecipeGrid.Value.pageScroll = false;
        config.betterRecipeList.Value.fastScroll.Key = false;
        config.smartConsumption.Value.baits = false;
        config.smartConsumption.Value.materials = false;
        config.SaveChanges();
    }

    public static UnloadedImprovements Instance => Compatibility.Instance.unloadedImprovements;
}

public sealed class UnloadedVisualChanges {
    public bool availableMaterialsCount_itemSlot;
    public bool recipeCount;

    public static void DisableAllILs() {
        var config = VisualChanges.Instance;
        config.availableMaterialsCount.Value.itemSlot = false;
        config.recipeCount = false;
        config.SaveChanges();
    }

    public static UnloadedVisualChanges Instance => Compatibility.Instance.unloadedVisualChanges;
}

public sealed class UnloadedFeatures {
    public bool recipeFiltering;
    public bool quickMove_displayHotkeys;

    public static void DisableAllILs() {
        var config = Features.Instance;
        config.recipeFiltering.Key = false;
        config.quickMove.Value.displayedHotkeys = HotkeyDisplayMode.None;
        config.SaveChanges();
    }

    public static UnloadedFeatures Instance => Compatibility.Instance.unloadedFeatures;
}

public sealed class UnloadedInventoryManagement {
    public bool materials;
    public bool baits;
    public bool pickupOverrideSlot;
    public bool pickupDedicatedSlot;
    public bool hotbarLast;
    public bool fixSlot;
    public bool displayFakeItem;
    public bool displayIcon;
    public bool favoriteInBanks;
    public bool shiftRight;
    public bool universalShift;
    public bool stackTrash;
    public bool craftStack;
    public bool quickStackComplete;
    public bool quickStackLimitedBanks;
    public bool inventorySlotsTexture;
    
    public static UnloadedInventoryManagement Value => Compatibility.Instance.unloadedInventoryManagement;
}

public sealed class UnloadedItemActions {
    public bool fixedTooltipPosition;
    public bool tooltipHover;
    public static UnloadedItemActions Value => Compatibility.Instance.unloadedItemActions;
}

public sealed class UnloadedItemSearch {
    public bool guideFavoritedRecipes;
    public bool guideCraftInMenu;
    public bool guideMoreRecipes;
    public bool guideCraftingStation;
    public bool guideRequiredObjectsDisplay;
    public bool guideUnknownDisplay;
    public bool bestiaryDisplayedInfo;
    public bool bestiaryUnknown;
    public bool recipeList;

    [JsonIgnore] public bool GuideAvailableRecipes { set { guideFavoritedRecipes = guideCraftInMenu = value; } }
    [JsonIgnore] public bool GuideRecipeFiltering { set { guideCraftInMenu = guideFavoritedRecipes = guideCraftingStation = guideMoreRecipes = guideUnknownDisplay = value; } }
    [JsonIgnore] public bool BestiaryUnlock { set { bestiaryUnknown = bestiaryDisplayedInfo = value; } }
    
    public static UnloadedItemSearch Value => Compatibility.Instance.unloadedItemSearch;
}