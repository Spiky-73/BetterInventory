using System.Collections;
using System.Collections.Generic;
using SpikysLib.Constants;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Default;

namespace BetterInventory.Default.Inventories;

public static class LoadoutInventoryHelper {
    public static Item[] Armor(this Player player, int loadout = -1) => loadout == -1 || player.CurrentLoadoutIndex == loadout ? player.armor : player.Loadouts[loadout].Armor;
    
    public static Item[] Dyes(this Player player, int loadout = -1) => loadout == -1 || player.CurrentLoadoutIndex == loadout ? player.dye : player.Loadouts[loadout].Dye;
    public static Item[] ExDyes(this Player player, int loadout = -1) => loadout == -1 || player.CurrentLoadoutIndex == loadout ?
        Reflection.ModAccessorySlotPlayer.exDyesAccessory.GetValue(player.GetModPlayer<ModAccessorySlotPlayer>()) :
        Reflection.ModAccessorySlotPlayer.ExEquipmentLoadout.ExDyesAccessory.GetValue(((IList)Reflection.ModAccessorySlotPlayer.exLoadouts.GetValue(player.GetModPlayer<ModAccessorySlotPlayer>())!)[loadout]!)!;


    public static Item[] ExAccessories(this Player player, int loadout = -1) => loadout == -1 || player.CurrentLoadoutIndex == loadout ?
        Reflection.ModAccessorySlotPlayer.exAccessorySlot.GetValue(player.GetModPlayer<ModAccessorySlotPlayer>()) :
        Reflection.ModAccessorySlotPlayer.ExEquipmentLoadout.ExAccessorySlot.GetValue(((IList)Reflection.ModAccessorySlotPlayer.exLoadouts.GetValue(player.GetModPlayer<ModAccessorySlotPlayer>())!)[loadout]!)!;


    public static List<int> UnlockedVanillaSlots(this Player player, bool vanity = false) {
        List<int> unlocked = [];
        int offset = vanity ? (ArmorSlots.Count + AccessorySlotLoader.MaxVanillaSlotCount) : 0;
        for (int i = 0; i < AccessorySlotLoader.MaxVanillaSlotCount; i++) if (player.IsItemSlotUnlockedAndUsable(i + ArmorSlots.Count)) unlocked.Add(i + ArmorSlots.Count + offset);
        return unlocked;
    }

    public static List<int> UnlockedModdedSlots(this Player player, bool shared, bool vanity = false) {
        List<int> unlocked = [];
        var accPlayer = player.GetModPlayer<ModAccessorySlotPlayer>();
        int offset = vanity ? accPlayer.SlotCount : 0;
        for (int i = 0; i < accPlayer.SlotCount; i++) {
            if (!LoaderManager.Get<AccessorySlotLoader>().ModdedIsItemSlotUnlockedAndUsable(i, player)) continue;
            if (shared != Reflection.ModAccessorySlotPlayer.IsSharedSlot.Invoke(accPlayer, i)) continue;
            unlocked.Add(i + offset);
        }
        return unlocked;
    }
}