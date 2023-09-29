using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Default;
using Terraria.UI;

using ContextID = Terraria.UI.ItemSlot.Context;

namespace BetterInventory.InventoryManagement;

public sealed partial class ItemSwap {
    public static bool Enabled => Configs.ClientConfig.Instance.itemSwap;
    public delegate bool TrySwapItemFn(Player player, ref Item item, int destSlot);


    public static readonly string[] SwapSlots = new[] {
        "Hotbar1",
        "Hotbar2",
        "Hotbar3",
        "Hotbar4",
        "Hotbar5",
        "Hotbar6",
        "Hotbar7",
        "Hotbar8",
        "Hotbar9",
        "Hotbar10"
    };
    public static readonly List<TrySwapItemFn> Swappers = new() {
        TrySwapAmmo,
        // TrySwapDye,
        TrySwapArmor,
        TrySwapArmorVanity,
        TrySwapAccessory,
        TrySwapAccessoryVanity,
        TrySwapEquip,
        TrySwapHotbar,
    };

    public static readonly HashSet<int> SwappableContexts = new(){
        ContextID.InventoryItem, ContextID.ChestItem, ContextID.BankItem,
        ContextID.InventoryAmmo, ContextID.InventoryCoin,
        // ContextID.EquipArmor, ContextID.EquipAccessoryVanity,
        ContextID.EquipAccessory, ContextID.EquipAccessoryVanity,
        // ContextID.EquipGrapple, ContextID.EquipMount, ContextID.EquipMinecart, ContextID.EquipPet, ContextID.EquipLight,
        // ContextID.EquipDye, ContextID.ModdedDyeSlot, ContextID.EquipMiscDye,
    };

    public static bool HoverSlot(Player player, Item[] inventory, int context, int slot) { // TODO chained swaps
        if (Configs.ClientConfig.Instance.itemSwap && SwappableContexts.Contains(context) && !inventory[slot].IsAir) {
            for (int destSlot = 0; destSlot < SwapSlots.Length; destSlot++) {
                if (!PlayerInput.Triggers.JustPressed.KeyStatus[SwapSlots[destSlot]]) continue;
                Swappers.Find(c => c(player, ref inventory[slot], destSlot));
                break;
            }
        }
        return false;
    }
    public static bool TrySwapHotbar(Player player, ref Item item, int destSlot) {
        Utils.Swap(ref item, ref player.inventory[Math.Clamp(destSlot, 0, 9)]);
        return true;
    }

    public static bool TrySwapAmmo(Player player, ref Item item, int destSlot) {
        if (!item.FitsAmmoSlot()) return false;
        item.favorited = false;
        Utils.Swap(ref item, ref player.inventory[54 + Math.Clamp(destSlot, 0, 3)]);
        return true;
    }

    public static bool TrySwapArmor(Player player, ref Item item, int _) {
        if (item.vanity) return false;
        int s;
        if (item.headSlot != -1) s = 0;
        else if (item.bodySlot != -1) s = 1;
        else if (item.legSlot != -1) s = 2;
        else return false;

        item.favorited = false;
        Main.EquipPageSelected = 0;
        Utils.Swap(ref item, ref player.armor[s]);
        return true;
    }
    public static bool TrySwapArmorVanity(Player player, ref Item item, int _) {
        int s;
        if (item.headSlot != -1) s = 0;
        else if (item.bodySlot != -1) s = 1;
        else if (item.legSlot != -1) s = 2;
        else return false;

        item.favorited = false;
        Main.EquipPageSelected = 0;
        Utils.Swap(ref item, ref player.armor[10 + s]);
        return true;
    }

