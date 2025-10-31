using System.ComponentModel;
using SpikysLib.Configs;
using Terraria.ModLoader.Config;

namespace BetterInventory.Configs;

public sealed class VisualChanges : ModConfig {
    public Toggle<AvailableMaterialsCount> availableMaterialsCount = new(true);
    [DefaultValue(true)] public bool recipeCount;
    public Toggle<RecipeTooltip> recipeTooltip = new(true);

    public static VisualChanges Instance = null!;
    public static bool AvailableMaterialsCount => Instance.availableMaterialsCount;
    public static bool RecipeCount => Instance.recipeCount && !UnloadedVisualChanges.Instance.recipeCount;
    public static bool RecipeTooltip => Instance.recipeTooltip;


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
