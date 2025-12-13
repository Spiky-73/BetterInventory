using System;
using System.Collections.Generic;
using System.Linq;
using SpikysLib;
using SpikysLib.Collections;
using Terraria;
using Terraria.UI;

namespace BetterInventory.Features.QuickMove;

public static class QuickMoveUtils {

    public static readonly string[] MoveKeyNames = [.. new SpikysLib.DataStructures.Range(0, 10).Select(i => $"Hotbar{i + 1}")];

    public static int HotkeyToSlotRaw(int hotkey, int slotCount) => Configs.QuickMove.Value.hotkeyMode switch {
        Configs.HotkeyMode.FromEnd => slotCount - MoveKeyNames.Length + hotkey,
        Configs.HotkeyMode.Reversed => MoveKeyNames.Length - hotkey - 1,
        Configs.HotkeyMode.Hotbar or _ => hotkey
    };
    public static int HotkeyToSlot(int hotkey, int slotCount) => Math.Clamp(HotkeyToSlotRaw(hotkey, slotCount), 0, slotCount - 1);

    /// <summary>
    /// Moves <paramref name="source"/> to <paramref name="target"/>, moving conflicting items if needed.
    /// If possible, <paramref name="source"/> and <paramref name="target"/> will be swapped
    /// </summary>
    /// <param name="source">An InventorySlot to move from</param>
    /// <param name="target">An InventorySlot to move <paramref name="source"/> to</param>
    /// <returns>A list containing every item moved by the function</returns>
    public static List<MovedItem> Move(InventorySlot source, InventorySlot target) {
        Item item = source.Item;

        // Check if the item can go to its target, moving items if needed
        if (!target.Fits(item, out var itemsToMove)) return [];


        // Move all the conflicting items out of their slots
        IList<Item> items = target.Inventory.Items;
        List<Item> freeItems = [];
        List<MovedItem> movedItems = [new(source, target.Inventory, item.type, item.prefix, item.favorited)];

        void FreeTargetItem(InventorySlot slot) {
            Item item = slot.Item;
            freeItems.Add(item);
            movedItems.Add(new(slot, source.Inventory, item.type, item.prefix, item.favorited));
            slot.Item = new();
            slot.OnChange();
        }
        FreeTargetItem(target);
        foreach (InventorySlot slot in itemsToMove) FreeTargetItem(slot);

        // Preserve the favorite states of items 
        // If item was favorited and will not stay favorited, the item it swaps with should be favorited
        bool canFavorite = ItemSlot.canFavoriteAt[Math.Abs(target.Inventory.Context)];
        bool keepFavorited = !canFavorite && item.favorited;

        // Move the item into its target slot
        items[target.Index] = ItemHelper.MoveInto(items[target.Index], item, out _, target.Inventory.MaxStack, canFavorite);

        // Try to move (or stack) the previous item back in its slot 
        items[target.Index] = ItemHelper.MoveInto(items[target.Index], freeItems[0], out _, target.Inventory.MaxStack, canFavorite);

        // Notify the slots
        source.OnChange();
        target.OnChange();

        // Makes the player pick back up the items that were freed
        var player = source.Inventory.Entity;
        for (int i = 0; i < freeItems.Count; i++) {
            Item free = freeItems[i];
            if (free.IsAir) continue;

            // Try GetItem on the inventory item came from, preserving the favorite state of the moved item
            bool f = free.favorited;
            if (Configs.ItemActions.KeepSwappedFavorited && keepFavorited) free.favorited = true;
            free = source.GetItem(free, GetItemSettings.GetItemInDropItemCheck);
            free.favorited = f; // Restore the original favorite state

            // General GetItem if it can't go in the original inventory
            player.GetDropItem(ref free);
        }

        return movedItems;
    }

    /// <summary>
    /// Moves the items described in <paramref name="movedItems"/> to their slot before they were moved.
    /// </summary>
    /// <param name="movedItems">A list of moved items</param>
    public static void UndoMove(List<MovedItem> movedItems) {
        foreach (MovedItem moved in movedItems) {
            // Try to find the item in the expected inventory, or in the entire player's inventory otherwise
            bool Predicate(Item i) => i.type == moved.Type && i.prefix == moved.Prefix;
            int index = moved.To.Items.FindIndex(Predicate);
            InventorySlot? slot = index != -1 ? new(moved.To, index) : InventoryLoader.FindItem(moved.To.Entity, Predicate);
            if (slot is null) continue;

            // Undo the move
            Item item = slot.Value.Item;
            bool fav = item.favorited;
            item.favorited = moved.Favorited;
            Move(slot.Value, moved.From);
            item.favorited = fav;
        }
        movedItems.Clear();
    }

    public static List<ModSubInventory> GetChain(Player player, Item item, ModSubInventory? prioritizedInventory) {
        var inventories = Configs.QuickMove.InactiveInventories ? InventoryLoader.GetPreferredInventories(player) : InventoryLoader.GetPreferredActiveInventories(player);
        List<ModSubInventory> targets = [.. inventories.Where(i => i.Accepts(item) && i.Items.Count > 0)];
        if (prioritizedInventory is not null && targets.Remove(prioritizedInventory) && prioritizedInventory.Items.Count > 1) targets.Insert(0, prioritizedInventory);
        return targets;
    }
}

/// <summary>
/// Describes the movement of the item from the slot <paramref name="From"/> to the inventory <paramref name="To"/>.
/// </summary>
/// <param name="From">The slot where the item was before the move</param>
/// <param name="To">The inventory where the item went after the move</param>
/// <param name="Type">The item's type</param>
/// <param name="Prefix">The item's prefix</param>
/// <param name="Favorited">The item's favorite state</param>
public readonly record struct MovedItem(InventorySlot From, ModSubInventory To, int Type, int Prefix, bool Favorited);
