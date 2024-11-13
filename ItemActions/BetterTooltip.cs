using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using Terraria;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace BetterInventory.ItemActions;

public class BetterTooltipPlayer: ModPlayer {
    public override void Load() {
        MonoModHooks.Add(typeof(ItemLoader).GetMethod(nameof(ItemLoader.ModifyTooltips)), HookHideTooltip);
        On_ItemSlot.Draw_SpriteBatch_ItemArray_int_int_Vector2_Color += HookFindSlotPosition;
        IL_Main.MouseText_DrawItemTooltip += static il => {
            if (!il.ApplyTo(ILFixTooltip, Configs.ItemActions.FixedTooltip)) Configs.UnloadedItemActions.Value.fixedTooltip = true;
        };
    }

    private static List<TooltipLine> HookHideTooltip(Reflection.ItemLoader.ModifyTooltipsFn orig, Item item, ref int numTooltips, string[] names, ref string[] text, ref bool[] modifier, ref bool[] badModifier, ref int oneDropLogo, out Color?[] overrideColor, int prefixlineIndex) {
        var tooltipsAll = orig.Invoke(item, ref numTooltips, names, ref text, ref modifier, ref badModifier, ref oneDropLogo, out overrideColor, prefixlineIndex);
        if (!Configs.TooltipScroll.Enabled) return tooltipsAll;
        if (_tooltipType != item.type) {
            _tooltipType = item.type;
            tooltipStart = 1;
        }

        int inset = Main.SettingsEnabled_OpaqueBoxBehindTooltips ? 18 : 4;
        int count = Math.Max(1, (int)((Main.screenHeight - inset) * Configs.TooltipScroll.Value.maximumHeight / FontAssets.MouseText.Value.LineSpacing) - 2);
        if (numTooltips <= count + 1) return tooltipsAll;
        HandleTooltipScroll(numTooltips, count);

        (T[] text, T[]) NewArray<T>(T[] array) {
            T[] a = new T[count + 2];
            a[0] = array[0];
            return (array, a);
        }
        (var numTooltipsAll, numTooltips) = (numTooltips, count + 2);
        List<TooltipLine> tooltips = new(count + 2) { tooltipsAll[0] };
        (var textAll, text) = NewArray(text);
        (var modifierAll, modifier) = NewArray(modifier);
        (var badModifierAll, badModifier) = NewArray(badModifier);
        (var oneDropLogoAll, oneDropLogo) = (oneDropLogo, -1);
        (var overrideColorAll, overrideColor) = NewArray(overrideColor);

        for (int i = 0; i < count; i++) {
            int iAll = tooltipStart + i;
            tooltips.Add(tooltipsAll[iAll]);
            text[i + 1] = textAll[iAll];
            modifier[i + 1] = modifierAll[iAll];
            badModifier[i + 1] = badModifierAll[iAll];
            if (oneDropLogoAll == iAll) oneDropLogo = i + 1;
            overrideColor[i + 1] = overrideColorAll[iAll];
        }

        TooltipLine scrollLine = new(BetterInventory.Instance, "scrollTooltip", Language.GetTextValue($"{Localization.Keys.UI}.ScrollTooltip", tooltipStart, tooltipStart + count - 1, numTooltipsAll - 1)) { OverrideColor = Colors.RarityTrash };
        tooltips.Add(scrollLine);
        text[count + 1] = scrollLine.Text;
        overrideColor[count + 1] = scrollLine.OverrideColor;

        return tooltips;
    }

    private static void HandleTooltipScroll(int numTooltips, int visibleLines) {
        PlayerInput.LockVanillaMouseScroll("BetterInventory: Scrollable Tooltip");
        int scroll = PlayerInput.ScrollWheelDelta / 120;
        if (scroll == 0) return;
        int s = tooltipStart;
        tooltipStart -= scroll;
        tooltipStart = Math.Clamp(tooltipStart, 1, numTooltips - visibleLines);
    }

    private static void HookFindSlotPosition(On_ItemSlot.orig_Draw_SpriteBatch_ItemArray_int_int_Vector2_Color orig, SpriteBatch spriteBatch, Item[] inv, int context, int slot, Vector2 position, Color lightColor) {
        if (Configs.ItemActions.FixedTooltip) {
            Rectangle rect = new((int)position.X, (int)position.Y, (int)(TextureAssets.InventoryBack.Width() * Main.inventoryScale), (int)(TextureAssets.InventoryBack.Height() * Main.inventoryScale));
            if (rect.Contains(Main.mouseX, Main.mouseY)) {
                _slotPosition = position;
                _scale = Main.inventoryScale;
            }
        }
        orig(spriteBatch, inv, context, slot, position, lightColor);
    }


    // Does not work because Main.MouseTextCache is private
    // private static void HookFixTooltip(On_Main.orig_MouseText_DrawItemTooltip orig, Main self, ValueType info, int rare, byte diff, int X, int Y) {
    //     if (Configs.ItemActions.FixedTooltip && _slotPosition.HasValue) {
    //         (X, Y) = ((int)_slotPosition.Value.X, (int)_slotPosition.Value.Y);
    //         _slotPosition = null;
    //     }
    //     orig(self, info, rare, diff, X, Y);
    // }
    private static void ILFixTooltip(ILContext il) {
        ILCursor cursor = new(il);

        cursor.EmitDelegate(() => Configs.ItemActions.FixedTooltip && _slotPosition.HasValue);
        ILLabel notFixed = cursor.DefineLabel();
        cursor.EmitBrfalse(notFixed);
        cursor.EmitDelegate(() => (int)(_slotPosition!.Value.X + TextureAssets.InventoryBack.Width()*_scale*1.1f));
        cursor.EmitStarg(4);
        cursor.EmitDelegate(() => {
            int y = (int)_slotPosition!.Value.Y;
            _slotPosition = null;
            return y;
        });
    cursor.EmitStarg(5);
        cursor.MarkLabel(notFixed);
    }

    private static int _tooltipType;
    public static int tooltipStart;

    private static Vector2? _slotPosition = null;
    private static float _scale;
}
