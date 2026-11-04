using UnityEngine;

[CreateAssetMenu(menuName = "Effects/Debuffs/Church Brand")]
public sealed class ChurchBrandData : EffectData
{
    public override Effect CreateEffect(GameObject owner)
    {
        ChurchBrand effect = owner.AddComponent<ChurchBrand>();
        effect.Initialize(this);
        return effect;
    }
}
