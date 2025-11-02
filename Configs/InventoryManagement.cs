using System.ComponentModel;
using System.Runtime.Serialization;
using Terraria.ModLoader.Config;
using Terraria.UI;
using SpikysLib.Configs;
using Microsoft.Xna.Framework;
using SpikysLib.Configs.UI;
using System.Collections.Generic;
using BetterInventory.InventoryManagement;
using Newtonsoft.Json;
using Terraria.ID;
using Terraria;

namespace BetterInventory.Configs;

public sealed class InventoryManagement : ModConfig {
    public Toggle<SmartConsumption> smartConsumption = new(true);
    public Toggle<SmartPickup> smartPickup = new(true);
    public Toggle<QuickMove> quickMove = new(true);
    public Toggle<CraftStack> craftStack = new(true);
    [DefaultValue(true)] public bool favoriteInBanks;
    public Toggle<BetterShiftClick> betterShiftClick = new(true);
    public Toggle<BetterTrash> betterTrash = new(true);
    [DefaultValue(true)] public bool depositClick;
    public Toggle<BetterQuickStack> betterQuickStack = new(true);
    [DefaultValue(true)] public bool inventorySlotsTexture = true;

    public static InventoryManagement Instance = null!;
    public static bool FavoriteInBanks => !UnloadedInventoryManagement.Value.favoriteInBanks && Instance.favoriteInBanks;
    public static bool DepositClick => Instance.depositClick;
    public static bool InventorySlotsTexture => !UnloadedInventoryManagement.Value.inventorySlotsTexture && Instance.inventorySlotsTexture;
    public static bool SmartPickup => Instance.smartPickup;

    // Compatibility version < v0.6
    [JsonProperty, DefaultValue(AutoEquipLevel.PreferredSlots)] private AutoEquipLevel autoEquip { set => ConfigHelper.MoveMember(value != AutoEquipLevel.PreferredSlots, _ => smartPickup.Value.autoEquip.Key = value); }
    [JsonProperty, DefaultValue(true)] private bool shiftRight { set => ConfigHelper.MoveMember<InventoryManagement>(!value, c => c.betterShiftClick.Key = value); }

    // Compatibility version < v0.9
    [JsonProperty, DefaultValue(true)] private bool stackTrash { set => ConfigHelper.MoveMember<InventoryManagement>(!value, c => c.betterTrash.Key = value); }

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
    [DefaultValue(false)] public bool self = false;

    public static bool Enabled => InventoryManagement.Instance.smartConsumption;
    public static bool Consumables => Enabled && Value.consumables;
    public static bool Ammo => Enabled && Value.ammo;
    public static bool Baits => Enabled && Value.baits && !UnloadedInventoryManagement.Value.baits;
    public static bool Paints => Enabled && Value.paints;
    public static bool Materials => Enabled && Value.materials && !UnloadedInventoryManagement.Value.materials;
    public static AllowedItems AllowedItems => (Value.mouse ? AllowedItems.Mouse : AllowedItems.None) | (Value.self ? AllowedItems.Self : AllowedItems.None);
    public static SmartConsumption Value => InventoryManagement.Instance.smartConsumption.Value;
}

public sealed class SmartPickup {
    [DefaultValue(true)] public bool refillMouse = true;
    public NestedValue<ItemPickupLevel, PreviousSlot> previousSlot = new(ItemPickupLevel.AllItems);
    public Toggle<QuickStackPickup> quickStack = new(true);
    [DefaultValue(AutoEquipLevel.PreferredSlots)] public NestedValue<AutoEquipLevel, AutoEquip> autoEquip = new(AutoEquipLevel.PreferredSlots);
    public Toggle<UpgradeItems> upgradeItems = new(true);
    [DefaultValue(false)] public bool voidBagFirst = false;
    [DefaultValue(true)] public bool hotbarLast = true;
    [DefaultValue(true)] public bool fixSlot = true;
    [DefaultValue(true)] public bool fixAmmo = true;

    // Compatibility version < v0.9
    [JsonProperty, DefaultValue(VoidBagLevel.IfInside)] private VoidBagLevel voidBag { set => ConfigHelper.MoveMember<InventoryManagement>(value != VoidBagLevel.IfInside, c => {
        c.smartPickup.Value.voidBagFirst = value == VoidBagLevel.Always;
        c.smartPickup.Value.quickStack.Key = value != VoidBagLevel.None;
    }); }


