using System;
using System.Collections.Generic;
using SpikysLib.Collections;
using SpikysLib.Configs;
using SpikysLib.DataStructures;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;
using System.Linq;

namespace BetterInventory;

public sealed class InventoryLoader : ILoadable {

    public static IEnumerable<ModSubInventory> GetInventories(Player player) 
        => SubInventoriesTemplates.SelectMany(subInventory => subInventory.GetInventories(player));

    public static IEnumerable<ModSubInventory> GetActiveInventories(Player player) 
        => SubInventoriesTemplates.SelectMany(subInventory => subInventory.GetActiveInventories(player));

    public static IEnumerable<ModSubInventory> GetPreferredInventories(Player player) 
        => _preferredTemplates.SelectMany(subInventory => subInventory.GetInventories(player));

    public static IEnumerable<ModSubInventory> GetPreferredActiveInventories(Player player) 
        => _preferredTemplates.SelectMany(subInventory => subInventory.GetActiveInventories(player));

    public static IEnumerable<ModSubInventory> PreferredInventoriesTemplates => _preferredTemplates;
    public static IEnumerable<ModSubInventory> AllPurposeInventoriesTemplates => _allPurposeTemplates;
    public static IEnumerable<ModSubInventory> SubInventoriesTemplates {
        get {
            foreach (var inventory in _preferredTemplates) yield return inventory;
            foreach (var inventory in _allPurposeTemplates) yield return inventory;
        }
    }

    public void Load(Mod mod) {
        On_Recipe.FindRecipes += HookClearCache;
    }

    private static void HookClearCache(On_Recipe.orig_FindRecipes orig, bool canDelayCheck) {
        orig(canDelayCheck);
        ClearCache();
    }

    public void Unload() {
        ClearCache();
        static void Clear(IList<ModSubInventory> list) {
            foreach (var inventory in list) ConfigHelper.SetInstance(inventory, true);
            list.Clear();
        }
        Clear(_allPurposeTemplates);
        Clear(_preferredTemplates);
    }

    internal static void Register(ModSubInventory inventory) {
        ConfigHelper.SetInstance(inventory);
        inventory.CanBePreferredInventory = LoaderUtils.HasOverride(inventory, i => i.IsPreferredInventory);
        List<ModSubInventory> list = inventory.CanBePreferredInventory ? _preferredTemplates : _allPurposeTemplates;

        int before = list.FindIndex(i => inventory.ComparePositionTo(i) < 0 || i.ComparePositionTo(inventory) > 0);
        if (before != -1) list.Insert(before, inventory);
        else list.Add(inventory);
    }

    public static InventorySlot? FindItem(Player player, Predicate<Item> predicate) {
        foreach (ModSubInventory subInventory in GetInventories(player)) {
            int slot = subInventory.Items.FindIndex(predicate);
            if (slot != -1) return new(subInventory, slot);
        }
        return null;
    }

    public static bool IsInventorySlot(Player player, Item[] inv, int context, int slot, out InventorySlot itemSlot) {
        var s = GetInventorySlot(player, inv, context, slot);
        itemSlot = s ?? default;
        return s.HasValue;
    }
    public static InventorySlot? GetInventorySlot(Player player, Item[] inventory, int context, int slot) => s_slotToInv.GetOrAdd(new(player.whoAmI, inventory, context, slot), () => {
        foreach (ModSubInventory subInventory in GetInventories(player)) {
        int i = GetSlotIndex(subInventory.Items, inventory, context, slot);
            if (i != -1) return new(subInventory, i);
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

    private static readonly List<ModSubInventory> _preferredTemplates = [];
    private static readonly List<ModSubInventory> _allPurposeTemplates = [];

    private static readonly Dictionary<VanillaSlot, InventorySlot?> s_slotToInv = [];
}

public readonly record struct VanillaSlot(int Player, Item[] Inventory, int Context, int Slot);