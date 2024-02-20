using System;
using System.Collections.Generic;
using BetterInventory.DataStructures;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;

namespace BetterInventory;

public enum SubInventoryType {
    Classic,
    NonClassic,
    Secondary,
    Default
}

public class InventoryLoader : ILoadable {

    public static IEnumerable<ModSubInventory> SubInventories {
        get {
            foreach (var inventory in _special) yield return inventory;
            foreach (var inventory in _nonClassic) yield return inventory;
            foreach (var inventory in _classic) yield return inventory;
        }
    }

    public void Load(Mod mod) {}


    public void Unload() {
        ClearCache();
        static void Clear(IList<ModSubInventory> list) {
            foreach(var inventory in list) Utility.SetInstance(inventory, true);
            list.Clear();
        }
        Clear(_special);
        Clear(_nonClassic);
        Clear(_classic);
    }

    internal static void Register(ModSubInventory inventory){
        Utility.SetInstance(inventory);
        if(!LoaderUtils.HasOverride(inventory.GetType(), Reflection.ModSubInventory.Accepts)) _classic.Add(inventory);
        else if(inventory.Accepts(new()))_nonClassic.Add(inventory);
        else _special.Add(inventory);
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
    public static Slot? GetInventorySlot(Player player, Item[] inventory, int context, int slot) {
        VanillaSlot key = new(inventory, context, slot);
        if (player == Main.LocalPlayer && s_cachedInvs.TryGetValue(key, out var cached)) return cached;
        foreach (ModSubInventory slots in SubInventories) {
            int slotOffset = 0;
            foreach (ListIndices<Item> items in slots.Items(player).Lists) {
                int index = items.FromInnerIndex(slot);
                if (items.List == inventory && index != -1) {
                    Slot s = new(slots, index + slotOffset);
                    if (player == Main.LocalPlayer) s_cachedInvs[key] = s;
                    return s;
                }
                slotOffset += items.Count;
            }
        }
        if (player == Main.LocalPlayer) s_cachedInvs[key] = null;
        return null;
    }

    public static IEnumerable<ModSubInventory> GetSubInventories(Item item, SubInventoryType level) {
        List<ModSubInventory> secondary = new();
        foreach (var inv in _special) {
            if (!inv.Accepts(item)) continue;
            if (inv.IsDefault(item)) yield return inv;
            else if (level <= SubInventoryType.Secondary) secondary.Add(inv);
        }
        foreach (var inv in secondary) yield return inv;

        if (level > SubInventoryType.NonClassic) yield break;
        foreach (var inv in _nonClassic) {
            if (inv.Accepts(item)) yield return inv;
        }

        if (level > SubInventoryType.Classic) yield break;
        foreach (var inv in _classic) yield return inv;
    }

    public static void ClearCache() => s_cachedInvs.Clear();

    private readonly static List<ModSubInventory> _special = new();
    private readonly static List<ModSubInventory> _nonClassic = new();
    private readonly static List<ModSubInventory> _classic = new();

    private static readonly Dictionary<VanillaSlot, Slot?> s_cachedInvs = new();
}

public readonly record struct VanillaSlot(Item[] Inv, int Context, int Slot);