using System.ComponentModel;
using Newtonsoft.Json;
using Terraria.ModLoader.Config;

namespace BetterInventory.Configs;

public sealed class CompatibilityConfig : ModConfig {

    [ReloadRequired, DefaultValue(true)] public bool loadDisabledFeatures = true;

    [JsonIgnore, ShowDespiteJsonIgnore] public UnloadedFeaturesConfig unloadedFeatures = new();

    public static CompatibilityConfig Instance = null!;

    public override ConfigScope Mode => ConfigScope.ClientSide;
}
