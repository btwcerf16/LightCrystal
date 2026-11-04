using UnityEngine;

[CreateAssetMenu(menuName = "Effects/Buffs/Sanity Shield", fileName = "SanityShield")]
public sealed class SanityShieldData : EffectData
{
    public EffectData BreakStun;

    public override Effect CreateEffect(GameObject owner)
    {
        SanityShield shield = owner.AddComponent<SanityShield>();
        shield.Initialize(this);
        return shield;
    }
}
