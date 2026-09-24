using UnityEngine;
using System;
using System.Collections.Generic;


public sealed class Stat
{
    private readonly List<StatModifier> _modifiers = new();

    private readonly float _minimumValue;
    private readonly float _maximumValue;

    public event Action<float> ValueChanged;

    public float BaseValue { get; private set; }
    public float Value { get; private set; }

    public Stat(float baseValue, float minimumValue = float.NegativeInfinity, float maximumValue = float.PositiveInfinity)
    {
        _minimumValue = minimumValue;
        _maximumValue = maximumValue;

        BaseValue = baseValue;
        Value = CalculateValue();
    }

    public void SetBaseValue(float baseValue)
    {
        BaseValue = baseValue;

        RecalculateValue();
    }

    public void AddModifier(StatModifier modifier)
    {
        if (modifier == null)
            return;

        _modifiers.Add(modifier);

        RecalculateValue();
    }

    public bool RemoveModifier(StatModifier modifier)
    {
        if (!_modifiers.Remove(modifier))
            return false;

        RecalculateValue();

        return true;
    }

    public void RemoveModifiersFrom(object source)
    {
        if (source == null)
            return;

        int removedModifiers = _modifiers.RemoveAll(
            modifier => ReferenceEquals(modifier.Source, source));

        if (removedModifiers > 0)
            RecalculateValue();
    }

    public void ClearModifiers()
    {
        if (_modifiers.Count == 0)
            return;

        _modifiers.Clear();

        RecalculateValue();
    }

    private void RecalculateValue()
    {
        float calculatedValue = CalculateValue();

        if (calculatedValue == Value)
            return;

        Value = calculatedValue;
        ValueChanged?.Invoke(Value);
    }

    private float CalculateValue()
    {
        float flatBonus = 0f;
        float additivePercent = 0f;
        float multiplier = 1f;

        foreach (StatModifier modifier in _modifiers)
        {
            switch (modifier.Type)
            {
                case StatModifierType.Flat:
                    flatBonus += modifier.Value;
                    break;

                case StatModifierType.AdditivePercent:
                    additivePercent += modifier.Value;
                    break;

                case StatModifierType.Multiplier:
                    multiplier *= modifier.Value;
                    break;
            }
        }

        float calculatedValue = (BaseValue + flatBonus)
            * (1f + additivePercent)
            * multiplier;

        return Mathf.Clamp(calculatedValue, _minimumValue, _maximumValue);
    }
}
