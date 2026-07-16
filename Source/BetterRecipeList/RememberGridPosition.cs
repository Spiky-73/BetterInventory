using System;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI.Gamepad;

namespace BetterInventory.BetterRecipeList;

public class RememberGridPosition : ILoadable {

    public bool IsLoadingEnabled(Mod mod) => Compatibility.LoadDisabledFeatures || BetterRecipeListConfig.RememberGridPosition;
    public void Load(Mod mod) {
        On_Main.DrawInterface_Resources_ClearBuffs += HookRememberListPosition;
        On_Recipe.ClearAvailableRecipes += HookClearAvailableRecipes;
    }
    public void Unload() { }

    private static void HookRememberListPosition(On_Main.orig_DrawInterface_Resources_ClearBuffs orig) {
        if (!BetterRecipeListConfig.RememberGridPosition) {
            orig();
            return;
        }
        _start = Main.recStart;
        orig();
        Main.recStart = _start;
    }


    private static void HookClearAvailableRecipes(On_Recipe.orig_ClearAvailableRecipes orig) {
        if (BetterRecipeListConfig.RememberGridPosition) {
            _focusedRecipeLine = GetRecipeLine(Main.focusRecipe);
            _focusedVisible = !_skipFollow && 0 <= _focusedRecipeLine && _focusedRecipeLine < UILinkPointNavigator.Shortcuts.CRAFT_IconsPerColumn;
        }
        orig();
    }

    // TODO Called in DisplayedRecipes
    internal static void HookTryRefocusingList(On_Recipe.orig_TryRefocusingRecipe orig, int oldRecipe) {
        orig(oldRecipe);
        if (!BetterRecipeListConfig.RememberGridPosition) return;
        _skipFollow = false;
        if (!_focusedVisible) return;
        Main.recStart = Math.Max(0, SpikysLib.MathHelper.Snap(Main.focusRecipe, UILinkPointNavigator.Shortcuts.CRAFT_IconsPerRow, SpikysLib.MathHelper.SnapMode.Floor) - _focusedRecipeLine * UILinkPointNavigator.Shortcuts.CRAFT_IconsPerRow);
    }

    // TODO called from FavoriteRecipes
    public static void DontFollowOnNextRefocus() {
        _skipFollow = true;
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