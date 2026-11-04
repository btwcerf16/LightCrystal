using UnityEngine;

[CreateAssetMenu(menuName = "Effects/Buffs/Bloodlust", fileName = "Bloodlust")]
public sealed class BloodlustData : EffectData
{
    [Min(1)] public int MaxStacks = 5;
    public float MoveSpeedBonus = 0.1f;
    public float PhysicalDamageBonus = 0.06f;
    public float MoveSpeedPercent => MoveSpeedBonus * 100f;
    public float PhysicalDamagePercent => PhysicalDamageBonus * 100f;

    public override Effect CreateEffect(GameObject owner)
    {
        BloodlustEffect effect = owner.AddComponent<BloodlustEffect>();
        effect.Initialize(this);
        return effect;
    }
}
