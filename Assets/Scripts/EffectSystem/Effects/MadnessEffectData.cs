using UnityEngine;

[CreateAssetMenu(menuName = "Effects/Debuffs/Madness", fileName = "Madness")]
public sealed class MadnessEffectData : EffectData
{
    [Range(0f, 1f)] public float ViewDistancePenalty = 0.2f;
    [Range(0f, 1f)] public float PhysicalDamagePenalty = 0.1f;
    [Range(0f, 1f)] public float MagicAmplificationPenalty = 0.1f;
    [Range(0f, 1f)] public float ResistancePenalty = 0.2f;
    [Range(0f, 1f)] public float MoveSpeedPenalty = 0.3f;
    public float ViewDistancePercent => ViewDistancePenalty * 100f;
    public float PhysicalDamagePercent => PhysicalDamagePenalty * 100f;
    public float MagicAmplificationPercent => MagicAmplificationPenalty * 100f;
    public float ResistancePercent => ResistancePenalty * 100f;
    public float MoveSpeedPercent => MoveSpeedPenalty * 100f;

    public override Effect CreateEffect(GameObject owner)
    {
        MadnessEffect effect = owner.AddComponent<MadnessEffect>();
        effect.Initialize(this);
        return effect;
    }
}