    public static bool RefillMouse => !UnloadedInventoryManagement.Value.pickupOverrideSlot && InventoryManagement.SmartPickup && Value.refillMouse;
    public static bool PreviousSlot => !UnloadedInventoryManagement.Value.pickupOverrideSlot && InventoryManagement.SmartPickup && Value.previousSlot > ItemPickupLevel.None;
    public static bool QuickStack => !UnloadedInventoryManagement.Value.pickupDedicatedSlot && InventoryManagement.SmartPickup && Value.quickStack;
    public static bool AutoEquip => !UnloadedInventoryManagement.Value.pickupDedicatedSlot && InventoryManagement.SmartPickup && Value.autoEquip > AutoEquipLevel.None;
    public static bool UpgradeItems => !UnloadedInventoryManagement.Value.pickupDedicatedSlot && InventoryManagement.SmartPickup && Value.upgradeItems;
    public static bool VoidBagFirst => !UnloadedInventoryManagement.Value.pickupDedicatedSlot && InventoryManagement.SmartPickup && Value.voidBagFirst;
    public static bool HotbarLast => !UnloadedInventoryManagement.Value.hotbarLast && InventoryManagement.SmartPickup && Value.hotbarLast;
    public static bool FixSlot => !UnloadedInventoryManagement.Value.fixSlot && InventoryManagement.SmartPickup && Value.fixSlot;
    public static bool FixAmmo => !UnloadedInventoryManagement.Value.pickupDedicatedSlot && InventoryManagement.SmartPickup && Value.fixAmmo;

    public static bool OverrideSlot => RefillMouse || PreviousSlot;
    public static bool DedicatedSlot => QuickStack || AutoEquip || UpgradeItems || VoidBagFirst;
    public static SmartPickup Value => InventoryManagement.Instance.smartPickup.Value;

    // Compatibility version < v0.6
    [JsonProperty, DefaultValue(true)] private bool mediumCore { set => ConfigHelper.MoveMember(!value, _ => previousSlot.Value.mediumCore = value); }
    [JsonProperty, DefaultValue(0.33f)] private float markIntensity { set => ConfigHelper.MoveMember(value != 0.33f, _ => {
        if (value == 0) previousSlot.Value.displayPrevious.Key = false;
        else previousSlot.Value.displayPrevious.Value.fakeItem.Value.intensity = value;
    }); }
}
public enum ItemPickupLevel { None, ImportantItems, AllItems }
public enum AutoEquipLevel { None, PreferredSlots, AnySlot }
public enum VoidBagLevel { None, IfInside, Always }

public sealed class PreviousSlot {
    [DefaultValue(true)] public bool mouse = true;
    [DefaultValue(true)] public bool shiftClick = true;
    [DefaultValue(true)] public bool consumption = true;
    [DefaultValue(true)] public bool mediumCore = true;
    [DefaultValue(false)] public bool overridePrevious = false;
    [DefaultValue(MovePolicy.NotFavorited)] public MovePolicy movePolicy = MovePolicy.NotFavorited;
    public Toggle<Materials> materials = new(true); // TODO refactor
    public Toggle<PreviousDisplay> displayPrevious = new(true);

    public static bool Mouse => SmartPickup.PreviousSlot && Value.mouse;
    public static bool ShiftClick => SmartPickup.PreviousSlot && Value.shiftClick;
    public static bool Consumption => SmartPickup.PreviousSlot && Value.consumption;
    public static bool MediumCore => SmartPickup.PreviousSlot && Value.mediumCore;
    public static PreviousSlot Value => SmartPickup.Value.previousSlot.Value;
}

public enum MovePolicy { Never, NotFavorited, Always }

public sealed class QuickStackPickup {
    [DefaultValue(true)] public bool chests = true;
    [DefaultValue(true)] public bool voidBag = true;

    public static QuickStackPickup Value => SmartPickup.Value.quickStack.Value;
    public static bool Chest => Value.chests && (Main.netMode != NetmodeID.MultiplayerClient || !UnloadedInventoryManagement.Value.pickupQuickStackChestsMulti);
}

public sealed class PreviousDisplay {
    public Toggle<FakeItemDisplay> fakeItem = new(true);
    public Toggle<IconDisplay> icon = new(true, new());

    public static bool Enabled => InventoryManagement.SmartPickup && PreviousSlot.Value.displayPrevious;
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

public sealed class AutoEquip {
    [DefaultValue(false)] public bool inactiveInventories = false;
}

public sealed class UpgradeItems {
    [CustomModConfigItem(typeof(DictionaryValuesElement))] public Dictionary<PickupUpgraderDefinition, bool> upgraders = [];
    [DefaultValue(true)] public bool importantOnly = true;

