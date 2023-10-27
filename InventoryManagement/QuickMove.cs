using System;
using System.Collections.Generic;
using System.Linq;
using BetterInventory.DataStructures;
using Terraria;
using Terraria.Audio;
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
        tooltips.Add(new(
            BetterInventory.Instance, "QuickMove",
            string.Join(" > ", from slots in _displayedChain where slots is not null select slots.Inventory.GetLocalizedValue(slots.LocalizationKey))
        ));
    }

    public static void ProcessTriggers(Player player) {
        if (_moveTime == 0) _selectedItem[0] = player.selectedItem;
    }

    public static void HoverItem(Player player, Item[] inventory, int context, int slot) {
        if (!Enabled) return;

        InventorySlots? source = null;
        int sourceSlot = -1;
        foreach (ModInventory modInventory in InventoryLoader.Inventories) {
            foreach (InventorySlots invSlots in modInventory.Slots) {
                foreach((int c, Func<Player, ListIndices<Item>> s) in invSlots.Slots) {
                    if(c == context && (sourceSlot = s(player).FromInnerIndex(slot)) != -1){
                        source = invSlots;
                        goto found;
                    }
                }
            }
        }
    found:
        UpdateDisplayedMoveChain(player, source, inventory[slot]);
        TryMove(player, source, sourceSlot);
    }

    private static void TryMove(Player player, InventorySlots? source, int slot){
        if (_moveTime > 0) {
            _moveTime--;
            if (_moveSource != source || _moveSourceSlot != slot) _moveTime = 0;
            player.selectedItem = _selectedItem[1];
        }

        int targetSlot = Array.FindIndex(MoveKeys, key => PlayerInput.Triggers.JustPressed.KeyStatus[key]);
        if (targetSlot == -1 || source is null) return;

        if (_moveTime == 0 || _moveTargetSlot != targetSlot) {
            _moveSourceSlot = slot;
            _moveSource = source;
            _moveTargetSlot = targetSlot;
            _moveIndex = 0;
            _moveChain = new(_displayedChain);
        } else {
            UndoMove(player, source, _movedItems);
            _moveIndex = (_moveIndex+1) % (_moveChain.Count+1);
        }

        if (_moveIndex < _moveChain.Count) {
            targetSlot = Math.Min(targetSlot, _moveChain[_moveIndex].Items(player).Count - 1);
            _movedItems = Move(player, source.Items(player)[slot], source, slot, _moveChain[_moveIndex], targetSlot);

            player.selectedItem = _selectedItem[0];
            _moveChain[_moveIndex].Inventory.PostMove(player, _moveChain[_moveIndex], targetSlot); // TODO re-add
            _selectedItem[1] = player.selectedItem;
        } else _selectedItem[1] = _selectedItem[0];

        SoundEngine.PlaySound(SoundID.Grab);
        _moveTime = 60;
    }

    private static List<MovedItem> Move(Player player, Item item, InventorySlots source, int sourceSlot, InventorySlots target, int targetSlot) {
        if (!target.Inventory.FitsSlot(player, item, target, targetSlot, out var itemsToMove)) return new();
        bool[] canFavoriteAt = Reflection.ItemSlot.canFavoriteAt.GetValue();

        IList<Item> items = target.Items(player);
        List<(int slot, Item item)> freeItems = new();
        foreach (int s in itemsToMove) {
            freeItems.Add((s, items[s]));
            items[s] = new();
            target.Inventory.OnSlotChange(player, target, s);
        }
        if (!freeItems.Exists(m => m.slot == targetSlot)) {
            freeItems.Insert(0, (targetSlot, items[targetSlot]));
            items[targetSlot] = new();
            target.Inventory.OnSlotChange(player, target, targetSlot);
        }

        List<MovedItem> movedItems = new() { new(source, sourceSlot, item.type, item.prefix, item.favorited) };
        if (items[targetSlot].Stack(item, target.Inventory.MaxStack, canFavoriteAt[Math.Abs(target.GetContext(player, targetSlot))])) {
            source.Inventory.OnSlotChange(player, source, sourceSlot);
            target.Inventory.OnSlotChange(player, target, targetSlot);
        }

        // if (!freeItems[destSlot].IsAir && item.IsAir) // TODO notify SmartPickup

        for (int i = 0; i < freeItems.Count; i++) {
            (int slot, Item free) = freeItems[i];
            if (free.IsAir) continue;
            movedItems.Add(new(target, slot, free.type, free.prefix, free.favorited));
            free = source.Inventory.GetItem(player, free, GetItemSettings.GetItemInDropItemCheck);
            player.GetDropItem(ref free);
        }

        return movedItems;
    }
    private static void UndoMove(Player player, InventorySlots oldTarget, List<MovedItem> movedItems) {

        foreach (MovedItem moved in movedItems) {
            bool Predicate(Item i) => i.type == moved.Type && i.prefix == moved.Prefix;

            InventorySlots? inventory = oldTarget;
            int slot = inventory.Items(player).FindIndex(Predicate);
            if (slot == -1) (inventory, slot) = FindIndex(player, Predicate);
            if (inventory is null) continue;
            Item item = inventory.Items(player)[slot];
            bool fav = item.favorited;
            item.favorited = moved.Favorited;
            Move(player, item, inventory, slot, moved.Inventory, moved.Slot);
            item.favorited = fav;
        }
        movedItems.Clear();
    }

    public static (InventorySlots? source, int slot) FindIndex(Player player, Predicate<Item> predicate){
        foreach(ModInventory inventory in InventoryLoader.Inventories){
            foreach (InventorySlots slots in inventory.Slots) {
                int slot = slots.Items(player).FindIndex(predicate);
                if (slot != -1) return (slots, slot);
            }
        }
        return (null, -1);
    }

    public static void UpdateDisplayedMoveChain(Player player, InventorySlots? slots, Item item) {
        if (slots is null || item.IsAir) {
            _displayedChain.Clear();
            _displayedItem = ItemID.None;
        } else if (_displayedItem != item.type) {
            _displayedChain = SmartishChain(player, item, slots);
            _displayedItem = item.type;
        }
    }

    private static List<InventorySlots> DefaultChain(Player player, Item item, InventorySlots source) {
        List<InventorySlots> targetSlots = new();
        foreach (ModInventory inventory in InventoryLoader.Inventories) {
            foreach (InventorySlots slots in inventory.Slots) {
                if (slots.LocalizationKey is null || slots.Accepts is not null && !slots.Accepts(item)) continue;
                if (slots != source || slots.Items(player).Count > 1) targetSlots.Add(slots);
            }
        }
        return targetSlots;
    }
    private static List<InventorySlots> SmartishChain(Player player, Item item, InventorySlots source) {
        List<InventorySlots> targetSlots = DefaultChain(player, item, source);

        targetSlots.Sort();

        int i = targetSlots.FindIndex(s => s == source && s.Items(player).Contains(item));
        if (source.Accepts is not null && i != -1){
            var self = targetSlots[i];
            targetSlots.RemoveAt(i);
            targetSlots.Insert(0, self);
        } 
        return targetSlots;
    }

    private static int _displayedItem = ItemID.None;
    private static List<InventorySlots> _displayedChain = new();
    
    private static int _moveIndex;
    private static List<InventorySlots> _moveChain = new();

    private static int _moveTime;
    private static InventorySlots? _moveSource;
    private static int _moveSourceSlot;
    private static int _moveTargetSlot;
    
    private static List<MovedItem> _movedItems = new();

    private static int[] _selectedItem = new int[2];
}
public readonly record struct MovedItem(InventorySlots Inventory, int Slot, int Type, int Prefix, bool Favorited);

public readonly record struct ArrayItem<T>(T[] Array, int Slot){
    public ref T Item => ref Array[Slot];
}