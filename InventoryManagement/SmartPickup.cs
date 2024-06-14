using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using SpikysLib.Extensions;
using SpikysLib.DataStructures;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using System;
using System.Diagnostics.CodeAnalysis;
using Terraria.UI.Gamepad;
using System.Linq;
using BetterInventory.Default.Inventories;
using System.Collections.ObjectModel;

namespace BetterInventory.InventoryManagement;

public sealed class SmartPickup : ILoadable {

    public void Load(Mod mod) {
        On_ItemSlot.LeftClick_ItemArray_int_int += HookLeftSaveType;
        On_ItemSlot.RightClick_ItemArray_int_int += HookRightSaveType;
        On_Player.DropItems += HookMarkItemsOnDeath;
    }
    public void Unload() {
        foreach (ModPickupUpgrader up in s_upgraders) ModConfigExtensions.SetInstance(up, true);
        s_upgraders.Clear();
    }

    private static void HookLeftSaveType(On_ItemSlot.orig_LeftClick_ItemArray_int_int orig, Item[] inv, int context, int slot) => UpdateMark((inv, context, slot) => orig(inv, context, slot), inv, context, slot, Main.mouseLeft && Main.mouseLeftRelease);
    private static void HookRightSaveType(On_ItemSlot.orig_RightClick_ItemArray_int_int orig, Item[] inv, int context, int slot) => UpdateMark((inv, context, slot) => orig(inv, context, slot), inv, context, slot, Main.mouseRight);
    private static void UpdateMark(Action<Item[], int, int> orig, Item[] inv, int context, int slot, bool update) {
        if (!update || !Configs.SmartPickup.Mouse || !InventoryLoader.IsInventorySlot(Main.LocalPlayer, inv, context, slot, out Slot mark)) {
            orig(inv, context, slot);
            return;
        }
        (int oldType, int oldMouse, bool oldFav) = (inv[slot].type, Main.mouseItem.type, inv[slot].favorited);
        orig(inv, context, slot);
        if (Main.mouseItem.type == oldMouse) return;

        bool removed = oldType != ItemID.None && (Configs.SmartPickup.Value.mouse == Configs.MousePickupLevel.AllItems || mark.Inventory.CanBePrimary || oldFav);
        bool placed = inv[slot].type != ItemID.None && (Configs.SmartPickup.Value.mouse == Configs.MousePickupLevel.AllItems || mark.Inventory.CanBePrimary || inv[slot].favorited);

        if (placed && removed) Remark(inv[slot].type, oldType, oldFav);
        else if (removed) Mark(oldType, mark, oldFav);
        else if (placed) Unmark(inv[slot].type);
        if (placed) Unmark(mark);
    }
    private static void HookMarkItemsOnDeath(On_Player.orig_DropItems orig, Player self) {
        if (!Configs.SmartPickup.MediumCore) {
            orig(self);
            return;
        }

        foreach (ModSubInventory inventory in InventoryLoader.SubInventories) {
            IList<Item> items = inventory.Items(self);
            for (int i = 0; i < items.Count; i++) {
                if (!items[i].IsAir && (Configs.SmartPickup.Value.mouse == Configs.MousePickupLevel.AllItems || inventory.CanBePrimary || items[i].favorited)) Mark(items[i].type, new(inventory, i), items[i].favorited);
            }
        }
        orig(self);
    }

