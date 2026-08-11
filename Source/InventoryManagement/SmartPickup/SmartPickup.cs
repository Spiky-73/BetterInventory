using MonoMod.Cil;
using Terraria;
using Terraria.ModLoader;
using System;
using SpikysLib.IL;
using BetterInventory.Default.Inventories;
using Terraria.UI;
using System.Collections.Generic;

namespace BetterInventory.InventoryManagement.SmartPickup;

public sealed class SmartPickupPlayer : ModPlayer {

    public override void Load() {
        IL_Player.GetItem += static il => {
            if (!il.ApplyTo(ILGetItem, Configs.SmartPickup.OverrideSlot)) Configs.UnloadedInventoryManagement.Value.pickupOverrideSlot = true;
            if (!il.ApplyTo(ILGetItemWorld, Configs.SmartPickup.DedicatedSlot)) Configs.UnloadedInventoryManagement.Value.pickupDedicatedSlot = true;
        };

        On_ChestUI.TryPlacingInChest += HookTryPlacingInChest;
        On_ItemSlot.ArmorSwap += HookArmorSwap;

        On_Recipe.FindRecipes += HookUpdateLockedItems;
    }

    private static void ILGetItem(ILContext il) {
        ILCursor cursor = new(il);

        cursor.GotoNextLoc(out int coin, i => i.Previous.MatchCallvirt(Reflection.Item.IsACoin.GetMethod!), 0);
        cursor.GotoNextLoc(out int returnItem, i => i.Previous.MatchLdarg2(), 1);

        // ...
        // if (newItem.uniqueStack && this.HasItem(newItem.type)) return item;
        cursor.GotoNext(i => i.SaferMatchCall(Reflection.Player.HasItem));
        cursor.GotoNext(MoveType.AfterLabel, i => i.MatchLdloc(coin));

        // ++ item = <previousSlot>
        EmitSmartPickup(cursor, returnItem, (self, plr, item, settings) => {
            if (vanillaGetItem) return item;
            if (!item.IsAir && Configs.SmartPickup.PreviousSlot && (IsGetItemWorld(self, settings, item) || item == Main.mouseItem || item == self.HeldItem)) item = self.GetModPlayer<PreviousSlotPlayer>().PickupItemToAnyPreviousSlot(item, settings);
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
        cursor.GotoNextLoc(out int returnItem, i => i.Previous.MatchLdarg2(), 1);

        // for(...) ...
        cursor.GotoNext(i => i.SaferMatchCall(Reflection.Player.GetItem_FillEmptyInventorySlot));
        cursor.GotoPrev(MoveType.AfterLabel, i => i.MatchLdloc(coin));

        // ++<upgradeItems>
        EmitSmartPickup(cursor, returnItem, (self, plr, item, settings) => {
            if (vanillaGetItem || !IsGetItemWorld(self, settings, item)) return item;
            if (!item.IsAir && Configs.SmartPickup.UpgradeItems) item = SmartEquip.UpgradeItems(self, item, settings);
            if (!item.IsAir && Configs.SmartPickup.AutoEquip) item = SmartEquip.AutoEquip(self, item, settings);
            return item;
        });
    }

    private static void EmitSmartPickup(ILCursor cursor, int returnItem, Func<Player, int, Item, GetItemSettings, Item> cb) {
        cursor.EmitLdarg0();
        cursor.EmitLdarg1();
        cursor.EmitLdloc(returnItem);
        cursor.EmitLdarg3();
        cursor.EmitDelegate(cb);
        cursor.EmitDup();
        cursor.EmitStloc(returnItem);

        // ++if (newItem.IsAir) return new()
        cursor.EmitDelegate((Item item) => item.IsAir);
        ILLabel skip = cursor.DefineLabel();
        cursor.EmitBrfalse(skip);
        cursor.EmitDelegate(() => new Item());
        cursor.EmitRet();
        cursor.MarkLabel(skip);
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
            [.. armorInventories, .. vanityInventories]
        );
        return orig(item, out success);
    }

    private static bool IsGetItemWorld(Player player, GetItemSettings settings, Item item) => !settings.NoText;

    private static void HookUpdateLockedItems(On_Recipe.orig_FindRecipes orig, bool canDelayCheck) {
        if (!canDelayCheck) SmartEquip.UpdateLockedItems(Main.LocalPlayer);
        orig(canDelayCheck);
    }
}