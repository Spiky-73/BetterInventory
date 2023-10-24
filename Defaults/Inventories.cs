using System;
using System.Collections.Generic;
using BetterInventory.DataStructures;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Default;
using Terraria.UI;
using ContextID = Terraria.UI.ItemSlot.Context;

namespace BetterInventory.InventoryManagement.Inventories;


public sealed class Inventory : ModInventory {

    public sealed override void Load() {
        AddSlots(new("Hotbar", ContextID.InventoryItem, p => new ArraySegment<Item>(p.inventory, 0, 10), null));
        AddSlots(new(null, ContextID.InventoryItem, p => new ArraySegment<Item>(p.inventory, 10, 40), null));
        AddSlots(new("Coin", ContextID.InventoryCoin, p => new ArraySegment<Item>(p.inventory, 50, 4), i => i.IsACoin));
        AddSlots(new("Ammo", ContextID.InventoryAmmo, p => new ArraySegment<Item>(p.inventory, 54, 4), i => i.FitsAmmoSlot()));
    }

    public sealed override bool FitsSlot(Player player, Item item, InventorySlots slots, int index, out IList<int> itemsToMove) {
        itemsToMove = Array.Empty<int>();
        return !player.preventAllItemPickups;
    }
    public sealed override Item GetItem(Player player, Item item, GetItemSettings settings) => player.GetItem(player.whoAmI, item, settings);
}

public sealed class Chest : ModInventory {

    public sealed override void Load() {
        // AddSlots(new(null, ContextID.ChestItem, p => new(p.chest >= 0 ? Main.chest[p.chest].item : Array.Empty<Item>(), false), null)); // TODO sync
        AddSlots(new(null, ContextID.BankItem, p => p.chest.InRange(-4, -2) ? p.Chest()! : Array.Empty<Item>(), null));
        AddSlots(new(null, ContextID.VoidItem, p => p.chest == -5 ? p.bank4.item : Array.Empty<Item>(), null));
    }

    public sealed override bool FitsSlot(Player player, Item item, InventorySlots slots, int index, out IList<int> itemsToMove) {
        itemsToMove = Array.Empty<int>();
        return !ChestUI.IsBlockedFromTransferIntoChest(item, player.Chest()!);
    }
    public sealed override Item GetItem(Player player, Item item, GetItemSettings settings) {
        ChestUI.TryPlacingInChest(item, false, ContextID.InventoryItem);
        return item;
    }
}

public sealed class Armor : ModInventory {

    public sealed override void Load() {
        AddSlots(new("Armor", ContextID.EquipArmor, p => new ArraySegment<Item>(p.armor, 0, 1), i => i.defense != 0 && i.headSlot != -1));
        AddSlots(new("Armor", ContextID.EquipArmor, p => new ArraySegment<Item>(p.armor, 1, 1), i => i.defense != 0 && i.bodySlot != -1));
        AddSlots(new("Armor", ContextID.EquipArmor, p => new ArraySegment<Item>(p.armor, 2, 1), i => i.defense != 0 && i.legSlot != -1));
        AddSlots(new("Vanity", ContextID.EquipArmorVanity, p => new ArraySegment<Item>(p.armor, Count + AccessorySlotLoader.MaxVanillaSlotCount + 0, 1), i => i.headSlot != -1));
        AddSlots(new("Vanity", ContextID.EquipArmorVanity, p => new ArraySegment<Item>(p.armor, Count + AccessorySlotLoader.MaxVanillaSlotCount + 1, 1), i => i.bodySlot != -1));
        AddSlots(new("Vanity", ContextID.EquipArmorVanity, p => new ArraySegment<Item>(p.armor, Count + AccessorySlotLoader.MaxVanillaSlotCount + 2, 1), i => i.legSlot != -1));
    }

    public const int Count = 3;
}

public sealed class Accessories : ModInventory {

    public sealed override void Load() { // TODO add modded slots
        // AddSlots("Accessory",
        //     new(ContextID.EquipAccessory, p => new ListIndices<Item>(p.armor, UnlockedVanillaSlots(p)), i => i.accessory),
        //     new(ContextID.ModdedAccessorySlot, p => {
        //         Item[] accs = ModdedAccessories(p);
        //         return new ListIndices<Item>(accs, UnlockedModdedSlots(p));
        //     }, i => i.accessory)
        // );
        // AddSlots("Social",
        //     new(ContextID.EquipAccessoryVanity, p => new ListIndices<Item>(p.armor, UnlockedVanillaSlots(p, Armor.Count*2 + AccessorySlotLoader.MaxVanillaSlotCount)), i => i.accessory),
        //     new(ContextID.ModdedVanityAccessorySlot, p => {
        //         Item[] accs = ModdedAccessories(p);
        //         return new ListIndices<Item>(accs, UnlockedModdedSlots(p, accs.Length/2));
        //     }, i => i.accessory));
    }

