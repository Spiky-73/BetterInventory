using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BetterInventory.Default.Inventories;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpikysLib;
using SpikysLib.Collections;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace BetterInventory.Features.QuickMove;

public sealed class QuickMovePlayer : ModPlayer {

    public override bool IsLoadingEnabled(Mod mod) => !Configs.Compatibility.CompatibilityMode || FeaturesConfig.QuickMove;
    public override void Load() {
        On_Main.DrawInventory += HookDrawInventory;
        On_ItemSlot.Draw_SpriteBatch_ItemArray_int_int_Vector2_Color += HookItemSlotDraw;
    }

    public override void ProcessTriggers(TriggersSet triggersSet) {
        if (!FeaturesConfig.QuickMove) return;
        // Break the chain if we close the inventory
        if (!Main.playerInventory) {
            QuickMove.BreakChain();
            return;
        }

        // Prevents Terraria from changing the selected item during the chain because of hotbar keys
        if (QuickMove.InChain()) triggersSet.KeyStatus[MoveKeyNames[_chainKey]] = false;
        // Save the selected item before the chain to restore it when it starts, this happens after the vanilla Hotbar behavior 
        else _preChainSelectedItem = Main.LocalPlayer.selectedItem;

    }

    private static void HookDrawInventory(On_Main.orig_DrawInventory orig, Main self) {
        orig(self);
        if (!FeaturesConfig.QuickMove) return;
        if (QuickMove.InChain() && !Main.LocalPlayer.mouseInterface) HandleInput(() => null);
    }
    public override bool HoverSlot(Item[] inventory, int context, int slot) {
        if (!FeaturesConfig.QuickMove) return false;
        HandleInput(() => InventoryLoader.GetInventorySlot(Main.LocalPlayer, inventory, context, slot));
        return false;
    }
    private static void HandleInput(Func<InventorySlot?> getSourceSlot) {
        // Breaks the chain if the cursors goes outside the original slot or if we waited too long
        if (!_slotPosition.Contains(Main.mouseX, Main.mouseY) || _graceTime == 0) QuickMove.BreakChain();
        if (_graceTime > 0) _graceTime--;

        // Check if we pressed a MoveKey
        int moveKey = Array.FindIndex(MoveKeyNames, key => PlayerInput.Triggers.JustPressed.KeyStatus[key]);
        if (moveKey == -1) return;

        if (!QuickMove.InChain() || _chainKey != moveKey) {
            // Break the chain if we cannot get the InventorySlot
            var itemSlot = getSourceSlot();
            if (!itemSlot.HasValue) {
                QuickMove.BreakChain();
                return;
            }
            // Otherwise create a new one
            _chainKey = moveKey;
            _saveSlotPosition = true;
            Main.LocalPlayer.selectedItem = _preChainSelectedItem;
            QuickMove.SetupChain(itemSlot.Value, c => HotkeyToSlot(moveKey, c));
        }
        QuickMove.ContinueChain();
        SoundEngine.PlaySound(SoundID.Grab);
        _graceTime = QuickMoveConfig.Instance.graceTime;
    }

    private static void HookItemSlotDraw(On_ItemSlot.orig_Draw_SpriteBatch_ItemArray_int_int_Vector2_Color orig, SpriteBatch spriteBatch, Item[] inv, int context, int slot, Vector2 position, Color lightColor) {
        orig(spriteBatch, inv, context, slot, position, lightColor);
        if (!FeaturesConfig.QuickMove) return;
        if (!_saveSlotPosition) return;
        _saveSlotPosition = false;
        _slotPosition = new((int)position.X, (int)position.Y, (int)(TextureAssets.InventoryBack.Width() * Main.inventoryScale), (int)(TextureAssets.InventoryBack.Height() * Main.inventoryScale));
    }


    public static readonly string[] MoveKeyNames = [.. new SpikysLib.DataStructures.Range(0, 10).Select(i => $"Hotbar{i + 1}")];

