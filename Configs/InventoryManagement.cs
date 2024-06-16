using System.ComponentModel;
using Terraria.ModLoader.Config;
using Terraria.UI;
using SpikysLib.Configs;
using Microsoft.Xna.Framework;
using SpikysLib.Configs.UI;
using System.Collections.Generic;
using BetterInventory.InventoryManagement;

namespace BetterInventory.Configs;

public sealed class InventoryManagement : ModConfig {
    public Toggle<SmartConsumption> smartConsumption = new(true);
    public Toggle<SmartPickup> smartPickup = new(true);

    public Toggle<QuickMove> quickMove = new(true);

    public Toggle<CraftStack> craftStack = new(true);
    [DefaultValue(true)] public bool favoriteInBanks;
    [DefaultValue(true)] public bool shiftRight;
    [DefaultValue(true)] public bool stackTrash;

    public static bool FavoriteInBanks => !UnloadedInventoryManagement.Value.favoriteInBanks && Instance.favoriteInBanks;
    public static bool ShiftRight => !UnloadedInventoryManagement.Value.shiftRight && Instance.shiftRight;
    public static bool StackTrash => !UnloadedInventoryManagement.Value.stackTrash && Instance.stackTrash;

    public override void OnChanged() {
        Reflection.ItemSlot.canFavoriteAt.GetValue()[ItemSlot.Context.BankItem] = FavoriteInBanks;
    }

    public override ConfigScope Mode => ConfigScope.ClientSide;
    public static InventoryManagement Instance = null!;

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

public sealed class SmartPickup { // TODO port settings
    public NestedValue<ItemPickupLevel, PreviousSlot> previousSlot = new(ItemPickupLevel.AllItems);
    [DefaultValue(AutoEquipLevel.PrimarySlots)] public AutoEquipLevel autoEquip = AutoEquipLevel.PrimarySlots;
    public Toggle<AutoUpgrade> autoUpgrade = new(true);
    [DefaultValue(true)] public bool hotbarLast = true;
    [DefaultValue(true)] public bool fixSlot = true;

    public static bool Enabled => InventoryManagement.Instance.smartPickup.Parent;
    public static bool AutoEquip => !UnloadedInventoryManagement.Value.autoEquip && Enabled && Value.autoEquip > AutoEquipLevel.None;
    public static bool HotbarLast => !UnloadedInventoryManagement.Value.hotbarLast && Enabled && Value.hotbarLast;
    public static bool FixSlot => !UnloadedInventoryManagement.Value.fixSlot && Enabled && Value.fixSlot;
    public static SmartPickup Value => InventoryManagement.Instance.smartPickup.Value;
}
public enum AutoEquipLevel { None, PrimarySlots, AnySlot }

public sealed class PreviousSlot {
    [DefaultValue(true)] public bool mouse = true;
    [DefaultValue(true)] public bool mediumCore = true;
    [DefaultValue(false)] public bool overrideMarks = false;
    public Toggle<MarksDisplay> displayMarks = new(true); // TODO port settings

    public static bool Enabled => SmartPickup.Enabled && !UnloadedInventoryManagement.Value.previousSlot && SmartPickup.Value.previousSlot.Parent > ItemPickupLevel.None;
    public static bool Mouse => Enabled && Value.mouse;
    public static bool MediumCore => Enabled && Value.mediumCore;
    public static PreviousSlot Value => SmartPickup.Value.previousSlot.Value;
}
public enum ItemPickupLevel { None, ImportantItems, AllItems }

public sealed class MarksDisplay {
    public Toggle<FakeItemDisplay> fakeItem = new(true);
    public Toggle<IconDisplay> icon = new(true);
    
