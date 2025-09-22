using System.Collections.Generic;
using SpikysLib.Constants;
using SpikysLib.DataStructures;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using ContextID = Terraria.UI.ItemSlot.Context;

namespace BetterInventory.Default.Inventories;

public class Accessories : ModSubLoadoutInventory {
    public static bool FitsSlot(Player player, Item item, int context, int slot, int loadout, out IList<InventorySlot> itemsToMove) {
        List<int> vanillaSlots = player.UnlockedVanillaSlots();
        List<int> moddedSlots = player.UnlockedModdedSlots(false);

        if (!(slot < vanillaSlots.Count ?
                ItemLoader.CanEquipAccessory(item, slot + ArmorSlots.Count, false) :
                ItemLoader.CanEquipAccessory(item, moddedSlots[slot - vanillaSlots.Count], true) && LoaderManager.Get<AccessorySlotLoader>().CanAcceptItem(moddedSlots[slot - vanillaSlots.Count], item, -context))) {
            itemsToMove = [];
            return false;
        }
        itemsToMove = GetIncompatibleItems(player, item, context == ContextID.EquipAccessoryVanity, loadout, out bool canAllMove);
        return canAllMove;
    }
    public static IList<InventorySlot> GetIncompatibleItems(Player player, Item item, bool vanity, int loadout, out bool canAllMove) {
        canAllMove = true;
        List<InventorySlot> incompatibles = [];
        void CheckAccessories(ModSubEquipmentInventory inventory, bool vanity, ref bool canAllMove) {
            IList<Item> items = inventory.Items;
            for (int i = 0; i < items.Count; i++) {
                if (item == items[i]) continue;
                if (item.type != items[i].type && (vanity || (item.wingSlot <= 0 || items[i].wingSlot <= 0) && ItemLoader.CanAccessoryBeEquippedWith(items[i], item))) continue;
                incompatibles.Add(new(inventory, i));
                if (ItemSlot.isEquipLocked(i)) canAllMove = false;
            }
        }

        var accessories = (Accessories)ModContent.GetInstance<Accessories>().NewInstance(player);
        accessories.LoadoutIndex = loadout;
        CheckAccessories(accessories, vanity, ref canAllMove);
        var social = (VanityAccessories)ModContent.GetInstance<VanityAccessories>().NewInstance(player);
        social.LoadoutIndex = loadout;
        CheckAccessories(social, true, ref canAllMove);
        var sharedAccessories = (SharedAccessories)ModContent.GetInstance<SharedAccessories>().NewInstance(player);
        CheckAccessories(sharedAccessories, vanity, ref canAllMove);
        var sharedSocial = (SharedVanityAccessories)ModContent.GetInstance<SharedVanityAccessories>().NewInstance(player);
        CheckAccessories(sharedSocial, true, ref canAllMove);
        return incompatibles;
    }

    public virtual bool Vanity => false;

    public sealed override int Context => Vanity ? ContextID.EquipAccessoryVanity : ContextID.EquipAccessory;
    public sealed override int EquipPage => 0;
    public sealed override bool Accepts(Item item) => item.accessory && (Vanity || !item.vanity);
    public sealed override bool IsPreferredInventory(Item item) => !Vanity || (item.vanity && item.FitsAccessoryVanitySlot);
    public sealed override JoinedLists<Item> Items => new(
        new ListIndices<Item>(Entity.Armor(LoadoutIndex), Entity.UnlockedVanillaSlots(Vanity)),
        new ListIndices<Item>(Entity.ExAccessories(LoadoutIndex), Entity.UnlockedModdedSlots(false, Vanity))
    );
    public sealed override bool FitsSlot(Item item, int slot, out IList<InventorySlot> itemsToMove) => FitsSlot(Entity, item, Context, slot, LoadoutIndex, out itemsToMove);
}
public sealed class VanityAccessories : Accessories {
    public sealed override bool Vanity => true;
    public sealed override int ComparePositionTo(ModSubInventory other) => other.GetType() == typeof(SharedAccessories) ? 1 : 0;
}

public class SharedAccessories : ModSubEquipmentInventory {
    public virtual bool Vanity => false;

    public sealed override int Context => Vanity ? ContextID.EquipAccessoryVanity : ContextID.EquipAccessory;
    public sealed override int EquipPage => 0;
    public sealed override bool Accepts(Item item) => item.accessory && (Vanity || !item.vanity);
    public sealed override bool IsPreferredInventory(Item item) => !Vanity || (item.vanity && item.FitsAccessoryVanitySlot);
    public sealed override ListIndices<Item> Items => new(Entity.ExAccessories(), Entity.UnlockedModdedSlots(true, Vanity));
    public sealed override bool FitsSlot(Item item, int slot, out IList<InventorySlot> itemsToMove) => Accessories.FitsSlot(Entity, item, Context, slot, -1, out itemsToMove);
    public override int ComparePositionTo(ModSubInventory other) => other.GetType() == typeof(Accessories) ? 1 : 0;
}

public sealed class SharedVanityAccessories : SharedAccessories {
    public sealed override bool Vanity => true;
    public sealed override int ComparePositionTo(ModSubInventory other) => other.GetType() == typeof(VanityAccessories) ? 1 : 0;
}
