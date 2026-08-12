using System.ComponentModel;
using SpikysLib.Configs;
using Terraria.ModLoader.Config;

namespace BetterInventory.BetterInventoryManagement;

public sealed class BetterInventoryManagementConfig : ModConfig {
    public override bool Autoload(ref string name) => BetterInventoryConfig.EnsureLoaded(this) && base.Autoload(ref name) && BetterInventoryConfig.BetterInventoryManagement;

    [Fallible, DefaultValue(true)] public bool completeQuickStack = true;
    [Fallible, DefaultValue(true)] public bool limitedBanksQuickStack = true;
    [Fallible, DefaultValue(true)] public bool stackTrash = true;
    [DefaultValue(true)] public bool trashTrash = true;
    [DefaultValue(true)] public bool fasterGrabBags;
    [DefaultValue(true)] public bool fasterExtractinator;
    [Fallible, DefaultValue(true)] public bool favoriteInBanks;
    [Fallible] public Toggle<SmartConsumptionConfig> smartConsumption = new(true);
    [DefaultValue(true)] public bool keepSwappedFavorited = true;
    [DefaultValue(true)] public bool craftWithMouse = true;
    public Toggle<EquipmentMaterialsConfig> craftWithEquipment = new(true);
    public Toggle<ExtraItemRightClickConfig> extraItemRightClick = new(true);
    [Fallible] public Toggle<QuickMoveConfig> quickMove = new(true);
    public Toggle<PreferFavoritedItemsConfig> preferFavoritedItems = new(true);
    [DefaultValue(true)] public bool quickActionsKeybinds = true;
    [DefaultValue(true)] public bool builderTogglesKeybinds = true;
    public Toggle<CraftStackConfig> craftStack = new(true);
    [DefaultValue(true)] public bool shiftRightClick = true;
    [Fallible, DefaultValue(true)] public bool universalShiftClick = true;
    [DefaultValue(true)] public bool depositClick = true;

    public static BetterInventoryManagementConfig Instance = null!;
    public static bool CompleteQuickStack => BetterInventoryConfig.BetterInventoryManagement && Instance.completeQuickStack && !FailedBetterInventoryManagementConfig.Instance.completeQuickStack;
    public static bool LimitedBanksQuickStack => BetterInventoryConfig.BetterInventoryManagement && Instance.limitedBanksQuickStack && !FailedBetterInventoryManagementConfig.Instance.limitedBanksQuickStack;
    public static bool StackTrash => BetterInventoryConfig.BetterInventoryManagement && Instance.stackTrash && !FailedBetterInventoryManagementConfig.Instance.stackTrash;
    public static bool TrashTrash => BetterInventoryConfig.BetterInventoryManagement && Instance.trashTrash;
    public static bool FasterGrabBags => BetterInventoryConfig.BetterInventoryManagement && Instance.fasterGrabBags;
    public static bool FasterExtractinator => BetterInventoryConfig.BetterInventoryManagement && Instance.fasterExtractinator;
    public static bool FavoriteInBanks => BetterInventoryConfig.BetterInventoryManagement && Instance.favoriteInBanks && !FailedBetterInventoryManagementConfig.Instance.favoriteInBanks;
    public static bool SmartConsumption => BetterInventoryConfig.BetterInventoryManagement && Instance.smartConsumption;
    public static bool KeepSwappedFavorited => BetterInventoryConfig.BetterInventoryManagement && Instance.keepSwappedFavorited;
    public static bool CraftWithMouse => BetterInventoryConfig.BetterInventoryManagement && Instance.craftWithMouse;
    public static bool CraftWithEquipment => BetterInventoryConfig.BetterInventoryManagement && Instance.craftWithEquipment;
    public static bool ExtraItemRightClick => BetterInventoryConfig.BetterInventoryManagement && Instance.extraItemRightClick;
    public static bool QuickMove => BetterInventoryConfig.BetterInventoryManagement && Instance.quickMove;
    public static bool PreferFavoritedItems => BetterInventoryConfig.BetterInventoryManagement && Instance.preferFavoritedItems;
    public static bool QuickActionsKeybinds => BetterInventoryConfig.BetterInventoryManagement && Instance.quickActionsKeybinds;
    public static bool BuilderTogglesKeybinds => BetterInventoryConfig.BetterInventoryManagement && Instance.builderTogglesKeybinds;
    public static bool CraftStack => BetterInventoryConfig.BetterInventoryManagement && Instance.craftStack;
    public static bool ShiftRightClick => BetterInventoryConfig.BetterInventoryManagement && Instance.shiftRightClick;
    public static bool UniversalShiftClick => BetterInventoryConfig.BetterInventoryManagement && Instance.universalShiftClick;
    public static bool DepositClick => BetterInventoryConfig.BetterInventoryManagement && Instance.depositClick;

    public override void OnChanged() {
        FavoriteInBanksPlayer.OnConfigChanged();
    }
    
    public override ConfigScope Mode => ConfigScope.ClientSide;
}

public sealed class SmartConsumptionConfig {
    [DefaultValue(true)] public bool consumables = true;
    [DefaultValue(true)] public bool ammo = true;
    [Fallible, DefaultValue(true)] public bool baits = true;
    [DefaultValue(true)] public bool paints = true;
    [Fallible, DefaultValue(true)] public bool materials = true;
    [DefaultValue(false)] public bool mouse = false;
    [DefaultValue(false)] public bool self = false;

