using UnityEngine;

public sealed class StatModifier
{
    public float Value { get; }
    public StatModifierType Type { get; }
    public object Source { get; }

    public StatModifier(float value, StatModifierType type, object source)
    {
        Value = value;
        Type = type;
        Source = source;
    }
}
