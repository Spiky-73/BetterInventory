using TShortcuts = Terraria.UI.Gamepad.UILinkPointNavigator.Shortcuts;
using SpikysLib.Reflection;
using TItemSlot = Terraria.UI.ItemSlot;
using TPlayer = Terraria.Player;
using TItem = Terraria.Item;
namespace BetterInventory.Reflection;

public static class ItemSlot {
    public static readonly StaticField<bool[]> canFavoriteAt = new(typeof(TItemSlot), nameof(canFavoriteAt));
    public static readonly StaticMethod<bool> AccessorySwap = new(typeof(TItemSlot), nameof(AccessorySwap), typeof(TPlayer), typeof(TItem), typeof(TItem).MakeByRefType()); // bool AccessorySwap(Player player, Item item, ref Item result)

}

public static class UILinkPointNavigator {
    public static readonly StaticField<int> CRAFT_CurrentIngredientsCount = new(typeof(TShortcuts), nameof(TShortcuts.CRAFT_CurrentIngredientsCount));
    public static readonly StaticField<int> CRAFT_IconsPerRow = new(typeof(TShortcuts), nameof(TShortcuts.CRAFT_IconsPerRow));
    public static readonly StaticField<int> CRAFT_CurrentRecipeSmall = new(typeof(TShortcuts), nameof(TShortcuts.CRAFT_CurrentRecipeSmall));
}