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
        AddSlots(ContextID.InventoryItem, p => new ListIndices<Item>(p.inventory, new DataStructures.Range(0, 9)), "Hotbar");
        AddSlots(ContextID.InventoryItem, p => new ListIndices<Item>(p.inventory, new DataStructures.Range(10, 49)));
        AddSlots(ContextID.InventoryCoin, p => new ListIndices<Item>(p.inventory, new DataStructures.Range(50, 53)), "Coin", i => i.IsACoin);
        AddSlots(ContextID.InventoryAmmo, p => new ListIndices<Item>(p.inventory, new DataStructures.Range(54, 57)), "Ammo", i => i.FitsAmmoSlot());
    }

    public sealed override bool FitsSlot(Player player, Item item, InventorySlots slots, int index, out IList<int> itemsToMove) {
        itemsToMove = Array.Empty<int>();
        return !player.preventAllItemPickups;
    }
    public sealed override Item GetItem(Player player, Item item, GetItemSettings settings, Configs.InventoryManagement.AutoEquipLevel filterSlots = Configs.InventoryManagement.AutoEquipLevel.Off) {
        if (filterSlots != Configs.InventoryManagement.AutoEquipLevel.Off) return base.GetItem(player, item, settings, filterSlots);
        Item i = BetterPlayer.GetItem_Inner(player, player.whoAmI, item, settings);
        return i;
    }

    public override void Focus(Player player, InventorySlots slots, int slot) {
        if (slots == Slots[0]) player.selectedItem = slot;
    }
}

public sealed class Chest : ModInventory {

    public sealed override void Load() {
        AddSlots(ContextID.ChestItem, p => new(p.chest >= 0 ? Main.chest[p.chest].item : Array.Empty<Item>()));
        AddSlots(ContextID.BankItem, p => new(p.chest.InRange(-4, -2) ? p.Chest()! : Array.Empty<Item>()));
        AddSlots(ContextID.VoidItem, p => new(p.chest == -5 ? p.bank4.item : Array.Empty<Item>()));
    }

    public sealed override bool FitsSlot(Player player, Item item, InventorySlots slots, int index, out IList<int> itemsToMove) {
        itemsToMove = Array.Empty<int>();
        return !ChestUI.IsBlockedFromTransferIntoChest(item, player.Chest()!);
    }
    public sealed override Item GetItem(Player player, Item item, GetItemSettings settings, Configs.InventoryManagement.AutoEquipLevel filterSlots = Configs.InventoryManagement.AutoEquipLevel.Off) {
        if(player.InChest(out _) && filterSlots == Configs.InventoryManagement.AutoEquipLevel.Off) ChestUI.TryPlacingInChest(item, false, ChestsContext(player.chest));
        return item;
    }

    public override void OnSlotChange(Player player, InventorySlots slots, int index) {
        if (slots.Slots[0].context == ContextID.ChestItem) NetMessage.SendData(MessageID.SyncChestItem, number: player.chest, number2: index);
    }

    public static int ChestsContext(int chest) => chest switch {
        >= 0 => ContextID.ChestItem,
        -5 => ContextID.VoidItem,
        _ => ContextID.BankItem
    };
}

public sealed class Armor : ModInventory {

    public sealed override void Load() {
        AddSlots(ContextID.EquipArmor, p => new ListIndices<Item>(p.armor, 0), "Armor", i => i.defense != 0 && i.headSlot != -1);
        AddSlots(ContextID.EquipArmor, p => new ListIndices<Item>(p.armor, 1), "Armor", i => i.defense != 0 && i.bodySlot != -1);
        AddSlots(ContextID.EquipArmor, p => new ListIndices<Item>(p.armor, 2), "Armor", i => i.defense != 0 && i.legSlot != -1);
        AddSlots(ContextID.EquipArmorVanity, p => new ListIndices<Item>(p.armor, Count + AccessorySlotLoader.MaxVanillaSlotCount + 0), "Vanity", i => i.headSlot != -1, i => i.vanity);
        AddSlots(ContextID.EquipArmorVanity, p => new ListIndices<Item>(p.armor, Count + AccessorySlotLoader.MaxVanillaSlotCount + 1), "Vanity", i => i.bodySlot != -1, i => i.vanity);
        AddSlots(ContextID.EquipArmorVanity, p => new ListIndices<Item>(p.armor, Count + AccessorySlotLoader.MaxVanillaSlotCount + 2), "Vanity", i => i.legSlot != -1, i => i.vanity);
    }
    public sealed override void Focus(Player player, InventorySlots slots, int slot) => Main.EquipPageSelected = 0;

