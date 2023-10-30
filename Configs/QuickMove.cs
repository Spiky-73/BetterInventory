using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace BetterInventory.Configs;

public class QuickMove {
    
    [DefaultValue(true)] public bool Return = true;
    [Range(0, 3600), DefaultValue(60)] public int ChainTime = 60;
    [DefaultValue(true)] public bool Tooltip = true;
    [DefaultValue(true)] public bool Slots = true;
}