using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using SpikysLib;
using SpikysLib.Collections;
using SpikysLib.Constants;
using SpikysLib.IL;
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
        IL_ItemSlot.Draw_SpriteBatch_ItemArray_int_int_Vector2_Color += static il => {
            if (!il.ApplyTo(ILHighlightSlot, Configs.QuickMove.Highlight)) Configs.UnloadedInventoryManagement.Value.quickMoveHighlight = true;
            if (!il.ApplyTo(ILDisplayHotkey, Configs.QuickMove.DisplayHotkeys)) Configs.UnloadedInventoryManagement.Value.quickMoveHotkeys = true;
        };
    }
    public void Unload() { }

    public static readonly string[] MoveKeys = [
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
    ];

    public static void ProcessTriggers() {
        if (!Configs.QuickMove.Enabled) return;
        (s_hover, s_frameHover) = (s_frameHover, false);
        if (s_ignoreHotbar >= 0) {
            if (PlayerInput.Triggers.JustPressed.KeyStatus[MoveKeys[s_ignoreHotbar]]) s_ignoreHotbar = -1;
            else PlayerInput.Triggers.Current.KeyStatus[MoveKeys[s_ignoreHotbar]] = false;
        }
        if (s_moveTime == 0) s_oldSelectedItem = Main.LocalPlayer.selectedItem;
    }

    public static void AddMoveChainLine(Item _, List<TooltipLine> tooltips) {
        if (!Configs.QuickMove.Enabled) return;
        if (!s_frameHover || !Configs.QuickMove.Value.tooltip || s_displayedChain.Count == 0) return;
        tooltips.Add(new(
            BetterInventory.Instance, "QuickMove",
            Language.GetTextValue($"{Localization.Keys.UI}.QuickMoveTooltip") + ": " + string.Join(" > ", s_displayedChain.Where(inv => inv is not null).Select(inv => inv.DisplayName))
        ));
    }

    public static void HoverItem(Item[] inventory, int context, int slot) {
        if (!Configs.QuickMove.Enabled) return;
        s_frameHover = true;
        if (!InventoryLoader.IsInventorySlot(Main.LocalPlayer, inventory, context, slot, out InventorySlot itemSlot)) {
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

    public static void UpdateChain(InventorySlot source) {
        if (s_moveTime > 0) {
            s_moveTime--;
            if (s_moveTime == Configs.QuickMove.Value.resetTime - 1) s_validSlots.Add(source);
            else if (!s_validSlots.Contains(source)) s_moveTime = 0;
        }

        int targetKey = Array.FindIndex(MoveKeys, key => PlayerInput.Triggers.JustPressed.KeyStatus[key]);
        if (targetKey == -1) return;

        if (s_moveTime == 0 || s_moveKey != targetKey) SetupChain(source, targetKey);
        if (s_moveChain.Count != 0) ContinueChain();
    }
    private static void SetupChain(InventorySlot source, int targetKey) {
        s_moveKey = targetKey;
        s_moveIndex = 0;
        s_moveChain = [
            source,
            .. s_displayedChain.Select(i => new InventorySlot(i, HotkeyToSlot(targetKey, i.Items.Count)))
        ];
        s_movedItems = [];
        s_validSlots = [source];
    }
    private static void ContinueChain() {
        Player player = Main.LocalPlayer;
        player.selectedItem = s_oldSelectedItem;
        s_ignoreHotbar = s_moveKey;
        UndoMove(player, s_movedItems);

        s_moveIndex = (s_moveIndex + 1) % s_moveChain.Count;
        if (!Configs.QuickMove.Value.returnToSlot && s_moveIndex == 0) s_moveIndex = 1;
        if (s_moveIndex == 0) s_moveTime = 0;
        else {
            s_movedItems = Move(player, s_moveChain[0], s_moveChain[s_moveIndex]);
            s_moveTime = Configs.QuickMove.Value.resetTime;
        }

        SoundEngine.PlaySound(SoundID.Grab);
        s_moveChain[s_moveIndex].Focus();
        ClearDisplayCache();
        Recipe.FindRecipes();
    }

    public static int SlotToHotkey(int slot, int slotCount) => Configs.QuickMove.Value.hotkeyMode switch {
        Configs.HotkeyMode.FromEnd => slotCount >= MoveKeys.Length ? slot : (slot + (MoveKeys.Length - slotCount)),
        Configs.HotkeyMode.Reversed => MoveKeys.Length - slot - 1,
        Configs.HotkeyMode.Hotbar or _ => slot
    };
    public static int HotkeyToSlot(int hotkey, int slotCount) => Math.Clamp(Configs.QuickMove.Value.hotkeyMode switch {
        Configs.HotkeyMode.FromEnd => hotkey - MoveKeys.Length + slotCount,
        _ => SlotToHotkey(hotkey, slotCount)
    }, 0, slotCount - 1);

    private static List<MovedItem> Move(Player player, InventorySlot source, InventorySlot target) {
        Item item = source.Item;
        if (!target.Fits(item, out var itemsToMove)) return [];

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

        bool canFavorite = Reflection.ItemSlot.canFavoriteAt.GetValue()[Math.Abs(target.Inventory.Context)];
        bool keepFavorited = !canFavorite && item.favorited; // Only apply when the items would be unfavorited, to avoid "generating" favorited items 
        items[target.Index] = ItemHelper.MoveInto(items[target.Index], item, out _, target.Inventory.MaxStack, canFavorite);
        items[target.Index] = ItemHelper.MoveInto(items[target.Index], freeItems[0], out _, target.Inventory.MaxStack, canFavorite);
        source.OnChange();
        target.OnChange();

        for (int i = 0; i < freeItems.Count; i++) {
            Item free = freeItems[i];
            if (free.IsAir) continue;
            bool f = free.favorited;
            if (Configs.ItemActions.KeepSwappedFavorited && keepFavorited) free.favorited = true;
            free = source.GetItem(free, GetItemSettings.GetItemInDropItemCheck);
            free.favorited = f;
            player.GetDropItem(ref free);
        }

        return movedItems;
    }
    private static void UndoMove(Player player, List<MovedItem> movedItems) {
        foreach (MovedItem moved in movedItems) {
            bool Predicate(Item i) => i.type == moved.Type && i.prefix == moved.Prefix;
            int index = moved.To.Items.FindIndex(Predicate);
            InventorySlot? slot = index != -1 ? new(moved.To, index) : InventoryLoader.FindItem(player, Predicate);
            if (slot is null) continue;
            Item item = slot.Value.Item;
            bool fav = item.favorited;
            item.favorited = moved.Favorited;
            Move(player, slot.Value, moved.From);
            item.favorited = fav;
        }
        movedItems.Clear();
    }


    private static void ILHighlightSlot(ILContext il) {
        ILCursor cursor = new(il);

        cursor.GotoNextLoc(out int scale, i => i.Previous.MatchLdsfld(Reflection.Main.inventoryScale), 2);

        // ...
        // if (!flag2) {
        //     spriteBatch.Draw(value, position, null, color2, 0f, default(Vector2), inventoryScale, 0, 0f);
        cursor.GotoNext(MoveType.After, i => i.MatchCallvirt(typeof(SpriteBatch), nameof(SpriteBatch.Draw)));

        // ++ <highlightSlot>
        cursor.EmitLdarg0().EmitLdarg1().EmitLdarg2().EmitLdarg3().EmitLdarg(4).EmitLdloc(scale);
        cursor.EmitDelegate((SpriteBatch spriteBatch, Item[] inv, int context, int slot, Vector2 position, float scale) => {
            if (!Configs.QuickMove.Highlight) return;
            if (!IsTargetableSlot(inv, context, slot, out int number, out int key) || (Configs.QuickMove.Value.displayedHotkeys == Configs.HotkeyDisplayMode.Next ? number != 1 : number == 0)) return;
            spriteBatch.Draw(TextureAssets.InventoryBack18.Value, position, null, Main.OurFavoriteColor * (Configs.QuickMove.Value.displayedHotkeys.Value.highlightIntensity / number) * Main.cursorAlpha, 0f, default, scale, 0, 0f);
        });
        // }

    }
    private static void ILDisplayHotkey(ILContext il) {
        ILCursor cursor = new(il);

        cursor.GotoNextLoc(out int scale, i => i.Previous.MatchLdsfld(Reflection.Main.inventoryScale), 2);

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
        cursor.EmitLdarg0().EmitLdarg1().EmitLdarg2().EmitLdarg3().EmitLdarg(4).EmitLdloc(scale);
        cursor.EmitDelegate((SpriteBatch spriteBatch, Item[] inv, int context, int slot, Vector2 position, float scale) => {
            if (!Configs.QuickMove.DisplayHotkeys) return;
            if (!IsTargetableSlot(inv, context, slot, out int number, out int key) || (Configs.QuickMove.Value.displayedHotkeys == Configs.HotkeyDisplayMode.Next ? number != 1 : number == 0)) return;
            key = (key + 1) % MoveKeys.Length;
            string text = number switch {
                1 => $"{key}",
                2 => $"{key}{key}",
                3 => $"{key}{key}{key}",
                _ => $"{key}x{number}",
            };
            Color baseColor = Main.inventoryBack;
            ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.ItemStack.Value, text.ToString(), position + new Vector2(6f, 4) * scale, baseColor, 0f, Vector2.Zero, new Vector2(scale), -1f, scale);
        });

        cursor.GotoPrev(i => i.SaferMatchCall(typeof(ChatManager), nameof(ChatManager.DrawColorCodedStringWithShadow)));
        cursor.GotoPrev(i => i.MatchLdarg2());
        cursor.GotoNext(MoveType.After, i => i.MatchLdarg3());

        cursor.EmitDelegate((int slot) => !Configs.QuickMove.DisplayHotkeys || s_moveTime <= 0 && (!s_hover || s_displayedChain.Count == 0) ? slot : InventorySlots.Hotbar.End);
    }

    public static bool IsTargetableSlot(Item[] inv, int context, int slot, out int numberInChain, out int key) {
        (numberInChain, key) = (-1, -1);
        if (s_moveTime == 0 && (!s_hover || s_displayedChain.Count == 0)) return false;
        if (!InventoryLoader.IsInventorySlot(Main.LocalPlayer, inv, context, slot, out InventorySlot itemSlot)) return false;
        (numberInChain, key) = GetSlotMoveInfo(itemSlot);
        return 0 <= key && key < MoveKeys.Length;
    }

    public static (int number, int key) GetSlotMoveInfo(InventorySlot slot) => s_slotMoveInfo.GetOrAdd(slot, () => {
        (int numberInChain, int key) = (-1, -1);
        if (s_moveTime > 0) {
            int index = s_moveChain.IndexOf(slot);
            if (index > -1) {
                numberInChain = (index - s_moveIndex + s_moveChain.Count) % s_moveChain.Count;
                if (!Configs.QuickMove.Value.returnToSlot && index < s_moveIndex) numberInChain--;
                key = s_moveKey;
            }
        } else if (s_hover && s_displayedChain.Count > 0) {
            int index = s_displayedChain.IndexOf(slot.Inventory);
            if (index > -1) {
                numberInChain = index + 1;
                key = SlotToHotkey(slot.Index, slot.Inventory.Items.Count);
            }
        }
        return (numberInChain, key);
    });
    private static readonly Dictionary<InventorySlot, (int number, int key)> s_slotMoveInfo = [];

    public static void UpdateDisplayedMoveChain(ModSubInventory slots, Item item) {
        if (item.IsAir) ClearDisplayedChain();
        else if (s_displayedItem != item.type) {
            s_displayedChain = GetDisplayedChain(Main.LocalPlayer, item, slots);
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
        s_slotMoveInfo.Clear();
    }

    private static List<ModSubInventory> GetDisplayedChain(Player player, Item item, ModSubInventory source) {
        List<ModSubInventory> targets = InventoryLoader.GetPreferredInventories(player).Where(i => i.Accepts(item) && i.Items.Count > 0).ToList();
        if (targets.Remove(source) && source.Items.Count > 1) targets.Insert(0, source);
        return targets;
    }

    private static int s_displayedItem = ItemID.None;
    private static List<ModSubInventory> s_displayedChain = [];

    private static int s_moveKey;
    private static int s_moveTime;
    private static List<InventorySlot> s_moveChain = [];
    private static int s_moveIndex = -1;
    private static HashSet<InventorySlot> s_validSlots = [];
    private static List<MovedItem> s_movedItems = [];

    private static bool s_hover, s_frameHover;
    private static int s_ignoreHotbar = -1, s_oldSelectedItem;
}

public readonly record struct MovedItem(InventorySlot From, ModSubInventory To, int Type, int Prefix, bool Favorited);