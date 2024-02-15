using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.UI.Chat;
using Terraria.UI.Gamepad;

namespace BetterInventory.InventoryManagement;

public sealed class QuickMove : ILoadable {

    public void Load(Mod mod) {
        On_Main.DrawInterface_36_Cursor += HookAlternateChain;
        IL_ItemSlot.Draw_SpriteBatch_ItemArray_int_int_Vector2_Color += ILHighlightSlot;
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
            Language.GetTextValue("Mods.BetterInventory.UI.QuickMoveTooltip") + ": " + string.Join(" > ", from slots in _displayedChain where slots is not null select slots.DisplayName)
        ));
    }

    public static void RecordSelectedSlot() {
        if (_moveTime == 0) _selectedItem[0] = Main.LocalPlayer.selectedItem;

        (_lastHover, _hover) = (_hover, false);
    }

    public static void HoverItem(Item[] inventory, int context, int slot) {
        if (!Enabled) return;
        _hover = true;
        if (!IsInventorySlot(inventory, context, slot, out Slot itemSlot)) {
            ClearMoveChain();
            _moveTime = 0;
        } else {
            UpdateDisplayedMoveChain(itemSlot.Inventory, inventory[slot]);
            UpdateChain(itemSlot);
        }
    }

    private static void HookAlternateChain(On_Main.orig_DrawInterface_36_Cursor orig) {
        if (_moveTime > 0 && !_hover) {
            if (!Main.playerInventory) _moveTime = 0;
            else {
                Main.LocalPlayer.selectedItem = _selectedItem[1];
                if (PlayerInput.Triggers.JustPressed.KeyStatus[MoveKeys[_moveKey]]) ContinueChain();
            }
        }
        orig();
    }

    public static void UpdateChain(Slot source) {
        if (_moveTime > 0) {
            _moveTime--;
            if (_moveTime == Config.chainTime - 1) _validSlots[source.Inventory] = source.Index;
            else if (!_validSlots.TryGetValue(source.Inventory, out int index) || index != source.Index) _moveTime = 0;
            Main.LocalPlayer.selectedItem = _selectedItem[1];
        }

        int targetKey = Array.FindIndex(MoveKeys, key => PlayerInput.Triggers.JustPressed.KeyStatus[key]);
        if (targetKey == -1) return;

        if (_moveTime == 0 || _moveKey != targetKey) SetupChain(source, targetKey);
        if(_moveChain.Count != 0) ContinueChain();
    }
    private static void SetupChain(Slot source, int targetKey) {
        _moveSource = source;
        _moveKey = targetKey;
        _moveIndex = -1;
        _moveChain = new(_displayedChain);
        _validSlots.Clear();
        _validSlots[source.Inventory] = source.Index;
        _movedItems.Clear();
    }
    private static void ContinueChain() {
        Player player = Main.LocalPlayer;
        UndoMove(player, _movedItems);
        
        _moveIndex = (_moveIndex + 1) % (_moveChain.Count + (Config.returnToSlot ? 1 : 0));
        player.selectedItem = _selectedItem[0];

        if (_moveIndex < _moveChain.Count) {
            int targetSlotCount = _moveChain[_moveIndex].Items(player).Count;
            int targetSlot = HotkeyToSlot(_moveKey, targetSlotCount);
            Slot target = new(_moveChain[_moveIndex], targetSlot);
            _movedItems = Move(player, _moveSource, target);
            _moveChain[_moveIndex].Focus(player, target.Index);
            _moveTime = Config.chainTime;
        } else {
            _moveSource.Inventory.Focus(player, _moveSource.Index);
            _moveTime = 0;
        }
        _selectedItem[1] = player.selectedItem;
        SoundEngine.PlaySound(SoundID.Grab);
        Recipe.FindRecipes();
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

    private static List<MovedItem> Move(Player player, Slot source, Slot target) {
        Item item = source.Item(player);
        if (!target.Inventory.FitsSlot(player, item, target.Index, out var itemsToMove)) return new();
        bool[] canFavoriteAt = Reflection.ItemSlot.canFavoriteAt.GetValue();

        IList<Item> items = target.Inventory.Items(player);
        List<Item> freeItems = new();
        List<MovedItem> movedItems = new() { new(source, target.Inventory, item.type, item.prefix, item.favorited) };
        
        void FreeTargetItem(Slot slot) {
            Item item = slot.Item(player);
            freeItems.Add(item);
            movedItems.Add(new(slot, source.Inventory, item.type, item.prefix, item.favorited));
            slot.Inventory.Items(player)[slot.Index] = new();
            target.Inventory.OnSlotChange(player, slot.Index);
        }

        FreeTargetItem(target);
        foreach (Slot slot in itemsToMove) FreeTargetItem(slot);

        bool canFavorite = canFavoriteAt[Math.Abs(target.Inventory.Context)];
        items[target.Index] = Utility.MoveInto(items[target.Index], item, out int moved, target.Inventory.MaxStack, canFavorite);
        items[target.Index] = Utility.MoveInto(items[target.Index], freeItems[0], out _, target.Inventory.MaxStack, canFavorite);
        if (moved != 0) {
            source.Inventory.OnSlotChange(player, source.Index);
            target.Inventory.OnSlotChange(player, target.Index);
        }

        if(!freeItems[0].IsAir) freeItems[0] = source.GetItem(player, freeItems[0], GetItemSettings.GetItemInDropItemCheck);

        for (int i = 0; i < freeItems.Count; i++) {
            Item free = freeItems[i];
            if (free.IsAir) continue;
            free = source.GetItem(player, free, GetItemSettings.GetItemInDropItemCheck);
            player.GetDropItem(ref free);
        }

        return movedItems;
    }
    private static void UndoMove(Player player, List<MovedItem> movedItems) {
        foreach (MovedItem moved in movedItems) {
            bool Predicate(Item i) => i.type == moved.Type && i.prefix == moved.Prefix;
            int index = moved.To.Items(player).FindIndex(Predicate);
            Slot? slot = index != -1 ? new(moved.To, index) : InventoryLoader.FindItem(player, Predicate);
            if (slot is null) continue;
            Item item = slot.Value.Item(player);
            bool fav = item.favorited;
            item.favorited = moved.Favorited;
            Move(player, slot.Value, moved.From);
            item.favorited = fav;
        }
        movedItems.Clear();
    }


    private static void ILHighlightSlot(ILContext il) {
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
            if (!Enabled || Config.displayHotkeys == Configs.QuickMove.HotkeyDisplayMode.Off || Config.displayHotkeys.Value.highlightIntensity == 0) return;
            if (!IsTargetableSlot(inv, context, slot, out int number, out int key) || (Config.displayHotkeys == Configs.QuickMove.HotkeyDisplayMode.First ? number != 1 : number == 0)) return;
            spriteBatch.Draw(TextureAssets.InventoryBack18.Value, position, null, Main.OurFavoriteColor * (Config.displayHotkeys.Value.highlightIntensity / number) * Main.cursorAlpha, 0f, default, scale, 0, 0f);
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
        // if (gamepadPointForSlot != -1) {
        //     UILinkPointNavigator.SetPosition(gamepadPointForSlot, position + vector * 0.75f);
        // }
        // ++ <drawSlotNumbers>
        cursor.GotoNext(i => i.MatchCall(typeof(UILinkPointNavigator), nameof(UILinkPointNavigator.SetPosition)));
        cursor.GotoNext(MoveType.AfterLabel, i => i.MatchRet());

        cursor.EmitLdarg0();
        cursor.EmitLdarg1();
        cursor.EmitLdarg2();
        cursor.EmitLdarg3();
        cursor.EmitLdarg(4);
        cursor.EmitLdloc2();
        cursor.EmitDelegate((SpriteBatch spriteBatch, Item[] inv, int context, int slot, Vector2 position, float scale) => {
            if (!Enabled || Config.displayHotkeys == Configs.QuickMove.HotkeyDisplayMode.Off) return;
            if (!IsTargetableSlot(inv, context, slot, out int number, out int key) || (Config.displayHotkeys == Configs.QuickMove.HotkeyDisplayMode.First ? number != 1 : number == 0)) return;
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
            if (!Enabled || Config.displayHotkeys == Configs.QuickMove.HotkeyDisplayMode.Off) return slot;
            if (_moveTime > 0) return 10;
            if (_lastHover && _displayedChain.Count != 0) return 10;
            return slot;
        });
    }

    public static bool IsInventorySlot(Item[] inv, int context, int slot, [MaybeNullWhen(false)] out Slot itemSlot) {
        CacheInv(inv, context, slot);
        if(!_ilCachedInv.itemSlot.HasValue){
            itemSlot = default;
            return false;
        }
        itemSlot = _ilCachedInv.itemSlot.Value;
        return true;
    }

    public static bool IsTargetableSlot(Item[] inv, int context, int slot, out int numberInChain, out int key) {
        numberInChain = -1;
        key = -1;
        if (_moveTime == 0 && (!_lastHover || _displayedChain.Count == 0)) return false;
        if (!IsInventorySlot(inv, context, slot, out Slot itemSlot)) return false;

        if (Config.returnToSlot && _moveTime > 0 && itemSlot == _moveSource) {
            int mod = _moveChain.Count + (Config.returnToSlot ? 1 : 0);
            numberInChain = (_moveChain.Count - _moveIndex + mod) % mod;
            key = _moveKey;
            return true;
        }

        CacheSlots(itemSlot.Inventory);
        if (_ilCachedSlots.number < 0) return false;
        CacheIndex(itemSlot.Index);
        if (_ilCachedIndex.key == -1 || _ilCachedIndex.index >= 10) return false;

        numberInChain = _ilCachedSlots.number;
        key = _ilCachedIndex.key;
        return true;
    }

    private static void CacheInv(Item[] inv, int context, int slot) {
        if (inv == _ilCachedInv.inv && context == _ilCachedInv.context && slot == _ilCachedInv.slot) return;
        _ilCachedInv = (inv, context, slot, InventoryLoader.GetInventorySlot(Main.LocalPlayer, inv, context, slot));
    }
    private static void CacheSlots(ModSubInventory slots) {
        if (slots == _ilCachedSlots.slots) return;
        _ilCachedSlots = (slots, -1, -1, -1);
        
        List<ModSubInventory> chain; int offset;
        if (_moveTime > 0) (chain, offset) = (_moveChain, _moveIndex);
        else if (_lastHover && _displayedChain.Count != 0) (chain, offset) = (_displayedChain, -1);
        else return;

        int number = chain.IndexOf(_ilCachedSlots.slots);
        if (number < 0) return;
        _ilCachedSlots.number = number - offset;
        if (_ilCachedSlots.number < 0) _ilCachedSlots.number += chain.Count + (Config.returnToSlot ? 1 : 0);
        _ilCachedSlots.count = _ilCachedSlots.slots.Items(Main.LocalPlayer).Count;
        _ilCachedSlots.moveSlot = HotkeyToSlot(_moveKey, _ilCachedSlots.count);
    }
    private static void CacheIndex(int index) {
        if (_ilCachedIndex.index == index) return;
        _ilCachedIndex = (index, -1);
        int key = SlotToHotkey(index, _ilCachedSlots.count);
        if (_moveTime > 0 && key != _moveKey) {
            if(_ilCachedSlots.moveSlot == index) _ilCachedIndex.key = _moveKey;
            return;
        }
        _ilCachedIndex.key = key;
    }
    private static (Item[] inv, int context, int slot, Slot? itemSlot) _ilCachedInv;
    private static (ModSubInventory slots, int number, int count, int moveSlot) _ilCachedSlots;
    private static (int index, int key) _ilCachedIndex;

    public static void UpdateDisplayedMoveChain(ModSubInventory slots, Item item) {
        if (item.IsAir) ClearMoveChain();
        else if (_displayedItem != item.type) {
            _displayedChain = GetChain(Main.LocalPlayer, item, slots);
            _displayedItem = item.type;
        }
    }
    public static void ClearMoveChain() {
        _displayedChain.Clear();
        _displayedItem = ItemID.None;
    }

    private static List<ModSubInventory> GetChain(Player player, Item item, ModSubInventory source) {
        List<ModSubInventory> targetSlots = new(InventoryLoader.GetSubInventories(item, SubInventoryType.NonClassic));
        if (targetSlots.Remove(source) && source.Items(player).Count > 1) targetSlots.Insert(0, source);
        return targetSlots;
    }

    private static int _displayedItem = ItemID.None;
    private static List<ModSubInventory> _displayedChain = new();
    
    private static int _moveTime;
    private static int _moveIndex = -1;
    private static List<ModSubInventory> _moveChain = new();

    private static Slot _moveSource;
    private static int _moveKey;
    private static readonly Dictionary<ModSubInventory, int> _validSlots = new();
    
    private static List<MovedItem> _movedItems = new();

    private static int[] _selectedItem = new int[2];
    private static bool _hover, _lastHover;
}
public readonly record struct MovedItem(Slot From, ModSubInventory To, int Type, int Prefix, bool Favorited);