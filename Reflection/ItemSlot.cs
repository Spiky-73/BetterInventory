using TItemSlot = Terraria.UI.ItemSlot;

namespace BetterInventory.Reflection;

public static class ItemSlot {
    public static readonly StaticField<bool[]> canFavoriteAt = new(typeof(TItemSlot), nameof(canFavoriteAt));
}
