using System.ComponentModel;
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

    [JsonIgnore, ShowDespiteJsonIgnore]
    public string SearchItemKeybind {
        get {
            var keys = SearchItem.Keybind?.GetAssignedKeys() ?? new();
            return keys.Count == 0 ? Lang.inter[23].Value : keys[0];
        }
    }    
    public override ConfigScope Mode => ConfigScope.ClientSide;
    public static ItemSearch Instance = null!;

    public override void OnChanged() => SearchItem.UpdateGuide();

    public enum UnknownDisplay { Hidden, Unknown, Known }
}

public class BetterGuide {
    [DefaultValue(true)] public bool anyItem = true;
    [DefaultValue(true)] public bool favoriteRecipes = true;
    [DefaultValue(true)] public bool craftInMenu = true;
    [DefaultValue(true)] public bool guideTile = true;
}

public class BetterBestiary {
    [DefaultValue(UnlockLevel.Drops)] public UnlockLevel displayedUnlock = UnlockLevel.Drops;
    [DefaultValue(true)] public bool showBagContent = true;
    [DefaultValue(true)] public bool unlockFilter = true;

    public enum UnlockLevel { Unchanged, Stats, Drops, DropRates }
}
