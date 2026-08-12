using SpikysLib;
using SpikysLib.Constants;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace BetterInventory.BetterItemPickup;

public sealed class SmartPickup : ILoadable {

    public bool IsLoadingEnabled(Mod mod) => BetterInventoryConfig.BetterItemPickup;
    public void Load(Mod mod) {
        On_Item.FitsAmmoSlot += HookSkipAmmoSlots;
        On_Player.FindItem_int_ItemArray += HookFixFindItemCollection;
    }

    private static int HookFixFindItemCollection(On_Player.orig_FindItem_int_ItemArray orig, Player self, int type, Item[] collection) {
        for (int i = 0; i < collection.Length; i++) {
            // Terraria uses `inventory[i].stack` instead of `collection[i].stack`
            if (collection[i].stack > 0 && type == collection[i].type) return i;
        }
        return -1;
    }

    public void Unload() { }

    public static bool GetItem_SmartPickup(Player player, int plr, Item item, GetItemSettings settings) {
        // Mouse item
        if (player.HasItem(item.type, [Main.mouseItem])) {
            if (GetItem_FillIntoMouseSlot(player, plr, item, settings)) return true;
        }

        // Inventory Item
        int slot = -1; bool inVoidBag = false;
        if (!item.IsCurrency) slot = player.FindItemInInventoryOrOpenVoidBag(item.type, out inVoidBag);
        else {
            int currency = item.CurrencyType();
            if (player.inventory[InventorySlots.Ammo.Start..InventorySlots.Ammo.End].CountCurrency(currency) > 0) slot = InventorySlots.Ammo.Start;
            else if (player.CountCurrency(currency, false) > 0) slot = 0;
            else if (player.bank4.item.CountCurrency(currency) > 0) (slot, inVoidBag) = (0, true);
        }
        if (slot >= 0) {
            if (inVoidBag) {
                if (GetItem_VoidVault(player, plr, item, settings)) return true;
            } else if (slot < InventorySlots.Items.End) {
                if (GetItem_Inventory(player, plr, item, settings)) return true;
            } else {
                if (GetItem_Ammo(player, plr, item, settings)) return true;
            }
        }

        return false;
    }

    private static bool GetItem_FillIntoMouseSlot(Player player, int plr, Item item, GetItemSettings settings) {
        if (Main.mouseItem.type <= ItemID.None || Main.mouseItem.stack >= Main.mouseItem.maxStack || !Main.mouseItem.IsTheSameAs(item)) return false;
        if (!ItemLoader.TryStackItems(Main.mouseItem, item, out var numTransferred)) return false;

        if (item.IsACoin) SoundEngine.PlaySound(SoundID.CoinPickup);
        else SoundEngine.PlaySound(SoundID.Grab);
        Main.mouseItem.position = player.position;
        BetterItemPickup.HandlePickup(player, plr, Main.mouseItem, numTransferred, settings);
        settings.HandlePostAction(Main.mouseItem);
        return item.stack <= 0;
    }

    private static bool GetItem_VoidVault(Player player, int plr, Item item, GetItemSettings settings) {
        if (!settings.CanGoIntoVoidVault || !player.IsVoidVaultEnabled || !player.CanVoidVaultAccept(item)) return false;
        return player.GetItem_VoidVault(plr, player.bank4.item, item, settings, item);
    }

    private static bool GetItem_Ammo(Player player, int plr, Item item, GetItemSettings settings) {
        if (!item.FitsAmmoSlot()) return false;
        return player.FillAmmo(plr, item, settings).IsAir;
    }

    private static bool GetItem_Inventory(Player player, int plr, Item item, GetItemSettings settings) {
        _skipAmmo = true;
        item = BetterItemPickup.GetItem_Inner(player, plr, item, new(settings.LongText, settings.NoText, false, settings.StepAfterHandlingSlotNormally));
        _skipAmmo = false;
        return item.IsAir;
    }
    private static bool _skipAmmo;
    private static bool HookSkipAmmoSlots(On_Item.orig_FitsAmmoSlot orig, Item self) {
        if (_skipAmmo) return false;
        return orig(self);
    }
}