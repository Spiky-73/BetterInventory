using System;
using System.Collections.Generic;
using MonoMod.Cil;
using SpikysLib.Constants;
using SpikysLib.IL;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace BetterInventory.InventoryManagement;

public sealed class BetterQuickStack : ILoadable {
    public void Load(Mod mod) {
        IL_ChestUI.QuickStack += static il => {
            if (!il.ApplyTo(ILCompleteQuickStack, Configs.BetterQuickStack.CompleteQuickStack)) Configs.UnloadedInventoryManagement.Value.quickStackComplete = true;
            if (!il.ApplyTo(ILNoSkipEmptySlots, Configs.BetterQuickStack.LimitedBanksQuickStack)) Configs.UnloadedInventoryManagement.Value.quickStackLimitedBanks = true;

        };
        IL_Player.QuickStackAllChests += static il => {
            if (!il.ApplyTo(ILCompleteQuickStackAllChests, Configs.BetterQuickStack.CompleteQuickStack)) Configs.UnloadedInventoryManagement.Value.quickStackComplete = true;
        };
        IL_ChestUI.DepositAll += static il => {
            if (!il.ApplyTo(ILCompleteDepositAll, Configs.BetterQuickStack.CompleteQuickStack)) Configs.UnloadedInventoryManagement.Value.quickStackComplete = true;
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
        cursor.EmitDelegate((int upper, Player player) => !Configs.BetterQuickStack.CompleteQuickStack ? upper : player.chest <= -2 ? (InventorySlots.Ammo.End - InventorySlots.Coins.Count) : InventorySlots.Ammo.End);
        cursor.GotoNextLoc(out int lowerBound, i => i.Previous.MatchLdcI4(10), 13);
        cursor.EmitDelegate((int lower) => !Configs.BetterQuickStack.CompleteQuickStack ? lower : 0);
    }

    private static void ILCompleteQuickStackAllChests(ILContext il) {
        ILCursor cursor = new(il);

        for (int i = 0; i < 2; i++) {
            cursor.GotoNext(MoveType.After, i => i.MatchLdcI4(10));
            cursor.EmitDelegate((int lower) => !Configs.BetterQuickStack.CompleteQuickStack ? lower : InventorySlots.Hotbar.Start);
            cursor.GotoNext(MoveType.After, i => i.MatchLdcI4(50));
            cursor.EmitDelegate((int upper) => !Configs.BetterQuickStack.CompleteQuickStack ? upper : InventorySlots.Ammo.End);
        }
    }


    private static void ILCompleteDepositAll(ILContext il) {
        ILCursor cursor = new(il);

        // for (int num = ++[57]; num >= ++[0]; num--)
        cursor.GotoNext(MoveType.After, i => i.MatchLdcI4(49));
        cursor.EmitDelegate((int upper) => !Configs.BetterQuickStack.CompleteQuickStack ? upper : InventorySlots.Ammo.End - 1);
        cursor.GotoNext(MoveType.After, i => i.MatchLdcI4(10));
        cursor.EmitDelegate((int lower) => !Configs.BetterQuickStack.CompleteQuickStack ? lower : InventorySlots.Hotbar.Start);
    }

    private static void ILNoSkipEmptySlots(ILContext context) {
        ILCursor cursor = new(context);

        cursor.GotoNextLoc(out int _, i => i.Previous.MatchNewobj<Dictionary<int, int>>(), 9);
        cursor.FindPrevLoc(out _, out int emptySlots, i => i.Previous.MatchNewobj<List<int>>(), 8);

        cursor.GotoNext(MoveType.After, i => i.MatchCallvirt(Reflection.List<int>.Count.GetMethod) && i.Previous.MatchLdloc(emptySlots));
        cursor.EmitDelegate((int count) => Configs.BetterQuickStack.LimitedBanksQuickStack && Main.LocalPlayer.chest < 0 ? 0 : count);
    }
}