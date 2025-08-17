using MonoMod.Cil;
using SpikysLib.Constants;
using SpikysLib.IL;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace BetterInventory.InventoryManagement;

public sealed class CompleteQuickStack : ILoadable {
    public void Load(Mod mod) {
        IL_ChestUI.QuickStack += static il => {
            if (!il.ApplyTo(ILCompleteQuickStack, Configs.InventoryManagement.CompleteQuickStack)) Configs.UnloadedInventoryManagement.Value.completeQuickStack = true;
        };
        IL_Player.QuickStackAllChests += static il => {
            if (!il.ApplyTo(ILCompleteQuickStackAllChests, Configs.InventoryManagement.CompleteQuickStack)) Configs.UnloadedInventoryManagement.Value.completeQuickStack = true;
        };
        IL_ChestUI.DepositAll += static il => {
            if (!il.ApplyTo(ILCompleteDepositAll, Configs.InventoryManagement.CompleteQuickStack)) Configs.UnloadedInventoryManagement.Value.completeQuickStack = true;
        };
    }
    public void Unload() { }

    private static void ILCompleteQuickStack(ILContext il) {
        ILCursor cursor = new(il);

        // int num = 50;
        // int num2 = 10;
        // ++ <completQuickStack>
        // if (player.chest <= -2) num += 4;
        cursor.GotoNextLoc(out int upperBound, i => i.Previous.MatchLdcI4(50), 12);
        cursor.EmitLdloc0();
        cursor.EmitDelegate((int upper, Player player) => !Configs.InventoryManagement.CompleteQuickStack ? upper : player.chest <= -2 ? (InventorySlots.Ammo.End - InventorySlots.Coins.Count) : InventorySlots.Ammo.End);
        cursor.GotoNextLoc(out int lowerBound, i => i.Previous.MatchLdcI4(10), 13);
        cursor.EmitDelegate((int lower) => !Configs.InventoryManagement.CompleteQuickStack ? lower : 0);
    }

    private static void ILCompleteQuickStackAllChests(ILContext il) {
        ILCursor cursor = new(il);

        for (int i = 0; i < 2; i++) {
            cursor.GotoNext(MoveType.After, i => i.MatchLdcI4(10));
            cursor.EmitDelegate((int lower) => !Configs.InventoryManagement.CompleteQuickStack ? lower : InventorySlots.Hotbar.Start);
            cursor.GotoNext(MoveType.After, i => i.MatchLdcI4(50));
            cursor.EmitDelegate((int upper) => !Configs.InventoryManagement.CompleteQuickStack ? upper : InventorySlots.Ammo.End);
        }
    }


    private static void ILCompleteDepositAll(ILContext il) {
        ILCursor cursor = new(il);

        // for (int num = [++57]; num >= [++0]; num--)
        cursor.GotoNext(MoveType.After, i => i.MatchLdcI4(49));
        cursor.EmitDelegate((int upper) => !Configs.InventoryManagement.CompleteQuickStack ? upper : InventorySlots.Ammo.End - 1);
        cursor.GotoNext(MoveType.After, i => i.MatchLdcI4(10));
        cursor.EmitDelegate((int lower) => !Configs.InventoryManagement.CompleteQuickStack ? lower : InventorySlots.Hotbar.Start);
    }

}