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
    public Toggle<GrabBagTooltip> grabBagTooltip = new(true);
    public Toggle<TooltipScroll> tooltipScroll = new(true);
    public bool fixedTooltipPosition;
    public Toggle<TooltipHover> tooltipHover = new(true);
    public Toggle<ItemAmmo> itemAmmo = new(true);
    [DefaultValue(true)] public bool quickStack;

    public static bool FastContainerOpening => Instance.fastContainerOpening;
    public static bool FastExtractinator => Instance.fastExtractinator;
    public static bool FavoritedBuff => Instance.favoritedBuff;
    public static bool BuilderAccs => Instance.builderAccs;
    public static bool KeepSwappedFavorited => Instance.keepSwappedFavorited;
    public static bool FixedTooltipPosition => Instance.fixedTooltipPosition && !UnloadedItemActions.Value.fixedTooltipPosition;
    public static bool QuickStack => Instance.quickStack;

    public override ConfigScope Mode => ConfigScope.ClientSide;
    public static ItemActions Instance = null!;

    public sealed override void OnChanged() {
        GrabBagTooltipItem.GetGrabBagContent(ItemID.None);
    }
}

public sealed class ItemRightClick {
    [DefaultValue(false)] public bool stackableItems = false;

    public static bool Enabled => ItemActions.Instance.itemRightClick;
    public static ItemRightClick Value => ItemActions.Instance.itemRightClick.Value;
}

public sealed class GrabBagTooltip {
    [DefaultValue(true)] public bool compact = true;
    
    public static bool Enabled => ItemActions.Instance.grabBagTooltip;
    public static GrabBagTooltip Value => ItemActions.Instance.grabBagTooltip.Value;
}

public sealed class TooltipHover {
    [DefaultValue(10)] public int graceTime = 10;

    public static bool Enabled => ItemActions.Instance.tooltipHover && !UnloadedItemActions.Value.tooltipHover;
    public static TooltipHover Value => ItemActions.Instance.tooltipHover.Value;
}

public sealed class ItemAmmo {
    [DefaultValue(true)] public bool tooltip = true;
    public Toggle<ItemSlotAmmo> itemSlot = new(true);

    public static bool Enabled => ItemActions.Instance.itemAmmo;
    public static ItemAmmo Value => ItemActions.Instance.itemAmmo.Value;
    public static bool Tooltip => Enabled && Value.tooltip;
    public static bool ItemSlot => Enabled && Value.itemSlot;
}
public sealed class ItemSlotAmmo {
    [DefaultValue(0.55f)] public float size = 0.55f;
    [DefaultValue(Corner.BottomRight)] public Corner position = Corner.BottomRight;
    [DefaultValue(true)] public bool hover = true;
}

public enum Corner { TopLeft, TopRight, BottomLeft, BottomRight }