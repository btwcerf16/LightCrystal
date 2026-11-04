using UnityEngine;

[CreateAssetMenu(menuName = "Spells/Apply Effect")]
public sealed class EffectSpellConfig : SpellConfig
{
    public EffectData Effect;
    public override Spell AddSpell(GameObject owner) => owner.AddComponent<EffectSpell>();
}
