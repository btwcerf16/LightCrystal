using UnityEngine;
[CreateAssetMenu(menuName = "Effects/Debuffs/Spider Web")]
public sealed class SpiderWebEffectData : EffectData
{
    [Range(0f, 1f)] public float Slow = 0.05f;
    public float SlowPercent => Slow * 100f;
    public override Effect CreateEffect(GameObject owner)
    {
        SpiderWebEffect effect = owner.AddComponent<SpiderWebEffect>();
        effect.Initialize(this);
        return effect;
    }
}
