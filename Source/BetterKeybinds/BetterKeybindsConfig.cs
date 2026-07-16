using System.ComponentModel;
using SpikysLib.Configs;
using Terraria.ModLoader.Config;

namespace BetterInventory.BetterKeybinds;

using BKUnloadableAttribute = UnloadableAttribute<UnloadedBetterKeybindsConfig>;
using CSDUnloadable = UnloadableAttribute<UnloadedConsistantScrollDirectionConfig>;

public sealed class BetterKeybindsConfig : ModConfig {
    [BKUnloadable(nameof(consistantScrollDirection))] public Toggle<ConsistantScrollDirectionConfig> consistantScrollDirection = new(true);
    [DefaultValue(true)] public bool favoritedBuff = true;
    [DefaultValue(true)] public bool builderAccs = true;
    [DefaultValue(true)] public bool quickStack = true;

    public static BetterKeybindsConfig Instance = null!;
    public static bool ConsistantScrollDirection => BetterInventoryConfig.BetterKeybinds && Instance.consistantScrollDirection;
    public static bool FavoritedBuff => BetterInventoryConfig.BetterKeybinds && Instance.favoritedBuff;
    public static bool BuilderAccs => BetterInventoryConfig.BetterKeybinds && Instance.builderAccs;
    public static bool QuickStack => BetterInventoryConfig.BetterKeybinds && Instance.quickStack;

    public override ConfigScope Mode => ConfigScope.ClientSide;
}

public sealed class ConsistantScrollDirectionConfig {
    [CSDUnloadable(nameof(recipesUnpaused)), DefaultValue(true)] public bool recipesUnpaused = true;
    [CSDUnloadable(nameof(recipesPaused)), DefaultValue(true)] public bool recipesPaused = true;
    [CSDUnloadable(nameof(accessories)), DefaultValue(true)] public bool accessories = true;

    public static ConsistantScrollDirectionConfig Instance => BetterKeybindsConfig.Instance.consistantScrollDirection.Value;
    public static bool RecipesUnpaused => Instance.recipesUnpaused && !UnloadedConsistantScrollDirectionConfig.Instance.recipesUnpaused;
    public static bool RecipesPaused => Instance.recipesPaused && !UnloadedConsistantScrollDirectionConfig.Instance.recipesPaused;
    public static bool Accessories => Instance.accessories && !UnloadedConsistantScrollDirectionConfig.Instance.accessories;
}
public sealed class UnloadedConsistantScrollDirectionConfig {
    public bool recipesUnpaused;
    public bool recipesPaused;
    public bool accessories;

    public static UnloadedConsistantScrollDirectionConfig Instance => UnloadedBetterKeybindsConfig.Instance.consistantScrollDirection;
}

public sealed class UnloadedBetterKeybindsConfig {
    public UnloadedConsistantScrollDirectionConfig consistantScrollDirection = new();

    public static UnloadedBetterKeybindsConfig Instance => BetterInventoryConfig.Instance.unloadedBetterKeybinds;
}
