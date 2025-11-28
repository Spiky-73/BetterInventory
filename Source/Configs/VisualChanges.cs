using System.ComponentModel;
using SpikysLib.Configs;
using Terraria.ModLoader.Config;

namespace BetterInventory.Configs;

public sealed class VisualChanges : ModConfig {
    public Toggle<AvailableMaterialsCount> availableMaterialsCount = new(true);
    [DefaultValue(true)] public bool recipeCount;
    public Toggle<RecipeTooltip> recipeTooltip = new(true);
    public Toggle<ItemAmmo> itemAmmo = new(true);

    public static VisualChanges Instance = null!;
    public static bool AvailableMaterialsCount => Instance.availableMaterialsCount;
    public static bool RecipeCount => Instance.recipeCount && !UnloadedVisualChanges.Instance.recipeCount;
    public static bool RecipeTooltip => Instance.recipeTooltip;
    public static bool ItemAmmo => Instance.itemAmmo;


    public override ConfigScope Mode => ConfigScope.ClientSide;
}

public sealed class AvailableMaterialsCount {
    [DefaultValue(true)] public bool tooltip = true;
    [DefaultValue(true)] public bool itemSlot = true;

    public static AvailableMaterialsCount Instance => VisualChanges.Instance.availableMaterialsCount.Value;
    public static bool Tooltip => VisualChanges.AvailableMaterialsCount && Instance.tooltip;
    public static bool ItemSlot => VisualChanges.AvailableMaterialsCount && Instance.itemSlot && !UnloadedVisualChanges.Instance.availableMaterialsCount_itemSlot;
}

public sealed class RecipeTooltip {
    [DefaultValue(false)] public bool objectsLine = false;

    public static RecipeTooltip Instance => VisualChanges.Instance.recipeTooltip.Value;
}

public sealed class ItemAmmo {
    [DefaultValue(true)] public bool tooltip = true;
    public Toggle<ItemSlotAmmo> itemSlot = new(true);

    public static ItemAmmo Instance => VisualChanges.Instance.itemAmmo.Value;
    public static bool Tooltip => VisualChanges.ItemAmmo && Instance.tooltip;
    public static bool ItemSlot => VisualChanges.ItemAmmo && Instance.itemSlot;
}
public sealed class ItemSlotAmmo {
    [DefaultValue(0.55f)] public float size = 0.55f;
    [DefaultValue(Corner.BottomRight)] public Corner position = Corner.BottomRight;
    [DefaultValue(true)] public bool hover = true;
}

public enum Corner { TopLeft, TopRight, BottomLeft, BottomRight }