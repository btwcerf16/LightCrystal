using System;
using UnityEngine;

public sealed class CropItem : Item
{
    private enum CropStat
    {
        ViewDistance, MaxHealth, PhysicalResistance, ForceResistance, AttackSpeed, CriticalChance,
        ProjectileSpeedMultiplier, ProjectileRangeMultiplier, MaxSanity, MagicLifeSteal, CastSpeed,
        ReviveSpeed, MagicResistance, EffectResistance, MadnessResistance, MoveSpeed, PhysicalLifeSteal,
        PhysicalDamage, CriticalDamageMultiplier
    }

    [Serializable]
    private struct CropBonus
    {
        public CropStat Stat;
        public StatModifierType ModifierType;
        public float Amount;
    }

    [SerializeField] private string _displayName;
    [SerializeField] private CropBonus[] _bonuses;

    public override string DisplayName => _displayName;
    protected override bool CanUse() => !DestroyOnDrop;

    public override void Use()
    {
        if (!IsServer || DestroyOnDrop)
            return;
        DestroyOnDrop = true;
        foreach (CropBonus bonus in _bonuses)
        {
            Stat stat = bonus.Stat switch
            {
                CropStat.ViewDistance => _ownerStats.ViewDistance,
                CropStat.MaxHealth => _ownerStats.MaxHealth,
                CropStat.PhysicalResistance => _ownerStats.PhysicalResistance,
                CropStat.ForceResistance => _ownerStats.ForceResistance,
                CropStat.AttackSpeed => _ownerStats.AttackSpeed,
                CropStat.CriticalChance => _ownerStats.CriticalChance,
                CropStat.ProjectileSpeedMultiplier => _ownerStats.ProjectileSpeedMultiplier,
                CropStat.ProjectileRangeMultiplier => _ownerStats.ProjectileRangeMultiplier,
                CropStat.MaxSanity => _ownerStats.MaxSanity,
                CropStat.MagicLifeSteal => _ownerStats.MagicLifeSteal,
                CropStat.CastSpeed => _ownerStats.CastSpeed,
                CropStat.ReviveSpeed => _ownerStats.ReviveSpeed,
                CropStat.MagicResistance => _ownerStats.MagicResistance,
                CropStat.EffectResistance => _ownerStats.EffectResistance,
                CropStat.MadnessResistance => _ownerStats.MadnessResistance,
                CropStat.MoveSpeed => _ownerStats.MoveSpeed,
                CropStat.PhysicalLifeSteal => _ownerStats.PhysicalLifeSteal,
                CropStat.PhysicalDamage => _ownerStats.PhysicalDamage,
                CropStat.CriticalDamageMultiplier => _ownerStats.CriticalDamageMultiplier,
                _ => throw new ArgumentOutOfRangeException()
            };
            stat.AddModifier(new StatModifier(bonus.Amount, bonus.ModifierType, _ownerStats));
        }
    }
}
