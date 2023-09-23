using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace BetterInventory.DataStructures;

public readonly record struct Range(int Start, int End);

public sealed class RangeSet : IEnumerable<Range> {

    public ReadOnlyCollection<Range> Ranges => new(_ranges);

    public void Add(int item) => Add(new Range(item, item));
    public void Add(Range range) {
        int i = FindInsertIndex(range.Start);
        if (i == 0 || range.Start - _ranges[i - 1].End > 1) _ranges.Insert(i, range);
        else if (range.End > _ranges[--i].End) _ranges[i] = new(_ranges[i].Start, range.End);
        
        int j = i + 1;
        while (j < _ranges.Count &&_ranges[j].End <= _ranges[i].End) j++;
        _ranges.RemoveRange(i+1, j - i - 1);

        if (i != _ranges.Count-1 && _ranges[i+1].Start - _ranges[i].End <= 1) {
            _ranges[i] = new(_ranges[i].Start, _ranges[i+1].End);
            _ranges.RemoveAt(i+1);
            
        }
    }

    public void Remove(int item) {
        int i = FindInsertIndex(item);
        if (i == 0 || i == _ranges.Count && _ranges[i - 1].End < item) return;
        if (item == _ranges[i - 1].Start) _ranges[i - 1] = new(item + 1, _ranges[i - 1].End);
        else {
            if (item != _ranges[i - 1].End) _ranges.Insert(i, new(item + 1, _ranges[i - 1].End));
            _ranges[i - 1] = new(_ranges[i - 1].Start, item - 1);
        }
    }

    public bool Contains(int item) {
        int i = FindInsertIndex(item);
        return i != 0 && item <= _ranges[i-1].End;
    }

    private int FindInsertIndex(int item) {
        for (int i = 0; i < _ranges.Count; i++) if (item < _ranges[i].Start) return i;
        return _ranges.Count;
    }

    public void Clear() => _ranges.Clear();

    public IEnumerator<Range> GetEnumerator() => _ranges.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private readonly List<Range> _ranges = new();
}