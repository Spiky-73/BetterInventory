using System;
using System.Collections.Generic;
using System.ComponentModel;
using BetterInventory.ItemSearch;
using SpikysLib.Configs;
using SpikysLib.Configs.UI;
using Terraria.ModLoader.Config;

namespace BetterInventory.Configs;

public sealed class ItemSearch : ModConfig {  
    public Toggle<BetterGuide> betterGuide = new(true);
    public Toggle<BetterBestiary> betterBestiary = new(true);
    public Toggle<QuickSearch> quickSearch = new(true);

    public static ItemSearch Instance = null!;
    
    public override ConfigScope Mode => ConfigScope.ClientSide;

    public override void OnChanged() => Default.SearchProviders.RecipeList.UpdateGuide();
}

public enum UnknownDisplay { Off, Hidden, Unknown, Known }

public sealed class BetterGuide {
    public Toggle<FavoriteRecipes> favoriteRecipes = new(true);
    [DefaultValue(true)] public bool craftInMenu = true;
    [DefaultValue(true)] public bool moreRecipes = true;
    [DefaultValue(true)] public bool tile = true;
    [DefaultValue(true)] public bool craftText = true;
    [DefaultValue(Configs.UnknownDisplay.Unknown)] public UnknownDisplay unknownDisplay = Configs.UnknownDisplay.Unknown;

    public static bool Enabled => ItemSearch.Instance.betterGuide;
    public static bool MoreRecipes => Enabled && Value.moreRecipes && !UnloadedItemSearch.Value.guideMoreRecipes;
    public static bool CraftText => Enabled && Value.craftText;
    public static bool FavoriteRecipes => Enabled && Value.favoriteRecipes && !UnloadedItemSearch.Value.guideFavorite;
    public static bool CraftInMenu => Enabled && Value.craftInMenu && !UnloadedItemSearch.Value.guideCraftInMenu;
    public static bool Tile => Enabled && Value.tile;
    public static bool UnknownDisplay => Enabled && Value.unknownDisplay > Configs.UnknownDisplay.Off && !UnloadedItemSearch.Value.guideUnknown;
    public static bool AvailableRecipes => FavoriteRecipes || CraftInMenu || UnknownDisplay;
    public static BetterGuide Value => ItemSearch.Instance.betterGuide.Value;
}

public sealed class FavoriteRecipes {
    [DefaultValue(Configs.UnfavoriteOnCraft.Favorited)] public UnfavoriteOnCraft unfavoriteOnCraft = Configs.UnfavoriteOnCraft.Favorited;

    public static bool UnfavoriteOnCraft => BetterGuide.FavoriteRecipes && Value.unfavoriteOnCraft != Configs.UnfavoriteOnCraft.Off && !UnloadedItemSearch.Value.guideUnfavoriteOnCraft;
    public static FavoriteRecipes Value => BetterGuide.Value.favoriteRecipes.Value;
}
[Flags] public enum UnfavoriteOnCraft { Off = 0b00, Favorited = 0b01, Blacklisted = 0b10, Both = Favorited | Blacklisted }

public sealed class BetterBestiary {
    [DefaultValue(UnlockLevel.Drops)] public UnlockLevel displayedUnlock = UnlockLevel.Drops;
    [DefaultValue(true)] public bool showBagContent = true;
    [DefaultValue(true)] public bool unlockFilter = true;
    [DefaultValue(Configs.UnknownDisplay.Unknown)] public UnknownDisplay unknownDisplay = Configs.UnknownDisplay.Unknown;

    public static bool Enabled => ItemSearch.Instance.betterBestiary;
    public static bool DisplayedUnlock => Enabled && Value.displayedUnlock != UnlockLevel.Off && !UnloadedItemSearch.Value.bestiaryDisplayedUnlock;
    public static bool ShowBagContent => Enabled && Value.showBagContent;
    public static bool UnlockFilter => Enabled && Value.unlockFilter;
    public static bool UnknownDisplay => Enabled && Value.unknownDisplay > Configs.UnknownDisplay.Off && !UnloadedItemSearch.Value.bestiaryUnknown;
    public static bool Unlock => UnknownDisplay || DisplayedUnlock;
    public static BetterBestiary Value => ItemSearch.Instance.betterBestiary.Value;
}
public enum UnlockLevel { Off, Name, Stats, Drops, DropRates }


public sealed class QuickSearch {
    public NestedValue<SearchAction, IndividualKeybinds> individualKeybinds = new(SearchAction.Both);
    public NestedValue<SearchAction, SharedKeybind> sharedKeybind = new(SearchAction.Toggle);
    [CustomModConfigItem(typeof(DictionaryValuesElement))] public Dictionary<SearchProviderDefinition, bool> providers {
        get => _providers;
        set {
            foreach (SearchProvider provider in global::BetterInventory.ItemSearch.QuickSearch.Providers) value.TryAdd(new(provider), true);
            _providers = value;
        }

    }
    [DefaultValue(RightClickAction.SearchPrevious)] public RightClickAction rightClick { get; set; } = RightClickAction.SearchPrevious;
    // [DefaultValue(false)] public bool air = false;

    private Dictionary<SearchProviderDefinition, bool> _providers = [];

    public static bool Enabled => ItemSearch.Instance.quickSearch;
    public static bool IndividualKeybinds => Enabled && Value.individualKeybinds > SearchAction.Off;
    public static bool SharedKeybind => Enabled && Value.sharedKeybind > SearchAction.Off;
    public static bool RightClick => Enabled && Value.rightClick != RightClickAction.Off;
    public static QuickSearch Value => ItemSearch.Instance.quickSearch.Value;
}
[Flags] public enum SearchAction { Off, Search, Toggle, Both }
public enum RightClickAction { Off, Clear, SearchPrevious }

public sealed class IndividualKeybinds {
    [DefaultValue(true)] public bool composite = true;

    public static IndividualKeybinds Value => QuickSearch.Value.individualKeybinds.Value;
}
public sealed class SharedKeybind {
    [DefaultValue(10)] public int tap = 10;
    [DefaultValue(10)] public int delay = 10;

    public static SharedKeybind Value => QuickSearch.Value.sharedKeybind.Value;
}