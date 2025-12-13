using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BetterInventory.Default.Inventories;
using Terraria;
using Terraria.ModLoader;

namespace BetterInventory.Features.QuickMove;

public static class QuickMoveChain {

    public static bool InChain() => _chain.Count > 1;

    public static void SetupChain(InventorySlot itemSlot, Func<int, int> countToSlot) {
        _chain = [];
        _index = 0;
        _movedItems = [];
        
        if (itemSlot.Item.IsAir) {
            ModSubInventory hotbar = ModContent.GetInstance<Hotbar>().NewInstance(Main.LocalPlayer);
            InventorySlot from = new(hotbar, countToSlot(hotbar.Items.Count));

            // No item to chain on
            if (!Configs.QuickMove.Instance.bringItem || from.Item.IsAir) return;

            // hotbar, source, chain
            var inventories = QuickMoveUtils.GetChain(itemSlot.Inventory.Entity, from.Item, itemSlot.Inventory);
            inventories.Remove(hotbar);
            _chain = [from, itemSlot, .. inventories.Select(i => new InventorySlot(i, countToSlot(i.Items.Count)))];
        } else {
            // source, chain
            _chain = [itemSlot, .. QuickMoveUtils.GetChain(itemSlot.Inventory.Entity, itemSlot.Item, itemSlot.Inventory).Select(i => new InventorySlot(i, countToSlot(i.Items.Count)))];
        }
    }

    public static void BreakChain() {
        _chain.Clear();
    }

    public static void ContinueChain() {
        if (!InChain()) return;

        if (_index != 0) {
            if (Configs.QuickMove.Instance.followItem) _chain[_index].Unfocus();
            if (Configs.QuickMove.Instance.returnToSlot || _index != _chain.Count - 1) QuickMoveUtils.UndoMove(_movedItems);
        }

        _index++;
        if (_index >= _chain.Count) {
            BreakChain();
        } else {
            if (Configs.QuickMove.Instance.followItem) _chain[_index].Focus();
            _movedItems = QuickMoveUtils.Move(_chain[0], _chain[_index]);
        }
        Recipe.FindRecipes();
    }

    public static ReadOnlyCollection<InventorySlot> Chain() => _chain.AsReadOnly();
    public static int ChainIndex() => _index;

    private static List<InventorySlot> _chain = [];
    private static int _index = 0;
    private static List<MovedItem> _movedItems = [];
}
