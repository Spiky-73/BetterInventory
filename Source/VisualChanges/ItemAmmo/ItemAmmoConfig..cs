using System.ComponentModel;
using SpikysLib.Configs;

namespace BetterInventory.VisualChanges.ItemAmmo;

public sealed class ItemAmmoConfig {
    [DefaultValue(true)] public bool tooltip = true;
    public Toggle<ItemSlotAmmoConfig> itemSlot = new(true);

    public static ItemAmmoConfig Instance => VisualChangesConfig.Instance.itemAmmo.Value;
    public static bool Tooltip => VisualChangesConfig.ItemAmmo && Instance.tooltip;
    public static bool ItemSlot => VisualChangesConfig.ItemAmmo && Instance.itemSlot;
}
public sealed class ItemSlotAmmoConfig {
    [DefaultValue(0.55f)] public float size = 0.55f;
    [DefaultValue(Corner.BottomRight)] public Corner position = Corner.BottomRight;
    [DefaultValue(true)] public bool hover = true;
    
    public static ItemSlotAmmoConfig Instance => ItemAmmoConfig.Instance.itemSlot.Value;
}

public enum Corner { TopLeft, TopRight, BottomLeft, BottomRight }