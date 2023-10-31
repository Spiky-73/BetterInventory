using System.ComponentModel;
using BetterInventory.ItemSearch;
using Newtonsoft.Json;
using Terraria;
using Terraria.ModLoader.Config;

namespace BetterInventory.Configs;

public sealed class ItemSearch : ModConfig {  
    public Toggle<BetterGuide> betterGuide = new(true);
    [DefaultValue(true)] public bool searchRecipes; // TODO impl
    [DefaultValue(true)] public bool searchDrops;
    [DefaultValue(UnknownDisplay.Known)] public UnknownDisplay unknownDisplay;

    [JsonIgnore, ShowDespiteJsonIgnore]
    public string SearchItemKeybind {
        get {
            var keys = SearchItem.Keybind.GetAssignedKeys();
            return keys.Count == 0 ? Lang.inter[23].Value : keys[0];
        }
    }    
    public override ConfigScope Mode => ConfigScope.ClientSide;
    public static ItemSearch Instance = null!;

    public enum UnknownDisplay { Hidden, Unknown, Known }
}

public sealed class BetterGuide {
    [DefaultValue(true)] public bool favoriteRecipes = true;
}


public sealed class SearchKey {
}