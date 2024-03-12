using System;
using System.ComponentModel;
using BetterInventory.Configs.UI;
using Terraria.ModLoader.Config;

namespace BetterInventory.Configs;

public sealed class ItemSearch : ModConfig {  
    public Toggle<BetterGuide> betterGuide = new(true);
    public Toggle<BetterBestiary> betterBestiary = new(true);
    public Toggle<QuickList> quickList = new(true);
    public Toggle<SearchItems> searchItems = new(true);

    public static ItemSearch Instance = null!;
    
    public override ConfigScope Mode => ConfigScope.ClientSide;

    public override void OnChanged() => global::BetterInventory.ItemSearch.SearchItem.UpdateGuide();
}

public enum UnknownDisplay { Off, Hidden, Unknown, Known }

public class BetterGuide {
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

public class FavoriteRecipes {
    [DefaultValue(Configs.UnfavoriteOnCraft.Favorited)] public UnfavoriteOnCraft unfavoriteOnCraft = Configs.UnfavoriteOnCraft.Favorited;

    public static bool UnfavoriteOnCraft => BetterGuide.FavoriteRecipes && Value.unfavoriteOnCraft != Configs.UnfavoriteOnCraft.Off && !UnloadedItemSearch.Value.guideUnfavoriteOnCraft;

    public static FavoriteRecipes Value => BetterGuide.Value.favoriteRecipes.Value;
}

[Flags] public enum UnfavoriteOnCraft { Off = 0b00, Favorited = 0b01, Blacklisted = 0b10, Both = Favorited | Blacklisted }

public class BetterBestiary {
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

public class QuickList {
    [DefaultValue(10)] public int tap = 10;
    [DefaultValue(10)] public int delay = 10;

    public static bool Enabled => ItemSearch.Instance.quickList;
    public static QuickList Value => ItemSearch.Instance.quickList.Value;
}

// TODO keybinds instead of hard codded
// TODO composite vs single mode
public class SearchItems {
    [DefaultValue(true)] public bool recipes = true;
    [DefaultValue(true)] public bool drops = true;
    [DefaultValue(true)] public bool history = true; // TODO remove to need for the other settings to be on

    public static bool Enabled => ItemSearch.Instance.searchItems;
    public static bool Recipes => Value.recipes && !UnloadedItemSearch.Value.searchRecipes;
    public static bool Drops => Value.drops;
    public static bool History => Value.history;
    public static SearchItems Value => ItemSearch.Instance.searchItems.Value;
}

public enum UnlockLevel { Off, Name, Stats, Drops, DropRates }
