using MonoMod.Cil;
using SpikysLib.IL;
using Terraria;
using Terraria.ModLoader;

namespace BetterInventory.BetterItemPickup;

public sealed class FixPickupSlot : ILoadable {

    public bool IsLoadingEnabled(Mod mod) => Compatibility.LoadDisabledFeatures || BetterItemPickupConfig.FixPickupSlot;
    public void Load(Mod mod) {
        IL_Player.GetItem += il => il.TryEdit(ILFixPickupSlot, ref UnloadedBetterItemPickupConfig.Instance.fixPickupSlot);
    }
    public void Unload() { }

    private static void ILFixPickupSlot(ILContext il) {
        ILCursor cursor = new(il);

        cursor.GotoNextLoc(out int returnItem, i => i.Previous.MatchLdarg2(), 1);

        while (cursor.TryGotoNext(MoveType.After, i => i.MatchLdarg2() && i.Next.MatchLdfld(out _))) {
            cursor.EmitLdloc(returnItem);
            cursor.EmitDelegate((Item newItem, Item returnItem) => BetterItemPickupConfig.FixPickupSlot ? returnItem : newItem);
        }
    }

}