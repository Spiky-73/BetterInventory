using System;
using System.Collections;
using System.Collections.Generic;

namespace BetterInventory.DataStructures;

public readonly record struct JoinedList<T> : IList<T>, IReadOnlyList<T> {
    public IList<T>[] Lists { get; }

    public T this[int index] {
        get {
            var i = InnerIndex(index);
            return Lists[i.list][i.index];
        }
        set {
            var i = InnerIndex(index);
            Lists[i.list][i.index] = value;
        }
    }

    public (int list, int index) InnerIndex(int index) {
        int l = 0;
        while(index >= Lists[l].Count) index -= Lists[l++].Count;
        return (l, index);
    }

    public int Count {
        get {
            int c = 0;
            foreach (IList<T> list in Lists) c += list.Count;
            return c;
        }
    }

    public bool IsReadOnly {
        get {
            foreach (IList<T> list in Lists) {
                if (!list.IsReadOnly) return false;
            }
            return true;
        }
    }

    public JoinedList(params IList<T>[] lists) => Lists = lists;

    public bool Contains(T item) {
        foreach (IList<T> list in Lists) if (list.Contains(item)) return true;
        return false;
    }


    public int IndexOf(T item) {
        int s = 0;
        foreach (IList<T> list in Lists) {
            int i = list.IndexOf(item);
            if (i != -1) return i;
            s += list.Count;
        }
        return -1;
    }

    public void CopyTo(T[] array, int arrayIndex) {
        foreach (IList<T> list in Lists) {
            list.CopyTo(array, arrayIndex);
            arrayIndex += list.Count;
        }
    }

    public IEnumerator<T> GetEnumerator() {
        foreach(IList<T> list in Lists){
            foreach (T item in list) yield return item;
        }
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    void ICollection<T>.Clear() => throw new NotSupportedException();
    void IList<T>.Insert(int index, T item) => throw new NotSupportedException();
    void ICollection<T>.Add(T item) => throw new NotSupportedException();
    bool ICollection<T>.Remove(T item) => throw new NotSupportedException();
    void IList<T>.RemoveAt(int index) => throw new NotSupportedException();

}
public readonly record struct ListIndices<T> : IList<T>, IReadOnlyList<T> {

    public IList<T> List { get; }
    public IList<int> Indices { get; }
    public bool ExcludeIndices { get; }

    public ListIndices(IList<T> list) : this(list, Array.Empty<int>(), true) { }
    public ListIndices(IList<T> list, params int[] indices) : this(list, (IList<int>)indices) { }
    public ListIndices(IList<T> list, bool excludeIndices, params int[] indices) : this (list, indices, excludeIndices) { }
    public ListIndices(IList<T> list, IList<int> indices, bool excludeIndices = false) {
        List = list;
        Indices = indices;
        ExcludeIndices = excludeIndices;
    }

    public T this[int index] { get => List[ToInnerIndex(index)]; set => List[ToInnerIndex(index)] = value; }

    public int Count => ExcludeIndices ? (List.Count - Indices.Count) : Indices.Count;

    public bool IsReadOnly => List.IsReadOnly;

    private int ToInnerIndex(int index) {
        if (!ExcludeIndices) return Indices[index];
        int i = 0;
        while (i < Indices.Count && Indices[i] <= index) i++;
        return index + i;
    }

    public int FromInnerIndex(int index) {
        if (!ExcludeIndices) return Indices.IndexOf(index);
        int i = 0;
        while (i < Indices.Count && Indices[i] <= index) i++;
        return index - i;
    }

    public bool Contains(T item) => IndexOf(item) != -1;
    public int IndexOf(T item) {
        int i = 0;
        foreach (int index in GetIndices()) {
            if (Equals(item, List[index])) return i;
            i++;
        }
        return -1;
    }

    public void CopyTo(T[] array, int arrayIndex) {
        foreach (T item in this) array[arrayIndex++] = item;
    }

    public IEnumerable<int> GetIndices() {
        if (!ExcludeIndices) {
            foreach (int i in Indices) yield return i;
        } else {
            int j = 0;
            for (int i = 0; i < List.Count; i++) {
                if (j < Indices.Count && Indices[j] < i) j++;
                if (j < Indices.Count && Indices[j] == i) continue;
                yield return i;
            }
        }
    }

    public IEnumerator<T> GetEnumerator() {
        foreach (int i in GetIndices()) yield return List[i]; 
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    void ICollection<T>.Clear() => throw new NotSupportedException();
    void IList<T>.Insert(int index, T item) => throw new NotSupportedException();
    void ICollection<T>.Add(T item) => throw new NotSupportedException();
    bool ICollection<T>.Remove(T item) => throw new NotSupportedException();
    void IList<T>.RemoveAt(int index) => throw new NotSupportedException();
}