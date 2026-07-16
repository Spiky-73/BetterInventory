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

    [JsonIgnore, ShowDespiteJsonIgnore, CustomModConfigItem(typeof(HideDefaultElement))] public UnloadedInventoryManagement unloadedInventoryManagement { get; set; } = new();
    [JsonIgnore, ShowDespiteJsonIgnore, CustomModConfigItem(typeof(HideDefaultElement))] public UnloadedItemSearch unloadedItemSearch { get; set; } = new();

    [DefaultValue(0), JsonProperty] internal int failedILs = 0;

    public static bool CompatibilityMode => Instance.compatibilityMode;
    public static Compatibility Instance = null!;

    private static void DisableAllILs() {
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
        InventoryManagement.Instance.craftStack.Key = false;
        InventoryManagement.Instance.Save();

        BetterGuide.Value.favoritedRecipes.Key = false;
        BetterGuide.Value.craftInMenu = false;
        BetterGuide.Value.moreRecipes = false;
        BetterGuide.Value.craftingStation = false;
        BetterGuide.Value.conditionsDisplay = false;
        BetterGuide.Value.unknownDisplay = UnknownDisplay.Vanilla;
        QuickSearch.Value.catalogues[new(RecipeList.Instance)] = false;
        ItemSearch.Instance.Save();
    }

    public override ConfigScope Mode => ConfigScope.ClientSide;
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
    public bool shiftRight;
    public bool universalShift;
    public bool craftStack;

    public static UnloadedInventoryManagement Value => Compatibility.Instance.unloadedInventoryManagement;
}

public sealed class UnloadedItemSearch {
    public bool guideFavoritedRecipes;
    public bool guideCraftInMenu;
    public bool guideMoreRecipes;
    public bool guideCraftingStation;
    public bool guideRequiredObjectsDisplay;
    public bool guideUnknownDisplay;
    public bool recipeList;

    [JsonIgnore] public bool GuideAvailableRecipes { set { guideFavoritedRecipes = guideCraftInMenu = value; } }
    [JsonIgnore] public bool GuideRecipeFiltering { set { guideCraftInMenu = guideFavoritedRecipes = guideCraftingStation = guideMoreRecipes = guideUnknownDisplay = value; } }

    public static UnloadedItemSearch Value => Compatibility.Instance.unloadedItemSearch;
}