using System;
using System.Collections.Generic;
using System.Linq;

namespace winUpdateMiniTool.Common;

public class MultiValueDictionary<TKey, TValue> : Dictionary<TKey, List<TValue>> {
  public void Add(TKey key, TValue value) {
    if (!TryGetValue(key, out var container)) {
      container = [];
      base.Add(key, container);
    }

    container.Add(value);
  }

  public bool ContainsValue(TKey key, TValue value) {
    var toReturn = false;
    if (TryGetValue(key, out var values)) toReturn = values.Contains(value);
    return toReturn;
  }

  public void Remove(TKey key, TValue value) {
    if (TryGetValue(key, out var container)) {
      container.Remove(value);
      if (container.Count <= 0) Remove(key);
    }
  }

  public List<TValue> GetValues(TKey key, bool returnEmptySet = true) {
    if (!TryGetValue(key, out var toReturn) && returnEmptySet) toReturn = [];
    return toReturn;
  }

  public int GetCount() {
    return this.Sum(pair => pair.Value.Count);
  }

  public TValue GetAt(int index) {
    var count = 0;
    foreach (var pair in this) {
      if (count + pair.Value.Count > index)
        return pair.Value[index - count];
      count += pair.Value.Count;
    }

    throw new IndexOutOfRangeException();
  }

  public TKey GetKey(int index) {
    var count = 0;
    foreach (var pair in this) {
      if (count + pair.Value.Count > index)
        return pair.Key;
      count += pair.Value.Count;
    }

    throw new IndexOutOfRangeException();
  }
}
