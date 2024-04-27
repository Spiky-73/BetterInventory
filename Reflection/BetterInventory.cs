using SpikysLib.Reflection;
using TInventory = BetterInventory.ModSubInventory;

namespace BetterInventory.Reflection;

public static class ModSubInventory {
    public static readonly Method<TInventory, bool> Accepts = new(nameof(TInventory.Accepts), typeof(Terraria.Item));
}