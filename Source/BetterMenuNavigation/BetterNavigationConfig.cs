using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using BetterInventory.Default.Interfaces;
using SpikysLib.Configs;
using Terraria.ModLoader.Config;

namespace BetterInventory.BetterMenuNavigation;

using BKUnloadableAttribute = UnloadableAttribute<UnloadedBetterMenuNavigationConfig>;
using CSDUnloadable = UnloadableAttribute<UnloadedConsistantScrollDirectionConfig>;

public sealed class BetterMenuNavigationConfig : ModConfig {
    [BKUnloadable(nameof(consistantScrollDirection))] public Toggle<ConsistantScrollDirectionConfig> consistantScrollDirection = new(true);
    public Toggle<MenuChainsConfig> menuChains = new(true);
    // public Toggle<QuickSearchConfig> quickSearch = new(true);

    public static BetterMenuNavigationConfig Instance = null!;
    public static bool ConsistantScrollDirection => BetterInventoryConfig.BetterMenuNavigation && Instance.consistantScrollDirection;
    public static bool MenuChains => BetterInventoryConfig.BetterMenuNavigation && Instance.menuChains;

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

public sealed class MenuChainsConfig {
    [DefaultValue(MenuChainMode.Skip)] public MenuChainMode mode = MenuChainMode.Skip;
    [ReloadRequired] public List<MenuChain> chains = [];
    [Range(0, 3600), DefaultValue(20)] public int holdTime = 20;
    [Range(0, 3600), DefaultValue(20)] public int graceTime = 20;

    // BUG [tML][research] list of Reference type are duplicating when initialized directly
    [OnDeserialized]
    private void OnDeserialized(StreamingContext context) {
        if (chains.Count > 0) return;
        chains = [ new() {
            name = "Toggle Equip Pages",
            interfaces = [
                new(nameof(BetterInventory), nameof(ArmorPage)),
                new(nameof(BetterInventory), nameof(MiscEquipPage)),
                new(nameof(BetterInventory), nameof(HousingPage)),
            ]
        }, new() {
            name = "Toggle Map Styles",
            interfaces = [
                new(nameof(BetterInventory), nameof(MapClosed)),
                new(nameof(BetterInventory), nameof(MiniMap)),
                new(nameof(BetterInventory), nameof(BackgroundMap)),
                new(nameof(BetterInventory), nameof(FullScreenMap)),
            ]
        }, new() {
            name = "Toggle Catalogues",
            interfaces = [
                new(nameof(BetterInventory), nameof(CataloguesClosed)),
                new(nameof(BetterInventory), nameof(RecipeList)),
                new(nameof(BetterInventory), nameof(Bestiary)),
                new(nameof(BetterInventory), nameof(JourneyCatalogue)),
            ]
        }, new() {
            name = "Toggle Main Interfaces",
            interfaces = [
                new(nameof(BetterInventory), nameof(GameInterface)),
                new(nameof(BetterInventory), nameof(PlayerInventory)),
                new(nameof(BetterInventory), nameof(Settings)),
                new(nameof(BetterInventory), nameof(ModConfigList)),
                new(nameof(BetterInventory), nameof(ControlsMenu)),
            ]
        }, ];
    }

    public static MenuChainsConfig Instance => BetterMenuNavigationConfig.Instance.menuChains.Value;
}

public sealed class MenuChain {
    [DefaultValue("")] public string name = "";
    public List<InterfaceDefinition> interfaces = [];
}

public enum MenuChainMode { // ex for a chain [0,1,(2),3]
    Restart, // Restart from 0 (close): (2), 0
    Continue, // Continue where we are: (2), 3, 0
    Skip, // (2), 0, 1, 3, 2
    Toggle, // (2), 1,0,3, 2
}

public sealed class UnloadedBetterMenuNavigationConfig {
    public UnloadedConsistantScrollDirectionConfig consistantScrollDirection = new();

    public static UnloadedBetterMenuNavigationConfig Instance => BetterInventoryConfig.Instance.unloadedBetterMenuNavigation;
}
