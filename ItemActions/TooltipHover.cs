using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using ReLogic.Graphics;
using SpikysLib;
using SpikysLib.IL;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.UI.Chat;

namespace BetterInventory.ItemActions;

public class TooltipHover : ModSystem {

    public override void Load() {
        HoverTooltipKb = KeybindLoader.RegisterKeybind(Mod, "HoverTooltip", Microsoft.Xna.Framework.Input.Keys.N);

        On_ChatManager.DrawColorCodedString_SpriteBatch_DynamicSpriteFont_TextSnippetArray_Vector2_Color_float_Vector2_Vector2_refInt32_float_bool += HookSnippetHover;
        IL_Main.MouseText_DrawItemTooltip += static il => {
            if (!il.ApplyTo(ILFreezeTooltip, Configs.TooltipHover.Enabled)) Configs.UnloadedItemActions.Value.tooltipHover = true; // Should be first
            if (!il.ApplyTo(ILTooltipHover, Configs.TooltipHover.Enabled)) Configs.UnloadedItemActions.Value.tooltipHover = true;
        };
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers) {
        int mouseTextIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Cursor"));
        if (mouseTextIndex != -1) layers.Insert(mouseTextIndex, frozenTooltipInterface);
    }
    private static readonly LegacyGameInterfaceLayer frozenTooltipInterface = new(
        "BetterInventory: Frozen Tooltips",
        () => { DrawInterface_FrozenTooltips(); return true; },
        InterfaceScaleType.UI
    );

    // BUG interface hover under tooltip
    private static void DrawInterface_FrozenTooltips() {
        if (_forcedFreezeTime > 0) _forcedFreezeTime--;

        if (!Configs.TooltipHover.Enabled || !Main.playerInventory || _frozenTooltips.Count <= 0) return;

        HashSet<Guid> drawUniqueIds = [];
        int lastHovered = -1;

        DrawingFrozenTooltips = true;
        for (int i = 0; i < _frozenTooltips.Count; i++) {
            FrozenTooltip tooltip = _frozenTooltips[i];
            (_hovered, _hoveredSnippet) = (false, null);
            (var hover, Main.HoverItem) = (Main.HoverItem, tooltip.HoverItem);
            Reflection.Main.MouseText_DrawItemTooltip.Invoke(Main.instance, Activator.CreateInstance(Reflection.Main.MouseTextCache), 0, tooltip.Diff, tooltip.X, tooltip.Y);
            Main.HoverItem = hover;
            drawUniqueIds.Add(tooltip.HoverItem.UniqueId());

            if (!_hovered) continue;
            lastHovered = i;
            Reflection.Main._mouseTextCache.SetValue(Main.instance, Activator.CreateInstance(Reflection.Main.MouseTextCache));
            if (_hoveredSnippet is not null) {
                _hoveredSnippet.OnHover();
                if (Main.mouseLeft && Main.mouseLeftRelease) _hoveredSnippet.OnClick();
            }
        }
        DrawingFrozenTooltips = false;

        if (lastHovered == _frozenTooltips.Count - 1) _forcedFreezeTime = Configs.TooltipHover.Value.graceTime;
        else if (_forcedFreezeTime <= 0 && !HoverTooltipKb.Current) _frozenTooltips.RemoveRange(lastHovered + 1, _frozenTooltips.Count - lastHovered-1);

        if (drawUniqueIds.Contains(Main.HoverItem.UniqueId())) Reflection.Main._mouseTextCache.SetValue(Main.instance, Activator.CreateInstance(Reflection.Main.MouseTextCache));
    }
    private static bool _hovered;
    private static TextSnippet? _hoveredSnippet;

    private static void ILFreezeTooltip(ILContext il) {
        ILCursor cursor = new(il);

        cursor.EmitLdarg(4).EmitLdarg(5).EmitLdarg3();
        cursor.EmitDelegate((int x, int y, byte diff) => {
            if (!DrawingFrozenTooltips && Configs.TooltipHover.Enabled && HoverTooltipKb.JustPressed) {
                if (_forcedFreezeTime <= 0) _frozenTooltips.Clear();
                _frozenTooltips.Add(new(x, y, Main.HoverItem.Clone(), diff));
                _forcedFreezeTime = Configs.TooltipHover.Value.graceTime;
            }
        });
    }

    private static void ILTooltipHover(ILContext il) {
        ILCursor cursor = new(il);
        cursor.FindNextLoc(out _, out int opaqueBoxBehindTooltips, i => i.Previous.MatchLdsfld(Reflection.Main.SettingsEnabled_OpaqueBoxBehindTooltips), 0);

        cursor.GotoNext(i => i.MatchCall(Reflection.ItemLoader.ModifyTooltips));
        cursor.FindPrevLoc(out _, out int zero, i => i.Previous.MatchCall(Reflection.Vector2.Zero.GetMethod!), 17);
        cursor.GotoNext(i => i.MatchCall(Reflection.ItemLoader.PreDrawTooltip));
        cursor.GotoPrev(MoveType.AfterLabel, i => i.MatchLdloc(opaqueBoxBehindTooltips));

        cursor.EmitLdarg(4).EmitLdarg(5).EmitLdloc(zero);
        cursor.EmitDelegate((int x, int y, Vector2 zero) => {
            if (!DrawingFrozenTooltips) return;
            if (Main.SettingsEnabled_OpaqueBoxBehindTooltips) {
                zero += new Vector2(2 * 14, 9 * 3 / 2);
                x -= 14;
                y -= 9;
            }
            Rectangle hitbox = new(x, y, (int)zero.X, (int)zero.Y);
            if (hitbox.Contains(Main.mouseX, Main.mouseY)) _hovered = true;
        });
    }
    private static Vector2 HookSnippetHover(On_ChatManager.orig_DrawColorCodedString_SpriteBatch_DynamicSpriteFont_TextSnippetArray_Vector2_Color_float_Vector2_Vector2_refInt32_float_bool orig, SpriteBatch spriteBatch, DynamicSpriteFont font, TextSnippet[] snippets, Vector2 position, Color baseColor, float rotation, Vector2 origin, Vector2 baseScale, out int hoveredSnippet, float maxWidth, bool ignoreColors) {
        var res = orig(spriteBatch, font, snippets, position, baseColor, rotation, origin, baseScale, out hoveredSnippet, maxWidth, ignoreColors);
        if (DrawingFrozenTooltips && hoveredSnippet >= 0) _hoveredSnippet = snippets[hoveredSnippet];
        return res;
    }

    public static ModKeybind HoverTooltipKb { get; private set; } = null!;
    public static bool DrawingFrozenTooltips { get; private set; }

    private static int _forcedFreezeTime = 0;
    private static readonly List<FrozenTooltip> _frozenTooltips = [];
}

public readonly record struct FrozenTooltip(int X, int Y, Item HoverItem, byte Diff);