using UnityEngine;

[CreateAssetMenu(menuName = "Effects/Buffs/Sanity Restoration", fileName = "SanityRestoration")]
public sealed class SanityRestorationData : EffectData
{
    [Range(0f, 1f)] public float RestorePercent = 0.1f;
    public float RestorePercentDisplay => RestorePercent * 100f;

    public override Effect CreateEffect(GameObject owner)
    {
        SanityRestoration effect = owner.AddComponent<SanityRestoration>();
        effect.Initialize(this);
        return effect;
    }
}
