using System.ComponentModel;
using SpikysLib.Configs.UI;
using Newtonsoft.Json;
using Terraria.ModLoader.Config;
using SpikysLib.UI;
using Terraria.ID;

namespace BetterInventory.Configs;

public sealed class Version : ModConfig {
    [DefaultValue(""), JsonProperty] internal string lastPlayedVersion = "";

    [JsonIgnore, ShowDespiteJsonIgnore] public Text? Info;
    [JsonIgnore, ShowDespiteJsonIgnore] public Text? Important = new(new StringLine(string.Empty, Colors.RarityAmber));
    [JsonIgnore, ShowDespiteJsonIgnore] public Text? Bug;
    [Header("Changelog")]
    [JsonIgnore, ShowDespiteJsonIgnore] public Text? Changelog;


    public static Version Instance = null!;
    public override ConfigScope Mode => ConfigScope.ClientSide;
}