    public static List<int> UnlockedVanillaSlots(Player player, int offset = 0) {
        List<int> unlocked = new();
        for (int i = 0; i < AccessorySlotLoader.MaxVanillaSlotCount; i++) if (player.IsItemSlotUnlockedAndUsable(i + Armor.Count)) unlocked.Add(i + Armor.Count + offset);
        return unlocked;
    }
    public static List<int> UnlockedModdedSlots(Player player, int offset = 0) {
        List<int> unlocked = new();
        int length = ModdedAccessories(player).Length;
        for (int i = 0; i < length; i++) if (LoaderManager.Get<AccessorySlotLoader>().ModdedIsItemSlotUnlockedAndUsable(i, player)) unlocked.Add(i + offset);
        return unlocked;
    }

    public sealed override bool FitsSlot(Player player, Item item, InventorySlots slots, int index, out IList<int> itemsToMove) {
        if (!(index < AccessorySlotLoader.MaxVanillaSlotCount ?
                ItemLoader.CanEquipAccessory(item, index + 3, false) :
                ItemLoader.CanEquipAccessory(item, index - AccessorySlotLoader.MaxVanillaSlotCount, true) && LoaderManager.Get<AccessorySlotLoader>().CanAcceptItem(index - AccessorySlotLoader.MaxVanillaSlotCount, item, slots == Slots[0] ? -10 : -11))) {
            itemsToMove = Array.Empty<int>();
            return false;
        }
        itemsToMove = GetIncompatibleItems(player, item, slots, out bool canAllMove);
        return canAllMove;
    }

    public IList<int> GetIncompatibleItems(Player player, Item item, InventorySlots slots, out bool canAllMove) {
        canAllMove = true;

        List<int> incompatibles = new();
        foreach (InventorySlots s in Slots) {
            IList<Item> items = s.Items(player);
            bool equip = slots == Slots[0] && s == Slots[0];
            for (int i = 0; i < items.Count; i++) {
                if (item == items[i]) continue;
                if (item.type != items[i].type && (!equip || (item.wingSlot <= 0 || items[i].wingSlot <= 0) && ItemLoader.CanAccessoryBeEquippedWith(items[i], item))) continue;
                incompatibles.Add(i);
                if (ItemSlot.isEquipLocked(i)) canAllMove = false;
            }
        }
        return incompatibles;
    }

    public static Item[] ModdedAccessories(Player player) => Reflection.ModAccessorySlotPlayer.exAccessorySlot.GetValue(player.GetModPlayer<ModAccessorySlotPlayer>());
}

public sealed class Equipement : ModInventory {
    public sealed override void Load() {
        AddSlots(new("Equipement", ContextID.EquipPet, p => new ArraySegment<Item>(p.miscEquips, 0, 1), i => i.buffType > 0 && Main.vanityPet[i.buffType]));
        AddSlots(new("Equipement", ContextID.EquipLight, p => new ArraySegment<Item>(p.miscEquips, 1, 1), i => i.buffType > 0 && Main.lightPet[i.buffType]));
        AddSlots(new("Equipement", ContextID.EquipMinecart, p => new ArraySegment<Item>(p.miscEquips, 2, 1), i => i.mountType != -1 && MountID.Sets.Cart[i.mountType]));
        AddSlots(new("Equipement", ContextID.EquipMount, p => new ArraySegment<Item>(p.miscEquips, 3, 1), i => i.mountType != -1 && !MountID.Sets.Cart[i.mountType]));
        AddSlots(new("Equipement", ContextID.EquipGrapple, p => new ArraySegment<Item>(p.miscEquips, 4, 1), i => Main.projHook[i.shoot]));
    }
}

public sealed class Dyes : ModInventory {

    public sealed override int? MaxStack => 1;

    public sealed override void Load() {
        AddSlots(new("ArmorDye", ContextID.EquipDye, p => new ArraySegment<Item>(p.dye, 0, Armor.Count), i => i.dye != 0));
        // AddSlots(new("AccessoryDye", ContextID.EquipDye, p => new ListIndices<Item>(p.dye, Accessories.UnlockedVanillaSlots(p)), i => i.dye != 0));
        AddSlots(new("EquipementDye", ContextID.EquipMiscDye, p => p.miscDyes, i => i.dye != 0));
    }
}