    public static UpgradeItems Value => SmartPickup.Value.upgradeItems.Value;

    [OnDeserialized]
    private void OnDeserialized(StreamingContext context) {
        foreach (ModPickupUpgrader upgrader in PickupUpgraderLoader.Upgraders) upgraders.TryAdd(new(upgrader), true);
    }
}

public sealed class QuickMove {
    [DefaultValue(HotkeyMode.Hotbar)] public HotkeyMode hotkeyMode = HotkeyMode.Hotbar;
    [Range(0, 3600), DefaultValue(60 * 3)] public int resetTime = 60 * 3;
    [DefaultValue(true)] public bool returnToSlot = true;
    public NestedValue<HotkeyDisplayMode, DisplayedHotkeys> displayedHotkeys = new(HotkeyDisplayMode.All);
    [DefaultValue(true)] public bool followItem = true;
    [DefaultValue(false)] public bool inactiveInventories = false;
    [DefaultValue(false)] public bool tooltip = false;
    [DefaultValue(true)] public bool bringItem = true;

    public static bool Enabled => InventoryManagement.Instance.quickMove;
    public static bool InactiveInventories => Value.inactiveInventories;
    public static bool FollowItem => Value.followItem;
    public static bool DisplayHotkeys => Value.displayedHotkeys != HotkeyDisplayMode.None && !UnloadedInventoryManagement.Value.quickMoveHotkeys;
    public static bool Highlight => DisplayHotkeys && Value.displayedHotkeys.Value.highlightIntensity != 0 && !UnloadedInventoryManagement.Value.quickMoveHighlight;
    public static QuickMove Value => InventoryManagement.Instance.quickMove.Value;

    // Compatibility version < v0.6
    [JsonProperty, DefaultValue(60 * 3)] private int chainTime { set => ConfigHelper.MoveMember(value != 60 * 3, _ => resetTime = value); }
    [JsonProperty, DefaultValue(false)] private bool showTooltip { set => ConfigHelper.MoveMember(value, _ => tooltip = value); }
    [JsonProperty] private NestedValue<HotkeyDisplayMode, DisplayedHotkeys> displayHotkeys { set => ConfigHelper.MoveMember(value is not null, _ => displayedHotkeys = value!); }
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
    [DefaultValue(true)] public bool tooltip = true;

    public static bool Enabled => !UnloadedInventoryManagement.Value.craftStack && InventoryManagement.Instance.craftStack;
    public static bool Tooltip => Enabled && Value.tooltip;
    public static CraftStack Value => InventoryManagement.Instance.craftStack.Value;

    // Compatibility version < v0.6
    [JsonProperty, DefaultValue(false)] private bool single { set => ConfigHelper.MoveMember(value, _ => repeat = !value); }
    [JsonProperty, DefaultValue(999)] private int maxAmount { set => ConfigHelper.MoveMember(value != 999, _ => maxItems.Key = value); }
}

public sealed class MaxCraftAmount : MultiChoice<int> {
    public MaxCraftAmount() : base() { }
    public MaxCraftAmount(int value) : base(value) { }

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

public sealed class BetterTrash {
    [DefaultValue(true)] public bool stackTrash = true;
    [DefaultValue(true)] public bool trashTrash = true;

    public static bool Enabled => InventoryManagement.Instance.betterTrash;
    public static bool StackTrash => !UnloadedInventoryManagement.Value.stackTrash && Enabled && Value.stackTrash;
    public static bool TrashTrash => Enabled && Value.trashTrash;
    public static BetterTrash Value => InventoryManagement.Instance.betterTrash.Value;
}

public sealed class BetterQuickStack {
    [DefaultValue(true)] public bool completeQuickStack = true;
    [DefaultValue(true)] public bool limitedBanksQuickStack = true;

    public static bool Enabled => InventoryManagement.Instance.betterQuickStack;
    public static bool CompleteQuickStack => !UnloadedInventoryManagement.Value.quickStackComplete && Enabled && Value.completeQuickStack;
    public static bool LimitedBanksQuickStack => !UnloadedInventoryManagement.Value.quickStackLimitedBanks && Enabled && Value.limitedBanksQuickStack;
    public static BetterQuickStack Value => InventoryManagement.Instance.betterQuickStack.Value;
}
