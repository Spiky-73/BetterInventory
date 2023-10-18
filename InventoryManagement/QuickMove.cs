using System;
using System.Collections.Generic;
using System.Linq;
using BetterInventory.InventoryManagement.Inventories;
using Terraria;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;

namespace BetterInventory.InventoryManagement;

public sealed partial class QuickMove {
    public static bool Enabled => Configs.ClientConfig.Instance.quickMove;

    public static readonly string[] MoveKeys = new[] {
        "Hotbar1",
        "Hotbar2",
        "Hotbar3",
        "Hotbar4",
        "Hotbar5",
        "Hotbar6",
        "Hotbar7",
        "Hotbar8",
        "Hotbar9",
        "Hotbar10"
    };

    public static void AddMoveChainLine(Item _, List<TooltipLine> tooltips){
        if (!Enabled || _displayedChain.Count == 0) return;
        tooltips.Add(new(BetterInventory.Instance, "QuickMove", string.Join(" > ", from sub in _displayedChain where sub is not null select sub.Value.Inventory.GetLocalizedValue(sub.Value.LocalizationKey))));
    }

    public static void TryMove(Player player, Item[] inventory, int context, int slot) {
        if (!Enabled) return;

        if (_moveTime > 0) _moveTime--;


        ModInventory? sourceInventory = InventoryLoader.Inventories.FirstOrDefault(i => i.Contexts.Contains(context));
        int index = sourceInventory!.ToIndex(player, context, slot);
        UpdateMoveChain(player, sourceInventory, inventory[slot], index);

        int destSlot = Array.FindIndex(MoveKeys, key => PlayerInput.Triggers.JustPressed.KeyStatus[key]);
        if (destSlot == -1) return;

        if (_moveTime == 0 || _moveSlot != slot || _moveInventory != sourceInventory || _moveDest != destSlot) {
            _moveIndex = 0;
            _moveChain = new(_displayedChain);
            if (_moveChain.Count == 0) return;
        } else {
            UndoMove(player, _moveInventory!, _movedItems);
            _moveIndex++;
        }
        _moveIndex %= _moveChain.Count;
        if (_moveChain[_moveIndex] is SubInventory sub) {
            // sub.Inventory.Focus(); // TODO re-add
            IList<int> slots = sub.Slots(player).Where(i => sub.Inventory.SlotEnabled(player, i)).ToArray();
            _movedItems = Move(player, inventory[slot], sourceInventory!, index, _moveChain[_moveIndex]!.Value.Inventory, slots[Math.Min(destSlot, slots.Count - 1)]);
        }
        _moveTime = 60; // TODO config
        _moveDest = destSlot ;
        _moveSlot = slot;
        _moveInventory = sourceInventory;
    }

    private static List<MovedItem> Move(Player player, Item item, ModInventory source, int sourceSlot, ModInventory target, int targetSlot) {
        if (!target.CanSlotAccepts(player, item, targetSlot, out var itemsToMove)) return new();

        IList<Item> items = target.Items(player);
        List<(int slot, Item item)> freeItems = new() { };
        foreach (int s in itemsToMove) {
            freeItems.Add((s, items[s]));
            items[s] = new();
        }
        if (!freeItems.Exists(m => m.slot == targetSlot)) {
            freeItems.Insert(0, (targetSlot, items[targetSlot]));
            items[targetSlot] = new();
        }

        (int type, int prefix) = (item.type, item.prefix);
        items[targetSlot].Stack(item, target.MaxStack);
        items[targetSlot].Stack(freeItems[0].item, target.MaxStack);

        // return movedItems;

        // if (!freeItems[destSlot].IsAir && item.IsAir) // TODO notify SmartPickup

        List<MovedItem> movedItems = new() { new(source, sourceSlot, type, prefix) };
        for (int i = 0; i < freeItems.Count; i++) {
            (int slot, Item free) = freeItems[i];
            movedItems.Add(new(target, slot, free.type, free.prefix));
            free = source.GetItem(player, free, GetItemSettings.GetItemInDropItemCheck);
            player.GetDropItem(ref free);
        }

        return movedItems;
    }

    private static void UndoMove(Player player, ModInventory source, List<MovedItem> movedItems) {
        foreach(MovedItem moved in movedItems) {

            int slot = source.IndexOf(player, moved.Type, moved.Prefix);
            if (slot == -1) (source, slot) = IndexOf(player, moved.Type, moved.Prefix);
            if (slot == -1) continue;
            Item item = source.Items(player)[slot];
            // if (!moved.Inventory.CanSlotAccepts(player, item, moved.Slot)) continue;
            Move(player, item, source, slot, moved.Inventory, moved.Slot);
        }
        movedItems.Clear();
    }

    public static (ModInventory source, int slot) IndexOf(Player player, int type, int prefix){
        foreach(ModInventory context in InventoryLoader.Inventories){
            int slot = context.IndexOf(player, type, prefix);
            if (slot != -1) return (context, slot);
        }
        return (null!, -1);
    }

    public static void UpdateMoveChain(Player player, ModInventory? inventory, Item item, int index) {
        if (inventory is null || item.IsAir) {
            _displayedChain.Clear();
            _displayedItem = ItemID.None;
        } else if (_displayedItem != item.type) {
            // TODO add variants
            List<SubInventory?> chain = OrderChain(player, item, inventory, index);
            if (true) chain.Add(null); // TODO  config
            _displayedChain = chain;
            _displayedItem = item.type;
        }
    }

    private static List<SubInventory?> OrderChain(Player player, Item item, ModInventory source, int index) {
        List<SubInventory?> subs = new();
        foreach(ModInventory inventory in InventoryLoader.Inventories){
            foreach(SubInventory sub in inventory.SubInventories){
                IList<int> slots = sub.Slots(player);
                if (sub.Accepts(item) && (inventory != source || slots.Count > 1 || slots[0] != index)) subs.Add(sub);
            }
        }
        return subs;
    }

    private static int _displayedItem = ItemID.None;
    private static List<SubInventory?> _displayedChain = new();
    
    private static int _moveIndex;
    private static List<SubInventory?> _moveChain = new();

    private static int _moveTime;
    private static int _moveDest;
    private static int _moveSlot;
    private static ModInventory? _moveInventory;
    
    private static List<MovedItem> _movedItems = new();

    // public List<(int slot, Item item)> PlaceItem(Player player, Item item, ref int slot) {
    //     IList<int> itemsToMove = GetIncompatibleItems(player, item, out bool canPickup);
    //     if (!canPickup) return new();

    //     IList<Item> items = Items(player);
    //     slot = ForcedSlot(item) ?? Math.Clamp(slot, 0, items.Count - 1);
    //     if (!SlotAccepts(player, item, slot)) return new();


    //     List<(int slot, Item item)> movedItems = new() { };

    //     foreach (int s in itemsToMove) {
    //         movedItems.Add((s, items[s]));
    //         items[s] = new();
    //     }
    //     int fixedSlot = slot;
    //     if (!movedItems.Exists(m => m.slot == fixedSlot)) {
    //         movedItems.Insert(0, (slot, items[slot]));
    //         items[slot] = new();
    //     }

    //     items[slot].Stack(item, MaxStack);
    //     items[slot].Stack(movedItems[0].item, MaxStack);

    //     return movedItems;
    // }
}
public readonly record struct MovedItem(ModInventory Inventory, int Slot, int Type, int Prefix);

public readonly record struct ArrayItem<T>(T[] Array, int Slot){
    public ref T Item => ref Array[Slot];
}