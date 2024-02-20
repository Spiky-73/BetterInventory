using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace BetterInventory.DataStructures;

public readonly record struct Range(int Start, int End) : IList<int> {
    public Range(int value) : this(value, value) {}
    public static Range FromCount(int start, int count) => new(start, start + count - 1);
    public readonly int Count => End - Start + 1;

    public bool IsReadOnly => true;

    public int this[int index] { get => Start + index; set => throw new NotSupportedException(); }

    public int IndexOf(int item) => Contains(item) ? item-Start : -1;
    public bool Contains(int item) => Start <= item && item <= End;

    public void CopyTo(int[] array, int arrayIndex) {
        for (int i = 0; i < Count; i++) array[i] = this[i];
    }

    public IEnumerator<int> GetEnumerator() {
        for (int i = 0; i < Count; i++) yield return this[i];
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    void ICollection<int>.Clear() => throw new NotSupportedException();
    void IList<int>.Insert(int index, int item) => throw new NotSupportedException();
    void ICollection<int>.Add(int item) => throw new NotSupportedException();
    bool ICollection<int>.Remove(int item) => throw new NotSupportedException();
    void IList<int>.RemoveAt(int index) => throw new NotSupportedException();
}

public sealed class RangeSet : IEnumerable<int> {

    public RangeSet() {
        Count = 0;
        _ranges = new();
    }

    public RangeSet(IEnumerable<int> values) :this() => AddRange(values);


    public ReadOnlyCollection<Range> Ranges => new(_ranges);

    public int Count { get; private set; }

    public bool Add(int item) => Add(new Range(item, item));
    public void AddRange(IEnumerable<int> enumerable) { foreach (int i in enumerable) Add(i); }
    
    public bool Add(Range range) {
        int i = FindInsertIndex(range.Start);
        if (i != 0 && _ranges[i - 1].Start <= range.Start && range.End <= _ranges[i - 1].End) return false;
        if (i == 0 || range.Start - _ranges[i - 1].End > 1) _ranges.Insert(i, range);
        else if (range.End > _ranges[--i].End) {
            Count -= _ranges[i].Count;
            _ranges[i] = new(_ranges[i].Start, range.End);
        }
        
        int j = i + 1;
        while (j < _ranges.Count && _ranges[j].End <= _ranges[i].End) {
            Count -= _ranges[j].Count;
            j++;
        }
        _ranges.RemoveRange(i+1, j - i - 1);

        if (i != _ranges.Count-1 && _ranges[i+1].Start - _ranges[i].End <= 1) {
            _ranges[i] = new(_ranges[i].Start, _ranges[i+1].End);
            Count -= _ranges[i + 1].Count;
            _ranges.RemoveAt(i+1);
        }
        Count += _ranges[i].Count;
        return true;
    }

    public bool Remove(int item) {
        if (!Contains(item, out int i)) return false;
        if (_ranges[i - 1].Count == 1) _ranges.RemoveAt(i - 1);
        else if (item == _ranges[i - 1].Start) _ranges[i - 1] = new(item + 1, _ranges[i - 1].End);
        else {
            if (item != _ranges[i - 1].End) _ranges.Insert(i, new(item + 1, _ranges[i - 1].End));
            _ranges[i - 1] = new(_ranges[i - 1].Start, item - 1);
        }
        Count--;
        return true;
    }

    public bool Contains(int item) => Contains(item, out _);
    private bool Contains(int item, out int index){
        index = FindInsertIndex(item);
        return index != 0 && item <= _ranges[index-1].End;
    }
    private int FindInsertIndex(int item) {
        for (int i = 0; i < _ranges.Count; i++) if (item < _ranges[i].Start) return i;
        return _ranges.Count;
    }

    public void Clear() {
        _ranges.Clear();
        Count = 0;
    }

    public IEnumerable<Range> GetRanges() => _ranges;
    public IEnumerator<int> GetEnumerator() {
        foreach (Range range in GetRanges()){
            foreach (int value in range) yield return value;
        }
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private readonly List<Range> _ranges;
}