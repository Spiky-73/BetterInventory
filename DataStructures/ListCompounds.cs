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

    public JoinedList(params IList<T>[] lists) {
        Lists = lists;
    }

    public bool Contains(T item) {
        foreach (IList<T> list in Lists) {
            if (list.Contains(item)) return true;
        }
        return false;
    }


    public int IndexOf(T item) {
        foreach (IList<T> list in Lists) {
            int i = list.IndexOf(item);
            if (i != -1) return i;
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
    public int[] Indices { get; }
    public bool ExcludeIndices { get; }

    public ListIndices(IList<T> array, bool excludeIndices, params int[] indices) {
        List = array;
        Array.Sort(indices);
        Indices = indices;
        ExcludeIndices = excludeIndices;
    }
    public T this[int index] { get => List[InnerIndex(index)]; set => List[InnerIndex(index)] = value; }

    public int Count => ExcludeIndices ? (List.Count - Indices.Length) : Indices.Length;

    public bool IsReadOnly => List.IsReadOnly;

    private int InnerIndex(int index) {
        if (!ExcludeIndices) return Array.IndexOf(Indices, index);
        int i = 0;
        while (Indices[i] <= index) i++;
        return index + i;
    }

    public bool Contains(T item) => IndexOf(item) != -1;
    public int IndexOf(T item) {
        int i = 0;
        foreach (T t in this) {
            if (Equals(item, i)) return i;
            i++;
        }
        return -1;
    }

    public void CopyTo(T[] array, int arrayIndex) {
        foreach (T item in this) array[arrayIndex++] = item;
    }

    public IEnumerator<T> GetEnumerator() {
        if (!ExcludeIndices) {
            foreach (int i in Indices) yield return List[i];
        } else {
            int j = 0;
            for (int i = 0; i < List.Count; i++) {
                if (j < Indices.Length && Indices[j] < i) j++;
                if (j < Indices.Length && Indices[j] == i) continue;
                yield return List[i];
            }
        }
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    void ICollection<T>.Clear() => throw new NotSupportedException();
    void IList<T>.Insert(int index, T item) => throw new NotSupportedException();
    void ICollection<T>.Add(T item) => throw new NotSupportedException();
    bool ICollection<T>.Remove(T item) => throw new NotSupportedException();
    void IList<T>.RemoveAt(int index) => throw new NotSupportedException();
}