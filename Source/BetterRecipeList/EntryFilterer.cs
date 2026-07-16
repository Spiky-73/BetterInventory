using System.Collections.Generic;
using System.Collections.ObjectModel;
using Terraria.DataStructures;

namespace BetterInventory.BetterRecipeList;

public sealed class EntryFilterer<T, U, V> where U : IEntryFilter<T> where V : ISearchFilter<T> {
    public void SetSearchFilter(V? filter) => _searchFilter = filter;
    public void SearchFilter(V filter) {
        _searchFilter = filter;
        _searchFilter.SetSearch(_search);
    }

    public void AddAvailableFilter(U filter) => _availableFilters.Add(filter);
    public void AddAvailableFilters(IEnumerable<U> filters) => _availableFilters.AddRange(filters);
    public ReadOnlyCollection<U> AvailableFilters() => _availableFilters.AsReadOnly();
    
    public void SetSearch(string? search) {
        _search = search;
        _searchFilter?.SetSearch(_search);
    }
    public void ClearSearch() {
        _search = string.Empty;
        _searchFilter?.SetSearch(_search);
    }
    public string? Search() => _search;

    public bool IsFilterActive(int index) => IsFilterActive(AvailableFilters()[index]);
    public bool IsFilterActive(U filter) => _activeFilters.Contains(filter);
    public void ToggleFilter(int index) => ToggleFilter(AvailableFilters()[index]);
    public void ToggleFilter(U filter) {
        if (!_activeFilters.Remove(filter)) _activeFilters.Add(filter);
    }
    public void ClearActiveFilters() => _activeFilters.Clear();
    public ReadOnlyCollection<U> ActiveFilters() => _activeFilters.AsReadOnly();

    public bool IsActive() => !string.IsNullOrEmpty(_search) || _activeFilters.Count > 0;
    public bool FitsFilters(T entry) {
        if (!string.IsNullOrEmpty(_search) && _searchFilter?.FitsFilter(entry) == false) return false;
        if (_activeFilters.Count == 0) return true;
        return _activeFilters.Exists(f => f.FitsFilter(entry));
    }

    private V? _searchFilter;
    private string? _search = string.Empty;
    private readonly List<U> _availableFilters = [];
    private readonly List<U> _activeFilters = [];
}
