using System;
using SpikysLib.IO;

namespace BetterInventory.IO;

public sealed class ToFromStringConverterFix(Type type) : ToFromStringConverter(type) {}