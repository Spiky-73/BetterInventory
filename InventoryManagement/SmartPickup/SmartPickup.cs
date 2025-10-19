using MonoMod.Cil;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System;
using SpikysLib.IL;
using BetterInventory.Default.Inventories;
using Terraria.UI;
using System.Collections.Generic;
using Terraria.DataStructures;

namespace BetterInventory.InventoryManagement.SmartPickup;

public sealed class SmartPickup : ModSystem {

    public override void Load() {
        IL_Player.GetItem += static il => {
            if (!il.ApplyTo(ILGetItem, Configs.SmartPickup.OverrideSlot)) Configs.UnloadedInventoryManagement.Value.pickupOverrideSlot = true;
            if (!il.ApplyTo(ILGetItemWorld, Configs.SmartPickup.DedicatedSlot)) Configs.UnloadedInventoryManagement.Value.pickupDedicatedSlot = true;
            if (!il.ApplyTo(ILHotbarLast, Configs.SmartPickup.HotbarLast)) Configs.UnloadedInventoryManagement.Value.hotbarLast = true;
            if (!il.ApplyTo(ILFixNewItem, Configs.SmartPickup.FixSlot)) Configs.UnloadedInventoryManagement.Value.fixSlot = true;
        };
        On_Item.CanFillEmptyAmmoSlot += HookForceSkipEmptySlots;

        On_ChestUI.TryPlacingInChest += HookTryPlacingInChest;
        // On_ItemSlot.EquipSwap += HookEquipSwap; // Not need a each item only goes to a single slot
        // On_ItemSlot.DyeSwap += HookDyeSwap // Unused code, probably does not work either?
        On_ItemSlot.ArmorSwap += HookArmorSwap;
        // On_ItemSlot.AccessorySwap += HookAccessorySwap; // Handled in HookArmorSwap

        On_ChestUI.LootAll += HookQuickStackLootAll;
        On_ChestUI.QuickStack += HookNoQuickStackToSameChest;
    }

    private static void ILGetItem(ILContext il) {
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
            if (!item.IsAir && Configs.SmartPickup.PreviousSlot && IsGetItemWorld(self, settings, item)) item = self.GetModPlayer<PreviousSlotPlayer>().PickupItemToAnyPreviousSlot(item, settings);
            if (!item.IsAir && Configs.SmartPickup.PreviousSlot) item = self.GetModPlayer<PreviousSlotPlayer>().PickupItemToPreviousSlot(
                item, settings,
                ModContent.GetInstance<Hotbar>().NewInstance(self),
                ModContent.GetInstance<Ammo>().NewInstance(self),
                ModContent.GetInstance<Coins>().NewInstance(self),
                ModContent.GetInstance<Inventory>().NewInstance(self)
            );

            return item;
        });
    }

    private static void ILGetItemWorld(ILContext il) {
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
            if (vanillaGetItem || !IsGetItemWorld(self, settings, item)) return item;
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

    private static bool HookTryPlacingInChest(On_ChestUI.orig_TryPlacingInChest orig, Item item, bool justCheck, int itemSlotContext) {
        ChestUI.GetContainerUsageInfo(out _, out Item[] chestInventory);
        if (justCheck || ChestUI.IsBlockedFromTransferIntoChest(item, chestInventory)) return orig(item, justCheck, itemSlotContext); ;

        if (!item.IsAir && Configs.SmartPickup.PreviousSlot) item = Main.LocalPlayer.GetModPlayer<PreviousSlotPlayer>().PickupItemToPreviousSlot(
            item,
            GetItemSettings.InventoryEntityToPlayerInventorySettings,
            [.. ModContent.GetInstance<Default.Inventories.Chest>().GetActiveInventories(Main.LocalPlayer)]
        );
        if (item.IsAir) return true;
        return orig(item, justCheck, itemSlotContext);
    }

    private static Item HookArmorSwap(On_ItemSlot.orig_ArmorSwap orig, Item item, out bool success) {
        if (item.stack < 1 || (item.headSlot == -1 && item.bodySlot == -1 && item.legSlot == -1 && !item.accessory)) return orig(item, out success);
        IEnumerable<ModSubInventory> armorInventories;
        IEnumerable<ModSubInventory> vanityInventories;
        Player player = Main.LocalPlayer;

        if (item.headSlot != -1) {
            armorInventories = ModContent.GetInstance<HeadArmor>().GetInventories(player);
            vanityInventories = ModContent.GetInstance<HeadVanity>().GetInventories(player);
        } else if (item.bodySlot != -1) {
            armorInventories = ModContent.GetInstance<BodyArmor>().GetInventories(player);
            vanityInventories = ModContent.GetInstance<BodyVanity>().GetInventories(player);
        } else if (item.legSlot != -1) {
            armorInventories = ModContent.GetInstance<LegArmor>().GetInventories(player);
            vanityInventories = ModContent.GetInstance<LegVanity>().GetInventories(player);
        } else if (item.accessory) {
            armorInventories = ModContent.GetInstance<Accessories>().GetInventories(player);
            vanityInventories = ModContent.GetInstance<VanityAccessories>().GetInventories(player);
        } else {
            return orig(item, out success);
        }
        item = player.GetModPlayer<PreviousSlotPlayer>().PickupItemToPreviousSlot(
            item,
            GetItemSettings.InventoryEntityToPlayerInventorySettings,
            [.. armorInventories, ..vanityInventories]
        );
        return orig(item, out success);
    }

    private static void HookQuickStackLootAll(On_ChestUI.orig_LootAll orig) {
        if (Configs.SmartPickup.QuickStack) _skippedQuickStack = Main.LocalPlayer.chest;
        orig();
        _skippedQuickStack = -1;
    }

    private static void HookNoQuickStackToSameChest(On_ChestUI.orig_QuickStack orig, ContainerTransferContext context, bool voidStack) {
        if (Main.LocalPlayer.chest == _skippedQuickStack) return;
        orig(context, voidStack);
    }
    private static int _skippedQuickStack = -1;

    private static bool IsGetItemWorld(Player player, GetItemSettings settings, Item item) => !settings.NoText || item == Main.mouseItem || item == player.HeldItem;

}