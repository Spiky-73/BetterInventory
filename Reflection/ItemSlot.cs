using TItemSlot = Terraria.UI.ItemSlot;
using TItem = Terraria.Item;

namespace BetterInventory.Reflection;

public static class ItemSlot {
    public static readonly StaticField<bool[]> canFavoriteAt = new(typeof(TItemSlot), nameof(canFavoriteAt));
    public static readonly StaticField<TItem[]> singleSlotArray = new(typeof(TItemSlot), nameof(canFavoriteAt));
}
