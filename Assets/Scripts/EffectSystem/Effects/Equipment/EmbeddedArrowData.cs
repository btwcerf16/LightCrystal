using UnityEngine;

[CreateAssetMenu(menuName = "Effects/Debuffs/Embedded Arrow")]
public sealed class EmbeddedArrowData : EffectData
{
    public override Effect CreateEffect(GameObject owner)
    {
        EmbeddedArrow effect = owner.AddComponent<EmbeddedArrow>();
        effect.Initialize(this);
        return effect;
    }
}
