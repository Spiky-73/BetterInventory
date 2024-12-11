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
            if(!il.ApplyTo(ILOverrideSlot, Configs.SmartPickup.OverrideSlot)) Configs.UnloadedInventoryManagement.Value.pickupOverrideSlot = true;
            if(!il.ApplyTo(ILDedicatedSlots, Configs.SmartPickup.DedicatedSlot)) Configs.UnloadedInventoryManagement.Value.pickupDedicatedSlot = true;
            if(!il.ApplyTo(ILHotbarLast, Configs.SmartPickup.HotbarLast)) Configs.UnloadedInventoryManagement.Value.hotbarLast = true;
            if(!il.ApplyTo(ILFixNewItem, Configs.SmartPickup.FixSlot)) Configs.UnloadedInventoryManagement.Value.fixSlot = true;
        };
    }

    private static void ILOverrideSlot(ILContext il) {
        ILCursor cursor = new(il);

        cursor.GotoNextLoc(out int coin, i => i.Previous.MatchCallvirt(Reflection.Item.IsACoin.GetMethod!), 0);
        cursor.GotoNextLoc(out int newitem, i => i.Previous.MatchLdarg2(), 1);

        // ...
        // if (newItem.uniqueStack && this.HasItem(newItem.type)) return item;
        cursor.GotoNext(i => i.SaferMatchCall(Reflection.Player.HasItem));
        cursor.GotoNext(MoveType.AfterLabel, i => i.MatchLdloc(coin));

        // ++ item = <previousSlot>
        EmitSmartPickup(cursor, newitem, (Player self, Item item, GetItemSettings settings) => {
            if( vanillaGetItem) return item;
            if (Configs.PreviousSlot.Enabled) item = PreviousSlot.PickupItemToPreviousSlot(self, item, settings);
            return item;
        });
    }

    private static void ILDedicatedSlots(ILContext il) {
        ILCursor cursor = new(il);

        cursor.GotoNextLoc(out int coin, i => i.Previous.MatchCallvirt(Reflection.Item.IsACoin.GetMethod!), 0);
        cursor.GotoNextLoc(out int newitem, i => i.Previous.MatchLdarg2(), 1);

        // if (isACoin) ...
        // if (item.FitsAmmoSlot()) ...
        // for(...) ...
        cursor.GotoNext(i => i.SaferMatchCall(Reflection.Player.GetItem_FillEmptyInventorySlot));
        cursor.GotoPrev(MoveType.AfterLabel, i => i.MatchLdloc(coin));

        // ++<upgradeItems>
        EmitSmartPickup(cursor, newitem, (Player self, Item item, GetItemSettings settings) => {
            if (vanillaGetItem || settings.NoText) return item;
            if (Configs.UpgradeItems.Enabled && !item.IsAir) item = SmartEquip.UpgradeItems(self, item, settings);
            if (Configs.SmartPickup.AutoEquip && !item.IsAir) item = SmartEquip.AutoEquip(self, item, settings);
            return item;
        });
    }
    private static void ILFixNewItem(ILContext il) {
        ILCursor cursor = new(il);

        cursor.GotoNextLoc(out int newitem, i => i.Previous.MatchLdarg2(), 1);

        cursor.GotoNext(MoveType.After, i => i.MatchStloc(newitem));
        while (cursor.TryGotoNext(MoveType.After, i => i.MatchLdarg2() && i.Next.MatchLdfld(out _))) {
            cursor.EmitLdloc(newitem);
            cursor.EmitDelegate((Item newItem, Item item) => Configs.SmartPickup.FixSlot ? item : newItem);
            cursor.GotoNext(MoveType.After, i => i.Next.MatchLdfld(out _));
        }
    }

    private static void EmitSmartPickup(ILCursor cursor, int newitem, Func<Player, Item, GetItemSettings, Item> cb) {
        cursor.EmitLdarg0();
        cursor.EmitLdloc(newitem);
        cursor.EmitLdarg3();
        cursor.EmitDelegate(cb);
        cursor.EmitDup();
        cursor.EmitStloc(newitem);

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