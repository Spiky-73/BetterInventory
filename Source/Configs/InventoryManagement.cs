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
    public NestedValue<ItemPickupLevel, PreviousSlot> previousSlot = new(ItemPickupLevel.AllItems);
    [DefaultValue(AutoEquipLevel.PreferredSlots)] public NestedValue<AutoEquipLevel, AutoEquip> autoEquip = new(AutoEquipLevel.PreferredSlots);
    public Toggle<UpgradeItems> upgradeItems = new(true);

    public static bool PreviousSlot => !UnloadedInventoryManagement.Value.pickupOverrideSlot && InventoryManagement.SmartPickup && Value.previousSlot > ItemPickupLevel.None;
    public static bool AutoEquip => !UnloadedInventoryManagement.Value.pickupDedicatedSlot && InventoryManagement.SmartPickup && Value.autoEquip > AutoEquipLevel.None;
    public static bool UpgradeItems => !UnloadedInventoryManagement.Value.pickupDedicatedSlot && InventoryManagement.SmartPickup && Value.upgradeItems;

    public static bool OverrideSlot => PreviousSlot;
    public static bool DedicatedSlot => AutoEquip || UpgradeItems;
    public static SmartPickup Value => InventoryManagement.Instance.smartPickup.Value;
}
public enum ItemPickupLevel { None, ImportantItems, AllItems }
public enum AutoEquipLevel { None, PreferredSlots, AnySlot }

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
