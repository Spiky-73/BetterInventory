using MonoMod.Cil;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System;
using SpikysLib.IL;

namespace BetterInventory.InventoryManagement.SmartPickup;

public sealed class SmartPickup : ModSystem {

    public override void Load() {
        IL_Player.GetItem += static il => {
            if (!il.ApplyTo(ILOverrideSlot, Configs.SmartPickup.OverrideSlot)) Configs.UnloadedInventoryManagement.Value.pickupOverrideSlot = true;
            if (!il.ApplyTo(ILDedicatedSlots, Configs.SmartPickup.DedicatedSlot)) Configs.UnloadedInventoryManagement.Value.pickupDedicatedSlot = true;
            if (!il.ApplyTo(ILHotbarLast, Configs.SmartPickup.HotbarLast)) Configs.UnloadedInventoryManagement.Value.hotbarLast = true;
            if (!il.ApplyTo(ILFixNewItem, Configs.SmartPickup.FixSlot)) Configs.UnloadedInventoryManagement.Value.fixSlot = true;
        };
        On_Item.CanFillEmptyAmmoSlot += HookForceSkipEmptySlots;
    }

    private static void ILOverrideSlot(ILContext il) {
        ILCursor cursor = new(il);

        cursor.GotoNextLoc(out int coin, i => i.Previous.MatchCallvirt(Reflection.Item.IsACoin.GetMethod!), 0);
        cursor.GotoNextLoc(out int newItem, i => i.Previous.MatchLdarg2(), 1);

        // ...
        // if (newItem.uniqueStack && this.HasItem(newItem.type)) return item;
        cursor.GotoNext(i => i.SaferMatchCall(Reflection.Player.HasItem));
        cursor.GotoNext(MoveType.AfterLabel, i => i.MatchLdloc(coin));

        // ++ item = <previousSlot>
        EmitSmartPickup(cursor, newItem, (Player self, int plr, Item item, GetItemSettings settings) => {
            if (vanillaGetItem) return item;
            if (!item.IsAir && Configs.SmartPickup.RefillMouse) item = SmartEquip.RefillMouse(self, item, settings);
            if (!item.IsAir && Configs.SmartPickup.PreviousSlot) item = self.GetModPlayer<PreviousSlotPlayer>().PickupItemToPreviousSlot(self, item, settings);
            return item;
        });
    }

    private static void ILDedicatedSlots(ILContext il) {
        ILCursor cursor = new(il);

        cursor.GotoNextLoc(out int coin, i => i.Previous.MatchCallvirt(Reflection.Item.IsACoin.GetMethod!), 0);
        cursor.GotoNextLoc(out int newItem, i => i.Previous.MatchLdarg2(), 1);

        // if (isACoin) ...
        // if (item.FitsAmmoSlot() ++[&& false]) ...
        cursor.GotoNext(MoveType.AfterLabel, i => i.SaferMatchCall(Reflection.Player.FillAmmo));
        cursor.EmitLdarg3();
        cursor.EmitDelegate<Action<GetItemSettings>>(settings => _forceSkipEmptyAmmoSlots = !(vanillaGetItem || settings.NoText) && Configs.SmartPickup.FixAmmo);
        cursor.GotoNext(MoveType.After, i => i.SaferMatchCall(Reflection.Player.FillAmmo));
        cursor.EmitDelegate<Action>(() => _forceSkipEmptyAmmoSlots = false);

        // for(...) ...
        cursor.GotoNext(i => i.SaferMatchCall(Reflection.Player.GetItem_FillEmptyInventorySlot));
        cursor.GotoPrev(MoveType.AfterLabel, i => i.MatchLdloc(coin));

        // ++<upgradeItems>
        EmitSmartPickup(cursor, newItem, (Player self, int plr, Item item, GetItemSettings settings) => {
            if (vanillaGetItem || settings.NoText) return item;
            if (!item.IsAir && Configs.SmartPickup.QuickStack) item = SmartEquip.QuickStack(self, item, settings);
            if (!item.IsAir && Configs.SmartPickup.FixAmmo && item.FitsAmmoSlot()) item = self.FillAmmo(plr, item, settings);
            if (!item.IsAir && Configs.SmartPickup.UpgradeItems) item = SmartEquip.UpgradeItems(self, item, settings);
            if (!item.IsAir && Configs.SmartPickup.AutoEquip) item = SmartEquip.AutoEquip(self, item, settings);
            if (!item.IsAir && !item.favorited && Configs.SmartPickup.VoidBagFirst) item = SmartEquip.VoidBagFirst(self, item, settings);
            return item;
        });
    }
    private static bool HookForceSkipEmptySlots(On_Item.orig_CanFillEmptyAmmoSlot orig, Item self) => !_forceSkipEmptyAmmoSlots && orig(self);
    private static bool _forceSkipEmptyAmmoSlots;

    private static void ILFixNewItem(ILContext il) {
        ILCursor cursor = new(il);

        cursor.GotoNextLoc(out int newItem, i => i.Previous.MatchLdarg2(), 1);

        cursor.GotoNext(MoveType.After, i => i.MatchStloc(newItem));
        while (cursor.TryGotoNext(MoveType.After, i => i.MatchLdarg2() && i.Next.MatchLdfld(out _))) {
            cursor.EmitLdloc(newItem);
            cursor.EmitDelegate((Item newItem, Item item) => Configs.SmartPickup.FixSlot ? item : newItem);
            cursor.GotoNext(MoveType.After, i => i.Next.MatchLdfld(out _));
        }
    }

    private static void EmitSmartPickup(ILCursor cursor, int newItem, Func<Player, int, Item, GetItemSettings, Item> cb) {
        cursor.EmitLdarg0();
        cursor.EmitLdarg1();
        cursor.EmitLdloc(newItem);
        cursor.EmitLdarg3();
        cursor.EmitDelegate(cb);
        cursor.EmitDup();
        cursor.EmitStloc(newItem);

        // ++if (newItem.IsAir) return new()
        cursor.EmitDelegate((Item item) => item.IsAir);
        ILLabel skip = cursor.DefineLabel();
        cursor.EmitBrfalse(skip);
        cursor.EmitDelegate(() => new Item());
        cursor.EmitRet();
        cursor.MarkLabel(skip);
    }

    private static void ILHotbarLast(ILContext il) {
        ILCursor cursor = new(il);

        // if (!isACoin ++[&& !<hotbarLast>] && newItem.useStyle != 0) <hotbar>
        cursor.GotoNext(MoveType.After, i => i.MatchLdfld(Reflection.Item.useStyle));

        cursor.EmitDelegate((int style) => {
            if (Configs.SmartPickup.HotbarLast) return ItemUseStyleID.None;
            return style;
        });
    }

    internal static bool vanillaGetItem;
}