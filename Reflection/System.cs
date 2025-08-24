using SpikysLib.Reflection;
using TInt32 = int;

namespace BetterInventory.Reflection;

public static class Int32 {
    new public static readonly Method<TInt32, string> ToString = new(nameof(ToString));
}

public static class List<T> {
    public static readonly Property<System.Collections.Generic.List<T>, int> Count = new(nameof(System.Collections.Generic.List<T>.Count));
    public static class Enumerator {
        public static Property<System.Collections.Generic.List<T>.Enumerator, T> Current = new(nameof(System.Collections.Generic.List<T>.Enumerator.Current));
    }
}
