using UnityEngine;

[CreateAssetMenu(menuName = "Effects/Mathematician/Statistical Error")]
public sealed class StatisticalErrorEffectData : EffectData
{
    public override Effect CreateEffect(GameObject owner)
    {
        StatisticalErrorEffect effect = owner.AddComponent<StatisticalErrorEffect>();
        effect.Initialize(this);
        return effect;
    }
}
