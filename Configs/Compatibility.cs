using System.ComponentModel;
using Newtonsoft.Json;
using Terraria.ModLoader.Config;

namespace BetterInventory.Configs;

public sealed class Compatibility : ModConfig {

    [DefaultValue(false)] public bool compatibilityMode;

    [JsonIgnore, ShowDespiteJsonIgnore] public UnloadedCrafting crafting => Hooks.UnloadedCrafting;
    [JsonIgnore, ShowDespiteJsonIgnore] public UnloadedInventoryManagement inventoryManagement => Hooks.UnloadedInventoryManagement;
    [JsonIgnore, ShowDespiteJsonIgnore] public UnloadedItemSearch itemSearch => Hooks.UnloadedItemSearch;

    public static bool CompatibilityMode => Instance.compatibilityMode;
    public static Compatibility Instance = null!;
    
    public override ConfigScope Mode => ConfigScope.ClientSide;
}