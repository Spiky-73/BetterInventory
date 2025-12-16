using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using SpikysLib.Constants;
using SpikysLib.IL;
using Terraria;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.UI.Chat;
using Terraria.UI.Gamepad;

namespace BetterInventory.Features.QuickMove;

public sealed class MoveChainUIItem : GlobalItem {

    public override bool IsLoadingEnabled(Mod mod) => Compatibility.LoadDisabledFeatures || QuickMoveConfig.ItemTooltip || QuickMoveConfig.DisplayHotkeys;
    public override void Load() {
        On_ItemSlot.MouseHover_ItemArray_int_int += HookItemSlotHover;
        On_ItemSlot.Draw_SpriteBatch_ItemArray_int_int_Vector2_Color += HookItemSlotDraw;
        IL_ItemSlot.Draw_SpriteBatch_ItemArray_int_int_Vector2_Color += il => il.TryEdit(ILHideHotbarText, ref UnloadedQuickMoveConfig.Instance.displayedHotkeys);
        On_Main.DrawInventory += HookDrawInventory;
    }

    private static void HookItemSlotHover(On_ItemSlot.orig_MouseHover_ItemArray_int_int orig, Item[] inv, int context, int slot) {
        orig(inv, context, slot);

        if (!QuickMoveConfig.DisplayHotkeys && !QuickMoveConfig.ItemTooltip) return;
        if (inv[slot].IsAir || !InventoryLoader.IsInventorySlot(Main.LocalPlayer, inv, context, slot, out var itemSlot)) return;

        _hovering = true;
        DisplayedMoveChain.SetupChain(itemSlot);
    }

    private static void HookItemSlotDraw(On_ItemSlot.orig_Draw_SpriteBatch_ItemArray_int_int_Vector2_Color orig, SpriteBatch spriteBatch, Item[] inv, int context, int slot, Vector2 position, Color lightColor) {
        orig(spriteBatch, inv, context, slot, position, lightColor);
        if (!QuickMoveConfig.DisplayHotkeys || !DisplayChainHotkeys) return;
        if (!InventoryLoader.IsInventorySlot(Main.LocalPlayer, inv, context, slot, out var itemSlot)) return;

        ChainSlotDisplay display;
        if (QuickMove.InChain()) {
            if (!DisplayedMoveChain.TryGetPlayerMoveChainSlot(itemSlot, out display)) return;
        } else {
            if (!DisplayedMoveChain.TryGetMoveChainSlot(itemSlot, out display)) return;
        }

        string? text = DisplayedMoveChain.GetDisplayedHotkey(display);
        if (string.IsNullOrEmpty(text)) return;

        var scale = Main.inventoryScale;
        ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.ItemStack.Value, text.ToString(), position + new Vector2(6f, 4) * scale, Main.inventoryBack, 0f, Vector2.Zero, new Vector2(scale), -1f, scale);
    }

    private static void ILHideHotbarText(ILContext il) {
        ILCursor cursor = new(il);

        // ...
        // if(...) {
        // } else if (context == 6) {
        //     ...
        //     spriteBatch.Draw(value10, position4, null, new Color(100, 100, 100, 100), 0f, default(Vector2), inventoryScale, 0, 0f);
        // }
        // if (context == 0 && ++[!<hideKeys> &&] slot < 10) {
        //     ...
        // }
        // if (gamepadPointForSlot != -1) {
        //     UILinkPointNavigator.SetPosition(gamepadPointForSlot, position + vector * 0.75f);
        // }
        cursor.GotoNext(i => i.MatchCall(() => UILinkPointNavigator.SetPosition));
        cursor.GotoPrev(i => i.SaferMatchCall(typeof(ChatManager), nameof(ChatManager.DrawColorCodedStringWithShadow)));
        cursor.GotoPrev(i => i.MatchLdarg2());
        cursor.GotoNext(MoveType.After, i => i.MatchLdarg3());

        cursor.EmitDelegate((int slot) => {
            if (!QuickMoveConfig.DisplayHotkeys || !DisplayChainHotkeys) return slot;
            return InventorySlots.Hotbar.End;
        });
    }

    private static void HookDrawInventory(On_Main.orig_DrawInventory orig, Main self) {
        orig(self);
        if (!QuickMoveConfig.DisplayHotkeys) return;
        _hovering = !Main.HoverItem.IsAir;
    }

    public override void ModifyTooltips(Item item, List<TooltipLine> tooltips) {
        if (!QuickMoveConfig.ItemTooltip || !DisplayedMoveChain.InChain()) return;

        tooltips.Add(new(BetterInventory.Instance, "QuickMove", DisplayedMoveChain.GetTooltip(DisplayedMoveChain.Chain())));
    }

    private static bool DisplayChainHotkeys => _hovering ? DisplayedMoveChain.InChain() : QuickMove.InChain();
    private static bool _hovering; // As Hover slot may be call BEFORE an item is hovered, we need to use the value of the last frame to ensure stability
}

