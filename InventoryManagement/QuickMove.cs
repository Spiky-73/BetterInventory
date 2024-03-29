using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using SpikysLib.Extensions;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI.Chat;
using Terraria.UI.Gamepad;

namespace BetterInventory.InventoryManagement;

public sealed class QuickMove : ILoadable {

    public void Load(Mod mod) {
        On_Main.DrawInterface_36_Cursor += HookAlternateChain;
    }
    public void Unload() {}

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

    public static void ProcessTriggers() {
        (s_hover, s_frameHover) = (s_frameHover, false);
        if (s_ignoreHotbar >= 0) {
            if (PlayerInput.Triggers.JustPressed.KeyStatus[MoveKeys[s_ignoreHotbar]]) s_ignoreHotbar = -1;
            else PlayerInput.Triggers.Current.KeyStatus[MoveKeys[s_ignoreHotbar]] = false;
        }
        if (s_moveTime == 0) s_oldSelectedItem = Main.LocalPlayer.selectedItem;
    }

    public static void AddMoveChainLine(Item _, List<TooltipLine> tooltips){
        if (!s_frameHover || !Configs.QuickMove.Value.showTooltip || s_displayedChain.Count == 0) return;
        tooltips.Add(new(
            BetterInventory.Instance, "QuickMove",
            Language.GetTextValue($"{Localization.Keys.UI}.QuickMoveTooltip") + ": " + string.Join(" > ", from slots in s_displayedChain where slots is not null select slots.DisplayName)
        ));
    }

    public static void HoverItem(Item[] inventory, int context, int slot) {
        if (!Configs.QuickMove.Enabled) return;
        s_frameHover = true;
        if (!InventoryLoader.IsInventorySlot(Main.LocalPlayer, inventory, context, slot, out Slot itemSlot)) {
            ClearDisplayedChain();
            s_moveTime = 0;
        } else {
            UpdateDisplayedMoveChain(itemSlot.Inventory, inventory[slot]);
            UpdateChain(itemSlot);
        }
    }

    private static void HookAlternateChain(On_Main.orig_DrawInterface_36_Cursor orig) {
        if (Configs.QuickMove.Enabled && s_moveTime > 0 && !s_frameHover) {
            if (!Main.playerInventory) s_moveTime = 0;
            else {
                if (PlayerInput.Triggers.JustPressed.KeyStatus[MoveKeys[s_moveKey]]) ContinueChain();
            }
        }
        orig();
    }

    public static void UpdateChain(Slot source) {
        if (s_moveTime > 0) {
            s_moveTime--;
            if (s_moveTime == Configs.QuickMove.Value.chainTime - 1) s_validSlots[source.Inventory] = source.Index;
            else if (!s_validSlots.TryGetValue(source.Inventory, out int index) || index != source.Index) s_moveTime = 0;
        }

        int targetKey = Array.FindIndex(MoveKeys, key => PlayerInput.Triggers.JustPressed.KeyStatus[key]);
        if (targetKey == -1) return;

        if (s_moveTime == 0 || s_moveKey != targetKey) SetupChain(source, targetKey);
        if(s_moveChain.Count != 0) ContinueChain();
    }
    private static void SetupChain(Slot source, int targetKey) {
        s_moveSource = source;
        s_moveKey = targetKey;
        s_moveIndex = -1;
        s_moveChain = new(s_displayedChain);
        s_validSlots.Clear();
        s_validSlots[source.Inventory] = source.Index;
        s_movedItems.Clear();
    }
    private static void ContinueChain() {
        Player player = Main.LocalPlayer;
        ClearDisplayCache();
        UndoMove(player, s_movedItems);
        s_moveIndex = (s_moveIndex + 1) % (s_moveChain.Count + (Configs.QuickMove.Value.returnToSlot ? 1 : 0));
        player.selectedItem = s_oldSelectedItem;
        s_ignoreHotbar = s_moveKey;

        if (s_moveIndex < s_moveChain.Count) {
            int targetSlotCount = s_moveChain[s_moveIndex].Items(player).Count;
            int targetSlot = HotkeyToSlot(s_moveKey, targetSlotCount);
            Slot target = new(s_moveChain[s_moveIndex], targetSlot);
            s_movedItems = Move(player, s_moveSource, target);
            s_moveChain[s_moveIndex].Focus(player, target.Index);
            s_moveTime = Configs.QuickMove.Value.chainTime;
        } else {
            s_moveSource.Inventory.Focus(player, s_moveSource.Index);
            s_moveTime = 0;
        }
        SoundEngine.PlaySound(SoundID.Grab);
        Recipe.FindRecipes();
    }

