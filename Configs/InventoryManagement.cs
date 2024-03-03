using System.ComponentModel;
using Terraria.ModLoader.Config;
using Terraria.UI;

namespace BetterInventory.Configs;

public sealed class InventoryManagement : ModConfig {
    public Toggle<SmartConsumption> smartConsumption = new(true);
    public ChildValue<SmartPickupLevel, SmartPickup> smartPickup = new(SmartPickupLevel.AllItems);
    [DefaultValue(AutoEquipLevel.DefaultSlots)] public AutoEquipLevel autoEquip;
    
    [DefaultValue(true)] public bool favoriteInBanks;
    public Toggle<QuickMove> quickMove = new(true);
    public Toggle<StackClick> craftStack = new(true);

    [DefaultValue(true)] public bool shiftRight;
    [DefaultValue(true)] public bool stackTrash;

    public override void OnChanged() {
        Reflection.ItemSlot.canFavoriteAt.GetValue()[ItemSlot.Context.BankItem] = favoriteInBanks;
    }

    public enum AutoEquipLevel { Off, DefaultSlots, AnySlot }
    public enum SmartPickupLevel { Off, FavoriteOnly, AllItems }

    public override ConfigScope Mode => ConfigScope.ClientSide;
    public static InventoryManagement Instance = null!;

}

public sealed class SmartConsumption {
    [DefaultValue(true)] public bool consumables = true;
    [DefaultValue(true)] public bool ammo = true;
    // [DefaultValue(true)] public bool materials = true; // TODO implement
}

public sealed class SmartPickup {
    // [DefaultValue(false)] public bool shiftClicks = false;
    [DefaultValue(true)] public bool mediumCore = true;
    [DefaultValue(0.33f)] public float markIntensity = 0.33f;
}

public sealed class StackClick {
    // [DefaultValue(true)] public bool crafting = true;
    // [DefaultValue(true)] public bool shops = true;
    [DefaultValue(false)] public bool invertClicks = false;
    [Range(1, 9999), DefaultValue(9999)] public int maxAmount = 9999;
}

public sealed class QuickMove {
    [Range(0, 3600), DefaultValue(60*3)] public int chainTime = 60*3;
    [DefaultValue(true)] public bool returnToSlot = true;
    [DefaultValue(false)] public bool showTooltip = false;
    [DefaultValue(HotkeyMode.Default)] public HotkeyMode hotkeyMode = HotkeyMode.Default;
    public ChildValue<HotkeyDisplayMode, HotkeyDisplay> displayHotkeys = new(HotkeyDisplayMode.All);
    
    public enum HotkeyDisplayMode { Off, First, All }
    public enum HotkeyMode { Default, FromEnd, Reversed }
}
public sealed class HotkeyDisplay {
    [DefaultValue(0.2f)] public float highlightIntensity = 0.2f;
}