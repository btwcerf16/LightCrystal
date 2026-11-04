using UnityEngine;

[CreateAssetMenu(menuName = "Effects/Buffs/Ritual")]
public sealed class RitualBuffData : EffectData
{
    public float SanityCostReduction;
    public float MadnessResistance;
    public float MaxSanityPenalty;
    public float RestoreSanityOnExpiry;
    public override Effect CreateEffect(GameObject owner)
    {
        RitualBuff effect = owner.AddComponent<RitualBuff>();
        effect.Initialize(this);
        return effect;
    }
}
