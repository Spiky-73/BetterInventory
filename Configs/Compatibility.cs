using System.ComponentModel;
using BetterInventory.Default.SearchProviders;
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
            MarksDisplay.Value.fakeItem.Parent = false;
            MarksDisplay.Value.icon.Parent = false;
            QuickMove.Value.displayHotkeys.Parent = HotkeyDisplayMode.Off;
            QuickMove.Value.displayHotkeys.Value.highlightIntensity = 0;
            InventoryManagement.Instance.autoEquip.Parent = false;
            InventoryManagement.Instance.favoriteInBanks = false;
            InventoryManagement.Instance.shiftRight = false;
            InventoryManagement.Instance.stackTrash = false;
            InventoryManagement.Instance.craftStack.Parent = false;
            InventoryManagement.Instance.smartPickup.Parent = false;
            InventoryManagement.Instance.Save();

            BetterGuide.Value.moreRecipes = false;
            BetterGuide.Value.craftInMenu = false;
            BetterGuide.Value.unknownDisplay = UnknownDisplay.Off;
            FavoriteRecipes.Value.unfavoriteOnCraft = UnfavoriteOnCraft.Off;
            BetterBestiary.Value.unknownDisplay = UnknownDisplay.Off;
            BetterBestiary.Value.displayedUnlock = UnlockLevel.Off;
            QuickSearch.Value.providers[new(RecipeList.Instance)] = false;
            ItemSearch.Instance.Save();
        }
    }

    [JsonIgnore, ShowDespiteJsonIgnore, CustomModConfigItem(typeof(HideDefaultElement))] public UnloadedCrafting UnloadedCrafting {get; set;} = new();
    [JsonIgnore, ShowDespiteJsonIgnore, CustomModConfigItem(typeof(HideDefaultElement))] public UnloadedInventoryManagement UnloadedInventoryManagement {get; set;} = new();
    [JsonIgnore, ShowDespiteJsonIgnore, CustomModConfigItem(typeof(HideDefaultElement))] public UnloadedItemSearch UnloadedItemSearch {get; set;} = new();

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
    public bool fakeItem = false;
    public bool marksIcon = false;
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