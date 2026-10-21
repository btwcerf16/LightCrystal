using UnityEngine;

public class Inventory : MonoBehaviour
{
    [SerializeField] private Item[] _items = new Item[6];

    public Item GetItem(int index) => index >= 0 && index < _items.Length ? _items[index] : null;

    public int ItemsSize => _items.Length;


    public void SetItem(int index, Item item)
    {
        if (index < 0 || index >= _items.Length || _items[index] == item)
            return;
        _items[index]?.Drop();
        _items[index] = item;
        item?.Equip();
    }

    public void EquipItem(Item item)
    {
        if (item == null || System.Array.IndexOf(_items, item) >= 0)
            return;
        for (int i = 0; i < _items.Length; i++) 
        {
            if(_items[i] == null)
            {
                _items[i] = item;
                item.Equip();
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
        }

    }
    public void DropItem(int index)
    {
        if (!HasItem(index))
            return;
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
}
