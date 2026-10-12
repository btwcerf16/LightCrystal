using UnityEngine;

public class Inventory : MonoBehaviour
{
    private Item[] _items = new Item[6];
    public int Size => _items.Length;

    public void EquipItem(Item item)
    {
        for (int i = 0; i < _items.Length; i++) 
        {
            if(_items[i] == null)
            {
                _items[i] = item;
                return;
            }
        }
        Debug.Log("Полный инвентарь");
    }
    public void ChangeItemSlot(int previousIndex, int newIndex)
    {
        if(newIndex <=  _items.Length && newIndex > 0) 
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
