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

public sealed class Hotbar : ModSubInventory {
    public override int Context => ContextID.InventoryItem;
    public override bool Accepts(Item item) => true;
    public override void Focus(Player player, int slot) => player.selectedItem = slot;
    public override Joined<ListIndices<Item>, Item> Items(Player player) => new ListIndices<Item>(player.inventory, DataStructures.Range.FromCount(0, 10));
}
public sealed class Inventory : ModSubInventory {
    public override int Context => ContextID.InventoryItem;
    public override Joined<ListIndices<Item>, Item> Items(Player player) => new ListIndices<Item>(player.inventory, DataStructures.Range.FromCount(10, 40));
    public override Item GetItem(Player player, Item item, GetItemSettings settings) => BetterPlayer.GetItem_Inner(player, player.whoAmI, item, settings);
}
public sealed class Coin : ModSubInventory {
    public override int Context => ContextID.InventoryCoin;
    public sealed override bool Accepts(Item item) => item.IsACoin;
    public override Joined<ListIndices<Item>, Item> Items(Player player) => new ListIndices<Item>(player.inventory, DataStructures.Range.FromCount(50, 4));
}
public sealed class Ammo : ModSubInventory {
    public override int Context => ContextID.InventoryAmmo;
    public override bool IsDefault(Item item) => true;
    public sealed override bool Accepts(Item item) => !item.IsAir && item.FitsAmmoSlot();
    public override Joined<ListIndices<Item>, Item> Items(Player player) => new ListIndices<Item>(player.inventory, DataStructures.Range.FromCount(54, 4));
}

public abstract class Container : ModSubInventory {
    public sealed override Joined<ListIndices<Item>, Item> Items(Player player) => new ListIndices<Item>(ChestItems(player));
    public sealed override bool FitsSlot(Player player, Item item, int slot, out IList<Slot> itemsToMove) {
        itemsToMove = Array.Empty<Slot>();
        return !ChestUI.IsBlockedFromTransferIntoChest(item, ChestItems(player));
    }
    public sealed override Item GetItem(Player player, Item item, GetItemSettings settings) {
        ChestUI.TryPlacingInChest(item, false, Context);
        return item;
    }
    public abstract Item[] ChestItems(Player player);
}
public sealed class Chest : Container {
    public override int Context => ContextID.ChestItem;
    public override void OnSlotChange(Player player, int slot) => NetMessage.SendData(MessageID.SyncChestItem, number: player.chest, number2: slot);
    public override Item[] ChestItems(Player player) => player.chest >= 0 ? Main.chest[player.chest].item : Array.Empty<Item>();
}
public sealed class PiggyBank : Container {
    public override int Context => ContextID.BankItem;
    public override Item[] ChestItems(Player player) => player.bank.item;
}
public sealed class Safe : Container {
    public override int Context => ContextID.BankItem;
    public override Item[] ChestItems(Player player) => player.bank2.item;
}
public sealed class DefenderForge : Container {
    public override int Context => ContextID.BankItem;
    public override Item[] ChestItems(Player player) => player.bank3.item;
}
public sealed class VoidBag : Container {
    public override int Context => ContextID.VoidItem;
    public override Item[] ChestItems(Player player) => player.bank4.item;
}

public abstract class Armor : ModSubInventory {
    public abstract int Index { get; }
    public abstract bool IsArmor(Item item);

    public override int Context => ContextID.EquipArmor;
    public override bool Accepts(Item item) => IsArmor(item) && item.defense != 0;
    public override bool IsDefault(Item item) => true;
    public override Joined<ListIndices<Item>, Item> Items(Player player) => new ListIndices<Item>(player.armor, Index);

