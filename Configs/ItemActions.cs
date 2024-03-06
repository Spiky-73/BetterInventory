using System.ComponentModel;
using BetterInventory.Configs.UI;
using BetterInventory.ItemActions;
using Newtonsoft.Json;
using Terraria;
using Terraria.ModLoader.Config;

namespace BetterInventory.Configs;

public sealed class ItemActions : ModConfig {
    [DefaultValue(true)] public bool fastContainerOpening;
    public Toggle<ItemRightClick> itemRightClick = new(true);

    [DefaultValue(true)] public bool builderKeys; // TODO check new api
    [JsonIgnore, ShowDespiteJsonIgnore] public string FavoriteBuffKeybind {
        get {
            var keys = BetterPlayer.FavoritedBuffKb?.GetAssignedKeys() ?? new();
            return keys.Count == 0 ? Lang.inter[23].Value : keys[0];
        }
    }

    public static bool FastContainerOpening => Instance.fastContainerOpening;
    public static bool BuilderKeys => Instance.builderKeys;    

    public override ConfigScope Mode => ConfigScope.ClientSide;
    public static ItemActions Instance = null!;

}

public sealed class ItemRightClick {
    [DefaultValue(false)] public bool stackableItems = false;

    public static bool Enabled => ItemActions.Instance.itemRightClick;
    public static ItemRightClick Value => ItemActions.Instance.itemRightClick.Value;
}