using System.ComponentModel;
using SpikysLib.Configs;
using Terraria.ModLoader.Config;

namespace BetterInventory.BetterItemInformationDisplay;

using BIDUnloadableAttribute = UnloadableAttribute<UnloadedBetterItemInformationDisplayConfig>;

// TODO review the usage to split / rename
public sealed class BetterItemInformationDisplayConfig : ModConfig {
    public Toggle<ItemAmmoConfig> itemAmmo = new(true);
    [BIDUnloadable(nameof(inventorySlotsTexture)), DefaultValue(true)] public bool inventorySlotsTexture = true;
    public Toggle<GrabBagContentConfig> grabBagContent = new(true);

    public static BetterItemInformationDisplayConfig Instance = null!;
    public static bool ItemAmmo => BetterInventoryConfig.BetterItemInformationDisplay && Instance.itemAmmo;
    public static bool InventorySlotsTexture => BetterInventoryConfig.BetterItemInformationDisplay && Instance.inventorySlotsTexture && !UnloadedBetterItemInformationDisplayConfig.Instance.inventorySlotsTexture;
    public static bool GrabBagContent => BetterInventoryConfig.BetterItemInformationDisplay && Instance.grabBagContent;

    public override ConfigScope Mode => ConfigScope.ClientSide;
}

// TODO Refactor to remove level 4 ?
public sealed class ItemAmmoConfig {
    [DefaultValue(true)] public bool tooltip = true;
    public Toggle<ItemSlotAmmoConfig> itemSlot = new(true);

    public static ItemAmmoConfig Instance => BetterItemInformationDisplayConfig.Instance.itemAmmo.Value;
    public static bool Tooltip => BetterItemInformationDisplayConfig.ItemAmmo && Instance.tooltip;
    public static bool ItemSlot => BetterItemInformationDisplayConfig.ItemAmmo && Instance.itemSlot;
}

public sealed class ItemSlotAmmoConfig {
    [DefaultValue(0.55f)] public float size = 0.55f;
    [DefaultValue(Corner.BottomRight)] public Corner position = Corner.BottomRight;
    [DefaultValue(true)] public bool hover = true;

    public static ItemSlotAmmoConfig Instance => ItemAmmoConfig.Instance.itemSlot.Value;
}

public enum Corner { TopLeft, TopRight, BottomLeft, BottomRight }

public sealed class GrabBagContentConfig {
    [DefaultValue(true)] public bool tooltip = true;
    [DefaultValue(true)] public bool compact = true;

    public static GrabBagContentConfig Instance => BetterItemInformationDisplayConfig.Instance.grabBagContent.Value;
}

public sealed class UnloadedBetterItemInformationDisplayConfig {
    public bool inventorySlotsTexture;

    public static UnloadedBetterItemInformationDisplayConfig Instance => BetterInventoryConfig.Instance.unloadedBetterItemInformationDisplay;
}
