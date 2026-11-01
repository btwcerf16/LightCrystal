using UnityEngine;

[CreateAssetMenu(menuName = "Effects/Status", fileName = "StatusEffect")]
public sealed class StatusEffectData : EffectData
{
    public override Effect CreateEffect(GameObject owner)
    {
        Effect effect = owner.AddComponent<StatusEffect>();
        effect.Initialize(this);
        return effect;
    }
}