    public static int HotkeyToSlotRaw(int hotkey, int slotCount) => QuickMoveConfig.Instance.hotkeyMode switch {
        HotkeyMode.FromEnd => slotCount - MoveKeyNames.Length + hotkey,
        HotkeyMode.Reversed => MoveKeyNames.Length - hotkey - 1,
        HotkeyMode.Hotbar or _ => hotkey
    };
    public static int HotkeyToSlot(int hotkey, int slotCount) => Math.Clamp(HotkeyToSlotRaw(hotkey, slotCount), 0, slotCount - 1);

    public static int ChainKey() => _chainKey;

    private static int _preChainSelectedItem = -1;

    private static int _graceTime;
    private static int _chainKey = -1;

    private static bool _saveSlotPosition;
    private static Rectangle _slotPosition;
}

public static class QuickMove {

    public static List<ModSubInventory> GetChain(Player player, Item item, ModSubInventory? prioritizedInventory) {
        var inventories = QuickMoveConfig.Instance.inactiveInventories ? InventoryLoader.GetPreferredInventories(player) : InventoryLoader.GetPreferredActiveInventories(player);
        List<ModSubInventory> targets = [.. inventories.Where(i => i.Accepts(item) && i.Items.Count > 0)];
        if (prioritizedInventory is not null && targets.Remove(prioritizedInventory) && prioritizedInventory.Items.Count > 1) targets.Insert(0, prioritizedInventory);
        return targets;
    }


    public static bool InChain() => _chain.Count > 1;

    public static void SetupChain(InventorySlot itemSlot, Func<int, int> countToSlot) {
        _chain = [];
        _index = 0;
        _movedItems = [];

        if (itemSlot.Item.IsAir) {
            ModSubInventory hotbar = ModContent.GetInstance<Hotbar>().NewInstance(Main.LocalPlayer);
            InventorySlot from = new(hotbar, countToSlot(hotbar.Items.Count));

            // No item to chain on
            if (!QuickMoveConfig.Instance.bringItem || from.Item.IsAir) return;

            // hotbar, source, chain
            var inventories = GetChain(itemSlot.Inventory.Entity, from.Item, itemSlot.Inventory);
            inventories.Remove(hotbar);
            _chain = [from, itemSlot, .. inventories.Select(i => new InventorySlot(i, countToSlot(i.Items.Count)))];
        } else {
            // source, chain
            _chain = [itemSlot, .. GetChain(itemSlot.Inventory.Entity, itemSlot.Item, itemSlot.Inventory).Select(i => new InventorySlot(i, countToSlot(i.Items.Count)))];
        }
    }

    public static void BreakChain() {
        _chain.Clear();
    }

    public static void ContinueChain() {
        if (!InChain()) return;

        if (_index != 0) {
            if (QuickMoveConfig.Instance.followItem) _chain[_index].Unfocus();
            if (QuickMoveConfig.Instance.returnToSlot || _index != _chain.Count - 1) UndoMove(_movedItems);
        }

        _index++;
        if (_index >= _chain.Count) {
            BreakChain();
        } else {
            if (QuickMoveConfig.Instance.followItem) _chain[_index].Focus();
            _movedItems = Move(_chain[0], _chain[_index]);
        }
        Recipe.FindRecipes();
    }

    public static ReadOnlyCollection<InventorySlot> Chain() => _chain.AsReadOnly();
    public static int ChainIndex() => _index;

    private static List<InventorySlot> _chain = [];
    private static int _index = 0;
    private static List<MovedItem> _movedItems = [];


    /// <summary>
    /// Moves <paramref name="source"/> to <paramref name="target"/>, moving conflicting items if needed.
    /// If possible, <paramref name="source"/> and <paramref name="target"/> will be swapped
    /// </summary>
    /// <param name="source">An InventorySlot to move from</param>
    /// <param name="target">An InventorySlot to move <paramref name="source"/> to</param>
    /// <returns>A list containing every item moved by the function</returns>
    private static List<MovedItem> Move(InventorySlot source, InventorySlot target) {
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
    private static void UndoMove(List<MovedItem> movedItems) {
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
