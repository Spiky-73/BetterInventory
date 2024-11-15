using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using ReLogic.Graphics;
using SpikysLib.IL;
using Terraria;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.UI.Chat;

namespace BetterInventory.ItemActions;

public class BetterTooltipSystem : ModSystem {
    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers) => BetterTooltipPlayer.ModifyInterfaceLayers(layers);

}

public class BetterTooltipPlayer : ModPlayer {
    
    private static readonly LegacyGameInterfaceLayer frozenTooltipInterface = new(
        "BetterInventory: Frozen Tooltips",
        () => {
            DrawInterface_FrozenTooltips();
            return true;
        },
        InterfaceScaleType.UI
    );

    internal static void ModifyInterfaceLayers(List<GameInterfaceLayer> layers) {
        int mouseTextIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Cursor"));
        if (mouseTextIndex != -1) layers.Insert(mouseTextIndex, frozenTooltipInterface);
    }

    // TODO disable ui click when hover / frozen
    private static void DrawInterface_FrozenTooltips() {
        if (_forcedFreezeTime > 0) _forcedFreezeTime--;

        if (!Configs.ItemActions.HoverableTooltip || !Main.playerInventory || _frozenTooltips.Count <= 0) return;
        
        Reflection.Main._mouseTextCache.SetValue(Main.instance, Activator.CreateInstance(Reflection.Main.MouseTextCache));

        _drawingFrozenTooltips = true;
        List<(bool hovered, TextSnippet? snippet)> hoverInfo = [];
        foreach (var tooltip in _frozenTooltips) {
            (_hovered, _hoveredSnippet) = (false, null);
            (var hover, Main.HoverItem) = (Main.HoverItem, tooltip.HoverItem);
            Reflection.Main.MouseText_DrawItemTooltip.Invoke(Main.instance, Activator.CreateInstance(Reflection.Main.MouseTextCache), 0, tooltip.Diff, tooltip.X, tooltip.Y);
            Main.HoverItem = hover;
            hoverInfo.Add((_hovered, _hoveredSnippet));
        }
        _drawingFrozenTooltips = false;

        for (int i = _frozenTooltips.Count - 1; i >= 0 ; i--) {
            var (hovered, hoveredSnippet) = hoverInfo[i];
            if (!hovered) {
                if (_forcedFreezeTime <= 0 && !HoverTooltipKb.Current) {
                    _frozenTooltips.RemoveAt(i);
                    continue;
                }
            } else {
                _forcedFreezeTime = 30;
                if (hoveredSnippet is not null) {
                    hoveredSnippet.OnHover();
                    if (Main.mouseLeft && Main.mouseLeftRelease) hoveredSnippet.OnClick();
                }
            }
            break;
        }
    }

    private static bool _hovered;
    private static TextSnippet? _hoveredSnippet;

    public static ModKeybind HoverTooltipKb { get; private set; } = null!;

    public override void Load() {
        HoverTooltipKb = KeybindLoader.RegisterKeybind(Mod, "HoverTooltip", Microsoft.Xna.Framework.Input.Keys.None);

        MonoModHooks.Add(typeof(ItemLoader).GetMethod(nameof(ItemLoader.ModifyTooltips)), HookTooltipScroll);

        On_ItemSlot.Draw_SpriteBatch_ItemArray_int_int_Vector2_Color += HookFindSlotPosition;
        On_ChatManager.DrawColorCodedString_SpriteBatch_DynamicSpriteFont_TextSnippetArray_Vector2_Color_float_Vector2_Vector2_refInt32_float_bool += HookSnippetHover;
        IL_Main.MouseText_DrawItemTooltip += static il => {
            if (!il.ApplyTo(ILFreezeTooltip, Configs.ItemActions.HoverableTooltip)) Configs.UnloadedItemActions.Value.hoverableTooltip = true; // Should be first
            if (!il.ApplyTo(ILTooltipSize, Configs.ItemActions.HoverableTooltip)) Configs.UnloadedItemActions.Value.hoverableTooltip = true;
            if (!il.ApplyTo(ILFixTooltip, Configs.ItemActions.FixedTooltip)) Configs.UnloadedItemActions.Value.fixedTooltip = true;
        };
    }

    private Vector2 HookSnippetHover(On_ChatManager.orig_DrawColorCodedString_SpriteBatch_DynamicSpriteFont_TextSnippetArray_Vector2_Color_float_Vector2_Vector2_refInt32_float_bool orig, SpriteBatch spriteBatch, DynamicSpriteFont font, TextSnippet[] snippets, Vector2 position, Color baseColor, float rotation, Vector2 origin, Vector2 baseScale, out int hoveredSnippet, float maxWidth, bool ignoreColors) {
        var res = orig(spriteBatch, font, snippets, position, baseColor, rotation, origin, baseScale, out hoveredSnippet, maxWidth, ignoreColors);
        if (_drawingFrozenTooltips && hoveredSnippet >= 0) _hoveredSnippet = snippets[hoveredSnippet];
        return res;
    }

