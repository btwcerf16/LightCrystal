using System;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    private Item[] _items = new Item[6];
    public int Size => _items.Length;
    public Item GetItem(int index) => index >= 0 && index < _items.Length ? _items[index] : null;

    public event Action ContentsChanged;

    public bool CanUseItems { get; private set; } //понадобится когда добавим нажатия для предметов

    public void EquipItem(Item item)
    {
        for (int i = 0; i < _items.Length; i++)
        {
            if (_items[i] == null)
            {
                _items[i] = item;
                ContentsChanged?.Invoke();
                return;
            }
        }
        Debug.Log("Полный инвентарь");
    }
    public void ChangeItemSlot(int previousIndex, int newIndex)
    {
        if (previousIndex >= 0 && previousIndex < _items.Length && newIndex >= 0 && newIndex < _items.Length)
        {
            var item = _items[newIndex];
            _items[newIndex] = _items[previousIndex];
            _items[previousIndex] = item;
            ContentsChanged?.Invoke();
        }
    }
    public void DropItem(int index)
    {
        if (!HasItem(index))
            return;
        ContentsChanged?.Invoke();
    }
    public void UseItem(int index)
    {
        if (!HasItem(index))
            return;
        _items[index].Use();
    }

    private bool HasItem(int index)
    {
        return index < _items.Length && index >= 0 && _items[index] != null;
    }

    public void SetCanUseItems(bool canUseItems) { CanUseItems = canUseItems; }
}