    public const int Count = 3;
}

public sealed class Accessories : ModInventory {

    public sealed override void Load() {
        AddSlots("Accessory", i => i.accessory && !i.vanity, i => true,
            (ContextID.EquipAccessory, p => new ListIndices<Item>(p.armor, UnlockedVanillaSlots(p))),
            (ContextID.ModdedAccessorySlot, p => new ListIndices<Item>(ModdedAccessories(p), UnlockedModdedSlots(p)))
        );
        AddSlots("Social", i => i.accessory && i.FitsAccessoryVanitySlot, i => i.vanity && i.FitsAccessoryVanitySlot,
            (ContextID.EquipAccessoryVanity, p => new ListIndices<Item>(p.armor, UnlockedVanillaSlots(p, Armor.Count + AccessorySlotLoader.MaxVanillaSlotCount))),
            (ContextID.ModdedVanityAccessorySlot, p => new ListIndices<Item>(ModdedAccessories(p), UnlockedModdedSlots(p, ModdedAccessories(p).Length/2)))
        );
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

    public sealed override void Focus(Player player, InventorySlots slots, int slot) => Main.EquipPageSelected = 0;

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
        AddSlots(ContextID.EquipPet, p => new ListIndices<Item>(p.miscEquips, 0), "Equipement", i => i.buffType > 0 && Main.vanityPet[i.buffType]);
        AddSlots(ContextID.EquipLight, p => new ListIndices<Item>(p.miscEquips, 1), "Equipement", i => i.buffType > 0 && Main.lightPet[i.buffType]);
        AddSlots(ContextID.EquipMinecart, p => new ListIndices<Item>(p.miscEquips, 2), "Equipement", i => i.mountType != -1 && MountID.Sets.Cart[i.mountType]);
        AddSlots(ContextID.EquipMount, p => new ListIndices<Item>(p.miscEquips, 3), "Equipement", i => i.mountType != -1 && !MountID.Sets.Cart[i.mountType]);
        AddSlots(ContextID.EquipGrapple, p => new ListIndices<Item>(p.miscEquips, 4), "Equipement", i => Main.projHook[i.shoot]);
    }

    public sealed override void Focus(Player player, InventorySlots slots, int slot) => Main.EquipPageSelected = 2;
}

public sealed class Dyes : ModInventory {

    public sealed override int? MaxStack => 1;

    public sealed override void Load() {
        AddSlots(ContextID.EquipDye, p => new ListIndices<Item>(p.dye, DataStructures.Range.FromCount(0, Armor.Count)), "ArmorDye", i => i.dye != 0, i => false);
        AddSlots("AccessoryDye", i => i.dye != 0, i => false,
            (ContextID.EquipDye, p => new ListIndices<Item>(p.dye, Accessories.UnlockedVanillaSlots(p))),
            (ContextID.ModdedDyeSlot, p => new ListIndices<Item>(ModdedDyes(p), Accessories.UnlockedModdedSlots(p)))
        );
        AddSlots(ContextID.EquipMiscDye, p => new(p.miscDyes), "EquipementDye", i => i.dye != 0, i => false);
    }

    public sealed override void Focus(Player player, InventorySlots slots, int slot) => Main.EquipPageSelected = slots != Slots[2] ? 0 : 2;

    public static Item[] ModdedDyes(Player player) => Reflection.ModAccessorySlotPlayer.exDyesAccessory.GetValue(player.GetModPlayer<ModAccessorySlotPlayer>());
}