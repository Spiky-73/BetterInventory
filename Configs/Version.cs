using System.ComponentModel;
using BetterInventory.Configs.UI;
using Newtonsoft.Json;
using Terraria.ModLoader.Config;

namespace BetterInventory.Configs;

public sealed class Version : ModConfig {
    [DefaultValue(""), JsonProperty] internal string lastPlayedVersion { get; set; } = "";

    [JsonIgnore, ShowDespiteJsonIgnore] public Text? Info;
    [JsonIgnore, ShowDespiteJsonIgnore] public Text? Important = new(UpdateNotification.ImportantTags);
    [JsonIgnore, ShowDespiteJsonIgnore] public Text? Bug = new(UpdateNotification.BugTags);
    [Header("Changelog")]
    [JsonIgnore, ShowDespiteJsonIgnore] public Text? Changelog;


    public static Version Instance = null!;
    public override ConfigScope Mode => ConfigScope.ClientSide;
}