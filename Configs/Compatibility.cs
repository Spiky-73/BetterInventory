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
        Crafting.Instance.recipeFilters.Key = false;
        Crafting.Instance.recipeSearchBar.Key = false;
        Crafting.Instance.craftOnList.Key = false;
        AvailableMaterials.Value.itemSlot = false;
        Crafting.Instance.Save();

        SmartConsumption.Value.materials = false;
        SmartConsumption.Value.baits = false;
        SmartPickup.Value.previousSlot.Key = ItemPickupLevel.None;
        SmartPickup.Value.autoEquip = AutoEquipLevel.None;
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

        ItemActions.Instance.fixedtooltip = false;
        ItemActions.Instance.hoverableTooltips = false;
        ItemActions.Instance.Save();

        BetterGuide.Value.moreRecipes = false;
        BetterGuide.Value.craftingStation = false;
        BetterGuide.Value.favoritedRecipes.Key = false;
        BetterGuide.Value.craftInMenu = false;
        BetterGuide.Value.unknownDisplay = UnknownDisplay.Vanilla;
        FavoritedRecipes.Value.unfavoriteOnCraft = UnfavoriteOnCraft.None;
        BetterBestiary.Value.displayedInfo = UnlockLevel.Vanilla;
        BetterBestiary.Value.unknownDisplay = UnknownDisplay.Vanilla;
        QuickSearch.Value.catalogues[new(RecipeList.Instance)] = false;
        ItemSearch.Instance.Save();
    }

    public override ConfigScope Mode => ConfigScope.ClientSide;
}

public sealed class UnloadedCrafting {
    public bool fastScroll = false;
    public bool scrollButtons = false;
    public bool wrapping = false;
    public bool recipeCount = false;
    public bool noRecStartOffset = false;
    public bool noRecListClose = false;
    public bool recipeFilters = false;
    public bool recipeSearchBar = false;
    public bool craftOnList = false;
    public bool availableMaterialsItemSlot;

    [JsonIgnore] public bool RecipeUI { set { recipeFilters = value; recipeSearchBar = value; } }

    public static UnloadedCrafting Value => Compatibility.Instance.unloadedCrafting;
}

public sealed class UnloadedInventoryManagement {
    public bool materials = false;
    public bool baits = false;
    public bool previousSlot = false;
    public bool autoEquip = false;
    public bool upgradeItems = false;
    public bool hotbarLast = false;
    public bool fixSlot = false;
    public bool displayFakeItem = false;
    public bool displayIcon = false;
    public bool quickMoveHotkeys = false;
    public bool quickMoveHighlight = false;
    public bool favoriteInBanks = false;
    public bool shiftRight = false;
    public bool universalShift = false;
    public bool stackTrash = false;
    public bool craftStack = false;
    
    public static UnloadedInventoryManagement Value => Compatibility.Instance.unloadedInventoryManagement;
}

public sealed class UnloadedItemActions {
    public bool fixedTooltip;
    public bool hoverableTooltip;
    public static UnloadedItemActions Value => Compatibility.Instance.unloadedItemActions;
}

public sealed class UnloadedItemSearch {
    public bool guideMoreRecipes = false;
    public bool guideCraftingStation = false;
    public bool guideFavorited = false;
    public bool guideCraftInMenu = false;
    public bool guideUnknown = false;
    public bool guideUnfavoriteOnCraft = false;
    public bool bestiaryDisplayedInfo = false;
    public bool bestiaryUnknown = false;
    public bool recipeList = false;

    [JsonIgnore] public bool BestiaryUnlock { set { bestiaryUnknown = value; bestiaryDisplayedInfo = value; } }
    [JsonIgnore] public bool GuideAvailableRecipes { set { guideFavorited = value; guideCraftInMenu = value; } }
    [JsonIgnore] public bool GuideRecipeOrdering { set { guideUnknown = value; guideFavorited = value; } }
    
    public static UnloadedItemSearch Value => Compatibility.Instance.unloadedItemSearch;
}