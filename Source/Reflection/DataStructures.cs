using SpikysLib.Reflection;
using Terraria.DataStructures;

namespace BetterInventory.Reflection;

public static class EntrySorter<TEntryType, TStepType> where TEntryType : new() where TStepType : IEntrySortStep<TEntryType> {
    public static readonly Field<Terraria.DataStructures.EntrySorter<TEntryType, TStepType>, int> _prioritizedStep = new(nameof(_prioritizedStep));
}