using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.Audio;
using MonoMod.Cil;

namespace BetterInventory.InventoryManagement;

public sealed class BetterTrash : ModPlayer {

    public override void Load() {
        IL_ItemSlot.SellOrTrash += static il => {
            if (!il.ApplyTo(ILStackTrash, Configs.BetterTrash.StackTrash)) Configs.UnloadedInventoryManagement.Value.stackTrash = true;
        };
        On_Chest.AddItemToShop += HookStackSold;

        On_ItemSlot.LeftClick_ItemArray_int_int += HookHoverTrashSlot;
        On_ItemSlot.LeftClick_SellOrTrash += HookTrashTrash;
    }

    private static void ILStackTrash(ILContext il) {
        ILCursor cursor = new(il);
        // if (<shop>){
        //     ...
        // }

        // else if (!inv[slot].favorited) {
        //     SoundEngine.PlaySound(7, -1, -1, 1, 1f, 0f);
        cursor.GotoNext(MoveType.Before, i => i.MatchStfld(Reflection.Player.trashItem));

        //     ++<stackTrash>
        cursor.EmitDelegate((Item trash) => {
            if (Configs.BetterTrash.StackTrash && trash.type == Main.LocalPlayer.trashItem.type) {
                if (ItemLoader.TryStackItems(Main.LocalPlayer.trashItem, trash, out int transfered)) return Main.LocalPlayer.trashItem;
            }
            return trash;
        });

        //     player.trashItem = inv[slot].Clone();
        //     ...
        // }
        // ...
    }

    private static int HookStackSold(On_Chest.orig_AddItemToShop orig, Chest self, Item newItem) {
        int bought = Main.shopSellbackHelper.GetAmount(newItem);
        if (!Configs.BetterTrash.StackTrash || bought >= newItem.stack) return orig(self, newItem);
        newItem.stack -= Main.shopSellbackHelper.Remove(newItem);
        for (int i = 0; i < self.item.Length; i++) {
            if (self.item[i].IsAir || self.item[i].type != newItem.type || !self.item[i].buyOnce) continue;
            if (!ItemLoader.TryStackItems(self.item[i], newItem, out int transferred)) continue;
            if (newItem.IsAir) return i;
        }

        return orig(self, newItem);
    }

    private static void HookHoverTrashSlot(On_ItemSlot.orig_LeftClick_ItemArray_int_int orig, Item[] inv, int context, int slot) {
        if (Configs.BetterTrash.TrashTrash && context == ItemSlot.Context.TrashItem
                && !ItemSlot.Options.DisableQuickTrash && (ItemSlot.Options.DisableLeftShiftTrashCan ? ItemSlot.ControlInUse : ItemSlot.ShiftInUse)) {
            Main.cursorOverride = CursorOverrideID.TrashCan;
        }
        orig(inv, context, slot);
    }

    private static bool HookTrashTrash(On_ItemSlot.orig_LeftClick_SellOrTrash orig, Item[] inv, int context, int slot) {
        if (Configs.BetterTrash.TrashTrash && context == ItemSlot.Context.TrashItem
                && Main.cursorOverride == CursorOverrideID.TrashCan) {
            inv[slot].TurnToAir();
            SoundEngine.PlaySound(SoundID.Grab);
            return true;
        }

        return orig(inv, context, slot);
    }
}