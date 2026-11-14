using System;
using UnityEngine;

[RequireComponent(typeof(ActorArsenal))]
public sealed class EnchantConsoleCommands : MonoBehaviour
{
    [ConsoleCommand]
    private string Enchant(EnchantData enchant)
    {
        Weapon weapon = GetComponent<ActorArsenal>().ActiveWeapon;
        if (weapon == null || !weapon.TryGetComponent(out WeaponEnchants enchants) || enchant == null)
            throw new InvalidOperationException("Нет выбранного оружия или такого зачарования.");
        if (!enchants.AddEnchant(enchant))
            throw new InvalidOperationException("Нет свободного слота чар или повтор запрещён.");
        return "Добавлено: " + enchant.DisplayName;
    }

    [ConsoleCommand]
    private string SetWeaponRarity(EItemRarity rarity)
    {
        Weapon weapon = GetComponent<ActorArsenal>().ActiveWeapon;
        if (weapon == null)
            throw new InvalidOperationException("Нет выбранного оружия.");
        bool changed = weapon.TryGetComponent(out WeaponEnchants enchants) ? enchants.SetRarity(rarity) : weapon.SetRarity(rarity);
        if (!changed)
            throw new InvalidOperationException("Нет оружия или в нём больше чар, чем допускает эта редкость.");
        return "Редкость: " + rarity;
    }

    [ConsoleCommand]
    private string ClearWeaponEnchants()
    {
        Weapon weapon = GetComponent<ActorArsenal>().ActiveWeapon;
        if (weapon == null || !weapon.TryGetComponent(out WeaponEnchants enchants))
            throw new InvalidOperationException("Нет выбранного оружия с поддержкой чар.");
        enchants.SetEnchants(Array.Empty<EnchantData>());
        return "Зачарования сняты.";
    }
}
