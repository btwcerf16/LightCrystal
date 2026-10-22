using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Items/Item Database")]
public sealed class ItemDatabase : ScriptableObject
{
    [SerializeField] private Item[] _items = new Item[0];

    public Item GetPrefab(int itemId)
    {
        if (itemId <= 0)
            return null;

        Item prefab = null;
        foreach (Item item in _items)
        {
            if (item == null || item.ItemId != itemId)
                continue;

            if (prefab != null)
            {
                Debug.LogError($"Item ID {itemId} is used more than once.", this);
                return null;
            }

            prefab = item;
        }

        return prefab;
    }

    [ContextMenu("Validate IDs")]
    private void OnValidate()
    {
        HashSet<int> ids = new HashSet<int>();
        foreach (Item item in _items)
        {
            if (item == null)
                continue;

            if (item.ItemId <= 0)
                Debug.LogError($"Item {item.name} must have a positive ID.", this);
            else if (!ids.Add(item.ItemId))
                Debug.LogError($"Item ID {item.ItemId} is used more than once.", this);
        }
    }
}
