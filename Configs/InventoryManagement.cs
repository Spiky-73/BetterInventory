using System.ComponentModel;
using Terraria.ModLoader.Config;
using Terraria.UI;
using BetterInventory.Configs.UI;

namespace BetterInventory.Configs;

public sealed class InventoryManagement : ModConfig {
    public Toggle<SmartConsumption> smartConsumption = new(true);
    public ChildValue<SmartPickupLevel, SmartPickup> smartPickup = new(SmartPickupLevel.AllItems);
    [DefaultValue(AutoEquipLevel.DefaultSlots)] public AutoEquipLevel autoEquip;
    
    [DefaultValue(true)] public bool favoriteInBanks;
    public Toggle<QuickMove> quickMove = new(true);
    public Toggle<CraftStack> craftStack = new(true);

    [DefaultValue(true)] public bool shiftRight;
    [DefaultValue(true)] public bool stackTrash;

    public static bool AutoEquip => !Hooks.UnloadedInventoryManagement.autoEquip && Instance.autoEquip != AutoEquipLevel.Off;
    public static bool FavoriteInBanks => !Hooks.UnloadedInventoryManagement.favoriteInBanks && Instance.favoriteInBanks;
    public static bool ShiftRight => !Hooks.UnloadedInventoryManagement.shiftRight && Instance.shiftRight;
    public static bool ClickOverrides => ShiftRight || CraftStack.Enabled;
    public static bool StackTrash => !Hooks.UnloadedInventoryManagement.stackTrash && Instance.stackTrash;

    public override void OnChanged() {
        Reflection.ItemSlot.canFavoriteAt.GetValue()[ItemSlot.Context.BankItem] = favoriteInBanks;
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

    public static bool Consumables => InventoryManagement.Instance.smartConsumption && InventoryManagement.Instance.smartConsumption.Value.consumables;
    public static bool Ammo => InventoryManagement.Instance.smartConsumption && InventoryManagement.Instance.smartConsumption.Value.ammo;
    public static bool Baits => InventoryManagement.Instance.smartConsumption && InventoryManagement.Instance.smartConsumption.Value.baits && !Hooks.UnloadedInventoryManagement.baits;
    public static bool Paints => InventoryManagement.Instance.smartConsumption && InventoryManagement.Instance.smartConsumption.Value.paints;
    public static bool Materials => InventoryManagement.Instance.smartConsumption && InventoryManagement.Instance.smartConsumption.Value.materials && !Hooks.UnloadedInventoryManagement.materials;
}

public sealed class SmartPickup {
    // [DefaultValue(false)] public bool shiftClicks = false;
    [DefaultValue(true)] public bool mediumCore = true;
    [DefaultValue(0.33f)] public float markIntensity = 0.33f;

    public static bool Enabled(bool favorited = true) => !Hooks.UnloadedInventoryManagement.smartPickup && InventoryManagement.Instance.smartPickup.Parent >= (favorited ? SmartPickupLevel.FavoriteOnly : SmartPickupLevel.AllItems);
    public static bool MediumCore => Enabled() && Value.mediumCore;
    public static bool Marks => Enabled() && Value.markIntensity != 0 && !Hooks.UnloadedInventoryManagement.marks;
    public static SmartPickup Value => InventoryManagement.Instance.smartPickup.Value;
}


public sealed class QuickMove {
    [Range(0, 3600), DefaultValue(60*3)] public int chainTime = 60*3;
    [DefaultValue(true)] public bool returnToSlot = true;
    [DefaultValue(false)] public bool showTooltip = false;
    [DefaultValue(HotkeyMode.Default)] public HotkeyMode hotkeyMode = HotkeyMode.Default;
    public ChildValue<HotkeyDisplayMode, HotkeyDisplay> displayHotkeys = new(HotkeyDisplayMode.All);

    public static bool Enabled => InventoryManagement.Instance.quickMove;
    public static bool DisplayHotkeys => Value.displayHotkeys != HotkeyDisplayMode.Off && !Hooks.UnloadedInventoryManagement.quickMoveDisplay;
    public static bool Hightlight => DisplayHotkeys && Value.displayHotkeys.Value.highlightIntensity != 0 && !Hooks.UnloadedInventoryManagement.quickMoveHightlight;
    public static QuickMove Value => InventoryManagement.Instance.quickMove.Value;
}

public enum HotkeyDisplayMode { Off, First, All }
public enum HotkeyMode { Default, FromEnd, Reversed }

public sealed class HotkeyDisplay {
    [DefaultValue(0.2f)] public float highlightIntensity = 0.2f;
}

public sealed class CraftStack {
    // [DefaultValue(true)] public bool crafting = true;
    // [DefaultValue(true)] public bool shops = true;
    [DefaultValue(false)] public bool invertClicks = false;
    [Range(1, 9999), DefaultValue(9999)] public int maxAmount = 9999;

    public static bool Enabled => !Hooks.UnloadedInventoryManagement.craftStack && InventoryManagement.Instance.craftStack;
    public static CraftStack Value => InventoryManagement.Instance.craftStack.Value;
}