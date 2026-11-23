using UnityEngine;

[CreateAssetMenu(menuName = "Effects/Buffs/Nyx Backstab", fileName = "NyxBackstabBuff")]
public sealed class NyxBackstabBuffData : EffectData
{
    public float CriticalChance = 0.1f;
    public float CriticalDamage = 0.05f;

    public override Effect CreateEffect(GameObject owner)
    {
        NyxBackstabBuff effect = owner.AddComponent<NyxBackstabBuff>();
        effect.Initialize(this);
        return effect;
    }
}
