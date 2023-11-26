using TInventory = BetterInventory.ModSubInventory;

namespace BetterInventory.Reflection;

public static class ModSubInventory {
    public static readonly Method<TInventory, Terraria.Item, bool> Accepts = new(nameof(TInventory.Accepts));
}