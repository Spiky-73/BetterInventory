using System.ComponentModel;
using SpikysLib.Configs;
using Terraria.ModLoader.Config;

namespace BetterInventory.BetterBestiary;

using BBUnloadableAttribute = UnloadableAttribute<UnloadedBetterBestiaryConfig>;

public sealed class BetterBestiaryConfig : ModConfig {
    [DefaultValue(true)] public bool unlockFilter = true;
    [BBUnloadable(nameof(minimalDisplayedInfo))] public Toggle<MinimalDisplayedInfoConfig> minimalDisplayedInfo = new(true);
    [DefaultValue(true)] public bool treasureBagContent = true;
    [BBUnloadable(nameof(unknownNPCs))] public Toggle<UnknownNPCsConfig> unknownNPCs = new(true);

    public static BetterBestiaryConfig Instance = null!;
    public static bool UnlockFilter => BetterInventoryConfig.BetterBestiary && Instance.unlockFilter;
    public static bool MinimalDisplayedInfo => BetterInventoryConfig.BetterBestiary && Instance.minimalDisplayedInfo && !UnloadedBetterBestiaryConfig.Instance.minimalDisplayedInfo;
    public static bool TreasureBagContent => BetterInventoryConfig.BetterBestiary && Instance.treasureBagContent;
    public static bool UnknownNPCs => BetterInventoryConfig.BetterBestiary && Instance.unknownNPCs && !UnloadedBetterBestiaryConfig.Instance.unknownNPCs;

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

public sealed class UnloadedBetterBestiaryConfig {
    public bool minimalDisplayedInfo;
    public bool unknownNPCs;

    public static UnloadedBetterBestiaryConfig Instance => BetterInventoryConfig.Instance.unloadedBetterBestiary;
}

