using System.ComponentModel;
using BetterInventory.Configs;
using SpikysLib.Configs;

namespace BetterInventory.Improvements.BetterRecipeGrid;

using BRGUnloadable = UnloadableAttribute<UnloadedBetterRecipeGridConfig>;

public sealed class BetterRecipeGridConfig {
    [BRGUnloadable(nameof(craftOnRecipeGrid))] public Toggle<CraftOnRecipeGridConfig> craftOnRecipeGrid = new(true);
    [BRGUnloadable(nameof(refocusButton)), DefaultValue(true)] public bool refocusButton = true;
    [BRGUnloadable(nameof(noRecGridOffset)), DefaultValue(true)] public bool noRecGridOffset = true;
    [BRGUnloadable(nameof(noRecGridClose)), DefaultValue(true)] public bool noRecGridClose = true;
    [BRGUnloadable(nameof(rememberGridPosition)), DefaultValue(true)] public bool rememberGridPosition = true;
    [BRGUnloadable(nameof(pageScroll)), DefaultValue(true)] public bool pageScroll = true;

    public static bool Enabled => ImprovementsConfig.Instance.betterRecipeGrid;
    public static BetterRecipeGridConfig Instance => ImprovementsConfig.Instance.betterRecipeGrid.Value;
    public static bool CraftOnRecGrid => Instance.craftOnRecipeGrid && !UnloadedBetterRecipeGridConfig.Instance.craftOnRecipeGrid;
    public static bool RefocusButton => Instance.refocusButton && !UnloadedBetterRecipeGridConfig.Instance.refocusButton;
    public static bool NoRecGridOffset => Instance.noRecGridOffset && !UnloadedBetterRecipeGridConfig.Instance.noRecGridOffset;
    public static bool NoRecGridClose => Instance.noRecGridClose && !UnloadedBetterRecipeGridConfig.Instance.noRecGridClose;
    public static bool RememberGridPosition => Instance.rememberGridPosition;
    public static bool PageScroll => Instance.pageScroll && !UnloadedBetterRecipeGridConfig.Instance.pageScroll;
}

public sealed class CraftOnRecipeGridConfig {
    [DefaultValue(false)] public bool focusHovered = false;

    public static CraftOnRecipeGridConfig Instance => BetterRecipeGridConfig.Instance.craftOnRecipeGrid.Value;
}

public sealed class UnloadedBetterRecipeGridConfig {
    public bool craftOnRecipeGrid;
    public bool refocusButton;
    public bool noRecGridOffset;
    public bool noRecGridClose;
    public bool pageScroll;

    public static UnloadedBetterRecipeGridConfig Instance => UnloadedImprovementsConfig.Instance.betterRecipeGrid;
}