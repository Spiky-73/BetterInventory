using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.UI.Chat;
using Terraria.UI.Gamepad;

namespace BetterInventory.InventoryManagement;

public sealed class QuickMove : ILoadable {

    public void Load(Mod mod) {
        On_Main.DrawInterface_36_Cursor += HookAlternateChain;
        IL_ItemSlot.Draw_SpriteBatch_ItemArray_int_int_Vector2_Color += HookHighlightSlot;
    }
    public void Unload() {}

    public static bool Enabled => Configs.InventoryManagement.Instance.quickMove;
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

    public static void RecordSelectedSlot() {
        if (_moveTime == 0) _selectedItem[0] = Main.LocalPlayer.selectedItem;

        (_lastHover, _hover) = (_hover, false);
    }

    public static void HoverItem(Item[] inventory, int context, int slot) {
        if (!Enabled) return;
        _hover = true;
        (InventorySlots? source, int sourceSlot) = InventoryLoader.GetInventorySlot(Main.LocalPlayer, inventory, context, slot);
        UpdateDisplayedMoveChain(source, inventory[slot]);
        UpdateChain(source, sourceSlot);
    }

    private static void HookAlternateChain(On_Main.orig_DrawInterface_36_Cursor orig) {
        if (_moveTime > 0 && !_hover) {
            if (!Main.playerInventory) _moveTime = 0;
            else {
                Main.LocalPlayer.selectedItem = _selectedItem[1];
                if (PlayerInput.Triggers.JustPressed.KeyStatus[MoveKeys[_moveTargetSlot]]) ContinueChain();
            }
        }
        orig();
    }

    public static void UpdateChain(InventorySlots? inventory, int slot) {
        if (_moveTime > 0) {
            _moveTime--;
            if (inventory is null) _moveTime = 0;
            else if (_moveTime == Config.chainTime - 1) _validSlots[inventory] = slot;
            else if (!_validSlots.TryGetValue(inventory, out int index) || index != slot) _moveTime = 0;
            Main.LocalPlayer.selectedItem = _selectedItem[1];
        }

        int targetSlot = Array.FindIndex(MoveKeys, key => PlayerInput.Triggers.JustPressed.KeyStatus[key]);
        if (targetSlot == -1 || inventory is null) return;

        if (_moveTime == 0 || _moveTargetSlot != targetSlot) SetupChain(inventory, slot, targetSlot);
        if(_moveChain.Count != 0) ContinueChain();
    }
    private static void SetupChain(InventorySlots? inventory, int slot, int targetSlot) {
        _moveSourceSlot = slot;
        _moveSource = inventory!;
        _moveTargetSlot = targetSlot;
        _moveIndex = -1;
        _moveChain = new(_displayedChain);
        _validSlots.Clear();
        _validSlots[inventory!] = slot;
        _movedItems.Clear();
    }
    private static void ContinueChain() {
        Player player = Main.LocalPlayer;
        UndoMove(player, _movedItems);
        
        _moveIndex = (_moveIndex + 1) % (_moveChain.Count + (Config.returnToSlot ? 1 : 0));
        player.selectedItem = _selectedItem[0];

        if (_moveIndex < _moveChain.Count) {
            int targetSlotCount = _moveChain[_moveIndex].Items(player).Count;
            int targetSlot = HotkeyToSlot(_moveTargetSlot, targetSlotCount);
            _movedItems = Move(player, _moveSource.Items(player)[_moveSourceSlot], _moveSource, _moveSourceSlot, _moveChain[_moveIndex], targetSlot);
            _moveChain[_moveIndex].Inventory.Focus(player, _moveChain[_moveIndex], targetSlot);
        } else {
            _moveSource.Inventory.Focus(player, _moveSource, _moveSourceSlot);
        }
        _selectedItem[1] = player.selectedItem;
        SoundEngine.PlaySound(SoundID.Grab);
        _moveTime = Config.chainTime;
    }

