using UnityEngine;

[CreateAssetMenu(menuName = "Effects/Buffs/Nightmare Growth", fileName = "NightmareGrowth")]
public sealed class NightmareGrowthData : EffectData
{
    public float HealthBonus = 0.05f;
    public float BaseDamageBonus = 0.05f;
    public float PhysicalResistanceBonus = 0.01f;
    public float MagicResistanceBonus = 0.01f;
    public float HealthPercent => HealthBonus * 100f;
    public float BaseDamagePercent => BaseDamageBonus * 100f;
    public float PhysicalResistancePercent => PhysicalResistanceBonus * 100f;
    public float MagicResistancePercent => MagicResistanceBonus * 100f;

    public override Effect CreateEffect(GameObject owner)
    {
        NightmareGrowthEffect effect = owner.AddComponent<NightmareGrowthEffect>();
        effect.Initialize(this);
        return effect;
    }
}