    private static void ILFreezeTooltip(ILContext il) {
        ILCursor cursor = new(il);

        cursor.EmitLdarg(4).EmitLdarg(5).EmitLdarg3();
        cursor.EmitDelegate((int x, int y, byte diff) => {
            if (!_drawingFrozenTooltips && Configs.ItemActions.HoverableTooltip && HoverTooltipKb.JustPressed) {
                if (_forcedFreezeTime <= 0) _frozenTooltips.Clear();
                _frozenTooltips.Add(new(x, y, Main.HoverItem.Clone(), diff));
                _forcedFreezeTime = 30; // TODO config
            }
        });
    }

    private static void ILTooltipSize(ILContext il) {
        ILCursor cursor = new(il);
        cursor.FindNextLoc(out _, out int opaqueBoxBehindTooltips, i => i.Previous.MatchLdsfld(Reflection.Main.SettingsEnabled_OpaqueBoxBehindTooltips), 1);

        cursor.GotoNext(i => i.MatchCall(Reflection.ItemLoader.ModifyTooltips));
        cursor.FindPrevLoc(out _, out int zero, i => i.Previous.MatchCall(Reflection.Vector2.Zero.GetMethod!), 17);
        cursor.GotoNext(i => i.MatchCall(Reflection.ItemLoader.PreDrawTooltip));
        cursor.GotoPrev(MoveType.AfterLabel, i => i.MatchLdloc(opaqueBoxBehindTooltips));

        cursor.EmitLdarg(4).EmitLdarg(5).EmitLdloc(zero);
        cursor.EmitDelegate((int x, int y, Vector2 zero) => {
            if (!_drawingFrozenTooltips) return;
            if (Main.SettingsEnabled_OpaqueBoxBehindTooltips) {
                zero += new Vector2(2 * 14, 9 * 3 / 2);
                x -= 14;
                y -= 9;
            }
            Rectangle hitbox = new(x, y, (int)zero.X, (int)zero.Y);
            if (hitbox.Contains(Main.mouseX, Main.mouseY)) _hovered = true;
        });
    }

    private static int _forcedFreezeTime = 0;
    private static bool _drawingFrozenTooltips = false;
    private static readonly List<FrozenTooltip> _frozenTooltips = [];

    private static List<TooltipLine> HookTooltipScroll(Reflection.ItemLoader.ModifyTooltipsFn orig, Item item, ref int numTooltips, string[] names, ref string[] text, ref bool[] modifier, ref bool[] badModifier, ref int oneDropLogo, out Color?[] overrideColor, int prefixlineIndex) {
        var tooltipsAll = orig.Invoke(item, ref numTooltips, names, ref text, ref modifier, ref badModifier, ref oneDropLogo, out overrideColor, prefixlineIndex);
        if (!Configs.TooltipScroll.Enabled) return tooltipsAll;
        if (_tooltipType != item.type) _tooltipType = item.type;

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

        int start = tooltipStart.GetValueOrDefault(_tooltipType, 1);
        for (int i = 0; i < count; i++) {
            int iAll = start + i;
            tooltips.Add(tooltipsAll[iAll]);
            text[i + 1] = textAll[iAll];
            modifier[i + 1] = modifierAll[iAll];
            badModifier[i + 1] = badModifierAll[iAll];
            if (oneDropLogoAll == iAll) oneDropLogo = i + 1;
            overrideColor[i + 1] = overrideColorAll[iAll];
        }

        TooltipLine scrollLine = new(BetterInventory.Instance, "scrollTooltip", Language.GetTextValue($"{Localization.Keys.UI}.ScrollTooltip", start, start + count - 1, numTooltipsAll - 1)) { OverrideColor = Colors.RarityTrash };
        tooltips.Add(scrollLine);
        text[count + 1] = scrollLine.Text;
        overrideColor[count + 1] = scrollLine.OverrideColor;

        return tooltips;
    }

    private static void HandleTooltipScroll(int numTooltips, int visibleLines) {
        PlayerInput.LockVanillaMouseScroll("BetterInventory: Scrollable Tooltip");
        int scroll = PlayerInput.ScrollWheelDelta / 120;
        if (scroll == 0) return;
        int s = tooltipStart.GetValueOrDefault(_tooltipType, 1);
        tooltipStart[_tooltipType] = Math.Clamp(s - scroll, 1, numTooltips - visibleLines);
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

        cursor.EmitDelegate(() => !_drawingFrozenTooltips && Configs.ItemActions.FixedTooltip && _slotPosition.HasValue);
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
    public static Dictionary<int, int> tooltipStart = [];

    private static Vector2? _slotPosition = null;
    private static float _scale;
}

public readonly record struct FrozenTooltip(int X, int Y, Item HoverItem, byte Diff);