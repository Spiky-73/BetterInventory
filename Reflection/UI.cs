using TShortcuts = Terraria.UI.Gamepad.UILinkPointNavigator.Shortcuts;
using SpikysLib.Reflection;
using TItemSlot = Terraria.UI.ItemSlot;

namespace BetterInventory.Reflection;

public static class ItemSlot {
    public static readonly StaticField<bool[]> canFavoriteAt = new(typeof(TItemSlot), nameof(canFavoriteAt));
}

public static class UILinkPointNavigator {
    public static readonly StaticField<int> CRAFT_CurrentIngredientsCount = new(typeof(TShortcuts), nameof(TShortcuts.CRAFT_CurrentIngredientsCount));
    public static readonly StaticField<int> CRAFT_IconsPerRow = new(typeof(TShortcuts), nameof(TShortcuts.CRAFT_IconsPerRow));
    public static readonly StaticField<int> CRAFT_CurrentRecipeSmall = new(typeof(TShortcuts), nameof(TShortcuts.CRAFT_CurrentRecipeSmall));
}