    internal static void ILSmartPickup(ILContext il) {
        ILCursor cursor = new(il);

        // ...
        // if (newItem.uniqueStack && this.HasItem(newItem.type)) return item;
        if (!cursor.TryGotoNext(i => i.SaferMatchCall(Reflection.Player.HasItem))
                || !cursor.TryGotoNext(MoveType.AfterLabel, i => i.MatchLdloc0())) {// bool isACoin
            BetterInventory.Instance.Logger.Error($"{nameof(ILSmartPickup)} failled to load");
            return;
        }

        // ++ item = <smartPickup>
        cursor.EmitLdarg0();
        cursor.EmitLdloc1();
        cursor.EmitLdarg3();
        cursor.EmitDelegate((Player self, Item item, GetItemSettings settings) => {
            if (VanillaGetItem || !Configs.SmartPickup.Enabled) return item;
            else return SmartGetItem(self, item, settings);
        });
        cursor.EmitDup();
        cursor.EmitStloc1();

        // ++if (newItem.IsAir) return new()
        EmitRetAir(cursor);
    }
    internal static void ILDrawFakeItem(ILContext il) {
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
            if (!Configs.MarksDisplay.FakeItem || !TryDrawMark(spriteBatch, inv, context, slot, position, scale, texture, color, Configs.MarksDisplay.Value.fakeItem.Value)) return num9;
            s_ilBackgroundMark = true;
            return -1;
        });
    }
    internal static void ILDrawIcon(ILContext il) {
        ILCursor cursor = new(il);

        // ...
        // if(...) {
        // } else if (context == 6) {
        //     ...
        //     spriteBatch.Draw(value10, position4, null, new Color(100, 100, 100, 100), 0f, default(Vector2), inventoryScale, 0, 0f);
        // }
        // if (context == 0 && ++[!<hideKeys> && slot < 10]) {
        //     ...
        // }
        cursor.GotoNext(i => i.SaferMatchCall(typeof(UILinkPointNavigator), nameof(UILinkPointNavigator.SetPosition)));
        cursor.GotoPrev(i => i.MatchCallvirt(typeof(SpriteBatch), nameof(SpriteBatch.Draw)));
        cursor.GotoNext(MoveType.AfterLabel, i => i.MatchLdarg2());

        // ++ <drawMark>
        cursor.EmitLdarg0();
        cursor.EmitLdarg1();
        cursor.EmitLdarg2();
        cursor.EmitLdarg3();
        cursor.EmitLdarg(4);
        cursor.EmitLdloc2();
        cursor.EmitLdloc(7);
        cursor.EmitLdloc(3);
        cursor.EmitDelegate((SpriteBatch spriteBatch, Item[] inv, int context, int slot, Vector2 position, float scale, Texture2D texture, Color color) => {
            if (!Main.gameMenu && !s_ilBackgroundMark && Configs.MarksDisplay.Icon) TryDrawMark(spriteBatch, inv, context, slot, position, scale, texture, color, Configs.MarksDisplay.Value.icon.Value);
            s_ilBackgroundMark = false;
        });
    }
    private static bool s_ilBackgroundMark;
    
    private static bool TryDrawMark(SpriteBatch spriteBatch, Item[] inv, int context, int slot, Vector2 position, float scale, Texture2D texture, Color color, Configs.IMarkDisplay ui) {
        if (!InventoryLoader.IsInventorySlot(Main.LocalPlayer, inv, context, slot, out Slot itemSlot) || !TryGetMark(itemSlot, out Item? mark)) return false;
        ItemSlot.DrawItemIcon(mark, context, spriteBatch, position + texture.Size() * ui.position * scale, scale * ui.scale, 32f, color * Main.cursorAlpha * ui.intensity);
        return true;
    }

    internal static void ILAutoEquip(ILContext il) {
        ILCursor cursor = new(il);

        // if (isACoin) ...
        // if (item.FitsAmmoSlot()) ...
        // for(...) ...
        cursor.GotoNext(i => i.SaferMatchCall(Reflection.Player.GetItem_FillEmptyInventorySlot));
        cursor.GotoPrev(MoveType.AfterLabel, i => i.MatchLdloc0());

        // ++<autoEquip & autoUpgrade>
        cursor.EmitLdarg0();
        cursor.EmitLdloc1();
        cursor.EmitLdarg3();
        cursor.EmitDelegate((Player self, Item item, GetItemSettings settings) => {
            if (VanillaGetItem || settings.NoText || !Configs.AutoEquip.Enabled) return item;
            return item = AutoEquip(self, item, settings);
        });
        cursor.EmitDup();
        cursor.EmitStloc1();

        // ++if (newItem.IsAir) return new()
        EmitRetAir(cursor);
    }
    internal static void ILAutoUprade(ILContext il) {
        ILCursor cursor = new(il);

        // if (isACoin) ...
        // if (item.FitsAmmoSlot()) ...
        // for(...) ...
        cursor.GotoNext(i => i.SaferMatchCall(Reflection.Player.GetItem_FillEmptyInventorySlot));
        cursor.GotoPrev(MoveType.AfterLabel, i => i.MatchLdloc0());

        // ++<autoEquip & autoUpgrade>
        cursor.EmitLdarg0();
        cursor.EmitLdloc1();
        cursor.EmitLdarg3();
        cursor.EmitDelegate((Player self, Item item, GetItemSettings settings) => {
            if (VanillaGetItem || settings.NoText || !Configs.AutoUpgrade.Enabled) return item;
            return AutoUpgrade(self, item, settings);
        });
        cursor.EmitDup();
        cursor.EmitStloc1();

        // ++if (newItem.IsAir) return new()
        EmitRetAir(cursor);
    }
    internal static void ILFixNewItem(ILContext il) {
        ILCursor cursor = new(il);
        cursor.GotoNext(MoveType.After, i => i.MatchStloc1());
        while(cursor.TryGotoNext(MoveType.Before, i => i.MatchLdarg2() && i.Next.MatchLdfld(out _))){
            cursor.Remove();
            cursor.GotoNext(MoveType.AfterLabel);
            cursor.EmitLdloc1();
        }
    }

    private static void EmitRetAir(ILCursor cursor) {
        cursor.EmitDelegate((Item item) => item.IsAir);
        ILLabel skip = cursor.DefineLabel();
        cursor.EmitBrfalse(skip);
        cursor.EmitDelegate(() => new Item());
        cursor.EmitRet();
        cursor.MarkLabel(skip);
    }

    internal static void ILHotbarLast(ILContext il) {
        ILCursor cursor = new(il);

        // if (!isACoin ++[&& !<hotbarLast>] && newItem.useStyle != 0) <hotbar>
        cursor.GotoNext(MoveType.After, i => i.MatchLdfld(Reflection.Item.useStyle));

        cursor.EmitDelegate((int style) => {
            if (Configs.InventoryManagement.HotbarLast) return ItemUseStyleID.None;
            return style;
        });
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
        while (ConsumeMark(item.type, out (Slot slot, bool favorited) mark)) {
            Joined<ListIndices<Item>, Item> items = mark.slot.Inventory.Items(player);
            if (mark.slot.Index >= items.Count) continue;
            if (!(item.favorited || mark.favorited) && items[mark.slot.Index].favorited) continue;

            item.favorited |= mark.favorited;
            (Item moved, items[mark.slot.Index]) = (items[mark.slot.Index], new());
            Item toMove = item.Clone();
            toMove.stack = 1;
            if (mark.slot.GetItem(player, toMove, settings).IsAir) item.stack--;
            moved = mark.slot.GetItem(player, moved, settings);
            player.GetDropItem(ref moved);
            if (item.IsAir) return item;
            slots.Add(mark.slot);
        }
        foreach (Slot slot in slots) {
            item = slot.GetItem(player, item, settings);
            if (item.IsAir) return item;
        }
        return item;
    }
    public static Item AutoEquip(Player player, Item item, GetItemSettings settings) {
        foreach (ModSubInventory inv in InventoryLoader.Special.Where(i => i is not Hotbar &&  i.Accepts(item))) {
            if (!Configs.AutoEquip.Value.nonPrimary && !inv.IsPrimaryFor(item)) continue;
            item = inv.GetItem(player, item, settings);
            if (item.IsAir) return item;
        }
        return item;
    }
    public static Item AutoUpgrade(Player player, Item item, GetItemSettings settings) {
        foreach (var upgrader in s_upgraders) {
            if (upgrader.Enabled && upgrader.AppliesTo(item)) item = upgrader.AttemptUpgrade(player, item);
        }
        return item;
    }

    public static bool IsMarked(int type) => s_marks.TryGetValue(type, out var marks) && marks.Count > 0;
    public static bool IsMarked(Slot slot) => s_marksData.ContainsKey(slot);
    public static bool TryGetMark(Slot slot, [MaybeNullWhen(false)] out Item mark) => s_marksData.TryGetValue(slot, out mark);
    public static bool ConsumeMark(int type, [MaybeNullWhen(false)] out (Slot slot, bool favorited) mark) {
        if (IsMarked(type)) {
            Slot slot = s_marks[type][^1];
            mark = (slot, s_marksData[slot].favorited);
            Unmark(slot);
            return true;
        }
        mark = default;
        return false;
    }

    public static void Mark(int type, Slot slot, bool favorited) {
        if (IsMarked(slot)) {
            if(!Configs.SmartPickup.Value.overrideMarks) return;
            Unmark(slot);
        }
        s_marks.TryAdd(type, new());
        s_marks[type].Add(slot);
        s_marksData[slot] = new(type){ favorited = favorited };
    }
    public static void Unmark(int type) {
        if (!IsMarked(type)) return;
        foreach (Slot mark in s_marks[type]) s_marksData.Remove(mark);
        s_marks.Remove(type);
    }
    public static void Unmark(Slot slot) {
        if (!IsMarked(slot)) return;
        s_marks[s_marksData[slot].type].Remove(slot);
        s_marksData.Remove(slot);
    }
    public static void Remark(int oldType, int newType, bool? favorited = null) {
        Unmark(newType);
        if (!IsMarked(oldType)) return;
        foreach (Slot slot in s_marks[oldType]) s_marksData[slot] = new(newType) { favorited = favorited ?? s_marksData[slot].favorited };
        s_marks[newType] = s_marks[oldType];
        s_marks.Remove(oldType);
    }

    public static void ClearMarks() {
        s_marksData.Clear();
        s_marks.Clear();
    }

    public static bool VanillaGetItem { get; private set; }

    private static readonly Dictionary<Slot, Item> s_marksData = new();
    private static readonly Dictionary<int, List<Slot>> s_marks = new();

    internal static void Register(ModPickupUpgrader upgrader) {
        ModConfigExtensions.SetInstance(upgrader);
        s_upgraders.Add(upgrader);
    }

    public static ModPickupUpgrader? GetPickupUpgrader(string mod, string name) => s_upgraders.Find(p => p.Mod.Name == mod && p.Name == name);
    public static ReadOnlyCollection<ModPickupUpgrader> Upgraders => s_upgraders.AsReadOnly();
    private static readonly List<ModPickupUpgrader> s_upgraders = [];
}