using System.ComponentModel;
using SpikysLib.Configs;
using Terraria.ModLoader.Config;

namespace BetterInventory.Configs;

public sealed class VanillaFixes : ModConfig {

    public Toggle<Empty> craftWhenHolding = new(true);
    [DefaultValue(true)] public bool ammoPickup;
    public Toggle<ConsistantScrollDirection> consistantScrollDirection = new(true);
    [DefaultValue(true)] public bool materialsWrapping;


    public static VanillaFixes Instance = null!;
    public static bool AmmoPickup => Instance.ammoPickup && !UnloadedVanillaFixes.Instance.ammoPickup;
    public static bool ConsistantScrollDirection => Instance.consistantScrollDirection;
    public static bool MaterialsWrapping => Instance.materialsWrapping && !UnloadedVanillaFixes.Instance.materialsWrapping;

    public override ConfigScope Mode => ConfigScope.ClientSide;
}

public sealed class ConsistantScrollDirection {
    [DefaultValue(true)] public bool recipesUnpaused = true;
    [DefaultValue(true)] public bool recipesPaused = true;
    [DefaultValue(true)] public bool accessories = true;

    public static ConsistantScrollDirection Instance => VanillaFixes.Instance.consistantScrollDirection.Value;
    public static bool RecipesUnpaused => VanillaFixes.ConsistantScrollDirection && Instance.recipesUnpaused && !UnloadedVanillaFixes.Instance.consistantScrollDirection_recipesUnpaused;
    public static bool RecipesPaused => VanillaFixes.ConsistantScrollDirection && Instance.recipesPaused && !UnloadedVanillaFixes.Instance.consistantScrollDirection_recipesPaused;
    public static bool Accessories => VanillaFixes.ConsistantScrollDirection && Instance.accessories && !UnloadedVanillaFixes.Instance.consistantScrollDirection_accessories;
}