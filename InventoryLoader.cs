using System;
using System.Collections.Generic;
using BetterInventory.DataStructures;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;

namespace BetterInventory;

public enum SubInventoryType {
    NoCondition,
    LooseCondition,
    WithCondition,
    RightClickTarget
}

public class InventoryLoader : ILoadable {

    public static IEnumerable<ModSubInventory> Inventories {
        get {
            foreach (var inventory in _condition) yield return inventory;
            foreach (var inventory in _looseCondition) yield return inventory;
            foreach (var inventory in _noCondition) yield return inventory;
        }
    }

    public void Load(Mod mod) {}

    public void Unload() {
        static void Clear(IList<ModSubInventory> list) {
            foreach(var inventory in list) Utility.SetInstance(inventory, true);
            list.Clear();
        }
        Clear(_condition);
        Clear(_looseCondition);
        Clear(_noCondition);
    }

    internal static void Register(ModSubInventory inventory){
        Utility.SetInstance(inventory);
        inventory.HasCondition = LoaderUtils.HasOverride(inventory.GetType(), Reflection.ModSubInventory.Accepts);
        if(!inventory.HasCondition) _noCondition.Add(inventory);
        else if(inventory.Accepts(new()))_looseCondition.Add(inventory);
        else _condition.Add(inventory);
    }

    public static Slot? FindItem(Player player, Predicate<Item> predicate) {
        foreach (ModSubInventory slots in Inventories) {
            int slot = slots.Items(player).FindIndex(predicate);
            if (slot != -1) return new(slots, slot);
        }
        return null;
    }

    public static Slot? GetInventorySlot(Player player, Item[] inventory, int context, int slot) {
        foreach (ModSubInventory slots in Inventories) {
            int slotOffset = 0;
            foreach (ListIndices<Item> items in slots.Items(player).Lists) {
                int index = items.FromInnerIndex(slot);
                if (items.List == inventory && index != -1) return new(slots, index + slotOffset);
                slotOffset += items.Count;
            }
        }
        return null;
    }

    public static IEnumerable<ModSubInventory> GetInventories(Item item, SubInventoryType level) {
        List<ModSubInventory> withCondition  = new();
        foreach(var inv in _condition) {
            if (!inv.Accepts(item)) continue;
            if (inv.IsRightClickTarget(item)) yield return inv;
            else if (level <= SubInventoryType.WithCondition) withCondition.Add(inv);
        }
        foreach (var inv in withCondition) yield return inv;
        if (level <= SubInventoryType.LooseCondition) {
            foreach (var inv in _looseCondition) {
                if (level <= SubInventoryType.WithCondition && inv.Accepts(item)) yield return inv;
            }
        }
        if (level <= SubInventoryType.NoCondition) {
            foreach (var inv in _noCondition) {
                if (level <= SubInventoryType.WithCondition && inv.Accepts(item)) yield return inv;
            }
        }
    }

    private readonly static List<ModSubInventory> _condition = new();
    private readonly static List<ModSubInventory> _looseCondition = new();
    private readonly static List<ModSubInventory> _noCondition = new();
}