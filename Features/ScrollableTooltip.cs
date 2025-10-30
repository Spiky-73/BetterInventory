using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace BetterInventory.Features;

public class ScrollableTooltipItem : GlobalItem {

    public override void Load() {
        MonoModHooks.Add(Reflection.ItemLoader.ModifyTooltips, HookTooltipScroll);
    }

    private static List<TooltipLine> HookTooltipScroll(Reflection.ItemLoader.ModifyTooltipsFn orig, Item item, ref int numTooltips, string[] names, ref string[] text, ref bool[] modifier, ref bool[] badModifier, ref int oneDropLogo, out Color?[] overrideColor, int prefixlineIndex) {
        var tooltips = orig.Invoke(item, ref numTooltips, names, ref text, ref modifier, ref badModifier, ref oneDropLogo, out overrideColor, prefixlineIndex);
        if (!Configs.FeatureList.ScrollableTooltip) return tooltips;

        if (!ScrollableTooltip.ScrollItemTooltip(item.type, PlayerInput.ScrollWheelDelta / 120, numTooltips)) return tooltips;

        PlayerInput.LockVanillaMouseScroll("BetterInventory/ScrollableTooltip");
        return ScrollableTooltip.CropItemTooltip(item.type, tooltips, ref numTooltips, ref text, ref modifier, ref badModifier, ref oneDropLogo, ref overrideColor);
    }
}


public static class ScrollableTooltip {

    public static bool ScrollItemTooltip(int type, int delta, int numTooltips) {
        int croppedNumTooltips = GetCroppedNumTooltips();
        if (numTooltips <= croppedNumTooltips) return false;

        _scroll[type] = Math.Clamp(GetTooltipScroll(type) - delta, 0, numTooltips - (croppedNumTooltips - 1));
        return true;
    }

    public static List<TooltipLine> CropItemTooltip(int type, List<TooltipLine> tooltips, ref int numTooltips, ref string[] text, ref bool[] modifier, ref bool[] badModifier, ref int oneDropLogo, ref Color?[] overrideColor) {
        int scroll = GetTooltipScroll(type);
        int croppedNumTooltips = GetCroppedNumTooltips();

        if (numTooltips <= croppedNumTooltips) return tooltips;

        (var start, var end) = (scroll + 1, scroll + croppedNumTooltips - 1);
        TooltipLine scrollLine = new(BetterInventory.Instance, "scrollTooltip", Language.GetTextValue($"{Localization.Keys.UI}.ScrollTooltip", start, end - 1, numTooltips - 1)) { OverrideColor = Colors.RarityTrash };

        tooltips = [tooltips[0], .. tooltips[start..end], scrollLine];
        numTooltips = croppedNumTooltips;
        text = [text[0], .. text[start..end], scrollLine.Text];
        modifier = [modifier[0], .. modifier[start..end], scrollLine.IsModifier];
        badModifier = [badModifier[0], .. badModifier[start..end], scrollLine.IsModifierBad];
        overrideColor = [overrideColor[0], .. overrideColor[start..end], scrollLine.OverrideColor];
        if (oneDropLogo > 0) {
            if (oneDropLogo < start || oneDropLogo >= end) oneDropLogo = -1;
            else oneDropLogo -= start - 1;
        }

        return tooltips;
    }

    public static int GetTooltipScroll(int type) => _scroll.GetValueOrDefault(type, 0);

    public static int GetCroppedNumTooltips() {
        int inset = Main.SettingsEnabled_OpaqueBoxBehindTooltips ? 18 : 4;
        return Math.Max(3, (int)((Main.screenHeight - inset) * Configs.ScrollableTooltip.Instance.maximumHeight / FontAssets.MouseText.Value.LineSpacing));
    }

    private static Dictionary<int, int> _scroll = [];
}