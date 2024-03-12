using System;
using System.ComponentModel;
using BetterInventory.Configs.UI;
using BetterInventory.ItemSearch;
using Newtonsoft.Json;
using Terraria;
using Terraria.ModLoader.Config;

namespace BetterInventory.Configs;

public sealed class ItemSearch : ModConfig {  
    public Toggle<BetterGuide> betterGuide = new(true);
    public Toggle<BetterBestiary> betterBestiary = new(true);
    [DefaultValue(true)] public bool searchRecipes;
    [DefaultValue(true)] public bool searchDrops;
    [DefaultValue(true)] public bool searchHistory;
    [DefaultValue(UnknownDisplay.Unknown)] public UnknownDisplay unknownDisplay;

    [JsonIgnore, ShowDespiteJsonIgnore] public string SearchItemKeybind {
        get {
            var keys = SearchItem.Keybind?.GetAssignedKeys() ?? new();
            return keys.Count == 0 ? Lang.inter[23].Value : keys[0];
        }
    }

    public static bool SearchRecipes => Instance.searchRecipes && !UnloadedItemSearch.Value.searchRecipes;
    public static bool SearchDrops => Instance.searchDrops && !UnloadedItemSearch.Value.searchDrops;
    public static bool SearchItems => SearchRecipes || SearchDrops;
    public static bool SearchHistory => Instance.searchHistory;
    public static ItemSearch Instance = null!;
    
    public override ConfigScope Mode => ConfigScope.ClientSide;

    public override void OnChanged() => SearchItem.UpdateGuide();

}

public enum UnknownDisplay { Hidden, Unknown, Known }

public class BetterGuide {
    [DefaultValue(true)] public bool moreRecipes = true;
    [DefaultValue(true)] public bool craftText = true;
    public Toggle<FavoriteRecipes> favoriteRecipes = new(true);
    [DefaultValue(true)] public bool craftInMenu = true;
    [DefaultValue(true)] public bool tile = true;
    [DefaultValue(true)] public bool progression = true;

    public static bool Enabled => ItemSearch.Instance.betterGuide;
    public static bool MoreRecipes => Enabled && Value.moreRecipes && !UnloadedItemSearch.Value.guideMoreRecipes;
    public static bool CraftText => Enabled && Value.craftText;
    public static bool FavoriteRecipes => Enabled && Value.favoriteRecipes && !UnloadedItemSearch.Value.guideFavorite;
    public static bool CraftInMenu => Enabled && Value.craftInMenu && !UnloadedItemSearch.Value.guideCraftInMenu;
    public static bool Tile => Enabled && Value.tile;
    public static bool Progression => Enabled && Value.progression && !UnloadedItemSearch.Value.guideProgression;
    public static bool AvailablesRecipes => FavoriteRecipes || CraftInMenu || Progression;
    
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
    [DefaultValue(true)] public bool progression = true;

    public static bool Enabled => ItemSearch.Instance.betterBestiary;
    public static bool DisplayedUnlock => Enabled && Value.displayedUnlock != UnlockLevel.Off && !UnloadedItemSearch.Value.bestiaryDisplayedUnlock;
    public static bool ShowBagContent => Enabled && Value.showBagContent;
    public static bool UnlockFilter => Enabled && Value.unlockFilter;
    public static bool Progression => Enabled && Value.progression && !UnloadedItemSearch.Value.bestiaryProgression;
    public static bool Unlock => Progression || DisplayedUnlock;
    public static BetterBestiary Value => ItemSearch.Instance.betterBestiary.Value;
}

public enum UnlockLevel { Off, Name, Stats, Drops, DropRates }
