using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace BetterInventory.Improvements.BetterTooltip;

public sealed class TooltipHoverSystem : ModSystem {

    public override bool IsLoadingEnabled(Mod mod) => Compatibility.LoadDisabledFeatures || BetterTooltipConfig.TooltipHover;
    public override void Load() {
        HoverTooltipKb = KeybindLoader.RegisterKeybind(Mod, "HoverTooltip", Microsoft.Xna.Framework.Input.Keys.N);

        On_ChatManager.DrawColorCodedString_SpriteBatch_DynamicSpriteFont_TextSnippetArray_Vector2_Color_float_Vector2_Vector2_refInt32_float_bool += HookSnippetHover;
        On_Main.DrawPendingMouseText += HookFreezeTooltip;
        IL_Main.MouseText_DrawItemTooltip += il => il.TryEdit(ILTooltipHover, ref UnloadedBetterTooltipConfig.Instance.tooltipHover);
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers) {
        if (!BetterTooltipConfig.TooltipHover) return;
        int mouseTextIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Cursor"));
        if (mouseTextIndex != -1) layers.Insert(mouseTextIndex, FrozenTooltipInterface);
    }

    private static void Interface_FrozenTooltips() {
        if (!BetterTooltipConfig.TooltipHover || !Main.playerInventory) return;
        int lastFrozen = FrozenTooltips.DrawFrozenTooltips();

        if (_graceTime > 0) _graceTime--;
        if (lastFrozen == FrozenTooltips.GetFrozenTooltips().Count - 1) _graceTime = TooltipHoverConfig.Value.graceTime;
        else if (_graceTime <= 0 && !HoverTooltipKb.Current) FrozenTooltips.UnfreezeTooltips(lastFrozen);
    }

    private static void HookFreezeTooltip(On_Main.orig_DrawPendingMouseText orig) {
        if (!BetterTooltipConfig.TooltipHover || !Main.playerInventory || !Main.instance._mouseTextCache.isValid) {
            orig();
            return;
        }
        if (HoverTooltipKb.JustPressed) {
            if (_graceTime <= 0) FrozenTooltips.UnfreezeTooltips();
            FrozenTooltips.FreezeTooltip();
            _graceTime = TooltipHoverConfig.Value.graceTime;
        }
        orig();
    }

    private static Vector2 HookSnippetHover(On_ChatManager.orig_DrawColorCodedString_SpriteBatch_DynamicSpriteFont_TextSnippetArray_Vector2_Color_float_Vector2_Vector2_refInt32_float_bool orig, SpriteBatch spriteBatch, DynamicSpriteFont font, TextSnippet[] snippets, Vector2 position, Color baseColor, float rotation, Vector2 origin, Vector2 baseScale, out int hoveredSnippet, float maxWidth, bool ignoreColors) {
        var res = orig(spriteBatch, font, snippets, position, baseColor, rotation, origin, baseScale, out hoveredSnippet, maxWidth, ignoreColors);
        if (hoveredSnippet != -1 && BetterTooltipConfig.TooltipHover) FrozenTooltips.HoveredSnippet(snippets[hoveredSnippet]);
        return res;
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
            if (!BetterTooltipConfig.TooltipHover) return;
            if (Main.SettingsEnabled_OpaqueBoxBehindTooltips) {
                zero += new Vector2(2 * 14, 9 * 3 / 2);
                x -= 14;
                y -= 9;
            }
            Rectangle hitbox = new(x, y, (int)zero.X, (int)zero.Y);
            if (hitbox.Contains(Main.mouseX, Main.mouseY)) FrozenTooltips.HoveredTooltip();
        });
    }


    public static readonly LegacyGameInterfaceLayer FrozenTooltipInterface = new("BetterInventory: Frozen Tooltips", () => { Interface_FrozenTooltips(); return true; }, InterfaceScaleType.UI);
    public static ModKeybind HoverTooltipKb = null!;
    private static int _graceTime;
}

public static class FrozenTooltips {

    public static void FreezeTooltip() {
        if (!Main.instance._mouseTextCache.isValid || Main.HoverItem.IsAir) return;
        var info = Main.instance._mouseTextCache;
        if (info.X == -1 || info.Y == -1) {
            info.X = Main.mouseX + 14 - 10;
            info.Y = Main.mouseY + 14 - 10;
        }
        if (Main.ThickMouse) {
            info.X += 6;
            info.Y += 6;
        }
        _frozenTooltips.Add(new(info, Main.HoverItem.Clone()));
    }
    public static void UnfreezeTooltips() => _frozenTooltips.Clear();
    public static void UnfreezeTooltips(int lastFrozenTooltip) {
        if (lastFrozenTooltip >= _frozenTooltips.Count - 1) return;
        _frozenTooltips.RemoveRange(lastFrozenTooltip + 1, _frozenTooltips.Count - (lastFrozenTooltip + 1));
    }
    public static ReadOnlyCollection<FrozenTooltip> GetFrozenTooltips() => _frozenTooltips.AsReadOnly();


    // BUG interface hover under tooltip
    public static int DrawFrozenTooltips() {
        if (_frozenTooltips.Count <= 0) return -1;
        int lastHovered = -1;

        for (int i = 0; i < _frozenTooltips.Count; i++) {
            (Main.MouseTextCache info, Item hoverItem) = _frozenTooltips[i];
            if (!Main.mouseItem.IsAir) info.X -= 34;
            if (Main.ThickMouse) {
                info.X -= 6;
                info.Y -= 6;
            }

            (_hovered, _hoveredSnippet) = (false, null);
            (var hover, Main.HoverItem) = (Main.HoverItem, hoverItem);
            Main.instance.MouseTextInner(info);
            Main.HoverItem = hover;

            // Do not draw the hovered item tooltip twice if it has been frozen
            if (hoverItem.UniqueId() == hover.UniqueId()) Main.instance._mouseTextCache.isValid = false;

            if (!_hovered) continue;
            lastHovered = i;
            Main.instance._mouseTextCache.isValid = false;
            if (_hoveredSnippet is not null) {
                _hoveredSnippet.OnHover();
                if (Main.mouseLeft && Main.mouseLeftRelease) _hoveredSnippet.OnClick();
            }
        }
        return lastHovered;
    }
    public static void HoveredTooltip() => _hovered = true;
    public static void HoveredSnippet(TextSnippet snippet) => _hoveredSnippet = snippet;

    private static bool _hovered;
    private static TextSnippet? _hoveredSnippet;
    private static readonly List<FrozenTooltip> _frozenTooltips = [];
}

public readonly record struct FrozenTooltip(Main.MouseTextCache Info, Item HoverItem);