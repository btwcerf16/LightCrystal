using UnityEngine;

[CreateAssetMenu(menuName = "Effects/Buffs/Heal", fileName = "HealEffect")]
public class HealBuffData : EffectData
{
    [Min(0f)] public float HealCount = 5f;
    public float TotalHeal => HealCount * EffectDuration;

    public override Effect CreateEffect(GameObject owner)
    {
        Effect effect = owner.AddComponent<HealBuff>();
        effect.Initialize(this);
        return effect;
    }
}
