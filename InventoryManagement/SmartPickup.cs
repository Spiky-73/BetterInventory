using System;
using System.Collections.Generic;
using System.Linq;
using BetterInventory.DataStructures;
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
        On_Player.DropItems += HookMarkItemsOnDeath;
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
        if (mouse != ItemID.None && Main.mouseItem.type != mouse && inv[slot].type == mouse) {
            if(SmartPickupEnabled(fav)) Remark(mouse, type, fav);
            else Unmark(mouse);
        } else if (SmartPickupEnabled(fav) && type != ItemID.None && (inv[slot].type != type && Main.mouseItem.type == type || Config.shiftClicks && inv[slot].IsAir)) Mark(type, inv, context, slot, fav);
    }

    public static Item SmartGetItem(Player player, Item item, GetItemSettings settings) {
        if (player.whoAmI == Main.myPlayer && IsMarked(item.type)) {
            var mark = _marks[item.type];
            item.favorited |= _marksData[item.type];
            Unmark(item.type);

            JoinedList<Item> items = mark.items.Items(player);

            if (mark.slot >= items.Count) return item;
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
    public static void Remark(int oldType, int newType, bool? favorite = null) {
        if (newType == ItemID.None) Unmark(oldType);
        else if (IsMarked(oldType)) Mark(newType, _marks[oldType], favorite ?? _marksData[oldType]);
    }
    public static void Unmark(int type) {
        _marks.Remove(type);
        _marksData.Remove(type);
    }

    private void HookMarkItemsOnDeath(On_Player.orig_DropItems orig, Player self) {
        if(Level == Configs.InventoryManagement.SmartPickupLevel.Off || !Config.mediumCore){
            orig(self);
            return;
        }
        foreach (ModInventory modInventory in InventoryLoader.Inventories) {
            foreach (InventorySlots slots in modInventory.Slots) {
                foreach ((int c, Func<Player, ListIndices<Item>> s) in slots.Slots) {
                    ListIndices<Item> items = s(self);
                    for (int i = 0; i < items.Count; i++) {
                        if(!items[i].IsAir && SmartPickupEnabled(items[i].favorited)) Mark(items[i].type, (slots, i), items[i].favorited);
                    }
                }
            }
        }
        
        orig(self);
    }

    public static bool SmartPickupEnabled(bool favorited) => Level switch {
        Configs.InventoryManagement.SmartPickupLevel.AllItems => true,
        Configs.InventoryManagement.SmartPickupLevel.FavoriteOnly => favorited,
        Configs.InventoryManagement.SmartPickupLevel.Off or _ => false
    };

    private static readonly Dictionary<int, (InventorySlots items, int slot)> _marks = new();
    private static readonly Dictionary<int, bool> _marksData = new();
}