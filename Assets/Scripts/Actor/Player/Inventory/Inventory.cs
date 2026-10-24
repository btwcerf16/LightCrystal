using System;
using System.Collections.Specialized;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    private Item[] _items = new Item[6];
    public int Size => _items.Length;
    public Item GetItem(int index) => index >= 0 && index < _items.Length ? _items[index] : null;

    public event Action ContentsChanged;
    public event Action<int, int> SlotChangeRequested;
    public event Action<int> ItemDropRequested;
    public event Action<int> ItemUseRequested;

    public void RequestUseItem(int index)
    {
        ItemUseRequested?.Invoke(index);
    }

    public int FindEmptySlot() => Array.IndexOf(_items, null);

    public void RequestChangeItemSlot(int previousIndex, int newIndex)
    {
        SlotChangeRequested?.Invoke(previousIndex, newIndex);
    }

    public void RequestDropItem(int index)
    {
        ItemDropRequested?.Invoke(index);
    }

    public bool CanUseItems { get; private set; } //понадобится когда добавим нажатия для предметов

    public void EquipItem(Item item)
    {
        for (int i = 0; i < _items.Length; i++)
        {
            if (_items[i] == null)
            {
                _items[i] = item;
                item.Equip();
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
    public void SetItem(int index, ulong uid)
    {
        if (index < 0 || index >= _items.Length)
            return;

        Item item = null;
        if (uid != 0)
        {
            Item[] items = GetComponentsInChildren<Item>(true);
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i].Uid == uid)
                {
                    item = items[i];
                    break;
                }
            }

            if (item == null)
            {
                Debug.LogError($"Item with UID {uid} was not found on this player.", this);
                return;
            }
        }

        if (_items[index] == item)
            return;

        if (_items[index] != null && _items[index].transform.IsChildOf(transform))
        {
            _items[index].Drop();
            _items[index].gameObject.SetActive(false);
        }
        _items[index] = item;
        item?.Equip();
        ContentsChanged?.Invoke();
    }

    public void DropItem(int index)
    {
        if (!HasItem(index))
            return;
        _items[index].Drop();
        _items[index] = null;
        ContentsChanged?.Invoke();
    }
    public void UseItem(int index)
    {
        if (!HasItem(index))
            return;
        if (_items[index].TryUse())
            ContentsChanged?.Invoke();
    }

    private bool HasItem(int index)
    {
        return index < _items.Length && index >= 0 && _items[index] != null;
    }

    public void SetCanUseItems(bool canUseItems) { CanUseItems = canUseItems; }
}
