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

    public sealed override HashSet<int> Contexts => new() { ContextID.InventoryItem, ContextID.InventoryAmmo, ContextID.InventoryCoin };

    public sealed override void SetStaticDefaults() {
        SubInventory("Hotbar", i => true, DataStructures.Range.FromCount(0, 10), -10);
        SubInventory("Coin", i => i.IsACoin, DataStructures.Range.FromCount(50, 4));
        SubInventory("Ammo", i => i.FitsAmmoSlot(), DataStructures.Range.FromCount(54, 4));
    }
    public sealed override IList<Item> Items(Player player) => player.inventory;

    public sealed override bool CanSlotAccepts(Player player, Item item, int slot, out IList<int> itemsToMove) {
        itemsToMove = Array.Empty<int>();
        return !player.preventAllItemPickups;
    }
    public sealed override Item GetItem(Player player, Item item, GetItemSettings settings) => player.GetItem(player.whoAmI, item, settings);

}

public sealed class Chest : ModInventory {

    public sealed override HashSet<int> Contexts => new() { ContextID.BankItem }; // TODO sync chests

    public sealed override IList<Item> Items(Player player) => player.InChest(out Item[]? chest) ? chest : Array.Empty<Item>();

    public sealed override bool CanSlotAccepts(Player player, Item item, int slot, out IList<int> itemsToMove) {
        itemsToMove = Array.Empty<int>();
        return !ChestUI.IsBlockedFromTransferIntoChest(item, player.Chest()!);
    }
    public sealed override Item GetItem(Player player, Item item, GetItemSettings settings) {
        ChestUI.TryPlacingInChest(item, false, ContextID.InventoryItem);
        return item;
    }
}

public sealed class Armor : ModInventory {

    public sealed override HashSet<int> Contexts => new() { ContextID.EquipArmor, ContextID.EquipArmorVanity };

    public sealed override void SetStaticDefaults() {
        SubInventory("Armor", i => i.defense != 0 && i.headSlot != -1, new int[]{0});
        SubInventory("Armor", i => i.defense != 0 && i.bodySlot != -1, new int[]{1});
        SubInventory("Armor", i => i.defense != 0 && i.legSlot != -1, new int[]{2});
        SubInventory("Vanity", i => i.headSlot != -1, new int[]{3});
        SubInventory("Vanity", i => i.bodySlot != -1, new int[]{4});
        SubInventory("Vanity", i => i.legSlot != -1, new int[]{5});
    }
    public sealed override IList<Item> Items(Player player) => new JoinedList<Item>(new ArraySegment<Item>(player.armor, 0, ArmorCount), new ArraySegment<Item>(player.armor, ArmorCount+Accessories.VanillaAccCount, ArmorCount));

    public sealed override int ToIndex(Player player, int context, int slot) => context == ContextID.EquipArmorVanity ? slot - Accessories.VanillaAccCount : slot;

    public const int ArmorCount = 3;
}

public sealed class Accessories : ModInventory<Accessories> {

    public sealed override HashSet<int> Contexts => new() { ContextID.EquipAccessory, ContextID.EquipAccessoryVanity, ContextID.ModdedAccessorySlot, ContextID.ModdedVanityAccessorySlot };

    public sealed override void SetStaticDefaults() {
        SubInventory("Accessory", i => i.accessory, player => DataStructures.Range.FromCount(0, Items(player).Count/2));
        SubInventory("Social", i => i.accessory && i.FitsAccessoryVanitySlot, player => {
            IList<Item> items = Items(player);
            return DataStructures.Range.FromCount(items.Count / 2, items.Count / 2);
        });
    }

    public sealed override bool SlotEnabled(Player player, int slot) => (slot % (VanillaAccCount + ModdedAccCount(player)) < VanillaAccCount) ? player.IsItemSlotUnlockedAndUsable(slot + Armor.ArmorCount) : LoaderManager.Get<AccessorySlotLoader>().ModdedIsItemSlotUnlockedAndUsable(slot - VanillaAccCount, player);
    public sealed override bool CanSlotAccepts(Player player, Item item, int slot, out IList<int> itemsToMove) {
        if (!ItemLoader.CanEquipAccessory(item, slot, slot >= VanillaAccCount * 2) || slot >= VanillaAccCount * 2
                && !LoaderManager.Get<AccessorySlotLoader>().CanAcceptItem(slot - VanillaAccCount * 2, item, (slot - VanillaAccCount * 2 < ModdedAccCount(player)) ? -10 : -11)) {
            itemsToMove = Array.Empty<int>();
            return false;
        }
        itemsToMove = GetIncompatibleItems(player, item, slot, out bool canAllMove);
        return canAllMove;
    }

