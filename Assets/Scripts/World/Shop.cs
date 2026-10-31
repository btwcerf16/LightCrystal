using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Shop : MonoBehaviour
{
    [SerializeField] private ItemDatabase _itemDatabase;
    [SerializeField] private Transform[] _itemsPosition = new Transform[6];
    [SerializeField] private int[] _lowRarityWeights = { 65, 25, 7, 2, 1 };
    [SerializeField] private int[] _averageRarityWeights = { 40, 35, 17, 6, 2 };
    [SerializeField] private int[] _highRarityWeights = { 15, 30, 30, 18, 7 };
    [SerializeField] private int[] _secretRarityWeights = { 0, 0, 0, 70, 30 };

    public Item[] Items { get; private set; } = new Item[0];
    public List<WorldItem> _worldItems = new();
    [SerializeField] private GameObject _worldItemPrefab; 

    public EShopCategory Category { get; private set; }

    public void Initialize(float capacityOfShop)
    {
        if (capacityOfShop <= 40)
            Initialize(EShopCategory.Low);
        else if (capacityOfShop <= 80)
            Initialize(EShopCategory.Average);
        else
            Initialize(EShopCategory.High);
    }

    public void Initialize(EShopCategory category)
    {
        Category = category;
        CreateShop();
    }

    public void CreateShop()
    {
        Items = new Item[0];
        if (_itemDatabase == null)
        {
            Debug.LogError("Shop item database is not assigned.", this);
            return;
        }

        switch (Category)
        {
            case EShopCategory.Low:
                CreateCategory(_lowRarityWeights);
                break;
            case EShopCategory.Average:
                CreateCategory(_averageRarityWeights);
                break;
            case EShopCategory.High:
                CreateCategory(_highRarityWeights);
                break;
            case EShopCategory.Secret:
                CreateCategory(_secretRarityWeights);
                break;
        }
    }

    public Transform GetItemPosition(int index)
    {
        if (index < 0 || index >= _itemsPosition.Length)
            return null;
        return _itemsPosition[index];
    }

    private void CreateCategory(int[] rarityWeights)
    {
        var itemsByRarity = new List<Item>[rarityWeights.Length];
        int totalWeight = 0;
        for (int i = 0; i < rarityWeights.Length; i++)
        {
            var rarity = (EItemRarity)i;
            itemsByRarity[i] = _itemDatabase.GetItemsByRarity(rarity, rarity);
            if (itemsByRarity[i].Count > 0)
                totalWeight += Mathf.Max(0, rarityWeights[i]);
        }

        if (totalWeight == 0)
        {
            Debug.LogWarning($"No available items with a positive weight for {Category} shop.", this);
            return;
        }

        Items = new Item[Mathf.Min(3, _itemsPosition.Length)];
        for (int i = 0; i < Items.Length; i++)
        {
            int roll = Random.Range(0, totalWeight);
            for (int rarity = 0; rarity < itemsByRarity.Length; rarity++)
            {
                var items = itemsByRarity[rarity];
                if (items.Count == 0)
                    continue;

                int weight = Mathf.Max(0, rarityWeights[rarity]);
                if (roll < weight)
                {
                    Items[i] = items[Random.Range(0, items.Count)];
                    break;
                }
                roll -= weight;
            }
            WorldItem worldItem = Instantiate(_worldItemPrefab, _itemsPosition[i].position, Quaternion.identity).GetComponent<WorldItem>();
            worldItem.SetItem(Items[i].gameObject, true);
            _worldItems.Add(worldItem);
        }

    }
    public List<WorldItem> GetItems()
    {
        return _worldItems;
    }


}
