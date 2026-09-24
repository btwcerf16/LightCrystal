using UnityEngine;

[CreateAssetMenu(fileName = "ActorStats", menuName = "Light Crystal/Actors/Stats Config")]
public sealed class ActorStatsConfig : ScriptableObject
{
    [Header("Vision")]
    [field: SerializeField, Min(0f)] public float ViewDistance { get; private set; } = 8f;
    [field: SerializeField, Range(0f, 360f)] public float ViewAngle { get; private set; } = 90f;

    [Header("Resources")]
    [field: SerializeField, Min(1f)] public float MaxHealth { get; private set; } = 100f;
    [field: SerializeField, Min(0f)] public float MaxSanity { get; private set; } = 100f;
    [field: SerializeField, Min(0f)] public float HealthRegeneration { get; private set; }

    [Header("Movement and attacks")]
    [field: SerializeField, Min(0f)] public float MoveSpeed { get; private set; } = 5f;
    [field: SerializeField, Min(0f)] public float AttackSpeed { get; private set; } = 1f;
    [field: SerializeField, Range(0f, 0.9f)] public float CooldownReduction { get; private set; }

    [Header("Damage multipliers")]
    [field: SerializeField, Min(0f)] public float PhysicalDamageMultiplier { get; private set; } = 1f;
    [field: SerializeField, Min(0f)] public float MagicDamageMultiplier { get; private set; } = 1f;

    [Header("Life steal")]
    [field: SerializeField, Range(0f, 1f)] public float PhysicalLifeSteal { get; private set; }
    [field: SerializeField, Range(0f, 1f)] public float MagicLifeSteal { get; private set; }

    [Header("Resistances")]
    [field: SerializeField, Range(0f, 1f)] public float PhysicalResistance { get; private set; }
    [field: SerializeField, Range(0f, 1f)] public float MagicResistance { get; private set; }
    [field: SerializeField, Range(0f, 1f)] public float ForceResistance { get; private set; }
    [field: SerializeField, Range(0f, 1f)] public float MadnessResistance { get; private set; }
    [field: SerializeField, Range(0f, 1f)] public float EffectResistance { get; private set; }
}