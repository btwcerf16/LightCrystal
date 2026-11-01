using UnityEngine;

[CreateAssetMenu(menuName = "Effects/Debuffs/Burning")]
public sealed class BurnEffectData : EffectData
{
    public override Effect CreateEffect(GameObject owner)
    {
        Effect effect = owner.AddComponent<BurnEffect>();
        effect.Initialize(this);
        return effect;
    }
}
