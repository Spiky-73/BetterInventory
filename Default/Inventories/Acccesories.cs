using System.Collections;
using System.Collections.Generic;
using SpikysLib.Constants;
using SpikysLib.DataStructures;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Default;
using Terraria.ModLoader.IO;
using Terraria.UI;
using ContextID = Terraria.UI.ItemSlot.Context;

namespace BetterInventory.Default.Inventories;

public abstract class AccessoryInventory : ModSubLoadoutInventory {
    public sealed override bool FitsSlot(Item item, int slot, out IList<InventorySlot> itemsToMove) {
        List<int> vanillaSlots = UnlockedVanillaSlots(Entity);
        List<int> moddedSlots = UnlockedModdedSlots(Entity, false);

        if (!(slot < vanillaSlots.Count ?
                ItemLoader.CanEquipAccessory(item, slot + ArmorSlots.Count, false) :
                ItemLoader.CanEquipAccessory(item, moddedSlots[slot - vanillaSlots.Count], true) && LoaderManager.Get<AccessorySlotLoader>().CanAcceptItem(moddedSlots[slot - vanillaSlots.Count], item, -Context))) {
            itemsToMove = [];
            return false;
        }
        itemsToMove = GetIncompatibleItems(item, Context == ContextID.EquipAccessoryVanity, out bool canAllMove);
        return canAllMove;
    }

    public IList<InventorySlot> GetIncompatibleItems(Item item, bool vanity, out bool canAllMove) {
        canAllMove = true;
        List<InventorySlot> incompatibles = [];
        void CheckAccessories(ModSubLoadoutInventory inventory, bool vanity, ref bool canAllMove) {
            IList<Item> items = inventory.Items;
            for (int i = 0; i < items.Count; i++) {
                if (item == items[i]) continue;
                if (item.type != items[i].type && (vanity || (item.wingSlot <= 0 || items[i].wingSlot <= 0) && ItemLoader.CanAccessoryBeEquippedWith(items[i], item))) continue;
                incompatibles.Add(new(inventory, i));
                if (ItemSlot.isEquipLocked(i)) canAllMove = false;
            }
        }

        var accessories = (Accessories)ModContent.GetInstance<Accessories>().NewInstance(Entity);
        accessories.LoadoutIndex = LoadoutIndex;
        CheckAccessories(accessories, vanity, ref canAllMove);
        var social = (VanityAccessories)ModContent.GetInstance<VanityAccessories>().NewInstance(Entity);
        social.LoadoutIndex = LoadoutIndex;
        CheckAccessories(social, true, ref canAllMove);
        var sharedAccessories = (SharedAccessories)ModContent.GetInstance<SharedAccessories>().NewInstance(Entity);
        CheckAccessories(sharedAccessories, vanity, ref canAllMove);
        var sharedSocial = (SharedVanityAccessories)ModContent.GetInstance<SharedVanityAccessories>().NewInstance(Entity);
        CheckAccessories(sharedSocial, true, ref canAllMove);
        return incompatibles;
    }

    public Item[] VanillaAccessories => Entity.CurrentLoadoutIndex == LoadoutIndex ?
        Entity.armor :
        Entity.Loadouts[LoadoutIndex].Armor;
    public Item[] ModdedAccessories => Entity.CurrentLoadoutIndex == LoadoutIndex || LoadoutIndex == -1 ?
        Reflection.ModAccessorySlotPlayer.exAccessorySlot.GetValue(Entity.GetModPlayer<ModAccessorySlotPlayer>()) :
        Reflection.ModAccessorySlotPlayer.ExEquipmentLoadout.ExAccessorySlot.GetValue(((IList)Reflection.ModAccessorySlotPlayer.exLoadouts.GetValue(Entity.GetModPlayer<ModAccessorySlotPlayer>())!)[LoadoutIndex]!)!;
    
    public static List<int> UnlockedVanillaSlots(Player player, bool vanity = false) {
        List<int> unlocked = [];
        int offset = vanity ? (ArmorSlots.Count + AccessorySlotLoader.MaxVanillaSlotCount) : 0;
        for (int i = 0; i < AccessorySlotLoader.MaxVanillaSlotCount; i++) if (player.IsItemSlotUnlockedAndUsable(i + ArmorSlots.Count)) unlocked.Add(i + ArmorSlots.Count + offset);
        return unlocked;
    }

    public static List<int> UnlockedModdedSlots(Player player, bool shared, bool vanity = false) {
        List<int> unlocked = [];
        var accPlayer = player.GetModPlayer<ModAccessorySlotPlayer>();
        int offset = vanity ? accPlayer.SlotCount : 0;
        for (int i = 0; i < accPlayer.SlotCount; i++){
            if (!LoaderManager.Get<AccessorySlotLoader>().ModdedIsItemSlotUnlockedAndUsable(i, player)) continue;
            if (shared != Reflection.ModAccessorySlotPlayer.IsSharedSlot.Invoke(accPlayer, i)) continue;
            unlocked.Add(i + offset);
        }
        return unlocked;
    }
}

public sealed class Accessories : AccessoryInventory {
    public override bool Accepts(Item item) => item.accessory && !item.vanity;
    public override bool IsPreferredInventory(Item item) => true;
    public override int Context => ContextID.EquipAccessory;
    public override JoinedLists<Item> Items => new(
        new ListIndices<Item>(VanillaAccessories, UnlockedVanillaSlots(Entity)),
        new ListIndices<Item>(ModdedAccessories, UnlockedModdedSlots(Entity, false))
    );
}

public sealed class SharedAccessories : AccessoryInventory {
    public override bool Accepts(Item item) => item.accessory && !item.vanity;
    public override bool IsPreferredInventory(Item item) => true;
    public override int Context => ContextID.EquipAccessory;
    public override ListIndices<Item> Items => new(ModdedAccessories, UnlockedModdedSlots(Entity, true));
    public override IList<ModSubInventory> GetInventories(Player player) => [NewInstance(player)];
    public sealed override int ComparePositionTo(ModSubInventory other) => other is Accessories ? 1 : 0;
}

public sealed class VanityAccessories : AccessoryInventory {
    public override bool Accepts(Item item) => item.accessory;
    public override bool IsPreferredInventory(Item item) => item.vanity && item.FitsAccessoryVanitySlot;
    public override int Context => ContextID.EquipAccessoryVanity;
    public override JoinedLists<Item> Items => new(
        new ListIndices<Item>(VanillaAccessories, UnlockedVanillaSlots(Entity, true)),
        new ListIndices<Item>(ModdedAccessories, UnlockedModdedSlots(Entity, false, true))
    );
    public sealed override int ComparePositionTo(ModSubInventory other) => other is SharedAccessories ? 1 : 0;
}

public sealed class SharedVanityAccessories : AccessoryInventory {
    public override bool Accepts(Item item) => item.accessory;
    public override bool IsPreferredInventory(Item item) => item.vanity && item.FitsAccessoryVanitySlot;
    public override int Context => ContextID.EquipAccessoryVanity;
    public override ListIndices<Item> Items => new(ModdedAccessories, UnlockedModdedSlots(Entity, true, true));
    public override IList<ModSubInventory> GetInventories(Player player) => [NewInstance(player)];
    public sealed override int ComparePositionTo(ModSubInventory other) => other is VanityAccessories ? 1 : 0;
}
