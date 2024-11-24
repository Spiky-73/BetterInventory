using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace BetterInventory.ItemActions;

public class TooltipScroll : ILoadable {
    
    public void Load(Mod mod) {
        MonoModHooks.Add(Reflection.ItemLoader.ModifyTooltips, HookTooltipScroll);
    }
    public void Unload() { }

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

    private static int _tooltipType;
    public static Dictionary<int, int> tooltipStart = [];
}
