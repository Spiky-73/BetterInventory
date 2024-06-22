using System;
using System.Collections.Generic;
using System.ComponentModel;
using BetterInventory.ItemSearch;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SpikysLib.Configs;
using SpikysLib.Configs.UI;
using SpikysLib.Extensions;
using Terraria;
using Terraria.ModLoader.Config;

namespace BetterInventory.Configs;

public sealed class ItemSearch : ModConfig {  
    public Toggle<BetterGuide> betterGuide = new(true);
    public Toggle<BetterBestiary> betterBestiary = new(true);
    public Toggle<QuickSearch> quickSearch = new(true);

    // Compatibility version < v0.6
    [JsonProperty] private Toggle<QuickList>? quickList { set => ModConfigExtensions.MoveMember(value is not null, _ => {
        quickSearch.Value.sharedKeybind.Parent = value!.Parent ? SearchAction.None : SearchAction.Toggle;
        quickSearch.Value.sharedKeybind.Value.tap = value.Value.tap;
        quickSearch.Value.sharedKeybind.Value.delay = value.Value.delay;
    }); }
    [JsonProperty] private Toggle<SearchItems>? searchItems { set => ModConfigExtensions.MoveMember(value is not null, _ => {
        quickSearch.Value.individualKeybinds.Parent = value!.Parent ? SearchAction.None : SearchAction.Both;
        quickSearch.Value.catalogues[new(Mod.Name, nameof(Default.Catalogues.RecipeList))] = value.Value.recipes;
        quickSearch.Value.catalogues[new(Mod.Name, nameof(Default.Catalogues.Bestiary))] = value.Value.drops;
        quickSearch.Value.rightClick = value.Value.rightClick;
    }); }

    public static ItemSearch Instance = null!;
    
    public override ConfigScope Mode => ConfigScope.ClientSide;

    public override void OnChanged() {
        if (!Main.gameMenu) Default.Catalogues.RecipeList.UpdateGuide();
    }
}

public enum UnknownDisplay { Vanilla, Hidden, Unknown, Known }

public sealed class BetterGuide {
    public Toggle<FavoritedRecipes> favoritedRecipes = new(true);
    [DefaultValue(true)] public bool craftInMenu = true;
    [DefaultValue(true)] public bool moreRecipes = true;
    [DefaultValue(true)] public bool craftingStation = true;
    [DefaultValue(true)] public bool conditionsDisplay = true;
    [DefaultValue(Configs.UnknownDisplay.Unknown)] public UnknownDisplay unknownDisplay = Configs.UnknownDisplay.Unknown;

    public static bool Enabled => ItemSearch.Instance.betterGuide;
    public static bool MoreRecipes => Enabled && Value.moreRecipes && !UnloadedItemSearch.Value.guideMoreRecipes;
    public static bool ConditionsDisplay => Enabled && Value.conditionsDisplay;
    public static bool FavoritedRecipes => Enabled && Value.favoritedRecipes && !UnloadedItemSearch.Value.guideFavorited;
    public static bool CraftInMenu => Enabled && Value.craftInMenu && !UnloadedItemSearch.Value.guideCraftInMenu;
    public static bool CraftingStation => Enabled && Value.craftingStation && !UnloadedItemSearch.Value.guideCraftingStation;
    public static bool UnknownDisplay => Enabled && Value.unknownDisplay > Configs.UnknownDisplay.Vanilla && !UnloadedItemSearch.Value.guideUnknown;
    public static bool AvailableRecipes => FavoritedRecipes || CraftInMenu || UnknownDisplay;
    public static BetterGuide Value => ItemSearch.Instance.betterGuide.Value;
    
    // Compatibility version < v0.6
    [JsonProperty] private Toggle<FavoritedRecipes>? favoriteRecipes { set => ModConfigExtensions.MoveMember(value is not null, _ => favoritedRecipes = value!); }
    [JsonProperty, DefaultValue(true)] private bool tile { set => ModConfigExtensions.MoveMember(value, _ => craftingStation = value); }
    [JsonProperty, DefaultValue(true)] private bool craftText { set => ModConfigExtensions.MoveMember(value, _ => conditionsDisplay = value); }
}

public sealed class FavoritedRecipes {
    [DefaultValue(Configs.UnfavoriteOnCraft.Favorited)] public UnfavoriteOnCraft unfavoriteOnCraft = Configs.UnfavoriteOnCraft.Favorited;

