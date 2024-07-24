using System.ComponentModel;
using Terraria.ModLoader.Config;
using Terraria.UI;
using SpikysLib.Configs;
using Microsoft.Xna.Framework;
using SpikysLib.Configs.UI;
using System.Collections.Generic;
using BetterInventory.InventoryManagement;
using Newtonsoft.Json;
using SpikysLib;

namespace BetterInventory.Configs;

public sealed class InventoryManagement : ModConfig {
    public Toggle<SmartConsumption> smartConsumption = new(true);
    public Toggle<SmartPickup> smartPickup = new(true);
    public Toggle<QuickMove> quickMove = new(true);
    public Toggle<CraftStack> craftStack = new(true);
    [DefaultValue(true)] public bool favoriteInBanks;
    public Toggle<BetterShiftClick> betterShiftClick = new(true);
    [DefaultValue(true)] public bool stackTrash;

    public static InventoryManagement Instance = null!;
    public static bool FavoriteInBanks => !UnloadedInventoryManagement.Value.favoriteInBanks && Instance.favoriteInBanks;
    public static bool StackTrash => !UnloadedInventoryManagement.Value.stackTrash && Instance.stackTrash;

    // Compatibility version < v0.6
    [JsonProperty, DefaultValue(AutoEquipLevel.PrimarySlots)] private AutoEquipLevel autoEquip { set => PortConfig.MoveMember(value != AutoEquipLevel.PrimarySlots, _ => smartPickup.Value.autoEquip = value); }
    [JsonProperty, DefaultValue(true)] private bool shiftRight { set => PortConfig.MoveMember<InventoryManagement>(!value, c => c.betterShiftClick.Key = value); }

    public override void OnChanged() {
        Reflection.ItemSlot.canFavoriteAt.GetValue()[ItemSlot.Context.BankItem] = FavoriteInBanks;
    }

    public override ConfigScope Mode => ConfigScope.ClientSide;
}

public sealed class SmartConsumption {
    [DefaultValue(true)] public bool consumables = true;
    [DefaultValue(true)] public bool ammo = true;
    [DefaultValue(true)] public bool baits = true;
    [DefaultValue(true)] public bool paints = true;
    [DefaultValue(true)] public bool materials = true;
    [DefaultValue(false)] public bool mouse = false;

    public static bool Enabled => InventoryManagement.Instance.smartConsumption;
    public static bool Consumables => Enabled && Value.consumables;
    public static bool Ammo => Enabled && Value.ammo;
    public static bool Baits => Enabled && Value.baits && !UnloadedInventoryManagement.Value.baits;
    public static bool Paints => Enabled && Value.paints;
    public static bool Materials => Enabled && Value.materials && !UnloadedInventoryManagement.Value.materials;
    public static AllowedItems Mouse => Value.mouse ? AllowedItems.Mouse : AllowedItems.None;
    public static SmartConsumption Value => InventoryManagement.Instance.smartConsumption.Value;
}

public sealed class SmartPickup {
    public NestedValue<ItemPickupLevel, PreviousSlot> previousSlot = new(ItemPickupLevel.AllItems);
    [DefaultValue(AutoEquipLevel.PrimarySlots)] public AutoEquipLevel autoEquip = AutoEquipLevel.PrimarySlots;
    public Toggle<UpgradeItems> upgradeItems = new(true);
    [DefaultValue(true)] public bool hotbarLast = true;
    [DefaultValue(true)] public bool fixSlot = true;

    public static bool Enabled => InventoryManagement.Instance.smartPickup.Key;
    public static bool AutoEquip => !UnloadedInventoryManagement.Value.autoEquip && Enabled && Value.autoEquip > AutoEquipLevel.None;
    public static bool HotbarLast => !UnloadedInventoryManagement.Value.hotbarLast && Enabled && Value.hotbarLast;
    public static bool FixSlot => !UnloadedInventoryManagement.Value.fixSlot && Enabled && Value.fixSlot;
    public static SmartPickup Value => InventoryManagement.Instance.smartPickup.Value;

