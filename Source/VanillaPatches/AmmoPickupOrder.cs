using MonoMod.Cil;
using SpikysLib.IL;
using Terraria;
using Terraria.ModLoader;

namespace BetterInventory.VanillaPatches;

public sealed class AmmoPickupOrder : ILoadable {

    public bool IsLoadingEnabled(Mod mod) => Compatibility.LoadDisabledFeatures || VanillaPatchesConfig.AmmoPickupOrder;
    public void Load(Mod mod) {
        IL_Player.GetItem += il => il.TryEdit(ILDelayAmmoPickup, ref UnloadedVanillaPatchesConfig.Instance.ammoPickupOrder);
        On_Item.CanFillEmptyAmmoSlot += HookForceSkipEmptySlots;
    }
    public void Unload() { }

    private static void ILDelayAmmoPickup(ILContext il) {
        ILCursor cursor = new(il);

        cursor.GotoNextLoc(out int isACoin, i => i.Previous.MatchGetppt((Item i) => i.IsACoin), 0);
        cursor.GotoNextLoc(out int item, i => i.Previous.MatchLdarg2(), 1);

        // if (isACoin) ...
        // if (item.FitsAmmoSlot()) {
        //     <fill ++[OCCUPIED] slots>
        // }
        cursor.GotoNext(MoveType.AfterLabel, i => i.SaferMatchCall((Player i) => i.FillAmmo));
        cursor.EmitDelegate(() => {
            if (!VanillaPatchesConfig.AmmoPickupOrder) return;
            _forceSkipEmptyAmmoSlots = true;
        });
        cursor.GotoNext(MoveType.After, i => i.SaferMatchCall((Player i) => i.FillAmmo));
        cursor.EmitDelegate(() => {
            if (!VanillaPatchesConfig.AmmoPickupOrder) return;
            _forceSkipEmptyAmmoSlots = false;
        });

        // <occupied slot>
        // <hotbar>
        cursor.GotoNext(i => i.SaferMatchCall((Player i) => i.GetItem_FillEmptyInventorySlot));
        cursor.GotoNext(i => i.MatchLdfld((Item i) => i.favorited));
        cursor.GotoPrev(MoveType.AfterLabel, i => i.MatchLdarg2());

        // ++<ammo pickup>
        cursor.EmitLdarg0().EmitLdarg1().EmitLdloc(item).EmitLdarg3();
        cursor.EmitDelegate((Player self, int plr, Item item, GetItemSettings settings) => {
            if (!VanillaPatchesConfig.AmmoPickupOrder || !item.FitsAmmoSlot()) return false;
            item = self.FillAmmo(plr, item, settings);
            return item.IsAir;
        });
        ILLabel skip = cursor.DefineLabel();
        cursor.EmitBrfalse(skip);
        cursor.EmitDelegate(() => new Item());
        cursor.EmitRet();
        cursor.MarkLabel(skip);
    }

    private static bool HookForceSkipEmptySlots(On_Item.orig_CanFillEmptyAmmoSlot orig, Item self) {
        if (!VanillaPatchesConfig.AmmoPickupOrder) return orig(self);
        return !_forceSkipEmptyAmmoSlots && orig(self);
    }

    private static bool _forceSkipEmptyAmmoSlots;
}