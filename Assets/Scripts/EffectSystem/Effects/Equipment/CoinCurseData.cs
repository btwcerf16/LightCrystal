using UnityEngine;

[CreateAssetMenu(menuName = "Effects/Debuffs/Three Sided Coin")]
public sealed class CoinCurseData : EffectData
{
    public ECoinCurse Curse;
    public float HealthPerSecond = 0.02f;
    public float SanityPerSecond = 4f;
    public float MissingSanityPerSecond = 0.05f;
    public float MoveSpeedPenalty = 0.4f;
    public override Effect CreateEffect(GameObject owner)
    {
        CoinCurse effect = owner.AddComponent<CoinCurse>();
        effect.Initialize(this);
        return effect;
    }
}