    public static bool TrySwapAccessory(Player player, ref Item item, int destSlot) => TrySwapAccessory(player, ref item, destSlot, false);
    public static bool TrySwapAccessoryVanity(Player player, ref Item item, int destSlot) => TrySwapAccessory(player, ref item, destSlot, true);
    private static bool TrySwapAccessory(Player player, ref Item item, int destSlot, bool vanity) {
        if (!item.accessory) return false;

        AccessorySlotLoader accLoader = LoaderManager.Get<AccessorySlotLoader>();
        Item[] accessories = Reflection.ModAccessorySlotPlayer.exAccessorySlot.GetValue(player.GetModPlayer<ModAccessorySlotPlayer>());

        (int vOffset, int mOffset) = vanity ? (13, accessories.Length-VanillaAccCount) : (3, -VanillaAccCount);

        int back = destSlot;
        while (destSlot < VanillaAccCount && !player.IsItemSlotUnlockedAndUsable(destSlot + vOffset)) destSlot++;
        if (destSlot >= VanillaAccCount) while (destSlot - VanillaAccCount < accessories.Length && !accLoader.ModdedIsItemSlotUnlockedAndUsable(destSlot + mOffset, player)) destSlot++;
        if (destSlot >= VanillaAccCount + accessories.Length) {
            destSlot = back;
            while (destSlot >= VanillaAccCount && (accessories.Length == 0 || !accLoader.ModdedIsItemSlotUnlockedAndUsable(destSlot + mOffset, player))) destSlot--;
            if (destSlot < VanillaAccCount) while (destSlot >= 0 && !player.IsItemSlotUnlockedAndUsable(destSlot + vOffset)) destSlot--;
            if (destSlot < 0) return false;
        }

        (Item[] accInv, int accIndex) = destSlot < VanillaAccCount ? (player.armor, destSlot + vOffset) : (accessories, destSlot + mOffset);
        if (destSlot < VanillaAccCount ? !ItemLoader.CanEquipAccessory(item, accIndex, false) : (!accLoader.CanAcceptItem(accIndex, item, (accIndex < accessories.Length / 2) ? -10 : -11) || !ItemLoader.CanEquipAccessory(item, accIndex, true))) return true;


        for (int i = accessories.Length - 1; i >= 0; i--) {
            if (item == accessories[i]) continue;
            if (item.type == accessories[i].type || i < accessories.Length / 2 && ((item.wingSlot > 0 && accessories[i].wingSlot > 0) || !ItemLoader.CanAccessoryBeEquippedWith(accessories[i], item))) {
                if (ItemSlot.isEquipLocked(accessories[i].type)) return true;
                if (!accLoader.CanAcceptItem(i, accInv[accIndex], (i < accessories.Length / 2) ? -10 : -11) || !ItemLoader.CanEquipAccessory(accInv[accIndex], i, true)) Utility.GetDropItem(player, ref accInv[accIndex]);
                Utils.Swap(ref accInv[accIndex], ref accessories[i]);
                goto swap;
            }
        }

        Item[] armor = player.armor;
        for (int i = armor.Length - 1; i >= 0; i--) {
            if (item == armor[i]) continue;
            if (item.type == armor[i].type || i < VanillaAccCount + 3 && ((item.wingSlot > 0 && armor[i].wingSlot > 0) || !ItemLoader.CanAccessoryBeEquippedWith(armor[i], item))) {
                if (ItemSlot.isEquipLocked(armor[i].type)) return true;
                if (!ItemLoader.CanEquipAccessory(accInv[accIndex], i, false)) Utility.GetDropItem(player, ref accInv[accIndex]);
                Utils.Swap(ref accInv[accIndex], ref armor[i]);
                goto swap;
            }
        }

    swap:
        item.favorited = false;
        Utils.Swap(ref item, ref accInv[accIndex]);
        return true;
    }

    public static bool TrySwapEquip(Player player, ref Item item, int destSlot) {
        int s;
        if (item.buffType > 0 && Main.vanityPet[item.buffType]) s = 0;
        else if (item.buffType > 0 && Main.lightPet[item.buffType]) s = 1;
        else if (item.mountType != -1 && MountID.Sets.Cart[item.mountType]) s = 2;
        else if (item.mountType != -1 && !MountID.Sets.Cart[item.mountType]) s = 3;
        else if (Main.projHook[item.shoot]) s = 4;
        else return false;

        item.favorited = false;
        Main.EquipPageSelected = 2;
        Utils.Swap(ref item, ref player.miscEquips[s]);
        return true;
    }

    public const int VanillaAccCount = 7;
}