using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI.Chat;

namespace BetterInventory.Features.QuickMove;

public static class MoveChainUI {

    public static void HoverSlot(Item[] inventory, int context, int slot) {
        if (inventory[slot].IsAir || !InventoryLoader.IsInventorySlot(Main.LocalPlayer, inventory, context, slot, out var itemSlot)) return;
        if (itemSlot == _hoverSlot && inventory[slot].type == _hoverType) return;
        _hoverSlot = itemSlot;
        _hoverType = inventory[slot].type;

        _hovering = true;
        _hoverChainSlots = QuickMove.GetChain(Main.LocalPlayer, itemSlot.Item, itemSlot.Inventory).SelectMany((inv, index) =>
            MoveChainInputs.MoveKeyNames.Select((_, key) =>
                (new InventorySlot(inv, MoveChainInputs.HotkeyToSlotRaw(key, inv.Items.Count)), (index + 1, key))
            )
        ).ToDictionary();
        _hoverChain = [itemSlot.Inventory, .. QuickMove.GetChain(Main.LocalPlayer, itemSlot.Item, itemSlot.Inventory)];
    }

    public static void ModifyTooltips(List<TooltipLine> tooltips) {
        if (_hoverChain.Count == 0) return;

        tooltips.Add(new(
            BetterInventory.Instance, "QuickMove",
            Language.GetTextValue($"{Localization.Keys.UI}.QuickMoveTooltip") + ": " + string.Join(" > ", _hoverChain.Select(inv => inv.DisplayName))
        ));
    }

    public static void PostDrawItemSlot(SpriteBatch spriteBatch, Item[] inv, int context, int slot, Vector2 position) {
        if (!DisplayChainHotkeys()) return;
        if (!InventoryLoader.IsInventorySlot(Main.LocalPlayer, inv, context, slot, out var itemSlot)) return;

        int presses;
        int moveKey;

        if (QuickMove.InChain()) {
            int index = QuickMove.Chain().IndexOf(itemSlot);
            if (index == -1) return;
            presses = (index - QuickMove.ChainIndex() + QuickMove.Chain().Count) % QuickMove.Chain().Count;
            moveKey = MoveChainInputs.ChainKey();
        } else {
            if (!_hoverChainSlots.TryGetValue(itemSlot, out var display)) return;
            presses = display.presses;
            moveKey = display.moveKey;

        }

        if (presses == 0 || QuickMoveConfig.Instance.displayedHotkeys == HotkeyDisplayMode.Next && presses != 1) return;
        var key = (moveKey + 1) % MoveChainInputs.MoveKeyNames.Length;
        string text = presses switch {
            1 => $"{key}",
            2 => $"{key}{key}",
            3 => $"{key}{key}{key}",
            _ => $"{key}x{presses}",
        };

        var scale = Main.inventoryScale;
        ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.ItemStack.Value, text.ToString(), position + new Vector2(6f, 4) * scale, Main.inventoryBack, 0f, Vector2.Zero, new Vector2(scale), -1f, scale);
    }

    public static bool HideHotbarText() => DisplayChainHotkeys();

    public static void PostDrawInventory() {
        _hovering = !Main.HoverItem.IsAir;
    }

    private static bool DisplayChainHotkeys() => _hovering || QuickMove.InChain();

    private static InventorySlot _hoverSlot;
    private static int _hoverType;
    private static List<ModSubInventory> _hoverChain = [];
    private static Dictionary<InventorySlot, (int presses, int moveKey)> _hoverChainSlots = [];
    private static bool _hovering; // As Hover slot may be call BEFORE an item is hovered, we need to use the value of the last frame to ensure stability
}
