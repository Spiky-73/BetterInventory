using System.Collections.Generic;
using BetterInventory.DataStructures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace BetterInventory.InventoryManagement;

public sealed class SmartPickup : ILoadable {

    public static Configs.InventoryManagement.SmartPickupLevel Level => Configs.InventoryManagement.Instance.smartPickup;
    public static Configs.SmartPickup Config => Configs.InventoryManagement.Instance.smartPickup.Value;

    public void Load(Mod mod) {
        On_ItemSlot.LeftClick_ItemArray_int_int += HookLeftSaveType;
        On_ItemSlot.RightClick_ItemArray_int_int += HookRightSaveType;
        On_Player.DropItems += HookMarkItemsOnDeath;

        IL_ItemSlot.Draw_SpriteBatch_ItemArray_int_int_Vector2_Color += ILDrawMark;
    }
    public void Unload() { }

    private static void HookRightSaveType(On_ItemSlot.orig_RightClick_ItemArray_int_int orig, Item[] inv, int context, int slot) {
        (int type, int mouse, bool fav) = (inv[slot].type, Main.mouseItem.type, inv[slot].favorited);
        orig(inv, context, slot);
        if(Main.mouseRight) UpdateMark(inv, context, slot, type, mouse, fav);
    }

    private static void HookLeftSaveType(On_ItemSlot.orig_LeftClick_ItemArray_int_int orig, Item[] inv, int context, int slot) {
        (int type, int mouse, bool fav) = (inv[slot].type, Main.mouseItem.type, inv[slot].favorited);
        orig(inv, context, slot);
        if (Main.mouseLeft && Main.mouseLeftRelease) UpdateMark(inv, context, slot, type, mouse, fav);
    }

    public static void UpdateMark(Item[] inv, int context, int slot, int oldType, int oldMouse, bool oldFav) {
        if (inv[slot].type == oldType && Main.mouseItem.type == oldMouse) return;
        if (Main.mouseItem.type == oldMouse && !Config.shiftClicks) return;
        if (oldType == ItemID.None) Unmark(inv[slot].type);
        else if (inv[slot].type == ItemID.None) Mark(oldType, inv, context, slot, oldFav);
        else Remark(inv[slot].type, oldType, oldFav);
    }

    public static Item SmartGetItem(Player player, Item item, GetItemSettings settings) {
        if (player.whoAmI != Main.myPlayer || !IsMarked(item.type)) return item;

        while (_marks[item.type].Count > 0) {
            (Slot mark, bool favorited) = ConsumeMark(item.type);
            Joined<ListIndices<Item>, Item> items = mark.Inventory.Items(player);
            if (mark.Index >= items.Count) continue;
            if (!item.favorited && !favorited && items[mark.Index].favorited) continue;

            item.favorited |= favorited;
            (Item moved, items[mark.Index]) = (items[mark.Index], new());
            item = mark.GetItem(player, item, settings);
            moved = mark.GetItem(player, moved, settings);
            if (item.IsAir) return moved;
            player.GetDropItem(ref moved);
        }
        return item;
    }

    private void ILDrawMark(ILContext il) {
        ILCursor cursor = new(il);

        // ...
        // int num9 = context switch { ... };
        // if ((item.type <= 0 || item.stack <= 0) && ++[!<drawMark> && num9 != -1]) <drawSlotTexture>
        cursor.GotoNext(MoveType.After, i => i.MatchLdloc(11));
        cursor.EmitLdarg0();
        cursor.EmitLdarg1();
        cursor.EmitLdarg2();
        cursor.EmitLdarg3();
        cursor.EmitLdarg(4);
        cursor.EmitLdloc2();
        cursor.EmitLdloc(7);
        cursor.EmitLdloc(3);
        cursor.EmitDelegate((int num9, SpriteBatch spriteBatch, Item[] inv, int context, int slot, Vector2 position, float scale, Texture2D texture, Color color) => {
            if (Level == Configs.InventoryManagement.SmartPickupLevel.Off || Config.markIntensity == 0 || !QuickMove.IsInventorySlot(inv, context, slot, out Slot itemSlot)) return num9;
            if (!IsMarked(itemSlot)) return num9;
            float scale2 = ItemSlot.DrawItemIcon(_marksData[itemSlot].fake, context, spriteBatch, position + texture.Size() / 2f * scale, scale, 32f, color * Config.markIntensity * Main.cursorAlpha);
            return -1;
        });
    }


    public static bool IsMarked(int type) => _marks.TryGetValue(type, out var marks) && marks.Count > 0;
    public static bool IsMarked(Slot slot) => _marksData.ContainsKey(slot);

    public static void Mark(int type, Item[] inv, int context, int slot, bool favorited) {
        Slot? mark = InventoryLoader.GetInventorySlot(Main.LocalPlayer, inv, context, slot);
        if (mark is not null) Mark(type, mark.Value, favorited);
    }
    public static void Mark(int type, Slot slot, bool favorited) {
        // if (IsMarked(type)) Unmark(type);
        if (IsMarked(slot)) Unmark(slot);

        _marks.TryAdd(type, new());
        _marks[type].Add(slot);

        _marksData[slot] = (new(type), favorited);
    }
    public static void Unmark(int type) {
        if (!IsMarked(type)) return;
        List<Slot> marks = _marks[type];
        foreach (Slot mark in marks) _marksData.Remove(mark);
        _marks.Remove(type);
    }
    public static void Unmark(Slot slot) {
        if (!IsMarked(slot)) return;
        _marks[_marksData[slot].fake.type].Remove(slot);
        _marksData.Remove(slot);
    }
    public static void Remark(int oldType, int newType, bool? favorited = null) {
        if (newType == oldType || newType == ItemID.None || !IsMarked(oldType)) {
            Unmark(oldType);
            return;
        }
        List<Slot> marks = _marks[oldType];
        foreach (Slot mark in marks) _marksData[mark] = (new(newType), favorited ?? _marksData[mark].favorited);

        _marks[newType] = _marks[oldType];
        _marks.Remove(oldType);
    }
    public static (Slot slot, bool favorited) ConsumeMark(int type) {
        Slot mark = _marks[type][^1];
        bool favorited = _marksData[mark].favorited;
        Unmark(mark);
        return (mark, favorited);
    }

    private void HookMarkItemsOnDeath(On_Player.orig_DropItems orig, Player self) {
        if(Level == Configs.InventoryManagement.SmartPickupLevel.Off || !Config.mediumCore){
            orig(self);
            return;
        }

        foreach (ModSubInventory inventory in InventoryLoader.SubInventories) {
            IList<Item> items = inventory.Items(self);
            for (int i = 0; i < items.Count; i++) {
                if(!items[i].IsAir && SmartPickupEnabled(items[i].favorited)) Mark(items[i].type, new(inventory, i), items[i].favorited);
            }
        }
        orig(self);
    }

    public static bool SmartPickupEnabled(bool favorited) => Level switch {
        Configs.InventoryManagement.SmartPickupLevel.AllItems => true,
        Configs.InventoryManagement.SmartPickupLevel.FavoriteOnly => favorited,
        Configs.InventoryManagement.SmartPickupLevel.Off or _ => false
    };

    private static readonly Dictionary<Slot, (Item fake, bool favorited)> _marksData = new();
    private static readonly Dictionary<int, List<Slot>> _marks = new();
}