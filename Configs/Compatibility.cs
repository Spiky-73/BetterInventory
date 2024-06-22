using System.ComponentModel;
using BetterInventory.Default.Catalogues;
using Newtonsoft.Json;
using SpikysLib.Configs.UI;
using SpikysLib.Extensions;
using Terraria.ModLoader.Config;

namespace BetterInventory.Configs;

public sealed class Compatibility : ModConfig {

    [DefaultValue(0), JsonProperty] internal int failedILs = 0;


    [ReloadRequired, DefaultValue(false)] public bool compatibilityMode;

    [JsonIgnore, ShowDespiteJsonIgnore, NullAllowed] public object? DisableAll {
        get => null;
        set {
            FixedUI.Value.fastScroll.Parent = false;
            FixedUI.Value.listScroll = false;
            FixedUI.Value.wrapping = false;
            Crafting.Instance.recipeFilters.Parent = false;
            Crafting.Instance.craftOnList.Parent = false;
            Crafting.Instance.Save();

            SmartConsumption.Value.materials = false;
            SmartConsumption.Value.baits = false;
            SmartPickup.Value.previousSlot.Parent = ItemPickupLevel.None;
            SmartPickup.Value.autoEquip = AutoEquipLevel.None;
            SmartPickup.Value.upgradeItems.Parent = false;
            SmartPickup.Value.hotbarLast = false;
            SmartPickup.Value.fixSlot = false;
            PreviousDisplay.Value.fakeItem.Parent = false;
            PreviousDisplay.Value.icon.Parent = false;
            QuickMove.Value.displayedHotkeys.Parent = HotkeyDisplayMode.None;
            QuickMove.Value.displayedHotkeys.Value.highlightIntensity = 0;
            InventoryManagement.Instance.favoriteInBanks = false;
            InventoryManagement.Instance.shiftRight = false;
            InventoryManagement.Instance.stackTrash = false;
            InventoryManagement.Instance.craftStack.Parent = false;
            InventoryManagement.Instance.Save();

            BetterGuide.Value.moreRecipes = false;
            BetterGuide.Value.craftingStation = false;
            BetterGuide.Value.favoritedRecipes.Parent = false;
            BetterGuide.Value.craftInMenu = false;
            BetterGuide.Value.unknownDisplay = UnknownDisplay.Vanilla;
            FavoritedRecipes.Value.unfavoriteOnCraft = UnfavoriteOnCraft.None;
            BetterBestiary.Value.displayedInfo = UnlockLevel.Vanilla;
            BetterBestiary.Value.unknownDisplay = UnknownDisplay.Vanilla;
            QuickSearch.Value.catalogues[new(RecipeList.Instance)] = false;
            ItemSearch.Instance.Save();
        }
    }

    [JsonIgnore, ShowDespiteJsonIgnore, CustomModConfigItem(typeof(HideDefaultElement))] public UnloadedCrafting unloadedCrafting {get; set;} = new();
    [JsonIgnore, ShowDespiteJsonIgnore, CustomModConfigItem(typeof(HideDefaultElement))] public UnloadedInventoryManagement unloadedInventoryManagement {get; set;} = new();
    [JsonIgnore, ShowDespiteJsonIgnore, CustomModConfigItem(typeof(HideDefaultElement))] public UnloadedItemActions unloadedItemActions { get; set;} = new();
    [JsonIgnore, ShowDespiteJsonIgnore, CustomModConfigItem(typeof(HideDefaultElement))] public UnloadedItemSearch unloadedItemSearch {get; set;} = new();

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
    public bool stackTrash = false;
    public bool craftStack = false;
    
    public static UnloadedInventoryManagement Value => Compatibility.Instance.unloadedInventoryManagement;
}

public sealed class UnloadedItemActions {
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
    [JsonIgnore] public bool GuideAvailableRecipes { set { guideFavorited = value; guideCraftInMenu = value; guideUnknown = value; } }
    
    public static UnloadedItemSearch Value => Compatibility.Instance.unloadedItemSearch;
}