    public static int HotkeyToSlot(int hotkey, int slotCount) => Math.Clamp(
        Config.hotkeyMode switch {
            Configs.QuickMove.HotkeyMode.FromEnd => slotCount > 10 ? hotkey : (hotkey - (MoveKeys.Length - slotCount)),
            Configs.QuickMove.HotkeyMode.Reversed => MoveKeys.Length - hotkey - 1,
            Configs.QuickMove.HotkeyMode.Default or _ => hotkey
        }, 0, slotCount - 1
    );
    public static int SlotToHotkey(int slot, int slotCount) => Config.hotkeyMode switch {
        Configs.QuickMove.HotkeyMode.FromEnd => slotCount > 10 ? slot : (slot + (MoveKeys.Length - slotCount)),
        Configs.QuickMove.HotkeyMode.Reversed => MoveKeys.Length - slot - 1,
        Configs.QuickMove.HotkeyMode.Default or _ => slot
    };

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

        if(!freeItems[0].IsAir) freeItems[0] = source.GetItem(player, freeItems[0], GetItemSettings.GetItemInDropItemCheck, sourceSlot);

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


    private void HookHighlightSlot(ILContext il) {
        ILCursor cursor = new(il);

        // ...
        // if (!flag2) {
        //     spriteBatch.Draw(value, position, null, color2, 0f, default(Vector2), inventoryScale, 0, 0f);
        cursor.GotoNext(MoveType.After, i => i.MatchCallvirt(typeof(SpriteBatch), nameof(SpriteBatch.Draw)));
        cursor.EmitLdarg0();
        cursor.EmitLdarg1();
        cursor.EmitLdarg2();
        cursor.EmitLdarg3();
        cursor.EmitLdarg(4);
        cursor.EmitLdloc2();
        cursor.EmitDelegate((SpriteBatch spriteBatch, Item[] inv, int context, int slot, Vector2 position, float scale) => {
            if (!Enabled || Config.hotkeyDisplay == Configs.QuickMove.HotkeyDisplayMode.Off || Config.hotkeyDisplay.Value.highlightIntensity == 0) return;
            if (!IsTargetableSlot(inv, context, slot, out int number, out int key) || (Config.hotkeyDisplay == Configs.QuickMove.HotkeyDisplayMode.First ? number != 1 : number == 0)) return;
            spriteBatch.Draw(TextureAssets.InventoryBack18.Value, position, null, Main.OurFavoriteColor * (Config.hotkeyDisplay.Value.highlightIntensity / number) * Main.cursorAlpha, 0f, default, scale, 0, 0f);
        });
        // }

        // ...
        // if(...) {
        // } else if (context == 6) {
        //     ...
        //     spriteBatch.Draw(value10, position4, null, new Color(100, 100, 100, 100), 0f, default(Vector2), inventoryScale, 0, 0f);
        // }
        // if (context == 0 && ++[!<hideKeys> && slot < 10]) {
        //     ...
        // }
        // ++ <drawSlotNumbers>
        // if (gamepadPointForSlot != -1) {
        //     UILinkPointNavigator.SetPosition(gamepadPointForSlot, position + vector * 0.75f);
        // }
        cursor.GotoNext(i => i.MatchCall(typeof(UILinkPointNavigator), nameof(UILinkPointNavigator.SetPosition)));
        cursor.GotoPrev(MoveType.Before, i => i.MatchLdloc(6));

        cursor.EmitLdarg0();
        cursor.EmitLdarg1();
        cursor.EmitLdarg2();
        cursor.EmitLdarg3();
        cursor.EmitLdarg(4);
        cursor.EmitLdloc2();
        cursor.EmitDelegate((SpriteBatch spriteBatch, Item[] inv, int context, int slot, Vector2 position, float scale) => {
            if (!Enabled || Config.hotkeyDisplay == Configs.QuickMove.HotkeyDisplayMode.Off) return;
            if (!IsTargetableSlot(inv, context, slot, out int number, out int key) || (Config.hotkeyDisplay == Configs.QuickMove.HotkeyDisplayMode.First ? number != 1 : number == 0)) return;
            key = (key + 1) % 10;
            StringBuilder text = new();
            text.Append(key);
            for (int i = 1; i < number; i++) {
                text.Append(key);
                scale *= 0.9f;
            }
            Color baseColor = Main.inventoryBack;
            ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.ItemStack.Value, text.ToString(), position + new Vector2(6f, 4) * scale, baseColor, 0f, Vector2.Zero, new Vector2(scale), -1f, scale);
        });

