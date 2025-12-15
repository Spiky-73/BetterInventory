using System;
using Terraria;
using Terraria.UI.Gamepad;

namespace BetterInventory.Improvements.BetterRecipeGrid;

public static class NoRecGridOffset {

    public static void PostScroll() {
        Main.recStart -= Main.recStart % UILinkPointNavigator.Shortcuts.CRAFT_IconsPerRow;
        Main.recStart = Math.Min(Main.recStart, Math.Max(0, SpikysLib.MathHelper.Snap(Main.numAvailableRecipes, UILinkPointNavigator.Shortcuts.CRAFT_IconsPerRow, SpikysLib.MathHelper.SnapMode.Ceiling) - UILinkPointNavigator.Shortcuts.CRAFT_IconsPerRow * UILinkPointNavigator.Shortcuts.CRAFT_IconsPerColumn));
    }
}