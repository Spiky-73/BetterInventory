using System;
using SpikysLib;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace BetterInventory.BetterInventoryManagement;

public sealed class ShiftRightClick : ILoadable {
    public bool IsLoadingEnabled(Mod mod) => Compatibility.LoadDisabledFeatures || BetterInventoryManagementConfig.ShiftRightClick;
    public void Load(Mod mod) {
        On_ItemSlot.RightClick_ItemArray_int_int += HookShiftRightClick;
    }

    public void Unload() { }

    private static void HookShiftRightClick(On_ItemSlot.orig_RightClick_ItemArray_int_int orig, Item[] inv, int context, int slot) {
        if (!BetterInventoryManagementConfig.ShiftRightClick || Main.cursorOverride <= CursorOverrideID.DefaultCursor || !Main.mouseRight) {
            orig(inv, context, slot);
            return;
        }

        // Do the real click with an empty mouseItem
        (Item mouse, Main.mouseItem) = (Main.mouseItem, new());
        orig(inv, context, slot);
        (Main.mouseItem, Item[] inv2) = (mouse, [Main.mouseItem]);
        if (inv2[0].IsAir) return;

        // Simulate a left click using the resulting mouseItem as the inventory
        (bool mouseLeft, bool mouseLeftRelease, Main.mouseLeft, Main.mouseLeftRelease) = (Main.mouseLeft, Main.mouseLeftRelease, true, true);
        int cursor = Main.cursorOverride;
        if (Array.IndexOf(WhitelistedCursors, cursor) == -1) (context, Main.cursorOverride) = (ItemSlot.Context.ChestItem, CursorOverrideID.ChestToInventory);
        ItemSlot.LeftClick(inv2, context, 0);
        (Main.mouseLeft, Main.mouseLeftRelease) = (mouseLeft, mouseLeftRelease);
        Main.cursorOverride = cursor;
        if (!inv2[0].IsAir) inv[slot] = ItemHelper.MoveInto(inv[slot], inv2[0], out _);
        Recipe.FindRecipes();
    }

    // Cursors where Right click can be done without issue
    public static readonly int[] WhitelistedCursors = [CursorOverrideID.QuickSell, CursorOverrideID.TrashCan, CursorOverrideID.InventoryToChest, CursorOverrideID.ChestToInventory];
}