using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class ObjectPool<T> where T : Component
{
    private readonly Func<T> _create;
    private readonly int _maxInactive;
    private readonly Stack<T> _available = new();
    private readonly HashSet<T> _returned = new();

    public ObjectPool(Func<T> create, int maxInactive = 128)
    {
        _create = create;
        _maxInactive = maxInactive;
    }

    public T GetObject()
    {
        T item = null;
        while (_available.Count > 0 && item == null)
        {
            item = _available.Pop();
            _returned.Remove(item);
        }
        if (item == null)
            item = _create();
        item.gameObject.SetActive(true);
        return item;
    }

    public void ReturnObject(T item)
    {
        if (item == null || !_returned.Add(item))
            return;
        item.gameObject.SetActive(false);
        if (_available.Count < _maxInactive)
            _available.Push(item);
        else
        {
            _returned.Remove(item);
            UnityEngine.Object.Destroy(item.gameObject);
        }
    }

    public void Clear()
    {
        foreach (T item in _available)
            if (item != null)
                UnityEngine.Object.Destroy(item.gameObject);
        _available.Clear();
        _returned.Clear();
    }
}
