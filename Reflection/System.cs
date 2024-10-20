using SpikysLib.Reflection;
using TInt32 = int;

namespace BetterInventory.Reflection;

public static class Int32 {
    new public static readonly Method<TInt32, string> ToString = new(nameof(ToString));
}
