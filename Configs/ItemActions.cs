using System.ComponentModel;
using SpikysLib.Configs;
using Terraria.ModLoader.Config;

namespace BetterInventory.Configs;

public sealed class ItemActions : ModConfig {
    [DefaultValue(true)] public bool fastContainerOpening;
    [DefaultValue(true)] public bool fastExtractinator;
    public Toggle<ItemRightClick> itemRightClick = new(true);
    [DefaultValue(true)] public bool favoritedBuff;
    [DefaultValue(true)] public bool builderAccs;
    [DefaultValue(true)] public bool keepSwappedFavorited;

    public static bool FastContainerOpening => Instance.fastContainerOpening;
    public static bool FastExtractinator => Instance.fastExtractinator;
    public static bool FavoritedBuff => Instance.favoritedBuff;
    public static bool BuilderAccs => Instance.builderAccs;
    public static bool KeepSwappedFavorited => Instance.keepSwappedFavorited;

    public override ConfigScope Mode => ConfigScope.ClientSide;
    public static ItemActions Instance = null!;

}

public sealed class ItemRightClick {
    [DefaultValue(false)] public bool stackableItems = false;

    public static bool Enabled => ItemActions.Instance.itemRightClick;
    public static ItemRightClick Value => ItemActions.Instance.itemRightClick.Value;
}