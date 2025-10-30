using System;
using MonoMod.Cil;
using SpikysLib.IL;
using Terraria;
using Terraria.ModLoader;

namespace BetterInventory.VanillaFixes;

public sealed class AmmoPickup : ILoadable {

    public bool IsLoadingEnabled(Mod mod) => !Configs.Compatibility.CompatibilityMode || Configs.VanillaFixes.ConsistantScrollDirection;
    public void Load(Mod mod) {
        IL_Player.GetItem += static il => {
            if (!il.ApplyTo(ILDelayAmmoPickup, Configs.VanillaFixes.AmmoPickup)) Configs.UnloadedVanillaFixes.Instance.ammoPickup = true;
        };

        On_Item.CanFillEmptyAmmoSlot += HookForceSkipEmptySlots;
    }
    public void Unload() { }


    private static void ILDelayAmmoPickup(ILContext il) {
        ILCursor cursor = new(il);

        cursor.GotoNextLoc(out int isACoin, i => i.Previous.MatchCallvirt(Reflection.Item.IsACoin.GetMethod!), 0);
        cursor.GotoNextLoc(out int item, i => i.Previous.MatchLdarg2(), 1);

        // if (isACoin) ...
        // if (item.FitsAmmoSlot()) {
        //     <fill ++[OCCUPIED] slots>
        // }
        cursor.GotoNext(MoveType.AfterLabel, i => i.SaferMatchCall(Reflection.Player.FillAmmo));
        cursor.EmitDelegate<Action>(() => _forceSkipEmptyAmmoSlots = Configs.VanillaFixes.AmmoPickup);
        cursor.GotoNext(MoveType.After, i => i.SaferMatchCall(Reflection.Player.FillAmmo));
        cursor.EmitDelegate<Action>(() => _forceSkipEmptyAmmoSlots = false);

        // <occupied slot>
        // <hotbar>
        cursor.GotoNext(i => i.SaferMatchCall(Reflection.Player.GetItem_FillEmptyInventorySlot));
        cursor.GotoPrev(i => i.MatchLdfld(Reflection.Item.favorited));
        cursor.GotoPrev(MoveType.AfterLabel, i => i.MatchLdarg2());

        // ++<ammo pickup>
        cursor.EmitLdarg0().EmitLdarg1().EmitLdloc(item).EmitLdarg3();
        cursor.EmitDelegate((Player self, int plr, Item item, GetItemSettings settings) => {
            if (!Configs.VanillaFixes.AmmoPickup || !item.FitsAmmoSlot()) return false;
            item = self.FillAmmo(plr, item, settings);
            return item.IsAir;
        });
        ILLabel skip = cursor.DefineLabel();
        cursor.EmitBrfalse(skip);
        cursor.EmitDelegate(() => new Item());
        cursor.EmitRet();
        cursor.MarkLabel(skip);
    }

    private static bool HookForceSkipEmptySlots(On_Item.orig_CanFillEmptyAmmoSlot orig, Item self) => !_forceSkipEmptyAmmoSlots && orig(self);

    private static bool _forceSkipEmptyAmmoSlots;
}