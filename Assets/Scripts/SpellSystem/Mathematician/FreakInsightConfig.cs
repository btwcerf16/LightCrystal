using UnityEngine;

[CreateAssetMenu(menuName = "Spells/Mathematician/Freak Insight")]
public sealed class FreakInsightConfig : SpellConfig
{
    public EffectData Effect;
    public override Spell AddSpell(GameObject owner) => owner.AddComponent<FreakInsightSpell>();
}
