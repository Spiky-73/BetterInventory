using System.ComponentModel;
using Terraria.ModLoader.Config;
using Terraria.UI;
using SpikysLib.Configs;

namespace BetterInventory.Configs;

public sealed class InventoryManagement : ModConfig {
    public Toggle<SmartConsumption> smartConsumption = new(true);
    public NestedValue<SmartPickupLevel, SmartPickup> smartPickup = new(SmartPickupLevel.AllItems);
    [DefaultValue(AutoEquipLevel.DefaultSlots)] public AutoEquipLevel autoEquip;
    
    [DefaultValue(true)] public bool favoriteInBanks;
    public Toggle<QuickMove> quickMove = new(true);
    public Toggle<CraftStack> craftStack = new(true);

    [DefaultValue(true)] public bool shiftRight;
    [DefaultValue(true)] public bool stackTrash;

    public static bool AutoEquip => !UnloadedInventoryManagement.Value.autoEquip && Instance.autoEquip != AutoEquipLevel.Off;
    public static bool FavoriteInBanks => !UnloadedInventoryManagement.Value.favoriteInBanks && Instance.favoriteInBanks;
    public static bool ShiftRight => !UnloadedInventoryManagement.Value.shiftRight && Instance.shiftRight;
    public static bool StackTrash => !UnloadedInventoryManagement.Value.stackTrash && Instance.stackTrash;

    public override void OnChanged() {
        Reflection.ItemSlot.canFavoriteAt.GetValue()[ItemSlot.Context.BankItem] = FavoriteInBanks;
    }

    public override ConfigScope Mode => ConfigScope.ClientSide;
    public static InventoryManagement Instance = null!;

}

public enum AutoEquipLevel { Off, DefaultSlots, AnySlot }
public enum SmartPickupLevel { Off, FavoriteOnly, AllItems }

public sealed class SmartConsumption {
    [DefaultValue(true)] public bool consumables = true;
    [DefaultValue(true)] public bool ammo = true;
    [DefaultValue(true)] public bool baits = true;
    [DefaultValue(true)] public bool paints = true;
    [DefaultValue(true)] public bool materials = true;

    public static bool Enabled => InventoryManagement.Instance.smartConsumption;
    public static bool Consumables => Enabled && Value.consumables;
    public static bool Ammo => Enabled && Value.ammo;
    public static bool Baits => Enabled && Value.baits && !UnloadedInventoryManagement.Value.baits;
    public static bool Paints => Enabled && Value.paints;
    public static bool Materials => Enabled && Value.materials && !UnloadedInventoryManagement.Value.materials;
    public static SmartConsumption Value => InventoryManagement.Instance.smartConsumption.Value;
}

public sealed class SmartPickup {
    // [DefaultValue(false)] public bool shiftClicks = false;
    [DefaultValue(true)] public bool mediumCore = true;
    [DefaultValue(0.33f)] public float markIntensity = 0.33f;

    public static bool Enabled(bool favorited = true) => !UnloadedInventoryManagement.Value.smartPickup && InventoryManagement.Instance.smartPickup.Parent >= (favorited ? SmartPickupLevel.FavoriteOnly : SmartPickupLevel.AllItems);
    public static bool MediumCore => Enabled() && Value.mediumCore;
    public static bool Marks => Enabled() && Value.markIntensity != 0 && !UnloadedInventoryManagement.Value.marks;
    public static SmartPickup Value => InventoryManagement.Instance.smartPickup.Value;
}


public sealed class QuickMove {
    [Range(0, 3600), DefaultValue(60*3)] public int chainTime = 60*3;
    [DefaultValue(true)] public bool returnToSlot = true;
    [DefaultValue(false)] public bool showTooltip = false;
    [DefaultValue(HotkeyMode.Default)] public HotkeyMode hotkeyMode = HotkeyMode.Default;
    public NestedValue<HotkeyDisplayMode, DisplayedHotkeys> displayHotkeys = new(HotkeyDisplayMode.All);

    public static bool Enabled => InventoryManagement.Instance.quickMove;
    public static bool DisplayHotkeys => Value.displayHotkeys != HotkeyDisplayMode.Off && !UnloadedInventoryManagement.Value.quickMoveHotkeys;
    public static bool Highlight => DisplayHotkeys && Value.displayHotkeys.Value.highlightIntensity != 0 && !UnloadedInventoryManagement.Value.quickMoveHighlight;
    public static QuickMove Value => InventoryManagement.Instance.quickMove.Value;
}

public enum HotkeyDisplayMode { Off, First, All }
public enum HotkeyMode { Default, FromEnd, Reversed }

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