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
    public static bool Enabled(bool favorited) => Level switch {
        Configs.InventoryManagement.SmartPickupLevel.AllItems => true,
        Configs.InventoryManagement.SmartPickupLevel.FavoriteOnly => favorited,
        Configs.InventoryManagement.SmartPickupLevel.Off or _ => false
    };
    public static Configs.SmartPickup Config => Configs.InventoryManagement.Instance.smartPickup.Value;

    public void Load(Mod mod) {
        On_ItemSlot.LeftClick_ItemArray_int_int += HookLeftSaveType;
        On_ItemSlot.RightClick_ItemArray_int_int += HookRightSaveType;
        On_Player.DropItems += HookMarkItemsOnDeath;
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
        // if (Main.mouseItem.type == oldMouse && (inv[slot].type == oldType || !Config.shiftClicks)) return;
        if (Main.mouseItem.type == oldMouse) return;
        if (oldType == ItemID.None) Unmark(inv[slot].type);
        else if (inv[slot].type == ItemID.None) Mark(oldType, inv, context, slot, oldFav);
        else Remark(inv[slot].type, oldType, oldFav);
    }
    private static void HookMarkItemsOnDeath(On_Player.orig_DropItems orig, Player self) {
        if(Level == Configs.InventoryManagement.SmartPickupLevel.Off || !Config.mediumCore){
            orig(self);
            return;
        }

        foreach (ModSubInventory inventory in InventoryLoader.SubInventories) {
            IList<Item> items = inventory.Items(self);
            for (int i = 0; i < items.Count; i++) {
                if(!items[i].IsAir && Enabled(items[i].favorited)) Mark(items[i].type, new(inventory, i), items[i].favorited);
            }
        }
        orig(self);
    }
    
    internal static void ILSmartPickup(ILContext il) {
        ILCursor cursor = new(il);

        // ...
        // if (newItem.uniqueStack && this.HasItem(newItem.type)) return item;
        cursor.GotoNext(i => i.MatchCall(Reflection.Player.HasItem));
        cursor.GotoNext(MoveType.AfterLabel, i => i.MatchLdloc0()); // bool isACoin

        // ++ item = <smartPickup>
        cursor.EmitLdarg0();
        cursor.EmitLdarg2();
        cursor.EmitLdarg3();
        cursor.EmitDelegate((Player self, Item newItem, GetItemSettings settings) => {
            if (VanillaGetItem || Level == Configs.InventoryManagement.SmartPickupLevel.Off) return newItem;
            else return SmartGetItem(self, newItem, settings);
        });
        cursor.EmitDup();
        cursor.EmitStarg(2);

        // ++if (newItem.IsAir) return new()
        cursor.EmitDelegate((Item item) => item.IsAir);
        ILLabel skip = cursor.DefineLabel();
        cursor.EmitBrfalse(skip);
        cursor.EmitDelegate(() => new Item());
        cursor.EmitRet();
        cursor.MarkLabel(skip);
    }
    internal static void ILDrawMarks(ILContext il) {
        ILCursor cursor = new(il);

        // ...
        // int num9 = context switch { ... };
        // if ((item.type <= 0 || item.stack <= 0) && ++[!<drawMark>] && num9 != -1) <drawSlotTexture>
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
            if (Level == Configs.InventoryManagement.SmartPickupLevel.Off || Config.markIntensity == 0 || !InventoryLoader.IsInventorySlot(Main.LocalPlayer, inv, context, slot, out Slot itemSlot)) return num9;
            if (!IsMarked(itemSlot)) return num9;
            float scale2 = ItemSlot.DrawItemIcon(s_marksData[itemSlot].fake, context, spriteBatch, position + texture.Size() / 2f * scale, scale, 32f, color * Config.markIntensity * Main.cursorAlpha);
            return -1;
        });
    }

    internal static void ILAutoEquip(ILContext il) {
        ILCursor cursor = new(il);

        // if (isACoin) ...
        // if (item.FitsAmmoSlot()) ...
        // for(...) ...
        cursor.GotoNext(i => i.MatchCall(Reflection.Player.GetItem_FillEmptyInventorySlot));
        cursor.GotoPrev(MoveType.AfterLabel, i => i.MatchLdloc0());

        // ++<autoEquip>
        cursor.EmitLdarg0();
        cursor.EmitLdarg2();
        cursor.EmitLdarg3();
        cursor.EmitDelegate((Player self, Item newItem, GetItemSettings settings) => {
            if (VanillaGetItem || settings.NoText || Configs.InventoryManagement.Instance.autoEquip == Configs.InventoryManagement.AutoEquipLevel.Off) return newItem;
            return AutoEquip(self, newItem, settings);
        });
        cursor.EmitDup();
        cursor.EmitStarg(2);

        // ++if (newItem.IsAir) return new()
        cursor.EmitDelegate((Item item) => item.IsAir);
        ILLabel skip = cursor.DefineLabel();
        cursor.EmitBrfalse(skip);
        cursor.EmitDelegate(() => new Item());
        cursor.EmitRet();
        cursor.MarkLabel(skip);
    }

    public static Item GetItem_Inner(Player self, int plr, Item newItem, GetItemSettings settings) {
        VanillaGetItem = true;
        Item i = self.GetItem(plr, newItem, settings);
        VanillaGetItem = false;
        return i;
    }

    public static Item SmartGetItem(Player player, Item item, GetItemSettings settings) {
        if (player.whoAmI != Main.myPlayer || !IsMarked(item.type)) return item;

        List<Slot> slots = new();
        while (s_marks[item.type].Count > 0) {
            (Slot mark, bool favorited) = ConsumeMark(item.type);
            Joined<ListIndices<Item>, Item> items = mark.Inventory.Items(player);
            if (mark.Index >= items.Count) continue;
            if (!item.favorited && !favorited && items[mark.Index].favorited) continue;

            item.favorited |= favorited;
            (Item moved, items[mark.Index]) = (items[mark.Index], new());
            Item toMove = item.Clone();
            toMove.stack = 1;
            if (mark.GetItem(player, toMove, settings).IsAir) item.stack--;
            moved = mark.GetItem(player, moved, settings);
            if (item.IsAir) return moved;
            slots.Add(mark);
            player.GetDropItem(ref moved);
        }
        foreach (Slot slot in slots) {
            item = slot.GetItem(player, item, settings);
            if (item.IsAir) return item;
        }
        return item;
    }
    public static Item AutoEquip(Player self, Item newItem, GetItemSettings settings) {
        foreach (ModSubInventory slots in InventoryLoader.GetSubInventories(newItem, Configs.InventoryManagement.Instance.autoEquip == Configs.InventoryManagement.AutoEquipLevel.DefaultSlots ? SubInventoryType.Default : SubInventoryType.Secondary)) {
            newItem = slots.GetItem(self, newItem, settings);
            if (newItem.IsAir) return newItem;
        }
        return newItem;
    }

    public static bool IsMarked(int type) => s_marks.TryGetValue(type, out var marks) && marks.Count > 0;
    public static bool IsMarked(Slot slot) => s_marksData.ContainsKey(slot);
    public static void Mark(int type, Item[] inv, int context, int slot, bool favorited) {
        if (InventoryLoader.IsInventorySlot(Main.LocalPlayer, inv, context, slot, out Slot mark)) Mark(type, mark, favorited);
    }
    public static void Mark(int type, Slot slot, bool favorited) {
        if (IsMarked(slot)) Unmark(slot);
        s_marks.TryAdd(type, new());
        s_marks[type].Add(slot);
        s_marksData[slot] = (new(type), favorited);
    }
    public static void Unmark(int type) {
        if (!IsMarked(type)) return;
        foreach (Slot mark in s_marks[type]) s_marksData.Remove(mark);
        s_marks.Remove(type);
    }
    public static void Unmark(Slot slot) {
        if (!IsMarked(slot)) return;
        s_marks[s_marksData[slot].fake.type].Remove(slot);
        s_marksData.Remove(slot);
    }
    public static void Remark(int oldType, int newType, bool? favorited = null) {
        if (newType == oldType || newType == ItemID.None || !IsMarked(oldType)) {
            Unmark(oldType);
            return;
        }
        foreach (Slot mark in s_marks[oldType]) s_marksData[mark] = (new(newType), favorited ?? s_marksData[mark].favorited);

        s_marks[newType] = s_marks[oldType];
        s_marks.Remove(oldType);
    }
    public static (Slot slot, bool favorited) ConsumeMark(int type) {
        Slot mark = s_marks[type][^1];
        bool favorited = s_marksData[mark].favorited;
        Unmark(mark);
        return (mark, favorited);
    }

    public static void ClearMarks() {
        s_marksData.Clear();
        s_marks.Clear();
    }

    public static bool VanillaGetItem { get; private set; }

    private static readonly Dictionary<Slot, (Item fake, bool favorited)> s_marksData = new();
    private static readonly Dictionary<int, List<Slot>> s_marks = new();
}