using MonoMod.Cil;
using SpikysLib.IL;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace BetterInventory.BetterItemPickup;

public sealed class PickupHotbarLast : ILoadable {

    public bool IsLoadingEnabled(Mod mod) => Compatibility.LoadDisabledFeatures || BetterItemPickupConfig.PickupHotbarLast;
    public void Load(Mod mod) {
        IL_Player.GetItem += il => il.TryEdit(ILPickupHotbarLast, ref UnloadedBetterItemPickupConfig.Instance.pickupHotbarLast);
    }
    public void Unload() { }

    private static void ILPickupHotbarLast(ILContext il) {
        ILCursor cursor = new(il);

        cursor.GotoNextLoc(out int newItem, i => i.Previous.MatchLdarg2(), 1);

        // if (!isACoin ++[&& !<hotbarLast>] && newItem.useStyle != 0) <hotbar>
        cursor.GotoNext(MoveType.After, i => i.MatchLdfld((Item i) => i.useStyle));
        cursor.EmitDelegate((int style) => BetterItemPickupConfig.PickupHotbarLast ? ItemUseStyleID.None : style);
    }

}