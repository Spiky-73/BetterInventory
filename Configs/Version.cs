using System.ComponentModel;
using BetterInventory.Configs.UI;
using Newtonsoft.Json;
using Terraria.ModLoader.Config;

namespace BetterInventory.Configs;

public sealed class Version : ModConfig {
    [DefaultValue(""), JsonProperty] internal string lastPlayedVersion { get; set; } = "";

    public Text? Info { get; set; }

    public Text? ChangeLog { get; set; }


    public static Version Instance = null!;
    public override ConfigScope Mode => ConfigScope.ClientSide;
}