    public static bool Enabled => SmartPickup.Enabled && PreviousSlot.Value.displayMarks;
    public static bool FakeItem => Enabled && Value.icon && !UnloadedInventoryManagement.Value.marksFakeItem;
    public static bool Icon => Enabled && Value.icon && !UnloadedInventoryManagement.Value.marksIcon;
    public static MarksDisplay Value => PreviousSlot.Value.displayMarks.Value;
}

public interface IMarkDisplay { Vector2 position { get; } float scale { get; } float intensity { get; } }
public sealed class FakeItemDisplay : IMarkDisplay {
    [DefaultValue(typeof(Vector2), "0.5, 0.5")] public Vector2 position { get; set; } = new(0.5f, 0.5f);
    [DefaultValue(1f)] public float scale { get; set; } = 1f;
    [DefaultValue(0.33f)] public float intensity { get; set; } = 0.33f;
}
public sealed class IconDisplay : IMarkDisplay {
    [DefaultValue(typeof(Vector2), "0.8, 0.8")] public Vector2 position { get; set; } = new(0.8f, 0.8f);
    [DefaultValue(1f)] public float scale { get; set; } = 0.4f;
    [DefaultValue(1f)] public float intensity { get; set; } = 1f;
}

public sealed class AutoUpgrade {
    [CustomModConfigItem(typeof(DictionaryValuesElement))]
    public Dictionary<ModPickupUpgraderDefinition, bool> upgraders {
        get => _upgraders;
        set {
            foreach (ModPickupUpgrader upgrader in global::BetterInventory.InventoryManagement.SmartPickup.Upgraders) value.TryAdd(new(upgrader), true);
            _upgraders = value;
        }
    }
    [DefaultValue(true)] public bool importantOnly { get; set; } = true;

    public static bool Enabled => SmartPickup.Enabled && !UnloadedInventoryManagement.Value.autoUpgrade && SmartPickup.Value.autoUpgrade;
    public static AutoUpgrade Value => SmartPickup.Value.autoUpgrade.Value;
    
    private Dictionary<ModPickupUpgraderDefinition, bool> _upgraders = [];
}

public sealed class QuickMove {
    [Range(0, 3600), DefaultValue(60*3)] public int chainTime = 60*3;
    [DefaultValue(true)] public bool returnToSlot = true;
    [DefaultValue(false)] public bool tooltip = false;
    [DefaultValue(HotkeyMode.Hotbar)] public HotkeyMode hotkeyMode = HotkeyMode.Hotbar;
    public NestedValue<HotkeyDisplayMode, DisplayedHotkeys> displayHotkeys = new(HotkeyDisplayMode.All);

    public static bool Enabled => InventoryManagement.Instance.quickMove;
    public static bool DisplayHotkeys => Value.displayHotkeys != HotkeyDisplayMode.None && !UnloadedInventoryManagement.Value.quickMoveHotkeys;
    public static bool Highlight => DisplayHotkeys && Value.displayHotkeys.Value.highlightIntensity != 0 && !UnloadedInventoryManagement.Value.quickMoveHighlight;
    public static QuickMove Value => InventoryManagement.Instance.quickMove.Value;
}

public enum HotkeyDisplayMode { None, First, All }
public enum HotkeyMode { Hotbar, FromEnd, Reversed }

public sealed class DisplayedHotkeys {
    [DefaultValue(0.2f)] public float highlightIntensity = 0.2f;
}

public sealed class CraftStack {
    [DefaultValue(false)] public bool single = false;
    [DefaultValue(false)] public bool invertClicks = false;
    public MaxCraftAmount maxAmount = new(999);
    [DefaultValue(false)] public bool tooltip = false;

    public static bool Enabled => !UnloadedInventoryManagement.Value.craftStack && InventoryManagement.Instance.craftStack;
    public static bool Tooltip => Enabled && Value.tooltip;
    public static CraftStack Value => InventoryManagement.Instance.craftStack.Value;
}

public sealed class MaxCraftAmount : MultiChoice<int> {
    public MaxCraftAmount() : base() { }
    public MaxCraftAmount(int value) : base(value) { }

    [Choice, Range(1, 9999), DefaultValue(999)] public int amount = 999;
    [Choice] public Text? spic { get; set; }

    public override int Value {
        get => Choice == nameof(spic) ? 0 : amount;
        set {
            if (value == 0) Choice = nameof(spic);
            else {
                Choice = nameof(amount);
                amount = value;
            }
        }
    }

    public static implicit operator MaxCraftAmount(int count) => new(count);
    public static MaxCraftAmount FromString(string s) => new(int.Parse(s));
}