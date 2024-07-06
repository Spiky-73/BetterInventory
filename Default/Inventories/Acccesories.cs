using System;
using System.Collections.Generic;
using SpikysLib.Constants;
using SpikysLib.DataStructures;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Default;
using Terraria.UI;
using ContextID = Terraria.UI.ItemSlot.Context;

namespace BetterInventory.Default.Inventories;

public abstract class AAccessories : ModSubInventory {
    public sealed override bool FitsSlot(Player player, Item item, int slot, out IList<Slot> itemsToMove) {
        List<int> vanillaSlots = UnlockedVanillaSlots(player);
        List<int> moddedSlots = UnlockedModdedSlots(player);

        if (!(slot < vanillaSlots.Count ?
                ItemLoader.CanEquipAccessory(item, slot + ArmorSlots.Count, false) :
                ItemLoader.CanEquipAccessory(item, moddedSlots[slot - vanillaSlots.Count], true) && LoaderManager.Get<AccessorySlotLoader>().CanAcceptItem(moddedSlots[slot - vanillaSlots.Count], item, -Context))) {
            itemsToMove = Array.Empty<Slot>();
            return false;
        }
        itemsToMove = GetIncompatibleItems(player, item, Context == ContextID.EquipAccessoryVanity, out bool canAllMove);
        return canAllMove;
    }

    public sealed override void Focus(Player player, int slot) => Main.EquipPageSelected = 0;

    public static IList<Slot> GetIncompatibleItems(Player player, Item item, bool vanity, out bool canAllMove) {
        canAllMove = true;
        List<Slot> incompatibles = new();
        void CheckAccessories(ModSubInventory inv, bool vanity, ref bool canAllMove) {
            IList<Item> items = inv.Items(player);
            for (int i = 0; i < items.Count; i++) {
                if (item == items[i]) continue;
                if (item.type != items[i].type && (vanity || (item.wingSlot <= 0 || items[i].wingSlot <= 0) && ItemLoader.CanAccessoryBeEquippedWith(items[i], item))) continue;
                incompatibles.Add(new(inv, i));
                if (ItemSlot.isEquipLocked(i)) canAllMove = false;
            }
        }
        CheckAccessories(Accessories.Instance, vanity, ref canAllMove);
        CheckAccessories(VanityAccessories.Instance, true, ref canAllMove);
        return incompatibles;
    }

    public static List<int> UnlockedVanillaSlots(Player player, int offset = 0) {
        List<int> unlocked = new();
        for (int i = 0; i < AccessorySlotLoader.MaxVanillaSlotCount; i++) if (player.IsItemSlotUnlockedAndUsable(i + ArmorSlots.Count)) unlocked.Add(i + ArmorSlots.Count + offset);
        return unlocked;
    }
    public static List<int> UnlockedModdedSlots(Player player, int offset = 0) {
        List<int> unlocked = new();
        int length = ModdedAccessories(player).Length / 2;
        for (int i = 0; i < length; i++) if (LoaderManager.Get<AccessorySlotLoader>().ModdedIsItemSlotUnlockedAndUsable(i, player)) unlocked.Add(i + offset);
        return unlocked;
    }
    public static Item[] ModdedAccessories(Player player) => Reflection.ModAccessorySlotPlayer.exAccessorySlot.GetValue(player.GetModPlayer<ModAccessorySlotPlayer>());
}
public sealed class Accessories : AAccessories {
    public static Accessories Instance = null!;
    public override bool Accepts(Item item) => item.accessory && !item.vanity;
    public override bool IsPrimaryFor(Item item) => true;
    public override int Context => ContextID.EquipAccessory;
    public override JoinedLists<Item> Items(Player player) => new(
        new ListIndices<Item>(player.armor, UnlockedVanillaSlots(player)),
        new ListIndices<Item>(ModdedAccessories(player), UnlockedModdedSlots(player))
    );
}
public sealed class VanityAccessories : AAccessories {
    public static VanityAccessories Instance = null!;

    public override bool Accepts(Item item) => item.accessory;
    public override bool IsPrimaryFor(Item item) => item.vanity && item.FitsAccessoryVanitySlot;
    public override int Context => ContextID.EquipAccessoryVanity;
    public override JoinedLists<Item> Items(Player player) => new(
        new ListIndices<Item>(player.armor, UnlockedVanillaSlots(player, ArmorSlots.Count + AccessorySlotLoader.MaxVanillaSlotCount)),
        new ListIndices<Item>(ModdedAccessories(player), UnlockedModdedSlots(player, ModdedAccessories(player).Length / 2))
    );
}
