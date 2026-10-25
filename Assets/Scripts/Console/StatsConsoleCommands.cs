using System;
using System.Reflection;
using UnityEngine;

[RequireComponent(typeof(ActorStats))]
[DisallowMultipleComponent]
public sealed class StatsConsoleCommands : MonoBehaviour
{
    private ActorStats _stats;

    private void Awake()
    {
        _stats = GetComponent<ActorStats>();
    }

    [ConsoleCommand]
    private string IncreaseStat([ConsoleStatName] string statName, float amount)
    {
        return ChangeStat(statName, amount, true);
    }

    [ConsoleCommand]
    private string DecreaseStat([ConsoleStatName] string statName, float amount)
    {
        return ChangeStat(statName, amount, false);
    }

    private string ChangeStat(string statName, float amount, bool isIncrease)
    {
        ValidateAmount(amount);
        PropertyInfo property = typeof(ActorStats).GetProperty(statName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
        if (property == null || property.PropertyType != typeof(Stat))
            throw new ArgumentException($"Стат {statName} не найден. Пример: AttackSpeed.");

        Stat stat = (Stat)property.GetValue(_stats);
        if (stat == null)
            throw new InvalidOperationException("Статы ещё не инициализированы.");

        float value = stat.BaseValue + (isIncrease ? amount : -amount);
        if (float.IsNaN(value) || float.IsInfinity(value))
            throw new ArgumentOutOfRangeException(nameof(amount), "Получилось слишком большое значение.");
        stat.SetBaseValue(value);
        return $"{property.Name}: база = {stat.BaseValue:0.##}, итог = {stat.Value:0.##}";
    }

    [ConsoleCommand]
    private string SetAttackSpeed(float value)
    {
        ValidateAmount(value);
        _stats.AttackSpeed.SetBaseValue(value);
        return "AttackSpeed = " + value;
    }

    [ConsoleCommand]
    private string SetTargetPriority(int value)
    {
        _stats.SetTargetPriority(value);
        return "TargetPriority = " + _stats.TargetPriority;
    }

    private void ValidateAmount(float amount)
    {
        if (amount < 0f || float.IsNaN(amount) || float.IsInfinity(amount))
            throw new ArgumentOutOfRangeException(nameof(amount), "Количество должно быть конечным неотрицательным числом.");
    }
}
