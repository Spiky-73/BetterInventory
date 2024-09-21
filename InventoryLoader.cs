using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using SpikysLib.Collections;
using SpikysLib.Configs;
using SpikysLib.DataStructures;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;

namespace BetterInventory;

public sealed class InventoryLoader : ILoadable {

    public static IEnumerable<ModSubInventory> SubInventories {
        get {
            foreach (var inventory in _special) yield return inventory;
            foreach (var inventory in _classic) yield return inventory;
        }
    }
    public static ReadOnlyCollection<ModSubInventory> Special => _special.AsReadOnly();
    public static ReadOnlyCollection<ModSubInventory> Classic => _classic.AsReadOnly();

    public void Load(Mod mod) { }

    public void Unload() {
        ClearCache();
        static void Clear(IList<ModSubInventory> list) {
            foreach (var inventory in list) ConfigHelper.SetInstance(inventory, true);
            list.Clear();
        }
        Clear(_classic);
        Clear(_special);
    }

    internal static void Register(ModSubInventory inventory) {
        ConfigHelper.SetInstance(inventory);
        inventory.CanBePrimary = LoaderUtils.HasOverride(inventory, i => i.IsPrimaryFor);
        List<ModSubInventory> list = inventory.CanBePrimary ? _special : _classic;

        int before = list.FindIndex(i => inventory.ComparePositionTo(i) < 0 || i.ComparePositionTo(inventory) > 0);
        if (before != -1) list.Insert(before, inventory);
        else list.Add(inventory);
    }

    public static Slot? FindItem(Player player, Predicate<Item> predicate) {
        foreach (ModSubInventory slots in SubInventories) {
            int slot = slots.Items(player).FindIndex(predicate);
            if (slot != -1) return new(slots, slot);
        }
        return null;
    }

    public static bool IsInventorySlot(Player player, Item[] inv, int context, int slot, out Slot itemSlot) {
        Slot? s = GetInventorySlot(player, inv, context, slot);
        itemSlot = s ?? default;
        return s.HasValue;
    }
    public static Slot? GetInventorySlot(Player player, Item[] inventory, int context, int slot) => s_slotToInv.GetOrAdd(new(player.whoAmI, inventory, context, slot), _ => {
        foreach (ModSubInventory slots in SubInventories) {
            int i = GetSlotIndex(slots.Items(player), inventory, context, slot);
            if (i != -1) return new(slots, i);
        }
        return null;
    });
    private static int GetSlotIndex(IList<Item> list, Item[] inv, int ctxt, int slot) {
        if (list == inv) return slot;
        else if(list is JoinedLists<Item> joined) {
            int offset = 0;
            foreach (IList<Item> l in joined.Lists) {
                int i = GetSlotIndex(l, inv, ctxt, slot);
                if (i != -1) return offset + i;
                offset += l.Count;
            }
        } else if (list is ListIndices<Item> li) {
            int i = GetSlotIndex(li.List, inv, ctxt, slot);
            if (i != -1) return li.FromInnerIndex(i);
        }
        return -1;
    }

    public static void ClearCache() => s_slotToInv.Clear();

    private static readonly List<ModSubInventory> _classic = [];
    private static readonly List<ModSubInventory> _special = [];

    private static readonly Dictionary<VanillaSlot, Slot?> s_slotToInv = [];
}

public readonly record struct VanillaSlot(int Player, Item[] Inventory, int Context, int Slot);