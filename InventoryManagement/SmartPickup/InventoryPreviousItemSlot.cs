using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Terraria;
using Terraria.ModLoader.IO;

namespace BetterInventory.InventoryManagement.SmartPickup;

public sealed class InventoryPreviousItemSlot {
    public Item Get(int slot) => _previousItems[slot];
    public void Set(int slot, Item item) => _previousItems[slot] = new(item.type, 1) { favorited = item.favorited };
    public void Unset(int slot) => _previousItems.Remove(slot);

    public bool TryGet(int slot, [MaybeNullWhen(false)] out Item item) => _previousItems.TryGetValue(slot, out item);

    public int[] GetSlots(Item item) => _previousItems.Where(kvp => kvp.Value.type == item.type).Select(kvp => kvp.Key).ToArray();
    public void ClearSlots(Item item) {
        foreach(int slot in GetSlots(item)) Unset(slot);
    }
    public void Replace(Item item, Item newItem) {
        foreach(var slot in GetSlots(item)) Set(slot, newItem);
    }

    internal readonly Dictionary<int, Item> _previousItems = [];
    internal readonly Dictionary<int, TagCompound> _unloadedItems = [];
}