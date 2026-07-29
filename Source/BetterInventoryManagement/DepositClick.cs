using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace BetterInventory.BetterInventoryManagement;

public sealed class DepositClick : ILoadable {
    public bool IsLoadingEnabled(Mod mod) => Compatibility.LoadDisabledFeatures || BetterInventoryManagementConfig.DepositClick;
    public void Load(Mod mod) {
        On_ItemSlot.LeftClick_ItemArray_int_int += HookDepositClick;
    }
    public void Unload() { }

    private static void HookDepositClick(On_ItemSlot.orig_LeftClick_ItemArray_int_int orig, Item[] inv, int context, int slot) {
        orig(inv, context, slot);

        if (!BetterInventoryManagementConfig.DepositClick || !Main.mouseMiddle) return;
        Main.preventStackSplitReset = true;
        Player player = Main.LocalPlayer;
        if (player.itemAnimation > 0 || Main.stackSplit > 1) return;

        if (Main.cursorOverride >= CursorOverrideID.DefaultCursor) {
            // Always leave an item on the slot
            if (inv[slot].stack <= 1) return;
            var remainingItem = ItemLoader.TransferWithLimit(inv[slot], 1);

            // Do the clicks
            (var mouseLeft, var mouseLeftRelease) = (Main.mouseLeft, Main.mouseLeftRelease);
            (Main.mouseLeft, Main.mouseLeftRelease) = (true, true);
            var stackSplit = Main.stackSplit;
            orig(inv, context, slot);
            (Main.mouseLeft, Main.mouseLeftRelease) = (mouseLeft, mouseLeftRelease);
            Main.stackSplit = stackSplit;
            ItemSlot.RefreshStackSplitCooldown();

            if (!inv[slot].IsAir) ItemLoader.StackItems(remainingItem, inv[slot], out _);
            inv[slot] = remainingItem;
            Recipe.FindRecipes();
        } else {
            var action = ItemSlot.PickItemMovementAction(inv, context, slot, Main.mouseItem);

            if (action == (int)ItemMovementActionID.Pickup && Main.mouseItem.IsAir) (Main.mouseItem, inv[slot]) = (inv[slot], Main.mouseItem); // if mouse is empty, swap mouse and items
            if (Main.mouseItem.IsAir || Main.mouseItem.maxStack == 1) return; // Nothing to deposit
            if (action == (int)ItemMovementActionID.Pickup && inv[slot].type != ItemID.None && (!Main.mouseItem.IsTheSameAs(inv[slot]) || !ItemLoader.CanStack(Main.mouseItem, inv[slot]))) return; // Cannot stack
            if (action == (int)ItemMovementActionID.Pickup && inv[slot].stack >= inv[slot].maxStack && inv[slot].type != ItemID.None) return; // Stack is full

            // Move the item to stack
            var mouseItem = Main.mouseItem;
            Main.mouseItem = ItemLoader.TransferWithLimit(mouseItem, Main.superFastStack + 1);

            // Perform the click
            (var mouseLeft, var mouseLeftRelease) = (Main.mouseLeft, Main.mouseLeftRelease);
            (Main.mouseLeft, Main.mouseLeftRelease) = (true, true);
            var stackSplit = Main.stackSplit;
            switch (action) {
            case (int)ItemMovementActionID.Pickup:
                orig(inv, context, slot);
                break;
            case (int)ItemMovementActionID.Buy or (int)ItemMovementActionID.Sell:
                orig(inv, context, inv.Length - 1);
                break;
            }
            Main.stackSplit = stackSplit;
            (Main.mouseLeft, Main.mouseLeftRelease) = (mouseLeft, mouseLeftRelease);
            ItemSlot.RefreshStackSplitCooldown();

            // Restore the mouse item
            if (!Main.mouseItem.IsAir) ItemLoader.StackItems(mouseItem, Main.mouseItem, out _);
            Main.mouseItem = mouseItem;
            Recipe.FindRecipes();
        }
    }
}

public enum ItemMovementActionID {
    None = -1,
    Pickup = 0,
    Equip = 1,
    EquipDye = 2,
    Buy = 3,
    Sell = 4,
    JourneyCreate = 5,
}