    public IList<int> GetIncompatibleItems(Player player, Item item, int slot, out bool canAllMove) {
        JoinedList<Item> items = (JoinedList<Item>)Items(player);

        canAllMove = true;

        bool vanity = slot >= items.Count/2;
        List<int> incompatibles = new();
        for (int i = 0; i < items.Count; i++) {
            if (item == items[i]) continue;

            if (item.type == items[i].type || (!vanity && i < items.Count/2 && (item.wingSlot > 0 && items[i].wingSlot > 0 || !ItemLoader.CanAccessoryBeEquippedWith(items[i], item)))) {
                incompatibles.Add(i);
                if (ItemSlot.isEquipLocked(i)) canAllMove = false;
            }
        }
        return incompatibles;
    }

    public sealed override IList<Item> Items(Player player) {
        Item[] accessories = Reflection.ModAccessorySlotPlayer.exAccessorySlot.GetValue(Main.LocalPlayer.GetModPlayer<ModAccessorySlotPlayer>());
        return new JoinedList<Item>(
            new ArraySegment<Item>(player.armor, Armor.ArmorCount, VanillaAccCount), new ArraySegment<Item>(accessories, 0, accessories.Length/2),
            new ArraySegment<Item>(player.armor, Armor.ArmorCount*2+VanillaAccCount, VanillaAccCount), new ArraySegment<Item>(accessories, accessories.Length / 2, accessories.Length / 2)
        );
    }

    public sealed override int ToIndex(Player player, int context, int slot) => context switch {
        ContextID.EquipAccessory => slot-Armor.ArmorCount,
        ContextID.ModdedAccessorySlot => slot + VanillaAccCount,
        ContextID.EquipAccessoryVanity => slot - (2*Armor.ArmorCount+VanillaAccCount) + VanillaAccCount + ModdedAccCount(player),
        ContextID.ModdedVanityAccessorySlot => slot + 2*VanillaAccCount + ModdedAccCount(player),
        _ => slot
    };

    public static int ModdedAccCount(Player player) => player.GetModPlayer<ModAccessorySlotPlayer>().SlotCount;

    public const int VanillaAccCount = 7;
}

public sealed class Equipement : ModInventory {

    public sealed override HashSet<int> Contexts => new() { ContextID.EquipPet, ContextID.EquipLight, ContextID.EquipMount, ContextID.EquipMinecart, ContextID.EquipGrapple };

    public sealed override void SetStaticDefaults() {
        SubInventory("Equipement", i => i.buffType > 0 && Main.vanityPet[i.buffType], new int[]{0});
        SubInventory("Equipement", i => i.buffType > 0 && Main.lightPet[i.buffType], new int[]{1});
        SubInventory("Equipement", i => i.mountType != -1 && MountID.Sets.Cart[i.mountType], new int[]{2});
        SubInventory("Equipement", i => i.mountType != -1 && !MountID.Sets.Cart[i.mountType], new int[]{3});
        SubInventory("Equipement", i => Main.projHook[i.shoot], new int[]{4});
    }
    public sealed override IList<Item> Items(Player player) => player.miscEquips;
}

public sealed class Dyes : ModInventory {

    public sealed override int? MaxStack => 1;

    public sealed override HashSet<int> Contexts => new() { ContextID.EquipDye, ContextID.ModdedDyeSlot, ContextID.EquipMiscDye };

    public sealed override bool SlotEnabled(Player player, int slot) => Armor.ArmorCount >= slot || slot >= Accessories.VanillaAccCount + Accessories.ModdedAccCount(player) || Accessories.Instance.SlotEnabled(player, slot - Armor.ArmorCount);

    public sealed override void SetStaticDefaults() {
        SubInventory("ArmorDye", i => i.dye != 0, DataStructures.Range.FromCount(0, Armor.ArmorCount));
        SubInventory("AccessoryDye", i => i.dye != 0, player => DataStructures.Range.FromCount(Armor.ArmorCount, Accessories.VanillaAccCount + Accessories.ModdedAccCount(player)));
        SubInventory("EquipementDye", i => i.dye != 0, player => DataStructures.Range.FromCount(Armor.ArmorCount + Accessories.VanillaAccCount + Accessories.ModdedAccCount(player), EquipementCount));
    }
    public sealed override IList<Item> Items(Player player) => new JoinedList<Item>(player.dye, Reflection.ModAccessorySlotPlayer.exDyesAccessory.GetValue(Main.LocalPlayer.GetModPlayer<ModAccessorySlotPlayer>()), player.miscDyes);

    public sealed override int ToIndex(Player player, int context, int slot) => context switch {
        ContextID.ModdedDyeSlot => slot + 10,
        ContextID.EquipMiscDye => slot + 10 + Accessories.ModdedAccCount(player),
        _ => slot
    };

    public const int EquipementCount = 5;
}