    public sealed override void Focus(Player player, int slot) => Main.EquipPageSelected = 0;
    public const int Count = 3;
}
public abstract class AVanityArmor : Armor {
    public sealed override int Context => ContextID.EquipArmorVanity;
    public override bool Accepts(Item item) => IsArmor(item);
    public override bool IsDefault(Item item) => item.vanity;
    public sealed override Joined<ListIndices<Item>, Item> Items(Player player) => new ListIndices<Item>(player.armor, Index + Count + AccessorySlotLoader.MaxVanillaSlotCount);
}
public sealed class HeadArmor : Armor {
    public sealed override bool IsArmor(Item item) => item.headSlot != -1;
    public sealed  override int Index => 0;
}
public sealed class BodyArmor : Armor {
    public sealed override bool IsArmor(Item item) => item.bodySlot != -1;
    public sealed  override int Index => 1;
}
public sealed class LegArmor : Armor {
    public sealed override bool IsArmor(Item item) => item.legSlot != -1;
    public sealed  override int Index => 2;
}
public sealed class HeadVanityArmor : AVanityArmor {
    public sealed override bool IsArmor(Item item) => item.headSlot != -1;
    public sealed  override int Index => 0;
}
public sealed class BodyVanityArmor : AVanityArmor {
    public sealed override bool IsArmor(Item item) => item.bodySlot != -1;
    public sealed  override int Index => 1;
}
public sealed class LegVanityArmor : AVanityArmor {
    public sealed override bool IsArmor(Item item) => item.legSlot != -1;
    public sealed  override int Index => 2;
}

public abstract class AAccessories<T> : ModSubInventory<T> where T: AAccessories<T> {
    public sealed override bool FitsSlot(Player player, Item item, int slot, out IList<Slot> itemsToMove) {
        List<int> vanillaSlots = UnlockedVanillaSlots(player);
        List<int> moddedSlots = UnlockedModdedSlots(player);

        if (!(slot < vanillaSlots.Count ?
                ItemLoader.CanEquipAccessory(item, slot + 3, false) :
                ItemLoader.CanEquipAccessory(item, moddedSlots[slot - vanillaSlots.Count], true) && LoaderManager.Get<AccessorySlotLoader>().CanAcceptItem(moddedSlots[slot - vanillaSlots.Count], item, -Context))) {
            itemsToMove = Array.Empty<Slot>();
            return false;
        }
        itemsToMove = GetIncompatibleItems(player, item, Context == 11, out bool canAllMove);
        return canAllMove;
    }

    public sealed override void Focus(Player player, int slot) => Main.EquipPageSelected = 0;

    public static IList<Slot> GetIncompatibleItems(Player player, Item item, bool vanity, out bool canAllMove) {
        canAllMove = true;
        List<Slot> incompatibles = new();
        void CheskAccessories(ModSubInventory inv, bool vanity, ref bool canAllMove){
            IList<Item> items = inv.Items(player);
            for (int i = 0; i < items.Count; i++) {
                if (item == items[i]) continue;
                if (item.type != items[i].type && (vanity || (item.wingSlot <= 0 || items[i].wingSlot <= 0) && ItemLoader.CanAccessoryBeEquippedWith(items[i], item))) continue;
                incompatibles.Add(new(inv, i));
                if (ItemSlot.isEquipLocked(i)) canAllMove = false;
            }
        }
        CheskAccessories(Accessories.Instance, vanity, ref canAllMove);
        CheskAccessories(VanityAccessories.Instance, true, ref canAllMove);
        return incompatibles;
    }