    public static bool UnfavoriteOnCraft => BetterGuide.FavoritedRecipes && Value.unfavoriteOnCraft != Configs.UnfavoriteOnCraft.None && !UnloadedItemSearch.Value.guideUnfavoriteOnCraft;
    public static FavoritedRecipes Value => BetterGuide.Value.favoritedRecipes.Value;
}
[Flags] public enum UnfavoriteOnCraft { None = 0b00, Favorited = 0b01, Blacklisted = 0b10, Both = Favorited | Blacklisted }

public sealed class BetterBestiary {
    [DefaultValue(UnlockLevel.Drops)] public UnlockLevel displayedInfo = UnlockLevel.Drops;
    [DefaultValue(true)] public bool showBagContent = true;
    [DefaultValue(true)] public bool unlockFilter = true;
    [DefaultValue(Configs.UnknownDisplay.Unknown)] public UnknownDisplay unknownDisplay = Configs.UnknownDisplay.Unknown;

    public static bool Enabled => ItemSearch.Instance.betterBestiary;
    public static bool DisplayedInfo => Enabled && Value.displayedInfo != UnlockLevel.Vanilla && !UnloadedItemSearch.Value.bestiaryDisplayedInfo;
    public static bool ShowBagContent => Enabled && Value.showBagContent;
    public static bool UnlockFilter => Enabled && Value.unlockFilter;
    public static bool UnknownDisplay => Enabled && Value.unknownDisplay > Configs.UnknownDisplay.Vanilla && !UnloadedItemSearch.Value.bestiaryUnknown;
    public static bool Unlock => UnknownDisplay || DisplayedInfo;
    public static BetterBestiary Value => ItemSearch.Instance.betterBestiary.Value;

    // Compatibility version < v0.6
    [JsonProperty, DefaultValue(UnlockLevel.Drops)] private UnlockLevel displayedUnlock { set => ModConfigExtensions.MoveMember(value != UnlockLevel.Drops, _ => displayedInfo = value); }
}
public enum UnlockLevel { Vanilla, Name, Stats, Drops, DropRates }


public sealed class QuickSearch {
    public NestedValue<SearchAction, IndividualKeybinds> individualKeybinds = new(SearchAction.Both);
    public NestedValue<SearchAction, SharedKeybind> sharedKeybind = new(SearchAction.Toggle);
    [CustomModConfigItem(typeof(DictionaryValuesElement))] public Dictionary<EntityCatalogueDefinition, bool> catalogues {
        get => _catalogues;
        set {
            foreach (ModEntityCatalogue catalogue in global::BetterInventory.ItemSearch.QuickSearch.EntityCatalogue) value.TryAdd(new(catalogue), true);
            _catalogues = value;
        }

    }
    [DefaultValue(RightClickAction.SearchPrevious)] public RightClickAction rightClick { get; set; } = RightClickAction.SearchPrevious;

    private Dictionary<EntityCatalogueDefinition, bool> _catalogues = [];

    public static bool Enabled => ItemSearch.Instance.quickSearch;
    public static bool IndividualKeybinds => Enabled && Value.individualKeybinds > SearchAction.None;
    public static bool SharedKeybind => Enabled && Value.sharedKeybind > SearchAction.None;
    public static bool RightClick => Enabled && Value.rightClick != RightClickAction.None;
    public static QuickSearch Value => ItemSearch.Instance.quickSearch.Value;
}
[Flags] public enum SearchAction { None, Search, Toggle, Both }
public enum RightClickAction { None, Clear, SearchPrevious }

public sealed class IndividualKeybinds {
    [DefaultValue(true)] public bool composite = true;

    public static IndividualKeybinds Value => QuickSearch.Value.individualKeybinds.Value;
}
public sealed class SharedKeybind {
    [DefaultValue(10)] public int tap = 10;
    [DefaultValue(10)] public int delay = 10;

    public static SharedKeybind Value => QuickSearch.Value.sharedKeybind.Value;
}

// Compatibility version < v0.6
class QuickList {
    [DefaultValue(10)] public int tap = 10;
    [DefaultValue(10)] public int delay = 10;
}
class SearchItems {
    [DefaultValue(true)] public bool recipes = true;
    [DefaultValue(true)] public bool drops = true;
    [DefaultValue(true)] public RightClickAction rightClick = RightClickAction.SearchPrevious;
}
