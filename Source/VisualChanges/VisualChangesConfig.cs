using System.ComponentModel;
using BetterInventory.Configs;
using BetterInventory.VisualChanges.AvailableMaterialsCount;
using BetterInventory.VisualChanges.ItemAmmo;
using BetterInventory.VisualChanges.RecipeTooltip;
using SpikysLib.Configs;
using Terraria.ModLoader.Config;

namespace BetterInventory.VisualChanges;

using VCUnloadableAttribute = UnloadableAttribute<UnloadedVisualChangesConfig>;

public sealed class VisualChangesConfig : ModConfig {
    [VCUnloadable(nameof(availableMaterialsCount))] public Toggle<AvailableMaterialsCountConfig> availableMaterialsCount = new(true);
    [VCUnloadable(nameof(recipeCount)), DefaultValue(true)] public bool recipeCount;
    public Toggle<RecipeTooltipConfig> recipeTooltip = new(true);
    public Toggle<ItemAmmoConfig> itemAmmo = new(true);
    [VCUnloadable(nameof(inventorySlotsTexture)), DefaultValue(true)] public bool inventorySlotsTexture = true;

    public static VisualChangesConfig Instance = null!;
    public static bool AvailableMaterialsCount => Instance.availableMaterialsCount;
    public static bool RecipeCount => Instance.recipeCount && !UnloadedVisualChangesConfig.Instance.recipeCount;
    public static bool RecipeTooltip => Instance.recipeTooltip;
    public static bool ItemAmmo => Instance.itemAmmo;
    public static bool InventorySlotsTexture => Instance.inventorySlotsTexture && !UnloadedVisualChangesConfig.Instance.inventorySlotsTexture;

    public override ConfigScope Mode => ConfigScope.ClientSide;
}

public sealed class UnloadedVisualChangesConfig {
    public bool recipeCount;
    public UnloadedAvailableMaterialsCountConfig availableMaterialsCount = new();
    public bool inventorySlotsTexture;

    public static UnloadedVisualChangesConfig Instance => CompatibilityConfig.Instance.unloadedVisualChanges;
}
