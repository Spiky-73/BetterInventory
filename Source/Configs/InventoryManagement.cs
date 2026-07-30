using System.ComponentModel;
using System.Runtime.Serialization;
using Terraria.ModLoader.Config;
using SpikysLib.Configs;
using Microsoft.Xna.Framework;
using SpikysLib.Configs.UI;
using System.Collections.Generic;
using BetterInventory.InventoryManagement;
using Newtonsoft.Json;
using Terraria.ID;
using Terraria;
using System;

namespace BetterInventory.Configs;

public sealed class InventoryManagement : ModConfig {
    public Toggle<SmartPickup> smartPickup = new(true);

    public static InventoryManagement Instance = null!;
    public static bool SmartPickup => Instance.smartPickup;

    // Compatibility version < v0.6
    [JsonProperty, DefaultValue(AutoEquipLevel.PreferredSlots)] private AutoEquipLevel autoEquip { set => ConfigHelper.MoveMember(value != AutoEquipLevel.PreferredSlots, _ => smartPickup.Value.autoEquip.Key = value); }

    public override ConfigScope Mode => ConfigScope.ClientSide;
}

public sealed class SmartPickup {
    [DefaultValue(true)] public bool refillMouse = true;
    public NestedValue<ItemPickupLevel, PreviousSlot> previousSlot = new(ItemPickupLevel.AllItems);
    public Toggle<QuickStackPickup> quickStack = new(true);
    [DefaultValue(AutoEquipLevel.PreferredSlots)] public NestedValue<AutoEquipLevel, AutoEquip> autoEquip = new(AutoEquipLevel.PreferredSlots);
    public Toggle<UpgradeItems> upgradeItems = new(true);
    [DefaultValue(false)] public bool voidBagFirst = false;

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
    [DefaultValue(true)] public bool autoLockItems = true;
    [DefaultValue(false)] public bool lockedTooltip = false;
    public HashSet<ItemDefinition> lockedItems = [];

    public static UpgradeItems Value => SmartPickup.Value.upgradeItems.Value;

    public bool IsLocked(ItemDefinition item) => lockedItems.Contains(item);
    public void Lock(ItemDefinition item){
        lockedItems.Add(item);
        InventoryManagement.Instance.SaveChanges();
    } 

    [OnDeserialized]
    private void OnDeserialized(StreamingContext context) {
        foreach (ModPickupUpgrader upgrader in PickupUpgraderLoader.Upgraders) upgraders.TryAdd(new(upgrader), true);
    }
}