    public static int HotkeyToSlot(int hotkey, int slotCount) => Math.Clamp(
        Configs.QuickMove.Value.hotkeyMode switch {
            Configs.HotkeyMode.FromEnd => slotCount > 10 ? hotkey : (hotkey - (MoveKeys.Length - slotCount)),
            Configs.HotkeyMode.Reversed => MoveKeys.Length - hotkey - 1,
            Configs.HotkeyMode.Default or _ => hotkey
        }, 0, slotCount - 1
    );
    public static int SlotToHotkey(int slot, int slotCount) => Configs.QuickMove.Value.hotkeyMode switch {
        Configs.HotkeyMode.FromEnd => slotCount > 10 ? slot : (slot + (MoveKeys.Length - slotCount)),
        Configs.HotkeyMode.Reversed => MoveKeys.Length - slot - 1,
        Configs.HotkeyMode.Default or _ => slot
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
        items[target.Index] = ItemExtensions.MoveInto(items[target.Index], item, out int moved, target.Inventory.MaxStack, canFavorite);
        items[target.Index] = ItemExtensions.MoveInto(items[target.Index], freeItems[0], out _, target.Inventory.MaxStack, canFavorite);
        if (moved != 0) {
            source.Inventory.OnSlotChange(player, source.Index);
            target.Inventory.OnSlotChange(player, target.Index);
        }

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


    internal static void ILHighlightSlot(ILContext il) {
        ILCursor cursor = new(il);

        // ...
        // if (!flag2) {
        //     spriteBatch.Draw(value, position, null, color2, 0f, default(Vector2), inventoryScale, 0, 0f);
        cursor.GotoNext(MoveType.After, i => i.MatchCallvirt(typeof(SpriteBatch), nameof(SpriteBatch.Draw)));

        // ++ <highlightSlot>
        cursor.EmitLdarg0();
        cursor.EmitLdarg1();
        cursor.EmitLdarg2();
        cursor.EmitLdarg3();
        cursor.EmitLdarg(4);
        cursor.EmitLdloc2();
        cursor.EmitDelegate((SpriteBatch spriteBatch, Item[] inv, int context, int slot, Vector2 position, float scale) => {
            if (!Configs.QuickMove.Highlight) return;
            if (!IsTargetableSlot(inv, context, slot, out int number, out int key) || (Configs.QuickMove.Value.displayHotkeys == Configs.HotkeyDisplayMode.First ? number != 1 : number == 0)) return;
            spriteBatch.Draw(TextureAssets.InventoryBack18.Value, position, null, Main.OurFavoriteColor * (Configs.QuickMove.Value.displayHotkeys.Value.highlightIntensity / number) * Main.cursorAlpha, 0f, default, scale, 0, 0f);
        });
        // }

    }
    internal static void ILDisplayHotkey(ILContext il){
        ILCursor cursor = new(il);

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
        cursor.GotoNext(i => i.SaferMatchCall(typeof(UILinkPointNavigator), nameof(UILinkPointNavigator.SetPosition)));
        cursor.GotoNext(MoveType.AfterLabel, i => i.MatchRet());

        // ++ <drawSlotNumbers>
        cursor.EmitLdarg0();
        cursor.EmitLdarg1();
        cursor.EmitLdarg2();
        cursor.EmitLdarg3();
        cursor.EmitLdarg(4);
        cursor.EmitLdloc2();
        cursor.EmitDelegate((SpriteBatch spriteBatch, Item[] inv, int context, int slot, Vector2 position, float scale) => {
            if (!Configs.QuickMove.DisplayHotkeys) return;
            if (!IsTargetableSlot(inv, context, slot, out int number, out int key) || (Configs.QuickMove.Value.displayHotkeys == Configs.HotkeyDisplayMode.First ? number != 1 : number == 0)) return;
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

        cursor.GotoPrev(i => i.SaferMatchCall(typeof(ChatManager), nameof(ChatManager.DrawColorCodedStringWithShadow)));
        cursor.GotoPrev(i => i.MatchLdarg2());
        cursor.GotoNext(MoveType.After, i => i.MatchLdarg3());

        cursor.EmitDelegate((int slot) => {
            if (!Configs.QuickMove.DisplayHotkeys) return slot;
            if (s_moveTime > 0) return 10;
            if (s_hover && s_displayedChain.Count != 0) return 10;
            return slot;
        });
    }

    public static bool IsTargetableSlot(Item[] inv, int context, int slot, out int numberInChain, out int key) {
        numberInChain = -1;
        key = -1;
        if (s_moveTime == 0 && (!s_hover || s_displayedChain.Count == 0)) return false;
        if (!InventoryLoader.IsInventorySlot(Main.LocalPlayer, inv, context, slot, out Slot itemSlot)) return false;
        if (s_slotMoveInfo.TryGetValue(itemSlot, out var cached)) {
            numberInChain = cached.number;
            key = cached.key;
            return numberInChain > 0 && key != -1;
        }

        if (Configs.QuickMove.Value.returnToSlot && s_moveTime > 0 && itemSlot == s_moveSource) {
            int mod = s_moveChain.Count + (Configs.QuickMove.Value.returnToSlot ? 1 : 0);
            numberInChain = (s_moveChain.Count - s_moveIndex + mod) % mod;
            key = s_moveKey;
            s_slotMoveInfo[itemSlot] = (numberInChain, key);
            return true;
        }

        (numberInChain, int count, int moveSlot) = GetMoveInfo(itemSlot.Inventory);
        if (numberInChain < 0) {
            s_slotMoveInfo[itemSlot] = (numberInChain, key);
            return false;
        }

        key = SlotToHotkey(itemSlot.Index, count);
        if (s_moveTime > 0 && key != s_moveKey) {
            if (moveSlot == itemSlot.Index){
                key = s_moveKey;
                s_slotMoveInfo[itemSlot] = (numberInChain, key);
                return true;
            }
            else {
                s_slotMoveInfo[itemSlot] = (numberInChain, -1);
                return false;
            }
        }
        if (key == -1 || itemSlot.Index >= 10) {
            s_slotMoveInfo[itemSlot] = (numberInChain, -1);
            return false;
        }
        s_slotMoveInfo[itemSlot] = (numberInChain, key);
        return true;
    }
    public static (int number, int count, int moveSlot) GetMoveInfo(ModSubInventory inv) {
        if (s_invMoveInfo.TryGetValue(inv, out var cached)) return cached;
       
        List<ModSubInventory> chain; int offset;
        if (s_moveTime > 0) (chain, offset) = (s_moveChain, s_moveIndex);
        else if (s_hover && s_displayedChain.Count != 0) (chain, offset) = (s_displayedChain, -1);
        else return s_invMoveInfo[inv] = (-1,-1,-1);

        int number = chain.IndexOf(inv);
        if (number < 0) return s_invMoveInfo[inv] = (-1, -1, -1);

        number -= offset;
        if (number < 0) number += chain.Count + (Configs.QuickMove.Value.returnToSlot ? 1 : 0);
        int count = inv.Items(Main.LocalPlayer).Count;
        return s_invMoveInfo[inv] = (number, count, HotkeyToSlot(s_moveKey, count));
    }

    private static readonly Dictionary<ModSubInventory, (int number, int count, int moveSlot)> s_invMoveInfo = new();
    private static readonly Dictionary<Slot, (int number, int key)> s_slotMoveInfo = new();

    public static void UpdateDisplayedMoveChain(ModSubInventory slots, Item item) {
        if (item.IsAir) ClearDisplayedChain();
        else if (s_displayedItem != item.type) {
            s_displayedChain = GetChain(Main.LocalPlayer, item, slots);
            s_displayedItem = item.type;
            ClearDisplayCache();
        }
    }
    public static void ClearDisplayedChain() {
        s_displayedChain.Clear();
        s_displayedItem = ItemID.None;
        ClearDisplayCache();
    }
    public static void ClearDisplayCache() {
        s_invMoveInfo.Clear();
        s_slotMoveInfo.Clear();
    }

    private static List<ModSubInventory> GetChain(Player player, Item item, ModSubInventory source) {
        List<ModSubInventory> targetSlots = new(InventoryLoader.GetSubInventories(item, SubInventoryType.NonClassic));
        if (targetSlots.Remove(source) && source.Items(player).Count > 1) targetSlots.Insert(0, source);
        return targetSlots;
    }

    private static int s_displayedItem = ItemID.None;
    private static List<ModSubInventory> s_displayedChain = new();
    
    private static Slot s_moveSource;
    private static int s_moveKey;
    private static List<ModSubInventory> s_moveChain = new();
    private static int s_moveTime;
    private static int s_moveIndex = -1;
    private static readonly Dictionary<ModSubInventory, int> s_validSlots = new();
    private static List<MovedItem> s_movedItems = new();

    private static bool s_hover, s_frameHover;
    private static int s_ignoreHotbar = -1, s_oldSelectedItem;

}
public readonly record struct MovedItem(Slot From, ModSubInventory To, int Type, int Prefix, bool Favorited);