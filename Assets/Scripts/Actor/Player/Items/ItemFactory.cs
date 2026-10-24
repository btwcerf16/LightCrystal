using Unity.Netcode;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(ItemUidGenerator))]
public sealed class ItemFactory : MonoBehaviour
{
    [SerializeField] private ItemDatabase _itemDatabase;

    private NetworkManager _networkManager;
    private ItemUidGenerator _uidGenerator;

    public Item GetPrefab(int itemId) => _itemDatabase.GetPrefab(itemId);

    private void Awake()
    {
        _networkManager = GetComponent<NetworkManager>();
        _uidGenerator = GetComponent<ItemUidGenerator>();
    }

    public Item CreateItem(int itemId, Transform parent)
    {
        if (!_networkManager.IsServer)
            return null;

        if (_itemDatabase == null)
        {
            Debug.LogError("Item database is not assigned.", this);
            return null;
        }

        Item prefab = _itemDatabase.GetPrefab(itemId);
        if (prefab == null)
        {
            Debug.LogError($"Item with ID {itemId} was not found in the database.", this);
            return null;
        }

        Item item = Instantiate(prefab, parent, false);
        item.SetUid(_uidGenerator.GenerateUid());
        return item;
    }

    public void AssignUid(Item item)
    {
        if (_networkManager.IsServer && item != null && item.Uid == 0)
            item.SetUid(_uidGenerator.GenerateUid());
    }

    public Item GetOrCreateItem(int itemId, ulong uid, Transform parent)
    {
        if (uid == 0)
            return null;

        Item item = null;
        foreach (Item candidate in FindObjectsByType<Item>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (candidate.Uid == uid)
            {
                item = candidate;
                break;
            }
        }

        if (item == null)
        {
            foreach (Item candidate in parent.GetComponentsInChildren<Item>(true))
            {
                if (candidate.Uid == 0 && candidate.ItemId == itemId)
                {
                    item = candidate;
                    break;
                }
            }
        }

        if (item == null)
        {
            Item prefab = _itemDatabase.GetPrefab(itemId);
            if (prefab == null)
                return null;
            item = Instantiate(prefab, parent, false);
            item.gameObject.SetActive(false);
        }
        else if (item.transform.parent != parent)
        {
            if (item.IsEquipped)
                item.Drop();
            item.gameObject.SetActive(false);
            item.transform.SetParent(parent, false);
            item.transform.localPosition = Vector3.zero;
        }

        item.SetUid(uid);
        return item;
    }
}