    public static List<int> UnlockedVanillaSlots(Player player, int offset = 0) {
        List<int> unlocked = new();
        for (int i = 0; i < AccessorySlotLoader.MaxVanillaSlotCount; i++) if (player.IsItemSlotUnlockedAndUsable(i + Armor.Count)) unlocked.Add(i + Armor.Count + offset);
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
public sealed class Accessories : AAccessories<Accessories> {
    public override bool Accepts(Item item) => item.accessory && !item.vanity;
    public override bool IsDefault(Item item) => true;
    public override int Context => ContextID.EquipAccessory;
    public override Joined<ListIndices<Item>, Item> Items(Player player) => new(
        new ListIndices<Item>(player.armor, UnlockedVanillaSlots(player)),
        new ListIndices<Item>(ModdedAccessories(player), UnlockedModdedSlots(player))
    );
}
public sealed class VanityAccessories : AAccessories<VanityAccessories> {
    public override bool Accepts(Item item) => item.accessory;
    public override bool IsDefault(Item item) => item.vanity && item.FitsAccessoryVanitySlot;
    public override int Context => ContextID.EquipAccessoryVanity;
    public override Joined<ListIndices<Item>, Item> Items(Player player) => new(
        new ListIndices<Item>(player.armor, UnlockedVanillaSlots(player, Armor.Count + AccessorySlotLoader.MaxVanillaSlotCount)),
        new ListIndices<Item>(ModdedAccessories(player), UnlockedModdedSlots(player, ModdedAccessories(player).Length / 2))
    );
}

public abstract class ALoadout : ModSubInventory {
    public abstract int Index { get; }
    public sealed override int? MaxStack => 1;
    public sealed override Joined<ListIndices<Item>, Item> Items(Player player) => new ListIndices<Item>(player.Loadouts[player.CurrentLoadoutIndex <= Index ? (Index + 1) : Index].Armor);
}
public abstract class Loadout1 : ALoadout {
    public sealed override int Index => 1;
}
public abstract class Loadout2 : ALoadout {
    public sealed override int Index => 2;
}

public abstract class Equipement : ModSubInventory {
    public abstract int Index { get; }
    public sealed override Joined<ListIndices<Item>, Item> Items(Player player) => new ListIndices<Item>(player.miscEquips, Index);
    public sealed override bool IsDefault(Item item) => true;
    public sealed override void Focus(Player player, int slot) => Main.EquipPageSelected = 2;
}
public sealed class Pet : Equipement {
    public sealed override int Index => 0;
    public sealed override int Context => ContextID.EquipPet;
    public sealed override bool Accepts(Item item) => item.buffType > 0 && Main.vanityPet[item.buffType];
}
public sealed class LightPet : Equipement {
    public sealed override int Index => 1;
    public sealed override int Context => ContextID.EquipLight;
    public sealed override bool Accepts(Item item) => item.buffType > 0 && Main.lightPet[item.buffType];
}
public sealed class Minecart : Equipement {
    public sealed override int Index => 2;
    public sealed override int Context => ContextID.EquipMinecart;
    public sealed override bool Accepts(Item item) => item.mountType != -1 && MountID.Sets.Cart[item.mountType];
}
public sealed class Mount : Equipement {
    public sealed override int Index => 3;
    public sealed override int Context => ContextID.EquipMount;
    public sealed override bool Accepts(Item item) => item.mountType != -1 && !MountID.Sets.Cart[item.mountType];
}
public sealed class Hook : Equipement {
    public sealed override int Index => 4;
    public sealed override int Context => ContextID.EquipGrapple;
    public sealed override bool Accepts(Item item) => Main.projHook[item.shoot];
}
public abstract class Dyes : ModSubInventory {
    public override int Context => ContextID.EquipDye;
    public override void Focus(Player player, int slot) => Main.EquipPageSelected = 0;

    public sealed override int? MaxStack => 1;
    public sealed override bool Accepts(Item item) => item.dye != 0;

    public static Item[] ModdedDyes(Player player) => Reflection.ModAccessorySlotPlayer.exDyesAccessory.GetValue(player.GetModPlayer<ModAccessorySlotPlayer>());
}
public sealed class ArmorDyes : Dyes {
    public sealed override Joined<ListIndices<Item>, Item> Items(Player player) => new ListIndices<Item>(player.dye, DataStructures.Range.FromCount(0, Armor.Count));
}
public sealed class AccessoryDyes : Dyes {
    public sealed override Joined<ListIndices<Item>, Item> Items(Player player) => new(
        new ListIndices<Item>(player.dye, Accessories.UnlockedVanillaSlots(player)),
        new ListIndices<Item>(ModdedDyes(player), Accessories.UnlockedModdedSlots(player))
    );
}
public sealed class EquipementDyes : Dyes {
    public sealed override int Context => ContextID.EquipMiscDye;
    public sealed override void Focus(Player player, int slot) => Main.EquipPageSelected = 2;
    public sealed override Joined<ListIndices<Item>, Item> Items(Player player) => new ListIndices<Item>(player.miscDyes);
}