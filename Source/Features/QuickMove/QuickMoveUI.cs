using System.Collections.Generic;
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

public sealed class QuickMoveItem : GlobalItem {

    public override void Load() {
        IL_ItemSlot.Draw_SpriteBatch_ItemArray_int_int_Vector2_Color += static il => {
            if (!il.ApplyTo(ILDisplayHotkey, Configs.QuickMove.DisplayHotkeys)) Configs.UnloadedInventoryManagement.Value.quickMoveHotkeys = true;
        };
        On_Main.DrawInventory += HookDrawInventory;
    }

    public static void HoverSlot(Item[] inventory, int context, int slot) {
        if (!(Configs.QuickMove.DisplayHotkeys || Configs.QuickMove.Value.tooltip) || inventory[slot].IsAir) return;
        if (!InventoryLoader.IsInventorySlot(Main.LocalPlayer, inventory, context, slot, out var itemSlot)) return;
        if (itemSlot == _hoverSlot && inventory[slot].type == _hoverType) return;
        _hoverSlot = itemSlot;
        _hoverType = inventory[slot].type;
        _hovering = true;

        if (Configs.QuickMove.DisplayHotkeys) {
            _hoverChainSlots = QuickMoveUtils.GetChain(Main.LocalPlayer, itemSlot.Item, itemSlot.Inventory).SelectMany((inv, index) =>
                QuickMoveUtils.MoveKeyNames.Select((_, key) =>
                    (new InventorySlot(inv, QuickMoveUtils.HotkeyToSlotRaw(key, inv.Items.Count)), (index + 1, key))
                )
            ).ToDictionary();
        }
        if (Configs.QuickMove.Value.tooltip) {
            _hoverChain = [itemSlot.Inventory, .. QuickMoveUtils.GetChain(Main.LocalPlayer, itemSlot.Item, itemSlot.Inventory)];
        }
    }
    public override void ModifyTooltips(Item item, List<TooltipLine> tooltips) {
        if (!Configs.QuickMove.Value.tooltip || !_hovering || _hoverChain.Count == 0) return;

        tooltips.Add(new(
            BetterInventory.Instance, "QuickMove",
            Language.GetTextValue($"{Localization.Keys.UI}.QuickMoveTooltip") + ": " + string.Join(" > ", _hoverChain.Select(inv => inv.DisplayName))
        ));
    }

    private static void ILDisplayHotkey(ILContext il) {
        ILCursor cursor = new(il);

        cursor.GotoNextLoc(out int scale, i => i.Previous.MatchLdsfld(() => Main.inventoryScale), 2);

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
        cursor.GotoNext(MoveType.AfterLabel, i => i.MatchRet());

        // ++ <drawSlotNumbers>
        cursor.EmitLdarg0().EmitLdarg1().EmitLdarg2().EmitLdarg3().EmitLdarg(4).EmitLdloc(scale);
        cursor.EmitDelegate((SpriteBatch spriteBatch, Item[] inv, int context, int slot, Vector2 position, float scale) => {
            if (!Configs.QuickMove.DisplayHotkeys || !DisplayHotkeys()) return;
            if (!InventoryLoader.IsInventorySlot(Main.LocalPlayer, inv, context, slot, out var itemSlot)) return;

            int presses;
            int moveKey;

            if (QuickMoveChain.InChain()) {
                int index = QuickMoveChain.Chain().IndexOf(itemSlot);
                if (index == -1) return;
                presses = (index - QuickMoveChain.ChainIndex() + QuickMoveChain.Chain().Count) % QuickMoveChain.Chain().Count;
                moveKey = QuickMovePlayer.LocalPlayer.ChainKey();
            } else {
                if (!_hoverChainSlots.TryGetValue(itemSlot, out var display)) return;
                presses = display.presses;
                moveKey = display.moveKey;

            }

            if (presses == 0 || Configs.QuickMove.Value.displayedHotkeys.Key == Configs.HotkeyDisplayMode.Next && presses != 1) return;
            var key = (moveKey + 1) % QuickMoveUtils.MoveKeyNames.Length;
            string text = presses switch {
                1 => $"{key}",
                2 => $"{key}{key}",
                3 => $"{key}{key}{key}",
                _ => $"{key}x{presses}",
            };
            ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.ItemStack.Value, text.ToString(), position + new Vector2(6f, 4) * scale, Main.inventoryBack, 0f, Vector2.Zero, new Vector2(scale), -1f, scale);
        });

        cursor.GotoPrev(i => i.SaferMatchCall(typeof(ChatManager), nameof(ChatManager.DrawColorCodedStringWithShadow)));
        cursor.GotoPrev(i => i.MatchLdarg2());
        cursor.GotoNext(MoveType.After, i => i.MatchLdarg3());

        cursor.EmitDelegate((int slot) => !Configs.QuickMove.DisplayHotkeys || !DisplayHotkeys() ? slot : InventorySlots.Hotbar.End);
    }

    private static bool DisplayHotkeys() => _hovering || QuickMoveChain.InChain();

    private void HookDrawInventory(On_Main.orig_DrawInventory orig, Main self) {
        orig(self);
        _hovering = !Main.HoverItem.IsAir;
    }

    private static InventorySlot _hoverSlot;
    private static int _hoverType;
    private static List<ModSubInventory> _hoverChain = [];
    private static Dictionary<InventorySlot, (int presses, int moveKey)> _hoverChainSlots = [];
    private static bool _hovering; // As Hover slot may be call BEFORE an item is hovered, we need to use the value of the last frame to ensure stability
}
