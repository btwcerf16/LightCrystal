using UnityEngine;
using System;

[DefaultExecutionOrder(-100)]
public sealed class ActorStats : MonoBehaviour
{
    [SerializeField] private ActorStatsConfig _config;

    public int TargetPriority { get; private set; }
    public event Action<int> TargetPriorityChanged;

    public Stat ViewDistance { get; private set; }
    public Stat ViewAngle { get; private set; }

    public Stat MaxHealth { get; private set; }
    public Stat MaxSanity { get; private set; }
    public Stat HealthRegeneration { get; private set; }
    public Stat ReviveSpeed { get; private set; }

    public Stat MoveSpeed { get; private set; }
    public Stat AttackSpeed { get; private set; }
    public Stat CooldownReduction { get; private set; }

    public Stat Stability { get; private set; }
    public Stat CameraDistance { get; private set; }
    public Stat CastSpeed { get; private set; }
    public Stat ProjectileSpeed { get; private set; }
    public Stat ProjectileSpeedMultiplier { get; private set; }
    public Stat ProjectileRange { get; private set; }
    public Stat ProjectilePiercing { get; private set; }
    public Stat SanityCostReduction { get; private set; }
    public Stat CriticalChance { get; private set; }
    public Stat CriticalDamageMultiplier { get; private set; }

    public Stat PhysicalDamage { get; private set; }
    public Stat PhysicalDamageMultiplier { get; private set; }
    public Stat MagicDamageMultiplier { get; private set; }

    public Stat PhysicalLifeSteal { get; private set; }
    public Stat MagicLifeSteal { get; private set; }

    public Stat PhysicalResistance { get; private set; }
    public Stat MagicResistance { get; private set; }
    public Stat ForceResistance { get; private set; }
    public Stat MadnessResistance { get; private set; }
    public Stat EffectResistance { get; private set; }


    private void Awake()
    {
        if (_config == null)
        {
            Debug.LogError($"ActorStatsConfig is not assigned on {name}.", this);
            enabled = false;
            return;
        }

        TargetPriority = _config.TargetPriority;
        ViewDistance = new Stat(_config.ViewDistance, 0f);
        ViewAngle = new Stat(_config.ViewAngle, 0f, 360f);

        MaxHealth = new Stat(_config.MaxHealth, 1f);
        MaxSanity = new Stat(_config.MaxSanity, 0f);
        HealthRegeneration = new Stat(_config.HealthRegeneration, 0f);
        ReviveSpeed = new Stat(_config.ReviveSpeed, 0f);

        MoveSpeed = new Stat(_config.MoveSpeed, 0f);
        AttackSpeed = new Stat(_config.AttackSpeed, 0f);
        CooldownReduction = new Stat(_config.CooldownReduction, 0f, 0.9f);

        Stability = new Stat(_config.Stability, 0f, 360f);
        CameraDistance = new Stat(_config.CameraDistance, 0.1f);
        CastSpeed = new Stat(_config.CastSpeed, 0.01f);
        ProjectileSpeed = new Stat(_config.ProjectileSpeed, 0f);
        ProjectileSpeedMultiplier = new Stat(_config.ProjectileSpeedMultiplier, 0f);
        ProjectileRange = new Stat(_config.ProjectileRange, 0f);
        ProjectilePiercing = new Stat(_config.ProjectilePiercing, 0f);
        SanityCostReduction = new Stat(_config.SanityCostReduction, 0f, 1f);
        CriticalChance = new Stat(_config.CriticalChance, 0f, 1f);
        CriticalDamageMultiplier = new Stat(_config.CriticalDamageMultiplier, 1f);

        PhysicalDamage = new Stat(_config.PhysicalDamage, 0f);
        PhysicalDamageMultiplier = new Stat(_config.PhysicalDamageMultiplier, 0f);
        MagicDamageMultiplier = new Stat(_config.MagicDamageMultiplier, 0f);

        PhysicalLifeSteal = new Stat(_config.PhysicalLifeSteal, 0f, 1f);
        MagicLifeSteal = new Stat(_config.MagicLifeSteal, 0f, 1f);

        PhysicalResistance = new Stat(_config.PhysicalResistance, 0f, 1f);
        MagicResistance = new Stat(_config.MagicResistance, 0f, 1f);
        ForceResistance = new Stat(_config.ForceResistance, 0f, 1f);
        MadnessResistance = new Stat(_config.MadnessResistance, 0f, 1f);
        EffectResistance = new Stat(_config.EffectResistance, 0f, 1f);
    }

    public void ApplyConfig(ActorStatsConfig config)
    {
        _config = config;
        SetTargetPriority(config.TargetPriority);
        ViewDistance.SetBaseValue(config.ViewDistance);
        ViewAngle.SetBaseValue(config.ViewAngle);
        MaxHealth.SetBaseValue(config.MaxHealth);
        MaxSanity.SetBaseValue(config.MaxSanity);
        HealthRegeneration.SetBaseValue(config.HealthRegeneration);
        ReviveSpeed.SetBaseValue(config.ReviveSpeed);
        MoveSpeed.SetBaseValue(config.MoveSpeed);
        AttackSpeed.SetBaseValue(config.AttackSpeed);
        CooldownReduction.SetBaseValue(config.CooldownReduction);
        Stability.SetBaseValue(config.Stability);
        CameraDistance.SetBaseValue(config.CameraDistance);
        CastSpeed.SetBaseValue(config.CastSpeed);
        ProjectileSpeed.SetBaseValue(config.ProjectileSpeed);
        ProjectileSpeedMultiplier.SetBaseValue(config.ProjectileSpeedMultiplier);
        ProjectileRange.SetBaseValue(config.ProjectileRange);
        ProjectilePiercing.SetBaseValue(config.ProjectilePiercing);
        SanityCostReduction.SetBaseValue(config.SanityCostReduction);
        CriticalChance.SetBaseValue(config.CriticalChance);
        CriticalDamageMultiplier.SetBaseValue(config.CriticalDamageMultiplier);
        PhysicalDamage.SetBaseValue(config.PhysicalDamage);
        PhysicalDamageMultiplier.SetBaseValue(config.PhysicalDamageMultiplier);
        MagicDamageMultiplier.SetBaseValue(config.MagicDamageMultiplier);
        PhysicalLifeSteal.SetBaseValue(config.PhysicalLifeSteal);
        MagicLifeSteal.SetBaseValue(config.MagicLifeSteal);
        PhysicalResistance.SetBaseValue(config.PhysicalResistance);
        MagicResistance.SetBaseValue(config.MagicResistance);
        ForceResistance.SetBaseValue(config.ForceResistance);
        MadnessResistance.SetBaseValue(config.MadnessResistance);
        EffectResistance.SetBaseValue(config.EffectResistance);
    }

    public void SetTargetPriority(int priority)
    {
        if (TargetPriority == priority)
            return;
        TargetPriority = priority;
        TargetPriorityChanged?.Invoke(priority);
    }
}