    // Compatibility version < v0.6
    [JsonProperty, DefaultValue(true)] private bool mediumCore { set => PortConfig.MoveMember(!value, _ => previousSlot.Value.mediumCore = value); }
    [JsonProperty, DefaultValue(0.33f)] private float markIntensity { set => PortConfig.MoveMember(value != 0.33f, _ => {
        if (value == 0) previousSlot.Value.displayPrevious.Key = false;
        else previousSlot.Value.displayPrevious.Value.fakeItem.Value.intensity = value;
    }); }
}
public enum ItemPickupLevel { None, ImportantItems, AllItems }
public enum AutoEquipLevel { None, PrimarySlots, AnySlot }

public sealed class PreviousSlot {
    [DefaultValue(true)] public bool mouse = true;
    [DefaultValue(true)] public bool mediumCore = true;
    [DefaultValue(false)] public bool overridePrevious = false;
    public Toggle<Materials> materials = new(true);
    public Toggle<PreviousDisplay> displayPrevious = new(true);

    public static bool Enabled => SmartPickup.Enabled && !UnloadedInventoryManagement.Value.previousSlot && SmartPickup.Value.previousSlot.Key > ItemPickupLevel.None;
    public static bool Mouse => Enabled && Value.mouse;
    public static bool MediumCore => Enabled && Value.mediumCore;
    public static PreviousSlot Value => SmartPickup.Value.previousSlot.Value;
}

public sealed class PreviousDisplay {
    public Toggle<FakeItemDisplay> fakeItem = new(true);
    public Toggle<IconDisplay> icon = new(true);
    
    public static bool Enabled => SmartPickup.Enabled && PreviousSlot.Value.displayPrevious;
    public static bool FakeItem => Enabled && Value.icon && !UnloadedInventoryManagement.Value.displayFakeItem;
    public static bool Icon => Enabled && Value.icon && !UnloadedInventoryManagement.Value.displayIcon;
    public static PreviousDisplay Value => PreviousSlot.Value.displayPrevious.Value;
}

public sealed class Materials {
    [Range(1, 100), DefaultValue(3)] public int maxDepth = 3;
    [Range(1, 9999), DefaultValue(250)] public int maxChecks = 250;
}

public interface IPreviousDisplay { Vector2 position { get; } float scale { get; } float intensity { get; } }
public sealed class FakeItemDisplay : IPreviousDisplay {
    [DefaultValue(typeof(Vector2), "0.5, 0.5")] public Vector2 position { get; set; } = new(0.5f, 0.5f);
    [DefaultValue(1f)] public float scale { get; set; } = 1f;
    [DefaultValue(0.33f)] public float intensity { get; set; } = 0.33f;
}
public sealed class IconDisplay : IPreviousDisplay {
    [DefaultValue(typeof(Vector2), "0.8, 0.8")] public Vector2 position { get; set; } = new(0.8f, 0.8f);
    [DefaultValue(0.4f)] public float scale { get; set; } = 0.4f;
    [DefaultValue(0.8f)] public float intensity { get; set; } = 0.8f;
}

public sealed class UpgradeItems {
    public UpgradeItems() => upgraders = [];

    [CustomModConfigItem(typeof(DictionaryValuesElement)), ValueWrapper(typeof(EntityDefinitionValueWrapper<,>))]
    public Dictionary<PickupUpgraderDefinition, bool> upgraders {
        get => _upgraders;
        set {
            foreach (ModPickupUpgrader upgrader in global::BetterInventory.InventoryManagement.SmartPickup.Upgraders) value.TryAdd(new(upgrader), true);
            _upgraders = value;
        }
    }
    [DefaultValue(true)] public bool importantOnly { get; set; } = true;

    public static bool Enabled => SmartPickup.Enabled && !UnloadedInventoryManagement.Value.upgradeItems && SmartPickup.Value.upgradeItems;
    public static UpgradeItems Value => SmartPickup.Value.upgradeItems.Value;
    