public static class DisplayedMoveChain {
    public static void SetupChain(InventorySlot itemSlot) {
        if (itemSlot == _itemSlot && itemSlot.Item.type == _itemType) return;
        _itemSlot = itemSlot;
        _itemType = itemSlot.Item.type;

        _chainSlots = QuickMove.GetChain(Main.LocalPlayer, itemSlot.Item, itemSlot.Inventory).SelectMany((inv, index) =>
            QuickMovePlayer.MoveKeyNames.Select((_, key) =>
                (new InventorySlot(inv, QuickMovePlayer.HotkeyToSlotRaw(key, inv.Items.Count)), new ChainSlotDisplay(key, index + 1))
            )
        ).ToDictionary();
        _chain = [itemSlot.Inventory, .. QuickMove.GetChain(Main.LocalPlayer, itemSlot.Item, itemSlot.Inventory)];
    }

    public static bool InChain() => _chain.Count > 1;
    public static ReadOnlyCollection<ModSubInventory> Chain() => _chain.AsReadOnly();
    public static ReadOnlyDictionary<InventorySlot, ChainSlotDisplay> ChainSlots() => _chainSlots.AsReadOnly();
    public static bool TryGetMoveChainSlot(InventorySlot slot, out ChainSlotDisplay display) => ChainSlots().TryGetValue(slot, out display);
    public static bool TryGetPlayerMoveChainSlot(InventorySlot slot, out ChainSlotDisplay display) {
        display = default;
        int index = QuickMove.Chain().IndexOf(slot);
        if (index == -1) return false;
        display = new(QuickMovePlayer.ChainKey(), (index - QuickMove.ChainIndex() + QuickMove.Chain().Count) % QuickMove.Chain().Count);
        return true;
    }

    public static string? GetDisplayedHotkey(ChainSlotDisplay slot) {
        if (slot.Presses == 0 || QuickMoveConfig.Instance.displayedHotkeys == HotkeyDisplayMode.Next && slot.Presses != 1) return null;
        var key = (slot.MoveKey + 1) % 10;
        return slot.Presses switch {
            1 => $"{key}",
            2 => $"{key}{key}",
            3 => $"{key}{key}{key}",
            _ => $"{key}x{slot.Presses}",
        };
    }

    public static string GetTooltip(ReadOnlyCollection<ModSubInventory> chain) {
        return Language.GetTextValue($"{Localization.Keys.UI}.QuickMoveTooltip") + ": " + string.Join(" > ", chain.Select(inv => inv.DisplayName));
    }

    private static InventorySlot _itemSlot;
    private static int _itemType;
    private static List<ModSubInventory> _chain = [];
    private static Dictionary<InventorySlot, ChainSlotDisplay> _chainSlots = [];
}

public record struct ChainSlotDisplay(int MoveKey, int Presses);