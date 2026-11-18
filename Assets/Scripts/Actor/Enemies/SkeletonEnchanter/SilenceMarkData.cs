using UnityEngine;

[CreateAssetMenu(menuName = "Effects/Silence Mark")]
public sealed class SilenceMarkData : EffectData
{
    public override Effect CreateEffect(GameObject owner)
    {
        var mark = owner.AddComponent<SilenceMarkEffect>();
        mark.Initialize(this);
        return mark;
    }
}
