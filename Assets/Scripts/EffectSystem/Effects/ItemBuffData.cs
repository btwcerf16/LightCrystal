using UnityEngine;

[CreateAssetMenu(menuName = "Effects/Buffs/Item Buff", fileName = "ItemBuff")]
public sealed class ItemBuffData : EffectData
{
    public float AttackSpeed;
    public float CastSpeedBonus;
    public float MoveSpeed;
    public float CriticalDamage;
    public float CriticalChance;
    public float CameraDistance;
    public float MadnessResistance;
    public float ViewDistance;
    public float ViewAngleBonus;
    public float ViewAngleMultiplier = 1f;
    public float MoveSpeedMultiplier = 1f;
    public float ProjectileSpeed;
    public float ProjectileSpeedBonus;
    public float ProjectileSpeedMultiplier = 1f;
    public float ProjectileRange;
    public float ProjectileRangeMultiplier = 1f;
    public float ProjectilePiercing;
    public float CriticalChancePercent => CriticalChance * 100f;
    public float CameraDistancePercent => CameraDistance * 100f;
    public float MadnessResistancePercent => MadnessResistance * 100f;

    public override Effect CreateEffect(GameObject owner)
    {
        ItemBuff effect = owner.AddComponent<ItemBuff>();
        effect.Initialize(this);
        return effect;
    }
}
