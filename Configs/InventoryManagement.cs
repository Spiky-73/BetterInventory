using System.ComponentModel;
using Terraria.ModLoader.Config;
using Terraria.UI;
using SpikysLib.Configs;
using Microsoft.Xna.Framework;

namespace BetterInventory.Configs;

public sealed class InventoryManagement : ModConfig {
    public Toggle<SmartConsumption> smartConsumption = new(true);
    public Toggle<SmartPickup> smartPickup = new(true); // TODO port settings
    public Toggle<AutoEquip> autoEquip = new(true); // TODO port settings
    
    [DefaultValue(true)] public bool favoriteInBanks;
    public Toggle<QuickMove> quickMove = new(true);
    public Toggle<CraftStack> craftStack = new(true);

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

public sealed class SmartPickup {
    [DefaultValue(MousePickupLevel.AllItems)] public MousePickupLevel mouse = MousePickupLevel.AllItems; // TODO port settings
    [DefaultValue(MousePickupLevel.AllItems)] public MousePickupLevel mediumCore = MousePickupLevel.AllItems;
    [DefaultValue(false)] public bool overrideMarks = false;
    public Toggle<MarksDisplay> displayMarks = new(true); // TODO port settings

    public static bool Enabled => !UnloadedInventoryManagement.Value.smartPickup && InventoryManagement.Instance.smartPickup.Parent;
    public static bool Mouse => Enabled && Value.mouse > MousePickupLevel.Off;
    public static bool MediumCore => Enabled && Value.mediumCore > MousePickupLevel.Off;
    public static SmartPickup Value => InventoryManagement.Instance.smartPickup.Value;
}
public enum MousePickupLevel { Off, FavoritedOnly, AllItems }

public sealed class MarksDisplay {
    public Toggle<FakeItemDisplay> fakeItem = new(true);
    public Toggle<IconDisplay> icon = new(true);
    
    public static bool Enabled => SmartPickup.Value.displayMarks;
    public static bool FakeItem => Enabled && Value.icon && !UnloadedInventoryManagement.Value.fakeItem;
    public static bool Icon => Enabled && Value.icon && !UnloadedInventoryManagement.Value.marksIcon;
    public static MarksDisplay Value => SmartPickup.Value.displayMarks.Value;
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

public sealed class AutoEquip {
    [DefaultValue(false)] public bool nonPrimary = false;

    public static bool Enabled => !UnloadedInventoryManagement.Value.autoEquip && InventoryManagement.Instance.autoEquip;
    public static AutoEquip Value => InventoryManagement.Instance.autoEquip.Value;
}

public sealed class QuickMove {
    [Range(0, 3600), DefaultValue(60*3)] public int chainTime = 60*3;
    [DefaultValue(true)] public bool returnToSlot = true;
    [DefaultValue(false)] public bool showTooltip = false;
    [DefaultValue(HotkeyMode.Hotbar)] public HotkeyMode hotkeyMode = HotkeyMode.Hotbar;
    public NestedValue<HotkeyDisplayMode, DisplayedHotkeys> displayHotkeys = new(HotkeyDisplayMode.All);

    public static bool Enabled => InventoryManagement.Instance.quickMove;
    public static bool DisplayHotkeys => Value.displayHotkeys != HotkeyDisplayMode.Off && !UnloadedInventoryManagement.Value.quickMoveHotkeys;
    public static bool Highlight => DisplayHotkeys && Value.displayHotkeys.Value.highlightIntensity != 0 && !UnloadedInventoryManagement.Value.quickMoveHighlight;
    public static QuickMove Value => InventoryManagement.Instance.quickMove.Value;
}

public enum HotkeyDisplayMode { Off, First, All }
public enum HotkeyMode { Hotbar, FromEnd, Reversed }

public sealed class DisplayedHotkeys {
    [DefaultValue(0.2f)] public float highlightIntensity = 0.2f;
}

public sealed class CraftStack {
    [DefaultValue(false)] public bool single = false;
    [DefaultValue(false)] public bool invertClicks = false;
    [Range(1, 9999), DefaultValue(999)] public int maxAmount = 999;

    public static bool Enabled => !UnloadedInventoryManagement.Value.craftStack && InventoryManagement.Instance.craftStack;
    public static CraftStack Value => InventoryManagement.Instance.craftStack.Value;
}