    private Dictionary<PickupUpgraderDefinition, bool> _upgraders = [];
}

public sealed class QuickMove {
    [DefaultValue(HotkeyMode.Hotbar)] public HotkeyMode hotkeyMode = HotkeyMode.Hotbar;
    [Range(0, 3600), DefaultValue(60*3)] public int resetTime = 60*3;
    [DefaultValue(true)] public bool returnToSlot = true;
    public NestedValue<HotkeyDisplayMode, DisplayedHotkeys> displayedHotkeys = new(HotkeyDisplayMode.All);
    [DefaultValue(false)] public bool tooltip = false;

    public static bool Enabled => InventoryManagement.Instance.quickMove;
    public static bool DisplayHotkeys => Value.displayedHotkeys != HotkeyDisplayMode.None && !UnloadedInventoryManagement.Value.quickMoveHotkeys;
    public static bool Highlight => DisplayHotkeys && Value.displayedHotkeys.Value.highlightIntensity != 0 && !UnloadedInventoryManagement.Value.quickMoveHighlight;
    public static QuickMove Value => InventoryManagement.Instance.quickMove.Value;

    // Compatibility version < v0.6
    [JsonProperty, DefaultValue(60 * 3)] private int chainTime { set => PortConfig.MoveMember(value != 60*3, _ => resetTime = value); }
    [JsonProperty, DefaultValue(false)] private bool showTooltip { set => PortConfig.MoveMember(value, _ => tooltip = value); }
    [JsonProperty] private NestedValue<HotkeyDisplayMode, DisplayedHotkeys> displayHotkeys { set => PortConfig.MoveMember(value is not null, _ => displayedHotkeys = value!); }
}

public enum HotkeyDisplayMode { None, Next, All }
public enum HotkeyMode { Hotbar, FromEnd, Reversed }

public sealed class DisplayedHotkeys {
    [DefaultValue(0.2f)] public float highlightIntensity = 0.2f;
}

public sealed class CraftStack {
    public NestedValue<MaxCraftAmount, MaxRounding> maxItems = new(999);
    [DefaultValue(true)] public bool repeat = true;
    [DefaultValue(false)] public bool invertClicks = false;
    [DefaultValue(false)] public bool tooltip = false;

    public static bool Enabled => !UnloadedInventoryManagement.Value.craftStack && InventoryManagement.Instance.craftStack;
    public static bool Tooltip => Enabled && Value.tooltip;
    public static CraftStack Value => InventoryManagement.Instance.craftStack.Value;

    // Compatibility version < v0.6
    [JsonProperty, DefaultValue(false)] private bool single { set => PortConfig.MoveMember(value, _ => repeat = !value); }
    [JsonProperty, DefaultValue(999)] private int maxAmount { set => PortConfig.MoveMember(value != 999, _ => maxItems.Key = value); }
}

public sealed class MaxCraftAmount : MultiChoice<int> {
    public MaxCraftAmount() : base() { }
    public MaxCraftAmount(int value) : base(value) { }

    [Choice, Range(1, 9999), DefaultValue(999)] public int amount = 999;
    [Choice] public Text? spicRequirement { get; set; }

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

    public static implicit operator MaxCraftAmount(int count) => new(count);
    public static MaxCraftAmount FromString(string s) => new(int.Parse(s));
}

public sealed class MaxRounding {
    [DefaultValue(true)] public bool above = true;
}

public sealed class BetterShiftClick {
    [DefaultValue(true)] public bool shiftRight = true;
    [DefaultValue(true)] public bool universalShift = true;

    public static bool Enabled => InventoryManagement.Instance.betterShiftClick;
    public static bool ShiftRight => !UnloadedInventoryManagement.Value.shiftRight && Value.shiftRight && Enabled;
    public static bool UniversalShift => !UnloadedInventoryManagement.Value.universalShift && Value.universalShift && Enabled;
    public static BetterShiftClick Value => InventoryManagement.Instance.betterShiftClick.Value;
}