using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using BetterInventory.Default.Interfaces;
using SpikysLib.Configs;
using Terraria.ModLoader.Config;

namespace BetterInventory.BetterMenuNavigation;

public sealed class BetterMenuNavigationConfig : ModConfig {
    public override bool Autoload(ref string name) => BetterInventoryConfig.EnsureLoaded(this) && base.Autoload(ref name) && BetterInventoryConfig.BetterMenuNavigation;

    [Fallible] public Toggle<ConsistantScrollDirectionConfig> consistantScrollDirection = new(true);
    public Toggle<MenuChainsConfig> menuChains = new(true);
    // public Toggle<QuickSearchConfig> quickSearch = new(true);

    public static BetterMenuNavigationConfig Instance = null!;
    public static bool ConsistantScrollDirection => BetterInventoryConfig.BetterMenuNavigation && Instance.consistantScrollDirection;
    public static bool MenuChains => BetterInventoryConfig.BetterMenuNavigation && Instance.menuChains;

    public override ConfigScope Mode => ConfigScope.ClientSide;
}

public sealed class ConsistantScrollDirectionConfig {
    [Fallible, DefaultValue(true)] public bool recipesUnpaused = true;
    [Fallible, DefaultValue(true)] public bool recipesPaused = true;
    [Fallible, DefaultValue(true)] public bool accessories = true;

    public static ConsistantScrollDirectionConfig Instance => BetterMenuNavigationConfig.Instance.consistantScrollDirection.Value;
    public static bool RecipesUnpaused => Instance.recipesUnpaused && !FailedConsistantScrollDirectionConfig.Instance.recipesUnpaused;
    public static bool RecipesPaused => Instance.recipesPaused && !FailedConsistantScrollDirectionConfig.Instance.recipesPaused;
    public static bool Accessories => Instance.accessories && !FailedConsistantScrollDirectionConfig.Instance.accessories;
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
                new(nameof(BetterInventory), nameof(CraftingWindow)),
                new(nameof(BetterInventory), nameof(Bestiary)),
                new(nameof(BetterInventory), nameof(DuplicationMenu)),
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

public sealed class FailedBetterMenuNavigationConfig {
    public FailedConsistantScrollDirectionConfig consistantScrollDirection = new();

    public static FailedBetterMenuNavigationConfig Instance => FailedBetterInventoryConfig.Instance.betterMenuNavigation;
}

public sealed class FailedConsistantScrollDirectionConfig {
    public bool recipesUnpaused;
    public bool recipesPaused;
    public bool accessories;

    public static FailedConsistantScrollDirectionConfig Instance => FailedBetterMenuNavigationConfig.Instance.consistantScrollDirection;
}