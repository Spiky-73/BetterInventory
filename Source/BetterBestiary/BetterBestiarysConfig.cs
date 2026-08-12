using System.ComponentModel;
using SpikysLib.Configs;
using Terraria.ModLoader.Config;

namespace BetterInventory.BetterBestiary;

public sealed class BetterBestiaryConfig : ModConfig {
    public override bool Autoload(ref string name) => BetterInventoryConfig.EnsureLoaded(this) && base.Autoload(ref name) && BetterInventoryConfig.BetterBestiary;

    [DefaultValue(true)] public bool unlockFilter = true;
    [Fallible] public Toggle<MinimalDisplayedInfoConfig> minimalDisplayedInfo = new(true);
    [DefaultValue(true)] public bool treasureBagContent = true;
    [Fallible] public Toggle<UnknownNPCsConfig> unknownNPCs = new(true);

    public static BetterBestiaryConfig Instance = null!;
    public static bool UnlockFilter => BetterInventoryConfig.BetterBestiary && Instance.unlockFilter;
    public static bool MinimalDisplayedInfo => BetterInventoryConfig.BetterBestiary && Instance.minimalDisplayedInfo && !FailedBetterBestiaryConfig.Instance.minimalDisplayedInfo;
    public static bool TreasureBagContent => BetterInventoryConfig.BetterBestiary && Instance.treasureBagContent;
    public static bool UnknownNPCs => BetterInventoryConfig.BetterBestiary && Instance.unknownNPCs && !FailedBetterBestiaryConfig.Instance.unknownNPCs;

    public override ConfigScope Mode => ConfigScope.ClientSide;
}

public enum DisplayedUnlockLevel { Locked, Name, Stats, Drops, DropRates }
public sealed class MinimalDisplayedInfoConfig {
    [DefaultValue(DisplayedUnlockLevel.Drops)] public DisplayedUnlockLevel unlockLevel = DisplayedUnlockLevel.Drops;

    public static MinimalDisplayedInfoConfig Instance => BetterBestiaryConfig.Instance.minimalDisplayedInfo.Value;
}

public enum UnknownDisplay { Hidden, Unknown, Known }

public sealed class UnknownNPCsConfig {
    [DefaultValue(UnknownDisplay.Unknown)] public UnknownDisplay unknownDisplay = UnknownDisplay.Unknown;

    public static UnknownNPCsConfig Instance => BetterBestiaryConfig.Instance.unknownNPCs.Value;
}

public sealed class FailedBetterBestiaryConfig {
    public bool minimalDisplayedInfo;
    public bool unknownNPCs;

    public static FailedBetterBestiaryConfig Instance => FailedBetterInventoryConfig.Instance.betterBestiary;
}

