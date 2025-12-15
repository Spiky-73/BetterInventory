using System.ComponentModel;
using SpikysLib.Configs;
using Terraria.ModLoader.Config;

namespace BetterInventory.Configs;

using VPUnloadable = UnloadableAttribute<UnloadedVanillaPatchesConfig>;
using CSDUnloadable = UnloadableAttribute<UnloadedConsistantScrollDirectionConfig>;

public sealed class VanillaPatchesConfig : ModConfig {

    [DefaultValue(true)] public bool ammoPickupOrder;
    [VPUnloadable(nameof(consistantScrollDirection))] public Toggle<ConsistantScrollDirectionConfig> consistantScrollDirection = new(true);
    [DefaultValue(true)] public bool materialsWrapping;


    public static VanillaPatchesConfig Instance = null!;
    public static bool AmmoPickupOrder => Instance.ammoPickupOrder && !UnloadedVanillaPatchesConfig.Instance.ammoPickupOrder;
    public static bool ConsistantScrollDirection => Instance.consistantScrollDirection;
    public static bool MaterialsWrapping => Instance.materialsWrapping && !UnloadedVanillaPatchesConfig.Instance.materialsWrapping;

    public override ConfigScope Mode => ConfigScope.ClientSide;
}
public sealed class UnloadedVanillaPatchesConfig {
    public bool ammoPickupOrder;
    public bool consistantScrollDirection_recipesUnpaused;
    public bool consistantScrollDirection_recipesPaused;
    public bool consistantScrollDirection_accessories;
    public bool materialsWrapping;
    public UnloadedConsistantScrollDirectionConfig consistantScrollDirection = new();

    public static UnloadedVanillaPatchesConfig Instance => CompatibilityConfig.Instance.unloadedVanillaPatches;
}

public sealed class ConsistantScrollDirectionConfig {
    [CSDUnloadable(nameof(recipesUnpaused)), DefaultValue(true)] public bool recipesUnpaused = true;
    [CSDUnloadable(nameof(recipesPaused)), DefaultValue(true)] public bool recipesPaused = true;
    [CSDUnloadable(nameof(accessories)), DefaultValue(true)] public bool accessories = true;

    public static ConsistantScrollDirectionConfig Instance => VanillaPatchesConfig.Instance.consistantScrollDirection.Value;
    public static bool RecipesUnpaused => Instance.recipesUnpaused && !UnloadedConsistantScrollDirectionConfig.Instance.recipesUnpaused;
    public static bool RecipesPaused => Instance.recipesPaused && !UnloadedConsistantScrollDirectionConfig.Instance.recipesPaused;
    public static bool Accessories => Instance.accessories && !UnloadedConsistantScrollDirectionConfig.Instance.accessories;
}
public sealed class UnloadedConsistantScrollDirectionConfig {
    public bool recipesUnpaused;
    public bool recipesPaused;
    public bool accessories;

    public static UnloadedConsistantScrollDirectionConfig Instance => UnloadedVanillaPatchesConfig.Instance.consistantScrollDirection;
}