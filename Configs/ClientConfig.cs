using System;
using System.ComponentModel;
using Terraria;
using Terraria.ModLoader.Config;

namespace BetterInventory.Configs;

public enum SmartPickupLevel {
    Off,
    FavoriteOnly,
    AllItems
}

public enum UnknownSearchBehaviour { Hidden, Unknown, Known}

public sealed class ClientConfig : ModConfig {

    [Header("InventoryManagement")]
    public Toggle<SmartItems> smartItems = new(true);
    public Toggle<QuickMove> quickMove = new(true);
    [DefaultValue(true)] public bool fastRightClick;
    [DefaultValue(true)] public bool itemRightClick;

    [Header("Crafting")]
    [DefaultValue(true)] public bool recipeFiltering;
    public Toggle<BetterCrafting> betterCrafting = new(true);

    [Header("ItemSearch")]
    [DefaultValue(true)] public bool betterGuide;
    [DefaultValue(true)] public bool searchDrops;
    [DefaultValue(UnknownSearchBehaviour.Known)] public UnknownSearchBehaviour unknownBehaviour;

    public override ConfigScope Mode => ConfigScope.ClientSide;
    public static ClientConfig Instance = null!;

    public override void OnChanged() {
        ItemSearch.SearchItem.UpdateMouseItem();
        if(!Main.gameMenu) Crafting.RecipeFiltering.FilterRecipes();
    }
}