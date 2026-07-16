using MonoMod.Cil;
using SpikysLib.Constants;
using SpikysLib.IL;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace BetterInventory.BetterInventoryManagement;

public sealed class CompleteQuickStack : ILoadable {

    public bool IsLoadingEnabled(Mod mod) => Compatibility.LoadDisabledFeatures || BetterInventoryManagementConfig.CompleteQuickStack;
    public void Load(Mod mod) {
        IL_ChestUI.QuickStack += il => il.TryEdit(ILCompleteQuickStack, ref UnloadedBetterInventoryManagementConfig.Instance.completeQuickStack);
        IL_Player.QuickStackAllChests += il => il.TryEdit(ILCompleteQuickStackAllChests, ref UnloadedBetterInventoryManagementConfig.Instance.completeQuickStack);
        IL_ChestUI.DepositAll += il => il.TryEdit(ILCompleteDepositAll, ref UnloadedBetterInventoryManagementConfig.Instance.completeQuickStack);
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
        cursor.EmitDelegate((int upper, Player player) => {
            if (!BetterInventoryManagementConfig.CompleteQuickStack) return upper;
            // Deposit coins into banks (chest <= -2)
            return player.chest <= InventorySlots.PiggyBank ? (InventorySlots.Ammo.End - InventorySlots.Coins.Count) : InventorySlots.Ammo.End;
        });
        cursor.GotoNextLoc(out int lowerBound, i => i.Previous.MatchLdcI4(10), 13);
        cursor.EmitDelegate((int lower) => !BetterInventoryManagementConfig.CompleteQuickStack ? lower : 0);
    }

    private static void ILCompleteQuickStackAllChests(ILContext il) {
        ILCursor cursor = new(il);

        for (int i = 0; i < 2; i++) {
            cursor.GotoNext(MoveType.After, i => i.MatchLdcI4(10));
            cursor.EmitDelegate((int lower) => !BetterInventoryManagementConfig.CompleteQuickStack ? lower : InventorySlots.Hotbar.Start);
            cursor.GotoNext(MoveType.After, i => i.MatchLdcI4(50));
            cursor.EmitDelegate((int upper) => !BetterInventoryManagementConfig.CompleteQuickStack ? upper : InventorySlots.Ammo.End);
        }
    }

    private static void ILCompleteDepositAll(ILContext il) {
        ILCursor cursor = new(il);

        // for (int num = ++[57]; num >= ++[0]; num--)
        cursor.GotoNext(MoveType.After, i => i.MatchLdcI4(49));
        cursor.EmitDelegate((int upper) => !BetterInventoryManagementConfig.CompleteQuickStack ? upper : InventorySlots.Ammo.End - 1);
        cursor.GotoNext(MoveType.After, i => i.MatchLdcI4(10));
        cursor.EmitDelegate((int lower) => !BetterInventoryManagementConfig.CompleteQuickStack ? lower : InventorySlots.Hotbar.Start);
    }

}