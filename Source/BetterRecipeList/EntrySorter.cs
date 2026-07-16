using System.Collections.Generic;
using System.Collections.ObjectModel;
using Terraria.DataStructures;

namespace BetterInventory.BetterRecipeList;

public sealed class EntrySorter<T,U> where U: IEntrySortStep<T> {
    public void AddSortStep(U step) => _availableSortSteps.Add(step);
    public void AddSortSteps(IEnumerable<U> steps) => _availableSortSteps.AddRange(steps);
    public ReadOnlyCollection<U> GetAvailableSortStep() => _availableSortSteps.AsReadOnly();
    
    public void SetActiveSortStep(int sortStep) => _sortIndex = sortStep;
    public int GetActiveSortStepIndex() => _sortIndex;
    public void ResetSortStep() => _sortIndex = 0;
    
    public U GetActiveSortStep() => GetAvailableSortStep()[GetActiveSortStepIndex()];
    public void SelectNextSortStep() => SetActiveSortStep((GetActiveSortStepIndex() + 1) % GetAvailableSortStep().Count);

    public bool IsActive() => _sortIndex != 0;
    public int Compare(T a, T b) => GetActiveSortStep().Compare(a, b);
    public IComparer<T> Comparer => GetActiveSortStep();

    private int _sortIndex = 0;
    private readonly List<U> _availableSortSteps = [];
}
