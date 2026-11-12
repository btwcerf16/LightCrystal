using UnityEngine;

[CreateAssetMenu(menuName = "Effects/Debuffs/Boss Fight")]
public sealed class BossFightData : EffectData
{
    [Range(0f, 0.95f)] public float ReviveSlow = 0.5f;
    public float ReviveSlowPercent => ReviveSlow * 100f;

    public override Effect CreateEffect(GameObject owner)
    {
        BossFightEffect effect = owner.AddComponent<BossFightEffect>();
        effect.Initialize(this);
        return effect;
    }
}