        cursor.GotoPrev(i => i.MatchCallvirt(typeof(SpriteBatch), nameof(SpriteBatch.Draw)));

        cursor.GotoNext(MoveType.After, i => i.MatchLdarg3());
        cursor.EmitDelegate((int slot) => {
            if (!Enabled || Config.hotkeyDisplay == Configs.QuickMove.HotkeyDisplayMode.Off) return slot;
            if (_moveTime > 0) return 10;
            if (_lastHover && _displayedChain.Count != 0) return 10;
            return slot;
        });
    }

    public static bool IsTargetableSlot(Item[] inv, int context, int slot, out int numberInChain, out int key) {
        numberInChain = -1;
        key = -1;
        if (_moveTime == 0 && (!_lastHover || _displayedChain.Count == 0)) return false;
        (InventorySlots? slots, int index) = InventoryLoader.GetInventorySlot(Main.LocalPlayer, inv, context, slot);
        if (slots is null) return false;

        if (Config.returnToSlot && _moveTime > 0 && slots == _moveSource && index == _moveSourceSlot) {
            int mod = _moveChain.Count + (Config.returnToSlot ? 1 : 0);
            numberInChain = (_moveChain.Count - _moveIndex + mod) % mod;
            key = _moveTargetSlot;
            return true;
        }

        CacheSlots(slots);
        if (_ilCachedSlots.number < 0) return false;
        CacheIndex(index);
        if (_ilCachedIndex.key == -1) return false;

        numberInChain = _ilCachedSlots.number;
        key = _ilCachedIndex.key;
        return true;
    }

    private static void CacheSlots(InventorySlots slots) {
        if (slots == _ilCachedSlots.slots) return;
        _ilCachedSlots = (slots, -1, -1);
        
        List<InventorySlots> chain; int offset;
        if (_moveTime > 0) (chain, offset) = (_moveChain, _moveIndex);
        else if (_lastHover && _displayedChain.Count != 0) (chain, offset) = (_displayedChain, -1);
        else return;

        int number = chain.IndexOf(_ilCachedSlots.slots);
        if (number < 0) return;
        int mod = chain.Count + (Config.returnToSlot ? 1 : 0);
        _ilCachedSlots.number = (number - offset + mod) % mod;
        _ilCachedSlots.count = _ilCachedSlots.slots.Items(Main.LocalPlayer).Count;
    }
    private static void CacheIndex(int index) {
        if (_ilCachedIndex.index == index) return;
        _ilCachedIndex = (index, -1);
        int key = SlotToHotkey(index, _ilCachedSlots.count);
        if (_moveTime > 0 && key != _moveTargetSlot) return;
        _ilCachedIndex.key = key;
    }

    private static (InventorySlots slots, int number, int count) _ilCachedSlots;
    private static (int index, int key) _ilCachedIndex;

    public static (InventorySlots? source, int slot) FindIndex(Player player, Predicate<Item> predicate){
        foreach(ModInventory inventory in InventoryLoader.Inventories){
            foreach (InventorySlots slots in inventory.Slots) {
                int slot = slots.Items(player).FindIndex(predicate);
                if (slot != -1) return (slots, slot);
            }
        }
        return (null, -1);
    }

    public static void UpdateDisplayedMoveChain(InventorySlots? slots, Item item) {
        if (slots is null || item.IsAir) {
            _displayedChain.Clear();
            _displayedItem = ItemID.None;
        } else if (_displayedItem != item.type) {
            _displayedChain = SmartishChain(Main.LocalPlayer, item, slots);
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
    private static int _moveIndex = -1;
    private static List<InventorySlots> _moveChain = new();

    private static InventorySlots _moveSource = null!;
    private static int _moveSourceSlot;
    private static int _moveTargetSlot;
    private static readonly Dictionary<InventorySlots, int> _validSlots = new();
    
    private static List<MovedItem> _movedItems = new();

    private static int[] _selectedItem = new int[2];
    private static bool _hover, _lastHover;
}
public readonly record struct MovedItem(InventorySlots From, int Slot, InventorySlots To, int Type, int Prefix, bool Favorited);