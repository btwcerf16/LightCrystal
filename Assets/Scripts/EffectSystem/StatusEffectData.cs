using UnityEngine;

[CreateAssetMenu(menuName = "Effects/Status", fileName = "StatusEffect")]
public sealed class StatusEffectData : EffectData
{
    [Range(0f, 1f)] public float ViewDistanceMultiplier = 1f;

    public override Effect CreateEffect(GameObject owner)
    {
        Effect effect = owner.AddComponent<StatusEffect>();
        effect.Initialize(this);
        return effect;
    }
}
