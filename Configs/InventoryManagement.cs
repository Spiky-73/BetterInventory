using System.ComponentModel;
using BetterInventory.InventoryManagement;
using Terraria;
using Terraria.ModLoader.Config;
using Terraria.UI;

namespace BetterInventory.Configs;

public sealed class InventoryManagement : ModConfig {
    [DefaultValue(true)] public bool smartConsumption;
    [DefaultValue(true)] public bool smartAmmo;
    public ChildValue<SmartPickupLevel, SmartPickup> smartPickup = new(SmartPickupLevel.AllItems);
    [DefaultValue(AutoEquipLevel.DefaultSlots)] public AutoEquipLevel autoEquip;
    
    [DefaultValue(true)] public bool favoriteInBanks;
    [DefaultValue(true)] public bool fastContainerOpening;
    [DefaultValue(true)] public bool itemRightClick;
    public Toggle<ClickOverride> clickOverrides = new(true);

    public Toggle<QuickMove> quickMove = new(true);
    [DefaultValue(true)] public bool builderKeys; // TODO check new api
    public string FavoriteBuffKeybind {
        get {
            var keys = BetterPlayer.FavoritedBuffKb?.GetAssignedKeys() ?? new();
            return keys.Count == 0 ? Lang.inter[23].Value : keys[0];
        }
    }

    public override void OnChanged() {
        Reflection.ItemSlot.canFavoriteAt.GetValue()[ItemSlot.Context.BankItem] = favoriteInBanks;
    }

    public enum AutoEquipLevel { Off, DefaultSlots, AnySlot }
    public enum SmartPickupLevel { Off, FavoriteOnly, AllItems }

    public override ConfigScope Mode => ConfigScope.ClientSide;
    public static InventoryManagement Instance = null!;

}

public sealed class ClickOverride {
    [DefaultValue(true)] public bool crafting = true;
    [DefaultValue(true)] public bool shops = true;
    [DefaultValue(true)] public bool shiftRight = true;
    [DefaultValue(true)] public bool stacking = true;
    [DefaultValue(false)] public bool invertClicks = false;
}

public sealed class SmartPickup {
    [DefaultValue(false)] public bool shiftClicks = false;
    [DefaultValue(true)] public bool mediumCore = true;
    [DefaultValue(0.33f)] public float markIntensity = 0.33f;
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