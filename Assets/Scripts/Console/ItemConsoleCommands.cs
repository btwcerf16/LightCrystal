using System;
using Unity.Netcode;
using UnityEngine;

public sealed class ItemConsoleCommands : MonoBehaviour
{
    [ConsoleCommand]
    private string GiveItem(int id)
    {
        ItemFactory factory = NetworkManager.Singleton.GetComponent<ItemFactory>();
        Item prefab = factory.GetPrefab(id);
        if (prefab == null)
            throw new ArgumentException("Предмет с таким ID не найден.");
        ActorArsenal arsenal = GetComponent<ActorArsenal>();
        Inventory inventory = GetComponent<Inventory>();
        int slot = prefab is Weapon ? arsenal.FindEmptySlot() : inventory.FindEmptySlot();
        if (slot < 0)
            throw new InvalidOperationException("Нет свободного слота.");
        Transform parent = prefab is Weapon ? GetComponentInChildren<ActorHand>().HandTransform : transform;
        Item item = factory.CreateItem(id, parent);
        if (item is Weapon weapon)
            arsenal.SetWeapon(slot, weapon);
        else
            inventory.EquipItem(item);
        return "Выдано: " + item.DisplayName;
    }
}
