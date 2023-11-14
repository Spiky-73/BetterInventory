using System.ComponentModel;
using BetterInventory.InventoryManagement;
using Terraria;
using Terraria.ModLoader.Config;
using Terraria.UI;

namespace BetterInventory.Configs;

public sealed class InventoryManagement : ModConfig {
    [DefaultValue(true)] public bool smartConsumption;
    [DefaultValue(true)] public bool smartAmmo;
    [DefaultValue(SmartPickupLevel.AllItems)] public SmartPickupLevel smartPickup = SmartPickupLevel.AllItems;
    [DefaultValue(AutoEquipLevel.MainSlots)] public AutoEquipLevel autoEquip;
    [DefaultValue(true)] public bool favoriteInBanks;
    

    public Toggle<QuickMove> quickMove = new(true);
    [DefaultValue(true)] public bool fastRightClick; // TODO fast extractinator
    [DefaultValue(true)] public bool itemRightClick;

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


    public enum AutoEquipLevel { Off, MainSlots, AllSlots }
    public enum SmartPickupLevel { Off, FavoriteOnly, AllItems }

    public override ConfigScope Mode => ConfigScope.ClientSide;
    public static InventoryManagement Instance = null!;

}

public sealed class QuickMove {
    [Range(0, 3600), DefaultValue(60)] public int chainTime = 60;
    [DefaultValue(true)] public bool showTooltip = true;
    // [DefaultValue(false)] public bool highlightSlots = false; // TODO implement
    [DefaultValue(true)] public bool returnToSlot = true;
}