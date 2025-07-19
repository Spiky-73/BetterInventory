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

    [JsonIgnore, ShowDespiteJsonIgnore, NullAllowed] public Empty? DisableAll { get => null; set => DisableILSettings(); }


    [JsonIgnore, ShowDespiteJsonIgnore, CustomModConfigItem(typeof(HideDefaultElement))] public UnloadedCrafting unloadedCrafting {get; set;} = new();
    [JsonIgnore, ShowDespiteJsonIgnore, CustomModConfigItem(typeof(HideDefaultElement))] public UnloadedInventoryManagement unloadedInventoryManagement {get; set;} = new();
    [JsonIgnore, ShowDespiteJsonIgnore, CustomModConfigItem(typeof(HideDefaultElement))] public UnloadedItemActions unloadedItemActions { get; set;} = new();
    [JsonIgnore, ShowDespiteJsonIgnore, CustomModConfigItem(typeof(HideDefaultElement))] public UnloadedItemSearch unloadedItemSearch {get; set;} = new();
    
    [DefaultValue(0), JsonProperty] internal int failedILs = 0;

    public static bool CompatibilityMode => Instance.compatibilityMode;
    public static Compatibility Instance = null!;

    private static void DisableILSettings() {
        FixedUI.Value.fastScroll.Key = false;
        FixedUI.Value.scrollButtons = false;
        FixedUI.Value.wrapping = false;
        FixedUI.Value.recipeCount = false;
        FixedUI.Value.noRecStartOffset = false;
        FixedUI.Value.noRecListClose = false;
        FixedUI.Value.focusButton = false;
        Crafting.Instance.recipeFilters.Key = false;
        Crafting.Instance.recipeSearchBar.Key = false;
        Crafting.Instance.recipeSort = false;
        Crafting.Instance.craftOnList.Key = false;
        AvailableMaterials.Value.itemSlot = false;
        Crafting.Instance.Save();

        SmartConsumption.Value.materials = false;
        SmartConsumption.Value.baits = false;
        SmartPickup.Value.previousSlot.Key = ItemPickupLevel.None;
        SmartPickup.Value.autoEquip.Key = AutoEquipLevel.None;
        SmartPickup.Value.upgradeItems.Key = false;
        SmartPickup.Value.hotbarLast = false;
        SmartPickup.Value.fixSlot = false;
        PreviousDisplay.Value.fakeItem.Key = false;
        PreviousDisplay.Value.icon.Key = false;
        QuickMove.Value.displayedHotkeys.Key = HotkeyDisplayMode.None;
        QuickMove.Value.displayedHotkeys.Value.highlightIntensity = 0;
        InventoryManagement.Instance.betterShiftClick.Value.shiftRight = false;
        InventoryManagement.Instance.betterShiftClick.Value.universalShift = false;
        InventoryManagement.Instance.favoriteInBanks = false;
        InventoryManagement.Instance.stackTrash = false;
        InventoryManagement.Instance.craftStack.Key = false;
        InventoryManagement.Instance.Save();

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
    }

    public override ConfigScope Mode => ConfigScope.ClientSide;
}

public sealed class UnloadedCrafting {
    public bool fastScroll;
    public bool scrollButtons;
    public bool wrapping;
    public bool recipeCount;
    public bool focusButton;
    public bool noRecStartOffset;
    public bool noRecListClose;
    public bool recipeFilters;
    public bool recipeSearchBar;
    public bool recipeSort;
    public bool craftOnList;
    public bool availableMaterialsItemSlot;

    [JsonIgnore] public bool RecipeListUI { set { recipeCount = focusButton = value; } }
    [JsonIgnore] public bool RecipeUI { set { recipeFilters = recipeSearchBar = recipeSort = value; } }

    public static UnloadedCrafting Value => Compatibility.Instance.unloadedCrafting;
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
    public bool quickMoveHotkeys;
    public bool quickMoveHighlight;
    public bool favoriteInBanks;
    public bool shiftRight;
    public bool universalShift;
    public bool stackTrash;
    public bool craftStack;
    
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