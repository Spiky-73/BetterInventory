using Terraria.UI.Gamepad;

namespace BetterInventory.Improvements.BetterRecipeGrid;

public static class PageScroll {

    public static float ModifyRecipeScroll(int delta) {
        return UILinkPointNavigator.Shortcuts.CRAFT_IconsPerRow * UILinkPointNavigator.Shortcuts.CRAFT_IconsPerColumn;
    }
}