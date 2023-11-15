using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace BetterInventory.InventoryManagement;

public sealed class SmartPickup : ILoadable {

    public static Configs.InventoryManagement.SmartPickupLevel Level => Configs.InventoryManagement.Instance.smartPickup;
    public static Configs.SmartPickup Config => Configs.InventoryManagement.Instance.smartPickup.Value;

    public void Load(Mod mod) {
        On_ItemSlot.LeftClick_ItemArray_int_int += HookLeftClick;
        On_ItemSlot.RightClick_ItemArray_int_int += HookRightClick;
        // On_ChestUI.TryPlacingInChest += HookPlaceInChest;
    }
    public void Unload() { }

    private static void HookRightClick(On_ItemSlot.orig_RightClick_ItemArray_int_int orig, Item[] inv, int context, int slot) {
        int type = inv[slot].type;
        bool fav = inv[slot].favorited;
        orig(inv, context, slot);
        if (SmartPickupEnabled(fav) && type != ItemID.None && inv[slot].IsAir && Main.mouseItem.type == type) Mark(type, inv, context, slot, fav);
    }

    private static void HookLeftClick(On_ItemSlot.orig_LeftClick_ItemArray_int_int orig, Item[] inv, int context, int slot) {
        (int type, int mouse) = (inv[slot].type, Main.mouseItem.type);
        bool fav = inv[slot].favorited;
        orig(inv, context, slot);
        if (mouse != ItemID.None && Main.mouseItem.type != mouse && inv[slot].type == mouse) Unmark(mouse, type, fav);
        else if (SmartPickupEnabled(fav) && type != ItemID.None && (inv[slot].type != type && Main.mouseItem.type == type || Config.shiftClicks && inv[slot].IsAir)) Mark(type, inv, context, slot, fav);
    }

    public static Item SmartGetItem(Player player, Item item, GetItemSettings settings) {
        if (player.whoAmI == Main.myPlayer && IsMarked(item.type)) {
            var mark = _marks[item.type];
            item.favorited |= _marksData[item.type];
            Unmark(item.type);

            DataStructures.JoinedList<Item> items = mark.items.Items(player);
            
            if (item.favorited || !items[mark.slot].favorited) {
                (Item moved, items[mark.slot]) = (items[mark.slot], new());
                item = mark.items.GetItem(player, item, settings, mark.slot);
                moved = mark.items.Inventory.GetItem(player, moved, settings);
                if (item.IsAir) return moved;
                player.GetDropItem(ref moved);
                return item;
            }
            return mark.items.GetItem(player, item, settings, mark.slot);
        }
        return item;
    }

    public static bool IsMarked(int type) => _marks.ContainsKey(type);

    public static void Mark(int type, Item[] inv, int context, int slot, bool favorited) {
        (InventorySlots? slots, int index) = InventoryLoader.GetInventorySlot(Main.LocalPlayer, inv, context, slot);
        if (slots is not null) Mark(type, (slots, index), favorited);
    }
    public static void Mark(int type, (InventorySlots items, int slot) mark, bool favorited) {
        Unmark(_marks.FirstOrDefault(kvp => kvp.Value == mark).Key);
        _marks[type] = mark;
        _marksData[type] = favorited;
    }
    public static void Unmark(int oldType, int newType, bool? favorite = null) {
        if (newType == ItemID.None) Unmark(oldType);
        else if (IsMarked(oldType)) Mark(newType, _marks[oldType], favorite ?? _marksData[oldType]);
    }
    public static void Unmark(int type) {
        _marks.Remove(type);
        _marksData.Remove(type);
    }

    // private bool HookPlaceInChest(On_ChestUI.orig_TryPlacingInChest orig, Item I, bool justCheck, int itemSlotContext) {
    //     ChestUI.GetContainerUsageInfo(out var sync, out Item[]? chest);
    //     if (ChestUI.IsBlockedFromTransferIntoChest(I, chest) || ! Configs.ClientConfig.SmartPickupEnabled(I)) return orig(I, justCheck, itemSlotContext);
    //     BetterPlayer betterPlayer = Main.LocalPlayer.GetModPlayer<BetterPlayer>();
    //     bool gotItems = false;
    //     int slot;
    //     if ((slot = Array.IndexOf(betterPlayer._lastTypeChest, I.type)) != -1 /* && chest[slot] != I */) {
    //         if (justCheck) return true;
    //         if (chest[slot].type == ItemID.None) {
    //             Item i = I.Clone();
    //             gotItems = (bool)FillEmptVoidMethod.Invoke(Main.LocalPlayer, new object[] { Main.myPlayer, chest, i, GetItemSettings.InventoryUIToInventorySettings, i, slot })!;
    //         } else if (chest[slot].type == I.type && I.maxStack > 1) gotItems = (bool)FillOccupiedVoidMethod.Invoke(Main.LocalPlayer, new object[] { Main.myPlayer, chest, I, GetItemSettings.InventoryUIToInventorySettings, I, slot })!;
    //         else if (I.favorited || !chest[slot].favorited) (chest[slot], I) = (I, chest[slot]);
    //         if (sync) NetMessage.SendData(MessageID.SyncChestItem, number: Main.LocalPlayer.chest, number2: slot);
    //     }
    //     if (gotItems) I.TurnToAir();
    //     return gotItems || orig(I, justCheck, itemSlotContext);
    // }

    public static bool SmartPickupEnabled(bool favorited) => Level switch {
        Configs.InventoryManagement.SmartPickupLevel.AllItems => true,
        Configs.InventoryManagement.SmartPickupLevel.FavoriteOnly => favorited,
        Configs.InventoryManagement.SmartPickupLevel.Off or _ => false
    };

    private static readonly Dictionary<int, (InventorySlots items, int slot)> _marks = new();
    private static readonly Dictionary<int, bool> _marksData = new();
}