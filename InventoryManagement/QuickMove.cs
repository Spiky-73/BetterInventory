using System;
using System.Collections.Generic;
using System.Linq;
using BetterInventory.DataStructures;
using Terraria;
using Terraria.Audio;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace BetterInventory.InventoryManagement;

public sealed class QuickMove : ILoadable {

    public void Load(Mod mod) {
        On_Main.DrawInterface_36_Cursor += HookAlternateChain;

    }

    public void Unload() {}

    public static Configs.QuickMove Config => Configs.InventoryManagement.Instance.quickMove.Value;

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
        if (!_hover || !Config.showTooltip || _displayedChain.Count == 0) return;
        tooltips.Add(new(
            BetterInventory.Instance, "QuickMove",
            string.Join(" > ", from slots in _displayedChain where slots is not null select slots.Inventory.GetLocalizedValue(slots.LocalizationKey))
        ));
    }

    public static void ProcessTriggers(Player player) {
        if (_moveTime == 0) _selectedItem[0] = player.selectedItem;
        _hover = false;
    }

    public static void HoverItem(Player player, Item[] inventory, int context, int slot) {
        if (!Configs.InventoryManagement.Instance.quickMove) return;
        _hover = true;
        InventorySlots? source = null;
        int sourceSlot = -1;
        foreach (ModInventory modInventory in InventoryLoader.Inventories) {
            foreach (InventorySlots invSlots in modInventory.Slots) {
                int slotOffset = 0;
                foreach((int c, Func<Player, ListIndices<Item>> s) in invSlots.Slots) {
                    bool accessory = context == ItemSlot.Context.EquipAccessoryVanity || context == ItemSlot.Context.EquipAccessory;
                    ListIndices<Item> items = s(player);
                    if ((c == context || (accessory && c == -context)) && (sourceSlot = items.FromInnerIndex(slot)) != -1){
                        source = invSlots;
                        sourceSlot += slotOffset;
                        goto found;
                    }
                    slotOffset += items.Count;
                }
            }
        }
    found:
        UpdateDisplayedMoveChain(player, source, inventory[slot]);
        UpdateChain(player, source, sourceSlot);
    }

    private static void HookAlternateChain(On_Main.orig_DrawInterface_36_Cursor orig) {
        if (_moveTime > 0 && !_hover) {
            if (!Main.playerInventory) _moveTime = 0;
            else {
                Main.LocalPlayer.selectedItem = _selectedItem[1];
                if (PlayerInput.Triggers.JustPressed.KeyStatus[MoveKeys[_moveTargetSlot]]) ContinueChain(Main.LocalPlayer);
            }
        }
        orig();
    }

    public static void UpdateChain(Player player, InventorySlots? inventory, int slot) {

        if (_moveTime > 0) {
            _moveTime--;
            if (inventory is null) _moveTime = 0;
            else if (_moveTime == Config.chainTime - 1) _validSlots[inventory] = slot;
            else if (!_validSlots.TryGetValue(inventory, out int index) || index != slot) _moveTime = 0;
            player.selectedItem = _selectedItem[1];
        }

        int targetSlot = Array.FindIndex(MoveKeys, key => PlayerInput.Triggers.JustPressed.KeyStatus[key]);
        if (targetSlot == -1 || inventory is null) return;

        if (_moveTime == 0 || _moveTargetSlot != targetSlot) SetupChain(inventory, slot, targetSlot);
        ContinueChain(player);
    }
    private static void SetupChain(InventorySlots? inventory, int slot, int targetSlot) {
        _moveSourceSlot = slot;
        _moveSource = inventory!;
        _moveTargetSlot = targetSlot;
        _moveIndex = 0;
        _moveChain = new(_displayedChain);
        _validSlots.Clear();
        _validSlots[inventory!] = slot;
        _movedItems.Clear();
    }
    private static void ContinueChain(Player player) {
        UndoMove(player, _movedItems);
        
        player.selectedItem = _selectedItem[0];
        if (_moveIndex < _moveChain.Count) {
            int targetSlot = Math.Min(_moveTargetSlot, _moveChain[_moveIndex].Items(player).Count - 1);
            _movedItems = Move(player, _moveSource.Items(player)[_moveSourceSlot], _moveSource, _moveSourceSlot, _moveChain[_moveIndex], targetSlot);

            _moveChain[_moveIndex].Inventory.Focus(player, _moveChain[_moveIndex], targetSlot);
        } else {
            _moveSource.Inventory.Focus(player, _moveSource, _moveSourceSlot);
        }
        _selectedItem[1] = player.selectedItem;
        SoundEngine.PlaySound(SoundID.Grab);
        _moveTime = Config.chainTime;
        _moveIndex = (_moveIndex + 1) % (_moveChain.Count + (Config.returnToSlot ? 1 : 0));
    }

    private static List<MovedItem> Move(Player player, Item item, InventorySlots source, int sourceSlot, InventorySlots target, int targetSlot) {
        if (!target.Inventory.FitsSlot(player, item, target, targetSlot, out var itemsToMove)) return new();
        bool[] canFavoriteAt = Reflection.ItemSlot.canFavoriteAt.GetValue();

        IList<Item> items = target.Items(player);
        List<Item> freeItems = new();
        List<MovedItem> movedItems = new() { new(source, sourceSlot, target, item.type, item.prefix, item.favorited) };
        
        void FreeTargetItem(int slot) {
            freeItems.Add(items[slot]);
            movedItems.Add(new(target, slot, source, items[slot].type, items[slot].prefix, items[slot].favorited));
            items[slot] = new();
            target.Inventory.OnSlotChange(player, target, slot);            
        }

        FreeTargetItem(targetSlot);
        foreach (int slot in itemsToMove) FreeTargetItem(slot);

        bool canFavorite = canFavoriteAt[Math.Abs(target.GetContext(player, targetSlot))];
        bool moved = items[targetSlot].Stack(item, out _, target.Inventory.MaxStack, canFavorite);
        items[targetSlot].Stack(freeItems[0], out _, target.Inventory.MaxStack, canFavorite);
        if (moved) {
            source.Inventory.OnSlotChange(player, source, sourceSlot);
            target.Inventory.OnSlotChange(player, target, targetSlot);
        }

        // if (!freeItems[destSlot].IsAir && item.IsAir) // TODO notify SmartPickup

        for (int i = 0; i < freeItems.Count; i++) {
            Item free = freeItems[i];
            if (free.IsAir) continue;
            free = source.Inventory.GetItem(player, free, GetItemSettings.GetItemInDropItemCheck);
            player.GetDropItem(ref free);
        }

        return movedItems;
    }
    private static void UndoMove(Player player, List<MovedItem> movedItems) {

        foreach (MovedItem moved in movedItems) {
            bool Predicate(Item i) => i.type == moved.Type && i.prefix == moved.Prefix;

            InventorySlots? inventory = moved.To;
            int slot = inventory.Items(player).FindIndex(Predicate);
            if (slot == -1) (inventory, slot) = FindIndex(player, Predicate);
            if (inventory is null) continue;
            Item item = inventory.Items(player)[slot];
            bool fav = item.favorited;
            item.favorited = moved.Favorited;
            Move(player, item, inventory, slot, moved.From, moved.Slot);
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

        targetSlots.SortSlots();

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
    
    private static int _moveTime;
    private static int _moveIndex;
    private static List<InventorySlots> _moveChain = new();

    private static InventorySlots _moveSource = null!;
    private static int _moveSourceSlot;
    private static int _moveTargetSlot;
    private static readonly Dictionary<InventorySlots, int> _validSlots = new();
    
    private static List<MovedItem> _movedItems = new();

    private static int[] _selectedItem = new int[2];
    private static bool _hover;
}
public readonly record struct MovedItem(InventorySlots From, int Slot, InventorySlots To, int Type, int Prefix, bool Favorited);