    public static SmartConsumptionConfig Instance => BetterInventoryManagementConfig.Instance.smartConsumption.Value;
    public static bool Consumables => BetterInventoryManagementConfig.SmartConsumption && Instance.consumables;
    public static bool Ammo => BetterInventoryManagementConfig.SmartConsumption && Instance.ammo;
    public static bool Baits => BetterInventoryManagementConfig.SmartConsumption && Instance.baits && !FailedSmartConsumptionConfig.Instance.baits;
    public static bool Paints => BetterInventoryManagementConfig.SmartConsumption && Instance.paints;
    public static bool Materials => BetterInventoryManagementConfig.SmartConsumption && Instance.materials && !FailedSmartConsumptionConfig.Instance.materials;
}

public sealed class EquipmentMaterialsConfig {
    [DefaultValue(false)] public bool allLoadouts = false;

    public static EquipmentMaterialsConfig Instance => BetterInventoryManagementConfig.Instance.craftWithEquipment.Value;
}

public sealed class ExtraItemRightClickConfig {
    [DefaultValue(false)] public bool stackableItems = false;

    public static ExtraItemRightClickConfig Instance => BetterInventoryManagementConfig.Instance.extraItemRightClick.Value;
}

public sealed class QuickMoveConfig {
    [DefaultValue(HotkeyMode.Hotbar)] public HotkeyMode hotkeyMode = HotkeyMode.Hotbar;
    [Range(0, 3600), DefaultValue(60 * 3)] public int graceTime = 60 * 3;

    [DefaultValue(true)] public bool followItem = true;
    [DefaultValue(true)] public bool bringItem = true;
    [DefaultValue(true)] public bool returnToSlot = true;
    [DefaultValue(false)] public bool inactiveInventories = false;

    [Fallible, DefaultValue(HotkeyDisplayMode.All)] public HotkeyDisplayMode displayedHotkeys = HotkeyDisplayMode.All;
    [DefaultValue(false)] public bool itemTooltip = false;

    public static QuickMoveConfig Instance => BetterInventoryManagementConfig.Instance.quickMove.Value;
    public static bool DisplayHotkeys => BetterInventoryManagementConfig.QuickMove && Instance.displayedHotkeys != HotkeyDisplayMode.None && !FailedQuickMoveConfig.Instance.displayedHotkeys;
    public static bool ItemTooltip => BetterInventoryManagementConfig.QuickMove && Instance.itemTooltip;
}

public enum HotkeyDisplayMode { None, Next, All }
public enum HotkeyMode { Hotbar, FromEnd, Reversed }

public sealed class PreferFavoritedItemsConfig {
    [DefaultValue(true)] public bool quickBuff = true;

    public static PreferFavoritedItemsConfig Instance => BetterInventoryManagementConfig.Instance.preferFavoritedItems.Value;
}

public sealed class CraftStackConfig {
    public NestedValue<MaxCraftAmountConfig, MaxRoundingConfig> maxItems = new(999);
    [DefaultValue(true)] public bool repeat = true;
    [DefaultValue(false)] public bool invertClicks = false;
    [DefaultValue(true)] public bool tooltip = true;

    public static CraftStackConfig Instance => BetterInventoryManagementConfig.Instance.craftStack.Value;
}

public sealed class MaxCraftAmountConfig : MultiChoice<int> {
    public MaxCraftAmountConfig() : base() { }
    public MaxCraftAmountConfig(int value) : base(value) { }

    [Choice, Range(1, 9999), DefaultValue(999)] public int amount = 999;
    [Choice] public Text? spicRequirement;

    public override int Value {
        get => Choice == nameof(spicRequirement) ? 0 : amount;
        set {
            if (value == 0) Choice = nameof(spicRequirement);
            else {
                Choice = nameof(amount);
                amount = value;
            }
        }
    }

    public static implicit operator MaxCraftAmountConfig(int count) => new(count);
    public static MaxCraftAmountConfig FromString(string s) => new(int.Parse(s));
}

public sealed class MaxRoundingConfig {
    [DefaultValue(true)] public bool above = true;
}

public sealed class FailedBetterInventoryManagementConfig {
    public bool completeQuickStack;
    public bool limitedBanksQuickStack;
    public bool stackTrash;
    public bool favoriteInBanks;
    public FailedSmartConsumptionConfig smartConsumption = new();
    public FailedQuickMoveConfig quickMove = new();
    public FailedUniversalShiftClick universalShiftClick = new();

    public static FailedBetterInventoryManagementConfig Instance => FailedBetterInventoryConfig.Instance.betterInventoryManagement;
}

public sealed class FailedSmartConsumptionConfig {
    public bool baits;
    public bool materials;

    public static FailedSmartConsumptionConfig Instance => FailedBetterInventoryManagementConfig.Instance.smartConsumption;
}

public sealed class FailedQuickMoveConfig {
    public bool displayedHotkeys;

    public static FailedQuickMoveConfig Instance => FailedBetterInventoryManagementConfig.Instance.quickMove;
}

public sealed class FailedUniversalShiftClick {
    public bool quickCraft;

    public static FailedUniversalShiftClick Instance => FailedBetterInventoryManagementConfig.Instance.universalShiftClick;
}
