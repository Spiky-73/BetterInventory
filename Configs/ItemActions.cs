using System.ComponentModel;
using BetterInventory.ItemActions;
using SpikysLib.Configs;
using Terraria.ID;
using Terraria.ModLoader.Config;

namespace BetterInventory.Configs;

public sealed class ItemActions : ModConfig {
    [DefaultValue(true)] public bool fastContainerOpening;
    [DefaultValue(true)] public bool fastExtractinator;
    public Toggle<ItemRightClick> itemRightClick = new(true);
    [DefaultValue(true)] public bool favoritedBuff;
    [DefaultValue(true)] public bool builderAccs;
    [DefaultValue(true)] public bool keepSwappedFavorited;
    public Toggle<ShowBagContent> showBagContent = new(true);
    public Toggle<TooltipScroll> tooltipScroll = new(true);

    public static bool FastContainerOpening => Instance.fastContainerOpening;
    public static bool FastExtractinator => Instance.fastExtractinator;
    public static bool FavoritedBuff => Instance.favoritedBuff;
    public static bool BuilderAccs => Instance.builderAccs;
    public static bool KeepSwappedFavorited => Instance.keepSwappedFavorited;
    public static bool ShowBagContent => Instance.showBagContent;

    public override ConfigScope Mode => ConfigScope.ClientSide;
    public static ItemActions Instance = null!;

    public sealed override void OnChanged() {
        BetterPlayer.GetGrabBagContent(ItemID.None);
    }
}

public sealed class ItemRightClick {
    [DefaultValue(false)] public bool stackableItems = false;

    public static bool Enabled => ItemActions.Instance.itemRightClick;
    public static ItemRightClick Value => ItemActions.Instance.itemRightClick.Value;
}

public sealed class ShowBagContent {
    public bool compact;
    
    public static bool Enabled => ItemActions.Instance.showBagContent;
    public static ShowBagContent Value => ItemActions.Instance.showBagContent.Value;
}

public sealed class TooltipScroll {
    [DefaultValue(1)] public float maximumHeight = 1;

    public static bool Enabled => ItemActions.Instance.tooltipScroll;
    public static TooltipScroll Value => ItemActions.Instance.tooltipScroll.Value;
}
