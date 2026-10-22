using Unity.Netcode;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(ItemUidGenerator))]
public sealed class ItemFactory : MonoBehaviour
{
    [SerializeField] private ItemDatabase _itemDatabase;

    private NetworkManager _networkManager;
    private ItemUidGenerator _uidGenerator;

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
}
