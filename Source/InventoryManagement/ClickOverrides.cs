using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.Audio;

namespace BetterInventory.InventoryManagement;

public sealed class ClickOverrides : ModPlayer {

    public override void Load() {
        On_ItemSlot.RightClick_ItemArray_int_int += HookDepositClick; // Needs to be added before `HookShiftRight` for Shift+Deposit to work 
    }

    private static void HookDepositClick(On_ItemSlot.orig_RightClick_ItemArray_int_int orig, Item[] inv, int context, int slot) {
        orig(inv, context, slot);
        if (ItemSlot.PickItemMovementAction(inv, context, slot, Main.mouseItem) == -1) return;
        if (!Configs.InventoryManagement.DepositClick) return;

        Player player = Main.LocalPlayer;
        if (player.itemAnimation > 0) return;

        if (!Main.mouseMiddle) {
            if (s_allowResetStackSplit) Main.preventStackSplitReset = s_allowResetStackSplit = false;
            return;
        }
        Main.preventStackSplitReset = s_allowResetStackSplit = true;
        if (Main.stackSplit > 1) return;

        Item testItem = Main.mouseItem.IsAir ? inv[slot] : Main.mouseItem;
        if (testItem.maxStack <= 1 && testItem.stack == 1) return;

        // Pickup all items if the mouse is empty
        if (Main.mouseMiddleRelease && Main.mouseItem.IsAir && context != ItemSlot.Context.CreativeInfinite && context != ItemSlot.Context.ShopItem) {
            Main.mouseItem = ItemLoader.TransferWithLimit(inv[slot], inv[slot].stack);
            ItemSlot.AnnounceTransfer(new ItemSlot.ItemTransferInfo(inv[slot], context, 21, 0));
        }

        int num = Main.superFastStack + 1;
        if (context == ItemSlot.Context.ShopItem) {
            Item[] toSell = [ItemLoader.TransferWithLimit(Main.mouseItem, num)];
            ItemSlot.SellOrTrash(toSell, ItemSlot.Context.MouseItem, 0);
            ItemSlot.RefreshStackSplitCooldown();
        } else if (inv[slot].type == ItemID.None || (inv[slot].type == Main.mouseItem.type && inv[slot].stack < inv[slot].maxStack && ItemLoader.CanStack(inv[slot], Main.mouseItem))) {
            DepositItemFromMouse(inv, context, slot, player, num);
            SoundEngine.PlaySound(SoundID.MenuTick);
            ItemSlot.RefreshStackSplitCooldown();
        }
    }

    public static void DepositItemFromMouse(Item[] inv, int context, int slot, Player player, int amount) {
        if (inv[slot].type == ItemID.None) {
            inv[slot] = ItemLoader.TransferWithLimit(Main.mouseItem, amount);
            ItemSlot.AnnounceTransfer(new ItemSlot.ItemTransferInfo(Main.mouseItem, ItemSlot.Context.MouseItem, context, 0));
        } else {
            if (context == ItemSlot.Context.CreativeInfinite) Main.mouseItem.stack -= amount;
            else ItemLoader.StackItems(inv[slot], Main.mouseItem, out _, false, amount);
        }
        if (Main.mouseItem.stack <= 0) Main.mouseItem = new Item();
        Recipe.FindRecipes();
        if (Main.netMode == NetmodeID.MultiplayerClient) {
            if (context == ItemSlot.Context.ChestItem) {
                NetMessage.SendData(MessageID.SyncChestItem, -1, -1, null, player.chest, slot, 0f, 0f, 0, 0, 0);
            }
            if (context == ItemSlot.Context.DisplayDollArmor || context == ItemSlot.Context.DisplayDollAccessory) {
                NetMessage.SendData(MessageID.TEDisplayDollItemSync, -1, -1, null, Main.myPlayer, player.tileEntityAnchor.interactEntityID, slot, 0f, 0, 0, 0);
            }
            if (context == ItemSlot.Context.DisplayDollDye) {
                NetMessage.SendData(MessageID.TEDisplayDollItemSync, -1, -1, null, Main.myPlayer, player.tileEntityAnchor.interactEntityID, slot, 1f, 0, 0, 0);
            }
            if (context == ItemSlot.Context.HatRackHat) {
                NetMessage.SendData(MessageID.TEHatRackItemSync, -1, -1, null, Main.myPlayer, player.tileEntityAnchor.interactEntityID, slot, 0f, 0, 0, 0);
            }
            if (context == ItemSlot.Context.HatRackDye) {
                NetMessage.SendData(MessageID.TEHatRackItemSync, -1, -1, null, Main.myPlayer, player.tileEntityAnchor.interactEntityID, slot, 1f, 0, 0, 0);
            }

        }
    }
    private static bool s_allowResetStackSplit = false;
}