using SpikysLib.Constants;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace BetterInventory.BetterInventoryManagement;

// TODO check if this should be in another module
public sealed class ExtraItemRightClickPlayer : ModPlayer {

    public override bool IsLoadingEnabled(Mod mod) => Compatibility.LoadDisabledFeatures || BetterInventoryManagementConfig.ExtraItemRightClick;
    public override void Load() {
        On_ItemSlot.PickupItemIntoMouse += HookNoPickupMouse;
    }
    public override bool PreItemCheck() {
        if (!BetterInventoryManagementConfig.ExtraItemRightClick || Main.myPlayer != Player.whoAmI) return true;
        if (Player.controlUseTile && Player.releaseUseItem && !Player.controlUseItem && !Player.tileInteractionHappened
                && !Player.mouseInterface && !Terraria.Graphics.Capture.CaptureManager.Instance.Active && !Main.HoveringOverAnNPC && !Main.SmartInteractShowingGenuine
                && Main.HoverItem.IsAir && Player.altFunctionUse == 0 && Player.selectedItem < InventorySlots.Hotbar.End) {
            Item item = Player.inventory[Player.selectedItem];
            (int type, int stack, int prefix) = (item.type, item.stack, item.prefix);
            int animation = Player.itemAnimation;
            Player.itemAnimation--;
            if (Main.stackSplit == 1) Player.itemAnimation = 0;

            if (!ExtraItemRightClickConfig.Instance.stackableItems) s_noMousePickup = true;
            ItemSlot.RightClick(Player.inventory, ItemSlot.Context.InventoryItem, Player.selectedItem);
            s_noMousePickup = false;

            if (type == item.type && stack == item.stack && prefix == item.prefix) {
                Player.itemAnimation = animation;
                return true;
            }
            if (!Main.mouseItem.IsAir) Player.DropSelectedItem();
            return false;
        }
        return true;
    }
    private void HookNoPickupMouse(On_ItemSlot.orig_PickupItemIntoMouse orig, Item[] inv, int context, int slot, Player player) {
        if (!BetterInventoryManagementConfig.ExtraItemRightClick || !s_noMousePickup) orig(inv, context, slot, player);
    }

    private static bool s_noMousePickup;
}