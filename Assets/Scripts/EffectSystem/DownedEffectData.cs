using UnityEngine;

[CreateAssetMenu(menuName = "Effects/Downed", fileName = "Downed")]
public sealed class DownedEffectData : EffectData
{
    [Min(0.1f)] public float ReviveRadius = 2f;
    [Min(0.1f)] public float ReviveTime = 10f;
    [Range(0.01f, 1f)] public float ReviveHealthFraction = 0.3f;

    public override Effect CreateEffect(GameObject owner)
    {
        Effect effect = owner.AddComponent<DownedEffect>();
        effect.Initialize(this);
        return effect;
    }
}
