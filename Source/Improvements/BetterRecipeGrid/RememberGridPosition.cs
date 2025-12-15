using System;
using Terraria;
using Terraria.UI.Gamepad;

namespace BetterInventory.Improvements.BetterRecipeGrid;

public static class RememberGridPosition {
    public static void PreClearBuffs() => _start = Main.recStart;
    public static void PostClearBuffs() => Main.recStart = _start;

    public static void PreClearAvailableRecipes() {
        _focusedRecipeLine = GetRecipeLine(Main.focusRecipe);
        _focusedVisible = !_skipFollow && 0 <= _focusedRecipeLine && _focusedRecipeLine < UILinkPointNavigator.Shortcuts.CRAFT_IconsPerColumn;
    }

    public static void DontFollowOnNextRefocus() {
        _skipFollow = true;
    }

    public static void TryRefocusingRecipe() {
        _skipFollow = false;
        if (!_focusedVisible) return;
        Main.recStart = Math.Max(0, SpikysLib.MathHelper.Snap(Main.focusRecipe, UILinkPointNavigator.Shortcuts.CRAFT_IconsPerRow, SpikysLib.MathHelper.SnapMode.Floor)
            - _focusedRecipeLine * UILinkPointNavigator.Shortcuts.CRAFT_IconsPerRow);
    }

    public static int GetRecipeLine(int availableRecipeIndex) {
        int delta = availableRecipeIndex - Main.recStart;
        int line = delta / UILinkPointNavigator.Shortcuts.CRAFT_IconsPerRow;
        if (delta < 0) line--;
        return line;
    }

    private static int _start;
    private static bool _skipFollow;
    private static bool _focusedVisible;
    private static int _focusedRecipeLine;
}