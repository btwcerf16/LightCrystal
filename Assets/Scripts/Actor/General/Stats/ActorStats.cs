using UnityEngine;

[DefaultExecutionOrder(-100)]
public sealed class ActorStats : MonoBehaviour
{
    [SerializeField] private ActorStatsConfig _config;

    public Stat ViewDistance { get; private set; }
    public Stat ViewAngle { get; private set; }

    public Stat MaxHealth { get; private set; }
    public Stat MaxSanity { get; private set; }
    public Stat HealthRegeneration { get; private set; }

    public Stat MoveSpeed { get; private set; }
    public Stat AttackSpeed { get; private set; }
    public Stat CooldownReduction { get; private set; }

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

        ViewDistance = new Stat(_config.ViewDistance, 0f);
        ViewAngle = new Stat(_config.ViewAngle, 0f, 360f);

        MaxHealth = new Stat(_config.MaxHealth, 1f);
        MaxSanity = new Stat(_config.MaxSanity, 0f);
        HealthRegeneration = new Stat(_config.HealthRegeneration, 0f);

        MoveSpeed = new Stat(_config.MoveSpeed, 0f);
        AttackSpeed = new Stat(_config.AttackSpeed, 0f);
        CooldownReduction = new Stat(_config.CooldownReduction, 0f, 0.9f);

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
}