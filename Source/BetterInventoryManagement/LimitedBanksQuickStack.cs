using System.Collections.Generic;
using MonoMod.Cil;
using SpikysLib.IL;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace BetterInventory.BetterInventoryManagement;

public sealed class LimitedBanksQuickStack : ILoadable {

    public bool IsLoadingEnabled(Mod mod) => Compatibility.LoadDisabledFeatures || BetterInventoryManagementConfig.LimitedBanksQuickStack;
    public void Load(Mod mod) {
        IL_ChestUI.QuickStack += il => il.TryEdit(ILNoSkipEmptySlots, ref UnloadedBetterInventoryManagementConfig.Instance.limitedBanksQuickStack);
    }
    public void Unload() { }

    private static void ILNoSkipEmptySlots(ILContext context) {
        ILCursor cursor = new(context);

        cursor.GotoNextLoc(out int _, i => i.Previous.MatchNewobj<Dictionary<int, int>>(), 9);
        cursor.FindPrevLoc(out _, out int emptySlots, i => i.Previous.MatchNewobj<List<int>>(), 8);

        cursor.GotoNext(MoveType.After, i => i.MatchGetppt((List<int> l) => l.Count) && i.Previous.MatchLdloc(emptySlots));
        cursor.EmitDelegate((int count) => BetterInventoryManagementConfig.LimitedBanksQuickStack && Main.LocalPlayer.chest < -1 ? 0 : count);
    }
}