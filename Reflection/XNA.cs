using SpikysLib.Reflection;
using TColor = Microsoft.Xna.Framework.Color;
using TVector2 = Microsoft.Xna.Framework.Vector2;

namespace BetterInventory.Reflection;

public static class Vector2 {
    public static readonly StaticProperty<TVector2> Zero = new(typeof(TVector2), nameof(TVector2.Zero));
    public static readonly StaticProperty<TColor> White = new(typeof(TColor), nameof(TColor.White));
}

public static class Color {
    public static readonly StaticProperty<TColor> White = new(typeof(TColor), nameof(TColor.White));
}