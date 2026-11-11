using UnityEngine;

[CreateAssetMenu(menuName = "Effects/Debuffs/Burning")]
public sealed class BurnEffectData : EffectData
{
    [Min(0f)] public float DamagePerSecond;
    [Min(0f)] public float TickInterval;

    public override Effect CreateEffect(GameObject owner)
    {
        Effect effect = owner.AddComponent<BurnEffect>();
        effect.Initialize(this);
        return effect;
    }
}
