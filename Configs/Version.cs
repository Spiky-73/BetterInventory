using System.ComponentModel;
using SpikysLib.Configs;
using Newtonsoft.Json;
using Terraria.ModLoader.Config;

namespace BetterInventory.Configs;

public sealed class Version : ModConfig {
    [DefaultValue(""), JsonProperty] internal string lastPlayedVersion = "";

    [Header("Info")]
    [JsonIgnore, ShowDespiteJsonIgnore] public Text? Sumary;
    // [JsonIgnore, ShowDespiteJsonIgnore] public Text? Details = new(new StringLine(string.Empty, Colors.RarityAmber));
    [JsonIgnore, ShowDespiteJsonIgnore] public Text? Bug;
    [Header("Changelog")]
    [JsonIgnore, ShowDespiteJsonIgnore] public Text? Changelog;


    public static Version Instance = null!;
    public override ConfigScope Mode => ConfigScope.ClientSide;
}