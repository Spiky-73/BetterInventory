using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using MonoMod.Cil;
using SpikysLib.IL;

namespace BetterInventory.BetterInventoryManagement;

public sealed class StackTrash : ILoadable {

    public bool IsLoadingEnabled(Mod mod) => Compatibility.LoadDisabledFeatures || BetterInventoryManagementConfig.StackTrash;
    public void Load(Mod mod) {
        IL_ItemSlot.SellOrTrash += static il => il.TryEdit(ILStackTrash, ref UnloadedBetterInventoryManagementConfig.Instance.stackTrash);
        On_Chest.AddItemToShop += HookStackSold;
    }
    public void Unload() { }

    private static void ILStackTrash(ILContext il) {
        ILCursor cursor = new(il);
        // if (<shop>){
        //     ...
        // }

        // else if (!inv[slot].favorited) {
        //     SoundEngine.PlaySound(7, -1, -1, 1, 1f, 0f);
        cursor.GotoNext(MoveType.Before, i => i.MatchStfld((Player p) => p.trashItem));

        //     ++<stackTrash>
        cursor.EmitDelegate((Item trash) => {
            if (!BetterInventoryManagementConfig.StackTrash || trash.type != Main.LocalPlayer.trashItem.type) return trash;
            if (ItemLoader.TryStackItems(Main.LocalPlayer.trashItem, trash, out int transfered)) return Main.LocalPlayer.trashItem;
            return trash;
        });

        //     player.trashItem = inv[slot].Clone();
        //     ...
        // }
        // ...
    }

    private static int HookStackSold(On_Chest.orig_AddItemToShop orig, Chest self, Item newItem) {
        int bought = Main.shopSellbackHelper.GetAmount(newItem);
        if (!BetterInventoryManagementConfig.StackTrash || bought >= newItem.stack) return orig(self, newItem);
        newItem.stack -= Main.shopSellbackHelper.Remove(newItem);
        for (int i = 0; i < self.item.Length; i++) {
            if (self.item[i].IsAir || self.item[i].type != newItem.type || !self.item[i].buyOnce) continue;
            if (!ItemLoader.TryStackItems(self.item[i], newItem, out int transferred)) continue;
            if (newItem.IsAir) return i;
        }

        return orig(self, newItem);
    }
}