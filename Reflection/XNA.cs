using SpikysLib.Reflection;
using TColor = Microsoft.Xna.Framework.Color;

namespace BetterInventory.Reflection;

public static class Color {
    public static readonly StaticProperty<TColor> White = new(typeof(TColor), nameof(TColor.White));
}