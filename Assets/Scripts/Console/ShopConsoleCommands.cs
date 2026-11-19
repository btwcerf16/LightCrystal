using System;
using Unity.Netcode;
using UnityEngine;

public sealed class ShopConsoleCommands : MonoBehaviour
{
    [SerializeField] private NetworkShop _shopPrefab;
    [SerializeField] private NetworkSummonZone _zonePrefab;
    [SerializeField] private NetworkEnchanter _enchanterPrefab;
    [SerializeField] private NetworkEnchanter _enchantRunePrefab;

    [ConsoleCommand]
    private string SpawnShop(float capacity)
    {
        if (capacity < 0f || float.IsNaN(capacity) || float.IsInfinity(capacity))
            throw new ArgumentOutOfRangeException(nameof(capacity), "Мощность должна быть конечным неотрицательным числом.");

        NetworkShop shop = CreateShop();
        shop.ConfigureSpawn(capacity, isWaveEvent: false);
        shop.NetworkObject.Spawn();
        return $"Создан магазин: {shop.GetComponent<Shop>().Category}, мощность {capacity}.";
    }

    [ConsoleCommand]
    private string SpawnShopCategory(EShopCategory category)
    {
        NetworkShop shop = CreateShop();
        shop.NetworkObject.Spawn();
        shop.Initialize(category);
        return $"Создан магазин: {category}.";
    }

    [ConsoleCommand]
    private string SpawnSummonZone()
    {
        NetworkManager manager = NetworkManager.Singleton;
        if (manager == null || !manager.IsListening || !manager.IsServer)
            throw new InvalidOperationException("Зону можно создать только на запущенном сервере.");
        Crystal crystal = FindFirstObjectByType<Crystal>();
        if (_zonePrefab == null || crystal == null || FindFirstObjectByType<ShopSpawnArea>() == null)
            throw new InvalidOperationException("Нужны префаб зоны, кристалл и карта с ShopSpawnArea.");

        float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        Vector3 position = crystal.transform.position + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * 12f;
        NetworkSummonZone zone = Instantiate(_zonePrefab, position, Quaternion.identity);
        zone.NetworkObject.Spawn();
        return "Зона призыва появилась рядом с кристаллом.";
    }

    [ConsoleCommand]
    private string SpawnEnchanter(float capacity = 50f)
    {
        if (!float.IsFinite(capacity) || capacity < 0f || capacity > 100f)
            throw new ArgumentOutOfRangeException(nameof(capacity), "Заполненность должна быть от 0 до 100.");

        NetworkManager manager = NetworkManager.Singleton;
        if (manager == null || !manager.IsListening || !manager.IsServer)
            throw new InvalidOperationException("Чарователя можно создать только на запущенном сервере.");
        if (_enchanterPrefab == null)
            throw new InvalidOperationException("Префаб чарователя не назначен.");
        NetworkEnchanter enchanter = Instantiate(_enchanterPrefab, transform.position + Vector3.right * 4f, Quaternion.identity);
        enchanter.ConfigureSpawn(capacity, isWaveEvent: false);
        enchanter.NetworkObject.Spawn();
        return $"Чарователь появился: заполненность {capacity}%, уровень {enchanter.EventLevel}.";
    }

    [ConsoleCommand]
    private string SpawnRune()
    {
        NetworkManager manager = NetworkManager.Singleton;
        if (manager == null || !manager.IsListening || !manager.IsServer)
            throw new InvalidOperationException("Кристалл можно создать только на запущенном сервере.");
        if (_enchantRunePrefab == null)
            throw new InvalidOperationException("Префаб рунического кристалла не назначен.");
        NetworkEnchanter rune = Instantiate(_enchantRunePrefab, transform.position + Vector3.right * 2f, Quaternion.identity);
        rune.NetworkObject.Spawn();
        return "Рунический кристалл появился.";
    }

    private NetworkShop CreateShop()
    {
        NetworkManager manager = NetworkManager.Singleton;
        if (manager == null || !manager.IsListening || !manager.IsServer)
            throw new InvalidOperationException("Магазин можно создать только на запущенном сервере.");
        if (_shopPrefab == null)
            throw new InvalidOperationException("Префаб магазина не назначен.");

        return Instantiate(_shopPrefab, transform.position + Vector3.right * 4f, Quaternion.identity);
    }
}
