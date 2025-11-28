using System.ComponentModel;
using SpikysLib.Configs;
using Terraria.ModLoader.Config;

namespace BetterInventory.Configs;

public sealed class Improvements : ModConfig {

    public Toggle<BetterRecipeList> betterRecipeList = new(true);
    public Toggle<BetterRecipeGrid> betterRecipeGrid = new(true);
    public Toggle<MoreMaterials> moreMaterials = new(true);
    public Toggle<ScrollableTooltip> scrollableTooltip = new(true);
    public Toggle<SmartConsumption> smartConsumption = new(true);

    public static Improvements Instance = null!;
    public static bool BetterRecipeList => Instance.betterRecipeList;
    public static bool BetterRecipeGrid => Instance.betterRecipeGrid;
    public static bool MoreMaterials => Instance.moreMaterials;
    public static bool ScrollableTooltip => Instance.scrollableTooltip;
    public static bool SmartConsumption => Instance.smartConsumption;

    public override ConfigScope Mode => ConfigScope.ClientSide;
}

public sealed class MoreMaterials {
    [DefaultValue(true)] public bool mouse = true;
    public Toggle<EquipmentMaterials> equipment = new(true);

    public static MoreMaterials Instance => Improvements.Instance.moreMaterials.Value;
    public static bool Mouse => Improvements.MoreMaterials && Instance.mouse;
    public static bool Equipment => Improvements.MoreMaterials && Instance.equipment;
}

public sealed class EquipmentMaterials {
    [DefaultValue(false)] public bool allLoadouts = false;
    
    public static EquipmentMaterials Instance => MoreMaterials.Instance.equipment.Value;
}

public sealed class BetterRecipeList {
    [DefaultValue(true)] public bool craftWhenHolding = true;
    public Toggle<FastScroll> fastScroll = new(true);

    public static BetterRecipeList Instance => Improvements.Instance.betterRecipeList.Value;
    public static bool CraftWhenHolding => Improvements.BetterRecipeList && Instance.craftWhenHolding;
    public static bool FastScroll => Improvements.BetterRecipeList && Instance.fastScroll && !UnloadedImprovements.Instance.betterRecipeList_fastScroll;
}

public sealed class FastScroll {
    [DefaultValue(true)] public bool listScroll = true;

    public static FastScroll Instance => BetterRecipeList.Instance.fastScroll.Value;
}

public sealed class BetterRecipeGrid {
    public Toggle<CraftOnRecipeGrid> craftOnRecipeGrid = new(true);
    [DefaultValue(true)] public bool refocusButton = true;
    [DefaultValue(true)] public bool noRecStartOffset = true;
    [DefaultValue(true)] public bool noRecListClose = true;
    [DefaultValue(true)] public bool rememberListPosition = true;
    [DefaultValue(true)] public bool pageScroll = true;

    public static BetterRecipeGrid Instance => Improvements.Instance.betterRecipeGrid.Value;
    public static bool CraftOnRecList => Improvements.BetterRecipeGrid && Instance.craftOnRecipeGrid && !UnloadedImprovements.Instance.betterRecipeGrid_craftOnRecipeGrid;
    public static bool RefocusButton => Improvements.BetterRecipeGrid && Instance.refocusButton && !UnloadedImprovements.Instance.betterRecipeGrid_refocusButton;
    public static bool NoRecStartOffset => Improvements.BetterRecipeGrid && Instance.noRecStartOffset && !UnloadedImprovements.Instance.betterRecipeGrid_noRecStartOffset;
    public static bool NoRecListClose => Improvements.BetterRecipeGrid && Instance.noRecListClose && !UnloadedImprovements.Instance.betterRecipeGrid_noRecListClose;
    public static bool RememberListPosition => Improvements.BetterRecipeGrid && Instance.rememberListPosition;
    public static bool PageScroll => Improvements.BetterRecipeGrid && Instance.pageScroll && !UnloadedImprovements.Instance.betterRecipeGrid_pageScroll;
}

public sealed class CraftOnRecipeGrid {
    [DefaultValue(false)] public bool focusHovered = false;

    public static CraftOnRecipeGrid Instance => BetterRecipeGrid.Instance.craftOnRecipeGrid.Value;
}

public sealed class ScrollableTooltip {
    [DefaultValue(1)] public float maximumHeight = 1;

    public static ScrollableTooltip Instance = Improvements.Instance.scrollableTooltip.Value;
}

public sealed class SmartConsumption {
    [DefaultValue(true)] public bool consumables = true;
    [DefaultValue(true)] public bool ammo = true;
    [DefaultValue(true)] public bool baits = true;
    [DefaultValue(true)] public bool paints = true;
    [DefaultValue(true)] public bool materials = true;
    [DefaultValue(false)] public bool mouse = false;
    [DefaultValue(false)] public bool self = false;

    public static SmartConsumption Value => Improvements.Instance.smartConsumption.Value;
    public static bool Consumables => Improvements.SmartConsumption && Value.consumables;
    public static bool Ammo => Improvements.SmartConsumption && Value.ammo;
    public static bool Baits => Improvements.SmartConsumption && Value.baits && !UnloadedImprovements.Instance.smartConsumption_baits;
    public static bool Paints => Improvements.SmartConsumption && Value.paints;
    public static bool Materials => Improvements.SmartConsumption && Value.materials && !UnloadedImprovements.Instance.smartConsumption_materials;
}