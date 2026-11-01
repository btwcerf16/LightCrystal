using System;
using UnityEngine;

[RequireComponent(typeof(ActorArsenal))]
public sealed class EnchantConsoleCommands : MonoBehaviour
{
    [ConsoleCommand]
    private string Enchant(EnchantData enchant)
    {
        Weapon weapon = GetComponent<ActorArsenal>().ActiveWeapon;
        if (weapon == null || enchant == null)
            throw new InvalidOperationException("Нет выбранного оружия или такого зачарования.");
        if (!weapon.AddEnchant(enchant))
            throw new InvalidOperationException("Нет свободного слота чар или повтор запрещён.");
        return "Добавлено: " + enchant.DisplayName;
    }

    [ConsoleCommand]
    private string SetWeaponRarity(EItemRarity rarity)
    {
        Weapon weapon = GetComponent<ActorArsenal>().ActiveWeapon;
        if (weapon == null || !weapon.SetRarity(rarity))
            throw new InvalidOperationException("Нет оружия или в нём больше чар, чем допускает эта редкость.");
        return "Редкость: " + rarity;
    }

    [ConsoleCommand]
    private string ClearWeaponEnchants()
    {
        Weapon weapon = GetComponent<ActorArsenal>().ActiveWeapon;
        if (weapon == null)
            throw new InvalidOperationException("Нет выбранного оружия.");
        weapon.SetEnchants(Array.Empty<EnchantData>());
        return "Зачарования сняты.";
    }
}
