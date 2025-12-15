using Terraria;
using Terraria.UI;

namespace BetterInventory.Improvements.BetterRecipeGrid;

public static class CraftOnRecipeGrid {
    public static void PreCraftItem(int i) {
        int f = Main.focusRecipe;
        if (CraftOnRecipeGridConfig.Instance.focusHovered) Main.focusRecipe = i;
        Main.HoverOverCraftingItemButton(i);
        if (f != Main.focusRecipe) Main.recFastScroll = true;
        Main.craftingHide = false;
    }

    public static void PostHoverRecipe(int i) {
        if (Main.numAvailableRecipes > 0 && Main.focusRecipe == i && !CraftOnRecipeGridConfig.Instance.focusHovered) ItemSlot.DrawGoldBGForCraftingMaterial = true;
    }

}