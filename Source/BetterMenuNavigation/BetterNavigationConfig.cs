using System.ComponentModel;
using SpikysLib.Configs;
using Terraria.ModLoader.Config;

namespace BetterInventory.BetterMenuNavigation;

using BKUnloadableAttribute = UnloadableAttribute<UnloadedBetterMenuNavigationConfig>;
using CSDUnloadable = UnloadableAttribute<UnloadedConsistantScrollDirectionConfig>;

public sealed class BetterMenuNavigationConfig : ModConfig {
    [BKUnloadable(nameof(consistantScrollDirection))] public Toggle<ConsistantScrollDirectionConfig> consistantScrollDirection = new(true);
    public Toggle<QuickSearchConfig> quickSearch = new();
    public Toggle<MenuCyclesConfig> menuCycles = new();

    public static BetterMenuNavigationConfig Instance = null!;
    public static bool ConsistantScrollDirection => BetterInventoryConfig.BetterMenuNavigation && Instance.consistantScrollDirection;
    public static bool MenuCycles => BetterInventoryConfig.BetterMenuNavigation && Instance.menuCycles;

    public override ConfigScope Mode => ConfigScope.ClientSide;
}

public sealed class ConsistantScrollDirectionConfig {
    [CSDUnloadable(nameof(recipesUnpaused)), DefaultValue(true)] public bool recipesUnpaused = true;
    [CSDUnloadable(nameof(recipesPaused)), DefaultValue(true)] public bool recipesPaused = true;
    [CSDUnloadable(nameof(accessories)), DefaultValue(true)] public bool accessories = true;

    public static ConsistantScrollDirectionConfig Instance => BetterMenuNavigationConfig.Instance.consistantScrollDirection.Value;
    public static bool RecipesUnpaused => Instance.recipesUnpaused && !UnloadedConsistantScrollDirectionConfig.Instance.recipesUnpaused;
    public static bool RecipesPaused => Instance.recipesPaused && !UnloadedConsistantScrollDirectionConfig.Instance.recipesPaused;
    public static bool Accessories => Instance.accessories && !UnloadedConsistantScrollDirectionConfig.Instance.accessories;
}
public sealed class UnloadedConsistantScrollDirectionConfig {
    public bool recipesUnpaused;
    public bool recipesPaused;
    public bool accessories;

    public static UnloadedConsistantScrollDirectionConfig Instance => UnloadedBetterMenuNavigationConfig.Instance.consistantScrollDirection;
}

public sealed class QuickSearchConfig {
    [DefaultValue(true)] public bool composite = true;
}

public sealed class MenuCyclesConfig {
    [DefaultValue(10)] public int tap = 10;
    [DefaultValue(10)] public int delay = 10;
    [DefaultValue(MenuCycleMode.Toggle)] public MenuCycleMode mode = MenuCycleMode.Toggle;

    public static MenuCyclesConfig Instance => BetterMenuNavigationConfig.Instance.menuCycles.Value;
}

public enum MenuCycleMode { // ex for a cycle [0,1,>2<,3]
    Restart, // Restart from 0 (close): (2), 0, 1, 2, 3
    Continue, // Continue where we are: (2), 3, 0, 1, 2
    Skip, // (2), 0, 1, 3
    Toggle, // (2), 1,0,3
}

public sealed class UnloadedBetterMenuNavigationConfig {
    public UnloadedConsistantScrollDirectionConfig consistantScrollDirection = new();

    public static UnloadedBetterMenuNavigationConfig Instance => BetterInventoryConfig.Instance.unloadedBetterMenuNavigation;
}
